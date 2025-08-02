// 新檔案模型 - 使用 file_id 命名標準，支援彈性關聯機制
using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
    /// <summary>
    /// 檔案模型 - 標準化檔案管理架構
    /// 設計理念：以檔案為中心，支援彈性的關聯關係
    /// </summary>
    public class FileModel
    {
        /// <summary>
        /// 檔案唯一識別碼 (UUID)
        /// </summary>
        [Key]
        public Guid FileId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 檔案擁有者 (使用者 ID)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string UserId { get; set; } = string.Empty;

        // ==================== 檔案基本資訊 ====================

        /// <summary>
        /// 系統產生的檔案名稱 (唯一)
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Filename { get; set; } = string.Empty;

        /// <summary>
        /// 使用者上傳的原始檔案名稱
        /// </summary>
        [Required]
        [StringLength(255)]
        public string OriginalFilename { get; set; } = string.Empty;

        /// <summary>
        /// 檔案實際儲存路徑
        /// </summary>
        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 檔案大小 (位元組)
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 檔案 MD5 雜湊值 (用於重複檢查)
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Md5Hash { get; set; } = string.Empty;

        /// <summary>
        /// 檔案類型 (excel, csv, pdf, image 等)
        /// </summary>
        [StringLength(50)]
        public string? FileType { get; set; }

        /// <summary>
        /// MIME 類型
        /// </summary>
        [StringLength(100)]
        public string? MimeType { get; set; }

        // ==================== 關聯資訊 ====================

        /// <summary>
        /// 關聯記錄 ID (取代原本的 project_id)
        /// 可關聯到 person, project, analysis 等任何業務實體
        /// </summary>
        [StringLength(50)]
        public string? AssociatedRecordId { get; set; }

        /// <summary>
        /// 關聯記錄類型 (person, project, analysis, photo, document 等)
        /// </summary>
        [StringLength(50)]
        public string? AssociatedRecordType { get; set; }

        // ==================== 狀態管理 ====================

        /// <summary>
        /// 上傳狀態
        /// </summary>
        [Required]
        [StringLength(50)]
        public string UploadStatus { get; set; } = FileUploadStatus.Uploaded;

        /// <summary>
        /// 是否已處理 (針對需要處理的檔案如 Excel)
        /// </summary>
        public bool IsProcessed { get; set; } = false;

        /// <summary>
        /// 處理完成時間
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        // ==================== 審計欄位 ====================

        /// <summary>
        /// 上傳時間
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最後更新時間
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 檔案上傳狀態常數
    /// </summary>
    public static class FileUploadStatus
    {
        public const string Uploaded = "uploaded";
        public const string Processing = "processing";
        public const string Processed = "processed";
        public const string Failed = "failed";
        public const string Deleted = "deleted";
    }

    /// <summary>
    /// 關聯記錄類型常數
    /// </summary>
    public static class AssociatedRecordType
    {
        public const string Person = "person";
        public const string Project = "project";
        public const string Analysis = "analysis";
        public const string Photo = "photo";
        public const string Document = "document";
    }

    // ==================== 請求模型 ====================

    /// <summary>
    /// 檔案上傳請求
    /// </summary>
    public class FileUploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        /// <summary>
        /// 關聯記錄 ID (可選)
        /// </summary>
        public string? AssociatedRecordId { get; set; }

        /// <summary>
        /// 關聯記錄類型 (可選)
        /// </summary>
        public string? AssociatedRecordType { get; set; }
    }

    /// <summary>
    /// 檔案關聯請求
    /// </summary>
    public class FileAssociationRequest
    {
        [Required]
        [StringLength(50)]
        public string RecordId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RecordType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 檔案查詢選項
    /// </summary>
    public class FileQueryOptions
    {
        public string? FileType { get; set; }
        public string? Status { get; set; }
        public string? AssociatedRecordType { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortBy { get; set; } = "UploadedAt";
        public bool SortDescending { get; set; } = true;
    }

    // ==================== 回應模型 ====================

    /// <summary>
    /// 檔案操作結果
    /// </summary>
    public class FileOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public FileModel? File { get; set; }
        public bool IsDuplicate { get; set; }
        public List<string> Errors { get; set; } = new();
        public string? FilePath { get; set; }
    }

    /// <summary>
    /// 檔案列表回應
    /// </summary>
    public class FileListResponse : ApiResponse
    {
        public List<FileModel> Files { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// 檔案統計資訊
    /// </summary>
    public class FileStatistics
    {
        public int TotalFiles { get; set; }
        public long TotalSizeBytes { get; set; }
        public double TotalSizeMB => Math.Round(TotalSizeBytes / 1024.0 / 1024.0, 2);
        public Dictionary<string, int> FilesByType { get; set; } = new();
        public Dictionary<string, int> FilesByStatus { get; set; } = new();
        public Dictionary<string, int> FilesByAssociationType { get; set; } = new();
        public DateTime? OldestFileDate { get; set; }
        public DateTime? NewestFileDate { get; set; }
    }

    /// <summary>
    /// 刪除影響分析結果
    /// </summary>
    public class DeleteImpactResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int AffectedRecords { get; set; }
        public List<string> AffectedRecordNames { get; set; } = new();
        public bool CanDelete { get; set; } = true;
        public string? Warning { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    // ==================== 向後相容性模型 ====================

    /// <summary>
    /// 舊檔案模型 (向後相容性)
    /// 用於與現有 FileUploadModel 的轉換
    /// </summary>
    public class LegacyFileModel
    {
        public int Id { get; set; }
        public string Filename { get; set; } = string.Empty;
        public string OriginalFilename { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Md5Hash { get; set; } = string.Empty;
        public DateTime UploadTime { get; set; }
        public bool IsMerged { get; set; }
        public DateTime? MergeTime { get; set; }
        public string Status { get; set; } = "uploaded";
        public string? ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 轉換為新檔案模型
        /// </summary>
        public FileModel ToFileModel(string userId)
        {
            return new FileModel
            {
                FileId = Guid.NewGuid(), // 新生成 UUID
                UserId = userId,
                Filename = Filename,
                OriginalFilename = OriginalFilename,
                FilePath = FilePath,
                FileSize = FileSize,
                Md5Hash = Md5Hash,
                AssociatedRecordId = ProjectId,
                AssociatedRecordType = !string.IsNullOrEmpty(ProjectId) ? AssociatedRecordType.Project : null,
                UploadStatus = Status,
                IsProcessed = IsMerged,
                ProcessedAt = MergeTime,
                UploadedAt = UploadTime,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }

    /// <summary>
    /// 檔案模型擴展方法
    /// </summary>
    public static class FileModelExtensions
    {
        /// <summary>
        /// 轉換為舊模型 (向後相容性)
        /// </summary>
        public static LegacyFileModel ToLegacyModel(this FileModel file, int legacyId = 0)
        {
            return new LegacyFileModel
            {
                Id = legacyId,
                Filename = file.Filename,
                OriginalFilename = file.OriginalFilename,
                FilePath = file.FilePath,
                FileSize = file.FileSize,
                Md5Hash = file.Md5Hash,
                UploadTime = file.UploadedAt,
                IsMerged = file.IsProcessed,
                MergeTime = file.ProcessedAt,
                Status = file.UploadStatus,
                ProjectId = file.AssociatedRecordType == AssociatedRecordType.Project ? file.AssociatedRecordId : null,
                CreatedAt = file.CreatedAt,
                UpdatedAt = file.UpdatedAt
            };
        }

        /// <summary>
        /// 檢查檔案是否為指定類型
        /// </summary>
        public static bool IsOfType(this FileModel file, string fileType)
        {
            return string.Equals(file.FileType, fileType, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 檢查檔案是否關聯到指定記錄
        /// </summary>
        public static bool IsAssociatedWith(this FileModel file, string recordId, string recordType)
        {
            return string.Equals(file.AssociatedRecordId, recordId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(file.AssociatedRecordType, recordType, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 取得檔案擴展名
        /// </summary>
        public static string GetFileExtension(this FileModel file)
        {
            return Path.GetExtension(file.OriginalFilename).ToLowerInvariant();
        }

        /// <summary>
        /// 檢查檔案是否可處理 (Excel 檔案)
        /// </summary>
        public static bool IsProcessable(this FileModel file)
        {
            var extension = file.GetFileExtension();
            return extension == ".xlsx" || extension == ".xls";
        }
    }
}