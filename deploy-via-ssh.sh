#!/bin/bash

# 通過 SSH 部署腳本
set -e

echo "開始通過 SSH 部署到 EC2..."

# 設置變數
INSTANCE_ID="i-043365febbf893121"
REGION="ap-southeast-2"

# 獲取實例的公共 IP
echo "獲取實例 IP..."
INSTANCE_IP=$(aws ec2 describe-instances \
  --instance-ids $INSTANCE_ID \
  --region $REGION \
  --query 'Reservations[0].Instances[0].PublicIpAddress' \
  --output text)

echo "實例 IP: $INSTANCE_IP"

# 檢查 SSH 金鑰
SSH_KEY="~/.ssh/your-key.pem"
if [ ! -f "$SSH_KEY" ]; then
    echo "請提供 SSH 金鑰路徑:"
    read -p "SSH 金鑰路徑: " SSH_KEY
fi

# 創建專案壓縮檔案
echo "創建專案壓縮檔案..."
tar -czf familytree-project.tar.gz \
  --exclude='node_modules' \
  --exclude='bin' \
  --exclude='obj' \
  --exclude='.git' \
  --exclude='*.log' \
  familytree-backend/ \
  familytree-frontend/ \
  database-backup/ \
  docker-compose.yml

# 上傳檔案到 EC2
echo "上傳檔案到 EC2..."
scp -i "$SSH_KEY" -o StrictHostKeyChecking=no \
  familytree-project.tar.gz \
  ubuntu@$INSTANCE_IP:/home/ubuntu/

# 在 EC2 上設置環境並部署
echo "在 EC2 上設置環境並部署..."
ssh -i "$SSH_KEY" -o StrictHostKeyChecking=no ubuntu@$INSTANCE_IP << 'EOF'
set -e

echo "開始在 EC2 上設置環境..."

# 更新系統
sudo apt-get update
sudo apt-get upgrade -y

# 安裝 Docker
if ! command -v docker &> /dev/null; then
    echo "安裝 Docker..."
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker ubuntu
    newgrp docker
fi

# 安裝 Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo "安裝 Docker Compose..."
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

# 安裝 PostgreSQL 客戶端
sudo apt-get install -y postgresql-client

# 創建應用程式目錄
mkdir -p /home/ubuntu/FamilyTree
cd /home/ubuntu/FamilyTree

# 解壓專案檔案
echo "解壓專案檔案..."
tar -xzf /home/ubuntu/familytree-project.tar.gz

# 停止現有容器
docker-compose down || true

# 清理舊映像
docker system prune -f || true

# 創建資料庫初始化腳本
mkdir -p /home/ubuntu/FamilyTree/database-backup
cat > /home/ubuntu/FamilyTree/database-backup/init-db.sh << 'DBINIT'
#!/bin/bash
set -e

echo "初始化資料庫..."

# 等待 PostgreSQL 啟動
until pg_isready -h postgres -p 5432 -U user; do
    echo "等待 PostgreSQL 啟動..."
    sleep 2
done

# 恢復資料庫
echo "恢復資料庫資料..."
PGPASSWORD="" psql -h postgres -p 5432 -U user -d familytree -f /docker-entrypoint-initdb.d/familytree_backup.sql

echo "資料庫初始化完成！"
DBINIT

chmod +x /home/ubuntu/FamilyTree/database-backup/init-db.sh

# 構建並啟動服務
echo "構建並啟動服務..."
docker-compose build --no-cache
docker-compose up -d

# 等待服務啟動
echo "等待服務啟動..."
sleep 30

# 檢查服務狀態
echo "檢查服務狀態..."
docker-compose ps

echo "部署完成！"
echo "前端: http://localhost:4200"
echo "後端: http://localhost:5088"
EOF

echo "部署完成！"
echo "應用程式應該在以下地址運行："
echo "前端: http://$INSTANCE_IP:4200"
echo "後端: http://$INSTANCE_IP:5088"

# 清理本地檔案
rm -f familytree-project.tar.gz 