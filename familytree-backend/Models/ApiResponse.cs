// 通用 API 回應模型 - 定義所有 API 回應的基礎結構
namespace familytree_backend.Models
{
    /// <summary>
    /// 通用 API 回應基類
    /// 設計理念：統一所有 API 回應的格式，提供一致的使用者體驗
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 回應訊息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 回應時間戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 錯誤詳情（僅在失敗時提供）
        /// </summary>
        public object? Details { get; set; }

        /// <summary>
        /// 建立成功回應
        /// </summary>
        public static ApiResponse SuccessResult(string message = "操作成功")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 建立錯誤回應
        /// </summary>
        public static ApiResponse ErrorResult(string message, object? details = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 泛型 API 回應類別
    /// </summary>
    /// <typeparam name="T">回應資料類型</typeparam>
    public class ApiResponse<T> : ApiResponse
    {
        /// <summary>
        /// 回應資料
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// 建立成功回應
        /// </summary>
        public static ApiResponse<T> SuccessResult(T data, string message = "操作成功")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 建立錯誤回應
        /// </summary>
        public static ApiResponse<T> ErrorResult(string message, T? data = default, object? details = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = data,
                Details = details,
                Timestamp = DateTime.UtcNow
            };
        }
    }
} 