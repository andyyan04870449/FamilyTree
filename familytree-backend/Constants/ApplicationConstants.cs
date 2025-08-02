// 應用程式常數定義 - 將所有硬編碼的值集中管理，提升可維護性
// 設計改善：新增缺失的常數，完善配置管理體系
namespace familytree_backend.Constants
{
    /// <summary>
    /// 應用程式全域常數
    /// 設計理念：將散佈在各處的硬編碼值集中管理，便於統一修改和維護
    /// 改善重點：新增缺失的常數，完善配置管理體系
    /// </summary>
    public static class ApplicationConstants
    {
        /// <summary>
        /// 檔案相關常數
        /// 用途：檔案上傳、處理、儲存的統一配置
        /// 改善重點：新增更多檔案處理相關常數
        /// </summary>
        public static class Files
        {
            /// <summary>
            /// 支援的檔案類型 MIME Type
            /// 設計考量：採用白名單方式，確保只接受安全的檔案格式
            /// </summary>
            public static readonly string[] AllowedMimeTypes = 
            {
                "application/vnd.ms-excel",                                                    // .xls 格式
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",         // .xlsx 格式
                "image/jpeg",                                                                  // .jpg, .jpeg 格式
                "image/png",                                                                   // .png 格式
                "application/zip",                                                             // .zip 格式
                "application/x-7z-compressed"                                                  // .7z 格式
            };

            /// <summary>
            /// 支援的檔案副檔名
            /// 設計考量：雙重驗證機制，除了 MIME Type 外還驗證副檔名
            /// 改善重點：新增圖片和壓縮檔支援
            /// </summary>
            public static readonly string[] AllowedExtensions = { 
                ".xls", ".xlsx", ".jpg", ".jpeg", ".png", ".zip", ".7z" 
            };

            /// <summary>
            /// 檔案狀態定義
            /// 設計理念：使用常數避免字串拼寫錯誤，提升程式碼品質
            /// </summary>
            public static class Status
            {
                public const string Uploaded = "uploaded";      // 已上傳，等待處理
                public const string Processing = "processing";  // 處理中
                public const string Processed = "processed";    // 已處理完成
                public const string Merged = "merged";         // 已合併到資料庫 (向後相容)
                public const string Failed = "failed";         // 處理失敗
                public const string Error = "error";           // 處理失敗 (向後相容)
                public const string Deleted = "deleted";       // 已刪除
                public const string Archived = "archived";     // 已封存
            }

            /// <summary>
            /// 檔案類型定義
            /// 設計理念：統一的檔案類型識別，避免硬編碼字串
            /// </summary>
            public static class Types
            {
                public const string Excel = "excel";
                public const string Csv = "csv";
                public const string Image = "image";
                public const string Photo = "photo";
                public const string Archive = "archive";
                public const string Document = "document";
                public const string Unknown = "unknown";
            }

            /// <summary>
            /// 檔案類型對應映射
            /// 設計理念：統一的檔案類型檢測邏輯
            /// </summary>
            public static readonly Dictionary<string, string> ExtensionToTypeMap = new()
            {
                { ".xls", Types.Excel },
                { ".xlsx", Types.Excel },
                { ".csv", Types.Csv },
                { ".jpg", Types.Photo },
                { ".jpeg", Types.Photo },
                { ".png", Types.Image },
                { ".zip", Types.Archive },
                { ".7z", Types.Archive },
                { ".pdf", Types.Document }
            };

            /// <summary>
            /// MIME 類型對應映射
            /// 設計理念：統一的 MIME 類型檢測邏輯
            /// </summary>
            public static readonly Dictionary<string, string> MimeTypeToTypeMap = new()
            {
                { "application/vnd.ms-excel", Types.Excel },
                { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Types.Excel },
                { "text/csv", Types.Csv },
                { "image/jpeg", Types.Photo },
                { "image/png", Types.Image },
                { "application/zip", Types.Archive },
                { "application/x-7z-compressed", Types.Archive },
                { "application/pdf", Types.Document }
            };

