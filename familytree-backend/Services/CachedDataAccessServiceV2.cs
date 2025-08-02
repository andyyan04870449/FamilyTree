using Microsoft.Extensions.Caching.Memory;
using familytree_backend.Models;

namespace familytree_backend.Services
{
    /// <summary>
    /// 快取版資料存取服務 V2
    /// 在原有 DataAccessServiceV2 基礎上增加快取功能
    /// </summary>
    public class CachedDataAccessServiceV2 : IDataAccessServiceV2
    {
        private readonly IDataAccessServiceV2 _baseService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedDataAccessServiceV2> _logger;

        // 快取設定
        private readonly MemoryCacheEntryOptions _shortCacheOptions;
        private readonly MemoryCacheEntryOptions _mediumCacheOptions;
        private readonly MemoryCacheEntryOptions _longCacheOptions;

        public CachedDataAccessServiceV2(
            IDataAccessServiceV2 baseService,
            IMemoryCache cache,
            ILogger<CachedDataAccessServiceV2> logger)
        {
            _baseService = baseService;
            _cache = cache;
            _logger = logger;

            // 設定不同的快取策略
            _shortCacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(2),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Size = 100,
                Priority = CacheItemPriority.High
            };

            _mediumCacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(5),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                Size = 50,
                Priority = CacheItemPriority.Normal
            };

