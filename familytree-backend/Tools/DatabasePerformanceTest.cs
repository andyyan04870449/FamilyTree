using System.Diagnostics;
using Dapper;
using Npgsql;
using familytree_backend.Services;
using familytree_backend.Models;

namespace familytree_backend.Tools
{
    /// <summary>
    /// 資料庫性能測試工具
    /// 用於驗證優化效果
    /// </summary>
    public class DatabasePerformanceTest
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabasePerformanceTest> _logger;

        public DatabasePerformanceTest(string connectionString, ILogger<DatabasePerformanceTest> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// 執行完整性能測試套件
        /// </summary>
        public async Task<PerformanceTestResults> RunFullTestSuite()
        {
            _logger.LogInformation("開始執行完整性能測試套件");
            
            var results = new PerformanceTestResults
            {
                TestStartTime = DateTime.UtcNow,
                Tests = new List<PerformanceTestResult>()
            };

            // 1. 人員列表查詢測試
            results.Tests.Add(await TestPersonListQuery());

            // 2. 人員詳情查詢測試
            results.Tests.Add(await TestPersonDetailsQuery());

            // 3. 全文搜索測試
            results.Tests.Add(await TestFullTextSearch());

            // 4. 關係圖查詢測試
            results.Tests.Add(await TestRelationshipQuery());

            // 5. 批量查詢測試
            results.Tests.Add(await TestBatchQuery());

            // 6. 索引使用率測試
            results.Tests.Add(await TestIndexUsage());

            results.TestEndTime = DateTime.UtcNow;
            results.TotalDuration = results.TestEndTime - results.TestStartTime;

            _logger.LogInformation("性能測試套件完成，總耗時：{Duration}ms", 
                results.TotalDuration.TotalMilliseconds);

            return results;
        }

        /// <summary>
        /// 測試人員列表查詢性能
        /// </summary>
        private async Task<PerformanceTestResult> TestPersonListQuery()
        {
            var test = new PerformanceTestResult
            {
                TestName = "Person List Query",
                TargetTime = 500 // 目標：500ms以下
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 模擬實際查詢：1000筆人員資料
                const string sql = @"
                    SELECT * FROM person_profile 
                    ORDER BY id DESC 
                    LIMIT 1000";

                var results = await connection.QueryAsync<PersonDataModel>(sql);
                
                stopwatch.Stop();

                test.ActualTime = (int)stopwatch.ElapsedMilliseconds;
                test.ResultCount = results.Count();
                test.IsSuccessful = test.ActualTime <= test.TargetTime;
                test.Details = $"查詢 {test.ResultCount} 筆人員資料";

                _logger.LogInformation("人員列表查詢測試：{Time}ms，{Count}筆，目標{Target}ms - {Result}", 
                    test.ActualTime, test.ResultCount, test.TargetTime, 
                    test.IsSuccessful ? "通過" : "未通過");
            }
            catch (Exception ex)
            {
                test.IsSuccessful = false;
                test.ErrorMessage = ex.Message;
                _logger.LogError(ex, "人員列表查詢測試失敗");
            }

            return test;
        }

        /// <summary>
        /// 測試人員詳情查詢性能
        /// </summary>
        private async Task<PerformanceTestResult> TestPersonDetailsQuery()
        {
            var test = new PerformanceTestResult
            {
                TestName = "Person Details Query",
                TargetTime = 300 // 目標：300ms以下
            };

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取一些測試用的人員ID
                var personIds = await connection.QueryAsync<int>(
                    "SELECT id FROM person_profile LIMIT 10");

                if (!personIds.Any())
                {
                    test.IsSuccessful = false;
                    test.ErrorMessage = "沒有測試資料";
                    return test;
                }

                var stopwatch = Stopwatch.StartNew();

                // 測試批量查詢人員詳情
                const string sql = @"
                    SELECT p.*, 
                           (SELECT COUNT(*) FROM relationships r WHERE r.person_id = p.id) as relationship_count,
                           (SELECT COUNT(*) FROM file_metadata f WHERE f.entity_type = 'person' AND f.entity_id = p.id AND f.is_deleted = false) as file_count
                    FROM person_profile p 
                    WHERE p.id = ANY(@PersonIds)";

                var results = await connection.QueryAsync(sql, new { PersonIds = personIds.ToArray() });

                stopwatch.Stop();

                test.ActualTime = (int)stopwatch.ElapsedMilliseconds;
                test.ResultCount = results.Count();
                test.IsSuccessful = test.ActualTime <= test.TargetTime;
                test.Details = $"查詢 {test.ResultCount} 個人員詳情（含關聯資料統計）";

                _logger.LogInformation("人員詳情查詢測試：{Time}ms，{Count}筆，目標{Target}ms - {Result}", 
                    test.ActualTime, test.ResultCount, test.TargetTime, 
                    test.IsSuccessful ? "通過" : "未通過");
            }
            catch (Exception ex)
            {
                test.IsSuccessful = false;
                test.ErrorMessage = ex.Message;
                _logger.LogError(ex, "人員詳情查詢測試失敗");
            }

            return test;
        }

