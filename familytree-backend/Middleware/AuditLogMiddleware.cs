using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using familytree_backend.Extensions;
using familytree_backend.Services;
using familytree_backend.Models;

namespace familytree_backend.Middleware
{
    /// <summary>
    /// 稽核日誌中介軟體
    /// 自動記錄所有API請求和回應
    /// </summary>
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLogMiddleware> _logger;
        private readonly AuditLogConfiguration _configuration;

        public AuditLogMiddleware(
            RequestDelegate next,
            ILogger<AuditLogMiddleware> logger,
            IOptions<AuditLogConfiguration> configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 檢查是否應該記錄此請求
            if (!ShouldLogRequest(context))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;
            
            try
            {
                // 準備記錄請求資訊
                var requestInfo = await CaptureRequestInfo(context);
                
                // 使用記憶體串流來捕獲回應
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // 執行請求
                await _next(context);

                // 記錄回應資訊
                stopwatch.Stop();
                var responseInfo = await CaptureResponseInfo(context, responseBody, originalBodyStream);

                // 記錄稽核日誌
                await LogAuditEntry(context, requestInfo, responseInfo, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "處理請求時發生錯誤");
                
                // 記錄錯誤到稽核日誌
                await LogErrorAuditEntry(context, ex, stopwatch.ElapsedMilliseconds);
                
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private bool ShouldLogRequest(HttpContext context)
        {
            if (!_configuration.EnableAutoAudit || !_configuration.LogApiCalls)
                return false;

            var path = context.Request.Path.Value?.ToLower() ?? "";
            
            // 排除特定路徑
            foreach (var excludedPath in _configuration.ExcludedPaths)
            {
                if (path.StartsWith(excludedPath.ToLower()))
                    return false;
            }

            // 檢查是否為讀取操作
            if (!_configuration.LogSuccessfulReads && 
                context.Request.Method == "GET" && 
                context.Response.StatusCode >= 200 && 
                context.Response.StatusCode < 300)
            {
                return false;
            }

            return true;
        }

        private async Task<RequestInfo> CaptureRequestInfo(HttpContext context)
        {
            var request = context.Request;
            request.EnableBuffering();

            var requestBody = "";
            if (request.ContentLength > 0 && request.Body.CanSeek)
            {
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            return new RequestInfo
            {
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.ToString(),
                Headers = GetSafeHeaders(request.Headers),
                Body = requestBody,
                IpAddress = GetClientIpAddress(context),
                UserAgent = request.Headers["User-Agent"].ToString()
            };
        }

        private async Task<ResponseInfo> CaptureResponseInfo(
            HttpContext context, 
            MemoryStream responseBody, 
            Stream originalBodyStream)
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            return new ResponseInfo
            {
                StatusCode = context.Response.StatusCode,
                Headers = GetSafeHeaders(context.Response.Headers),
                Body = responseText
            };
        }

        private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
        {
            var safeHeaders = new Dictionary<string, string>();
            var sensitiveHeaders = new[] { "Authorization", "Cookie", "X-Api-Key" };

            foreach (var header in headers)
            {
                if (sensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
                {
                    safeHeaders[header.Key] = "[REDACTED]";
                }
                else
                {
                    safeHeaders[header.Key] = header.Value.ToString();
                }
            }

            return safeHeaders;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // 檢查 X-Forwarded-For header (用於反向代理)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // 檢查 X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // 使用直接連線的 IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private async Task LogAuditEntry(
            HttpContext context, 
            RequestInfo requestInfo, 
            ResponseInfo responseInfo,
            long elapsedMilliseconds)
        {
            // 只有在回應時間超過閾值或特定狀態碼時才記錄
            if (elapsedMilliseconds < _configuration.MinApiResponseTimeToLog &&
                !_configuration.LoggedStatusCodes.Contains(responseInfo.StatusCode))
            {
                return;
            }

            try
            {
                var auditService = context.RequestServices.GetRequiredService<IAuditLogService>();
                var userId = context.User?.FindFirst("userId")?.Value;

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    EventType = DetermineEventType(requestInfo.Method, requestInfo.Path),
                    ResourceType = DetermineResourceType(requestInfo.Path),
                    ResourceId = ExtractResourceId(requestInfo.Path),
                    Action = $"{requestInfo.Method} {requestInfo.Path}",
                    IpAddress = requestInfo.IpAddress,
                    UserAgent = requestInfo.UserAgent,
                    RequestMethod = requestInfo.Method,
                    RequestPath = requestInfo.Path,
                    RequestQuery = requestInfo.QueryString,
                    ResponseCode = responseInfo.StatusCode,
                    ResponseTime = (int)elapsedMilliseconds,
                    SecurityLevel = DetermineSecurityLevel(responseInfo.StatusCode),
                    Details = JsonSerializer.Serialize(new
                    {
                        Request = new
                        {
                            requestInfo.Headers,
                            Body = ShouldLogRequestBody(requestInfo.Path) ? requestInfo.Body : "[EXCLUDED]"
                        },
                        Response = new
                        {
                            responseInfo.Headers,
                            Body = ShouldLogResponseBody(requestInfo.Path, responseInfo.StatusCode) ? 
                                responseInfo.Body : "[EXCLUDED]"
                        }
                    })
                };

                await auditService.LogAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "無法記錄稽核日誌");
            }
        }

        private async Task LogErrorAuditEntry(HttpContext context, Exception exception, long elapsedMilliseconds)
        {
            try
            {
                var auditService = context.RequestServices.GetRequiredService<IAuditLogService>();
                var userId = context.User?.FindFirst("userId")?.Value;

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    EventType = "API_ERROR",
                    ResourceType = "API",
                    Action = $"{context.Request.Method} {context.Request.Path}",
                    IpAddress = GetClientIpAddress(context),
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    RequestMethod = context.Request.Method,
                    RequestPath = context.Request.Path.ToString(),
                    ResponseCode = 500,
                    ResponseTime = (int)elapsedMilliseconds,
                    SecurityLevel = "HIGH",
                    ErrorMessage = exception.Message,
                    Details = JsonSerializer.Serialize(new
                    {
                        ExceptionType = exception.GetType().Name,
                        Message = exception.Message,
                        StackTrace = exception.StackTrace
                    })
                };

                await auditService.LogAsync(auditLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "無法記錄錯誤稽核日誌");
            }
        }

