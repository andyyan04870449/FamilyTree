// 人員資料控制器 - 提供人員資料的 CRUD 操作
// 優化重點：移除硬編碼、統一回應格式、改善 OOP 設計、使用配置管理
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;
using Dapper;
using familytree_backend.Constants;
using familytree_backend.Models;
using familytree_backend.Services;

namespace familytree_backend.Controllers
{
    /// <summary>
    /// 人員資料控制器
    /// 職責：處理人員資料的查詢、建立、更新、刪除操作
    /// 設計改善：繼承 BaseController，使用配置服務，移除硬編碼的日誌和連接字串
    /// </summary>
    [Route("api/[controller]")]
    public class PersonController : BaseController
    {
        private readonly string _connectionString;
        private readonly PaginationConfiguration _paginationConfig;

        /// <summary>
        /// 人員資料控制器建構子
        /// 設計改善：透過配置服務獲取設定，避免直接依賴 IConfiguration
        /// </summary>
        public PersonController(
            ILogger<PersonController> logger,
            IConfigurationService configurationService) 
            : base(logger, configurationService)
        {
            // 從配置服務獲取連接字串，避免直接依賴 IConfiguration
            _connectionString = configurationService.GetConnectionString();
            
            // 獲取分頁配置
            _paginationConfig = configurationService.GetPaginationConfiguration();
        }

        /// <summary>
        /// 獲取人員列表 API
        /// 設計改善：加入分頁支援、專案隔離驗證、統一回應格式
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <param name="page">頁碼</param>
        /// <param name="pageSize">頁面大小</param>
        /// <param name="keyword">搜尋關鍵字</param>
        /// <returns>人員列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetPersons(
            [FromQuery] string? project_id = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 0,
            [FromQuery] string? keyword = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取人員列表", new { ProjectId = project_id, Page = page, PageSize = pageSize, Keyword = keyword });

                // 步驟 1：驗證專案 ID
                var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：正規化分頁參數
                if (pageSize <= 0) pageSize = _paginationConfig.DefaultPageSize;
                var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);

                // 步驟 3：建立資料庫連接
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 步驟 4：建構查詢條件
                var whereConditions = new List<string> { "project_id = @project_id" };
                var parameters = new DynamicParameters();
                parameters.Add("@project_id", project_id);

                // 關鍵字搜尋條件
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    whereConditions.Add("(name ILIKE @keyword OR id_number ILIKE @keyword OR mobile ILIKE @keyword)");
                    parameters.Add("@keyword", $"%{keyword}%");
                }

                var whereClause = string.Join(" AND ", whereConditions);

                // 步驟 5：查詢總筆數（用於分頁計算）
                var countSql = $"SELECT COUNT(*) FROM person_profile WHERE {whereClause}";
                var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);

                // 步驟 6：查詢分頁資料
                var offset = (normalizedPage - 1) * normalizedPageSize;
                var dataSql = BuildPersonSelectQuery(whereClause, offset, normalizedPageSize);
                
                var persons = await connection.QueryAsync<PersonDataModel>(dataSql, parameters);

                Logger.LogInformation("成功獲取人員資料：總計 {TotalCount} 筆，當前頁 {CurrentPage}/{TotalPages}，本頁 {PageCount} 筆", 
                    totalCount, normalizedPage, (int)Math.Ceiling((double)totalCount / normalizedPageSize), persons.Count());

                // 步驟 7：建立回應資料
                var responseData = new PersonDataListResponse
                {
                    Success = true,
                    Message = ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully,
                    PersonDataList = persons.ToList(),
                    TotalCount = totalCount,
                    PageNumber = normalizedPage,
                    PageSize = normalizedPageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / normalizedPageSize)
                };

                LogRequestComplete("獲取人員列表", persons.Count());
                return CreatePagedResponse(responseData.PersonDataList, totalCount, normalizedPage, normalizedPageSize);

            }, "獲取人員列表");
        }

        /// <summary>
        /// 獲取單一人員資料 API
        /// 設計改善：加入詳細參數驗證和統一錯誤處理
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
        /// <returns>更新結果</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePerson(int id, [FromBody] PersonDataModel person)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("更新人員資料", new { Id = id, Name = person.Name, ProjectId = person.ProjectId });

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

                var projectValidationResult = ValidateProjectId(person.ProjectId, allowNull: false);
                if (projectValidationResult != null)
                {
                    return projectValidationResult;
                }

                // 步驟 2：檢查資料是否存在
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var existsCheck = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM person_profile WHERE id = @id AND project_id = @project_id",
                    new { id, project_id = person.ProjectId });

                if (!existsCheck.HasValue)
                {
                    return CreateNotFoundResponse("人員資料", id);
                }

                // 步驟 3：執行更新
                person.Id = id;
                person.UpdatedAt = DateTime.UtcNow.ToString(ApplicationConstants.Logging.LogTimeFormat);

                var sql = BuildPersonUpdateQuery();
                var rowsAffected = await connection.ExecuteAsync(sql, person);

                if (rowsAffected == 0)
                {
                    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
                }

                Logger.LogInformation("成功更新人員資料：ID {PersonId}，姓名 {PersonName}，專案 {ProjectId}", 
                    id, person.Name, person.ProjectId);

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
        /// 人員資料統計 API
        /// 設計理念：提供專案內人員資料的統計資訊
        /// </summary>
        /// <param name="project_id">專案 ID</param>
        /// <returns>統計資料</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetPersonStatistics([FromQuery] string? project_id = null)
        {
            return await ExecuteWithExceptionHandling(async () =>
            {
                LogRequestStart("獲取人員統計", new { ProjectId = project_id });

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
                        COUNT(CASE WHEN email IS NOT NULL AND email != '' THEN 1 END) as HasEmailCount
                    FROM person_profile 
                    WHERE project_id = @project_id";

                var stats = await connection.QueryFirstAsync(statisticsSql, new { project_id });

                LogRequestComplete("獲取人員統計");
                return CreateSuccessResponse(stats, ApplicationConstants.ApiResponse.SuccessMessages.DataRetrievedSuccessfully);

            }, "獲取人員統計");
        }

        #region 私有輔助方法

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