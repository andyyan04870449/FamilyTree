#!/bin/bash

echo "🚀 啟動家族樹前端服務器..."

# 檢查是否在正確的目錄
if [ ! -d "familytree-frontend" ]; then
    echo "❌ 錯誤：找不到 familytree-frontend 目錄"
    echo "請確保你在 FamilyTree 根目錄下運行此腳本"
    exit 1
fi

# 檢查端口是否被佔用
PORT=4200
if lsof -i :$PORT > /dev/null 2>&1; then
    echo "⚠️  端口 $PORT 已被佔用，正在停止現有進程..."
    lsof -ti :$PORT | xargs kill -9 2>/dev/null
    sleep 2
fi

# 進入前端目錄
cd familytree-frontend

echo "📁 進入前端目錄: $(pwd)"

# 檢查 Node.js 是否安裝
if ! command -v node &> /dev/null; then
    echo "❌ 錯誤：找不到 node 命令"
    echo "請確保已安裝 Node.js"
    exit 1
fi

echo "✅ Node.js 已安裝: $(node --version)"

# 檢查 npm 是否安裝
if ! command -v npm &> /dev/null; then
    echo "❌ 錯誤：找不到 npm 命令"
    echo "請確保已安裝 npm"
    exit 1
fi

echo "✅ npm 已安裝: $(npm --version)"

# 檢查 package.json 是否存在
if [ ! -f "package.json" ]; then
    echo "❌ 錯誤：找不到 package.json 文件"
    exit 1
fi

echo "✅ 找到 package.json"

# 檢查 node_modules 是否存在
if [ ! -d "node_modules" ]; then
    echo "📦 安裝依賴包..."
    npm install
else
    echo "✅ 依賴包已安裝"
fi

# 啟動開發服務器
echo "🌐 啟動前端服務器在 http://localhost:$PORT..."
echo "按 Ctrl+C 停止服務器"
echo ""

npm start 