        /// <summary>
        /// 測試全文搜索性能
        /// </summary>
        private async Task<PerformanceTestResult> TestFullTextSearch()
        {
            var test = new PerformanceTestResult
            {
                TestName = "Full Text Search",
                TargetTime = 800 // 目標：800ms以下
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 使用全文搜索索引
                const string sql = @"
                    SELECT * FROM person_profile 
                    WHERE to_tsvector('simple', 
                        coalesce(name,'') || ' ' || 
                        coalesce(email,'') || ' ' || 
                        coalesce(mobile,'') || ' ' ||
                        coalesce(family_relationships,'') || ' ' ||
                        coalesce(friends,'') || ' ' ||
                        coalesce(remarks,'')
                    ) @@ to_tsquery('simple', '王 | 李 | 張')
                    LIMIT 100";

                var results = await connection.QueryAsync<PersonDataModel>(sql);

                stopwatch.Stop();

                test.ActualTime = (int)stopwatch.ElapsedMilliseconds;
                test.ResultCount = results.Count();
                test.IsSuccessful = test.ActualTime <= test.TargetTime;
                test.Details = $"全文搜索找到 {test.ResultCount} 筆結果";

                _logger.LogInformation("全文搜索測試：{Time}ms，{Count}筆，目標{Target}ms - {Result}", 
                    test.ActualTime, test.ResultCount, test.TargetTime, 
                    test.IsSuccessful ? "通過" : "未通過");
            }
            catch (Exception ex)
            {
                test.IsSuccessful = false;
                test.ErrorMessage = ex.Message;
                _logger.LogError(ex, "全文搜索測試失敗");
            }

            return test;
        }

        /// <summary>
        /// 測試關係圖查詢性能
        /// </summary>
        private async Task<PerformanceTestResult> TestRelationshipQuery()
        {
            var test = new PerformanceTestResult
            {
                TestName = "Relationship Graph Query",
                TargetTime = 1000 // 目標：1000ms以下
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 模擬關係圖查詢
                const string sql = @"
                    WITH RECURSIVE relationship_tree AS (
                        -- 起始節點
                        SELECT id, name, 0 as level
                        FROM person_profile 
                        WHERE id IN (SELECT id FROM person_profile LIMIT 5)
                        
                        UNION ALL
                        
                        -- 遞歸查詢關係
                        SELECT p.id, p.name, rt.level + 1
                        FROM person_profile p
                        INNER JOIN relationships r ON p.id = r.related_person_id
                        INNER JOIN relationship_tree rt ON r.person_id = rt.id
                        WHERE rt.level < 3
                    )
                    SELECT * FROM relationship_tree
                    ORDER BY level, id";

                var results = await connection.QueryAsync(sql);

                stopwatch.Stop();

                test.ActualTime = (int)stopwatch.ElapsedMilliseconds;
                test.ResultCount = results.Count();
                test.IsSuccessful = test.ActualTime <= test.TargetTime;
                test.Details = $"關係圖查詢找到 {test.ResultCount} 個節點";

                _logger.LogInformation("關係圖查詢測試：{Time}ms，{Count}筆，目標{Target}ms - {Result}", 
                    test.ActualTime, test.ResultCount, test.TargetTime, 
                    test.IsSuccessful ? "通過" : "未通過");
            }
            catch (Exception ex)
            {
                test.IsSuccessful = false;
                test.ErrorMessage = ex.Message;
                _logger.LogError(ex, "關係圖查詢測試失敗");
            }

            return test;
        }

