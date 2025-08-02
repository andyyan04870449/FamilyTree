using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using FamilyTree.Services;

namespace FamilyTree.Attributes
{
    /// <summary>
    /// 權限檢查屬性，用於控制器或動作方法上
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly bool _allowIfOwner;

        /// <summary>
        /// 建立權限檢查屬性
        /// </summary>
        /// <param name="permission">需要的權限字串，例如 "user:create"</param>
        /// <param name="allowIfOwner">是否允許資源擁有者存取</param>
        public RequirePermissionAttribute(string permission, bool allowIfOwner = false)
        {
            _permission = permission;
            _allowIfOwner = allowIfOwner;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 檢查使用者是否已認證
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 取得權限服務
            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            
            // 取得當前使用者ID
            var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 檢查權限
            var hasPermission = await permissionService.HasPermissionAsync(userId, _permission);
            
            // 如果沒有權限但允許擁有者存取，檢查資源擁有權
            if (!hasPermission && _allowIfOwner)
            {
                var resourceId = GetResourceId(context);
                var resourceType = GetResourceType(context);
                
                if (!string.IsNullOrEmpty(resourceId) && !string.IsNullOrEmpty(resourceType))
                {
                    hasPermission = await permissionService.IsResourceOwnerAsync(userId, resourceType, resourceId);
                }
            }

            // 如果沒有權限，返回 403 Forbidden
            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }

        /// <summary>
        /// 從路由或查詢參數中取得資源ID
        /// </summary>
        private string GetResourceId(AuthorizationFilterContext context)
        {
            // 嘗試從路由值取得 id
            if (context.RouteData.Values.TryGetValue("id", out var id))
            {
                return id?.ToString();
            }

            // 嘗試從查詢參數取得 id
            if (context.HttpContext.Request.Query.TryGetValue("id", out var queryId))
            {
                return queryId.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// 根據控制器名稱推斷資源類型
        /// </summary>
        private string GetResourceType(AuthorizationFilterContext context)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString()?.ToLower();
            
            return controllerName switch
            {
                "user" => "user",
                "project" => "project",
                "person" or "persondata" => "person",
                "file" or "fileupload" => "file",
                _ => null
            };
        }
    }

    /// <summary>
    /// 需要任一權限的屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAnyPermissionAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;

        public RequireAnyPermissionAttribute(params string[] permissions)
        {
            _permissions = permissions;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var hasAnyPermission = await permissionService.HasAnyPermissionAsync(userId, _permissions);
            
            if (!hasAnyPermission)
            {
                context.Result = new ForbidResult();
            }
        }
    }

    /// <summary>
    /// 需要所有權限的屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAllPermissionsAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _permissions;

        public RequireAllPermissionsAttribute(params string[] permissions)
        {
            _permissions = permissions;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var hasAllPermissions = await permissionService.HasAllPermissionsAsync(userId, _permissions);
            
            if (!hasAllPermissions)
            {
                context.Result = new ForbidResult();
            }
        }
    }

    /// <summary>
    /// 專案權限檢查屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireProjectPermissionAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;

        public RequireProjectPermissionAttribute(string permission)
        {
            _permission = permission;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 取得專案ID
            var projectId = GetProjectId(context);
            if (string.IsNullOrEmpty(projectId))
            {
                context.Result = new BadRequestObjectResult(new { error = "Project ID is required" });
                return;
            }

            var hasPermission = await permissionService.HasProjectPermissionAsync(userId, projectId, _permission);
            
            if (!hasPermission)
            {
                context.Result = new ForbidResult();
            }
        }

        private string GetProjectId(AuthorizationFilterContext context)
        {
            // 從路由取得 - 支援 projectId 或 id
            if (context.RouteData.Values.TryGetValue("projectId", out var projectId))
            {
                return projectId?.ToString();
            }
            
            // 也支援 id 參數（用於 ProjectController）
            if (context.RouteData.Values.TryGetValue("id", out var id))
            {
                return id?.ToString();
            }

            // 從查詢參數取得
            if (context.HttpContext.Request.Query.TryGetValue("projectId", out var queryProjectId))
            {
                return queryProjectId.FirstOrDefault();
            }

            // 從請求主體取得（如果是 POST/PUT）
            if (context.HttpContext.Request.Method == "POST" || context.HttpContext.Request.Method == "PUT")
            {
                // 這裡需要更複雜的邏輯來從請求主體讀取
                // 暫時返回 null
            }

            return null;
        }
    }
}