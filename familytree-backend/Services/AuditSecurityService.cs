using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using familytree_backend.Models;
using FamilyTree.Constants;

namespace familytree_backend.Services
{
    /// <summary>
    /// 稽核安全服務介面
    /// 處理稽核日誌的安全性、權限控制和敏感資料保護
    /// </summary>
    public interface IAuditSecurityService
    {
        // 權限檢查
        Task<bool> CanAccessAuditLogsAsync(string userId, string userRole);
        Task<bool> CanViewSensitiveDataAsync(string userId, string userRole);
        Task<bool> CanManageAuditSystemAsync(string userId, string userRole);
        Task<bool> CanAccessUserLogsAsync(string requesterId, string requesterRole, string targetUserId);
        
        // 資料遮罩和清理
        AuditLogModel MaskSensitiveData(AuditLogModel log, string viewerRole);
        IEnumerable<AuditLogModel> MaskSensitiveData(IEnumerable<AuditLogModel> logs, string viewerRole);
        object MaskJsonData(object data, string[] sensitiveFields);
        
        // 存取記錄
        Task RecordAuditAccessAsync(string userId, string accessType, object searchCriteria, int recordCount);
        
        // 風險評估
        int CalculateRiskScore(CreateAuditLogDto auditLog);
        bool IsSuspiciousActivity(CreateAuditLogDto auditLog, string userId);
        
        // 合規性檢查
        bool RequiresApprovalForAccess(AuditLogFilterModel filter, string userRole);
        Task<List<string>> GetComplianceViolationsAsync(AuditLogModel log);
        
        // 資料分類
        string ClassifyDataSensitivity(string fieldName, object value);
        bool IsPersonallyIdentifiableInfo(string fieldName, object value);
    }

    /// <summary>
    /// 稽核安全服務實作
    /// 提供完整的稽核日誌安全性保護機制
    /// </summary>
    public class AuditSecurityService : IAuditSecurityService
    {
        private readonly ILogger<AuditSecurityService> _logger;
        private readonly IUserService _userService;
        
        // 敏感欄位定義
        private readonly HashSet<string> _sensitiveFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "password", "pwd", "secret", "token", "key", "ssn", "social_security_number",
            "credit_card", "card_number", "cvv", "pin", "passport", "license_number",
            "phone", "mobile", "email", "address", "birthday", "birth_date",
            "salary", "income", "bank_account", "account_number", "routing_number"
        };

