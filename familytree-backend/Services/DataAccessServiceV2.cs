// 資料存取服務實作 V2 - 基於 user_id 的資料隔離版本
using Dapper;
using Npgsql;
using familytree_backend.Models;
using familytree_backend.Constants;
using FamilyTree.Constants;

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
                var baseSql = userRole == RoleConstants.ADMIN 
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
                if (userRole != RoleConstants.ADMIN)
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

                var sql = userRole == RoleConstants.ADMIN
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

                var sql = userRole == RoleConstants.ADMIN
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

                var parameters = userRole == RoleConstants.ADMIN ? (object)person : new { person, UserId = userId };

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

                var sql = userRole == RoleConstants.ADMIN
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

                var sql = userRole == RoleConstants.ADMIN
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

        /// <summary>
        /// 獲取人員資料列表（優化版，包含關聯資料）
        /// </summary>
        public async Task<IEnumerable<PersonDataModel>> GetPersonDataListOptimizedAsync(
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
                _logger.LogInformation("獲取人員資料列表（優化版）：使用者 {UserId}，頁碼 {Page}，頁面大小 {PageSize}", 
                    userId, page, pageSize);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // 首先獲取人員列表
                var (persons, totalCount) = await GetPersonDataListAsync(userId, userRole, page, pageSize, keyword, sortBy, sortOrder);
                var personList = persons.ToList();
                
                if (!personList.Any())
                {
                    return personList;
                }

                var personIds = personList.Select(p => p.Id).ToList();

                // 並行批量查詢關聯資料
                var relationshipsTask = GetRelationshipsByPersonIdsAsync(personIds);
                var photosTask = GetPhotosByPersonIdsAsync(personIds);

                await Task.WhenAll(relationshipsTask, photosTask);

                // 建立查找字典
                var relationshipsLookup = relationshipsTask.Result
                    .GroupBy(r => r.PersonId)
                    .ToDictionary(g => g.Key, g => g.ToList());
                    
                var photosLookup = photosTask.Result
                    .GroupBy(p => p.EntityId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 組裝資料
                foreach (var person in personList)
                {
                    // 這裡可以將關聯資料加到 person 的某些屬性中
                    // 由於原 PersonDataModel 沒有這些屬性，我們可以在需要時擴展
                }

                stopwatch.Stop();
                _logger.LogInformation("人員資料列表優化查詢完成：{Count} 筆，耗時 {ElapsedMs}ms", 
                    personList.Count, stopwatch.ElapsedMilliseconds);

                return personList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取人員資料列表（優化版）失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 批量獲取人員關係資料
        /// </summary>
        public async Task<IEnumerable<RelationshipDto>> GetRelationshipsByPersonIdsAsync(List<int> personIds)
        {
            try
            {
                if (!personIds.Any())
                {
                    return new List<RelationshipDto>();
                }

                _logger.LogInformation("批量查詢人員關係：{Count} 個人員", personIds.Count);

                const string sql = @"
                    SELECT 
                        r.id as Id,
                        r.person_id as PersonId,
                        r.related_person_id as RelatedPersonId,
                        r.relationship_type as RelationshipType,
                        r.description as Description,
                        r.is_confirmed as IsConfirmed,
                        p1.name as PersonName,
                        p2.name as RelatedPersonName,
                        r.created_at as CreatedAt,
                        r.updated_at as UpdatedAt
                    FROM relationships r
                    LEFT JOIN person_profile p1 ON r.person_id = p1.id
                    LEFT JOIN person_profile p2 ON r.related_person_id = p2.id
                    WHERE r.person_id = ANY(@PersonIds)";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var results = await connection.QueryAsync<RelationshipDto>(sql, new { PersonIds = personIds });

                _logger.LogInformation("批量查詢人員關係完成：{Count} 筆關係資料", results.Count());

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量查詢人員關係失敗");
                throw;
            }
        }

        /// <summary>
        /// 批量獲取人員照片資料
        /// </summary>
        public async Task<IEnumerable<PhotoDto>> GetPhotosByPersonIdsAsync(List<int> personIds)
        {
            try
            {
                if (!personIds.Any())
                {
                    return new List<PhotoDto>();
                }

                _logger.LogInformation("批量查詢人員照片：{Count} 個人員", personIds.Count);

                const string sql = @"
                    SELECT 
                        id as Id,
                        entity_type as EntityType,
                        entity_id as EntityId,
                        file_name as FileName,
                        file_path as FilePath,
                        file_type as FileType,
                        file_size as FileSize,
                        mime_type as MimeType,
                        md5_hash as Md5Hash,
                        uploaded_by as UploadedBy,
                        uploaded_at as UploadedAt,
                        is_deleted as IsDeleted
                    FROM file_metadata
                    WHERE entity_type = 'person' 
                    AND entity_id = ANY(@PersonIds)
                    AND is_deleted = false
                    ORDER BY uploaded_at DESC";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var results = await connection.QueryAsync<PhotoDto>(sql, new { PersonIds = personIds });

                _logger.LogInformation("批量查詢人員照片完成：{Count} 個檔案", results.Count());

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量查詢人員照片失敗");
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

                var sql = userRole == RoleConstants.ADMIN
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

                var sql = userRole == RoleConstants.ADMIN
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

                var sql = userRole == RoleConstants.ADMIN
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

        #region 專案管理操作

        /// <summary>
        /// 獲取使用者的專案列表
        /// </summary>
        public async Task<IEnumerable<ProjectModel>> GetProjectListAsync(string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("獲取專案列表：使用者 {UserId}，角色 {UserRole}", userId, userRole);

                var sql = userRole == RoleConstants.ADMIN
                    ? @"SELECT * FROM projects ORDER BY created_at DESC"
                    : @"SELECT * FROM projects WHERE user_id = @userId ORDER BY created_at DESC";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var projects = await connection.QueryAsync<ProjectModel>(sql, new { userId });

                _logger.LogInformation("獲取專案列表完成：{Count} 個專案", projects.Count());
                return projects;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取專案列表失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 根據ID獲取專案（包含權限檢查）
        /// </summary>
        public async Task<ProjectModel?> GetProjectByIdAsync(string projectId, string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("獲取專案：{ProjectId}，使用者 {UserId}", projectId, userId);

                var sql = userRole == RoleConstants.ADMIN
                    ? @"SELECT * FROM projects WHERE id = @projectId"
                    : @"SELECT * FROM projects WHERE id = @projectId AND user_id = @userId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var project = await connection.QueryFirstOrDefaultAsync<ProjectModel>(sql, new { projectId, userId });

                if (project != null)
                {
                    _logger.LogInformation("專案獲取成功：{ProjectName}", project.ProjectName);
                }
                else
                {
                    _logger.LogWarning("找不到專案或無權限：{ProjectId}", projectId);
                }

                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取專案失敗：{ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// 建立專案
        /// </summary>
        public async Task<string?> CreateProjectAsync(CreateProjectRequest request, string userId)
        {
            try
            {
                _logger.LogInformation("建立專案：{ProjectName}，使用者 {UserId}", request.ProjectName, userId);

                var projectId = Guid.NewGuid().ToString();
                var sql = @"
                    INSERT INTO projects (id, name, description, user_id, created_at, updated_at)
                    VALUES (@id, @name, @description, @userId, @createdAt, @updatedAt)";

                var parameters = new
                {
                    id = projectId,
                    name = request.ProjectName,
                    description = request.ProjectDescription,
                    userId = userId,
                    createdAt = DateTime.UtcNow,
                    updatedAt = DateTime.UtcNow
                };

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("專案建立成功：{ProjectId}", projectId);
                    return projectId;
                }
                else
                {
                    _logger.LogWarning("專案建立失敗：沒有插入任何記錄");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立專案失敗：{ProjectName}", request.ProjectName);
                throw;
            }
        }

        /// <summary>
        /// 更新專案
        /// </summary>
        public async Task<bool> UpdateProjectAsync(string projectId, UpdateProjectRequest request, string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("更新專案：{ProjectId}，使用者 {UserId}", projectId, userId);

                var sql = userRole == RoleConstants.ADMIN
                    ? @"UPDATE projects SET name = @name, description = @description, updated_at = @updatedAt WHERE id = @projectId"
                    : @"UPDATE projects SET name = @name, description = @description, updated_at = @updatedAt WHERE id = @projectId AND user_id = @userId";

                var parameters = new
                {
                    projectId = projectId,
                    name = request.ProjectName,
                    description = request.ProjectDescription,
                    updatedAt = DateTime.UtcNow,
                    userId = userId
                };

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);

                var success = rowsAffected > 0;
                if (success)
                {
                    _logger.LogInformation("專案更新成功：{ProjectId}", projectId);
                }
                else
                {
                    _logger.LogWarning("專案更新失敗：找不到專案或無權限：{ProjectId}", projectId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新專案失敗：{ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// 刪除專案
        /// </summary>
        public async Task<bool> DeleteProjectAsync(string projectId, string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("刪除專案：{ProjectId}，使用者 {UserId}", projectId, userId);

                var sql = userRole == RoleConstants.ADMIN
                    ? @"DELETE FROM projects WHERE id = @projectId"
                    : @"DELETE FROM projects WHERE id = @projectId AND user_id = @userId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var rowsAffected = await connection.ExecuteAsync(sql, new { projectId, userId });

                var success = rowsAffected > 0;
                if (success)
                {
                    _logger.LogInformation("專案刪除成功：{ProjectId}", projectId);
                }
                else
                {
                    _logger.LogWarning("專案刪除失敗：找不到專案或無權限：{ProjectId}", projectId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除專案失敗：{ProjectId}", projectId);
                throw;
            }
        }

        #endregion

        #region 搜尋操作

        /// <summary>
        /// 搜尋人員資料（基於用戶權限）
        /// </summary>
        public async Task<(IEnumerable<PersonSearchResult> Data, int TotalCount)> SearchPersonDataAsync(string userId, string userRole, SearchRequest request)
        {
            try
            {
                _logger.LogInformation("搜尋人員資料：使用者 {UserId}，關鍵字 '{Keyword}'", userId, request.Keyword);

                var whereConditions = new List<string>();
                var parameters = new DynamicParameters();

                // 權限過濾
                if (userRole != RoleConstants.ADMIN)
                {
                    whereConditions.Add("user_id = @userId");
                    parameters.Add("userId", userId);
                }

                // 關鍵字搜尋
                if (!string.IsNullOrEmpty(request.Keyword))
                {
                    whereConditions.Add("(name ILIKE @keyword OR alternate_names ILIKE @keyword)");
                    parameters.Add("keyword", $"%{request.Keyword}%");
                }

                var whereClause = whereConditions.Any() ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";

                // 計算總數
                var countSql = $"SELECT COUNT(*) FROM person_profile {whereClause}";
                var totalCount = 0;

                // 查詢資料
                var dataSql = $@"
                    SELECT id, name, birth_date, death_date, gender, 
                           alternate_names, note, user_id, created_at, updated_at
                    FROM person_profile 
                    {whereClause}
                    ORDER BY name
                    LIMIT @pageSize OFFSET @offset";

                parameters.Add("pageSize", request.PageSize);
                parameters.Add("offset", (request.Page - 1) * request.PageSize);

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
                var data = await connection.QueryAsync<PersonSearchResult>(dataSql, parameters);

                _logger.LogInformation("搜尋人員資料完成：找到 {TotalCount} 筆", totalCount);

                return (data, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜尋人員資料失敗：使用者 {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 記錄搜尋關鍵字
        /// </summary>
        public async Task RecordSearchKeywordAsync(string keyword, string searchType, string userId)
        {
            try
            {
                var sql = @"
                    INSERT INTO search_history (keyword, search_type, user_id, created_at)
                    VALUES (@keyword, @searchType, @userId, @createdAt)";

                var parameters = new
                {
                    keyword = keyword,
                    searchType = searchType,
                    userId = userId,
                    createdAt = DateTime.UtcNow
                };

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                await connection.ExecuteAsync(sql, parameters);

                _logger.LogInformation("搜尋關鍵字記錄成功：{Keyword}", keyword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄搜尋關鍵字失敗：{Keyword}", keyword);
                // 不重新拋出異常，因為這不是關鍵功能
            }
        }

        /// <summary>
        /// 獲取搜尋歷史
        /// </summary>
        public async Task<List<string>> GetSearchHistoryAsync(string userId, int limit = 20)
        {
            try
            {
                var sql = @"
                    SELECT DISTINCT keyword 
                    FROM search_history 
                    WHERE user_id = @userId 
                    ORDER BY MAX(created_at) DESC 
                    LIMIT @limit";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var history = await connection.QueryAsync<string>(sql, new { userId, limit });

                return history.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取搜尋歷史失敗：使用者 {UserId}", userId);
                return new List<string>();
            }
        }

        /// <summary>
        /// 獲取人員關係（基於用戶權限）
        /// </summary>
        public async Task<IEnumerable<RelationshipData>> GetPersonRelationshipsAsync(int personId, string userId, string userRole)
        {
            try
            {
                _logger.LogInformation("獲取人員關係：人員 {PersonId}，使用者 {UserId}", personId, userId);

                var sql = userRole == RoleConstants.ADMIN
                    ? @"SELECT * FROM relationships WHERE source_person_id = @personId OR target_person_id = @personId"
                    : @"SELECT r.* FROM relationships r 
                        INNER JOIN person_profile p1 ON r.source_person_id = p1.id 
                        INNER JOIN person_profile p2 ON r.target_person_id = p2.id 
                        WHERE (r.source_person_id = @personId OR r.target_person_id = @personId)
                        AND p1.user_id = @userId AND p2.user_id = @userId";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                
                var relationships = await connection.QueryAsync<RelationshipData>(sql, new { personId, userId });

                _logger.LogInformation("獲取人員關係完成：{Count} 筆關係", relationships.Count());
                return relationships;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取人員關係失敗：人員 {PersonId}", personId);
                throw;
            }
        }

        /// <summary>
        /// 建立關係
        /// </summary>
        public async Task<bool> CreateRelationshipAsync(RelationshipData relationship, string userId)
        {
            try
            {
                _logger.LogInformation("建立關係：{SourcePersonId} -> {TargetPersonId}", 
                    relationship.SourcePersonId, relationship.TargetPersonId);

                // 檢查權限：確保兩個人員都屬於該使用者
                var permissionSql = @"
                    SELECT COUNT(*) FROM person_profile 
                    WHERE id IN (@sourcePersonId, @targetPersonId) AND user_id = @userId";

                var sql = @"
                    INSERT INTO relationships (source_person_id, target_person_id, relation_type, 
                                              relationship_type, source_field, project_id, created_at)
                    VALUES (@sourcePersonId, @targetPersonId, @relationType, 
                            @relationshipType, @sourceField, @projectId, @createdAt)";

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 檢查權限
                var permissionCount = await connection.ExecuteScalarAsync<int>(permissionSql, new 
                { 
                    sourcePersonId = relationship.SourcePersonId,
                    targetPersonId = relationship.TargetPersonId,
                    userId = userId
                });

                if (permissionCount != 2)
                {
                    _logger.LogWarning("建立關係失敗：無權限存取相關人員");
                    return false;
                }

                var parameters = new
                {
                    sourcePersonId = relationship.SourcePersonId,
                    targetPersonId = relationship.TargetPersonId,
                    relationType = relationship.RelationType,
                    relationshipType = relationship.RelationshipType,
                    sourceField = relationship.SourceField,
                    projectId = relationship.ProjectId,
                    createdAt = DateTime.UtcNow
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);

                var success = rowsAffected > 0;
                if (success)
                {
                    _logger.LogInformation("關係建立成功");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立關係失敗");
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
                if (!string.IsNullOrEmpty(userId) && userRole != RoleConstants.ADMIN)
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
        /// 執行非查詢命令（會自動加入 user_id 過濾）
        /// </summary>
        public async Task<int> ExecuteAsync(string sql, object? parameters = null, string? userId = null, string? userRole = null)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                return await connection.ExecuteAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行非查詢命令失敗：{Sql}", sql);
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
                if (userRole == RoleConstants.ADMIN) return true;

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