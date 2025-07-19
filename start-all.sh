#!/bin/bash

echo "🚀 啟動家族樹全棧應用..."

# 檢查是否在正確的目錄
if [ ! -d "familytree-backend" ] || [ ! -d "familytree-frontend" ]; then
    echo "❌ 錯誤：找不到後端或前端目錄"
    echo "請確保你在 FamilyTree 根目錄下運行此腳本"
    exit 1
fi

echo "✅ 找到前後端目錄"

# 檢查 .NET 是否安裝
if ! command -v dotnet &> /dev/null; then
    echo "❌ 錯誤：找不到 dotnet 命令"
    echo "請確保已安裝 .NET Core 8 SDK"
    exit 1
fi

# 檢查 Node.js 是否安裝
if ! command -v node &> /dev/null; then
    echo "❌ 錯誤：找不到 node 命令"
    echo "請確保已安裝 Node.js"
    exit 1
fi

echo "✅ 環境檢查通過"

# 清理可能佔用的端口
echo "🧹 清理可能佔用的端口..."
lsof -ti :5087 | xargs kill -9 2>/dev/null
lsof -ti :4200 | xargs kill -9 2>/dev/null
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

# 函數：清理進程
cleanup() {
    echo ""
    echo "🛑 正在停止服務器..."
    if [ ! -z "$BACKEND_PID" ]; then
        kill $BACKEND_PID 2>/dev/null
        echo "✅ 後端已停止"
    fi
    if [ ! -z "$FRONTEND_PID" ]; then
        kill $FRONTEND_PID 2>/dev/null
        echo "✅ 前端已停止"
    fi
    exit 0
}

# 設置信號處理
trap cleanup SIGINT SIGTERM

# 啟動服務器
start_backend
sleep 3
start_frontend

echo ""
echo "🎉 服務器啟動完成！"
echo "📱 前端: http://localhost:4200"
echo "🔧 後端: http://localhost:5087"
echo "📊 API: http://localhost:5087/api/person"
echo ""
echo "按 Ctrl+C 停止所有服務器"

# 等待用戶中斷
wait 