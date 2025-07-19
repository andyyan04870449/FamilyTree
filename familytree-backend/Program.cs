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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add AI Service (used internally by AnalysisBackgroundService)
builder.Services.AddSingleton<familytree_backend.Services.AIService>();

// Add Analysis Background Service
builder.Services.AddSingleton<familytree_backend.Services.AnalysisBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<familytree_backend.Services.AnalysisBackgroundService>());

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
