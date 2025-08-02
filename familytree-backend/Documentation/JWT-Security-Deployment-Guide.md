# JWT 密鑰管理安全部署指南

## 概述

本指南說明如何在不同環境中安全地配置 JWT 密鑰，包括開發、測試和生產環境。

## 安全要求

### JWT 密鑰強度要求
- **最小長度**: 32 字符（256 位）
- **推薦長度**: 64 字符或更長
- **複雜度要求**:
  - 必須包含大寫字母
  - 必須包含小寫字母
  - 必須包含數字
  - 必須包含特殊字符

### 密鑰評分標準
- 長度評分（最多 40 分）：
  - 32+ 字符：20 分
  - 48+ 字符：30 分
  - 64+ 字符：40 分
- 複雜度評分（60 分）：
  - 大寫字母：15 分
  - 小寫字母：15 分
  - 數字：15 分
  - 特殊字符：15 分

**最低接受分數**: 100 分（滿分）

## 環境配置

### 開發環境 (Development)

#### 使用 User Secrets（推薦）

1. **初始化 User Secrets**：
   ```bash
   dotnet user-secrets init
   ```

2. **設置 JWT 密鑰**：
   ```bash
   dotnet user-secrets set "Jwt:Secret" "D3v3l0pm3ntS3cr3tK3yF0rJWTT0k3nGen3r4t10n2024!@#$%^&*()"
   ```

3. **查看設置的密鑰**：
   ```bash
   dotnet user-secrets list
   ```

4. **移除密鑰**（如果需要）：
   ```bash
   dotnet user-secrets remove "Jwt:Secret"
   ```

#### 使用環境變數（備選）

在開發機器上設置環境變數：

**Windows**:
```cmd
set JWT_SECRET=D3v3l0pm3ntS3cr3tK3yF0rJWTT0k3nGen3r4t10n2024!@#$%^&*()
```

**macOS/Linux**:
```bash
export JWT_SECRET="D3v3l0pm3ntS3cr3tK3yF0rJWTT0k3nGen3r4t10n2024!@#$%^&*()"
```

### 測試環境 (Testing)

使用環境變數配置，避免在配置檔案中儲存敏感資訊：

```bash
export JWT_SECRET="T3st1ngS3cr3tK3yF0rJWTT0k3nGen3r4t10n2024!@#$%^&*()"
export ASPNETCORE_ENVIRONMENT="Testing"
```

### 生產環境 (Production)

#### 方法 1: 環境變數（推薦）

1. **在伺服器上設置環境變數**：
   ```bash
   export JWT_SECRET="Pr0duct10nS3cr3tK3yF0rJWTT0k3nGen3r4t10n2024!@#$%^&*()"
   export ASPNETCORE_ENVIRONMENT="Production"
   ```

2. **在 Docker 容器中**：
   ```dockerfile
   ENV JWT_SECRET="Pr0duct10nS3cr3tK3yF0rJWTT0k3nGen3r4t10n2024!@#$%^&*()"
   ENV ASPNETCORE_ENVIRONMENT="Production"
   ```

3. **在 Kubernetes 中使用 Secret**：
   ```yaml
   apiVersion: v1
   kind: Secret
   metadata:
     name: familytree-jwt-secret
   type: Opaque
   data:
     JWT_SECRET: UHIwZHVjdDEwblMzY3IzdEs... # Base64 編碼的密鑰
   ```

#### 方法 2: Azure Key Vault（企業級推薦）

1. **建立 Azure Key Vault**：
   ```bash
   az keyvault create --name "familytree-keyvault" --resource-group "familytree-rg" --location "East Asia"
   ```

2. **設置密鑰**：
   ```bash
   az keyvault secret set --vault-name "familytree-keyvault" --name "JWT-SECRET" --value "Pr0duct10nS3cr3tK3yF0rJWTT0k3nGen3r4t10n2024!@#$%^&*()"
   ```

3. **配置環境變數**：
   ```bash
   export AZURE_KEY_VAULT_ENDPOINT="https://familytree-keyvault.vault.azure.net/"
   ```

4. **安裝必要的 NuGet 套件**：
   ```bash
   dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
   dotnet add package Azure.Identity
   ```

## 密鑰生成

### 使用內建生成器

系統提供了內建的安全密鑰生成器：

```csharp
// 生成 64 字符的安全密鑰
string secureSecret = JwtSecretValidator.GenerateSecureSecret(64);
```

### 使用命令列工具

**PowerShell**:
```powershell
Add-Type -AssemblyName System.Web
[System.Web.Security.Membership]::GeneratePassword(64, 20)
```

**Linux/macOS**:
```bash
openssl rand -base64 48 | tr -d "=" | tr "/" "_" | tr "+" "-"
```

## 密鑰輪換

### 自動輪換配置

在 `appsettings.json` 中配置密鑰輪換：

```json
{
  "KeyRotation": {
    "EnableAutoRotation": true,
    "KeyValidityDays": 30,
    "RotationAdvanceDays": 7,
    "OldKeyRetentionDays": 2,
    "MinKeyLength": 64,
    "CheckIntervalMinutes": 60
  }
}
```

### 手動輪換流程

1. **生成新密鑰**
2. **更新環境變數或 Key Vault**
3. **重啟應用程式**
4. **監控舊 token 失效情況**
5. **清理舊密鑰**

## 安全檢查清單

### 部署前檢查

- [ ] 確認密鑰強度評分達到 100 分
- [ ] 確認密鑰未包含在版本控制系統中
- [ ] 確認生產環境使用環境變數或 Key Vault
- [ ] 確認不同環境使用不同的密鑰
- [ ] 確認日誌中不會記錄敏感資訊

### 運行時監控

- [ ] 監控 JWT 驗證失敗率
- [ ] 監控密鑰輪換日誌
- [ ] 監控安全警告和錯誤
- [ ] 定期檢查密鑰過期時間

## 故障排除

### 常見錯誤

1. **JWT Secret is not configured**
   - 檢查環境變數是否正確設置
   - 檢查 User Secrets 是否正確配置

2. **JWT Secret does not meet security requirements**
   - 確保密鑰長度至少 32 字符
   - 確保密鑰包含所有必要的字符類型

3. **Token validation failed**
   - 檢查密鑰是否在中間件和服務中一致
   - 檢查是否在密鑰輪換期間發生問題

### 診斷命令

檢查應用程式配置：
```bash
# 檢查環境變數
echo $JWT_SECRET

# 檢查 User Secrets
dotnet user-secrets list

# 測試密鑰強度
# （需要實作密鑰強度檢查工具）
```

## 災難恢復

### 密鑰洩露處理

1. **立即輪換所有受影響的密鑰**
2. **撤銷所有現有的 JWT token**
3. **強制所有使用者重新登入**
4. **檢查安全日誌以確定洩露範圍**
5. **更新安全政策和流程**

### 備份和恢復

- 定期備份 Key Vault 配置
- 準備應急密鑰輪換程序
- 維護密鑰輪換的操作文檔

## 相關文檔

- [JWT 最佳實踐指南](https://tools.ietf.org/html/rfc7519)
- [Azure Key Vault 文檔](https://docs.microsoft.com/azure/key-vault/)
- [ASP.NET Core User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets)