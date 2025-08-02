using System.Net;

namespace familytree_backend.Models.Exceptions
{
    /// <summary>
    /// 業務邏輯異常
    /// 用於處理業務規則違反、邏輯錯誤等情況
    /// </summary>
    public class BusinessLogicException : BaseApplicationException
    {
        public BusinessLogicException(string message, object? details = null)
            : base(message, HttpStatusCode.BadRequest, "BUSINESS_LOGIC_ERROR", details, shouldLog: false)
        {
        }

        public BusinessLogicException(string message, Exception innerException, object? details = null)
            : base(message, HttpStatusCode.BadRequest, "BUSINESS_LOGIC_ERROR", details, shouldLog: true, innerException)
        {
        }
    }

    /// <summary>
    /// 資源未找到異常
    /// </summary>
    public class ResourceNotFoundException : BaseApplicationException
    {
        public ResourceNotFoundException(string resourceName, object? resourceId = null)
            : base(
                resourceId != null ? $"找不到{resourceName}（ID: {resourceId}）" : $"找不到{resourceName}",
                HttpStatusCode.NotFound,
                "RESOURCE_NOT_FOUND",
                new { ResourceName = resourceName, ResourceId = resourceId },
                shouldLog: false)
        {
        }
    }

    /// <summary>
    /// 參數驗證異常
    /// </summary>
    public class ValidationException : BaseApplicationException
    {
        public ValidationException(string message, object? validationErrors = null)
            : base(message, HttpStatusCode.BadRequest, "VALIDATION_ERROR", validationErrors, shouldLog: false)
        {
        }

        public ValidationException(string parameterName, string errorMessage)
            : base($"參數驗證失敗: {parameterName}", HttpStatusCode.BadRequest, "VALIDATION_ERROR", 
                  new { Parameter = parameterName, Error = errorMessage }, shouldLog: false)
        {
        }
    }

    /// <summary>
    /// 權限不足異常
    /// </summary>
    public class UnauthorizedException : BaseApplicationException
    {
        public UnauthorizedException(string message = "權限不足")
            : base(message, HttpStatusCode.Unauthorized, "UNAUTHORIZED", shouldLog: false)
        {
        }
    }

    /// <summary>
    /// 存取被拒絕異常
    /// </summary>
    public class ForbiddenException : BaseApplicationException
    {
        public ForbiddenException(string message = "存取被拒絕", object? details = null)
            : base(message, HttpStatusCode.Forbidden, "FORBIDDEN", details, shouldLog: false)
        {
        }
    }

    /// <summary>
    /// 資源衝突異常
    /// </summary>
    public class ConflictException : BaseApplicationException
    {
        public ConflictException(string message, object? details = null)
            : base(message, HttpStatusCode.Conflict, "CONFLICT", details, shouldLog: false)
        {
        }
    }
}