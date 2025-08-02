using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using FamilyTree.Constants;
using FamilyTree.Services;

namespace FamilyTree.Services.Authorization
{
    /// <summary>
    /// 角色為基礎的權限策略實現
    /// 支援角色階層和權限繼承
    /// </summary>
    public class RoleBasedPermissionStrategy : IPermissionStrategy
    {
        private readonly IPermissionService _permissionService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RoleBasedPermissionStrategy> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        // 角色階層定義（數字越大權限越高）
        private static readonly Dictionary<string, int> RoleHierarchy = new()
        {
            [RoleConstants.SUPER_ADMIN] = 100,
            [RoleConstants.ADMIN] = 80,
            ["AuditReader"] = 60,
            [RoleConstants.USER] = 40,
            ["Guest"] = 20
        };

        // 權限前綴和對應的最低要求角色
        private static readonly Dictionary<string, string> PermissionRoleMapping = new()
        {
            ["system:"] = RoleConstants.SUPER_ADMIN,
            ["role:"] = RoleConstants.SUPER_ADMIN,
            ["audit:admin"] = RoleConstants.ADMIN,
            ["audit:read"] = "AuditReader",
            ["user:delete"] = RoleConstants.ADMIN,
            ["user:create"] = RoleConstants.ADMIN,
            ["project:delete"] = RoleConstants.ADMIN,
            ["file:manage"] = RoleConstants.ADMIN
        };

        public RoleBasedPermissionStrategy(
            IPermissionService permissionService,
            IMemoryCache cache,
            ILogger<RoleBasedPermissionStrategy> logger)
        {
            _permissionService = permissionService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission, object? resource = null)
        {
            try
            {
                if (!user.Identity?.IsAuthenticated == true)
                    return false;

                var userId = GetUserId(user);
                if (string.IsNullOrEmpty(userId))
                    return false;

                // 檢查快取
                var cacheKey = $"permission_{userId}_{permission}";
                if (_cache.TryGetValue<bool>(cacheKey, out var cachedResult))
                    return cachedResult;

                // 超級管理員有所有權限
                if (HasRole(user, RoleConstants.SUPER_ADMIN))
                {
                    _cache.Set(cacheKey, true, _cacheExpiration);
                    return true;
                }

                var hasPermission = false;

                // 1. 檢查角色階層權限
                hasPermission = await CheckRoleHierarchyPermissionAsync(user, permission);

                // 2. 如果沒有角色權限，檢查具體權限
                if (!hasPermission)
                {
                    hasPermission = await _permissionService.HasPermissionAsync(userId, permission);
                }

                // 3. 如果允許擁有者存取且是資源類型權限，檢查擁有權
                if (!hasPermission && resource is ResourceInfo resourceInfo)
                {
                    hasPermission = await IsResourceOwnerAsync(user, resourceInfo.Type, resourceInfo.Id);
                }

                _cache.Set(cacheKey, hasPermission, _cacheExpiration);
                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查權限時發生錯誤: Permission={Permission}, UserId={UserId}", 
                    permission, GetUserId(user));
                return false;
            }
        }

        public async Task<bool> HasAnyPermissionAsync(ClaimsPrincipal user, IEnumerable<string> permissions, object? resource = null)
        {
            foreach (var permission in permissions)
            {
                if (await HasPermissionAsync(user, permission, resource))
                    return true;
            }
            return false;
        }

        public async Task<bool> HasAllPermissionsAsync(ClaimsPrincipal user, IEnumerable<string> permissions, object? resource = null)
        {
            foreach (var permission in permissions)
            {
                if (!await HasPermissionAsync(user, permission, resource))
                    return false;
            }
            return true;
        }

        public async Task<bool> IsResourceOwnerAsync(ClaimsPrincipal user, string resourceType, string resourceId)
        {
            try
            {
                var userId = GetUserId(user);
                if (string.IsNullOrEmpty(userId))
                    return false;

                var cacheKey = $"owner_{userId}_{resourceType}_{resourceId}";
                if (_cache.TryGetValue<bool>(cacheKey, out var cachedResult))
                    return cachedResult;

                var isOwner = await _permissionService.IsResourceOwnerAsync(userId, resourceType, resourceId);
                _cache.Set(cacheKey, isOwner, _cacheExpiration);
                
                return isOwner;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查資源擁有權時發生錯誤: ResourceType={ResourceType}, ResourceId={ResourceId}, UserId={UserId}", 
                    resourceType, resourceId, GetUserId(user));
                return false;
            }
        }

        /// <summary>
        /// 根據角色階層檢查權限
        /// </summary>
        private async Task<bool> CheckRoleHierarchyPermissionAsync(ClaimsPrincipal user, string permission)
        {
            var userRoles = GetUserRoles(user);
            var userMaxLevel = GetMaxRoleLevel(userRoles);

            // 檢查權限前綴映射
            foreach (var mapping in PermissionRoleMapping)
            {
                if (permission.StartsWith(mapping.Key))
                {
                    var requiredRole = mapping.Value;
                    var requiredLevel = RoleHierarchy.GetValueOrDefault(requiredRole, 0);
                    return userMaxLevel >= requiredLevel;
                }
            }

            // 特殊權限檢查
            return permission switch
            {
                var p when p.StartsWith("audit:") => userMaxLevel >= RoleHierarchy.GetValueOrDefault("AuditReader", 0),
                var p when p.Contains("admin") => userMaxLevel >= RoleHierarchy.GetValueOrDefault(RoleConstants.ADMIN, 0),
                var p when p.Contains("delete") => userMaxLevel >= RoleHierarchy.GetValueOrDefault(RoleConstants.ADMIN, 0),
                _ => false
            };
        }

        /// <summary>
        /// 獲取使用者的最高角色等級
        /// </summary>
        private int GetMaxRoleLevel(List<string> userRoles)
        {
            return userRoles
                .Select(role => RoleHierarchy.GetValueOrDefault(role, 0))
                .DefaultIfEmpty(0)
                .Max();
        }

        /// <summary>
        /// 獲取使用者角色列表
        /// </summary>
        private List<string> GetUserRoles(ClaimsPrincipal user)
        {
            return user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }

        /// <summary>
        /// 檢查使用者是否有指定角色
        /// </summary>
        private bool HasRole(ClaimsPrincipal user, string role)
        {
            return user.IsInRole(role);
        }

        /// <summary>
        /// 獲取使用者ID
        /// </summary>
        private string? GetUserId(ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}