-- 檔案上傳命名標準化 - Phase 2: 後端程式碼更新腳本
-- 執行日期: 2025-08-02
-- 目標: 建立新的檔案模型和服務，遷移從 project_id 到 file_id 命名模式

-- =====================================================
-- 第一步：新檔案模型設計
-- =====================================================

-- 檔案模型重構說明：
-- 舊模型：FileUploadModel (使用 project_id)
-- 新模型：FileModel (使用 file_id + associated_record_id/type)

/*
新 FileModel 設計 (C# 程式碼生成參考):

public class FileModel
{
    // 基本識別資訊
    public Guid FileId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    
    // 檔案基本資訊
    public string Filename { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Md5Hash { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public string? MimeType { get; set; }
    
    // 關聯資訊 (取代原本的 project_id)
    public string? AssociatedRecordId { get; set; }
    public string? AssociatedRecordType { get; set; } // 'person', 'project', 'analysis', etc.
    
    // 狀態管理
    public string UploadStatus { get; set; } = "uploaded";
    public bool IsProcessed { get; set; } = false;
    public DateTime? ProcessedAt { get; set; }
    
    // 審計欄位
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// 關聯記錄類別
public enum AssociatedRecordType
{
    Person = "person",
    Project = "project", // 向後相容
    Analysis = "analysis",
    Photo = "photo",
    Document = "document"
}

// 檔案狀態
public enum FileUploadStatus
{
    Uploaded = "uploaded",
    Processing = "processing",
    Processed = "processed",
    Failed = "failed",
    Deleted = "deleted"
}
*/

-- =====================================================
-- 第二步：服務層程式碼更新架構
-- =====================================================

-- 檔案服務重構說明：
-- 舊服務：FileUploadService (專案為中心的檔案管理)
-- 新服務：FileService (以檔案為中心的統一管理)

/*
新 FileService 主要方法設計：

public class FileService
{
    // 核心檔案操作
    Task<FileOperationResult> UploadFileAsync(IFormFile file, string userId, string? associatedRecordId = null, string? associatedRecordType = null);
    Task<FileModel?> GetFileAsync(Guid fileId, string userId);
    Task<List<FileModel>> GetUserFilesAsync(string userId, string? fileType = null, string? status = null);
    Task<List<FileModel>> GetAssociatedFilesAsync(string associatedRecordId, string associatedRecordType, string userId);
    Task<FileOperationResult> DeleteFileAsync(Guid fileId, string userId);
    
    // 檔案處理
    Task<FileOperationResult> ProcessFileAsync(Guid fileId, string userId);
    Task<FileOperationResult> UpdateFileStatusAsync(Guid fileId, string status, string userId);
    
    // 檔案查詢和統計
    Task<FileStatistics> GetFileStatisticsAsync(string userId);
    Task<DeleteImpactResult> GetDeleteImpactAsync(Guid fileId, string userId);
    
    // 檔案關聯管理
    Task<FileOperationResult> AssociateFileAsync(Guid fileId, string recordId, string recordType, string userId);
    Task<FileOperationResult> DisassociateFileAsync(Guid fileId, string userId);
    
    // 檔案驗證和工具
    Task<string> CalculateMd5Async(IFormFile file);
    Task<bool> IsFileExistsAsync(string md5Hash, string userId, string? associatedRecordId = null);
    string GenerateUniqueFileName(string originalFileName);
}
*/

-- =====================================================
-- 第三步：控制器程式碼更新架構
-- =====================================================

-- 控制器重構說明：
-- 舊控制器：FileUploadController (參數使用 project_id)
-- 新控制器：FileController (參數使用 file_id 和 associated_record_id/type)

