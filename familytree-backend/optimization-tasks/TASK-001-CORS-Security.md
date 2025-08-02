# 工單 #001：CORS 配置安全加固

## 工單資訊
- **工單編號**：TASK-001
- **優先級**：🔴 緊急
- **預估時間**：4 小時
- **負責 Agent**：dotnet-backend-api-developer
- **創建日期**：2025-08-02
- **狀態**：待處理

## 問題描述
當前 CORS 配置過於寬鬆，允許所有來源的請求，這是一個嚴重的安全風險。在生產環境中，這可能導致：
- CSRF 攻擊
- 未授權的 API 訪問
- 資料洩露風險

## 當前代碼
```csharp
// Program.cs (Line 30-36)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

## 修復方案

### 1. 開發環境配置
```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Development", builder =>
        {
            builder.WithOrigins("http://localhost:4200", "http://localhost:3000")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
    });
}
```

### 2. 生產環境配置
```csharp
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Production", builder =>
        {
            var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            builder.WithOrigins(allowedOrigins)
                   .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                   .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                   .AllowCredentials()
                   .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)); // 24 hours
        });
    });
}
```

### 3. 配置文件更新
```json
// appsettings.Production.json
{
  "Cors": {
    "AllowedOrigins": [
      "https://familytree.yourdomain.com",
      "https://app.familytree.yourdomain.com"
    ]
  }
}
```

### 4. 中間件更新
```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
}
```

## 測試要求
1. 開發環境測試
   - 確認 localhost:4200 可以正常訪問
   - 確認其他來源被拒絕

2. 生產環境測試
   - 確認配置的域名可以訪問
   - 確認未配置的域名被拒絕
   - 測試 preflight 請求

3. 錯誤場景測試
   - 測試無 Origin header 的請求
   - 測試錯誤的 Origin
   - 測試不同的 HTTP 方法

## 驗收標準
- [ ] CORS 配置根據環境正確設置
- [ ] 生產環境只允許白名單域名
- [ ] 預檢請求正確處理
- [ ] 所有 API 端點都受到保護
- [ ] 單元測試覆蓋 CORS 邏輯
- [ ] 文檔更新完成

## 相關文件
- [MDN CORS Documentation](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS)
- [ASP.NET Core CORS](https://docs.microsoft.com/en-us/aspnet/core/security/cors)

## 注意事項
1. 更改 CORS 配置可能影響現有前端應用
2. 需要與前端團隊協調測試
3. 部署時需要逐步進行，避免服務中斷