        private string DetermineEventType(string method, string path)
        {
            var pathLower = path.ToLower();

            if (pathLower.Contains("/auth/login"))
                return "USER_LOGIN";
            if (pathLower.Contains("/auth/logout"))
                return "USER_LOGOUT";
            
            return method switch
            {
                "GET" => "DATA_ACCESS",
                "POST" => "DATA_CREATE",
                "PUT" => "DATA_UPDATE",
                "DELETE" => "DATA_DELETE",
                _ => "API_CALL"
            };
        }

        private string DetermineResourceType(string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && segments[0] == "api")
            {
                return segments[1].ToUpper();
            }
            return "UNKNOWN";
        }

        private string? ExtractResourceId(string path)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 3)
            {
                // 嘗試找到可能是 ID 的片段
                for (int i = 2; i < segments.Length; i++)
                {
                    if (Guid.TryParse(segments[i], out _) || int.TryParse(segments[i], out _))
                    {
                        return segments[i];
                    }
                }
            }
            return null;
        }

        private string DetermineSecurityLevel(int statusCode)
        {
            return statusCode switch
            {
                401 or 403 => "HIGH",
                400 or 404 => "MEDIUM",
                >= 500 => "HIGH",
                _ => "LOW"
            };
        }

        private bool ShouldLogRequestBody(string path)
        {
            var sensitiveEndpoints = new[] { "/auth/", "/password", "/token" };
            return !sensitiveEndpoints.Any(endpoint => path.Contains(endpoint, StringComparison.OrdinalIgnoreCase));
        }

        private bool ShouldLogResponseBody(string path, int statusCode)
        {
            // 不記錄大型檔案回應
            if (path.Contains("/download") || path.Contains("/export"))
                return false;

            // 不記錄敏感端點的成功回應
            if (statusCode >= 200 && statusCode < 300)
            {
                var sensitiveEndpoints = new[] { "/auth/", "/user/profile" };
                return !sensitiveEndpoints.Any(endpoint => path.Contains(endpoint, StringComparison.OrdinalIgnoreCase));
            }

            return true;
        }

        private class RequestInfo
        {
            public string Method { get; set; } = "";
            public string Path { get; set; } = "";
            public string QueryString { get; set; } = "";
            public Dictionary<string, string> Headers { get; set; } = new();
            public string Body { get; set; } = "";
            public string IpAddress { get; set; } = "";
            public string UserAgent { get; set; } = "";
        }

        private class ResponseInfo
        {
            public int StatusCode { get; set; }
            public Dictionary<string, string> Headers { get; set; } = new();
            public string Body { get; set; } = "";
        }
    }
}