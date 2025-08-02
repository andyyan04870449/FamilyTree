// 統一日誌服務 - 提供標準化的日誌記錄功能
// 設計改善：統一日誌格式，改善日誌內容和效能
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using System.Diagnostics;

namespace familytree_backend.Services
{
    /// <summary>
    /// 統一日誌服務介面
    /// 設計理念：提供標準化的日誌記錄功能，統一日誌格式
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// 記錄請求開始
        /// </summary>
        void LogRequestStart(string operation, object? context = null);

        /// <summary>
        /// 記錄請求完成
        /// </summary>
        void LogRequestComplete(string operation, object? result = null);

        /// <summary>
        /// 記錄資料庫操作
        /// </summary>
        void LogDatabaseOperation(string operation, string query, object? parameters = null, TimeSpan? duration = null);

        /// <summary>
        /// 記錄檔案操作
        /// </summary>
        void LogFileOperation(string operation, string filePath, object? context = null);

        /// <summary>
        /// 記錄效能指標
        /// </summary>
        void LogPerformance(string operation, TimeSpan duration, object? context = null);

        /// <summary>
        /// 記錄安全性事件
        /// </summary>
        void LogSecurityEvent(string eventType, string userId, object? context = null);

        /// <summary>
        /// 記錄業務事件
        /// </summary>
        void LogBusinessEvent(string eventType, object? data = null);
        
        /// <summary>
        /// 記錄使用者活動 (非同步)
        /// </summary>
        Task LogActivityAsync(string userId, string action, string details, string? ipAddress = null);

        /// <summary>
        /// 記錄錯誤
        /// </summary>
        void LogError(string operation, Exception exception, object? context = null);

        /// <summary>
        /// 記錄警告
        /// </summary>
        void LogWarning(string operation, string message, object? context = null);

        /// <summary>
        /// 記錄資訊
        /// </summary>
        void LogInformation(string operation, string message, object? context = null);

