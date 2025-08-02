using System.Security.Claims;

namespace FamilyTree.Services.Authorization
{
    /// <summary>
    /// 權限上下文介面
    /// 提供當前請求的權限相關資訊
    /// </summary>
    public interface IPermissionContext
    {
        /// <summary>
        /// 當前使用者
        /// </summary>
        ClaimsPrincipal? CurrentUser { get; }

        /// <summary>
        /// 當前使用者ID
        /// </summary>
        string? UserId { get; }

        /// <summary>
        /// 當前使用者角色
        /// </summary>
        List<string> UserRoles { get; }

        /// <summary>
        /// 當前請求的資源資訊
        /// </summary>
        ResourceInfo? CurrentResource { get; set; }

        /// <summary>
        /// 檢查當前使用者是否有指定權限
        /// </summary>
        Task<bool> HasPermissionAsync(string permission);

        /// <summary>
        /// 檢查當前使用者是否有任一權限
        /// </summary>
        Task<bool> HasAnyPermissionAsync(params string[] permissions);

        /// <summary>
        /// 檢查當前使用者是否有所有權限
        /// </summary>
        Task<bool> HasAllPermissionsAsync(params string[] permissions);

        /// <summary>
        /// 檢查當前使用者是否為資源擁有者
        /// </summary>
        Task<bool> IsResourceOwnerAsync(string resourceType, string resourceId);

        /// <summary>
        /// 設定當前資源
        /// </summary>
        void SetResource(string resourceType, string resourceId, string? ownerId = null);
    }

    /// <summary>
    /// 權限上下文實現
    /// </summary>
    public class PermissionContext : IPermissionContext
    {
        private readonly IPermissionStrategy _permissionStrategy;

        public PermissionContext(IPermissionStrategy permissionStrategy, ClaimsPrincipal? user = null)
        {
            _permissionStrategy = permissionStrategy;
            CurrentUser = user;
        }

        public ClaimsPrincipal? CurrentUser { get; private set; }

        public string? UserId => CurrentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public List<string> UserRoles => CurrentUser?.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList() ?? new List<string>();

        public ResourceInfo? CurrentResource { get; set; }

        public async Task<bool> HasPermissionAsync(string permission)
        {
            if (CurrentUser == null) return false;
            return await _permissionStrategy.HasPermissionAsync(CurrentUser, permission, CurrentResource);
        }

        public async Task<bool> HasAnyPermissionAsync(params string[] permissions)
        {
            if (CurrentUser == null) return false;
            return await _permissionStrategy.HasAnyPermissionAsync(CurrentUser, permissions, CurrentResource);
        }

        public async Task<bool> HasAllPermissionsAsync(params string[] permissions)
        {
            if (CurrentUser == null) return false;
            return await _permissionStrategy.HasAllPermissionsAsync(CurrentUser, permissions, CurrentResource);
        }

        public async Task<bool> IsResourceOwnerAsync(string resourceType, string resourceId)
        {
            if (CurrentUser == null) return false;
            return await _permissionStrategy.IsResourceOwnerAsync(CurrentUser, resourceType, resourceId);
        }

        public void SetResource(string resourceType, string resourceId, string? ownerId = null)
        {
            CurrentResource = new ResourceInfo(resourceType, resourceId, ownerId);
        }

        public void SetUser(ClaimsPrincipal user)
        {
            CurrentUser = user;
        }
    }
}