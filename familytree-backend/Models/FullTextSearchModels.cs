// 全文檢索功能資料模型
// 此檔案的目的：定義全文檢索功能所需的所有資料模型，包括搜索請求、結果、收藏等

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace familytree_backend.Models
{
    /// <summary>
    /// 搜索請求模型
    /// </summary>
    public class SearchRequest
    {
        [Required(ErrorMessage = "搜索關鍵字不能為空")]
        [StringLength(255, ErrorMessage = "關鍵字長度不能超過255個字符")]
        public string Keyword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "搜索類型不能為空")]
        public string SearchType { get; set; } = "fuzzy"; // exact: 精準查詢, fuzzy: 模糊查詢
        
        public int Page { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// 搜索結果模型
    /// </summary>
    public class SearchResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public SearchData? Data { get; set; }
    }

    /// <summary>
    /// 搜索資料模型
    /// </summary>
    public class SearchData
    {
        public string Keyword { get; set; } = string.Empty;
        public string SearchType { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<PersonSearchResult> Results { get; set; } = new List<PersonSearchResult>();
        public List<string> PopularKeywords { get; set; } = new List<string>();
        public List<string> SearchHistory { get; set; } = new List<string>();
    }

    /// <summary>
    /// 人員搜索結果模型
    /// </summary>
    public class PersonSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Birthday { get; set; } = string.Empty;
        public string Nationality { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? IdNumber { get; set; }
        public string? PassportNumber { get; set; }
        public string? FamilyRelationships { get; set; }
        public string? Friends { get; set; }
        public string? ProfileData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // 附加欄位
        public bool IsFavorited { get; set; } = false; // 是否已收藏
        public string Source { get; set; } = "分公司客戶基資表"; // 資料來源
        public string MatchedFields { get; set; } = string.Empty; // 匹配的欄位
    }

    /// <summary>
    /// 搜索關鍵字記錄模型
    /// </summary>
    public class SearchKeyword
    {
        public int Id { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public int SearchCount { get; set; }
        public string SearchType { get; set; } = string.Empty;
        public DateTime LastSearchTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 用戶收藏模型
    /// </summary>
    public class UserFavorite
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public DateTime? LastViewedTime { get; set; }
        public DateTime FavoritedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 收藏請求模型
    /// </summary>
    public class FavoriteRequest
    {
        [Required(ErrorMessage = "人員ID不能為空")]
        public int PersonId { get; set; }
        
        [Required(ErrorMessage = "人員姓名不能為空")]
        [StringLength(100, ErrorMessage = "人員姓名長度不能超過100個字符")]
        public string PersonName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 收藏操作結果模型
    /// </summary>
    public class FavoriteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public FavoriteData? Data { get; set; }
    }

    /// <summary>
    /// 收藏資料模型
    /// </summary>
    public class FavoriteData
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public DateTime FavoritedAt { get; set; }
    }

    /// <summary>
    /// 收藏列表回應模型
    /// </summary>
    public class FavoriteListResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FavoriteItem> Data { get; set; } = new List<FavoriteItem>();
    }

    /// <summary>
    /// 收藏項目模型
    /// </summary>
    public class FavoriteItem
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string PersonName { get; set; } = string.Empty;
        public DateTime? LastViewedTime { get; set; }
        public DateTime FavoritedAt { get; set; }
        public string DisplayTime { get; set; } = string.Empty; // 用於前端顯示的時間
        public bool CanDelete { get; set; } = true;
    }

    /// <summary>
    /// 熱門關鍵字模型
    /// </summary>
    public class PopularKeyword
    {
        public string Keyword { get; set; } = string.Empty;
        public int SearchCount { get; set; }
        public DateTime LastSearchTime { get; set; }
        public string PopularityLevel { get; set; } = string.Empty; // 熱門程度：熱門、常用、一般
    }

    /// <summary>
    /// 搜索統計模型
    /// </summary>
    public class SearchStatistics
    {
        public int TotalSearches { get; set; }
        public int UniqueKeywords { get; set; }
        public int TotalFavorites { get; set; }
        public List<PopularKeyword> TopKeywords { get; set; } = new List<PopularKeyword>();
        public Dictionary<string, int> SearchTypeStats { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// 搜索日誌模型
    /// </summary>
    public class SearchLog
    {
        public int Id { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public string SearchType { get; set; } = string.Empty;
        public int ResultCount { get; set; }
        public DateTime SearchTime { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// API 基礎回應模型
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public static ApiResponse<T> SuccessResult(T data, string message = "操作成功")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }
        
        public static ApiResponse<T> ErrorResult(string message, T? data = default)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = data
            };
        }
    }
} 