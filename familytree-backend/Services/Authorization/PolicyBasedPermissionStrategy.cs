using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace FamilyTree.Services.Authorization
{
    /// <summary>
    /// 政策為基礎的權限策略實現
    /// 使用 ASP.NET Core 的 IAuthorizationService
    /// </summary>
    public class PolicyBasedPermissionStrategy : IPermissionStrategy
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly ILogger<PolicyBasedPermissionStrategy> _logger;

        public PolicyBasedPermissionStrategy(
            IAuthorizationService authorizationService,
            ILogger<PolicyBasedPermissionStrategy> logger)
        {
            _authorizationService = authorizationService;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission, object? resource = null)
        {
            try
            {
                var authResult = await _authorizationService.AuthorizeAsync(user, resource, permission);
                return authResult.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查政策權限時發生錯誤: Permission={Permission}", permission);
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
                var resource = new ResourceInfo(resourceType, resourceId);
                var authResult = await _authorizationService.AuthorizeAsync(user, resource, "ResourceOwner");
                return authResult.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "檢查資源擁有權時發生錯誤: ResourceType={ResourceType}, ResourceId={ResourceId}", 
                    resourceType, resourceId);
                return false;
            }
        }
    }
}