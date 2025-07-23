// 配置管理服務 - 統一管理應用程式配置，避免散佈的硬編碼
using familytree_backend.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace familytree_backend.Services
{
    /// <summary>
    /// 配置管理服務介面
    /// 設計理念：採用介面隔離原則，便於單元測試和依賴注入
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// 獲取資料庫連接字串
        /// </summary>
        string GetConnectionString();

        /// <summary>
        /// 獲取檔案上傳設定
        /// </summary>
        FileUploadConfiguration GetFileUploadConfiguration();

        /// <summary>
        /// 獲取分頁設定
        /// </summary>
        PaginationConfiguration GetPaginationConfiguration();

        /// <summary>
        /// 獲取日誌設定
        /// </summary>
        LoggingConfiguration GetLoggingConfiguration();

        /// <summary>
        /// 獲取搜尋設定
        /// </summary>
        SearchConfiguration GetSearchConfiguration();
    }

    /// <summary>
    /// 配置管理服務實作
    /// 職責：集中管理所有配置項目，提供型別安全的配置存取
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// 建構子
        /// 設計考量：透過依賴注入獲取 IConfiguration，遵循 DI 原則
        /// </summary>
        public ConfigurationService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>
        /// 獲取資料庫連接字串
        /// 設計理念：集中管理連接字串，支援不同環境的配置
        /// </summary>
        public string GetConnectionString()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("找不到資料庫連接字串配置 'DefaultConnection'");
            }

            return connectionString;
        }

        /// <summary>
        /// 獲取檔案上傳設定
        /// 設計考量：將檔案相關設定集中管理，便於調整和維護
        /// </summary>
        public FileUploadConfiguration GetFileUploadConfiguration()
        {
            return new FileUploadConfiguration
            {
                // 從配置檔讀取，如果沒有設定則使用預設值
                MaxFileSizeBytes = _configuration.GetValue<long?>("FileUpload:MaxFileSizeBytes") 
                                  ?? ApplicationConstants.Files.MaxFileSizeBytes,
                
                AllowedMimeTypes = ApplicationConstants.Files.AllowedMimeTypes,
                AllowedExtensions = ApplicationConstants.Files.AllowedExtensions,
                
                // 上傳目錄路徑：結合專案根目錄和設定的目錄名稱
                UploadDirectory = Path.Combine(_environment.ContentRootPath, 
                                              ApplicationConstants.Files.UploadDirectoryName),
                
                // 是否允許覆蓋同名檔案
                AllowOverwrite = _configuration.GetValue<bool>("FileUpload:AllowOverwrite", false)
            };
        }

        /// <summary>
        /// 獲取分頁設定
        /// 設計理念：統一管理分頁參數，確保系統效能和使用者體驗的平衡
        /// </summary>
        public PaginationConfiguration GetPaginationConfiguration()
        {
            return new PaginationConfiguration
            {
                DefaultPageSize = _configuration.GetValue<int?>("Pagination:DefaultPageSize") 
                                 ?? ApplicationConstants.Database.DefaultPageSize,
                
                MaxPageSize = _configuration.GetValue<int?>("Pagination:MaxPageSize") 
                             ?? ApplicationConstants.Database.MaxPageSize,
                
                MinPageSize = ApplicationConstants.Database.MinPageSize
            };
        }

        /// <summary>
        /// 獲取日誌設定
        /// 設計考量：統一管理日誌相關配置，支援不同環境的日誌策略
        /// </summary>
        public LoggingConfiguration GetLoggingConfiguration()
        {
            return new LoggingConfiguration
            {
                // 日誌目錄：結合專案根目錄和日誌目錄名稱
                LogDirectory = Path.Combine(_environment.ContentRootPath, 
                                           ApplicationConstants.Logging.LogDirectoryName),
                
                // 日誌檔案名稱格式
                LogFileNameFormat = ApplicationConstants.Logging.LogFileNameFormat,
                
                // 日誌時間格式
                LogTimeFormat = ApplicationConstants.Logging.LogTimeFormat,
                
                // 是否啟用詳細日誌（開發環境預設啟用）
                EnableVerboseLogging = _configuration.GetValue<bool?>("Logging:EnableVerboseLogging") 
                                      ?? _environment.IsDevelopment(),
                
                // 日誌保留天數
                LogRetentionDays = _configuration.GetValue<int>("Logging:RetentionDays", 30)
            };
        }

        /// <summary>
        /// 獲取搜尋設定
        /// 設計理念：集中管理搜尋相關參數，平衡搜尋效能和結果品質
        /// </summary>
        public SearchConfiguration GetSearchConfiguration()
        {
            return new SearchConfiguration
            {
                DefaultPageSize = _configuration.GetValue<int?>("Search:DefaultPageSize") 
                                 ?? ApplicationConstants.Search.DefaultSearchPageSize,
                
                MaxResults = _configuration.GetValue<int?>("Search:MaxResults") 
                            ?? ApplicationConstants.Search.MaxSearchResults,
                
                // 搜尋超時時間（秒）
                TimeoutSeconds = _configuration.GetValue<int>("Search:TimeoutSeconds", 30),
                
                // 是否啟用模糊搜尋
                EnableFuzzySearch = _configuration.GetValue<bool>("Search:EnableFuzzySearch", true),
                
                // 搜尋介面相關設定
                MaxPopularKeywords = _configuration.GetValue<int>("Search:MaxPopularKeywords", 10),
                MaxSearchHistoryItems = _configuration.GetValue<int>("Search:MaxSearchHistoryItems", 20),
                MaxTopKeywords = _configuration.GetValue<int>("Search:MaxTopKeywords", 10)
            };
        }
    }

    #region 配置模型類別

    /// <summary>
    /// 檔案上傳配置模型
    /// 設計理念：使用強型別配置物件，提升程式碼的型別安全性
    /// </summary>
    public class FileUploadConfiguration
    {
        /// <summary>
        /// 最大檔案大小（位元組）
        /// </summary>
        public long MaxFileSizeBytes { get; set; }

        /// <summary>
        /// 允許的 MIME 類型
        /// </summary>
        public string[] AllowedMimeTypes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 允許的檔案副檔名
        /// </summary>
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 上傳目錄路徑
        /// </summary>
        public string UploadDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 是否允許覆蓋同名檔案
        /// </summary>
        public bool AllowOverwrite { get; set; }
    }

    /// <summary>
    /// 分頁配置模型
    /// </summary>
    public class PaginationConfiguration
    {
        /// <summary>
        /// 預設頁面大小
        /// </summary>
        public int DefaultPageSize { get; set; }

        /// <summary>
        /// 最大頁面大小
        /// </summary>
        public int MaxPageSize { get; set; }

        /// <summary>
        /// 最小頁面大小
        /// </summary>
        public int MinPageSize { get; set; }
    }

    /// <summary>
    /// 日誌配置模型
    /// </summary>
    public class LoggingConfiguration
    {
        /// <summary>
        /// 日誌目錄路徑
        /// </summary>
        public string LogDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 日誌檔案名稱格式
        /// </summary>
        public string LogFileNameFormat { get; set; } = string.Empty;

        /// <summary>
        /// 日誌時間格式
        /// </summary>
        public string LogTimeFormat { get; set; } = string.Empty;

        /// <summary>
        /// 是否啟用詳細日誌
        /// </summary>
        public bool EnableVerboseLogging { get; set; }

        /// <summary>
        /// 日誌保留天數
        /// </summary>
        public int LogRetentionDays { get; set; }
    }

    /// <summary>
    /// 搜尋配置模型
    /// </summary>
    public class SearchConfiguration
    {
        /// <summary>
        /// 預設頁面大小
        /// </summary>
        public int DefaultPageSize { get; set; }

        /// <summary>
        /// 最大結果數量
        /// </summary>
        public int MaxResults { get; set; }

        /// <summary>
        /// 搜尋超時時間（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// 是否啟用模糊搜尋
        /// </summary>
        public bool EnableFuzzySearch { get; set; }

        /// <summary>
        /// 最大熱門關鍵字數量
        /// </summary>
        public int MaxPopularKeywords { get; set; }

        /// <summary>
        /// 最大搜索歷史項目數
        /// </summary>
        public int MaxSearchHistoryItems { get; set; }

        /// <summary>
        /// 最大頂級關鍵字數量
        /// </summary>
        public int MaxTopKeywords { get; set; }
    }

    #endregion
} 