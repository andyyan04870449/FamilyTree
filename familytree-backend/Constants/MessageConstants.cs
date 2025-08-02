// 訊息常數定義 - 統一管理所有系統訊息
// 設計理念：集中管理所有使用者可見的訊息，便於維護和多語言支援

namespace familytree_backend.Constants
{
    /// <summary>
    /// 系統訊息常數
    /// </summary>
    public static class MessageConstants
    {
        /// <summary>
        /// 成功訊息
        /// </summary>
        public static class Success
        {
            // 通用成功訊息
            public const string OperationSuccess = "操作成功";
            public const string DataRetrieved = "資料獲取成功";
            public const string DataCreated = "資料建立成功";
            public const string DataUpdated = "資料更新成功";
            public const string DataDeleted = "資料刪除成功";
            public const string DataSaved = "資料儲存成功";
            
            // 檔案相關
            public const string FileUploaded = "檔案上傳成功";
            public const string FileDeleted = "檔案刪除成功";
            public const string FileProcessed = "檔案處理成功";
            public const string FileDownloaded = "檔案下載成功";
            
            // 專案相關
            public const string ProjectCreated = "專案建立成功";
            public const string ProjectUpdated = "專案更新成功";
            public const string ProjectDeleted = "專案刪除成功";
            public const string ProjectRetrieved = "專案資料獲取成功";
            public const string ProjectListRetrieved = "專案列表獲取成功";
            
            // 人員相關
            public const string PersonCreated = "人員資料建立成功";
            public const string PersonUpdated = "人員資料更新成功";
            public const string PersonDeleted = "人員資料刪除成功";
            public const string PersonRetrieved = "人員資料獲取成功";
            public const string PersonListRetrieved = "人員列表獲取成功";
            
            // 關係相關
            public const string RelationshipCreated = "關係建立成功";
            public const string RelationshipUpdated = "關係更新成功";
            public const string RelationshipDeleted = "關係刪除成功";
            public const string RelationshipAnalyzed = "關係分析成功";
            
            // 收藏相關
            public const string FavoriteAdded = "收藏成功";
            public const string FavoriteRemoved = "取消收藏成功";
            public const string FavoritesCleared = "清空收藏成功";
            public const string FavoriteListRetrieved = "收藏列表獲取成功";
            
            // 搜尋相關
            public const string SearchCompleted = "搜尋完成";
            public const string SearchResultsFound = "找到符合的搜尋結果";
            public const string NoSearchResults = "沒有找到符合的結果";
            
            // 認證相關
            public const string LoginSuccess = "登入成功";
            public const string LogoutSuccess = "登出成功";
            public const string PasswordChanged = "密碼變更成功";
            public const string PasswordReset = "密碼重設成功";
            
