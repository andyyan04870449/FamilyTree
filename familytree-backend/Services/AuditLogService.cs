using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Dapper;
using familytree_backend.Models;
using familytree_backend.Constants;
using System.Net;

namespace familytree_backend.Services
{
    /// <summary>
    /// 稽核日誌服務介面
    /// </summary>
    public interface IAuditLogService
    {
        // 基本日誌記錄
        Task<long> LogEventAsync(CreateAuditLogDto auditLog);
        Task<long> LogEventAsync(string eventType, string action, object? context = null);
        Task LogAsync(AuditLog auditLog);
        
        // 批次日誌記錄
        Task<Guid> StartBatchAsync(string batchDescription = "");
        Task CompleteBatchAsync(Guid batchId);
        Task<long> LogEventToBatchAsync(Guid batchId, CreateAuditLogDto auditLog);
        
        // 專用日誌方法
        Task LogAuthenticationEventAsync(string eventType, string userId, bool success, string? errorMessage = null, object? additionalData = null);
        Task LogDataOperationAsync(string action, string resourceType, string resourceId, object? oldValues = null, object? newValues = null, string? changesSummary = null);
        Task LogFileOperationAsync(string action, string filePath, long? fileSize = null, string? mimeType = null);
        Task LogSystemEventAsync(string eventType, string? description = null, object? context = null);
        Task LogSecurityEventAsync(string eventType, string? description = null, int riskScore = 0, bool isSuspicious = false);
        Task LogApiCallAsync(string method, string endpoint, int statusCode, long responseTimeMs, string? errorMessage = null);
        
        // 查詢和報告
        Task<AuditLogQueryResult> QueryLogsAsync(AuditLogFilterModel filter);
        Task<IEnumerable<AuditLogSummaryModel>> GetSummaryAsync(DateTime fromDate, DateTime toDate);
        Task<AuditStatisticsDto> GetStatisticsAsync(DateTime fromDate, DateTime toDate);
        
        // 存取控制
        Task LogAuditAccessAsync(string accessorUserId, string accessType, AuditLogFilterModel? searchCriteria = null, int? recordsReturned = null, string? purpose = null);
        
        // 合規性和匯出
        Task<Guid> GenerateComplianceReportAsync(string reportType, DateTime fromDate, DateTime toDate, string generatedBy);
        Task<byte[]> ExportLogsAsync(AuditLogFilterModel filter, string format = "CSV");
        
        // 清理和維護
        Task<int> CleanupExpiredLogsAsync();
        Task<int> ArchiveOldLogsAsync(DateTime beforeDate);
    }

    /// <summary>
    /// 稽核日誌服務實作
    /// 提供完整的稽核日誌功能，包括非同步處理、批次操作、合規性報告等
    /// </summary>
    public class AuditLogService : IAuditLogService, IDisposable
    {
        private readonly ILogger<AuditLogService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly string _connectionString;
        private readonly Channel<CreateAuditLogDto> _logChannel;
        private readonly ChannelWriter<CreateAuditLogDto> _logWriter;
        private readonly ChannelReader<CreateAuditLogDto> _logReader;
        private readonly Dictionary<Guid, BatchInfo> _activeBatches = new();
        private readonly object _batchLock = new object();

        // 當前請求的上下文資訊
        private string? _currentUserId;
        private string? _currentUserName;
        private string? _currentUserRole;
        private string? _currentSessionId;
        private string? _currentIpAddress;
        private string? _currentUserAgent;
        private string? _currentRequestId;

        private class BatchInfo
        {
            public Guid BatchId { get; set; }
            public string Description { get; set; } = "";
            public DateTime StartedAt { get; set; } = DateTime.UtcNow;
            public List<CreateAuditLogDto> PendingLogs { get; set; } = new();
        }

