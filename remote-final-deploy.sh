#!/bin/bash
set -e

echo "🚀 開始最終部署 FamilyTree..."

# 更新系統
echo "📦 更新系統..."
sudo apt-get update

# 安裝 Docker
echo "🐳 安裝 Docker..."
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker ubuntu
    newgrp docker
fi

# 安裝 Docker Compose
echo "📦 安裝 Docker Compose..."
if ! command -v docker-compose &> /dev/null; then
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

# 停止現有容器
echo "🛑 停止現有容器..."
docker-compose down || true

# 清理舊映像
echo "🧹 清理舊映像..."
docker system prune -f || true

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

# 測試服務
echo "🧪 測試服務..."
echo "測試後端..."
curl -f http://localhost:5088/ping || echo "後端測試失敗"

echo "測試前端..."
curl -f http://localhost:4200 || echo "前端測試失敗"

# 獲取公共 IP
PUBLIC_IP=$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4)

echo "✅ 部署完成！"
echo ""
echo "🌐 訪問地址："
echo "  前端: http://$PUBLIC_IP:4200"
echo "  後端: http://$PUBLIC_IP:5088"
echo ""
echo "📋 管理命令："
echo "  查看狀態: docker-compose ps"
echo "  查看日誌: docker-compose logs"
echo "  重啟服務: docker-compose restart"
echo "  停止服務: docker-compose down"
