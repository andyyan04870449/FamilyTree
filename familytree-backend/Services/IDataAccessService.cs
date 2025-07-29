using familytree_backend.Models;

namespace familytree_backend.Services
{
    /// <summary>
    /// 資料存取服務介面
    /// 提供統一的資料庫存取方法
    /// </summary>
    public interface IDataAccessService
    {
        /// <summary>
        /// 獲取人員資料列表
        /// </summary>
        Task<IEnumerable<PersonDataModel>> GetPersonDataAsync(string projectId, int page = 1, int pageSize = 10);
        
        /// <summary>
        /// 根據ID獲取人員資料
        /// </summary>
        Task<PersonDataModel?> GetPersonByIdAsync(int personId, string projectId);
        
        /// <summary>
        /// 新增人員資料
        /// </summary>
        Task<bool> AddPersonAsync(PersonDataModel person, string projectId);
        
        /// <summary>
        /// 更新人員資料
        /// </summary>
        Task<bool> UpdatePersonAsync(PersonDataModel person, string projectId);
        
        /// <summary>
        /// 刪除人員資料
        /// </summary>
        Task<bool> DeletePersonAsync(int personId, string projectId);
        
        /// <summary>
        /// 搜尋人員資料
        /// </summary>
        Task<IEnumerable<PersonDataModel>> SearchPersonsAsync(SearchRequest request);
        
        /// <summary>
        /// 獲取人員關係
        /// </summary>
        Task<IEnumerable<RelationshipData>> GetPersonRelationshipsAsync(int personId, string projectId);
        
        /// <summary>
        /// 獲取專案列表
        /// </summary>
        Task<IEnumerable<ProjectModel>> GetProjectListAsync(string userId);
        
        /// <summary>
        /// 通過 ID 獲取專案
        /// </summary>
        Task<ProjectModel?> GetProjectByIdAsync(string projectId);
        
        /// <summary>
        /// 建立專案
        /// </summary>
        Task<string?> CreateProjectAsync(CreateProjectRequest request, string userId);
        
        /// <summary>
        /// 更新專案
        /// </summary>
        Task<bool> UpdateProjectAsync(string projectId, UpdateProjectRequest request);
        
        /// <summary>
        /// 刪除專案
        /// </summary>
        Task<bool> DeleteProjectAsync(string projectId, string userId);
        
        /// <summary>
        /// 獲取檔案上傳記錄
        /// </summary>
        Task<IEnumerable<FileUploadRecord>> GetFileUploadRecordsAsync(string projectId);
        
        /// <summary>
        /// 執行通用查詢
        /// </summary>
        Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object? parameters = null);
        
        /// <summary>
        /// 執行標量查詢
        /// </summary>
        Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);

        /// <summary>
        /// 獲取人員資料列表（帶分頁和搜尋）
        /// </summary>
        Task<(IEnumerable<PersonDataModel> Data, int TotalCount)> GetPersonDataListAsync(
            string projectId, 
            int page, 
            int pageSize, 
            string? keyword = null,
            string? sortBy = null,
            string? sortOrder = null);

        /// <summary>
        /// 根據ID獲取人員資料
        /// </summary>
        Task<PersonDataModel?> GetPersonDataByIdAsync(int id, string projectId);

        /// <summary>
        /// 建立人員資料
        /// </summary>
        Task<int> CreatePersonDataAsync(PersonDataModel personData);

        /// <summary>
        /// 更新人員資料
        /// </summary>
        Task<bool> UpdatePersonDataAsync(PersonDataModel personData);

        /// <summary>
        /// 刪除人員資料
        /// </summary>
        Task<bool> DeletePersonDataAsync(int id, string projectId);

        /// <summary>
        /// 搜尋人員資料
        /// </summary>
        Task<(IEnumerable<PersonSearchResult> Data, int TotalCount)> SearchPersonDataAsync(SearchRequest request);

        /// <summary>
        /// 記錄搜尋關鍵字
        /// </summary>
        Task RecordSearchKeywordAsync(string keyword, string searchType, string? projectId = null);

        /// <summary>
        /// 獲取熱門關鍵字
        /// </summary>
        Task<List<string>> GetPopularKeywordsAsync(string? projectId = null, int limit = 10);

        /// <summary>
        /// 獲取搜尋歷史
        /// </summary>
        Task<List<string>> GetSearchHistoryAsync(string? projectId = null, int limit = 20);

        /// <summary>
        /// 建立關係
        /// </summary>
        Task<bool> CreateRelationshipAsync(RelationshipData relationship);

        /// <summary>
        /// 檢查檔案是否存在
        /// </summary>
        Task<bool> FileExistsAsync(string md5Hash);

        /// <summary>
        /// 記錄檔案上傳
        /// </summary>
        Task RecordFileUploadAsync(FileUploadRecord record);    }

    /// <summary>
    /// 關係資料模型
    /// </summary>
    public class RelationshipData
    {
        public int SourcePersonId { get; set; }
        public int TargetPersonId { get; set; }
        public string RelationType { get; set; } = string.Empty;
        public string RelationshipType { get; set; } = string.Empty;
        public string? SourceField { get; set; }
        public string? ProjectId { get; set; }
        public int? VisualAnalysisGraphId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 檔案上傳記錄模型
    /// </summary>
    public class FileUploadRecord
    {
        public string FileName { get; set; } = string.Empty;
        public string Md5Hash { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ProjectId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public DateTime UploadTime { get; set; }
    }
}
