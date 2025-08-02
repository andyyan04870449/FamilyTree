// 檔案服務 - 使用 file_id 標準化架構的檔案管理服務
using System.Security.Cryptography;
using familytree_backend.Models;
using Microsoft.Extensions.Caching.Memory;

namespace familytree_backend.Services
{
    /// <summary>
    /// 檔案服務 - 以檔案為中心的統一檔案管理
    /// 設計理念：提供完整的檔案生命週期管理，支援彈性關聯機制
    /// </summary>
    public interface IFileService
    {
        // 核心檔案操作
        Task<FileOperationResult> UploadFileAsync(IFormFile file, string userId, string? associatedRecordId = null, string? associatedRecordType = null);
        Task<FileModel?> GetFileAsync(Guid fileId, string userId);
        Task<List<FileModel>> GetUserFilesAsync(string userId, FileQueryOptions? options = null);
        Task<List<FileModel>> GetAssociatedFilesAsync(string associatedRecordId, string associatedRecordType, string userId);
        Task<FileOperationResult> DeleteFileAsync(Guid fileId, string userId);
        
        // 檔案處理
        Task<FileOperationResult> ProcessFileAsync(Guid fileId, string userId);
        Task<FileOperationResult> UpdateFileStatusAsync(Guid fileId, string status, string userId);
        
        // 檔案查詢和統計
        Task<FileStatistics> GetFileStatisticsAsync(string userId);
        Task<DeleteImpactResult> GetDeleteImpactAsync(Guid fileId, string userId);
        
        // 檔案關聯管理
        Task<FileOperationResult> AssociateFileAsync(Guid fileId, string recordId, string recordType, string userId);
        Task<FileOperationResult> DisassociateFileAsync(Guid fileId, string userId);
        
        // 檔案驗證和工具
        Task<string> CalculateMd5Async(IFormFile file);
        Task<bool> IsFileExistsAsync(string md5Hash, string userId, string? associatedRecordId = null);
        string GenerateUniqueFileName(string originalFileName);
    }

    /// <summary>
    /// 檔案服務實作
    /// </summary>
    public class FileService : IFileService
    {
        private readonly IDataAccessServiceV2 _dataAccessService;
        private readonly ILogger<FileService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ExcelProcessingService _excelProcessingService;
        private readonly string _uploadDirectory;

        /// <summary>
        /// 檔案服務建構子
        /// </summary>
        public FileService(
            IDataAccessServiceV2 dataAccessService,
            ILogger<FileService> logger,
            IConfiguration configuration,
            IMemoryCache cache,
            ExcelProcessingService excelProcessingService)
        {
            _dataAccessService = dataAccessService ?? throw new ArgumentNullException(nameof(dataAccessService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _excelProcessingService = excelProcessingService ?? throw new ArgumentNullException(nameof(excelProcessingService));

            // 設定上傳目錄
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "user_upload");
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
                _logger.LogInformation("建立上傳目錄: {UploadDirectory}", _uploadDirectory);
            }
        }

        // ==================== 核心檔案操作 ====================

