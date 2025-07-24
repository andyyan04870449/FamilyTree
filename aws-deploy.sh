#!/bin/bash

# FamilyTree AWS 部署腳本
# 用途：在 AWS EC2 上安裝 Docker 並部署 FamilyTree 應用
# 主要功能：
# 1. 檢查本地環境和 AWS 連線
# 2. 安裝 Docker 和 Docker Compose 到 EC2
# 3. 上傳專案文件到 EC2
# 4. 在 AWS 上運行 Docker Compose 部署應用

set -e

# 配置變數
AWS_HOST="52.62.99.253"
AWS_USER="ubuntu"
KEY_FILE="POC.pem"
APP_DIR="/home/ubuntu/familytree"
PROJECT_NAME="familytree"

# 顏色輸出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=======================================${NC}"
echo -e "${BLUE}🚀 FamilyTree AWS 部署腳本${NC}"
echo -e "${BLUE}=======================================${NC}"
echo "時間: $(date)"
echo "目標主機: $AWS_HOST"
echo "用戶: $AWS_USER"
echo

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

# 函數：檢查本地環境
check_local_environment() {
    log_info "檢查本地環境..."
    
    # 檢查密鑰文件
    if [ ! -f "$KEY_FILE" ]; then
        log_error "密鑰文件 $KEY_FILE 不存在"
        exit 1
    fi
    
    # 設置密鑰文件權限
    chmod 400 "$KEY_FILE"
    log_info "密鑰文件權限已設置"
    
    # 檢查 AWS CLI
    if ! command -v aws &> /dev/null; then
        log_warn "AWS CLI 未安裝，請先安裝 AWS CLI"
    else
        log_info "AWS CLI 可用"
    fi
    
    # 檢查必要文件
    if [ ! -f "docker-compose.yml" ]; then
        log_error "docker-compose.yml 文件不存在"
        exit 1
    fi
    
    log_info "本地環境檢查完成"
}

# 函數：測試 SSH 連線
test_ssh_connection() {
    log_info "測試 SSH 連線..."
    
    if ssh -i "$KEY_FILE" -o ConnectTimeout=10 -o BatchMode=yes "$AWS_USER@$AWS_HOST" "echo 'SSH 連線成功'" 2>/dev/null; then
        log_info "SSH 連線測試成功"
    else
        log_error "SSH 連線失敗，請檢查網路和實例狀態"
        exit 1
    fi
}

# 函數：在 AWS 上安裝 Docker
install_docker_on_aws() {
    log_info "在 AWS EC2 上安裝 Docker..."
    
    ssh -i "$KEY_FILE" "$AWS_USER@$AWS_HOST" << 'EOF'
        # 更新系統
        sudo apt-get update -y
        
        # 安裝必要的套件
        sudo apt-get install -y \
            ca-certificates \
            curl \
            gnupg \
            lsb-release
            
        # 添加 Docker 官方 GPG key
        sudo mkdir -p /etc/apt/keyrings
        curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
        
        # 設置 Docker repository
        echo \
          "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
          $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
          
        # 更新 apt 包索引
        sudo apt-get update -y
        
        # 安裝 Docker Engine
        sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
        
        # 啟動 Docker 服務
        sudo systemctl start docker
        sudo systemctl enable docker
        
        # 將用戶添加到 docker 群組
        sudo usermod -aG docker ubuntu
        
        # 檢查 Docker 版本
        sudo docker --version
        sudo docker compose version
        
        echo "Docker 安裝完成"
EOF

    log_info "Docker 安裝完成"
}

# 函數：創建應用目錄
create_app_directory() {
    log_info "創建應用目錄..."
    
    ssh -i "$KEY_FILE" "$AWS_USER@$AWS_HOST" << EOF
        # 創建應用目錄
        mkdir -p $APP_DIR
        
        # 創建資料庫備份目錄
        mkdir -p $APP_DIR/database-backup
        
        # 設置目錄權限
        sudo chown -R ubuntu:ubuntu $APP_DIR
        
        echo "應用目錄創建完成: $APP_DIR"
EOF
}

