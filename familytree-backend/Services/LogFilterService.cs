// 日誌過濾和分級服務 - 提供智能日誌管理
// 設計改善：建立日誌過濾和分級機制，改善日誌檔案管理
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using System.Text.RegularExpressions;

namespace familytree_backend.Services
{
    /// <summary>
    /// 日誌過濾服務介面
    /// 設計理念：提供智能的日誌過濾和分級功能
    /// </summary>
    public interface ILogFilterService
    {
        /// <summary>
        /// 檢查是否應該記錄此日誌
        /// </summary>
        bool ShouldLog(LogLevel level, string category, string message);

        /// <summary>
        /// 過濾敏感資訊
        /// </summary>
        string FilterSensitiveData(string message);

        /// <summary>
        /// 分類日誌等級
        /// </summary>
        LogCategory CategorizeLog(LogLevel level, string category, string message);

        /// <summary>
        /// 檢查是否為重複日誌
        /// </summary>
        bool IsDuplicateLog(string message, TimeSpan window);
    }

    /// <summary>
    /// 日誌過濾服務實作
    /// 職責：提供智能的日誌過濾、分級和重複檢測功能
    /// </summary>
    public class LogFilterService : ILogFilterService
    {
        private readonly IConfigurationService _configurationService;
        private readonly LoggingConfiguration _loggingConfig;
        private readonly SecurityConfiguration _securityConfig;
        private readonly Dictionary<string, DateTime> _recentLogs;
        private readonly object _lockObject = new object();

        // 敏感資料模式
        private static readonly Regex[] SensitivePatterns = new[]
        {
            new Regex(@"password\s*=\s*['""]?[^'""\s]+['""]?", RegexOptions.IgnoreCase),
            new Regex(@"api[_-]?key\s*=\s*['""]?[^'""\s]+['""]?", RegexOptions.IgnoreCase),
            new Regex(@"token\s*=\s*['""]?[^'""\s]+['""]?", RegexOptions.IgnoreCase),
            new Regex(@"secret\s*=\s*['""]?[^'""\s]+['""]?", RegexOptions.IgnoreCase),
            new Regex(@"connection[_-]?string\s*=\s*['""]?[^'""\s]+['""]?", RegexOptions.IgnoreCase),
            new Regex(@"\b\d{4}[- ]?\d{4}[- ]?\d{4}[- ]?\d{4}\b"), // 信用卡號
            new Regex(@"\b\d{3}-\d{2}-\d{4}\b"), // 社會安全號碼
            new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b") // 電子郵件
        };

        // 忽略的日誌模式
        private static readonly string[] IgnoredPatterns = new[]
        {
            "health check",
            "heartbeat",
            "ping",
            "keepalive",
            "background task",
            "scheduled job"
        };

        // 重要日誌模式
        private static readonly string[] ImportantPatterns = new[]
        {
            "error",
            "exception",
            "failed",
            "timeout",
            "deadlock",
            "connection lost",
            "authentication failed",
            "authorization denied",
            "security violation",
            "data breach"
        };

        /// <summary>
        /// 建構子
        /// </summary>
        public LogFilterService(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _loggingConfig = configurationService.GetLoggingConfiguration();
            _securityConfig = configurationService.GetSecurityConfiguration();
            _recentLogs = new Dictionary<string, DateTime>();
        }