        /// <summary>
        /// 測試批量查詢性能
        /// </summary>
        private async Task<PerformanceTestResult> TestBatchQuery()
        {
            var test = new PerformanceTestResult
            {
                TestName = "Batch Query Performance",
                TargetTime = 400 // 目標：400ms以下
            };

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 獲取測試用的人員ID
                var personIds = await connection.QueryAsync<int>(
                    "SELECT id FROM person_profile LIMIT 20");

                if (!personIds.Any())
                {
                    test.IsSuccessful = false;
                    test.ErrorMessage = "沒有測試資料";
                    return test;
                }

                var stopwatch = Stopwatch.StartNew();

                // 批量查詢：人員 + 關係 + 檔案
                var personTask = connection.QueryAsync<PersonDataModel>(
                    "SELECT * FROM person_profile WHERE id = ANY(@PersonIds)",
                    new { PersonIds = personIds.ToArray() });

                var relationshipTask = connection.QueryAsync<RelationshipDto>(
                    @"SELECT r.*, p1.name as PersonName, p2.name as RelatedPersonName
                      FROM relationships r
                      LEFT JOIN person_profile p1 ON r.person_id = p1.id
                      LEFT JOIN person_profile p2 ON r.related_person_id = p2.id
                      WHERE r.person_id = ANY(@PersonIds)",
                    new { PersonIds = personIds.ToArray() });

                var fileTask = connection.QueryAsync<PhotoDto>(
                    @"SELECT * FROM file_metadata 
                      WHERE entity_type = 'person' AND entity_id = ANY(@PersonIds) AND is_deleted = false",
                    new { PersonIds = personIds.ToArray() });

                await Task.WhenAll(personTask, relationshipTask, fileTask);

                stopwatch.Stop();

                var totalResults = personTask.Result.Count() + relationshipTask.Result.Count() + fileTask.Result.Count();

                test.ActualTime = (int)stopwatch.ElapsedMilliseconds;
                test.ResultCount = totalResults;
                test.IsSuccessful = test.ActualTime <= test.TargetTime;
                test.Details = $"批量查詢：{personTask.Result.Count()}人員，{relationshipTask.Result.Count()}關係，{fileTask.Result.Count()}檔案";

                _logger.LogInformation("批量查詢測試：{Time}ms，{Count}筆，目標{Target}ms - {Result}", 
                    test.ActualTime, test.ResultCount, test.TargetTime, 
                    test.IsSuccessful ? "通過" : "未通過");
            }
            catch (Exception ex)
            {
                test.IsSuccessful = false;
                test.ErrorMessage = ex.Message;
                _logger.LogError(ex, "批量查詢測試失敗");
            }

            return test;
        }

        /// <summary>
        /// 測試索引使用率
        /// </summary>
        private async Task<PerformanceTestResult> TestIndexUsage()
        {
            var test = new PerformanceTestResult
            {
                TestName = "Index Usage Analysis",
                TargetTime = 100 // 目標：100ms以下
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 檢查索引使用率
                const string sql = @"
                    SELECT 
                        schemaname,
                        tablename,
                        indexrelname,
                        idx_scan,
                        idx_tup_read,
                        idx_tup_fetch
                    FROM pg_stat_user_indexes 
                    WHERE schemaname = 'public'
                    AND tablename IN ('person_profile', 'relationships', 'file_metadata')
                    ORDER BY idx_scan DESC";

                var results = await connection.QueryAsync(sql);

                stopwatch.Stop();

                test.ActualTime = (int)stopwatch.ElapsedMilliseconds;
                test.ResultCount = results.Count();
                test.IsSuccessful = test.ActualTime <= test.TargetTime;
                
                var usedIndexes = results.Count(r => r.idx_scan > 0);
                test.Details = $"檢查 {test.ResultCount} 個索引，{usedIndexes} 個被使用";

                _logger.LogInformation("索引使用率測試：{Time}ms，{Count}個索引，{Used}個被使用 - {Result}", 
                    test.ActualTime, test.ResultCount, usedIndexes,
                    test.IsSuccessful ? "通過" : "未通過");
            }
            catch (Exception ex)
            {
                test.IsSuccessful = false;
                test.ErrorMessage = ex.Message;
                _logger.LogError(ex, "索引使用率測試失敗");
            }

            return test;
        }

        /// <summary>
        /// 生成性能測試報告
        /// </summary>
        public string GenerateReport(PerformanceTestResults results)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== 資料庫性能測試報告 ===");
            report.AppendLine($"測試時間：{results.TestStartTime:yyyy-MM-dd HH:mm:ss} - {results.TestEndTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"總耗時：{results.TotalDuration.TotalMilliseconds:F0}ms");
            report.AppendLine();

            var passedTests = results.Tests.Count(t => t.IsSuccessful);
            var totalTests = results.Tests.Count;
            
            report.AppendLine($"測試結果：{passedTests}/{totalTests} 通過");
            report.AppendLine();

            foreach (var test in results.Tests)
            {
                report.AppendLine($"[{(test.IsSuccessful ? "✓" : "✗")}] {test.TestName}");
                report.AppendLine($"    實際時間：{test.ActualTime}ms / 目標時間：{test.TargetTime}ms");
                report.AppendLine($"    結果數量：{test.ResultCount}");
                if (!string.IsNullOrEmpty(test.Details))
                    report.AppendLine($"    詳情：{test.Details}");
                if (!string.IsNullOrEmpty(test.ErrorMessage))
                    report.AppendLine($"    錯誤：{test.ErrorMessage}");
                report.AppendLine();
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// 性能測試結果集合
    /// </summary>
    public class PerformanceTestResults
    {
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<PerformanceTestResult> Tests { get; set; } = new();
    }

    /// <summary>
    /// 單個性能測試結果
    /// </summary>
    public class PerformanceTestResult
    {
        public string TestName { get; set; } = string.Empty;
        public int TargetTime { get; set; } // 目標時間（毫秒）
        public int ActualTime { get; set; } // 實際時間（毫秒）
        public int ResultCount { get; set; } // 結果數量
        public bool IsSuccessful { get; set; } // 是否通過
        public string? Details { get; set; } // 詳細信息
        public string? ErrorMessage { get; set; } // 錯誤信息
    }
}