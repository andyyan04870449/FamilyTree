using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using familytree_backend.Models;
using familytree_backend.Constants;

namespace familytree_backend.Services
{
    /// <summary>
    /// 審計日誌服務實作
    /// 提供完整的審計日誌功能
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly string _connectionString;
        private readonly ILogger<AuditLogService> _logger;
        
        // 請求上下文
        private string? _userId;
        private string? _userName;
        private string? _userRole;
        private string? _sessionId;
        private string? _ipAddress;
        private string? _userAgent;
        private string? _requestId;

        public AuditLogService(IConfiguration configuration, ILogger<AuditLogService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("DefaultConnection");
            _logger = logger;
        }

        /// <summary>
        /// 設定請求上下文
        /// </summary>
        public void SetRequestContext(string? userId, string? userName, string? userRole, 
            string? sessionId, string? ipAddress, string? userAgent, string? requestId)
        {
            _userId = userId;
            _userName = userName;
            _userRole = userRole;
            _sessionId = sessionId;
            _ipAddress = ipAddress;
            _userAgent = userAgent;
            _requestId = requestId;
        }

        /// <summary>
        /// 查詢審計日誌
        /// </summary>
        public async Task<AuditLogQueryResult> QueryLogsAsync(AuditLogFilterModel filter)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 建構查詢條件
            var conditions = new List<string> { "1=1" };
            var parameters = new DynamicParameters();

            if (!string.IsNullOrEmpty(filter.UserId))
            {
                conditions.Add("user_id = @UserId");
                parameters.Add("UserId", filter.UserId);
            }

            if (filter.EventTypes != null && filter.EventTypes.Length > 0)
            {
                conditions.Add("event_type = ANY(@EventTypes)");
                parameters.Add("EventTypes", filter.EventTypes);
            }

            if (filter.Actions != null && filter.Actions.Length > 0)
            {
                conditions.Add("action = ANY(@Actions)");
                parameters.Add("Actions", filter.Actions);
            }

            if (!string.IsNullOrEmpty(filter.ResourceType))
            {
                conditions.Add("resource_type = @ResourceType");
                parameters.Add("ResourceType", filter.ResourceType);
            }

