using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using familytree_backend.Services;

namespace familytree_backend.Middleware
{
    /// <summary>
    /// JWT 認證中介軟體
    /// </summary>
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;

        public JwtAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<JwtAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // JWT 認證由 ASP.NET Core 的認證中介軟體處理
            // 這裡可以加入額外的處理邏輯
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT Authentication Middleware error");
                throw;
            }
        }
    }

    /// <summary>
    /// JWT 認證設定擴充方法
    /// </summary>
    public static class JwtAuthenticationExtensions
    {
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, 
            IConfiguration configuration,
            string jwtSecret)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !configuration.GetValue<bool>("Environment:EnableDebugMode", false);
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    // 支援密鑰輪換的簽名密鑰解析器
                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        var signingKeys = new List<SecurityKey>();
                        
                        // 主要密鑰
                        signingKeys.Add(new SymmetricSecurityKey(key));
                        
                        // TODO: 未來支援密鑰輪換時，可以從 KeyRotationService 獲取所有有效密鑰
                        // 目前先使用單一密鑰，避免服務提供者循環依賴問題
                        
                        return signingKeys;
                    },
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    // 設定密鑰解析失敗時的處理
                    TryAllIssuerSigningKeys = true
                };

                // 自訂事件處理
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<JwtAuthenticationMiddleware>>();
                        
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                            logger?.LogWarning("JWT token expired for request: {Path}", context.Request.Path);
                        }
                        else if (context.Exception.GetType() == typeof(SecurityTokenInvalidSignatureException))
                        {
                            context.Response.Headers["Token-Invalid"] = "true";
                            logger?.LogWarning("JWT token has invalid signature for request: {Path}", context.Request.Path);
                        }
                        else
                        {
                            logger?.LogWarning(context.Exception, "JWT authentication failed for request: {Path}", context.Request.Path);
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // 可以在這裡加入額外的驗證邏輯
                        var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            // 檢查使用者是否仍然有效
                            context.HttpContext.Items["UserId"] = userId;
                            
                            // 可以加入更多驗證邏輯，例如檢查使用者狀態
                            var userRole = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                            context.HttpContext.Items["UserRole"] = userRole;
                        }
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetService<ILogger<JwtAuthenticationMiddleware>>();
                        
                        logger?.LogWarning("JWT authentication challenge for request: {Path}", context.Request.Path);
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }
    }
}