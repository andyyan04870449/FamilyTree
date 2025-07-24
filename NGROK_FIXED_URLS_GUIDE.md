# ngrok 固定網址使用指南

## 概述
此指南說明如何使用 ngrok 付費版的固定網址功能，讓您的公開網址在每次重啟後保持不變。

## 固定網址設定

### 當前配置
- **前端固定網址**: `https://KUNYOU-POC-frontend.ngrok.io`
- **後端固定網址**: `https://KUNYOU-POC-backend.ngrok.io`

### 修改固定網址
如果您想要修改固定網址，請編輯 `ngrok-config.sh` 檔案：

```bash
# 修改前端固定網址
export FRONTEND_SUBDOMAIN="your-frontend-name"

# 修改後端固定網址  
export BACKEND_SUBDOMAIN="your-backend-name"
```

## 使用方法

### 1. 啟動服務
```bash
./start-with-ngrok.sh
```

### 2. 查看固定網址
腳本會自動顯示您的固定網址：
```
📱 前端固定網址: https://KUNYOU-POC-frontend.ngrok.io
🔧 後端固定網址: https://KUNYOU-POC-backend.ngrok.io
```

### 3. 分享給客戶
將前端固定網址分享給客戶，他們就可以直接訪問您的應用。

## 優勢

### 🔒 網址穩定
- 每次重啟服務後，網址保持不變
- 客戶不需要重新獲取新的網址
- 便於長期使用和分享

### 🎯 專業形象
- 使用固定的子域名，看起來更專業
- 便於品牌推廣和記憶

### ⚡ 快速部署
- 無需等待隨機網址生成
- 立即可以使用預設的固定網址

## 注意事項

### 1. 子域名唯一性
- 確保您選擇的子域名在 ngrok 平台上是唯一的
- 如果子域名已被使用，需要選擇其他名稱

### 2. 付費版限制
- 固定網址功能需要 ngrok 付費版
- 免費版無法使用此功能

### 3. 配置檔案
- 修改配置後需要重新啟動服務
- 建議在 `ngrok-config.sh` 中集中管理所有設定

## 故障排除

### 問題：子域名已被使用
**解決方案**：
1. 修改 `ngrok-config.sh` 中的子域名
2. 選擇一個更獨特的名稱
3. 重新啟動服務

### 問題：隧道啟動失敗
**解決方案**：
1. 檢查 ngrok 認證令牌是否正確
2. 確認付費版狀態
3. 查看 ngrok 管理界面：`http://localhost:4040`

### 問題：網址無法訪問
**解決方案**：
1. 確認本地服務正在運行
2. 檢查防火牆設定
3. 驗證 ngrok 隧道狀態

## 進階配置

### 自定義端口
在 `ngrok-config.sh` 中修改端口設定：
```bash
export FRONTEND_PORT="3000"  # 修改前端端口
export BACKEND_PORT="5000"   # 修改後端端口
```

### 日誌設定
調整日誌級別：
```bash
export NGROK_LOG_LEVEL="stdout"  # stdout, stderr, none
```

### 重試設定
調整重試參數：
```bash
export MAX_RETRIES="10"      # 最大重試次數
export RETRY_DELAY="5"       # 重試間隔（秒）
```

## 相關檔案

- `start-with-ngrok.sh` - 主啟動腳本
- `ngrok-config.sh` - 配置文件
- `NGROK_FIXED_URLS_GUIDE.md` - 本指南

## 支援

如果您遇到問題，請：
1. 檢查 ngrok 官方文檔
2. 查看 ngrok 管理界面
3. 檢查腳本日誌輸出 