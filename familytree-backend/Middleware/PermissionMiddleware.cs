using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using FamilyTree.Services.Authorization;

namespace FamilyTree.Middleware
{
    /// <summary>
    /// 權限檢查中介軟體
    /// 提供請求層級的權限驗證和上下文設定
    /// </summary>
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;

        public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IPermissionContext permissionContext)
        {
            try
            {
                // 設定權限上下文
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    SetPermissionContext(context, permissionContext);
                }

                // 記錄權限檢查日誌
                LogPermissionCheck(context);

                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "權限檢查失敗: Path={Path}, UserId={UserId}", 
                    context.Request.Path, 
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "權限中介軟體處理時發生錯誤");
                throw;
            }
        }

        /// <summary>
        /// 設定權限上下文
        /// </summary>
        private void SetPermissionContext(HttpContext context, IPermissionContext permissionContext)
        {
            if (permissionContext is PermissionContext pc)
            {
                pc.SetUser(context.User);
                
                // 從路由或查詢參數提取資源資訊
                var resourceInfo = ExtractResourceInfo(context);
                if (resourceInfo != null)
                {
                    pc.CurrentResource = resourceInfo;
                }
            }
        }

        /// <summary>
        /// 從請求中提取資源資訊
        /// </summary>
        private ResourceInfo? ExtractResourceInfo(HttpContext context)
        {
            try
            {
                var routeData = context.GetRouteData();
                var controllerName = routeData?.Values["controller"]?.ToString()?.ToLower();
                var actionName = routeData?.Values["action"]?.ToString()?.ToLower();

                // 根據控制器名稱推斷資源類型
                var resourceType = InferResourceType(controllerName);
                if (string.IsNullOrEmpty(resourceType))
                    return null;

                // 嘗試從路由參數獲取資源ID
                var resourceId = ExtractResourceId(context, routeData);
                if (string.IsNullOrEmpty(resourceId))
                    return null;

                return new ResourceInfo(resourceType, resourceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "提取資源資訊時發生錯誤");
                return null;
            }
        }

        /// <summary>
        /// 根據控制器名稱推斷資源類型
        /// </summary>
        private string? InferResourceType(string? controllerName)
        {
            return controllerName switch
            {
                "user" or "usercontroller" => "user",
                "project" or "projectcontroller" => "project",
                "person" or "persondata" or "persondatacontroller" => "person",
                "file" or "fileupload" or "filecontroller" => "file",
                "audit" or "auditlog" or "auditlogcontroller" => "audit",
                _ => null
            };
        }

        /// <summary>
        /// 提取資源ID
        /// </summary>
        private string? ExtractResourceId(HttpContext context, RouteData? routeData)
        {
            // 1. 從路由參數獲取
            var routeId = routeData?.Values["id"]?.ToString();
            if (!string.IsNullOrEmpty(routeId))
                return routeId;

            // 2. 從查詢參數獲取
            var queryId = context.Request.Query["id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(queryId))
                return queryId;

            // 3. 從專案相關參數獲取
            var projectId = routeData?.Values["projectId"]?.ToString() ?? 
                           context.Request.Query["projectId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(projectId))
                return projectId;

            return null;
        }

        /// <summary>
        /// 記錄權限檢查日誌
        /// </summary>
        private void LogPermissionCheck(HttpContext context)
        {
            if (!ShouldLogPermissionCheck(context))
                return;

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var userRoles = string.Join(",", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));
            var path = context.Request.Path;
            var method = context.Request.Method;

            _logger.LogDebug("權限檢查: UserId={UserId}, Roles={Roles}, Method={Method}, Path={Path}", 
                userId, userRoles, method, path);
        }

        /// <summary>
        /// 判斷是否應該記錄權限檢查
        /// </summary>
        private bool ShouldLogPermissionCheck(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // 排除靜態資源和健康檢查
            if (path.Contains("/health") || 
                path.Contains("/swagger") || 
                path.Contains("/css/") || 
                path.Contains("/js/") || 
                path.Contains("/images/"))
            {
                return false;
            }

            // 只記錄 API 請求
            return path.StartsWith("/api/");
        }
    }

    /// <summary>
    /// 權限檢查中介軟體擴展
    /// </summary>
    public static class PermissionMiddlewareExtensions
    {
        /// <summary>
        /// 使用權限檢查中介軟體
        /// </summary>
        public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PermissionMiddleware>();
        }
    }

    /// <summary>
    /// 動態權限檢查中介軟體
    /// 可根據請求動態檢查權限
    /// </summary>
    public class DynamicPermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<DynamicPermissionMiddleware> _logger;

        // 端點權限映射
        private static readonly Dictionary<string, List<string>> EndpointPermissions = new()
        {
            ["/api/audit/query"] = new() { "audit:read" },
            ["/api/audit/export"] = new() { "audit:export" },
            ["/api/audit/create"] = new() { "audit:create" },
            ["/api/audit/cleanup"] = new() { "audit:cleanup" },
            ["/api/audit/system-status"] = new() { "audit:admin" },
            ["/api/user/create"] = new() { "user:create" },
            ["/api/user/delete"] = new() { "user:delete" },
            ["/api/project/create"] = new() { "project:create" },
            ["/api/project/delete"] = new() { "project:delete" },
            ["/api/file/upload"] = new() { "file:upload" },
            ["/api/file/delete"] = new() { "file:delete" }
        };

        public DynamicPermissionMiddleware(
            RequestDelegate next, 
            IAuthorizationService authorizationService,
            ILogger<DynamicPermissionMiddleware> logger)
        {
            _next = next;
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            
            // 檢查是否需要權限驗證
            if (RequiresPermissionCheck(path))
            {
                var hasPermission = await CheckEndpointPermission(context, path);
                if (!hasPermission)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Insufficient permissions");
                    return;
                }
            }

            await _next(context);
        }

        /// <summary>
        /// 檢查端點權限
        /// </summary>
        private async Task<bool> CheckEndpointPermission(HttpContext context, string? path)
        {
            if (string.IsNullOrEmpty(path) || !context.User.Identity?.IsAuthenticated == true)
                return false;

            // 查找匹配的權限要求
            var requiredPermissions = FindRequiredPermissions(path);
            if (!requiredPermissions.Any())
                return true; // 沒有特殊權限要求

            // 檢查是否有任一所需權限
            foreach (var permission in requiredPermissions)
            {
                var authResult = await _authorizationService.AuthorizeAsync(
                    context.User, 
                    null, 
                    $"Permission_{permission.Replace(":", "_")}");
                
                if (authResult.Succeeded)
                    return true;
            }

            _logger.LogWarning("權限檢查失敗: Path={Path}, UserId={UserId}, RequiredPermissions={Permissions}", 
                path, 
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                string.Join(",", requiredPermissions));

            return false;
        }

        /// <summary>
        /// 查找所需權限
        /// </summary>
        private List<string> FindRequiredPermissions(string path)
        {
            // 精確匹配
            if (EndpointPermissions.TryGetValue(path, out var exactPermissions))
                return exactPermissions;

            // 模式匹配
            foreach (var (pattern, permissions) in EndpointPermissions)
            {
                if (IsPathMatch(path, pattern))
                    return permissions;
            }

            return new List<string>();
        }

        /// <summary>
        /// 路徑模式匹配
        /// </summary>
        private bool IsPathMatch(string path, string pattern)
        {
            // 支援簡單的萬用字元匹配
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern[..^1];
                return path.StartsWith(prefix);
            }

            return path == pattern;
        }

        /// <summary>
        /// 判斷是否需要權限檢查
        /// </summary>
        private bool RequiresPermissionCheck(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // 排除不需要檢查的路徑
            var excludePaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/health",
                "/swagger"
            };

            return !excludePaths.Any(excluded => path.StartsWith(excluded)) && path.StartsWith("/api/");
        }
    }

    /// <summary>
    /// 動態權限檢查中介軟體擴展
    /// </summary>
    public static class DynamicPermissionMiddlewareExtensions
    {
        /// <summary>
        /// 使用動態權限檢查中介軟體
        /// </summary>
        public static IApplicationBuilder UseDynamicPermissionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DynamicPermissionMiddleware>();
        }
    }
}