// 應用程式常數定義 - 將所有硬編碼的值集中管理，提升可維護性
namespace familytree_backend.Constants
{
    /// <summary>
    /// 應用程式全域常數
    /// 設計理念：將散佈在各處的硬編碼值集中管理，便於統一修改和維護
    /// </summary>
    public static class ApplicationConstants
    {
        /// <summary>
        /// 檔案相關常數
        /// 用途：檔案上傳、處理、儲存的統一配置
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
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"          // .xlsx 格式
            };

            /// <summary>
            /// 支援的檔案副檔名
            /// 設計考量：雙重驗證機制，除了 MIME Type 外還驗證副檔名
            /// </summary>
            public static readonly string[] AllowedExtensions = { ".xls", ".xlsx" };

            /// <summary>
            /// 檔案狀態定義
            /// 設計理念：使用常數避免字串拼寫錯誤，提升程式碼品質
            /// </summary>
            public static class Status
            {
                public const string Uploaded = "uploaded";      // 已上傳，等待處理
                public const string Processing = "processing";  // 處理中
                public const string Merged = "merged";         // 已合併到資料庫
                public const string Error = "error";           // 處理失敗
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
        }

        /// <summary>
        /// 資料庫相關常數
        /// 用途：資料庫操作的統一配置和約束
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
            public const int ProjectIdMaxLength = 25;
            
            /// <summary>
            /// 常用欄位長度限制
            /// 設計考量：與資料庫 schema 保持一致，避免資料截斷
            /// </summary>
            public const int NameMaxLength = 100;
            public const int EmailMaxLength = 255;
            public const int PhoneMaxLength = 20;
        }

        /// <summary>
        /// API 回應相關常數
        /// 用途：統一 API 回應格式和訊息
        /// </summary>
        public static class ApiResponse
        {
            /// <summary>
            /// 標準成功訊息
            /// 設計理念：提供一致的使用者體驗
            /// </summary>
            public static class SuccessMessages
            {
                public const string DataRetrievedSuccessfully = "資料獲取成功";
                public const string DataCreatedSuccessfully = "資料建立成功";
                public const string DataUpdatedSuccessfully = "資料更新成功";
                public const string DataDeletedSuccessfully = "資料刪除成功";
                public const string FileUploadedSuccessfully = "檔案上傳成功";
                public const string FileProcessedSuccessfully = "檔案處理成功";
            }

            /// <summary>
            /// 標準錯誤訊息
            /// 設計理念：提供清楚的錯誤說明，便於問題診斷
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
            }
        }

        /// <summary>
        /// Excel 處理相關常數
        /// 用途：Excel 檔案解析和處理的統一配置
        /// </summary>
        public static class Excel
        {
            /// <summary>
            /// 預設工作表設定
            /// 設計考量：大多數檔案的資料都在第一個工作表
            /// </summary>
            public const int DefaultWorksheetIndex = 0;

            /// <summary>
            /// 資料開始行數（通常第一行是標題）
            /// 設計理念：跳過標題行，從實際資料開始處理
            /// </summary>
            public const int DataStartRow = 2;

            /// <summary>
            /// 最大處理行數限制
            /// 設計考量：防止超大檔案造成記憶體溢出
            /// </summary>
            public const int MaxProcessingRows = 10000;
        }

        /// <summary>
        /// 日誌相關常數
        /// 用途：統一日誌格式和檔案管理
        /// </summary>
        public static class Logging
        {
            /// <summary>
            /// 日誌檔案名稱格式
            /// 設計理念：包含日期的檔案名，便於日誌輪轉和管理
            /// </summary>
            public const string LogFileNameFormat = "familytree-{0}-.log";

            /// <summary>
            /// 日誌目錄名稱
            /// </summary>
            public const string LogDirectoryName = "logs";

            /// <summary>
            /// 日誌時間格式
            /// 設計考量：包含毫秒的精確時間戳，便於問題追蹤
            /// </summary>
            public const string LogTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
        }

        /// <summary>
        /// 搜尋相關常數
        /// 用途：全文檢索和搜尋功能的配置
        /// </summary>
        public static class Search
        {
            /// <summary>
            /// 搜尋類型定義
            /// 設計理念：支援不同的搜尋模式以滿足不同需求
            /// </summary>
            public static class Types
            {
                public const string Exact = "exact";     // 精確搜尋
                public const string Fuzzy = "fuzzy";     // 模糊搜尋
            }

            /// <summary>
            /// 搜尋結果限制
            /// 設計考量：平衡搜尋效能和結果完整性
            /// </summary>
            public const int MaxSearchResults = 1000;
            public const int DefaultSearchPageSize = 10;
        }

        /// <summary>
        /// 關係圖譜相關常數
        /// 用途：關係分析和圖譜生成的配置
        /// </summary>
        public static class RelationshipGraph
        {
            /// <summary>
            /// 預設分析深度
            /// 設計考量：平衡分析完整性和效能
            /// </summary>
            public const int DefaultAnalysisDepth = 3;
            public const int MaxAnalysisDepth = 5;

            /// <summary>
            /// 圖譜節點限制
            /// 設計考量：防止圖譜過於複雜影響視覺化效果
            /// </summary>
            public const int MaxGraphNodes = 500;
        }
    }
} 