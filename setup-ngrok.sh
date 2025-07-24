#!/bin/bash

# FamilyTree ngrok 安裝與設定腳本
# 用途：在 AWS EC2 上安裝並設定 ngrok，提供固定的公開網址
# 主要功能：
# 1. 自動下載並安裝 ngrok
# 2. 使用提供的 Authtoken 進行認證
# 3. 創建一個 systemd 服務，確保 ngrok 開機自動啟動

set -e

# --- 配置 ---
NGROK_AUTHTOKEN="2wcEhFbbW0lJlInEZPqKNzHhfFv_6EoK7JfA5h2BjoNca35yu"
FRONTEND_SUBDOMAIN="KUNYOU-POC-frontend"
BACKEND_SUBDOMAIN="KUNYOU-POC-backend"

# 本地服務的端口（由 docker-compose.yml 決定）
LOCAL_FRONTEND_PORT="4200"
LOCAL_BACKEND_PORT="5088"

# 顏色輸出
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=======================================${NC}"
echo -e "${BLUE}🚀 ngrok 安裝與設定腳本 for AWS${NC}"
echo -e "${BLUE}=======================================${NC}"

# 函數：日誌輸出
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

# 1. 安裝 ngrok
log_info "正在下載並安裝 ngrok..."
curl -s https://ngrok-agent.s3.amazonaws.com/ngrok.asc | sudo tee /etc/apt/trusted.gpg.d/ngrok.asc >/dev/null
echo "deb https://ngrok-agent.s3.amazonaws.com buster main" | sudo tee /etc/apt/sources.list.d/ngrok.list
sudo apt-get update
sudo apt-get install -y ngrok
log_info "ngrok 安裝完成"

# 2. 設定 Authtoken
log_info "設定 ngrok Authtoken..."
ngrok config add-authtoken ${NGROK_AUTHTOKEN}
log_info "Authtoken 設定完成"

# 3. 創建 ngrok 設定檔
log_info "創建 ngrok 設定檔..."
sudo mkdir -p /etc/ngrok
sudo tee /etc/ngrok/config.yml > /dev/null <<EOF
version: "2"
authtoken: ${NGROK_AUTHTOKEN}
tunnels:
  frontend:
    proto: http
    addr: ${LOCAL_FRONTEND_PORT}
    domain: ${FRONTEND_SUBDOMAIN}.ngrok.io
  backend:
    proto: http
    addr: ${LOCAL_BACKEND_PORT}
    domain: ${BACKEND_SUBDOMAIN}.ngrok.io
EOF
log_info "ngrok 設定檔已創建於 /etc/ngrok/config.yml"

# 4. 創建 systemd 服務
log_info "創建 ngrok systemd 服務..."
sudo tee /etc/systemd/system/ngrok.service > /dev/null <<EOF
[Unit]
Description=ngrok Tunnel Service
After=network.target docker.service
Requires=docker.service

[Service]
User=ubuntu
ExecStart=/usr/bin/ngrok start --all --config /etc/ngrok/config.yml
ExecReload=/bin/kill -HUP \$MAINPID
Restart=always
RestartSec=15
KillMode=process

[Install]
WantedBy=multi-user.target
EOF
log_info "systemd 服務檔案已創建"

# 5. 啟用並啟動 ngrok 服務
log_info "啟用並啟動 ngrok 服務..."
sudo systemctl enable ngrok.service
sudo systemctl daemon-reload
sudo systemctl restart ngrok.service
log_info "ngrok 服務已啟動並設定為開機自啟"

# 6. 檢查服務狀態
log_info "正在檢查 ngrok 服務狀態..."
sleep 5
sudo systemctl status ngrok.service --no-pager

echo -e "${BLUE}=======================================${NC}"
echo -e "${GREEN}🎉 ngrok 設定完成！${NC}"
echo
echo "您的應用程式現在可以透過以下固定網址訪問："
echo "  • 前端: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
echo "  • 後端 API: https://${BACKEND_SUBDOMAIN}.ngrok.io"
echo
echo "ngrok 服務將在背景持續運行，並在伺服器重啟後自動啟動。"
echo -e "${BLUE}=======================================${NC}" 