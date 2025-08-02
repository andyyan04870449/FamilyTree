// 基礎控制器 - 提供所有控制器的通用功能，遵循 DRY 原則
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using familytree_backend.Services;
using familytree_backend.Extensions;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 基礎控制器抽象類別
    /// 設計理念：將所有控制器的共通功能集中在此，避免代碼重複
    /// 職責：提供統一的日誌、配置、參數驗證、安全性檢查和回應格式化功能
    /// </summary>
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected readonly ILogger Logger;
        protected readonly IConfigurationService ConfigurationService;
        protected readonly IValidationService ValidationService;
        protected readonly IAccessControlService AccessControlService;
        protected readonly ILoggingService LoggingService;

        /// <summary>
        /// 基礎控制器建構子
        /// 設計考量：所有子控制器都需要日誌、配置、驗證和存取控制服務，在基類統一注入
        /// </summary>
        protected BaseController(
            ILogger logger, 
            IConfigurationService configurationService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ConfigurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            ValidationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            AccessControlService = accessControlService ?? throw new ArgumentNullException(nameof(accessControlService));
            LoggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        #region 使用者資訊

        /// <summary>
        /// 取得當前登入使用者的資訊
        /// </summary>
        /// <returns>使用者 ID 和角色</returns>
        protected (string userId, string userRole) GetUserInfo()
        {
            var userId = User.FindFirst("userId")?.Value ?? "";
            var userRole = User.FindFirst("role")?.Value ?? "user";
            return (userId, userRole);
        }

        #endregion

        #region 專案參數處理

        /// <summary>
        /// 驗證專案 ID 參數
        /// 設計理念：專案隔離是系統的核心功能，需要統一的參數驗證邏輯
        /// </summary>
        /// <param name="projectId">專案 ID</param>
        /// <param name="allowNull">是否允許空值</param>
        /// <returns>驗證結果</returns>
        protected IActionResult? ValidateProjectId(string? projectId, bool allowNull = false)
        {
            // 如果不允許空值且專案 ID 為空，返回錯誤
            if (!allowNull && string.IsNullOrWhiteSpace(projectId))
            {
                Logger.LogWarning("專案 ID 參數驗證失敗：專案 ID 不能為空");
                return BadRequest(CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.ProjectIdRequired));
            }

            // 如果專案 ID 不為空，驗證格式
            if (!string.IsNullOrWhiteSpace(projectId) && projectId.Length > ApplicationConstants.Database.ProjectIdMaxLength)
            {
                Logger.LogWarning("專案 ID 參數驗證失敗：專案 ID 長度超過限制 ({Length} > {MaxLength})", 
                    projectId.Length, ApplicationConstants.Database.ProjectIdMaxLength);
                return BadRequest(CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.InvalidParameters));
            }

            return null; // 驗證通過
        }

        #endregion

        #region 使用者資訊

        /// <summary>
        /// 取得當前使用者 ID
        /// </summary>
        protected string GetCurrentUserId()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        /// <summary>
        /// 取得當前使用者角色
        /// </summary>
        protected string GetCurrentUserRole()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        }

        /// <summary>
        /// 檢查是否為管理員
        /// </summary>
        protected bool IsAdmin()
        {
            return GetCurrentUserRole() == "admin";
        }

        #endregion

        #region 安全性驗證

        /// <summary>
        /// 驗證專案 ID 並檢查存取權限
        /// 設計理念：統一的專案存取權限檢查
        /// </summary>
        /// <param name="projectId">專案 ID</param>
        /// <param name="accessLevel">需要的存取等級</param>
        /// <param name="userId">用戶 ID</param>
        /// <returns>驗證結果</returns>
        protected async Task<IActionResult?> ValidateProjectAccessAsync(string? projectId, ProjectAccessLevel accessLevel, string? userId = null)
        {
            // 驗證專案 ID 格式
            var projectValidation = ValidationService.ValidateProjectId(projectId);
            if (!projectValidation.IsValid)
            {
                Logger.LogWarning("專案 ID 驗證失敗：{ErrorMessage}", projectValidation.ErrorMessage);
                return BadRequest(CreateErrorResponse(projectValidation.ErrorMessage));
            }

            // 檢查專案存取權限
            if (!string.IsNullOrEmpty(userId))
            {
                var hasAccess = await AccessControlService.HasProjectAccessAsync(userId, projectId!, accessLevel);
                if (!hasAccess)
                {
                    Logger.LogWarning("專案存取權限檢查失敗：用戶 {UserId} 無權存取專案 {ProjectId}", userId, projectId);
                    return Forbid();
                }
            }

            return null; // 驗證通過
        }

        /// <summary>
        /// 驗證搜尋關鍵字
        /// 設計理念：統一的搜尋關鍵字安全性驗證
        /// </summary>
        /// <param name="keyword">搜尋關鍵字</param>
        /// <returns>驗證結果</returns>
        protected IActionResult? ValidateSearchKeyword(string? keyword)
        {
            var validation = ValidationService.ValidateSearchKeyword(keyword);
            if (!validation.IsValid)
            {
                Logger.LogWarning("搜尋關鍵字驗證失敗：{ErrorMessage}", validation.ErrorMessage);
                return BadRequest(CreateErrorResponse(validation.ErrorMessage));
            }

            return null; // 驗證通過
        }

        /// <summary>
        /// 驗證檔案上傳
        /// 設計理念：統一的檔案上傳安全性驗證
        /// </summary>
        /// <param name="fileName">檔案名稱</param>
        /// <param name="fileSize">檔案大小</param>
        /// <param name="allowedExtensions">允許的副檔名</param>
        /// <param name="maxSize">最大檔案大小</param>
        /// <returns>驗證結果</returns>
        protected IActionResult? ValidateFileUpload(string? fileName, long fileSize, string[] allowedExtensions, long maxSize)
        {
            // 驗證檔案名稱
            var fileNameValidation = ValidationService.ValidateFileName(fileName);
            if (!fileNameValidation.IsValid)
            {
                Logger.LogWarning("檔案名稱驗證失敗：{ErrorMessage}", fileNameValidation.ErrorMessage);
                return BadRequest(CreateErrorResponse(fileNameValidation.ErrorMessage));
            }

            // 驗證檔案類型
            var fileTypeValidation = ValidationService.ValidateFileType(fileName, allowedExtensions);
            if (!fileTypeValidation.IsValid)
            {
                Logger.LogWarning("檔案類型驗證失敗：{ErrorMessage}", fileTypeValidation.ErrorMessage);
                return BadRequest(CreateErrorResponse(fileTypeValidation.ErrorMessage));
            }

            // 驗證檔案大小
            var fileSizeValidation = ValidationService.ValidateFileSize(fileSize, maxSize);
            if (!fileSizeValidation.IsValid)
            {
                Logger.LogWarning("檔案大小驗證失敗：{ErrorMessage}", fileSizeValidation.ErrorMessage);
                return BadRequest(CreateErrorResponse(fileSizeValidation.ErrorMessage));
            }

            return null; // 驗證通過
        }

        /// <summary>
        /// 檢查操作權限
        /// 設計理念：統一的操作權限檢查
        /// </summary>
        /// <param name="action">操作名稱</param>
        /// <param name="userId">用戶 ID</param>
        /// <param name="context">操作上下文</param>
        /// <returns>權限檢查結果</returns>
        protected async Task<bool> CheckOperationPermissionAsync(string action, string? userId, object? context = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogWarning("操作權限檢查失敗：用戶 ID 為空");
                return false;
            }

            var hasPermission = await AccessControlService.CanPerformActionAsync(userId, action, context);
            if (!hasPermission)
            {
                Logger.LogWarning("操作權限檢查失敗：用戶 {UserId} 無權執行操作 {Action}", userId, action);
            }

            return hasPermission;
        }

        #endregion

        #region 分頁參數處理

        /// <summary>
        /// 驗證和正規化分頁參數
        /// 設計理念：所有列表查詢都需要分頁，統一處理可確保一致性和效能
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">頁面大小</param>
        /// <returns>正規化後的分頁參數</returns>
        protected (int page, int pageSize) ValidateAndNormalizePagination(int page, int pageSize)
        {
            var paginationConfig = ConfigurationService.GetPaginationConfiguration();

            // 正規化頁碼：最小為 1
            var normalizedPage = Math.Max(page, 1);

            // 正規化頁面大小：在允許範圍內
            var normalizedPageSize = Math.Max(paginationConfig.MinPageSize, 
                                            Math.Min(pageSize, paginationConfig.MaxPageSize));

            // 如果參數被調整，記錄日誌
            if (normalizedPage != page || normalizedPageSize != pageSize)
            {
                Logger.LogInformation("分頁參數已正規化：原始({OriginalPage}, {OriginalPageSize}) -> 調整後({NormalizedPage}, {NormalizedPageSize})",
                    page, pageSize, normalizedPage, normalizedPageSize);
            }

            return (normalizedPage, normalizedPageSize);
        }

        #endregion

        #region 回應格式化

        /// <summary>
        /// 建立標準成功回應
        /// 設計理念：統一 API 回應格式，提升前端整合體驗
        /// </summary>
        /// <typeparam name="T">資料類型</typeparam>
        /// <param name="data">回應資料</param>
        /// <param name="message">訊息</param>
        /// <returns>標準化成功回應</returns>
        protected IActionResult CreateSuccessResponse<T>(T data, string? message = null)
        {
            var response = new
            {
                success = true,
                message = message ?? ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully,
                data = data,
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }

        /// <summary>
        /// 建立標準錯誤回應
        /// 設計理念：統一錯誤處理格式，便於前端錯誤處理和問題診斷
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        /// <param name="details">詳細錯誤資訊（僅開發環境顯示）</param>
        /// <returns>標準化錯誤回應</returns>
        protected IActionResult CreateErrorResponse(string message, object? details = null)
        {
            var response = new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow,
                // 只在開發環境中包含詳細錯誤資訊
                details = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? details : null
            };

            return BadRequest(response);
        }

        /// <summary>
        /// 建立資源未找到回應
        /// 設計理念：RESTful API 的標準 404 回應
        /// </summary>
        /// <param name="resourceName">資源名稱</param>
        /// <param name="resourceId">資源 ID</param>
        /// <returns>404 回應</returns>
        protected IActionResult CreateNotFoundResponse(string resourceName, object? resourceId = null)
        {
            var message = resourceId != null 
                ? $"找不到{resourceName}（ID: {resourceId}）"
                : $"找不到{resourceName}";

            Logger.LogWarning("資源未找到：{ResourceName}, ID: {ResourceId}", resourceName, resourceId);

            var response = new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow
            };

            return NotFound(response);
        }

        /// <summary>
        /// 建立分頁回應
        /// 設計理念：統一分頁回應格式，包含完整的分頁資訊
        /// </summary>
        /// <typeparam name="T">資料類型</typeparam>
        /// <param name="data">分頁資料</param>
        /// <param name="totalCount">總筆數</param>
        /// <param name="page">當前頁碼</param>
        /// <param name="pageSize">頁面大小</param>
        /// <param name="message">訊息</param>
        /// <returns>分頁回應</returns>
        protected IActionResult CreatePagedResponse<T>(
            IEnumerable<T> data, 
            int totalCount, 
            int page, 
            int pageSize, 
            string? message = null)
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var response = new
            {
                success = true,
                message = message ?? ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully,
                data = data,
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                },
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }

        #endregion

        #region 例外處理

        /// <summary>
        /// 處理控制器動作中的例外
        /// 設計理念：統一例外處理邏輯，確保一致的錯誤回應格式
        /// </summary>
        /// <param name="action">要執行的動作</param>
        /// <param name="actionName">動作名稱（用於日誌）</param>
        /// <returns>動作結果</returns>
        protected async Task<IActionResult> ExecuteWithExceptionHandling(
            Func<Task<IActionResult>> action, 
            string actionName)
        {
            try
            {
                Logger.LogInformation("開始執行動作：{ActionName}", actionName);
                var result = await action();
                Logger.LogInformation("動作執行完成：{ActionName}", actionName);
                return result;
            }
            catch (ArgumentException ex)
            {
                Logger.LogWarning(ex, "參數驗證失敗：{ActionName} - {Message}", actionName, ex.Message);
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.InvalidParameters, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                Logger.LogWarning(ex, "操作無效：{ActionName} - {Message}", actionName, ex.Message);
                return CreateErrorResponse(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "動作執行失敗：{ActionName} - {Message}", actionName, ex.Message);
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        #endregion

        #region 權限管理輔助方法

        /// <summary>
        /// 建立成功回應
        /// </summary>
        protected IActionResult SuccessResponse(object data, string message = "操作成功")
        {
            return CreateSuccessResponse(data, message);
        }

        /// <summary>
        /// 建立錯誤回應
        /// </summary>
        protected IActionResult ErrorResponse(string message)
        {
            return CreateErrorResponse(message);
        }

        /// <summary>
        /// 建立 Not Found 回應
        /// </summary>
        protected IActionResult NotFoundResponse(string message)
        {
            return NotFound(new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 建立 Forbidden 回應
        /// </summary>
        protected IActionResult ForbiddenResponse(string message)
        {
            return StatusCode(403, new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 建立 Bad Request 回應
        /// </summary>
        protected IActionResult BadRequestResponse(string message)
        {
            return BadRequest(new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 建立 Conflict 回應
        /// </summary>
        protected IActionResult ConflictResponse(string message)
        {
            return Conflict(new
            {
                success = false,
                message = message,
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 記錄活動日誌
        /// </summary>
        protected async Task LogActivityAsync(string action, string resourceType, string resourceId, string details)
        {
            try
            {
                var userId = GetCurrentUserId();
                var ipAddress = HttpContext.GetClientIpAddress();
                
                // 格式化詳細資訊包含資源類型和ID
                var formattedDetails = $"{action} - {resourceType}:{resourceId} - {details}";
                
                await LoggingService.LogActivityAsync(userId, action, formattedDetails, ipAddress);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "記錄活動日誌失敗");
            }
        }

        #endregion

        #region 日誌輔助方法

        /// <summary>
        /// 記錄 API 請求開始
        /// 設計理念：統一的請求日誌格式，便於追蹤和除錯
        /// </summary>
        /// <param name="actionName">動作名稱</param>
        /// <param name="parameters">參數物件</param>
        protected void LogRequestStart(string actionName, object? parameters = null)
        {
            var parametersJson = parameters != null 
                ? System.Text.Json.JsonSerializer.Serialize(parameters) 
                : "無參數";
                
            Logger.LogInformation("=== API 請求開始 ===");
            Logger.LogInformation("動作：{ActionName}", actionName);
            Logger.LogInformation("參數：{Parameters}", parametersJson);
            Logger.LogInformation("時間：{RequestTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        }

        /// <summary>
        /// 記錄 API 請求完成
        /// 設計理念：統一的回應日誌格式，包含執行時間統計
        /// </summary>
        /// <param name="actionName">動作名稱</param>
        /// <param name="resultCount">結果筆數（可選）</param>
        protected void LogRequestComplete(string actionName, int? resultCount = null)
        {
            var countInfo = resultCount.HasValue ? $"，結果筆數：{resultCount}" : "";
            Logger.LogInformation("✅ API 請求完成：{ActionName}{CountInfo}", actionName, countInfo);
        }

        #endregion
    }
} 