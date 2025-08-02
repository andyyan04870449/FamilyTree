using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using familytree_backend.Services;
using familytree_backend.Controllers;

namespace familytree_backend.Extensions
{
    /// <summary>
    /// 稽核日誌服務擴展方法
    /// 提供依賴注入和中介軟體配置
    /// </summary>
    public static class AuditLogServiceExtensions
    {
        /// <summary>
        /// 註冊稽核日誌相關服務
        /// </summary>
        public static IServiceCollection AddAuditLogServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 註冊核心服務
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IAuditSecurityService, AuditSecurityService>();
            
            // 註冊背景服務
            services.AddHostedService<AuditLogBackgroundService>();
            
            // 註冊稽核配置
            services.Configure<AuditLogConfiguration>(configuration.GetSection("AuditLog"));
            
            return services;
        }

        /// <summary>
        /// 使用稽核日誌中介軟體
        /// </summary>
        public static IApplicationBuilder UseAuditLogMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuditLogMiddleware>();
        }
    }

    /// <summary>
    /// 稽核日誌配置類別
    /// </summary>
    public class AuditLogConfiguration
    {
        /// <summary>
        /// 是否啟用自動稽核
        /// </summary>
        public bool EnableAutoAudit { get; set; } = true;

        /// <summary>
        /// 是否記錄 API 呼叫
        /// </summary>
        public bool LogApiCalls { get; set; } = true;

        /// <summary>
        /// API 呼叫記錄的最小回應時間（毫秒）
        /// </summary>
        public int MinApiResponseTimeToLog { get; set; } = 1000;

        /// <summary>
        /// 是否記錄成功的讀取操作
        /// </summary>
        public bool LogSuccessfulReads { get; set; } = false;

        /// <summary>
        /// 批次處理大小
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// 批次處理間隔（秒）
        /// </summary>
        public int BatchIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// 預設資料保留天數
        /// </summary>
        public int DefaultRetentionDays { get; set; } = 90;

        /// <summary>
        /// 是否啟用敏感資料遮罩
        /// </summary>
        public bool EnableDataMasking { get; set; } = true;

        /// <summary>
        /// 是否啟用風險評估
        /// </summary>
        public bool EnableRiskAssessment { get; set; } = true;

        /// <summary>
        /// 可疑活動風險分數閾值
        /// </summary>
        public int SuspiciousRiskThreshold { get; set; } = 70;

        /// <summary>
        /// 需要即時處理的安全等級
        /// </summary>
        public string[] ImmediateProcessingLevels { get; set; } = { "HIGH", "CRITICAL" };

        /// <summary>
        /// 排除的端點路徑
        /// </summary>
        public string[] ExcludedPaths { get; set; } = { "/health", "/swagger", "/css", "/js", "/images" };

        /// <summary>
        /// 需要記錄的 HTTP 狀態碼
        /// </summary>
        public int[] LoggedStatusCodes { get; set; } = { 400, 401, 403, 404, 500, 502, 503 };

        /// <summary>
        /// 清理作業執行間隔（小時）
        /// </summary>
        public int CleanupIntervalHours { get; set; } = 6;

        /// <summary>
        /// 歸檔作業執行間隔（天）
        /// </summary>
        public int ArchiveIntervalDays { get; set; } = 7;

        /// <summary>
        /// 合規性報告配置
        /// </summary>
        public ComplianceReportConfiguration ComplianceReports { get; set; } = new();
    }

    /// <summary>
    /// 合規性報告配置
    /// </summary>
    public class ComplianceReportConfiguration
    {
        /// <summary>
        /// 報告儲存路徑
        /// </summary>
        public string StoragePath { get; set; } = "reports/compliance";

        /// <summary>
        /// 報告保留天數
        /// </summary>
        public int RetentionDays { get; set; } = 2555; // 7年

        /// <summary>
        /// 支援的報告類型
        /// </summary>
        public string[] SupportedTypes { get; set; } = { "GDPR", "HIPAA", "SOX", "PCI_DSS" };

        /// <summary>
        /// 自動生成報告的排程
        /// </summary>
        public AutoReportSchedule[] AutoSchedules { get; set; } = Array.Empty<AutoReportSchedule>();
    }

    /// <summary>
    /// 自動報告排程
    /// </summary>
    public class AutoReportSchedule
    {
        /// <summary>
        /// 報告類型
        /// </summary>
        public string ReportType { get; set; } = string.Empty;

        /// <summary>
        /// 執行頻率（天）
        /// </summary>
        public int FrequencyDays { get; set; } = 30;

        /// <summary>
        /// 報告期間（天）
        /// </summary>
        public int PeriodDays { get; set; } = 30;

        /// <summary>
        /// 收件人電子郵件
        /// </summary>
        public string[] Recipients { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool Enabled { get; set; } = false;
    }
}