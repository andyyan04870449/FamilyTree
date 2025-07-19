using System.Text.Json;

namespace familytree_backend.Models
{
    public class AnalysisResult
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public JsonDocument AnalysisResultData { get; set; }
        public DateTime AnalysisDate { get; set; }
        public int ProgressPercentage { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class RelationshipData
    {
        public string Relation { get; set; } = "";
        public string TargetName { get; set; } = "";
        public int? TargetId { get; set; }
        public string SourceField { get; set; } = ""; // family_relationships, friends, activities
    }

    public class AnalysisRequest
    {
        public int PersonId { get; set; }
        public int MaxDepth { get; set; } = 10; // 最大分析深度，預設為10層
    }

    public class AnalysisProgressResponse
    {
        public int PersonId { get; set; }
        public string PersonName { get; set; } = "";
        public int ProgressPercentage { get; set; }
        public string Status { get; set; } = "";
        public JsonDocument? AnalysisResult { get; set; }
    }

    public class AnalysisJobResponse
    {
        public int PersonId { get; set; }
        public string PersonName { get; set; } = "";
        public string Status { get; set; } = "";
        public int ProgressPercentage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CurrentStep { get; set; } // 當前執行的步驟描述
        public string? StatusMessage { get; set; } // 詳細的狀態信息
    }

    public class AnalysisResultResponse
    {
        public int PersonId { get; set; }
        public string PersonName { get; set; } = "";
        public JsonDocument AnalysisResult { get; set; }
        public DateTime AnalysisDate { get; set; }
        public string Status { get; set; } = "";
    }
} 