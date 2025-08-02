namespace familytree_backend.Models
{
    /// <summary>
    /// Excel 處理結果模型
    /// </summary>
    public class ExcelProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? FileMd5 { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int DuplicateCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<ProcessedPersonData> ProcessedData { get; set; } = new List<ProcessedPersonData>();
        public List<ExcelRowResult> SuccessRows { get; set; } = new List<ExcelRowResult>();
        public List<ExcelRowResult> FailedRows { get; set; } = new List<ExcelRowResult>();
        public List<string> UnmappedFields { get; set; } = new List<string>();
    }

    /// <summary>
    /// 已處理的人員資料
    /// </summary>
    public class ProcessedPersonData
    {
        public int RowNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public PersonDataModel? PersonData { get; set; }
    }

    /// <summary>
    /// Excel 行處理結果
    /// </summary>
    public class ExcelRowResult
    {
        public int RowNumber { get; set; }
        public Dictionary<string, object?> Data { get; set; } = new Dictionary<string, object?>();
        public string? Error { get; set; }
        public bool IsSuccess => string.IsNullOrEmpty(Error);
    }
}