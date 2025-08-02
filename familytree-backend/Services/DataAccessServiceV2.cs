// 資料存取服務實作 V2 - 基於 user_id 的資料隔離版本
using Dapper;
using Npgsql;
using familytree_backend.Models;
using familytree_backend.Constants;

namespace familytree_backend.Services
{
    /// <summary>
    /// 資料存取服務實作 V2
    /// 基於 user_id 的資料隔離版本
    /// </summary>
    public class DataAccessServiceV2 : IDataAccessServiceV2
    {
        private readonly string _connectionString;
        private readonly ILogger<DataAccessServiceV2> _logger;
        private readonly IConfigurationService _configurationService;

        public DataAccessServiceV2(
            IConfigurationService configurationService,
            ILogger<DataAccessServiceV2> logger)
        {
            _configurationService = configurationService;
            _connectionString = configurationService.GetConnectionString();
            _logger = logger;
        }

        #region 人員資料操作

        /// <summary>
        /// 獲取人員資料列表（帶分頁和搜尋）
        /// </summary>
        public async Task<(IEnumerable<PersonDataModel> Data, int TotalCount)> GetPersonDataListAsync(
            string userId,
            string userRole,
            int page, 
            int pageSize, 
            string? keyword = null,
            string? sortBy = null,
            string? sortOrder = null)
        {
            try
            {
                _logger.LogInformation("獲取人員資料列表：使用者 {UserId}，頁碼 {Page}，頁面大小 {PageSize}，關鍵字 '{Keyword}'", 
                    userId, page, pageSize, keyword ?? "無");

                // 構建基礎查詢
                var baseSql = userRole == "admin" 
                    ? "FROM person_profile" 
                    : "FROM person_profile WHERE user_id = @userId";
                
                var countSql = $"SELECT COUNT(*) {baseSql}";
                var selectSql = $"SELECT * {baseSql}";

                // 添加關鍵字搜索條件
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var keywordCondition = SqlQueries.Search.FuzzySearchCondition;
                    var connector = baseSql.Contains("WHERE") ? " AND " : " WHERE ";
                    baseSql += $"{connector}{keywordCondition}";
                    countSql = $"SELECT COUNT(*) {baseSql}";
                    selectSql = $"SELECT * {baseSql}";
                }

                // 添加排序
                var validSortBy = new[] { "id", "name", "created_at", "updated_at" }.Contains(sortBy?.ToLower()) ? sortBy?.ToLower() : "id";
                var validSortOrder = new[] { "asc", "desc" }.Contains(sortOrder?.ToLower()) ? sortOrder?.ToLower() : "desc";
                selectSql += $" ORDER BY {validSortBy} {validSortOrder}";

                // 添加分頁
                var offset = (page - 1) * pageSize;
                selectSql += " LIMIT @pageSize OFFSET @offset";

                // 準備參數
                var parameters = new DynamicParameters();
                if (userRole != "admin")
                {
                    parameters.Add("userId", userId);
                }
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    parameters.Add("keyword", $"%{keyword}%");
                }
                parameters.Add("pageSize", pageSize);
                parameters.Add("offset", offset);

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 執行查詢
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
                var personDataList = await connection.QueryAsync<PersonDataModel>(selectSql, parameters);

                _logger.LogInformation("人員資料列表查詢完成：總計 {TotalCount} 筆，當前頁 {Count} 筆", 
                    totalCount, personDataList.Count());

