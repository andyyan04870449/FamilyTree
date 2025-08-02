using System.Net;

namespace familytree_backend.Models.Exceptions
{
    /// <summary>
    /// 應用程式基礎異常類別
    /// 所有自定義異常都應該繼承此類別
    /// </summary>
    public abstract class BaseApplicationException : Exception
    {
        /// <summary>
        /// HTTP 狀態碼
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// 錯誤代碼（用於API回應和前端處理）
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// 額外的錯誤詳情（僅在開發環境顯示）
        /// </summary>
        public object? Details { get; }

        /// <summary>
        /// 是否應該記錄到錯誤日誌
        /// </summary>
        public bool ShouldLog { get; }

        /// <summary>
        /// 基礎應用程式異常建構子
        /// </summary>
        /// <param name="message">錯誤訊息</param>
        /// <param name="statusCode">HTTP 狀態碼</param>
        /// <param name="errorCode">錯誤代碼</param>
        /// <param name="details">額外詳情</param>
        /// <param name="shouldLog">是否記錄日誌</param>
        /// <param name="innerException">內部異常</param>
        protected BaseApplicationException(
            string message,
            HttpStatusCode statusCode,
            string errorCode,
            object? details = null,
            bool shouldLog = true,
            Exception? innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Details = details;
            ShouldLog = shouldLog;
        }
    }
}