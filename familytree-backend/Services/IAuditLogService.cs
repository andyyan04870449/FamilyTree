using familytree_backend.Models;

namespace familytree_backend.Services
{
    /// <summary>
    /// 審計日誌服務介面
    /// 提供審計日誌的記錄、查詢、統計和管理功能
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// 查詢審計日誌
        /// </summary>
        Task<AuditLogQueryResult> QueryLogsAsync(AuditLogFilterModel filter);

        /// <summary>
        /// 獲取審計日誌摘要統計
        /// </summary>
        Task<IEnumerable<AuditLogSummaryModel>> GetSummaryAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// 獲取審計統計資料
        /// </summary>
        Task<AuditStatisticsDto> GetStatisticsAsync(DateTime fromDate, DateTime toDate);

        /// <summary>
        /// 匯出審計日誌
        /// </summary>
        Task<byte[]> ExportLogsAsync(AuditLogFilterModel filter, string format);

        /// <summary>
        /// 記錄審計事件
        /// </summary>
        Task<long> LogEventAsync(string eventType, string action, object? details = null);

        /// <summary>
        /// 記錄審計事件（使用 DTO）
        /// </summary>
        Task<long> LogEventAsync(CreateAuditLogDto auditLog);

        /// <summary>
        /// 記錄 API 呼叫
        /// </summary>
        Task LogApiCallAsync(string method, string path, int statusCode, long responseTimeMs, string? errorMessage = null);

        /// <summary>
        /// 生成合規性報告
        /// </summary>
        Task<string> GenerateComplianceReportAsync(string reportType, DateTime fromDate, DateTime toDate, string requestedBy);

        /// <summary>
        /// 清理過期的審計日誌
        /// </summary>
        Task<int> CleanupExpiredLogsAsync();

        /// <summary>
        /// 設定請求上下文
        /// </summary>
        void SetRequestContext(string? userId, string? userName, string? userRole, 
            string? sessionId, string? ipAddress, string? userAgent, string? requestId);
    }
}