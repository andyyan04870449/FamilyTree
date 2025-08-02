using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using familytree_backend.Extensions;
using familytree_backend.Models;

namespace familytree_backend.Services
{
    /// <summary>
    /// 稽核日誌背景服務
    /// 負責批次處理稽核日誌的寫入
    /// </summary>
    public class AuditLogBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogBackgroundService> _logger;
        private readonly AuditLogConfiguration _configuration;
        private readonly Channel<AuditLog> _channel;
        private Timer? _batchTimer;
        private readonly List<AuditLog> _batchBuffer;
        private readonly SemaphoreSlim _batchSemaphore;

        public AuditLogBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AuditLogBackgroundService> logger,
            IOptions<AuditLogConfiguration> configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration.Value;
            _batchBuffer = new List<AuditLog>();
            _batchSemaphore = new SemaphoreSlim(1, 1);
            
            // 創建 Channel 用於非同步處理
            _channel = Channel.CreateUnbounded<AuditLog>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = true
            });
        }

        /// <summary>
        /// 取得 Channel Writer 供其他服務使用
        /// </summary>
        public ChannelWriter<AuditLog> Writer => _channel.Writer;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("稽核日誌背景服務已啟動");
            
            // 設定批次處理定時器
            _batchTimer = new Timer(
                async _ => await ProcessBatchAsync(),
                null,
                TimeSpan.FromSeconds(_configuration.BatchIntervalSeconds),
                TimeSpan.FromSeconds(_configuration.BatchIntervalSeconds));

            // 開始處理日誌
            await ProcessLogsAsync(stoppingToken);
        }

        private async Task ProcessLogsAsync(CancellationToken cancellationToken)
        {
            await foreach (var auditLog in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await _batchSemaphore.WaitAsync(cancellationToken);
                    
                    _batchBuffer.Add(auditLog);
                    
                    // 如果批次已滿，立即處理
                    if (_batchBuffer.Count >= _configuration.BatchSize)
                    {
                        await ProcessBatchAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "處理稽核日誌時發生錯誤");
                }
                finally
                {
                    _batchSemaphore.Release();
                }
            }
        }

        private async Task ProcessBatchAsync()
        {
            try
            {
                await _batchSemaphore.WaitAsync();
                
                if (_batchBuffer.Count == 0)
                    return;

                var logsToProcess = _batchBuffer.ToList();
                _batchBuffer.Clear();
                
                _batchSemaphore.Release();

                // 使用新的 scope 來取得服務
                using var scope = _serviceProvider.CreateScope();
                var dataAccess = scope.ServiceProvider.GetRequiredService<IDataAccessService>();
                
                // 批次插入稽核日誌
                await BatchInsertAuditLogsAsync(dataAccess, logsToProcess);
                
                _logger.LogDebug("已處理 {Count} 筆稽核日誌", logsToProcess.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次處理稽核日誌時發生錯誤");
            }
        }

        private async Task BatchInsertAuditLogsAsync(IDataAccessService dataAccess, List<AuditLog> logs)
        {
            var sql = @"
                INSERT INTO audit_logs (
                    user_id, event_type, resource_type, resource_id, action,
                    ip_address, user_agent, request_method, request_path, request_query,
                    request_body, response_code, response_time, security_level,
                    risk_score, error_message, details, compliance_tags, created_at
                ) VALUES ";

            var values = new List<string>();
            var parameters = new Dictionary<string, object?>();
            var paramIndex = 0;

            foreach (var log in logs)
            {
                var valueParams = new List<string>();
                
                // 處理每個欄位
                AddParameter(ref paramIndex, "user_id", log.UserId, valueParams, parameters);
                AddParameter(ref paramIndex, "event_type", log.EventType, valueParams, parameters);
                AddParameter(ref paramIndex, "resource_type", log.ResourceType, valueParams, parameters);
                AddParameter(ref paramIndex, "resource_id", log.ResourceId, valueParams, parameters);
                AddParameter(ref paramIndex, "action", log.Action, valueParams, parameters);
                AddParameter(ref paramIndex, "ip_address", log.IpAddress, valueParams, parameters);
                AddParameter(ref paramIndex, "user_agent", log.UserAgent, valueParams, parameters);
                AddParameter(ref paramIndex, "request_method", log.RequestMethod, valueParams, parameters);
                AddParameter(ref paramIndex, "request_path", log.RequestPath, valueParams, parameters);
                AddParameter(ref paramIndex, "request_query", log.RequestQuery, valueParams, parameters);
                AddParameter(ref paramIndex, "request_body", log.RequestBody, valueParams, parameters);
                AddParameter(ref paramIndex, "response_code", log.ResponseCode, valueParams, parameters);
                AddParameter(ref paramIndex, "response_time", log.ResponseTime, valueParams, parameters);
                AddParameter(ref paramIndex, "security_level", log.SecurityLevel, valueParams, parameters);
                AddParameter(ref paramIndex, "risk_score", log.RiskScore, valueParams, parameters);
                AddParameter(ref paramIndex, "error_message", log.ErrorMessage, valueParams, parameters);
                AddParameter(ref paramIndex, "details", log.Details, valueParams, parameters);
                AddParameter(ref paramIndex, "compliance_tags", 
                    log.ComplianceTags != null ? string.Join(",", log.ComplianceTags) : null, 
                    valueParams, parameters);
                AddParameter(ref paramIndex, "created_at", log.CreatedAt, valueParams, parameters);
                
                values.Add($"({string.Join(", ", valueParams)})");
            }

            sql += string.Join(", ", values);
            
            await dataAccess.ExecuteAsync(sql, parameters);
        }

        private void AddParameter(
            ref int paramIndex, 
            string name, 
            object? value, 
            List<string> valueParams, 
            Dictionary<string, object?> parameters)
        {
            var paramName = $"@p{paramIndex++}";
            valueParams.Add(paramName);
            parameters[paramName] = value ?? DBNull.Value;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("稽核日誌背景服務正在停止");
            
            // 停止定時器
            _batchTimer?.Dispose();
            
            // 標記 Channel 為完成
            _channel.Writer.TryComplete();
            
            // 處理剩餘的批次
            await ProcessBatchAsync();
            
            await base.StopAsync(cancellationToken);
            
            _logger.LogInformation("稽核日誌背景服務已停止");
        }

        public override void Dispose()
        {
            _batchTimer?.Dispose();
            _batchSemaphore?.Dispose();
            base.Dispose();
        }
    }
}