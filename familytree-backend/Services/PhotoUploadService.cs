// 照片上傳服務：處理圖片檔案上傳、ZIP/7z解壓縮和專案分離存儲
// 主要功能：支援多種圖片格式、ZIP/7z自動解壓縮、檔案重複處理、專案目錄分離

using System.IO.Compression;
using System.Security.Cryptography;
using SharpCompress.Archives;
using SharpCompress.Common;
using familytree_backend.Models;
using Dapper;
using Npgsql;

namespace familytree_backend.Services
{
    /// <summary>
    /// 照片上傳服務
    /// 職責：處理圖片檔案和ZIP檔案的上傳、解壓縮、存儲管理
    /// </summary>
    public class PhotoUploadService
    {
        private readonly string _connectionString;
        private readonly string _photosDirectory;
        private readonly ILogger<PhotoUploadService> _logger;
        private readonly IConfigurationService _configurationService;

        // 支援的圖片格式
        private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] ImageMimeTypes = { "image/jpeg", "image/png" };

        public PhotoUploadService(
            IConfiguration configuration, 
            ILogger<PhotoUploadService> logger,
            IConfigurationService configurationService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            _configurationService = configurationService;
            
            // 建立照片儲存根目錄
            _photosDirectory = Path.Combine(Directory.GetCurrentDirectory(), "photos");
            if (!Directory.Exists(_photosDirectory))
            {
                Directory.CreateDirectory(_photosDirectory);
                _logger.LogInformation("📸 建立照片儲存目錄: {PhotosDirectory}", _photosDirectory);
            }
        }

