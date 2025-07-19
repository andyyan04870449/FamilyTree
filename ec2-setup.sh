#!/bin/bash

# EC2 初始化腳本
# 在 EC2 實例上執行此腳本來設置部署環境

set -e

echo "開始設置 EC2 環境..."

# 更新系統
sudo apt-get update
sudo apt-get upgrade -y

# 安裝 Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker ubuntu

# 安裝 Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# 安裝 Git
sudo apt-get install -y git

# 創建應用程式目錄
mkdir -p /home/ubuntu/FamilyTree
cd /home/ubuntu/FamilyTree

# 克隆專案（需要設置 SSH 金鑰或使用 HTTPS）
# git clone https://github.com/your-username/FamilyTree.git .

# 設置環境變數
echo "export DOCKER_HOST=unix:///var/run/docker.sock" >> ~/.bashrc
source ~/.bashrc

# 創建部署腳本
cat > /home/ubuntu/deploy.sh << 'EOF'
#!/bin/bash
set -e

echo "開始部署..."

cd /home/ubuntu/FamilyTree

# 停止現有容器
docker-compose down || true

# 拉取最新代碼
git pull origin main

# 構建新映像
docker-compose build --no-cache

# 啟動服務
docker-compose up -d

# 清理舊映像
docker image prune -f

echo "部署完成！"
EOF

chmod +x /home/ubuntu/deploy.sh

echo "EC2 環境設置完成！"
echo "請確保："
echo "1. 已設置 GitHub SSH 金鑰或使用 HTTPS 克隆"
echo "2. 已配置 AWS SSM 權限"
echo "3. 已設置 GitHub Secrets" 