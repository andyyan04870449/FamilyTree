using Serilog;
using Serilog.Events;
using familytree_backend.Services;
using familytree_backend.Constants;
using familytree_backend.Middleware;
using FamilyTree.Services;
using Microsoft.Extensions.Caching.Memory;
using familytree_backend.Extensions;

namespace familytree_backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

// 設定 ContentRoot 路徑
builder.Configuration["ContentRoot"] = builder.Environment.ContentRootPath;

// 建立配置服務實例以獲取日誌配置
var configurationService = new ConfigurationService(builder.Configuration, builder.Environment);
var loggingConfig = configurationService.GetLoggingConfiguration();
var environmentConfig = configurationService.GetEnvironmentConfiguration();

// 配置 Serilog 日誌記錄 - 使用統一的配置管理
var logDirectory = loggingConfig.LogDirectory;
if (!Directory.Exists(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}

// 根據環境設定日誌等級
var minimumLogLevel = environmentConfig.EnableDetailedLogging 
    ? LogEventLevel.Debug 
    : (environmentConfig.Name == "Production" ? LogEventLevel.Warning : LogEventLevel.Information);

// 統一的日誌格式模板
var consoleTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
var fileTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} | {RequestId} | {UserId} | {Message:lj}{NewLine}{Exception}";

// 建立 Serilog 配置
var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Is(minimumLogLevel)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FamilyTree")
    .Enrich.WithProperty("Environment", environmentConfig.Name)
    .Enrich.WithProperty("Version", "1.0.0");

// 根據環境添加不同的日誌接收器
if (environmentConfig.EnableDebugMode)
{
    loggerConfiguration.WriteTo.Console(
        outputTemplate: consoleTemplate,
        restrictedToMinimumLevel: LogEventLevel.Debug
    );
}
else
{
    loggerConfiguration.WriteTo.Console(
        outputTemplate: consoleTemplate,
        restrictedToMinimumLevel: LogEventLevel.Information
    );
}

// 檔案日誌配置
loggerConfiguration.WriteTo.File(
    Path.Combine(logDirectory, loggingConfig.LogFileNameFormat.Replace("{0}", "familytree")),
    rollingInterval: RollingInterval.Day,
    outputTemplate: fileTemplate,
    fileSizeLimitBytes: loggingConfig.MaxLogFileSizeMB * 1024 * 1024,
    retainedFileCountLimit: loggingConfig.MaxLogFiles,
    rollOnFileSizeLimit: true,
    shared: true,
    flushToDiskInterval: TimeSpan.FromSeconds(1),
    restrictedToMinimumLevel: LogEventLevel.Information
);

// 結構化日誌配置
if (loggingConfig.EnableStructuredLogging)
{
    loggerConfiguration.WriteTo.File(
        new Serilog.Formatting.Json.JsonFormatter(),
        Path.Combine(logDirectory, "familytree-structured-.json"),
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: loggingConfig.MaxLogFileSizeMB * 1024 * 1024,
        retainedFileCountLimit: loggingConfig.MaxLogFiles,
        rollOnFileSizeLimit: true,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1),
        restrictedToMinimumLevel: LogEventLevel.Information
    );
}

// 效能日誌配置
if (loggingConfig.EnablePerformanceLogging)
{
    loggerConfiguration.WriteTo.File(
        Path.Combine(logDirectory, "familytree-performance-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext} | {Duration}ms | {Message:lj}{NewLine}{Exception}",
        fileSizeLimitBytes: loggingConfig.MaxLogFileSizeMB * 1024 * 1024,
        retainedFileCountLimit: loggingConfig.MaxLogFiles,
        rollOnFileSizeLimit: true,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1),
        restrictedToMinimumLevel: LogEventLevel.Information
    );
}

// 錯誤日誌配置
loggerConfiguration.WriteTo.File(
    Path.Combine(logDirectory, "familytree-errors-.log"),
    rollingInterval: RollingInterval.Day,
    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} | {RequestId} | {UserId} | {Message:lj}{NewLine}{Exception}",
    fileSizeLimitBytes: loggingConfig.MaxLogFileSizeMB * 1024 * 1024,
    retainedFileCountLimit: loggingConfig.MaxLogFiles,
    rollOnFileSizeLimit: true,
    shared: true,
    flushToDiskInterval: TimeSpan.FromSeconds(1),
    restrictedToMinimumLevel: LogEventLevel.Error
);

