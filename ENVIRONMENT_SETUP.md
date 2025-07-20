# FamilyTree 環境變數設定說明

## 概述

FamilyTree 系統需要設定環境變數來正確運行，特別是 OpenAI API Key。本文檔說明如何設定和管理環境變數。

## 快速開始

### 方法 1：使用整合啟動腳本（推薦）

```bash
# 直接啟動後端（包含環境變數設定）
./start-backend-with-env.sh
```

### 方法 2：手動設定環境變數

```bash
# 設定環境變數
source set-env.sh

# 然後啟動後端
./start-backend.sh
```

### 方法 3：使用載入腳本

```bash
# 載入 .env 檔案（如果存在）
source load-env.sh

# 然後啟動後端
./start-backend.sh
```

## 環境變數說明

### 必需的環境變數

| 變數名稱 | 說明 | 範例值 |
|---------|------|--------|
| `OPENAI_API_KEY` | OpenAI API 金鑰 | `sk-proj-...` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core 環境 | `Development` |
| `ASPNETCORE_URLS` | 應用程式 URL | `http://localhost:5087` |

### 可選的環境變數

| 變數名稱 | 說明 | 預設值 |
|---------|------|--------|
| `LOG_LEVEL` | 日誌等級 | `Information` |

## 腳本說明

### 1. `set-env.sh`
- **用途**：直接設定環境變數
- **使用方式**：`source set-env.sh`
- **特點**：包含完整的 API Key，適合開發環境

### 2. `load-env.sh`
- **用途**：從 `.env` 檔案載入環境變數
- **使用方式**：`source load-env.sh`
- **特點**：需要先創建 `.env` 檔案

### 3. `start-backend-with-env.sh`
- **用途**：整合環境變數設定和後端啟動
- **使用方式**：`./start-backend-with-env.sh`
- **特點**：一鍵啟動，最方便

## 安全注意事項

### 1. API Key 保護
- ✅ 使用環境變數而不是硬編碼
- ✅ 不要將 API Key 提交到版本控制
- ✅ 定期輪換 API Key

### 2. 檔案權限
- ✅ 確保腳本有執行權限：`chmod +x *.sh`
- ✅ 限制 `.env` 檔案權限：`chmod 600 .env`

### 3. 版本控制
- ✅ 將 `.env` 加入 `.gitignore`
- ✅ 提供 `.env.example` 作為範本

## 故障排除

### 常見問題

1. **環境變數未生效**
   ```bash
   # 檢查環境變數
   echo $OPENAI_API_KEY
   
   # 重新載入
   source set-env.sh
   ```

2. **腳本權限錯誤**
   ```bash
   # 設定執行權限
   chmod +x *.sh
   ```

3. **API Key 無效**
   - 檢查 API Key 是否正確
   - 確認 API Key 有足夠額度
   - 檢查網路連接

### 驗證設定

啟動應用程式後，檢查日誌輸出：

- **成功**：`✅ OpenAI 客戶端已成功初始化`
- **失敗**：`❌ OpenAI API Key 未配置或為預設值`

## 生產環境設定

### 1. 使用環境變數
```bash
export OPENAI_API_KEY="your-production-api-key"
export ASPNETCORE_ENVIRONMENT="Production"
```

### 2. 使用 Docker
```bash
docker run -e OPENAI_API_KEY="your-api-key" your-app
```

### 3. 使用 Kubernetes
```yaml
env:
- name: OPENAI_API_KEY
  valueFrom:
    secretKeyRef:
      name: openai-secret
      key: api-key
```

## 開發工作流程

### 日常開發
1. 使用 `./start-backend-with-env.sh` 啟動後端
2. 使用 `./start-frontend.sh` 啟動前端
3. 或者使用 `./start-all.sh` 同時啟動

### 環境切換
```bash
# 開發環境
source set-env.sh

# 測試環境
export ASPNETCORE_ENVIRONMENT="Staging"

# 生產環境
export ASPNETCORE_ENVIRONMENT="Production"
```

## 相關檔案

- `set-env.sh` - 環境變數設定腳本
- `load-env.sh` - 環境變數載入腳本
- `start-backend-with-env.sh` - 整合啟動腳本
- `appsettings.Development.json` - 開發環境配置
- `familytree-backend/Services/AIService.cs` - AI 服務實現 