        /// <summary>
        /// 上傳檔案
        /// </summary>
        public async Task<FileOperationResult> UploadFileAsync(IFormFile file, string userId, string? associatedRecordId = null, string? associatedRecordType = null)
        {
            try
            {
                _logger.LogInformation("開始檔案上傳：使用者 {UserId}，檔案 {FileName}，關聯 {RecordType}:{RecordId}", 
                    userId, file.FileName, associatedRecordType, associatedRecordId);

                // 步驟 1：驗證檔案
                var validationResult = ValidateFile(file);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                // 步驟 2：計算檔案雜湊
                var md5Hash = await CalculateMd5Async(file);

                // 步驟 3：檢查重複檔案
                var existingFile = await CheckDuplicateFileAsync(md5Hash, userId, associatedRecordId);
                if (existingFile != null)
                {
                    _logger.LogWarning("檔案重複：{FileName}，MD5: {Md5Hash}", file.FileName, md5Hash);
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "檔案已存在，無法重複上傳",
                        IsDuplicate = true,
                        File = existingFile
                    };
                }

                // 步驟 4：儲存檔案到磁碟
                var fileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(_uploadDirectory, fileName);
                await SaveFileToDiskAsync(file, filePath);

                // 步驟 5：建立檔案記錄
                var fileModel = new FileModel
                {
                    FileId = Guid.NewGuid(),
                    UserId = userId,
                    Filename = fileName,
                    OriginalFilename = file.FileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    Md5Hash = md5Hash,
                    FileType = DetermineFileType(file.FileName),
                    MimeType = file.ContentType,
                    AssociatedRecordId = associatedRecordId,
                    AssociatedRecordType = associatedRecordType,
                    UploadStatus = FileUploadStatus.Uploaded,
                    UploadedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 步驟 6：儲存到資料庫
                var savedFile = await _dataAccessService.CreateFileRecordAsync(fileModel);

                _logger.LogInformation("檔案上傳成功：ID {FileId}，檔案 {FileName}", savedFile.FileId, savedFile.OriginalFilename);

                // 步驟 7：自動處理 Excel 檔案
                if (fileModel.IsProcessable())
                {
                    _ = Task.Run(async () => await ProcessFileInBackground(savedFile));
                }

                // 清除快取
                ClearUserFileCache(userId);

                return new FileOperationResult
                {
                    Success = true,
                    Message = "檔案上傳成功",
                    File = savedFile,
                    FilePath = filePath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案上傳失敗：使用者 {UserId}，檔案 {FileName}", userId, file.FileName);
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"檔案上傳失敗：{ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// 取得檔案
        /// </summary>
        public async Task<FileModel?> GetFileAsync(Guid fileId, string userId)
        {
            try
            {
                var file = await _dataAccessService.GetFileByIdAsync(fileId, userId);
                
                if (file != null)
                {
                    _logger.LogDebug("成功取得檔案：ID {FileId}，使用者 {UserId}", fileId, userId);
                }
                else
                {
                    _logger.LogWarning("檔案不存在或無權限：ID {FileId}，使用者 {UserId}", fileId, userId);
                }

                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得檔案失敗：ID {FileId}，使用者 {UserId}", fileId, userId);
                return null;
            }
        }

        /// <summary>
        /// 取得使用者檔案列表
        /// </summary>
        public async Task<List<FileModel>> GetUserFilesAsync(string userId, FileQueryOptions? options = null)
        {
            try
            {
                options ??= new FileQueryOptions();
                
                // 檢查快取
                var cacheKey = $"user_files_{userId}_{options.Page}_{options.PageSize}_{options.FileType}_{options.Status}";
                if (_cache.TryGetValue(cacheKey, out List<FileModel>? cachedFiles))
                {
                    _logger.LogDebug("從快取取得使用者檔案：使用者 {UserId}，數量 {Count}", userId, cachedFiles?.Count ?? 0);
                    return cachedFiles ?? new List<FileModel>();
                }

                var files = await _dataAccessService.GetUserFilesAsync(userId, options);
                
                // 快取 5 分鐘
                _cache.Set(cacheKey, files, TimeSpan.FromMinutes(5));
                
                _logger.LogInformation("取得使用者檔案列表：使用者 {UserId}，數量 {Count}", userId, files.Count);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者檔案列表失敗：使用者 {UserId}", userId);
                return new List<FileModel>();
            }
        }

        /// <summary>
        /// 取得關聯檔案
        /// </summary>
        public async Task<List<FileModel>> GetAssociatedFilesAsync(string associatedRecordId, string associatedRecordType, string userId)
        {
            try
            {
                var files = await _dataAccessService.GetAssociatedFilesAsync(associatedRecordId, associatedRecordType, userId);
                
                _logger.LogInformation("取得關聯檔案：{RecordType}:{RecordId}，使用者 {UserId}，數量 {Count}", 
                    associatedRecordType, associatedRecordId, userId, files.Count);
                
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得關聯檔案失敗：{RecordType}:{RecordId}，使用者 {UserId}", 
                    associatedRecordType, associatedRecordId, userId);
                return new List<FileModel>();
            }
        }

        /// <summary>
        /// 刪除檔案
        /// </summary>
        public async Task<FileOperationResult> DeleteFileAsync(Guid fileId, string userId)
        {
            try
            {
                _logger.LogInformation("開始刪除檔案：ID {FileId}，使用者 {UserId}", fileId, userId);

                // 取得檔案資訊
                var file = await _dataAccessService.GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "檔案不存在或無權限存取"
                    };
                }

                // 分析刪除影響
                var impact = await GetDeleteImpactAsync(fileId, userId);
                if (!impact.CanDelete)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = impact.Warning ?? "檔案無法刪除",
                        Errors = new List<string> { impact.Message }
                    };
                }