// 建立日誌記錄器
Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// 安全的 JWT 密鑰配置
var jwtSecret = GetSecureJwtSecret(builder);
ValidateJwtSecret(jwtSecret);

// 註冊 JWT 設定
builder.Services.Configure<familytree_backend.Models.JwtSettings>(jwtConfig =>
{
    var jwtSection = builder.Configuration.GetSection("Jwt");
    jwtSection.Bind(jwtConfig);
    jwtConfig.Secret = jwtSecret; // 使用安全來源的密鑰覆蓋配置
});

// 註冊密鑰輪換設定
builder.Services.Configure<familytree_backend.Models.KeyRotationSettings>(
    builder.Configuration.GetSection("KeyRotation"));

// 加入 JWT 認證
builder.Services.AddJwtAuthentication(builder.Configuration, jwtSecret);

// 優化重點：註冊新的配置管理服務，移除硬編碼依賴
// 註冊配置管理服務 - 提供統一的配置存取介面
builder.Services.AddScoped<familytree_backend.Services.IConfigurationService, familytree_backend.Services.ConfigurationService>();

// Add AI Service (still used by other services)
builder.Services.AddSingleton<familytree_backend.Services.AIService>();

// Add Excel Processing Service
builder.Services.AddScoped<familytree_backend.Services.ExcelProcessingService>();

// Add File Upload Service
builder.Services.AddScoped<familytree_backend.Services.FileUploadService>();

// Add Photo Upload Service
builder.Services.AddScoped<familytree_backend.Services.PhotoUploadService>();

// Add Data Access Service - 統一資料庫操作
builder.Services.AddScoped<familytree_backend.Services.IDataAccessService, familytree_backend.Services.DataAccessService>();

// Add Data Access Service V2 - 基於 user_id 的資料隔離版本
builder.Services.AddScoped<familytree_backend.Services.DataAccessServiceV2>();
builder.Services.AddScoped<familytree_backend.Services.IDataAccessServiceV2>(provider =>
{
    var baseService = provider.GetRequiredService<familytree_backend.Services.DataAccessServiceV2>();
    var cache = provider.GetRequiredService<IMemoryCache>();
    var logger = provider.GetRequiredService<ILogger<familytree_backend.Services.CachedDataAccessServiceV2>>();
    return new familytree_backend.Services.CachedDataAccessServiceV2(baseService, cache, logger);
});

// Add Logging Services - 統一日誌管理
builder.Services.AddScoped<familytree_backend.Services.ILoggingService, familytree_backend.Services.LoggingService>();
builder.Services.AddScoped<familytree_backend.Services.ILogFilterService, familytree_backend.Services.LogFilterService>();
builder.Services.AddScoped<familytree_backend.Services.ILogFileManagementService, familytree_backend.Services.LogFileManagementService>();

// Add Security Services - 統一安全性管理
builder.Services.AddScoped<familytree_backend.Services.IValidationService, familytree_backend.Services.ValidationService>();
builder.Services.AddScoped<familytree_backend.Services.IAccessControlService, familytree_backend.Services.AccessControlService>();

// Add Performance Services - 統一效能優化管理
builder.Services.AddScoped<familytree_backend.Services.ICacheService, familytree_backend.Services.CacheService>();
builder.Services.AddScoped<familytree_backend.Services.IQueryOptimizationService, familytree_backend.Services.QueryOptimizationService>();
builder.Services.AddScoped<familytree_backend.Services.IMemoryManagementService, familytree_backend.Services.MemoryManagementService>();

// Add User Management Services - 使用者管理服務
builder.Services.AddScoped<familytree_backend.Services.IUserService, familytree_backend.Services.UserService>();
builder.Services.AddScoped<familytree_backend.Services.ITokenService, familytree_backend.Services.TokenService>();
builder.Services.AddScoped<familytree_backend.Services.IAuthService, familytree_backend.Services.AuthService>();

// Add Key Rotation Service - 密鑰輪換服務
builder.Services.AddScoped<familytree_backend.Services.IKeyRotationService, familytree_backend.Services.KeyRotationService>();

// Add Permission Service - 權限管理服務
builder.Services.AddScoped<FamilyTree.Services.IDatabaseHelper, FamilyTree.Services.DatabaseHelper>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddMemoryCache(); // 權限服務需要快取

// Add Database Initialization Service - 資料庫初始化服務
builder.Services.AddHostedService<familytree_backend.Services.DatabaseInitializationService>();