            /// <summary>
            /// 取得檔案類型（根據副檔名）
            /// </summary>
            public static string GetFileTypeByExtension(string fileName)
            {
                if (string.IsNullOrEmpty(fileName)) return Types.Unknown;
                
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                return ExtensionToTypeMap.TryGetValue(extension, out var fileType) ? fileType : Types.Unknown;
            }

            /// <summary>
            /// 取得檔案類型（根據 MIME 類型）
            /// </summary>
            public static string GetFileTypeByMimeType(string mimeType)
            {
                if (string.IsNullOrEmpty(mimeType)) return Types.Unknown;
                
                return MimeTypeToTypeMap.TryGetValue(mimeType, out var fileType) ? fileType : Types.Unknown;
            }

            /// <summary>
            /// 取得 MIME 類型（根據副檔名）
            /// </summary>
            public static string GetMimeTypeByExtension(string fileName)
            {
                if (string.IsNullOrEmpty(fileName)) return "application/octet-stream";
                
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                return extension switch
                {
                    ".xls" => "application/vnd.ms-excel",
                    ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ".csv" => "text/csv",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".zip" => "application/zip",
                    ".7z" => "application/x-7z-compressed",
                    ".pdf" => "application/pdf",
                    _ => "application/octet-stream"
                };
            }

            /// <summary>
            /// 檢查檔案類型是否有效
            /// </summary>
            public static bool IsValidFileType(string fileName, string? mimeType = null)
            {
                var typeByExtension = GetFileTypeByExtension(fileName);
                if (typeByExtension != Types.Unknown) return true;
                
                if (!string.IsNullOrEmpty(mimeType))
                {
                    var typeByMimeType = GetFileTypeByMimeType(mimeType);
                    return typeByMimeType != Types.Unknown;
                }
                
                return false;
            }

            /// <summary>
            /// 關聯記錄類型定義
            /// 設計理念：統一的關聯類型管理
            /// </summary>
            public static class AssociationTypes
            {
                public const string Person = "person";
                public const string Project = "project";
                public const string Analysis = "analysis";
                public const string Photo = "photo";
                public const string Document = "document";
            }

            /// <summary>
            /// 檔案大小限制（位元組）
            /// 設計考量：防止超大檔案影響系統效能和儲存空間
            /// </summary>
            public const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

            /// <summary>
            /// 上傳目錄名稱
            /// 設計理念：集中定義路徑，方便統一管理和修改
            /// </summary>
            public const string UploadDirectoryName = "user_upload";

            /// <summary>
            /// 照片上傳目錄名稱
            /// 改善重點：新增照片專用目錄
            /// </summary>
            public const string PhotoUploadDirectoryName = "photos";

            /// <summary>
            /// 檔案命名規則
            /// 改善重點：統一的檔案命名規範
            /// </summary>
            public static class Naming
            {
                public const string DateFormat = "yyyyMMddHHmmss";
                public const string PhotoIndexFormat = "000000";
                public const int MaxPhotoIndexLength = 6;
            }

            /// <summary>
            /// 檔案處理相關常數
            /// 改善重點：新增檔案處理配置
            /// </summary>
            public static class Processing
            {
                public const int MaxConcurrentUploads = 5;
                public const int UploadTimeoutSeconds = 300;
                public const int ProcessingTimeoutSeconds = 600;
                public const bool EnableDuplicateCheck = true;
                public const bool EnableAutoCompression = false;
            }
        }

        /// <summary>
        /// 資料庫相關常數
        /// 用途：資料庫操作的統一配置和約束
        /// 改善重點：新增更多資料庫相關常數
        /// </summary>
        public static class Database
        {
            /// <summary>
            /// 分頁相關設定
            /// 設計考量：防止一次查詢過多資料影響效能
            /// </summary>
            public const int DefaultPageSize = 20;
            public const int MaxPageSize = 100;
            public const int MinPageSize = 1;

            /// <summary>
            /// 專案 ID 格式驗證
            /// 設計理念：確保專案 ID 格式的一致性
            /// </summary>
            public const int ProjectIdMaxLength = 50;
            
