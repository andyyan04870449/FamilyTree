using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// 設定 ContentRoot 路徑
builder.Configuration["ContentRoot"] = builder.Environment.ContentRootPath;

// 配置 Serilog 日誌記錄
var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
if (!Directory.Exists(logDirectory))
{
    Directory.CreateDirectory(logDirectory);
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        Path.Combine(logDirectory, "familytree-analysis-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
        retainedFileCountLimit: 10,
        rollOnFileSizeLimit: true,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1)
    )
    .CreateLogger();

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

// Use CORS
app.UseCors("AllowAll");

app.MapControllers();

try
{
    Log.Information("啟動 FamilyTree 後端服務...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "服務啟動失敗");
}
finally
{
    Log.CloseAndFlush();
}