                return (personDataList, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取人員資料列表失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 根據ID獲取人員資料
        /// </summary>
        public async Task<PersonDataModel?> GetPersonDataByIdAsync(int id, string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("查詢人員資料：ID {Id}，使用者 {UserId}", id, userId);

                var sql = userRole == "admin"
                    ? @"SELECT * FROM person_profile WHERE id = @id"
                    : @"SELECT * FROM person_profile WHERE id = @id AND user_id = @userId";

                var parameters = new { id, userId };

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var result = await connection.QuerySingleOrDefaultAsync<PersonDataModel>(sql, parameters);

                if (result != null)
                {
                    _logger.LogInformation("找到人員資料：ID {Id}，姓名 {Name}", id, result.Name);
                }
                else
                {
                    _logger.LogWarning("未找到人員資料：ID {Id}，使用者 {UserId}", id, userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢人員資料失敗：ID {Id}，使用者 {UserId}", id, userId);
                throw;
            }
        }

        /// <summary>
        /// 新增人員資料
        /// </summary>
        public async Task<int> AddPersonAsync(PersonDataModel person, string userId)
        {
            try
            {
                _logger.LogInformation("新增人員資料：姓名 {Name}，使用者 {UserId}", person.Name, userId);

                person.UserId = userId;
                person.CreatedAt = DateTime.UtcNow;
                person.UpdatedAt = DateTime.UtcNow;

                var sql = @"
                    INSERT INTO person_profile (
                        name, gender, birthday, nationality, mobile, phone, 
                        id_number, passport_number, family_relationships, friends, 
                        activities, experience, education, publications, email, 
                        current_employer, address, mailing_address, birthplace, 
                        ethnicity, ancestral_origin, political_party, online_accounts, 
                        frequent_locations, travel_history, discovery_process, 
                        remarks, file_md5, extra_data, photo_index, photo,
                        user_id, created_at, updated_at
                    ) VALUES (
                        @Name, @Gender, @Birthday, @Nationality, @Mobile, @Phone, 
                        @IdNumber, @PassportNumber, @FamilyRelationships, @Friends, 
                        @Activities, @Experience, @Education, @Publications, @Email, 
                        @CurrentEmployer, @Address, @MailingAddress, @Birthplace, 
                        @Ethnicity, @AncestralOrigin, @PoliticalParty, @OnlineAccounts, 
                        @FrequentLocations, @TravelHistory, @DiscoveryProcess, 
                        @Remarks, @FileMd5, @ExtraData, @PhotoIndex, @Photo,
                        @UserId, @CreatedAt, @UpdatedAt
                    ) RETURNING id";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var newId = await connection.ExecuteScalarAsync<int>(sql, person);

                _logger.LogInformation("人員資料新增成功：ID {Id}，姓名 {Name}", newId, person.Name);

                return newId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增人員資料失敗：姓名 {Name}", person.Name);
                throw;
            }
        }

        /// <summary>
        /// 更新人員資料
        /// </summary>
        public async Task<bool> UpdatePersonAsync(int personId, PersonDataModel person, string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("更新人員資料：ID {PersonId}，使用者 {UserId}", personId, userId);

                person.Id = personId;
                person.UpdatedAt = DateTime.UtcNow;

                var sql = userRole == "admin"
                    ? @"UPDATE person_profile 
                        SET name = @Name, gender = @Gender, birthday = @Birthday, 
                            nationality = @Nationality, mobile = @Mobile, phone = @Phone, 
                            id_number = @IdNumber, passport_number = @PassportNumber, 
                            family_relationships = @FamilyRelationships, friends = @Friends, 
                            activities = @Activities, experience = @Experience, education = @Education, 
                            publications = @Publications, email = @Email, current_employer = @CurrentEmployer, 
                            address = @Address, mailing_address = @MailingAddress, birthplace = @Birthplace, 
                            ethnicity = @Ethnicity, ancestral_origin = @AncestralOrigin, 
                            political_party = @PoliticalParty, online_accounts = @OnlineAccounts, 
                            frequent_locations = @FrequentLocations, travel_history = @TravelHistory, 
                            discovery_process = @DiscoveryProcess, remarks = @Remarks, 
                            file_md5 = @FileMd5, extra_data = @ExtraData, photo_index = @PhotoIndex, 
                            photo = @Photo, updated_at = @UpdatedAt
                        WHERE id = @Id"
                    : @"UPDATE person_profile 
                        SET name = @Name, gender = @Gender, birthday = @Birthday, 
                            nationality = @Nationality, mobile = @Mobile, phone = @Phone, 
                            id_number = @IdNumber, passport_number = @PassportNumber, 
                            family_relationships = @FamilyRelationships, friends = @Friends, 
                            activities = @Activities, experience = @Experience, education = @Education, 
                            publications = @Publications, email = @Email, current_employer = @CurrentEmployer, 
                            address = @Address, mailing_address = @MailingAddress, birthplace = @Birthplace, 
                            ethnicity = @Ethnicity, ancestral_origin = @AncestralOrigin, 
                            political_party = @PoliticalParty, online_accounts = @OnlineAccounts, 
                            frequent_locations = @FrequentLocations, travel_history = @TravelHistory, 
                            discovery_process = @DiscoveryProcess, remarks = @Remarks, 
                            file_md5 = @FileMd5, extra_data = @ExtraData, photo_index = @PhotoIndex, 
                            photo = @Photo, updated_at = @UpdatedAt
                        WHERE id = @Id AND user_id = @UserId";

                var parameters = userRole == "admin" ? (object)person : new { person, UserId = userId };

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var result = await connection.ExecuteAsync(sql, parameters);

                _logger.LogInformation("人員資料更新{Result}：ID {PersonId}", result > 0 ? "成功" : "失敗", personId);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新人員資料失敗：ID {PersonId}", personId);
                throw;
            }
        }

        /// <summary>
        /// 刪除人員資料
        /// </summary>
        public async Task<bool> DeletePersonAsync(int personId, string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("刪除人員資料：ID {PersonId}，使用者 {UserId}", personId, userId);

                var sql = userRole == "admin"
                    ? @"DELETE FROM person_profile WHERE id = @personId"
                    : @"DELETE FROM person_profile WHERE id = @personId AND user_id = @userId";

                var parameters = new { personId, userId };

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var result = await connection.ExecuteAsync(sql, parameters);

                _logger.LogInformation("人員資料刪除{Result}：ID {PersonId}", result > 0 ? "成功" : "失敗", personId);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除人員資料失敗：ID {PersonId}", personId);
                throw;
            }
        }

