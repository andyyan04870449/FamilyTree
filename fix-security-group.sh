#!/bin/bash

# 修復 EC2 安全群組設定
set -e

echo "🔧 修復 EC2 安全群組設定..."

# 設置變數
INSTANCE_ID="i-07e8d754a2a75b2b1"
SSH_KEY="TreeTest.pem"

echo "📋 檢查 AWS CLI..."
if ! command -v aws &> /dev/null; then
    echo "❌ AWS CLI 未安裝，請先安裝 AWS CLI"
    echo "   安裝指令: brew install awscli"
    exit 1
fi

echo "🔍 獲取實例的安全群組 ID..."
SECURITY_GROUP_ID=$(aws ec2 describe-instances \
    --instance-ids $INSTANCE_ID \
    --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' \
    --output text)

echo "📋 安全群組 ID: $SECURITY_GROUP_ID"

echo "🔓 開放 HTTP 端口 (80)..."
aws ec2 authorize-security-group-ingress \
    --group-id $SECURITY_GROUP_ID \
    --protocol tcp \
    --port 80 \
    --cidr 0.0.0.0/0 || echo "端口 80 可能已經開放"

echo "🔓 開放 HTTPS 端口 (443)..."
aws ec2 authorize-security-group-ingress \
    --group-id $SECURITY_GROUP_ID \
    --protocol tcp \
    --port 443 \
    --cidr 0.0.0.0/0 || echo "端口 443 可能已經開放"

echo "🔓 開放前端端口 (4200)..."
aws ec2 authorize-security-group-ingress \
    --group-id $SECURITY_GROUP_ID \
    --protocol tcp \
    --port 4200 \
    --cidr 0.0.0.0/0 || echo "端口 4200 可能已經開放"

echo "🔓 開放後端端口 (5088)..."
aws ec2 authorize-security-group-ingress \
    --group-id $SECURITY_GROUP_ID \
    --protocol tcp \
    --port 5088 \
    --cidr 0.0.0.0/0 || echo "端口 5088 可能已經開放"

echo "🔓 開放 PostgreSQL 端口 (5432)..."
aws ec2 authorize-security-group-ingress \
    --group-id $SECURITY_GROUP_ID \
    --protocol tcp \
    --port 5432 \
    --cidr 0.0.0.0/0 || echo "端口 5432 可能已經開放"

echo "📊 顯示當前安全群組規則..."
aws ec2 describe-security-groups \
    --group-ids $SECURITY_GROUP_ID \
    --query 'SecurityGroups[0].IpPermissions' \
    --output table

echo "✅ 安全群組設定完成！"
echo ""
echo "🌐 現在應該可以訪問："
echo "  前端: http://16.176.220.138:4200"
echo "  後端: http://16.176.220.138:5088"
echo ""
echo "⏳ 請等待 1-2 分鐘讓設定生效，然後再次嘗試訪問。" 