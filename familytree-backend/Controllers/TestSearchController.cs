// 測試搜索控制器：用於測試基本的資料庫查詢功能
// 主要功能：簡化的搜索測試

using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestSearchController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<TestSearchController> _logger;

        public TestSearchController(IConfiguration configuration, ILogger<TestSearchController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("找不到資料庫連接字符串");
            _logger = logger;
        }

        [HttpGet("simple/{keyword}")]
        public async Task<IActionResult> SimpleSearch(string keyword)
        {
            _logger.LogInformation("🔍 簡單搜索測試: 關鍵字='{keyword}'", keyword);

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                _logger.LogInformation("🔍 開始執行搜索，關鍵字='{keyword}'", keyword);

                var sql = @"
                    SELECT id, name, gender, nationality, mobile, current_workplace as current_employer, education,
                           'person_profile' as source_table, discovery_process as source
                    FROM person_profile 
                    WHERE name ILIKE @keyword OR 
                          mobile ILIKE @keyword OR 
                          phone ILIKE @keyword OR 
                          id_number ILIKE @keyword OR 
                          passport_number ILIKE @keyword OR
                          family_relationships ILIKE @keyword OR
                          important_friends ILIKE @keyword OR
                          current_workplace ILIKE @keyword OR
                          education ILIKE @keyword
                    ORDER BY name, gender
                    LIMIT 10";

                _logger.LogInformation("🔍 執行SQL查詢: {sql}", sql);
                var results = await connection.QueryAsync(sql, new { keyword = $"%{keyword}%" });

                _logger.LogInformation("✅ 簡單搜索成功: 找到{count}筆結果", results.Count());

                var formattedResults = results.Select(r => new
                {
                    id = r.id,
                    name = r.name,
                    gender = r.gender,
                    nationality = r.nationality,
                    mobile = r.mobile,
                    current_employer = r.current_employer,
                    education = r.education,
                    source_table = r.source_table,
                    source = r.source
                });

                return Ok(new
                {
                    success = true,
                    message = $"找到 {results.Count()} 筆結果",
                    data = formattedResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 簡單搜索失敗: 關鍵字='{keyword}', 錯誤={error}", keyword, ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "搜索時發生錯誤",
                    error = ex.Message
                });
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetPersonCount()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var count = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM person_profile");

                return Ok(new
                {
                    success = true,
                    message = "獲取人員數量成功",
                    data = new { totalPersons = count }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取人員數量失敗: {error}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "獲取人員數量時發生錯誤",
                    error = ex.Message
                });
            }
        }
    }
} 