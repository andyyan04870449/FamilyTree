// 資料存取服務實作 - 統一管理所有資料庫操作（簡化版）
using Dapper;
using Npgsql;
using familytree_backend.Models;
using familytree_backend.Constants;

namespace familytree_backend.Services
{
    /// <summary>
    /// 資料存取服務實作（簡化版，避免循環依賴）
    /// </summary>
    public class DataAccessService : IDataAccessService
    {
        private readonly string _connectionString;
        private readonly ILogger<DataAccessService> _logger;
        private readonly IConfigurationService _configurationService;

        public DataAccessService(
            IConfigurationService configurationService,
            ILogger<DataAccessService> logger)
        {
            _configurationService = configurationService;
            _connectionString = configurationService.GetConnectionString();
            _logger = logger;
        }

        #region 專案管理操作

        /// <summary>
        /// 獲取專案列表
        /// </summary>
        public async Task<IEnumerable<ProjectModel>> GetProjectListAsync(string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        id as Id,
                        user_id as UserId,
                        project_name as ProjectName,
                        project_description as ProjectDescription,
                        status as Status,
                        created_at as CreatedAt,
                        completed_at as CompletedAt,
                        updated_at as UpdatedAt,
                        COALESCE((SELECT COUNT(*) FROM person_profile WHERE project_id = p.id), 0) as MemberCount,
                        0 as RelationshipCount
                    FROM projects p 
                    WHERE status != 'deleted'
                    ORDER BY created_at DESC";

                var projects = await connection.QueryAsync<ProjectModel>(sql);
                return projects;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取專案列表失敗");
                throw;
            }
        }

        /// <summary>
        /// 根據ID獲取專案
        /// </summary>
        public async Task<ProjectModel?> GetProjectByIdAsync(string projectId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        id as Id,
                        user_id as UserId,
                        project_name as ProjectName,
                        project_description as ProjectDescription,
                        status as Status,
                        created_at as CreatedAt,
                        completed_at as CompletedAt,
                        updated_at as UpdatedAt,
                        COALESCE((SELECT COUNT(*) FROM person_profile WHERE project_id = p.id), 0) as MemberCount,
                        0 as RelationshipCount
                    FROM projects p 
                    WHERE id = @ProjectId AND status != 'deleted'";

                var project = await connection.QuerySingleOrDefaultAsync<ProjectModel>(sql, new { ProjectId = projectId });
                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取專案失敗: ProjectId={ProjectId}", projectId);
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
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 生成短的專案 ID（在 25 字符限制內）
                var timestamp = DateTime.Now.ToString("yyMMddHHmmss"); // 12 字符
                var randomSuffix = Guid.NewGuid().ToString("N")[..6]; // 6 字符
                var projectId = $"p{timestamp}{randomSuffix}"; // 總共 19 字符 (1+12+6)
                
                var sql = @"
                    INSERT INTO projects (id, user_id, project_name, project_description, status, created_at, updated_at)
                    VALUES (@Id, @UserId, @ProjectName, @ProjectDescription, @Status, @CreatedAt, @UpdatedAt)";

                await connection.ExecuteAsync(sql, new
                {
                    Id = projectId,
                    UserId = userId,
                    ProjectName = request.ProjectName,
                    ProjectDescription = request.ProjectDescription,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                return projectId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立專案失敗: Name={ProjectName}", request.ProjectName);
                throw;
            }
        }

        /// <summary>
        /// 更新專案
        /// </summary>
        public async Task<bool> UpdateProjectAsync(string projectId, UpdateProjectRequest request)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE projects 
                    SET project_name = @ProjectName,
                        project_description = @ProjectDescription,
                        status = @Status,
                        updated_at = @UpdatedAt,
                        completed_at = CASE WHEN @Status = 'completed' THEN @CompletedAt ELSE completed_at END
                    WHERE id = @ProjectId AND status != 'deleted'";

                var result = await connection.ExecuteAsync(sql, new
                {
                    ProjectId = projectId,
                    ProjectName = request.ProjectName,
                    ProjectDescription = request.ProjectDescription,
                    Status = request.Status,
                    UpdatedAt = DateTime.UtcNow,
                    CompletedAt = request.Status == "completed" ? DateTime.UtcNow : (DateTime?)null
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新專案失敗: ProjectId={ProjectId}", projectId);
                throw;
            }
        }

        /// <summary>
        /// 刪除專案（軟刪除）
        /// </summary>
        public async Task<bool> DeleteProjectAsync(string projectId, string userId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE projects 
                    SET status = 'deleted',
                        updated_at = @UpdatedAt
                    WHERE id = @ProjectId";

                var result = await connection.ExecuteAsync(sql, new
                {
                    ProjectId = projectId,
                    UpdatedAt = DateTime.UtcNow
                });

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除專案失敗: ProjectId={ProjectId}", projectId);
                throw;
            }
        }

