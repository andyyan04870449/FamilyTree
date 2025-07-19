#!/bin/bash

# 簡化部署腳本
set -e

echo "開始簡化部署到 EC2..."

# 設置變數
INSTANCE_ID="i-043365febbf893121"
REGION="ap-southeast-2"

# 檢查實例狀態
echo "檢查實例狀態..."
aws ec2 describe-instances --instance-ids $INSTANCE_ID --region $REGION --query 'Reservations[0].Instances[0].State.Name' --output text

# 創建部署包
echo "創建部署包..."
tar -czf familytree-deploy.tar.gz \
  --exclude='node_modules' \
  --exclude='bin' \
  --exclude='obj' \
  --exclude='.git' \
  --exclude='*.log' \
  familytree-backend/ \
  familytree-frontend/ \
  database-backup/ \
  docker-compose.yml

# 上傳到 S3
echo "上傳到 S3..."
aws s3 mb s3://familytree-deploy-$RANDOM --region $REGION || true
BUCKET_NAME=$(aws s3 ls --region $REGION | grep familytree-deploy | head -1 | awk '{print $3}')
aws s3 cp familytree-deploy.tar.gz s3://$BUCKET_NAME/ --region $REGION

# 創建部署腳本
cat > deploy-commands.txt << 'EOF'
#!/bin/bash
set -e

echo "開始在 EC2 上部署..."

# 更新系統
sudo apt-get update
sudo apt-get upgrade -y

# 安裝 Docker
if ! command -v docker &> /dev/null; then
    curl -fsSL https://get.docker.com -o get-docker.sh
    sudo sh get-docker.sh
    sudo usermod -aG docker ubuntu
fi

# 安裝 Docker Compose
if ! command -v docker-compose &> /dev/null; then
    sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    sudo chmod +x /usr/local/bin/docker-compose
fi

# 安裝 AWS CLI
if ! command -v aws &> /dev/null; then
    curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
    unzip awscliv2.zip
    sudo ./aws/install
fi

# 下載部署包
aws s3 cp s3://BUCKET_NAME/familytree-deploy.tar.gz /home/ubuntu/
cd /home/ubuntu
tar -xzf familytree-deploy.tar.gz

# 創建目錄結構
mkdir -p FamilyTree
mv familytree-backend FamilyTree/
mv familytree-frontend FamilyTree/
mv database-backup FamilyTree/
mv docker-compose.yml FamilyTree/

cd FamilyTree

# 停止現有容器
docker-compose down || true

# 構建並啟動
docker-compose build --no-cache
docker-compose up -d

echo "部署完成！"
docker-compose ps
EOF

# 替換 bucket 名稱
sed -i "s/BUCKET_NAME/$BUCKET_NAME/g" deploy-commands.txt

echo "部署腳本已準備好！"
echo "請手動執行以下步驟："
echo ""
echo "1. 連接到您的 EC2 實例："
echo "   ssh -i your-key.pem ubuntu@your-ec2-ip"
echo ""
echo "2. 在 EC2 上執行："
echo "   aws configure"
echo "   輸入您的 AWS 憑證"
echo ""
echo "3. 執行部署："
echo "   bash deploy-commands.txt"
echo ""
echo "4. 檢查部署狀態："
echo "   docker-compose ps"
echo ""
echo "S3 Bucket: $BUCKET_NAME"
echo "部署包: familytree-deploy.tar.gz" 