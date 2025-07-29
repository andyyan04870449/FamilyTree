// 安全性中介軟體 - 提供統一的請求安全檢查
// 設計改善：建立安全性中介軟體，強化安全性
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using familytree_backend.Services;
using familytree_backend.Constants;

namespace familytree_backend.Middleware
{
    /// <summary>
    /// 安全性中介軟體
    /// 設計理念：提供統一的請求安全檢查，防止各種攻擊
    /// </summary>
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;
        private readonly IValidationService _validationService;
        private readonly IAccessControlService _accessControlService;
        private readonly ILoggingService _loggingService;
        private readonly SecurityConfiguration _securityConfig;

        /// <summary>
        /// 建構子
        /// </summary>
        public SecurityMiddleware(
            RequestDelegate next,
            ILogger<SecurityMiddleware> logger,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService,
            IConfigurationService configurationService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _accessControlService = accessControlService ?? throw new ArgumentNullException(nameof(accessControlService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _securityConfig = configurationService.GetSecurityConfiguration();
        }

        /// <summary>
        /// 中介軟體執行方法
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 記錄請求開始
                _loggingService.LogRequestStart("SecurityMiddleware", new
                {
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
                });

                // 執行安全性檢查
                var securityCheckResult = await PerformSecurityChecksAsync(context);
                if (!securityCheckResult.IsValid)
                {
                    // 安全性檢查失敗，返回錯誤
                    await ReturnSecurityErrorAsync(context, securityCheckResult);
                    return;
                }

                // 安全性檢查通過，繼續處理請求
                await _next(context);

                // 記錄請求完成
                _loggingService.LogRequestComplete("SecurityMiddleware", new
                {
                    StatusCode = context.Response.StatusCode,
                    Path = context.Request.Path,
                    Method = context.Request.Method
                });
            }
            catch (Exception ex)
            {
                // 記錄安全性錯誤
                _loggingService.LogError("SecurityMiddleware", ex, new
                {
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
                });

                // 重新拋出異常
                throw;
            }
        }

        /// <summary>
        /// 執行安全性檢查
        /// 設計理念：全面的請求安全性檢查
        /// </summary>
        private async Task<SecurityCheckResult> PerformSecurityChecksAsync(HttpContext context)
        {
            var result = new SecurityCheckResult { IsValid = true };

            // 1. 檢查請求方法
            if (!IsAllowedHttpMethod(context.Request.Method))
            {
                result.IsValid = false;
                result.ErrorMessage = "不支援的 HTTP 方法";
                result.ErrorCode = "INVALID_HTTP_METHOD";
                return result;
            }

            // 2. 檢查請求路徑
            var pathCheck = _validationService.ValidateString(context.Request.Path.Value, "RequestPath", new ValidationOptions
            {
                MaxLength = 500,
                CheckSqlInjection = true,
                CheckXss = true
            });
            if (!pathCheck.IsValid)
            {
                result.IsValid = false;
                result.ErrorMessage = "請求路徑包含危險內容";
                result.ErrorCode = "DANGEROUS_PATH";
                return result;
            }

            // 3. 檢查 User-Agent
            var userAgent = context.Request.Headers.UserAgent.ToString();
            if (!string.IsNullOrEmpty(userAgent))
            {
                var userAgentCheck = _validationService.ValidateString(userAgent, "UserAgent", new ValidationOptions
                {
                    MaxLength = 500,
                    CheckSqlInjection = true,
                    CheckXss = true
                });
                if (!userAgentCheck.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "User-Agent 包含危險內容";
                    result.ErrorCode = "DANGEROUS_USER_AGENT";
                    return result;
                }
            }

            // 4. 檢查請求標頭
            foreach (var header in context.Request.Headers)
            {
                var headerCheck = _validationService.ValidateString(header.Value.ToString(), $"Header_{header.Key}", new ValidationOptions
                {
                    MaxLength = 1000,
                    CheckSqlInjection = true,
                    CheckXss = true
                });
                if (!headerCheck.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"請求標頭 {header.Key} 包含危險內容";
                    result.ErrorCode = "DANGEROUS_HEADER";
                    return result;
                }
            }

            // 5. 檢查查詢參數
            foreach (var query in context.Request.Query)
            {
                var queryCheck = _validationService.ValidateString(query.Value.ToString(), $"Query_{query.Key}", new ValidationOptions
                {
                    MaxLength = 1000,
                    CheckSqlInjection = true,
                    CheckXss = true
                });
                if (!queryCheck.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"查詢參數 {query.Key} 包含危險內容";
                    result.ErrorCode = "DANGEROUS_QUERY";
                    return result;
                }
            }

            // 6. 檢查請求大小
            if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _securityConfig.MaxRequestSize)
            {
                result.IsValid = false;
                result.ErrorMessage = "請求大小超過限制";
                result.ErrorCode = "REQUEST_TOO_LARGE";
                return result;
            }

