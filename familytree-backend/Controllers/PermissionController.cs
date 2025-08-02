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
using familytree_backend.Attributes;

namespace FamilyTree.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionController : BaseController
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(
            IPermissionService permissionService,
            ILogger<PermissionController> logger,
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
        /// 取得所有權限定義
        /// </summary>
        [HttpGet]
        [RoleManagePermission]
        public async Task<IActionResult> GetPermissions()
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionDefinitionsAsync();
                
                // 取得所有角色以顯示權限指派情況
                var roles = await _permissionService.GetAllRolesAsync();
                var rolePermissionsMap = new Dictionary<string, List<string>>();
                
                foreach (var role in roles)
                {
                    var rolePerms = await _permissionService.GetRolePermissionsAsync(role.Id);
                    foreach (var perm in rolePerms)
                    {
                        if (!rolePermissionsMap.ContainsKey(perm))
                        {
                            rolePermissionsMap[perm] = new List<string>();
                        }
                        rolePermissionsMap[perm].Add(role.Id);
                    }
                }

                // 轉換為 DTO
                var permissionDtos = permissions.Select(p => new PermissionDto
                {
                    Resource = p.Resource,
                    Action = p.Action,
                    Permission = p.Permission,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    Category = p.Category,
                    IsSystem = p.IsSystem,
                    AssignedRoles = rolePermissionsMap.ContainsKey(p.Permission) 
                        ? rolePermissionsMap[p.Permission] 
                        : new List<string>()
                }).ToList();

                return SuccessResponse(permissionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限定義時發生錯誤");
                return ErrorResponse("取得權限定義失敗");
            }
        }

        /// <summary>
        /// 取得權限分類
        /// </summary>
        [HttpGet("categories")]
        [RoleManagePermission]
        public async Task<IActionResult> GetPermissionCategories()
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionDefinitionsAsync();
                
                // 按類別分組
                var categories = permissions
                    .GroupBy(p => p.Category)
                    .Select(g => new PermissionCategoryDto
                    {
                        Category = g.Key,
                        DisplayName = GetCategoryDisplayName(g.Key),
                        PermissionCount = g.Count(),
                        Permissions = g.Select(p => new PermissionDto
                        {
                            Resource = p.Resource,
                            Action = p.Action,
                            Permission = p.Permission,
                            DisplayName = p.DisplayName,
                            Description = p.Description,
                            Category = p.Category,
                            IsSystem = p.IsSystem
                        }).ToList()
                    })
                    .OrderBy(c => GetCategoryOrder(c.Category))
                    .ToList();

                return SuccessResponse(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限分類時發生錯誤");
                return ErrorResponse("取得權限分類失敗");
            }
        }

        /// <summary>
        /// 取得當前使用者的權限資訊
        /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentUserPermissions()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userRoles = await _permissionService.GetUserRolesAsync(userId);
                var allPermissions = await _permissionService.GetUserPermissionsAsync(userId);

                return SuccessResponse(new
                {
                    userId = userId,
                    roles = userRoles,
                    permissions = allPermissions
                }, "取得當前使用者權限成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得當前使用者權限時發生錯誤");
                return ErrorResponse($"取得權限失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 取得所有權限定義
        /// </summary>
        [HttpGet("definitions")]
        public async Task<IActionResult> GetPermissionDefinitions()
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionDefinitionsAsync();
                
                var definitions = permissions.Select(p => new
                {
                    permission = p.Permission,
                    displayName = p.DisplayName,
                    description = p.Description,
                    category = p.Category
                }).ToList();

                return SuccessResponse(definitions, "取得權限定義成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限定義時發生錯誤");
                return ErrorResponse($"取得權限定義失敗: {ex.Message}");
            }
        }

        // 輔助方法：取得權限顯示名稱
        private string GetPermissionDisplayName(string permission)
        {
            var parts = permission.Split(':');
            if (parts.Length != 2) return permission;

            var resource = parts[0];
            var action = parts[1];

            var resourceName = resource switch
            {
                "user" => "使用者",
                "role" => "角色",
                "project" => "專案",
                "person" => "人員",
                "file" => "檔案",
                "search" => "搜尋",
                "report" => "報表",
                _ => resource
            };

            var actionName = action switch
            {
                "create" => "建立",
                "read" => "檢視",
                "update" => "更新",
                "delete" => "刪除",
                "manage" => "管理",
                "upload" => "上傳",
                "download" => "下載",
                "process" => "處理",
                "perform" => "執行",
                "view" => "檢視",
                "export" => "匯出",
                _ => action
            };

            return $"{actionName}{resourceName}";
        }

        // 輔助方法：取得權限描述
        private string GetPermissionDescription(string permission)
        {
            return permission switch
            {
                "user:create" => "建立新使用者帳號",
                "user:read" => "檢視使用者資訊",
                "user:update" => "更新使用者資訊",
                "user:delete" => "刪除使用者帳號",
                "role:manage" => "管理角色和權限",
                "project:create" => "建立新專案",
                "project:read" => "檢視專案資訊",
                "project:update" => "更新專案資訊",
                "project:delete" => "刪除專案",
                "person:create" => "建立人員資料",
                "person:read" => "檢視人員資料",
                "person:update" => "更新人員資料",
                "person:delete" => "刪除人員資料",
                "file:upload" => "上傳檔案",
                "file:read" => "檢視檔案",
                "file:delete" => "刪除檔案",
                "file:download" => "下載檔案",
                "file:process" => "處理檔案內容",
                "search:perform" => "執行搜尋功能",
                "report:view" => "檢視報表",
                "report:export" => "匯出報表",
                _ => $"允許執行 {permission} 操作"
            };
        }

        // 輔助方法：取得權限分類
        private string GetPermissionCategory(string permission)
        {
            var resource = permission.Split(':')[0];
            return resource switch
            {
                "user" or "role" => "系統管理",
                "project" => "專案管理", 
                "person" => "人員管理",
                "file" => "檔案管理",
                "search" => "搜尋功能",
                "report" => "報表功能",
                _ => "其他"
            };
        }

        /// <summary>
        /// 取得使用者的權限資訊
        /// </summary>
        [HttpGet("user/{userId}")]
        // 移除權限要求，在方法內部進行權限檢查
        public async Task<IActionResult> GetUserPermissions(string userId)
        {
            try
            {
                // 檢查是否有權查看其他使用者的權限
                var currentUserId = GetCurrentUserId();
                
                // 如果是查看自己的權限，允許
                if (currentUserId == userId)
                {
                    _logger.LogInformation("User is viewing their own permissions - allowed");
                }
                // 否則檢查是否是管理員
                else
                {
                    // 首先檢查舊的角色系統（從 JWT token 中的 role claim）
                    var isAdminInOldSystem = User.HasClaim(c => c.Type == "role" && c.Value == "admin");
                    
                    if (isAdminInOldSystem)
                    {
                        _logger.LogInformation($"User {currentUserId} is admin in old system - allowed to view permissions for user {userId}");
                    }
                    else
                    {
                        // 檢查新的角色系統
                        try 
                        {
                            var currentUserRoles = await _permissionService.GetUserRolesAsync(currentUserId);
                            var isAdminInNewSystem = currentUserRoles.Any(r => r.Id == "admin" || r.Id == "superadmin");
                            
                            if (!isAdminInNewSystem)
                            {
                                // 最後檢查是否有 role:manage 權限
                                var hasPermission = await _permissionService.HasPermissionAsync(currentUserId, "role:manage");
                                
                                if (!hasPermission)
                                {
                                    _logger.LogWarning($"User {currentUserId} denied access to view permissions for user {userId}");
                                    return ForbiddenResponse("無權查看其他使用者的權限");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Error checking new role system for user {currentUserId}, falling back to old system check");
                            if (!isAdminInOldSystem)
                            {
                                return ForbiddenResponse("無權查看其他使用者的權限");
                            }
                        }
                    }
                }

                // 取得使用者資訊（這裡簡化處理，實際應該從 UserService 取得）
                var userRoles = await _permissionService.GetUserRolesAsync(userId);
                var allPermissions = await _permissionService.GetUserPermissionsAsync(userId);
                
                // TODO: 從 UserService 取得使用者詳細資訊
                var userPermissionsDto = new UserPermissionsDto
                {
                    UserId = userId,
                    Username = "N/A", // TODO: 從 UserService 取得
                    Email = "N/A", // TODO: 從 UserService 取得
                    FullName = "N/A", // TODO: 從 UserService 取得
                    Roles = userRoles.Select(r => new RoleDto
                    {
                        Id = r.Id,
                        DisplayName = r.DisplayName,
                        Description = r.Description,
                        Level = r.Level,
                        IsSystem = r.IsSystem
                    }).ToList(),
                    DirectPermissions = new List<string>(), // TODO: 實作直接權限
                    AllPermissions = allPermissions,
                    ProjectPermissions = new Dictionary<string, ProjectPermissionDto>() // TODO: 實作專案權限
                };

                return SuccessResponse(userPermissionsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者權限時發生錯誤: UserId={UserId}", userId);
                return ErrorResponse("取得使用者權限失敗");
            }
        }

        /// <summary>
        /// 設定使用者的額外權限
        /// </summary>
        [HttpPost("user/{userId}")]
        [RoleManagePermission]
        public async Task<IActionResult> SetUserPermissions(string userId, [FromBody] SetUserPermissionsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 驗證權限字串格式
                foreach (var permission in dto.Permissions)
                {
                    if (!IsValidPermissionFormat(permission))
                    {
                        return BadRequestResponse($"無效的權限格式: {permission}");
                    }
                }

                var success = await _permissionService.SetUserPermissionsAsync(userId, dto.Permissions);

                if (success)
                {
                    await LogActivityAsync("user.permissions.update", "user", userId, 
                        $"更新使用者額外權限，權限數量: {dto.Permissions.Count}");
                    return SuccessResponse(null, "使用者權限設定成功");
                }

                return ErrorResponse("使用者權限設定失敗");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定使用者權限時發生錯誤: UserId={UserId}", userId);
                return ErrorResponse("使用者權限設定失敗");
            }
        }

        /// <summary>
        /// 指派角色給使用者
        /// </summary>
        [HttpPost("user/{userId}/role")]
        [RoleManagePermission]
        public async Task<IActionResult> AssignRoleToUser(string userId, [FromBody] AssignRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 驗證使用者ID一致性
                if (userId != dto.UserId)
                {
                    return BadRequestResponse("URL和請求主體中的使用者ID不一致");
                }

                // 檢查角色是否存在
                var roles = await _permissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => r.Id == dto.RoleId);
                
                if (role == null)
                {
                    return NotFoundResponse("角色不存在");
                }

                var success = await _permissionService.AssignRoleToUserAsync(dto.UserId, dto.RoleId);

                if (success)
                {
                    await LogActivityAsync("user.role.assign", "user", dto.UserId, 
                        $"指派角色 {role.DisplayName} 給使用者");
                    return SuccessResponse(null, "角色指派成功");
                }

                return ErrorResponse("角色指派失敗");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "指派角色時發生錯誤: UserId={UserId}, RoleId={RoleId}", 
                    dto.UserId, dto.RoleId);
                return ErrorResponse("角色指派失敗");
            }
        }

        /// <summary>
        /// 移除使用者的角色
        /// </summary>
        [HttpDelete("user/{userId}/role/{roleId}")]
        [RoleManagePermission]
        public async Task<IActionResult> RemoveRoleFromUser(string userId, string roleId)
        {
            try
            {
                // 檢查是否為最後一個角色
                var userRoles = await _permissionService.GetUserRolesAsync(userId);
                if (userRoles.Count <= 1)
                {
                    return ConflictResponse("無法移除使用者的最後一個角色");
                }

                var success = await _permissionService.RemoveRoleFromUserAsync(userId, roleId);

                if (success)
                {
                    await LogActivityAsync("user.role.remove", "user", userId, 
                        $"移除使用者的角色 {roleId}");
                    return SuccessResponse(null, "角色移除成功");
                }

                return NotFoundResponse("使用者或角色不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除角色時發生錯誤: UserId={UserId}, RoleId={RoleId}", 
                    userId, roleId);
                return ErrorResponse("角色移除失敗");
            }
        }

        /// <summary>
        /// 批量指派角色
        /// </summary>
        [HttpPost("batch/assign-role")]
        [RoleManagePermission]
        public async Task<IActionResult> BatchAssignRole([FromBody] BatchAssignRoleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 檢查角色是否存在
                var roles = await _permissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => r.Id == dto.RoleId);
                
                if (role == null)
                {
                    return NotFoundResponse("角色不存在");
                }

                var successCount = 0;
                var failedUsers = new List<string>();

                foreach (var userId in dto.UserIds)
                {
                    try
                    {
                        var success = await _permissionService.AssignRoleToUserAsync(userId, dto.RoleId);
                        if (success)
                        {
                            successCount++;
                        }
                        else
                        {
                            failedUsers.Add(userId);
                        }
                    }
                    catch
                    {
                        failedUsers.Add(userId);
                    }
                }

                await LogActivityAsync("user.role.batch_assign", "role", dto.RoleId, 
                    $"批量指派角色 {role.DisplayName} 給 {successCount} 個使用者");

                return SuccessResponse(new
                {
                    successCount,
                    failedCount = failedUsers.Count,
                    failedUsers
                }, $"成功指派 {successCount} 個使用者，失敗 {failedUsers.Count} 個");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量指派角色時發生錯誤");
                return ErrorResponse("批量指派角色失敗");
            }
        }

        /// <summary>
        /// 驗證權限
        /// </summary>
        [HttpPost("validate")]
        [RoleManagePermission]
        public async Task<IActionResult> ValidatePermission([FromBody] ValidatePermissionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var hasPermission = false;
                var reason = "無權限";

                // 基本權限檢查
                if (await _permissionService.HasPermissionAsync(dto.UserId, dto.Permission))
                {
                    hasPermission = true;
                    reason = "擁有系統權限";
                }
                // 專案權限檢查
                else if (!string.IsNullOrEmpty(dto.ProjectId))
                {
                    if (await _permissionService.HasProjectPermissionAsync(dto.UserId, dto.ProjectId, dto.Permission))
                    {
                        hasPermission = true;
                        reason = "擁有專案權限";
                    }
                }
                // 資源擁有者檢查
                else if (!string.IsNullOrEmpty(dto.ResourceId) && !string.IsNullOrEmpty(dto.ResourceType))
                {
                    if (await _permissionService.IsResourceOwnerAsync(dto.UserId, dto.ResourceType, dto.ResourceId))
                    {
                        hasPermission = true;
                        reason = "資源擁有者";
                    }
                }

                var userPermissions = await _permissionService.GetUserPermissionsAsync(dto.UserId);
                
                var result = new PermissionCheckResult
                {
                    HasPermission = hasPermission,
                    Reason = reason,
                    RequiredPermissions = new List<string> { dto.Permission },
                    UserPermissions = userPermissions
                };

                return SuccessResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "驗證權限時發生錯誤");
                return ErrorResponse("權限驗證失敗");
            }
        }

        /// <summary>
        /// 取得類別顯示名稱
        /// </summary>
        private string GetCategoryDisplayName(string category)
        {
            return category switch
            {
                "系統管理" => "系統管理",
                "使用者管理" => "使用者管理",
                "專案管理" => "專案管理",
                "人員管理" => "人員管理",
                "檔案管理" => "檔案管理",
                "報表管理" => "報表管理",
                _ => category
            };
        }

        /// <summary>
        /// 取得類別排序順序
        /// </summary>
        private int GetCategoryOrder(string category)
        {
            return category switch
            {
                "系統管理" => 1,
                "使用者管理" => 2,
                "專案管理" => 3,
                "人員管理" => 4,
                "檔案管理" => 5,
                "報表管理" => 6,
                _ => 99
            };
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