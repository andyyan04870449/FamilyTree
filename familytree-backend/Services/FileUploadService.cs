// 檔案上傳服務 - 使用新的 file_uploads 表和標準化命名
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using familytree_backend.Models;
using familytree_backend.Constants;
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
        private readonly IConfiguration _configuration;

        public FileUploadService(
            IConfiguration configuration, 
            ILogger<FileUploadService> logger, 
            ExcelProcessingService excelProcessingService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            _excelProcessingService = excelProcessingService;
            _configuration = configuration;
            
            // 設定上傳目錄
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), 
                configuration.GetValue<string>("FileUpload:UploadDirectory") ?? ApplicationConstants.Files.UploadDirectoryName);
                
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
                _logger.LogInformation("建立上傳目錄: {UploadDirectory}", _uploadDirectory);
            }
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        public async Task<FileOperationResult> UploadFileAsync(
            IFormFile file, 
            string userId, 
            string? associatedRecordId = null,
            string? associatedRecordType = null)
        {
            var uploadId = Guid.NewGuid().ToString("N")[..8]; // 生成8位追蹤ID
            _logger.LogInformation("🚀 [檔案上傳-{UploadId}] 開始處理檔案上傳 - 檔案: {FileName}, 大小: {FileSize} bytes, 用戶: {UserId}, 專案: {ProjectId}", 
                uploadId, file.FileName, file.Length, userId, associatedRecordId ?? "無");
            
            try
            {
                // 驗證檔案類型
                _logger.LogInformation("📋 [檔案上傳-{UploadId}] 開始驗證檔案類型", uploadId);
                var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions")
                    .Get<string[]>() ?? ApplicationConstants.Files.AllowedExtensions;
                    
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                _logger.LogInformation("🔍 [檔案上傳-{UploadId}] 檔案副檔名: {Extension}, 允許的類型: {AllowedTypes}", 
                    uploadId, fileExtension, string.Join(", ", allowedExtensions));
                
                if (!ApplicationConstants.Files.IsValidFileType(file.FileName, file.ContentType))
                {
                    _logger.LogWarning("❌ [檔案上傳-{UploadId}] 檔案類型不被支援: {Extension}", uploadId, fileExtension);
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = ApplicationConstants.ApiResponse.ErrorMessages.FileTypeNotSupported
                    };
                }

                // 計算 MD5
                _logger.LogInformation("🔐 [檔案上傳-{UploadId}] 開始計算檔案 MD5 雜湊值", uploadId);
                var md5Hash = await CalculateMd5Async(file);
                _logger.LogInformation("✅ [檔案上傳-{UploadId}] MD5 計算完成: {MD5Hash}", uploadId, md5Hash);
                
                // 檢查是否為重複檔案（同一用戶、同一專案、同一檔案）
                _logger.LogInformation("🔍 [檔案上傳-{UploadId}] 檢查檔案是否在此專案中重複", uploadId);
                var existingFile = await GetFileByMd5AndProjectAsync(md5Hash, userId, associatedRecordId);
                if (existingFile != null)
                {
                    _logger.LogInformation("♻️ [檔案上傳-{UploadId}] 檔案已存在於此專案中 - FileId: {FileId}, 使用現有檔案", 
                        uploadId, existingFile.FileId);
                    return new FileOperationResult
                    {
                        Success = true,
                        Message = "檔案已存在於此專案中，使用現有檔案",
                        IsDuplicate = true,
                        File = existingFile,
                        FilePath = existingFile.FilePath
                    };
                }

                _logger.LogInformation("🆕 [檔案上傳-{UploadId}] 檔案在此專案中為新檔案，開始處理", uploadId);

                // 生成唯一檔名
                var fileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(_uploadDirectory, fileName);
                _logger.LogInformation("📁 [檔案上傳-{UploadId}] 生成檔案路徑: {FilePath}", uploadId, filePath);

                // 儲存檔案
                _logger.LogInformation("💾 [檔案上傳-{UploadId}] 開始儲存實體檔案", uploadId);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("✅ [檔案上傳-{UploadId}] 實體檔案儲存完成", uploadId);

                // 準備檔案模型
                _logger.LogInformation("📝 [檔案上傳-{UploadId}] 準備檔案資料模型", uploadId);
                var fileModel = new FileModel
                {
                    FileId = Guid.NewGuid(),
                    UserId = userId,
                    Filename = fileName,
                    OriginalFilename = file.FileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    Md5Hash = md5Hash,
                    FileType = ApplicationConstants.Files.GetFileTypeByExtension(file.FileName),
                    MimeType = ApplicationConstants.Files.GetMimeTypeByExtension(file.FileName),
                    AssociatedRecordId = associatedRecordId,
                    AssociatedRecordType = associatedRecordType,
                    UploadStatus = ApplicationConstants.Files.Status.Uploaded,
                    UploadedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("🔤 [檔案上傳-{UploadId}] 檔案模型詳情 - FileId: {FileId}, 類型: {FileType}, MIME: {MimeType}", 
                    uploadId, fileModel.FileId, fileModel.FileType, fileModel.MimeType);

                // 儲存到資料庫
                _logger.LogInformation("💾 [檔案上傳-{UploadId}] 開始儲存檔案記錄到資料庫", uploadId);
                var savedFile = await SaveFileToDatabaseAsync(fileModel);
                _logger.LogInformation("✅ [檔案上傳-{UploadId}] 檔案記錄儲存完成", uploadId);

                _logger.LogInformation("🎉 [檔案上傳-{UploadId}] 檔案上傳成功 - 原檔名: {OriginalFilename}, 儲存檔名: {Filename}, FileId: {FileId}", 
                    uploadId, file.FileName, fileName, savedFile.FileId);

                // 如果是Excel檔案，自動處理
                if (fileModel.IsProcessable())
                {
                    _logger.LogInformation("📊 [檔案上傳-{UploadId}] 檔案可處理，開始背景 Excel 處理任務", uploadId);
                    _ = Task.Run(async () => await ProcessExcelFileInternalAsync(savedFile));
                }
                else
                {
                    _logger.LogInformation("📄 [檔案上傳-{UploadId}] 檔案不需要處理（非Excel檔案）", uploadId);
                }

                return new FileOperationResult
                {
                    Success = true,
                    Message = "檔案上傳成功",
                    File = savedFile,
                    IsDuplicate = false,
                    FilePath = filePath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [檔案上傳-{UploadId}] 檔案上傳過程發生異常 - 檔案: {FileName}, 錯誤: {ErrorMessage}", 
                    uploadId, file.FileName, ex.Message);
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"檔案上傳失敗: {ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// 獲取使用者的檔案列表
        /// </summary>
        public async Task<FileListResponse> GetUserFilesAsync(string userId, FileQueryOptions options)
        {
            try
            {
                _logger.LogInformation($"🔍 [FileUploadService] 開始查詢檔案列表 - 用戶: {userId}");
                _logger.LogInformation($"📋 [FileUploadService] 查詢選項 - AssociatedRecordId: {options.AssociatedRecordId}, AssociatedRecordType: {options.AssociatedRecordType}, FileType: {options.FileType}");
                
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 構建查詢條件 - 排除已刪除的檔案
                var whereConditions = new List<string> { "user_id = @userId", "upload_status != @deletedStatus" };
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);
                parameters.Add("deletedStatus", ApplicationConstants.Files.Status.Deleted);
                
                _logger.LogInformation($"🔧 [FileUploadService] 基本查詢條件 - user_id = {userId}, upload_status != {ApplicationConstants.Files.Status.Deleted}");

                if (!string.IsNullOrEmpty(options.FileType))
                {
                    whereConditions.Add("file_type = @fileType");
                    parameters.Add("fileType", options.FileType);
                }

                if (!string.IsNullOrEmpty(options.Status))
                {
                    whereConditions.Add("upload_status = @status");
                    parameters.Add("status", options.Status);
                }

                if (!string.IsNullOrEmpty(options.AssociatedRecordType))
                {
                    whereConditions.Add("associated_record_type = @recordType");
                    parameters.Add("recordType", options.AssociatedRecordType);
                }

                if (!string.IsNullOrEmpty(options.AssociatedRecordId))
                {
                    whereConditions.Add("associated_record_id = @recordId");
                    parameters.Add("recordId", options.AssociatedRecordId);
                    _logger.LogInformation($"➕ [FileUploadService] 添加專案ID條件 - associated_record_id = {options.AssociatedRecordId}");
                }

                var whereClause = string.Join(" AND ", whereConditions);
                _logger.LogInformation($"🔍 [FileUploadService] 最終查詢條件: {whereClause}");

                // 查詢總數
                var countSql = $"SELECT COUNT(*) FROM file_uploads WHERE {whereClause}";
                _logger.LogInformation($"📊 [FileUploadService] 執行計數查詢: {countSql}");
                var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);
                _logger.LogInformation($"📈 [FileUploadService] 查詢到總數: {totalCount}");

                // 查詢資料
                var orderBy = options.SortDescending ? "DESC" : "ASC";
                var offset = (options.Page - 1) * options.PageSize;
                
                // 將 PascalCase 屬性名稱轉換為 snake_case 資料庫欄位名稱
                var sortColumn = ConvertPropertyToColumnName(options.SortBy);
                _logger.LogInformation($"🔄 [FileUploadService] 排序欄位轉換: {options.SortBy} -> {sortColumn}");
                
                var dataSql = $@"
                    SELECT f.file_id as FileId, f.user_id as UserId, f.filename, f.original_filename as OriginalFilename,
                           f.file_path as FilePath, f.file_size as FileSize, f.md5_hash as Md5Hash,
                           f.file_type as FileType, f.mime_type as MimeType,
                           f.associated_record_id as AssociatedRecordId, 
                           f.associated_record_type as AssociatedRecordType,
                           f.upload_status as UploadStatus, f.is_processed as IsProcessed,
                           f.processed_at as ProcessedAt, f.uploaded_at as UploadedAt,
                           f.created_at as CreatedAt, f.updated_at as UpdatedAt,
                           COALESCE(p.person_count, 0) as RelatedPersonsCount
                    FROM file_uploads f
                    LEFT JOIN (
                        SELECT file_md5, COUNT(*) as person_count
                        FROM person_profile
                        WHERE file_md5 IS NOT NULL
                        GROUP BY file_md5
                    ) p ON f.md5_hash = p.file_md5
                    WHERE {whereClause}
                    ORDER BY f.{sortColumn} {orderBy}
                    LIMIT @limit OFFSET @offset";

                parameters.Add("limit", options.PageSize);
                parameters.Add("offset", offset);

                var files = await connection.QueryAsync<FileModel>(dataSql, parameters);

                return new FileListResponse
                {
                    Success = true,
                    Message = "成功獲取檔案列表",
                    Files = files.ToList(),
                    TotalCount = totalCount,
                    Page = options.Page,
                    PageSize = options.PageSize,
                    HasMore = totalCount > offset + options.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取檔案列表失敗");
                return new FileListResponse
                {
                    Success = false,
                    Message = $"獲取檔案列表失敗: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 根據ID獲取檔案
        /// </summary>
        public async Task<FileModel?> GetFileByIdAsync(Guid fileId, string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT f.file_id as FileId, f.user_id as UserId, f.filename, f.original_filename as OriginalFilename,
                           f.file_path as FilePath, f.file_size as FileSize, f.md5_hash as Md5Hash,
                           f.file_type as FileType, f.mime_type as MimeType,
                           f.associated_record_id as AssociatedRecordId, 
                           f.associated_record_type as AssociatedRecordType,
                           f.upload_status as UploadStatus, f.is_processed as IsProcessed,
                           f.processed_at as ProcessedAt, f.uploaded_at as UploadedAt,
                           f.created_at as CreatedAt, f.updated_at as UpdatedAt,
                           COALESCE(p.person_count, 0) as RelatedPersonsCount
                    FROM file_uploads f
                    LEFT JOIN (
                        SELECT file_md5, COUNT(*) as person_count
                        FROM person_profile
                        WHERE file_md5 IS NOT NULL
                        GROUP BY file_md5
                    ) p ON f.md5_hash = p.file_md5
                    WHERE f.file_id = @fileId AND f.user_id = @userId";

                return await connection.QueryFirstOrDefaultAsync<FileModel>(sql, new { fileId, userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取檔案失敗: {FileId}", fileId);
                return null;
            }
        }

        /// <summary>
        /// 獲取檔案以供下載
        /// </summary>
        public async Task<FileDownloadResult> GetFileForDownloadAsync(Guid fileId, string userId)
        {
            try
            {
                var file = await GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return new FileDownloadResult
                    {
                        Success = false,
                        Message = "找不到指定的檔案"
                    };
                }

                if (!File.Exists(file.FilePath))
                {
                    return new FileDownloadResult
                    {
                        Success = false,
                        Message = "檔案不存在於伺服器"
                    };
                }

                var fileContent = await File.ReadAllBytesAsync(file.FilePath);
                
                return new FileDownloadResult
                {
                    Success = true,
                    FileContent = fileContent,
                    FileName = file.OriginalFilename,
                    MimeType = file.MimeType ?? "application/octet-stream"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下載檔案失敗: {FileId}", fileId);
                return new FileDownloadResult
                {
                    Success = false,
                    Message = $"下載檔案失敗: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 刪除檔案
        /// </summary>
        public async Task<FileOperationResult> DeleteFileAsync(Guid fileId, string userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("開始刪除檔案: FileId={FileId}, UserId={UserId}", fileId, userId);

                var file = await GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    _logger.LogWarning("找不到要刪除的檔案: FileId={FileId}, UserId={UserId}", fileId, userId);
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "找不到指定的檔案"
                    };
                }

                _logger.LogInformation("找到要刪除的檔案: {FileName}, 路徑: {FilePath}", file.OriginalFilename, file.FilePath);

                // 刪除實體檔案
                var physicalFileDeleted = false;
                if (File.Exists(file.FilePath))
                {
                    try
                    {
                        File.Delete(file.FilePath);
                        physicalFileDeleted = true;
                        _logger.LogInformation("實體檔案刪除成功: {FilePath}", file.FilePath);
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogError(fileEx, "刪除實體檔案失敗: {FilePath}", file.FilePath);
                        // 即使實體檔案刪除失敗，仍然繼續更新資料庫狀態
                    }
                }
                else
                {
                    _logger.LogWarning("實體檔案不存在: {FilePath}", file.FilePath);
                    physicalFileDeleted = true; // 檔案不存在視為已刪除
                }

                // 更新資料庫狀態為已刪除（軟刪除）
                var sql = @"
                    UPDATE file_uploads 
                    SET upload_status = @status, updated_at = @updatedAt
                    WHERE file_id = @fileId AND user_id = @userId";

                var rowsAffected = await connection.ExecuteAsync(sql, new 
                { 
                    status = ApplicationConstants.Files.Status.Deleted,
                    updatedAt = DateTime.UtcNow,
                    fileId, 
                    userId 
                }, transaction);

                if (rowsAffected == 0)
                {
                    _logger.LogWarning("資料庫更新失敗，沒有找到匹配的記錄: FileId={FileId}, UserId={UserId}", fileId, userId);
                    await transaction.RollbackAsync();
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "無法更新檔案狀態，可能檔案不存在或無權限"
                    };
                }

                await transaction.CommitAsync();

                var successMessage = physicalFileDeleted 
                    ? "檔案和資料記錄刪除成功" 
                    : "資料記錄已標記為刪除（實體檔案刪除時發生錯誤）";

                _logger.LogInformation("檔案刪除操作完成: {FileName}, FileId={FileId}, 實體檔案刪除={PhysicalDeleted}", 
                    file.OriginalFilename, fileId, physicalFileDeleted);

                return new FileOperationResult
                {
                    Success = true,
                    Message = successMessage
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "檔案刪除過程發生異常: FileId={FileId}, UserId={UserId}", fileId, userId);
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"檔案刪除失敗: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 分析刪除影響
        /// </summary>
        public async Task<DeleteImpactResult> AnalyzeDeleteImpactAsync(Guid fileId, string userId)
        {
            try
            {
                var file = await GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return new DeleteImpactResult
                    {
                        Success = false,
                        Message = "找不到指定的檔案"
                    };
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 查詢受影響的記錄
                var affectedCount = 0;
                var affectedNames = new List<string>();

                if (file.IsProcessable())
                {
                    // 如果是Excel檔案，查詢相關的人員資料
                    var sql = @"
                        SELECT COUNT(*) FROM person_profile 
                        WHERE file_md5 = @md5Hash";
                    
                    affectedCount = await connection.QuerySingleAsync<int>(sql, new { md5Hash = file.Md5Hash });

                    if (affectedCount > 0)
                    {
                        var namesSql = @"
                            SELECT name FROM person_profile 
                            WHERE file_md5 = @md5Hash 
                            LIMIT 10";
                        
                        var names = await connection.QueryAsync<string>(namesSql, new { md5Hash = file.Md5Hash });
                        affectedNames = names.Where(n => !string.IsNullOrEmpty(n)).ToList();
                    }
                }

                return new DeleteImpactResult
                {
                    Success = true,
                    Message = affectedCount > 0 
                        ? $"刪除此檔案將影響 {affectedCount} 筆相關記錄" 
                        : "刪除此檔案不會影響其他記錄",
                    AffectedRecords = affectedCount,
                    AffectedRecordNames = affectedNames,
                    CanDelete = true,
                    FileName = file.OriginalFilename
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析刪除影響失敗: {FileId}", fileId);
                return new DeleteImpactResult
                {
                    Success = false,
                    Message = $"分析失敗: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 更新檔案關聯
        /// </summary>
        public async Task<FileOperationResult> UpdateFileAssociationAsync(
            Guid fileId, 
            string userId, 
            string recordId, 
            string recordType)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE file_uploads 
                    SET associated_record_id = @recordId, 
                        associated_record_type = @recordType,
                        updated_at = @updatedAt
                    WHERE file_id = @fileId AND user_id = @userId";

                var affected = await connection.ExecuteAsync(sql, new 
                { 
                    recordId, 
                    recordType, 
                    updatedAt = DateTime.UtcNow,
                    fileId, 
                    userId 
                });

                if (affected == 0)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "找不到指定的檔案或無權限更新"
                    };
                }

                return new FileOperationResult
                {
                    Success = true,
                    Message = "成功更新檔案關聯"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新檔案關聯失敗: {FileId}", fileId);
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"更新失敗: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 獲取檔案統計資訊
        /// </summary>
        public async Task<FileStatistics> GetFileStatisticsAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        COUNT(*) as TotalFiles,
                        COALESCE(SUM(file_size), 0) as TotalSizeBytes,
                        MIN(uploaded_at) as OldestFileDate,
                        MAX(uploaded_at) as NewestFileDate
                    FROM file_uploads 
                    WHERE user_id = @userId AND upload_status != 'deleted'";

                var stats = await connection.QuerySingleAsync<FileStatistics>(sql, new { userId });

                // 按類型統計
                var typeSql = @"
                    SELECT file_type, COUNT(*) as count
                    FROM file_uploads 
                    WHERE user_id = @userId AND upload_status != 'deleted'
                    GROUP BY file_type";

                var typeStats = await connection.QueryAsync<(string type, int count)>(typeSql, new { userId });
                stats.FilesByType = typeStats.ToDictionary(x => x.type ?? "unknown", x => x.count);

                // 按狀態統計
                var statusSql = @"
                    SELECT upload_status, COUNT(*) as count
                    FROM file_uploads 
                    WHERE user_id = @userId
                    GROUP BY upload_status";

                var statusStats = await connection.QueryAsync<(string status, int count)>(statusSql, new { userId });
                stats.FilesByStatus = statusStats.ToDictionary(x => x.status, x => x.count);

                // 按關聯類型統計
                var assocSql = @"
                    SELECT associated_record_type, COUNT(*) as count
                    FROM file_uploads 
                    WHERE user_id = @userId AND upload_status != 'deleted' AND associated_record_type IS NOT NULL
                    GROUP BY associated_record_type";

                var assocStats = await connection.QueryAsync<(string type, int count)>(assocSql, new { userId });
                stats.FilesByAssociationType = assocStats.ToDictionary(x => x.type, x => x.count);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取檔案統計失敗");
                return new FileStatistics();
            }
        }

        /// <summary>
        /// 檢查檔案是否重複（全局檢查，不限專案）
        /// </summary>
        public async Task<bool> CheckDuplicateAsync(string md5Hash, string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT COUNT(*) 
                    FROM file_uploads 
                    WHERE md5_hash = @md5Hash AND user_id = @userId AND upload_status != 'deleted'";

                var count = await connection.QuerySingleAsync<int>(sql, new { md5Hash, userId });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查檔案重複失敗");
                return false;
            }
        }

        /// <summary>
        /// 檢查檔案在特定專案中是否重複
        /// </summary>
        public async Task<bool> CheckDuplicateInProjectAsync(string md5Hash, string userId, string? projectId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT COUNT(*) 
                    FROM file_uploads 
                    WHERE md5_hash = @md5Hash 
                        AND user_id = @userId 
                        AND associated_record_id = @projectId 
                        AND upload_status != 'deleted'";

                var count = await connection.QuerySingleAsync<int>(sql, new { md5Hash, userId, projectId });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查專案檔案重複失敗");
                return false;
            }
        }

        /// <summary>
        /// 處理Excel檔案
        /// </summary>
        public async Task<ProcessResult> ProcessExcelFileAsync(Guid fileId, string userId)
        {
            try
            {
                var file = await GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return new ProcessResult
                    {
                        Success = false,
                        Message = "找不到指定的檔案"
                    };
                }

                if (!file.IsProcessable())
                {
                    return new ProcessResult
                    {
                        Success = false,
                        Message = "此檔案類型不支援處理"
                    };
                }

                if (file.IsProcessed)
                {
                    return new ProcessResult
                    {
                        Success = true,
                        Message = "檔案已經處理過"
                    };
                }

                return await ProcessExcelFileInternalAsync(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理Excel檔案失敗: {FileId}", fileId);
                return new ProcessResult
                {
                    Success = false,
                    Message = $"處理失敗: {ex.Message}"
                };
            }
        }

        // ========== 私有方法 ==========

        private async Task<ProcessResult> ProcessExcelFileInternalAsync(FileModel file)
        {
            var processId = Guid.NewGuid().ToString("N")[..8]; // 生成8位處理追蹤ID
            _logger.LogInformation("🔄 [Excel處理-{ProcessId}] 開始處理Excel檔案 - FileId: {FileId}, 檔案: {FileName}, 專案: {ProjectId}", 
                processId, file.FileId, file.OriginalFilename, file.AssociatedRecordId ?? "無");
                
            try
            {
                // 更新狀態為處理中
                _logger.LogInformation("📝 [Excel處理-{ProcessId}] 更新檔案狀態為處理中", processId);
                await UpdateFileStatusAsync(file.FileId, ApplicationConstants.Files.Status.Processing);

                // 呼叫Excel處理服務
                _logger.LogInformation("📊 [Excel處理-{ProcessId}] 開始呼叫 ExcelProcessingService", processId);
                var startTime = DateTime.UtcNow;
                var result = await _excelProcessingService.ProcessExcelFileAsync(
                    file.FilePath, 
                    file.Md5Hash, 
                    file.AssociatedRecordId);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("⏱️ [Excel處理-{ProcessId}] ExcelProcessingService 處理完成，耗時: {Duration} 秒", 
                    processId, duration.TotalSeconds);

                if (result.Success)
                {
                    _logger.LogInformation("✅ [Excel處理-{ProcessId}] Excel處理成功 - 成功: {SuccessCount} 行, 失敗: {FailureCount} 行, 總計: {TotalRecords} 筆", 
                        processId, result.SuccessCount, result.FailureCount, result.TotalRecords);
                        
                    // 更新狀態為已處理
                    _logger.LogInformation("📝 [Excel處理-{ProcessId}] 更新檔案狀態為已處理", processId);
                    await UpdateFileStatusAsync(file.FileId, ApplicationConstants.Files.Status.Processed, true, DateTime.UtcNow);

                    _logger.LogInformation("🎉 [Excel處理-{ProcessId}] Excel檔案處理完全成功 - FileId: {FileId}, 處理時間: {Duration} 秒", 
                        processId, file.FileId, duration.TotalSeconds);

                    return new ProcessResult
                    {
                        Success = true,
                        Message = $"檔案處理成功，成功 {result.SuccessCount} 行，失敗 {result.FailureCount} 行",
                        ProcessedRows = result.SuccessCount,
                        TotalRows = result.TotalRecords,
                        ProcessingDuration = duration
                    };
                }
                else
                {
                    _logger.LogError("❌ [Excel處理-{ProcessId}] Excel處理失敗 - 錯誤訊息: {ErrorMessage}", 
                        processId, result.Message);
                    if (result.Errors != null && result.Errors.Any())
                    {
                        _logger.LogError("❌ [Excel處理-{ProcessId}] 詳細錯誤: {Errors}", 
                            processId, string.Join("; ", result.Errors));
                    }
                        
                    // 更新狀態為失敗
                    _logger.LogInformation("📝 [Excel處理-{ProcessId}] 更新檔案狀態為失敗", processId);
                    await UpdateFileStatusAsync(file.FileId, ApplicationConstants.Files.Status.Failed);

                    return new ProcessResult
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [Excel處理-{ProcessId}] Excel處理過程發生異常 - FileId: {FileId}, 錯誤: {ErrorMessage}", 
                    processId, file.FileId, ex.Message);
                await UpdateFileStatusAsync(file.FileId, ApplicationConstants.Files.Status.Failed);
                throw;
            }
        }

        private async Task UpdateFileStatusAsync(Guid fileId, string status, bool isProcessed = false, DateTime? processedAt = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                UPDATE file_uploads 
                SET upload_status = @status, 
                    is_processed = @isProcessed,
                    processed_at = @processedAt,
                    updated_at = @updatedAt
                WHERE file_id = @fileId";

            await connection.ExecuteAsync(sql, new 
            { 
                status, 
                isProcessed,
                processedAt,
                updatedAt = DateTime.UtcNow,
                fileId 
            });
        }

        private async Task<FileModel?> GetFileByMd5Async(string md5Hash, string userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT f.file_id as FileId, f.user_id as UserId, f.filename, f.original_filename as OriginalFilename,
                       f.file_path as FilePath, f.file_size as FileSize, f.md5_hash as Md5Hash,
                       f.file_type as FileType, f.mime_type as MimeType,
                       f.associated_record_id as AssociatedRecordId, 
                       f.associated_record_type as AssociatedRecordType,
                       f.upload_status as UploadStatus, f.is_processed as IsProcessed,
                       f.processed_at as ProcessedAt, f.uploaded_at as UploadedAt,
                       f.created_at as CreatedAt, f.updated_at as UpdatedAt,
                       COALESCE(p.person_count, 0) as RelatedPersonsCount
                FROM file_uploads f
                LEFT JOIN (
                    SELECT file_md5, COUNT(*) as person_count
                    FROM person_profile
                    WHERE file_md5 IS NOT NULL
                    GROUP BY file_md5
                ) p ON f.md5_hash = p.file_md5
                WHERE f.md5_hash = @md5Hash AND f.user_id = @userId AND f.upload_status != 'deleted'
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<FileModel>(sql, new { md5Hash, userId });
        }

        /// <summary>
        /// 檢查檔案是否在特定專案中已存在（按MD5、用戶ID、專案ID檢查）
        /// </summary>
        private async Task<FileModel?> GetFileByMd5AndProjectAsync(string md5Hash, string userId, string? projectId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT f.file_id as FileId, f.user_id as UserId, f.filename, f.original_filename as OriginalFilename,
                       f.file_path as FilePath, f.file_size as FileSize, f.md5_hash as Md5Hash,
                       f.file_type as FileType, f.mime_type as MimeType,
                       f.associated_record_id as AssociatedRecordId, 
                       f.associated_record_type as AssociatedRecordType,
                       f.upload_status as UploadStatus, f.is_processed as IsProcessed,
                       f.processed_at as ProcessedAt, f.uploaded_at as UploadedAt,
                       f.created_at as CreatedAt, f.updated_at as UpdatedAt,
                       COALESCE(p.person_count, 0) as RelatedPersonsCount
                FROM file_uploads f
                LEFT JOIN (
                    SELECT file_md5, COUNT(*) as person_count
                    FROM person_profile
                    WHERE file_md5 IS NOT NULL
                    GROUP BY file_md5
                ) p ON f.md5_hash = p.file_md5
                WHERE f.md5_hash = @md5Hash 
                    AND f.user_id = @userId 
                    AND f.associated_record_id = @projectId 
                    AND f.upload_status != 'deleted'
                LIMIT 1";

            return await connection.QueryFirstOrDefaultAsync<FileModel>(sql, new { md5Hash, userId, projectId });
        }

        private async Task<FileModel> SaveFileToDatabaseAsync(FileModel file)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO file_uploads (
                    file_id, user_id, filename, original_filename, file_path, 
                    file_size, md5_hash, file_type, mime_type,
                    associated_record_id, associated_record_type,
                    upload_status, is_processed, processed_at,
                    uploaded_at, created_at, updated_at
                ) VALUES (
                    @FileId, @UserId, @Filename, @OriginalFilename, @FilePath,
                    @FileSize, @Md5Hash, @FileType, @MimeType,
                    @AssociatedRecordId, @AssociatedRecordType,
                    @UploadStatus, @IsProcessed, @ProcessedAt,
                    @UploadedAt, @CreatedAt, @UpdatedAt
                ) RETURNING 
                    file_id as FileId, user_id as UserId, filename, original_filename as OriginalFilename,
                    file_path as FilePath, file_size as FileSize, md5_hash as Md5Hash,
                    file_type as FileType, mime_type as MimeType,
                    associated_record_id as AssociatedRecordId, 
                    associated_record_type as AssociatedRecordType,
                    upload_status as UploadStatus, is_processed as IsProcessed,
                    processed_at as ProcessedAt, uploaded_at as UploadedAt,
                    created_at as CreatedAt, updated_at as UpdatedAt,
                    0 as RelatedPersonsCount";

            var savedFile = await connection.QuerySingleAsync<FileModel>(sql, file);
            return savedFile;
        }

        private async Task<string> CalculateMd5Async(IFormFile file)
        {
            using var md5 = MD5.Create();
            using var stream = file.OpenReadStream();
            var hash = await md5.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var random = Guid.NewGuid().ToString("N").Substring(0, 8);
            
            return $"{fileNameWithoutExtension}_{timestamp}_{random}{extension}";
        }


        /// <summary>
        /// 將 PascalCase 屬性名稱轉換為 snake_case 資料庫欄位名稱
        /// </summary>
        private string ConvertPropertyToColumnName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return "uploaded_at"; // 預設排序欄位
                
            // 特定屬性名稱的映射
            var columnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "FileId", "file_id" },
                { "UserId", "user_id" },
                { "Filename", "filename" },
                { "OriginalFilename", "original_filename" },
                { "FilePath", "file_path" },
                { "FileSize", "file_size" },
                { "Md5Hash", "md5_hash" },
                { "FileType", "file_type" },
                { "MimeType", "mime_type" },
                { "AssociatedRecordId", "associated_record_id" },
                { "AssociatedRecordType", "associated_record_type" },
                { "UploadStatus", "upload_status" },
                { "IsProcessed", "is_processed" },
                { "ProcessedAt", "processed_at" },
                { "UploadedAt", "uploaded_at" },
                { "CreatedAt", "created_at" },
                { "UpdatedAt", "updated_at" }
            };
            
            // 如果有明確的映射就使用
            if (columnMappings.TryGetValue(propertyName, out var columnName))
            {
                return columnName;
            }
            
            // 否則進行通用的 PascalCase 到 snake_case 轉換
            return Regex.Replace(propertyName, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
        }
    }

    // ========== 結果模型 ==========

    public class FileDownloadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public byte[]? FileContent { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
    }

    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedRows { get; set; }
        public int TotalRows { get; set; }
        public TimeSpan ProcessingDuration { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}