            if (filter.FromDate.HasValue)
            {
                conditions.Add("occurred_at >= @FromDate");
                parameters.Add("FromDate", filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                conditions.Add("occurred_at <= @ToDate");
                parameters.Add("ToDate", filter.ToDate.Value);
            }

            if (filter.Success.HasValue)
            {
                conditions.Add("success = @Success");
                parameters.Add("Success", filter.Success.Value);
            }

            var whereClause = string.Join(" AND ", conditions);

            // 查詢總數
            var countSql = $"SELECT COUNT(*) FROM audit_logs WHERE {whereClause}";
            var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

            // 查詢資料（改進版本，LEFT JOIN users 表獲取使用者資訊）
            var offset = (filter.Page - 1) * filter.PageSize;
            var dataSql = $@"
                SELECT 
                    al.id as Id,
                    al.event_id as EventId,
                    al.batch_id as BatchId,
                    al.session_id as SessionId,
                    al.user_id as UserId,
                    -- 優先使用 audit_logs 中的 user_name，如果為空則從 users 表獲取，最後處理系統使用者
                    COALESCE(
                        NULLIF(al.user_name, ''), 
                        u.full_name, 
                        u.username,
                        CASE 
                            WHEN al.user_id = 'admin_default' THEN '系統管理員'
                            WHEN al.user_id = 'system' THEN '系統'
                            WHEN al.user_id IS NULL THEN 'Anonymous'
                            ELSE al.user_id
                        END
                    ) as UserName,
                    -- 優先使用 audit_logs 中的 user_role，如果為空則從 users 表獲取
                    COALESCE(
                        NULLIF(al.user_role, ''), 
                        u.role,
                        CASE 
                            WHEN al.user_id = 'admin_default' THEN 'admin'
                            WHEN al.user_id = 'system' THEN 'system'
                            ELSE 'unknown'
                        END
                    ) as UserRole,
                    al.impersonator_id as ImpersonatorId,
                    al.event_type as EventType,
                    al.action as Action,
                    al.resource_type as ResourceType,
                    al.resource_id as ResourceId,
                    al.resource_name as ResourceName,
                    al.old_values as OldValues,
                    al.new_values as NewValues,
                    al.changes_summary as ChangesSummary,
                    al.ip_address::text as IpAddress,
                    al.user_agent as UserAgent,
                    al.request_method as RequestMethod,
                    al.request_url as RequestUrl,
                    al.request_id as RequestId,
                    al.success as Success,
                    al.error_message as ErrorMessage,
                    al.error_code as ErrorCode,
                    al.response_time_ms as ResponseTimeMs,
                    al.security_level as SecurityLevel,
                    al.risk_score as RiskScore,
                    al.is_suspicious as IsSuspicious,
                    al.additional_data as AdditionalMetadata,
                    al.occurred_at as OccurredAt,
                    al.created_at as CreatedAt
                FROM audit_logs al
                LEFT JOIN users u ON al.user_id = u.id 
                WHERE {whereClause}
                ORDER BY al.occurred_at DESC
                LIMIT @PageSize OFFSET @Offset";

            parameters.Add("PageSize", filter.PageSize);
            parameters.Add("Offset", offset);

            var logs = await connection.QueryAsync<AuditLogModel>(dataSql, parameters);

            return new AuditLogQueryResult
            {
                Logs = logs,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        /// <summary>
        /// 獲取審計日誌摘要統計
        /// </summary>
        public async Task<IEnumerable<AuditLogSummaryModel>> GetSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT 
                    DATE(occurred_at) as Date,
                    event_type as EventType,
                    COUNT(*) as Count,
                    COUNT(DISTINCT user_id) as UniqueUsers,
                    SUM(CASE WHEN success = true THEN 1 ELSE 0 END) as SuccessCount,
                    SUM(CASE WHEN success = false THEN 1 ELSE 0 END) as FailureCount,
                    AVG(response_time_ms) as AvgResponseTime
                FROM audit_logs
                WHERE occurred_at >= @FromDate AND occurred_at <= @ToDate
                GROUP BY DATE(occurred_at), event_type
                ORDER BY Date DESC, Count DESC";

            var summaries = await connection.QueryAsync<AuditLogSummaryModel>(sql, new { FromDate = fromDate, ToDate = toDate });
            return summaries;
        }

        /// <summary>
        /// 獲取審計統計資料
        /// </summary>
        public async Task<AuditStatisticsDto> GetStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var statistics = new AuditStatisticsDto();

            // 總體統計
            var generalStatsSql = @"
                SELECT 
                    COUNT(*) as TotalEvents,
                    COUNT(DISTINCT user_id) as UniqueUsers,
                    COUNT(DISTINCT session_id) as UniqueSessions,
                    SUM(CASE WHEN success = true THEN 1 ELSE 0 END) as SuccessfulEvents,
                    SUM(CASE WHEN success = false THEN 1 ELSE 0 END) as FailedEvents,
                    AVG(response_time_ms) as AvgResponseTime,
                    MAX(response_time_ms) as MaxResponseTime,
                    MIN(response_time_ms) as MinResponseTime
                FROM audit_logs
                WHERE occurred_at >= @FromDate AND occurred_at <= @ToDate";

            var generalStats = await connection.QuerySingleAsync<dynamic>(generalStatsSql, new { FromDate = fromDate, ToDate = toDate });
            
            statistics.TotalEvents = generalStats.TotalEvents;
            statistics.UniqueUsers = generalStats.UniqueUsers;
            statistics.UniqueSessions = generalStats.UniqueSessions;
            statistics.SuccessfulEvents = generalStats.SuccessfulEvents;
            statistics.FailedEvents = generalStats.FailedEvents;
            statistics.AvgResponseTime = generalStats.AvgResponseTime;
            statistics.MaxResponseTime = generalStats.MaxResponseTime;
            statistics.MinResponseTime = generalStats.MinResponseTime;

            // 事件類型分佈
            var eventTypeStatsSql = @"
                SELECT event_type, COUNT(*) as count
                FROM audit_logs
                WHERE occurred_at >= @FromDate AND occurred_at <= @ToDate
                GROUP BY event_type
                ORDER BY count DESC";

            var eventTypeStats = await connection.QueryAsync<(string eventType, int count)>(eventTypeStatsSql, 
                new { FromDate = fromDate, ToDate = toDate });
            statistics.EventTypeDistribution = eventTypeStats.ToDictionary(x => x.eventType, x => x.count);

            // 使用者活動排行（改進版本，LEFT JOIN users 表獲取使用者資訊）
            var topUsersSql = @"
                SELECT 
                    al.user_id, 
                    COALESCE(
                        NULLIF(al.user_name, ''), 
                        u.full_name, 
                        u.username,
                        CASE 
                            WHEN al.user_id = 'admin_default' THEN '系統管理員'
                            WHEN al.user_id = 'system' THEN '系統'
                            ELSE al.user_id
                        END
                    ) as user_name,
                    COUNT(*) as event_count
                FROM audit_logs al
                LEFT JOIN users u ON al.user_id = u.id
                WHERE al.occurred_at >= @FromDate AND al.occurred_at <= @ToDate
                    AND al.user_id IS NOT NULL
                GROUP BY al.user_id, 
                    COALESCE(
                        NULLIF(al.user_name, ''), 
                        u.full_name, 
                        u.username,
                        CASE 
                            WHEN al.user_id = 'admin_default' THEN '系統管理員'
                            WHEN al.user_id = 'system' THEN '系統'
                            ELSE al.user_id
                        END
                    )
                ORDER BY event_count DESC
                LIMIT 10";

            var topUsers = await connection.QueryAsync<UserActivityModel>(topUsersSql, 
                new { FromDate = fromDate, ToDate = toDate });
            statistics.TopActiveUsers = topUsers;

            // 風險事件統計
            var riskEventsSql = @"
                SELECT COUNT(*) 
                FROM audit_logs
                WHERE occurred_at >= @FromDate AND occurred_at <= @ToDate
                    AND (risk_score >= 80 OR is_suspicious = true)";

            statistics.HighRiskEvents = await connection.QuerySingleAsync<int>(riskEventsSql, 
                new { FromDate = fromDate, ToDate = toDate });

            return statistics;
        }