            /// <summary>
            /// 常用欄位長度限制
            /// 設計考量：與資料庫 schema 保持一致，避免資料截斷
            /// 改善重點：新增更多欄位長度限制
            /// </summary>
            public const int NameMaxLength = 100;
            public const int EmailMaxLength = 255;
            public const int PhoneMaxLength = 20;
            public const int ProjectNameMaxLength = 200;
            public const int ProjectDescriptionMaxLength = 1000;
            public const int RelationshipTypeMaxLength = 50;
            public const int FileNameMaxLength = 255;
            public const int FilePathMaxLength = 500;

            /// <summary>
            /// 資料庫連線相關常數
            /// 改善重點：新增連線池和超時設定
            /// </summary>
            public static class Connection
            {
                public const int MaxPoolSize = 100;
                public const int MinPoolSize = 5;
                public const int ConnectionTimeout = 30;
                public const int CommandTimeout = 60;
                public const bool EnableRetryOnFailure = true;
                public const int MaxRetryCount = 3;
            }

            /// <summary>
            /// 查詢相關常數
            /// 改善重點：新增查詢限制和優化設定
            /// </summary>
            public static class Query
            {
                public const int MaxQueryTimeout = 300;
                public const int MaxResultSetSize = 10000;
                public const bool EnableQueryCache = true;
                public const int CacheExpirationMinutes = 30;
            }
        }

        /// <summary>
        /// API 回應相關常數
        /// 用途：統一 API 回應格式和訊息
        /// 改善重點：新增更多回應訊息
        /// </summary>
        public static class ApiResponse
        {
            /// <summary>
            /// 標準成功訊息
            /// 設計理念：提供一致的使用者體驗
            /// 改善重點：新增更多成功訊息
            /// </summary>
            public static class SuccessMessages
            {
                public const string DataRetrievedSuccessfully = "資料獲取成功";
                public const string DataCreatedSuccessfully = "資料建立成功";
                public const string DataUpdatedSuccessfully = "資料更新成功";
                public const string DataDeletedSuccessfully = "資料刪除成功";
                public const string FileUploadedSuccessfully = "檔案上傳成功";
                public const string FileProcessedSuccessfully = "檔案處理成功";
                public const string FileDeletedSuccessfully = "檔案刪除成功";
                public const string ProjectCreatedSuccessfully = "專案建立成功";
                public const string ProjectUpdatedSuccessfully = "專案更新成功";
                public const string ProjectDeletedSuccessfully = "專案刪除成功";
                public const string RelationshipCreatedSuccessfully = "關係建立成功";
                public const string AnalysisCompletedSuccessfully = "分析完成成功";
                public const string SearchCompletedSuccessfully = "搜尋完成成功";
            }

            /// <summary>
            /// 標準錯誤訊息
            /// 設計理念：提供一致的錯誤處理體驗
            /// 改善重點：新增更多錯誤訊息
            /// </summary>
            public static class ErrorMessages
            {
                public const string DataNotFound = "找不到指定的資料";
                public const string InvalidParameters = "參數驗證失敗";
                public const string DatabaseError = "資料庫操作失敗";
                public const string FileNotFound = "找不到指定的檔案";
                public const string FileTypeNotSupported = "不支援的檔案格式";
                public const string FileSizeExceeded = "檔案大小超過限制";
                public const string ProjectNotFound = "找不到指定的專案";
                public const string ProjectIdRequired = "請先選擇專案";
                public const string UnauthorizedAccess = "沒有權限存取此資源";
                public const string DuplicateFile = "檔案已存在";
                public const string DuplicateData = "資料已存在";
                public const string InvalidFileFormat = "檔案格式不正確";
                public const string ProcessingFailed = "處理失敗";
                public const string TimeoutError = "操作超時";
                public const string SystemError = "系統錯誤";
                public const string NetworkError = "網路連線錯誤";
                public const string ValidationError = "資料驗證失敗";
            }

            /// <summary>
            /// HTTP 狀態碼對應
            /// 改善重點：統一的 HTTP 狀態碼管理
            /// </summary>
            public static class StatusCodes
            {
                public const int Success = 200;
                public const int Created = 201;
                public const int NoContent = 204;
                public const int BadRequest = 400;
                public const int Unauthorized = 401;
                public const int Forbidden = 403;
                public const int NotFound = 404;
                public const int Conflict = 409;
                public const int InternalServerError = 500;
                public const int ServiceUnavailable = 503;
            }
        }

