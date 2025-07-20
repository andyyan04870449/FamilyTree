#!/bin/bash

# 此檔案的目的：提供 ngrok 隧道服務，讓外部用戶可以透過公開網址訪問家族樹應用
# 主要功能：啟動 ngrok 隧道，暴露前端和後端服務端口

echo "🌐 啟動 ngrok 隧道服務..."

# 檢查 ngrok 是否安裝
if ! command -v ngrok &> /dev/null; then
    echo "❌ 錯誤：找不到 ngrok 命令"
    echo "請先安裝 ngrok: brew install ngrok/ngrok/ngrok"
    exit 1
fi

echo "✅ ngrok 已安裝"

# 檢查是否已經有 ngrok 進程在運行
if pgrep -x "ngrok" > /dev/null; then
    echo "⚠️  發現已有 ngrok 進程在運行，正在停止..."
    pkill ngrok
    sleep 2
fi

# 函數：清理進程
cleanup() {
    echo ""
    echo "🛑 正在停止 ngrok 隧道..."
    pkill ngrok
    echo "✅ ngrok 已停止"
    exit 0
}

# 設置信號處理
trap cleanup SIGINT SIGTERM

echo "🚀 啟動前端隧道 (端口 4200)..."
ngrok http 4200 --log=stdout --authtoken 2wcEhFbbW0lJlInEZPqKNzHhfFv_6EoK7JfA5h2BjoNca35yu --host-header=localhost:4200 &
FRONTEND_TUNNEL_PID=$!

echo "✅ 後端保持本地隱藏 (端口 5087)"

echo ""
echo "⏳ 等待隧道建立..."
sleep 5

echo ""
echo "🌐 ngrok 隧道已啟動！"
echo ""
echo "📱 前端公開網址:"
FRONTEND_URL=$(curl -s http://localhost:4040/api/tunnels | grep -o '"public_url":"[^"]*"' | cut -d'"' -f4)
if [ ! -z "$FRONTEND_URL" ]; then
    echo "$FRONTEND_URL"
    echo ""
    echo "💡 將此網址分享給客戶即可！"
else
    echo "❌ 無法獲取前端公開網址"
fi
echo ""
echo "📊 ngrok 管理界面: http://localhost:4040"
echo ""
echo "按 Ctrl+C 停止所有隧道"

# 等待用戶中斷
wait 