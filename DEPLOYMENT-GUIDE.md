# FamilyTree 完整部署指南

## 概述

本指南將幫助您將本地 FamilyTree 專案完整部署到 AWS EC2 實例，包括：
- 應用程式代碼
- PostgreSQL 資料庫（包含所有資料）
- Docker 容器化部署

## 前置需求

### 1. AWS 配置
- EC2 實例 ID: `i-043365febbf893121`
- 區域: `ap-southeast-2`
- SSH 金鑰檔案

### 2. 本地環境
- AWS CLI 已配置
- SSH 金鑰已設置
- PostgreSQL 資料庫已備份

## 部署步驟

### 步驟 1: 備份本地資料庫
```bash
# 執行資料庫備份
./backup-database.sh
```

### 步驟 2: 準備部署檔案
已創建的檔案：
- `docker-compose.yml` - 容器編排配置
- `familytree-backend/Dockerfile` - 後端 Docker 配置
- `familytree-frontend/Dockerfile` - 前端 Docker 配置
- `familytree-frontend/nginx.conf` - Nginx 配置

### 步驟 3: 執行部署
```bash
# 設置執行權限
chmod +x deploy-via-ssh.sh

# 執行部署
./deploy-via-ssh.sh
```

## 部署內容

### 1. 資料庫
- PostgreSQL 15
- 資料庫名稱: `familytree`
- 用戶: `user`
- 包含所有本地資料

### 2. 後端服務
- .NET 8.0
- 端口: 5088
- 環境: Production

### 3. 前端服務
- Angular 應用程式
- Nginx 伺服器
- 端口: 4200

### 4. 網路配置
- 前端代理後端 API 請求
- 靜態資源快取
- 容器間網路通信

## 驗證部署

### 1. 檢查服務狀態
```bash
# SSH 到 EC2
ssh -i your-key.pem ubuntu@your-ec2-ip

# 檢查容器狀態
docker-compose ps

# 檢查日誌
docker-compose logs
```

### 2. 測試端點
- 前端: `http://your-ec2-ip:4200`
- 後端: `http://your-ec2-ip:5088/ping`
- 資料庫: `http://your-ec2-ip:5432`

## 安全群組設置

確保 EC2 安全群組允許以下端口：
- 22 (SSH)
- 80 (HTTP)
- 443 (HTTPS)
- 4200 (前端)
- 5088 (後端)
- 5432 (PostgreSQL)

## 故障排除

### 1. 容器無法啟動
```bash
# 檢查日誌
docker-compose logs familytree-backend
docker-compose logs familytree-frontend
docker-compose logs postgres
```

### 2. 資料庫連接問題
```bash
# 檢查資料庫狀態
docker-compose exec postgres psql -U user -d familytree -c "\dt"
```

### 3. 網路問題
```bash
# 檢查網路配置
docker network ls
docker network inspect familytree_familytree-network
```

## 維護

### 1. 更新應用程式
```bash
# 重新部署
./deploy-via-ssh.sh
```

### 2. 備份資料庫
```bash
# 在 EC2 上執行
docker-compose exec postgres pg_dump -U user familytree > backup.sql
```

### 3. 監控
```bash
# 查看資源使用情況
docker stats

# 查看日誌
docker-compose logs -f
```

## 注意事項

1. **安全**: 請立即輪換 AWS 憑證
2. **備份**: 定期備份資料庫
3. **監控**: 設置 CloudWatch 監控
4. **日誌**: 配置集中式日誌管理
5. **SSL**: 考慮設置 HTTPS

## 支援

如果遇到問題，請檢查：
1. SSH 金鑰權限
2. 安全群組設置
3. 實例狀態
4. Docker 服務狀態
5. 網路連接 