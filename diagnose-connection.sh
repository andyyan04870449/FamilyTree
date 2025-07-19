#!/bin/bash

# 連接診斷腳本
set -e

echo "🔍 開始診斷 EC2 連接問題..."

# 設置變數
INSTANCE_ID="i-07e8d754a2a75b2b1"
REGION="ap-southeast-2"

echo "📋 檢查實例狀態..."

# 檢查實例狀態
INSTANCE_STATE=$(aws ec2 describe-instances \
  --instance-ids $INSTANCE_ID \
  --region $REGION \
  --query 'Reservations[0].Instances[0].State.Name' \
  --output text 2>/dev/null || echo "unknown")

echo "實例狀態: $INSTANCE_STATE"

if [ "$INSTANCE_STATE" != "running" ]; then
    echo "❌ 實例未運行，嘗試啟動..."
    aws ec2 start-instances --instance-ids $INSTANCE_ID --region $REGION
    echo "⏳ 等待實例啟動..."
    aws ec2 wait instance-running --instance-ids $INSTANCE_ID --region $REGION
    echo "✅ 實例已啟動"
fi

# 獲取實例詳細資訊
echo "🌐 獲取實例詳細資訊..."
INSTANCE_INFO=$(aws ec2 describe-instances \
  --instance-ids $INSTANCE_ID \
  --region $REGION \
  --query 'Reservations[0].Instances[0]' \
  --output json 2>/dev/null || echo "{}")

# 提取資訊
PUBLIC_IP=$(echo $INSTANCE_INFO | grep -o '"PublicIpAddress":"[^"]*"' | cut -d'"' -f4)
SECURITY_GROUP=$(echo $INSTANCE_INFO | grep -o '"GroupId":"[^"]*"' | cut -d'"' -f4)
KEY_NAME=$(echo $INSTANCE_INFO | grep -o '"KeyName":"[^"]*"' | cut -d'"' -f4)

echo "實例 IP: $PUBLIC_IP"
echo "安全群組: $SECURITY_GROUP"
echo "金鑰名稱: $KEY_NAME"

# 檢查安全群組規則
echo "🔒 檢查安全群組規則..."
SECURITY_GROUP_RULES=$(aws ec2 describe-security-groups \
  --group-ids $SECURITY_GROUP \
  --region $REGION \
  --query 'SecurityGroups[0].IpPermissions' \
  --output json 2>/dev/null || echo "[]")

echo "安全群組規則:"
echo $SECURITY_GROUP_RULES | jq '.[] | {Port: .FromPort, Protocol: .IpProtocol, Source: .IpRanges[0].CidrIp}' 2>/dev/null || echo "無法解析安全群組規則"

# 測試連接
echo "🌐 測試網路連接..."
if [ ! -z "$PUBLIC_IP" ]; then
    echo "測試 SSH 連接 (端口 22)..."
    nc -z -w5 $PUBLIC_IP 22 && echo "✅ SSH 端口開放" || echo "❌ SSH 端口關閉"
    
    echo "測試 HTTP 連接 (端口 80)..."
    nc -z -w5 $PUBLIC_IP 80 && echo "✅ HTTP 端口開放" || echo "❌ HTTP 端口關閉"
    
    echo "測試 HTTPS 連接 (端口 443)..."
    nc -z -w5 $PUBLIC_IP 443 && echo "✅ HTTPS 端口開放" || echo "❌ HTTPS 端口關閉"
else
    echo "❌ 無法獲取實例 IP"
fi

echo ""
echo "📋 診斷結果："
echo "1. 實例狀態: $INSTANCE_STATE"
echo "2. 實例 IP: $PUBLIC_IP"
echo "3. 安全群組: $SECURITY_GROUP"
echo "4. 金鑰名稱: $KEY_NAME"
echo ""
echo "🔧 可能的解決方案："
echo ""
if [ "$INSTANCE_STATE" != "running" ]; then
    echo "1. 實例未運行，請等待啟動完成"
fi

if [ -z "$PUBLIC_IP" ]; then
    echo "2. 實例沒有公共 IP，請檢查網路設置"
fi

echo "3. 檢查安全群組是否開放端口 22 (SSH)"
echo "4. 確認您有正確的 SSH 金鑰檔案"
echo "5. 檢查 SSH 金鑰權限: chmod 400 your-key.pem"
echo ""
echo "📖 連接命令："
if [ ! -z "$PUBLIC_IP" ]; then
    echo "ssh -i your-key.pem ubuntu@$PUBLIC_IP"
fi 