        /// <summary>
        /// Excel 處理相關常數
        /// 用途：Excel 檔案處理的統一配置
        /// 改善重點：新增更多 Excel 處理配置
        /// </summary>
        public static class Excel
        {
            /// <summary>
            /// Excel 欄位對應
            /// 設計理念：集中管理欄位對應關係，便於維護
            /// </summary>
            public static class ColumnMappings
            {
                public const string Name = "姓名";
                public const string Gender = "性別";
                public const string Birthday = "生日";
                public const string Mobile = "手機";
                public const string FamilyRelationships = "家庭關係";
                public const string ImportantFriends = "重要朋友";
            }

            /// <summary>
            /// 預設工作表索引
            /// 設計考量：大多數 Excel 檔案的第一個工作表包含資料
            /// </summary>
            public const int DefaultWorksheetIndex = 0;

            /// <summary>
            /// 資料開始行
            /// 設計考量：跳過標題行，從第二行開始讀取資料
            /// </summary>
            public const int DataStartRow = 2;

            /// <summary>
            /// 最大處理行數
            /// 設計考量：防止處理過大的檔案影響系統效能
            /// </summary>
            public const int MaxProcessingRows = 10000;

            /// <summary>
            /// Excel 處理相關常數
            /// 改善重點：新增更多處理配置
            /// </summary>
            public static class Processing
            {
                public const int BatchSize = 100;
                public const int MaxConcurrentProcessing = 3;
                public const int ProcessingTimeoutMinutes = 30;
                public const bool EnableDataValidation = true;
                public const bool EnableDuplicateCheck = true;
            }
        }

        /// <summary>
        /// 日誌相關常數
        /// 用途：日誌記錄的統一配置
        /// 改善重點：新增更多日誌配置
        /// </summary>
        public static class Logging
        {
            /// <summary>
            /// 日誌檔案名稱格式
            /// 設計理念：統一的日誌檔案命名規則
            /// </summary>
            public const string LogFileNameFormat = "familytree-{0}-.log";

            /// <summary>
            /// 日誌目錄名稱
            /// 設計理念：集中管理日誌檔案位置
            /// </summary>
            public const string LogDirectoryName = "logs";

            /// <summary>
            /// 日誌時間格式
            /// 設計理念：統一的時間格式，便於日誌分析
            /// </summary>
            public const string LogTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

            /// <summary>
            /// 日誌相關常數
            /// 改善重點：新增更多日誌配置
            /// </summary>
            public static class Configuration
            {
                public const int MaxLogFileSizeMB = 100;
                public const int LogRetentionDays = 30;
                public const int MaxLogFiles = 100;
                public const bool EnableStructuredLogging = true;
                public const bool EnablePerformanceLogging = true;
                public const string LogLevel = "Information";
            }
        }

        /// <summary>
        /// 搜尋相關常數
        /// 用途：搜尋功能的統一配置
        /// 改善重點：新增更多搜尋配置
        /// </summary>
        public static class Search
        {
            /// <summary>
            /// 搜尋類型定義
            /// 設計理念：使用常數避免字串拼寫錯誤
            /// </summary>
            public static class Types
            {
                public const string Exact = "exact";     // 精確搜尋
                public const string Fuzzy = "fuzzy";     // 模糊搜尋
                public const string Partial = "partial"; // 部分匹配
                public const string Regex = "regex";     // 正則表達式
            }

            /// <summary>
            /// 搜尋相關常數
            /// 改善重點：新增更多搜尋配置
            /// </summary>
            public const int MaxSearchResults = 1000;
            public const int DefaultSearchPageSize = 10;
            public const int SearchTimeoutSeconds = 30;
            public const double MinFuzzyMatchScore = 0.7;
            public const int MaxSearchKeywords = 10;
            public const bool EnableSearchCache = true;
            public const int SearchCacheExpirationMinutes = 15;
        }

