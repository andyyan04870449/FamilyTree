// 全文檢索控制器：提供全文搜索、關鍵字管理、搜索歷史等功能
// 主要功能：關鍵字搜索（精準/模糊）、搜索歷史管理、熱門關鍵字統計

using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using familytree_backend.Models;
using System.Text;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FullTextSearchController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<FullTextSearchController> _logger;

        public FullTextSearchController(IConfiguration configuration, ILogger<FullTextSearchController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("找不到資料庫連接字符串");
            _logger = logger;
        }

        /// <summary>
        /// 全文檢索搜索
        /// </summary>
        /// <param name="request">搜索請求</param>
        /// <returns>搜索結果</returns>
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            _logger.LogInformation("📋 收到全文檢索請求: 關鍵字='{keyword}', 搜索類型={searchType}, 頁碼={page}, 頁面大小={pageSize}", 
                request.Keyword, request.SearchType, request.Page, request.PageSize);

            try
            {
                // 參數驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    var errorMessage = string.Join("; ", errors);
                    _logger.LogWarning("⚠️  請求參數驗證失敗: {errors}", errorMessage);
                    return BadRequest(ApiResponse<object>.ErrorResult($"參數驗證失敗: {errorMessage}"));
                }

                var startTime = DateTime.UtcNow;
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                _logger.LogInformation("✅ 資料庫連接成功，開始執行搜索");

                // 1. 記錄搜索關鍵字
                await RecordSearchKeyword(connection, request.Keyword, request.SearchType);

                // 2. 執行搜索
                var countSql = BuildCountSql(request.SearchType);
                var parameters = BuildSearchParameters(request.Keyword, request.SearchType);
                var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

                var sql = BuildSearchSql(request.SearchType);
                var results = await connection.QueryAsync<dynamic>(sql, parameters);
                var searchResults = results.Select(r => new PersonSearchResult
                {
                    Id = r.id,
                    Name = r.name,
                    Gender = r.gender,
                    Birthday = r.birthday?.ToString() ?? "",
                    Nationality = r.nationality,
                    Mobile = r.mobile,
                    Phone = r.phone,
                    IdNumber = r.id_number,
                    PassportNumber = r.passport_number,
                    FamilyRelationships = r.family_relationships,
                    Friends = r.friends,
                    ProfileData = BuildProfileData(r),
                    CreatedAt = r.created_at ?? DateTime.Now,
                    UpdatedAt = r.updated_at ?? DateTime.Now,
                    IsFavorited = false,
                    Source = r.source ?? "檔案上傳",
                    MatchedFields = GetMatchedFields(r, request.Keyword, request.SearchType)
                }).ToList();

                // 3. 檢查收藏狀態
                await CheckFavoriteStatus(connection, searchResults);

                // 4. 獲取熱門關鍵字
                var popularKeywords = await GetPopularKeywords(connection);

                // 5. 獲取搜索歷史
                var searchHistory = await GetSearchHistory(connection);

                // 6. 記錄搜索日誌
                await LogSearchActivity(connection, request, totalCount, GetClientIpAddress(), Request.Headers["User-Agent"].ToString());

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalMilliseconds;

                _logger.LogInformation("📊 準備返回結果: {@searchResults}", searchResults);

                var result = new SearchResult
                {
                    Success = true,
                    Message = $"搜索完成，找到 {totalCount} 筆資料",
                    Data = new SearchData
                    {
                        Keyword = request.Keyword,
                        SearchType = request.SearchType,
                        TotalCount = totalCount,
                        Page = request.Page,
                        PageSize = request.PageSize,
                        Results = searchResults,
                        PopularKeywords = popularKeywords,
                        SearchHistory = searchHistory
                    }
                };

                _logger.LogInformation("✅ 搜索完成: 關鍵字='{keyword}', 找到{count}筆資料, 耗時{duration}ms", 
                    request.Keyword, searchResults.Count, duration);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 全文檢索搜索失敗: 關鍵字='{keyword}', 錯誤={error}", request.Keyword, ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("搜索時發生錯誤，請稍後再試"));
            }
        }

        /// <summary>
        /// 獲取熱門關鍵字
        /// </summary>
        /// <returns>熱門關鍵字列表</returns>
        [HttpGet("popular-keywords")]
        public async Task<IActionResult> GetPopularKeywords()
        {
            _logger.LogInformation("📋 獲取熱門關鍵字請求");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var keywords = await GetPopularKeywords(connection, 10);

                _logger.LogInformation("✅ 成功獲取熱門關鍵字: 數量={count}", keywords.Count);

                return Ok(ApiResponse<List<string>>.SuccessResult(keywords, "獲取熱門關鍵字成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取熱門關鍵字失敗: {error}", ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("獲取熱門關鍵字時發生錯誤"));
            }
        }

        /// <summary>
        /// 獲取搜索歷史
        /// </summary>
        /// <returns>搜索歷史列表</returns>
        [HttpGet("search-history")]
        public async Task<IActionResult> GetSearchHistory()
        {
            _logger.LogInformation("📋 獲取搜索歷史請求");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var history = await GetSearchHistory(connection, 20);

                _logger.LogInformation("✅ 成功獲取搜索歷史: 數量={count}", history.Count);

                return Ok(ApiResponse<List<string>>.SuccessResult(history, "獲取搜索歷史成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取搜索歷史失敗: {error}", ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("獲取搜索歷史時發生錯誤"));
            }
        }

        /// <summary>
        /// 清除搜索歷史
        /// </summary>
        /// <returns>操作結果</returns>
        [HttpDelete("search-history")]
        public async Task<IActionResult> ClearSearchHistory()
        {
            _logger.LogInformation("📋 清除搜索歷史請求");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var deletedCount = await connection.ExecuteAsync("DELETE FROM search_keywords");

                _logger.LogInformation("✅ 成功清除搜索歷史: 刪除{count}筆記錄", deletedCount);

                return Ok(ApiResponse<object>.SuccessResult(new { deletedCount }, $"成功清除 {deletedCount} 筆搜索歷史"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 清除搜索歷史失敗: {error}", ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("清除搜索歷史時發生錯誤"));
            }
        }

        /// <summary>
        /// 獲取搜索統計
        /// </summary>
        /// <returns>搜索統計資料</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetSearchStatistics()
        {
            _logger.LogInformation("📋 獲取搜索統計請求");

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var stats = new SearchStatistics
                {
                    TotalSearches = await connection.QuerySingleAsync<int>("SELECT COALESCE(SUM(search_count), 0) FROM search_keywords"),
                    UniqueKeywords = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM search_keywords"),
                    TotalFavorites = await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM user_favorites"),
                    TopKeywords = await GetTopKeywordsWithDetails(connection, 10),
                    SearchTypeStats = await GetSearchTypeStatistics(connection)
                };

                _logger.LogInformation("✅ 成功獲取搜索統計: 總搜索={totalSearches}, 唯一關鍵字={uniqueKeywords}, 收藏數={totalFavorites}", 
                    stats.TotalSearches, stats.UniqueKeywords, stats.TotalFavorites);

                return Ok(ApiResponse<SearchStatistics>.SuccessResult(stats, "獲取搜索統計成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取搜索統計失敗: {error}", ex.Message);
                return StatusCode(500, ApiResponse<object>.ErrorResult("獲取搜索統計時發生錯誤"));
            }
        }

        /// <summary>
        /// 執行實際的搜索操作
        /// </summary>
        private async Task<(List<PersonSearchResult> Results, int TotalCount)> PerformSearch(NpgsqlConnection connection, SearchRequest request)
        {
            _logger.LogInformation("🔍 開始執行{searchType}搜索: 關鍵字='{keyword}'", 
                request.SearchType == "exact" ? "精準" : "模糊", request.Keyword);

            try
            {
                // 1. 獲取總數
                var countSql = BuildCountSql(request.SearchType);
                var parameters = BuildSearchParameters(request.Keyword, request.SearchType);
                
                _logger.LogInformation("🔍 執行計數SQL查詢: {sql} 參數: {@parameters}", countSql, parameters);
                var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);
                
                // 2. 獲取分頁數據
                var sql = BuildSearchSql(request.SearchType);
                _logger.LogInformation("🔍 執行搜索SQL查詢: {sql} 參數: {@parameters}", sql, parameters);
                
                var results = await connection.QueryAsync<dynamic>(sql, parameters);
                
                _logger.LogInformation("📊 原始查詢結果: {@results}", results);
                
                var personResults = results.Select(r => new PersonSearchResult
                {
                    Id = (int)r.id,
                    Name = (string)r.name,
                    Gender = (string)r.gender,
                    Birthday = r.birthday?.ToString() ?? "",
                    Nationality = (string)r.nationality,
                    Mobile = (string)r.mobile,
                    Phone = (string)r.phone,
                    IdNumber = (string)r.id_number,
                    PassportNumber = (string)r.passport_number,
                    FamilyRelationships = (string)r.family_relationships,
                    Friends = (string)r.friends,
                    ProfileData = BuildProfileData(r),
                    CreatedAt = r.created_at ?? DateTime.Now,
                    UpdatedAt = r.updated_at ?? DateTime.Now,
                    IsFavorited = false,
                    Source = (string)r.source ?? "檔案上傳",
                    MatchedFields = GetMatchedFields(r, request.Keyword, request.SearchType)
                }).ToList();

                _logger.LogInformation("✅ 搜索執行完成: 找到{count}筆結果, 結果: {@personResults}", totalCount, personResults);
                return (Results: personResults, TotalCount: totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 搜索執行失敗: {error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 建立計數 SQL 語句
        /// </summary>
        private string BuildCountSql(string searchType)
        {
            var baseSql = @"
                SELECT COUNT(*)
                FROM person_profile 
                WHERE ";

            if (searchType == "exact")
            {
                return baseSql + @"
                    (name = @keyword OR 
                     mobile = @keyword OR 
                     phone = @keyword OR 
                     id_number = @keyword OR 
                     passport_number = @keyword OR
                     family_relationships = @keyword OR
                     friends = @keyword OR
                     current_employer = @keyword OR
                     education = @keyword OR
                     activities = @keyword OR
                     experience = @keyword OR
                     publications = @keyword OR
                     email = @keyword OR
                     address = @keyword OR
                     birthplace = @keyword OR
                     ethnicity = @keyword OR
                     ancestral_origin = @keyword OR
                     political_party = @keyword OR
                     online_accounts = @keyword OR
                     frequent_locations = @keyword OR
                     travel_history = @keyword OR
                     discovery_process = @keyword OR
                     remarks = @keyword)";
            }
            else
            {
                return baseSql + @"
                    (name ILIKE @fuzzyKeyword OR 
                     mobile ILIKE @fuzzyKeyword OR 
                     phone ILIKE @fuzzyKeyword OR 
                     id_number ILIKE @fuzzyKeyword OR 
                     passport_number ILIKE @fuzzyKeyword OR
                     family_relationships ILIKE @fuzzyKeyword OR
                     friends ILIKE @fuzzyKeyword OR
                     current_employer ILIKE @fuzzyKeyword OR
                     education ILIKE @fuzzyKeyword OR
                     activities ILIKE @fuzzyKeyword OR
                     experience ILIKE @fuzzyKeyword OR
                     publications ILIKE @fuzzyKeyword OR
                     email ILIKE @fuzzyKeyword OR
                     address ILIKE @fuzzyKeyword OR
                     birthplace ILIKE @fuzzyKeyword OR
                     ethnicity ILIKE @fuzzyKeyword OR
                     ancestral_origin ILIKE @fuzzyKeyword OR
                     political_party ILIKE @fuzzyKeyword OR
                     online_accounts ILIKE @fuzzyKeyword OR
                     frequent_locations ILIKE @fuzzyKeyword OR
                     travel_history ILIKE @fuzzyKeyword OR
                     discovery_process ILIKE @fuzzyKeyword OR
                     remarks ILIKE @fuzzyKeyword)";
            }
        }

        /// <summary>
        /// 建立搜索 SQL 語句
        /// </summary>
        private string BuildSearchSql(string searchType)
        {
            var baseSql = @"
                SELECT id, name, gender, birthday, nationality, mobile, phone, 
                       id_number, passport_number, family_relationships, friends, 
                       current_employer, education, activities, experience, publications,
                       email, address, mailing_address, birthplace, ethnicity, 
                       ancestral_origin, political_party, online_accounts, 
                       frequent_locations, travel_history, discovery_process, remarks,
                       file_md5, created_at, updated_at,
                       'person_profile' as source_table, COALESCE(discovery_process, '檔案上傳') as source
                FROM person_profile 
                WHERE ";

            if (searchType == "exact")
            {
                return baseSql + @"
                    (name = @keyword OR 
                     mobile = @keyword OR 
                     phone = @keyword OR 
                     id_number = @keyword OR 
                     passport_number = @keyword OR
                     family_relationships = @keyword OR
                     friends = @keyword OR
                     current_employer = @keyword OR
                     education = @keyword OR
                     activities = @keyword OR
                     experience = @keyword OR
                     publications = @keyword OR
                     email = @keyword OR
                     address = @keyword OR
                     birthplace = @keyword OR
                     ethnicity = @keyword OR
                     ancestral_origin = @keyword OR
                     political_party = @keyword OR
                     online_accounts = @keyword OR
                     frequent_locations = @keyword OR
                     travel_history = @keyword OR
                     discovery_process = @keyword OR
                     remarks = @keyword)
                    ORDER BY name, gender";
            }
            else
            {
                return baseSql + @"
                    (name ILIKE @fuzzyKeyword OR 
                     mobile ILIKE @fuzzyKeyword OR 
                     phone ILIKE @fuzzyKeyword OR 
                     id_number ILIKE @fuzzyKeyword OR 
                     passport_number ILIKE @fuzzyKeyword OR
                     family_relationships ILIKE @fuzzyKeyword OR
                     friends ILIKE @fuzzyKeyword OR
                     current_employer ILIKE @fuzzyKeyword OR
                     education ILIKE @fuzzyKeyword OR
                     activities ILIKE @fuzzyKeyword OR
                     experience ILIKE @fuzzyKeyword OR
                     publications ILIKE @fuzzyKeyword OR
                     email ILIKE @fuzzyKeyword OR
                     address ILIKE @fuzzyKeyword OR
                     birthplace ILIKE @fuzzyKeyword OR
                     ethnicity ILIKE @fuzzyKeyword OR
                     ancestral_origin ILIKE @fuzzyKeyword OR
                     political_party ILIKE @fuzzyKeyword OR
                     online_accounts ILIKE @fuzzyKeyword OR
                     frequent_locations ILIKE @fuzzyKeyword OR
                     travel_history ILIKE @fuzzyKeyword OR
                     discovery_process ILIKE @fuzzyKeyword OR
                     remarks ILIKE @fuzzyKeyword)
                    ORDER BY 
                        CASE WHEN name ILIKE @fuzzyKeyword THEN 1 ELSE 2 END,
                        name, gender";
            }
        }

        /// <summary>
        /// 建立搜索參數
        /// </summary>
        private object BuildSearchParameters(string keyword, string searchType)
        {
            if (searchType == "exact")
            {
                return new { keyword };
            }
            else
            {
                return new { fuzzyKeyword = $"%{keyword}%" };
            }
        }

        /// <summary>
        /// 獲取匹配的欄位
        /// </summary>
        private string GetMatchedFields(dynamic r, string keyword, string searchType)
        {
            var matchedFields = new List<string>();

            bool IsMatch(string? value) => searchType == "exact" 
                ? string.Equals(value, keyword, StringComparison.OrdinalIgnoreCase)
                : !string.IsNullOrEmpty(value) && value.Contains(keyword, StringComparison.OrdinalIgnoreCase);

            if (IsMatch(r.name)) matchedFields.Add("姓名");
            if (IsMatch(r.mobile)) matchedFields.Add("手機");
            if (IsMatch(r.phone)) matchedFields.Add("電話");
            if (IsMatch(r.id_number)) matchedFields.Add("身分證號");
            if (IsMatch(r.passport_number)) matchedFields.Add("護照號碼");
            if (IsMatch(r.family_relationships)) matchedFields.Add("家庭關係");
            if (IsMatch(r.friends)) matchedFields.Add("朋友關係");
            if (IsMatch(r.current_employer)) matchedFields.Add("工作單位");
            if (IsMatch(r.education)) matchedFields.Add("教育背景");
            if (IsMatch(r.activities)) matchedFields.Add("活動");
            if (IsMatch(r.experience)) matchedFields.Add("經歷");
            if (IsMatch(r.publications)) matchedFields.Add("出版物");
            if (IsMatch(r.email)) matchedFields.Add("電子郵件");
            if (IsMatch(r.address)) matchedFields.Add("地址");
            if (IsMatch(r.mailing_address)) matchedFields.Add("郵寄地址");
            if (IsMatch(r.birthplace)) matchedFields.Add("出生地");
            if (IsMatch(r.ethnicity)) matchedFields.Add("民族");
            if (IsMatch(r.ancestral_origin)) matchedFields.Add("祖籍");
            if (IsMatch(r.political_party)) matchedFields.Add("政治黨派");
            if (IsMatch(r.online_accounts)) matchedFields.Add("線上帳號");
            if (IsMatch(r.frequent_locations)) matchedFields.Add("常訪地點");
            if (IsMatch(r.travel_history)) matchedFields.Add("旅行歷史");
            if (IsMatch(r.discovery_process)) matchedFields.Add("發現過程");
            if (IsMatch(r.remarks)) matchedFields.Add("備註");

            return string.Join(", ", matchedFields);
        }

        /// <summary>
        /// 記錄搜索關鍵字
        /// </summary>
        private async Task RecordSearchKeyword(NpgsqlConnection connection, string keyword, string searchType)
        {
            _logger.LogInformation("📝 記錄搜索關鍵字: '{keyword}', 類型={searchType}", keyword, searchType);

            try
            {
                var sql = @"
                    INSERT INTO search_keywords (keyword, search_count, search_type, last_search_time, created_at, updated_at)
                    VALUES (@keyword, 1, @searchType, @now, @now, @now)
                    ON CONFLICT (keyword) 
                    DO UPDATE SET 
                        search_count = search_keywords.search_count + 1,
                        search_type = @searchType,
                        last_search_time = @now,
                        updated_at = @now";

                var now = DateTime.UtcNow;
                await connection.ExecuteAsync(sql, new { keyword, searchType, now });

                _logger.LogInformation("✅ 搜索關鍵字記錄成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 記錄搜索關鍵字失敗: {error}", ex.Message);
                // 不拋出異常，避免影響主要搜索功能
            }
        }

        /// <summary>
        /// 檢查收藏狀態
        /// </summary>
        private async Task CheckFavoriteStatus(NpgsqlConnection connection, List<PersonSearchResult> results)
        {
            if (results.Count == 0) return;

            _logger.LogInformation("💖 檢查{count}筆結果的收藏狀態", results.Count);

            try
            {
                var personIds = results.Select(r => r.Id).ToArray();
                var favoritedIds = await connection.QueryAsync<int>(
                    "SELECT person_id FROM user_favorites WHERE person_id = ANY(@personIds)",
                    new { personIds });

                var favoritedSet = new HashSet<int>(favoritedIds);

                foreach (var result in results)
                {
                    result.IsFavorited = favoritedSet.Contains(result.Id);
                }

                _logger.LogInformation("✅ 收藏狀態檢查完成: {favoritedCount}筆已收藏", favoritedSet.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 檢查收藏狀態失敗: {error}", ex.Message);
                // 不拋出異常，收藏狀態不影響主要功能
            }
        }

        /// <summary>
        /// 獲取熱門關鍵字
        /// </summary>
        private async Task<List<string>> GetPopularKeywords(NpgsqlConnection connection, int limit = 10)
        {
            try
            {
                var keywords = await connection.QueryAsync<string>(
                    "SELECT keyword FROM search_keywords ORDER BY search_count DESC, last_search_time DESC LIMIT @limit",
                    new { limit });

                return keywords.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取熱門關鍵字失敗: {error}", ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// 獲取搜索歷史
        /// </summary>
        private async Task<List<string>> GetSearchHistory(NpgsqlConnection connection, int limit = 20)
        {
            try
            {
                var history = await connection.QueryAsync<string>(
                    "SELECT keyword FROM search_keywords ORDER BY last_search_time DESC LIMIT @limit",
                    new { limit });

                return history.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取搜索歷史失敗: {error}", ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// 記錄搜索活動
        /// </summary>
        private async Task LogSearchActivity(NpgsqlConnection connection, SearchRequest request, int resultCount, string ipAddress, string userAgent)
        {
            try
            {
                var sql = @"
                    INSERT INTO search_logs (keyword, search_type, result_count, search_time, ip_address, user_agent)
                    VALUES (@keyword, @searchType, @resultCount, @searchTime, @ipAddress, @userAgent)";

                await connection.ExecuteAsync(sql, new
                {
                    keyword = request.Keyword,
                    searchType = request.SearchType,
                    resultCount,
                    searchTime = DateTime.UtcNow,
                    ipAddress,
                    userAgent
                });

                _logger.LogInformation("📊 搜索活動記錄成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 記錄搜索活動失敗: {error}", ex.Message);
                // 不拋出異常，避免影響主要功能
            }
        }

        /// <summary>
        /// 獲取詳細的熱門關鍵字資訊
        /// </summary>
        private async Task<List<PopularKeyword>> GetTopKeywordsWithDetails(NpgsqlConnection connection, int limit)
        {
            try
            {
                var sql = @"
                    SELECT keyword, search_count, last_search_time,
                           CASE 
                               WHEN search_count >= 10 THEN '熱門'
                               WHEN search_count >= 5 THEN '常用'
                               ELSE '一般'
                           END as popularity_level
                    FROM search_keywords 
                    ORDER BY search_count DESC, last_search_time DESC 
                    LIMIT @limit";

                var results = await connection.QueryAsync<PopularKeyword>(sql, new { limit });
                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取詳細熱門關鍵字失敗: {error}", ex.Message);
                return new List<PopularKeyword>();
            }
        }

        /// <summary>
        /// 獲取搜索類型統計
        /// </summary>
        private async Task<Dictionary<string, int>> GetSearchTypeStatistics(NpgsqlConnection connection)
        {
            try
            {
                var sql = @"
                    SELECT search_type, SUM(search_count) as total_count
                    FROM search_keywords 
                    GROUP BY search_type";

                var results = await connection.QueryAsync<dynamic>(sql);
                return results.ToDictionary(
                    r => (string)r.search_type,
                    r => (int)r.total_count
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 獲取搜索類型統計失敗: {error}", ex.Message);
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// 獲取客戶端IP地址
        /// </summary>
        private string GetClientIpAddress()
        {
            return Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                   Request.Headers["X-Real-IP"].FirstOrDefault() ??
                   HttpContext.Connection.RemoteIpAddress?.ToString() ??
                   "unknown";
        }

        /// <summary>
        /// 構建個人資料字符串
        /// </summary>
        private string BuildProfileData(dynamic r)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(r.current_employer)) sb.AppendLine($"工作單位: {r.current_employer}");
            if (!string.IsNullOrEmpty(r.education)) sb.AppendLine($"教育背景: {r.education}");
            if (!string.IsNullOrEmpty(r.activities)) sb.AppendLine($"活動: {r.activities}");
            if (!string.IsNullOrEmpty(r.experience)) sb.AppendLine($"經歷: {r.experience}");
            if (!string.IsNullOrEmpty(r.publications)) sb.AppendLine($"出版物: {r.publications}");
            if (!string.IsNullOrEmpty(r.email)) sb.AppendLine($"電子郵件: {r.email}");
            if (!string.IsNullOrEmpty(r.address)) sb.AppendLine($"地址: {r.address}");
            if (!string.IsNullOrEmpty(r.mailing_address)) sb.AppendLine($"郵寄地址: {r.mailing_address}");
            if (!string.IsNullOrEmpty(r.birthplace)) sb.AppendLine($"出生地: {r.birthplace}");
            if (!string.IsNullOrEmpty(r.ethnicity)) sb.AppendLine($"民族: {r.ethnicity}");
            if (!string.IsNullOrEmpty(r.ancestral_origin)) sb.AppendLine($"祖籍: {r.ancestral_origin}");
            if (!string.IsNullOrEmpty(r.political_party)) sb.AppendLine($"政治黨派: {r.political_party}");
            if (!string.IsNullOrEmpty(r.online_accounts)) sb.AppendLine($"線上帳號: {r.online_accounts}");
            if (!string.IsNullOrEmpty(r.frequent_locations)) sb.AppendLine($"常訪地點: {r.frequent_locations}");
            if (!string.IsNullOrEmpty(r.travel_history)) sb.AppendLine($"旅行歷史: {r.travel_history}");
            if (!string.IsNullOrEmpty(r.discovery_process)) sb.AppendLine($"發現過程: {r.discovery_process}");
            if (!string.IsNullOrEmpty(r.remarks)) sb.AppendLine($"備註: {r.remarks}");
            return sb.ToString().TrimEnd();
        }
    }
} 