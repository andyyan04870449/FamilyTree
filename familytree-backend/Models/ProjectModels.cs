// 專案管理相關模型 - 定義專案管理相關的資料結構
using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
    /// <summary>
    /// 專案資料模型
    /// </summary>
    public class ProjectModel
    {
        public string Id { get; set; } = string.Empty;                    // 專案ID (userID-YYYYMMDDHHMMSS)
        public string UserId { get; set; } = string.Empty;               // 建立者用戶ID
        public string ProjectName { get; set; } = string.Empty;          // 專案名稱
        public string? ProjectDescription { get; set; }                  // 專案描述
        public string Status { get; set; } = "active";                   // 專案狀態 (active, completed, archived, deleted)
        public DateTime CreatedAt { get; set; }                          // 建立時間
        public DateTime? CompletedAt { get; set; }                       // 完成時間
        public DateTime UpdatedAt { get; set; }                          // 更新時間
        
        // 統計資料 (由查詢計算得出)
        public int MemberCount { get; set; }                             // 成員數量
        public int RelationshipCount { get; set; }                       // 關係數量
        
        // 計算進度百分比
        public int Progress 
        { 
            get 
            {
                // 簡單的進度計算邏輯，基於成員和關係數量
                if (MemberCount == 0) return 0;
                if (RelationshipCount == 0) return MemberCount > 0 ? 25 : 0;
                
                // 基礎進度 + 關係數據加成
                var baseProgress = Math.Min(50, MemberCount * 2);
                var relationshipProgress = Math.Min(50, RelationshipCount * 5);
                
                return Math.Min(100, baseProgress + relationshipProgress);
            }
        }
        
        // 格式化的標籤 (從描述中提取或預設)
        public List<string> Tags
        {
            get
            {
                var tags = new List<string>();
                
                // 根據狀態添加標籤
                switch (Status)
                {
                    case "active":
                        tags.Add("進行中");
                        break;
                    case "completed":
                        tags.Add("已完成");
                        break;
                    case "archived":
                        tags.Add("已封存");
                        break;
                }
                
                // 根據成員數量添加標籤
                if (MemberCount > 100)
                    tags.Add("大型專案");
                else if (MemberCount > 50)
                    tags.Add("中型專案");
                else if (MemberCount > 0)
                    tags.Add("小型專案");
                
                return tags;
            }
        }
    }

    /// <summary>
    /// 建立專案請求模型
    /// </summary>
    public class CreateProjectRequest
    {
        [Required(ErrorMessage = "專案名稱為必填項目")]
        [StringLength(200, ErrorMessage = "專案名稱不能超過200個字元")]
        public string ProjectName { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "專案描述不能超過1000個字元")]
        public string? ProjectDescription { get; set; }
        
        [Required(ErrorMessage = "用戶ID為必填項目")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "用戶ID必須為6位數字")]
        public string UserId { get; set; } = string.Empty;
        
        [RegularExpression("^(active|completed|archived|draft)$", ErrorMessage = "無效的專案狀態")]
        public string? Status { get; set; } = "active";
    }

    /// <summary>
    /// 更新專案請求模型
    /// </summary>
    public class UpdateProjectRequest
    {
        [Required(ErrorMessage = "專案名稱為必填項目")]
        [StringLength(200, ErrorMessage = "專案名稱不能超過200個字元")]
        public string ProjectName { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "專案描述不能超過1000個字元")]
        public string? ProjectDescription { get; set; }
        
        [RegularExpression("^(active|completed|archived)$", ErrorMessage = "無效的專案狀態")]
        public string? Status { get; set; }
    }



    /// <summary>
    /// 專案搜尋參數模型
    /// </summary>
    public class ProjectSearchParams
    {
        public string? Status { get; set; }                 // 狀態篩選
        public string? Search { get; set; }                 // 搜尋關鍵字
        public int PageNumber { get; set; } = 1;            // 頁碼
        public int PageSize { get; set; } = 50;             // 每頁筆數
        public string SortBy { get; set; } = "created_at";  // 排序欄位
        public string SortOrder { get; set; } = "desc";     // 排序方向
    }


} 