#!/bin/bash

# 測試 CI/CD 部署狀態
set -e

echo "🧪 測試 CI/CD 部署狀態"
echo "========================"

# 設置變數
INSTANCE_IP="16.176.220.138"
SSH_KEY="../FamilyTree/TreeTest.pem"

echo "📋 檢查 SSH 金鑰..."
if [ ! -f "$SSH_KEY" ]; then
    echo "❌ SSH 金鑰檔案不存在: $SSH_KEY"
    echo "請確保 SSH 金鑰檔案存在於原始 FamilyTree 目錄中"
    exit 1
fi

echo "🔐 設置 SSH 金鑰權限..."
chmod 400 $SSH_KEY

echo "🌐 測試 SSH 連接..."
ssh -i $SSH_KEY -o ConnectTimeout=10 -o StrictHostKeyChecking=no ubuntu@$INSTANCE_IP "echo 'SSH 連接成功'" || {
    echo "❌ SSH 連接失敗"
    exit 1
}

echo "📊 檢查服務狀態..."
ssh -i $SSH_KEY ubuntu@$INSTANCE_IP "cd /home/ubuntu/FamilyTree && docker-compose ps"

echo "🏥 執行健康檢查..."

# 檢查後端
echo "🔧 檢查後端服務..."
if curl -f http://$INSTANCE_IP:5088/ping >/dev/null 2>&1; then
    echo "✅ 後端服務正常"
else
    echo "❌ 後端服務異常"
fi

# 檢查前端
echo "🌐 檢查前端服務..."
if curl -f http://$INSTANCE_IP:4200 >/dev/null 2>&1; then
    echo "✅ 前端服務正常"
else
    echo "❌ 前端服務異常"
fi

echo ""
echo "📋 部署狀態總結："
echo "========================"
echo "🌐 前端地址: http://$INSTANCE_IP:4200"
echo "🔧 後端地址: http://$INSTANCE_IP:5088"
echo ""
echo "📋 管理命令："
echo "  SSH 連接: ssh -i $SSH_KEY ubuntu@$INSTANCE_IP"
echo "  查看日誌: cd /home/ubuntu/FamilyTree && docker-compose logs"
echo "  重啟服務: cd /home/ubuntu/FamilyTree && docker-compose restart"
echo ""
echo "🎉 測試完成！" 