        public AuditLogService(ILogger<AuditLogService> logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _connectionString = _configurationService.GetConnectionString();

            // 創建高性能的非同步處理通道
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            _logChannel = Channel.CreateBounded<CreateAuditLogDto>(options);
            _logWriter = _logChannel.Writer;
            _logReader = _logChannel.Reader;

            // 啟動背景處理
            _ = Task.Run(ProcessLogsAsync);
        }

        #region 上下文設定

        /// <summary>
        /// 設定當前請求的上下文資訊
        /// </summary>
        public void SetRequestContext(string? userId, string? userName, string? userRole, 
            string? sessionId, string? ipAddress, string? userAgent, string? requestId)
        {
            _currentUserId = userId;
            _currentUserName = userName;
            _currentUserRole = userRole;
            _currentSessionId = sessionId;
            _currentIpAddress = ipAddress;
            _currentUserAgent = userAgent;
            _currentRequestId = requestId;
        }

        /// <summary>
        /// 從當前上下文填充稽核日誌資料
        /// </summary>
        private void PopulateFromContext(CreateAuditLogDto auditLog)
        {
            auditLog.UserId ??= _currentUserId;
            auditLog.UserName ??= _currentUserName;
            auditLog.UserRole ??= _currentUserRole;
            auditLog.SessionId ??= _currentSessionId;
            auditLog.IpAddress ??= _currentIpAddress;
            auditLog.UserAgent ??= _currentUserAgent;
            auditLog.RequestId ??= _currentRequestId;
        }

        #endregion

        #region 基本日誌記錄

        /// <summary>
        /// 記錄稽核事件（完整版本）
        /// </summary>
        public async Task<long> LogEventAsync(CreateAuditLogDto auditLog)
        {
            try
            {
                // 從上下文填充資料
                PopulateFromContext(auditLog);
                
                // 設定預設值
                auditLog.SecurityLevel ??= SecurityLevels.NORMAL;
                auditLog.RiskScore ??= 0;
                auditLog.IsSuspicious ??= false;

                // 非同步寫入通道
                await _logWriter.WriteAsync(auditLog);
                
                // 對於高風險事件，立即處理
                if (auditLog.SecurityLevel == SecurityLevels.CRITICAL || 
                    auditLog.RiskScore > 80 || 
                    auditLog.IsSuspicious == true)
                {
                    return await WriteToDatabase(auditLog);
                }

                return 0; // 正常事件會在背景處理
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄稽核事件失敗: {EventType} - {Action}", auditLog.EventType, auditLog.Action);
                throw;
            }
        }

        /// <summary>
        /// 記錄稽核事件（簡化版本）
        /// </summary>
        public async Task<long> LogEventAsync(string eventType, string action, object? context = null)
        {
            var auditLog = new CreateAuditLogDto
            {
                EventType = eventType,
                Action = action,
                AdditionalData = context
            };

            return await LogEventAsync(auditLog);
        }

        /// <summary>
        /// 記錄稽核日誌（供中介軟體使用）
        /// </summary>
        public async Task LogAsync(AuditLog auditLog)
        {
            var createDto = new CreateAuditLogDto
            {
                UserId = auditLog.UserId,
                EventType = auditLog.EventType,
                ResourceType = auditLog.ResourceType,
                ResourceId = auditLog.ResourceId,
                Action = auditLog.Action,
                IpAddress = auditLog.IpAddress,
                UserAgent = auditLog.UserAgent,
                RequestMethod = auditLog.RequestMethod,
                RequestPath = auditLog.RequestPath,
                RequestQuery = auditLog.RequestQuery,
                RequestBody = auditLog.RequestBody,
                ResponseCode = auditLog.ResponseCode,
                ResponseTime = auditLog.ResponseTime,
                SecurityLevel = auditLog.SecurityLevel,
                RiskScore = auditLog.RiskScore,
                IsSuspicious = auditLog.RiskScore > 70,
                ErrorMessage = auditLog.ErrorMessage,
                AdditionalData = auditLog.Details != null ? JsonSerializer.Deserialize<object>(auditLog.Details) : null
            };

            await LogEventAsync(createDto);
        }