        /// <summary>
        /// 記錄除錯資訊
        /// </summary>
        void LogDebug(string operation, string message, object? context = null);
    }

    /// <summary>
    /// 統一日誌服務實作
    /// 職責：提供標準化的日誌記錄功能，統一日誌格式和內容
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly LoggingConfiguration _loggingConfig;

        /// <summary>
        /// 建構子
        /// </summary>
        public LoggingService(ILogger<LoggingService> logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _loggingConfig = configurationService.GetLoggingConfiguration();
        }

        /// <summary>
        /// 記錄請求開始
        /// 設計理念：統一的請求開始日誌格式
        /// </summary>
        public void LogRequestStart(string operation, object? context = null)
        {
            var logData = new
            {
                Operation = operation,
                Timestamp = DateTime.UtcNow,
                Context = context,
                EventType = "RequestStart"
            };

            _logger.LogInformation("🔵 請求開始 - {Operation} | 上下文: {@Context}", 
                operation, logData);
        }

        /// <summary>
        /// 記錄請求完成
        /// 設計理念：統一的請求完成日誌格式
        /// </summary>
        public void LogRequestComplete(string operation, object? result = null)
        {
            var logData = new
            {
                Operation = operation,
                Timestamp = DateTime.UtcNow,
                Result = result,
                EventType = "RequestComplete"
            };

            _logger.LogInformation("✅ 請求完成 - {Operation} | 結果: {@Result}", 
                operation, logData);
        }

        /// <summary>
        /// 記錄資料庫操作
        /// 設計理念：統一的資料庫操作日誌格式
        /// </summary>
        public void LogDatabaseOperation(string operation, string query, object? parameters = null, TimeSpan? duration = null)
        {
            var logData = new
            {
                Operation = operation,
                Query = query,
                Parameters = parameters,
                Duration = duration?.TotalMilliseconds,
                Timestamp = DateTime.UtcNow,
                EventType = "DatabaseOperation"
            };

            if (duration.HasValue && duration.Value.TotalMilliseconds > 1000)
            {
                _logger.LogWarning("🐌 慢查詢警告 - {Operation} | 耗時: {Duration}ms | 查詢: {Query}", 
                    operation, duration.Value.TotalMilliseconds, query);
            }
            else
            {
                _logger.LogDebug("🗄️ 資料庫操作 - {Operation} | 耗時: {Duration}ms | 查詢: {Query}", 
                    operation, duration?.TotalMilliseconds ?? 0, query);
            }
        }

        /// <summary>
        /// 記錄檔案操作
        /// 設計理念：統一的檔案操作日誌格式
        /// </summary>
        public void LogFileOperation(string operation, string filePath, object? context = null)
        {
            var logData = new
            {
                Operation = operation,
                FilePath = filePath,
                Context = context,
                Timestamp = DateTime.UtcNow,
                EventType = "FileOperation"
            };

            _logger.LogInformation("📁 檔案操作 - {Operation} | 路徑: {FilePath} | 上下文: {@Context}", 
                operation, filePath, logData);
        }

        /// <summary>
        /// 記錄效能指標
        /// 設計理念：統一的效能指標日誌格式
        /// </summary>
        public void LogPerformance(string operation, TimeSpan duration, object? context = null)
        {
            var logData = new
            {
                Operation = operation,
                Duration = duration.TotalMilliseconds,
                Context = context,
                Timestamp = DateTime.UtcNow,
                EventType = "Performance"
            };

            if (duration.TotalMilliseconds > 5000)
            {
                _logger.LogWarning("⚠️ 效能警告 - {Operation} | 耗時: {Duration}ms | 上下文: {@Context}", 
                    operation, duration.TotalMilliseconds, logData);
            }
            else if (duration.TotalMilliseconds > 1000)
            {
                _logger.LogInformation("⚡ 效能指標 - {Operation} | 耗時: {Duration}ms | 上下文: {@Context}", 
                    operation, duration.TotalMilliseconds, logData);
            }
            else
            {
                _logger.LogDebug("⚡ 效能指標 - {Operation} | 耗時: {Duration}ms | 上下文: {@Context}", 
                    operation, duration.TotalMilliseconds, logData);
            }
        }

        /// <summary>
        /// 記錄安全性事件
        /// 設計理念：統一的安全性事件日誌格式
        /// </summary>
        public void LogSecurityEvent(string eventType, string userId, object? context = null)
        {
            var logData = new
            {
                EventType = eventType,
                UserId = userId,
                Context = context,
                Timestamp = DateTime.UtcNow,
                Category = "Security"
            };

            _logger.LogWarning("🔒 安全性事件 - {EventType} | 用戶: {UserId} | 上下文: {@Context}", 
                eventType, userId, logData);
        }

        /// <summary>
        /// 記錄業務事件
        /// 設計理念：統一的業務事件日誌格式
        /// </summary>
        public void LogBusinessEvent(string eventType, object? data = null)
        {
            var logData = new
            {
                EventType = eventType,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Category = "Business"
            };

            _logger.LogInformation("💼 業務事件 - {EventType} | 資料: {@Data}", 
                eventType, logData);
        }

        /// <summary>
        /// 記錄錯誤
        /// 設計理念：統一的錯誤日誌格式
        /// </summary>
        public void LogError(string operation, Exception exception, object? context = null)
        {
            var logData = new
            {
                Operation = operation,
                Exception = new
                {
                    Type = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace
                },
                Context = context,
                Timestamp = DateTime.UtcNow,
                EventType = "Error"
            };

            _logger.LogError(exception, "❌ 錯誤發生 - {Operation} | 例外: {ExceptionType} | 訊息: {Message} | 上下文: {@Context}", 
                operation, exception.GetType().Name, exception.Message, logData);
        }

        /// <summary>
        /// 記錄警告
        /// 設計理念：統一的警告日誌格式
        /// </summary>
        public void LogWarning(string operation, string message, object? context = null)
        {
            var logData = new
            {
                Operation = operation,
                Message = message,
                Context = context,
                Timestamp = DateTime.UtcNow,
                EventType = "Warning"
            };

            _logger.LogWarning("⚠️ 警告 - {Operation} | 訊息: {Message} | 上下文: {@Context}", 
                operation, message, logData);
        }

        /// <summary>
        /// 記錄使用者活動 (非同步)
        /// </summary>
        public async Task LogActivityAsync(string userId, string action, string details, string? ipAddress = null)
        {
            var logData = new
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
                EventType = "UserActivity"
            };

            _logger.LogInformation("👤 使用者活動 - 使用者: {UserId} | 動作: {Action} | 詳情: {Details} | IP: {IpAddress}", 
                userId, action, details, ipAddress ?? "N/A");
            
            // 這裡可以加入將活動記錄到資料庫的邏輯
            await Task.CompletedTask; // 暫時只記錄到日誌
        }

        /// <summary>
        /// 記錄資訊
        /// 設計理念：統一的資訊日誌格式
        /// </summary>
        public void LogInformation(string operation, string message, object? context = null)
        {
            var logData = new
            {
                Operation = operation,
                Message = message,
                Context = context,
                Timestamp = DateTime.UtcNow,
                EventType = "Information"
            };

            _logger.LogInformation("ℹ️ 資訊 - {Operation} | 訊息: {Message} | 上下文: {@Context}", 
                operation, message, logData);
        }

        /// <summary>
        /// 記錄除錯資訊
        /// 設計理念：統一的除錯日誌格式
        /// </summary>
        public void LogDebug(string operation, string message, object? context = null)
        {
            var logData = new
            {
                Operation = operation,
                Message = message,
                Context = context,
                Timestamp = DateTime.UtcNow,
                EventType = "Debug"
            };

            _logger.LogDebug("🔍 除錯 - {Operation} | 訊息: {Message} | 上下文: {@Context}", 
                operation, message, logData);
        }
    }

    /// <summary>
    /// 日誌上下文擴展
    /// 設計理念：提供日誌上下文的擴展方法
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// 記錄方法執行時間
        /// </summary>
        public static void LogExecutionTime<T>(this ILogger<T> logger, string operation, Action action, object? context = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                action();
                stopwatch.Stop();
                logger.LogInformation("✅ {Operation} 執行完成 - 耗時: {Duration}ms", 
                    operation, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "❌ {Operation} 執行失敗 - 耗時: {Duration}ms", 
                    operation, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// 記錄非同步方法執行時間
        /// </summary>
        public static async Task LogExecutionTimeAsync<T>(this ILogger<T> logger, string operation, Func<Task> action, object? context = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await action();
                stopwatch.Stop();
                logger.LogInformation("✅ {Operation} 執行完成 - 耗時: {Duration}ms", 
                    operation, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "❌ {Operation} 執行失敗 - 耗時: {Duration}ms", 
                    operation, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// 記錄非同步方法執行時間（帶回傳值）
        /// </summary>
        public static async Task<T> LogExecutionTimeAsync<T>(this ILogger<T> logger, string operation, Func<Task<T>> action, object? context = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await action();
                stopwatch.Stop();
                logger.LogInformation("✅ {Operation} 執行完成 - 耗時: {Duration}ms", 
                    operation, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "❌ {Operation} 執行失敗 - 耗時: {Duration}ms", 
                    operation, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
} 