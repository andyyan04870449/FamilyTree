// 檔案上傳模型 - 定義檔案上傳相關的資料結構
using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
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
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }

    public class FileUploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }

    public class FileUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public FileUploadModel? FileInfo { get; set; }
        public bool IsDuplicate { get; set; }
    }

    public class FileListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FileUploadModel> Files { get; set; } = new();
        public int TotalCount { get; set; }
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
} 