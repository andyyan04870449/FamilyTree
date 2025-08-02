using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Dapper;
using Npgsql;
using BCrypt.Net;

namespace familytree_backend.Services
{
    /// <summary>
    /// 資料庫初始化服務
    /// 在應用程式啟動時執行必要的初始化工作
    /// </summary>
    public class DatabaseInitializationService : IHostedService
    {
        private readonly ILogger<DatabaseInitializationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _connectionString;

        public DatabaseInitializationService(
            ILogger<DatabaseInitializationService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("ConnectionString");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("開始資料庫初始化檢查...");

                // 檢查並更新管理員密碼
                await InitializeAdminPasswordAsync();
                
                // 確保管理員有正確的角色
                await EnsureAdminRoleAsync();

                _logger.LogInformation("資料庫初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資料庫初始化失敗");
                // 不要讓初始化錯誤阻止應用程式啟動
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task InitializeAdminPasswordAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                // 檢查管理員密碼是否為預設值
                var sql = @"
                    SELECT password_hash 
                    FROM users 
                    WHERE username = 'admin'";

                var currentHash = await connection.QuerySingleOrDefaultAsync<string>(sql);
                
                if (currentHash == null)
                {
                    _logger.LogWarning("找不到管理員帳號");
                    return;
                }

                // 如果密碼還是預設的 placeholder，更新為真實的密碼
                if (currentHash.Contains("dummyHash") || currentHash.Contains("PLACEHOLDER"))
                {
                    var defaultPassword = "Admin@123";
                    var newHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword);

                    var updateSql = @"
                        UPDATE users 
                        SET password_hash = @PasswordHash 
                        WHERE username = 'admin'";

                    await connection.ExecuteAsync(updateSql, new { PasswordHash = newHash });

                    _logger.LogInformation("管理員密碼已更新為預設密碼: Admin@123");
                    _logger.LogWarning("請儘快變更管理員密碼！");
                }
                else
                {
                    _logger.LogInformation("管理員密碼已設定");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化管理員密碼時發生錯誤");
            }
        }

        private async Task EnsureAdminRoleAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                
                // 檢查管理員是否已有角色
                var checkSql = @"
                    SELECT COUNT(*) 
                    FROM users u
                    LEFT JOIN user_roles ur ON u.id = ur.user_id
                    WHERE u.username = 'admin' AND ur.role_id = 'admin'";

                var hasAdminRole = await connection.QuerySingleOrDefaultAsync<int>(checkSql);
                
