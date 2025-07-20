#!/bin/bash

# 測試固定網址功能
# 此檔案的目的：測試 ngrok 固定網址功能是否正常工作
# 主要功能：啟動單個隧道並驗證固定網址

echo "🧪 測試 ngrok 固定網址功能..."

# 載入配置
if [ -f "ngrok-config.sh" ]; then
    source ngrok-config.sh
else
    echo "❌ 找不到 ngrok-config.sh"
    exit 1
fi

# 停止現有的 ngrok 進程
echo "🛑 停止現有的 ngrok 進程..."
pkill -f ngrok 2>/dev/null
sleep 2

# 測試前端固定網址
echo "📱 測試前端固定網址: ${FRONTEND_SUBDOMAIN}.ngrok.io"
ngrok http ${FRONTEND_PORT} --url ${FRONTEND_SUBDOMAIN}.ngrok.io > /dev/null 2>&1 &
TUNNEL_PID=$!

echo "⏳ 等待隧道建立..."
sleep 8

# 檢查隧道狀態
echo "🔍 檢查隧道狀態..."
TUNNELS_RESPONSE=$(curl -s http://localhost:${NGROK_UI_PORT}/api/tunnels 2>/dev/null)

if [ ! -z "$TUNNELS_RESPONSE" ]; then
    echo "✅ ngrok API 響應正常"
    
    # 解析網址
    if command -v jq &> /dev/null; then
        ACTUAL_URL=$(echo "$TUNNELS_RESPONSE" | jq -r '.tunnels[0].public_url' 2>/dev/null)
    else
        ACTUAL_URL=$(echo "$TUNNELS_RESPONSE" | grep -o '"public_url":"[^"]*"' | sed 's/"public_url":"//g' | sed 's/"//g' | head -1)
    fi
    
    if [ ! -z "$ACTUAL_URL" ]; then
        echo "🎯 實際網址: $ACTUAL_URL"
        echo "🔗 測試訪問網址..."
        
        # 測試網址是否可訪問
        HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$ACTUAL_URL" 2>/dev/null)
        
        if [ "$HTTP_STATUS" = "200" ] || [ "$HTTP_STATUS" = "404" ]; then
            echo "✅ 網址可以訪問 (HTTP狀態: $HTTP_STATUS)"
        else
            echo "⚠️  網址可能無法訪問 (HTTP狀態: $HTTP_STATUS)"
        fi
    else
        echo "❌ 無法解析網址"
    fi
else
    echo "❌ ngrok API 無響應"
fi

# 停止測試隧道
echo "🛑 停止測試隧道..."
kill $TUNNEL_PID 2>/dev/null
pkill -f ngrok 2>/dev/null

echo "🧪 測試完成" 