using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using familytree_backend.Services;

namespace FamilyTree.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IDatabaseHelper _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        // 預設權限定義
        private static readonly List<PermissionDefinition> DefaultPermissions = new()
        {
            // 系統管理權限
            new() { Resource = "system", Action = "manage", Permission = "system:manage", DisplayName = "系統設定管理", Description = "可以修改系統設定", Category = "系統管理", IsSystem = true },
            new() { Resource = "role", Action = "manage", Permission = "role:manage", DisplayName = "角色權限管理", Description = "可以管理角色和權限", Category = "系統管理", IsSystem = true },
            new() { Resource = "log", Action = "view", Permission = "log:view", DisplayName = "查看系統日誌", Description = "可以查看系統日誌", Category = "系統管理", IsSystem = true },
            
            // 使用者管理權限
            new() { Resource = "user", Action = "create", Permission = "user:create", DisplayName = "建立使用者", Description = "可以建立新使用者", Category = "使用者管理", IsSystem = true },
            new() { Resource = "user", Action = "read", Permission = "user:read", DisplayName = "查看使用者", Description = "可以查看使用者資料", Category = "使用者管理", IsSystem = true },
            new() { Resource = "user", Action = "update", Permission = "user:update", DisplayName = "更新使用者", Description = "可以修改使用者資料", Category = "使用者管理", IsSystem = true },
            new() { Resource = "user", Action = "delete", Permission = "user:delete", DisplayName = "刪除使用者", Description = "可以刪除使用者", Category = "使用者管理", IsSystem = true },
            
            // 專案管理權限
            new() { Resource = "project", Action = "create", Permission = "project:create", DisplayName = "建立專案", Description = "可以建立新專案", Category = "專案管理", IsSystem = true },
            new() { Resource = "project", Action = "read", Permission = "project:read", DisplayName = "查看專案", Description = "可以查看專案資料", Category = "專案管理", IsSystem = true },
            new() { Resource = "project", Action = "update", Permission = "project:update", DisplayName = "更新專案", Description = "可以修改專案資料", Category = "專案管理", IsSystem = true },
            new() { Resource = "project", Action = "delete", Permission = "project:delete", DisplayName = "刪除專案", Description = "可以刪除專案", Category = "專案管理", IsSystem = true },
            new() { Resource = "project", Action = "manage_members", Permission = "project:manage_members", DisplayName = "管理專案成員", Description = "可以管理專案成員", Category = "專案管理", IsSystem = true },
            
            // 人員資料權限
            new() { Resource = "person", Action = "create", Permission = "person:create", DisplayName = "建立人員", Description = "可以建立人員資料", Category = "人員管理", IsSystem = true },
            new() { Resource = "person", Action = "read", Permission = "person:read", DisplayName = "查看人員", Description = "可以查看人員資料", Category = "人員管理", IsSystem = true },
            new() { Resource = "person", Action = "update", Permission = "person:update", DisplayName = "更新人員", Description = "可以修改人員資料", Category = "人員管理", IsSystem = true },
            new() { Resource = "person", Action = "delete", Permission = "person:delete", DisplayName = "刪除人員", Description = "可以刪除人員資料", Category = "人員管理", IsSystem = true },
            new() { Resource = "person", Action = "export", Permission = "person:export", DisplayName = "匯出人員", Description = "可以匯出人員資料", Category = "人員管理", IsSystem = true },
            
            // 檔案管理權限
            new() { Resource = "file", Action = "upload", Permission = "file:upload", DisplayName = "上傳檔案", Description = "可以上傳檔案", Category = "檔案管理", IsSystem = true },
            new() { Resource = "file", Action = "download", Permission = "file:download", DisplayName = "下載檔案", Description = "可以下載檔案", Category = "檔案管理", IsSystem = true },
            new() { Resource = "file", Action = "delete", Permission = "file:delete", DisplayName = "刪除檔案", Description = "可以刪除檔案", Category = "檔案管理", IsSystem = true },
            new() { Resource = "file", Action = "manage", Permission = "file:manage", DisplayName = "管理檔案", Description = "可以管理所有檔案", Category = "檔案管理", IsSystem = true },
            
            // 報表權限
            new() { Resource = "report", Action = "view", Permission = "report:view", DisplayName = "查看報表", Description = "可以查看報表", Category = "報表管理", IsSystem = true },
            new() { Resource = "report", Action = "export", Permission = "report:export", DisplayName = "匯出報表", Description = "可以匯出報表", Category = "報表管理", IsSystem = true },
            new() { Resource = "report", Action = "create", Permission = "report:create", DisplayName = "建立報表", Description = "可以建立報表", Category = "報表管理", IsSystem = true }
        };

        // 預設角色權限對應
        private static readonly Dictionary<string, List<string>> DefaultRolePermissions = new()
        {
            ["superadmin"] = new List<string> { "*" }, // 所有權限
            ["admin"] = new List<string> 
            { 
                "user:*", "project:*", "person:*", "file:*", "report:*", "search:*"
            },
            ["user"] = new List<string> 
            { 
                "project:read", "project:create", 
                "person:*", 
                "file:upload", "file:download",
                "search:perform",
                "report:view"
            },
            ["guest"] = new List<string> 
            { 
                "project:read", "person:read", "report:view"
            }
        };

        public PermissionService(IDatabaseHelper db, IMemoryCache cache, ILogger<PermissionService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(string userId, string permission)
        {
            try
            {
                // 取得使用者的所有權限
                var userPermissions = await GetUserPermissionsAsync(userId);
                
                // 檢查是否有萬用字元權限
                if (userPermissions.Contains("*"))
                    return true;
                
                // 檢查精確權限
                if (userPermissions.Contains(permission))
                    return true;
                
                // 檢查資源層級的萬用字元權限 (例如: user:*)
                var resourceWildcard = permission.Split(':')[0] + ":*";
                if (userPermissions.Contains(resourceWildcard))
                    return true;
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查權限時發生錯誤: UserId={UserId}, Permission={Permission}", userId, permission);
                return false;
            }
        }

        public async Task<bool> HasAnyPermissionAsync(string userId, params string[] permissions)
        {
            foreach (var permission in permissions)
            {
                if (await HasPermissionAsync(userId, permission))
                    return true;
            }
            return false;
        }

        public async Task<bool> HasAllPermissionsAsync(string userId, params string[] permissions)
        {
            foreach (var permission in permissions)
            {
                if (!await HasPermissionAsync(userId, permission))
                    return false;
            }
            return true;
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            var cacheKey = $"user_permissions_{userId}";
            
            if (_cache.TryGetValue<List<string>>(cacheKey, out var cachedPermissions))
            {
                return cachedPermissions;
            }

            try
            {
                var permissions = new HashSet<string>();

                // 取得使用者的所有角色
                var userRoles = await GetUserRolesAsync(userId);
                
                // 取得每個角色的權限
                foreach (var role in userRoles)
                {
                    var rolePermissions = await GetRolePermissionsAsync(role.Id);
                    foreach (var permission in rolePermissions)
                    {
                        permissions.Add(permission);
                    }
                }

                // 取得使用者的額外權限
                var sql = @"
                    SELECT p.permission 
                    FROM user_permissions up
                    JOIN permissions p ON up.permission_id = p.id
                    WHERE up.user_id = @UserId";
                
                var userSpecificPermissions = await _db.QueryAsync<string>(sql, new { UserId = userId });
                foreach (var permission in userSpecificPermissions)
                {
                    permissions.Add(permission);
                }

                var result = permissions.ToList();
                _cache.Set(cacheKey, result, _cacheExpiration);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者權限時發生錯誤: UserId={UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<List<string>> GetRolePermissionsAsync(string roleId)
        {
            var cacheKey = $"role_permissions_{roleId}";
            
            if (_cache.TryGetValue<List<string>>(cacheKey, out var cachedPermissions))
            {
                return cachedPermissions;
            }

            try
            {
                // 檢查是否為預設角色
                if (DefaultRolePermissions.ContainsKey(roleId))
                {
                    var defaultPerms = DefaultRolePermissions[roleId];
                    _cache.Set(cacheKey, defaultPerms, _cacheExpiration);
                    return defaultPerms;
                }

                // 從資料庫取得角色權限
                var sql = @"
                    SELECT p.permission 
                    FROM role_permissions rp
                    JOIN permissions p ON rp.permission_id = p.id
                    WHERE rp.role_id = @RoleId";
                
                var permissions = await _db.QueryAsync<string>(sql, new { RoleId = roleId });
                var result = permissions.ToList();
                
                _cache.Set(cacheKey, result, _cacheExpiration);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得角色權限時發生錯誤: RoleId={RoleId}", roleId);
                return new List<string>();
            }
        }

        public async Task<bool> SetRolePermissionsAsync(string roleId, List<string> permissions)
        {
            try
            {
                using var connection = _db.GetConnection();
                using var transaction = connection.BeginTransaction();

                // 刪除現有權限
                await connection.ExecuteAsync(
                    "DELETE FROM role_permissions WHERE role_id = @RoleId",
                    new { RoleId = roleId },
                    transaction);

                // 新增權限
                foreach (var permission in permissions)
                {
                    var permissionId = await GetOrCreatePermissionIdAsync(permission, connection, transaction);
                    if (!string.IsNullOrEmpty(permissionId))
                    {
                        await connection.ExecuteAsync(
                            "INSERT INTO role_permissions (role_id, permission_id) VALUES (@RoleId, @PermissionId)",
                            new { RoleId = roleId, PermissionId = permissionId },
                            transaction);
                    }
                }

                transaction.Commit();
                
                // 清除快取
                _cache.Remove($"role_permissions_{roleId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定角色權限時發生錯誤: RoleId={RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> SetUserPermissionsAsync(string userId, List<string> permissions)
        {
            try
            {
                using var connection = _db.GetConnection();
                using var transaction = connection.BeginTransaction();

                // 刪除現有額外權限
                await connection.ExecuteAsync(
                    "DELETE FROM user_permissions WHERE user_id = @UserId",
                    new { UserId = userId },
                    transaction);

                // 新增額外權限
                foreach (var permission in permissions)
                {
                    var permissionId = await GetOrCreatePermissionIdAsync(permission, connection, transaction);
                    if (!string.IsNullOrEmpty(permissionId))
                    {
                        await connection.ExecuteAsync(
                            "INSERT INTO user_permissions (user_id, permission_id) VALUES (@UserId, @PermissionId)",
                            new { UserId = userId, PermissionId = permissionId },
                            transaction);
                    }
                }

                transaction.Commit();
                
                // 清除快取
                _cache.Remove($"user_permissions_{userId}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定使用者權限時發生錯誤: UserId={UserId}", userId);
                return false;
            }
        }

        public async Task<bool> HasProjectPermissionAsync(string userId, string projectId, string permission)
        {
            try
            {
                // 先檢查系統權限
                if (await HasPermissionAsync(userId, permission))
                    return true;

                // 檢查專案成員權限
                var sql = @"
                    SELECT pm.role
                    FROM project_members pm
                    WHERE pm.project_id = @ProjectId AND pm.user_id = @UserId";
                
                var projectRole = await _db.QueryFirstOrDefaultAsync<string>(sql, new { ProjectId = projectId, UserId = userId });
                
                if (string.IsNullOrEmpty(projectRole))
                    return false;

                // 根據專案角色檢查權限
                return projectRole switch
                {
                    "owner" => true, // 專案擁有者有所有權限
                    "admin" => permission.Contains("read") || permission.Contains("update") || permission.Contains("manage_members"),
                    "editor" => permission.Contains("read") || permission.Contains("update"),
                    "viewer" => permission.Contains("read"),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查專案權限時發生錯誤: UserId={UserId}, ProjectId={ProjectId}, Permission={Permission}", 
                    userId, projectId, permission);
                return false;
            }
        }

        public async Task<bool> IsResourceOwnerAsync(string userId, string resourceType, string resourceId)
        {
            try
            {
                var sql = resourceType.ToLower() switch
                {
                    "project" => "SELECT COUNT(*) FROM projects WHERE id = @ResourceId AND created_by = @UserId",
                    "person" => "SELECT COUNT(*) FROM person_data WHERE id = @ResourceId AND created_by = @UserId",
                    "file" => "SELECT COUNT(*) FROM file_metadata WHERE id = @ResourceId AND created_by = @UserId",
                    _ => null
                };

                if (sql == null)
                    return false;

                var count = await _db.QueryFirstOrDefaultAsync<int>(sql, new { ResourceId = resourceId, UserId = userId });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查資源擁有權時發生錯誤: UserId={UserId}, ResourceType={ResourceType}, ResourceId={ResourceId}", 
                    userId, resourceType, resourceId);
                return false;
            }
        }

        public async Task<List<PermissionDefinition>> GetAllPermissionDefinitionsAsync()
        {
            try
            {
                var sql = @"
                    SELECT resource, action, permission, display_name as DisplayName, 
                           description, category, is_system as IsSystem
                    FROM permissions
                    ORDER BY category, resource, action";
                
                var dbPermissions = await _db.QueryAsync<PermissionDefinition>(sql);
                var result = dbPermissions.ToList();

                // 如果資料庫沒有權限定義，使用預設值
                if (!result.Any())
                {
                    await InitializeDefaultPermissionsAsync();
                    return DefaultPermissions;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限定義時發生錯誤");
                return DefaultPermissions;
            }
        }

        public async Task<List<RoleInfo>> GetAllRolesAsync()
        {
            try
            {
                var sql = @"
                    SELECT 
                        r.id as Id,
                        r.display_name as DisplayName,
                        r.description as Description,
                        r.level as Level,
                        r.is_system as IsSystem,
                        r.created_at as CreatedAt,
                        r.updated_at as UpdatedAt,
                        COUNT(DISTINCT ur.user_id) as UserCount
                    FROM roles r
                    LEFT JOIN user_roles ur ON r.id = ur.role_id
                    GROUP BY r.id, r.display_name, r.description, r.level, r.is_system, r.created_at, r.updated_at
                    ORDER BY r.level DESC";
                
                var roles = await _db.QueryAsync<RoleInfo>(sql);
                return roles.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得所有角色時發生錯誤");
                return new List<RoleInfo>();
            }
        }

        public async Task<bool> CreateRoleAsync(string roleId, string displayName, string description, int level)
        {
            try
            {
                var sql = @"
                    INSERT INTO roles (id, display_name, description, level, is_system, created_at, updated_at)
                    VALUES (@Id, @DisplayName, @Description, @Level, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";
                
                await _db.ExecuteAsync(sql, new
                {
                    Id = roleId,
                    DisplayName = displayName,
                    Description = description,
                    Level = level
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立角色時發生錯誤: RoleId={RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> UpdateRoleAsync(string roleId, string displayName, string description, int level)
        {
            try
            {
                var sql = @"
                    UPDATE roles 
                    SET display_name = @DisplayName, 
                        description = @Description, 
                        level = @Level,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE id = @Id AND is_system = false";
                
                var affected = await _db.ExecuteAsync(sql, new
                {
                    Id = roleId,
                    DisplayName = displayName,
                    Description = description,
                    Level = level
                });

                // 清除快取
                _cache.Remove($"role_permissions_{roleId}");

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新角色時發生錯誤: RoleId={RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> DeleteRoleAsync(string roleId)
        {
            try
            {
                // 檢查是否為系統角色
                var checkSql = "SELECT is_system FROM roles WHERE id = @Id";
                var isSystem = await _db.QueryFirstOrDefaultAsync<bool>(checkSql, new { Id = roleId });
                
                if (isSystem)
                {
                    _logger.LogWarning("嘗試刪除系統角色: RoleId={RoleId}", roleId);
                    return false;
                }

                // 刪除角色（相關的 user_roles 和 role_permissions 會由 CASCADE 自動刪除）
                var sql = "DELETE FROM roles WHERE id = @Id AND is_system = false";
                var affected = await _db.ExecuteAsync(sql, new { Id = roleId });

                // 清除快取
                _cache.Remove($"role_permissions_{roleId}");

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除角色時發生錯誤: RoleId={RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> AssignRoleToUserAsync(string userId, string roleId)
        {
            try
            {
                // 檢查是否已有該角色
                var checkSql = "SELECT COUNT(*) FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId";
                var exists = await _db.QueryFirstOrDefaultAsync<int>(checkSql, new { UserId = userId, RoleId = roleId });
                
                if (exists > 0)
                    return true;

                // 指派角色
                var sql = @"
                    INSERT INTO user_roles (user_id, role_id, assigned_at, assigned_by)
                    VALUES (@UserId, @RoleId, CURRENT_TIMESTAMP, @AssignedBy)";
                
                await _db.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedBy = "system" // TODO: 從 context 取得當前使用者
                });

                // 清除快取
                _cache.Remove($"user_permissions_{userId}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "指派角色給使用者時發生錯誤: UserId={UserId}, RoleId={RoleId}", userId, roleId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleFromUserAsync(string userId, string roleId)
        {
            try
            {
                var sql = "DELETE FROM user_roles WHERE user_id = @UserId AND role_id = @RoleId";
                var affected = await _db.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId });

                // 清除快取
                _cache.Remove($"user_permissions_{userId}");

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除使用者角色時發生錯誤: UserId={UserId}, RoleId={RoleId}", userId, roleId);
                return false;
            }
        }

        public async Task<List<RoleInfo>> GetUserRolesAsync(string userId)
        {
            try
            {
                var sql = @"
                    SELECT 
                        r.id as Id,
                        r.display_name as DisplayName,
                        r.description as Description,
                        r.level as Level,
                        r.is_system as IsSystem,
                        ur.assigned_at as CreatedAt
                    FROM user_roles ur
                    JOIN roles r ON ur.role_id = r.id
                    WHERE ur.user_id = @UserId
                    ORDER BY r.level DESC";
                
                var roles = await _db.QueryAsync<RoleInfo>(sql, new { UserId = userId });
                return roles.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得使用者角色時發生錯誤: UserId={UserId}", userId);
                return new List<RoleInfo>();
            }
        }

        private async Task<string> GetOrCreatePermissionIdAsync(string permission, IDbConnection connection, IDbTransaction transaction)
        {
            try
            {
                // 檢查權限是否存在
                var checkSql = "SELECT id FROM permissions WHERE permission = @Permission";
                var permissionId = await connection.QueryFirstOrDefaultAsync<string>(
                    checkSql, 
                    new { Permission = permission }, 
                    transaction);

                if (!string.IsNullOrEmpty(permissionId))
                    return permissionId;

                // 建立新權限
                var parts = permission.Split(':');
                if (parts.Length != 2)
                    return null;

                var defaultPerm = DefaultPermissions.FirstOrDefault(p => p.Permission == permission);
                if (defaultPerm == null)
                {
                    // 如果不是預設權限，建立一個基本的權限定義
                    defaultPerm = new PermissionDefinition
                    {
                        Resource = parts[0],
                        Action = parts[1],
                        Permission = permission,
                        DisplayName = $"{parts[0]} {parts[1]}",
                        Description = $"Permission for {parts[0]} {parts[1]}",
                        Category = "自訂權限",
                        IsSystem = false
                    };
                }

                var insertSql = @"
                    INSERT INTO permissions (id, resource, action, permission, display_name, description, category, is_system)
                    VALUES (gen_random_uuid()::text, @Resource, @Action, @Permission, @DisplayName, @Description, @Category, @IsSystem)
                    RETURNING id";

                permissionId = await connection.QueryFirstOrDefaultAsync<string>(
                    insertSql, 
                    defaultPerm, 
                    transaction);

                return permissionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得或建立權限ID時發生錯誤: Permission={Permission}", permission);
                return null;
            }
        }

        private async Task InitializeDefaultPermissionsAsync()
        {
            try
            {
                using var connection = _db.GetConnection();
                using var transaction = connection.BeginTransaction();

                foreach (var perm in DefaultPermissions)
                {
                    var sql = @"
                        INSERT INTO permissions (id, resource, action, permission, display_name, description, category, is_system)
                        VALUES (gen_random_uuid()::text, @Resource, @Action, @Permission, @DisplayName, @Description, @Category, @IsSystem)
                        ON CONFLICT (permission) DO NOTHING";

                    await connection.ExecuteAsync(sql, perm, transaction);
                }

                transaction.Commit();
                _logger.LogInformation("預設權限初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化預設權限時發生錯誤");
            }
        }
    }
}