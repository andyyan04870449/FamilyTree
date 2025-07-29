// 記憶體管理服務 - 提供記憶體使用優化和監控
// 設計改善：改善記憶體使用，提供記憶體監控和優化
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using System.Diagnostics;

namespace familytree_backend.Services
{
    /// <summary>
    /// 記憶體管理服務介面
    /// 設計理念：提供記憶體使用優化和監控功能
    /// </summary>
    public interface IMemoryManagementService
    {
        /// <summary>
        /// 獲取記憶體使用狀況
        /// </summary>
        Task<MemoryUsageInfo> GetMemoryUsageAsync();

        /// <summary>
        /// 執行記憶體清理
        /// </summary>
        Task<MemoryCleanupResult> PerformMemoryCleanupAsync();

        /// <summary>
        /// 檢查記憶體健康狀態
        /// </summary>
        Task<MemoryHealthStatus> CheckMemoryHealthAsync();

        /// <summary>
        /// 監控記憶體使用
        /// </summary>
        Task<MemoryMonitoringResult> MonitorMemoryUsageAsync();

        /// <summary>
        /// 優化記憶體使用
        /// </summary>
        Task<MemoryOptimizationResult> OptimizeMemoryUsageAsync();

        /// <summary>
        /// 獲取記憶體統計資訊
        /// </summary>
        Task<MemoryStatistics> GetMemoryStatisticsAsync();

        /// <summary>
        /// 設定記憶體限制
        /// </summary>
        Task SetMemoryLimitAsync(long limitInBytes);

        /// <summary>
        /// 檢查是否超過記憶體限制
        /// </summary>
        Task<bool> IsMemoryLimitExceededAsync();
    }

    /// <summary>
    /// 記憶體管理服務實作
    /// 職責：提供記憶體使用優化和監控，改善系統效能
    /// </summary>
    public class MemoryManagementService : IMemoryManagementService
    {
        private readonly ILogger<MemoryManagementService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly PerformanceConfiguration _performanceConfig;
        private readonly List<MemoryUsageSnapshot> _memoryHistory;
        private readonly object _historyLock = new object();
        private long _memoryLimit = 1024 * 1024 * 1024; // 1GB 預設限制

        /// <summary>
        /// 建構子
        /// </summary>
        public MemoryManagementService(ILogger<MemoryManagementService> logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _performanceConfig = configurationService.GetPerformanceConfiguration();
            _memoryHistory = new List<MemoryUsageSnapshot>();

            _logger.LogInformation("記憶體管理服務已初始化 - 記憶體限制: {MemoryLimit}MB", _memoryLimit / (1024 * 1024));
        }

        /// <summary>
        /// 獲取記憶體使用狀況
        /// 設計理念：提供詳細的記憶體使用資訊
        /// </summary>
        public async Task<MemoryUsageInfo> GetMemoryUsageAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var workingSet = process.WorkingSet64;
                var privateMemory = process.PrivateMemorySize64;
                var virtualMemory = process.VirtualMemorySize64;
                var managedMemory = GC.GetTotalMemory(false);

                var memoryInfo = new MemoryUsageInfo
                {
                    WorkingSet = workingSet,
                    PrivateMemory = privateMemory,
                    VirtualMemory = virtualMemory,
                    ManagedMemory = managedMemory,
                    AvailableMemory = GetAvailableSystemMemory(),
                    TotalSystemMemory = GetTotalSystemMemory(),
                    Timestamp = DateTime.UtcNow
                };

                // 記錄記憶體歷史
                lock (_historyLock)
                {
                    _memoryHistory.Add(new MemoryUsageSnapshot
                    {
                        WorkingSet = workingSet,
                        PrivateMemory = privateMemory,
                        ManagedMemory = managedMemory,
                        Timestamp = DateTime.UtcNow
                    });

                    // 保留最近 100 個快照
                    if (_memoryHistory.Count > 100)
                    {
                        _memoryHistory.RemoveAt(0);
                    }
                }

