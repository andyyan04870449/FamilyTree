using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using familytree_backend.Models;
using familytree_backend.Services;
using familytree_backend.Constants;

namespace familytree_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;

        public AuthController(
            ILogger<AuthController> logger,
            IConfigurationService configurationService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService,
            IAuthService authService,
            IUserService userService) 
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _authService = authService;
            _userService = userService;
        }

        /// <summary>
        /// 使用者註冊
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                LogRequestStart("Register", new { Username = dto.Username, Email = dto.Email });

                // 驗證輸入
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 檢查使用者是否已存在
                if (await _userService.UserExistsAsync(dto.Username, dto.Email))
                {
                    return CreateErrorResponse("使用者名稱或 Email 已被使用");
                }

                // 註冊使用者
                var user = await _authService.RegisterAsync(dto);
                if (user == null)
                {
                    return CreateErrorResponse("註冊失敗");
                }

                LogRequestComplete("Register");
                return CreateSuccessResponse(new { userId = user.Id }, "註冊成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "註冊失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 使用者登入
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                LogRequestStart("Login", new { UsernameOrEmail = dto.UsernameOrEmail });

                // 驗證輸入
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 取得 IP 位址
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // 執行登入
                var result = await _authService.LoginAsync(dto, ipAddress);
                
                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message);
                }

                LogRequestComplete("Login");
                return CreateSuccessResponse(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt,
                    user = new
                    {
                        id = result.User?.Id,
                        username = result.User?.Username,
                        email = result.User?.Email,
                        fullName = result.User?.FullName,
                        role = result.User?.Role
                    }
                }, result.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "登入失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 使用者登出
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                LogRequestStart("Logout", new { UserId = userId });

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // 執行登出
                var success = await _authService.LogoutAsync(userId, dto.RefreshToken);

                LogRequestComplete("Logout");
                return CreateSuccessResponse(new { success }, "登出成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "登出失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 更新 Token
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            try
            {
                LogRequestStart("RefreshToken");

                // 驗證輸入
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 更新 token
                var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
                
                if (!result.Success)
                {
                    return Unauthorized(CreateErrorResponse("無效的 Refresh Token"));
                }

                LogRequestComplete("RefreshToken");
                return CreateSuccessResponse(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt
                }, "Token 更新成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Token 更新失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 取得當前使用者資訊
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = GetCurrentUserId();
                LogRequestStart("GetCurrentUser", new { UserId = userId });

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                {
                    return CreateNotFoundResponse("使用者", userId);
                }

                LogRequestComplete("GetCurrentUser");
                return CreateSuccessResponse(new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    fullName = user.FullName,
                    role = user.Role,
                    status = user.Status,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取得使用者資訊失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }
    }
}