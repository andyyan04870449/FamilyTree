using Serilog;
using Serilog.Events;
using familytree_backend.Services;
using familytree_backend.Constants;

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

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Use Security Middleware - 安全性中介軟體 (暫時停用以避免依賴注入問題)
// app.UseMiddleware<familytree_backend.Middleware.SecurityMiddleware>();

// Use CORS
app.UseCors("AllowAll");

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
    }
}
