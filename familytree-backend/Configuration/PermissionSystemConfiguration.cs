using Microsoft.AspNetCore.Authorization;
using FamilyTree.Authorization;
using FamilyTree.Services.Authorization;
using FamilyTree.Extensions;
using FamilyTree.Middleware;

namespace FamilyTree.Configuration
{
    /// <summary>
    /// 權限系統配置類
    /// 提供權限系統的完整配置方法
    /// </summary>
    public static class PermissionSystemConfiguration
    {
        /// <summary>
        /// 配置權限系統的所有服務
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服務集合</returns>
        public static IServiceCollection ConfigurePermissionSystem(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // 1. 添加基本的授權服務
            services.AddAuthorization();

            // 2. 添加HTTP上下文訪問器（用於權限上下文）
            services.AddHttpContextAccessor();

            // 3. 添加記憶體快取（用於權限快取）
            services.AddMemoryCache();

            // 4. 註冊權限策略為基礎的授權系統
            services.AddPermissionBasedAuthorization();

            // 5. 註冊混合權限策略（如果需要）
            // services.AddHybridPermissionAuthorization();

            // 6. 添加權限策略工廠
            services.AddPermissionStrategyFactory();

            // 7. 添加動態權限政策（如果配置檔案中有定義）
            if (configuration.GetSection("Authorization:DynamicPolicies").Exists())
            {
                services.AddDynamicPermissionPolicies(configuration);
            }

            // 8. 配置授權選項
            services.Configure<AuthorizationOptions>(options =>
            {
                // 配置預設策略
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // 配置後備策略
                options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                // 添加自定義政策
                FamilyTree.Authorization.AuthorizationPolicyBuilder.BuildPolicies(options);
            });

            return services;
        }

        /// <summary>
        /// 配置權限中介軟體
        /// </summary>
        /// <param name="app">應用程式建構器</param>
        /// <param name="env">環境</param>
        /// <returns>應用程式建構器</returns>
        public static IApplicationBuilder ConfigurePermissionMiddleware(
            this IApplicationBuilder app, 
            IWebHostEnvironment env)
        {
            // 開發環境中啟用詳細的授權日誌
            if (env.IsDevelopment())
            {
                app.UseMiddleware<AuthorizationLoggingMiddleware>();
            }

            // 使用認證
            app.UseAuthentication();

            // 使用權限檢查中介軟體
            app.UsePermissionMiddleware();

            // 使用動態權限檢查中介軟體（可選）
            // app.UseDynamicPermissionMiddleware();

            // 使用授權
            app.UseAuthorization();

            return app;
        }

        /// <summary>
        /// 驗證權限系統配置
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <returns>驗證結果</returns>
        public static bool ValidatePermissionSystemConfiguration(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            
            try
            {
                // 檢查必要的服務是否已註冊
                var permissionStrategy = serviceProvider.GetService<IPermissionStrategy>();
                var permissionContext = serviceProvider.GetService<IPermissionContext>();
                var authorizationService = serviceProvider.GetService<IAuthorizationService>();
                
                if (permissionStrategy == null)
                {
                    throw new InvalidOperationException("IPermissionStrategy 服務未註冊");
                }
                
                if (permissionContext == null)
                {
                    throw new InvalidOperationException("IPermissionContext 服務未註冊");
                }
                
                if (authorizationService == null)
                {
                    throw new InvalidOperationException("IAuthorizationService 服務未註冊");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"權限系統配置驗證失敗: {ex.Message}");
                return false;
            }
            finally
            {
                serviceProvider.Dispose();
            }
        }
    }

    /// <summary>
    /// 授權日誌中介軟體（開發用）
    /// </summary>
    public class AuthorizationLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationLoggingMiddleware> _logger;

        public AuthorizationLoggingMiddleware(RequestDelegate next, ILogger<AuthorizationLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;
            var method = context.Request.Method;
            var user = context.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var roles = string.Join(",", user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value));
                
                _logger.LogDebug("授權檢查: Method={Method}, Path={Path}, UserId={UserId}, Roles={Roles}", 
                    method, path, userId, roles);
            }

            await _next(context);

            // 記錄授權結果
            if (context.Response.StatusCode == 403)
            {
                _logger.LogWarning("授權失敗: Method={Method}, Path={Path}, StatusCode={StatusCode}", 
                    method, path, context.Response.StatusCode);
            }
        }
    }

    /// <summary>
    /// 權限系統擴展方法
    /// </summary>
    public static class PermissionSystemExtensions
    {
        /// <summary>
        /// 添加完整的權限系統配置
        /// </summary>
        /// <param name="services">服務集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服務集合</returns>
        public static IServiceCollection AddFamilyTreePermissionSystem(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            return services.ConfigurePermissionSystem(configuration);
        }

        /// <summary>
        /// 使用完整的權限系統中介軟體
        /// </summary>
        /// <param name="app">應用程式建構器</param>
        /// <param name="env">環境</param>
        /// <returns>應用程式建構器</returns>
        public static IApplicationBuilder UseFamilyTreePermissionSystem(
            this IApplicationBuilder app, 
            IWebHostEnvironment env)
        {
            return app.ConfigurePermissionMiddleware(env);
        }
    }
}