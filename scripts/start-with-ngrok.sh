#!/bin/bash

# 此檔案的目的：整合啟動家族樹應用和 ngrok 隧道，提供完整的遠程訪問解決方案，並加入完善的日誌記錄
# 主要功能：同時啟動前後端服務和 ngrok 隧道，顯示公開網址，並提供詳細的狀態監控

# 日誌函數
log_info() {
    echo "📋 [$(date '+%Y-%m-%d %H:%M:%S')] ℹ️  $1"
}

log_success() {
    echo "✅ [$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

log_warning() {
    echo "⚠️  [$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

log_error() {
    echo "❌ [$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

log_step() {
    echo ""
    echo "🚀 [$(date '+%Y-%m-%d %H:%M:%S')] === $1 ==="
}

# 載入 ngrok 配置
load_ngrok_config() {
    log_step "載入 ngrok 配置"
    # 直接硬編碼token，不再需要外部配置文件
    export NGROK_AUTHTOKEN="2wcEhFbbW0lJlInEZPqKNzHhfFv_6EoK7JfA5h2BjoNca35yu"
    export FRONTEND_SUBDOMAIN="kunyou-poc-frontend"
    export BACKEND_SUBDOMAIN="kunyou-poc-backend"
    export FRONTEND_PORT="4200"
    export BACKEND_PORT="5087"
    export NGROK_UI_PORT="4040"
    export TUNNEL_INIT_DELAY="5"
    export MAX_RETRIES="5"
    export RETRY_DELAY="3"

    log_info "配置資訊："
    log_info "  前端端口: ${FRONTEND_PORT}"
    log_info "  後端端口: ${BACKEND_PORT}"
    log_info "  ngrok UI 端口: ${NGROK_UI_PORT}"
    log_info "  前端固定網址: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
    log_info "  後端固定網址: https://${BACKEND_SUBDOMAIN}.ngrok.io"
}

# 檢查環境
check_environment() {
    log_step "檢查運行環境"
    
    # 檢查是否在正確的目錄
    if [ ! -d "familytree-backend" ] || [ ! -d "familytree-frontend" ]; then
        log_error "找不到後端或前端目錄"
        log_error "請確保你在 FamilyTree 根目錄下運行此腳本"
        exit 1
    fi
    log_success "找到前後端目錄"

    # 檢查必要工具
    if ! command -v dotnet &> /dev/null; then
        log_error "找不到 dotnet 命令"
        log_error "請確保已安裝 .NET Core 8 SDK"
        exit 1
    fi
    log_success "✅ .NET Core SDK 已安裝: $(dotnet --version)"

    if ! command -v node &> /dev/null; then
        log_error "找不到 node 命令"
        log_error "請確保已安裝 Node.js"
        exit 1
    fi
    log_success "✅ Node.js 已安裝: $(node --version)"

    if ! command -v ngrok &> /dev/null; then
        log_error "找不到 ngrok 命令"
        log_error "請先安裝 ngrok: brew install ngrok/ngrok/ngrok"
        exit 1
    fi
    log_success "✅ ngrok 已安裝: $(ngrok version | head -1)"

    # 檢查 ngrok 配置
    if ngrok config check &> /dev/null; then
        log_success "✅ ngrok 配置檔案有效"
    else
        log_warning "ngrok 配置檔案無效，嘗試使用 authtoken 進行驗證..."
        ngrok config add-authtoken $NGROK_AUTHTOKEN
        if ngrok config check &> /dev/null; then
            log_success "✅ ngrok authtoken 設置成功"
        else
            log_error "ngrok authtoken 設置失敗，請手動檢查"
            exit 1
        fi
    fi
}

# 清理端口
cleanup_ports() {
    log_step "清理可能佔用的端口"
    
    # 檢查並清理後端端口
    if lsof -ti :${BACKEND_PORT} &> /dev/null; then
        log_warning "後端端口 ${BACKEND_PORT} 被佔用，正在清理..."
        lsof -ti :${BACKEND_PORT} | xargs kill -9 2>/dev/null
        sleep 2
        log_success "後端端口已清理"
    else
        log_info "後端端口 ${BACKEND_PORT} 空閒"
    fi
    
    # 檢查並清理前端端口
    if lsof -ti :${FRONTEND_PORT} &> /dev/null; then
        log_warning "前端端口 ${FRONTEND_PORT} 被佔用，正在清理..."
        lsof -ti :${FRONTEND_PORT} | xargs kill -9 2>/dev/null
        sleep 2
        log_success "前端端口已清理"
    else
        log_info "前端端口 ${FRONTEND_PORT} 空閒"
    fi
    
    # 清理 ngrok 進程
    if pgrep ngrok &> /dev/null; then
        log_warning "發現運行中的 ngrok 進程，正在清理..."
        pkill ngrok 2>/dev/null
        sleep 2
        log_success "ngrok 進程已清理"
    else
        log_info "無運行中的 ngrok 進程"
    fi
}

# 檢查服務健康狀態
check_service_health() {
    local service_name=$1
    local url=$2
    local max_attempts=10
    local attempt=1
    
    log_info "檢查 ${service_name} 健康狀態: ${url}"
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s --max-time 5 "$url" > /dev/null 2>&1; then
            log_success "${service_name} 健康檢查通過"
            return 0
        fi
        
        log_info "${service_name} 健康檢查第 ${attempt}/${max_attempts} 次失敗，等待重試..."
        sleep 3
        attempt=$((attempt + 1))
    done
    
    log_error "${service_name} 健康檢查失敗"
    return 1
}

# 函數：啟動後端
start_backend() {
    log_step "啟動後端服務器"
    
    cd familytree-backend || { log_error "無法進入後端目錄"; exit 1; }
    
    # 檢查項目文件
    if [ ! -f "familytree-backend.csproj" ]; then
        log_error "找不到後端項目文件"
        cd ..
        exit 1
    fi
    
    log_info "正在啟動 .NET Core 應用..."
    dotnet run --urls "http://localhost:${BACKEND_PORT}" > ../backend.log 2>&1 &
    BACKEND_PID=$!
    cd ..
    
    if [ ! -z "$BACKEND_PID" ]; then
        log_success "後端已啟動 (PID: $BACKEND_PID)"
        
        # 等待後端啟動完成
        log_info "等待後端服務啟動完成..."
        sleep 8
        
        # 檢查後端健康狀態
        if check_service_health "後端服務" "http://localhost:${BACKEND_PORT}/api"; then
            log_success "後端服務運行正常"
        else
            log_warning "後端服務可能未完全啟動，但繼續進行..."
        fi
    else
        log_error "後端啟動失敗"
        exit 1
    fi
}

# 函數：啟動前端
start_frontend() {
    log_step "啟動前端服務器"
    
    cd familytree-frontend || { log_error "無法進入前端目錄"; exit 1; }
    
    # 檢查項目文件
    if [ ! -f "package.json" ]; then
        log_error "找不到前端項目文件"
        cd ..
        exit 1
    fi
    
    # 檢查 node_modules
    if [ ! -d "node_modules" ]; then
        log_warning "找不到 node_modules，正在安裝依賴..."
        npm install
    fi
    
    log_info "正在啟動 Angular 應用..."
    npm start > ../frontend.log 2>&1 &
    FRONTEND_PID=$!
    cd ..
    
    if [ ! -z "$FRONTEND_PID" ]; then
        log_success "前端已啟動 (PID: $FRONTEND_PID)"
        
        # 等待前端啟動完成
        log_info "等待前端服務啟動完成..."
        sleep 15
        
        # 檢查前端健康狀態
        if check_service_health "前端服務" "http://localhost:${FRONTEND_PORT}"; then
            log_success "前端服務運行正常"
        else
            log_warning "前端服務可能未完全啟動，但繼續進行..."
        fi
    else
        log_error "前端啟動失敗"
        exit 1
    fi
}

# 函數：創建臨時 ngrok 配置
create_ngrok_config() {
    log_info "創建 ngrok 配置檔案..."
    
    cat > ./ngrok-temp.yml << EOF
version: "2"
authtoken: ${NGROK_AUTHTOKEN}
tunnels:
  frontend:
    addr: ${FRONTEND_PORT}
    proto: http
    host_header: localhost:${FRONTEND_PORT}
    url: https://${FRONTEND_SUBDOMAIN}.ngrok.io
  backend:
    addr: ${BACKEND_PORT}
    proto: http
    host_header: localhost:${BACKEND_PORT}
    url: https://${BACKEND_SUBDOMAIN}.ngrok.io
EOF
    
    log_success "配置檔案已創建: ./ngrok-temp.yml"
}

# 函數：啟動 ngrok 隧道
start_ngrok() {
    log_step "啟動 ngrok 固定網址隧道"
    
    # 確保 ngrok 進程已停止
    pkill -f ngrok 2>/dev/null
    sleep 2
    
    log_info "使用付費版固定網址功能"
    log_info "前端固定網址: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
    log_info "後端固定網址: https://${BACKEND_SUBDOMAIN}.ngrok.io"
    
    # 創建 ngrok 配置文件
    create_ngrok_config
    
    # 啟動單一 ngrok 會話，包含多個隧道
    log_info "啟動 ngrok 會話（包含前端和後端隧道）..."
    ngrok start --all --config=./ngrok-temp.yml > ngrok.log 2>&1 &
    NGROK_PID=$!
    
    if [ ! -z "$NGROK_PID" ]; then
        log_success "ngrok 固定網址隧道已啟動 (PID: $NGROK_PID)"
        log_info "等待隧道初始化..."
        sleep ${TUNNEL_INIT_DELAY}
    else
        log_error "ngrok 啟動失敗"
        exit 1
    fi
}

# 函數：顯示公開網址
show_public_urls() {
    log_step "獲取和驗證公開網址"
    
    # 增加等待時間，確保隧道完全建立
    sleep 10
    
    log_info "從 ngrok API 獲取實際固定網址..."
    
    # 重試機制：最多嘗試 5 次，每次間隔 3 秒
    RETRY_COUNT=0
    
    while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
        log_info "獲取固定網址 (第 $((RETRY_COUNT + 1))/${MAX_RETRIES} 次)..."
        
        # 獲取 ngrok API 響應
        TUNNELS_RESPONSE=$(curl -s http://localhost:${NGROK_UI_PORT}/api/tunnels 2>/dev/null)
        
        if [ ! -z "$TUNNELS_RESPONSE" ]; then
            # 使用 jq 解析 JSON（如果可用）
            if command -v jq &> /dev/null; then
                FRONTEND_URL=$(echo "$TUNNELS_RESPONSE" | jq -r '.tunnels[] | select(.name=="frontend") | .public_url' 2>/dev/null | head -1)
                BACKEND_URL=$(echo "$TUNNELS_RESPONSE" | jq -r '.tunnels[] | select(.name=="backend") | .public_url' 2>/dev/null | head -1)
            else
                # 備用方案：使用 grep 和 sed
                FRONTEND_URL=$(echo "$TUNNELS_RESPONSE" | grep -o '"public_url":"https://'${FRONTEND_SUBDOMAIN}'[^"]*"' | sed 's/"public_url":"//g;s/"//g' | head -1)
                BACKEND_URL=$(echo "$TUNNELS_RESPONSE" | grep -o '"public_url":"https://'${BACKEND_SUBDOMAIN}'[^"]*"' | sed 's/"public_url":"//g;s/"//g' | head -1)
            fi
            
            # 如果無法從 API 獲取，使用預設的固定網址
            if [ -z "$FRONTEND_URL" ]; then
                FRONTEND_URL="https://${FRONTEND_SUBDOMAIN}.ngrok.io"
            fi
            if [ -z "$BACKEND_URL" ]; then
                BACKEND_URL="https://${BACKEND_SUBDOMAIN}.ngrok.io"
            fi
                
            if [ ! -z "$FRONTEND_URL" ] && [ ! -z "$BACKEND_URL" ]; then
                log_success "成功獲取固定網址！"
                break
            fi
        fi
        
        log_warning "網址尚未就緒，等待重試..."
        sleep ${RETRY_DELAY}
        RETRY_COUNT=$((RETRY_COUNT + 1))
    done
    
    # 顯示最終結果
    echo ""
    echo "🌐 ===== 固定網址資訊 ===== 🌐"
    echo "========================================="
    
    if [ ! -z "$FRONTEND_URL" ]; then
        echo "📱 前端固定網址: $FRONTEND_URL"
        log_info "測試前端網址連通性..."
        if curl -s --max-time 10 "$FRONTEND_URL" > /dev/null 2>&1; then
            log_success "前端網址連通正常"
        else
            log_warning "前端網址可能尚未就緒，請稍後再試"
        fi
    else
        log_error "無法獲取前端固定網址"
    fi
    
    if [ ! -z "$BACKEND_URL" ]; then
        echo "🔧 後端固定網址: $BACKEND_URL"
        log_info "測試後端網址連通性..."
        if curl -s --max-time 10 "$BACKEND_URL" > /dev/null 2>&1; then
            log_success "後端網址連通正常"
        else
            log_warning "後端網址可能尚未就緒，請稍後再試"
        fi
    else
        log_error "無法獲取後端固定網址"
    fi
    
    echo ""
    echo "📊 其他資訊："
    echo "  ngrok 管理界面: http://localhost:${NGROK_UI_PORT}"
    echo "  本地前端: http://localhost:${FRONTEND_PORT}"
    echo "  本地後端: http://localhost:${BACKEND_PORT}"
    echo ""
    echo "💡 將前端固定網址分享給客戶即可！"
    echo "🔒 使用固定網址，每次重啟都不會變動！"
    echo "🎯 主要前端網址: $FRONTEND_URL"
    echo ""
    echo "📋 日誌檔案："
    echo "  後端日誌: backend.log"
    echo "  前端日誌: frontend.log"  
    echo "  ngrok 日誌: ngrok.log"
}

# 函數：監控服務狀態
monitor_services() {
    log_step "開始監控服務狀態"
    
    while true; do
        sleep 30  # 每30秒檢查一次
        
        # 檢查後端進程
        if ! kill -0 $BACKEND_PID 2>/dev/null; then
            log_error "後端進程意外停止！"
            break
        fi
        
        # 檢查前端進程
        if ! kill -0 $FRONTEND_PID 2>/dev/null; then
            log_error "前端進程意外停止！"
            break
        fi
        
        # 檢查 ngrok 進程
        if ! kill -0 $NGROK_PID 2>/dev/null; then
            log_error "ngrok 進程意外停止！"
            break
        fi
        
        log_info "所有服務運行正常 ✅"
    done
}

# 函數：清理進程
cleanup() {
    echo ""
    log_step "停止所有服務"
    
    # 停止後端
    if [ ! -z "$BACKEND_PID" ] && kill -0 $BACKEND_PID 2>/dev/null; then
        kill $BACKEND_PID 2>/dev/null
        log_success "後端已停止"
    fi
    
    # 停止前端
    if [ ! -z "$FRONTEND_PID" ] && kill -0 $FRONTEND_PID 2>/dev/null; then
        kill $FRONTEND_PID 2>/dev/null
        log_success "前端已停止"
    fi
    
    # 停止 ngrok 進程
    if [ ! -z "$NGROK_PID" ] && kill -0 $NGROK_PID 2>/dev/null; then
        kill $NGROK_PID 2>/dev/null
    fi
    pkill -f ngrok 2>/dev/null
    log_success "ngrok 已停止"
    
    # 清理端口
    lsof -ti :${BACKEND_PORT} | xargs kill -9 2>/dev/null
    lsof -ti :${FRONTEND_PORT} | xargs kill -9 2>/dev/null
    
    # 清理臨時檔案
    rm -f ./ngrok-temp.yml 2>/dev/null
    
    log_success "清理完成"
    exit 0
}

# 主執行流程
main() {
    echo "🚀 ===== FamilyTree 應用 + ngrok 固定網址隧道啟動程序 ===== 🚀"
    echo ""
    
    # 設置信號處理
    trap cleanup SIGINT SIGTERM
    
    # 載入配置
    load_ngrok_config
    
    # 檢查環境
    check_environment
    
    # 清理端口
    cleanup_ports
    
    # 啟動所有服務
    start_backend
    start_frontend
    start_ngrok
    
    # 顯示公開網址
    show_public_urls
    
    echo ""
    log_success "所有服務已啟動完成！"
    echo "按 Ctrl+C 停止所有服務"
    echo ""
    
    # 監控服務狀態
    monitor_services
    
    # 等待用戶中斷
    wait
}

# 執行主程序
main 