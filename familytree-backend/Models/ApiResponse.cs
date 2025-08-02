// 通用 API 回應模型 - 定義所有 API 回應的基礎結構
using System.Net;
using familytree_backend.Models.Exceptions;

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
        /// 錯誤代碼（僅在失敗時提供）
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 錯誤詳情（僅在失敗時提供）
        /// </summary>
        public object? Details { get; set; }

        /// <summary>
        /// 請求ID（用於錯誤追蹤）
        /// </summary>
        public string? RequestId { get; set; }

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
        public static ApiResponse ErrorResult(string message, object? details = null, string? errorCode = null, string? requestId = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                Details = details,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 從自定義異常建立錯誤回應
        /// </summary>
        public static ApiResponse FromException(BaseApplicationException exception, string? requestId = null, bool includeDetails = false)
        {
            return new ApiResponse
            {
                Success = false,
                Message = exception.Message,
                ErrorCode = exception.ErrorCode,
                Details = includeDetails ? exception.Details : null,
                RequestId = requestId,
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
        public static ApiResponse<T> ErrorResult(string message, T? data = default, object? details = null, string? errorCode = null, string? requestId = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = data,
                ErrorCode = errorCode,
                Details = details,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 從自定義異常建立錯誤回應
        /// </summary>
        public static ApiResponse<T> FromException(BaseApplicationException exception, T? data = default, string? requestId = null, bool includeDetails = false)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = exception.Message,
                Data = data,
                ErrorCode = exception.ErrorCode,
                Details = includeDetails ? exception.Details : null,
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 分頁回應類別
    /// </summary>
    /// <typeparam name="T">資料項目類型</typeparam>
    public class PagedApiResponse<T> : ApiResponse<IEnumerable<T>>
    {
        /// <summary>
        /// 分頁資訊
        /// </summary>
        public PaginationInfo Pagination { get; set; } = new();

        /// <summary>
        /// 建立分頁成功回應
        /// </summary>
        public static PagedApiResponse<T> SuccessResult(
            IEnumerable<T> data, 
            int totalCount, 
            int page, 
            int pageSize, 
            string message = "操作成功")
        {
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            return new PagedApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Pagination = new PaginationInfo
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages,
                    HasPreviousPage = page > 1
                },
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 分頁資訊
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// 目前頁碼
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 每頁項目數
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 總項目數
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 總頁數
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 是否有下一頁
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// 是否有上一頁
        /// </summary>
        public bool HasPreviousPage { get; set; }
    }
} 