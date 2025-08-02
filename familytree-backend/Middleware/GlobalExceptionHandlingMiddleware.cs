using System.Net;
using System.Text.Json;
using familytree_backend.Models;
using familytree_backend.Models.Exceptions;
using familytree_backend.Services;

namespace familytree_backend.Middleware
{
    /// <summary>
    /// 全域異常處理中介軟體
    /// 負責攔截所有未處理的異常，並轉換為標準化的API回應格式
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly JsonSerializerOptions _jsonOptions;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        /// <summary>
        /// 處理異常並產生標準化回應
        /// </summary>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var requestId = context.TraceIdentifier;
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // 根據異常類型決定回應
            var (statusCode, apiResponse) = exception switch
            {
                BaseApplicationException appEx => HandleApplicationException(appEx, requestId),
                ArgumentNullException nullEx => HandleArgumentNullException(nullEx, requestId),
                ArgumentException argEx => HandleArgumentException(argEx, requestId),
                InvalidOperationException opEx => HandleInvalidOperationException(opEx, requestId),
                UnauthorizedAccessException => HandleUnauthorizedException(requestId),
                NotImplementedException => HandleNotImplementedException(requestId),
                TaskCanceledException => HandleTimeoutException(requestId),
                HttpRequestException httpEx => HandleHttpRequestException(httpEx, requestId),
                _ => HandleGenericException(exception, requestId)
            };

            // 記錄異常（如果需要）
            LogException(exception, context, userId, requestId);

            // 設定回應
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(apiResponse, _jsonOptions);
            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// 處理自定義應用程式異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleApplicationException(BaseApplicationException exception, string requestId)
        {
            var includeDetails = _environment.IsDevelopment();
            var response = ApiResponse.FromException(exception, requestId, includeDetails);
            return (exception.StatusCode, response);
        }

        /// <summary>
        /// 處理參數異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleArgumentException(ArgumentException exception, string requestId)
        {
            var response = ApiResponse.ErrorResult(
                "參數錯誤",
                _environment.IsDevelopment() ? exception.Message : null,
                "INVALID_ARGUMENT",
                requestId);
            return (HttpStatusCode.BadRequest, response);
        }

        /// <summary>
        /// 處理空參數異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleArgumentNullException(ArgumentNullException exception, string requestId)
        {
            var response = ApiResponse.ErrorResult(
                "必要參數不能為空",
                _environment.IsDevelopment() ? new { ParameterName = exception.ParamName, Message = exception.Message } : null,
                "NULL_ARGUMENT",
                requestId);
            return (HttpStatusCode.BadRequest, response);
        }

        /// <summary>
        /// 處理無效操作異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleInvalidOperationException(InvalidOperationException exception, string requestId)
        {
            var response = ApiResponse.ErrorResult(
                "操作無效",
                _environment.IsDevelopment() ? exception.Message : null,
                "INVALID_OPERATION",
                requestId);
            return (HttpStatusCode.BadRequest, response);
        }

        /// <summary>
        /// 處理未授權異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleUnauthorizedException(string requestId)
        {
            var response = ApiResponse.ErrorResult(
                "未授權存取",
                null,
                "UNAUTHORIZED",
                requestId);
            return (HttpStatusCode.Unauthorized, response);
        }

        /// <summary>
        /// 處理未實現異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleNotImplementedException(string requestId)
        {
            var response = ApiResponse.ErrorResult(
                "功能尚未實現",
                null,
                "NOT_IMPLEMENTED",
                requestId);
            return (HttpStatusCode.NotImplemented, response);
        }

        /// <summary>
        /// 處理逾時異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleTimeoutException(string requestId)
        {
            var response = ApiResponse.ErrorResult(
                "請求逾時",
                null,
                "TIMEOUT",
                requestId);
            return (HttpStatusCode.RequestTimeout, response);
        }

        /// <summary>
        /// 處理HTTP請求異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleHttpRequestException(HttpRequestException exception, string requestId)
        {
            var response = ApiResponse.ErrorResult(
                "外部服務錯誤",
                _environment.IsDevelopment() ? exception.Message : null,
                "EXTERNAL_SERVICE_ERROR",
                requestId);
            return (HttpStatusCode.BadGateway, response);
        }

        /// <summary>
        /// 處理一般異常
        /// </summary>
        private (HttpStatusCode, ApiResponse) HandleGenericException(Exception exception, string requestId)
        {
            var response = ApiResponse.ErrorResult(
                "系統發生錯誤，請稍後再試",
                _environment.IsDevelopment() ? new 
                { 
                    Type = exception.GetType().Name, 
                    Message = exception.Message,
                    StackTrace = exception.StackTrace 
                } : null,
                "INTERNAL_SERVER_ERROR",
                requestId);
            return (HttpStatusCode.InternalServerError, response);
        }

        /// <summary>
        /// 記錄異常資訊
        /// </summary>
        private void LogException(Exception exception, HttpContext context, string? userId, string requestId)
        {
            var logLevel = exception switch
            {
                BaseApplicationException appEx when !appEx.ShouldLog => LogLevel.Debug,
                ArgumentNullException => LogLevel.Warning,
                ArgumentException => LogLevel.Warning,
                InvalidOperationException => LogLevel.Warning,
                UnauthorizedAccessException => LogLevel.Warning,
                NotImplementedException => LogLevel.Information,
                TaskCanceledException => LogLevel.Information,
                _ => LogLevel.Error
            };

            var logMessage = "處理請求時發生異常";
            var logData = new
            {
                RequestId = requestId,
                UserId = userId,
                Path = context.Request.Path.Value,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.Value,
                UserAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exception.Message
            };

            using (_logger.BeginScope(logData))
            {
                _logger.Log(logLevel, exception, logMessage);
            }

            // 如果是嚴重錯誤，額外記錄結構化日誌
            if (logLevel == LogLevel.Error)
            {
                _logger.LogError(
                    "嚴重錯誤 - RequestId: {RequestId}, UserId: {UserId}, Path: {Path}, Method: {Method}, Exception: {ExceptionType}, Message: {Message}",
                    requestId, userId, context.Request.Path, context.Request.Method, 
                    exception.GetType().Name, exception.Message);
            }
        }
    }

    /// <summary>
    /// 全域異常處理中介軟體擴展方法
    /// </summary>
    public static class GlobalExceptionHandlingMiddlewareExtensions
    {
        /// <summary>
        /// 使用全域異常處理中介軟體
        /// </summary>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }
    }
}