        // PII 欄位模式
        private readonly Dictionary<string, Regex> _piiPatterns = new()
        {
            { "email", new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b") },
            { "phone", new Regex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b") },
            { "ssn", new Regex(@"\b\d{3}-\d{2}-\d{4}\b") },
            { "credit_card", new Regex(@"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b") },
            { "ip_address", new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b") }
        };

        // 可疑活動模式
        private readonly Dictionary<string, SuspiciousPattern> _suspiciousPatterns = new()
        {
            { "multiple_failed_logins", new SuspiciousPattern { Threshold = 5, TimeWindow = TimeSpan.FromMinutes(15) } },
            { "privilege_escalation", new SuspiciousPattern { Threshold = 3, TimeWindow = TimeSpan.FromHours(1) } },
            { "bulk_data_access", new SuspiciousPattern { Threshold = 100, TimeWindow = TimeSpan.FromMinutes(30) } },
            { "off_hours_access", new SuspiciousPattern { Threshold = 1, TimeWindow = TimeSpan.FromDays(1) } },
            { "suspicious_ip", new SuspiciousPattern { Threshold = 1, TimeWindow = TimeSpan.FromDays(1) } }
        };

        private class SuspiciousPattern
        {
            public int Threshold { get; set; }
            public TimeSpan TimeWindow { get; set; }
        }

        public AuditSecurityService(ILogger<AuditSecurityService> logger, IUserService userService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        #region 權限檢查

        /// <summary>
        /// 檢查是否可以存取稽核日誌
        /// </summary>
        public async Task<bool> CanAccessAuditLogsAsync(string userId, string userRole)
        {
            try
            {
                // 管理員有完整存取權限
                if (userRole == RoleConstants.ADMIN)
                    return true;

                // 檢查是否有稽核讀取權限（基於角色）
                var user = await _userService.GetByIdAsync(userId);
                if (user?.Role == RoleConstants.ADMIN || user?.Role == RoleConstants.AUDITOR)
                    return true;

                // 檢查特定角色權限
                if (userRole == RoleConstants.AUDIT_READER || userRole == RoleConstants.SECURITY_OFFICER)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查稽核日誌存取權限失敗: UserId={UserId}, Role={Role}", userId, userRole);
                return false;
            }
        }

        /// <summary>
        /// 檢查是否可以檢視敏感資料
        /// </summary>
        public async Task<bool> CanViewSensitiveDataAsync(string userId, string userRole)
        {
            try
            {
                // 只有管理員和安全官可以檢視敏感資料
                if (userRole == RoleConstants.ADMIN || userRole == RoleConstants.SECURITY_OFFICER)
                    return true;

                // 檢查特定權限（基於角色）
                var user = await _userService.GetByIdAsync(userId);
                return user?.Role == RoleConstants.ADMIN || user?.Role == RoleConstants.SECURITY_ADMIN;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查敏感資料檢視權限失敗: UserId={UserId}, Role={Role}", userId, userRole);
                return false;
            }
        }

        /// <summary>
        /// 檢查是否可以管理稽核系統
        /// </summary>
        public async Task<bool> CanManageAuditSystemAsync(string userId, string userRole)
        {
            try
            {
                // 只有管理員可以管理稽核系統
                if (userRole == RoleConstants.ADMIN)
                    return true;

                // 檢查特定權限（基於角色）
                var user = await _userService.GetByIdAsync(userId);
                return user?.Role == RoleConstants.ADMIN;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查稽核系統管理權限失敗: UserId={UserId}, Role={Role}", userId, userRole);
                return false;
            }
        }

        /// <summary>
        /// 檢查是否可以存取特定使用者的日誌
        /// </summary>
        public async Task<bool> CanAccessUserLogsAsync(string requesterId, string requesterRole, string targetUserId)
        {
            try
            {
                // 管理員可以存取所有使用者的日誌
                if (requesterRole == RoleConstants.ADMIN)
                    return true;

                // 使用者可以存取自己的日誌
                if (requesterId == targetUserId)
                    return true;

                // 安全官可以存取所有使用者的日誌
                if (requesterRole == RoleConstants.SECURITY_OFFICER)
                    return true;

                // 檢查是否有特定權限（基於角色）
                var user = await _userService.GetByIdAsync(requesterId);
                return user?.Role == RoleConstants.ADMIN || user?.Role == RoleConstants.AUDITOR;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查使用者日誌存取權限失敗: RequesterId={RequesterId}, TargetUserId={TargetUserId}", 
                    requesterId, targetUserId);
                return false;
            }
        }

        #endregion

        #region 資料遮罩和清理

        /// <summary>
        /// 遮罩單個日誌記錄的敏感資料
        /// </summary>
        public AuditLogModel MaskSensitiveData(AuditLogModel log, string viewerRole)
        {
            if (log == null) return log;

            var maskedLog = new AuditLogModel
            {
                Id = log.Id,
                EventId = log.EventId,
                BatchId = log.BatchId,
                SessionId = ShouldMaskField("session_id", viewerRole) ? MaskValue(log.SessionId) : log.SessionId,
                UserId = log.UserId,
                UserName = log.UserName,
                UserRole = log.UserRole,
                ImpersonatorId = log.ImpersonatorId,
                EventType = log.EventType,
                Action = log.Action,
                ResourceType = log.ResourceType,
                ResourceId = log.ResourceId,
                ResourceName = log.ResourceName,
                OldValues = MaskJsonData(log.OldValues, viewerRole),
                NewValues = MaskJsonData(log.NewValues, viewerRole),
                ChangesSummary = log.ChangesSummary,
                IpAddress = ShouldMaskField("ip_address", viewerRole) ? MaskIpAddress(log.IpAddress) : log.IpAddress,
                UserAgent = ShouldMaskField("user_agent", viewerRole) ? MaskValue(log.UserAgent) : log.UserAgent,
                RequestMethod = log.RequestMethod,
                RequestUrl = ShouldMaskField("request_url", viewerRole) ? MaskUrl(log.RequestUrl) : log.RequestUrl,
                RequestId = log.RequestId,
                Success = log.Success,
                ErrorMessage = log.ErrorMessage,
                ErrorCode = log.ErrorCode,
                ResponseTimeMs = log.ResponseTimeMs,
                SecurityLevel = log.SecurityLevel,
                RiskScore = log.RiskScore,
                IsSuspicious = log.IsSuspicious,
                OccurredAt = log.OccurredAt,
                CreatedAt = log.CreatedAt,
                AdditionalData = MaskJsonData(log.AdditionalData, viewerRole),
                Tags = log.Tags,
                ComplianceFlags = log.ComplianceFlags
            };

            return maskedLog;
        }

        /// <summary>
        /// 遮罩多個日誌記錄的敏感資料
        /// </summary>
        public IEnumerable<AuditLogModel> MaskSensitiveData(IEnumerable<AuditLogModel> logs, string viewerRole)
        {
            return logs?.Select(log => MaskSensitiveData(log, viewerRole)) ?? Enumerable.Empty<AuditLogModel>();
        }

        /// <summary>
        /// 遮罩 JSON 資料中的敏感欄位
        /// </summary>
        public object MaskJsonData(object data, string[] sensitiveFields)
        {
            if (data == null) return null;

            try
            {
                var jsonString = data is JsonDocument doc ? doc.RootElement.GetRawText() : JsonSerializer.Serialize(data);
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
                
                return MaskJsonElement(jsonElement, sensitiveFields);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "遮罩JSON資料失敗");
                return data;
            }
        }

        /// <summary>
        /// 遮罩 JSON 資料（依據檢視者角色）
        /// </summary>
        private JsonDocument? MaskJsonData(JsonDocument? data, string viewerRole)
        {
            if (data == null) return null;

            try
            {
                var maskedElement = MaskJsonElement(data.RootElement, viewerRole);
                var maskedJson = JsonSerializer.Serialize(maskedElement);
                return JsonDocument.Parse(maskedJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "遮罩JSON資料失敗");
                return data;
            }
        }

        private object MaskJsonElement(JsonElement element, string viewerRole)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var result = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var value = MaskJsonElement(property.Value, viewerRole);
                        if (ShouldMaskField(property.Name, viewerRole))
                        {
                            value = MaskValue(value?.ToString());
                        }
                        result[property.Name] = value;
                    }
                    return result;

                case JsonValueKind.Array:
                    return element.EnumerateArray()
                        .Select(item => MaskJsonElement(item, viewerRole))
                        .ToArray();

                default:
                    return element.ToString();
            }
        }

        private object MaskJsonElement(JsonElement element, string[] sensitiveFields)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var result = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var value = MaskJsonElement(property.Value, sensitiveFields);
                        if (sensitiveFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            value = MaskValue(value?.ToString());
                        }
                        result[property.Name] = value;
                    }
                    return result;

                case JsonValueKind.Array:
                    return element.EnumerateArray()
                        .Select(item => MaskJsonElement(item, sensitiveFields))
                        .ToArray();

                default:
                    return element.ToString();
            }
        }

        #endregion

        #region 存取記錄

        /// <summary>
        /// 記錄稽核日誌存取
        /// </summary>
        public async Task RecordAuditAccessAsync(string userId, string accessType, object searchCriteria, int recordCount)
        {
            try
            {
                // 這裡應該記錄到稽核存取表
                _logger.LogInformation("稽核日誌存取記錄: UserId={UserId}, AccessType={AccessType}, RecordCount={RecordCount}", 
                    userId, accessType, recordCount);
                
                // 實際實作會寫入 audit_log_access 表
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄稽核存取失敗: UserId={UserId}", userId);
            }
        }

        #endregion

        #region 風險評估

        /// <summary>
        /// 計算風險評分
        /// </summary>
        public int CalculateRiskScore(CreateAuditLogDto auditLog)
        {
            int score = 0;

            try
            {
                // 基於事件類型的風險評分
                score += GetEventTypeRiskScore(auditLog.EventType);

                // 基於操作類型的風險評分
                score += GetActionRiskScore(auditLog.Action);

                // 失敗操作增加風險
                if (!auditLog.Success)
                    score += 20;

                // 非工作時間操作增加風險
                if (IsOffHours(DateTime.UtcNow))
                    score += 15;

                // 可疑IP地址增加風險
                if (IsSuspiciousIpAddress(auditLog.IpAddress))
                    score += 25;

                // 高特權操作增加風險
                if (IsHighPrivilegeOperation(auditLog))
                    score += 30;

                // 敏感資源操作增加風險
                if (IsSensitiveResource(auditLog.ResourceType))
                    score += 20;

                // 限制最大值
                return Math.Min(score, 100);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "計算風險評分失敗");
                return 0;
            }
        }

        /// <summary>
        /// 檢測是否為可疑活動
        /// </summary>
        public bool IsSuspiciousActivity(CreateAuditLogDto auditLog, string userId)
        {
            try
            {
                // 多次登入失敗
                if (auditLog.EventType == AuditEventTypes.LOGIN_FAILED)
                {
                    // 這裡應該查詢最近的失敗次數
                    return true; // 簡化實作
                }

                // 權限提升操作
                if (auditLog.EventType.Contains("PERMISSION") || auditLog.EventType.Contains("ROLE"))
                {
                    return true;
                }

                // 大量資料存取
                if (auditLog.EventType == AuditEventTypes.DATA_EXPORT || 
                    auditLog.EventType == AuditEventTypes.DATA_VIEW)
                {
                    // 檢查短時間內的存取量
                    return false; // 簡化實作
                }

                // 非工作時間的系統操作
                if (IsSystemOperation(auditLog) && IsOffHours(DateTime.UtcNow))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "檢測可疑活動失敗");
                return false;
            }
        }

        #endregion

        #region 合規性檢查

        /// <summary>
        /// 檢查是否需要審批存取
        /// </summary>
        public bool RequiresApprovalForAccess(AuditLogFilterModel filter, string userRole)
        {
            // 管理員不需要審批
            if (userRole == RoleConstants.ADMIN)
                return false;

            // 檢視敏感資料需要審批
            if (filter.IncludeChangeDetails == true || filter.MaskSensitiveData == false)
                return true;

            // 大範圍查詢需要審批
            if (filter.FromDate != null && filter.ToDate != null)
            {
                if (DateTime.TryParse(filter.FromDate.ToString(), out var fromDate) && 
                    DateTime.TryParse(filter.ToDate.ToString(), out var toDate))
                {
                    var timeSpan = toDate - fromDate;
                    if (timeSpan.TotalDays > 30)
                        return true;
                }
            }

            // 查詢所有使用者的資料需要審批
            if (string.IsNullOrEmpty(filter.UserId))
                return true;

            return false;
        }

        /// <summary>
        /// 獲取合規性違規清單
        /// </summary>
        public async Task<List<string>> GetComplianceViolationsAsync(AuditLogModel log)
        {
            var violations = new List<string>();

            try
            {
                // GDPR 檢查
                if (ContainsPersonalData(log) && !HasGdprCompliance(log))
                {
                    violations.Add("GDPR: 個人資料處理未符合 GDPR 要求");
                }

                // SOX 檢查（財務相關）
                if (IsFinancialData(log) && !HasSoxCompliance(log))
                {
                    violations.Add("SOX: 財務資料存取未符合 SOX 要求");
                }

                // 資料保留政策檢查
                if (IsExpiredData(log))
                {
                    violations.Add("DATA_RETENTION: 存取過期資料");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "合規性檢查失敗");
            }

            return violations;
        }

        #endregion

        #region 資料分類

        /// <summary>
        /// 分類資料敏感性
        /// </summary>
        public string ClassifyDataSensitivity(string fieldName, object value)
        {
            if (IsPersonallyIdentifiableInfo(fieldName, value))
                return "PII";

            if (_sensitiveFields.Contains(fieldName))
                return "SENSITIVE";

            if (IsFinancialField(fieldName))
                return "FINANCIAL";

            return "PUBLIC";
        }

        /// <summary>
        /// 檢查是否為個人識別資訊
        /// </summary>
        public bool IsPersonallyIdentifiableInfo(string fieldName, object value)
        {
            if (value == null) return false;

            var valueString = value.ToString();
            if (string.IsNullOrEmpty(valueString)) return false;

            // 檢查欄位名稱
            var piiFields = new[] { "ssn", "social_security", "passport", "license", "phone", "email", "address" };
            if (piiFields.Any(field => fieldName.Contains(field, StringComparison.OrdinalIgnoreCase)))
                return true;

            // 檢查值的模式
            foreach (var pattern in _piiPatterns.Values)
            {
                if (pattern.IsMatch(valueString))
                    return true;
            }

            return false;
        }

        #endregion

        #region 私有輔助方法

        private bool ShouldMaskField(string fieldName, string viewerRole)
        {
            // 管理員和安全官可以看到所有資料
            if (viewerRole == RoleConstants.ADMIN || viewerRole == RoleConstants.SECURITY_OFFICER)
                return false;

            return _sensitiveFields.Contains(fieldName) || IsPersonallyIdentifiableInfo(fieldName, "");
        }

        private string MaskValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= 4)
                return "***";

            return value.Substring(0, 2) + new string('*', value.Length - 4) + value.Substring(value.Length - 2);
        }

        private string MaskIpAddress(string? ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return ipAddress;

            var parts = ipAddress.Split('.');
            if (parts.Length == 4)
                return $"{parts[0]}.{parts[1]}.*.***";

            return "***.***.***";
        }

        private string MaskUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            try
            {
                var uri = new Uri(url);
                return $"{uri.Scheme}://{uri.Host}/***";
            }
            catch
            {
                return "***";
            }
        }

        private int GetEventTypeRiskScore(string eventType)
        {
            return eventType switch
            {
                AuditEventTypes.SECURITY_BREACH => 50,
                AuditEventTypes.SUSPICIOUS_ACTIVITY => 40,
                AuditEventTypes.LOGIN_FAILED => 20,
                AuditEventTypes.PERMISSION_GRANTED => 25,
                AuditEventTypes.PERMISSION_REVOKED => 25,
                AuditEventTypes.ROLE_CHANGED => 30,
                AuditEventTypes.DATA_DELETE => 25,
                AuditEventTypes.SYSTEM_CONFIG => 20,
                AuditEventTypes.PASSWORD_RESET => 15,
                _ => 5
            };
        }

        private int GetActionRiskScore(string action)
        {
            return action.ToUpper() switch
            {
                "DELETE" => 20,
                "MODIFY" => 15,
                "EXPORT" => 15,
                "GRANT" => 20,
                "REVOKE" => 20,
                "ESCALATE" => 25,
                _ => 5
            };
        }

        private bool IsOffHours(DateTime dateTime)
        {
            var hour = dateTime.Hour;
            return hour < 7 || hour > 19; // 工作時間 7:00 - 19:00
        }

        private bool IsSuspiciousIpAddress(string? ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return false;

            // 這裡應該檢查已知的惡意IP清單或異常IP模式
            // 簡化實作
            return false;
        }

        private bool IsHighPrivilegeOperation(CreateAuditLogDto auditLog)
        {
            var privilegeOperations = new[] { "GRANT", "REVOKE", "ESCALATE", "ADMIN", "ROOT" };
            return privilegeOperations.Any(op => 
                auditLog.Action.Contains(op, StringComparison.OrdinalIgnoreCase) ||
                auditLog.EventType.Contains(op, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsSensitiveResource(string? resourceType)
        {
            if (string.IsNullOrEmpty(resourceType))
                return false;

            var sensitiveTypes = new[] { "User", "Permission", "Role", "System", "Config", "Key" };
            return sensitiveTypes.Contains(resourceType, StringComparer.OrdinalIgnoreCase);
        }

        private bool IsSystemOperation(CreateAuditLogDto auditLog)
        {
            return auditLog.EventType.StartsWith("SYSTEM_") || 
                   auditLog.ResourceType == "System";
        }

        private bool ContainsPersonalData(AuditLogModel log)
        {
            // 檢查是否包含個人資料
            return log.ResourceType == "Person" || 
                   log.EventType.Contains("PERSONAL") ||
                   (log.AdditionalData != null && ContainsPersonalDataInJson(log.AdditionalData));
        }

        private bool ContainsPersonalDataInJson(JsonDocument data)
        {
            // 簡化實作，實際應該深度檢查JSON內容
            return data.RootElement.EnumerateObject()
                .Any(prop => IsPersonallyIdentifiableInfo(prop.Name, prop.Value.ToString()));
        }

        private bool HasGdprCompliance(AuditLogModel log)
        {
            return log.ComplianceFlags?.Contains("GDPR") == true;
        }

        private bool HasSoxCompliance(AuditLogModel log)
        {
            return log.ComplianceFlags?.Contains("SOX") == true;
        }

        private bool IsFinancialData(AuditLogModel log)
        {
            var financialTypes = new[] { "Financial", "Payment", "Invoice", "Salary" };
            return financialTypes.Contains(log.ResourceType, StringComparer.OrdinalIgnoreCase);
        }

        private bool IsFinancialField(string fieldName)
        {
            var financialFields = new[] { "salary", "income", "payment", "account", "bank", "credit" };
            return financialFields.Any(field => fieldName.Contains(field, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsExpiredData(AuditLogModel log)
        {
            // 檢查資料是否已過期（根據保留政策）
            var retentionPeriod = TimeSpan.FromDays(90); // 預設保留期
            return log.OccurredAt < DateTime.UtcNow - retentionPeriod;
        }

        #endregion
    }
}