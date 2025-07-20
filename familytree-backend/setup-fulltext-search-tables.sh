#!/bin/bash

# 全文檢索功能資料庫初始化腳本
# 此檔案的目的：自動執行全文檢索功能的資料庫表建立和索引優化

echo "🚀 開始初始化全文檢索功能資料庫表..."

# 設定資料庫連接參數（可根據需要修改）
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-familytree}"
DB_USER="${DB_USER:-postgres}"
DB_PASSWORD="${DB_PASSWORD:-}"

# 顏色輸出函數
log_info() {
    echo -e "\033[34m📋 [INFO]\033[0m $1"
}

log_success() {
    echo -e "\033[32m✅ [SUCCESS]\033[0m $1"
}

log_error() {
    echo -e "\033[31m❌ [ERROR]\033[0m $1"
}

log_warning() {
    echo -e "\033[33m⚠️  [WARNING]\033[0m $1"
}

# 檢查 PostgreSQL 是否可連接
check_database_connection() {
    log_info "檢查資料庫連接..."
    
    if command -v psql &> /dev/null; then
        if PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT version();" &> /dev/null; then
            log_success "資料庫連接成功"
            return 0
        else
            log_error "無法連接到資料庫"
            log_error "請檢查連接參數：Host=$DB_HOST, Port=$DB_PORT, DB=$DB_NAME, User=$DB_USER"
            return 1
        fi
    else
        log_error "找不到 psql 命令，請確保 PostgreSQL 客戶端已安裝"
        return 1
    fi
}

# 執行 SQL 文件
execute_sql_file() {
    local sql_file=$1
    local description=$2
    
    log_info "執行 $description..."
    
    if [ -f "$sql_file" ]; then
        if PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f "$sql_file"; then
            log_success "$description 完成"
            return 0
        else
            log_error "$description 失敗"
            return 1
        fi
    else
        log_error "找不到 SQL 文件: $sql_file"
        return 1
    fi
}

# 執行 SQL 命令
execute_sql_command() {
    local sql_command="$1"
    local description="$2"
    
    log_info "執行 $description..."
    
    if PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "$sql_command"; then
        log_success "$description 完成"
        return 0
    else
        log_error "$description 失敗"
        return 1
    fi
}

# 檢查表是否存在
check_table_exists() {
    local table_name=$1
    local exists=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '$table_name');")
    
    if [[ "$exists" =~ "t" ]]; then
        return 0
    else
        return 1
    fi
}

