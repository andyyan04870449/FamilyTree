using System.Security.Claims;

namespace FamilyTree.Services.Authorization
{
    /// <summary>
    /// 權限策略介面
    /// 定義權限檢查的核心邏輯
    /// </summary>
    public interface IPermissionStrategy
    {
        /// <summary>
        /// 檢查使用者是否有指定權限
        /// </summary>
        /// <param name="user">使用者聲明主體</param>
        /// <param name="permission">權限要求</param>
        /// <param name="resource">資源資訊（可選）</param>
        /// <returns>是否有權限</returns>
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission, object? resource = null);

        /// <summary>
        /// 檢查使用者是否有任一權限
        /// </summary>
        /// <param name="user">使用者聲明主體</param>
        /// <param name="permissions">權限列表</param>
        /// <param name="resource">資源資訊（可選）</param>
        /// <returns>是否有任一權限</returns>
        Task<bool> HasAnyPermissionAsync(ClaimsPrincipal user, IEnumerable<string> permissions, object? resource = null);

        /// <summary>
        /// 檢查使用者是否有所有權限
        /// </summary>
        /// <param name="user">使用者聲明主體</param>
        /// <param name="permissions">權限列表</param>
        /// <param name="resource">資源資訊（可選）</param>
        /// <returns>是否有所有權限</returns>
        Task<bool> HasAllPermissionsAsync(ClaimsPrincipal user, IEnumerable<string> permissions, object? resource = null);

        /// <summary>
        /// 檢查使用者是否為資源擁有者
        /// </summary>
        /// <param name="user">使用者聲明主體</param>
        /// <param name="resourceType">資源類型</param>
        /// <param name="resourceId">資源ID</param>
        /// <returns>是否為資源擁有者</returns>
        Task<bool> IsResourceOwnerAsync(ClaimsPrincipal user, string resourceType, string resourceId);
    }

    /// <summary>
    /// 權限要求資訊
    /// </summary>
    public class PermissionRequirement
    {
        public string Permission { get; }
        public bool AllowOwner { get; }
        public string? ResourceType { get; }

        public PermissionRequirement(string permission, bool allowOwner = false, string? resourceType = null)
        {
            Permission = permission;
            AllowOwner = allowOwner;
            ResourceType = resourceType;
        }
    }

    /// <summary>
    /// 資源資訊
    /// </summary>
    public class ResourceInfo
    {
        public string Type { get; }
        public string Id { get; }
        public string? OwnerId { get; }
        public Dictionary<string, object> Properties { get; }

        public ResourceInfo(string type, string id, string? ownerId = null)
        {
            Type = type;
            Id = id;
            OwnerId = ownerId;
            Properties = new Dictionary<string, object>();
        }
    }
}