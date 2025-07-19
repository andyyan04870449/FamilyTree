#!/bin/bash

echo "🚀 啟動家族樹後端服務器..."

# 檢查是否在正確的目錄
if [ ! -d "familytree-backend" ]; then
    echo "❌ 錯誤：找不到 familytree-backend 目錄"
    echo "請確保你在 FamilyTree 根目錄下運行此腳本"
    exit 1
fi

# 檢查端口是否被佔用
PORT=5087
if lsof -i :$PORT > /dev/null 2>&1; then
    echo "⚠️  端口 $PORT 已被佔用，正在停止現有進程..."
    lsof -ti :$PORT | xargs kill -9 2>/dev/null
    sleep 2
fi

# 進入後端目錄
cd familytree-backend

echo "📁 進入後端目錄: $(pwd)"

# 檢查 .NET 是否安裝
if ! command -v dotnet &> /dev/null; then
    echo "❌ 錯誤：找不到 dotnet 命令"
    echo "請確保已安裝 .NET Core 8 SDK"
    exit 1
fi

echo "✅ .NET 已安裝: $(dotnet --version)"

# 檢查項目文件是否存在
if [ ! -f "familytree-backend.csproj" ]; then
    echo "❌ 錯誤：找不到 familytree-backend.csproj 文件"
    exit 1
fi

echo "✅ 找到項目文件"

# 恢復依賴包
echo "📦 恢復 NuGet 包..."
dotnet restore

# 構建項目
echo "🔨 構建項目..."
dotnet build

# 啟動服務器
echo "🌐 啟動服務器在 http://localhost:$PORT..."
echo "按 Ctrl+C 停止服務器"
echo ""

dotnet run --urls "http://localhost:$PORT" 