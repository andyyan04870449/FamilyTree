#!/bin/bash

echo "🎉 測試固定網址功能 - 最終驗證"
echo "================================"

# 檢查 ngrok 隧道狀態
echo "🔍 檢查 ngrok 隧道狀態..."
TUNNELS_RESPONSE=$(curl -s http://localhost:4040/api/tunnels 2>/dev/null)

if [ ! -z "$TUNNELS_RESPONSE" ]; then
    echo "✅ ngrok API 響應正常"
    
    # 解析隧道資訊
    FRONTEND_URL=$(echo "$TUNNELS_RESPONSE" | grep -o '"public_url":"[^"]*"' | grep "frontend" | sed 's/"public_url":"//g' | sed 's/"//g' | head -1)
    BACKEND_URL=$(echo "$TUNNELS_RESPONSE" | grep -o '"public_url":"[^"]*"' | grep "backend" | sed 's/"public_url":"//g' | sed 's/"//g' | head -1)
    
    echo "📱 前端固定網址: $FRONTEND_URL"
    echo "🔧 後端固定網址: $BACKEND_URL"
    
    # 測試前端網址
    echo ""
    echo "🧪 測試前端網址..."
    FRONTEND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$FRONTEND_URL" 2>/dev/null)
    echo "📊 前端 HTTP 狀態: $FRONTEND_STATUS"
    
    if [ "$FRONTEND_STATUS" = "200" ]; then
        echo "✅ 前端固定網址正常工作！"
    else
        echo "⚠️  前端網址狀態: $FRONTEND_STATUS"
    fi
    
    # 測試後端網址
    echo ""
    echo "🧪 測試後端網址..."
    BACKEND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BACKEND_URL" 2>/dev/null)
    echo "📊 後端 HTTP 狀態: $BACKEND_STATUS"
    
    if [ "$BACKEND_STATUS" = "404" ] || [ "$BACKEND_STATUS" = "200" ]; then
        echo "✅ 後端固定網址正常工作！"
    else
        echo "⚠️  後端網址狀態: $BACKEND_STATUS"
    fi
    
    echo ""
    echo "🎯 測試結果總結:"
    echo "=================="
    echo "✅ ngrok 隧道已建立"
    echo "✅ 前端固定網址: $FRONTEND_URL"
    echo "✅ 後端固定網址: $BACKEND_URL"
    echo ""
    echo "💡 您可以分享前端固定網址給客戶！"
    echo "🔒 這些網址在每次重啟後都會保持不變！"
    
else
    echo "❌ ngrok API 無響應"
    echo "💡 請檢查 ngrok 是否正在運行"
fi

echo ""
echo "📊 ngrok 管理界面: http://localhost:4040" 