                if (hasAdminRole == 0)
                {
                    // 取得管理員使用者 ID
                    var getUserIdSql = "SELECT id FROM users WHERE username = 'admin'";
                    var adminUserId = await connection.QuerySingleOrDefaultAsync<string>(getUserIdSql);
                    
                    if (!string.IsNullOrEmpty(adminUserId))
                    {
                        // 檢查 admin 角色是否存在
                        var roleExistsSql = "SELECT COUNT(*) FROM roles WHERE id = 'admin'";
                        var roleExists = await connection.QuerySingleOrDefaultAsync<int>(roleExistsSql);
                        
                        if (roleExists == 0)
                        {
                            // 建立 admin 角色
                            var createRoleSql = @"
                                INSERT INTO roles (id, display_name, description, level, is_system, created_at, updated_at)
                                VALUES ('admin', '管理員', '系統管理員角色', 90, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)";
                            await connection.ExecuteAsync(createRoleSql);
                            _logger.LogInformation("已建立管理員角色");
                            
                            // 設定 admin 角色的預設權限
                            await SetDefaultAdminPermissionsAsync(connection);
                        }
                        
                        // 指派角色給管理員
                        var assignRoleSql = @"
                            INSERT INTO user_roles (user_id, role_id, assigned_at, assigned_by)
                            VALUES (@UserId, 'admin', CURRENT_TIMESTAMP, 'system')
                            ON CONFLICT (user_id, role_id) DO NOTHING";
                        
                        await connection.ExecuteAsync(assignRoleSql, new { UserId = adminUserId });
                        _logger.LogInformation("已為管理員指派 admin 角色");
                    }
                    else
                    {
                        _logger.LogWarning("找不到管理員帳號");
                    }
                }
                else
                {
                    _logger.LogInformation("管理員已有正確的角色");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "確保管理員角色時發生錯誤");
            }
        }

        private async Task SetDefaultAdminPermissionsAsync(NpgsqlConnection connection)
        {
            try
            {
                // admin 角色的預設權限
                var adminPermissions = new[] { "user:*", "project:*", "person:*", "file:*", "report:*", "role:manage" };
                
                foreach (var permission in adminPermissions)
                {
                    // 確保權限存在
                    var permissionId = await GetOrCreatePermissionIdAsync(connection, permission);
                    
                    if (!string.IsNullOrEmpty(permissionId))
                    {
                        // 指派權限給 admin 角色
                        var assignPermissionSql = @"
                            INSERT INTO role_permissions (role_id, permission_id)
                            VALUES ('admin', @PermissionId)
                            ON CONFLICT (role_id, permission_id) DO NOTHING";
                        
                        await connection.ExecuteAsync(assignPermissionSql, new { PermissionId = permissionId });
                    }
                }
                
                _logger.LogInformation("已設定管理員角色的預設權限");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "設定管理員預設權限時發生錯誤");
            }
        }

        private async Task<string> GetOrCreatePermissionIdAsync(NpgsqlConnection connection, string permission)
        {
            try
            {
                // 檢查權限是否存在
                var checkSql = "SELECT id FROM permissions WHERE permission = @Permission";
                var permissionId = await connection.QueryFirstOrDefaultAsync<string>(checkSql, new { Permission = permission });
                
                if (!string.IsNullOrEmpty(permissionId))
                    return permissionId;
                
                // 建立新權限
                var parts = permission.Split(':');
                if (parts.Length != 2)
                    return null;
                
                var insertSql = @"
                    INSERT INTO permissions (id, resource, action, permission, display_name, description, category, is_system)
                    VALUES (gen_random_uuid()::text, @Resource, @Action, @Permission, @DisplayName, @Description, @Category, true)
                    RETURNING id";
                
                var parameters = new
                {
                    Resource = parts[0],
                    Action = parts[1],
                    Permission = permission,
                    DisplayName = GetPermissionDisplayName(permission),
                    Description = GetPermissionDescription(permission),
                    Category = GetPermissionCategory(parts[0])
                };
                
                permissionId = await connection.QueryFirstOrDefaultAsync<string>(insertSql, parameters);
                return permissionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得或建立權限ID時發生錯誤: Permission={Permission}", permission);
                return null;
            }
        }

        private string GetPermissionDisplayName(string permission)
        {
            return permission switch
            {
                "user:*" => "使用者管理（全部）",
                "project:*" => "專案管理（全部）",
                "person:*" => "人員管理（全部）",
                "file:*" => "檔案管理（全部）",
                "report:*" => "報表管理（全部）",
                "role:manage" => "角色權限管理",
                _ => permission
            };
        }

        private string GetPermissionDescription(string permission)
        {
            return permission switch
            {
                "user:*" => "可以執行所有使用者相關操作",
                "project:*" => "可以執行所有專案相關操作",
                "person:*" => "可以執行所有人員相關操作",
                "file:*" => "可以執行所有檔案相關操作",
                "report:*" => "可以執行所有報表相關操作",
                "role:manage" => "可以管理角色和權限",
                _ => $"允許執行 {permission} 操作"
            };
        }

        private string GetPermissionCategory(string resource)
        {
            return resource switch
            {
                "user" => "使用者管理",
                "role" => "系統管理",
                "project" => "專案管理",
                "person" => "人員管理",
                "file" => "檔案管理",
                "report" => "報表管理",
                _ => "其他"
            };
        }
    }
}