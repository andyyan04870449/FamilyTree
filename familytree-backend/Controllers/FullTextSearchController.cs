// 全文檢索控制器：提供全文搜索、關鍵字管理、搜索歷史等功能
// 主要功能：關鍵字搜索（精準/模糊）、搜索歷史管理、熱門關鍵字統計
// 重要更新：實現專案隔離，確保搜索結果僅限於當前專案

using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using familytree_backend.Models;
using familytree_backend.Constants;
using familytree_backend.Services;
using System.Text;

namespace familytree_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FullTextSearchController : BaseController
    {
        private readonly string _connectionString;
        private readonly SearchConfiguration _searchConfig;

        /// <summary>
        /// 全文檢索控制器建構子
        /// 設計改善：繼承 BaseController，使用配置服務管理設定
        /// </summary>
        public FullTextSearchController(
            ILogger<FullTextSearchController> logger,
            IConfigurationService configurationService) 
            : base(logger, configurationService)
        {
            _connectionString = configurationService.GetConnectionString();
            _searchConfig = configurationService.GetSearchConfiguration();
        }

        /// <summary>
        /// 全文檢索搜索
        /// 重大更新：實現專案隔離，確保只搜索當前專案的資料
        /// </summary>
        /// <param name="request">搜索請求</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>搜索結果</returns>
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request, [FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("全文檢索搜索", new { 
                    Keyword = request.Keyword, 
                    SearchType = request.SearchType, 
                    Page = request.Page, 
                    PageSize = request.PageSize, 
                    ProjectId = project_id 
                });

                // 步驟 1：驗證專案 ID（專案隔離的關鍵）
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：參數驗證
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    var errorMessage = string.Join("; ", errors);
                    Logger.LogWarning("請求參數驗證失敗: {errors}", errorMessage);
                    return CreateErrorResponse($"參數驗證失敗: {errorMessage}");
                }

                var startTime = DateTime.UtcNow;
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                Logger.LogInformation("資料庫連接成功，開始執行搜索");

                // 步驟 3：記錄搜索關鍵字（按專案記錄）
                await RecordSearchKeyword(connection, request.Keyword, request.SearchType, project_id);

                // 步驟 4：執行專案隔離的搜索
                var countSql = BuildCountSql(request.SearchType, project_id);
                var parameters = BuildSearchParameters(request.Keyword, request.SearchType, project_id);
                var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

                var sql = BuildSearchSql(request.SearchType, project_id);
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
                    CreatedAt = DateTime.TryParse(r.created_at?.ToString(), out DateTime createdDate) ? createdDate : DateTime.Now,
                    UpdatedAt = DateTime.TryParse(r.updated_at?.ToString(), out DateTime updatedDate) ? updatedDate : DateTime.Now,
                    IsFavorited = false,
                    Source = r.source ?? "檔案上傳",
                    MatchedFields = GetMatchedFields(r, request.Keyword, request.SearchType)
                }).ToList();

                // 步驟 5：檢查收藏狀態（按專案過濾）
                await CheckFavoriteStatus(connection, searchResults, project_id);

                // 步驟 6：獲取專案相關的熱門關鍵字和搜索歷史
                var popularKeywords = await GetPopularKeywords(connection, project_id, _searchConfig.MaxPopularKeywords);
                var searchHistory = await GetSearchHistory(connection, project_id, _searchConfig.MaxSearchHistoryItems);

                // 步驟 7：記錄搜索活動
                await LogSearchActivity(connection, request, totalCount, GetClientIpAddress(), Request.Headers["User-Agent"].ToString(), project_id);

                var endTime = DateTime.UtcNow;
                var duration = (endTime - startTime).TotalMilliseconds;

                Logger.LogInformation("準備返回結果: {@searchResults}", searchResults);

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

                Logger.LogInformation("搜索完成: 關鍵字='{keyword}', 專案={projectId}, 找到{count}筆資料, 耗時{duration}ms", 
                    request.Keyword, project_id, searchResults.Count, duration);

                LogRequestComplete("全文檢索搜索");
                return CreateSuccessResponse(result.Data, result.Message);

            }, "全文檢索搜索");
        }

        /// <summary>
        /// 獲取熱門關鍵字（按專案過濾）
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>熱門關鍵字列表</returns>
        [HttpGet("popular-keywords")]
        public async Task<IActionResult> GetPopularKeywords([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取熱門關鍵字", new { ProjectId = project_id });

                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var keywords = await GetPopularKeywords(connection, project_id, _searchConfig.MaxPopularKeywords);

                Logger.LogInformation("成功獲取熱門關鍵字: 專案={projectId}, 數量={count}", project_id, keywords.Count);

                LogRequestComplete("獲取熱門關鍵字");
                return CreateSuccessResponse(keywords, "獲取熱門關鍵字成功");

            }, "獲取熱門關鍵字");
        }

        /// <summary>
        /// 獲取搜索歷史（按專案過濾）
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>搜索歷史列表</returns>
        [HttpGet("search-history")]
        public async Task<IActionResult> GetSearchHistory([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取搜索歷史", new { ProjectId = project_id });

                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var history = await GetSearchHistory(connection, project_id, _searchConfig.MaxSearchHistoryItems);

                Logger.LogInformation("成功獲取搜索歷史: 專案={projectId}, 數量={count}", project_id, history.Count);

                LogRequestComplete("獲取搜索歷史");
                return CreateSuccessResponse(history, "獲取搜索歷史成功");

            }, "獲取搜索歷史");
        }

        /// <summary>
        /// 清除搜索歷史（按專案過濾）
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>操作結果</returns>
        [HttpDelete("search-history")]
        public async Task<IActionResult> ClearSearchHistory([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("清除搜索歷史", new { ProjectId = project_id });

                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var deletedCount = await connection.ExecuteAsync(
                    "DELETE FROM search_keywords WHERE project_id = @project_id", 
                    new { project_id });

                Logger.LogInformation("成功清除搜索歷史: 專案={projectId}, 刪除{count}筆記錄", project_id, deletedCount);

                LogRequestComplete("清除搜索歷史");
                return CreateSuccessResponse(new { deletedCount }, $"成功清除 {deletedCount} 筆搜索歷史");

            }, "清除搜索歷史");
        }

        /// <summary>
        /// 獲取搜索統計（按專案過濾）
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>搜索統計資料</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetSearchStatistics([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取搜索統計", new { ProjectId = project_id });

                // 驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var stats = new SearchStatistics
                {
                    TotalSearches = await connection.QuerySingleAsync<int>(
                        "SELECT COALESCE(SUM(search_count), 0) FROM search_keywords WHERE project_id = @project_id", 
                        new { project_id }),
                    UniqueKeywords = await connection.QuerySingleAsync<int>(
                        "SELECT COUNT(*) FROM search_keywords WHERE project_id = @project_id", 
                        new { project_id }),
                    TotalFavorites = await connection.QuerySingleAsync<int>(
                        "SELECT COUNT(*) FROM user_favorites WHERE project_id = @project_id", 
                        new { project_id }),
                    TopKeywords = await GetTopKeywordsWithDetails(connection, project_id, _searchConfig.MaxTopKeywords),
                    SearchTypeStats = await GetSearchTypeStatistics(connection, project_id)
                };

                Logger.LogInformation("成功獲取搜索統計: 專案={projectId}, 總搜索={totalSearches}, 唯一關鍵字={uniqueKeywords}, 收藏數={totalFavorites}", 
                    project_id, stats.TotalSearches, stats.UniqueKeywords, stats.TotalFavorites);

                LogRequestComplete("獲取搜索統計");
                return CreateSuccessResponse(stats, "獲取搜索統計成功");

            }, "獲取搜索統計");
        }

        #region 私有輔助方法

        /// <summary>
        /// 建立計數 SQL 語句（包含專案隔離）
        /// </summary>
        private string BuildCountSql(string searchType, string projectId)
        {
            var baseSql = @"
                SELECT COUNT(*)
                FROM person_profile 
                WHERE project_id = @project_id AND ";

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
                     discovery_source = @keyword OR
                     important_friends = @keyword OR
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
                     discovery_source ILIKE @fuzzyKeyword OR
                     important_friends ILIKE @fuzzyKeyword OR
                     remarks ILIKE @fuzzyKeyword)";
            }
        }

        /// <summary>
        /// 建立搜索 SQL 語句（包含專案隔離）
        /// </summary>
        private string BuildSearchSql(string searchType, string projectId)
        {
            var baseSql = @"
                SELECT id, name, gender, birthday, nationality, mobile, phone, 
                       id_number, passport_number, family_relationships, friends, 
                       current_employer, education, activities, experience, publications,
                       email, address, mailing_address, birthplace, ethnicity, 
                       ancestral_origin, political_party, online_accounts, 
                       frequent_locations, travel_history, discovery_source, remarks,
                       file_md5, created_at, updated_at,
                       'person_profile' as source_table, COALESCE(discovery_source, '檔案上傳') as source
                FROM person_profile 
                WHERE project_id = @project_id AND ";

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
                     discovery_source = @keyword OR
                     important_friends = @keyword OR
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
                     discovery_source ILIKE @fuzzyKeyword OR
                     important_friends ILIKE @fuzzyKeyword OR
                     remarks ILIKE @fuzzyKeyword)
                    ORDER BY 
                        CASE WHEN name ILIKE @fuzzyKeyword THEN 1 ELSE 2 END,
                        name, gender";
            }
        }

        /// <summary>
        /// 建立搜索參數（包含專案 ID）
        /// </summary>
        private object BuildSearchParameters(string keyword, string searchType, string projectId)
        {
            if (searchType == "exact")
            {
                return new { keyword, project_id = projectId };
            }
            else
            {
                return new { fuzzyKeyword = $"%{keyword}%", project_id = projectId };
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
            if (IsMatch(r.discovery_source)) matchedFields.Add("發現過程");
            if (IsMatch(r.remarks)) matchedFields.Add("備註");

            return string.Join(", ", matchedFields);
        }

        /// <summary>
        /// 記錄搜索關鍵字（按專案記錄）
        /// </summary>
        private async Task RecordSearchKeyword(NpgsqlConnection connection, string keyword, string searchType, string projectId)
        {
            Logger.LogInformation("記錄搜索關鍵字: '{keyword}', 類型={searchType}, 專案={projectId}", keyword, searchType, projectId);

            try
            {
                var sql = @"
                    INSERT INTO search_keywords (keyword, search_count, search_type, last_search_time, created_at, updated_at, project_id)
                    VALUES (@keyword, 1, @searchType, @now, @now, @now, @project_id)
                    ON CONFLICT (keyword, project_id) 
                    DO UPDATE SET 
                        search_count = search_keywords.search_count + 1,
                        search_type = @searchType,
                        last_search_time = @now,
                        updated_at = @now";

                var now = DateTime.UtcNow;
                await connection.ExecuteAsync(sql, new { keyword, searchType, now, project_id = projectId });

                Logger.LogInformation("搜索關鍵字記錄成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "記錄搜索關鍵字失敗: {error}", ex.Message);
                // 不拋出異常，避免影響主要搜索功能
            }
        }

        /// <summary>
        /// 檢查收藏狀態（按專案過濾）
        /// </summary>
        private async Task CheckFavoriteStatus(NpgsqlConnection connection, List<PersonSearchResult> results, string projectId)
        {
            if (results.Count == 0) return;

            Logger.LogInformation("檢查{count}筆結果的收藏狀態", results.Count);

            try
            {
                var personIds = results.Select(r => r.Id).ToArray();
                var favoritedIds = await connection.QueryAsync<int>(
                    "SELECT person_id FROM user_favorites WHERE person_id = ANY(@personIds) AND project_id = @project_id",
                    new { personIds, project_id = projectId });

                var favoritedSet = new HashSet<int>(favoritedIds);

                foreach (var result in results)
                {
                    result.IsFavorited = favoritedSet.Contains(result.Id);
                }

                Logger.LogInformation("收藏狀態檢查完成: {favoritedCount}筆已收藏", favoritedSet.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "檢查收藏狀態失敗: {error}", ex.Message);
                // 不拋出異常，收藏狀態不影響主要功能
            }
        }

        /// <summary>
        /// 獲取熱門關鍵字（按專案過濾）
        /// </summary>
        private async Task<List<string>> GetPopularKeywords(NpgsqlConnection connection, string projectId, int limit = 10)
        {
            try
            {
                var keywords = await connection.QueryAsync<string>(
                    "SELECT keyword FROM search_keywords WHERE project_id = @project_id ORDER BY search_count DESC, last_search_time DESC LIMIT @limit",
                    new { project_id = projectId, limit });

                return keywords.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "獲取熱門關鍵字失敗: {error}", ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// 獲取搜索歷史（按專案過濾）
        /// </summary>
        private async Task<List<string>> GetSearchHistory(NpgsqlConnection connection, string projectId, int limit = 20)
        {
            try
            {
                var history = await connection.QueryAsync<string>(
                    "SELECT keyword FROM search_keywords WHERE project_id = @project_id ORDER BY last_search_time DESC LIMIT @limit",
                    new { project_id = projectId, limit });

                return history.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "獲取搜索歷史失敗: {error}", ex.Message);
                return new List<string>();
            }
        }

        /// <summary>
        /// 記錄搜索活動（按專案記錄）
        /// </summary>
        private async Task LogSearchActivity(NpgsqlConnection connection, SearchRequest request, int resultCount, string ipAddress, string userAgent, string projectId)
        {
            try
            {
                var sql = @"
                    INSERT INTO search_logs (keyword, search_type, result_count, search_time, ip_address, user_agent, project_id)
                    VALUES (@keyword, @searchType, @resultCount, @searchTime, @ipAddress, @userAgent, @project_id)";

                await connection.ExecuteAsync(sql, new
                {
                    keyword = request.Keyword,
                    searchType = request.SearchType,
                    resultCount,
                    searchTime = DateTime.UtcNow,
                    ipAddress,
                    userAgent,
                    project_id = projectId
                });

                Logger.LogInformation("搜索活動記錄成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "記錄搜索活動失敗: {error}", ex.Message);
                // 不拋出異常，避免影響主要功能
            }
        }

        /// <summary>
        /// 獲取詳細的熱門關鍵字資訊（按專案過濾）
        /// </summary>
        private async Task<List<PopularKeyword>> GetTopKeywordsWithDetails(NpgsqlConnection connection, string projectId, int limit)
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
                    WHERE project_id = @project_id
                    ORDER BY search_count DESC, last_search_time DESC 
                    LIMIT @limit";

                var results = await connection.QueryAsync<PopularKeyword>(sql, new { project_id = projectId, limit });
                return results.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "獲取詳細熱門關鍵字失敗: {error}", ex.Message);
                return new List<PopularKeyword>();
            }
        }

        /// <summary>
        /// 獲取搜索類型統計（按專案過濾）
        /// </summary>
        private async Task<Dictionary<string, int>> GetSearchTypeStatistics(NpgsqlConnection connection, string projectId)
        {
            try
            {
                var sql = @"
                    SELECT search_type, SUM(search_count) as total_count
                    FROM search_keywords 
                    WHERE project_id = @project_id
                    GROUP BY search_type";

                var results = await connection.QueryAsync<dynamic>(sql, new { project_id = projectId });
                return results.ToDictionary(
                    r => (string)r.search_type,
                    r => (int)r.total_count
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "獲取搜索類型統計失敗: {error}", ex.Message);
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
            if (!string.IsNullOrEmpty(r.discovery_source)) sb.AppendLine($"發現過程: {r.discovery_source}");
            if (!string.IsNullOrEmpty(r.remarks)) sb.AppendLine($"備註: {r.remarks}");
            return sb.ToString().TrimEnd();
        }

        #endregion
    }
} 