// 檔案上傳服務 - 處理檔案操作、MD5計算、本地儲存等邏輯
using System.Security.Cryptography;
using System.Text;
using familytree_backend.Models;
using Dapper;
using Npgsql;

namespace familytree_backend.Services
{
    public class FileUploadService
    {
        private readonly string _connectionString;
        private readonly string _uploadDirectory;
        private readonly ILogger<FileUploadService> _logger;

        private readonly ExcelProcessingService _excelProcessingService;

        public FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger, ExcelProcessingService excelProcessingService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            _excelProcessingService = excelProcessingService;
            
            // 設定上傳目錄
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "user_upload");
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
                _logger.LogInformation("建立上傳目錄: {UploadDirectory}", _uploadDirectory);
            }
        }

        public async Task<FileUploadResponse> UploadFileAsync(IFormFile file, string? projectId = null)
        {
            try
            {
                // 驗證檔案類型
                if (!IsValidFileType(file))
                {
                    return new FileUploadResponse
                    {
                        Success = false,
                        Message = "只支援 .xls 和 .xlsx 檔案格式"
                    };
                }

                // 計算 MD5
                var md5Hash = await CalculateMd5Async(file);
                
                // 檢查是否為重複檔案（只在同專案內檢查）
                var existingFile = await GetFileByMd5Async(md5Hash, projectId);
                if (existingFile != null)
                {
                    return new FileUploadResponse
                    {
                        Success = true,
                        Message = "檔案已存在，使用現有檔案",
                        IsDuplicate = true,
                        FileInfo = existingFile,
                        FilePath = existingFile.FilePath
                    };
                }

                // 生成唯一檔名
                var fileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(_uploadDirectory, fileName);

                // 儲存檔案
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 儲存到資料庫
                var fileInfo = new FileUploadModel
                {
                    Filename = fileName,
                    OriginalFilename = file.FileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    Md5Hash = md5Hash,
                    UploadTime = DateTime.UtcNow,
                    Status = "uploaded",
                    ProjectId = projectId
                };

                var savedFile = await SaveFileToDatabaseAsync(fileInfo);

                _logger.LogInformation("檔案上傳成功: {OriginalFilename} -> {Filename}", file.FileName, fileName);

                // 自動處理Excel檔案
                try
                {
                    _logger.LogInformation("開始處理Excel檔案: {FilePath}", filePath);
                    var processingResult = await _excelProcessingService.ProcessExcelFileAsync(filePath, md5Hash, projectId);
                    
                    if (processingResult.Success)
                    {
                        _logger.LogInformation("Excel檔案處理成功: 成功處理 {SuccessRows} 行，失敗 {FailedRows} 行", 
                            processingResult.SuccessRows, processingResult.FailedRows);
                    }
                    else
                    {
                        _logger.LogWarning("Excel檔案處理失敗: {Message}", processingResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "處理Excel檔案時發生錯誤: {FilePath}", filePath);
                }

                return new FileUploadResponse
                {
                    Success = true,
                    Message = "檔案上傳成功",
                    FileInfo = savedFile,
                    IsDuplicate = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案上傳失敗: {FileName}", file.FileName);
                return new FileUploadResponse
                {
                    Success = false,
                    Message = $"檔案上傳失敗: {ex.Message}"
                };
            }
        }

        public async Task<FileListResponse> GetFileListAsync(string? projectId = null)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"SELECT id, filename, original_filename, file_path, file_size, md5_hash, 
                                  upload_time, is_merged, merge_time, status, created_at, updated_at, project_id
                           FROM user_update_file 
                           WHERE (@projectId IS NULL OR project_id = @projectId)
                           ORDER BY upload_time DESC";

                var files = await connection.QueryAsync<dynamic>(sql, new { projectId });
                var fileList = new List<FileModel>();
                
                foreach (var file in files)
                {
                    fileList.Add(new FileModel
                    {
                        FileId = Guid.NewGuid(), // 舊數據可能沒有 GUID
                        UserId = "system", // 舊數據可能沒有用戶 ID
                        Filename = file.filename,
                        OriginalFilename = file.original_filename,
                        FilePath = file.file_path,
                        FileSize = file.file_size,
                        Md5Hash = file.md5_hash,
                        FileType = Path.GetExtension(file.original_filename),
                        UploadStatus = file.status,
                        IsProcessed = file.is_merged,
                        ProcessedAt = file.merge_time,
                        AssociatedRecordId = file.project_id,
                        AssociatedRecordType = "project",
                        UploadedAt = file.upload_time,
                        CreatedAt = file.created_at,
                        UpdatedAt = file.updated_at
                    });
                }

                return new FileListResponse
                {
                    Success = true,
                    Message = "取得檔案列表成功",
                    Files = fileList,
                    TotalCount = fileList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得檔案列表失敗");
                return new FileListResponse
                {
                    Success = false,
                    Message = $"取得檔案列表失敗: {ex.Message}"
                };
            }
        }

        public async Task<FileUploadResponse> ProcessFileAsync(Guid fileId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 取得檔案資訊
                var file = await connection.QueryFirstOrDefaultAsync<FileUploadModel>(
                    @"SELECT id, filename, original_filename as OriginalFilename, file_path as FilePath, 
                             file_size as FileSize, md5_hash as Md5Hash, upload_time as UploadTime, 
                             is_merged as IsMerged, merge_time as MergeTime, status, project_id as ProjectId,
                             created_at as CreatedAt, updated_at as UpdatedAt 
                      FROM user_update_file WHERE id = @id", new { id = fileId });

                if (file == null)
                {
                    _logger.LogWarning("找不到檔案ID: {FileId}", fileId);
                    return new FileUploadResponse
                    {
                        Success = false,
                        Message = "檔案不存在"
                    };
                }

                _logger.LogInformation("取得檔案資訊: ID={FileId}, FileName={FileName}, FilePath={FilePath}, Status={Status}", 
                    file.Id, file.Filename, file.FilePath, file.Status);

                if (file.Status == "merged")
                {
                    return new FileUploadResponse
                    {
                        Success = true,
                        Message = "檔案已經處理過"
                    };
                }

                if (string.IsNullOrWhiteSpace(file.FilePath))
                {
                    _logger.LogError("檔案路徑為空: FileId={FileId}", fileId);
                    return new FileUploadResponse
                    {
                        Success = false,
                        Message = "檔案路徑無效"
                    };
                }

                // 處理Excel檔案
                _logger.LogInformation("開始處理Excel檔案: {FilePath}", file.FilePath);
                var processingResult = await _excelProcessingService.ProcessExcelFileAsync(file.FilePath, file.Md5Hash, file.ProjectId);
                
                if (processingResult.Success)
                {
                    _logger.LogInformation("Excel檔案處理成功: 成功處理 {SuccessRows} 行，失敗 {FailedRows} 行", 
                        processingResult.SuccessRows, processingResult.FailedRows);
                    
                    return new FileUploadResponse
                    {
                        Success = true,
                        Message = $"檔案處理成功，成功處理 {processingResult.SuccessRows} 行，失敗 {processingResult.FailedRows} 行"
                    };
                }
                else
                {
                    _logger.LogWarning("Excel檔案處理失敗: {Message}", processingResult.Message);
                    return new FileUploadResponse
                    {
                        Success = false,
                        Message = $"檔案處理失敗: {processingResult.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案處理失敗: {FileId}", fileId);
                return new FileUploadResponse
                {
                    Success = false,
                    Message = $"檔案處理失敗: {ex.Message}"
                };
            }
        }

        public async Task<FileUploadResponse> DeleteFileAsync(Guid fileId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 取得檔案資訊
                var file = await connection.QueryFirstOrDefaultAsync<FileUploadModel>(
                    @"SELECT id, filename, original_filename as OriginalFilename, file_path as FilePath, 
                             file_size as FileSize, md5_hash as Md5Hash, upload_time as UploadTime, 
                             is_merged as IsMerged, merge_time as MergeTime, status, 
                             created_at as CreatedAt, updated_at as UpdatedAt 
                      FROM user_update_file WHERE id = @id", new { id = fileId });

                if (file == null)
                {
                    return new FileUploadResponse
                    {
                        Success = false,
                        Message = "檔案不存在"
                    };
                }

                // 檢查會影響的人員資料數量
                var personDataCount = await connection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM person_profile WHERE file_md5 = @md5", new { md5 = file.Md5Hash });

                _logger.LogInformation("準備刪除檔案: {Filename}, 將同時刪除 {PersonCount} 筆相關人員資料", 
                    file.Filename, personDataCount);

                // 刪除相關人員資料
                if (personDataCount > 0)
                {
                    var deletedPersons = await connection.ExecuteAsync(
                        "DELETE FROM person_profile WHERE file_md5 = @md5", new { md5 = file.Md5Hash });
                    
                    _logger.LogInformation("已刪除 {DeletedCount} 筆人員資料", deletedPersons);
                }

                // 刪除實體檔案
                if (File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                }

                // 刪除資料庫記錄
                await connection.ExecuteAsync(
                    "DELETE FROM user_update_file WHERE id = @id", new { id = fileId });

                _logger.LogInformation("檔案刪除成功: {Filename}, 同時刪除了 {PersonCount} 筆人員資料", 
                    file.Filename, personDataCount);

                return new FileUploadResponse
                {
                    Success = true,
                    Message = personDataCount > 0 
                        ? $"檔案刪除成功，同時刪除了 {personDataCount} 筆相關人員資料" 
                        : "檔案刪除成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案刪除失敗: {FileId}", fileId);
                return new FileUploadResponse
                {
                    Success = false,
                    Message = $"檔案刪除失敗: {ex.Message}"
                };
            }
        }

        public async Task<DeleteImpactResponse> GetDeleteImpactAsync(Guid fileId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 取得檔案資訊
                var file = await connection.QueryFirstOrDefaultAsync<FileUploadModel>(
                    @"SELECT id, filename, original_filename as OriginalFilename, file_path as FilePath, 
                             file_size as FileSize, md5_hash as Md5Hash, upload_time as UploadTime, 
                             is_merged as IsMerged, merge_time as MergeTime, status, 
                             created_at as CreatedAt, updated_at as UpdatedAt 
              FROM user_update_file WHERE id = @id", new { id = fileId });

                if (file == null)
                {
                    _logger.LogWarning("找不到檔案ID: {FileId}", fileId);
                    return new DeleteImpactResponse
                    {
                        Success = false,
                        Message = "檔案不存在",
                        PersonCount = 0,
                        PersonNames = new List<string>(),
                        FileName = "",
                        HasMorePersons = false
                    };
                }

                // 查詢與此檔案相關的人員資料
                var sql = @"SELECT id, name, gender, nationality 
                   FROM person_profile 
                   WHERE file_md5 = @md5Hash";

                var affectedPersons = await connection.QueryAsync<PersonDataModel>(sql, new { md5Hash = file.Md5Hash });
                var personCount = affectedPersons.Count();

                _logger.LogInformation("分析刪除影響: 檔案={FileName}, 影響人數={Count}", 
                    file.OriginalFilename, personCount);

                // 取得人員名稱列表，最多顯示10個
                var maxDisplayNames = 10;
                var personNames = affectedPersons.Take(maxDisplayNames).Select(p => p.Name ?? "").Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
                var hasMorePersons = personCount > maxDisplayNames;

                var message = personCount > 0 
                    ? $"刪除檔案 '{file.OriginalFilename}' 將同時刪除 {personCount} 筆人員資料"
                    : $"刪除檔案 '{file.OriginalFilename}' 不會影響任何人員資料";

                return new DeleteImpactResponse
                {
                    Success = true,
                    Message = message,
                    PersonCount = personCount,
                    PersonNames = personNames,
                    FileName = file.OriginalFilename,
                    HasMorePersons = hasMorePersons
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析刪除影響時發生錯誤: {FileId}", fileId);
                return new DeleteImpactResponse
                {
                    Success = false,
                    Message = $"分析刪除影響時發生錯誤: {ex.Message}",
                    PersonCount = 0,
                    PersonNames = new List<string>(),
                    FileName = "",
                    HasMorePersons = false
                };
            }
        }

        private bool IsValidFileType(IFormFile file)
        {
            var allowedExtensions = new[] { ".xls", ".xlsx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(fileExtension);
        }

        private async Task<string> CalculateMd5Async(IFormFile file)
        {
            using var md5 = MD5.Create();
            using var stream = file.OpenReadStream();
            var hash = await md5.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private async Task<FileUploadModel?> GetFileByMd5Async(string md5Hash, string? projectId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 如果有專案ID，只在該專案內檢查重複；否則全局檢查
            var sql = projectId != null 
                ? "SELECT * FROM user_update_file WHERE md5_hash = @md5Hash AND project_id = @projectId"
                : "SELECT * FROM user_update_file WHERE md5_hash = @md5Hash";

            return await connection.QueryFirstOrDefaultAsync<FileUploadModel>(sql, new { md5Hash, projectId });
        }

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var random = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            return $"{fileNameWithoutExtension}_{timestamp}_{random}{extension}";
        }

        private async Task<FileUploadModel> SaveFileToDatabaseAsync(FileUploadModel fileInfo)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"INSERT INTO user_update_file (filename, original_filename, file_path, file_size, md5_hash, upload_time, status, project_id) 
                       VALUES (@Filename, @OriginalFilename, @FilePath, @FileSize, @Md5Hash, @UploadTime, @Status, @ProjectId) 
                       RETURNING id, filename, original_filename, file_path, file_size, md5_hash, upload_time, is_merged, merge_time, status, created_at, updated_at, project_id";

            var result = await connection.QueryFirstAsync<FileUploadModel>(sql, fileInfo);
            
            // 確保返回的物件包含所有正確的資料
            result.OriginalFilename = fileInfo.OriginalFilename;
            result.FilePath = fileInfo.FilePath;
            result.FileSize = fileInfo.FileSize;
            result.Md5Hash = fileInfo.Md5Hash;
            result.UploadTime = fileInfo.UploadTime;
            
            return result;
        }
    }
} 