                // 刪除實體檔案
                if (File.Exists(file.FilePath))
                {
                    File.Delete(file.FilePath);
                    _logger.LogInformation("已刪除實體檔案：{FilePath}", file.FilePath);
                }

                // 刪除資料庫記錄
                var deleted = await _dataAccessService.DeleteFileAsync(fileId, userId);
                if (!deleted)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "刪除檔案記錄失敗"
                    };
                }

                // 清除快取
                ClearUserFileCache(userId);

                _logger.LogInformation("檔案刪除成功：ID {FileId}，檔案 {FileName}，影響 {Count} 筆記錄", 
                    fileId, file.OriginalFilename, impact.AffectedRecords);

                return new FileOperationResult
                {
                    Success = true,
                    Message = impact.AffectedRecords > 0 
                        ? $"檔案刪除成功，同時刪除了 {impact.AffectedRecords} 筆相關記錄"
                        : "檔案刪除成功",
                    File = file
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除檔案失敗：ID {FileId}，使用者 {UserId}", fileId, userId);
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"刪除檔案失敗：{ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // ==================== 檔案處理 ====================

        /// <summary>
        /// 處理檔案 (主要針對 Excel 檔案)
        /// </summary>
        public async Task<FileOperationResult> ProcessFileAsync(Guid fileId, string userId)
        {
            try
            {
                _logger.LogInformation("開始處理檔案：ID {FileId}，使用者 {UserId}", fileId, userId);

                var file = await _dataAccessService.GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "檔案不存在或無權限存取"
                    };
                }

                if (file.IsProcessed)
                {
                    return new FileOperationResult
                    {
                        Success = true,
                        Message = "檔案已經處理過",
                        File = file
                    };
                }

                if (!file.IsProcessable())
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "此檔案類型無法處理"
                    };
                }

                // 更新狀態為處理中
                await UpdateFileStatusAsync(fileId, FileUploadStatus.Processing, userId);

                // 處理 Excel 檔案
                var processingResult = await _excelProcessingService.ProcessExcelFileAsync(
                    file.FilePath, 
                    file.Md5Hash, 
                    file.AssociatedRecordId // 原本的 project_id 參數
                );

                // 更新處理結果
                var newStatus = processingResult.Success ? FileUploadStatus.Processed : FileUploadStatus.Failed;
                await UpdateFileStatusAsync(fileId, newStatus, userId);

                if (processingResult.Success)
                {
                    // 更新處理時間
                    file.IsProcessed = true;
                    file.ProcessedAt = DateTime.UtcNow;
                    await _dataAccessService.UpdateFileAsync(file);
                }

                _logger.LogInformation("檔案處理完成：ID {FileId}，成功 {Success}，結果 {Message}", 
                    fileId, processingResult.Success, processingResult.Message);

                return new FileOperationResult
                {
                    Success = processingResult.Success,
                    Message = processingResult.Message,
                    File = file
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理檔案失敗：ID {FileId}，使用者 {UserId}", fileId, userId);
                
                // 標記為失敗
                await UpdateFileStatusAsync(fileId, FileUploadStatus.Failed, userId);

                return new FileOperationResult
                {
                    Success = false,
                    Message = $"處理檔案失敗：{ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// 更新檔案狀態
        /// </summary>
        public async Task<FileOperationResult> UpdateFileStatusAsync(Guid fileId, string status, string userId)
        {
            try
            {
                var file = await _dataAccessService.GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "檔案不存在或無權限存取"
                    };
                }

                file.UploadStatus = status;
                file.UpdatedAt = DateTime.UtcNow;

                var updated = await _dataAccessService.UpdateFileAsync(file);
                if (!updated)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "更新檔案狀態失敗"
                    };
                }

                // 清除快取
                ClearUserFileCache(userId);

                _logger.LogDebug("檔案狀態更新成功：ID {FileId}，狀態 {Status}", fileId, status);

                return new FileOperationResult
                {
                    Success = true,
                    Message = "檔案狀態更新成功",
                    File = file
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新檔案狀態失敗：ID {FileId}，使用者 {UserId}", fileId, userId);
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"更新檔案狀態失敗：{ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // ==================== 檔案統計和分析 ====================

        /// <summary>
        /// 取得檔案統計資訊
        /// </summary>
        public async Task<FileStatistics> GetFileStatisticsAsync(string userId)
        {
            try
            {
                // 檢查快取
                var cacheKey = $"file_stats_{userId}";
                if (_cache.TryGetValue(cacheKey, out FileStatistics? cachedStats))
                {
                    return cachedStats!;
                }

                var stats = await _dataAccessService.GetFileStatisticsAsync(userId);
                
                // 快取 10 分鐘
                _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(10));
                
                _logger.LogDebug("取得檔案統計：使用者 {UserId}，檔案數 {Count}，總大小 {Size}MB", 
                    userId, stats.TotalFiles, stats.TotalSizeMB);
                
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得檔案統計失敗：使用者 {UserId}", userId);
                return new FileStatistics();
            }
        }

        /// <summary>
        /// 分析檔案刪除影響
        /// </summary>
        public async Task<DeleteImpactResult> GetDeleteImpactAsync(Guid fileId, string userId)
        {
            try
            {
                var file = await _dataAccessService.GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return new DeleteImpactResult
                    {
                        Success = false,
                        Message = "檔案不存在或無權限存取",
                        CanDelete = false
                    };
                }

                // 查詢與此檔案 MD5 相關的人員資料數量
                var affectedPersons = await _dataAccessService.GetPersonsByFileMd5Async(file.Md5Hash, userId);
                var personCount = affectedPersons.Count;
                var personNames = affectedPersons.Take(10).Select(p => p.Name ?? "未知").ToList();

                var message = personCount > 0
                    ? $"刪除檔案 '{file.OriginalFilename}' 將同時刪除 {personCount} 筆人員資料"
                    : $"刪除檔案 '{file.OriginalFilename}' 不會影響任何人員資料";

                _logger.LogInformation("檔案刪除影響分析：ID {FileId}，檔案 {FileName}，影響人數 {Count}", 
                    fileId, file.OriginalFilename, personCount);

                return new DeleteImpactResult
                {
                    Success = true,
                    Message = message,
                    AffectedRecords = personCount,
                    AffectedRecordNames = personNames,
                    CanDelete = true, // 預設允許刪除，可根據業務規則調整
                    FileName = file.OriginalFilename,
                    Warning = personCount > 0 ? $"將同時刪除 {personCount} 筆相關資料" : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析檔案刪除影響失敗：ID {FileId}，使用者 {UserId}", fileId, userId);
                return new DeleteImpactResult
                {
                    Success = false,
                    Message = $"分析失敗：{ex.Message}",
                    CanDelete = false
                };
            }
        }

        // ==================== 檔案關聯管理 ====================

        /// <summary>
        /// 關聯檔案到記錄
        /// </summary>
        public async Task<FileOperationResult> AssociateFileAsync(Guid fileId, string recordId, string recordType, string userId)
        {
            try
            {
                var file = await _dataAccessService.GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "檔案不存在或無權限存取"
                    };
                }

                var success = await _dataAccessService.AssociateFileWithRecordAsync(fileId, recordId, recordType, userId);
                if (!success)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "檔案關聯失敗"
                    };
                }

                // 更新檔案物件
                file.AssociatedRecordId = recordId;
                file.AssociatedRecordType = recordType;
                file.UpdatedAt = DateTime.UtcNow;

                // 清除快取
                ClearUserFileCache(userId);

                _logger.LogInformation("檔案關聯成功：ID {FileId} 關聯到 {RecordType}:{RecordId}", 
                    fileId, recordType, recordId);

                return new FileOperationResult
                {
                    Success = true,
                    Message = "檔案關聯成功",
                    File = file
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案關聯失敗：ID {FileId}，使用者 {UserId}", fileId, userId);
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"檔案關聯失敗：{ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>
        /// 移除檔案關聯
        /// </summary>
        public async Task<FileOperationResult> DisassociateFileAsync(Guid fileId, string userId)
        {
            try
            {
                var success = await _dataAccessService.DisassociateFileAsync(fileId, userId);
                if (!success)
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        Message = "移除檔案關聯失敗"
                    };
                }

                // 清除快取
                ClearUserFileCache(userId);

                _logger.LogInformation("檔案關聯移除成功：ID {FileId}", fileId);

                return new FileOperationResult
                {
                    Success = true,
                    Message = "檔案關聯移除成功"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除檔案關聯失敗：ID {FileId}，使用者 {UserId}", fileId, userId);
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"移除檔案關聯失敗：{ex.Message}",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // ==================== 檔案驗證和工具 ====================

        /// <summary>
        /// 計算檔案 MD5 雜湊值
        /// </summary>
        public async Task<string> CalculateMd5Async(IFormFile file)
        {
            using var md5 = MD5.Create();
            using var stream = file.OpenReadStream();
            var hash = await md5.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// 檢查檔案是否已存在
        /// </summary>
        public async Task<bool> IsFileExistsAsync(string md5Hash, string userId, string? associatedRecordId = null)
        {
            try
            {
                var files = await _dataAccessService.GetFilesByMd5Async(md5Hash, userId);
                
                if (associatedRecordId != null)
                {
                    // 檢查特定關聯記錄的重複
                    return files.Any(f => f.AssociatedRecordId == associatedRecordId);
                }
                
                // 檢查使用者的任何重複
                return files.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查檔案存在性失敗：MD5 {Md5Hash}，使用者 {UserId}", md5Hash, userId);
                return false;
            }
        }

        /// <summary>
        /// 產生唯一檔案名稱
        /// </summary>
        public string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var random = Guid.NewGuid().ToString("N")[..8];
            
            return $"{fileNameWithoutExtension}_{timestamp}_{random}{extension}";
        }

        // ==================== 私有輔助方法 ====================

        private FileOperationResult ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return new FileOperationResult
                {
                    Success = false,
                    Message = "檔案不能為空",
                    Errors = new List<string> { "檔案不能為空" }
                };
            }

            // 檢查檔案大小 (預設 100MB)
            var maxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSize", 100 * 1024 * 1024);
            if (file.Length > maxFileSize)
            {
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"檔案大小不能超過 {maxFileSize / 1024 / 1024} MB",
                    Errors = new List<string> { "檔案大小超過限制" }
                };
            }

            // 檢查檔案類型
            var allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() 
                ?? new[] { ".xlsx", ".xls", ".csv", ".pdf" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return new FileOperationResult
                {
                    Success = false,
                    Message = $"不支援的檔案格式：{fileExtension}。支援的格式：{string.Join(", ", allowedExtensions)}",
                    Errors = new List<string> { "檔案格式不支援" }
                };
            }

            return new FileOperationResult { Success = true };
        }

        private async Task<FileModel?> CheckDuplicateFileAsync(string md5Hash, string userId, string? associatedRecordId)
        {
            var files = await _dataAccessService.GetFilesByMd5Async(md5Hash, userId);
            
            if (associatedRecordId != null)
            {
                // 在特定關聯記錄內檢查重複
                return files.FirstOrDefault(f => f.AssociatedRecordId == associatedRecordId);
            }
            
            // 在使用者所有檔案中檢查重複
            return files.FirstOrDefault();
        }

        private async Task SaveFileToDiskAsync(IFormFile file, string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
        }

        private string DetermineFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".xlsx" or ".xls" => "excel",
                ".csv" => "csv",
                ".pdf" => "pdf",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "image",
                ".doc" or ".docx" => "document",
                _ => "unknown"
            };
        }

        private async Task ProcessFileInBackground(FileModel file)
        {
            try
            {
                await ProcessFileAsync(file.FileId, file.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "背景處理檔案失敗：ID {FileId}", file.FileId);
            }
        }

        private void ClearUserFileCache(string userId)
        {
            // 清除使用者相關的快取
            var patterns = new[]
            {
                $"user_files_{userId}*",
                $"file_stats_{userId}"
            };

            foreach (var pattern in patterns)
            {
                _cache.Remove(pattern);
            }
        }
    }
}