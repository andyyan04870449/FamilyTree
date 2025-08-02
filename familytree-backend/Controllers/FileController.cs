// 檔案控制器 - 使用 file_id 標準化命名的檔案管理 API
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using familytree_backend.Models;
using familytree_backend.Services;
using FamilyTree.Attributes;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 檔案控制器 - 標準化檔案管理 API
    /// 設計理念：以檔案為中心的 RESTful API，使用 file_id 作為主要識別符
    /// 取代舊有的 FileUploadController，提供更一致的檔案管理體驗
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : BaseController
    {
        private readonly IFileService _fileService;

        /// <summary>
        /// 檔案控制器建構子
        /// </summary>
        public FileController(
            IFileService fileService,
            ILogger<FileController> logger,
            IConfigurationService configurationService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService)
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        // ==================== 檔案上傳 API ====================

        /// <summary>
        /// 檔案上傳 API (新版本)
        /// 使用 file_id 和彈性關聯機制取代固定的 project_id
        /// </summary>
        /// <param name="file">要上傳的檔案</param>
        /// <param name="associated_record_id">關聯記錄 ID (可選)</param>
        /// <param name="associated_record_type">關聯記錄類型 (可選)</param>
        /// <returns>上傳結果</returns>
        [HttpPost("upload")]
        [RequirePermission("file:upload")]
        public async Task<IActionResult> UploadFile(
            [FromForm] IFormFile file,
            [FromForm] string? associated_record_id = null,
            [FromForm] string? associated_record_type = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("檔案上傳", new { 
                    FileName = file?.FileName, 
                    AssociatedRecordId = associated_record_id,
                    AssociatedRecordType = associated_record_type 
                });

                // 驗證檔案
                if (file == null || file.Length == 0)
                {
                    return CreateErrorResponse("檔案不能為空");
                }

                // 驗證關聯記錄類型
                if (!string.IsNullOrEmpty(associated_record_type))
                {
                    var validRecordTypes = new[] { 
                        AssociatedRecordType.Person, 
                        AssociatedRecordType.Project, 
                        AssociatedRecordType.Analysis, 
                        AssociatedRecordType.Photo, 
                        AssociatedRecordType.Document 
                    };
                    
                    if (!validRecordTypes.Contains(associated_record_type))
                    {
                        return CreateErrorResponse($"不支援的關聯記錄類型：{associated_record_type}");
                    }
                }

                Logger.LogInformation("檔案上傳驗證通過 - 檔案: {FileName}, 大小: {FileSize} bytes, 關聯: {RecordType}:{RecordId}", 
                    file.FileName, file.Length, associated_record_type, associated_record_id);

                // 執行檔案上傳
                var result = await _fileService.UploadFileAsync(
                    file, 
                    GetCurrentUserId(), 
                    associated_record_id, 
                    associated_record_type);

                if (result.Success)
                {
                    var response = new FileUploadResponse
                    {
                        Success = true,
                        Message = result.Message,
                        File = result.File!,
                        IsDuplicate = result.IsDuplicate
                    };

                    LogRequestComplete("檔案上傳", result.File?.FileId);
                    return Ok(response);
                }
                else if (result.IsDuplicate)
                {
                    Logger.LogWarning("檔案重複：{FileName}", file.FileName);
                    
                    var response = new FileUploadResponse
                    {
                        Success = true,
                        Message = "檔案已存在，未重複上傳",
                        File = result.File,
                        IsDuplicate = true
                    };

                    return Ok(response);
                }
                else
                {
                    return CreateErrorResponse(result.Message, result.Errors);
                }

            }, "檔案上傳");
        }

        // ==================== 檔案查詢 API ====================

        /// <summary>
        /// 取得使用者檔案列表
        /// </summary>
        /// <param name="file_type">檔案類型篩選 (可選)</param>
        /// <param name="status">狀態篩選 (可選)</param>
        /// <param name="associated_record_type">關聯記錄類型篩選 (可選)</param>
        /// <param name="page">頁碼 (預設 1)</param>
        /// <param name="limit">每頁筆數 (預設 50)</param>
        /// <returns>檔案列表</returns>
        [HttpGet]
        [RequirePermission("file:read")]
        public async Task<IActionResult> GetFiles(
            [FromQuery] string? file_type = null,
            [FromQuery] string? status = null,
            [FromQuery] string? associated_record_type = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("取得檔案列表", new { 
                    FileType = file_type, 
                    Status = status,
                    AssociatedRecordType = associated_record_type,
                    Page = page, 
                    Limit = limit 
                });

                // 驗證分頁參數
                if (page < 1) page = 1;
                if (limit < 1 || limit > 200) limit = 50;

                var options = new FileQueryOptions
                {
                    FileType = file_type,
                    Status = status,
                    AssociatedRecordType = associated_record_type,
                    Page = page,
                    PageSize = limit
                };

                var files = await _fileService.GetUserFilesAsync(GetCurrentUserId(), options);

                Logger.LogInformation("成功取得檔案列表：檔案數量 {Count}", files.Count);

                var response = new FileListResponse
                {
                    Success = true,
                    Message = "檔案列表取得成功",
                    Files = files,
                    TotalCount = files.Count,
                    Page = page,
                    PageSize = limit,
                    HasMore = files.Count == limit // 簡化的判斷，可改進
                };

                LogRequestComplete("取得檔案列表", files.Count);
                return Ok(response);
            }, "取得檔案列表");
        }

        /// <summary>
        /// 取得單一檔案資訊
        /// </summary>
        /// <param name="file_id">檔案 ID</param>
        /// <returns>檔案資訊</returns>
        [HttpGet("{file_id}")]
        [RequirePermission("file:read")]
        public async Task<IActionResult> GetFile(Guid file_id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("取得檔案", new { FileId = file_id });

                var file = await _fileService.GetFileAsync(file_id, GetCurrentUserId());
                if (file == null)
                {
                    return CreateErrorResponse("檔案不存在或無權限存取");
                }

                Logger.LogInformation("成功取得檔案：ID {FileId}，檔案 {FileName}", file_id, file.OriginalFilename);

                var response = new SingleFileResponse
                {
                    Success = true,
                    Message = "檔案資訊取得成功",
                    File = file
                };

                LogRequestComplete("取得檔案");
                return Ok(response);
            }, "取得檔案");
        }

        /// <summary>
        /// 取得關聯檔案列表
        /// </summary>
        /// <param name="record_type">記錄類型</param>
        /// <param name="record_id">記錄 ID</param>
        /// <returns>關聯檔案列表</returns>
        [HttpGet("associated/{record_type}/{record_id}")]
        [RequirePermission("file:read")]
        public async Task<IActionResult> GetAssociatedFiles(string record_type, string record_id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("取得關聯檔案", new { RecordType = record_type, RecordId = record_id });

                // 驗證記錄類型
                var validRecordTypes = new[] { 
                    AssociatedRecordType.Person, 
                    AssociatedRecordType.Project, 
                    AssociatedRecordType.Analysis, 
                    AssociatedRecordType.Photo, 
                    AssociatedRecordType.Document 
                };
                
                if (!validRecordTypes.Contains(record_type))
                {
                    return CreateErrorResponse($"不支援的記錄類型：{record_type}");
                }

                var files = await _fileService.GetAssociatedFilesAsync(record_id, record_type, GetCurrentUserId());

                Logger.LogInformation("成功取得關聯檔案：{RecordType}:{RecordId}，檔案數量 {Count}", 
                    record_type, record_id, files.Count);

                var response = new FileListResponse
                {
                    Success = true,
                    Message = "關聯檔案列表取得成功",
                    Files = files,
                    TotalCount = files.Count,
                    Page = 1,
                    PageSize = files.Count,
                    HasMore = false
                };

                LogRequestComplete("取得關聯檔案", files.Count);
                return Ok(response);
            }, "取得關聯檔案");
        }

        // ==================== 檔案處理 API ====================

        /// <summary>
        /// 處理檔案 (主要針對 Excel 檔案)
        /// </summary>
        /// <param name="file_id">檔案 ID</param>
        /// <returns>處理結果</returns>
        [HttpPost("{file_id}/process")]
        [RequirePermission("file:process")]
        public async Task<IActionResult> ProcessFile(Guid file_id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("處理檔案", new { FileId = file_id });

                var result = await _fileService.ProcessFileAsync(file_id, GetCurrentUserId());

                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message, result.Errors);
                }

                Logger.LogInformation("檔案處理成功：ID {FileId}，結果 {Message}", file_id, result.Message);

                var response = new FileProcessResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    File = result.File
                };

                LogRequestComplete("處理檔案");
                return Ok(response);
            }, "處理檔案");
        }

        /// <summary>
        /// 更新檔案狀態
        /// </summary>
        /// <param name="file_id">檔案 ID</param>
        /// <param name="request">狀態更新請求</param>
        /// <returns>更新結果</returns>
        [HttpPut("{file_id}/status")]
        [RequirePermission("file:update")]
        public async Task<IActionResult> UpdateFileStatus(Guid file_id, [FromBody] FileStatusUpdateRequest request)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("更新檔案狀態", new { FileId = file_id, Status = request.Status });

                // 驗證狀態值
                var validStatuses = new[] {
                    FileUploadStatus.Uploaded,
                    FileUploadStatus.Processing,
                    FileUploadStatus.Processed,
                    FileUploadStatus.Failed,
                    FileUploadStatus.Deleted
                };

                if (!validStatuses.Contains(request.Status))
                {
                    return CreateErrorResponse($"無效的檔案狀態：{request.Status}");
                }

                var result = await _fileService.UpdateFileStatusAsync(file_id, request.Status, GetCurrentUserId());

                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message, result.Errors);
                }

                Logger.LogInformation("檔案狀態更新成功：ID {FileId}，狀態 {Status}", file_id, request.Status);

                var response = new ApiResponse
                {
                    Success = result.Success,
                    Message = result.Message
                };

                LogRequestComplete("更新檔案狀態");
                return Ok(response);
            }, "更新檔案狀態");
        }

        // ==================== 檔案刪除 API ====================

        /// <summary>
        /// 刪除檔案
        /// </summary>
        /// <param name="file_id">檔案 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{file_id}")]
        [RequirePermission("file:delete")]
        public async Task<IActionResult> DeleteFile(Guid file_id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("刪除檔案", new { FileId = file_id });

                var result = await _fileService.DeleteFileAsync(file_id, GetCurrentUserId());

                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message, result.Errors);
                }

                Logger.LogInformation("檔案刪除成功：ID {FileId}", file_id);

                var response = new ApiResponse
                {
                    Success = result.Success,
                    Message = result.Message
                };

                LogRequestComplete("刪除檔案");
                return Ok(response);
            }, "刪除檔案");
        }

        /// <summary>
        /// 取得檔案刪除影響分析
        /// </summary>
        /// <param name="file_id">檔案 ID</param>
        /// <returns>刪除影響分析</returns>
        [HttpGet("{file_id}/delete-impact")]
        [RequirePermission("file:delete")]
        public async Task<IActionResult> GetDeleteImpact(Guid file_id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("檔案刪除影響分析", new { FileId = file_id });

                var impact = await _fileService.GetDeleteImpactAsync(file_id, GetCurrentUserId());

                if (!impact.Success)
                {
                    return CreateErrorResponse(impact.Message);
                }

                Logger.LogInformation("檔案刪除影響分析完成：ID {FileId}，影響 {Count} 筆記錄", 
                    file_id, impact.AffectedRecords);

                var response = new DeleteImpactResponse
                {
                    Success = impact.Success,
                    Message = impact.Message,
                    Impact = impact
                };

                LogRequestComplete("檔案刪除影響分析");
                return Ok(response);
            }, "檔案刪除影響分析");
        }

        // ==================== 檔案關聯管理 API ====================

        /// <summary>
        /// 關聯檔案到記錄
        /// </summary>
        /// <param name="file_id">檔案 ID</param>
        /// <param name="request">關聯請求</param>
        /// <returns>關聯結果</returns>
        [HttpPost("{file_id}/associate")]
        [RequirePermission("file:associate")]
        public async Task<IActionResult> AssociateFile(Guid file_id, [FromBody] FileAssociationRequest request)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("檔案關聯", new { 
                    FileId = file_id, 
                    RecordId = request.RecordId, 
                    RecordType = request.RecordType 
                });

                // 驗證關聯記錄類型
                var validRecordTypes = new[] { 
                    AssociatedRecordType.Person, 
                    AssociatedRecordType.Project, 
                    AssociatedRecordType.Analysis, 
                    AssociatedRecordType.Photo, 
                    AssociatedRecordType.Document 
                };
                
                if (!validRecordTypes.Contains(request.RecordType))
                {
                    return CreateErrorResponse($"不支援的關聯記錄類型：{request.RecordType}");
                }

                var result = await _fileService.AssociateFileAsync(
                    file_id, 
                    request.RecordId, 
                    request.RecordType, 
                    GetCurrentUserId());

                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message, result.Errors);
                }

                Logger.LogInformation("檔案關聯成功：ID {FileId} 關聯到 {RecordType}:{RecordId}", 
                    file_id, request.RecordType, request.RecordId);

                var response = new ApiResponse
                {
                    Success = result.Success,
                    Message = result.Message
                };

                LogRequestComplete("檔案關聯");
                return Ok(response);
            }, "檔案關聯");
        }

        /// <summary>
        /// 移除檔案關聯
        /// </summary>
        /// <param name="file_id">檔案 ID</param>
        /// <returns>移除結果</returns>
        [HttpDelete("{file_id}/associate")]
        [RequirePermission("file:associate")]
        public async Task<IActionResult> DisassociateFile(Guid file_id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("移除檔案關聯", new { FileId = file_id });

                var result = await _fileService.DisassociateFileAsync(file_id, GetCurrentUserId());

                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message, result.Errors);
                }

                Logger.LogInformation("檔案關聯移除成功：ID {FileId}", file_id);

                var response = new ApiResponse
                {
                    Success = result.Success,
                    Message = result.Message
                };

                LogRequestComplete("移除檔案關聯");
                return Ok(response);
            }, "移除檔案關聯");
        }

        // ==================== 檔案統計 API ====================

        /// <summary>
        /// 取得檔案統計資訊
        /// </summary>
        /// <returns>檔案統計</returns>
        [HttpGet("statistics")]
        [RequirePermission("file:read")]
        public async Task<IActionResult> GetFileStatistics()
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("檔案統計");

                var statistics = await _fileService.GetFileStatisticsAsync(GetCurrentUserId());

                Logger.LogInformation("檔案統計取得成功：檔案數 {Count}，總大小 {Size}MB", 
                    statistics.TotalFiles, statistics.TotalSizeMB);

                var response = new FileStatisticsResponse
                {
                    Success = true,
                    Message = "檔案統計取得成功",
                    Statistics = statistics
                };

                LogRequestComplete("檔案統計");
                return Ok(response);
            }, "檔案統計");
        }

        // ==================== 健康檢查與測試 API ====================

        /// <summary>
        /// 健康檢查 API
        /// </summary>
        /// <returns>健康狀態</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                message = "FileController 健康檢查通過",
                timestamp = DateTime.UtcNow,
                status = "healthy",
                version = "v2.0-file-id-standard"
            });
        }

        /// <summary>
        /// 測試 API 端點
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "FileController 測試成功",
                timestamp = DateTime.UtcNow,
                version = "v2.0-file-id-standard",
                features = new[]
                {
                    "file_id primary key",
                    "flexible association",
                    "improved REST API",
                    "comprehensive file management"
                }
            });
        }

        #region 私有輔助方法

        /// <summary>
        /// 取得當前使用者 ID
        /// </summary>
        /// <returns>使用者 ID</returns>
        private string GetCurrentUserId()
        {
            // 從 JWT Token 或認證上下文取得使用者 ID
            return User?.Identity?.Name ?? "anonymous";
        }

        #endregion
    }

    #region 回應模型

    /// <summary>
    /// 檔案上傳回應
    /// </summary>
    public class FileUploadResponse : ApiResponse
    {
        public FileModel File { get; set; } = new();
        public bool IsDuplicate { get; set; }
    }

    /// <summary>
    /// 單一檔案回應
    /// </summary>
    public class SingleFileResponse : ApiResponse
    {
        public FileModel File { get; set; } = new();
    }

    /// <summary>
    /// 檔案處理回應
    /// </summary>
    public class FileProcessResponse : ApiResponse
    {
        public FileModel? File { get; set; }
    }

    /// <summary>
    /// 刪除影響回應
    /// </summary>
    public class DeleteImpactResponse : ApiResponse
    {
        public DeleteImpactResult Impact { get; set; } = new();
    }

    /// <summary>
    /// 檔案統計回應
    /// </summary>
    public class FileStatisticsResponse : ApiResponse
    {
        public FileStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// 檔案狀態更新請求
    /// </summary>
    public class FileStatusUpdateRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    #endregion
}