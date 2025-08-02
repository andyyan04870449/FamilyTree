using Microsoft.AspNetCore.Mvc;
using familytree_backend.Models;
using familytree_backend.Models.Exceptions;
using System.Net;

namespace familytree_backend.Extensions
{
    /// <summary>
    /// 控制器擴展方法
    /// 提供統一的回應建立和錯誤處理方法
    /// </summary>
    public static class ControllerExtensions
    {
        /// <summary>
        /// 建立標準成功回應
        /// </summary>
        public static IActionResult SuccessResponse<T>(this ControllerBase controller, T data, string message = "操作成功")
        {
            var response = ApiResponse<T>.SuccessResult(data, message);
            return controller.Ok(response);
        }

        /// <summary>
        /// 建立標準成功回應（無資料）
        /// </summary>
        public static IActionResult SuccessResponse(this ControllerBase controller, string message = "操作成功")
        {
            var response = ApiResponse.SuccessResult(message);
            return controller.Ok(response);
        }

        /// <summary>
        /// 建立分頁成功回應
        /// </summary>
        public static IActionResult PagedSuccessResponse<T>(
            this ControllerBase controller,
            IEnumerable<T> data,
            int totalCount,
            int page,
            int pageSize,
            string message = "操作成功")
        {
            var response = PagedApiResponse<T>.SuccessResult(data, totalCount, page, pageSize, message);
            return controller.Ok(response);
        }

        /// <summary>
        /// 建立錯誤回應
        /// </summary>
        public static IActionResult ErrorResponse(
            this ControllerBase controller,
            string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest,
            object? details = null,
            string? errorCode = null)
        {
            var requestId = controller.HttpContext.TraceIdentifier;
            var response = ApiResponse.ErrorResult(message, details, errorCode, requestId);
            return controller.StatusCode((int)statusCode, response);
        }

        /// <summary>
        /// 建立錯誤回應（帶資料）
        /// </summary>
        public static IActionResult ErrorResponse<T>(
            this ControllerBase controller,
            string message,
            T? data = default,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest,
            object? details = null,
            string? errorCode = null)
        {
            var requestId = controller.HttpContext.TraceIdentifier;
            var response = ApiResponse<T>.ErrorResult(message, data, details, errorCode, requestId);
            return controller.StatusCode((int)statusCode, response);
        }

        /// <summary>
        /// 從異常建立錯誤回應
        /// </summary>
        public static IActionResult ErrorResponseFromException(
            this ControllerBase controller,
            BaseApplicationException exception,
            bool includeDetails = false)
        {
            var requestId = controller.HttpContext.TraceIdentifier;
            var response = ApiResponse.FromException(exception, requestId, includeDetails);
            return controller.StatusCode((int)exception.StatusCode, response);
        }

        /// <summary>
        /// 從異常建立錯誤回應（帶資料）
        /// </summary>
        public static IActionResult ErrorResponseFromException<T>(
            this ControllerBase controller,
            BaseApplicationException exception,
            T? data = default,
            bool includeDetails = false)
        {
            var requestId = controller.HttpContext.TraceIdentifier;
            var response = ApiResponse<T>.FromException(exception, data, requestId, includeDetails);
            return controller.StatusCode((int)exception.StatusCode, response);
        }

        /// <summary>
        /// 建立 400 Bad Request 回應
        /// </summary>
        public static IActionResult BadRequestResponse(this ControllerBase controller, string message, object? details = null)
        {
            return controller.ErrorResponse(message, HttpStatusCode.BadRequest, details, "BAD_REQUEST");
        }

        /// <summary>
        /// 建立 401 Unauthorized 回應
        /// </summary>
        public static IActionResult UnauthorizedResponse(this ControllerBase controller, string message = "未授權存取")
        {
            return controller.ErrorResponse(message, HttpStatusCode.Unauthorized, null, "UNAUTHORIZED");
        }

        /// <summary>
        /// 建立 403 Forbidden 回應
        /// </summary>
        public static IActionResult ForbiddenResponse(this ControllerBase controller, string message = "存取被拒絕", object? details = null)
        {
            return controller.ErrorResponse(message, HttpStatusCode.Forbidden, details, "FORBIDDEN");
        }

