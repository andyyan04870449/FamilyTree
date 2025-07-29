// 配置管理服務 - 統一管理應用程式配置，避免散佈的硬編碼
// 設計改善：新增更多配置類型支援，完善配置管理體系
using familytree_backend.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace familytree_backend.Services
{
    /// <summary>
    /// 配置管理服務介面
    /// 設計理念：採用介面隔離原則，便於單元測試和依賴注入
    /// 改善重點：新增更多配置類型支援
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

        /// <summary>
        /// 獲取資料庫設定
        /// </summary>
        DatabaseConfiguration GetDatabaseConfiguration();

        /// <summary>
        /// 獲取關係圖譜設定
        /// </summary>
        RelationshipGraphConfiguration GetRelationshipGraphConfiguration();

        /// <summary>
        /// 獲取 Excel 處理設定
        /// </summary>
        ExcelConfiguration GetExcelConfiguration();

        /// <summary>
        /// 獲取安全性設定
        /// </summary>
        SecurityConfiguration GetSecurityConfiguration();

        /// <summary>
        /// 獲取效能設定
        /// </summary>
        PerformanceConfiguration GetPerformanceConfiguration();

        /// <summary>
        /// 獲取環境設定
        /// </summary>
        EnvironmentConfiguration GetEnvironmentConfiguration();

        /// <summary>
        /// 獲取 API 設定
        /// </summary>
        ApiConfiguration GetApiConfiguration();

        /// <summary>
        /// 獲取監控設定
        /// </summary>
        MonitoringConfiguration GetMonitoringConfiguration();

        /// <summary>
        /// 獲取 OpenAI 設定
        /// </summary>
        OpenAIConfiguration GetOpenAIConfiguration();
    }

    /// <summary>
    /// 配置管理服務實作
    /// 職責：集中管理所有配置項目，提供型別安全的配置存取
    /// 改善重點：新增更多配置類型支援，完善配置管理體系
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
        /// 改善重點：新增更多檔案上傳配置選項
        /// </summary>
        public FileUploadConfiguration GetFileUploadConfiguration()
        {
            return new FileUploadConfiguration
            {
                // 從配置檔讀取，如果沒有設定則使用預設值
                MaxFileSizeBytes = _configuration.GetValue<long?>("FileUpload:MaxFileSizeBytes") 
                                  ?? ApplicationConstants.Files.MaxFileSizeBytes,
                
                AllowedMimeTypes = _configuration.GetSection("FileUpload:AllowedMimeTypes").Get<string[]>() 
                                  ?? ApplicationConstants.Files.AllowedMimeTypes,
                
                AllowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>() 
                                   ?? ApplicationConstants.Files.AllowedExtensions,
                
                // 上傳目錄路徑：結合專案根目錄和設定的目錄名稱
                UploadDirectory = Path.Combine(_environment.ContentRootPath, 
                                              _configuration.GetValue<string>("FileUpload:UploadDirectory") 
                                              ?? ApplicationConstants.Files.UploadDirectoryName),
                
                PhotoUploadDirectory = Path.Combine(_environment.ContentRootPath,
                                                   _configuration.GetValue<string>("FileUpload:PhotoUploadDirectory")
                                                   ?? ApplicationConstants.Files.PhotoUploadDirectoryName),
                
                // 是否允許覆蓋同名檔案
                AllowOverwrite = _configuration.GetValue<bool>("FileUpload:AllowOverwrite", false),
                
                // 新增的配置選項
                MaxConcurrentUploads = _configuration.GetValue<int>("FileUpload:MaxConcurrentUploads", 
                                                                   ApplicationConstants.Files.Processing.MaxConcurrentUploads),
                
                UploadTimeoutSeconds = _configuration.GetValue<int>("FileUpload:UploadTimeoutSeconds",
                                                                   ApplicationConstants.Files.Processing.UploadTimeoutSeconds),
                
                ProcessingTimeoutSeconds = _configuration.GetValue<int>("FileUpload:ProcessingTimeoutSeconds",
                                                                       ApplicationConstants.Files.Processing.ProcessingTimeoutSeconds),
                
                EnableDuplicateCheck = _configuration.GetValue<bool>("FileUpload:EnableDuplicateCheck",
                                                                    ApplicationConstants.Files.Processing.EnableDuplicateCheck),
                
                EnableAutoCompression = _configuration.GetValue<bool>("FileUpload:EnableAutoCompression",
                                                                     ApplicationConstants.Files.Processing.EnableAutoCompression)
            };
        }

        /// <summary>
        /// 獲取分頁設定
        /// 設計考量：統一管理分頁相關設定，支援不同場景的需求
        /// </summary>
        public PaginationConfiguration GetPaginationConfiguration()
        {
            return new PaginationConfiguration
            {
                DefaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize", 
                                                              ApplicationConstants.Database.DefaultPageSize),
                
                MaxPageSize = _configuration.GetValue<int>("Pagination:MaxPageSize", 
                                                          ApplicationConstants.Database.MaxPageSize),
                
                MinPageSize = _configuration.GetValue<int>("Pagination:MinPageSize", 
                                                          ApplicationConstants.Database.MinPageSize)
            };
        }

        /// <summary>
        /// 獲取日誌設定
        /// 設計考量：集中管理日誌相關設定，支援不同環境的需求
        /// 改善重點：新增更多日誌配置選項
        /// </summary>
        public LoggingConfiguration GetLoggingConfiguration()
        {
            return new LoggingConfiguration
            {
                LogDirectory = Path.Combine(_environment.ContentRootPath,
                                          _configuration.GetValue<string>("Logging:LogDirectory")
                                          ?? ApplicationConstants.Logging.LogDirectoryName),
                
                LogFileNameFormat = _configuration.GetValue<string>("Logging:LogFileNameFormat")
                                   ?? ApplicationConstants.Logging.LogFileNameFormat,
                
                LogTimeFormat = _configuration.GetValue<string>("Logging:LogTimeFormat")
                               ?? ApplicationConstants.Logging.LogTimeFormat,
                
                EnableVerboseLogging = _configuration.GetValue<bool>("Logging:EnableVerboseLogging", false),
                
                LogRetentionDays = _configuration.GetValue<int>("Logging:RetentionDays", 
                                                               ApplicationConstants.Logging.Configuration.LogRetentionDays),
                
                // 新增的配置選項
                MaxLogFileSizeMB = _configuration.GetValue<int>("Logging:MaxLogFileSizeMB",
                                                               ApplicationConstants.Logging.Configuration.MaxLogFileSizeMB),
                
                MaxLogFiles = _configuration.GetValue<int>("Logging:MaxLogFiles",
                                                          ApplicationConstants.Logging.Configuration.MaxLogFiles),
                
                EnableStructuredLogging = _configuration.GetValue<bool>("Logging:EnableStructuredLogging",
                                                                       ApplicationConstants.Logging.Configuration.EnableStructuredLogging),
                
                EnablePerformanceLogging = _configuration.GetValue<bool>("Logging:EnablePerformanceLogging",
                                                                        ApplicationConstants.Logging.Configuration.EnablePerformanceLogging),
                
                LogLevel = _configuration.GetValue<string>("Logging:LogLevel",
                                                          ApplicationConstants.Logging.Configuration.LogLevel)
            };
        }

        /// <summary>
        /// 獲取搜尋設定
        /// 設計考量：統一管理搜尋相關設定，支援不同搜尋模式
        /// 改善重點：新增更多搜尋配置選項
        /// </summary>
        public SearchConfiguration GetSearchConfiguration()
        {
            return new SearchConfiguration
            {
                DefaultPageSize = _configuration.GetValue<int>("Search:DefaultPageSize", 
                                                              ApplicationConstants.Search.DefaultSearchPageSize),
                
                MaxResults = _configuration.GetValue<int>("Search:MaxResults", 
                                                         ApplicationConstants.Search.MaxSearchResults),
                
                TimeoutSeconds = _configuration.GetValue<int>("Search:TimeoutSeconds",
                                                             ApplicationConstants.Search.SearchTimeoutSeconds),
                
                EnableFuzzySearch = _configuration.GetValue<bool>("Search:EnableFuzzySearch", true),
                
                // 新增的配置選項
                MinFuzzyMatchScore = _configuration.GetValue<double>("Search:MinFuzzyMatchScore",
                                                                    ApplicationConstants.Search.MinFuzzyMatchScore),
                
                MaxSearchKeywords = _configuration.GetValue<int>("Search:MaxSearchKeywords",
                                                                ApplicationConstants.Search.MaxSearchKeywords),
                
                EnableSearchCache = _configuration.GetValue<bool>("Search:EnableSearchCache",
                                                                 ApplicationConstants.Search.EnableSearchCache),
                
                SearchCacheExpirationMinutes = _configuration.GetValue<int>("Search:SearchCacheExpirationMinutes",
                                                                           ApplicationConstants.Search.SearchCacheExpirationMinutes),
                
                MaxPopularKeywords = _configuration.GetValue<int>("Search:MaxPopularKeywords", 10),
                MaxSearchHistoryItems = _configuration.GetValue<int>("Search:MaxSearchHistoryItems", 50),
                MaxTopKeywords = _configuration.GetValue<int>("Search:MaxTopKeywords", 20)
            };
        }

        /// <summary>
        /// 獲取資料庫設定
        /// 改善重點：新增資料庫配置支援
        /// </summary>
        public DatabaseConfiguration GetDatabaseConfiguration()
        {
            return new DatabaseConfiguration
            {
                MaxPoolSize = _configuration.GetValue<int>("Database:MaxPoolSize",
                                                          ApplicationConstants.Database.Connection.MaxPoolSize),
                
                MinPoolSize = _configuration.GetValue<int>("Database:MinPoolSize",
                                                          ApplicationConstants.Database.Connection.MinPoolSize),
                
                ConnectionTimeout = _configuration.GetValue<int>("Database:ConnectionTimeout",
                                                                ApplicationConstants.Database.Connection.ConnectionTimeout),
                
                CommandTimeout = _configuration.GetValue<int>("Database:CommandTimeout",
                                                             ApplicationConstants.Database.Connection.CommandTimeout),
                
                EnableRetryOnFailure = _configuration.GetValue<bool>("Database:EnableRetryOnFailure",
                                                                    ApplicationConstants.Database.Connection.EnableRetryOnFailure),
                
                MaxRetryCount = _configuration.GetValue<int>("Database:MaxRetryCount",
                                                            ApplicationConstants.Database.Connection.MaxRetryCount),
                
                MaxQueryTimeout = _configuration.GetValue<int>("Database:MaxQueryTimeout",
                                                              ApplicationConstants.Database.Query.MaxQueryTimeout),
                
                MaxResultSetSize = _configuration.GetValue<int>("Database:MaxResultSetSize",
                                                               ApplicationConstants.Database.Query.MaxResultSetSize),
                
                EnableQueryCache = _configuration.GetValue<bool>("Database:EnableQueryCache",
                                                                ApplicationConstants.Database.Query.EnableQueryCache),
                
                CacheExpirationMinutes = _configuration.GetValue<int>("Database:CacheExpirationMinutes",
                                                                     ApplicationConstants.Database.Query.CacheExpirationMinutes)
            };
        }

        /// <summary>
        /// 獲取關係圖譜設定
        /// 改善重點：新增關係圖譜配置支援
        /// </summary>
        public RelationshipGraphConfiguration GetRelationshipGraphConfiguration()
        {
            return new RelationshipGraphConfiguration
            {
                DefaultAnalysisDepth = _configuration.GetValue<int>("RelationshipGraph:DefaultAnalysisDepth",
                                                                   ApplicationConstants.RelationshipGraph.DefaultAnalysisDepth),
                
                MaxAnalysisDepth = _configuration.GetValue<int>("RelationshipGraph:MaxAnalysisDepth",
                                                               ApplicationConstants.RelationshipGraph.MaxAnalysisDepth),
                
                MinAnalysisDepth = _configuration.GetValue<int>("RelationshipGraph:MinAnalysisDepth",
                                                               ApplicationConstants.RelationshipGraph.MinAnalysisDepth),
                
                MaxGraphNodes = _configuration.GetValue<int>("RelationshipGraph:MaxGraphNodes",
                                                            ApplicationConstants.RelationshipGraph.MaxGraphNodes),
                
                MaxGraphLinks = _configuration.GetValue<int>("RelationshipGraph:MaxGraphLinks",
                                                            ApplicationConstants.RelationshipGraph.MaxGraphLinks),
                
                MaxConcurrentAnalysis = _configuration.GetValue<int>("RelationshipGraph:MaxConcurrentAnalysis",
                                                                    ApplicationConstants.RelationshipGraph.Analysis.MaxConcurrentAnalysis),
                
                AnalysisTimeoutMinutes = _configuration.GetValue<int>("RelationshipGraph:AnalysisTimeoutMinutes",
                                                                     ApplicationConstants.RelationshipGraph.Analysis.AnalysisTimeoutMinutes),
                
                EnableCaching = _configuration.GetValue<bool>("RelationshipGraph:EnableCaching",
                                                             ApplicationConstants.RelationshipGraph.Analysis.EnableCaching),
                
                CacheExpirationMinutes = _configuration.GetValue<int>("RelationshipGraph:CacheExpirationMinutes",
                                                                     ApplicationConstants.RelationshipGraph.Analysis.CacheExpirationMinutes),
                
                MinRelationshipStrength = _configuration.GetValue<double>("RelationshipGraph:MinRelationshipStrength",
                                                                         ApplicationConstants.RelationshipGraph.Analysis.MinRelationshipStrength),
                
                MaxFamilyDepth = _configuration.GetValue<int>("RelationshipGraph:MaxFamilyDepth",
                                                             ApplicationConstants.RelationshipGraph.Analysis.MaxFamilyDepth),
                
                MaxFriendConnections = _configuration.GetValue<int>("RelationshipGraph:MaxFriendConnections",
                                                                   ApplicationConstants.RelationshipGraph.Analysis.MaxFriendConnections)
            };
        }

        /// <summary>
        /// 獲取 Excel 處理設定
        /// 改善重點：新增 Excel 處理配置支援
        /// </summary>
        public ExcelConfiguration GetExcelConfiguration()
        {
            return new ExcelConfiguration
            {
                DefaultWorksheetIndex = _configuration.GetValue<int>("Excel:DefaultWorksheetIndex",
                                                                     ApplicationConstants.Excel.DefaultWorksheetIndex),
                
                DataStartRow = _configuration.GetValue<int>("Excel:DataStartRow",
                                                           ApplicationConstants.Excel.DataStartRow),
                
                MaxProcessingRows = _configuration.GetValue<int>("Excel:MaxProcessingRows",
                                                                ApplicationConstants.Excel.MaxProcessingRows),
                
                BatchSize = _configuration.GetValue<int>("Excel:BatchSize",
                                                        ApplicationConstants.Excel.Processing.BatchSize),
                
                MaxConcurrentProcessing = _configuration.GetValue<int>("Excel:MaxConcurrentProcessing",
                                                                      ApplicationConstants.Excel.Processing.MaxConcurrentProcessing),
                
                ProcessingTimeoutMinutes = _configuration.GetValue<int>("Excel:ProcessingTimeoutMinutes",
                                                                       ApplicationConstants.Excel.Processing.ProcessingTimeoutMinutes),
                
                EnableDataValidation = _configuration.GetValue<bool>("Excel:EnableDataValidation",
                                                                    ApplicationConstants.Excel.Processing.EnableDataValidation),
                
                EnableDuplicateCheck = _configuration.GetValue<bool>("Excel:EnableDuplicateCheck",
                                                                    ApplicationConstants.Excel.Processing.EnableDuplicateCheck)
            };
        }

        /// <summary>
        /// 獲取安全性設定
        /// 改善重點：新增安全性配置支援
        /// </summary>
        public SecurityConfiguration GetSecurityConfiguration()
        {
            return new SecurityConfiguration
            {
                SessionTimeoutMinutes = _configuration.GetValue<int>("Security:Authentication:SessionTimeoutMinutes",
                                                                    ApplicationConstants.Security.Authentication.SessionTimeoutMinutes),
                
                MaxLoginAttempts = _configuration.GetValue<int>("Security:Authentication:MaxLoginAttempts",
                                                               ApplicationConstants.Security.Authentication.MaxLoginAttempts),
                
                LockoutDurationMinutes = _configuration.GetValue<int>("Security:Authentication:LockoutDurationMinutes",
                                                                     ApplicationConstants.Security.Authentication.LockoutDurationMinutes),
                
                EnableTwoFactorAuth = _configuration.GetValue<bool>("Security:Authentication:EnableTwoFactorAuth",
                                                                   ApplicationConstants.Security.Authentication.EnableTwoFactorAuth),
                
                PasswordMinLength = _configuration.GetValue<int>("Security:Authentication:PasswordMinLength",
                                                                ApplicationConstants.Security.Authentication.PasswordMinLength),
                
                RequireSpecialCharacters = _configuration.GetValue<bool>("Security:Authentication:RequireSpecialCharacters",
                                                                        ApplicationConstants.Security.Authentication.RequireSpecialCharacters),
                
                AdminRole = _configuration.GetValue<string>("Security:Authorization:AdminRole",
                                                           ApplicationConstants.Security.Authorization.AdminRole),
                
                UserRole = _configuration.GetValue<string>("Security:Authorization:UserRole",
                                                          ApplicationConstants.Security.Authorization.UserRole),
                
                GuestRole = _configuration.GetValue<string>("Security:Authorization:GuestRole",
                                                           ApplicationConstants.Security.Authorization.GuestRole),
                
                EnableRoleBasedAccess = _configuration.GetValue<bool>("Security:Authorization:EnableRoleBasedAccess",
                                                                     ApplicationConstants.Security.Authorization.EnableRoleBasedAccess),
                
                EnableResourceLevelAccess = _configuration.GetValue<bool>("Security:Authorization:EnableResourceLevelAccess",
                                                                         ApplicationConstants.Security.Authorization.EnableResourceLevelAccess),
                
                EnableDataEncryption = _configuration.GetValue<bool>("Security:DataProtection:EnableDataEncryption",
                                                                    ApplicationConstants.Security.DataProtection.EnableDataEncryption),
                
                EnableAuditLogging = _configuration.GetValue<bool>("Security:DataProtection:EnableAuditLogging",
                                                                  ApplicationConstants.Security.DataProtection.EnableAuditLogging),
                
                AuditLogRetentionDays = _configuration.GetValue<int>("Security:DataProtection:AuditLogRetentionDays",
                                                                    ApplicationConstants.Security.DataProtection.AuditLogRetentionDays),
                
                MaskSensitiveData = _configuration.GetValue<bool>("Security:DataProtection:MaskSensitiveData",
                                                                 ApplicationConstants.Security.DataProtection.MaskSensitiveData)
            };
        }

        /// <summary>
        /// 獲取效能設定
        /// 改善重點：新增效能配置支援
        /// </summary>
        public PerformanceConfiguration GetPerformanceConfiguration()
        {
            return new PerformanceConfiguration
            {
                EnableMemoryCache = _configuration.GetValue<bool>("Performance:Caching:EnableMemoryCache",
                                                                 ApplicationConstants.Performance.Caching.EnableMemoryCache),
                
                MemoryCacheSizeMB = _configuration.GetValue<int>("Performance:Caching:MemoryCacheSizeMB",
                                                                ApplicationConstants.Performance.Caching.MemoryCacheSizeMB),
                
                EnableDistributedCache = _configuration.GetValue<bool>("Performance:Caching:EnableDistributedCache",
                                                                      ApplicationConstants.Performance.Caching.EnableDistributedCache),
                
                CacheExpirationMinutes = _configuration.GetValue<int>("Performance:Caching:CacheExpirationMinutes",
                                                                     ApplicationConstants.Performance.Caching.CacheExpirationMinutes),
                
                EnableCacheCompression = _configuration.GetValue<bool>("Performance:Caching:EnableCacheCompression",
                                                                      ApplicationConstants.Performance.Caching.EnableCacheCompression),
                
                MaxConcurrentRequests = _configuration.GetValue<int>("Performance:Concurrency:MaxConcurrentRequests",
                                                                    ApplicationConstants.Performance.Concurrency.MaxConcurrentRequests),
                
                MaxConcurrentDatabaseConnections = _configuration.GetValue<int>("Performance:Concurrency:MaxConcurrentDatabaseConnections",
                                                                               ApplicationConstants.Performance.Concurrency.MaxConcurrentDatabaseConnections),
                
                RequestTimeoutSeconds = _configuration.GetValue<int>("Performance:Concurrency:RequestTimeoutSeconds",
                                                                    ApplicationConstants.Performance.Concurrency.RequestTimeoutSeconds),
                
                EnableRequestThrottling = _configuration.GetValue<bool>("Performance:Concurrency:EnableRequestThrottling",
                                                                       ApplicationConstants.Performance.Concurrency.EnableRequestThrottling),
                
                ThrottleLimit = _configuration.GetValue<int>("Performance:Concurrency:ThrottleLimit",
                                                            ApplicationConstants.Performance.Concurrency.ThrottleLimit)
            };
        }

        /// <summary>
        /// 獲取環境設定
        /// 改善重點：新增環境配置支援
        /// </summary>
        public EnvironmentConfiguration GetEnvironmentConfiguration()
        {
            return new EnvironmentConfiguration
            {
                Name = _configuration.GetValue<string>("Environment:Name", _environment.EnvironmentName),
                
                EnableDebugMode = _configuration.GetValue<bool>("Environment:EnableDebugMode",
                                                               ApplicationConstants.Environment.Configuration.EnableDebugMode),
                
                EnableDetailedLogging = _configuration.GetValue<bool>("Environment:EnableDetailedLogging",
                                                                     ApplicationConstants.Environment.Configuration.EnableDetailedLogging),
                
                EnablePerformanceMonitoring = _configuration.GetValue<bool>("Environment:EnablePerformanceMonitoring",
                                                                           ApplicationConstants.Environment.Configuration.EnablePerformanceMonitoring),
                
                EnableErrorReporting = _configuration.GetValue<bool>("Environment:EnableErrorReporting",
                                                                    ApplicationConstants.Environment.Configuration.EnableErrorReporting),
                
                DefaultTimeZone = _configuration.GetValue<string>("Environment:DefaultTimeZone",
                                                                 ApplicationConstants.Environment.Configuration.DefaultTimeZone)
            };
        }

        /// <summary>
        /// 獲取 API 設定
        /// 改善重點：新增 API 配置支援
        /// </summary>
        public ApiConfiguration GetApiConfiguration()
        {
            return new ApiConfiguration
            {
                Version = _configuration.GetValue<string>("API:Version", "1.0.0"),
                Title = _configuration.GetValue<string>("API:Title", "FamilyTree API"),
                Description = _configuration.GetValue<string>("API:Description", "家族樹管理系統 API"),
                
                ContactName = _configuration.GetValue<string>("API:Contact:Name", "FamilyTree Team"),
                ContactEmail = _configuration.GetValue<string>("API:Contact:Email", "support@familytree.com"),
                
                LicenseName = _configuration.GetValue<string>("API:License:Name", "MIT"),
                LicenseUrl = _configuration.GetValue<string>("API:License:Url", "https://opensource.org/licenses/MIT"),
                
                IncludeTimestamp = _configuration.GetValue<bool>("API:ResponseFormat:IncludeTimestamp", true),
                IncludeRequestId = _configuration.GetValue<bool>("API:ResponseFormat:IncludeRequestId", true),
                EnableDetailedErrors = _configuration.GetValue<bool>("API:ResponseFormat:EnableDetailedErrors", false)
            };
        }

        /// <summary>
        /// 獲取監控設定
        /// 改善重點：新增監控配置支援
        /// </summary>
        public MonitoringConfiguration GetMonitoringConfiguration()
        {
            return new MonitoringConfiguration
            {
                EnableHealthChecks = _configuration.GetValue<bool>("Monitoring:EnableHealthChecks", true),
                EnableMetrics = _configuration.GetValue<bool>("Monitoring:EnableMetrics", true),
                EnableTracing = _configuration.GetValue<bool>("Monitoring:EnableTracing", false),
                
                HealthCheckTimeout = _configuration.GetValue<int>("Monitoring:HealthCheckTimeout", 30),
                MetricsInterval = _configuration.GetValue<int>("Monitoring:MetricsInterval", 60),
                
                EnableApplicationInsights = _configuration.GetValue<bool>("Monitoring:EnableApplicationInsights", false),
                ApplicationInsightsKey = _configuration.GetValue<string>("Monitoring:ApplicationInsightsKey", "")
            };
        }

        /// <summary>
        /// 獲取 OpenAI 設定
        /// 改善重點：新增 OpenAI 配置支援
        /// </summary>
        public OpenAIConfiguration GetOpenAIConfiguration()
        {
            return new OpenAIConfiguration
            {
                ApiKey = _configuration.GetValue<string>("OpenAI:ApiKey", ""),
                Model = _configuration.GetValue<string>("OpenAI:Model", "gpt-3.5-turbo"),
                MaxTokens = _configuration.GetValue<int>("OpenAI:MaxTokens", 2000),
                Temperature = _configuration.GetValue<double>("OpenAI:Temperature", 0.7),
                TimeoutSeconds = _configuration.GetValue<int>("OpenAI:TimeoutSeconds", 60),
                EnableRetryOnFailure = _configuration.GetValue<bool>("OpenAI:EnableRetryOnFailure", true),
                MaxRetryCount = _configuration.GetValue<int>("OpenAI:MaxRetryCount", 3)
            };
        }
    }

    #region 配置模型

    /// <summary>
    /// 檔案上傳配置
    /// 改善重點：新增更多檔案上傳配置選項
    /// </summary>
    public class FileUploadConfiguration
    {
        /// <summary>
        /// 最大檔案大小（位元組）
        /// </summary>
        public long MaxFileSizeBytes { get; set; }
        
        /// <summary>
        /// 最大檔案大小（位元組） - 別名屬性
        /// </summary>
        public long MaxFileSize => MaxFileSizeBytes;

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
        /// 照片上傳目錄路徑
        /// </summary>
        public string PhotoUploadDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 是否允許覆蓋同名檔案
        /// </summary>
        public bool AllowOverwrite { get; set; }

        /// <summary>
        /// 最大並發上傳數
        /// </summary>
        public int MaxConcurrentUploads { get; set; }

        /// <summary>
        /// 上傳超時時間（秒）
        /// </summary>
        public int UploadTimeoutSeconds { get; set; }

        /// <summary>
        /// 處理超時時間（秒）
        /// </summary>
        public int ProcessingTimeoutSeconds { get; set; }

        /// <summary>
        /// 是否啟用重複檢查
        /// </summary>
        public bool EnableDuplicateCheck { get; set; }

        /// <summary>
        /// 是否啟用自動壓縮
        /// </summary>
        public bool EnableAutoCompression { get; set; }
    }

    /// <summary>
    /// 分頁配置
    /// </summary>
    public class PaginationConfiguration
    {
        /// <summary>
        /// 預設分頁大小
        /// </summary>
        public int DefaultPageSize { get; set; }

        /// <summary>
        /// 最大分頁大小
        /// </summary>
        public int MaxPageSize { get; set; }

        /// <summary>
        /// 最小分頁大小
        /// </summary>
        public int MinPageSize { get; set; }
    }

    /// <summary>
    /// 日誌配置
    /// 改善重點：新增更多日誌配置選項
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

        /// <summary>
        /// 最大日誌檔案大小（MB）
        /// </summary>
        public int MaxLogFileSizeMB { get; set; }

        /// <summary>
        /// 最大日誌檔案數量
        /// </summary>
        public int MaxLogFiles { get; set; }

        /// <summary>
        /// 是否啟用結構化日誌
        /// </summary>
        public bool EnableStructuredLogging { get; set; }

        /// <summary>
        /// 是否啟用效能日誌
        /// </summary>
        public bool EnablePerformanceLogging { get; set; }

        /// <summary>
        /// 日誌等級
        /// </summary>
        public string LogLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// 搜尋配置
    /// 改善重點：新增更多搜尋配置選項
    /// </summary>
    public class SearchConfiguration
    {
        /// <summary>
        /// 預設分頁大小
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
        /// 最小模糊匹配分數
        /// </summary>
        public double MinFuzzyMatchScore { get; set; }

        /// <summary>
        /// 最大搜尋關鍵字數量
        /// </summary>
        public int MaxSearchKeywords { get; set; }

        /// <summary>
        /// 是否啟用搜尋快取
        /// </summary>
        public bool EnableSearchCache { get; set; }

        /// <summary>
        /// 搜尋快取過期時間（分鐘）
        /// </summary>
        public int SearchCacheExpirationMinutes { get; set; }

        /// <summary>
        /// 最大熱門關鍵字數量
        /// </summary>
        public int MaxPopularKeywords { get; set; }

        /// <summary>
        /// 最大搜尋歷史項目數量
        /// </summary>
        public int MaxSearchHistoryItems { get; set; }
        
        /// <summary>
        /// 最大搜尋歷史項目數量 - 別名屬性
        /// </summary>
        public int MaxSearchHistory => MaxSearchHistoryItems;

        /// <summary>
        /// 最大頂級關鍵字數量
        /// </summary>
        public int MaxTopKeywords { get; set; }
    }

    /// <summary>
    /// 資料庫配置
    /// 改善重點：新增資料庫配置支援
    /// </summary>
    public class DatabaseConfiguration
    {
        public int MaxPoolSize { get; set; }
        public int MinPoolSize { get; set; }
        public int ConnectionTimeout { get; set; }
        public int CommandTimeout { get; set; }
        public bool EnableRetryOnFailure { get; set; }
        public int MaxRetryCount { get; set; }
        public int MaxQueryTimeout { get; set; }
        public int MaxResultSetSize { get; set; }
        public bool EnableQueryCache { get; set; }
        public int CacheExpirationMinutes { get; set; }
    }

    /// <summary>
    /// 關係圖譜配置
    /// 改善重點：新增關係圖譜配置支援
    /// </summary>
    public class RelationshipGraphConfiguration
    {
        public int DefaultAnalysisDepth { get; set; }
        public int MaxAnalysisDepth { get; set; }
        public int MinAnalysisDepth { get; set; }
        public int MaxGraphNodes { get; set; }
        public int MaxGraphLinks { get; set; }
        public int MaxConcurrentAnalysis { get; set; }
        public int AnalysisTimeoutMinutes { get; set; }
        public bool EnableCaching { get; set; }
        public int CacheExpirationMinutes { get; set; }
        public double MinRelationshipStrength { get; set; }
        public int MaxFamilyDepth { get; set; }
        public int MaxFriendConnections { get; set; }
    }

    /// <summary>
    /// Excel 配置
    /// 改善重點：新增 Excel 配置支援
    /// </summary>
    public class ExcelConfiguration
    {
        public int DefaultWorksheetIndex { get; set; }
        public int DataStartRow { get; set; }
        public int MaxProcessingRows { get; set; }
        public int BatchSize { get; set; }
        public int MaxConcurrentProcessing { get; set; }
        public int ProcessingTimeoutMinutes { get; set; }
        public bool EnableDataValidation { get; set; }
        public bool EnableDuplicateCheck { get; set; }
    }

    /// <summary>
    /// 安全性配置
    /// 改善重點：新增安全性配置支援
    /// </summary>
    public class SecurityConfiguration
    {
        public int SessionTimeoutMinutes { get; set; }
        public int MaxLoginAttempts { get; set; }
        public int LockoutDurationMinutes { get; set; }
        public bool EnableTwoFactorAuth { get; set; }
        public int PasswordMinLength { get; set; }
        public bool RequireSpecialCharacters { get; set; }
        public string AdminRole { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string GuestRole { get; set; } = string.Empty;
        public bool EnableRoleBasedAccess { get; set; }
        public bool EnableResourceLevelAccess { get; set; }
        public bool EnableDataEncryption { get; set; }
        public bool EnableAuditLogging { get; set; }
        public int AuditLogRetentionDays { get; set; }
        public bool MaskSensitiveData { get; set; }
        
        // 新增缺失的屬性
        public long MaxRequestSize { get; set; } = 10 * 1024 * 1024; // 10MB
        public bool EnableIpWhitelist { get; set; } = false;
        public bool ValidateReferer { get; set; } = false;
    }

    /// <summary>
    /// 效能配置
    /// 改善重點：新增效能配置支援
    /// </summary>
    public class PerformanceConfiguration
    {
        public bool EnableMemoryCache { get; set; }
        public int MemoryCacheSizeMB { get; set; }
        public bool EnableDistributedCache { get; set; }
        public int CacheExpirationMinutes { get; set; }
        public bool EnableCacheCompression { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public int MaxConcurrentDatabaseConnections { get; set; }
        public int RequestTimeoutSeconds { get; set; }
        public bool EnableRequestThrottling { get; set; }
        public int ThrottleLimit { get; set; }
        
        // 新增缺失的屬性
        public int MaxMemoryUsageMB { get; set; } = 1024;
        public int MaxManagedMemoryMB { get; set; } = 512;
        public int MaxCacheItems { get; set; } = 10000;
        public int MaxCacheMemoryMB { get; set; } = 100;
        public int CacheCleanupIntervalMinutes { get; set; } = 15;
        public int QueryCacheExpirationMinutes { get; set; } = 30;
        public int SlowQueryThresholdMs { get; set; } = 1000;
    }

    /// <summary>
    /// 環境配置
    /// 改善重點：新增環境配置支援
    /// </summary>
    public class EnvironmentConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public bool EnableDebugMode { get; set; }
        public bool EnableDetailedLogging { get; set; }
        public bool EnablePerformanceMonitoring { get; set; }
        public bool EnableErrorReporting { get; set; }
        public string DefaultTimeZone { get; set; } = string.Empty;
    }

    /// <summary>
    /// API 配置
    /// 改善重點：新增 API 配置支援
    /// </summary>
    public class ApiConfiguration
    {
        public string Version { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string LicenseName { get; set; } = string.Empty;
        public string LicenseUrl { get; set; } = string.Empty;
        public bool IncludeTimestamp { get; set; }
        public bool IncludeRequestId { get; set; }
        public bool EnableDetailedErrors { get; set; }
    }

    /// <summary>
    /// 監控配置
    /// 改善重點：新增監控配置支援
    /// </summary>
    public class MonitoringConfiguration
    {
        public bool EnableHealthChecks { get; set; }
        public bool EnableMetrics { get; set; }
        public bool EnableTracing { get; set; }
        public int HealthCheckTimeout { get; set; }
        public int MetricsInterval { get; set; }
        public bool EnableApplicationInsights { get; set; }
        public string ApplicationInsightsKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// OpenAI 配置
    /// 改善重點：新增 OpenAI 配置支援
    /// </summary>
    public class OpenAIConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
        public int TimeoutSeconds { get; set; }
        public bool EnableRetryOnFailure { get; set; }
        public int MaxRetryCount { get; set; }
    }

    #endregion
} 