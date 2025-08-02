using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using FamilyTree.Services;
using FamilyTree.Models;
using FamilyTree.Attributes;
using familytree_backend.Controllers;
using familytree_backend.Services;

namespace FamilyTree.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RoleController : BaseController
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            IPermissionService permissionService,
            ILogger<RoleController> logger,
            IConfigurationService configurationService,
            IValidationService validationService,
            IAccessControlService accessControlService,
            ILoggingService loggingService)
            : base(logger, configurationService, validationService, accessControlService, loggingService)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        /// <summary>
        /// 取得所有角色列表
        /// </summary>
        [HttpGet]
        // 任何已登入的使用者都可以查看角色列表
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _permissionService.GetAllRolesAsync();
                
                // 轉換為 DTO 並包含權限資訊
                var roleDtos = new List<RoleDto>();
                foreach (var role in roles)
                {
                    var permissions = await _permissionService.GetRolePermissionsAsync(role.Id);
                    roleDtos.Add(new RoleDto
                    {
                        Id = role.Id,
                        DisplayName = role.DisplayName,
                        Description = role.Description,
                        Level = role.Level,
                        IsSystem = role.IsSystem,
                        UserCount = role.UserCount,
                        CreatedAt = role.CreatedAt,
                        UpdatedAt = role.UpdatedAt,
                        Permissions = permissions
                    });
                }

                return SuccessResponse(roleDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得角色列表時發生錯誤");
                return ErrorResponse("取得角色列表失敗");
            }
        }

        /// <summary>
        /// 取得特定角色資訊
        /// </summary>
        [HttpGet("{roleId}")]
        [RequirePermission("role:manage")]
        public async Task<IActionResult> GetRole(string roleId)
        {
            try
            {
                var roles = await _permissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                
                if (role == null)
                {
                    return NotFoundResponse("角色不存在");
                }

                var permissions = await _permissionService.GetRolePermissionsAsync(roleId);
                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    DisplayName = role.DisplayName,
                    Description = role.Description,
                    Level = role.Level,
                    IsSystem = role.IsSystem,
                    UserCount = role.UserCount,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt,
                    Permissions = permissions
                };

                return SuccessResponse(roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得角色資訊時發生錯誤: RoleId={RoleId}", roleId);
                return ErrorResponse("取得角色資訊失敗");
            }
        }

        /// <summary>
        /// 建立新角色
        /// </summary>
        [HttpPost]
        [RequirePermission("role:manage")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 檢查角色ID是否已存在
                var existingRoles = await _permissionService.GetAllRolesAsync();
                if (existingRoles.Any(r => r.Id == dto.RoleId))
                {
                    return ConflictResponse("角色ID已存在");
                }

                var success = await _permissionService.CreateRoleAsync(
                    dto.RoleId,
                    dto.DisplayName,
                    dto.Description,
                    dto.Level
                );

                if (success)
                {
                    await LogActivityAsync("role.create", "role", dto.RoleId, 
                        $"建立角色: {dto.DisplayName}");
                    return SuccessResponse(new { roleId = dto.RoleId }, "角色建立成功");
                }

                return ErrorResponse("角色建立失敗");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立角色時發生錯誤");
                return ErrorResponse("角色建立失敗");
            }
        }

        /// <summary>
        /// 更新角色資訊
        /// </summary>
        [HttpPut("{roleId}")]
        [RequirePermission("role:manage")]
        public async Task<IActionResult> UpdateRole(string roleId, [FromBody] UpdateRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var success = await _permissionService.UpdateRoleAsync(
                    roleId,
                    dto.DisplayName,
                    dto.Description,
                    dto.Level
                );

                if (success)
                {
                    await LogActivityAsync("role.update", "role", roleId, 
                        $"更新角色: {dto.DisplayName}");
                    return SuccessResponse(null, "角色更新成功");
                }

                return NotFoundResponse("角色不存在或為系統角色");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新角色時發生錯誤: RoleId={RoleId}", roleId);
                return ErrorResponse("角色更新失敗");
            }
        }

        /// <summary>
        /// 刪除角色
        /// </summary>
        [HttpDelete("{roleId}")]
        [RequirePermission("role:manage")]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            try
            {
                // 檢查是否為系統角色
                var roles = await _permissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                
                if (role == null)
                {
                    return NotFoundResponse("角色不存在");
                }

                if (role.IsSystem)
                {
                    return ForbiddenResponse("無法刪除系統角色");
                }

                if (role.UserCount > 0)
                {
                    return ConflictResponse($"角色仍有 {role.UserCount} 個使用者使用中，無法刪除");
                }

                var success = await _permissionService.DeleteRoleAsync(roleId);

                if (success)
                {
                    await LogActivityAsync("role.delete", "role", roleId, 
                        $"刪除角色: {role.DisplayName}");
                    return SuccessResponse(null, "角色刪除成功");
                }

                return ErrorResponse("角色刪除失敗");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除角色時發生錯誤: RoleId={RoleId}", roleId);
                return ErrorResponse("角色刪除失敗");
            }
        }

        /// <summary>
        /// 設定角色權限
        /// </summary>
        [HttpPost("{roleId}/permissions")]
        [RequirePermission("role:manage")]
        public async Task<IActionResult> SetRolePermissions(string roleId, [FromBody] SetRolePermissionsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 檢查角色是否存在
                var roles = await _permissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                
                if (role == null)
                {
                    return NotFoundResponse("角色不存在");
                }

                // 驗證權限字串格式
                foreach (var permission in dto.Permissions)
                {
                    if (!IsValidPermissionFormat(permission))
                    {
                        return BadRequestResponse($"無效的權限格式: {permission}");
                    }
                }

                var success = await _permissionService.SetRolePermissionsAsync(roleId, dto.Permissions);

                if (success)
                {
                    await LogActivityAsync("role.permissions.update", "role", roleId, 
                        $"更新角色權限: {role.DisplayName}, 權限數量: {dto.Permissions.Count}");
                    return SuccessResponse(null, "角色權限設定成功");
                }

                return ErrorResponse("角色權限設定失敗");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定角色權限時發生錯誤: RoleId={RoleId}", roleId);
                return ErrorResponse("角色權限設定失敗");
            }
        }

        /// <summary>
        /// 複製角色
        /// </summary>
        [HttpPost("copy")]
        [RequirePermission("role:manage")]
        public async Task<IActionResult> CopyRole([FromBody] CopyRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 檢查來源角色是否存在
                var roles = await _permissionService.GetAllRolesAsync();
                var sourceRole = roles.FirstOrDefault(r => r.Id == dto.SourceRoleId);
                
                if (sourceRole == null)
                {
                    return NotFoundResponse("來源角色不存在");
                }

                // 檢查新角色ID是否已存在
                if (roles.Any(r => r.Id == dto.NewRoleId))
                {
                    return ConflictResponse("新角色ID已存在");
                }

                // 建立新角色
                var createSuccess = await _permissionService.CreateRoleAsync(
                    dto.NewRoleId,
                    dto.DisplayName,
                    dto.Description ?? $"複製自 {sourceRole.DisplayName}",
                    sourceRole.Level
                );

                if (!createSuccess)
                {
                    return ErrorResponse("建立新角色失敗");
                }

                // 複製權限
                var sourcePermissions = await _permissionService.GetRolePermissionsAsync(dto.SourceRoleId);
                var copySuccess = await _permissionService.SetRolePermissionsAsync(dto.NewRoleId, sourcePermissions);

                if (copySuccess)
                {
                    await LogActivityAsync("role.copy", "role", dto.NewRoleId, 
                        $"複製角色: 從 {sourceRole.DisplayName} 到 {dto.DisplayName}");
                    return SuccessResponse(new { roleId = dto.NewRoleId }, "角色複製成功");
                }

                // 如果權限複製失敗，刪除已建立的角色
                await _permissionService.DeleteRoleAsync(dto.NewRoleId);
                return ErrorResponse("角色權限複製失敗");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "複製角色時發生錯誤");
                return ErrorResponse("角色複製失敗");
            }
        }

        /// <summary>
        /// 驗證權限字串格式
        /// </summary>
        private bool IsValidPermissionFormat(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return false;

            // 允許萬用字元
            if (permission == "*")
                return true;

            // 檢查格式 resource:action 或 resource:*
            var parts = permission.Split(':');
            if (parts.Length != 2)
                return false;

            // 資源名稱必須是小寫字母
            if (!System.Text.RegularExpressions.Regex.IsMatch(parts[0], @"^[a-z]+$"))
                return false;

            // 動作名稱必須是小寫字母、底線或萬用字元
            if (!System.Text.RegularExpressions.Regex.IsMatch(parts[1], @"^[a-z_*]+$"))
                return false;

            return true;
        }
    }
}