using Microsoft.AspNetCore.Authorization;

namespace FamilyTree.Authorization
{
    /// <summary>
    /// 權限授權需求
    /// </summary>
    public class PermissionAuthorizationRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public bool AllowOwner { get; }
        public string? ResourceType { get; }

        public PermissionAuthorizationRequirement(string permission, bool allowOwner = false, string? resourceType = null)
        {
            Permission = permission;
            AllowOwner = allowOwner;
            ResourceType = resourceType;
        }
    }

    /// <summary>
    /// 任一權限授權需求
    /// </summary>
    public class AnyPermissionAuthorizationRequirement : IAuthorizationRequirement
    {
        public string[] Permissions { get; }
        public bool AllowOwner { get; }
        public string? ResourceType { get; }

        public AnyPermissionAuthorizationRequirement(string[] permissions, bool allowOwner = false, string? resourceType = null)
        {
            Permissions = permissions;
            AllowOwner = allowOwner;
            ResourceType = resourceType;
        }
    }

    /// <summary>
    /// 所有權限授權需求
    /// </summary>
    public class AllPermissionsAuthorizationRequirement : IAuthorizationRequirement
    {
        public string[] Permissions { get; }
        public bool AllowOwner { get; }
        public string? ResourceType { get; }

        public AllPermissionsAuthorizationRequirement(string[] permissions, bool allowOwner = false, string? resourceType = null)
        {
            Permissions = permissions;
            AllowOwner = allowOwner;
            ResourceType = resourceType;
        }
    }

    /// <summary>
    /// 資源擁有者授權需求
    /// </summary>
    public class ResourceOwnerAuthorizationRequirement : IAuthorizationRequirement
    {
        public string ResourceType { get; }

        public ResourceOwnerAuthorizationRequirement(string resourceType)
        {
            ResourceType = resourceType;
        }
    }

    /// <summary>
    /// 角色階層授權需求
    /// </summary>
    public class RoleHierarchyAuthorizationRequirement : IAuthorizationRequirement
    {
        public string MinimumRole { get; }
        public int MinimumLevel { get; }

        public RoleHierarchyAuthorizationRequirement(string minimumRole, int minimumLevel)
        {
            MinimumRole = minimumRole;
            MinimumLevel = minimumLevel;
        }
    }
}