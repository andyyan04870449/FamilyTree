using System;

namespace familytree_backend.Models
{
    public class ActivityLog
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? ResourceType { get; set; }
        public string? ResourceId { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    // 別名，讓稽核日誌系統可以使用
    public class AuditLog : ActivityLog
    {
        public string EventType { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public string? RequestMethod { get; set; }
        public string? RequestPath { get; set; }
        public string? RequestQuery { get; set; }
        public string? RequestBody { get; set; }
        public int? ResponseCode { get; set; }
        public int? ResponseTime { get; set; }
        public string SecurityLevel { get; set; } = "LOW";
        public int? RiskScore { get; set; }
        public string? ErrorMessage { get; set; }
        public string[]? ComplianceTags { get; set; }
    }
}