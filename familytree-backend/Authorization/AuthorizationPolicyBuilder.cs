using Microsoft.AspNetCore.Authorization;
using FamilyTree.Constants;

namespace FamilyTree.Authorization
{
    /// <summary>
    /// 授權政策建構器
    /// 集中管理所有授權政策的定義
    /// </summary>
    public static class AuthorizationPolicyBuilder
    {
        /// <summary>
        /// 建構所有授權政策
        /// </summary>
        public static void BuildPolicies(AuthorizationOptions options)
        {
            // 基本角色政策
            BuildRolePolicies(options);

            // 權限為基礎的政策
            BuildPermissionPolicies(options);

            // 稽核系統政策
            BuildAuditPolicies(options);

            // 資源擁有者政策
            BuildResourceOwnerPolicies(options);

            // 角色階層政策
            BuildRoleHierarchyPolicies(options);

            // 複合權限政策
            BuildCompositePermissionPolicies(options);
        }

        /// <summary>
        /// 建構基本角色政策
        /// </summary>
        private static void BuildRolePolicies(AuthorizationOptions options)
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole(RoleConstants.ADMIN, RoleConstants.SUPER_ADMIN));

            options.AddPolicy("RequireSuperAdmin", policy =>
                policy.RequireRole(RoleConstants.SUPER_ADMIN));

            options.AddPolicy("RequireUser", policy =>
                policy.RequireRole(RoleConstants.USER, RoleConstants.ADMIN, RoleConstants.SUPER_ADMIN));

