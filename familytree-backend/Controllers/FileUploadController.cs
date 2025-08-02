// 檔案上傳控制器 - 提供檔案上傳、列表查詢、刪除等 API 端點
// 設計改善：使用統一的資料存取服務，移除重複代碼，改善架構設計
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using familytree_backend.Models;
using familytree_backend.Services;
using FamilyTree.Attributes;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 檔案上傳控制器 (舊版，已棄用)
    /// 職責：處理檔案上傳、查詢、刪除相關的 HTTP 請求
    /// ⚠️ 已棄用：請使用新的 /api/file 端點，此控制器將在 v2.1 中移除
    /// 設計改善：使用統一的資料存取服務，移除重複的 SQL 查詢邏輯
    /// </summary>
    [Obsolete("請使用新的 FileController (/api/file) 端點。此控制器使用舊的 project_id 命名模式，將在 v2.1 中移除。", false)]
    [Route("api/[controller]")]
    [Authorize]
    public class FileUploadController : BaseController
    {
        private readonly FileUploadService _fileUploadService;
        private readonly IDataAccessService _dataAccessService;
        private readonly FileUploadConfiguration _fileUploadConfig;

        /// <summary>
        /// 檔案上傳控制器建構子
        /// 設計改善：使用統一的資料存取服務，避免直接操作資料庫
        /// </summary>
        public FileUploadController(
            FileUploadService fileUploadService,
            IDataAccessService dataAccessService,
            ILogger<FileUploadController> logger,
            IConfigurationService configurationService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService) 
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _fileUploadService = fileUploadService ?? throw new ArgumentNullException(nameof(fileUploadService));
            _dataAccessService = dataAccessService ?? throw new ArgumentNullException(nameof(dataAccessService));
            
            // 獲取檔案上傳配置，避免硬編碼
            _fileUploadConfig = configurationService.GetFileUploadConfiguration();
        }

        /// <summary>
        /// 檔案上傳 API
        /// 設計改善：使用統一的資料存取服務，簡化檔案上傳邏輯
        /// </summary>
        /// <param name="file">要上傳的檔案</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>上傳結果</returns>
        [HttpPost("upload")]
        [RequirePermission("file:upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("檔案上傳", new { FileName = file?.FileName, ProjectId = project_id });

                // 步驟 1：驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：驗證檔案基本參數
                var fileValidationResult = ValidateUploadedFile(file);
                if (fileValidationResult != null)
                {
                    return fileValidationResult;
                }

                Logger.LogInformation("檔案上傳驗證通過 - 檔案: {FileName}, 大小: {FileSize} bytes, 專案ID: {ProjectId}", 
                    file!.FileName, file.Length, project_id);

                // 步驟 3：執行檔案上傳
                var result = await _fileUploadService.UploadFileAsync(file, project_id);

                // 步驟 4：記錄檔案上傳到資料庫
                if (result.Success)
                {
                    var fileRecord = new FileUploadRecord
                    {
                        FileName = file.FileName,
                        FilePath = result.FilePath,
                        FileSize = file.Length,
                        FileType = Path.GetExtension(file.FileName),
                        ProjectId = project_id,
                        UploadTime = DateTime.UtcNow,
                        Status = "uploaded"
                    };

                    await _dataAccessService.RecordFileUploadAsync(fileRecord);
                }

                // 步驟 5：根據結果回應
                if (result.Success)
                {
                    var response = new FileUploadResponse
                    {
                        Success = true,
                        Message = ApplicationConstants.ApiResponse.SuccessMessages.FileUploadedSuccessfully,
                        FileData = new FileData
                        {
                            FileName = file.FileName,
                            FilePath = result.FilePath,
                            FileSize = file.Length,
                            UploadTime = DateTime.UtcNow
                        }
                    };

                    LogRequestComplete("檔案上傳");
                    return Ok(response);
                }
                else if (result.IsDuplicate)
                {
                    // 重複檔案仍視為成功，但附帶警告訊息
                    Logger.LogWarning("檔案重複：{FileName}", file.FileName);
                    
                    var response = new FileUploadResponse
                    {
                        Success = true,
                        Message = "檔案已存在，未重複上傳",
                        FileData = new FileData
                        {
                            FileName = file.FileName,
                            FilePath = result.FilePath,
                            FileSize = file.Length,
                            UploadTime = DateTime.UtcNow
                        }
                    };

                    return Ok(response);
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? ApplicationConstants.ApiResponse.ErrorMessages.FileNotFound);
                }

            }, "檔案上傳");
        }

        /// <summary>
        /// 檔案列表查詢 API
        /// 設計改善：使用統一的資料存取服務，簡化檔案列表查詢邏輯
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>檔案列表</returns>
        [HttpGet("list")]
        [RequirePermission("file:read")]
        public async Task<IActionResult> GetFileList([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取檔案列表", new { ProjectId = project_id });

                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 使用統一的資料存取服務獲取檔案列表
                var fileRecords = await _dataAccessService.GetFileUploadRecordsAsync(project_id!);

                Logger.LogInformation("成功獲取檔案列表：專案 {ProjectId}，檔案數量 {Count}", 
                    project_id, fileRecords.Count());

                var response = new FileListResponse
                {
                    Success = true,
                    Message = "檔案列表獲取成功",
                    Files = fileRecords.Select(f => new FileData
                    {
                        FileName = f.FileName,
                        FilePath = f.FilePath,
                        FileSize = f.FileSize,
                        FileType = f.FileType,
                        UploadTime = f.UploadTime,
                        Status = f.Status
                    }).ToList(),
                    TotalCount = fileRecords.Count()
                };

                LogRequestComplete("獲取檔案列表", fileRecords.Count());
                return Ok(response);
            }, "獲取檔案列表");
        }

        /// <summary>
        /// 檔案刪除 API
        /// 設計改善：使用統一的資料存取服務，簡化檔案刪除邏輯
        /// </summary>
        /// <param name="id">檔案 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        [RequirePermission("file:delete")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("刪除檔案", new { FileId = id });

                // 參數驗證
                if (id <= 0)
                {
                    return CreateErrorResponse("檔案 ID 必須大於 0");
                }

                // 執行檔案刪除（內部已處理檔案存在性檢查）
                var result = await _fileUploadService.DeleteFileAsync(id);

                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message);
                }

                Logger.LogInformation("成功刪除檔案：ID {FileId}", id);

                var response = new ApiResponse
                {
                    Success = true,
                    Message = result.Message
                };

                LogRequestComplete("刪除檔案");
                return Ok(response);
            }, "刪除檔案");
        }

        /// <summary>
        /// 獲取檔案刪除影響分析 API
        /// 設計改善：使用統一的資料存取服務，簡化影響分析邏輯
        /// </summary>
        /// <param name="id">檔案 ID</param>
        /// <returns>刪除影響分析</returns>
        [HttpGet("{id}/impact")]
        public async Task<IActionResult> GetDeleteImpact(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取檔案刪除影響", new { FileId = id });

                // 參數驗證
                if (id <= 0)
                {
                    return CreateErrorResponse("檔案 ID 必須大於 0");
                }

                // 使用 FileUploadService 分析刪除影響
                var impactResult = await _fileUploadService.GetDeleteImpactAsync(id);
                
                if (!impactResult.Success)
                {
                    return CreateErrorResponse(impactResult.Message);
                }

                // 轉換為控制器使用的模型格式
                var impact = new FileDeleteImpact
                {
                    FileId = id,
                    AffectedPersons = impactResult.PersonCount,
                    AffectedRelationships = 0, // 暫時設為0，可以根據需求擴展
                    CanDelete = true,
                    WarningMessage = impactResult.Message
                };

                Logger.LogInformation("檔案刪除影響分析完成：ID {FileId}，影響人數 {PersonCount}", 
                    id, impactResult.PersonCount);

                var response = new FileDeleteImpactResponse
                {
                    Success = true,
                    Message = "刪除影響分析完成",
                    Impact = impact
                };

                LogRequestComplete("獲取檔案刪除影響");
                return Ok(response);
            }, "獲取檔案刪除影響");
        }

        /// <summary>
        /// 檔案處理 API
        /// 設計改善：使用統一的資料存取服務，簡化檔案處理邏輯
        /// </summary>
        /// <param name="id">檔案 ID</param>
        /// <returns>處理結果</returns>
        [HttpPost("process/{id}")]
        [RequirePermission("file:process")]
        public async Task<IActionResult> ProcessFile(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("處理檔案", new { FileId = id });

                // 參數驗證
                if (id <= 0)
                {
                    return CreateErrorResponse("檔案 ID 必須大於 0");
                }

                // 執行檔案處理（內部會檢查檔案是否存在）
                var result = await _fileUploadService.ProcessFileAsync(id);

                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message ?? "檔案處理失敗");
                }

                Logger.LogInformation("成功處理檔案：ID {FileId}，處理結果 {Result}", 
                    id, result.Message);

                var response = new FileProcessResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    ProcessResult = new FileUploadResult
                    {
                        Success = result.Success,
                        Message = result.Message,
                        ProcessedRows = 0, // 可以根據需要從result中提取
                        SuccessRows = 0,
                        ErrorRows = 0,
                        Errors = new List<string>(),
                        ProcessTime = DateTime.UtcNow
                    }
                };

                LogRequestComplete("處理檔案");
                return Ok(response);
            }, "處理檔案");
        }

        /// <summary>
        /// 健康檢查 API
        /// </summary>
        /// <returns>健康狀態</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { 
                message = "FileUploadController 健康檢查通過", 
                timestamp = DateTime.UtcNow,
                status = "healthy"
            });
        }

        /// <summary>
        /// 測試 API 端點
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "FileUploadController 測試成功", timestamp = DateTime.UtcNow });
        }

        #region 私有輔助方法

        /// <summary>
        /// 驗證上傳的檔案
        /// 設計理念：統一的檔案驗證邏輯
        /// </summary>
        private IActionResult? ValidateUploadedFile(IFormFile? file)
        {
            if (file == null)
            {
                return CreateErrorResponse("檔案不能為空");
            }

            if (file.Length == 0)
            {
                return CreateErrorResponse("檔案不能為空");
            }

            if (file.Length > _fileUploadConfig.MaxFileSize)
            {
                return CreateErrorResponse($"檔案大小不能超過 {_fileUploadConfig.MaxFileSize / 1024 / 1024} MB");
            }

            var allowedExtensions = _fileUploadConfig.AllowedExtensions;
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return CreateErrorResponse($"不支援的檔案格式：{fileExtension}。支援的格式：{string.Join(", ", allowedExtensions)}");
            }

            return null;
        }

        #endregion
    }

    #region 回應模型

    /// <summary>
    /// 檔案上傳回應
    /// </summary>
    public class FileUploadResponse : ApiResponse
    {
        public FileData FileData { get; set; } = new();
    }

    /// <summary>
    /// 檔案列表回應
    /// </summary>
    public class FileListResponse : ApiResponse
    {
        public List<FileData> Files { get; set; } = new();
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 檔案刪除影響回應
    /// </summary>
    public class FileDeleteImpactResponse : ApiResponse
    {
        public FileDeleteImpact Impact { get; set; } = new();
    }

    /// <summary>
    /// 檔案處理回應
    /// </summary>
    public class FileProcessResponse : ApiResponse
    {
        public FileUploadResult ProcessResult { get; set; } = new();
    }

    /// <summary>
    /// 檔案資料
    /// </summary>
    public class FileData
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? FileType { get; set; }
        public DateTime UploadTime { get; set; }
        public string? Status { get; set; }
    }

    /// <summary>
    /// 檔案刪除影響
    /// </summary>
    public class FileDeleteImpact
    {
        public int FileId { get; set; }
        public int AffectedPersons { get; set; }
        public int AffectedRelationships { get; set; }
        public bool CanDelete { get; set; }
        public string? WarningMessage { get; set; }
    }

    /// <summary>
    /// 檔案上傳結果
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedRows { get; set; }
        public int SuccessRows { get; set; }
        public int ErrorRows { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime ProcessTime { get; set; } = DateTime.UtcNow;
    }

    #endregion
} 