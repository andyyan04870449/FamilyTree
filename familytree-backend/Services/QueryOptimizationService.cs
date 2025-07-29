// 查詢優化服務 - 提供查詢效能優化和 N+1 問題解決
// 設計改善：優化資料庫查詢，減少 N+1 問題，改善記憶體使用
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace familytree_backend.Services
{
    /// <summary>
    /// 查詢優化服務介面
    /// 設計理念：提供查詢效能優化和 N+1 問題解決
    /// </summary>
    public interface IQueryOptimizationService
    {
        /// <summary>
        /// 執行批次查詢
        /// </summary>
        Task<IEnumerable<T>> ExecuteBatchQueryAsync<T>(string sql, IEnumerable<object> parameters, int batchSize = 1000);

        /// <summary>
        /// 執行分頁查詢優化
        /// </summary>
        Task<(IEnumerable<T> Data, int TotalCount)> ExecutePagedQueryAsync<T>(
            string countSql, 
            string dataSql, 
            object parameters, 
            int page, 
            int pageSize,
            string? orderBy = null);

        /// <summary>
        /// 執行關聯查詢優化
        /// </summary>
        Task<Dictionary<int, T>> ExecuteRelatedQueryAsync<T>(
            string sql, 
            object parameters, 
            Func<dynamic, int> keySelector,
            Func<dynamic, T> valueSelector);

        /// <summary>
        /// 執行查詢效能監控
        /// </summary>
        Task<QueryPerformanceMetrics> MonitorQueryPerformanceAsync<T>(Func<Task<T>> query, string queryName);

        /// <summary>
        /// 執行查詢效能監控並返回結果
        /// </summary>
        Task<T> ExecuteWithPerformanceMonitoringAsync<T>(Func<Task<T>> query, string queryName);

        /// <summary>
        /// 檢查查詢效能
        /// </summary>
        Task<QueryHealthStatus> CheckQueryHealthAsync();

        /// <summary>
        /// 優化查詢語句
        /// </summary>
        string OptimizeQuery(string sql, QueryOptimizationOptions? options = null);

        /// <summary>
        /// 建立查詢索引建議
        /// </summary>
        Task<List<IndexSuggestion>> GenerateIndexSuggestionsAsync(string sql, object parameters);
    }

    /// <summary>
    /// 查詢優化服務實作
    /// 職責：提供查詢效能優化，解決 N+1 問題，改善記憶體使用
    /// </summary>
    public class QueryOptimizationService : IQueryOptimizationService
    {
        private readonly ILogger<QueryOptimizationService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly IDataAccessService _dataAccessService;
        private readonly ICacheService _cacheService;
        private readonly PerformanceConfiguration _performanceConfig;
        private readonly DatabaseConfiguration _databaseConfig;

        // 查詢效能統計
        private readonly Dictionary<string, QueryStats> _queryStats = new();
        private readonly object _statsLock = new object();

        /// <summary>
        /// 建構子
        /// </summary>
        public QueryOptimizationService(
            ILogger<QueryOptimizationService> logger,
            IConfigurationService configurationService,
            IDataAccessService dataAccessService,
            ICacheService cacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _dataAccessService = dataAccessService ?? throw new ArgumentNullException(nameof(dataAccessService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _performanceConfig = configurationService.GetPerformanceConfiguration();
            _databaseConfig = configurationService.GetDatabaseConfiguration();
        }

        /// <summary>
        /// 執行批次查詢
        /// 設計理念：分批處理大量資料，避免記憶體溢出
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteBatchQueryAsync<T>(string sql, IEnumerable<object> parameters, int batchSize = 1000)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var results = new List<T>();
                var parameterList = parameters.ToList();
                var totalBatches = (int)Math.Ceiling((double)parameterList.Count / batchSize);

                _logger.LogInformation("開始批次查詢 - 總參數數: {ParameterCount}, 批次大小: {BatchSize}, 總批次數: {TotalBatches}", 
                    parameterList.Count, batchSize, totalBatches);

                for (int i = 0; i < totalBatches; i++)
                {
                    var batchParameters = parameterList.Skip(i * batchSize).Take(batchSize);
                    var batchSql = sql;

                    // 為批次查詢優化 SQL
                    if (batchParameters.Count() < batchSize)
                    {
                        batchSql = OptimizeQueryForBatch(sql, batchParameters.Count());
                    }

                    var batchResults = await _dataAccessService.ExecuteQueryAsync<T>(batchSql, batchParameters);
                    results.AddRange(batchResults);

                    _logger.LogDebug("批次 {BatchNumber}/{TotalBatches} 完成 - 結果數: {ResultCount}", 
                        i + 1, totalBatches, batchResults.Count());
                }

                stopwatch.Stop();
                _logger.LogInformation("批次查詢完成 - 總結果數: {TotalResults}, 耗時: {Duration}ms", 
                    results.Count, stopwatch.ElapsedMilliseconds);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批次查詢失敗 - SQL: {Sql}, 參數數: {ParameterCount}", 
                    sql, parameters.Count());
                throw;
            }
        }

        /// <summary>
        /// 執行分頁查詢優化
        /// 設計理念：優化分頁查詢效能，減少不必要的資料傳輸
        /// </summary>
        public async Task<(IEnumerable<T> Data, int TotalCount)> ExecutePagedQueryAsync<T>(
            string countSql, 
            string dataSql, 
            object parameters, 
            int page, 
            int pageSize,
            string? orderBy = null)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var cacheKey = $"paged_query:{typeof(T).Name}:{page}:{pageSize}:{orderBy}:{JsonSerializer.Serialize(parameters)}";

                // 嘗試從快取獲取
                var cachedResult = await _cacheService.GetAsync<(IEnumerable<T> Data, int TotalCount)>(cacheKey);
                if (cachedResult.Data != null)
                {
                    _logger.LogDebug("分頁查詢快取命中: {CacheKey}", cacheKey);
                    return cachedResult;
                }

                // 優化查詢語句
                var optimizedCountSql = OptimizeQuery(countSql);
                var optimizedDataSql = OptimizeQuery(dataSql);

                // 並行執行計數和資料查詢
                var countTask = _dataAccessService.ExecuteScalarAsync<int>(optimizedCountSql, parameters);
                var dataTask = _dataAccessService.ExecuteQueryAsync<T>(optimizedDataSql, parameters);

                await Task.WhenAll(countTask, dataTask);

                var totalCount = await countTask;
                var data = await dataTask;

                var result = (data, totalCount);

                // 快取結果
                var cacheExpiration = TimeSpan.FromMinutes(_performanceConfig.QueryCacheExpirationMinutes);
                await _cacheService.SetAsync(cacheKey, result, cacheExpiration);

                stopwatch.Stop();
                _logger.LogInformation("分頁查詢完成 - 頁碼: {Page}, 頁面大小: {PageSize}, 總數: {TotalCount}, 結果數: {ResultCount}, 耗時: {Duration}ms", 
                    page, pageSize, totalCount, data.Count(), stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分頁查詢失敗 - 頁碼: {Page}, 頁面大小: {PageSize}", page, pageSize);
                throw;
            }
        }

        /// <summary>
        /// 執行關聯查詢優化
        /// 設計理念：解決 N+1 問題，一次性獲取關聯資料
        /// </summary>
        public async Task<Dictionary<int, T>> ExecuteRelatedQueryAsync<T>(
            string sql, 
            object parameters, 
            Func<dynamic, int> keySelector,
            Func<dynamic, T> valueSelector)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var cacheKey = $"related_query:{typeof(T).Name}:{JsonSerializer.Serialize(parameters)}";

                // 嘗試從快取獲取
                var cachedResult = await _cacheService.GetAsync<Dictionary<int, T>>(cacheKey);
                if (cachedResult != null)
                {
                    _logger.LogDebug("關聯查詢快取命中: {CacheKey}", cacheKey);
                    return cachedResult;
                }

                // 優化查詢語句
                var optimizedSql = OptimizeQuery(sql);

                // 執行查詢
                var results = await _dataAccessService.ExecuteQueryAsync<dynamic>(optimizedSql, parameters);
                var resultDict = new Dictionary<int, T>();
                foreach (var row in results)
                {
                    var key = keySelector(row);
                    var value = valueSelector(row);
                    resultDict[key] = value;
                }

                // 快取結果
                var cacheExpiration = TimeSpan.FromMinutes(_performanceConfig.QueryCacheExpirationMinutes);
                await _cacheService.SetAsync(cacheKey, resultDict, cacheExpiration);

                stopwatch.Stop();
                _logger.LogInformation("關聯查詢完成 - 結果數: {ResultCount}, 耗時: {Duration}ms", 
                    resultDict.Count, stopwatch.ElapsedMilliseconds);

                return resultDict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "關聯查詢失敗 - SQL: {Sql}", sql);
                throw;
            }
        }

        /// <summary>
        /// 執行查詢效能監控（返回指標）
        /// 設計理念：監控查詢效能，收集統計資訊
        /// </summary>
        public async Task<QueryPerformanceMetrics> MonitorQueryPerformanceAsync<T>(Func<Task<T>> query, string queryName)
        {
            var stopwatch = Stopwatch.StartNew();
            var memoryBefore = GC.GetTotalMemory(false);

            try
            {
                var result = await query();
                stopwatch.Stop();

                var memoryAfter = GC.GetTotalMemory(false);
                var memoryUsed = memoryAfter - memoryBefore;

                var metrics = new QueryPerformanceMetrics
                {
                    QueryName = queryName,
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    MemoryUsed = memoryUsed,
                    Timestamp = DateTime.UtcNow,
                    IsSuccessful = true
                };

                // 記錄效能指標
                UpdateQueryStats("BatchQuery", metrics);

                return metrics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                var metrics = new QueryPerformanceMetrics
                {
                    QueryName = queryName,
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    MemoryUsed = 0,
                    Timestamp = DateTime.UtcNow,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };

                UpdateQueryStats("BatchQuery", metrics);
                throw;
            }
        }

        /// <summary>
        /// 執行查詢效能監控（返回結果）
        /// 設計理念：監控查詢效能，並返回查詢結果
        /// </summary>
        public async Task<T> ExecuteWithPerformanceMonitoringAsync<T>(Func<Task<T>> query, string queryName)
        {
            var stopwatch = Stopwatch.StartNew();
            var memoryBefore = GC.GetTotalMemory(false);

            try
            {
                var result = await query();
                stopwatch.Stop();

                var memoryAfter = GC.GetTotalMemory(false);
                var memoryUsed = memoryAfter - memoryBefore;

                var metrics = new QueryPerformanceMetrics
                {
                    QueryName = queryName,
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    MemoryUsed = memoryUsed,
                    Timestamp = DateTime.UtcNow,
                    IsSuccessful = true
                };

                // 更新統計資訊
                UpdateQueryStats(queryName, metrics);

                // 記錄慢查詢
                if (stopwatch.ElapsedMilliseconds > _performanceConfig.SlowQueryThresholdMs)
                {
                    _logger.LogWarning("慢查詢檢測 - 查詢: {QueryName}, 耗時: {Duration}ms, 記憶體使用: {MemoryUsed} bytes", 
                        queryName, stopwatch.ElapsedMilliseconds, memoryUsed);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false);
                var memoryUsed = memoryAfter - memoryBefore;

                var metrics = new QueryPerformanceMetrics
                {
                    QueryName = queryName,
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    MemoryUsed = memoryUsed,
                    Timestamp = DateTime.UtcNow,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message
                };

                UpdateQueryStats(queryName, metrics);
                throw;
            }
        }

        /// <summary>
        /// 檢查查詢效能
        /// 設計理念：監控查詢健康狀態
        /// </summary>
        public async Task<QueryHealthStatus> CheckQueryHealthAsync()
        {
            try
            {
                lock (_statsLock)
                {
                    var healthStatus = new QueryHealthStatus
                    {
                        IsHealthy = true,
                        Issues = new List<string>()
                    };

                    var now = DateTime.UtcNow;
                    var recentStats = _queryStats.Values
                        .Where(stat => now - stat.LastExecution < TimeSpan.FromMinutes(10))
                        .ToList();

                    if (recentStats.Any())
                    {
                        var avgExecutionTime = recentStats.Average(stat => stat.AverageExecutionTime);
                        var maxExecutionTime = recentStats.Max(stat => stat.MaxExecutionTime);
                        var errorRate = recentStats.Count(stat => stat.ErrorCount > 0) / (double)recentStats.Count;

                        // 檢查平均執行時間
                        if (avgExecutionTime > _performanceConfig.SlowQueryThresholdMs)
                        {
                            healthStatus.IsHealthy = false;
                            healthStatus.Issues.Add($"平均查詢執行時間過高: {avgExecutionTime:F2}ms");
                        }

                        // 檢查最大執行時間
                        if (maxExecutionTime > _performanceConfig.SlowQueryThresholdMs * 2)
                        {
                            healthStatus.IsHealthy = false;
                            healthStatus.Issues.Add($"最大查詢執行時間過高: {maxExecutionTime:F2}ms");
                        }

                        // 檢查錯誤率
                        if (errorRate > 0.1) // 超過 10% 的錯誤率
                        {
                            healthStatus.IsHealthy = false;
                            healthStatus.Issues.Add($"查詢錯誤率過高: {errorRate:P}");
                        }
                    }

                    return healthStatus;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢健康檢查失敗");
                return new QueryHealthStatus { IsHealthy = false, Issues = new List<string> { "健康檢查失敗" } };
            }
        }

        /// <summary>
        /// 優化查詢語句
        /// 設計理念：自動優化查詢語句，提升效能
        /// </summary>
        public string OptimizeQuery(string sql, QueryOptimizationOptions? options = null)
        {
            try
            {
                var optimizedSql = sql;

                // 移除不必要的空白
                optimizedSql = Regex.Replace(optimizedSql, @"\s+", " ").Trim();

                // 優化 SELECT 語句
                if (options?.OptimizeSelect == true)
                {
                    optimizedSql = OptimizeSelectClause(optimizedSql);
                }

                // 優化 WHERE 語句
                if (options?.OptimizeWhere == true)
                {
                    optimizedSql = OptimizeWhereClause(optimizedSql);
                }

                // 優化 ORDER BY 語句
                if (options?.OptimizeOrderBy == true)
                {
                    optimizedSql = OptimizeOrderByClause(optimizedSql);
                }

                // 添加查詢提示
                if (options?.AddQueryHints == true)
                {
                    optimizedSql = AddQueryHints(optimizedSql);
                }

                _logger.LogDebug("查詢優化完成 - 原始長度: {OriginalLength}, 優化後長度: {OptimizedLength}", 
                    sql.Length, optimizedSql.Length);

                return optimizedSql;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢優化失敗 - SQL: {Sql}", sql);
                return sql; // 返回原始 SQL
            }
        }

        /// <summary>
        /// 建立查詢索引建議
        /// 設計理念：分析查詢並提供索引建議
        /// </summary>
        public async Task<List<IndexSuggestion>> GenerateIndexSuggestionsAsync(string sql, object parameters)
        {
            try
            {
                var suggestions = new List<IndexSuggestion>();

                // 分析 WHERE 子句中的欄位
                var whereFields = ExtractWhereFields(sql);
                foreach (var field in whereFields)
                {
                    suggestions.Add(new IndexSuggestion
                    {
                        TableName = ExtractTableName(sql),
                        ColumnName = field,
                        IndexType = "BTREE",
                        Reason = $"WHERE 子句中使用的欄位: {field}",
                        Priority = "HIGH"
                    });
                }

                // 分析 ORDER BY 子句中的欄位
                var orderByFields = ExtractOrderByFields(sql);
                foreach (var field in orderByFields)
                {
                    if (!whereFields.Contains(field))
                    {
                        suggestions.Add(new IndexSuggestion
                        {
                            TableName = ExtractTableName(sql),
                            ColumnName = field,
                            IndexType = "BTREE",
                            Reason = $"ORDER BY 子句中使用的欄位: {field}",
                            Priority = "MEDIUM"
                        });
                    }
                }

                // 分析 JOIN 子句中的欄位
                var joinFields = ExtractJoinFields(sql);
                foreach (var field in joinFields)
                {
                    if (!whereFields.Contains(field))
                    {
                        suggestions.Add(new IndexSuggestion
                        {
                            TableName = ExtractTableName(sql),
                            ColumnName = field,
                            IndexType = "BTREE",
                            Reason = $"JOIN 子句中使用的欄位: {field}",
                            Priority = "HIGH"
                        });
                    }
                }

                _logger.LogInformation("索引建議生成完成 - 查詢: {QueryName}, 建議數: {SuggestionCount}", 
                    "Generated", suggestions.Count);

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "索引建議生成失敗 - SQL: {Sql}", sql);
                return new List<IndexSuggestion>();
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 更新查詢統計資訊
        /// </summary>
        private void UpdateQueryStats(string queryName, QueryPerformanceMetrics metrics)
        {
            lock (_statsLock)
            {
                if (!_queryStats.ContainsKey(queryName))
                {
                    _queryStats[queryName] = new QueryStats();
                }

                var stats = _queryStats[queryName];
                stats.TotalExecutions++;
                stats.TotalExecutionTime += metrics.ExecutionTime;
                stats.AverageExecutionTime = stats.TotalExecutionTime / stats.TotalExecutions;
                stats.MaxExecutionTime = Math.Max(stats.MaxExecutionTime, metrics.ExecutionTime);
                stats.LastExecution = metrics.Timestamp;

                if (!metrics.IsSuccessful)
                {
                    stats.ErrorCount++;
                }
            }
        }

        /// <summary>
        /// 為批次查詢優化 SQL
        /// </summary>
        private string OptimizeQueryForBatch(string sql, int batchSize)
        {
            // 這裡可以根據批次大小優化 SQL
            // 例如：調整 LIMIT 子句
            return sql;
        }

        /// <summary>
        /// 優化 SELECT 子句
        /// </summary>
        private string OptimizeSelectClause(string sql)
        {
            // 移除 SELECT *，改為明確的欄位列表
            if (sql.Contains("SELECT *"))
            {
                // 這裡應該根據實際的資料表結構替換為具體欄位
                // 暫時返回原始 SQL
                return sql;
            }
            return sql;
        }

        /// <summary>
        /// 優化 WHERE 子句
        /// </summary>
        private string OptimizeWhereClause(string sql)
        {
            // 這裡可以優化 WHERE 子句的順序和條件
            return sql;
        }

        /// <summary>
        /// 優化 ORDER BY 子句
        /// </summary>
        private string OptimizeOrderByClause(string sql)
        {
            // 這裡可以優化 ORDER BY 子句
            return sql;
        }

        /// <summary>
        /// 添加查詢提示
        /// </summary>
        private string AddQueryHints(string sql)
        {
            // 這裡可以添加資料庫特定的查詢提示
            return sql;
        }

        /// <summary>
        /// 提取 WHERE 子句中的欄位
        /// </summary>
        private List<string> ExtractWhereFields(string sql)
        {
            var fields = new List<string>();
            var whereMatch = Regex.Match(sql, @"WHERE\s+(.+?)(?:\s+ORDER\s+BY|\s+GROUP\s+BY|\s+LIMIT|$)", RegexOptions.IgnoreCase);
            if (whereMatch.Success)
            {
                var whereClause = whereMatch.Groups[1].Value;
                var fieldMatches = Regex.Matches(whereClause, @"(\w+)\s*[=<>!]");
                foreach (Match match in fieldMatches)
                {
                    fields.Add(match.Groups[1].Value);
                }
            }
            return fields.Distinct().ToList();
        }

        /// <summary>
        /// 提取 ORDER BY 子句中的欄位
        /// </summary>
        private List<string> ExtractOrderByFields(string sql)
        {
            var fields = new List<string>();
            var orderByMatch = Regex.Match(sql, @"ORDER\s+BY\s+(.+?)(?:\s+LIMIT|$)", RegexOptions.IgnoreCase);
            if (orderByMatch.Success)
            {
                var orderByClause = orderByMatch.Groups[1].Value;
                var fieldMatches = Regex.Matches(orderByClause, @"(\w+)(?:\s+ASC|\s+DESC)?");
                foreach (Match match in fieldMatches)
                {
                    fields.Add(match.Groups[1].Value);
                }
            }
            return fields.Distinct().ToList();
        }

        /// <summary>
        /// 提取 JOIN 子句中的欄位
        /// </summary>
        private List<string> ExtractJoinFields(string sql)
        {
            var fields = new List<string>();
            var joinMatches = Regex.Matches(sql, @"JOIN\s+\w+\s+ON\s+(\w+\.\w+)\s*=\s*(\w+\.\w+)", RegexOptions.IgnoreCase);
            foreach (Match match in joinMatches)
            {
                fields.Add(match.Groups[1].Value.Split('.')[1]);
                fields.Add(match.Groups[2].Value.Split('.')[1]);
            }
            return fields.Distinct().ToList();
        }

        /// <summary>
        /// 提取資料表名稱
        /// </summary>
        private string ExtractTableName(string sql)
        {
            var fromMatch = Regex.Match(sql, @"FROM\s+(\w+)", RegexOptions.IgnoreCase);
            return fromMatch.Success ? fromMatch.Groups[1].Value : "unknown";
        }

        #endregion
    }

    /// <summary>
    /// 查詢效能指標
    /// </summary>
    public class QueryPerformanceMetrics
    {
        public string QueryName { get; set; } = string.Empty;
        public long ExecutionTime { get; set; }
        public long MemoryUsed { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 查詢統計資訊
    /// </summary>
    public class QueryStats
    {
        public int TotalExecutions { get; set; }
        public long TotalExecutionTime { get; set; }
        public double AverageExecutionTime { get; set; }
        public long MaxExecutionTime { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastExecution { get; set; }
    }

    /// <summary>
    /// 查詢健康狀態
    /// </summary>
    public class QueryHealthStatus
    {
        public bool IsHealthy { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    /// <summary>
    /// 查詢優化選項
    /// </summary>
    public class QueryOptimizationOptions
    {
        public bool OptimizeSelect { get; set; } = true;
        public bool OptimizeWhere { get; set; } = true;
        public bool OptimizeOrderBy { get; set; } = true;
        public bool AddQueryHints { get; set; } = false;
    }

    /// <summary>
    /// 索引建議
    /// </summary>
    public class IndexSuggestion
    {
        public string TableName { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public string IndexType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }
} 