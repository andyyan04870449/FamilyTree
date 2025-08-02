using System;
using System.ComponentModel.DataAnnotations;

namespace familytree_backend.Models
{
    /// <summary>
    /// 使用者基本資料模型
    /// </summary>
    public class UserModel
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "user";
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// 使用者註冊 DTO
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "使用者名稱為必填")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "使用者名稱長度必須在 3-100 字元之間")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email 為必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "密碼為必填")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "密碼長度至少 8 個字元")]
        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 使用者登入 DTO
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "使用者名稱或 Email 為必填")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "密碼為必填")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// 登入回應
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserModel? User { get; set; }
    }

    /// <summary>
    /// 更新使用者資料 DTO
    /// </summary>
    public class UpdateUserDto
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Status { get; set; }
        public string? Role { get; set; } // 只有 admin 可以更新
    }

    /// <summary>
    /// 變更密碼 DTO
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "舊密碼為必填")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "新密碼為必填")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "密碼長度至少 8 個字元")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 重設密碼回應
    /// </summary>
    public class ResetPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TemporaryPassword { get; set; }
    }

    /// <summary>
    /// Token 更新 DTO
    /// </summary>
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token 為必填")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Token 回應
    /// </summary>
    public class TokenResponse
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 使用者 Token 模型
    /// </summary>
    public class UserTokenModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }

    /// <summary>
    /// 分頁結果
    /// </summary>
    public class PagedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}