# AWS FamilyTree Docker 部署備份指南

## 📅 備份資訊
- **備份時間**: 2025-07-25 08:47 UTC
- **AWS實例**: i-04a08ac869c2c20af
- **公網IP**: 52.62.99.253
- **Docker版本**: 28.3.2, build 578ccf6
- **Docker Compose版本**: v2.38.2

## 🎯 備份完成狀況
✅ **備份已成功完成** - AWS上運行中的Docker版本已備份

### 📦 備份內容位置
```
AWS位置: /home/ubuntu/backup/familytree_20250725_084659/
本地存取: scp -i POC.pem ubuntu@52.62.99.253:~/backup/familytree_20250725_084659/* ./
```

### 📁 備份檔案清單
- `docker-compose.yml` - Docker服務編排配置
- `ngrok.yml` - Ngrok隧道配置  
- `familytree_database.sql` - PostgreSQL完整資料備份 (110KB)
- `source-code/familytree-backend/` - .NET Core 8.0 後端源碼
- `source-code/familytree-frontend/` - Angular 20.1.1 前端源碼
- `docker_containers_status.txt` - 容器運行狀態記錄
- `docker_images_info.txt` - Docker映像資訊
- `AWS_DOCKER_BACKUP_README.md` - 詳細重建指南

## 🐳 當前運行狀態
| 服務 | 映像 | 狀態 | 端口 |
|------|------|------|------|
| familytree-frontend | familytree-frontend | ✅ 運行5小時 | 4200→80 |
| familytree-backend | familytree-backend | ✅ 運行5小時 | 5088→80 |
| postgres | postgres:15 | ✅ 運行5小時 | 5432→5432 |
| ngrok | ngrok/ngrok:latest | ✅ 運行5小時 | 4040→4040 |

## 🌐 公開訪問網址
- **前端應用**: https://kunyou-poc-frontend.ngrok.io
- **後端API**: https://kunyou-poc-backend.ngrok.io

## 🚀 如何下載備份
```bash
# 下載單一檔案
scp -i POC.pem ubuntu@52.62.99.253:~/backup/familytree_20250725_084659/familytree_database.sql ./

# 下載完整備份目錄
scp -i POC.pem -r ubuntu@52.62.99.253:~/backup/familytree_20250725_084659/ ./aws_backup/

# 下載壓縮備份檔案 (如果已建立)
scp -i POC.pem ubuntu@52.62.99.253:~/backup/familytree_20250725_084659/familytree_backup.tar.gz ./
```

## 🔧 重建部署快速指南

### 1. 環境準備
```bash
sudo apt update && sudo apt install -y docker.io docker-compose-plugin
sudo usermod -aG docker $USER
newgrp docker
```

### 2. 部署應用
```bash
# 解壓備份並重建
tar -xzf familytree_backup.tar.gz
cd source-code
cp ../docker-compose.yml .
cp ../ngrok.yml .
docker compose up --build -d
```

### 3. 還原資料庫
```bash
# 等待容器啟動完成
sleep 30
docker exec -i familytree-postgres-1 psql -U user familytree < ../familytree_database.sql
```

## 📊 備份統計
- **總檔案數**: 217個檔案
- **備份大小**: 2.7MB (未壓縮)
- **資料庫大小**: 110KB
- **照片檔案**: 包含多專案照片資料

## ⚠️ 重要提醒
1. **此備份包含當前運行中的生產版本**
2. **包含完整的資料庫資料和照片檔案**  
3. **Ngrok配置需要有效的auth token**
4. **重建時確保環境變數正確設置**

## 📞 SSH連線資訊
```bash
ssh -i POC.pem ubuntu@52.62.99.253
```

備份已完成！系統持續在AWS上穩定運行中。 