        /// <summary>
        /// 處理檔案上傳（支援單一圖片和ZIP檔案）
        /// </summary>
        /// <param name="file">上傳的檔案</param>
        /// <param name="projectId">專案ID</param>
        /// <returns>上傳處理結果</returns>
        public async Task<PhotoUploadResponse> UploadPhotoAsync(IFormFile file, string projectId)
        {
            try
            {
                _logger.LogInformation("📸 開始處理照片上傳 - 檔案: {FileName}, 專案: {ProjectId}", 
                    file.FileName, projectId);

                // 建立專案照片目錄
                var projectPhotoDir = GetProjectPhotoDirectory(projectId);
                EnsureDirectoryExists(projectPhotoDir);

                var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

                if (fileExtension == ".zip" || fileExtension == ".7z")
                {
                    // 處理壓縮檔案（ZIP/7z）
                    return await ProcessArchiveFileAsync(file, projectId, projectPhotoDir);
                }
                else if (IsImageFile(fileExtension, file.ContentType))
                {
                    // 處理單一圖片檔案
                    return await ProcessSingleImageAsync(file, projectId, projectPhotoDir);
                }
                else
                {
                    return new PhotoUploadResponse
                    {
                        Success = false,
                        Message = $"不支援的檔案格式: {fileExtension}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 照片上傳處理失敗 - 檔案: {FileName}", file.FileName);
                return new PhotoUploadResponse
                {
                    Success = false,
                    Message = $"照片上傳處理失敗: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 處理ZIP檔案解壓縮
        /// </summary>
        private async Task<PhotoUploadResponse> ProcessZipFileAsync(IFormFile zipFile, string projectId, string projectPhotoDir)
        {
            var response = new PhotoUploadResponse { Success = true, UploadedFiles = new List<PhotoFileInfo>() };
            var tempZipPath = Path.GetTempFileName();

            try
            {
                _logger.LogInformation("📦 開始處理ZIP檔案: {FileName}", zipFile.FileName);

                // 儲存ZIP檔案到臨時位置
                using (var stream = new FileStream(tempZipPath, FileMode.Create))
                {
                    await zipFile.CopyToAsync(stream);
                }

                // 解壓縮ZIP檔案
                using (var archive = ZipFile.OpenRead(tempZipPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // 跳過目錄和隱藏檔案
                        if (string.IsNullOrEmpty(entry.Name) || entry.Name.StartsWith("."))
                            continue;

                        var entryExtension = Path.GetExtension(entry.Name)?.ToLowerInvariant();
                        if (!IsImageFile(entryExtension, null))
                        {
                            _logger.LogWarning("⚠️ 跳過非圖片檔案: {FileName}", entry.Name);
                            continue;
                        }

                        try
                        {
                            // 解壓縮並儲存圖片
                            var savedFile = await ExtractAndSaveImageFromZip(entry, projectId, projectPhotoDir);
                            if (savedFile != null)
                            {
                                response.UploadedFiles.Add(savedFile);
                                _logger.LogInformation("✅ 成功解壓縮圖片: {FileName}", entry.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ 解壓縮圖片失敗: {FileName}", entry.Name);
                            response.FailedFiles = response.FailedFiles ?? new List<string>();
                            response.FailedFiles.Add($"{entry.Name}: {ex.Message}");
                        }
                    }
                }

                // 記錄ZIP檔案上傳記錄
                await SaveFileUploadRecord(zipFile, projectId, "zip_processed");

                response.Message = $"ZIP檔案處理完成。成功: {response.UploadedFiles.Count} 個檔案";
                if (response.FailedFiles?.Count > 0)
                {
                    response.Message += $"，失敗: {response.FailedFiles.Count} 個檔案";
                }

                _logger.LogInformation("📦 ZIP檔案處理完成 - 成功: {SuccessCount}, 失敗: {FailedCount}", 
                    response.UploadedFiles.Count, response.FailedFiles?.Count ?? 0);

                return response;
            }
            finally
            {
                // 清理臨時檔案
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
            }
        }

        /// <summary>
        /// 處理壓縮檔案解壓縮（支援 ZIP 和 7z）
        /// </summary>
        private async Task<PhotoUploadResponse> ProcessArchiveFileAsync(IFormFile archiveFile, string projectId, string projectPhotoDir)
        {
            var response = new PhotoUploadResponse { Success = true, UploadedFiles = new List<PhotoFileInfo>() };
            var tempArchivePath = Path.GetTempFileName();
            var fileExtension = Path.GetExtension(archiveFile.FileName)?.ToLowerInvariant();

            try
            {
                _logger.LogInformation("📦 開始處理壓縮檔案: {FileName} ({Extension}), 大小: {FileSize}", 
                    archiveFile.FileName, fileExtension, archiveFile.Length);

                // 儲存壓縮檔案到臨時位置
                using (var stream = new FileStream(tempArchivePath, FileMode.Create))
                {
                    await archiveFile.CopyToAsync(stream);
                    _logger.LogDebug("壓縮檔案儲存到臨時位置完成");
                }

                // 檢查臨時檔案大小
                var tempFileInfo = new FileInfo(tempArchivePath);
                _logger.LogDebug("臨時壓縮檔案大小: {TempFileSize}, 存在: {TempFileExists}", 
                    tempFileInfo.Length, tempFileInfo.Exists);

                // 使用 SharpCompress 處理多種壓縮格式
                using (var archive = ArchiveFactory.Open(tempArchivePath))
                {
                    _logger.LogDebug("開啟壓縮檔案成功，總條目數: {TotalEntries}", archive.Entries.Count());
                    
                    foreach (var entry in archive.Entries)
                    {
                        _logger.LogDebug("處理壓縮條目: {EntryKey}, 大小: {EntrySize}, 是否目錄: {IsDirectory}", 
                            entry.Key, entry.Size, entry.IsDirectory);
                        
                        // 跳過目錄和隱藏檔案
                        if (entry.IsDirectory || string.IsNullOrEmpty(entry.Key) || entry.Key.StartsWith("."))
                        {
                            _logger.LogDebug("跳過條目: {EntryKey} (目錄或隱藏檔案)", entry.Key);
                            continue;
                        }

                        var entryExtension = Path.GetExtension(entry.Key)?.ToLowerInvariant();
                        if (!IsImageFile(entryExtension, null))
                        {
                            _logger.LogWarning("⚠️ 跳過非圖片檔案: {FileName}", entry.Key);
                            continue;
                        }

                        try
                        {
                            _logger.LogDebug("開始解壓縮圖片: {FileName}", entry.Key);
                            
                            // 解壓縮並儲存圖片
                            var savedFile = await ExtractAndSaveImageFromArchive(entry, projectId, projectPhotoDir);
                            if (savedFile != null)
                            {
                                response.UploadedFiles.Add(savedFile);
                                _logger.LogInformation("✅ 成功解壓縮圖片: {FileName}, 儲存大小: {SavedSize}", 
                                    entry.Key, savedFile.FileSize);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ 解壓縮圖片失敗: {FileName}", entry.Key);
                                response.FailedFiles = response.FailedFiles ?? new List<string>();
                                response.FailedFiles.Add($"{entry.Key}: 解壓縮失敗");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ 解壓縮圖片失敗: {FileName}", entry.Key);
                            response.FailedFiles = response.FailedFiles ?? new List<string>();
                            response.FailedFiles.Add($"{entry.Key}: {ex.Message}");
                        }
                    }
                }

                // 記錄ZIP檔案上傳記錄
                await SaveFileUploadRecord(archiveFile, projectId, "archive_processed");

                response.Message = $"壓縮檔案處理完成。成功: {response.UploadedFiles.Count} 個檔案";
                if (response.FailedFiles?.Count > 0)
                {
                    response.Message += $"，失敗: {response.FailedFiles.Count} 個檔案";
                }

                _logger.LogInformation("📦 壓縮檔案處理完成 - 成功: {SuccessCount}, 失敗: {FailedCount}", 
                    response.UploadedFiles.Count, response.FailedFiles?.Count ?? 0);

                return response;
            }
            finally
            {
                // 清理臨時檔案
                if (File.Exists(tempArchivePath))
                {
                    File.Delete(tempArchivePath);
                    _logger.LogDebug("臨時壓縮檔案已清理: {TempPath}", tempArchivePath);
                }
            }
        }

        /// <summary>
        /// 處理單一圖片檔案
        /// </summary>
        private async Task<PhotoUploadResponse> ProcessSingleImageAsync(IFormFile imageFile, string projectId, string projectPhotoDir)
        {
            _logger.LogInformation("🖼️ 處理單一圖片檔案: {FileName}", imageFile.FileName);

            var savedFile = await SaveImageFile(imageFile, projectId, projectPhotoDir, imageFile.FileName);
            
            if (savedFile != null)
            {
                return new PhotoUploadResponse
                {
                    Success = true,
                    Message = "圖片上傳成功",
                    UploadedFiles = new List<PhotoFileInfo> { savedFile }
                };
            }
            else
            {
                return new PhotoUploadResponse
                {
                    Success = false,
                    Message = "圖片儲存失敗"
                };
            }
        }

        /// <summary>
        /// 從ZIP檔案中解壓縮並儲存圖片
        /// </summary>
        private async Task<PhotoFileInfo?> ExtractAndSaveImageFromZip(ZipArchiveEntry entry, string projectId, string projectPhotoDir)
        {
            using (var entryStream = entry.Open())
            {
                var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // 建立臨時 IFormFile 物件，FormFileFromStream將負責管理memoryStream的生命週期
                using var formFile = new FormFileFromStream(memoryStream, entry.Name, entry.Name, "image/jpeg", memoryStream.Length);
                
                return await SaveImageFile(formFile, projectId, projectPhotoDir, entry.Name);
            }
        }

        /// <summary>
        /// 從壓縮檔案中解壓縮並儲存圖片（使用 SharpCompress）
        /// </summary>
        private async Task<PhotoFileInfo?> ExtractAndSaveImageFromArchive(IArchiveEntry entry, string projectId, string projectPhotoDir)
        {
            try
            {
                _logger.LogDebug("開始解壓縮檔案: {EntryKey}, 大小: {EntrySize}, 是否目錄: {IsDirectory}", 
                    entry.Key, entry.Size, entry.IsDirectory);

                using (var entryStream = entry.OpenEntryStream())
                {
                    _logger.LogDebug("開啟entry stream成功，開始讀取資料");
                    
                    var memoryStream = new MemoryStream();
                    await entryStream.CopyToAsync(memoryStream);
                    
                    _logger.LogDebug("複製到memory stream完成，MemoryStream長度: {MemoryStreamLength}", 
                        memoryStream.Length);
                    
                    memoryStream.Position = 0;

                    _logger.LogDebug("準備建立FormFileFromStream，檔名: {FileName}, 大小: {Size}", 
                        entry.Key, memoryStream.Length);

                    // 建立臨時 IFormFile 物件，不立即釋放，讓SaveImageFile完成所有操作
                    var formFile = new FormFileFromStream(memoryStream, entry.Key, entry.Key, "image/jpeg", memoryStream.Length);
                    
                    _logger.LogDebug("FormFileFromStream建立完成，Length屬性: {FormFileLength}", formFile.Length);
                    
                    var result = await SaveImageFile(formFile, projectId, projectPhotoDir, entry.Key);
                    
                    // 手動釋放資源
                    formFile.Dispose();
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExtractAndSaveImageFromArchive失敗: {EntryKey}", entry.Key);
                return null;
            }
        }

        /// <summary>
        /// 儲存圖片檔案
        /// </summary>
        private async Task<PhotoFileInfo?> SaveImageFile(IFormFile imageFile, string projectId, string projectPhotoDir, string originalFileName)
        {
            try
            {
                _logger.LogDebug("SaveImageFile開始，原始檔名: {OriginalFileName}, 檔案大小: {FileSize}", 
                    originalFileName, imageFile.Length);

                // 正規化檔名（數字檔名補零到6位）
                var normalizedFileName = NormalizePhotoFileName(originalFileName);
                _logger.LogDebug("檔名正規化: {OriginalFileName} -> {NormalizedFileName}", 
                    originalFileName, normalizedFileName);

                // 計算檔案MD5
                _logger.LogDebug("開始計算MD5，檔案大小: {FileSize}", imageFile.Length);
                var md5Hash = await CalculateMd5Async(imageFile);
                _logger.LogDebug("MD5計算完成: {Md5Hash}", md5Hash);

                // 檢查重複檔案
                var existingFile = await GetExistingPhotoByMd5Async(md5Hash, projectId);
                if (existingFile != null)
                {
                    _logger.LogWarning("⚠️ 圖片檔案重複: {FileName} (MD5: {Md5})", originalFileName, md5Hash);
                    return existingFile; // 返回已存在的檔案資訊
                }

                // 生成唯一檔名（處理重複檔名）
                var uniqueFileName = GenerateUniqueFileName(projectPhotoDir, normalizedFileName);
                var filePath = Path.Combine(projectPhotoDir, uniqueFileName);
                _logger.LogDebug("生成檔案路徑: {FilePath}", filePath);

                // 儲存檔案
                _logger.LogDebug("開始儲存檔案到磁碟，檔案大小: {FileSize}", imageFile.Length);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                    _logger.LogDebug("檔案儲存完成");
                }

                // 檢查實際儲存的檔案大小
                var fileInfo = new FileInfo(filePath);
                _logger.LogDebug("實際儲存檔案大小: {ActualFileSize}, 檔案存在: {FileExists}", 
                    fileInfo.Length, fileInfo.Exists);

                // 建立檔案資訊
                var photoInfo = new PhotoFileInfo
                {
                    OriginalFileName = originalFileName,        // 保留原始檔名
                    SavedFileName = uniqueFileName,             // 使用正規化後的檔名
                    FilePath = filePath,
                    FileSize = imageFile.Length,
                    Md5Hash = md5Hash,
                    ProjectId = projectId,
                    UploadTime = DateTime.UtcNow
                };

                _logger.LogDebug("PhotoFileInfo建立完成，FileSize: {FileSize}", photoInfo.FileSize);

                // 儲存到資料庫
                await SavePhotoToDatabaseAsync(photoInfo);

                _logger.LogInformation("✅ 圖片儲存成功: {OriginalFileName} → {SavedFileName}, 大小: {FileSize}", 
                    originalFileName, uniqueFileName, photoInfo.FileSize);

                return photoInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 圖片儲存失敗: {FileName}", originalFileName);
                return null;
            }
        }

        /// <summary>
        /// 取得專案照片目錄
        /// </summary>
        private string GetProjectPhotoDirectory(string projectId)
        {
            return Path.Combine(_photosDirectory, projectId);
        }

        /// <summary>
        /// 確保目錄存在
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation("📁 建立目錄: {Path}", path);
            }
        }

        /// <summary>
        /// 檢查是否為圖片檔案
        /// </summary>
        private bool IsImageFile(string? extension, string? mimeType)
        {
            if (string.IsNullOrEmpty(extension)) return false;
            
            var isValidExtension = ImageExtensions.Contains(extension);
            var isValidMimeType = string.IsNullOrEmpty(mimeType) || ImageMimeTypes.Contains(mimeType);
            
            return isValidExtension && isValidMimeType;
        }

        /// <summary>
        /// 生成唯一檔名（處理重複）
        /// </summary>
        private string GenerateUniqueFileName(string directory, string originalFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            var counter = 0;
            string uniqueFileName = originalFileName;

            while (File.Exists(Path.Combine(directory, uniqueFileName)))
            {
                counter++;
                uniqueFileName = $"{fileName}-{counter}{extension}";
            }

            return uniqueFileName;
        }

        /// <summary>
        /// 計算檔案的MD5雜湊值
        /// </summary>
        private async Task<string> CalculateMd5Async(IFormFile file)
        {
            try
            {
                _logger.LogDebug("CalculateMd5Async開始，檔案大小: {FileSize}", file.Length);
                
                using (var md5 = MD5.Create())
                using (var stream = file.OpenReadStream())
                {
                    _logger.LogDebug("CalculateMd5Async - 開啟檔案stream，長度: {StreamLength}", stream.Length);
                    
                    var hash = await md5.ComputeHashAsync(stream);
                    var hashString = Convert.ToHexString(hash).ToLowerInvariant();
                    
                    _logger.LogDebug("CalculateMd5Async完成，MD5: {Md5Hash}", hashString);
                    
                    return hashString;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CalculateMd5Async失敗，檔案大小: {FileSize}", file.Length);
                throw;
            }
        }

        /// <summary>
        /// 取得已存在的照片（透過MD5）
        /// </summary>
        private async Task<PhotoFileInfo?> GetExistingPhotoByMd5Async(string md5Hash, string projectId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = @"
                    SELECT original_filename, saved_filename, file_path, file_size, md5_hash, project_id, upload_time
                    FROM photos 
                    WHERE md5_hash = @Md5Hash AND project_id = @ProjectId";

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Md5Hash = md5Hash, ProjectId = projectId });
                
                if (result != null)
                {
                    return new PhotoFileInfo
                    {
                        OriginalFileName = result.original_filename,
                        SavedFileName = result.saved_filename,
                        FilePath = result.file_path,
                        FileSize = result.file_size,
                        Md5Hash = result.md5_hash,
                        ProjectId = result.project_id,
                        UploadTime = result.upload_time
                    };
                }

                return null;
            }
        }

        /// <summary>
        /// 儲存照片資訊到資料庫
        /// </summary>
        private async Task SavePhotoToDatabaseAsync(PhotoFileInfo photoInfo)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = @"
                    INSERT INTO photos (original_filename, saved_filename, file_path, file_size, md5_hash, project_id, upload_time)
                    VALUES (@OriginalFileName, @SavedFileName, @FilePath, @FileSize, @Md5Hash, @ProjectId, @UploadTime)";

                await connection.ExecuteAsync(sql, photoInfo);
            }
        }

        /// <summary>
        /// 記錄檔案上傳記錄
        /// </summary>
        private async Task SaveFileUploadRecord(IFormFile file, string projectId, string status)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = @"
                    INSERT INTO user_update_file (filename, original_filename, file_path, file_size, md5_hash, upload_time, status, project_id)
                    VALUES (@Filename, @OriginalFilename, @FilePath, @FileSize, @Md5Hash, @UploadTime, @Status, @ProjectId)";

                var md5Hash = await CalculateMd5Async(file);
                
                await connection.ExecuteAsync(sql, new
                {
                    Filename = file.FileName,
                    OriginalFilename = file.FileName,
                    FilePath = "",
                    FileSize = file.Length,
                    Md5Hash = md5Hash,
                    UploadTime = DateTime.UtcNow,
                    Status = status,
                    ProjectId = projectId
                });
            }
        }

        /// <summary>
        /// 記錄照片到photos資料表
        /// </summary>
        private async Task SavePhotoRecord(IFormFile file, PhotoFileInfo savedFile, string projectId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = @"
                    INSERT INTO photos (original_filename, saved_filename, file_path, file_size, md5_hash, project_id, upload_time, created_at, updated_at)
                    VALUES (@OriginalFilename, @SavedFilename, @FilePath, @FileSize, @Md5Hash, @ProjectId, @UploadTime, @CreatedAt, @UpdatedAt)";

                var md5Hash = await CalculateMd5Async(file);
                var now = DateTime.UtcNow;
                
                await connection.ExecuteAsync(sql, new
                {
                    OriginalFilename = savedFile.OriginalFileName,
                    SavedFilename = savedFile.SavedFileName,
                    FilePath = savedFile.FilePath,
                    FileSize = savedFile.FileSize,
                    Md5Hash = md5Hash,
                    ProjectId = projectId,
                    UploadTime = now,
                    CreatedAt = now,
                    UpdatedAt = now
                });
                
                _logger.LogInformation("📝 照片記錄已保存到photos表: {FileName}", savedFile.SavedFileName);
            }
        }

        /// <summary>
        /// 取得照片列表
        /// </summary>
        public async Task<PhotoListResponse> GetPhotoListAsync(string projectId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    var sql = @"
                        SELECT 
                            id as Id,
                            original_filename as OriginalFileName, 
                            saved_filename as SavedFileName, 
                            file_path as FilePath, 
                            file_size as FileSize, 
                            md5_hash as Md5Hash, 
                            project_id as ProjectId, 
                            upload_time as UploadTime
                        FROM photos 
                        WHERE project_id = @ProjectId 
                        ORDER BY upload_time DESC";

                    var photos = await connection.QueryAsync<PhotoFileInfo>(sql, new { ProjectId = projectId });

                    return new PhotoListResponse
                    {
                        Success = true,
                        Photos = photos.ToList(),
                        Message = $"找到 {photos.Count()} 張照片"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 取得照片列表失敗 - 專案: {ProjectId}", projectId);
                return new PhotoListResponse
                {
                    Success = false,
                    Message = $"取得照片列表失敗: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 刪除照片
        /// </summary>
        public async Task<PhotoDeleteResponse> DeletePhotoAsync(int photoId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 先取得照片資訊以便刪除實體檔案
                var photoInfoSql = @"
                    SELECT id as Id, original_filename as OriginalFileName, saved_filename as SavedFileName, 
                           file_path as FilePath, file_size as FileSize, md5_hash as Md5Hash, 
                           project_id as ProjectId, upload_time as UploadTime
                    FROM photos 
                    WHERE id = @id";

                var photoInfo = await connection.QueryFirstOrDefaultAsync<PhotoFileInfo>(photoInfoSql, new { id = photoId });
                if (photoInfo == null)
                {
                    return new PhotoDeleteResponse
                    {
                        Success = false,
                        Message = "照片不存在"
                    };
                }

                // 刪除實體檔案
                if (File.Exists(photoInfo.FilePath))
                {
                    File.Delete(photoInfo.FilePath);
                    _logger.LogInformation("🗑️ 刪除照片檔案: {FilePath}", photoInfo.FilePath);
                }

                // 從資料庫刪除記錄
                var sql = "DELETE FROM photos WHERE id = @PhotoId";
                var affectedRows = await connection.ExecuteAsync(sql, new { PhotoId = photoId });

                if (affectedRows > 0)
                {
                    _logger.LogInformation("✅ 照片刪除成功 - ID: {PhotoId}", photoId);
                    return new PhotoDeleteResponse
                    {
                        Success = true,
                        Message = "照片刪除成功"
                    };
                }
                else
                {
                    return new PhotoDeleteResponse
                    {
                        Success = false,
                        Message = "照片刪除失敗，資料庫記錄不存在"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 照片刪除失敗 - ID: {PhotoId}", photoId);
                return new PhotoDeleteResponse
                {
                    Success = false,
                    Message = $"照片刪除失敗: {ex.Message}"
                };
            }
        }



        /// <summary>
        /// 根據照片索引號查找照片
        /// </summary>
        public async Task<PhotoFileInfo?> GetPhotoByIndexAsync(string photoIndex, string projectId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 查找匹配索引號的照片檔案
                var sql = @"
                    SELECT id as Id, original_filename as OriginalFileName, saved_filename as SavedFileName, 
                           file_path as FilePath, file_size as FileSize, md5_hash as Md5Hash, 
                           project_id as ProjectId, upload_time as UploadTime
                    FROM photos 
                    WHERE project_id = @ProjectId 
                    AND saved_filename LIKE @Pattern
                    ORDER BY saved_filename
                    LIMIT 1";

                // 構建搜尋模式，支援不同格式
                var pattern = $"{photoIndex.PadLeft(6, '0')}%"; // 將索引補零至6位並加上萬用字元
                
                _logger.LogInformation("SQL查詢參數: projectId={ProjectId}, pattern={Pattern}", projectId, pattern);

                var photo = await connection.QueryFirstOrDefaultAsync<PhotoFileInfo>(sql, new { 
                    ProjectId = projectId, 
                    Pattern = pattern 
                });

                _logger.LogInformation("根據索引查找照片: {PhotoIndex} -> {FileName}", 
                    photoIndex, photo?.SavedFileName ?? "未找到");

                return photo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根據索引查找照片失敗: {PhotoIndex}, ProjectId: {ProjectId}", 
                    photoIndex, projectId);
                return null;
            }
        }



        /// <summary>
        /// 正規化照片檔名（數字檔名補零到6位）
        /// </summary>
        /// <param name="originalFileName">原始檔名</param>
        /// <returns>正規化後的檔名</returns>
        private string NormalizePhotoFileName(string originalFileName)
        {
            try
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                var extension = Path.GetExtension(originalFileName);

                // 檢查檔名是否為純數字
                if (int.TryParse(fileNameWithoutExtension, out int numericValue))
                {
                    // 如果是數字且小於6位，補零到6位
                    if (numericValue >= 0 && fileNameWithoutExtension.Length < 6)
                    {
                        var normalizedName = numericValue.ToString("D6") + extension;
                        _logger.LogInformation("📝 檔名正規化: {OriginalName} → {NormalizedName}", 
                            originalFileName, normalizedName);
                        return normalizedName;
                    }
                }

                // 非數字檔名或已經是6位以上數字，保持原樣
                return originalFileName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "檔名正規化失敗: {FileName}，使用原始檔名", originalFileName);
                return originalFileName;
            }
        }


    }

    /// <summary>
    /// 照片上傳回應模型
    /// </summary>
    public class PhotoUploadResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<PhotoFileInfo>? UploadedFiles { get; set; }
        public List<string>? FailedFiles { get; set; }
    }

    /// <summary>
    /// 照片檔案資訊模型
    /// </summary>
    public class PhotoFileInfo
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string SavedFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Md5Hash { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public DateTime UploadTime { get; set; }
    }

    /// <summary>
    /// 照片列表回應模型
    /// </summary>
    public class PhotoListResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<PhotoFileInfo>? Photos { get; set; }
    }

    /// <summary>
    /// 照片刪除回應模型
    /// </summary>
    public class PhotoDeleteResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// 從 Stream 建立 IFormFile 的輔助類別
    /// </summary>
    public class FormFileFromStream : IFormFile, IDisposable
    {
        private readonly Stream _stream;
        private readonly string _name;
        private readonly string _fileName;
        private readonly long _length;
        private bool _disposed = false;

        public FormFileFromStream(Stream stream, string name, string fileName, string contentType, long length)
        {
            _stream = stream;
            _name = name;
            _fileName = fileName;
            ContentType = contentType;
            _length = length;
        }

        public string ContentType { get; }
        public string ContentDisposition => $"form-data; name=\"{_name}\"; filename=\"{_fileName}\"";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => _length;
        public string Name => _name;
        public string FileName => _fileName;

        public Stream OpenReadStream()
        {
            // 創建一個新的 MemoryStream 副本，避免原始 stream 位置問題
            var memoryStream = new MemoryStream();
            _stream.Position = 0; // 重置原始 stream 位置
            _stream.CopyTo(memoryStream);
            memoryStream.Position = 0; // 重置新 stream 位置
            return memoryStream;
        }

        public void CopyTo(Stream target)
        {
            _stream.Position = 0;
            _stream.CopyTo(target);
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            _stream.Position = 0; // Reset position before copying
            return _stream.CopyToAsync(target, cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _stream?.Dispose();
                _disposed = true;
            }
        }
    }
} 