        #endregion

        #region 基礎查詢方法（實作IDataAccessService需要的方法）

        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<T>(sql, parameters);
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<T>(sql, parameters);
        }

        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.ExecuteAsync(sql, parameters);
        }

        // 暫時實作其他必需方法以滿足介面要求
        public async Task<(IEnumerable<PersonDataModel> Data, int TotalCount)> GetPersonDataListAsync(string projectId, int page, int pageSize, string? keyword = null, string? sortBy = null, string? sortOrder = null)
        {
            try
            {
                _logger.LogInformation("獲取人員資料列表：專案 {ProjectId}，頁碼 {Page}，頁面大小 {PageSize}，關鍵字 '{Keyword}'", 
                    projectId, page, pageSize, keyword ?? "無");

                // 構建基礎查詢
                var baseSql = "FROM person_profile WHERE project_id = @projectId";
                var countSql = $"SELECT COUNT(*) {baseSql}";
                var selectSql = $"SELECT * {baseSql}";

                // 添加關鍵字搜索條件
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var keywordCondition = SqlQueries.Search.FuzzySearchCondition;
                    baseSql += $" AND {keywordCondition}";
                    countSql = $"SELECT COUNT(*) FROM person_profile WHERE project_id = @projectId AND {keywordCondition}";
                    selectSql = $"SELECT * FROM person_profile WHERE project_id = @projectId AND {keywordCondition}";
                }

                // 添加排序
                var validSortBy = new[] { "id", "name", "created_at", "updated_at" }.Contains(sortBy?.ToLower()) ? sortBy?.ToLower() : "id";
                var validSortOrder = new[] { "asc", "desc" }.Contains(sortOrder?.ToLower()) ? sortOrder?.ToLower() : "desc";
                selectSql += $" ORDER BY {validSortBy} {validSortOrder}";

                // 添加分頁
                var offset = (page - 1) * pageSize;
                selectSql += " LIMIT @pageSize OFFSET @offset";

                // 準備參數
                object countParameters;
                object selectParameters;

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var keywordParam = $"%{keyword}%";
                    countParameters = new { projectId, keyword = keywordParam };
                    selectParameters = new { projectId, keyword = keywordParam, pageSize, offset };
                }
                else
                {
                    countParameters = new { projectId };
                    selectParameters = new { projectId, pageSize, offset };
                }

                // 執行查詢
                var totalCount = await ExecuteScalarAsync<int>(countSql, countParameters);
                var personDataList = await ExecuteQueryAsync<PersonDataModel>(selectSql, selectParameters);

                _logger.LogInformation("人員資料列表查詢完成：總計 {TotalCount} 筆，當前頁 {Count} 筆", 
                    totalCount, personDataList.Count());

                return (personDataList, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取人員資料列表失敗：專案 {ProjectId}", projectId);
                throw;
            }
        }

        public async Task<PersonDataModel?> GetPersonDataByIdAsync(int id, string projectId)
        {
            try
            {
                _logger.LogInformation("查詢人員資料：ID {Id}，專案 {ProjectId}", id, projectId);

                var sql = @"
                    SELECT 
                        id, name, gender, birthday, nationality, mobile, phone, 
                        id_number, passport_number, family_relationships, friends, 
                        activities, experience, education, publications, email, 
                        current_employer, address, mailing_address, birthplace, 
                        ethnicity, ancestral_origin, political_party, online_accounts, 
                        frequent_locations, travel_history, discovery_process, 
                        remarks, file_md5, extra_data, photo_index,
                        project_id, created_at, updated_at
                    FROM person_profile 
                    WHERE id = @id AND project_id = @projectId";

                var parameters = new { id, projectId };

                var personData = await ExecuteQueryAsync<PersonDataModel>(sql, parameters);

                var result = personData.FirstOrDefault();

                if (result != null)
                {
                    _logger.LogInformation("找到人員資料：ID {Id}，姓名 {Name}", id, result.Name);
                }
                else
                {
                    _logger.LogWarning("未找到人員資料：ID {Id}，專案 {ProjectId}", id, projectId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢人員資料失敗：ID {Id}，專案 {ProjectId}", id, projectId);
                throw;
            }
        }

        public Task<int> CreatePersonDataAsync(PersonDataModel personData)
        {
            throw new NotImplementedException("暫未實作");
        }

        public Task<bool> UpdatePersonDataAsync(PersonDataModel personData)
        {
            throw new NotImplementedException("暫未實作");
        }

        public Task<bool> DeletePersonDataAsync(string projectId, int personId)
        {
            throw new NotImplementedException("暫未實作");
        }

        // 介面適配方法
        public async Task<IEnumerable<PersonDataModel>> GetPersonDataAsync(string projectId, int page = 1, int pageSize = 10)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task<PersonDataModel?> GetPersonByIdAsync(int personId, string projectId)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task<bool> AddPersonAsync(PersonDataModel person, string projectId)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task<bool> UpdatePersonAsync(PersonDataModel person, string projectId)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task<bool> DeletePersonAsync(int personId, string projectId)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task<IEnumerable<PersonDataModel>> SearchPersonsAsync(SearchRequest request)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task<IEnumerable<RelationshipData>> GetPersonRelationshipsAsync(int personId, string projectId)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task<IEnumerable<FileUploadRecord>> GetFileUploadRecordsAsync(string projectId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        file_id as Id,
                        filename as FileName,
                        original_filename as OriginalFileName,
                        file_path as FilePath,
                        file_size as FileSize,
                        md5_hash as Md5Hash,
                        file_type as FileType,
                        user_id as UserId,
                        associated_record_id as ProjectId,
                        upload_status as Status,
                        uploaded_at as UploadTime
                    FROM file_uploads 
                    WHERE associated_record_id = @ProjectId 
                      AND associated_record_type = 'project'
                      AND upload_status != 'deleted'
                    ORDER BY uploaded_at DESC";

                var records = await connection.QueryAsync<FileUploadRecord>(sql, new { ProjectId = projectId });
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取檔案上傳記錄失敗: ProjectId={ProjectId}", projectId);
                throw;
            }
        }



        public async Task<bool> DeletePersonDataAsync(int id, string projectId)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task<(IEnumerable<PersonSearchResult> Data, int TotalCount)> SearchPersonDataAsync(SearchRequest request)
        {
            try
            {
                _logger.LogInformation("開始執行搜索：關鍵字 '{Keyword}'，類型 {SearchType}，專案 {ProjectId}", 
                    request.Keyword, request.SearchType, request.ProjectId ?? "全專案");

                // 構建搜索條件
                var searchCondition = request.SearchType.ToLower() == "exact" 
                    ? SqlQueries.Search.ExactSearchCondition 
                    : SqlQueries.Search.FuzzySearchCondition;

                // 構建完整的SQL查詢
                var sql = $@"
                    SELECT {SqlQueries.Search.SearchResultFields}
                    FROM person_profile 
                    WHERE {searchCondition}";

                // 添加專案篩選條件
                if (!string.IsNullOrEmpty(request.ProjectId))
                {
                    sql += " AND project_id = @projectId";
                }

                // 獲取總數
                var countSql = $@"
                    SELECT COUNT(*) 
                    FROM person_profile 
                    WHERE {searchCondition}";

                if (!string.IsNullOrEmpty(request.ProjectId))
                {
                    countSql += " AND project_id = @projectId";
                }

                // 添加分頁
                var offset = (request.Page - 1) * request.PageSize;
                sql += " ORDER BY id DESC LIMIT @pageSize OFFSET @offset";

                // 準備參數
                var parameters = new
                {
                    keyword = request.SearchType.ToLower() == "exact" ? request.Keyword : $"%{request.Keyword}%",
                    projectId = request.ProjectId,
                    pageSize = request.PageSize,
                    offset = offset
                };

                var countParameters = new
                {
                    keyword = request.SearchType.ToLower() == "exact" ? request.Keyword : $"%{request.Keyword}%",
                    projectId = request.ProjectId
                };

                // 執行查詢
                var searchResults = await ExecuteQueryAsync<PersonSearchResult>(sql, parameters);
                var totalCount = await ExecuteScalarAsync<int>(countSql, countParameters);

                _logger.LogInformation("搜索完成：找到 {Count} 筆結果", totalCount);

                return (searchResults, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索執行失敗：關鍵字 '{Keyword}'", request.Keyword);
                throw;
            }
        }

        public async Task RecordSearchKeywordAsync(string keyword, string searchType, string? projectId = null)
        {
            try
            {
                _logger.LogInformation("記錄搜索關鍵字：'{Keyword}'，類型 {SearchType}，專案 {ProjectId}", 
                    keyword, searchType, projectId ?? "全專案");

                var parameters = new
                {
                    keyword = keyword,
                    searchType = searchType,
                    projectId = projectId,
                    now = DateTime.UtcNow
                };

                await ExecuteAsync(SqlQueries.Search.RecordKeyword, parameters);

                _logger.LogInformation("搜索關鍵字記錄成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄搜索關鍵字失敗：'{Keyword}'", keyword);
                // 不拋出異常，避免影響搜索功能
            }
        }


        public async Task<List<string>> GetSearchHistoryAsync(string? projectId = null, int limit = 20)
        {
            try
            {
                _logger.LogInformation("獲取搜索歷史：專案 {ProjectId}，限制 {Limit}", 
                    projectId ?? "全專案", limit);

                var parameters = new
                {
                    projectId = projectId,
                    limit = limit
                };

                var history = await ExecuteQueryAsync<string>(SqlQueries.Search.GetSearchHistory, parameters);

                _logger.LogInformation("獲取搜索歷史成功：{Count} 個", history.Count());

                return history.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取搜索歷史失敗");
                return new List<string>();
            }
        }

        public async Task<bool> CreateRelationshipAsync(RelationshipData relationship)
        {
            try
            {
                _logger.LogInformation("建立人員關係：{SourceId} -> {TargetId} ({Type})", 
                    relationship.SourcePersonId, relationship.TargetPersonId, relationship.RelationshipType);

                var sql = @"
                    INSERT INTO relationship_layers (
                        source_person_id, 
                        target_person_id, 
                        relation_type, 
                        source_field, 
                        project_id, 
                        visual_analysis_graph_id
                    ) VALUES (
                        @SourcePersonId, 
                        @TargetPersonId, 
                        @RelationType, 
                        @SourceField, 
                        @ProjectId, 
                        @VisualAnalysisGraphId
                    )";

                var parameters = new
                {
                    relationship.SourcePersonId,
                    relationship.TargetPersonId,
                    RelationType = relationship.RelationshipType,
                    SourceField = relationship.SourceField ?? "manual",
                    ProjectId = relationship.ProjectId,
                    relationship.VisualAnalysisGraphId
                };

                await ExecuteAsync(sql, parameters);

                _logger.LogInformation("人員關係建立成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立人員關係失敗：{SourceId} -> {TargetId}，錯誤詳情：{ErrorMessage}", 
                    relationship.SourcePersonId, relationship.TargetPersonId, ex.Message);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string md5Hash)
        {
            throw new NotImplementedException("暫未實作");
        }

        public async Task RecordFileUploadAsync(FileUploadRecord record)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO file_uploads (
                        user_id, filename, original_filename, file_path, file_size, 
                        md5_hash, file_type, associated_record_id, associated_record_type,
                        upload_status, uploaded_at
                    ) VALUES (
                        @UserId, @FileName, @FileName, @FilePath, @FileSize,
                        @Md5Hash, @FileType, @ProjectId, 'project',
                        @Status, @UploadTime
                    )";

                await connection.ExecuteAsync(sql, record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄檔案上傳失敗: FileName={FileName}", record.FileName);
                throw;
            }
        }

        #endregion
    }
}
