// 檔案上傳控制器 - 提供檔案上傳、列表查詢、刪除等 API 端點
// 優化重點：移除硬編碼、統一回應格式、改善 OOP 設計
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using familytree_backend.Models;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 檔案上傳控制器
    /// 職責：處理檔案上傳、查詢、刪除相關的 HTTP 請求
    /// 設計改善：繼承 BaseController 以獲得統一的錯誤處理和回應格式
    /// </summary>
    [Route("api/[controller]")]
    public class FileUploadController : BaseController
    {
        private readonly FileUploadService _fileUploadService;
        private readonly FileUploadConfiguration _fileUploadConfig;

        /// <summary>
        /// 檔案上傳控制器建構子
        /// 設計改善：注入配置服務，移除硬編碼的檔案限制和路徑設定
        /// </summary>
        public FileUploadController(
            FileUploadService fileUploadService, 
            ILogger<FileUploadController> logger,
            IConfigurationService configurationService) 
            : base(logger, configurationService)
        {
            _fileUploadService = fileUploadService ?? throw new ArgumentNullException(nameof(fileUploadService));
            
            // 獲取檔案上傳配置，避免硬編碼
            _fileUploadConfig = configurationService.GetFileUploadConfiguration();
        }

        /// <summary>
        /// 檔案上傳 API
        /// 設計改善：使用統一的參數驗證、錯誤處理和回應格式
        /// </summary>
        /// <param name="file">要上傳的檔案</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>上傳結果</returns>
        [HttpPost("upload")]
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

                Logger.LogInformation("📁 檔案上傳驗證通過 - 檔案: {FileName}, 大小: {FileSize} bytes, 專案ID: {ProjectId}", 
                    file!.FileName, file.Length, project_id);

                // 步驟 3：執行檔案上傳
                var result = await _fileUploadService.UploadFileAsync(file, project_id);

                // 步驟 4：根據結果回應
                if (result.Success)
                {
                    LogRequestComplete("檔案上傳");
                    return CreateSuccessResponse(result, ApplicationConstants.ApiResponse.SuccessMessages.FileUploadedSuccessfully);
                }
                else if (result.IsDuplicate)
                {
                    // 重複檔案仍視為成功，但附帶警告訊息
                    Logger.LogWarning("檔案重複：{FileName}", file.FileName);
                    return CreateSuccessResponse(result, "檔案已存在，未重複上傳");
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? ApplicationConstants.ApiResponse.ErrorMessages.FileNotFound);
                }

            }, "檔案上傳");
        }

        /// <summary>
        /// 檔案列表查詢 API
        /// 設計改善：加入專案隔離驗證和統一回應格式
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>檔案列表</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetFileList([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("檔案列表查詢", new { ProjectId = project_id });

                // 驗證專案 ID（允許空值以支援管理功能）
                var projectValidationResult = ValidateProjectId(project_id, allowNull: true);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 執行查詢
                var result = await _fileUploadService.GetFileListAsync(project_id);

                if (result.Success)
                {
                    LogRequestComplete("檔案列表查詢", result.Files?.Count);
                    return CreateSuccessResponse(result.Files, ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully);
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? ApplicationConstants.ApiResponse.ErrorMessages.DataNotFound);
                }

            }, "檔案列表查詢");
        }

        /// <summary>
        /// 檔案刪除 API
        /// 設計改善：加入詳細日誌和統一錯誤處理
        /// </summary>
        /// <param name="id">檔案 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("檔案刪除", new { FileId = id });

                // 驗證參數
                if (id <= 0)
                {
                    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.InvalidParameters);
                }

                // 執行刪除
                var result = await _fileUploadService.DeleteFileAsync(id);

                if (result.Success)
                {
                    LogRequestComplete("檔案刪除");
                    return CreateSuccessResponse(result, ApplicationConstants.ApiResponse.SuccessMessages.DataDeletedSuccessfully);
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? ApplicationConstants.ApiResponse.ErrorMessages.DataNotFound);
                }

            }, "檔案刪除");
        }

        /// <summary>
        /// 檔案刪除影響分析 API
        /// 設計理念：在刪除前分析影響範圍，提升使用者體驗
        /// </summary>
        /// <param name="id">檔案 ID</param>
        /// <returns>刪除影響分析結果</returns>
        [HttpGet("{id}/impact")]
        public async Task<IActionResult> GetDeleteImpact(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("刪除影響分析", new { FileId = id });

                // 驗證參數
                if (id <= 0)
                {
                    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.InvalidParameters);
                }

                // 執行影響分析
                var result = await _fileUploadService.GetDeleteImpactAsync(id);
                
                if (result.Success)
                {
                    LogRequestComplete("刪除影響分析");
                    return CreateSuccessResponse(result, ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully);
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? ApplicationConstants.ApiResponse.ErrorMessages.DataNotFound);
                }

            }, "刪除影響分析");
        }

        /// <summary>
        /// 檔案處理 API
        /// 設計理念：觸發已上傳檔案的處理流程
        /// </summary>
        /// <param name="id">檔案 ID</param>
        /// <returns>處理結果</returns>
        [HttpPost("process/{id}")]
        public async Task<IActionResult> ProcessFile(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("檔案處理", new { FileId = id });

                // 驗證參數
                if (id <= 0)
                {
                    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.InvalidParameters);
                }

                // 執行檔案處理
                var result = await _fileUploadService.ProcessFileAsync(id);

                if (result.Success)
                {
                    LogRequestComplete("檔案處理");
                    return CreateSuccessResponse(result, ApplicationConstants.ApiResponse.SuccessMessages.FileProcessedSuccessfully);
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
                }

            }, "檔案處理");
        }

        /// <summary>
        /// 服務健康檢查 API
        /// 設計理念：提供服務狀態監控端點
        /// </summary>
        /// <returns>健康狀態</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            Logger.LogInformation("檔案上傳服務健康檢查");
            
            var healthData = new
            {
                service = "FileUploadController",
                status = "healthy",
                maxFileSize = _fileUploadConfig.MaxFileSizeBytes,
                allowedTypes = _fileUploadConfig.AllowedExtensions,
                uploadDirectory = _fileUploadConfig.UploadDirectory
            };

            return CreateSuccessResponse(healthData, "檔案上傳服務正常運作");
        }

        #region 私有輔助方法

        /// <summary>
        /// 驗證上傳的檔案
        /// 設計理念：集中檔案驗證邏輯，使用配置而非硬編碼
        /// </summary>
        /// <param name="file">要驗證的檔案</param>
        /// <returns>驗證結果（null 表示通過）</returns>
        private IActionResult? ValidateUploadedFile(IFormFile? file)
        {
            // 檢查檔案是否存在
            if (file == null || file.Length == 0)
            {
                Logger.LogWarning("檔案驗證失敗：未選擇檔案或檔案為空");
                return CreateErrorResponse("請選擇要上傳的檔案");
            }

            // 檢查檔案大小
            if (file.Length > _fileUploadConfig.MaxFileSizeBytes)
            {
                Logger.LogWarning("檔案驗證失敗：檔案大小超過限制 ({FileSize} > {MaxSize})", 
                    file.Length, _fileUploadConfig.MaxFileSizeBytes);
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.FileSizeExceeded);
            }

            // 檢查檔案類型
            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !_fileUploadConfig.AllowedExtensions.Contains(fileExtension))
            {
                Logger.LogWarning("檔案驗證失敗：不支援的檔案類型 {FileExtension}", fileExtension);
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.FileTypeNotSupported);
            }

            // 檢查 MIME 類型
            if (!_fileUploadConfig.AllowedMimeTypes.Contains(file.ContentType))
            {
                Logger.LogWarning("檔案驗證失敗：不支援的 MIME 類型 {ContentType}", file.ContentType);
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.FileTypeNotSupported);
            }

            return null; // 驗證通過
        }

        #endregion
    }
} 