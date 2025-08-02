#!/bin/bash
# 本地開發環境啟動腳本
# 此檔案的目的：啟動本地前端和後端服務，用於本地開發和測試
# 主要功能：檢查環境、清理端口、啟動前後端服務、提供狀態監控

set -e

# 顏色輸出
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}=======================================${NC}"
echo -e "${BLUE}🚀 FamilyTree 本地開發環境啟動${NC}"
echo -e "${BLUE}=======================================${NC}"

# 函數：日誌輸出
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 檢查是否在正確的目錄
if [ ! -d "familytree-backend" ] || [ ! -d "familytree-frontend" ]; then
    log_error "找不到後端或前端目錄"
    echo "請確保你在 FamilyTree 根目錄下運行此腳本"
    exit 1
fi

log_info "找到前後端目錄"

# 檢查 .NET 是否安裝
if ! command -v dotnet &> /dev/null; then
    log_error "找不到 dotnet 命令"
    echo "請確保已安裝 .NET Core 8 SDK"
    exit 1
fi

# 檢查 Node.js 是否安裝
if ! command -v node &> /dev/null; then
    log_error "找不到 node 命令"
    echo "請確保已安裝 Node.js"
    exit 1
fi

# 檢查 PostgreSQL 連接
log_info "檢查 PostgreSQL 連接..."
if ! psql -h localhost -U user -d familytree -c "SELECT 1;" > /dev/null 2>&1; then
    log_warn "無法連接到 PostgreSQL，請確保 PostgreSQL 服務已啟動"
    echo "資料庫連接: postgresql://user@localhost:5432/familytree"
fi

log_info "環境檢查通過"

# 清理可能佔用的端口
log_info "清理可能佔用的端口..."
lsof -ti :5088 | xargs kill -9 2>/dev/null || true
lsof -ti :4200 | xargs kill -9 2>/dev/null || true
sleep 2

# 定義服務 PID 變數
BACKEND_PID=""
FRONTEND_PID=""

# 函數：啟動後端
start_backend() {
    log_info "啟動後端服務 (.NET Core)..."
    cd familytree-backend
    
    # 檢查是否需要還原依賴
    if [ ! -d "bin" ] || [ ! -d "obj" ]; then
        log_info "還原 .NET 依賴..."
        if ! dotnet restore; then
            log_error "後端依賴還原失敗"
            exit 1
        fi
    fi
    
    # 啟動後端
    dotnet run --urls "http://localhost:5088" > ../backend.log 2>&1 &
    BACKEND_PID=$!
    cd ..
    
    # 檢查進程是否成功啟動
    if ! kill -0 $BACKEND_PID 2>/dev/null; then
        log_error "後端服務啟動失敗"
        log_error "請檢查 backend.log 檔案查看詳細錯誤"
        exit 1
    fi
    
    log_info "後端已啟動 (PID: $BACKEND_PID)"
    log_info "後端地址: http://localhost:5088"
    log_info "Swagger UI: http://localhost:5088/swagger"
}

# 函數：啟動前端
start_frontend() {
    log_info "啟動前端服務 (Angular)..."
    cd familytree-frontend
    
    # 檢查是否需要安裝依賴
    if [ ! -d "node_modules" ]; then
        log_info "安裝 npm 依賴..."
        if ! npm install; then
            log_error "前端依賴安裝失敗"
            exit 1
        fi
    fi
    
    # 啟動前端
    npm start > ../frontend.log 2>&1 &
    FRONTEND_PID=$!
    cd ..
    
    # 檢查進程是否成功啟動
    if ! kill -0 $FRONTEND_PID 2>/dev/null; then
        log_error "前端服務啟動失敗"
        log_error "請檢查 frontend.log 檔案查看詳細錯誤"
        exit 1
    fi
    
    log_info "前端已啟動 (PID: $FRONTEND_PID)"
    log_info "前端地址: http://localhost:4200"
}

