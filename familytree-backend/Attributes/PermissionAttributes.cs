using Microsoft.AspNetCore.Authorization;
using FamilyTree.Constants;

namespace FamilyTree.Attributes
{
    /// <summary>
    /// 權限授權屬性 - 使用政策為基礎的授權
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(string permission, bool allowOwner = false, string? resourceType = null)
        {
            Policy = CreatePolicyName("Permission", permission, allowOwner, resourceType);
        }

        private static string CreatePolicyName(string type, string permission, bool allowOwner, string? resourceType)
        {
            var parts = new List<string> { type, permission };
            if (allowOwner) parts.Add("AllowOwner");
            if (!string.IsNullOrEmpty(resourceType)) parts.Add($"Resource_{resourceType}");
            return string.Join("_", parts);
        }
    }

    /// <summary>
    /// 任一權限授權屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HasAnyPermissionAttribute : AuthorizeAttribute
    {
        public HasAnyPermissionAttribute(params string[] permissions)
        {
            Policy = $"AnyPermission_{string.Join("_", permissions)}";
        }
    }

    /// <summary>
    /// 所有權限授權屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HasAllPermissionsAttribute : AuthorizeAttribute
    {
        public HasAllPermissionsAttribute(params string[] permissions)
        {
            Policy = $"AllPermissions_{string.Join("_", permissions)}";
        }
    }

    /// <summary>
    /// 資源擁有者授權屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireResourceOwnerAttribute : AuthorizeAttribute
    {
        public RequireResourceOwnerAttribute(string resourceType)
        {
            Policy = $"ResourceOwner_{resourceType}";
        }
    }

    /// <summary>
    /// 角色階層授權屬性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireRoleHierarchyAttribute : AuthorizeAttribute
    {
        public RequireRoleHierarchyAttribute(string minimumRole)
        {
            Policy = $"RoleHierarchy_{minimumRole}";
        }
    }

    /// <summary>
    /// 稽核系統權限屬性
    /// </summary>
    public class AuditPermissionAttribute : HasPermissionAttribute
    {
        public AuditPermissionAttribute(string action) : base($"audit:{action}")
        {
        }
    }

    /// <summary>
    /// 管理員權限屬性
    /// </summary>
    public class AdminPermissionAttribute : RequireRoleHierarchyAttribute
    {
        public AdminPermissionAttribute() : base(RoleConstants.ADMIN)
        {
        }
    }

    /// <summary>
    /// 超級管理員權限屬性
    /// </summary>
    public class SuperAdminPermissionAttribute : RequireRoleHierarchyAttribute
    {
        public SuperAdminPermissionAttribute() : base(RoleConstants.SUPER_ADMIN)
        {
        }
    }

    /// <summary>
    /// 稽核讀取權限屬性
    /// </summary>
    public class AuditReaderPermissionAttribute : HasAnyPermissionAttribute
    {
        public AuditReaderPermissionAttribute() : base("audit:read", "role:admin", "role:super_admin")
        {
        }
    }
}