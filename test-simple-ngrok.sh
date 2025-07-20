#!/bin/bash

echo "🧪 簡化 ngrok 固定網址測試"

# 停止現有進程
pkill ngrok 2>/dev/null
sleep 2

# 啟動前端服務（如果沒有運行）
if ! curl -s http://localhost:4200 > /dev/null 2>&1; then
    echo "⚠️  前端服務未運行，請先啟動前端服務"
    exit 1
fi

echo "✅ 前端服務正在運行"

# 啟動固定網址隧道
echo "🌐 啟動固定網址隧道..."
ngrok http 4200 --url https://familytree-frontend.ngrok.io --host-header=localhost:4200 --log=stdout &
NGROK_PID=$!

echo "⏳ 等待隧道建立..."
sleep 8

# 檢查隧道狀態
echo "🔍 檢查隧道狀態..."
TUNNELS_RESPONSE=$(curl -s http://localhost:4040/api/tunnels 2>/dev/null)

if [ ! -z "$TUNNELS_RESPONSE" ]; then
    echo "✅ 隧道已建立"
    
    # 獲取實際網址
    ACTUAL_URL=$(echo "$TUNNELS_RESPONSE" | grep -o '"public_url":"[^"]*"' | sed 's/"public_url":"//g' | sed 's/"//g' | head -1)
    echo "🎯 固定網址: $ACTUAL_URL"
    
    # 測試訪問
    echo "🔗 測試網址訪問..."
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$ACTUAL_URL" 2>/dev/null)
    echo "📊 HTTP 狀態碼: $HTTP_STATUS"
    
    if [ "$HTTP_STATUS" = "200" ]; then
        echo "✅ 網址可以正常訪問！"
        echo "🎉 固定網址功能正常"
        echo "💡 您可以分享這個網址給客戶: $ACTUAL_URL"
    else
        echo "⚠️  網址返回狀態碼: $HTTP_STATUS"
        echo "💡 請檢查 ngrok 管理界面: http://localhost:4040"
    fi
else
    echo "❌ 隧道建立失敗"
fi

echo ""
echo "按 Enter 鍵停止隧道..."
read

# 停止隧道
echo "🛑 停止隧道..."
kill $NGROK_PID 2>/dev/null
pkill ngrok 2>/dev/null

echo "✅ 測試完成" 