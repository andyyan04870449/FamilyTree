// 視覺化分析相關模型 - 定義視覺化分析圖的資料結構
using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
    /// <summary>
    /// 視覺化分析圖資料模型
    /// </summary>
    public class VisualAnalysisGraphModel
    {
        public int Id { get; set; }                                      // 分析圖ID
        public string Name { get; set; } = string.Empty;                // 分析圖名稱
        public string ProjectIds { get; set; } = string.Empty;          // 專案ID列表（逗號分隔）
        public string UpdatedBy { get; set; } = "user";                 // 最後更新人
        public DateTime UpdatedAt { get; set; }                         // 最後更新時間
        
        // 計算屬性
        public List<string> ProjectIdsList => 
            string.IsNullOrEmpty(ProjectIds) ? new List<string>() : ProjectIds.Split(',').ToList();
        
        public int RelationCount { get; set; }                          // 關聯人數（計算得出）
        public List<string> Cases { get; set; } = new List<string>();   // 案件名稱列表（計算得出）
    }

    /// <summary>
    /// 創建視覺化分析圖請求模型
    /// </summary>
    public class CreateVisualAnalysisGraphRequest
    {
        [Required(ErrorMessage = "分析圖名稱不能為空")]
        [StringLength(255, ErrorMessage = "分析圖名稱長度不能超過255字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "至少需要選擇一個專案")]
        public List<string> ProjectIds { get; set; } = new List<string>();
        
        public string UpdatedBy { get; set; } = "user";
    }

    /// <summary>
    /// 更新視覺化分析圖請求模型
    /// </summary>
    public class UpdateVisualAnalysisGraphRequest
    {
        [Required(ErrorMessage = "分析圖名稱不能為空")]
        [StringLength(255, ErrorMessage = "分析圖名稱長度不能超過255字符")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "至少需要選擇一個專案")]
        public List<string> ProjectIds { get; set; } = new List<string>();
        
        public string UpdatedBy { get; set; } = "user";
    }

    /// <summary>
    /// 視覺化分析圖列表響應模型
    /// </summary>
    public class VisualAnalysisGraphListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<VisualAnalysisGraphModel> Graphs { get; set; } = new List<VisualAnalysisGraphModel>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// 通用API響應模型
    /// </summary>
    public class VisualAnalysisApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    /// <summary>
    /// 視覺化分析圖表節點模型
    /// </summary>
    public class VisualAnalysisNodeModel
    {
        public int Id { get; set; }
        public int GraphId { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public int PersonId { get; set; }
        public bool IsVisible { get; set; } = true;
        public float NodeX { get; set; } = 0;
        public float NodeY { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 關聯的人員資料
        public string PersonName { get; set; } = string.Empty;
        public string PersonGender { get; set; } = "男";
        public string ProjectName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 視覺化分析關係模型
    /// </summary>
    public class VisualAnalysisRelationshipModel
    {
        public int Id { get; set; }
        public int SourcePersonId { get; set; }
        public int TargetPersonId { get; set; }
        public string RelationType { get; set; } = string.Empty;
        public int? VisualAnalysisGraphId { get; set; }
        public string SourcePersonName { get; set; } = string.Empty;
        public string TargetPersonName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 圖表編輯器資料回應
    /// </summary>
    public class VisualAnalysisEditorResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public VisualAnalysisGraphModel? Graph { get; set; }
        public List<VisualAnalysisNodeModel> Nodes { get; set; } = new List<VisualAnalysisNodeModel>();
        public List<VisualAnalysisRelationshipModel> Relationships { get; set; } = new List<VisualAnalysisRelationshipModel>();
        public List<ProjectNodeGroup> ProjectGroups { get; set; } = new List<ProjectNodeGroup>();
    }

    /// <summary>
    /// 專案節點群組
    /// </summary>
    public class ProjectNodeGroup
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public List<PersonNode> Persons { get; set; } = new List<PersonNode>();
    }

    /// <summary>
    /// 人員節點
    /// </summary>
    public class PersonNode
    {
        public int PersonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// 更新節點可見性請求
    /// </summary>
    public class UpdateNodeVisibilityRequest
    {
        public int GraphId { get; set; }
        public List<NodeVisibilityUpdate> Updates { get; set; } = new List<NodeVisibilityUpdate>();
    }

    /// <summary>
    /// 節點可見性更新
    /// </summary>
    public class NodeVisibilityUpdate
    {
        public string ProjectId { get; set; } = string.Empty;
        public int PersonId { get; set; }
        public bool IsVisible { get; set; }
    }
} 