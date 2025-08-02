# 工單 #002：JWT 密鑰管理改進

## 工單資訊
- **工單編號**：TASK-002
- **優先級**：🔴 緊急
- **預估時間**：6 小時
- **負責 Agent**：dotnet-backend-api-developer
- **創建日期**：2025-08-02
- **狀態**：待處理

## 問題描述
JWT 密鑰當前硬編碼在 appsettings.json 中，這存在以下安全風險：
- 密鑰可能被提交到版本控制系統
- 無法實現密鑰輪換
- 不同環境使用相同密鑰
- 密鑰強度不足

## 當前代碼
```json
// appsettings.json
{
  "Jwt": {
    "Secret": "your-256-bit-secret-key-for-jwt-token",
    "Issuer": "FamilyTreeAPI",
    "Audience": "FamilyTreeClient",
    "ExpirationMinutes": 1440
  }
}
```

## 修復方案

### 1. 實施 User Secrets（開發環境）
```bash
# 初始化 User Secrets
dotnet user-secrets init

# 設置 JWT 密鑰
dotnet user-secrets set "Jwt:Secret" "your-development-secret-key-at-least-32-characters-long"
```

### 2. 使用環境變數（生產環境）
```csharp
// Program.cs
var jwtSecret = builder.Environment.IsDevelopment() 
    ? builder.Configuration["Jwt:Secret"] 
    : Environment.GetEnvironmentVariable("JWT_SECRET");

if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT Secret is not configured");
}
```

### 3. 實施密鑰強度驗證
```csharp
// Services/TokenService.cs
public class JwtSecretValidator
{
    public static bool ValidateSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret))
            return false;
            
        // 至少 32 字符（256 位）
        if (secret.Length < 32)
            return false;
            
        // 檢查複雜度
        var hasUpper = secret.Any(char.IsUpper);
        var hasLower = secret.Any(char.IsLower);
        var hasDigit = secret.Any(char.IsDigit);
        var hasSpecial = secret.Any(ch => !char.IsLetterOrDigit(ch));
        
        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
```

### 4. 實施密鑰輪換機制
```csharp
// Models/JwtKeyRotation.cs
public class JwtKeyRotation
{
    public string CurrentKeyId { get; set; }
    public string CurrentKey { get; set; }
    public DateTime CurrentKeyExpiry { get; set; }
    
    public string? NextKeyId { get; set; }
    public string? NextKey { get; set; }
    public DateTime? NextKeyActivation { get; set; }
}

// Services/KeyRotationService.cs
public interface IKeyRotationService
{
    Task<JwtKeyRotation> GetCurrentKeys();
    Task RotateKeys();
    bool ShouldRotate();
}
```

### 5. 更新 JWT 中間件配置
```csharp
// Middleware/JwtAuthenticationExtensions.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                // 支援多個密鑰用於輪換
                var keyRotationService = services.BuildServiceProvider()
                    .GetRequiredService<IKeyRotationService>();
                var keys = keyRotationService.GetCurrentKeys().Result;
                
                var signingKeys = new List<SecurityKey>();
                signingKeys.Add(new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(keys.CurrentKey)));
                    
                if (!string.IsNullOrEmpty(keys.NextKey))
                {
                    signingKeys.Add(new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(keys.NextKey)));
                }
                
                return signingKeys;
            },
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### 6. Azure Key Vault 整合（推薦用於生產）
```csharp
// Program.cs
if (builder.Environment.IsProduction())
{
    var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_ENDPOINT"));
    builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());
}
```

### 7. 配置文件清理
```json
// appsettings.json (移除密鑰)
{
  "Jwt": {
    "Issuer": "FamilyTreeAPI",
    "Audience": "FamilyTreeClient",
    "ExpirationMinutes": 1440,
    "RefreshExpirationDays": 7
  }
}
```

## 測試要求
1. 密鑰強度測試
   - 弱密鑰應被拒絕
   - 強密鑰應被接受

2. 密鑰輪換測試
   - 新舊密鑰都能驗證 token
   - 過期密鑰被拒絕

3. 環境隔離測試
   - 開發環境使用 User Secrets
   - 生產環境使用環境變數

## 驗收標準
- [ ] 密鑰從配置文件中移除
- [ ] User Secrets 配置完成
- [ ] 環境變數配置文檔
- [ ] 密鑰強度驗證實施
- [ ] 密鑰輪換機制就緒
- [ ] 所有測試通過
- [ ] 部署指南更新

## 遷移步驟
1. 部署新代碼（但保留舊配置兼容性）
2. 設置環境變數
3. 測試新配置
4. 移除舊配置
5. 監控 token 驗證錯誤

## 注意事項
1. 需要協調所有環境的密鑰更新
2. 確保現有 token 在過渡期間仍然有效
3. 準備回滾計劃
4. 更新 CI/CD 配置