#!/bin/bash

# ngrok 診斷腳本
# 此檔案的目的：診斷 ngrok 固定網址問題
# 主要功能：檢查付費狀態、可用網址和配置問題

echo "🔍 ngrok 診斷工具"
echo "=================="

# 檢查 ngrok 版本
echo "📋 ngrok 版本:"
ngrok version

echo ""
echo "🔑 認證狀態:"
ngrok config check

echo ""
echo "💳 檢查付費功能..."

# 嘗試啟動一個簡單的隧道來測試基本功能
echo "🧪 測試基本隧道功能..."
ngrok http 4200 --log=stdout > /tmp/ngrok_test.log 2>&1 &
TEST_PID=$!

sleep 5

# 檢查隧道是否成功啟動
if curl -s http://localhost:4040/api/tunnels > /dev/null 2>&1; then
    echo "✅ 基本隧道功能正常"
    
    # 獲取實際的網址
    ACTUAL_URL=$(curl -s http://localhost:4040/api/tunnels | grep -o '"public_url":"[^"]*"' | sed 's/"public_url":"//g' | sed 's/"//g' | head -1)
    echo "🌐 實際網址: $ACTUAL_URL"
    
    # 分析網址格式
    if [[ $ACTUAL_URL == *".jp.ngrok.io" ]]; then
        echo "📍 區域: 日本 (jp)"
        SUGGESTED_SUBDOMAIN="familytree-frontend-jp"
    elif [[ $ACTUAL_URL == *".us.ngrok.io" ]]; then
        echo "📍 區域: 美國 (us)"
        SUGGESTED_SUBDOMAIN="familytree-frontend-us"
    elif [[ $ACTUAL_URL == *".eu.ngrok.io" ]]; then
        echo "📍 區域: 歐洲 (eu)"
        SUGGESTED_SUBDOMAIN="familytree-frontend-eu"
    else
        echo "📍 區域: 其他"
        SUGGESTED_SUBDOMAIN="familytree-frontend-custom"
    fi
    
    echo "💡 建議的子域名: $SUGGESTED_SUBDOMAIN"
    
else
    echo "❌ 基本隧道功能異常"
fi

# 停止測試隧道
kill $TEST_PID 2>/dev/null
pkill ngrok 2>/dev/null
sleep 2

echo ""
echo "🔧 測試固定網址功能..."

# 測試不同的固定網址選項
SUBDOMAINS=("familytree-frontend" "familytree-frontend-test" "my-familytree" "familytree-app")

for subdomain in "${SUBDOMAINS[@]}"; do
    echo "🧪 測試子域名: $subdomain"
    
    # 嘗試啟動固定網址隧道
    ngrok http 4200 --url https://$subdomain.ngrok.io --log=stdout > /tmp/ngrok_fixed_$subdomain.log 2>&1 &
    FIXED_PID=$!
    
    sleep 3
    
    # 檢查是否成功
    if curl -s http://localhost:4040/api/tunnels > /dev/null 2>&1; then
        echo "✅ $subdomain 可用！"
        ACTUAL_FIXED_URL=$(curl -s http://localhost:4040/api/tunnels | grep -o '"public_url":"[^"]*"' | sed 's/"public_url":"//g' | sed 's/"//g' | head -1)
        echo "🎯 固定網址: $ACTUAL_FIXED_URL"
        
        # 停止隧道
        kill $FIXED_PID 2>/dev/null
        pkill ngrok 2>/dev/null
        sleep 2
        
        echo "💡 建議使用: $subdomain"
        break
    else
        echo "❌ $subdomain 不可用"
        kill $FIXED_PID 2>/dev/null
        pkill ngrok 2>/dev/null
        sleep 2
    fi
done

echo ""
echo "📊 診斷結果:"
echo "============"

if [ ! -z "$ACTUAL_FIXED_URL" ]; then
    echo "✅ 找到可用的固定網址: $ACTUAL_FIXED_URL"
    echo "💡 請更新 ngrok-config.sh 中的子域名設定"
else
    echo "❌ 無法找到可用的固定網址"
    echo "💡 可能的原因:"
    echo "   1. 帳戶尚未升級到付費版"
    echo "   2. 所有測試的子域名都已被使用"
    echo "   3. 需要聯繫 ngrok 支援"
fi

echo ""
echo "📝 日誌檔案:"
echo "   - 基本測試: /tmp/ngrok_test.log"
for subdomain in "${SUBDOMAINS[@]}"; do
    echo "   - $subdomain 測試: /tmp/ngrok_fixed_$subdomain.log"
done

echo ""
echo "🔗 有用的連結:"
echo "   - ngrok 儀表板: https://dashboard.ngrok.com/"
echo "   - 帳戶設定: https://dashboard.ngrok.com/get-started/your-authtoken"
echo "   - 付費升級: https://dashboard.ngrok.com/billing/subscription" 