#!/bin/bash

# FamilyTree AWS 快速狀態檢查腳本
# 生成時間: 2025-01-27

echo "======================================="
echo "🚀 FamilyTree AWS 快速狀態檢查"
echo "======================================="
echo "時間: $(date)"
echo

# 檢查AWS CLI連接
echo "🔐 檢查AWS連接..."
if aws sts get-caller-identity >/dev/null 2>&1; then
    echo "✅ AWS CLI 連接正常"
else
    echo "❌ AWS CLI 連接失敗"
    exit 1
fi
echo

# 檢查EC2實例狀態
echo "🖥️  檢查EC2實例狀態..."
INSTANCE_ID="i-00612c7b5caa2940a"
INSTANCE_STATUS=$(aws ec2 describe-instances \
  --instance-ids $INSTANCE_ID \
  --query 'Reservations[0].Instances[0].State.Name' \
  --output text 2>/dev/null)

PUBLIC_IP=$(aws ec2 describe-instances \
  --instance-ids $INSTANCE_ID \
  --query 'Reservations[0].Instances[0].PublicIpAddress' \
  --output text 2>/dev/null)

if [ "$INSTANCE_STATUS" = "running" ]; then
    echo "✅ EC2實例狀態: $INSTANCE_STATUS"
    echo "🌐 公網IP: $PUBLIC_IP"
else
    echo "⚠️  EC2實例狀態: $INSTANCE_STATUS"
    echo "🌐 公網IP: $PUBLIC_IP"
fi
echo

# 檢查S3存儲桶
echo "🗄️  檢查S3存儲桶..."
S3_BUCKET="familytree-deploy-1564"
S3_CONTENT=$(aws s3 ls s3://$S3_BUCKET/ 2>/dev/null)
if [ $? -eq 0 ]; then
    echo "✅ S3存儲桶可訪問"
    echo "$S3_CONTENT"
else
    echo "❌ S3存儲桶無法訪問"
fi
echo

# 檢查服務連通性
echo "🔧 檢查服務連通性..."

# 檢查HTTP端口80
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://$PUBLIC_IP --connect-timeout 5)
if [ "$HTTP_STATUS" = "200" ] || [ "$HTTP_STATUS" = "301" ] || [ "$HTTP_STATUS" = "302" ]; then
    echo "✅ HTTP (80): $HTTP_STATUS"
else
    echo "❌ HTTP (80): 無法連接 (狀態碼: $HTTP_STATUS)"
fi

# 檢查前端端口4200
FRONTEND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://$PUBLIC_IP:4200 --connect-timeout 5)
if [ "$FRONTEND_STATUS" = "200" ] || [ "$FRONTEND_STATUS" = "301" ] || [ "$FRONTEND_STATUS" = "302" ]; then
    echo "✅ 前端 (4200): $FRONTEND_STATUS"
else
    echo "❌ 前端 (4200): 無法連接 (狀態碼: $FRONTEND_STATUS)"
fi

# 檢查後端API端口5088
API_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://$PUBLIC_IP:5088 --connect-timeout 5)
if [ "$API_STATUS" = "200" ] || [ "$API_STATUS" = "301" ] || [ "$API_STATUS" = "302" ]; then
    echo "✅ 後端API (5088): $API_STATUS"
else
    echo "❌ 後端API (5088): 無法連接 (狀態碼: $API_STATUS)"
fi
echo

# 檢查SSH連通性
echo "🔑 檢查SSH連通性..."
SSH_CHECK=$(nc -z -w5 $PUBLIC_IP 22 2>/dev/null && echo "open" || echo "closed")
if [ "$SSH_CHECK" = "open" ]; then
    echo "✅ SSH (22): 端口開放"
else
    echo "❌ SSH (22): 端口關閉或無法連接"
fi
echo

# 總結
echo "======================================="
echo "📊 檢查總結"
echo "======================================="
echo "實例ID: $INSTANCE_ID"
echo "實例狀態: $INSTANCE_STATUS"
echo "公網IP: $PUBLIC_IP"
echo "主要訪問URL:"
echo "  - http://$PUBLIC_IP (HTTP)"
echo "  - http://$PUBLIC_IP:4200 (前端)"
echo "  - http://$PUBLIC_IP:5088 (後端API)"
echo
echo "SSH連接命令:"
echo "  ssh -i TreeTest.pem ec2-user@$PUBLIC_IP"
echo "=======================================" 