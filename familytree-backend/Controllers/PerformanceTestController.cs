using Microsoft.AspNetCore.Mvc;
using familytree_backend.Tools;
using familytree_backend.Services;
using familytree_backend.Models;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 性能測試控制器
    /// 用於測試和驗證資料庫優化效果
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PerformanceTestController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<PerformanceTestController> _logger;

        public PerformanceTestController(
            IConfigurationService configurationService,
            ILogger<PerformanceTestController> logger)
        {
            _configurationService = configurationService;
            _logger = logger;
        }

        /// <summary>
        /// 執行完整的資料庫性能測試
        /// </summary>
        [HttpPost("run-full-test")]
        public async Task<ActionResult<ApiResponse<PerformanceTestResults>>> RunFullPerformanceTest()
        {
            try
            {
                _logger.LogInformation("開始執行完整性能測試");

                var connectionString = _configurationService.GetConnectionString();
                var performanceTest = new DatabasePerformanceTest(connectionString, 
                    HttpContext.RequestServices.GetRequiredService<ILogger<DatabasePerformanceTest>>());

                var results = await performanceTest.RunFullTestSuite();

                var report = performanceTest.GenerateReport(results);
                _logger.LogInformation("性能測試完成：\n{Report}", report);

                return Ok(new ApiResponse<PerformanceTestResults>
                {
                    Success = true,
                    Data = results,
                    Message = "性能測試完成",
                    Details = report
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行性能測試失敗");
                return StatusCode(500, new ApiResponse<PerformanceTestResults>
                {
                    Success = false,
                    Message = "性能測試執行失敗",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 執行特定的性能測試
        /// </summary>
        [HttpPost("run-specific-test/{testName}")]
        public async Task<ActionResult<ApiResponse<PerformanceTestResult>>> RunSpecificTest(string testName)
        {
            try
            {
                _logger.LogInformation("開始執行特定性能測試：{TestName}", testName);

                var connectionString = _configurationService.GetConnectionString();
                var performanceTest = new DatabasePerformanceTest(connectionString, 
                    HttpContext.RequestServices.GetRequiredService<ILogger<DatabasePerformanceTest>>());

                PerformanceTestResult result;

                switch (testName.ToLower())
                {
                    case "personlist":
                        var fullResults = await performanceTest.RunFullTestSuite();
                        result = fullResults.Tests.FirstOrDefault(t => t.TestName == "Person List Query") 
                               ?? new PerformanceTestResult { TestName = testName, IsSuccessful = false, ErrorMessage = "測試未找到" };
                        break;
                    
                    case "fulltext":
                        fullResults = await performanceTest.RunFullTestSuite();
                        result = fullResults.Tests.FirstOrDefault(t => t.TestName == "Full Text Search") 
                               ?? new PerformanceTestResult { TestName = testName, IsSuccessful = false, ErrorMessage = "測試未找到" };
                        break;
                    
                    default:
                        return BadRequest(new ApiResponse<PerformanceTestResult>
                        {
                            Success = false,
                            Message = "不支援的測試類型",
                            Details = "支援的測試類型：personlist, fulltext"
                        });
                }

                return Ok(new ApiResponse<PerformanceTestResult>
                {
                    Success = true,
                    Data = result,
                    Message = $"測試 {testName} 完成",
                    Details = $"結果：{(result.IsSuccessful ? "通過" : "未通過")} - {result.ActualTime}ms"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行特定性能測試失敗：{TestName}", testName);
                return StatusCode(500, new ApiResponse<PerformanceTestResult>
                {
                    Success = false,
                    Message = "性能測試執行失敗",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取資料庫健康狀況
        /// </summary>
        [HttpGet("database-health")]
        public async Task<ActionResult<ApiResponse<object>>> GetDatabaseHealth()
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(_configurationService.GetConnectionString());
                await connection.OpenAsync();

                // 執行健康檢查查詢
                var healthData = await Dapper.SqlMapper.QueryAsync(connection, @"
                    SELECT 
                        'database_size' as metric,
                        pg_size_pretty(pg_database_size(current_database())) as value,
                        'Total database size' as description
                    UNION ALL
                    SELECT 
                        'active_connections' as metric,
                        COUNT(*)::TEXT as value,
                        'Active database connections' as description
                    FROM pg_stat_activity 
                    WHERE state = 'active'
                    UNION ALL
                    SELECT 
                        'person_profile_count' as metric,
                        COUNT(*)::TEXT as value,
                        'Total person records' as description
                    FROM person_profile
                    UNION ALL
                    SELECT 
                        'relationships_count' as metric,
                        COUNT(*)::TEXT as value,
                        'Total relationship records' as description
                    FROM relationships
                    WHERE EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'relationships')
                ");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = healthData,
                    Message = "資料庫健康狀況檢查完成"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取資料庫健康狀況失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "資料庫健康檢查失敗",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 獲取索引使用統計
        /// </summary>
        [HttpGet("index-stats")]
        public async Task<ActionResult<ApiResponse<object>>> GetIndexStats()
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(_configurationService.GetConnectionString());
                await connection.OpenAsync();

                var indexStats = await Dapper.SqlMapper.QueryAsync(connection, @"
                    SELECT 
                        schemaname || '.' || tablename as table_name,
                        indexrelname as index_name,
                        idx_scan as scans,
                        idx_tup_read as tuples_read,
                        idx_tup_fetch as tuples_fetched,
                        pg_size_pretty(pg_relation_size(indexrelname::regclass)) as size
                    FROM pg_stat_user_indexes 
                    WHERE schemaname = 'public'
                    AND tablename IN ('person_profile', 'relationships', 'file_metadata', 'user_favorites')
                    ORDER BY idx_scan DESC
                ");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = indexStats,
                    Message = "索引使用統計獲取完成"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取索引統計失敗");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "索引統計獲取失敗",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 執行查詢計劃分析
        /// </summary>
        [HttpPost("analyze-query")]
        public async Task<ActionResult<ApiResponse<object>>> AnalyzeQuery([FromBody] QueryAnalysisRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "查詢語句不能為空"
                    });
                }

                // 簡單的安全檢查 - 只允許 SELECT 語句
                if (!request.Query.Trim().ToUpper().StartsWith("SELECT"))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "只允許 SELECT 查詢"
                    });
                }

                using var connection = new Npgsql.NpgsqlConnection(_configurationService.GetConnectionString());
                await connection.OpenAsync();

                // 執行 EXPLAIN ANALYZE
                var explainQuery = $"EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) {request.Query}";
                var result = await Dapper.SqlMapper.QueryFirstAsync<string>(connection, explainQuery);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = result,
                    Message = "查詢計劃分析完成"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢計劃分析失敗：{Query}", request.Query);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "查詢計劃分析失敗",
                    Details = ex.Message
                });
            }
        }

        /// <summary>
        /// 執行資料庫維護
        /// </summary>
        [HttpPost("maintenance")]
        public async Task<ActionResult<ApiResponse<string>>> RunMaintenance()
        {
            try
            {
                _logger.LogInformation("開始執行資料庫維護");

                using var connection = new Npgsql.NpgsqlConnection(_configurationService.GetConnectionString());
                await connection.OpenAsync();

                // 執行快速維護
                var result = await Dapper.SqlMapper.QueryFirstAsync<string>(connection, "SELECT daily_maintenance()");

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = result,
                    Message = "資料庫維護完成"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫維護失敗");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "資料庫維護失敗",
                    Details = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// 查詢分析請求
    /// </summary>
    public class QueryAnalysisRequest
    {
        public string Query { get; set; } = string.Empty;
    }
}