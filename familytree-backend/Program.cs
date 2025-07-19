var builder = WebApplication.CreateBuilder(args);

// 設定 ContentRoot 路徑
builder.Configuration["ContentRoot"] = builder.Environment.ContentRootPath;

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

app.Run();
