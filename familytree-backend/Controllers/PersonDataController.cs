// 人員資料控制器 - 提供人員資料的CRUD操作和分頁查詢
// 優化重點：移除硬編碼、統一回應格式、改善 OOP 設計、使用配置管理
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using familytree_backend.Models;
using familytree_backend.Services;
using Dapper;
using Npgsql;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 人員資料控制器
    /// 職責：提供人員資料的進階查詢、分頁、搜尋等功能
    /// 設計改善：繼承 BaseController，使用配置管理，移除硬編碼的分頁設定
    /// </summary>
    [Route("api/[controller]")]
    public class PersonDataController : BaseController
    {
        private readonly string _connectionString;
        private readonly PaginationConfiguration _paginationConfig;

        /// <summary>
        /// 人員資料控制器建構子
        /// 設計改善：透過配置服務統一管理設定，避免硬編碼
        /// </summary>
        public PersonDataController(
            ILogger<PersonDataController> logger,
            IConfigurationService configurationService) 
            : base(logger, configurationService)
        {
            _connectionString = configurationService.GetConnectionString();
            _paginationConfig = configurationService.GetPaginationConfiguration();
        }

        /// <summary>
        /// 獲取人員資料列表 API（分頁查詢）
        /// 設計改善：移除硬編碼的分頁限制，使用配置管理，統一回應格式
        /// </summary>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">頁面大小</param>
        /// <param name="project_id">專案 ID</param>
        /// <param name="keyword">搜尋關鍵字</param>
        /// <param name="sortBy">排序欄位</param>
        /// <param name="sortOrder">排序方向（asc/desc）</param>
        /// <returns>分頁人員資料列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetPersonDataList(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 0, 
            [FromQuery] string? project_id = null,
            [FromQuery] string? keyword = null,
            [FromQuery] string? sortBy = "id",
            [FromQuery] string? sortOrder = "desc")
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取人員資料列表", new { 
                    Page = page, PageSize = pageSize, ProjectId = project_id, 
                    Keyword = keyword, SortBy = sortBy, SortOrder = sortOrder 
                });

                // 步驟 1：驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：正規化分頁參數（使用配置而非硬編碼）
                if (pageSize <= 0) pageSize = _paginationConfig.DefaultPageSize;
                var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);

                // 步驟 3：建立資料庫連接
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 步驟 4：建構查詢條件
                var (whereClause, parameters) = BuildSearchConditions(project_id, keyword);

                // 步驟 5：查詢總筆數
                var countSql = $"SELECT COUNT(*) FROM person_profile WHERE {whereClause}";
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                Logger.LogInformation("人員資料查詢統計：專案 {ProjectId}，總筆數 {TotalCount}，查詢關鍵字 '{Keyword}'", 
                    project_id, totalCount, keyword ?? "無");

                // 步驟 6：查詢分頁資料
                var offset = (normalizedPage - 1) * normalizedPageSize;
                var dataSql = BuildPersonDataSelectQuery(whereClause, sortBy, sortOrder, offset, normalizedPageSize);
                
                var personDataList = await connection.QueryAsync<PersonDataModel>(dataSql, parameters);

                // 步驟 7：建立回應資料
                var response = new PersonDataListResponse
                {
                    Success = true,
                    Message = ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully,
                    PersonDataList = personDataList.ToList(),
                    TotalCount = totalCount,
                    PageNumber = normalizedPage,
                    PageSize = normalizedPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / normalizedPageSize)
                };

                LogRequestComplete("獲取人員資料列表", personDataList.Count());
                return CreatePagedResponse(response.PersonDataList, totalCount, normalizedPage, normalizedPageSize);

            }, "獲取人員資料列表");
        }

        /// <summary>
        /// 測試端點
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "PersonData API is working", timestamp = DateTime.Now });
        }

        /// <summary>
        /// 修復專案人員資料的project_id欄位
        /// </summary>
        [HttpGet("repair/{project_id}")]
        public async Task<IActionResult> RepairProjectIds(string project_id)
        {
            try
            {
                Logger.LogInformation($"開始修復專案 {project_id} 的人員資料project_id欄位");

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 更新所有project_id為null的記錄
                var updateSql = @"
                    UPDATE person_profile 
                    SET project_id = @project_id, updated_at = CURRENT_TIMESTAMP 
                    WHERE project_id IS NULL";

                var affectedRows = await connection.ExecuteAsync(updateSql, new { project_id });

                Logger.LogInformation($"成功修復 {affectedRows} 筆人員資料的project_id欄位");

                return Ok(new { 
                    success = true,
                    affectedRows = affectedRows,
                    projectId = project_id,
                    message = $"成功修復 {affectedRows} 筆記錄"
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"修復專案ID時發生錯誤: {project_id}");
                return StatusCode(500, new { 
                    success = false, 
                    message = $"修復失敗: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// 人員資料搜尋 API
        /// 設計改善：支援多欄位搜尋，使用配置管理搜尋參數
        /// </summary>
        /// <param name="query">搜尋關鍵字</param>
        /// <param name="project_id">專案 ID</param>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">頁面大小</param>
        /// <returns>搜尋結果</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchPersonData(
            [FromQuery] string query,
            [FromQuery] string? project_id = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 0)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("人員資料搜尋", new { Query = query, ProjectId = project_id, Page = page, PageSize = pageSize });

                // 步驟 1：驗證搜尋參數
                if (string.IsNullOrWhiteSpace(query))
                {
                    return CreateErrorResponse("搜尋關鍵字不能為空");
                }

                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：正規化分頁參數
                if (pageSize <= 0) pageSize = _paginationConfig.DefaultPageSize;
                var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);

                // 步驟 3：執行搜尋
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var (whereClause, parameters) = BuildAdvancedSearchConditions(project_id, query);

                // 查詢總筆數
                var countSql = $"SELECT COUNT(*) FROM person_profile WHERE {whereClause}";
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // 查詢分頁資料（搜尋結果按相關性排序）
                var offset = (normalizedPage - 1) * normalizedPageSize;
                var dataSql = BuildPersonDataSelectQuery(whereClause, "id", "desc", offset, normalizedPageSize);
                
                var searchResults = await connection.QueryAsync<PersonDataModel>(dataSql, parameters);

                Logger.LogInformation("人員資料搜尋完成：關鍵字 '{Query}'，專案 {ProjectId}，找到 {TotalCount} 筆結果", 
                    query, project_id, totalCount);

                LogRequestComplete("人員資料搜尋", searchResults.Count());
                return CreatePagedResponse(searchResults, totalCount, normalizedPage, normalizedPageSize);

            }, "人員資料搜尋");
        }

        /// <summary>
        /// 獲取人員資料統計資訊 API
        /// 設計理念：提供專案內人員資料的詳細統計
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>統計資訊</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetPersonDataStatistics([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取人員資料統計", new { ProjectId = project_id });

                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var statisticsSql = @"
                    SELECT 
                        COUNT(*) as TotalCount,
                        COUNT(CASE WHEN TRIM(gender) = '男' THEN 1 END) as MaleCount,
                        COUNT(CASE WHEN TRIM(gender) = '女' THEN 1 END) as FemaleCount,
                        COUNT(CASE WHEN birthday IS NOT NULL AND birthday != '' THEN 1 END) as HasBirthdayCount,
                        COUNT(CASE WHEN mobile IS NOT NULL AND mobile != '' THEN 1 END) as HasMobileCount,
                        COUNT(CASE WHEN email IS NOT NULL AND email != '' THEN 1 END) as HasEmailCount,
                        COUNT(CASE WHEN id_number IS NOT NULL AND id_number != '' THEN 1 END) as HasIdNumberCount,
                        COUNT(CASE WHEN current_employer IS NOT NULL AND current_employer != '' THEN 1 END) as HasWorkplaceCount,
                        COUNT(DISTINCT nationality) as NationalityVarietyCount,
                        COUNT(DISTINCT ethnicity) as EthnicityVarietyCount
                    FROM person_profile 
                    WHERE project_id = @project_id";

                var stats = await connection.QueryFirstAsync(statisticsSql, new { project_id });

                LogRequestComplete("獲取人員資料統計");
                return CreateSuccessResponse(stats, ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully);

            }, "獲取人員資料統計");
        }

        #region 私有輔助方法

        /// <summary>
        /// 建構人員資料查詢 SQL
        /// 設計理念：統一 SQL 建構邏輯，支援排序和分頁
        /// </summary>
        private static string BuildPersonDataSelectQuery(
            string? whereClause = null, 
            string? sortBy = null, 
            string? sortOrder = null,
            int? offset = null, 
            int? limit = null)
        {
            // 基本查詢語句 - 包含所有必要欄位的映射
            var sql = @"
                SELECT 
                    id, file_md5, photo_index as photo, name, discovery_source as discovery_process, 
                    gender, birthday, birthplace, nationality, ethnicity, ancestral_origin as ancestral_home, 
                    political_party, id_number, passport_number, phone, mobile, email, 
                    current_employer as current_workplace, address as current_address, 
                    mailing_address, experience, education, online_accounts, publications, 
                    activities, frequent_locations as frequent_places, travel_history as travel_records, 
                    family_relationships, friends as important_friends, remarks as notes, 
                    extra_data as profiledata, created_at, updated_at, project_id
                FROM person_profile";

            // 加入 WHERE 條件
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            // 加入排序（使用白名單避免 SQL 注入）
            var allowedSortFields = new[] { "id", "name", "birthday", "created_at", "updated_at" };
            var sortByLower = sortBy?.ToLowerInvariant();
            var normalizedSortBy = !string.IsNullOrEmpty(sortByLower) && allowedSortFields.Contains(sortByLower) 
                ? sortByLower 
                : "id";
            
            var normalizedSortOrder = sortOrder?.ToLowerInvariant() == "asc" ? "ASC" : "DESC";
            sql += $" ORDER BY {normalizedSortBy} {normalizedSortOrder}";

            // 加入分頁
            if (limit.HasValue)
            {
                sql += $" LIMIT {limit}";
            }

            if (offset.HasValue)
            {
                sql += $" OFFSET {offset}";
            }

            return sql;
        }

        /// <summary>
        /// 建構基本搜尋條件
        /// 設計理念：集中查詢條件建構邏輯，支援專案隔離和關鍵字搜尋
        /// </summary>
        private static (string whereClause, DynamicParameters parameters) BuildSearchConditions(
            string? projectId, 
            string? keyword)
        {
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            // 專案隔離條件（必要）
            conditions.Add("project_id = @project_id");
            parameters.Add("@project_id", projectId);

            // 關鍵字搜尋條件（可選）
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                conditions.Add(@"(
                    name ILIKE @keyword OR 
                    id_number ILIKE @keyword OR 
                    mobile ILIKE @keyword OR 
                    email ILIKE @keyword
                )");
                parameters.Add("@keyword", $"%{keyword}%");
            }

            return (string.Join(" AND ", conditions), parameters);
        }

        /// <summary>
        /// 建構進階搜尋條件
        /// 設計理念：支援多欄位的全文搜尋功能
        /// </summary>
        private static (string whereClause, DynamicParameters parameters) BuildAdvancedSearchConditions(
            string? projectId, 
            string keyword)
        {
            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            // 專案隔離條件
            conditions.Add("project_id = @project_id");
            parameters.Add("@project_id", projectId);

            // 進階關鍵字搜尋 - 涵蓋更多欄位
            conditions.Add(@"(
                name ILIKE @keyword OR 
                id_number ILIKE @keyword OR 
                passport_number ILIKE @keyword OR 
                mobile ILIKE @keyword OR 
                phone ILIKE @keyword OR 
                email ILIKE @keyword OR 
                current_employer ILIKE @keyword OR 
                address ILIKE @keyword OR 
                birthplace ILIKE @keyword OR 
                nationality ILIKE @keyword OR 
                ethnicity ILIKE @keyword OR 
                remarks ILIKE @keyword
            )");
            parameters.Add("@keyword", $"%{keyword}%");

            return (string.Join(" AND ", conditions), parameters);
        }

        #endregion

        #region CRUD 操作 (從 PersonController 合併)

        /// <summary>
        /// 獲取單一人員資料 API
        /// 設計改善：加入詳細錯誤處理，專案隔離驗證
        /// </summary>
        /// <param name="id">人員 ID</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>人員資料</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerson(int id, [FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取單一人員資料", new { Id = id, ProjectId = project_id });

                // 步驟 1：驗證參數
                if (id <= 0)
                {
                    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.InvalidParameters);
                }

                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：查詢資料
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = BuildPersonSelectQuery("id = @id AND project_id = @project_id");
                var person = await connection.QueryFirstOrDefaultAsync<PersonDataModel>(sql, new { id, project_id });

                if (person == null)
                {
                    return CreateNotFoundResponse("人員資料", id);
                }

                LogRequestComplete("獲取單一人員資料");
                return CreateSuccessResponse(person, ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully);

            }, "獲取單一人員資料");
        }

        /// <summary>
        /// 建立人員資料 API
        /// 設計改善：加入資料驗證、自動設定時間戳記、專案關聯
        /// </summary>
        /// <param name="person">人員資料</param>
        /// <returns>建立結果</returns>
        [HttpPost]
        public async Task<IActionResult> CreatePerson([FromBody] PersonDataModel person)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("建立人員資料", new { Name = person.Name, ProjectId = person.ProjectId });

                // 步驟 1：驗證必要欄位
                var validationResult = ValidatePersonData(person);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var projectValidationResult = ValidateProjectId(person.ProjectId, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：設定系統欄位
                var currentTime = DateTime.UtcNow;
                person.CreatedAt = currentTime.ToString(ApplicationConstants.Logging.LogTimeFormat);
                person.UpdatedAt = currentTime.ToString(ApplicationConstants.Logging.LogTimeFormat);

                // 步驟 3：執行資料庫插入
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = BuildPersonInsertQuery();
                var newId = await connection.ExecuteScalarAsync<int>(sql, person);

                person.Id = newId;

                Logger.LogInformation("成功建立人員資料：ID {PersonId}，姓名 {PersonName}，專案 {ProjectId}", 
                    newId, person.Name, person.ProjectId);

                LogRequestComplete("建立人員資料");
                return CreatedAtAction(nameof(GetPerson), new { id = person.Id, project_id = person.ProjectId }, person);

            }, "建立人員資料");
        }

        /// <summary>
        /// 更新人員資料 API
        /// 設計改善：加入資料存在性檢查、專案隔離驗證
        /// </summary>
        /// <param name="id">人員 ID</param>
        /// <param name="person">人員資料</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>更新結果</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePerson(int id, [FromBody] PersonDataModel person, [FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("更新人員資料", new { Id = id, Name = person.Name, ProjectId = project_id });

                // 步驟 1：驗證參數
                if (id <= 0)
                {
                    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.InvalidParameters);
                }

                var validationResult = ValidatePersonData(person);
                if (validationResult != null)
                {
                    return validationResult;
                }

                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：檢查資料是否存在
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var existsCheck = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM person_profile WHERE id = @id AND project_id = @project_id",
                    new { id, project_id = project_id });

                if (!existsCheck.HasValue)
                {
                    return CreateNotFoundResponse("人員資料", id);
                }

                // 步驟 3：執行更新
                person.Id = id;
                person.ProjectId = project_id; // 確保使用正確的專案 ID
                person.UpdatedAt = DateTime.UtcNow.ToString(ApplicationConstants.Logging.LogTimeFormat);

                var sql = BuildPersonUpdateQuery();
                var rowsAffected = await connection.ExecuteAsync(sql, person);

                if (rowsAffected == 0)
                {
                    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
                }

                Logger.LogInformation("成功更新人員資料：ID {PersonId}，姓名 {PersonName}，專案 {ProjectId}", 
                    id, person.Name, project_id);

                LogRequestComplete("更新人員資料");
                return CreateSuccessResponse(person, ApplicationConstants.ApiResponse.SuccessMessages.DataUpdatedSuccessfully);

            }, "更新人員資料");
        }

        /// <summary>
        /// 刪除人員資料 API
        /// 設計改善：加入專案隔離驗證、刪除前檢查
        /// </summary>
        /// <param name="id">人員 ID</param>
        /// <param name="project_id">專案 ID</param>
        /// <returns>刪除結果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerson(int id, [FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("刪除人員資料", new { Id = id, ProjectId = project_id });

                // 步驟 1：驗證參數
                if (id <= 0)
                {
                    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.InvalidParameters);
                }

                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：執行刪除
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var rowsAffected = await connection.ExecuteAsync(
                    "DELETE FROM person_profile WHERE id = @id AND project_id = @project_id",
                    new { id, project_id });

                if (rowsAffected == 0)
                {
                    return CreateNotFoundResponse("人員資料", id);
                }

                Logger.LogInformation("成功刪除人員資料：ID {PersonId}，專案 {ProjectId}", id, project_id);

                LogRequestComplete("刪除人員資料");
                return CreateSuccessResponse(new { id, deleted = true }, ApplicationConstants.ApiResponse.SuccessMessages.DataDeletedSuccessfully);

            }, "刪除人員資料");
        }

        /// <summary>
        /// 建構人員查詢 SQL
        /// 設計理念：統一 SQL 建構邏輯，避免重複代碼
        /// </summary>
        private static string BuildPersonSelectQuery(string? whereClause = null, int? offset = null, int? limit = null)
        {
            var sql = @"
                SELECT 
                    id,
                    name,
                    TRIM(gender) as gender,
                    birthday,
                    nationality,
                    mobile,
                    phone,
                    id_number as IdNumber,
                    passport_number as PassportNumber,
                    family_relationships as FamilyRelationships,
                    friends as ImportantFriends,
                    extra_data as profiledata,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt,
                    photo_index as Photo,
                    discovery_source as DiscoveryProcess,
                    birthplace as Birthplace,
                    ethnicity as Ethnicity,
                    ancestral_origin as AncestralHome,
                    political_party as PoliticalParty,
                    email as Email,
                    current_employer as CurrentWorkplace,
                    address as CurrentAddress,
                    mailing_address as MailingAddress,
                    experience as Experience,
                    education as Education,
                    online_accounts as OnlineAccounts,
                    publications as Publications,
                    activities as Activities,
                    frequent_locations as FrequentPlaces,
                    travel_history as TravelRecords,
                    remarks as Notes,
                    file_md5 as FileMd5,
                    project_id
                FROM person_profile";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            sql += " ORDER BY id DESC";

            if (limit.HasValue)
            {
                sql += $" LIMIT {limit}";
            }

            if (offset.HasValue)
            {
                sql += $" OFFSET {offset}";
            }

            return sql;
        }

        /// <summary>
        /// 建構人員插入 SQL
        /// 設計理念：集中 SQL 建構邏輯，便於維護
        /// </summary>
        private static string BuildPersonInsertQuery()
        {
            return @"
                INSERT INTO person_profile (
                    name, gender, birthday, nationality, mobile, phone, id_number, 
                    passport_number, family_relationships, friends, extra_data, 
                    created_at, updated_at, photo_index, discovery_source, birthplace, 
                    ethnicity, ancestral_origin, political_party, email, current_employer, 
                    address, mailing_address, experience, education, online_accounts, 
                    publications, activities, frequent_locations, travel_history, 
                    remarks, file_md5, project_id
                ) VALUES (
                    @Name, @Gender, @Birthday, @Nationality, @Mobile, @Phone, @IdNumber,
                    @PassportNumber, @FamilyRelationships, @ImportantFriends, @ProfileData,
                    @CreatedAt, @UpdatedAt, @Photo, @DiscoveryProcess, @Birthplace,
                    @Ethnicity, @AncestralHome, @PoliticalParty, @Email, @CurrentWorkplace,
                    @CurrentAddress, @MailingAddress, @Experience, @Education, @OnlineAccounts,
                    @Publications, @Activities, @FrequentPlaces, @TravelRecords,
                    @Notes, @FileMd5, @ProjectId
                ) RETURNING id";
        }

        /// <summary>
        /// 建構人員更新 SQL
        /// 設計理念：集中 SQL 建構邏輯，便於維護
        /// </summary>
        private static string BuildPersonUpdateQuery()
        {
            return @"
                UPDATE person_profile SET 
                    name = @Name, 
                    gender = @Gender, 
                    birthday = @Birthday, 
                    nationality = @Nationality, 
                    mobile = @Mobile, 
                    phone = @Phone,
                    id_number = @IdNumber,
                    passport_number = @PassportNumber,
                    family_relationships = @FamilyRelationships,
                    friends = @ImportantFriends,
                    extra_data = @ProfileData,
                    updated_at = @UpdatedAt,
                    photo_index = @Photo,
                    discovery_source = @DiscoveryProcess,
                    birthplace = @Birthplace,
                    ethnicity = @Ethnicity,
                    ancestral_origin = @AncestralHome,
                    political_party = @PoliticalParty,
                    email = @Email,
                    current_employer = @CurrentWorkplace,
                    address = @CurrentAddress,
                    mailing_address = @MailingAddress,
                    experience = @Experience,
                    education = @Education,
                    online_accounts = @OnlineAccounts,
                    publications = @Publications,
                    activities = @Activities,
                    frequent_locations = @FrequentPlaces,
                    travel_history = @TravelRecords,
                    remarks = @Notes,
                    file_md5 = @FileMd5
                WHERE id = @Id AND project_id = @ProjectId";
        }

        /// <summary>
        /// 驗證人員資料
        /// 設計理念：集中資料驗證邏輯，使用常數而非硬編碼長度限制
        /// </summary>
        private IActionResult? ValidatePersonData(PersonDataModel person)
        {
            if (person == null)
            {
                return CreateErrorResponse("人員資料不能為空");
            }

            // 驗證必要欄位
            if (string.IsNullOrWhiteSpace(person.Name))
            {
                return CreateErrorResponse("姓名不能為空");
            }

            // 驗證欄位長度（使用常數而非硬編碼）
            if (person.Name.Length > ApplicationConstants.Database.NameMaxLength)
            {
                return CreateErrorResponse($"姓名長度不能超過 {ApplicationConstants.Database.NameMaxLength} 字元");
            }

            if (!string.IsNullOrWhiteSpace(person.Email) && person.Email.Length > ApplicationConstants.Database.EmailMaxLength)
            {
                return CreateErrorResponse($"電子郵件長度不能超過 {ApplicationConstants.Database.EmailMaxLength} 字元");
            }

            if (!string.IsNullOrWhiteSpace(person.Mobile) && person.Mobile.Length > ApplicationConstants.Database.PhoneMaxLength)
            {
                return CreateErrorResponse($"手機號碼長度不能超過 {ApplicationConstants.Database.PhoneMaxLength} 字元");
            }

            return null; // 驗證通過
        }

        #endregion
    }
} 