# 函數：上傳專案文件
upload_project_files() {
    log_info "上傳專案文件到 AWS..."
    
    # 創建臨時排除文件
    cat > .rsync_exclude << 'EOF'
.git/
node_modules/
.angular/
dist/
bin/
obj/
*.log
.env
.DS_Store
*.swp
*.swo
*~
RelationalDiagram.pem
TreeTest.pem
*.pem
archive/
test-files/
EOF

    # 使用 rsync 上傳文件（排除不必要的文件）
    rsync -avz --progress \
        --exclude-from=.rsync_exclude \
        -e "ssh -i $KEY_FILE" \
        ./ "$AWS_USER@$AWS_HOST:$APP_DIR/"
    
    # 清理臨時文件
    rm -f .rsync_exclude
    
    log_info "專案文件上傳完成"
}

# 函數：準備資料庫
prepare_database() {
    log_info "準備資料庫初始化腳本..."
    
    ssh -i "$KEY_FILE" "$AWS_USER@$AWS_HOST" << EOF
        cd $APP_DIR
        
        # 複製資料庫 schema 到初始化目錄
        if [ -f "database_schema.sql" ]; then
            cp database_schema.sql database-backup/01-schema.sql
            echo "資料庫 schema 已準備"
        fi
        
        # 複製後端的資料庫腳本
        if [ -f "familytree-backend/database_schema.sql" ]; then
            cp familytree-backend/database_schema.sql database-backup/02-backend-schema.sql
            echo "後端資料庫 schema 已準備"
        fi
EOF
}

# 函數：部署應用
deploy_application() {
    log_info "部署應用..."
    
    ssh -i "$KEY_FILE" "$AWS_USER@$AWS_HOST" << EOF
        cd $APP_DIR
        
        # 停止現有的容器（如果存在）
        sudo docker compose down || true
        
        # 清理舊的 images（可選）
        # sudo docker system prune -f
        
        # 構建並啟動服務
        sudo docker compose up -d --build
        
        # 等待服務啟動
        echo "等待服務啟動..."
        sleep 30
        
        # 檢查服務狀態
        sudo docker compose ps
        
        echo "應用部署完成"
EOF
}

# 函數：檢查部署狀態
check_deployment_status() {
    log_info "檢查部署狀態..."
    
    ssh -i "$KEY_FILE" "$AWS_USER@$AWS_HOST" << EOF
        cd $APP_DIR
        
        echo "=== Docker 容器狀態 ==="
        sudo docker compose ps
        
        echo ""
        echo "=== 檢查服務日誌 ==="
        echo "PostgreSQL 日誌:"
        sudo docker compose logs --tail=10 postgres
        
        echo ""
        echo "後端服務日誌:"
        sudo docker compose logs --tail=10 familytree-backend
        
        echo ""
        echo "前端服務日誌:"
        sudo docker compose logs --tail=10 familytree-frontend
EOF

    log_info "檢查應用服務連通性..."
    
    # 檢查前端
    if curl -s -f "http://$AWS_HOST:4200" > /dev/null 2>&1; then
        log_info "✅ 前端服務 (4200) 正常運行"
    else
        log_warn "⚠️  前端服務 (4200) 無法訪問"
    fi
    
    # 檢查後端
    if curl -s -f "http://$AWS_HOST:5088" > /dev/null 2>&1; then
        log_info "✅ 後端服務 (5088) 正常運行"
    else
        log_warn "⚠️  後端服務 (5088) 無法訪問"
    fi
}

# 函數：顯示訪問信息
show_access_info() {
    echo
    echo -e "${BLUE}=======================================${NC}"
    echo -e "${BLUE}🎉 部署完成！${NC}"
    echo -e "${BLUE}=======================================${NC}"
    echo
    echo "應用訪問地址："
    echo "  • 前端: http://$AWS_HOST:4200"
    echo "  • 後端API: http://$AWS_HOST:5088"
    echo
    echo "SSH 連線："
    echo "  ssh -i $KEY_FILE $AWS_USER@$AWS_HOST"
    echo
    echo "管理命令："
    echo "  • 查看服務狀態: sudo docker compose ps"
    echo "  • 查看日誌: sudo docker compose logs -f [service_name]"
    echo "  • 重啟服務: sudo docker compose restart"
    echo "  • 停止服務: sudo docker compose down"
    echo
}

# 主要部署流程
main() {
    echo "開始部署流程..."
    
    check_local_environment
    test_ssh_connection
    install_docker_on_aws
    create_app_directory
    upload_project_files
    prepare_database
    deploy_application
    check_deployment_status
    show_access_info
    
    log_info "部署流程完成！"
}

# 執行主流程
main "$@" 