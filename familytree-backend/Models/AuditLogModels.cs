using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace familytree_backend.Models
{
    /// <summary>
    /// 稽核日誌主模型
    /// 對應資料庫 audit_logs 表
    /// </summary>
    public class AuditLogModel
    {
        public long Id { get; set; }
        
        // 基本資訊
        public Guid EventId { get; set; } = Guid.NewGuid();
        public Guid? BatchId { get; set; }
        public string? SessionId { get; set; }
        
        // 使用者資訊
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        public string? ImpersonatorId { get; set; }
        
        // 操作資訊
        [Required]
        public string EventType { get; set; } = string.Empty;
        [Required]
        public string Action { get; set; } = string.Empty;
        public string? ResourceType { get; set; }
        public string? ResourceId { get; set; }
        public string? ResourceName { get; set; }
        
        // 變更詳情
        public JsonDocument? OldValues { get; set; }
        public JsonDocument? NewValues { get; set; }
        public string? ChangesSummary { get; set; }
        
        // 技術資訊
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestMethod { get; set; }
        public string? RequestUrl { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestQuery { get; set; }
        public string? RequestBody { get; set; }
        public string? RequestId { get; set; }
        
        // 結果資訊
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public int? ResponseTimeMs { get; set; }
        
        // 安全資訊
        public string SecurityLevel { get; set; } = "NORMAL";
        public int RiskScore { get; set; } = 0;
        public bool IsSuspicious { get; set; } = false;
        
        // 時間資訊
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // 額外資訊
        public JsonDocument? AdditionalData { get; set; }
        public string[]? Tags { get; set; }
        public string[]? ComplianceFlags { get; set; }
    }

    /// <summary>
    /// 稽核日誌事件類型模型
    /// 對應資料庫 audit_event_types 表
    /// </summary>
    public class AuditEventTypeModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string Severity { get; set; } = "INFO";
        public int RetentionDays { get; set; } = 90;
        public bool RequiresApproval { get; set; } = false;
        public bool ComplianceRequired { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 稽核日誌變更詳情模型
    /// 對應資料庫 audit_change_details 表
    /// </summary>
    public class AuditChangeDetailModel
    {
        public long Id { get; set; }
        public long AuditLogId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? FieldType { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangeType { get; set; }
        public bool IsSensitive { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 稽核日誌存取記錄模型
    /// 對應資料庫 audit_log_access 表
    /// </summary>
    public class AuditLogAccessModel
    {
        public long Id { get; set; }
        public string AccessorUserId { get; set; } = string.Empty;
        public string? AccessorUserName { get; set; }
        public string? AccessorIp { get; set; }
        public long? AccessedLogId { get; set; }
        public string? AccessedResourceType { get; set; }
        public string? AccessedResourceId { get; set; }
        public string AccessType { get; set; } = string.Empty;
        public JsonDocument? SearchCriteria { get; set; }
        public int? RecordsReturned { get; set; }
        public string? Purpose { get; set; }
        public bool ApprovalRequired { get; set; } = false;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 稽核日誌查詢過濾器
    /// </summary>
    public class AuditLogFilterModel
    {
        // 時間範圍
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        
        // 使用者過濾
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        
        // 事件過濾
        public string[]? EventTypes { get; set; }
        public string[]? Actions { get; set; }
        public string? EventCategory { get; set; }
        
        // 資源過濾
        public string? ResourceType { get; set; }
        public string? ResourceId { get; set; }
        public string? ResourceName { get; set; }
        
        // 結果過濾
        public bool? Success { get; set; }
        public string? ErrorCode { get; set; }
        
        // 安全過濾
        public string[]? SecurityLevels { get; set; }
        public bool? OnlySuspicious { get; set; }
        public int? MinRiskScore { get; set; }
        public int? MaxRiskScore { get; set; }
        
        // 技術過濾
        public string? IpAddress { get; set; }
        public string? SessionId { get; set; }
        public Guid? BatchId { get; set; }
        
        // 標籤過濾
        public string[]? Tags { get; set; }
        public string[]? ComplianceFlags { get; set; }
        
        // 搜尋
        public string? SearchText { get; set; }
        public string[]? SearchFields { get; set; }
        
        // 分頁
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortField { get; set; } = "OccurredAt";
        public string? SortDirection { get; set; } = "DESC";
        
        // 匯出選項
        public bool IncludeChangeDetails { get; set; } = false;
        public bool MaskSensitiveData { get; set; } = true;
    }

    /// <summary>
    /// 稽核日誌查詢結果
    /// </summary>
    public class AuditLogQueryResult
    {
        public IEnumerable<AuditLogModel> Logs { get; set; } = new List<AuditLogModel>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
        public Dictionary<string, object>? Aggregations { get; set; }
        public DateTime QueryExecutedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan QueryDuration { get; set; }
    }

    /// <summary>
    /// 稽核日誌摘要統計
    /// </summary>
    public class AuditLogSummaryModel
    {
        public DateTime LogDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? ResourceType { get; set; }
        public int EventCount { get; set; }
        public int FailedCount { get; set; }
        public int SuspiciousCount { get; set; }
        public double? AvgResponseTime { get; set; }
        public int UniqueUsers { get; set; }
    }

    /// <summary>
    /// 稽核日誌建立請求 DTO
    /// </summary>
    public class CreateAuditLogDto
    {
        // 基本必要資訊
        [Required]
        public string EventType { get; set; } = string.Empty;
        [Required]
        public string Action { get; set; } = string.Empty;
        
        // 選用資訊
        public Guid? BatchId { get; set; }
        public string? SessionId { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserRole { get; set; }
        public string? ResourceType { get; set; }
        public string? ResourceId { get; set; }
        public string? ResourceName { get; set; }
        
        // 變更資料
        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public string? ChangesSummary { get; set; }
        
        // 請求資訊
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestMethod { get; set; }
        public string? RequestUrl { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestQuery { get; set; }
        public string? RequestBody { get; set; }
        public string? RequestId { get; set; }
        
        // 結果資訊
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public int? ResponseCode { get; set; }
        public int? ResponseTime { get; set; }
        public int? ResponseTimeMs { get; set; }
        
        // 安全資訊
        public string? SecurityLevel { get; set; }
        public int? RiskScore { get; set; }
        public bool? IsSuspicious { get; set; }
        
        // 額外資訊
        public object? AdditionalData { get; set; }
        public string[]? Tags { get; set; }
        public string[]? ComplianceFlags { get; set; }
        
        // 變更詳情
        public List<AuditChangeDetailDto>? ChangeDetails { get; set; }
    }

    /// <summary>
    /// 稽核變更詳情 DTO
    /// </summary>
    public class AuditChangeDetailDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string? FieldType { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? ChangeType { get; set; }
        public bool IsSensitive { get; set; } = false;
    }

    /// <summary>
    /// 合規性報告模型
    /// </summary>
    public class AuditComplianceReportModel
    {
        public long Id { get; set; }
        public Guid ReportId { get; set; } = Guid.NewGuid();
        public string ReportType { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "PENDING";
        public long? TotalEvents { get; set; }
        public long? CriticalEvents { get; set; }
        public long? SecurityEvents { get; set; }
        public long? ComplianceViolations { get; set; }
        public string? FilePath { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? Checksum { get; set; }
        public DateTime? RetentionUntil { get; set; }
        public JsonDocument? AdditionalMetadata { get; set; }
    }

    /// <summary>
    /// 稽核統計資料 DTO
    /// </summary>
    public class AuditStatisticsDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public long TotalEvents { get; set; }
        public long SuccessfulEvents { get; set; }
        public long FailedEvents { get; set; }
        public long SuspiciousEvents { get; set; }
        public long UniqueUsers { get; set; }
        public long UniqueResources { get; set; }
        public double SuccessRate => TotalEvents > 0 ? (double)SuccessfulEvents / TotalEvents * 100 : 0;
        public Dictionary<string, long> EventTypeBreakdown { get; set; } = new();
        public Dictionary<string, long> ActionBreakdown { get; set; } = new();
        public Dictionary<string, long> SecurityLevelBreakdown { get; set; } = new();
        public Dictionary<string, long> HourlyBreakdown { get; set; } = new();
        public List<TopUserActivity> TopUsers { get; set; } = new();
        public List<TopResourceActivity> TopResources { get; set; } = new();
    }

    /// <summary>
    /// 用戶活動統計
    /// </summary>
    public class TopUserActivity
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public long EventCount { get; set; }
        public long FailedEventCount { get; set; }
        public DateTime LastActivity { get; set; }
    }

    /// <summary>
    /// 資源活動統計
    /// </summary>
    public class TopResourceActivity
    {
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public long EventCount { get; set; }
        public DateTime LastAccessed { get; set; }
    }

    /// <summary>
    /// 稽核事件類型常數
    /// </summary>
    public static class AuditEventTypes
    {
        // 認證相關
        public const string LOGIN = "LOGIN";
        public const string LOGIN_FAILED = "LOGIN_FAILED";
        public const string LOGOUT = "LOGOUT";
        public const string PASSWORD_CHANGE = "PASSWORD_CHANGE";
        public const string PASSWORD_RESET = "PASSWORD_RESET";
        
        // 授權相關
        public const string PERMISSION_GRANTED = "PERMISSION_GRANTED";
        public const string PERMISSION_REVOKED = "PERMISSION_REVOKED";
        public const string ROLE_CHANGED = "ROLE_CHANGED";
        public const string ACCESS_DENIED = "ACCESS_DENIED";
        
        // 資料操作
        public const string DATA_VIEW = "DATA_VIEW";
        public const string DATA_CREATE = "DATA_CREATE";
        public const string DATA_UPDATE = "DATA_UPDATE";
        public const string DATA_DELETE = "DATA_DELETE";
        public const string DATA_EXPORT = "DATA_EXPORT";
        public const string DATA_IMPORT = "DATA_IMPORT";
        
        // 檔案操作
        public const string FILE_UPLOAD = "FILE_UPLOAD";
        public const string FILE_DOWNLOAD = "FILE_DOWNLOAD";
        public const string FILE_DELETE = "FILE_DELETE";
        public const string FILE_MODIFY = "FILE_MODIFY";
        
        // 系統操作
        public const string SYSTEM_CONFIG = "SYSTEM_CONFIG";
        public const string SYSTEM_BACKUP = "SYSTEM_BACKUP";
        public const string SYSTEM_RESTORE = "SYSTEM_RESTORE";
        public const string SYSTEM_MAINTENANCE = "SYSTEM_MAINTENANCE";
        
        // 安全事件
        public const string SECURITY_BREACH = "SECURITY_BREACH";
        public const string SUSPICIOUS_ACTIVITY = "SUSPICIOUS_ACTIVITY";
        public const string SECURITY_SCAN = "SECURITY_SCAN";
        
        // API 操作
        public const string API_CALL = "API_CALL";
        public const string API_ERROR = "API_ERROR";
        public const string API_RATE_LIMIT = "API_RATE_LIMIT";
    }

    /// <summary>
    /// 稽核動作類型常數
    /// </summary>
    public static class AuditActions
    {
        public const string CREATE = "CREATE";
        public const string READ = "READ";
        public const string UPDATE = "UPDATE";
        public const string DELETE = "DELETE";
        public const string EXECUTE = "EXECUTE";
        public const string DOWNLOAD = "DOWNLOAD";
        public const string UPLOAD = "UPLOAD";
        public const string EXPORT = "EXPORT";
        public const string IMPORT = "IMPORT";
        public const string LOGIN = "LOGIN";
        public const string LOGOUT = "LOGOUT";
        public const string AUTHORIZE = "AUTHORIZE";
        public const string DENY = "DENY";
    }

    /// <summary>
    /// 安全等級常數
    /// </summary>
    public static class SecurityLevels
    {
        public const string LOW = "LOW";
        public const string NORMAL = "NORMAL";
        public const string HIGH = "HIGH";
        public const string CRITICAL = "CRITICAL";
    }
}