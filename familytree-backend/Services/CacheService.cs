// 查詢快取服務 - 提供統一的快取管理功能
// 設計改善：實作查詢快取機制，優化資料庫查詢效能
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using System.Text.Json;

namespace familytree_backend.Services
{
    /// <summary>
    /// 查詢快取服務介面
    /// 設計理念：提供統一的快取管理功能，提升查詢效能
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// 獲取快取值
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// 設定快取值
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// 移除快取值
        /// </summary>
        Task RemoveAsync(string key);

        /// <summary>
        /// 檢查快取是否存在
        /// </summary>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// 獲取或設定快取值
        /// </summary>
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        /// <summary>
        /// 清除所有快取
        /// </summary>
        Task ClearAsync();

        /// <summary>
        /// 清除指定模式的快取
        /// </summary>
        Task ClearPatternAsync(string pattern);

        /// <summary>
        /// 獲取快取統計資訊
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync();

        /// <summary>
        /// 檢查快取健康狀態
        /// </summary>
        Task<CacheHealthStatus> CheckHealthAsync();
    }

    /// <summary>
    /// 查詢快取服務實作
    /// 職責：提供統一的快取管理功能，提升查詢效能和系統回應速度
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly ILogger<CacheService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly PerformanceConfiguration _performanceConfig;
        private readonly Dictionary<string, CacheItem> _cache;
        private readonly object _lockObject = new object();
        private readonly Timer _cleanupTimer;

        /// <summary>
        /// 建構子
        /// </summary>
        public CacheService(ILogger<CacheService> logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _performanceConfig = configurationService.GetPerformanceConfiguration();
            _cache = new Dictionary<string, CacheItem>();

            // 設定定期清理過期快取
            var cleanupInterval = TimeSpan.FromMinutes(_performanceConfig.CacheCleanupIntervalMinutes);
            _cleanupTimer = new Timer(CleanupExpiredItems, null, cleanupInterval, cleanupInterval);

            _logger.LogInformation("快取服務已初始化 - 清理間隔: {CleanupInterval} 分鐘", _performanceConfig.CacheCleanupIntervalMinutes);
        }

        /// <summary>
        /// 獲取快取值
        /// 設計理念：統一的快取值獲取邏輯
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return default(T);
                }

                lock (_lockObject)
                {
                    if (_cache.TryGetValue(key, out var item))
                    {
                        // 檢查是否過期
                        if (item.ExpirationTime.HasValue && DateTime.UtcNow > item.ExpirationTime.Value)
                        {
                            _cache.Remove(key);
                            _logger.LogDebug("快取項目已過期，已移除: {Key}", key);
                            return default(T);
                        }

                        // 更新存取時間
                        item.LastAccessed = DateTime.UtcNow;
                        item.AccessCount++;

                        _logger.LogDebug("快取命中: {Key}, 類型: {Type}", key, typeof(T).Name);
                        return JsonSerializer.Deserialize<T>(item.Value);
                    }
                }

                _logger.LogDebug("快取未命中: {Key}, 類型: {Type}", key, typeof(T).Name);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取快取值失敗: {Key}, 類型: {Type}", key, typeof(T).Name);
                return default(T);
            }
        }

        /// <summary>
        /// 設定快取值
        /// 設計理念：統一的快取值設定邏輯
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return;
                }

                DateTime? expirationTime = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null;
                var serializedValue = JsonSerializer.Serialize(value);

                lock (_lockObject)
                {
                    // 檢查快取大小限制
                    if (_cache.Count >= _performanceConfig.MaxCacheItems)
                    {
                        // 移除最少使用的項目
                        RemoveLeastUsedItem();
                    }

                    _cache[key] = new CacheItem
                    {
                        Value = serializedValue,
                        ExpirationTime = expirationTime,
                        CreatedTime = DateTime.UtcNow,
                        LastAccessed = DateTime.UtcNow,
                        AccessCount = 0
                    };
                }

                _logger.LogDebug("快取值已設定: {Key}, 類型: {Type}, 過期時間: {Expiration}", 
                    key, typeof(T).Name, expirationTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定快取值失敗: {Key}, 類型: {Type}", key, typeof(T).Name);
            }
        }

        /// <summary>
        /// 移除快取值
        /// 設計理念：統一的快取值移除邏輯
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return;
                }

                lock (_lockObject)
                {
                    if (_cache.Remove(key))
                    {
                        _logger.LogDebug("快取值已移除: {Key}", key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除快取值失敗: {Key}", key);
            }
        }

        /// <summary>
        /// 檢查快取是否存在
        /// 設計理念：統一的快取存在性檢查
        /// </summary>
        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return false;
                }

                lock (_lockObject)
                {
                    if (_cache.TryGetValue(key, out var item))
                    {
                        // 檢查是否過期
                        if (item.ExpirationTime.HasValue && DateTime.UtcNow > item.ExpirationTime.Value)
                        {
                            _cache.Remove(key);
                            return false;
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查快取存在性失敗: {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// 獲取或設定快取值
        /// 設計理念：懶載入快取模式
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            try
            {
                // 嘗試從快取獲取
                var cachedValue = await GetAsync<T>(key);
                if (cachedValue != null)
                {
                    return cachedValue;
                }

                // 快取未命中，執行工廠方法
                var value = await factory();
                await SetAsync(key, value, expiration);
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取或設定快取值失敗: {Key}, 類型: {Type}", key, typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// 清除所有快取
        /// 設計理念：統一的快取清除邏輯
        /// </summary>
        public async Task ClearAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    var count = _cache.Count;
                    _cache.Clear();
                    _logger.LogInformation("已清除所有快取項目: {Count} 個", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除所有快取失敗");
            }
        }

        /// <summary>
        /// 清除指定模式的快取
        /// 設計理念：模式匹配的快取清除
        /// </summary>
        public async Task ClearPatternAsync(string pattern)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    return;
                }

                lock (_lockObject)
                {
                    var keysToRemove = _cache.Keys.Where(key => key.Contains(pattern)).ToList();
                    foreach (var key in keysToRemove)
                    {
                        _cache.Remove(key);
                    }

                    _logger.LogInformation("已清除模式快取: {Pattern}, 移除 {Count} 個項目", pattern, keysToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除模式快取失敗: {Pattern}", pattern);
            }
        }

        /// <summary>
        /// 獲取快取統計資訊
        /// 設計理念：提供詳細的快取統計資訊
        /// </summary>
        public async Task<CacheStatistics> GetStatisticsAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    var now = DateTime.UtcNow;
                    var totalItems = _cache.Count;
                    var expiredItems = _cache.Values.Count(item => item.ExpirationTime.HasValue && now > item.ExpirationTime.Value);
                    var totalAccessCount = _cache.Values.Sum(item => item.AccessCount);
                    var averageAccessCount = totalItems > 0 ? totalAccessCount / totalItems : 0;

                    var statistics = new CacheStatistics
                    {
                        TotalItems = totalItems,
                        ExpiredItems = expiredItems,
                        ValidItems = totalItems - expiredItems,
                        TotalAccessCount = totalAccessCount,
                        AverageAccessCount = averageAccessCount,
                        MemoryUsage = EstimateMemoryUsage(),
                        LastCleanupTime = now
                    };

                    _logger.LogDebug("快取統計: 總項目 {TotalItems}, 有效項目 {ValidItems}, 平均存取次數 {AverageAccessCount}", 
                        totalItems, statistics.ValidItems, averageAccessCount);

                    return statistics;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取快取統計資訊失敗");
                return new CacheStatistics();
            }
        }

        /// <summary>
        /// 檢查快取健康狀態
        /// 設計理念：監控快取健康狀態
        /// </summary>
        public async Task<CacheHealthStatus> CheckHealthAsync()
        {
            try
            {
                var statistics = await GetStatisticsAsync();
                var healthStatus = new CacheHealthStatus
                {
                    IsHealthy = true,
                    Issues = new List<string>()
                };

                // 檢查快取大小
                if (statistics.TotalItems > _performanceConfig.MaxCacheItems * 0.9)
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"快取項目數量接近上限: {statistics.TotalItems}/{_performanceConfig.MaxCacheItems}");
                }

                // 檢查過期項目比例
                var expiredRatio = statistics.TotalItems > 0 ? (double)statistics.ExpiredItems / statistics.TotalItems : 0;
                if (expiredRatio > 0.3) // 超過 30% 的項目過期
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"過期項目比例過高: {expiredRatio:P}");
                }

                // 檢查記憶體使用
                if (statistics.MemoryUsage > _performanceConfig.MaxCacheMemoryMB * 1024 * 1024 * 0.9)
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"快取記憶體使用接近上限: {statistics.MemoryUsage / (1024 * 1024):F2}MB");
                }

                _logger.LogDebug("快取健康檢查完成 - 健康狀態: {IsHealthy}, 問題數: {IssueCount}", 
                    healthStatus.IsHealthy, healthStatus.Issues.Count);

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快取健康檢查失敗");
                return new CacheHealthStatus { IsHealthy = false, Issues = new List<string> { "健康檢查失敗" } };
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 清理過期項目
        /// </summary>
        private void CleanupExpiredItems(object? state)
        {
            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = new List<string>();

                lock (_lockObject)
                {
                    foreach (var kvp in _cache)
                    {
                        if (kvp.Value.ExpirationTime.HasValue && now > kvp.Value.ExpirationTime.Value)
                        {
                            expiredKeys.Add(kvp.Key);
                        }
                    }

                    foreach (var key in expiredKeys)
                    {
                        _cache.Remove(key);
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogInformation("快取清理完成 - 移除 {Count} 個過期項目", expiredKeys.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快取清理失敗");
            }
        }

        /// <summary>
        /// 移除最少使用的項目
        /// </summary>
        private void RemoveLeastUsedItem()
        {
            var leastUsed = _cache.OrderBy(kvp => kvp.Value.AccessCount)
                                 .ThenBy(kvp => kvp.Value.LastAccessed)
                                 .First();

            _cache.Remove(leastUsed.Key);
            _logger.LogDebug("移除最少使用的快取項目: {Key}, 存取次數: {AccessCount}", 
                leastUsed.Key, leastUsed.Value.AccessCount);
        }

        /// <summary>
        /// 估算記憶體使用量
        /// </summary>
        private long EstimateMemoryUsage()
        {
            try
            {
                var totalSize = 0L;
                foreach (var kvp in _cache)
                {
                    // 估算每個快取項目的大小
                    totalSize += kvp.Key.Length * 2; // UTF-16 字元
                    totalSize += kvp.Value.Value.Length * 2; // JSON 字串
                    totalSize += 64; // 其他欄位
                }
                return totalSize;
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }

    /// <summary>
    /// 快取項目
    /// </summary>
    public class CacheItem
    {
        public string Value { get; set; } = string.Empty;
        public DateTime? ExpirationTime { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
    }

    /// <summary>
    /// 快取統計資訊
    /// </summary>
    public class CacheStatistics
    {
        public int TotalItems { get; set; }
        public int ExpiredItems { get; set; }
        public int ValidItems { get; set; }
        public long TotalAccessCount { get; set; }
        public double AverageAccessCount { get; set; }
        public long MemoryUsage { get; set; }
        public DateTime LastCleanupTime { get; set; }
    }

    /// <summary>
    /// 快取健康狀態
    /// </summary>
    public class CacheHealthStatus
    {
        public bool IsHealthy { get; set; }
        public List<string> Issues { get; set; } = new();
    }
} 