using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using familytree_backend.Models;
using familytree_backend.Services;
using familytree_backend.Constants;

namespace familytree_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;

        public UserController(
            ILogger<UserController> logger,
            IConfigurationService configurationService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService,
            IUserService userService,
            IAuthService authService) 
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _userService = userService;
            _authService = authService;
        }

        /// <summary>
        /// 取得使用者列表（僅管理員）
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                LogRequestStart("GetUsers", new { page, pageSize });

                // 驗證分頁參數
                var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);

                // 取得使用者列表
                var result = await _userService.GetUsersAsync(normalizedPage, normalizedPageSize);

                LogRequestComplete("GetUsers", result.TotalCount);
                return CreatePagedResponse(result.Data, result.TotalCount, result.Page, result.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取得使用者列表失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 取得特定使用者資料
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                LogRequestStart("GetUser", new { UserId = id });

                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // 權限檢查：只能查看自己的資料，除非是管理員
                if (currentUserRole != "admin" && currentUserId != id)
                {
                    return Forbid();
                }

                // 取得使用者資料
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                {
                    return CreateNotFoundResponse("使用者", id);
                }

                LogRequestComplete("GetUser");
                return CreateSuccessResponse(new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    fullName = user.FullName,
                    role = user.Role,
                    status = user.Status,
                    createdAt = user.CreatedAt,
                    updatedAt = user.UpdatedAt,
                    lastLoginAt = user.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取得使用者資料失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 更新使用者資料
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                LogRequestStart("UpdateUser", new { UserId = id, Dto = dto });

                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // 權限檢查：只能更新自己的資料，除非是管理員
                if (currentUserRole != "admin" && currentUserId != id)
                {
                    return Forbid();
                }

                // 只有管理員可以更新角色
                if (dto.Role != null && currentUserRole != "admin")
                {
                    return CreateErrorResponse("只有管理員可以更新角色");
                }

                // 更新使用者資料
                var user = await _userService.UpdateAsync(id, dto);
                if (user == null)
                {
                    return CreateNotFoundResponse("使用者", id);
                }

                // 記錄活動
                await LoggingService.LogActivityAsync(
                    currentUserId,
                    "update_user",
                    $"Updated user: {id}"
                );

                LogRequestComplete("UpdateUser");
                return CreateSuccessResponse(new
                {
                    id = user.Id,
                    username = user.Username,
                    email = user.Email,
                    fullName = user.FullName,
                    role = user.Role,
                    status = user.Status
                }, "使用者資料更新成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "更新使用者資料失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 變更密碼
        /// </summary>
        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordDto dto)
        {
            try
            {
                LogRequestStart("ChangePassword", new { UserId = id });

                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();

                // 權限檢查：只能變更自己的密碼
                if (currentUserId != id)
                {
                    return Forbid();
                }

                // 驗證輸入
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 變更密碼
                var success = await _userService.ChangePasswordAsync(id, dto.OldPassword, dto.NewPassword);
                if (!success)
                {
                    return CreateErrorResponse("舊密碼錯誤");
                }

                LogRequestComplete("ChangePassword");
                return CreateSuccessResponse(new { success }, "密碼變更成功");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "變更密碼失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 重設密碼（僅管理員）
        /// </summary>
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ResetPassword(string id)
        {
            try
            {
                LogRequestStart("ResetPassword", new { UserId = id });

                var adminUserId = GetCurrentUserId();

                // 檢查使用者是否存在
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                {
                    return CreateNotFoundResponse("使用者", id);
                }

                // 重設密碼
                var result = await _authService.ResetPasswordAsync(id, adminUserId);
                if (!result.Success)
                {
                    return CreateErrorResponse(result.Message);
                }

                LogRequestComplete("ResetPassword");
                return CreateSuccessResponse(new 
                { 
                    success = result.Success,
                    temporaryPassword = result.TemporaryPassword 
                }, result.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "重設密碼失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }

        /// <summary>
        /// 停用使用者（僅管理員）
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DisableUser(string id)
        {
            try
            {
                LogRequestStart("DisableUser", new { UserId = id });

                var adminUserId = GetCurrentUserId();

                // 防止停用自己
                if (adminUserId == id)
                {
                    return CreateErrorResponse("無法停用自己的帳號");
                }

                // 停用使用者
                var user = await _userService.UpdateAsync(id, new UpdateUserDto { Status = "inactive" });
                if (user == null)
                {
                    return CreateNotFoundResponse("使用者", id);
                }

                // 記錄活動
                await LoggingService.LogActivityAsync(
                    adminUserId,
                    "disable_user",
                    $"Disabled user: {id}"
                );

                LogRequestComplete("DisableUser");
                return CreateSuccessResponse(new { success = true }, "使用者已停用");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "停用使用者失敗");
                return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.DatabaseError);
            }
        }
    }
}