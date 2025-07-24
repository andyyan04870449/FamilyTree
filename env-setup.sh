#!/bin/bash

# 環境設定腳本
# 此檔案的目的：為不同開發環境設定不同的配置
# 主要功能：根據分支自動設定環境變數和配置

echo "⚙️  環境設定工具"
echo "================"

# 獲取當前分支
CURRENT_BRANCH=$(git branch --show-current)
echo "📍 當前分支: $CURRENT_BRANCH"

# 根據分支設定環境
case $CURRENT_BRANCH in
    "main")
        echo "🏭 設定生產環境配置..."
        export ENVIRONMENT="production"
        export NODE_ENV="production"
        export ASPNETCORE_ENVIRONMENT="Production"
        
        # 生產環境的 ngrok 配置
        export FRONTEND_SUBDOMAIN="familytree-frontend-prod"
        export BACKEND_SUBDOMAIN="familytree-backend-prod"
        
        echo "✅ 生產環境配置已載入"
        echo "📱 前端網址: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
        echo "🔧 後端網址: https://${BACKEND_SUBDOMAIN}.ngrok.io"
        ;;
    "development")
        echo "🔧 設定開發環境配置..."
        export ENVIRONMENT="development"
        export NODE_ENV="development"
        export ASPNETCORE_ENVIRONMENT="Development"
        
        # 開發環境的 ngrok 配置
        export FRONTEND_SUBDOMAIN="KUNYOU-POC-frontend"
        export BACKEND_SUBDOMAIN="KUNYOU-POC-backend"
        
        echo "✅ 開發環境配置已載入"
        echo "📱 前端網址: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
        echo "🔧 後端網址: https://${BACKEND_SUBDOMAIN}.ngrok.io"
        ;;
    *)
        echo "🧪 設定測試環境配置..."
        export ENVIRONMENT="testing"
        export NODE_ENV="development"
        export ASPNETCORE_ENVIRONMENT="Development"
        
        # 測試環境的 ngrok 配置
        export FRONTEND_SUBDOMAIN="familytree-frontend-test"
        export BACKEND_SUBDOMAIN="familytree-backend-test"
        
        echo "✅ 測試環境配置已載入"
        echo "📱 前端網址: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
        echo "🔧 後端網址: https://${BACKEND_SUBDOMAIN}.ngrok.io"
        ;;
esac

# 更新 ngrok 配置檔案
echo "📝 更新 ngrok 配置..."
cat > ./ngrok-config.sh << EOF
#!/bin/bash

# ngrok 配置文件 - $ENVIRONMENT 環境
# 此檔案的目的：管理 ngrok 隧道配置，包含固定網址設定
# 主要功能：集中管理 ngrok 的認證令牌和固定網址設定

# ngrok 認證令牌
export NGROK_AUTHTOKEN="2wcEhFbbW0lJlInEZPqKNzHhfFv_6EoK7JfA5h2BjoNca35yu"

# 固定網址設定（付費版功能）
export FRONTEND_SUBDOMAIN="$FRONTEND_SUBDOMAIN"
export BACKEND_SUBDOMAIN="$BACKEND_SUBDOMAIN"

# 本地服務端口
export FRONTEND_PORT="4200"
export BACKEND_PORT="5087"

# ngrok 管理界面端口
export NGROK_UI_PORT="4040"

# 隧道啟動延遲（秒）
export TUNNEL_START_DELAY="3"
export TUNNEL_INIT_DELAY="5"

# 重試設定
export MAX_RETRIES="5"
export RETRY_DELAY="3"

# 日誌設定
export NGROK_LOG_LEVEL="stdout"

echo "✅ ngrok 配置已載入 ($ENVIRONMENT 環境)"
echo "📱 前端固定網址: https://\${FRONTEND_SUBDOMAIN}.ngrok.io"
echo "🔧 後端固定網址: https://\${BACKEND_SUBDOMAIN}.ngrok.io"
EOF

echo "✅ ngrok 配置檔案已更新"

# 顯示環境資訊
echo ""
echo "📊 環境資訊:"
echo "============"
echo "🌿 分支: $CURRENT_BRANCH"
echo "🏷️  環境: $ENVIRONMENT"
echo "📱 前端網址: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
echo "🔧 後端網址: https://${BACKEND_SUBDOMAIN}.ngrok.io"
echo "🔧 前端端口: $FRONTEND_PORT"
echo "🔧 後端端口: $BACKEND_PORT"

echo ""
echo "💡 提示:"
echo "   - 使用 ./start-with-ngrok.sh 啟動服務"
echo "   - 使用 ./branch-manager.sh 管理分支"
echo "   - 使用 ./test-fixed-urls-working.sh 測試網址" 