            options.AddPolicy("RequireAuditReader", policy =>
                policy.RequireRole(RoleConstants.ADMIN, RoleConstants.SUPER_ADMIN, "AuditReader"));
        }

        /// <summary>
        /// 建構權限為基礎的政策
        /// </summary>
        private static void BuildPermissionPolicies(AuthorizationOptions options)
        {
            // 使用者管理權限
            options.AddPolicy("Permission_user:create", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("user:create")));

            options.AddPolicy("Permission_user:read", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("user:read", allowOwner: true, resourceType: "user")));

            options.AddPolicy("Permission_user:update", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("user:update", allowOwner: true, resourceType: "user")));

            options.AddPolicy("Permission_user:delete", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("user:delete")));

            // 專案管理權限
            options.AddPolicy("Permission_project:create", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("project:create")));

            options.AddPolicy("Permission_project:read", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("project:read", allowOwner: true, resourceType: "project")));

            options.AddPolicy("Permission_project:update", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("project:update", allowOwner: true, resourceType: "project")));

            options.AddPolicy("Permission_project:delete", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("project:delete")));

            options.AddPolicy("Permission_project:manage_members", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("project:manage_members", allowOwner: true, resourceType: "project")));

            // 人員資料權限
            options.AddPolicy("Permission_person:create", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("person:create")));

            options.AddPolicy("Permission_person:read", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("person:read")));

            options.AddPolicy("Permission_person:update", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("person:update")));

            options.AddPolicy("Permission_person:delete", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("person:delete")));

            options.AddPolicy("Permission_person:export", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("person:export")));

            // 檔案管理權限
            options.AddPolicy("Permission_file:upload", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("file:upload")));

            options.AddPolicy("Permission_file:download", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("file:download", allowOwner: true, resourceType: "file")));

            options.AddPolicy("Permission_file:delete", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("file:delete", allowOwner: true, resourceType: "file")));

            options.AddPolicy("Permission_file:manage", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("file:manage")));
        }

        /// <summary>
        /// 建構稽核系統政策
        /// </summary>
        private static void BuildAuditPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("Permission_audit:read", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("audit:read")));

            options.AddPolicy("Permission_audit:export", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("audit:export")));

            options.AddPolicy("Permission_audit:admin", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("audit:admin")));

            options.AddPolicy("Permission_audit:create", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("audit:create")));

            options.AddPolicy("Permission_audit:cleanup", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("audit:cleanup")));

            options.AddPolicy("Permission_audit:report", policy =>
                policy.Requirements.Add(new PermissionAuthorizationRequirement("audit:report")));
        }

        /// <summary>
        /// 建構資源擁有者政策
        /// </summary>
        private static void BuildResourceOwnerPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("ResourceOwner_user", policy =>
                policy.Requirements.Add(new ResourceOwnerAuthorizationRequirement("user")));

            options.AddPolicy("ResourceOwner_project", policy =>
                policy.Requirements.Add(new ResourceOwnerAuthorizationRequirement("project")));

            options.AddPolicy("ResourceOwner_file", policy =>
                policy.Requirements.Add(new ResourceOwnerAuthorizationRequirement("file")));

            options.AddPolicy("ResourceOwner_person", policy =>
                policy.Requirements.Add(new ResourceOwnerAuthorizationRequirement("person")));
        }

        /// <summary>
        /// 建構角色階層政策
        /// </summary>
        private static void BuildRoleHierarchyPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("RoleHierarchy_SuperAdmin", policy =>
                policy.Requirements.Add(new RoleHierarchyAuthorizationRequirement(RoleConstants.SUPER_ADMIN, 100)));

            options.AddPolicy("RoleHierarchy_Admin", policy =>
                policy.Requirements.Add(new RoleHierarchyAuthorizationRequirement(RoleConstants.ADMIN, 80)));

            options.AddPolicy("RoleHierarchy_AuditReader", policy =>
                policy.Requirements.Add(new RoleHierarchyAuthorizationRequirement("AuditReader", 60)));

            options.AddPolicy("RoleHierarchy_User", policy =>
                policy.Requirements.Add(new RoleHierarchyAuthorizationRequirement(RoleConstants.USER, 40)));
        }

        /// <summary>
        /// 建構複合權限政策
        /// </summary>
        private static void BuildCompositePermissionPolicies(AuthorizationOptions options)
        {
            // 任一稽核權限
            options.AddPolicy("AnyPermission_audit:read_role:admin_role:super_admin", policy =>
                policy.Requirements.Add(new AnyPermissionAuthorizationRequirement(
                    new[] { "audit:read", "role:admin", "role:super_admin" })));

            // 使用者管理的任一權限
            options.AddPolicy("AnyPermission_user:create_user:update_user:delete", policy =>
                policy.Requirements.Add(new AnyPermissionAuthorizationRequirement(
                    new[] { "user:create", "user:update", "user:delete" })));

            // 專案的所有基本權限
            options.AddPolicy("AllPermissions_project:read_project:update", policy =>
                policy.Requirements.Add(new AllPermissionsAuthorizationRequirement(
                    new[] { "project:read", "project:update" })));

            // 檔案的完整管理權限
            options.AddPolicy("AllPermissions_file:upload_file:download_file:delete", policy =>
                policy.Requirements.Add(new AllPermissionsAuthorizationRequirement(
                    new[] { "file:upload", "file:download", "file:delete" })));
        }

        /// <summary>
        /// 動態建構權限政策
        /// </summary>
        public static void BuildDynamicPermissionPolicy(AuthorizationOptions options, string permission, bool allowOwner = false, string? resourceType = null)
        {
            var policyName = CreatePermissionPolicyName(permission, allowOwner, resourceType);
            
            if (!options.GetPolicy(policyName)?.Requirements.Any() == true)
            {
                options.AddPolicy(policyName, policy =>
                    policy.Requirements.Add(new PermissionAuthorizationRequirement(permission, allowOwner, resourceType)));
            }
        }

        /// <summary>
        /// 動態建構任一權限政策
        /// </summary>
        public static void BuildDynamicAnyPermissionPolicy(AuthorizationOptions options, string[] permissions)
        {
            var policyName = $"AnyPermission_{string.Join("_", permissions)}";
            
            if (!options.GetPolicy(policyName)?.Requirements.Any() == true)
            {
                options.AddPolicy(policyName, policy =>
                    policy.Requirements.Add(new AnyPermissionAuthorizationRequirement(permissions)));
            }
        }

        /// <summary>
        /// 建立權限政策名稱
        /// </summary>
        private static string CreatePermissionPolicyName(string permission, bool allowOwner, string? resourceType)
        {
            var parts = new List<string> { "Permission", permission.Replace(":", "_") };
            if (allowOwner) parts.Add("AllowOwner");
            if (!string.IsNullOrEmpty(resourceType)) parts.Add($"Resource_{resourceType}");
            return string.Join("_", parts);
        }
    }
}