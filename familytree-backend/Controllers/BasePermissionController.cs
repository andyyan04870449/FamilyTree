using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FamilyTree.Services.Authorization;
using familytree_backend.Models;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 基本權限控制器
    /// 提供權限檢查的共用功能
    /// </summary>
    [ApiController]
    [Authorize]
    public abstract class BasePermissionController : ControllerBase
    {
        protected readonly IPermissionContext PermissionContext;
        protected readonly ILogger Logger;

        protected BasePermissionController(IPermissionContext permissionContext, ILogger logger)
        {
            PermissionContext = permissionContext;
            Logger = logger;
        }

        /// <summary>
        /// 當前使用者ID
        /// </summary>
        protected string? CurrentUserId => PermissionContext.UserId;

        /// <summary>
        /// 當前使用者角色
        /// </summary>
        protected List<string> CurrentUserRoles => PermissionContext.UserRoles;

        /// <summary>
        /// 當前使用者名稱
        /// </summary>
        protected string? CurrentUserName => User.FindFirst(ClaimTypes.Name)?.Value;

        /// <summary>
        /// 檢查當前使用者是否有指定權限
        /// </summary>
        /// <param name="permission">權限字符串</param>
        /// <returns>是否有權限</returns>
        protected async Task<bool> HasPermissionAsync(string permission)
        {
            return await PermissionContext.HasPermissionAsync(permission);
        }

        /// <summary>
        /// 檢查當前使用者是否有任一權限
        /// </summary>
        /// <param name="permissions">權限列表</param>
        /// <returns>是否有任一權限</returns>
        protected async Task<bool> HasAnyPermissionAsync(params string[] permissions)
        {
            return await PermissionContext.HasAnyPermissionAsync(permissions);
        }

        /// <summary>
        /// 檢查當前使用者是否有所有權限
        /// </summary>
        /// <param name="permissions">權限列表</param>
        /// <returns>是否有所有權限</returns>
        protected async Task<bool> HasAllPermissionsAsync(params string[] permissions)
        {
            return await PermissionContext.HasAllPermissionsAsync(permissions);
        }

        /// <summary>
        /// 檢查當前使用者是否為資源擁有者
        /// </summary>
        /// <param name="resourceType">資源類型</param>
        /// <param name="resourceId">資源ID</param>
        /// <returns>是否為擁有者</returns>
        protected async Task<bool> IsResourceOwnerAsync(string resourceType, string resourceId)
        {
            return await PermissionContext.IsResourceOwnerAsync(resourceType, resourceId);
        }

        /// <summary>
        /// 設定當前資源
        /// </summary>
        /// <param name="resourceType">資源類型</param>
        /// <param name="resourceId">資源ID</param>
        /// <param name="ownerId">擁有者ID（可選）</param>
        protected void SetCurrentResource(string resourceType, string resourceId, string? ownerId = null)
        {
            PermissionContext.SetResource(resourceType, resourceId, ownerId);
        }

        /// <summary>
        /// 需要權限的操作
        /// 如果沒有權限則拋出 UnauthorizedAccessException
        /// </summary>
        /// <param name="permission">所需權限</param>
        /// <param name="errorMessage">錯誤訊息</param>
        protected async Task RequirePermissionAsync(string permission, string? errorMessage = null)
        {
            if (!await HasPermissionAsync(permission))
            {
                Logger.LogWarning("權限檢查失敗: UserId={UserId}, Permission={Permission}", 
                    CurrentUserId, permission);
                throw new UnauthorizedAccessException(errorMessage ?? $"需要權限: {permission}");
            }
        }

        /// <summary>
        /// 需要任一權限的操作
        /// </summary>
        /// <param name="permissions">權限列表</param>
        /// <param name="errorMessage">錯誤訊息</param>
        protected async Task RequireAnyPermissionAsync(string[] permissions, string? errorMessage = null)
        {
            if (!await HasAnyPermissionAsync(permissions))
            {
                Logger.LogWarning("權限檢查失敗: UserId={UserId}, Permissions={Permissions}", 
                    CurrentUserId, string.Join(",", permissions));
                throw new UnauthorizedAccessException(errorMessage ?? $"需要任一權限: {string.Join(", ", permissions)}");
            }
        }

        /// <summary>
        /// 需要資源擁有權的操作
        /// </summary>
        /// <param name="resourceType">資源類型</param>
        /// <param name="resourceId">資源ID</param>
        /// <param name="errorMessage">錯誤訊息</param>
        protected async Task RequireResourceOwnershipAsync(string resourceType, string resourceId, string? errorMessage = null)
        {
            if (!await IsResourceOwnerAsync(resourceType, resourceId))
            {
                Logger.LogWarning("資源擁有權檢查失敗: UserId={UserId}, ResourceType={ResourceType}, ResourceId={ResourceId}", 
                    CurrentUserId, resourceType, resourceId);
                throw new UnauthorizedAccessException(errorMessage ?? $"需要資源擁有權: {resourceType}#{resourceId}");
            }
        }

        /// <summary>
        /// 需要權限或擁有權的操作
        /// </summary>
        /// <param name="permission">所需權限</param>
        /// <param name="resourceType">資源類型</param>
        /// <param name="resourceId">資源ID</param>
        /// <param name="errorMessage">錯誤訊息</param>
        protected async Task RequirePermissionOrOwnershipAsync(string permission, string resourceType, string resourceId, string? errorMessage = null)
        {
            var hasPermission = await HasPermissionAsync(permission);
            var isOwner = await IsResourceOwnerAsync(resourceType, resourceId);

            if (!hasPermission && !isOwner)
            {
                Logger.LogWarning("權限或擁有權檢查失敗: UserId={UserId}, Permission={Permission}, ResourceType={ResourceType}, ResourceId={ResourceId}", 
                    CurrentUserId, permission, resourceType, resourceId);
                throw new UnauthorizedAccessException(errorMessage ?? $"需要權限 {permission} 或資源擁有權");
            }
        }

        /// <summary>
        /// 創建成功回應
        /// </summary>
        /// <typeparam name="T">數據類型</typeparam>
        /// <param name="data">數據</param>
        /// <param name="message">訊息</param>
        /// <returns>API回應</returns>
        protected IActionResult SuccessResponse<T>(T data, string message = "操作成功")
        {
            return Ok(new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            });
        }

        /// <summary>
        /// 創建錯誤回應
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        /// <param name="details">詳細信息</param>
        /// <param name="statusCode">HTTP狀態碼</param>
        /// <returns>API回應</returns>
        protected IActionResult ErrorResponse(string message, string? details = null, int statusCode = 500)
        {
            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Details = details
            };

            return statusCode switch
            {
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                _ => StatusCode(statusCode, response)
            };
        }

        /// <summary>
        /// 處理操作異常並返回適當的回應
        /// </summary>
        /// <param name="ex">異常</param>
        /// <param name="operationName">操作名稱</param>
        /// <returns>API回應</returns>
        protected IActionResult HandleException(Exception ex, string operationName)
        {
            Logger.LogError(ex, "{OperationName} 操作失敗", operationName);

            return ex switch
            {
                UnauthorizedAccessException => ErrorResponse("權限不足", ex.Message, 403),
                ArgumentException => ErrorResponse("參數錯誤", ex.Message, 400),
                KeyNotFoundException => ErrorResponse("資源不存在", ex.Message, 404),
                _ => ErrorResponse($"{operationName} 時發生錯誤", ex.Message, 500)
            };
        }

        /// <summary>
        /// 檢查模型狀態並返回驗證錯誤
        /// </summary>
        /// <returns>驗證錯誤回應（如果有錯誤）</returns>
        protected IActionResult? ValidateModelState()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                return ErrorResponse("請求資料驗證失敗", errors, 400);
            }

            return null;
        }

        /// <summary>
        /// 在操作中檢查並處理模型驗證
        /// </summary>
        /// <typeparam name="T">操作結果類型</typeparam>
        /// <param name="operation">要執行的操作</param>
        /// <param name="operationName">操作名稱</param>
        /// <returns>API回應</returns>
        protected async Task<IActionResult> ExecuteWithValidationAsync<T>(
            Func<Task<T>> operation, 
            string operationName)
        {
            try
            {
                var validationError = ValidateModelState();
                if (validationError != null)
                    return validationError;

                var result = await operation();
                return SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex, operationName);
            }
        }
    }
}