// 日誌檔案管理服務 - 提供日誌輪轉、壓縮、清理功能
// 設計改善：改善日誌檔案管理，優化儲存效能
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;
using System.IO.Compression;

namespace familytree_backend.Services
{
    /// <summary>
    /// 日誌檔案管理服務介面
    /// 設計理念：提供完整的日誌檔案生命週期管理
    /// </summary>
    public interface ILogFileManagementService
    {
        /// <summary>
        /// 執行日誌檔案清理
        /// </summary>
        Task CleanupLogFilesAsync();

        /// <summary>
        /// 壓縮舊日誌檔案
        /// </summary>
        Task CompressOldLogFilesAsync();

        /// <summary>
        /// 獲取日誌檔案統計資訊
        /// </summary>
        Task<LogFileStatistics> GetLogFileStatisticsAsync();

        /// <summary>
        /// 備份日誌檔案
        /// </summary>
        Task BackupLogFilesAsync(string backupDirectory);

        /// <summary>
        /// 檢查日誌檔案健康狀態
        /// </summary>
        Task<LogFileHealthStatus> CheckLogFileHealthAsync();
    }

    /// <summary>
    /// 日誌檔案管理服務實作
    /// 職責：管理日誌檔案的完整生命週期，包括清理、壓縮、備份等
    /// </summary>
    public class LogFileManagementService : ILogFileManagementService
    {
        private readonly ILogger<LogFileManagementService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly LoggingConfiguration _loggingConfig;

        /// <summary>
        /// 建構子
        /// </summary>
        public LogFileManagementService(ILogger<LogFileManagementService> logger, IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _loggingConfig = configurationService.GetLoggingConfiguration();
        }

        /// <summary>
        /// 執行日誌檔案清理
        /// 設計理念：根據配置自動清理過期的日誌檔案
        /// </summary>
        public async Task CleanupLogFilesAsync()
        {
            try
            {
                _logger.LogInformation("開始執行日誌檔案清理...");

                var logDirectory = _loggingConfig.LogDirectory;
                if (!Directory.Exists(logDirectory))
                {
                    _logger.LogWarning("日誌目錄不存在: {LogDirectory}", logDirectory);
                    return;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-_loggingConfig.LogRetentionDays);
                var deletedFiles = 0;
                var totalSize = 0L;

                // 清理過期的日誌檔案
                var logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly);
                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < cutoffDate)
                    {
                        try
                        {
                            totalSize += fileInfo.Length;
                            File.Delete(file);
                            deletedFiles++;
                            _logger.LogDebug("已刪除過期日誌檔案: {FileName}", fileInfo.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "刪除日誌檔案失敗: {FileName}", fileInfo.Name);
                        }
                    }
                }

