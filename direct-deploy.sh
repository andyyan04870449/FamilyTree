#!/bin/bash

# 直接檔案上傳部署腳本
set -e

echo "🚀 開始直接檔案上傳部署..."

# 設置變數
INSTANCE_IP="16.176.220.138"
SSH_KEY="TreeTest.pem"

echo "📋 檢查 SSH 金鑰..."
if [ ! -f "$SSH_KEY" ]; then
    echo "❌ SSH 金鑰檔案不存在: $SSH_KEY"
    exit 1
fi

echo "🔐 設置 SSH 金鑰權限..."
chmod 400 $SSH_KEY

echo "🌐 測試 SSH 連接..."
ssh -i $SSH_KEY -o ConnectTimeout=10 -o StrictHostKeyChecking=no ubuntu@$INSTANCE_IP "echo 'SSH 連接成功'" || {
    echo "❌ SSH 連接失敗"
    exit 1
}

echo "📦 開始遠程部署..."

# 創建遠程部署腳本
cat > remote-direct-deploy.sh << 'EOF'
#!/bin/bash
set -e

echo "🚀 開始直接部署 FamilyTree..."

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
EOF

echo "📤 上傳部署腳本到實例..."
scp -i $SSH_KEY remote-direct-deploy.sh ubuntu@$INSTANCE_IP:/home/ubuntu/

echo "📁 上傳專案檔案到實例..."
ssh -i $SSH_KEY ubuntu@$INSTANCE_IP "mkdir -p /home/ubuntu/FamilyTree"

# 上傳後端
echo "📤 上傳後端..."
scp -i $SSH_KEY -r familytree-backend/ ubuntu@$INSTANCE_IP:/home/ubuntu/FamilyTree/

# 上傳前端
echo "📤 上傳前端..."
scp -i $SSH_KEY -r familytree-frontend/ ubuntu@$INSTANCE_IP:/home/ubuntu/FamilyTree/

# 上傳資料庫備份
echo "📤 上傳資料庫備份..."
scp -i $SSH_KEY -r database-backup/ ubuntu@$INSTANCE_IP:/home/ubuntu/FamilyTree/

# 上傳 docker-compose.yml
echo "📤 上傳 docker-compose.yml..."
scp -i $SSH_KEY docker-compose.yml ubuntu@$INSTANCE_IP:/home/ubuntu/FamilyTree/

echo "🔧 執行遠程部署..."
ssh -i $SSH_KEY ubuntu@$INSTANCE_IP "cd /home/ubuntu/FamilyTree && chmod +x /home/ubuntu/remote-direct-deploy.sh && /home/ubuntu/remote-direct-deploy.sh"

echo "🧹 清理本地檔案..."
rm -f remote-direct-deploy.sh

echo "✅ 直接部署完成！"
echo ""
echo "🌐 您的應用程式現在應該可以訪問："
echo "  前端: http://$INSTANCE_IP:4200"
echo "  後端: http://$INSTANCE_IP:5088"
echo ""
echo "📋 如果仍有問題，請 SSH 連接到實例檢查："
echo "  ssh -i $SSH_KEY ubuntu@$INSTANCE_IP" 