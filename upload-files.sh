#!/bin/bash

# 檔案上傳腳本
set -e

echo "開始上傳檔案到 EC2..."

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

# 創建壓縮檔案
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
scp -i ~/.ssh/your-key.pem -o StrictHostKeyChecking=no \
  familytree-project.tar.gz \
  ubuntu@$INSTANCE_IP:/home/ubuntu/

# 在 EC2 上解壓並部署
echo "在 EC2 上解壓並部署..."
ssh -i ~/.ssh/your-key.pem -o StrictHostKeyChecking=no ubuntu@$INSTANCE_IP << 'EOF'
cd /home/ubuntu
tar -xzf familytree-project.tar.gz
cd FamilyTree
docker-compose up -d
echo "部署完成！"
EOF

echo "檔案上傳和部署完成！"
echo "應用程式應該在以下地址運行："
echo "前端: http://$INSTANCE_IP:4200"
echo "後端: http://$INSTANCE_IP:5088" 