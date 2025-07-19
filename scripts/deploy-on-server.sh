#!/bin/bash

# EC2 實例上的自動部署腳本
set -e

echo "🚀 開始自動部署 FamilyTree 應用程式..."

# 設置變數
PROJECT_DIR="/home/ubuntu/FamilyTree"
BACKUP_DIR="/home/ubuntu/backups/$(date +%Y%m%d_%H%M%S)"

# 創建備份
echo "📦 創建備份..."
mkdir -p $BACKUP_DIR
if [ -d "$PROJECT_DIR" ]; then
    cp -r $PROJECT_DIR/* $BACKUP_DIR/ 2>/dev/null || true
    echo "✅ 備份已創建: $BACKUP_DIR"
fi

# 更新系統
echo "📦 更新系統..."
sudo apt-get update

# 確保 Docker 已安裝
if ! command -v docker &> /dev/null; then
    echo "🐳 安裝 Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker ubuntu
    newgrp docker
fi

# 確保 Docker Compose 已安裝
if ! command -v docker-compose &> /dev/null; then
    echo "📦 安裝 Docker Compose..."
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

# 創建專案目錄
echo "📂 準備專案目錄..."
mkdir -p $PROJECT_DIR
cd $PROJECT_DIR

# 停止現有容器
echo "🛑 停止現有容器..."
docker-compose down || true

# 清理舊映像
echo "🧹 清理舊映像..."
docker system prune -f || true

# 等待部署包解壓完成（由 GitHub Actions 處理）
echo "⏳ 等待檔案準備完成..."
sleep 10

# 構建容器
echo "🔨 構建容器..."
docker-compose build --no-cache

# 啟動服務
echo "🚀 啟動服務..."
docker-compose up -d

# 等待服務啟動
echo "⏳ 等待服務啟動..."
sleep 30

# 檢查服務狀態
echo "📊 檢查服務狀態..."
docker-compose ps

# 健康檢查
echo "🏥 執行健康檢查..."

# 檢查後端
echo "🔧 檢查後端..."
for i in {1..10}; do
    if curl -f http://localhost:5088/ping >/dev/null 2>&1; then
        echo "✅ 後端服務正常"
        break
    else
        echo "⏳ 等待後端啟動... ($i/10)"
        sleep 5
    fi
done

# 檢查前端
echo "🌐 檢查前端..."
for i in {1..10}; do
    if curl -f http://localhost:4200 >/dev/null 2>&1; then
        echo "✅ 前端服務正常"
        break
    else
        echo "⏳ 等待前端啟動... ($i/10)"
        sleep 5
    fi
done

# 獲取公共 IP
PUBLIC_IP=$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4)

echo ""
echo "🎉 部署完成！"
echo ""
echo "🌐 訪問地址："
echo "  前端: http://$PUBLIC_IP:4200"
echo "  後端: http://$PUBLIC_IP:5088"
echo ""
echo "📋 管理命令："
echo "  查看狀態: cd $PROJECT_DIR && docker-compose ps"
echo "  查看日誌: cd $PROJECT_DIR && docker-compose logs"
echo "  重啟服務: cd $PROJECT_DIR && docker-compose restart"
echo "  停止服務: cd $PROJECT_DIR && docker-compose down"
echo ""
echo "�� 備份位置: $BACKUP_DIR" 