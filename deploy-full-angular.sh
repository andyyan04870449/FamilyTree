#!/bin/bash

# 完整部署 Angular 應用程式到 EC2
set -e

echo "🚀 開始完整部署 Angular 應用程式..."

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

echo "📦 創建完整的 Angular 部署包..."
tar -czf familytree-angular-full.tar.gz \
  --exclude='node_modules' \
  --exclude='bin' \
  --exclude='obj' \
  --exclude='.git' \
  --exclude='*.log' \
  --exclude='dist' \
  familytree-backend/ \
  familytree-frontend/ \
  docker-compose.yml

echo "📤 上傳完整部署包到實例..."
scp -i $SSH_KEY familytree-angular-full.tar.gz ubuntu@$INSTANCE_IP:/home/ubuntu/

echo "🔧 執行完整 Angular 部署..."

# 創建遠程部署腳本
cat > remote-deploy-angular.sh << 'EOF'
#!/bin/bash
set -e

echo "🚀 開始完整 Angular 部署..."

# 更新系統
echo "📦 更新系統..."
sudo apt-get update

# 安裝 Node.js 和 npm
echo "📦 安裝 Node.js..."
if ! command -v node &> /dev/null; then
    curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
    sudo apt-get install -y nodejs
fi

# 安裝 Angular CLI
echo "📦 安裝 Angular CLI..."
if ! command -v ng &> /dev/null; then
    sudo npm install -g @angular/cli
fi

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

# 解壓部署包
echo "📁 解壓部署包..."
cd /home/ubuntu
tar -xzf familytree-angular-full.tar.gz

# 創建目錄結構
echo "📂 創建目錄結構..."
mkdir -p FamilyTree
mv familytree-backend FamilyTree/ 2>/dev/null || echo "backend 目錄已存在"
mv familytree-frontend FamilyTree/ 2>/dev/null || echo "frontend 目錄已存在"
mv docker-compose.yml FamilyTree/ 2>/dev/null || echo "docker-compose.yml 已存在"

cd FamilyTree

# 構建 Angular 應用程式
echo "🔨 構建 Angular 應用程式..."
cd familytree-frontend

# 安裝依賴
echo "📦 安裝前端依賴..."
npm install

# 構建生產版本
echo "🔨 構建生產版本..."
ng build --configuration production

# 回到根目錄
cd ..

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

echo "✅ 完整 Angular 部署完成！"
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

echo "📤 上傳遠程部署腳本..."
scp -i $SSH_KEY remote-deploy-angular.sh ubuntu@$INSTANCE_IP:/home/ubuntu/

echo "🔧 執行完整 Angular 部署..."
ssh -i $SSH_KEY ubuntu@$INSTANCE_IP "chmod +x /home/ubuntu/remote-deploy-angular.sh && /home/ubuntu/remote-deploy-angular.sh"

echo "🧹 清理本地檔案..."
rm -f familytree-angular-full.tar.gz remote-deploy-angular.sh

echo "✅ 完整 Angular 部署完成！"
echo ""
echo "🌐 您的 Angular 應用程式現在應該可以訪問："
echo "  前端: http://$INSTANCE_IP:4200"
echo "  後端: http://$INSTANCE_IP:5088"
echo ""
echo "📋 如果仍有問題，請 SSH 連接到實例檢查："
echo "  ssh -i $SSH_KEY ubuntu@$INSTANCE_IP" 