// 照片上傳控制器：處理圖片和ZIP檔案的上傳功能
// 主要功能：支援圖片格式、ZIP解壓縮、專案分離、重複處理

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 照片上傳控制器
    /// 職責：處理圖片檔案和ZIP檔案的上傳、查詢、管理
    /// </summary>
    [Route("api/[controller]")]
    public class PhotoUploadController : BaseController
    {
        private readonly PhotoUploadService _photoUploadService;

        public PhotoUploadController(
            PhotoUploadService photoUploadService, 
            ILogger<PhotoUploadController> logger,
            IConfigurationService configurationService) 
            : base(logger, configurationService)
        {
            _photoUploadService = photoUploadService ?? throw new ArgumentNullException(nameof(photoUploadService));
        }

        /// <summary>
        /// 照片上傳 API（支援圖片和ZIP檔案）
        /// </summary>
        /// <param name="file">要上傳的檔案</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>上傳結果</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPhoto()
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                // 手動從 Request.Form 獲取參數
                var file = Request.Form.Files.FirstOrDefault();
                var project_id = Request.Form["project_id"].FirstOrDefault();
                
                LogRequestStart("照片上傳", new { FileName = file?.FileName, ProjectId = project_id });

                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 驗證檔案基本參數
                var fileValidationResult = ValidateUploadedFile(file);
                if (fileValidationResult != null)
                {
                    return fileValidationResult;
                }

                Logger.LogInformation("📸 照片上傳驗證通過 - 檔案: {FileName}, 大小: {FileSize} bytes, 專案ID: {ProjectId}", 
                    file!.FileName, file.Length, project_id);

                // 執行照片上傳
                var result = await _photoUploadService.UploadPhotoAsync(file, project_id!);

                if (result.Success)
                {
                    LogRequestComplete("照片上傳");
                    
                    var responseData = new
                    {
                        uploadedFiles = result.UploadedFiles,
                        failedFiles = result.FailedFiles,
                        totalUploaded = result.UploadedFiles?.Count ?? 0,
                        totalFailed = result.FailedFiles?.Count ?? 0
                    };

                    return CreateSuccessResponse(responseData, result.Message ?? "照片上傳成功");
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? "照片上傳失敗");
                }

            }, "照片上傳");
        }

        /// <summary>
        /// 照片列表查詢 API
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>照片列表</returns>
        [HttpGet("list")]
        public async Task<IActionResult> GetPhotoList([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("照片列表查詢", new { ProjectId = project_id });

                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 執行查詢
                var result = await _photoUploadService.GetPhotoListAsync(project_id!);

                if (result.Success)
                {
                    LogRequestComplete("照片列表查詢", result.Photos?.Count);
                    return CreateSuccessResponse(result.Photos, "照片列表查詢成功");
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? "照片列表查詢失敗");
                }

            }, "照片列表查詢");
        }

        /// <summary>
        /// 照片刪除 API
        /// </summary>
        /// <param name="id">照片 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("照片刪除", new { PhotoId = id });

                // 驗證參數
                if (id <= 0)
                {
                    return CreateErrorResponse("無效的照片ID");
                }

                // 執行刪除
                var result = await _photoUploadService.DeletePhotoAsync(id);

                if (result.Success)
                {
                    LogRequestComplete("照片刪除");
                    return CreateSuccessResponse(result, "照片刪除成功");
                }
                else
                {
                    return CreateErrorResponse(result.Message ?? "照片刪除失敗");
                }

            }, "照片刪除");
        }

        /// <summary>
        /// 照片檔案服務 API
        /// </summary>
        /// <param name="id">照片 ID</param>
        /// <returns>照片檔案</returns>
        [HttpGet("{id}/file")]
        public async Task<IActionResult> GetPhotoFile(int id)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("照片檔案查詢", new { PhotoId = id });

                // 驗證參數
                if (id <= 0)
                {
                    return CreateErrorResponse("無效的照片ID");
                }

                // 取得照片資訊
                var photoInfo = await _photoUploadService.GetPhotoInfoAsync(id);
                
                if (photoInfo == null)
                {
                    return CreateErrorResponse("照片不存在");
                }

                // 檢查檔案是否存在
                if (!System.IO.File.Exists(photoInfo.FilePath))
                {
                    Logger.LogWarning("照片檔案不存在: {FilePath}", photoInfo.FilePath);
                    return CreateErrorResponse("照片檔案不存在");
                }

                // 返回檔案
                var fileBytes = await System.IO.File.ReadAllBytesAsync(photoInfo.FilePath);
                var contentType = GetContentType(photoInfo.SavedFileName);
                
                LogRequestComplete("照片檔案查詢");
                return File(fileBytes, contentType, photoInfo.SavedFileName);

            }, "照片檔案查詢");
        }

        /// <summary>
        /// 通過檔名獲取照片檔案 API
        /// </summary>
        /// <param name="filename">照片檔名</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>照片檔案</returns>
        [HttpGet("file/{filename}")]
        public async Task<IActionResult> GetPhotoFileByName(string filename, [FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("通過檔名查詢照片", new { FileName = filename, ProjectId = project_id });

                // 驗證參數
                if (string.IsNullOrWhiteSpace(filename))
                {
                    return CreateErrorResponse("檔名不能為空");
                }

                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 取得照片資訊
                var photoInfo = await _photoUploadService.GetPhotoInfoByFileNameAsync(filename, project_id!);
                
                if (photoInfo == null)
                {
                    Logger.LogInformation("找不到照片檔案: {FileName} 在專案 {ProjectId}", filename, project_id);
                    return CreateNotFoundResponse("照片檔案", filename);
                }

                // 檢查檔案是否存在
                if (!System.IO.File.Exists(photoInfo.FilePath))
                {
                    Logger.LogWarning("照片檔案不存在於磁碟: {FilePath}", photoInfo.FilePath);
                    return CreateErrorResponse("照片檔案不存在");
                }

                // 返回檔案
                var fileBytes = await System.IO.File.ReadAllBytesAsync(photoInfo.FilePath);
                var contentType = GetContentType(photoInfo.SavedFileName);
                
                LogRequestComplete("通過檔名查詢照片");
                return File(fileBytes, contentType, photoInfo.SavedFileName);

            }, "通過檔名查詢照片");
        }

        /// <summary>
        /// 服務健康檢查 API
        /// </summary>
        /// <returns>健康狀態</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            Logger.LogInformation("照片上傳服務健康檢查");
            
            var healthData = new
            {
                service = "PhotoUploadController",
                status = "healthy",
                supportedFormats = new string[] { "jpg", "jpeg", "png", "zip", "7z" },
                features = new string[] { "專案分離", "ZIP/7Z解壓縮", "重複檢測", "檔名處理" }
            };

            return CreateSuccessResponse(healthData, "照片上傳服務正常運作");
        }

        #region 私有輔助方法

        /// <summary>
        /// 驗證上傳的檔案
        /// </summary>
        private IActionResult? ValidateUploadedFile(IFormFile? file)
        {
            // 檢查檔案是否存在
            if (file == null || file.Length == 0)
            {
                Logger.LogWarning("檔案驗證失敗：未選擇檔案或檔案為空");
                return CreateErrorResponse("請選擇要上傳的檔案");
            }

            // 檢查檔案大小 (50MB限制)
            const long maxFileSize = 50 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                Logger.LogWarning("檔案驗證失敗：檔案大小超過限制 ({FileSize} > {MaxSize})", 
                    file.Length, maxFileSize);
                return CreateErrorResponse("檔案大小超過50MB限制");
            }

            // 檢查檔案類型
            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".zip", ".7z" };
            
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                Logger.LogWarning("檔案驗證失敗：不支援的檔案類型 {FileExtension}", fileExtension);
                return CreateErrorResponse("僅支援 JPG、PNG 圖片格式和 ZIP、7Z 壓縮檔");
            }

            return null; // 驗證通過
        }

        /// <summary>
        /// 取得內容類型
        /// </summary>
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }
} 