        #endregion

        #region 批次操作

        /// <summary>
        /// 開始批次操作
        /// </summary>
        public Task<Guid> StartBatchAsync(string batchDescription = "")
        {
            var batchId = Guid.NewGuid();
            
            lock (_batchLock)
            {
                _activeBatches[batchId] = new BatchInfo
                {
                    BatchId = batchId,
                    Description = batchDescription,
                    StartedAt = DateTime.UtcNow
                };
            }

            _logger.LogInformation("開始批次操作: {BatchId} - {Description}", batchId, batchDescription);
            return Task.FromResult(batchId);
        }

        /// <summary>
        /// 完成批次操作
        /// </summary>
        public async Task CompleteBatchAsync(Guid batchId)
        {
            BatchInfo? batchInfo;
            
            lock (_batchLock)
            {
                if (!_activeBatches.TryGetValue(batchId, out batchInfo))
                {
                    _logger.LogWarning("嘗試完成不存在的批次操作: {BatchId}", batchId);
                    return;
                }
                _activeBatches.Remove(batchId);
            }

            // 批次寫入資料庫
            if (batchInfo.PendingLogs.Any())
            {
                await WriteBatchToDatabase(batchInfo.PendingLogs);
            }

            _logger.LogInformation("完成批次操作: {BatchId} - 處理了 {Count} 個事件", 
                batchId, batchInfo.PendingLogs.Count);
        }

        /// <summary>
        /// 記錄事件到批次
        /// </summary>
        public Task<long> LogEventToBatchAsync(Guid batchId, CreateAuditLogDto auditLog)
        {
            auditLog.BatchId = batchId;
            PopulateFromContext(auditLog);

            lock (_batchLock)
            {
                if (_activeBatches.TryGetValue(batchId, out var batchInfo))
                {
                    batchInfo.PendingLogs.Add(auditLog);
                    return Task.FromResult(0L); // 批次處理，暫不返回實際ID
                }
            }

            // 如果批次不存在，直接記錄
            return LogEventAsync(auditLog);
        }

        #endregion

        #region 專用日誌方法

        /// <summary>
        /// 記錄認證事件
        /// </summary>
        public async Task LogAuthenticationEventAsync(string eventType, string userId, bool success, 
            string? errorMessage = null, object? additionalData = null)
        {
            var auditLog = new CreateAuditLogDto
            {
                EventType = eventType,
                Action = success ? AuditActions.AUTHORIZE : AuditActions.DENY,
                UserId = userId,
                Success = success,
                ErrorMessage = errorMessage,
                SecurityLevel = success ? SecurityLevels.NORMAL : SecurityLevels.HIGH,
                RiskScore = success ? 0 : 30,
                AdditionalData = additionalData,
                Tags = new[] { "authentication", success ? "success" : "failure" }
            };

            await LogEventAsync(auditLog);
        }

        /// <summary>
        /// 記錄資料操作事件
        /// </summary>
        public async Task LogDataOperationAsync(string action, string resourceType, string resourceId, 
            object? oldValues = null, object? newValues = null, string? changesSummary = null)
        {
            var auditLog = new CreateAuditLogDto
            {
                EventType = GetDataEventType(action),
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                OldValues = oldValues,
                NewValues = newValues,
                ChangesSummary = changesSummary,
                SecurityLevel = action == AuditActions.DELETE ? SecurityLevels.HIGH : SecurityLevels.NORMAL,
                Tags = new[] { "data-operation", action.ToLower(), resourceType.ToLower() }
            };

            // 計算變更詳情
            if (oldValues != null && newValues != null)
            {
                auditLog.ChangeDetails = CalculateChanges(oldValues, newValues);
            }

            await LogEventAsync(auditLog);
        }