        /// <summary>
        /// 搜尋人員資料
        /// </summary>
        public async Task<IEnumerable<PersonDataModel>> SearchPersonsAsync(string userId, string userRole, SearchRequest request)
        {
            try
            {
                _logger.LogInformation("搜尋人員資料：關鍵字 '{Keyword}'，使用者 {UserId}", request.Keyword, userId);

                var searchCondition = request.SearchType?.ToLower() == "exact" 
                    ? SqlQueries.Search.ExactSearchCondition 
                    : SqlQueries.Search.FuzzySearchCondition;

                var sql = userRole == "admin"
                    ? $@"SELECT * FROM person_profile WHERE {searchCondition}"
                    : $@"SELECT * FROM person_profile WHERE user_id = @userId AND {searchCondition}";

                var offset = (request.Page - 1) * request.PageSize;
                sql += " ORDER BY id DESC LIMIT @pageSize OFFSET @offset";

                var parameters = new
                {
                    userId,
                    keyword = request.SearchType?.ToLower() == "exact" ? request.Keyword : $"%{request.Keyword}%",
                    pageSize = request.PageSize,
                    offset
                };

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var results = await connection.QueryAsync<PersonDataModel>(sql, parameters);

                _logger.LogInformation("搜尋完成：找到 {Count} 筆結果", results.Count());

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋人員資料失敗：關鍵字 '{Keyword}'", request.Keyword);
                throw;
            }
        }

        #endregion

        #region 我的最愛操作

