#!/bin/bash

# 此檔案的目的：整合啟動家族樹應用和 ngrok 隧道，提供完整的遠程訪問解決方案
# 主要功能：同時啟動前後端服務和 ngrok 隧道，並顯示公開網址

echo "🚀 啟動家族樹應用 + ngrok 隧道..."

# 檢查是否在正確的目錄
if [ ! -d "familytree-backend" ] || [ ! -d "familytree-frontend" ]; then
    echo "❌ 錯誤：找不到後端或前端目錄"
    echo "請確保你在 FamilyTree 根目錄下運行此腳本"
    exit 1
fi

# 檢查必要工具
if ! command -v dotnet &> /dev/null; then
    echo "❌ 錯誤：找不到 dotnet 命令"
    echo "請確保已安裝 .NET Core 8 SDK"
    exit 1
fi

if ! command -v node &> /dev/null; then
    echo "❌ 錯誤：找不到 node 命令"
    echo "請確保已安裝 Node.js"
    exit 1
fi

if ! command -v ngrok &> /dev/null; then
    echo "❌ 錯誤：找不到 ngrok 命令"
    echo "請先安裝 ngrok: brew install ngrok/ngrok/ngrok"
    exit 1
fi

echo "✅ 環境檢查通過"

# 清理可能佔用的端口
echo "🧹 清理可能佔用的端口..."
lsof -ti :5087 | xargs kill -9 2>/dev/null
lsof -ti :4200 | xargs kill -9 2>/dev/null
pkill ngrok 2>/dev/null
sleep 2

# 函數：啟動後端
start_backend() {
    echo "🔧 啟動後端服務器..."
    cd familytree-backend
    dotnet run --urls "http://localhost:5087" &
    BACKEND_PID=$!
    cd ..
    echo "✅ 後端已啟動 (PID: $BACKEND_PID)"
}

# 函數：啟動前端
start_frontend() {
    echo "🎨 啟動前端服務器..."
    cd familytree-frontend
    npm start &
    FRONTEND_PID=$!
    cd ..
    echo "✅ 前端已啟動 (PID: $FRONTEND_PID)"
}

# 函數：啟動 ngrok 隧道
start_ngrok() {
    echo "🌐 啟動 ngrok 隧道..."
    
    # 啟動前端隧道
    ngrok http 4200 --log=stdout --authtoken 2wcEhFbbW0lJlInEZPqKNzHhfFv_6EoK7JfA5h2BjoNca35yu --host-header=localhost:4200 > /dev/null 2>&1 &
    FRONTEND_TUNNEL_PID=$!
    
    # 啟動後端隧道
    ngrok http 5087 --log=stdout --authtoken 2wcEhFbbW0lJlInEZPqKNzHhfFv_6EoK7JfA5h2BjoNca35yu --host-header=localhost:5087 > /dev/null 2>&1 &
    BACKEND_TUNNEL_PID=$!
    
    echo "✅ ngrok 隧道已啟動"
}

# 函數：顯示公開網址
show_public_urls() {
    echo ""
    echo "⏳ 等待隧道建立..."
    sleep 8
    
    echo ""
    echo "🌐 公開網址資訊："
    echo "=================="
    
    # 獲取前端公開網址
    FRONTEND_URL=$(curl -s http://localhost:4040/api/tunnels | grep -o '"public_url":"[^"]*"' | grep "4200" | cut -d'"' -f4)
    if [ ! -z "$FRONTEND_URL" ]; then
        echo "📱 前端公開網址: $FRONTEND_URL"
    else
        echo "❌ 無法獲取前端公開網址"
    fi
    
    # 獲取後端公開網址
    BACKEND_URL=$(curl -s http://localhost:4040/api/tunnels | grep -o '"public_url":"[^"]*"' | grep "5087" | cut -d'"' -f4)
    if [ ! -z "$BACKEND_URL" ]; then
        echo "🔧 後端公開網址: $BACKEND_URL"
    else
        echo "❌ 無法獲取後端公開網址"
    fi
    
    echo ""
    echo "📊 ngrok 管理界面: http://localhost:4040"
    echo "🏠 本地前端: http://localhost:4200"
    echo "🔧 本地後端: http://localhost:5087"
    echo ""
    echo "💡 將前端公開網址分享給客戶即可！"
}

# 函數：清理進程
cleanup() {
    echo ""
    echo "🛑 正在停止所有服務..."
    if [ ! -z "$BACKEND_PID" ]; then
        kill $BACKEND_PID 2>/dev/null
        echo "✅ 後端已停止"
    fi
    if [ ! -z "$FRONTEND_PID" ]; then
        kill $FRONTEND_PID 2>/dev/null
        echo "✅ 前端已停止"
    fi
    pkill ngrok 2>/dev/null
    echo "✅ ngrok 已停止"
    exit 0
}

# 設置信號處理
trap cleanup SIGINT SIGTERM

# 啟動所有服務
start_backend
sleep 3
start_frontend
sleep 5
start_ngrok

# 顯示公開網址
show_public_urls

echo ""
echo "按 Ctrl+C 停止所有服務"

# 等待用戶中斷
wait 