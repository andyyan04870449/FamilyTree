// 存取控制服務 - 提供統一的權限驗證和存取控制
// 設計改善：建立存取控制檢查，強化安全性
using Microsoft.Extensions.Logging;
using familytree_backend.Constants;

namespace familytree_backend.Services
{
    /// <summary>
    /// 存取控制服務介面
    /// 設計理念：提供統一的權限驗證和存取控制功能
    /// </summary>
    public interface IAccessControlService
    {
        /// <summary>
        /// 檢查用戶是否有權限存取資源
        /// </summary>
        Task<bool> HasPermissionAsync(string userId, string resource, string action);

        /// <summary>
        /// 檢查用戶是否有權限存取專案
        /// </summary>
        Task<bool> HasProjectAccessAsync(string userId, string projectId, ProjectAccessLevel accessLevel);

        /// <summary>
        /// 檢查用戶是否有權限執行操作
        /// </summary>
        Task<bool> CanPerformActionAsync(string userId, string action, object? context = null);

        /// <summary>
        /// 驗證用戶角色
        /// </summary>
        Task<bool> ValidateUserRoleAsync(string userId, string requiredRole);

        /// <summary>
        /// 檢查資源擁有權
        /// </summary>
        Task<bool> IsResourceOwnerAsync(string userId, string resourceType, string resourceId);

        /// <summary>
        /// 記錄存取事件
        /// </summary>
        Task LogAccessEventAsync(string userId, string resource, string action, bool isAllowed, object? context = null);

        /// <summary>
        /// 檢查 API 存取限制
        /// </summary>
        Task<bool> CheckApiAccessLimitAsync(string userId, string apiEndpoint);

        /// <summary>
        /// 檢查檔案存取權限
        /// </summary>
        Task<bool> HasFileAccessAsync(string userId, string filePath, FileAccessLevel accessLevel);
    }

    /// <summary>
    /// 存取控制服務實作
    /// 職責：提供統一的權限驗證和存取控制，確保系統安全性
    /// </summary>
    public class AccessControlService : IAccessControlService
    {
        private readonly ILogger<AccessControlService> _logger;
        private readonly IConfigurationService _configurationService;
        private readonly ILoggingService _loggingService;
        private readonly SecurityConfiguration _securityConfig;

        // 存取限制快取（簡單實作，生產環境應使用 Redis 等分散式快取）
        private readonly Dictionary<string, DateTime> _accessAttempts = new();
        private readonly object _lockObject = new object();

        /// <summary>
        /// 建構子
        /// </summary>
        public AccessControlService(
            ILogger<AccessControlService> logger, 
            IConfigurationService configurationService,
            ILoggingService loggingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _securityConfig = configurationService.GetSecurityConfiguration();
        }

        /// <summary>
        /// 檢查用戶是否有權限存取資源
        /// 設計理念：統一的資源存取權限檢查
        /// </summary>
        public async Task<bool> HasPermissionAsync(string userId, string resource, string action)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("權限檢查失敗 - 用戶 ID 為空");
                    return false;
                }

                // 檢查用戶是否存在（這裡應該查詢資料庫）
                if (!await UserExistsAsync(userId))
                {
                    _logger.LogWarning("權限檢查失敗 - 用戶不存在: {UserId}", userId);
                    return false;
                }

                // 檢查用戶角色權限
                var userRole = await GetUserRoleAsync(userId);
                if (string.IsNullOrEmpty(userRole))
                {
                    _logger.LogWarning("權限檢查失敗 - 用戶無角色: {UserId}", userId);
                    return false;
                }

                // 根據角色檢查權限
                var hasPermission = await CheckRolePermissionAsync(userRole, resource, action);
                