        /// <summary>
        /// 獲取我的最愛列表
        /// </summary>
        public async Task<IEnumerable<FavoriteModel>> GetFavoritesAsync(string userId)
        {
            try
            {
                _logger.LogInformation("獲取我的最愛列表：使用者 {UserId}", userId);

                var sql = @"
                    SELECT 
                        f.id AS Id,
                        f.user_id AS UserId,
                        f.person_id AS PersonId,
                        f.created_at AS CreatedAt,
                        p.name AS PersonName,
                        p.gender AS PersonGender,
                        p.email AS PersonEmail,
                        p.mobile AS PersonMobile
                    FROM user_favorites f
                    INNER JOIN person_profile p ON f.person_id = p.id
                    WHERE f.user_id = @userId
                    ORDER BY f.created_at DESC";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var favorites = await connection.QueryAsync<FavoriteModel>(sql, new { userId });

                _logger.LogInformation("獲取我的最愛完成：{Count} 筆", favorites.Count());

                return favorites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取我的最愛失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 新增我的最愛
        /// </summary>
        public async Task<bool> AddFavoriteAsync(string userId, int personId)
        {
            try
            {
                _logger.LogInformation("新增我的最愛：使用者 {UserId}，人員 {PersonId}", userId, personId);

                var sql = @"
                    INSERT INTO user_favorites (user_id, person_id, created_at)
                    VALUES (@userId, @personId, @createdAt)
                    ON CONFLICT (user_id, person_id) DO NOTHING";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var result = await connection.ExecuteAsync(sql, new
                {
                    userId,
                    personId,
                    createdAt = DateTime.UtcNow
                });

                _logger.LogInformation("新增我的最愛{Result}", result > 0 ? "成功" : "已存在");

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增我的最愛失敗：使用者 {UserId}，人員 {PersonId}", userId, personId);
                throw;
            }
        }

        /// <summary>
        /// 移除我的最愛
        /// </summary>
        public async Task<bool> RemoveFavoriteAsync(string userId, int personId)
        {
            try
            {
                _logger.LogInformation("移除我的最愛：使用者 {UserId}，人員 {PersonId}", userId, personId);

                var sql = @"DELETE FROM user_favorites WHERE user_id = @userId AND person_id = @personId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var result = await connection.ExecuteAsync(sql, new { userId, personId });

                _logger.LogInformation("移除我的最愛{Result}", result > 0 ? "成功" : "不存在");

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除我的最愛失敗：使用者 {UserId}，人員 {PersonId}", userId, personId);
                throw;
            }
        }

        /// <summary>
        /// 檢查是否為我的最愛
        /// </summary>
        public async Task<bool> IsFavoriteAsync(string userId, int personId)
        {
            try
            {
                var sql = @"SELECT COUNT(*) FROM user_favorites WHERE user_id = @userId AND person_id = @personId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var count = await connection.ExecuteScalarAsync<int>(sql, new { userId, personId });

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查我的最愛失敗：使用者 {UserId}，人員 {PersonId}", userId, personId);
                throw;
            }
        }

        #endregion

        #region 檔案操作

        /// <summary>
        /// 獲取使用者的檔案上傳記錄
        /// </summary>
        public async Task<IEnumerable<FileUploadRecord>> GetFileUploadRecordsAsync(string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("獲取檔案上傳記錄：使用者 {UserId}", userId);

                var sql = userRole == "admin"
                    ? @"SELECT * FROM file_uploads ORDER BY upload_time DESC"
                    : @"SELECT * FROM file_uploads WHERE user_id = @userId ORDER BY upload_time DESC";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var records = await connection.QueryAsync<FileUploadRecord>(sql, new { userId });

                _logger.LogInformation("獲取檔案上傳記錄完成：{Count} 筆", records.Count());

                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取檔案上傳記錄失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 新增檔案上傳記錄
        /// </summary>
        public async Task<bool> AddFileUploadRecordAsync(FileUploadRecord record, string userId)
        {
            try
            {
                _logger.LogInformation("新增檔案上傳記錄：檔案 {FileName}，使用者 {UserId}", record.FileName, userId);

                record.UserId = userId;
                record.UploadTime = DateTime.UtcNow;

                var sql = @"
                    INSERT INTO file_uploads (
                        file_name, md5_hash, file_size, status, user_id, 
                        file_path, file_type, upload_time
                    ) VALUES (
                        @FileName, @Md5Hash, @FileSize, @Status, @UserId, 
                        @FilePath, @FileType, @UploadTime
                    )";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var result = await connection.ExecuteAsync(sql, record);

                _logger.LogInformation("新增檔案上傳記錄{Result}", result > 0 ? "成功" : "失敗");

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增檔案上傳記錄失敗：檔案 {FileName}", record.FileName);
                throw;
            }
        }

        #endregion

        #region 分析操作

        /// <summary>
        /// 獲取分析會話列表
        /// </summary>
        public async Task<IEnumerable<AnalysisSessionModel>> GetAnalysisSessionsAsync(string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("獲取分析會話列表：使用者 {UserId}", userId);

                var sql = userRole == "admin"
                    ? @"SELECT * FROM analysis_sessions ORDER BY created_at DESC"
                    : @"SELECT * FROM analysis_sessions WHERE user_id = @userId ORDER BY created_at DESC";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var sessions = await connection.QueryAsync<AnalysisSessionModel>(sql, new { userId });

                _logger.LogInformation("獲取分析會話列表完成：{Count} 筆", sessions.Count());

                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取分析會話列表失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 獲取分析結果
        /// </summary>
        public async Task<AnalysisResultModel?> GetAnalysisResultAsync(string sessionId, string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("獲取分析結果：會話 {SessionId}，使用者 {UserId}", sessionId, userId);

                var sql = userRole == "admin"
                    ? @"SELECT r.* FROM analysis_results r 
                        INNER JOIN analysis_sessions s ON r.session_id = s.id
                        WHERE r.session_id = @sessionId"
                    : @"SELECT r.* FROM analysis_results r 
                        INNER JOIN analysis_sessions s ON r.session_id = s.id
                        WHERE r.session_id = @sessionId AND s.user_id = @userId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var result = await connection.QuerySingleOrDefaultAsync<AnalysisResultModel>(sql, new { sessionId, userId });

                if (result != null)
                {
                    _logger.LogInformation("找到分析結果：會話 {SessionId}", sessionId);
                }
                else
                {
                    _logger.LogWarning("未找到分析結果：會話 {SessionId}", sessionId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取分析結果失敗：會話 {SessionId}", sessionId);
                throw;
            }
        }

        #endregion

        #region 欄位對應操作

        /// <summary>
        /// 獲取使用者的欄位對應設定
        /// </summary>
        public async Task<IEnumerable<FieldMappingModel>> GetFieldMappingsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("獲取欄位對應設定：使用者 {UserId}", userId);

                var sql = @"SELECT * FROM field_mapping WHERE user_id = @userId ORDER BY created_at DESC";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var mappings = await connection.QueryAsync<FieldMappingModel>(sql, new { userId });

                _logger.LogInformation("獲取欄位對應設定完成：{Count} 筆", mappings.Count());

                return mappings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取欄位對應設定失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 儲存欄位對應設定
        /// </summary>
        public async Task<bool> SaveFieldMappingsAsync(string userId, IEnumerable<FieldMappingModel> mappings)
        {
            try
            {
                _logger.LogInformation("儲存欄位對應設定：使用者 {UserId}，{Count} 筆", userId, mappings.Count());

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    // 刪除舊的對應設定
                    var deleteSql = @"DELETE FROM field_mapping WHERE user_id = @userId";
                    await connection.ExecuteAsync(deleteSql, new { userId }, transaction);

                    // 新增新的對應設定
                    var insertSql = @"
                        INSERT INTO field_mapping (
                            user_id, source_field, target_field, field_type, 
                            is_required, default_value, validation_rules, 
                            created_at, updated_at
                        ) VALUES (
                            @UserId, @SourceField, @TargetField, @FieldType, 
                            @IsRequired, @DefaultValue, @ValidationRules, 
                            @CreatedAt, @UpdatedAt
                        )";

                    foreach (var mapping in mappings)
                    {
                        mapping.UserId = userId;
                        mapping.CreatedAt = DateTime.UtcNow;
                        mapping.UpdatedAt = DateTime.UtcNow;
                        await connection.ExecuteAsync(insertSql, mapping, transaction);
                    }

                    await transaction.CommitAsync();

                    _logger.LogInformation("儲存欄位對應設定成功");
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "儲存欄位對應設定失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region 通用查詢

        /// <summary>
        /// 執行通用查詢（會自動加入 user_id 過濾）
        /// </summary>
        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object? parameters = null, string? userId = null, string? userRole = null)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 如果提供了 userId 且不是 admin，自動加入 user_id 過濾
                if (!string.IsNullOrEmpty(userId) && userRole != "admin")
                {
                    // 簡單的 SQL 解析，加入 user_id 條件
                    if (sql.ToUpper().Contains("WHERE"))
                    {
                        sql = sql.Replace("WHERE", "WHERE user_id = @__userId AND ", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (sql.ToUpper().Contains("FROM"))
                    {
                        var fromIndex = sql.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
                        var nextClauseIndex = sql.IndexOfAny(new[] { "ORDER", "GROUP", "HAVING", "LIMIT" }, fromIndex, StringComparison.OrdinalIgnoreCase);
                        if (nextClauseIndex == -1) nextClauseIndex = sql.Length;
                        
                        sql = sql.Insert(nextClauseIndex, " WHERE user_id = @__userId ");
                    }

                    // 動態添加 userId 參數
                    var dynamicParams = new DynamicParameters(parameters);
                    dynamicParams.Add("__userId", userId);
                    parameters = dynamicParams;
                }

                return await connection.QueryAsync<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行通用查詢失敗：{Sql}", sql);
                throw;
            }
        }

        /// <summary>
        /// 執行標量查詢
        /// </summary>
        public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行標量查詢失敗：{Sql}", sql);
                throw;
            }
        }

        /// <summary>
        /// 檢查資源擁有權
        /// </summary>
        public async Task<bool> CheckResourceOwnershipAsync(string resourceType, string resourceId, string userId, string userRole)
        {
            try
            {
                if (userRole == "admin") return true;

                var sql = resourceType.ToLower() switch
                {
                    "person" => "SELECT COUNT(*) FROM person_profile WHERE id = @resourceId AND user_id = @userId",
                    "file" => "SELECT COUNT(*) FROM file_uploads WHERE id = @resourceId AND user_id = @userId",
                    "analysis" => "SELECT COUNT(*) FROM analysis_sessions WHERE id = @resourceId AND user_id = @userId",
                    _ => throw new ArgumentException($"未知的資源類型：{resourceType}")
                };

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var count = await connection.ExecuteScalarAsync<int>(sql, new { resourceId, userId });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查資源擁有權失敗：{ResourceType} {ResourceId}", resourceType, resourceId);
                throw;
            }
        }

        #endregion
    }

    // StringExtensions for IndexOfAny
    internal static class StringExtensions
    {
        public static int IndexOfAny(this string source, string[] values, int startIndex, StringComparison comparisonType)
        {
            var minIndex = -1;
            foreach (var value in values)
            {
                var index = source.IndexOf(value, startIndex, comparisonType);
                if (index >= 0 && (minIndex == -1 || index < minIndex))
                {
                    minIndex = index;
                }
            }
            return minIndex;
        }
    }
}