        /// <summary>
        /// 匯出審計日誌
        /// </summary>
        public async Task<byte[]> ExportLogsAsync(AuditLogFilterModel filter, string format)
        {
            var result = await QueryLogsAsync(filter);
            
            if (format.ToUpper() == "CSV")
            {
                return ExportToCsv(result.Logs);
            }
            else if (format.ToUpper() == "JSON")
            {
                return ExportToJson(result.Logs);
            }
            else
            {
                throw new NotSupportedException($"不支援的匯出格式: {format}");
            }
        }

        /// <summary>
        /// 記錄審計事件
        /// </summary>
        public async Task<long> LogEventAsync(string eventType, string action, object? details = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 自動補全使用者資訊
            var enrichedUserInfo = await EnrichUserInfoAsync(connection, _userId, _userName, _userRole);

            var sql = @"
                INSERT INTO audit_logs (
                    event_id, session_id, user_id, user_name, user_role,
                    event_type, action, ip_address, user_agent, request_id,
                    success, occurred_at, created_at, additional_data
                ) VALUES (
                    gen_random_uuid(), @SessionId, @UserId, @UserName, @UserRole,
                    @EventType, @Action, @IpAddress::inet, @UserAgent, @RequestId,
                    true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, @Metadata::jsonb
                ) RETURNING id";

            var metadata = details != null ? JsonSerializer.Serialize(details) : null;

            var id = await connection.QuerySingleAsync<long>(sql, new
            {
                SessionId = _sessionId,
                UserId = enrichedUserInfo.UserId,
                UserName = enrichedUserInfo.UserName,
                UserRole = enrichedUserInfo.UserRole,
                EventType = eventType,
                Action = action,
                IpAddress = _ipAddress,
                UserAgent = _userAgent,
                RequestId = _requestId,
                Metadata = metadata
            });

            return id;
        }

        /// <summary>
        /// 記錄審計事件（使用 DTO）
        /// </summary>
        public async Task<long> LogEventAsync(CreateAuditLogDto auditLog)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 自動補全使用者資訊
            var enrichedUserInfo = await EnrichUserInfoAsync(connection, 
                auditLog.UserId ?? _userId, 
                auditLog.UserName ?? _userName, 
                auditLog.UserRole ?? _userRole);

            var sql = @"
                INSERT INTO audit_logs (
                    event_id, batch_id, session_id, user_id, user_name, user_role,
                    event_type, action, resource_type, resource_id, resource_name,
                    old_values, new_values, changes_summary, ip_address, user_agent,
                    request_method, request_url, request_id, success, error_message,
                    error_code, response_time_ms, security_level, risk_score,
                    is_suspicious, additional_data, occurred_at, created_at
                ) VALUES (
                    gen_random_uuid(), @BatchId, @SessionId, @UserId, @UserName, @UserRole,
                    @EventType, @Action, @ResourceType, @ResourceId, @ResourceName,
                    @OldValues::jsonb, @NewValues::jsonb, @ChangesSummary, @IpAddress::inet, @UserAgent,
                    @RequestMethod, @RequestUrl, @RequestId, @Success, @ErrorMessage,
                    @ErrorCode, @ResponseTimeMs, @SecurityLevel, @RiskScore,
                    @IsSuspicious, @AdditionalMetadata::jsonb, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                ) RETURNING id";

