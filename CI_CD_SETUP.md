# FamilyTree CI/CD 自動部署設置指南

## 概述

這個 CI/CD 系統會在您推送代碼到 GitHub 時自動部署到 EC2 實例。

## 設置步驟

### 1. 準備 SSH 金鑰

確保您有 EC2 實例的 SSH 私鑰檔案（TreeTest.pem）。

### 2. 設置 GitHub Secrets

在您的 GitHub 倉庫中設置以下 Secrets：

1. 進入 GitHub 倉庫
2. 點擊 `Settings` → `Secrets and variables` → `Actions`
3. 點擊 `New repository secret`
4. 添加以下 Secret：

```
Name: EC2_SSH_KEY
Value: [您的 SSH 私鑰內容]
```

**獲取 SSH 私鑰內容：**
```bash
cat TreeTest.pem
```
複製整個內容（包括 `-----BEGIN OPENSSH PRIVATE KEY-----` 和 `-----END OPENSSH PRIVATE KEY-----`）

### 3. 確保 EC2 安全群組設置

確保 EC2 實例的安全群組開放以下端口：
- 22 (SSH)
- 80 (HTTP)
- 443 (HTTPS)
- 4200 (前端)
- 5088 (後端)
- 5432 (PostgreSQL)

### 4. 上傳專案到 GitHub

```bash
# 初始化 Git 倉庫（如果還沒有）
git init

# 添加所有檔案
git add .

# 提交變更
git commit -m "Initial commit with CI/CD setup"

# 添加遠程倉庫（替換為您的 GitHub 倉庫 URL）
git remote add origin https://github.com/yourusername/FamilyTree.git

# 推送到主分支
git push -u origin main
```

## 自動部署流程

### 觸發條件
- 推送到 `main` 或 `master` 分支
- 手動觸發（在 GitHub Actions 頁面）

### 部署步驟
1. **檢查代碼** - 從 GitHub 拉取最新代碼
2. **設置 SSH** - 配置到 EC2 的 SSH 連接
3. **上傳部署腳本** - 將部署腳本上傳到 EC2
4. **創建部署包** - 打包應用程式檔案
5. **執行部署** - 在 EC2 上執行自動部署
6. **健康檢查** - 驗證服務是否正常運行

## 部署腳本功能

`scripts/deploy-on-server.sh` 包含以下功能：

- ✅ 自動備份現有部署
- ✅ 安裝/更新 Docker 和 Docker Compose
- ✅ 停止現有容器
- ✅ 構建新容器映像
- ✅ 啟動服務
- ✅ 健康檢查
- ✅ 部署狀態報告

## 監控和管理

### 查看部署狀態
- 在 GitHub 倉庫的 `Actions` 標籤頁查看部署進度
- 檢查部署日誌和錯誤信息

### 手動管理服務
SSH 到 EC2 實例後，可以使用以下命令：

```bash
# 查看服務狀態
cd /home/ubuntu/FamilyTree
docker-compose ps

# 查看日誌
docker-compose logs

# 重啟服務
docker-compose restart

# 停止服務
docker-compose down

# 手動執行部署
/home/ubuntu/deploy-on-server.sh
```

### 查看備份
```bash
ls -la /home/ubuntu/backups/
```

## 故障排除

### 常見問題

1. **SSH 連接失敗**
   - 檢查 EC2 實例是否運行
   - 確認安全群組允許 SSH 連接
   - 驗證 SSH 金鑰是否正確

2. **部署失敗**
   - 檢查 GitHub Actions 日誌
   - 確認所有 Secrets 已正確設置
   - 檢查 EC2 實例的磁碟空間

3. **服務無法訪問**
   - 確認安全群組開放必要端口
   - 檢查 Docker 容器狀態
   - 查看容器日誌

### 手動部署

如果自動部署失敗，可以手動執行：

```bash
# 在本地執行
./scripts/deploy-on-server.sh
```

## 安全注意事項

- 🔒 SSH 金鑰應妥善保管，不要提交到代碼倉庫
- 🔒 定期更新 EC2 實例的安全群組規則
- 🔒 監控部署日誌，及時發現異常
- 🔒 定期備份重要數據

## 聯繫支持

如果遇到問題，請檢查：
1. GitHub Actions 日誌
2. EC2 實例的系統日誌
3. Docker 容器日誌 