        /// <summary>
        /// 關係圖譜相關常數
        /// 用途：關係圖譜分析的統一配置
        /// 改善重點：新增更多圖譜配置
        /// </summary>
        public static class RelationshipGraph
        {
            /// <summary>
            /// 圖譜分析相關常數
            /// 設計考量：平衡分析深度和效能
            /// </summary>
            public const int DefaultAnalysisDepth = 3;
            public const int MaxAnalysisDepth = 5;
            public const int MinAnalysisDepth = 1;

            /// <summary>
            /// 圖譜節點和連線限制
            /// 設計考量：防止圖譜過於複雜影響效能
            /// </summary>
            public const int MaxGraphNodes = 500;
            public const int MaxGraphLinks = 1000;

            /// <summary>
            /// 圖譜相關常數
            /// 改善重點：新增更多圖譜配置
            /// </summary>
            public static class Analysis
            {
                public const int MaxConcurrentAnalysis = 2;
                public const int AnalysisTimeoutMinutes = 10;
                public const bool EnableCaching = true;
                public const int CacheExpirationMinutes = 60;
                public const double MinRelationshipStrength = 0.1;
                public const int MaxFamilyDepth = 3;
                public const int MaxFriendConnections = 10;
            }
        }

        /// <summary>
        /// 安全性相關常數
        /// 用途：安全相關的統一配置
        /// 改善重點：新增安全性配置
        /// </summary>
        public static class Security
        {
            /// <summary>
            /// 認證相關常數
            /// 改善重點：新增認證配置
            /// </summary>
            public static class Authentication
            {
                public const int SessionTimeoutMinutes = 30;
                public const int MaxLoginAttempts = 5;
                public const int LockoutDurationMinutes = 15;
                public const bool EnableTwoFactorAuth = false;
                public const int PasswordMinLength = 8;
                public const bool RequireSpecialCharacters = true;
            }

            /// <summary>
            /// 授權相關常數
            /// 改善重點：新增授權配置
            /// </summary>
            public static class Authorization
            {
                public const string AdminRole = "Admin";
                public const string UserRole = "User";
                public const string GuestRole = "Guest";
                public const bool EnableRoleBasedAccess = true;
                public const bool EnableResourceLevelAccess = false;
            }

            /// <summary>
            /// 資料保護相關常數
            /// 改善重點：新增資料保護配置
            /// </summary>
            public static class DataProtection
            {
                public const bool EnableDataEncryption = false;
                public const bool EnableAuditLogging = true;
                public const int AuditLogRetentionDays = 90;
                public const bool MaskSensitiveData = true;
            }
        }

        /// <summary>
        /// 效能相關常數
        /// 用途：效能優化的統一配置
        /// 改善重點：新增效能配置
        /// </summary>
        public static class Performance
        {
            /// <summary>
            /// 快取相關常數
            /// 改善重點：新增快取配置
            /// </summary>
            public static class Caching
            {
                public const bool EnableMemoryCache = true;
                public const int MemoryCacheSizeMB = 100;
                public const bool EnableDistributedCache = false;
                public const int CacheExpirationMinutes = 30;
                public const bool EnableCacheCompression = true;
            }

            /// <summary>
            /// 並發相關常數
            /// 改善重點：新增並發配置
            /// </summary>
            public static class Concurrency
            {
                public const int MaxConcurrentRequests = 100;
                public const int MaxConcurrentDatabaseConnections = 20;
                public const int RequestTimeoutSeconds = 60;
                public const bool EnableRequestThrottling = true;
                public const int ThrottleLimit = 1000;
            }
        }

        /// <summary>
        /// 環境相關常數
        /// 用途：不同環境的統一配置
        /// 改善重點：新增環境配置
        /// </summary>
        public static class Environment
        {
            public const string Development = "Development";
            public const string Staging = "Staging";
            public const string Production = "Production";
            public const string Testing = "Testing";

            /// <summary>
            /// 環境特定配置
            /// 改善重點：新增環境特定配置
            /// </summary>
            public static class Configuration
            {
                public const bool EnableDebugMode = true;
                public const bool EnableDetailedLogging = true;
                public const bool EnablePerformanceMonitoring = true;
                public const bool EnableErrorReporting = true;
                public const string DefaultTimeZone = "Asia/Taipei";
            }
        }
    }
} 