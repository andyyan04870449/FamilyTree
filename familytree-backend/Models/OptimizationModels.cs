using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
    /// <summary>
    /// 關係資料傳輸物件
    /// </summary>
    public class RelationshipDto
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public int RelatedPersonId { get; set; }
        public string RelationshipType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsConfirmed { get; set; }
        public string? PersonName { get; set; }
        public string? RelatedPersonName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 照片資料傳輸物件
    /// </summary>
    public class PhotoDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public string? Md5Hash { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    /// <summary>
    /// 批量查詢請求
    /// </summary>
    public class BatchQueryRequest
    {
        public List<int> PersonIds { get; set; } = new List<int>();
        public bool IncludeRelationships { get; set; } = false;
        public bool IncludePhotos { get; set; } = false;
        public bool IncludeFiles { get; set; } = false;
    }

    /// <summary>
    /// 優化版人員資料模型（包含關聯資料）
    /// </summary>
    public class PersonDataOptimizedModel : PersonDataModel
    {
        public List<RelationshipDto> Relationships { get; set; } = new List<RelationshipDto>();
        public List<PhotoDto> Photos { get; set; } = new List<PhotoDto>();
        public List<PhotoDto> Files { get; set; } = new List<PhotoDto>();
    }

    /// <summary>
    /// 查詢性能統計
    /// </summary>
    public class QueryPerformanceStats
    {
        public string QueryName { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public int ResultCount { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// 快取統計資訊
    /// </summary>
    public class CacheStats
    {
        public string CacheKey { get; set; } = string.Empty;
        public bool Hit { get; set; }
        public TimeSpan? RetrievalTime { get; set; }
        public int? ItemCount { get; set; }
        public DateTime RequestTime { get; set; }
    }
}