/*
新 FileController API 端點設計：

[Route("api/[controller]")]
public class FileController : BaseController
{
    // 檔案上傳 API
    [HttpPost("upload")]
    [RequirePermission("file:upload")]
    public async Task<IActionResult> UploadFile(
        [FromForm] IFormFile file, 
        [FromForm] string? associated_record_id = null,
        [FromForm] string? associated_record_type = null);
    
    // 檔案查詢 API
    [HttpGet]
    [RequirePermission("file:read")]
    public async Task<IActionResult> GetFiles(
        [FromQuery] string? file_type = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50);
    
    // 單一檔案查詢
    [HttpGet("{file_id}")]
    [RequirePermission("file:read")]
    public async Task<IActionResult> GetFile(Guid file_id);
    
    // 關聯檔案查詢
    [HttpGet("associated/{record_type}/{record_id}")]
    [RequirePermission("file:read")]
    public async Task<IActionResult> GetAssociatedFiles(
        string record_type, 
        string record_id);
    
    // 檔案處理
    [HttpPost("{file_id}/process")]
    [RequirePermission("file:process")]
    public async Task<IActionResult> ProcessFile(Guid file_id);
    
    // 檔案刪除
    [HttpDelete("{file_id}")]
    [RequirePermission("file:delete")]
    public async Task<IActionResult> DeleteFile(Guid file_id);
    
    // 檔案關聯管理
    [HttpPost("{file_id}/associate")]
    [RequirePermission("file:associate")]
    public async Task<IActionResult> AssociateFile(
        Guid file_id,
        [FromBody] FileAssociationRequest request);
    
    // 檔案統計
    [HttpGet("statistics")]
    [RequirePermission("file:read")]
    public async Task<IActionResult> GetFileStatistics();
    
    // 刪除影響分析
    [HttpGet("{file_id}/delete-impact")]
    [RequirePermission("file:delete")]
    public async Task<IActionResult> GetDeleteImpact(Guid file_id);
}
*/

-- =====================================================
-- 第四步：資料存取層更新架構
-- =====================================================

-- 資料存取服務更新說明：
-- 需要新增支援新檔案表的 CRUD 操作
-- 同時保持向後相容性支援舊有檔案表查詢

/*
IDataAccessServiceV2 新增檔案管理方法：

// 檔案基本操作
Task<FileModel> CreateFileRecordAsync(FileModel file);
Task<FileModel?> GetFileByIdAsync(Guid fileId, string userId);
Task<List<FileModel>> GetUserFilesAsync(string userId, FileQueryOptions options);
Task<bool> UpdateFileAsync(FileModel file);
Task<bool> DeleteFileAsync(Guid fileId, string userId);

// 檔案查詢方法
Task<List<FileModel>> GetFilesByMd5Async(string md5Hash, string userId);
Task<List<FileModel>> GetAssociatedFilesAsync(string recordId, string recordType, string userId);
Task<FileStatistics> GetFileStatisticsAsync(string userId);

// 檔案關聯操作
Task<bool> AssociateFileWithRecordAsync(Guid fileId, string recordId, string recordType, string userId);
Task<bool> DisassociateFileAsync(Guid fileId, string userId);

// 向後相容性方法 (使用檢視查詢舊資料)
Task<List<LegacyFileModel>> GetLegacyFilesAsync(string projectId);
Task<LegacyFileModel?> GetLegacyFileByIdAsync(int oldFileId);
*/

-- =====================================================
-- 第五步：檔案響應模型更新
-- =====================================================

-- 回應模型重構說明：
-- 新增統一的檔案操作回應模型

/*
回應模型設計：

// 檔案操作結果
public class FileOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FileModel? File { get; set; }
    public bool IsDuplicate { get; set; }
    public List<string> Errors { get; set; } = new();
}

// 檔案列表回應
public class FileListResponse : ApiResponse
{
    public List<FileModel> Files { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

// 檔案統計資訊
public class FileStatistics
{
    public int TotalFiles { get; set; }
    public long TotalSizeBytes { get; set; }
    public Dictionary<string, int> FilesByType { get; set; } = new();
    public Dictionary<string, int> FilesByStatus { get; set; } = new();
    public Dictionary<string, int> FilesByAssociationType { get; set; } = new();
}

// 檔案關聯請求
public class FileAssociationRequest
{
    public string RecordId { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
}

// 刪除影響分析結果
public class DeleteImpactResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AffectedRecords { get; set; }
    public List<string> AffectedRecordNames { get; set; } = new();
    public bool CanDelete { get; set; }
    public string? Warning { get; set; }
}
*/

-- =====================================================
-- 第六步：遷移策略和向後相容性
-- =====================================================

-- 向後相容性實施策略：
-- 1. 保留舊有 FileUploadController 並標記為 [Obsolete]
-- 2. 新建 FileController 使用新的命名規範
-- 3. 建立檔案資料遷移工具，將舊表資料遷移到新表
-- 4. 提供 API 版本控制，允許兩種 API 並存一段時間

