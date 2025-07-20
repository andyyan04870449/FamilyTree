#!/bin/bash

# ngrok 配置文件 - development 環境
# 此檔案的目的：管理 ngrok 隧道配置，包含固定網址設定
# 主要功能：集中管理 ngrok 的認證令牌和固定網址設定

# ngrok 認證令牌
export NGROK_AUTHTOKEN="2wcEhFbbW0lJlInEZPqKNzHhfFv_6EoK7JfA5h2BjoNca35yu"

# 固定網址設定（付費版功能）
export FRONTEND_SUBDOMAIN="familytree-frontend-dev"
export BACKEND_SUBDOMAIN="familytree-backend-dev"

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

echo "✅ ngrok 配置已載入 (development 環境)"
echo "📱 前端固定網址: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
echo "🔧 後端固定網址: https://${BACKEND_SUBDOMAIN}.ngrok.io"
