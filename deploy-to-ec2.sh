#!/bin/bash

# 部署到 EC2 的腳本
set -e

echo "開始部署到 EC2..."

# 檢查 AWS CLI 配置
echo "檢查 AWS 配置..."
aws configure list

# 檢查實例狀態
echo "檢查 EC2 實例狀態..."
INSTANCE_ID="i-043365febbf893121"
REGION="ap-southeast-2"

# 獲取實例狀態
INSTANCE_STATE=$(aws ec2 describe-instances \
  --instance-ids $INSTANCE_ID \
  --region $REGION \
  --query 'Reservations[0].Instances[0].State.Name' \
  --output text 2>/dev/null || echo "unknown")

echo "實例狀態: $INSTANCE_STATE"

if [ "$INSTANCE_STATE" != "running" ]; then
    echo "實例未運行，嘗試啟動..."
    aws ec2 start-instances --instance-ids $INSTANCE_ID --region $REGION
    echo "等待實例啟動..."
    aws ec2 wait instance-running --instance-ids $INSTANCE_ID --region $REGION
fi

# 檢查 SSM 連接
echo "檢查 SSM 連接..."
SSM_STATUS=$(aws ssm describe-instance-information \
  --filters "Key=InstanceIds,Values=$INSTANCE_ID" \
  --region $REGION \
  --query 'InstanceInformationList[0].PingStatus' \
  --output text 2>/dev/null || echo "unknown")

echo "SSM 狀態: $SSM_STATUS"

if [ "$SSM_STATUS" = "Online" ]; then
    echo "SSM 連接正常，開始部署..."
    
    # 執行部署命令
    COMMAND_ID=$(aws ssm send-command \
      --instance-ids $INSTANCE_ID \
      --document-name "AWS-RunShellScript" \
      --parameters 'commands=[
        "cd /home/ubuntu",
        "git clone https://github.com/your-username/FamilyTree.git || cd FamilyTree && git pull",
        "cd FamilyTree",
        "chmod +x ec2-setup.sh",
        "./ec2-setup.sh",
        "docker-compose down || true",
        "docker-compose build --no-cache",
        "docker-compose up -d"
      ]' \
      --region $REGION \
      --query 'Command.CommandId' \
      --output text)
    
    echo "部署命令已發送，Command ID: $COMMAND_ID"
    
    # 等待命令完成
    echo "等待部署完成..."
    aws ssm wait command-executed \
      --command-id $COMMAND_ID \
      --instance-id $INSTANCE_ID \
      --region $REGION
    
    echo "部署完成！"
else
    echo "SSM 未連接，請檢查："
    echo "1. 實例是否已安裝 SSM Agent"
    echo "2. 實例是否有正確的 IAM 角色"
    echo "3. 安全群組是否允許 SSM 連接"
fi 