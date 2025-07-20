#!/bin/bash

# FamilyTree 環境變數載入腳本
# 此腳本用於從 .env 檔案載入環境變數

echo "🔧 載入 FamilyTree 環境變數..."

# 檢查 .env 檔案是否存在
if [ ! -f ".env" ]; then
    echo "❌ 錯誤：找不到 .env 檔案"
    echo "請先創建 .env 檔案並設定必要的環境變數"
    exit 1
fi

# 載入 .env 檔案
echo "📄 載入 .env 檔案..."
while IFS= read -r line; do
    # 跳過註釋和空行
    if [[ $line =~ ^[[:space:]]*# ]] || [[ -z $line ]]; then
        continue
    fi
    
    # 設定環境變數
    export "$line"
    echo "  ✅ 載入: ${line%%=*}"
done < .env

echo ""
echo "✅ 環境變數載入完成！"
echo "  - OPENAI_API_KEY: ${OPENAI_API_KEY:0:20}..."
echo "  - ASPNETCORE_ENVIRONMENT: $ASPNETCORE_ENVIRONMENT"
echo "  - ASPNETCORE_URLS: $ASPNETCORE_URLS"

echo ""
echo "💡 提示："
echo "  - 使用 'source load-env.sh' 來載入環境變數"
echo "  - 或者使用 './set-env.sh' 直接設定環境變數"
echo ""
echo "🚀 現在可以啟動應用程式了！" 