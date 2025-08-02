using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using familytree_backend.Models;
using familytree_backend.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 新版檔案管理控制器 - 使用 file_uploads 表和標準化命名
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly FileUploadService _fileUploadService;
        private readonly ILogger<FileController> _logger;
        private readonly IConfiguration _configuration;

        public FileController(
            FileUploadService fileUploadService,
            ILogger<FileController> logger,
            IConfiguration configuration)
        {
            _fileUploadService = fileUploadService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 獲取當前用戶ID
        /// </summary>
        private string? GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                   User.FindFirst("userId")?.Value;
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadRequest request)
        {
            try
            {
                _logger.LogInformation($"開始上傳檔案: {request.File?.FileName ?? "Unknown"}");

                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "未選擇檔案或檔案大小為 0" 
                    });
                }

                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                // 檢查檔案大小限制
                var maxSize = _configuration.GetValue<long>("FileUpload:MaxFileSizeBytes", 52428800);
                if (request.File.Length > maxSize)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = $"檔案大小超過限制 ({maxSize / 1024 / 1024} MB)"
                    });
                }

                // 呼叫服務層處理檔案上傳
                var result = await _fileUploadService.UploadFileAsync(
                    request.File, 
                    userId,
                    request.AssociatedRecordId,
                    request.AssociatedRecordType);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = result.IsDuplicate ? "檔案已存在" : "檔案上傳成功",
                    Data = result.File,
                    IsDuplicate = result.IsDuplicate,
                    FilePath = result.FilePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案上傳失敗");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "檔案上傳失敗: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取使用者的檔案列表
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetUserFiles([FromQuery] FileQueryOptions options)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                var result = await _fileUploadService.GetUserFilesAsync(userId, options);

                return Ok(new FileListResponse
                {
                    Success = true,
                    Message = "成功獲取檔案列表",
                    Files = result.Files,
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PageSize = result.PageSize,
                    HasMore = result.HasMore
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取檔案列表失敗");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "獲取檔案列表失敗: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取檔案詳細資訊
        /// </summary>
        [HttpGet("{fileId:guid}")]
        public async Task<IActionResult> GetFileDetails(Guid fileId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                var file = await _fileUploadService.GetFileByIdAsync(fileId, userId);
                if (file == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "找不到指定的檔案"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "成功獲取檔案詳情",
                    Data = file
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"獲取檔案詳情失敗: {fileId}");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "獲取檔案詳情失敗: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 下載檔案
        /// </summary>
        [HttpGet("{fileId:guid}/download")]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                var result = await _fileUploadService.GetFileForDownloadAsync(fileId, userId);
                if (!result.Success)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return File(result.FileContent!, result.MimeType!, result.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"下載檔案失敗: {fileId}");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "下載檔案失敗: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 刪除檔案
        /// </summary>
        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteFile(Guid fileId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                // 刪除前分析影響
                var impactResult = await _fileUploadService.AnalyzeDeleteImpactAsync(fileId, userId);
                if (!impactResult.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = impactResult.Message
                    });
                }

                if (!impactResult.CanDelete)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "無法刪除此檔案: " + (impactResult.Warning ?? "檔案正在使用中")
                    });
                }

                // 執行刪除
                var deleteResult = await _fileUploadService.DeleteFileAsync(fileId, userId);
                if (!deleteResult.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = deleteResult.Message
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"成功刪除檔案: {impactResult.FileName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"刪除檔案失敗: {fileId}");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "刪除檔案失敗: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 更新檔案關聯
        /// </summary>
        [HttpPut("{fileId:guid}/associate")]
        public async Task<IActionResult> UpdateFileAssociation(Guid fileId, [FromBody] FileAssociationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "請求資料驗證失敗"
                    });
                }

                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                var result = await _fileUploadService.UpdateFileAssociationAsync(
                    fileId, 
                    userId, 
                    request.RecordId, 
                    request.RecordType);

                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "成功更新檔案關聯"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新檔案關聯失敗: {fileId}");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "更新檔案關聯失敗: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取檔案統計資訊
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetFileStatistics()
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                var stats = await _fileUploadService.GetFileStatisticsAsync(userId);

                return Ok(new
                {
                    Success = true,
                    Message = "成功獲取檔案統計資訊",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取檔案統計失敗");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "獲取檔案統計失敗: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 檢查檔案是否重複
        /// </summary>
        [HttpPost("check-duplicate")]
        public async Task<IActionResult> CheckDuplicateFile([FromBody] CheckDuplicateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "請求資料驗證失敗"
                    });
                }

                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                var isDuplicate = await _fileUploadService.CheckDuplicateAsync(request.Md5Hash, userId);

                return Ok(new
                {
                    Success = true,
                    IsDuplicate = isDuplicate,
                    Message = isDuplicate ? "檔案已存在" : "檔案不重複"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查檔案重複失敗");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "檢查檔案重複失敗: " + ex.Message
                });
            }
        }

        /// <summary>
        /// 處理 Excel 檔案
        /// </summary>
        [HttpPost("{fileId:guid}/process")]
        public async Task<IActionResult> ProcessExcelFile(Guid fileId)
        {
            try
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ApiResponse 
                    { 
                        Success = false, 
                        Message = "無法識別使用者身份" 
                    });
                }

                var result = await _fileUploadService.ProcessExcelFileAsync(fileId, userId);
                if (!result.Success)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "檔案處理成功",
                    Data = new
                    {
                        ProcessedRows = result.ProcessedRows,
                        TotalRows = result.TotalRows,
                        Duration = result.ProcessingDuration
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"處理Excel檔案失敗: {fileId}");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "處理檔案失敗: " + ex.Message
                });
            }
        }
    }

    /// <summary>
    /// 檢查重複請求模型
    /// </summary>
    public class CheckDuplicateRequest
    {
        [Required]
        [StringLength(32)]
        public string Md5Hash { get; set; } = string.Empty;
    }
}