            // 7. 檢查 API 存取限制
            var userId = GetUserIdFromContext(context);
            if (!string.IsNullOrEmpty(userId))
            {
                var apiEndpoint = $"{context.Request.Method}:{context.Request.Path}";
                var accessLimitCheck = await _accessControlService.CheckApiAccessLimitAsync(userId, apiEndpoint);
                if (!accessLimitCheck)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "API 存取頻率過高";
                    result.ErrorCode = "RATE_LIMIT_EXCEEDED";
                    return result;
                }
            }

            // 8. 檢查 IP 白名單（如果啟用）
            if (_securityConfig.EnableIpWhitelist)
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(clientIp) && !IsIpAllowed(clientIp))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "IP 地址不在白名單中";
                    result.ErrorCode = "IP_NOT_ALLOWED";
                    return result;
                }
            }

            // 9. 檢查請求來源（Referer）
            if (_securityConfig.ValidateReferer)
            {
                var referer = context.Request.Headers.Referer.ToString();
                if (!string.IsNullOrEmpty(referer) && !IsValidReferer(referer))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "無效的請求來源";
                    result.ErrorCode = "INVALID_REFERER";
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// 返回安全性錯誤
        /// 設計理念：統一的錯誤回應格式
        /// </summary>
        private async Task ReturnSecurityErrorAsync(HttpContext context, SecurityCheckResult result)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                Success = false,
                Message = result.ErrorMessage,
                ErrorCode = result.ErrorCode,
                Timestamp = DateTime.UtcNow
            };

            var jsonResponse = System.Text.Json.JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(jsonResponse);

            // 記錄安全性事件
            _loggingService.LogSecurityEvent("SecurityCheckFailed", GetUserIdFromContext(context) ?? "anonymous", new
            {
                Path = context.Request.Path,
                Method = context.Request.Method,
                ErrorCode = result.ErrorCode,
                ErrorMessage = result.ErrorMessage,
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
            });
        }

        #region 私有輔助方法

        /// <summary>
        /// 檢查是否為允許的 HTTP 方法
        /// </summary>
        private bool IsAllowedHttpMethod(string method)
        {
            var allowedMethods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };
            return allowedMethods.Contains(method.ToUpperInvariant());
        }

        /// <summary>
        /// 從上下文中獲取用戶 ID
        /// </summary>
        private string? GetUserIdFromContext(HttpContext context)
        {
            // 這裡應該從 JWT Token 或其他認證機制中獲取用戶 ID
            // 暫時返回 null
            return null;
        }

        /// <summary>
        /// 檢查 IP 是否在白名單中
        /// </summary>
        private bool IsIpAllowed(string ip)
        {
            // 這裡應該檢查 IP 白名單
            // 暫時返回 true
            return true;
        }

        /// <summary>
        /// 檢查 Referer 是否有效
        /// </summary>
        private bool IsValidReferer(string referer)
        {
            // 這裡應該檢查 Referer 是否來自允許的域名
            // 暫時返回 true
            return true;
        }

        #endregion
    }

    /// <summary>
    /// 安全性檢查結果
    /// </summary>
    public class SecurityCheckResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// 安全性中介軟體擴展
    /// </summary>
    public static class SecurityMiddlewareExtensions
    {
        /// <summary>
        /// 添加安全性中介軟體
        /// </summary>
        public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityMiddleware>();
        }
    }
} 