        /// <summary>
        /// 記錄檔案操作事件
        /// </summary>
        public async Task LogFileOperationAsync(string action, string filePath, long? fileSize = null, string? mimeType = null)
        {
            var fileName = Path.GetFileName(filePath);
            var auditLog = new CreateAuditLogDto
            {
                EventType = GetFileEventType(action),
                Action = action,
                ResourceType = "File",
                ResourceId = filePath,
                ResourceName = fileName,
                AdditionalData = new
                {
                    FilePath = filePath,
                    FileName = fileName,
                    FileSize = fileSize,
                    MimeType = mimeType
                },
                SecurityLevel = action == AuditActions.DELETE ? SecurityLevels.HIGH : SecurityLevels.NORMAL,
                Tags = new[] { "file-operation", action.ToLower() }
            };

            await LogEventAsync(auditLog);
        }

        /// <summary>
        /// 記錄系統事件
        /// </summary>
        public async Task LogSystemEventAsync(string eventType, string? description = null, object? context = null)
        {
            var auditLog = new CreateAuditLogDto
            {
                EventType = eventType,
                Action = AuditActions.EXECUTE,
                ChangesSummary = description,
                AdditionalData = context,
                SecurityLevel = SecurityLevels.HIGH,
                Tags = new[] { "system", eventType.ToLower() }
            };

            await LogEventAsync(auditLog);
        }

        /// <summary>
        /// 記錄安全事件
        /// </summary>
        public async Task LogSecurityEventAsync(string eventType, string? description = null, 
            int riskScore = 0, bool isSuspicious = false)
        {
            var auditLog = new CreateAuditLogDto
            {
                EventType = eventType,
                Action = "SECURITY_EVENT",
                ChangesSummary = description,
                SecurityLevel = isSuspicious || riskScore > 70 ? SecurityLevels.CRITICAL : SecurityLevels.HIGH,
                RiskScore = riskScore,
                IsSuspicious = isSuspicious,
                Tags = new[] { "security", eventType.ToLower() }
            };

            await LogEventAsync(auditLog);
        }

        /// <summary>
        /// 記錄 API 呼叫事件
        /// </summary>
        public async Task LogApiCallAsync(string method, string endpoint, int statusCode, 
            long responseTimeMs, string? errorMessage = null)
        {
            var success = statusCode >= 200 && statusCode < 400;
            var auditLog = new CreateAuditLogDto
            {
                EventType = success ? AuditEventTypes.API_CALL : AuditEventTypes.API_ERROR,
                Action = method,
                RequestMethod = method,
                RequestUrl = endpoint,
                Success = success,
                ErrorMessage = errorMessage,
                ResponseTimeMs = (int)responseTimeMs,
                SecurityLevel = success ? SecurityLevels.LOW : SecurityLevels.NORMAL,
                AdditionalData = new
                {
                    StatusCode = statusCode,
                    ResponseTime = responseTimeMs,
                    Endpoint = endpoint
                },
                Tags = new[] { "api", method.ToLower(), success ? "success" : "error" }
            };

            // 只記錄錯誤和高延遲的API呼叫
            if (!success || responseTimeMs > 5000)
            {
                await LogEventAsync(auditLog);
            }
        }

        #endregion

        #region 查詢和報告

        /// <summary>
        /// 查詢稽核日誌
        /// </summary>
        public async Task<AuditLogQueryResult> QueryLogsAsync(AuditLogFilterModel filter)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var (whereClause, parameters) = BuildWhereClause(filter);
                var orderClause = BuildOrderClause(filter);
                var limitClause = $"LIMIT {filter.PageSize} OFFSET {(filter.Page - 1) * filter.PageSize}";

                // 查詢資料
                var sql = $@"
                    SELECT id, event_id, batch_id, session_id, user_id, user_name, user_role,
                           event_type, action, resource_type, resource_id, resource_name,
                           old_values, new_values, changes_summary, ip_address, user_agent,
                           request_method, request_url, request_id, success, error_message,
                           error_code, response_time_ms, security_level, risk_score,
                           is_suspicious, occurred_at, created_at, additional_data, tags,
                           compliance_flags
                    FROM audit_logs 
                    {whereClause} 
                    {orderClause} 
                    {limitClause}";