            // 權限相關
            public const string PermissionGranted = "權限授予成功";
            public const string PermissionRevoked = "權限撤銷成功";
            public const string RoleAssigned = "角色指派成功";
            public const string RoleRemoved = "角色移除成功";
        }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public static class Error
        {
            // 通用錯誤訊息
            public const string OperationFailed = "操作失敗";
            public const string SystemError = "系統錯誤";
            public const string UnknownError = "發生未知錯誤";
            public const string InvalidRequest = "無效的請求";
            public const string InvalidParameters = "參數錯誤";
            public const string ValidationFailed = "資料驗證失敗";
            public const string DatabaseError = "資料庫操作失敗";
            public const string NetworkError = "網路連線錯誤";
            public const string Timeout = "操作超時";
            
            // 資料相關
            public const string DataNotFound = "找不到資料";
            public const string DataAlreadyExists = "資料已存在";
            public const string DataUpdateFailed = "資料更新失敗";
            public const string DataDeleteFailed = "資料刪除失敗";
            public const string DataCreateFailed = "資料建立失敗";
            public const string DataConflict = "資料衝突";
            
            // 檔案相關
            public const string FileNotFound = "找不到檔案";
            public const string FileTypeNotSupported = "不支援的檔案格式";
            public const string FileSizeExceeded = "檔案大小超過限制";
            public const string FileUploadFailed = "檔案上傳失敗";
            public const string FileProcessingFailed = "檔案處理失敗";
            public const string FileDuplicate = "檔案已存在";
            public const string FileCorrupted = "檔案損壞";
            
            // 專案相關
            public const string ProjectNotFound = "找不到專案";
            public const string ProjectNameDuplicate = "專案名稱已存在";
            public const string ProjectCreateFailed = "專案建立失敗";
            public const string ProjectUpdateFailed = "專案更新失敗";
            public const string ProjectDeleteFailed = "專案刪除失敗";
            public const string ProjectNotSelected = "請先選擇專案";
            public const string ProjectAccessDenied = "無權存取此專案";
            
            // 人員相關
            public const string PersonNotFound = "找不到人員資料";
            public const string PersonDuplicate = "人員資料已存在";
            public const string PersonCreateFailed = "人員資料建立失敗";
            public const string PersonUpdateFailed = "人員資料更新失敗";
            public const string PersonDeleteFailed = "人員資料刪除失敗";
            
            // 認證相關
            public const string Unauthorized = "未授權";
            public const string AuthenticationFailed = "認證失敗";
            public const string InvalidCredentials = "帳號或密碼錯誤";
            public const string AccountLocked = "帳號已鎖定";
            public const string AccountDisabled = "帳號已停用";
            public const string SessionExpired = "登入已過期";
            public const string TokenInvalid = "無效的憑證";
            public const string TokenExpired = "憑證已過期";
            
            // 權限相關
            public const string AccessDenied = "存取被拒絕";
            public const string InsufficientPermissions = "權限不足";
            public const string RoleNotFound = "找不到角色";
            public const string PermissionNotFound = "找不到權限";
            public const string CannotModifySuperAdmin = "無法修改超級管理員";
            
            // 收藏相關
            public const string FavoriteNotFound = "找不到收藏";
            public const string FavoriteAlreadyExists = "已經收藏過了";
            public const string FavoriteAddFailed = "收藏失敗";
            public const string FavoriteRemoveFailed = "取消收藏失敗";
            
            // 驗證相關
            public const string RequiredField = "此欄位為必填";
            public const string InvalidFormat = "格式不正確";
            public const string InvalidEmail = "電子郵件格式不正確";
            public const string InvalidPhone = "電話號碼格式不正確";
            public const string InvalidDate = "日期格式不正確";
            public const string InvalidLength = "長度不符合要求";
            public const string InvalidRange = "數值超出範圍";
            public const string PasswordTooWeak = "密碼強度不足";
            public const string PasswordMismatch = "密碼不一致";
        }

        /// <summary>
        /// 資訊訊息
        /// </summary>
        public static class Info
        {
            public const string Processing = "處理中...";
            public const string Loading = "載入中...";
            public const string Saving = "儲存中...";
            public const string Uploading = "上傳中...";
            public const string Downloading = "下載中...";
            public const string Analyzing = "分析中...";
            public const string Searching = "搜尋中...";
            public const string PleaseWait = "請稍候...";
            public const string NoData = "暫無資料";
            public const string EndOfData = "已載入所有資料";
        }

        /// <summary>
        /// 警告訊息
        /// </summary>
        public static class Warning
        {
            public const string DataWillBeLost = "資料將會遺失";
            public const string UnsavedChanges = "有未儲存的變更";
            public const string IrreversibleAction = "此操作無法復原";
            public const string LimitExceeded = "已達到限制";
            public const string PartialSuccess = "部分操作成功";
            public const string DeprecatedFeature = "此功能即將停用";
            public const string MaintenanceMode = "系統維護中";
        }

        /// <summary>
        /// 確認訊息
        /// </summary>
        public static class Confirm
        {
            public const string DeleteConfirm = "確定要刪除嗎？";
            public const string SaveConfirm = "確定要儲存嗎？";
            public const string CancelConfirm = "確定要取消嗎？";
            public const string ExitConfirm = "確定要離開嗎？";
            public const string ClearConfirm = "確定要清空嗎？";
            public const string ResetConfirm = "確定要重設嗎？";
            public const string OverwriteConfirm = "確定要覆蓋嗎？";
        }

        /// <summary>
        /// 格式化訊息方法
        /// </summary>
        public static class Format
        {
            public static string WithCount(string message, int count)
            {
                return $"{message}，共 {count} 筆";
            }

            public static string WithName(string message, string name)
            {
                return $"{message}：{name}";
            }

            public static string WithTime(string message, DateTime time)
            {
                return $"{message} ({time:yyyy-MM-dd HH:mm:ss})";
            }

            public static string WithReason(string message, string reason)
            {
                return $"{message}，原因：{reason}";
            }

            public static string WithDetails(string message, string details)
            {
                return $"{message}。{details}";
            }

            public static string SuccessWithCount(string itemName, int count)
            {
                return $"成功處理 {count} 個{itemName}";
            }

            public static string FailedWithCount(string itemName, int successCount, int failedCount)
            {
                return $"處理{itemName}：成功 {successCount} 個，失敗 {failedCount} 個";
            }
        }
    }
}