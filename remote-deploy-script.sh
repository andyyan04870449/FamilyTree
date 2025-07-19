#!/bin/bash
set -e

echo "🚀 開始遠程部署 FamilyTree..."

# 設置變數
BUCKET_NAME="familytree-deploy-1564"
REGION="ap-southeast-2"

echo "📦 下載部署包..."
aws s3 cp s3://$BUCKET_NAME/familytree-deploy.tar.gz /home/ubuntu/ || {
    echo "❌ 無法下載部署包，請檢查 AWS CLI 配置"
    echo "執行: aws configure"
    exit 1
}

echo "📁 解壓部署包..."
cd /home/ubuntu
tar -xzf familytree-deploy.tar.gz

echo "📂 創建目錄結構..."
mkdir -p FamilyTree
mv familytree-backend FamilyTree/ 2>/dev/null || echo "backend 目錄已存在"
mv familytree-frontend FamilyTree/ 2>/dev/null || echo "frontend 目錄已存在"
mv database-backup FamilyTree/ 2>/dev/null || echo "database-backup 目錄已存在"
mv docker-compose.yml FamilyTree/ 2>/dev/null || echo "docker-compose.yml 已存在"

cd FamilyTree

echo "🐳 檢查 Docker..."
if ! command -v docker &> /dev/null; then
    echo "安裝 Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker ubuntu
    newgrp docker
fi

echo "📦 檢查 Docker Compose..."
if ! command -v docker-compose &> /dev/null; then
    echo "安裝 Docker Compose..."
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

echo "🛑 停止現有容器..."
docker-compose down || true

echo "🧹 清理舊映像..."
docker system prune -f || true

echo "🔨 構建容器..."
docker-compose build --no-cache

echo "🚀 啟動服務..."
docker-compose up -d

echo "⏳ 等待服務啟動..."
sleep 30

echo "📊 檢查服務狀態..."
docker-compose ps

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
