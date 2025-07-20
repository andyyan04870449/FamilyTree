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

        public FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            
            // 設定上傳目錄
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "user_upload");
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
                _logger.LogInformation("建立上傳目錄: {UploadDirectory}", _uploadDirectory);
            }
        }

        public async Task<FileUploadResponse> UploadFileAsync(IFormFile file)
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
                
                // 檢查是否為重複檔案
                var existingFile = await GetFileByMd5Async(md5Hash);
                if (existingFile != null)
                {
                    return new FileUploadResponse
                    {
                        Success = false,
                        Message = "檔案已存在，無法重複上傳",
                        IsDuplicate = true,
                        FileInfo = existingFile
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
                    Status = "uploaded"
                };

                var savedFile = await SaveFileToDatabaseAsync(fileInfo);

                _logger.LogInformation("檔案上傳成功: {OriginalFilename} -> {Filename}", file.FileName, fileName);

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

        public async Task<FileListResponse> GetFileListAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"SELECT id, filename, original_filename, file_path, file_size, md5_hash, 
                                  upload_time, is_merged, merge_time, status, created_at, updated_at 
                           FROM user_update_file 
                           ORDER BY upload_time DESC";

                var files = await connection.QueryAsync<dynamic>(sql);
                var fileList = new List<FileUploadModel>();
                
                foreach (var file in files)
                {
                    fileList.Add(new FileUploadModel
                    {
                        Id = file.id,
                        Filename = file.filename,
                        OriginalFilename = file.original_filename,
                        FilePath = file.file_path,
                        FileSize = file.file_size,
                        Md5Hash = file.md5_hash,
                        UploadTime = file.upload_time,
                        IsMerged = file.is_merged,
                        MergeTime = file.merge_time,
                        Status = file.status,
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

        public async Task<FileUploadResponse> DeleteFileAsync(int fileId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 取得檔案資訊
                var file = await connection.QueryFirstOrDefaultAsync<FileUploadModel>(
                    "SELECT * FROM user_update_file WHERE id = @id", new { id = fileId });

                if (file == null)
                {
                    return new FileUploadResponse
                    {
                        Success = false,
                        Message = "檔案不存在"
                    };
                }

                // 刪除實體檔案
                if (File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                }

                // 刪除資料庫記錄
                await connection.ExecuteAsync(
                    "DELETE FROM user_update_file WHERE id = @id", new { id = fileId });

                _logger.LogInformation("檔案刪除成功: {Filename}", file.Filename);

                return new FileUploadResponse
                {
                    Success = true,
                    Message = "檔案刪除成功"
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

        private async Task<FileUploadModel?> GetFileByMd5Async(string md5Hash)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            return await connection.QueryFirstOrDefaultAsync<FileUploadModel>(
                "SELECT * FROM user_update_file WHERE md5_hash = @md5Hash",
                new { md5Hash });
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

            var sql = @"INSERT INTO user_update_file (filename, original_filename, file_path, file_size, md5_hash, upload_time, status) 
                       VALUES (@Filename, @OriginalFilename, @FilePath, @FileSize, @Md5Hash, @UploadTime, @Status) 
                       RETURNING id, filename, original_filename, file_path, file_size, md5_hash, upload_time, is_merged, merge_time, status, created_at, updated_at";

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