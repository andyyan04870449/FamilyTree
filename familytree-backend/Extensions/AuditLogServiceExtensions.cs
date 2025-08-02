using familytree_backend.Controllers;
using familytree_backend.Services;

namespace familytree_backend.Extensions
{
    /// <summary>
    /// 審計日誌服務擴展方法
    /// </summary>
    public static class AuditLogServiceExtensions
    {
        /// <summary>
        /// 註冊審計日誌相關服務
        /// </summary>
        public static IServiceCollection AddAuditLogServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 註冊審計日誌服務
            services.AddScoped<IAuditLogService, AuditLogService>();
            
            // 註冊日誌服務（如果還沒有實作，先使用簡單的實作）
            services.AddScoped<ILoggingService, LoggingService>();
            
            return services;
        }

        /// <summary>
        /// 使用審計日誌中介軟體
        /// </summary>
        public static IApplicationBuilder UseAuditLogMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuditLogMiddleware>();
        }
    }
}