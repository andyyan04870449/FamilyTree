using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using familytree_backend.Models;
using familytree_backend.Models.Exceptions;
using familytree_backend.Services;
using familytree_backend.Constants;
using familytree_backend.Extensions;

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
            return await this.ExecuteWithErrorHandlingAsync(async () =>
            {
                LogRequestStart("Register", new { Username = dto.Username, Email = dto.Email });

                // 驗證輸入
                this.ValidateModelState();
                this.ValidateNotNull(dto, nameof(dto));

                // 檢查使用者是否已存在
                if (await _userService.UserExistsAsync(dto.Username, dto.Email))
                {
                    throw new ConflictException(MessageConstants.Error.DataAlreadyExists);
                }

                // 註冊使用者
                var user = await _authService.RegisterAsync(dto);
                if (user == null)
                {
                    throw new TechnicalException(MessageConstants.Error.DataCreateFailed);
                }

                LogRequestComplete("Register");
                return this.SuccessResponse(new { userId = user.Id }, MessageConstants.Success.DataCreated);
            }, "Register");
        }

        /// <summary>
        /// 使用者登入
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            return await this.ExecuteWithErrorHandlingAsync(async () =>
            {
                LogRequestStart("Login", new { UsernameOrEmail = dto.UsernameOrEmail });

                // 驗證輸入
                this.ValidateModelState();
                this.ValidateNotNull(dto, nameof(dto));

                // 取得 IP 位址，使用強化的IP位址提取邏輯
                var ipAddress = HttpContext.GetClientIpAddress();

                // 執行登入
                var result = await _authService.LoginAsync(dto, ipAddress);
                
                if (!result.Success)
                {
                    throw new UnauthorizedException(result.Message);
                }

                LogRequestComplete("Login");
                return this.SuccessResponse(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt,
                    expiresIn = 15 * 60, // 15 分鐘，轉換為秒
                    user = new
                    {
                        id = result.User?.Id,
                        username = result.User?.Username,
                        email = result.User?.Email,
                        fullName = result.User?.FullName,
                        role = result.User?.Role
                    }
                }, result.Message);
            }, "Login");
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
                return CreateSuccessResponse(new { success }, MessageConstants.Success.LogoutSuccess);
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
                    return Unauthorized(CreateErrorResponse(MessageConstants.Error.TokenInvalid));
                }

                LogRequestComplete("RefreshToken");
                return CreateSuccessResponse(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt,
                    expiresIn = 15 * 60 // 15 分鐘，轉換為秒
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
            return await this.ExecuteWithErrorHandlingAsync(async () =>
            {
                var userId = GetCurrentUserId();
                LogRequestStart("GetCurrentUser", new { UserId = userId });

                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedException();
                }

                var user = await _userService.GetByIdAsync(userId);
                this.ValidateResourceExists(user, "使用者", userId);

                LogRequestComplete("GetCurrentUser");
                return this.SuccessResponse(new
                {
                    id = user!.Id,
                    username = user.Username,
                    email = user.Email,
                    fullName = user.FullName,
                    role = user.Role,
                    status = user.Status,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt
                });
            }, "GetCurrentUser");
        }
    }
}