                var logs = await connection.QueryAsync<AuditLogModel>(sql, parameters);

                // 查詢總數
                var countSql = $"SELECT COUNT(*) FROM audit_logs {whereClause}";
                var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

                var queryDuration = DateTime.UtcNow - startTime;

                // 記錄稽核存取
                await LogAuditAccessAsync(_currentUserId ?? "system", "QUERY", filter, logs.Count(), "查詢稽核日誌");

                return new AuditLogQueryResult
                {
                    Logs = logs,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    QueryDuration = queryDuration
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢稽核日誌失敗");
                throw;
            }
        }

        /// <summary>
        /// 獲取稽核摘要
        /// </summary>
        public async Task<IEnumerable<AuditLogSummaryModel>> GetSummaryAsync(DateTime fromDate, DateTime toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT log_date, event_type, action, resource_type, event_count,
                       failed_count, suspicious_count, avg_response_time, unique_users
                FROM audit_logs_summary 
                WHERE log_date BETWEEN @FromDate AND @ToDate
                ORDER BY log_date DESC, event_count DESC";

            return await connection.QueryAsync<AuditLogSummaryModel>(sql, new { FromDate = fromDate, ToDate = toDate });
        }

        /// <summary>
        /// 獲取稽核統計
        /// </summary>
        public async Task<AuditStatisticsDto> GetStatisticsAsync(DateTime fromDate, DateTime toDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 基本統計
            var basicStats = await connection.QuerySingleAsync<dynamic>(@"
                SELECT 
                    COUNT(*) as TotalEvents,
                    COUNT(CASE WHEN success = true THEN 1 END) as SuccessfulEvents,
                    COUNT(CASE WHEN success = false THEN 1 END) as FailedEvents,
                    COUNT(CASE WHEN is_suspicious = true THEN 1 END) as SuspiciousEvents,
                    COUNT(DISTINCT user_id) as UniqueUsers,
                    COUNT(DISTINCT CONCAT(resource_type, ':', resource_id)) as UniqueResources
                FROM audit_logs 
                WHERE occurred_at BETWEEN @FromDate AND @ToDate",
                new { FromDate = fromDate, ToDate = toDate });

            var statistics = new AuditStatisticsDto
            {
                PeriodStart = fromDate,
                PeriodEnd = toDate,
                TotalEvents = basicStats.TotalEvents,
                SuccessfulEvents = basicStats.SuccessfulEvents,
                FailedEvents = basicStats.FailedEvents,
                SuspiciousEvents = basicStats.SuspiciousEvents,
                UniqueUsers = basicStats.UniqueUsers,
                UniqueResources = basicStats.UniqueResources
            };

            // 事件類型分佈
            var eventTypeBreakdown = await connection.QueryAsync<dynamic>(@"
                SELECT event_type, COUNT(*) as count
                FROM audit_logs 
                WHERE occurred_at BETWEEN @FromDate AND @ToDate
                GROUP BY event_type
                ORDER BY count DESC",
                new { FromDate = fromDate, ToDate = toDate });

            statistics.EventTypeBreakdown = eventTypeBreakdown.ToDictionary(x => (string)x.event_type, x => (long)x.count);

            return statistics;
        }

        #endregion

        #region 存取控制

        /// <summary>
        /// 記錄稽核日誌存取
        /// </summary>
        public async Task LogAuditAccessAsync(string accessorUserId, string accessType, 
            AuditLogFilterModel? searchCriteria = null, int? recordsReturned = null, string? purpose = null)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO audit_log_access 
                    (accessor_user_id, accessor_user_name, accessor_ip, access_type, 
                     search_criteria, records_returned, purpose, accessed_at)
                    VALUES (@AccessorUserId, @AccessorUserName, @AccessorIp, @AccessType,
                            @SearchCriteria, @RecordsReturned, @Purpose, @AccessedAt)";