// Add Audit Log Services - 稽核日誌服務
builder.Services.AddAuditLogServices(builder.Configuration);

// Add CORS - Environment-specific configuration for security
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Development", policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:8080")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });
}
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Production", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "https://localhost:7173" }; // Fallback for safety
            
            policy.WithOrigins(allowedOrigins)
                  .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                  .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "Accept", "Origin")
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)); // 24 hours cache for preflight
        });
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Use Security Middleware - 安全性中介軟體 (暫時停用以避免依賴注入問題)
// app.UseMiddleware<familytree_backend.Middleware.SecurityMiddleware>();

// Use CORS - Environment-specific policy
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
}

// Use JWT Authentication
app.UseJwtAuthentication();

// Use Audit Log Middleware - 稽核日誌中介軟體
app.UseAuditLogMiddleware();

app.MapControllers();

try
{
    Log.Information("啟動 FamilyTree 後端服務 - 環境: {Environment}, 日誌等級: {LogLevel}", 
        environmentConfig.Name, minimumLogLevel);
    Log.Information("日誌目錄: {LogDirectory}, 最大檔案大小: {MaxFileSize}MB", 
        logDirectory, loggingConfig.MaxLogFileSizeMB);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "服務啟動失敗 - 環境: {Environment}", environmentConfig.Name);
}
finally
{
    Log.CloseAndFlush();
}
        }

        /// <summary>
        /// 從安全來源獲取 JWT 密鑰
        /// </summary>
        /// <param name="builder">WebApplication builder</param>
        /// <returns>JWT 密鑰</returns>
        private static string GetSecureJwtSecret(WebApplicationBuilder builder)
        {
            string? jwtSecret = null;

            if (builder.Environment.IsDevelopment())
            {
                // 開發環境：優先使用 User Secrets，然後是環境變數，最後是配置檔案
                jwtSecret = builder.Configuration["Jwt:Secret"]; // User Secrets 會自動覆蓋配置檔案
                
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
                }
            }
            else
            {
                // 生產環境：優先使用環境變數，然後是 Azure Key Vault（如果配置）
                jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
                
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    // Azure Key Vault 配置（如果有設定）
                    var keyVaultEndpoint = Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT");
                    if (!string.IsNullOrEmpty(keyVaultEndpoint))
                    {
                        try
                        {
                            // 注意：這裡需要 Azure.Extensions.AspNetCore.Configuration.Secrets 套件
                            // builder.Configuration.AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
                            // jwtSecret = builder.Configuration["JWT-SECRET"]; // Key Vault 中的密鑰名稱
                            Log.Information("Azure Key Vault endpoint configured: {Endpoint}", keyVaultEndpoint);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Failed to configure Azure Key Vault, falling back to environment variables");
                        }
                    }
                }
                
                // 最後回退到配置檔案（不建議在生產環境使用）
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    jwtSecret = builder.Configuration["Jwt:Secret"];
                    if (!string.IsNullOrEmpty(jwtSecret))
                    {
                        Log.Warning("使用配置檔案中的 JWT 密鑰，這在生產環境中不安全！");
                    }
                }
            }

            if (string.IsNullOrEmpty(jwtSecret))
            {
                var errorMessage = $"JWT Secret is not configured for environment: {builder.Environment.EnvironmentName}. " +
                    "Please set JWT_SECRET environment variable or configure User Secrets for development.";
                Log.Fatal(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return jwtSecret;
        }

        /// <summary>
        /// 驗證 JWT 密鑰強度
        /// </summary>
        /// <param name="jwtSecret">JWT 密鑰</param>
        private static void ValidateJwtSecret(string jwtSecret)
        {
            if (!familytree_backend.Services.JwtSecretValidator.ValidateSecret(jwtSecret))
            {
                var score = familytree_backend.Services.JwtSecretValidator.GetSecretStrengthScore(jwtSecret);
                var errorMessage = $"JWT Secret does not meet security requirements. Strength score: {score}/100. " +
                    "Secret must be at least 32 characters long and contain uppercase, lowercase, digits, and special characters.";
                
                Log.Fatal(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var strengthScore = familytree_backend.Services.JwtSecretValidator.GetSecretStrengthScore(jwtSecret);
            Log.Information("JWT Secret validation passed. Strength score: {Score}/100", strengthScore);
        }
    }
}