            _longCacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(15),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                Size = 20,
                Priority = CacheItemPriority.Low
            };
        }

        #region 人員資料操作

        public async Task<(IEnumerable<PersonDataModel> Data, int TotalCount)> GetPersonDataListAsync(
            string userId, string userRole, int page, int pageSize, 
            string? keyword = null, string? sortBy = null, string? sortOrder = null)
        {
            var cacheKey = $"person_list_{userId}_{userRole}_{page}_{pageSize}_{keyword}_{sortBy}_{sortOrder}";
            
            if (_cache.TryGetValue(cacheKey, out (IEnumerable<PersonDataModel> Data, int TotalCount) cached))
            {
                _logger.LogInformation("快取命中：人員列表查詢 - 快取鍵：{CacheKey}", cacheKey);
                return cached;
            }

            _logger.LogInformation("快取未命中：人員列表查詢 - 快取鍵：{CacheKey}", cacheKey);
            var result = await _baseService.GetPersonDataListAsync(userId, userRole, page, pageSize, keyword, sortBy, sortOrder);
            
            _cache.Set(cacheKey, result, _shortCacheOptions);
            return result;
        }

        public async Task<PersonDataModel?> GetPersonDataByIdAsync(int id, string userId, string userRole)
        {
            var cacheKey = $"person_{id}_{userId}_{userRole}";
            
            if (_cache.TryGetValue(cacheKey, out PersonDataModel? cached))
            {
                _logger.LogInformation("快取命中：人員詳情查詢 - ID：{Id}", id);
                return cached;
            }

            _logger.LogInformation("快取未命中：人員詳情查詢 - ID：{Id}", id);
            var result = await _baseService.GetPersonDataByIdAsync(id, userId, userRole);
            
            if (result != null)
            {
                _cache.Set(cacheKey, result, _mediumCacheOptions);
            }
            
            return result;
        }

        public async Task<IEnumerable<PersonDataModel>> GetPersonDataListOptimizedAsync(
            string userId, string userRole, int page, int pageSize, 
            string? keyword = null, string? sortBy = null, string? sortOrder = null)
        {
            var cacheKey = $"person_list_optimized_{userId}_{userRole}_{page}_{pageSize}_{keyword}_{sortBy}_{sortOrder}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<PersonDataModel>? cached))
            {
                _logger.LogInformation("快取命中：優化版人員列表查詢 - 快取鍵：{CacheKey}", cacheKey);
                return cached;
            }

            _logger.LogInformation("快取未命中：優化版人員列表查詢 - 快取鍵：{CacheKey}", cacheKey);
            var result = await _baseService.GetPersonDataListOptimizedAsync(userId, userRole, page, pageSize, keyword, sortBy, sortOrder);
            
            _cache.Set(cacheKey, result, _shortCacheOptions);
            return result;
        }

        public async Task<IEnumerable<RelationshipDto>> GetRelationshipsByPersonIdsAsync(List<int> personIds)
        {
            var cacheKey = $"relationships_{string.Join(",", personIds.OrderBy(x => x))}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<RelationshipDto>? cached))
            {
                _logger.LogInformation("快取命中：批量關係查詢 - 人員數量：{Count}", personIds.Count);
                return cached;
            }

            _logger.LogInformation("快取未命中：批量關係查詢 - 人員數量：{Count}", personIds.Count);
            var result = await _baseService.GetRelationshipsByPersonIdsAsync(personIds);
            
            _cache.Set(cacheKey, result, _mediumCacheOptions);
            return result;
        }

        public async Task<IEnumerable<PhotoDto>> GetPhotosByPersonIdsAsync(List<int> personIds)
        {
            var cacheKey = $"photos_{string.Join(",", personIds.OrderBy(x => x))}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<PhotoDto>? cached))
            {
                _logger.LogInformation("快取命中：批量照片查詢 - 人員數量：{Count}", personIds.Count);
                return cached;
            }

            _logger.LogInformation("快取未命中：批量照片查詢 - 人員數量：{Count}", personIds.Count);
            var result = await _baseService.GetPhotosByPersonIdsAsync(personIds);
            
            _cache.Set(cacheKey, result, _longCacheOptions);
            return result;
        }

        public async Task<int> AddPersonAsync(PersonDataModel person, string userId)
        {
            var result = await _baseService.AddPersonAsync(person, userId);
            
            // 清除相關快取
            ClearPersonCaches(userId);
            
            return result;
        }

        public async Task<bool> UpdatePersonAsync(int personId, PersonDataModel person, string userId, string userRole)
        {
            var result = await _baseService.UpdatePersonAsync(personId, person, userId, userRole);
            
            if (result)
            {
                // 清除相關快取
                ClearPersonCaches(userId);
                _cache.Remove($"person_{personId}_{userId}_{userRole}");
            }
            
            return result;
        }

        public async Task<bool> DeletePersonAsync(int personId, string userId, string userRole)
        {
            var result = await _baseService.DeletePersonAsync(personId, userId, userRole);
            
            if (result)
            {
                // 清除相關快取
                ClearPersonCaches(userId);
                _cache.Remove($"person_{personId}_{userId}_{userRole}");
            }
            
            return result;
        }

        public async Task<IEnumerable<PersonDataModel>> SearchPersonsAsync(string userId, string userRole, SearchRequest request)
        {
            var cacheKey = $"search_{userId}_{userRole}_{request.Keyword}_{request.SearchType}_{request.Page}_{request.PageSize}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<PersonDataModel>? cached))
            {
                _logger.LogInformation("快取命中：搜尋查詢 - 關鍵字：{Keyword}", request.Keyword);
                return cached;
            }

            _logger.LogInformation("快取未命中：搜尋查詢 - 關鍵字：{Keyword}", request.Keyword);
            var result = await _baseService.SearchPersonsAsync(userId, userRole, request);
            
            _cache.Set(cacheKey, result, _shortCacheOptions);
            return result;
        }

        #endregion

        #region 我的最愛操作

        public async Task<IEnumerable<FavoriteModel>> GetFavoritesAsync(string userId)
        {
            var cacheKey = $"favorites_{userId}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<FavoriteModel>? cached))
            {
                _logger.LogInformation("快取命中：我的最愛查詢 - 使用者：{UserId}", userId);
                return cached;
            }

            var result = await _baseService.GetFavoritesAsync(userId);
            _cache.Set(cacheKey, result, _mediumCacheOptions);
            return result;
        }

        public async Task<bool> AddFavoriteAsync(string userId, int personId)
        {
            var result = await _baseService.AddFavoriteAsync(userId, personId);
            
            if (result)
            {
                _cache.Remove($"favorites_{userId}");
            }
            
            return result;
        }

        public async Task<bool> RemoveFavoriteAsync(string userId, int personId)
        {
            var result = await _baseService.RemoveFavoriteAsync(userId, personId);
            
            if (result)
            {
                _cache.Remove($"favorites_{userId}");
            }
            
            return result;
        }

        public async Task<bool> IsFavoriteAsync(string userId, int personId)
        {
            return await _baseService.IsFavoriteAsync(userId, personId);
        }

        #endregion

        #region 檔案操作

        public async Task<IEnumerable<FileUploadRecord>> GetFileUploadRecordsAsync(string userId, string userRole)
        {
            var cacheKey = $"file_records_{userId}_{userRole}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<FileUploadRecord>? cached))
            {
                return cached;
            }

            var result = await _baseService.GetFileUploadRecordsAsync(userId, userRole);
            _cache.Set(cacheKey, result, _mediumCacheOptions);
            return result;
        }

        public async Task<bool> AddFileUploadRecordAsync(FileUploadRecord record, string userId)
        {
            var result = await _baseService.AddFileUploadRecordAsync(record, userId);
            
            if (result)
            {
                _cache.Remove($"file_records_{userId}_user");
                _cache.Remove($"file_records_{userId}_admin");
            }
            
            return result;
        }

        #endregion

        #region 分析操作

        public async Task<IEnumerable<AnalysisSessionModel>> GetAnalysisSessionsAsync(string userId, string userRole)
        {
            return await _baseService.GetAnalysisSessionsAsync(userId, userRole);
        }

        public async Task<AnalysisResultModel?> GetAnalysisResultAsync(string sessionId, string userId, string userRole)
        {
            var cacheKey = $"analysis_result_{sessionId}_{userId}_{userRole}";
            
            if (_cache.TryGetValue(cacheKey, out AnalysisResultModel? cached))
            {
                return cached;
            }

            var result = await _baseService.GetAnalysisResultAsync(sessionId, userId, userRole);
            
            if (result != null)
            {
                _cache.Set(cacheKey, result, _longCacheOptions);
            }
            
            return result;
        }

        #endregion

        #region 欄位對應操作

        public async Task<IEnumerable<FieldMappingModel>> GetFieldMappingsAsync(string userId)
        {
            return await _baseService.GetFieldMappingsAsync(userId);
        }

        public async Task<bool> SaveFieldMappingsAsync(string userId, IEnumerable<FieldMappingModel> mappings)
        {
            return await _baseService.SaveFieldMappingsAsync(userId, mappings);
        }

        #endregion

        #region 專案管理操作

        public async Task<IEnumerable<ProjectModel>> GetProjectListAsync(string userId, string userRole)
        {
            var cacheKey = $"project_list_{userId}_{userRole}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<ProjectModel>? cached))
            {
                _logger.LogInformation("快取命中：專案列表查詢 - 使用者：{UserId}", userId);
                return cached;
            }

            var result = await _baseService.GetProjectListAsync(userId, userRole);
            _cache.Set(cacheKey, result, _mediumCacheOptions);
            return result;
        }

        public async Task<ProjectModel?> GetProjectByIdAsync(string projectId, string userId, string userRole)
        {
            var cacheKey = $"project_{projectId}_{userId}_{userRole}";
            
            if (_cache.TryGetValue(cacheKey, out ProjectModel? cached))
            {
                _logger.LogInformation("快取命中：專案詳情查詢 - 專案：{ProjectId}", projectId);
                return cached;
            }

            var result = await _baseService.GetProjectByIdAsync(projectId, userId, userRole);
            
            if (result != null)
            {
                _cache.Set(cacheKey, result, _mediumCacheOptions);
            }
            
            return result;
        }

        public async Task<string?> CreateProjectAsync(CreateProjectRequest request, string userId)
        {
            var result = await _baseService.CreateProjectAsync(request, userId);
            
            if (result != null)
            {
                // 清除專案列表快取
                _cache.Remove($"project_list_{userId}_user");
                _cache.Remove($"project_list_{userId}_admin");
            }
            
            return result;
        }

        public async Task<bool> UpdateProjectAsync(string projectId, UpdateProjectRequest request, string userId, string userRole)
        {
            var result = await _baseService.UpdateProjectAsync(projectId, request, userId, userRole);
            
            if (result)
            {
                // 清除相關快取
                _cache.Remove($"project_{projectId}_{userId}_{userRole}");
                _cache.Remove($"project_list_{userId}_user");
                _cache.Remove($"project_list_{userId}_admin");
            }
            
            return result;
        }

        public async Task<bool> DeleteProjectAsync(string projectId, string userId, string userRole)
        {
            var result = await _baseService.DeleteProjectAsync(projectId, userId, userRole);
            
            if (result)
            {
                // 清除相關快取
                _cache.Remove($"project_{projectId}_{userId}_{userRole}");
                _cache.Remove($"project_list_{userId}_user");
                _cache.Remove($"project_list_{userId}_admin");
            }
            
            return result;
        }

        #endregion

        #region 搜尋操作

        public async Task<(IEnumerable<PersonSearchResult> Data, int TotalCount)> SearchPersonDataAsync(string userId, string userRole, SearchRequest request)
        {
            var cacheKey = $"search_data_{userId}_{userRole}_{request.Keyword}_{request.Page}_{request.PageSize}";
            
            if (_cache.TryGetValue(cacheKey, out (IEnumerable<PersonSearchResult> Data, int TotalCount) cached))
            {
                _logger.LogInformation("快取命中：搜尋人員資料 - 關鍵字：{Keyword}", request.Keyword);
                return cached;
            }

            var result = await _baseService.SearchPersonDataAsync(userId, userRole, request);
            _cache.Set(cacheKey, result, _shortCacheOptions);
            return result;
        }

        public async Task RecordSearchKeywordAsync(string keyword, string searchType, string userId)
        {
            await _baseService.RecordSearchKeywordAsync(keyword, searchType, userId);
        }

        public async Task<List<string>> GetSearchHistoryAsync(string userId, int limit = 20)
        {
            var cacheKey = $"search_history_{userId}_{limit}";
            
            if (_cache.TryGetValue(cacheKey, out List<string>? cached))
            {
                return cached;
            }

            var result = await _baseService.GetSearchHistoryAsync(userId, limit);
            _cache.Set(cacheKey, result, _shortCacheOptions);
            return result;
        }

        public async Task<IEnumerable<RelationshipData>> GetPersonRelationshipsAsync(int personId, string userId, string userRole)
        {
            var cacheKey = $"person_relationships_{personId}_{userId}_{userRole}";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<RelationshipData>? cached))
            {
                return cached;
            }

            var result = await _baseService.GetPersonRelationshipsAsync(personId, userId, userRole);
            _cache.Set(cacheKey, result, _mediumCacheOptions);
            return result;
        }

        public async Task<bool> CreateRelationshipAsync(RelationshipData relationship, string userId)
        {
            var result = await _baseService.CreateRelationshipAsync(relationship, userId);
            
            if (result)
            {
                // 清除相關關係快取
                _cache.Remove($"person_relationships_{relationship.SourcePersonId}_{userId}_user");
                _cache.Remove($"person_relationships_{relationship.TargetPersonId}_{userId}_user");
                _cache.Remove($"person_relationships_{relationship.SourcePersonId}_{userId}_admin");
                _cache.Remove($"person_relationships_{relationship.TargetPersonId}_{userId}_admin");
            }
            
            return result;
        }

        #endregion

        #region 通用查詢

        public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object? parameters = null, string? userId = null, string? userRole = null)
        {
            return await _baseService.ExecuteQueryAsync<T>(sql, parameters, userId, userRole);
        }

        public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
        {
            return await _baseService.ExecuteScalarAsync<T>(sql, parameters);
        }

        public async Task<int> ExecuteAsync(string sql, object? parameters = null, string? userId = null, string? userRole = null)
        {
            return await _baseService.ExecuteAsync(sql, parameters, userId, userRole);
        }

        public async Task<bool> CheckResourceOwnershipAsync(string resourceType, string resourceId, string userId, string userRole)
        {
            return await _baseService.CheckResourceOwnershipAsync(resourceType, resourceId, userId, userRole);
        }

        #endregion

        #region 快取管理

        /// <summary>
        /// 清除特定使用者的人員資料相關快取
        /// </summary>
        private void ClearPersonCaches(string userId)
        {
            var keysToRemove = new List<string>();
            
            // 由於 MemoryCache 沒有提供列舉所有鍵的方法，我們只能清除已知的模式
            // 實際應用中可以考慮使用支援鍵列舉的快取系統，如 Redis
            
            // 清除人員列表快取（不同分頁）
            for (int page = 1; page <= 10; page++) // 假設最多快取前10頁
            {
                for (int pageSize = 10; pageSize <= 100; pageSize += 10)
                {
                    keysToRemove.Add($"person_list_{userId}_user_{page}_{pageSize}___");
                    keysToRemove.Add($"person_list_{userId}_admin_{page}_{pageSize}___");
                    keysToRemove.Add($"person_list_optimized_{userId}_user_{page}_{pageSize}___");
                    keysToRemove.Add($"person_list_optimized_{userId}_admin_{page}_{pageSize}___");
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            _logger.LogInformation("已清除使用者 {UserId} 的人員資料快取", userId);
        }

        /// <summary>
        /// 獲取快取統計資訊（簡化版）
        /// </summary>
        public CacheStats GetCacheStats(string cacheKey)
        {
            var hasValue = _cache.TryGetValue(cacheKey, out _);
            
            return new CacheStats
            {
                CacheKey = cacheKey,
                Hit = hasValue,
                RequestTime = DateTime.UtcNow
            };
        }

        #endregion
    }
}