                await connection.ExecuteAsync(sql, new
                {
                    AccessorUserId = accessorUserId,
                    AccessorUserName = _currentUserName,
                    AccessorIp = _currentIpAddress,
                    AccessType = accessType,
                    SearchCriteria = searchCriteria != null ? JsonSerializer.Serialize(searchCriteria) : null,
                    RecordsReturned = recordsReturned,
                    Purpose = purpose,
                    AccessedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄稽核存取失敗");
            }
        }

        #endregion

        #region 合規性和匯出

        /// <summary>
        /// 生成合規性報告
        /// </summary>
        public async Task<Guid> GenerateComplianceReportAsync(string reportType, DateTime fromDate, DateTime toDate, string generatedBy)
        {
            var reportId = Guid.NewGuid();
            
            // 實際報告生成會在背景執行
            _ = Task.Run(async () =>
            {
                try
                {
                    await GenerateComplianceReportInternal(reportId, reportType, fromDate, toDate, generatedBy);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "生成合規性報告失敗: {ReportId}", reportId);
                }
            });

            return reportId;
        }

        /// <summary>
        /// 匯出稽核日誌
        /// </summary>
        public async Task<byte[]> ExportLogsAsync(AuditLogFilterModel filter, string format = "CSV")
        {
            var result = await QueryLogsAsync(filter);
            
            switch (format.ToUpper())
            {
                case "CSV":
                    return ExportToCsv(result.Logs);
                case "JSON":
                    return ExportToJson(result.Logs);
                default:
                    throw new ArgumentException($"不支援的匯出格式: {format}");
            }
        }

        #endregion

        #region 清理和維護

        /// <summary>
        /// 清理過期日誌
        /// </summary>
        public async Task<int> CleanupExpiredLogsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT cleanup_old_audit_logs()";
            return await connection.QuerySingleAsync<int>(sql);
        }

        /// <summary>
        /// 歸檔舊日誌
        /// </summary>
        public async Task<int> ArchiveOldLogsAsync(DateTime beforeDate)
        {
            // 實作會將舊日誌移到歸檔表或檔案系統
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // 這裡簡化為刪除，實際應該移到歸檔儲存
            var sql = "DELETE FROM audit_logs WHERE occurred_at < @BeforeDate";
            return await connection.ExecuteAsync(sql, new { BeforeDate = beforeDate });
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 背景處理日誌佇列
        /// </summary>
        private async Task ProcessLogsAsync()
        {
            var buffer = new List<CreateAuditLogDto>(100);
            
            await foreach (var log in _logReader.ReadAllAsync())
            {
                buffer.Add(log);
                
                // 批次處理或達到一定數量時寫入
                if (buffer.Count >= 100)
                {
                    await WriteBatchToDatabase(buffer);
                    buffer.Clear();
                }
            }
            
            // 處理剩餘的日誌
            if (buffer.Any())
            {
                await WriteBatchToDatabase(buffer);
            }
        }

        /// <summary>
        /// 寫入單個日誌到資料庫
        /// </summary>
        private async Task<long> WriteToDatabase(CreateAuditLogDto auditLog)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO audit_logs 
                (event_id, batch_id, session_id, user_id, user_name, user_role, event_type, action,
                 resource_type, resource_id, resource_name, old_values, new_values, changes_summary,
                 ip_address, user_agent, request_method, request_url, request_id, success,
                 error_message, error_code, response_time_ms, security_level, risk_score,
                 is_suspicious, occurred_at, additional_data, tags, compliance_flags)
                VALUES 
                (@EventId, @BatchId, @SessionId, @UserId, @UserName, @UserRole, @EventType, @Action,
                 @ResourceType, @ResourceId, @ResourceName, @OldValues, @NewValues, @ChangesSummary,
                 @IpAddress, @UserAgent, @RequestMethod, @RequestUrl, @RequestId, @Success,
                 @ErrorMessage, @ErrorCode, @ResponseTimeMs, @SecurityLevel, @RiskScore,
                 @IsSuspicious, @OccurredAt, @AdditionalData, @Tags, @ComplianceFlags)
                RETURNING id";

            var parameters = new
            {
                EventId = Guid.NewGuid(),
                auditLog.BatchId,
                auditLog.SessionId,
                auditLog.UserId,
                auditLog.UserName,
                auditLog.UserRole,
                auditLog.EventType,
                auditLog.Action,
                auditLog.ResourceType,
                auditLog.ResourceId,
                auditLog.ResourceName,
                OldValues = auditLog.OldValues != null ? JsonSerializer.Serialize(auditLog.OldValues) : null,
                NewValues = auditLog.NewValues != null ? JsonSerializer.Serialize(auditLog.NewValues) : null,
                auditLog.ChangesSummary,
                IpAddress = auditLog.IpAddress != null ? IPAddress.Parse(auditLog.IpAddress) : null,
                auditLog.UserAgent,
                auditLog.RequestMethod,
                auditLog.RequestUrl,
                auditLog.RequestId,
                auditLog.Success,
                auditLog.ErrorMessage,
                auditLog.ErrorCode,
                auditLog.ResponseTimeMs,
                SecurityLevel = auditLog.SecurityLevel ?? SecurityLevels.NORMAL,
                RiskScore = auditLog.RiskScore ?? 0,
                IsSuspicious = auditLog.IsSuspicious ?? false,
                OccurredAt = DateTime.UtcNow,
                AdditionalData = auditLog.AdditionalData != null ? JsonSerializer.Serialize(auditLog.AdditionalData) : null,
                Tags = auditLog.Tags,
                ComplianceFlags = auditLog.ComplianceFlags
            };

            var id = await connection.QuerySingleAsync<long>(sql, parameters);

            // 如果有變更詳情，也要寫入
            if (auditLog.ChangeDetails?.Any() == true)
            {
                await WriteChangeDetails(connection, id, auditLog.ChangeDetails);
            }

            return id;
        }

        /// <summary>
        /// 批次寫入日誌到資料庫
        /// </summary>
        private async Task WriteBatchToDatabase(IEnumerable<CreateAuditLogDto> logs)
        {
            if (!logs.Any()) return;

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                foreach (var log in logs)
                {
                    await WriteToDatabase(log);
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次寫入稽核日誌失敗，影響 {Count} 個日誌", logs.Count());
                throw;
            }
        }

        /// <summary>
        /// 寫入變更詳情
        /// </summary>
        private async Task WriteChangeDetails(NpgsqlConnection connection, long auditLogId, 
            IEnumerable<AuditChangeDetailDto> changeDetails)
        {
            var sql = @"
                INSERT INTO audit_change_details 
                (audit_log_id, field_name, field_type, old_value, new_value, change_type, is_sensitive)
                VALUES (@AuditLogId, @FieldName, @FieldType, @OldValue, @NewValue, @ChangeType, @IsSensitive)";

            foreach (var detail in changeDetails)
            {
                await connection.ExecuteAsync(sql, new
                {
                    AuditLogId = auditLogId,
                    detail.FieldName,
                    detail.FieldType,
                    detail.OldValue,
                    detail.NewValue,
                    detail.ChangeType,
                    detail.IsSensitive
                });
            }
        }

        /// <summary>
        /// 計算物件變更
        /// </summary>
        private List<AuditChangeDetailDto> CalculateChanges(object oldValues, object newValues)
        {
            var changes = new List<AuditChangeDetailDto>();
            
            // 這裡應該實作深度比較邏輯
            // 簡化版本，實際應該使用反射或JSON比較
            
            return changes;
        }

        /// <summary>
        /// 構建 WHERE 子句
        /// </summary>
        private (string whereClause, object parameters) BuildWhereClause(AuditLogFilterModel filter)
        {
            var conditions = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (filter.FromDate.HasValue)
            {
                conditions.Add("occurred_at >= @FromDate");
                parameters["FromDate"] = filter.FromDate.Value;
            }

            if (filter.ToDate.HasValue)
            {
                conditions.Add("occurred_at <= @ToDate");
                parameters["ToDate"] = filter.ToDate.Value;
            }

            if (!string.IsNullOrEmpty(filter.UserId))
            {
                conditions.Add("user_id = @UserId");
                parameters["UserId"] = filter.UserId;
            }

            if (filter.EventTypes?.Any() == true)
            {
                conditions.Add("event_type = ANY(@EventTypes)");
                parameters["EventTypes"] = filter.EventTypes;
            }

            if (!string.IsNullOrEmpty(filter.ResourceType))
            {
                conditions.Add("resource_type = @ResourceType");
                parameters["ResourceType"] = filter.ResourceType;
            }

            if (filter.Success.HasValue)
            {
                conditions.Add("success = @Success");
                parameters["Success"] = filter.Success.Value;
            }

            if (filter.OnlySuspicious == true)
            {
                conditions.Add("is_suspicious = true");
            }

            var whereClause = conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
            return (whereClause, parameters);
        }

        /// <summary>
        /// 構建 ORDER BY 子句
        /// </summary>
        private string BuildOrderClause(AuditLogFilterModel filter)
        {
            var field = filter.SortField ?? "occurred_at";
            var direction = filter.SortDirection?.ToUpper() == "ASC" ? "ASC" : "DESC";
            return $"ORDER BY {field} {direction}";
        }

        /// <summary>
        /// 根據操作類型獲取資料事件類型
        /// </summary>
        private string GetDataEventType(string action)
        {
            return action.ToUpper() switch
            {
                AuditActions.CREATE => AuditEventTypes.DATA_CREATE,
                AuditActions.UPDATE => AuditEventTypes.DATA_UPDATE,
                AuditActions.DELETE => AuditEventTypes.DATA_DELETE,
                AuditActions.READ => AuditEventTypes.DATA_VIEW,
                AuditActions.EXPORT => AuditEventTypes.DATA_EXPORT,
                AuditActions.IMPORT => AuditEventTypes.DATA_IMPORT,
                _ => AuditEventTypes.DATA_VIEW
            };
        }

        /// <summary>
        /// 根據操作類型獲取檔案事件類型
        /// </summary>
        private string GetFileEventType(string action)
        {
            return action.ToUpper() switch
            {
                AuditActions.UPLOAD => AuditEventTypes.FILE_UPLOAD,
                AuditActions.DOWNLOAD => AuditEventTypes.FILE_DOWNLOAD,
                AuditActions.DELETE => AuditEventTypes.FILE_DELETE,
                AuditActions.UPDATE => AuditEventTypes.FILE_MODIFY,
                _ => AuditEventTypes.FILE_UPLOAD
            };
        }

        /// <summary>
        /// 生成合規性報告（內部方法）
        /// </summary>
        private async Task GenerateComplianceReportInternal(Guid reportId, string reportType, 
            DateTime fromDate, DateTime toDate, string generatedBy)
        {
            // 實作報告生成邏輯
            await Task.Delay(1000); // 模擬報告生成時間
        }

        /// <summary>
        /// 匯出為 CSV
        /// </summary>
        private byte[] ExportToCsv(IEnumerable<AuditLogModel> logs)
        {
            // 實作 CSV 匯出邏輯
            return Array.Empty<byte>();
        }

        /// <summary>
        /// 匯出為 JSON
        /// </summary>
        private byte[] ExportToJson(IEnumerable<AuditLogModel> logs)
        {
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _logWriter?.TryComplete();
            _logChannel?.Writer?.Complete();
        }

        #endregion
    }
}