        /// <summary>
        /// 檢查是否應該記錄此日誌
        /// 設計理念：根據配置和內容智能過濾日誌
        /// </summary>
        public bool ShouldLog(LogLevel level, string category, string message)
        {
            // 檢查日誌等級
            if (level < GetMinimumLogLevel())
            {
                return false;
            }

            // 檢查是否為忽略的模式
            if (IsIgnoredPattern(message))
            {
                return false;
            }

            // 檢查是否為重複日誌
            if (IsDuplicateLog(message, TimeSpan.FromMinutes(5)))
            {
                return false;
            }

            // 檢查類別過濾
            if (IsCategoryFiltered(category))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 過濾敏感資訊
        /// 設計理念：保護敏感資料，避免洩露
        /// </summary>
        public string FilterSensitiveData(string message)
        {
            if (!_securityConfig.MaskSensitiveData)
            {
                return message;
            }

            var filteredMessage = message;

            foreach (var pattern in SensitivePatterns)
            {
                filteredMessage = pattern.Replace(filteredMessage, "[SENSITIVE_DATA]");
            }

            return filteredMessage;
        }

        /// <summary>
        /// 分類日誌等級
        /// 設計理念：根據內容智能分類日誌
        /// </summary>
        public LogCategory CategorizeLog(LogLevel level, string category, string message)
        {
            // 檢查是否為安全性事件
            if (IsSecurityEvent(message))
            {
                return LogCategory.Security;
            }

            // 檢查是否為效能事件
            if (IsPerformanceEvent(message))
            {
                return LogCategory.Performance;
            }

            // 檢查是否為業務事件
            if (IsBusinessEvent(message))
            {
                return LogCategory.Business;
            }

            // 檢查是否為系統事件
            if (IsSystemEvent(message))
            {
                return LogCategory.System;
            }

            // 檢查是否為資料庫事件
            if (IsDatabaseEvent(message))
            {
                return LogCategory.Database;
            }

            // 檢查是否為檔案事件
            if (IsFileEvent(message))
            {
                return LogCategory.File;
            }

            // 檢查是否為網路事件
            if (IsNetworkEvent(message))
            {
                return LogCategory.Network;
            }

            return LogCategory.General;
        }

        /// <summary>
        /// 檢查是否為重複日誌
        /// 設計理念：避免重複日誌氾濫
        /// </summary>
        public bool IsDuplicateLog(string message, TimeSpan window)
        {
            lock (_lockObject)
            {
                var messageHash = GetMessageHash(message);
                var now = DateTime.UtcNow;

                // 清理過期的日誌記錄
                var expiredKeys = _recentLogs.Where(kvp => now - kvp.Value > window).ToList();
                foreach (var key in expiredKeys)
                {
                    _recentLogs.Remove(key.Key);
                }

                // 檢查是否為重複日誌
                if (_recentLogs.ContainsKey(messageHash))
                {
                    return true;
                }

                // 記錄新的日誌
                _recentLogs[messageHash] = now;
                return false;
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 獲取最小日誌等級
        /// </summary>
        private LogLevel GetMinimumLogLevel()
        {
            return _loggingConfig.LogLevel.ToLower() switch
            {
                "debug" => LogLevel.Debug,
                "information" => LogLevel.Information,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "critical" => LogLevel.Critical,
                _ => LogLevel.Information
            };
        }

        /// <summary>
        /// 檢查是否為忽略的模式
        /// </summary>
        private bool IsIgnoredPattern(string message)
        {
            return IgnoredPatterns.Any(pattern => 
                message.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查是否為重要模式
        /// </summary>
        private bool IsImportantPattern(string message)
        {
            return ImportantPatterns.Any(pattern => 
                message.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查類別是否被過濾
        /// </summary>
        private bool IsCategoryFiltered(string category)
        {
            // 可以根據配置添加類別過濾邏輯
            return false;
        }

        /// <summary>
        /// 檢查是否為安全性事件
        /// </summary>
        private bool IsSecurityEvent(string message)
        {
            var securityKeywords = new[] { "authentication", "authorization", "login", "logout", "security", "breach", "violation", "unauthorized", "forbidden" };
            return securityKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查是否為效能事件
        /// </summary>
        private bool IsPerformanceEvent(string message)
        {
            var performanceKeywords = new[] { "performance", "slow", "timeout", "duration", "latency", "throughput", "memory", "cpu" };
            return performanceKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查是否為業務事件
        /// </summary>
        private bool IsBusinessEvent(string message)
        {
            var businessKeywords = new[] { "business", "transaction", "order", "payment", "user", "customer", "data", "record" };
            return businessKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查是否為系統事件
        /// </summary>
        private bool IsSystemEvent(string message)
        {
            var systemKeywords = new[] { "system", "service", "startup", "shutdown", "restart", "health", "status" };
            return systemKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查是否為資料庫事件
        /// </summary>
        private bool IsDatabaseEvent(string message)
        {
            var databaseKeywords = new[] { "database", "sql", "query", "connection", "transaction", "deadlock", "timeout" };
            return databaseKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查是否為檔案事件
        /// </summary>
        private bool IsFileEvent(string message)
        {
            var fileKeywords = new[] { "file", "upload", "download", "save", "delete", "path", "directory" };
            return fileKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 檢查是否為網路事件
        /// </summary>
        private bool IsNetworkEvent(string message)
        {
            var networkKeywords = new[] { "network", "http", "https", "request", "response", "api", "endpoint" };
            return networkKeywords.Any(keyword => message.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 獲取訊息雜湊值
        /// </summary>
        private string GetMessageHash(string message)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(message);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        #endregion
    }

    /// <summary>
    /// 日誌分類
    /// </summary>
    public enum LogCategory
    {
        General,
        Security,
        Performance,
        Business,
        System,
        Database,
        File,
        Network
    }

    /// <summary>
    /// 日誌過濾器
    /// 設計理念：提供 Serilog 過濾器實作
    /// </summary>
    public class LogFilter : Serilog.Core.ILogEventFilter
    {
        private readonly ILogFilterService _filterService;

        public LogFilter(ILogFilterService filterService)
        {
            _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
        }

        public bool IsEnabled(Serilog.Events.LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            var category = logEvent.Properties.ContainsKey("SourceContext") 
                ? logEvent.Properties["SourceContext"].ToString()?.Trim('"') ?? ""
                : "";

            return _filterService.ShouldLog(
                ConvertLogLevel(logEvent.Level),
                category,
                message);
        }

        private static LogLevel ConvertLogLevel(Serilog.Events.LogEventLevel level)
        {
            return level switch
            {
                Serilog.Events.LogEventLevel.Verbose => LogLevel.Trace,
                Serilog.Events.LogEventLevel.Debug => LogLevel.Debug,
                Serilog.Events.LogEventLevel.Information => LogLevel.Information,
                Serilog.Events.LogEventLevel.Warning => LogLevel.Warning,
                Serilog.Events.LogEventLevel.Error => LogLevel.Error,
                Serilog.Events.LogEventLevel.Fatal => LogLevel.Critical,
                _ => LogLevel.Information
            };
        }
    }
} 