            var id = await connection.QuerySingleAsync<long>(sql, new
            {
                auditLog.BatchId,
                SessionId = auditLog.SessionId ?? _sessionId,
                UserId = enrichedUserInfo.UserId,
                UserName = enrichedUserInfo.UserName,
                UserRole = enrichedUserInfo.UserRole,
                auditLog.EventType,
                auditLog.Action,
                auditLog.ResourceType,
                auditLog.ResourceId,
                auditLog.ResourceName,
                OldValues = auditLog.OldValues != null ? JsonSerializer.Serialize(auditLog.OldValues) : null,
                NewValues = auditLog.NewValues != null ? JsonSerializer.Serialize(auditLog.NewValues) : null,
                auditLog.ChangesSummary,
                IpAddress = auditLog.IpAddress ?? _ipAddress,
                UserAgent = auditLog.UserAgent ?? _userAgent,
                auditLog.RequestMethod,
                auditLog.RequestUrl,
                RequestId = auditLog.RequestId ?? _requestId,
                auditLog.Success,
                auditLog.ErrorMessage,
                auditLog.ErrorCode,
                auditLog.ResponseTimeMs,
                auditLog.SecurityLevel,
                auditLog.RiskScore,
                auditLog.IsSuspicious,
                AdditionalMetadata = auditLog.AdditionalMetadata != null ? JsonSerializer.Serialize(auditLog.AdditionalMetadata) : null
            });