# 函數：檢查服務狀態
check_services() {
    log_info "檢查服務狀態..."
    
    # 等待服務啟動
    sleep 5
    
    # 檢查後端
    local backend_retries=0
    local max_retries=10
    
    while [ $backend_retries -lt $max_retries ]; do
        if curl -s http://localhost:5088/api/project > /dev/null 2>&1; then
            log_info "✅ 後端服務正常運行"
            break
        else
            backend_retries=$((backend_retries + 1))
            if [ $backend_retries -eq $max_retries ]; then
                log_error "❌ 後端服務啟動失敗或無法連接"
                log_error "請檢查 backend.log 檔案查看詳細錯誤"
                cleanup
                exit 1
            fi
            log_warn "⚠️  後端服務還在啟動中... (嘗試 $backend_retries/$max_retries)"
            sleep 2
        fi
    done
    
    # 檢查前端
    local frontend_retries=0
    
    while [ $frontend_retries -lt $max_retries ]; do
        if curl -s http://localhost:4200 > /dev/null 2>&1; then
            log_info "✅ 前端服務正常運行"
            break
        else
            frontend_retries=$((frontend_retries + 1))
            if [ $frontend_retries -eq $max_retries ]; then
                log_error "❌ 前端服務啟動失敗或無法連接"
                log_error "請檢查 frontend.log 檔案查看詳細錯誤"
                cleanup
                exit 1
            fi
            log_warn "⚠️  前端服務還在啟動中... (嘗試 $frontend_retries/$max_retries)"
            sleep 2
        fi
    done
}

# 函數：清理進程
cleanup() {
    echo ""
    log_info "正在停止服務器..."
    
    if [ ! -z "$BACKEND_PID" ] && kill -0 $BACKEND_PID 2>/dev/null; then
        kill $BACKEND_PID 2>/dev/null
        log_info "✅ 後端已停止 (PID: $BACKEND_PID)"
    fi
    
    if [ ! -z "$FRONTEND_PID" ] && kill -0 $FRONTEND_PID 2>/dev/null; then
        kill $FRONTEND_PID 2>/dev/null
        log_info "✅ 前端已停止 (PID: $FRONTEND_PID)"
    fi
    
    # 強制清理端口
    lsof -ti :5088 | xargs kill -9 2>/dev/null || true
    lsof -ti :4200 | xargs kill -9 2>/dev/null || true
    
    echo -e "${BLUE}=======================================${NC}"
    echo -e "${GREEN}🛑 所有服務已停止${NC}"
    echo -e "${BLUE}=======================================${NC}"
    exit 0
}

# 設置信號處理
trap cleanup SIGINT SIGTERM

# 啟動服務器
start_backend
sleep 3
start_frontend

# 檢查服務狀態
check_services

echo ""
echo -e "${BLUE}=======================================${NC}"
echo -e "${GREEN}🎉 本地開發環境啟動完成！${NC}"
echo -e "${BLUE}=======================================${NC}"
echo ""
echo -e "${GREEN}📱 前端應用:${NC} http://localhost:4200"
echo -e "${GREEN}🔧 後端 API:${NC} http://localhost:5088"
echo -e "${GREEN}📖 API 文檔:${NC} http://localhost:5088/swagger"
echo -e "${GREEN}🗄️  資料庫:${NC} postgresql://user@localhost:5432/familytree"
echo ""
echo -e "${YELLOW}📋 日誌檔案:${NC}"
echo -e "   後端: ./backend.log"
echo -e "   前端: ./frontend.log"
echo ""
echo -e "${YELLOW}💡 提示:${NC}"
echo -e "   • 前端完全啟動可能需要 1-2 分鐘"
echo -e "   • 使用 ${GREEN}Ctrl+C${NC} 停止所有服務"
echo -e "   • 使用 ${GREEN}tail -f backend.log${NC} 查看後端日誌"
echo -e "   • 使用 ${GREEN}tail -f frontend.log${NC} 查看前端日誌"
echo ""
echo -e "${BLUE}=======================================${NC}"
echo -e "${GREEN}按 Ctrl+C 停止所有服務器${NC}"
echo -e "${BLUE}=======================================${NC}"

# 等待用戶中斷
wait 