                _logger.LogDebug("記憶體使用狀況 - 工作集: {WorkingSet}MB, 私有記憶體: {PrivateMemory}MB, 託管記憶體: {ManagedMemory}MB", 
                    workingSet / (1024 * 1024), privateMemory / (1024 * 1024), managedMemory / (1024 * 1024));

                return memoryInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取記憶體使用狀況失敗");
                return new MemoryUsageInfo();
            }
        }

        /// <summary>
        /// 執行記憶體清理
        /// 設計理念：主動清理記憶體，釋放資源
        /// </summary>
        public async Task<MemoryCleanupResult> PerformMemoryCleanupAsync()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var beforeMemory = GC.GetTotalMemory(false);

                // 執行垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var afterMemory = GC.GetTotalMemory(false);
                var freedMemory = beforeMemory - afterMemory;

                stopwatch.Stop();

                var result = new MemoryCleanupResult
                {
                    FreedMemory = freedMemory,
                    BeforeMemory = beforeMemory,
                    AfterMemory = afterMemory,
                    CleanupTime = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("記憶體清理完成 - 釋放記憶體: {FreedMemory}MB, 耗時: {CleanupTime}ms", 
                    freedMemory / (1024 * 1024), stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記憶體清理失敗");
                return new MemoryCleanupResult();
            }
        }

        /// <summary>
        /// 檢查記憶體健康狀態
        /// 設計理念：監控記憶體健康狀態
        /// </summary>
        public async Task<MemoryHealthStatus> CheckMemoryHealthAsync()
        {
            try
            {
                var memoryInfo = await GetMemoryUsageAsync();
                var healthStatus = new MemoryHealthStatus
                {
                    IsHealthy = true,
                    Issues = new List<string>()
                };

                // 檢查工作集記憶體
                var workingSetMB = memoryInfo.WorkingSet / (1024 * 1024);
                if (workingSetMB > _performanceConfig.MaxMemoryUsageMB)
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"工作集記憶體使用過高: {workingSetMB}MB > {_performanceConfig.MaxMemoryUsageMB}MB");
                }

                // 檢查託管記憶體
                var managedMemoryMB = memoryInfo.ManagedMemory / (1024 * 1024);
                if (managedMemoryMB > _performanceConfig.MaxManagedMemoryMB)
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"託管記憶體使用過高: {managedMemoryMB}MB > {_performanceConfig.MaxManagedMemoryMB}MB");
                }

                // 檢查記憶體使用率
                var memoryUsageRatio = (double)memoryInfo.WorkingSet / memoryInfo.TotalSystemMemory;
                if (memoryUsageRatio > 0.8) // 超過 80%
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"記憶體使用率過高: {memoryUsageRatio:P}");
                }

                // 檢查可用記憶體
                var availableMemoryMB = memoryInfo.AvailableMemory / (1024 * 1024);
                if (availableMemoryMB < 100) // 少於 100MB
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"可用記憶體不足: {availableMemoryMB}MB");
                }

                _logger.LogDebug("記憶體健康檢查完成 - 健康狀態: {IsHealthy}, 問題數: {IssueCount}", 
                    healthStatus.IsHealthy, healthStatus.Issues.Count);

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記憶體健康檢查失敗");
                return new MemoryHealthStatus { IsHealthy = false, Issues = new List<string> { "健康檢查失敗" } };
            }
        }

        /// <summary>
        /// 監控記憶體使用
        /// 設計理念：持續監控記憶體使用趨勢
        /// </summary>
        public async Task<MemoryMonitoringResult> MonitorMemoryUsageAsync()
        {
            try
            {
                lock (_historyLock)
                {
                    if (_memoryHistory.Count < 2)
                    {
                        return new MemoryMonitoringResult
                        {
                            IsStable = true,
                            Trend = "Stable",
                            AverageUsage = 0,
                            PeakUsage = 0
                        };
                    }

                    var recentSnapshots = _memoryHistory.TakeLast(10).ToList();
                    var averageUsage = recentSnapshots.Average(s => s.WorkingSet);
                    var peakUsage = recentSnapshots.Max(s => s.WorkingSet);
                    var trend = CalculateMemoryTrend(recentSnapshots);

                    var isStable = trend == "Stable" || trend == "Decreasing";

                    var result = new MemoryMonitoringResult
                    {
                        IsStable = isStable,
                        Trend = trend,
                        AverageUsage = averageUsage,
                        PeakUsage = peakUsage,
                        MonitoringPeriod = TimeSpan.FromMinutes(10)
                    };

                    _logger.LogDebug("記憶體監控結果 - 趨勢: {Trend}, 平均使用: {AverageUsage}MB, 峰值使用: {PeakUsage}MB", 
                        trend, averageUsage / (1024 * 1024), peakUsage / (1024 * 1024));

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記憶體監控失敗");
                return new MemoryMonitoringResult();
            }
        }

        /// <summary>
        /// 優化記憶體使用
        /// 設計理念：主動優化記憶體使用
        /// </summary>
        public async Task<MemoryOptimizationResult> OptimizeMemoryUsageAsync()
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var beforeMemory = await GetMemoryUsageAsync();

                // 執行記憶體清理
                var cleanupResult = await PerformMemoryCleanupAsync();

                // 檢查是否需要強制垃圾回收
                if (beforeMemory.ManagedMemory > _performanceConfig.MaxManagedMemoryMB * 1024 * 1024)
                {
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    GC.WaitForPendingFinalizers();
                }

                var afterMemory = await GetMemoryUsageAsync();
                stopwatch.Stop();

                var result = new MemoryOptimizationResult
                {
                    BeforeMemory = beforeMemory,
                    AfterMemory = afterMemory,
                    OptimizedMemory = beforeMemory.ManagedMemory - afterMemory.ManagedMemory,
                    OptimizationTime = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("記憶體優化完成 - 優化記憶體: {OptimizedMemory}MB, 耗時: {OptimizationTime}ms", 
                    result.OptimizedMemory / (1024 * 1024), stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記憶體優化失敗");
                return new MemoryOptimizationResult();
            }
        }

        /// <summary>
        /// 獲取記憶體統計資訊
        /// 設計理念：提供詳細的記憶體統計資訊
        /// </summary>
        public async Task<MemoryStatistics> GetMemoryStatisticsAsync()
        {
            try
            {
                lock (_historyLock)
                {
                    if (_memoryHistory.Count == 0)
                    {
                        return new MemoryStatistics();
                    }

                    var recentSnapshots = _memoryHistory.TakeLast(50).ToList();
                    var workingSetStats = recentSnapshots.Select(s => s.WorkingSet);
                    var managedMemoryStats = recentSnapshots.Select(s => s.ManagedMemory);

                    var statistics = new MemoryStatistics
                    {
                        TotalSnapshots = _memoryHistory.Count,
                        AverageWorkingSet = workingSetStats.Average(),
                        MaxWorkingSet = workingSetStats.Max(),
                        MinWorkingSet = workingSetStats.Min(),
                        AverageManagedMemory = managedMemoryStats.Average(),
                        MaxManagedMemory = managedMemoryStats.Max(),
                        MinManagedMemory = managedMemoryStats.Min(),
                        MemoryLimit = _memoryLimit,
                        LastUpdated = DateTime.UtcNow
                    };

                    _logger.LogDebug("記憶體統計資訊 - 平均工作集: {AverageWorkingSet}MB, 最大工作集: {MaxWorkingSet}MB", 
                        statistics.AverageWorkingSet / (1024 * 1024), statistics.MaxWorkingSet / (1024 * 1024));

                    return statistics;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取記憶體統計資訊失敗");
                return new MemoryStatistics();
            }
        }

        /// <summary>
        /// 設定記憶體限制
        /// 設計理念：動態設定記憶體使用限制
        /// </summary>
        public async Task SetMemoryLimitAsync(long limitInBytes)
        {
            try
            {
                _memoryLimit = limitInBytes;
                _logger.LogInformation("記憶體限制已設定: {MemoryLimit}MB", limitInBytes / (1024 * 1024));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定記憶體限制失敗");
            }
        }

        /// <summary>
        /// 檢查是否超過記憶體限制
        /// 設計理念：檢查記憶體使用是否超過限制
        /// </summary>
        public async Task<bool> IsMemoryLimitExceededAsync()
        {
            try
            {
                var memoryInfo = await GetMemoryUsageAsync();
                var isExceeded = memoryInfo.WorkingSet > _memoryLimit;

                if (isExceeded)
                {
                    _logger.LogWarning("記憶體使用超過限制 - 當前使用: {CurrentUsage}MB, 限制: {Limit}MB", 
                        memoryInfo.WorkingSet / (1024 * 1024), _memoryLimit / (1024 * 1024));
                }

                return isExceeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查記憶體限制失敗");
                return false;
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 獲取可用系統記憶體
        /// </summary>
        private long GetAvailableSystemMemory()
        {
            try
            {
                var totalMemory = GC.GetTotalMemory(false);
                var workingSet = Environment.WorkingSet;
                // 簡化實作，返回近似可用記憶體
                return Math.Max(0, workingSet - totalMemory);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 獲取總系統記憶體
        /// </summary>
        private long GetTotalSystemMemory()
        {
            try
            {
                // 使用 Environment.WorkingSet 作為近似值
                return Environment.WorkingSet;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 計算記憶體使用趨勢
        /// </summary>
        private string CalculateMemoryTrend(List<MemoryUsageSnapshot> snapshots)
        {
            if (snapshots.Count < 2)
            {
                return "Stable";
            }

            var firstHalf = snapshots.Take(snapshots.Count / 2).Average(s => s.WorkingSet);
            var secondHalf = snapshots.Skip(snapshots.Count / 2).Average(s => s.WorkingSet);

            var difference = secondHalf - firstHalf;
            var percentageChange = Math.Abs(difference) / firstHalf;

            if (percentageChange < 0.05) // 變化小於 5%
            {
                return "Stable";
            }
            else if (difference > 0)
            {
                return "Increasing";
            }
            else
            {
                return "Decreasing";
            }
        }

        #endregion
    }

    /// <summary>
    /// 記憶體使用資訊
    /// </summary>
    public class MemoryUsageInfo
    {
        public long WorkingSet { get; set; }
        public long PrivateMemory { get; set; }
        public long VirtualMemory { get; set; }
        public long ManagedMemory { get; set; }
        public long AvailableMemory { get; set; }
        public long TotalSystemMemory { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 記憶體清理結果
    /// </summary>
    public class MemoryCleanupResult
    {
        public long FreedMemory { get; set; }
        public long BeforeMemory { get; set; }
        public long AfterMemory { get; set; }
        public long CleanupTime { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 記憶體健康狀態
    /// </summary>
    public class MemoryHealthStatus
    {
        public bool IsHealthy { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    /// <summary>
    /// 記憶體監控結果
    /// </summary>
    public class MemoryMonitoringResult
    {
        public bool IsStable { get; set; }
        public string Trend { get; set; } = string.Empty;
        public double AverageUsage { get; set; }
        public long PeakUsage { get; set; }
        public TimeSpan MonitoringPeriod { get; set; }
    }

    /// <summary>
    /// 記憶體優化結果
    /// </summary>
    public class MemoryOptimizationResult
    {
        public MemoryUsageInfo BeforeMemory { get; set; } = new();
        public MemoryUsageInfo AfterMemory { get; set; } = new();
        public long OptimizedMemory { get; set; }
        public long OptimizationTime { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 記憶體統計資訊
    /// </summary>
    public class MemoryStatistics
    {
        public int TotalSnapshots { get; set; }
        public double AverageWorkingSet { get; set; }
        public long MaxWorkingSet { get; set; }
        public long MinWorkingSet { get; set; }
        public double AverageManagedMemory { get; set; }
        public long MaxManagedMemory { get; set; }
        public long MinManagedMemory { get; set; }
        public long MemoryLimit { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// 記憶體使用快照
    /// </summary>
    public class MemoryUsageSnapshot
    {
        public long WorkingSet { get; set; }
        public long PrivateMemory { get; set; }
        public long ManagedMemory { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 