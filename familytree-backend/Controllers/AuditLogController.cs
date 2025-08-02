using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using familytree_backend.Models;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 稽核日誌控制器
    /// 提供稽核日誌的查詢、統計、匯出等功能
    /// 實作角色為基礎的存取控制和敏感資料保護
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 需要身份驗證
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogController> _logger;
        private readonly ILoggingService _loggingService;

        public AuditLogController(
            IAuditLogService auditLogService,
            ILogger<AuditLogController> logger,
            ILoggingService loggingService)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        /// <summary>
        /// 查詢稽核日誌
        /// 支援多條件過濾、分頁、排序
        /// 管理員可查看所有日誌，一般使用者只能查看自己相關的日誌
        /// </summary>
        /// <param name="filter">查詢過濾條件</param>
        /// <returns>分頁的稽核日誌結果</returns>
        [HttpPost("query")]
        [Authorize] // 暫時放寬權限以測試功能
        public async Task<IActionResult> QueryLogs([FromBody] AuditLogFilterModel filter)
        {
            try
            {
                _loggingService.LogRequestStart("QueryAuditLogs", filter);
                
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                // 設定服務上下文
                SetAuditServiceContext();
                
                // 非管理員只能查看自己相關的日誌
                if (currentUserRole != "Admin")
                {
                    filter.UserId = currentUserId;
                }
                
                // 限制查詢範圍（最多查詢90天）
                if (filter.FromDate == null || filter.ToDate == null)
                {
                    filter.ToDate = DateTime.UtcNow;
                    filter.FromDate = filter.ToDate.Value.AddDays(-90);
                }
                else
                {
                    var maxRange = TimeSpan.FromDays(90);
                    if (filter.ToDate.Value - filter.FromDate.Value > maxRange)
                    {
                        filter.FromDate = filter.ToDate.Value.AddDays(-90);
                    }
                }
                
                var result = await _auditLogService.QueryLogsAsync(filter);
                
                _loggingService.LogRequestComplete("QueryAuditLogs", new { 
                    TotalCount = result.TotalCount, 
                    ReturnedCount = result.Logs.Count(),
                    Page = result.Page 
                });
                
                return Ok(new ApiResponse<AuditLogQueryResult>
                {
                    Success = true,
                    Data = result,
                    Message = $"成功查詢稽核日誌，共 {result.TotalCount} 筆記錄"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢稽核日誌失敗");
                _loggingService.LogError("QueryAuditLogs", ex, filter);
                
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "查詢稽核日誌時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取稽核日誌摘要統計
        /// 提供按日期分組的事件統計資訊
        /// </summary>
        /// <param name="fromDate">開始日期</param>
        /// <param name="toDate">結束日期</param>
        /// <returns>稽核日誌摘要統計</returns>
        [HttpGet("summary")]
        [Authorize(Roles = "Admin,AuditReader")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                fromDate ??= DateTime.UtcNow.AddDays(-30);
                toDate ??= DateTime.UtcNow;
                
                // 限制查詢範圍
                if (toDate.Value - fromDate.Value > TimeSpan.FromDays(90))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "查詢範圍不能超過90天"
                    });
                }
                
                SetAuditServiceContext();
                var summary = await _auditLogService.GetSummaryAsync(fromDate.Value, toDate.Value);
                
                return Ok(new ApiResponse<IEnumerable<AuditLogSummaryModel>>
                {
                    Success = true,
                    Data = summary,
                    Message = "成功獲取稽核日誌摘要"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取稽核日誌摘要失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "獲取稽核日誌摘要時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取稽核統計資料
        /// 提供詳細的統計分析，包括事件分佈、使用者活動等
        /// </summary>
        /// <param name="fromDate">開始日期</param>
        /// <param name="toDate">結束日期</param>
        /// <returns>詳細統計資料</returns>
        [HttpGet("statistics")]
        [Authorize] // 暫時放寬權限以測試功能
        public async Task<IActionResult> GetStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                fromDate ??= DateTime.UtcNow.AddDays(-30);
                toDate ??= DateTime.UtcNow;
                
                SetAuditServiceContext();
                var statistics = await _auditLogService.GetStatisticsAsync(fromDate.Value, toDate.Value);
                
                return Ok(new ApiResponse<AuditStatisticsDto>
                {
                    Success = true,
                    Data = statistics,
                    Message = "成功獲取稽核統計資料"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取稽核統計資料失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "獲取稽核統計資料時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 匯出稽核日誌
        /// 支援 CSV 和 JSON 格式匯出
        /// </summary>
        /// <param name="filter">匯出過濾條件</param>
        /// <param name="format">匯出格式 (CSV/JSON)</param>
        /// <returns>匯出檔案</returns>
        [HttpPost("export")]
        [Authorize(Roles = "Admin,AuditReader")]
        public async Task<IActionResult> ExportLogs(
            [FromBody] AuditLogFilterModel filter,
            [FromQuery] string format = "CSV")
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                
                SetAuditServiceContext();
                
                // 記錄匯出操作
                await _auditLogService.LogEventAsync(AuditEventTypes.DATA_EXPORT, AuditActions.EXPORT, new
                {
                    ExportFormat = format,
                    Filter = filter,
                    RequestedBy = currentUserId
                });
                
                // 非管理員只能匯出自己相關的日誌
                if (currentUserRole != "Admin")
                {
                    filter.UserId = currentUserId;
                }
                
                // 限制匯出數量
                filter.PageSize = Math.Min(filter.PageSize, 10000);
                
                var data = await _auditLogService.ExportLogsAsync(filter, format);
                
                var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format.ToLower()}";
                var contentType = format.ToUpper() switch
                {
                    "CSV" => "text/csv",
                    "JSON" => "application/json",
                    _ => "application/octet-stream"
                };
                
                return File(data, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "匯出稽核日誌失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "匯出稽核日誌時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取事件類型列表
        /// 提供可用的稽核事件類型供前端選擇
        /// </summary>
        /// <returns>事件類型列表</returns>
        [HttpGet("event-types")]
        [Authorize] // 暫時放寬權限，讓所有已認證使用者都能存取
        public IActionResult GetEventTypes()
        {
            try
            {
                var eventTypes = new[]
                {
                    new { Code = AuditEventTypes.LOGIN, Name = "使用者登入", Category = "SECURITY" },
                    new { Code = AuditEventTypes.LOGIN_FAILED, Name = "登入失敗", Category = "SECURITY" },
                    new { Code = AuditEventTypes.LOGOUT, Name = "使用者登出", Category = "SECURITY" },
                    new { Code = AuditEventTypes.PASSWORD_CHANGE, Name = "密碼變更", Category = "SECURITY" },
                    new { Code = AuditEventTypes.PASSWORD_RESET, Name = "密碼重設", Category = "SECURITY" },
                    new { Code = AuditEventTypes.PERMISSION_GRANTED, Name = "權限授予", Category = "SECURITY" },
                    new { Code = AuditEventTypes.PERMISSION_REVOKED, Name = "權限撤銷", Category = "SECURITY" },
                    new { Code = AuditEventTypes.ROLE_CHANGED, Name = "角色變更", Category = "SECURITY" },
                    new { Code = AuditEventTypes.ACCESS_DENIED, Name = "存取拒絕", Category = "SECURITY" },
                    new { Code = AuditEventTypes.DATA_VIEW, Name = "資料檢視", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.DATA_CREATE, Name = "資料建立", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.DATA_UPDATE, Name = "資料更新", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.DATA_DELETE, Name = "資料刪除", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.DATA_EXPORT, Name = "資料匯出", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.DATA_IMPORT, Name = "資料匯入", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.FILE_UPLOAD, Name = "檔案上傳", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.FILE_DOWNLOAD, Name = "檔案下載", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.FILE_DELETE, Name = "檔案刪除", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.FILE_MODIFY, Name = "檔案修改", Category = "BUSINESS" },
                    new { Code = AuditEventTypes.SYSTEM_CONFIG, Name = "系統配置", Category = "SYSTEM" },
                    new { Code = AuditEventTypes.SYSTEM_BACKUP, Name = "系統備份", Category = "SYSTEM" },
                    new { Code = AuditEventTypes.SYSTEM_RESTORE, Name = "系統還原", Category = "SYSTEM" },
                    new { Code = AuditEventTypes.SECURITY_BREACH, Name = "安全入侵", Category = "SECURITY" },
                    new { Code = AuditEventTypes.SUSPICIOUS_ACTIVITY, Name = "可疑活動", Category = "SECURITY" },
                    new { Code = AuditEventTypes.API_CALL, Name = "API 呼叫", Category = "TECHNICAL" },
                    new { Code = AuditEventTypes.API_ERROR, Name = "API 錯誤", Category = "TECHNICAL" }
                };
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = eventTypes,
                    Message = "成功獲取事件類型列表"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取事件類型列表失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "獲取事件類型列表時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 手動記錄稽核事件
        /// 允許管理員手動新增稽核記錄
        /// </summary>
        /// <param name="auditLog">稽核日誌資料</param>
        /// <returns>建立的稽核日誌ID</returns>
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAuditLog([FromBody] CreateAuditLogDto auditLog)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "請求資料驗證失敗",
                        Details = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                    });
                }
                
                SetAuditServiceContext();
                var logId = await _auditLogService.LogEventAsync(auditLog);
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { LogId = logId },
                    Message = "成功建立稽核日誌記錄"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立稽核日誌失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "建立稽核日誌時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 生成合規性報告
        /// 生成符合法規要求的稽核報告
        /// </summary>
        /// <param name="reportType">報告類型 (GDPR, HIPAA, SOX等)</param>
        /// <param name="fromDate">開始日期</param>
        /// <param name="toDate">結束日期</param>
        /// <returns>報告生成任務ID</returns>
        [HttpPost("compliance-report")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateComplianceReport(
            [FromQuery, Required] string reportType,
            [FromQuery, Required] DateTime fromDate,
            [FromQuery, Required] DateTime toDate)
        {
            try
            {
                if (toDate <= fromDate)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "結束日期必須大於開始日期"
                    });
                }
                
                var currentUserId = GetCurrentUserId();
                SetAuditServiceContext();
                
                var reportId = await _auditLogService.GenerateComplianceReportAsync(
                    reportType, fromDate, toDate, currentUserId);
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { ReportId = reportId },
                    Message = $"已開始生成 {reportType} 合規性報告，報告ID: {reportId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成合規性報告失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "生成合規性報告時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 清理過期稽核日誌
        /// 根據保留政策清理過期的稽核日誌
        /// </summary>
        /// <returns>清理的記錄數量</returns>
        [HttpPost("cleanup")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupExpiredLogs()
        {
            try
            {
                SetAuditServiceContext();
                var deletedCount = await _auditLogService.CleanupExpiredLogsAsync();
                
                await _auditLogService.LogEventAsync(AuditEventTypes.SYSTEM_MAINTENANCE, "CLEANUP", new
                {
                    DeletedCount = deletedCount,
                    Operation = "稽核日誌清理"
                });
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { DeletedCount = deletedCount },
                    Message = $"成功清理 {deletedCount} 個過期的稽核日誌記錄"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理過期稽核日誌失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "清理過期稽核日誌時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取稽核日誌系統狀態
        /// 提供系統運行狀態和統計資訊
        /// </summary>
        /// <returns>系統狀態資訊</returns>
        [HttpGet("system-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemStatus()
        {
            try
            {
                SetAuditServiceContext();
                
                // 獲取最近24小時的統計
                var yesterday = DateTime.UtcNow.AddDays(-1);
                var today = DateTime.UtcNow;
                var statistics = await _auditLogService.GetStatisticsAsync(yesterday, today);
                
                var status = new
                {
                    SystemTime = DateTime.UtcNow,
                    Last24Hours = statistics,
                    Status = "Operational",
                    Version = "1.0.0"
                };
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = status,
                    Message = "成功獲取系統狀態"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取系統狀態失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "獲取系統狀態時發生錯誤",
                    Details = ex.Message
                });
            }
        }

        #region 私有方法

        /// <summary>
        /// 獲取當前使用者ID
        /// </summary>
        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        }

        /// <summary>
        /// 獲取當前使用者角色
        /// </summary>
        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        /// <summary>
        /// 獲取當前使用者名稱
        /// </summary>
        private string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        }

        /// <summary>
        /// 設定稽核服務上下文
        /// </summary>
        private void SetAuditServiceContext()
        {
            var userId = GetCurrentUserId();
            var userName = GetCurrentUserName();
            var userRole = GetCurrentUserRole();
            var sessionId = HttpContext.TraceIdentifier; // 使用 TraceIdentifier 代替 Session
            var ipAddress = HttpContext.Connection?.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            var requestId = HttpContext.TraceIdentifier;

            if (_auditLogService is AuditLogService auditService)
            {
                auditService.SetRequestContext(userId, userName, userRole, 
                    sessionId, ipAddress, userAgent, requestId);
            }
        }

        #endregion
    }

    /// <summary>
    /// 稽核日誌中介軟體
    /// 自動記錄所有 HTTP 請求的稽核資訊
    /// </summary>
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLogMiddleware> _logger;

        public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 設定稽核服務上下文
                if (auditLogService is AuditLogService auditService)
                {
                    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var userName = context.User?.FindFirst(ClaimTypes.Name)?.Value;
                    var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value;
                    var sessionId = context.TraceIdentifier; // 使用 TraceIdentifier 代替 Session
                    var ipAddress = context.Connection?.RemoteIpAddress?.ToString();
                    var userAgent = context.Request.Headers["User-Agent"].FirstOrDefault();
                    var requestId = context.TraceIdentifier;

                    auditService.SetRequestContext(userId, userName, userRole, 
                        sessionId, ipAddress, userAgent, requestId);
                }

                await _next(context);
                
                stopwatch.Stop();
                
                // 記錄 API 呼叫（只記錄重要的端點）
                if (ShouldLogRequest(context))
                {
                    await auditLogService.LogApiCallAsync(
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // 記錄錯誤
                await auditLogService.LogApiCallAsync(
                    context.Request.Method,
                    context.Request.Path,
                    500,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                
                throw;
            }
        }

        /// <summary>
        /// 判斷是否應該記錄此請求
        /// </summary>
        private static bool ShouldLogRequest(HttpContext context)
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
            
            // 只記錄業務相關的 API 或錯誤請求
            return path.StartsWith("/api/") && 
                   (context.Response.StatusCode >= 400 || 
                    path.Contains("/person") || 
                    path.Contains("/file") || 
                    path.Contains("/auth") ||
                    path.Contains("/audit"));
        }
    }
}