/*
向後相容性實作範例：

// 舊 API 保持運作但標記為過時
[Obsolete("請使用新的 /api/file 端點。此端點將在 v2.0 中移除。")]
[Route("api/file-upload")]
public class FileUploadController : ControllerBase
{
    // 提供重導向到新 API 的包裝方法
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string? project_id = null)
    {
        // 重導向到新 API，將 project_id 轉換為 associated_record_id
        return await _fileController.UploadFile(file, project_id, "project");
    }
}

// 資料庫檢視提供舊格式相容性
CREATE VIEW user_update_file_compat AS
SELECT 
    ROW_NUMBER() OVER (ORDER BY uploaded_at) as id,
    filename,
    original_filename,
    file_path,
    file_size,
    md5_hash,
    uploaded_at as upload_time,
    is_processed as is_merged,
    processed_at as merge_time,
    upload_status as status,
    created_at,
    updated_at,
    associated_record_id as project_id  -- 關鍵的相容性映射
FROM file_uploads
WHERE associated_record_type = 'project' OR associated_record_type IS NULL
ORDER BY uploaded_at DESC;
*/

-- =====================================================
-- 第七步：程式碼實施檢查清單
-- =====================================================

-- 後端程式碼更新檢查清單：

-- [ ] 1. 建立新的 FileModel.cs
-- [ ] 2. 建立新的 FileService.cs 
-- [ ] 3. 建立新的 FileController.cs
-- [ ] 4. 更新 IDataAccessServiceV2.cs 介面
-- [ ] 5. 實作 DataAccessServiceV2.cs 檔案操作方法
-- [ ] 6. 建立檔案相關的回應模型類別
-- [ ] 7. 更新依賴注入設定 (Program.cs)
-- [ ] 8. 建立檔案遷移工具
-- [ ] 9. 更新現有控制器中的檔案參考
-- [ ] 10. 更新 ExcelProcessingService 以使用新的檔案 ID
-- [ ] 11. 建立單元測試
-- [ ] 12. 建立整合測試
-- [ ] 13. 更新 API 文檔
-- [ ] 14. 標記舊 API 為過時

-- =====================================================
-- 第八步：效能和安全考量
-- =====================================================

-- 效能最佳化：
-- 1. 檔案查詢使用索引優化
-- 2. 大檔案上傳使用串流處理
-- 3. 檔案統計使用快取機制
-- 4. 批次操作支援

-- 安全強化：
-- 1. 檔案類型驗證加強
-- 2. 檔案大小限制
-- 3. 病毒掃描整合點預留
-- 4. 存取權限嚴格控制

/*
效能優化範例：

// 使用快取改善檔案統計查詢
[HttpGet("statistics")]
[ResponseCache(Duration = 300)] // 5分鐘快取
public async Task<IActionResult> GetFileStatistics()
{
    var cacheKey = $"file_stats_{CurrentUserId}";
    if (_cache.TryGetValue(cacheKey, out FileStatistics cachedStats))
    {
        return Ok(cachedStats);
    }
    
    var stats = await _fileService.GetFileStatisticsAsync(CurrentUserId);
    _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(5));
    return Ok(stats);
}

// 檔案串流下載
[HttpGet("{file_id}/download")]
public async Task<IActionResult> DownloadFile(Guid file_id)
{
    var file = await _fileService.GetFileAsync(file_id, CurrentUserId);
    if (file == null) return NotFound();
    
    var stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read);
    return File(stream, file.MimeType ?? "application/octet-stream", file.OriginalFilename);
}
*/

-- =====================================================
-- 總結
-- =====================================================

-- 本腳本定義了檔案上傳命名標準化的第二階段：後端程式碼更新
-- 
-- 主要改進：
-- 1. 從以專案為中心改為以檔案為中心的架構
-- 2. 統一使用 file_id (Guid) 作為主要識別符
-- 3. 彈性的關聯機制取代固定的 project_id
-- 4. 完整的向後相容性支援
-- 5. 更好的效能和安全性設計
-- 
-- 下一步：
-- 需要實際建立這些 C# 程式碼檔案並實施這個架構設計

-- 記錄更新到遷移日誌
INSERT INTO data_migration_log (migration_name, executed_by, status, records_affected, notes)
VALUES (
    'FileNamingStandardization-Phase2-Design', 
    'system', 
    'design_completed',
    0,
    'Completed Phase 2 design: Backend code architecture for file/file_id naming standardization. Includes new FileModel, FileService, FileController designs with backward compatibility strategy.'
);