                // 清理過期的壓縮檔案
                var compressedFiles = Directory.GetFiles(logDirectory, "*.gz", SearchOption.TopDirectoryOnly);
                foreach (var file in compressedFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < cutoffDate)
                    {
                        try
                        {
                            totalSize += fileInfo.Length;
                            File.Delete(file);
                            deletedFiles++;
                            _logger.LogDebug("已刪除過期壓縮檔案: {FileName}", fileInfo.Name);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "刪除壓縮檔案失敗: {FileName}", fileInfo.Name);
                        }
                    }
                }

                _logger.LogInformation("日誌檔案清理完成 - 刪除檔案數: {DeletedFiles}, 釋放空間: {TotalSize} bytes", 
                    deletedFiles, totalSize);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日誌檔案清理失敗");
                throw;
            }
        }

        /// <summary>
        /// 壓縮舊日誌檔案
        /// 設計理念：壓縮舊日誌檔案以節省儲存空間
        /// </summary>
        public async Task CompressOldLogFilesAsync()
        {
            try
            {
                _logger.LogInformation("開始壓縮舊日誌檔案...");

                var logDirectory = _loggingConfig.LogDirectory;
                if (!Directory.Exists(logDirectory))
                {
                    _logger.LogWarning("日誌目錄不存在: {LogDirectory}", logDirectory);
                    return;
                }

                var cutoffDate = DateTime.UtcNow.AddDays(-1); // 壓縮一天前的檔案
                var compressedFiles = 0;

                // 壓縮舊的日誌檔案
                var logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly);
                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < cutoffDate)
                    {
                        try
                        {
                            var compressedFile = file + ".gz";
                            if (!File.Exists(compressedFile))
                            {
                                await CompressFileAsync(file, compressedFile);
                                compressedFiles++;
                                _logger.LogDebug("已壓縮日誌檔案: {FileName}", fileInfo.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "壓縮日誌檔案失敗: {FileName}", fileInfo.Name);
                        }
                    }
                }

                _logger.LogInformation("日誌檔案壓縮完成 - 壓縮檔案數: {CompressedFiles}", compressedFiles);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日誌檔案壓縮失敗");
                throw;
            }
        }

        /// <summary>
        /// 獲取日誌檔案統計資訊
        /// 設計理念：提供詳細的日誌檔案統計資訊
        /// </summary>
        public async Task<LogFileStatistics> GetLogFileStatisticsAsync()
        {
            try
            {
                var logDirectory = _loggingConfig.LogDirectory;
                if (!Directory.Exists(logDirectory))
                {
                    return new LogFileStatistics
                    {
                        TotalFiles = 0,
                        TotalSize = 0,
                        OldestFile = null,
                        NewestFile = null,
                        FileTypes = new Dictionary<string, int>()
                    };
                }

                var statistics = new LogFileStatistics
                {
                    FileTypes = new Dictionary<string, int>(),
                    OldestFile = null,
                    NewestFile = null
                };

                var allFiles = Directory.GetFiles(logDirectory, "*.*", SearchOption.TopDirectoryOnly);
                var oldestDate = DateTime.MaxValue;
                var newestDate = DateTime.MinValue;

                foreach (var file in allFiles)
                {
                    var fileInfo = new FileInfo(file);
                    var extension = fileInfo.Extension.ToLowerInvariant();

                    // 統計檔案類型
                    if (statistics.FileTypes.ContainsKey(extension))
                    {
                        statistics.FileTypes[extension]++;
                    }
                    else
                    {
                        statistics.FileTypes[extension] = 1;
                    }

                    // 統計檔案大小
                    statistics.TotalSize += fileInfo.Length;

                    // 找出最舊和最新的檔案
                    if (fileInfo.LastWriteTime < oldestDate)
                    {
                        oldestDate = fileInfo.LastWriteTime;
                        statistics.OldestFile = fileInfo.Name;
                    }

                    if (fileInfo.LastWriteTime > newestDate)
                    {
                        newestDate = fileInfo.LastWriteTime;
                        statistics.NewestFile = fileInfo.Name;
                    }
                }

                statistics.TotalFiles = allFiles.Length;

                _logger.LogDebug("日誌檔案統計完成 - 總檔案數: {TotalFiles}, 總大小: {TotalSize} bytes", 
                    statistics.TotalFiles, statistics.TotalSize);

                return await Task.FromResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "獲取日誌檔案統計資訊失敗");
                throw;
            }
        }

        /// <summary>
        /// 備份日誌檔案
        /// 設計理念：提供日誌檔案備份功能
        /// </summary>
        public async Task BackupLogFilesAsync(string backupDirectory)
        {
            try
            {
                _logger.LogInformation("開始備份日誌檔案到: {BackupDirectory}", backupDirectory);

                var logDirectory = _loggingConfig.LogDirectory;
                if (!Directory.Exists(logDirectory))
                {
                    _logger.LogWarning("日誌目錄不存在: {LogDirectory}", logDirectory);
                    return;
                }

                // 建立備份目錄
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                var backupTimestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"logs_backup_{backupTimestamp}.zip";
                var backupPath = Path.Combine(backupDirectory, backupFileName);

                // 建立備份壓縮檔
                using (var zipArchive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
                {
                    var logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly);
                    foreach (var file in logFiles)
                    {
                        var fileName = Path.GetFileName(file);
                        zipArchive.CreateEntryFromFile(file, fileName);
                    }
                }

                _logger.LogInformation("日誌檔案備份完成: {BackupPath}", backupPath);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日誌檔案備份失敗");
                throw;
            }
        }

        /// <summary>
        /// 檢查日誌檔案健康狀態
        /// 設計理念：監控日誌檔案的健康狀態
        /// </summary>
        public async Task<LogFileHealthStatus> CheckLogFileHealthAsync()
        {
            try
            {
                var logDirectory = _loggingConfig.LogDirectory;
                var healthStatus = new LogFileHealthStatus
                {
                    IsHealthy = true,
                    Issues = new List<string>()
                };

                if (!Directory.Exists(logDirectory))
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add("日誌目錄不存在");
                    return await Task.FromResult(healthStatus);
                }

                // 檢查磁碟空間
                var driveInfo = new DriveInfo(Path.GetPathRoot(logDirectory)!);
                var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                if (freeSpaceGB < 1.0) // 少於 1GB
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"磁碟空間不足: {freeSpaceGB:F2} GB");
                }

                // 檢查日誌檔案數量
                var logFiles = Directory.GetFiles(logDirectory, "*.log", SearchOption.TopDirectoryOnly);
                if (logFiles.Length > _loggingConfig.MaxLogFiles)
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"日誌檔案數量過多: {logFiles.Length} > {_loggingConfig.MaxLogFiles}");
                }

                // 檢查日誌檔案大小
                var totalSize = logFiles.Sum(file => new FileInfo(file).Length);
                var maxTotalSize = (long)_loggingConfig.MaxLogFileSizeMB * 1024 * 1024 * _loggingConfig.MaxLogFiles;
                if (totalSize > maxTotalSize)
                {
                    healthStatus.IsHealthy = false;
                    healthStatus.Issues.Add($"日誌檔案總大小過大: {totalSize / (1024 * 1024)} MB");
                }

                // 檢查是否有損壞的檔案
                foreach (var file in logFiles)
                {
                    try
                    {
                        using var stream = File.OpenRead(file);
                        // 嘗試讀取檔案以檢查是否損壞
                    }
                    catch (Exception)
                    {
                        healthStatus.IsHealthy = false;
                        healthStatus.Issues.Add($"日誌檔案損壞: {Path.GetFileName(file)}");
                    }
                }

                _logger.LogDebug("日誌檔案健康檢查完成 - 健康狀態: {IsHealthy}, 問題數: {IssueCount}", 
                    healthStatus.IsHealthy, healthStatus.Issues.Count);

                return await Task.FromResult(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日誌檔案健康檢查失敗");
                throw;
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 壓縮檔案
        /// </summary>
        private async Task CompressFileAsync(string sourceFile, string compressedFile)
        {
            using var inputStream = File.OpenRead(sourceFile);
            using var outputStream = File.Create(compressedFile);
            using var gzipStream = new GZipStream(outputStream, CompressionMode.Compress);
            await inputStream.CopyToAsync(gzipStream);
        }

        #endregion
    }

    /// <summary>
    /// 日誌檔案統計資訊
    /// </summary>
    public class LogFileStatistics
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public string? OldestFile { get; set; }
        public string? NewestFile { get; set; }
        public Dictionary<string, int> FileTypes { get; set; } = new();
    }

    /// <summary>
    /// 日誌檔案健康狀態
    /// </summary>
    public class LogFileHealthStatus
    {
        public bool IsHealthy { get; set; }
        public List<string> Issues { get; set; } = new();
    }
} 