# 主要安裝流程
main() {
    echo "============================================"
    echo "🔍 全文檢索功能資料庫初始化"
    echo "============================================"
    echo "Host: $DB_HOST:$DB_PORT"
    echo "Database: $DB_NAME"
    echo "User: $DB_USER"
    echo "============================================"
    
    # 檢查資料庫連接
    if ! check_database_connection; then
        exit 1
    fi
    
    # 執行主要的建表 SQL
    if ! execute_sql_file "create_fulltext_search_tables.sql" "建立全文檢索相關資料表"; then
        log_error "建表失敗，安裝中止"
        exit 1
    fi
    
    # 檢查重要的表是否建立成功
    log_info "驗證資料表建立狀態..."
    
    tables=("search_keywords" "user_favorites" "search_logs")
    all_tables_exist=true
    
    for table in "${tables[@]}"; do
        if check_table_exists "$table"; then
            log_success "資料表 $table 建立成功"
        else
            log_error "資料表 $table 建立失敗"
            all_tables_exist=false
        fi
    done
    
    if [ "$all_tables_exist" = false ]; then
        log_error "部分資料表建立失敗，請檢查錯誤訊息"
        exit 1
    fi
    
    # 檢查和優化索引
    log_info "驗證索引建立狀態..."
    
    # 檢查主要索引是否存在
    indexes=(
        "idx_keyword"
        "idx_search_count" 
        "idx_person_id"
        "idx_person_name_search"
        "idx_person_fulltext_search"
    )
    
    for index in "${indexes[@]}"; do
        index_exists=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = '$index');")
        
        if [[ "$index_exists" =~ "t" ]]; then
            log_success "索引 $index 建立成功"
        else
            log_warning "索引 $index 可能未建立，但這不影響基本功能"
        fi
    done
    
    # 插入測試資料（可選）
    if [ "$1" = "--with-test-data" ]; then
        log_info "插入測試資料..."
        
        # 檢查是否已有測試資料
        test_data_count=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM search_keywords WHERE keyword IN ('北大', '台科大', '上海');")
        
        if [ "$test_data_count" -gt 0 ]; then
            log_info "測試資料已存在，跳過插入"
        else
            execute_sql_command "
                INSERT INTO search_keywords (keyword, search_count, search_type, last_search_time) VALUES
                ('北大', 5, 'fuzzy', CURRENT_TIMESTAMP),
                ('台科大', 4, 'fuzzy', CURRENT_TIMESTAMP),
                ('上海', 3, 'fuzzy', CURRENT_TIMESTAMP),
                ('廣告大學', 3, 'fuzzy', CURRENT_TIMESTAMP),
                ('東京電信', 2, 'fuzzy', CURRENT_TIMESTAMP),
                ('中國總商會', 2, 'fuzzy', CURRENT_TIMESTAMP)
                ON CONFLICT (keyword) DO NOTHING;
            " "插入測試關鍵字資料"
        fi
    fi
    
    # 執行 VACUUM 和 ANALYZE 優化
    log_info "執行資料庫優化..."
    execute_sql_command "VACUUM ANALYZE search_keywords, user_favorites, search_logs;" "資料庫優化"
    
    # 顯示統計資訊
    log_info "顯示建立完成的資料表統計..."
    
    for table in "${tables[@]}"; do
        count=$(PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -t -c "SELECT COUNT(*) FROM $table;")
        log_info "資料表 $table: $count 筆記錄"
    done
    
    echo ""
    echo "============================================"
    log_success "全文檢索功能資料庫初始化完成！"
    echo "============================================"
    echo ""
    echo "📋 接下來的步驟："
    echo "1. 啟動後端服務: dotnet run"
    echo "2. 啟動前端服務: ng serve"
    echo "3. 訪問全文檢索頁面測試功能"
    echo ""
    echo "🔧 API 端點："
    echo "- 搜索: POST /api/FullTextSearch/search"
    echo "- 收藏: GET/POST/DELETE /api/Favorites"
    echo "- 熱門關鍵字: GET /api/FullTextSearch/popular-keywords"
    echo ""
    echo "📖 更多資訊請參考 FULL_TEXT_SEARCH_GUIDE.md"
    echo ""
}

# 顯示幫助資訊
show_help() {
    echo "全文檢索功能資料庫初始化腳本"
    echo ""
    echo "用法: $0 [選項]"
    echo ""
    echo "選項:"
    echo "  --with-test-data    同時插入測試資料"
    echo "  --help             顯示此幫助資訊"
    echo ""
    echo "環境變數:"
    echo "  DB_HOST            資料庫主機 (預設: localhost)"
    echo "  DB_PORT            資料庫埠號 (預設: 5432)"
    echo "  DB_NAME            資料庫名稱 (預設: familytree)"
    echo "  DB_USER            資料庫使用者 (預設: postgres)"
    echo "  DB_PASSWORD        資料庫密碼"
    echo ""
    echo "範例:"
    echo "  $0                          # 基本安裝"
    echo "  $0 --with-test-data        # 安裝並插入測試資料"
    echo "  DB_HOST=remote $0          # 使用遠程資料庫"
}

# 處理命令列參數
case "$1" in
    --help)
        show_help
        exit 0
        ;;
    --with-test-data)
        main --with-test-data
        ;;
    "")
        main
        ;;
    *)
        echo "未知選項: $1"
        echo "使用 --help 獲取幫助資訊"
        exit 1
        ;;
esac 