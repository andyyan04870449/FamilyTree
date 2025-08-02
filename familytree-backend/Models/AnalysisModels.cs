namespace familytree_backend.Models
{
    /// <summary>
    /// 分析會話模型
    /// </summary>
    public class AnalysisSessionModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string SessionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Parameters { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 分析結果模型
    /// </summary>
    public class AnalysisResultModel
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string ResultType { get; set; } = string.Empty;
        public string ResultData { get; set; } = string.Empty;
        public double? ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 欄位對應模型
    /// </summary>
    public class FieldMappingModel
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public string DbFieldName { get; set; } = string.Empty;  // 資料庫欄位名稱
        public string ExcelFieldName { get; set; } = string.Empty;  // Excel欄位名稱
        public string? FieldType { get; set; }
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? ValidationRules { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}