#!/bin/bash

# 遠端部署腳本 - 在 EC2 上執行
set -e

echo "🚀 開始遠端部署 FamilyTree..."

# 設置變數
BUCKET_NAME="familytree-deploy-1564"
REGION="ap-southeast-2"

# 檢查是否在 EC2 上
if ! curl -s http://169.254.169.254/latest/meta-data/instance-id > /dev/null 2>&1; then
    echo "❌ 此腳本需要在 EC2 實例上執行"
    exit 1
fi

echo "✅ 確認在 EC2 實例上運行"

# 檢查 AWS CLI
if ! command -v aws &> /dev/null; then
    echo "📦 安裝 AWS CLI..."
    curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
    unzip awscliv2.zip
    sudo ./aws/install
fi

# 檢查 AWS 配置
if ! aws sts get-caller-identity &> /dev/null; then
    echo "⚠️  AWS CLI 未配置，請先運行："
    echo "aws configure"
    echo "然後輸入您的 AWS 憑證"
    exit 1
fi

echo "📦 下載部署包..."
aws s3 cp s3://$BUCKET_NAME/familytree-deploy.tar.gz /home/ubuntu/

echo "📁 解壓部署包..."
cd /home/ubuntu
tar -xzf familytree-deploy.tar.gz

echo "📂 創建目錄結構..."
mkdir -p FamilyTree
mv familytree-backend FamilyTree/
mv familytree-frontend FamilyTree/
mv database-backup FamilyTree/
mv docker-compose.yml FamilyTree/

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