                // 記錄存取事件
                await LogAccessEventAsync(userId, resource, action, hasPermission);

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "權限檢查發生錯誤 - 用戶: {UserId}, 資源: {Resource}, 操作: {Action}", 
                    userId, resource, action);
                return false;
            }
        }

        /// <summary>
        /// 檢查用戶是否有權限存取專案
        /// 設計理念：專案級別的存取控制
        /// </summary>
        public async Task<bool> HasProjectAccessAsync(string userId, string projectId, ProjectAccessLevel accessLevel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(projectId))
                {
                    _logger.LogWarning("專案存取檢查失敗 - 參數為空");
                    return false;
                }

                // 檢查專案是否存在
                if (!await ProjectExistsAsync(projectId))
                {
                    _logger.LogWarning("專案存取檢查失敗 - 專案不存在: {ProjectId}", projectId);
                    return false;
                }

                // 檢查用戶角色
                var userRole = await GetUserRoleAsync(userId);
                if (string.IsNullOrEmpty(userRole))
                {
                    _logger.LogWarning("專案存取檢查失敗 - 用戶無角色: {UserId}", userId);
                    return false;
                }

                // 管理員擁有所有權限
                if (userRole == _securityConfig.AdminRole)
                {
                    return true;
                }

                // 檢查專案成員權限
                var projectAccess = await GetProjectAccessLevelAsync(userId, projectId);
                var hasAccess = projectAccess >= accessLevel;

                // 記錄存取事件
                await LogAccessEventAsync(userId, $"project:{projectId}", accessLevel.ToString(), hasAccess);

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "專案存取檢查發生錯誤 - 用戶: {UserId}, 專案: {ProjectId}, 權限等級: {AccessLevel}", 
                    userId, projectId, accessLevel);
                return false;
            }
        }

        /// <summary>
        /// 檢查用戶是否有權限執行操作
        /// 設計理念：操作級別的權限檢查
        /// </summary>
        public async Task<bool> CanPerformActionAsync(string userId, string action, object? context = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    _logger.LogWarning("操作權限檢查失敗 - 用戶 ID 為空");
                    return false;
                }

                // 檢查用戶角色
                var userRole = await GetUserRoleAsync(userId);
                if (string.IsNullOrEmpty(userRole))
                {
                    _logger.LogWarning("操作權限檢查失敗 - 用戶無角色: {UserId}", userId);
                    return false;
                }

                // 根據操作類型檢查權限
                var canPerform = await CheckActionPermissionAsync(userRole, action, context);

                // 記錄存取事件
                await LogAccessEventAsync(userId, "action", action, canPerform, context);

                return canPerform;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "操作權限檢查發生錯誤 - 用戶: {UserId}, 操作: {Action}", 
                    userId, action);
                return false;
            }
        }

        /// <summary>
        /// 驗證用戶角色
        /// 設計理念：角色驗證功能
        /// </summary>
        public async Task<bool> ValidateUserRoleAsync(string userId, string requiredRole)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(requiredRole))
                {
                    return false;
                }

                var userRole = await GetUserRoleAsync(userId);
                if (string.IsNullOrEmpty(userRole))
                {
                    return false;
                }

                // 檢查角色層級
                var hasRole = await CheckRoleHierarchyAsync(userRole, requiredRole);

                _logger.LogDebug("角色驗證 - 用戶: {UserId}, 當前角色: {UserRole}, 要求角色: {RequiredRole}, 結果: {HasRole}", 
                    userId, userRole, requiredRole, hasRole);

                return hasRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "角色驗證發生錯誤 - 用戶: {UserId}, 要求角色: {RequiredRole}", 
                    userId, requiredRole);
                return false;
            }
        }

        /// <summary>
        /// 檢查資源擁有權
        /// 設計理念：資源擁有權驗證
        /// </summary>
        public async Task<bool> IsResourceOwnerAsync(string userId, string resourceType, string resourceId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(resourceType) || string.IsNullOrWhiteSpace(resourceId))
                {
                    return false;
                }

                // 檢查用戶角色（管理員擁有所有資源）
                var userRole = await GetUserRoleAsync(userId);
                if (userRole == _securityConfig.AdminRole)
                {
                    return true;
                }

                // 根據資源類型檢查擁有權
                var isOwner = await CheckResourceOwnershipAsync(userId, resourceType, resourceId);

                _logger.LogDebug("資源擁有權檢查 - 用戶: {UserId}, 資源類型: {ResourceType}, 資源ID: {ResourceId}, 結果: {IsOwner}", 
                    userId, resourceType, resourceId, isOwner);

                return isOwner;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "資源擁有權檢查發生錯誤 - 用戶: {UserId}, 資源類型: {ResourceType}, 資源ID: {ResourceId}", 
                    userId, resourceType, resourceId);
                return false;
            }
        }

        /// <summary>
        /// 記錄存取事件
        /// 設計理念：記錄所有存取事件用於審計
        /// </summary>
        public async Task LogAccessEventAsync(string userId, string resource, string action, bool isAllowed, object? context = null)
        {
            try
            {
                var eventType = isAllowed ? "AccessAllowed" : "AccessDenied";
                var logData = new
                {
                    UserId = userId,
                    Resource = resource,
                    Action = action,
                    IsAllowed = isAllowed,
                    Context = context,
                    Timestamp = DateTime.UtcNow
                };

                if (isAllowed)
                {
                    _logger.LogInformation("🔓 存取允許 - 用戶: {UserId}, 資源: {Resource}, 操作: {Action}", 
                        userId, resource, action);
                }
                else
                {
                    _logger.LogWarning("🔒 存取拒絕 - 用戶: {UserId}, 資源: {Resource}, 操作: {Action}", 
                        userId, resource, action);
                }

                // 記錄安全性事件
                _loggingService.LogSecurityEvent(eventType, userId, logData);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "記錄存取事件失敗 - 用戶: {UserId}, 資源: {Resource}, 操作: {Action}", 
                    userId, resource, action);
            }
        }

        /// <summary>
        /// 檢查 API 存取限制
        /// 設計理念：防止 API 濫用
        /// </summary>
        public async Task<bool> CheckApiAccessLimitAsync(string userId, string apiEndpoint)
        {
            try
            {
                var key = $"{userId}:{apiEndpoint}";
                var now = DateTime.UtcNow;

                lock (_lockObject)
                {
                    // 清理過期的存取記錄
                    var expiredKeys = _accessAttempts.Where(kvp => now - kvp.Value > TimeSpan.FromMinutes(1)).ToList();
                    foreach (var expiredKey in expiredKeys)
                    {
                        _accessAttempts.Remove(expiredKey.Key);
                    }

                    // 檢查存取頻率
                    if (_accessAttempts.ContainsKey(key))
                    {
                        var lastAccess = _accessAttempts[key];
                        if (now - lastAccess < TimeSpan.FromSeconds(1)) // 每秒最多一次
                        {
                            _logger.LogWarning("API 存取限制 - 用戶: {UserId}, 端點: {ApiEndpoint}, 頻率過高", 
                                userId, apiEndpoint);
                            return false;
                        }
                    }

                    // 記錄存取時間
                    _accessAttempts[key] = now;
                }

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API 存取限制檢查發生錯誤 - 用戶: {UserId}, 端點: {ApiEndpoint}", 
                    userId, apiEndpoint);
                return false;
            }
        }

        /// <summary>
        /// 檢查檔案存取權限
        /// 設計理念：檔案級別的存取控制
        /// </summary>
        public async Task<bool> HasFileAccessAsync(string userId, string filePath, FileAccessLevel accessLevel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(filePath))
                {
                    return false;
                }

                // 檢查檔案是否存在
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("檔案存取檢查失敗 - 檔案不存在: {FilePath}", filePath);
                    return false;
                }

                // 檢查用戶角色
                var userRole = await GetUserRoleAsync(userId);
                if (string.IsNullOrEmpty(userRole))
                {
                    return false;
                }

                // 管理員擁有所有權限
                if (userRole == _securityConfig.AdminRole)
                {
                    return true;
                }

                // 檢查檔案擁有權
                var isOwner = await IsFileOwnerAsync(userId, filePath);
                if (isOwner)
                {
                    return true;
                }

                // 檢查檔案共享權限
                var hasSharedAccess = await CheckFileSharedAccessAsync(userId, filePath, accessLevel);

                // 記錄存取事件
                await LogAccessEventAsync(userId, $"file:{filePath}", accessLevel.ToString(), hasSharedAccess);

                return hasSharedAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檔案存取檢查發生錯誤 - 用戶: {UserId}, 檔案: {FilePath}, 權限等級: {AccessLevel}", 
                    userId, filePath, accessLevel);
                return false;
            }
        }

        #region 私有輔助方法

        /// <summary>
        /// 檢查用戶是否存在
        /// </summary>
        private async Task<bool> UserExistsAsync(string userId)
        {
            // 這裡應該查詢資料庫檢查用戶是否存在
            // 暫時返回 true 作為預設值
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 獲取用戶角色
        /// </summary>
        private async Task<string> GetUserRoleAsync(string userId)
        {
            // 這裡應該查詢資料庫獲取用戶角色
            // 暫時返回 User 角色作為預設值
            return await Task.FromResult(_securityConfig.UserRole);
        }

        /// <summary>
        /// 檢查角色權限
        /// </summary>
        private async Task<bool> CheckRolePermissionAsync(string userRole, string resource, string action)
        {
            // 這裡應該查詢資料庫或配置檢查角色權限
            // 暫時的權限邏輯
            if (userRole == _securityConfig.AdminRole)
            {
                return await Task.FromResult(true);
            }

            if (userRole == _securityConfig.UserRole)
            {
                // 用戶角色可以執行大部分操作，但有一些限制
                var restrictedActions = new[] { "delete", "admin", "system" };
                return await Task.FromResult(!restrictedActions.Contains(action.ToLower()));
            }

            return await Task.FromResult(false);
        }

        /// <summary>
        /// 檢查專案是否存在
        /// </summary>
        private async Task<bool> ProjectExistsAsync(string projectId)
        {
            // 這裡應該查詢資料庫檢查專案是否存在
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 獲取專案存取等級
        /// </summary>
        private async Task<ProjectAccessLevel> GetProjectAccessLevelAsync(string userId, string projectId)
        {
            // 這裡應該查詢資料庫獲取用戶在專案中的存取等級
            return await Task.FromResult(ProjectAccessLevel.Read);
        }

        /// <summary>
        /// 檢查操作權限
        /// </summary>
        private async Task<bool> CheckActionPermissionAsync(string userRole, string action, object? context)
        {
            // 這裡應該根據角色和操作類型檢查權限
            if (userRole == _securityConfig.AdminRole)
            {
                return await Task.FromResult(true);
            }

            // 根據操作類型檢查權限
            var allowedActions = new[] { "read", "create", "update", "search" };
            return await Task.FromResult(allowedActions.Contains(action.ToLower()));
        }

        /// <summary>
        /// 檢查角色層級
        /// </summary>
        private async Task<bool> CheckRoleHierarchyAsync(string userRole, string requiredRole)
        {
            // 角色層級：Admin > User > Guest
            var roleHierarchy = new Dictionary<string, int>
            {
                { _securityConfig.AdminRole, 3 },
                { _securityConfig.UserRole, 2 },
                { _securityConfig.GuestRole, 1 }
            };

            var userLevel = roleHierarchy.GetValueOrDefault(userRole, 0);
            var requiredLevel = roleHierarchy.GetValueOrDefault(requiredRole, 0);

            return await Task.FromResult(userLevel >= requiredLevel);
        }

        /// <summary>
        /// 檢查資源擁有權
        /// </summary>
        private async Task<bool> CheckResourceOwnershipAsync(string userId, string resourceType, string resourceId)
        {
            // 這裡應該查詢資料庫檢查資源擁有權
            // 暫時返回 false 作為預設值
            return await Task.FromResult(false);
        }

        /// <summary>
        /// 檢查檔案擁有權
        /// </summary>
        private async Task<bool> IsFileOwnerAsync(string userId, string filePath)
        {
            // 這裡應該查詢資料庫檢查檔案擁有權
            return await Task.FromResult(false);
        }

        /// <summary>
        /// 檢查檔案共享權限
        /// </summary>
        private async Task<bool> CheckFileSharedAccessAsync(string userId, string filePath, FileAccessLevel accessLevel)
        {
            // 這裡應該查詢資料庫檢查檔案共享權限
            return await Task.FromResult(false);
        }

        #endregion
    }

    /// <summary>
    /// 專案存取等級
    /// </summary>
    public enum ProjectAccessLevel
    {
        None = 0,
        Read = 1,
        Write = 2,
        Admin = 3
    }

    /// <summary>
    /// 檔案存取等級
    /// </summary>
    public enum FileAccessLevel
    {
        None = 0,
        Read = 1,
        Write = 2,
        Delete = 3
    }
} 