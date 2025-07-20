// 欄位對應模型 - 定義Excel欄位到資料庫欄位的對應關係
using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
    public class FieldMappingModel
    {
        public int Id { get; set; }
        
        [Required]
        public string ExcelFieldName { get; set; } = string.Empty;  // Excel表格上的欄位名稱
        
        [Required]
        public string DbFieldName { get; set; } = string.Empty;     // 資料庫中欄位的名稱
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class FieldMappingResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public FieldMappingModel? FieldMapping { get; set; }
    }

    public class FieldMappingListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FieldMappingModel> FieldMappings { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class FieldMappingRequest
    {
        [Required]
        public string ExcelFieldName { get; set; } = string.Empty;
        
        [Required]
        public string DbFieldName { get; set; } = string.Empty;
    }

    public class ExcelProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public List<string> UnmappedFields { get; set; } = new();
        public string FileMd5 { get; set; } = string.Empty;
    }
} 