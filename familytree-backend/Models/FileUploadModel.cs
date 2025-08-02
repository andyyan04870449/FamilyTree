// 檔案上傳模型 - 定義檔案上傳相關的資料結構 (舊版，已棄用)
// ⚠️ 已棄用：請使用新的 FileModel，此模型將在 v2.1 中移除
using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
    [Obsolete("請使用新的 FileModel。此模型使用舊的 project_id 命名模式，將在 v2.1 中移除。", false)]
    public class FileUploadModel
    {
        public int Id { get; set; }
        
        [Required]
        public string Filename { get; set; } = string.Empty;
        
        [Required]
        public string OriginalFilename { get; set; } = string.Empty;
        
        [Required]
        public string FilePath { get; set; } = string.Empty;
        
        public long FileSize { get; set; }
        
        [Required]
        public string Md5Hash { get; set; } = string.Empty;
        
        public DateTime UploadTime { get; set; }
        
        public bool IsMerged { get; set; }
        
        public DateTime? MergeTime { get; set; }
        
        public string Status { get; set; } = "uploaded";
        
        public string? ProjectId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }

    public class DeleteImpactResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PersonCount { get; set; }
        public List<string> PersonNames { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
        public bool HasMorePersons { get; set; }
    }


    public class FileData
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadTime { get; set; }
    }

    public class FileProcessResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public FileUploadResult? ProcessResult { get; set; }
    }

    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? FilePath { get; set; }
        public string? Md5Hash { get; set; }
        public bool IsDuplicate { get; set; }
        public int ProcessedRows { get; set; }
        public int SuccessRows { get; set; }
        public int ErrorRows { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime ProcessTime { get; set; }
    }
} 