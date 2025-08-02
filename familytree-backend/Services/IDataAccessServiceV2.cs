using familytree_backend.Models;

namespace familytree_backend.Services
{
    /// <summary>
    /// 資料存取服務介面 V2
    /// 基於 user_id 的資料隔離版本
    /// </summary>
    public interface IDataAccessServiceV2
    {
        #region 人員資料操作

        /// <summary>
        /// 獲取人員資料列表（帶分頁和搜尋）
        /// </summary>
        Task<(IEnumerable<PersonDataModel> Data, int TotalCount)> GetPersonDataListAsync(
            string userId,
            string userRole,
            int page, 
            int pageSize, 
            string? keyword = null,
            string? sortBy = null,
            string? sortOrder = null);

        /// <summary>
        /// 根據ID獲取人員資料
        /// </summary>
        Task<PersonDataModel?> GetPersonDataByIdAsync(int id, string userId, string userRole);
        
        /// <summary>
        /// 新增人員資料
        /// </summary>
        Task<int> AddPersonAsync(PersonDataModel person, string userId);
        
        /// <summary>
        /// 更新人員資料
        /// </summary>
        Task<bool> UpdatePersonAsync(int personId, PersonDataModel person, string userId, string userRole);
        
        /// <summary>
        /// 刪除人員資料
        /// </summary>
        Task<bool> DeletePersonAsync(int personId, string userId, string userRole);
        
        /// <summary>
        /// 搜尋人員資料
        /// </summary>
        Task<IEnumerable<PersonDataModel>> SearchPersonsAsync(string userId, string userRole, SearchRequest request);

        /// <summary>
        /// 獲取人員資料列表（優化版，包含關聯資料）
        /// </summary>
        Task<IEnumerable<PersonDataModel>> GetPersonDataListOptimizedAsync(
            string userId,
            string userRole,
            int page, 
            int pageSize, 
            string? keyword = null,
            string? sortBy = null,
            string? sortOrder = null);

        /// <summary>
        /// 批量獲取人員關係資料
        /// </summary>
        Task<IEnumerable<RelationshipDto>> GetRelationshipsByPersonIdsAsync(List<int> personIds);

        /// <summary>
        /// 批量獲取人員照片資料
        /// </summary>
        Task<IEnumerable<PhotoDto>> GetPhotosByPersonIdsAsync(List<int> personIds);

        #endregion

        #region 我的最愛操作

        /// <summary>
        /// 獲取我的最愛列表
        /// </summary>
        Task<IEnumerable<FavoriteModel>> GetFavoritesAsync(string userId);

        /// <summary>
        /// 新增我的最愛
        /// </summary>
        Task<bool> AddFavoriteAsync(string userId, int personId);

        /// <summary>
        /// 移除我的最愛
        /// </summary>
        Task<bool> RemoveFavoriteAsync(string userId, int personId);

        /// <summary>
        /// 檢查是否為我的最愛
        /// </summary>
        Task<bool> IsFavoriteAsync(string userId, int personId);

        #endregion

        #region 檔案操作

        /// <summary>
        /// 獲取使用者的檔案上傳記錄
        /// </summary>
        Task<IEnumerable<FileUploadRecord>> GetFileUploadRecordsAsync(string userId, string userRole);

        /// <summary>
        /// 新增檔案上傳記錄
        /// </summary>
        Task<bool> AddFileUploadRecordAsync(FileUploadRecord record, string userId);

        #endregion

        #region 分析操作

        /// <summary>
        /// 獲取分析會話列表
        /// </summary>
        Task<IEnumerable<AnalysisSessionModel>> GetAnalysisSessionsAsync(string userId, string userRole);

        /// <summary>
        /// 獲取分析結果
        /// </summary>
        Task<AnalysisResultModel?> GetAnalysisResultAsync(string sessionId, string userId, string userRole);

        #endregion

        #region 欄位對應操作

        /// <summary>
        /// 獲取使用者的欄位對應設定
        /// </summary>
        Task<IEnumerable<FieldMappingModel>> GetFieldMappingsAsync(string userId);

        /// <summary>
        /// 儲存欄位對應設定
        /// </summary>
        Task<bool> SaveFieldMappingsAsync(string userId, IEnumerable<FieldMappingModel> mappings);

        #endregion

        #region 專案管理操作

        /// <summary>
        /// 獲取使用者的專案列表
        /// </summary>
        Task<IEnumerable<ProjectModel>> GetProjectListAsync(string userId, string userRole);

        /// <summary>
        /// 根據ID獲取專案（包含權限檢查）
        /// </summary>
        Task<ProjectModel?> GetProjectByIdAsync(string projectId, string userId, string userRole);

        /// <summary>
        /// 建立專案
        /// </summary>
        Task<string?> CreateProjectAsync(CreateProjectRequest request, string userId);

        /// <summary>
        /// 更新專案
        /// </summary>
        Task<bool> UpdateProjectAsync(string projectId, UpdateProjectRequest request, string userId, string userRole);

        /// <summary>
        /// 刪除專案
        /// </summary>
        Task<bool> DeleteProjectAsync(string projectId, string userId, string userRole);

        #endregion

        #region 搜尋操作

        /// <summary>
        /// 搜尋人員資料（基於用戶權限）
        /// </summary>
        Task<(IEnumerable<PersonSearchResult> Data, int TotalCount)> SearchPersonDataAsync(string userId, string userRole, SearchRequest request);

        /// <summary>
        /// 記錄搜尋關鍵字
        /// </summary>
        Task RecordSearchKeywordAsync(string keyword, string searchType, string userId);

        /// <summary>
        /// 獲取搜尋歷史
        /// </summary>
        Task<List<string>> GetSearchHistoryAsync(string userId, int limit = 20);

        /// <summary>
        /// 獲取人員關係（基於用戶權限）
        /// </summary>
        Task<IEnumerable<RelationshipData>> GetPersonRelationshipsAsync(int personId, string userId, string userRole);

        /// <summary>
        /// 建立關係
        /// </summary>
        Task<bool> CreateRelationshipAsync(RelationshipData relationship, string userId);

        #endregion

        #region 通用查詢

        /// <summary>
        /// 執行通用查詢（會自動加入 user_id 過濾）
        /// </summary>
        Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object? parameters = null, string? userId = null, string? userRole = null);
        
        /// <summary>
        /// 執行標量查詢
        /// </summary>
        Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);

        /// <summary>
        /// 執行非查詢命令（會自動加入 user_id 過濾）
        /// </summary>
        Task<int> ExecuteAsync(string sql, object? parameters = null, string? userId = null, string? userRole = null);

        /// <summary>
        /// 檢查資源擁有權
        /// </summary>
        Task<bool> CheckResourceOwnershipAsync(string resourceType, string resourceId, string userId, string userRole);

        #endregion
    }
}