        /// <summary>
        /// 建立 404 Not Found 回應
        /// </summary>
        public static IActionResult NotFoundResponse(this ControllerBase controller, string resourceName, object? resourceId = null)
        {
            var message = resourceId != null 
                ? $"找不到{resourceName}（ID: {resourceId}）" 
                : $"找不到{resourceName}";
            var details = new { ResourceName = resourceName, ResourceId = resourceId };
            return controller.ErrorResponse(message, HttpStatusCode.NotFound, details, "RESOURCE_NOT_FOUND");
        }

        /// <summary>
        /// 建立 409 Conflict 回應
        /// </summary>
        public static IActionResult ConflictResponse(this ControllerBase controller, string message, object? details = null)
        {
            return controller.ErrorResponse(message, HttpStatusCode.Conflict, details, "CONFLICT");
        }

        /// <summary>
        /// 建立 422 Unprocessable Entity 回應
        /// </summary>
        public static IActionResult UnprocessableEntityResponse(this ControllerBase controller, string message, object? validationErrors = null)
        {
            return controller.ErrorResponse(message, HttpStatusCode.UnprocessableEntity, validationErrors, "VALIDATION_ERROR");
        }

        /// <summary>
        /// 建立 500 Internal Server Error 回應
        /// </summary>
        public static IActionResult InternalServerErrorResponse(this ControllerBase controller, string message = "系統發生錯誤，請稍後再試")
        {
            return controller.ErrorResponse(message, HttpStatusCode.InternalServerError, null, "INTERNAL_SERVER_ERROR");
        }

        /// <summary>
        /// 建立模型驗證錯誤回應
        /// </summary>
        public static IActionResult ModelValidationErrorResponse(this ControllerBase controller)
        {
            var errors = controller.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );

            var message = "輸入資料驗證失敗";
            return controller.ErrorResponse(message, HttpStatusCode.BadRequest, errors, "VALIDATION_ERROR");
        }
    }

    /// <summary>
    /// 異常處理擴展方法
    /// </summary>
    public static class ExceptionHandlingExtensions
    {
        /// <summary>
        /// 執行動作並處理異常
        /// </summary>
        public static async Task<IActionResult> ExecuteWithErrorHandlingAsync(
            this ControllerBase controller,
            Func<Task<IActionResult>> action,
            string? actionName = null)
        {
            try
            {
                return await action();
            }
            catch (BaseApplicationException ex)
            {
                // 自定義異常直接拋出，讓全域中介軟體處理
                throw;
            }
            catch (Exception ex)
            {
                // 將一般異常包裝成技術異常
                throw new TechnicalException($"執行 {actionName ?? "操作"} 時發生錯誤", ex);
            }
        }

        /// <summary>
        /// 執行動作並處理異常（同步版本）
        /// </summary>
        public static IActionResult ExecuteWithErrorHandling(
            this ControllerBase controller,
            Func<IActionResult> action,
            string? actionName = null)
        {
            try
            {
                return action();
            }
            catch (BaseApplicationException ex)
            {
                // 自定義異常直接拋出，讓全域中介軟體處理
                throw;
            }
            catch (Exception ex)
            {
                // 將一般異常包裝成技術異常
                throw new TechnicalException($"執行 {actionName ?? "操作"} 時發生錯誤", ex);
            }
        }
    }

    /// <summary>
    /// 資源驗證擴展方法
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// 驗證資源是否存在
        /// </summary>
        public static void ValidateResourceExists<T>(this ControllerBase controller, T? resource, string resourceName, object? resourceId = null)
        {
            if (resource == null)
            {
                throw new ResourceNotFoundException(resourceName, resourceId);
            }
        }

        /// <summary>
        /// 驗證參數不為空
        /// </summary>
        public static void ValidateNotNull(this ControllerBase controller, object? value, string parameterName)
        {
            if (value == null)
            {
                throw new ValidationException(parameterName, "參數不能為空");
            }
        }

        /// <summary>
        /// 驗證字串參數不為空
        /// </summary>
        public static void ValidateNotNullOrEmpty(this ControllerBase controller, string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException(parameterName, "參數不能為空或空白");
            }
        }

        /// <summary>
        /// 驗證模型狀態
        /// </summary>
        public static void ValidateModelState(this ControllerBase controller)
        {
            if (!controller.ModelState.IsValid)
            {
                var errors = controller.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                throw new ValidationException("輸入資料驗證失敗", errors);
            }
        }
    }
}