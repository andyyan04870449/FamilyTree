using Microsoft.AspNetCore.Authorization;
using FamilyTree.Authorization;
using FamilyTree.Services.Authorization;

namespace FamilyTree.Extensions
{
    /// <summary>
    /// 授權服務擴展方法
    /// </summary>
    public static class AuthorizationServiceExtensions
    {
        /// <summary>
        /// 註冊權限策略為基礎的授權服務
        /// </summary>
        public static IServiceCollection AddPermissionBasedAuthorization(this IServiceCollection services)
        {
            // 註冊權限策略
            services.AddScoped<IPermissionStrategy, RoleBasedPermissionStrategy>();
            services.AddScoped<PolicyBasedPermissionStrategy>();
            
            // 註冊權限上下文
            services.AddScoped<IPermissionContext, PermissionContext>();
            
            // 註冊授權處理器
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, AllPermissionsAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, ResourceOwnerAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, RoleHierarchyAuthorizationHandler>();

            // 配置授權政策
            services.AddAuthorization(options =>
            {
                FamilyTree.Authorization.AuthorizationPolicyBuilder.BuildPolicies(options);
            });

            return services;
        }

        /// <summary>
        /// 使用混合權限策略（角色為基礎 + 政策為基礎）
        /// </summary>
        public static IServiceCollection AddHybridPermissionAuthorization(this IServiceCollection services)
        {
            // 註冊多個策略
            services.AddScoped<RoleBasedPermissionStrategy>();
            services.AddScoped<PolicyBasedPermissionStrategy>();
            
            // 使用工廠模式選擇策略
            services.AddScoped<IPermissionStrategy>(provider =>
            {
                // 這裡可以根據配置或其他條件選擇策略
                // 預設使用角色為基礎的策略
                return provider.GetRequiredService<RoleBasedPermissionStrategy>();
            });
            
            // 註冊權限上下文
            services.AddScoped<IPermissionContext>(provider =>
            {
                var strategy = provider.GetRequiredService<IPermissionStrategy>();
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
                var user = httpContextAccessor.HttpContext?.User;
                return new PermissionContext(strategy, user);
            });
            
            // 註冊授權處理器
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, AnyPermissionAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, AllPermissionsAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, ResourceOwnerAuthorizationHandler>();
            services.AddScoped<IAuthorizationHandler, RoleHierarchyAuthorizationHandler>();

            // 配置授權政策
            services.AddAuthorization(options =>
            {
                FamilyTree.Authorization.AuthorizationPolicyBuilder.BuildPolicies(options);
            });

            return services;
        }

        /// <summary>
        /// 添加權限策略工廠
        /// </summary>
        public static IServiceCollection AddPermissionStrategyFactory(this IServiceCollection services)
        {
            services.AddScoped<IPermissionStrategyFactory, PermissionStrategyFactory>();
            return services;
        }

        /// <summary>
        /// 動態註冊權限政策
        /// </summary>
        public static IServiceCollection AddDynamicPermissionPolicies(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            services.Configure<AuthorizationOptions>(options =>
            {
                // 從配置檔案讀取動態權限政策
                var permissionPolicies = configuration.GetSection("Authorization:DynamicPolicies");
                
                foreach (var policy in permissionPolicies.GetChildren())
                {
                    var permission = policy["Permission"];
                    var allowOwner = policy.GetValue<bool>("AllowOwner");
                    var resourceType = policy["ResourceType"];
                    
                    if (!string.IsNullOrEmpty(permission))
                    {
                        FamilyTree.Authorization.AuthorizationPolicyBuilder.BuildDynamicPermissionPolicy(options, permission, allowOwner, resourceType);
                    }
                }
            });

            return services;
        }
    }

    /// <summary>
    /// 權限策略工廠介面
    /// </summary>
    public interface IPermissionStrategyFactory
    {
        IPermissionStrategy CreateStrategy(string strategyType);
        IPermissionStrategy GetDefaultStrategy();
    }

    /// <summary>
    /// 權限策略工廠實現
    /// </summary>
    public class PermissionStrategyFactory : IPermissionStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PermissionStrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPermissionStrategy CreateStrategy(string strategyType)
        {
            return strategyType.ToLower() switch
            {
                "role" => _serviceProvider.GetRequiredService<RoleBasedPermissionStrategy>(),
                "policy" => _serviceProvider.GetRequiredService<PolicyBasedPermissionStrategy>(),
                _ => GetDefaultStrategy()
            };
        }

        public IPermissionStrategy GetDefaultStrategy()
        {
            return _serviceProvider.GetRequiredService<RoleBasedPermissionStrategy>();
        }
    }
}