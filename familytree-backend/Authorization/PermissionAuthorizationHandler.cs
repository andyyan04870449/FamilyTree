using Microsoft.AspNetCore.Authorization;
using FamilyTree.Services.Authorization;
using FamilyTree.Constants;

namespace FamilyTree.Authorization
{
    /// <summary>
    /// 權限授權處理器
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
    {
        private readonly IPermissionStrategy _permissionStrategy;

        public PermissionAuthorizationHandler(IPermissionStrategy permissionStrategy)
        {
            _permissionStrategy = permissionStrategy;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionAuthorizationRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                context.Fail();
                return;
            }

            // 準備資源資訊
            object? resource = null;
            if (requirement.AllowOwner && !string.IsNullOrEmpty(requirement.ResourceType) && context.Resource != null)
            {
                resource = context.Resource;
            }

            // 檢查權限
            var hasPermission = await _permissionStrategy.HasPermissionAsync(
                context.User, 
                requirement.Permission, 
                resource);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }

    /// <summary>
    /// 任一權限授權處理器
    /// </summary>
    public class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionAuthorizationRequirement>
    {
        private readonly IPermissionStrategy _permissionStrategy;

        public AnyPermissionAuthorizationHandler(IPermissionStrategy permissionStrategy)
        {
            _permissionStrategy = permissionStrategy;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AnyPermissionAuthorizationRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                context.Fail();
                return;
            }

            object? resource = null;
            if (requirement.AllowOwner && !string.IsNullOrEmpty(requirement.ResourceType) && context.Resource != null)
            {
                resource = context.Resource;
            }

            var hasAnyPermission = await _permissionStrategy.HasAnyPermissionAsync(
                context.User, 
                requirement.Permissions, 
                resource);

            if (hasAnyPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }

    /// <summary>
    /// 所有權限授權處理器
    /// </summary>
    public class AllPermissionsAuthorizationHandler : AuthorizationHandler<AllPermissionsAuthorizationRequirement>
    {
        private readonly IPermissionStrategy _permissionStrategy;

        public AllPermissionsAuthorizationHandler(IPermissionStrategy permissionStrategy)
        {
            _permissionStrategy = permissionStrategy;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AllPermissionsAuthorizationRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                context.Fail();
                return;
            }

            object? resource = null;
            if (requirement.AllowOwner && !string.IsNullOrEmpty(requirement.ResourceType) && context.Resource != null)
            {
                resource = context.Resource;
            }

            var hasAllPermissions = await _permissionStrategy.HasAllPermissionsAsync(
                context.User, 
                requirement.Permissions, 
                resource);

            if (hasAllPermissions)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }

    /// <summary>
    /// 資源擁有者授權處理器
    /// </summary>
    public class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerAuthorizationRequirement>
    {
        private readonly IPermissionStrategy _permissionStrategy;

        public ResourceOwnerAuthorizationHandler(IPermissionStrategy permissionStrategy)
        {
            _permissionStrategy = permissionStrategy;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnerAuthorizationRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                context.Fail();
                return;
            }

            if (context.Resource is not ResourceInfo resourceInfo)
            {
                context.Fail();
                return;
            }

            var isOwner = await _permissionStrategy.IsResourceOwnerAsync(
                context.User, 
                resourceInfo.Type, 
                resourceInfo.Id);

            if (isOwner)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }

    /// <summary>
    /// 角色階層授權處理器
    /// </summary>
    public class RoleHierarchyAuthorizationHandler : AuthorizationHandler<RoleHierarchyAuthorizationRequirement>
    {
        // 角色階層定義
        private static readonly Dictionary<string, int> RoleHierarchy = new()
        {
            [RoleConstants.SUPER_ADMIN] = 100,
            [RoleConstants.ADMIN] = 80,
            ["AuditReader"] = 60,
            [RoleConstants.USER] = 40,
            ["Guest"] = 20
        };

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleHierarchyAuthorizationRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated == true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var userRoles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            var userMaxLevel = userRoles
                .Select(role => RoleHierarchy.GetValueOrDefault(role, 0))
                .DefaultIfEmpty(0)
                .Max();

            if (userMaxLevel >= requirement.MinimumLevel)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}