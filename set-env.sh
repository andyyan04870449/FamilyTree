#!/bin/bash

# FamilyTree 環境變數設定腳本
# 此腳本用於設定 FamilyTree 系統所需的環境變數

echo "🔧 設定 FamilyTree 環境變數..."

# 設定 OpenAI API Key
export OPENAI_API_KEY="your-openai-api-key-here"

# 設定其他可能需要的環境變數
export ASPNETCORE_ENVIRONMENT="Development"
export ASPNETCORE_URLS="http://localhost:5087"

# 驗證環境變數是否設定成功
echo "✅ 環境變數設定完成："
echo "  - OPENAI_API_KEY: ${OPENAI_API_KEY:0:20}..."
echo "  - ASPNETCORE_ENVIRONMENT: $ASPNETCORE_ENVIRONMENT"
echo "  - ASPNETCORE_URLS: $ASPNETCORE_URLS"

echo ""
echo "💡 提示："
echo "  - 這些環境變數只在當前終端會話中有效"
echo "  - 要永久設定，請將此腳本加入您的 shell 配置檔案"
echo "  - 或者使用 'source set-env.sh' 來載入環境變數"
echo ""
echo "🚀 現在可以啟動應用程式了！" 