            return id;
        }

        /// <summary>
        /// 記錄 API 呼叫
        /// </summary>
        public async Task LogApiCallAsync(string method, string path, int statusCode, long responseTimeMs, string? errorMessage = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var success = statusCode >= 200 && statusCode < 400;
            var eventType = success ? AuditEventTypes.API_CALL : AuditEventTypes.API_ERROR;

            // 自動補全使用者資訊
            var enrichedUserInfo = await EnrichUserInfoAsync(connection, _userId, _userName, _userRole);

            var sql = @"
                INSERT INTO audit_logs (
                    event_id, session_id, user_id, user_name, user_role,
                    event_type, action, request_method, request_url,
                    ip_address, user_agent, request_id, success,
                    error_message, response_time_ms, occurred_at, created_at
                ) VALUES (
                    gen_random_uuid(), @SessionId, @UserId, @UserName, @UserRole,
                    @EventType, @Action, @Method, @Path,
                    @IpAddress::inet, @UserAgent, @RequestId, @Success,
                    @ErrorMessage, @ResponseTimeMs, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                )";

            await connection.ExecuteAsync(sql, new
            {
                SessionId = _sessionId,
                UserId = enrichedUserInfo.UserId,
                UserName = enrichedUserInfo.UserName,
                UserRole = enrichedUserInfo.UserRole,
                EventType = eventType,
                Action = $"{method} {statusCode}",
                Method = method,
                Path = path,
                IpAddress = _ipAddress,
                UserAgent = _userAgent,
                RequestId = _requestId,
                Success = success,
                ErrorMessage = errorMessage,
                ResponseTimeMs = responseTimeMs
            });
        }

        /// <summary>
        /// 生成合規性報告
        /// </summary>
        public async Task<string> GenerateComplianceReportAsync(string reportType, DateTime fromDate, DateTime toDate, string requestedBy)
        {
            var reportId = Guid.NewGuid().ToString();
            
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 記錄報告生成請求
            var sql = @"
                INSERT INTO audit_compliance_reports (
                    report_id, report_type, requested_by, requested_at,
                    from_date, to_date, status, created_at, updated_at
                ) VALUES (
                    @ReportId, @ReportType, @RequestedBy, CURRENT_TIMESTAMP,
                    @FromDate, @ToDate, 'PENDING', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                )";

            await connection.ExecuteAsync(sql, new
            {
                ReportId = reportId,
                ReportType = reportType,
                RequestedBy = requestedBy,
                FromDate = fromDate,
                ToDate = toDate
            });

            // 實際的報告生成邏輯應該在背景任務中執行
            _ = Task.Run(async () => await GenerateReportAsync(reportId, reportType, fromDate, toDate));

            return reportId;
        }

        /// <summary>
        /// 清理過期的審計日誌
        /// </summary>
        public async Task<int> CleanupExpiredLogsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 預設保留90天的日誌
            var retentionDays = 90;
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var sql = @"
                DELETE FROM audit_logs 
                WHERE created_at < @CutoffDate 
                    AND event_type NOT IN (
                        'SECURITY_BREACH', 'SUSPICIOUS_ACTIVITY', 
                        'PERMISSION_GRANTED', 'PERMISSION_REVOKED'
                    )";

            var deletedCount = await connection.ExecuteAsync(sql, new { CutoffDate = cutoffDate });

            // 記錄清理操作
            await LogEventAsync(AuditEventTypes.SYSTEM_MAINTENANCE, "CLEANUP", new
            {
                DeletedCount = deletedCount,
                CutoffDate = cutoffDate,
                RetentionDays = retentionDays
            });

            return deletedCount;
        }

        #region 私有方法

        /// <summary>
        /// 自動補全使用者資訊
        /// </summary>
        /// <param name="connection">資料庫連線</param>
        /// <param name="userId">使用者ID</param>
        /// <param name="userName">現有使用者名稱</param>
        /// <param name="userRole">現有使用者角色</param>
        /// <returns>補全後的使用者資訊</returns>
        private async Task<EnrichedUserInfo> EnrichUserInfoAsync(NpgsqlConnection connection, 
            string? userId, string? userName, string? userRole)
        {
            // 如果已經有完整的使用者資訊，直接返回
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(userRole))
            {
                return new EnrichedUserInfo 
                { 
                    UserId = userId, 
                    UserName = userName, 
                    UserRole = userRole 
                };
            }

            // 處理系統使用者的特殊情況
            if (!string.IsNullOrEmpty(userId))
            {
                var systemUserInfo = GetSystemUserInfo(userId);
                if (systemUserInfo != null)
                {
                    return systemUserInfo;
                }
            }

            // 如果有 userId 但缺少其他資訊，從 users 表查詢
            if (!string.IsNullOrEmpty(userId))
            {
                var userInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    SqlQueries.AuditLogs.EnrichUserInfo, 
                    new { UserId = userId });
                
                if (userInfo != null)
                {
                    return new EnrichedUserInfo
                    {
                        UserId = userId,
                        UserName = userName ?? userInfo.full_name ?? userInfo.username ?? userId,
                        UserRole = userRole ?? userInfo.role ?? "unknown"
                    };
                }
            }

            // 如果無法獲取使用者資訊，返回預設值
            return new EnrichedUserInfo
            {
                UserId = userId ?? "anonymous",
                UserName = userName ?? "Anonymous",
                UserRole = userRole ?? "unknown"
            };
        }

        /// <summary>
        /// 獲取系統使用者資訊
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <returns>系統使用者資訊，如果不是系統使用者則返回 null</returns>
        private static EnrichedUserInfo? GetSystemUserInfo(string userId)
        {
            return userId switch
            {
                "admin_default" => new EnrichedUserInfo 
                { 
                    UserId = userId, 
                    UserName = "系統管理員", 
                    UserRole = "admin" 
                },
                "system" => new EnrichedUserInfo 
                { 
                    UserId = userId, 
                    UserName = "系統", 
                    UserRole = "system" 
                },
                "anonymous" => new EnrichedUserInfo 
                { 
                    UserId = userId, 
                    UserName = "Anonymous", 
                    UserRole = "anonymous" 
                },
                _ => null
            };
        }

        private byte[] ExportToCsv(IEnumerable<AuditLogModel> logs)
        {
            var csv = new StringBuilder();
            csv.AppendLine("ID,EventID,SessionID,UserID,UserName,EventType,Action,ResourceType,ResourceID,Success,OccurredAt,IPAddress");

            foreach (var log in logs)
            {
                csv.AppendLine($"{log.Id},{log.EventId},{log.SessionId},{log.UserId},{log.UserName}," +
                    $"{log.EventType},{log.Action},{log.ResourceType},{log.ResourceId}," +
                    $"{log.Success},{log.OccurredAt:yyyy-MM-dd HH:mm:ss},{log.IpAddress}");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        private byte[] ExportToJson(IEnumerable<AuditLogModel> logs)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return Encoding.UTF8.GetBytes(json);
        }

        private async Task GenerateReportAsync(string reportId, string reportType, DateTime fromDate, DateTime toDate)
        {
            try
            {
                // TODO: 實作具體的報告生成邏輯
                await Task.Delay(5000); // 模擬報告生成

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE audit_compliance_reports 
                    SET status = 'COMPLETED', 
                        completed_at = CURRENT_TIMESTAMP,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE report_id = @ReportId";

                await connection.ExecuteAsync(sql, new { ReportId = reportId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成合規性報告失敗: {ReportId}", reportId);

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE audit_compliance_reports 
                    SET status = 'FAILED', 
                        error_message = @ErrorMessage,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE report_id = @ReportId";

                await connection.ExecuteAsync(sql, new { ReportId = reportId, ErrorMessage = ex.Message });
            }
        }

        #endregion
    }
}