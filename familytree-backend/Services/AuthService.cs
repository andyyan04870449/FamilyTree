using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using familytree_backend.Models;

namespace familytree_backend.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginDto dto, string? ipAddress = null);
        Task<bool> LogoutAsync(string userId, string token);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken);
        Task<UserModel?> RegisterAsync(RegisterDto dto);
        Task<ResetPasswordResponse> ResetPasswordAsync(string userId, string adminUserId);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly ITokenService _tokenService;
        private readonly ILoggingService _loggingService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserService userService,
            ITokenService tokenService,
            ILoggingService loggingService,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _userService = userService;
            _tokenService = tokenService;
            _loggingService = loggingService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginDto dto, string? ipAddress = null)
        {
            try
            {
                // 查找使用者（支援 username 或 email 登入）
                UserModel? user = null;
                
                if (dto.UsernameOrEmail.Contains('@'))
                {
                    user = await _userService.GetByEmailAsync(dto.UsernameOrEmail);
                }
                else
                {
                    user = await _userService.GetByUsernameAsync(dto.UsernameOrEmail);
                }

                if (user == null)
                {
                    _logger.LogWarning("Login failed - user not found: {UsernameOrEmail}", dto.UsernameOrEmail);
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "使用者名稱或密碼錯誤"
                    };
                }

                // 驗證密碼
                var isPasswordValid = await _userService.ValidatePasswordAsync(user.Id, dto.Password);
                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login failed - invalid password for user: {UserId}", user.Id);
                    
                    // 記錄失敗登入
                    await _loggingService.LogActivityAsync(
                        user.Id,
                        "login_failed",
                        "Invalid password",
                        ipAddress
                    );

                    return new LoginResponse
                    {
                        Success = false,
                        Message = "使用者名稱或密碼錯誤"
                    };
                }

                // 產生 tokens
                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.RefreshTokenExpiration);

                // 儲存 refresh token
                await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry);

                // 更新最後登入時間
                await _userService.UpdateLastLoginAsync(user.Id);

                // 記錄成功登入
                await _loggingService.LogActivityAsync(
                    user.Id,
                    "login_success",
                    "User logged in successfully",
                    ipAddress
                );

                _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

                return new LoginResponse
                {
                    Success = true,
                    Message = "登入成功",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiration),
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new LoginResponse
                {
                    Success = false,
                    Message = "登入時發生錯誤"
                };
            }
        }

        public async Task<bool> LogoutAsync(string userId, string token)
        {
            try
            {
                // 撤銷 refresh token
                var revoked = await _tokenService.RevokeRefreshTokenAsync(token);

                // 記錄登出
                await _loggingService.LogActivityAsync(
                    userId,
                    "logout",
                    "User logged out"
                );

                _logger.LogInformation("User logged out: {UserId}", userId);
                return revoked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // 驗證 refresh token
                var tokenData = await _tokenService.GetRefreshTokenAsync(refreshToken);
                if (tokenData == null)
                {
                    return new TokenResponse
                    {
                        Success = false
                    };
                }

                // 取得使用者資料
                var user = await _userService.GetByIdAsync(tokenData.UserId);
                if (user == null || user.Status != "active")
                {
                    return new TokenResponse
                    {
                        Success = false
                    };
                }

                // 產生新的 tokens
                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();
                var refreshTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.RefreshTokenExpiration);

                // 撤銷舊的 refresh token
                await _tokenService.RevokeRefreshTokenAsync(refreshToken);

                // 儲存新的 refresh token
                await _tokenService.SaveRefreshTokenAsync(user.Id, newRefreshToken, refreshTokenExpiry);

                _logger.LogInformation("Token refreshed for user: {UserId}", user.Id);

                return new TokenResponse
                {
                    Success = true,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiration)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new TokenResponse
                {
                    Success = false
                };
            }
        }

        public async Task<UserModel?> RegisterAsync(RegisterDto dto)
        {
            try
            {
                var user = await _userService.CreateAsync(dto);
                
                if (user != null)
                {
                    _logger.LogInformation("User registered successfully: {UserId}", user.Id);
                }

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                throw;
            }
        }

        public async Task<ResetPasswordResponse> ResetPasswordAsync(string userId, string adminUserId)
        {
            try
            {
                // 產生臨時密碼
                var tempPassword = await _userService.GenerateTemporaryPasswordAsync();
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

                // 更新密碼
                var success = await _userService.UpdatePasswordAsync(userId, passwordHash);

                if (success)
                {
                    // 記錄密碼重設
                    await _loggingService.LogActivityAsync(
                        adminUserId,
                        "reset_password",
                        $"Password reset by admin for user {userId}"
                    );

                    _logger.LogInformation("Password reset for user {UserId} by admin {AdminUserId}", userId, adminUserId);

                    return new ResetPasswordResponse
                    {
                        Success = true,
                        Message = "密碼已重設",
                        TemporaryPassword = tempPassword
                    };
                }

                return new ResetPasswordResponse
                {
                    Success = false,
                    Message = "密碼重設失敗"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {UserId}", userId);
                return new ResetPasswordResponse
                {
                    Success = false,
                    Message = "密碼重設時發生錯誤"
                };
            }
        }
    }
}