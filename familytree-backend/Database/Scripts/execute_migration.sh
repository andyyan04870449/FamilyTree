#!/bin/bash

# =============================================
# 使用者帳號管理系統 - 資料庫遷移執行腳本
# =============================================

# 設定顏色輸出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 資料庫連線參數（根據 appsettings.json 設定）
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="familytree"
DB_USER="user"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}使用者帳號管理系統 - 資料庫遷移${NC}"
echo -e "${GREEN}========================================${NC}"

# 提示輸入資料庫密碼
echo -n "請輸入資料庫密碼: "
read -s DB_PASSWORD
echo

# 建立 .pgpass 檔案以避免重複輸入密碼
echo "$DB_HOST:$DB_PORT:$DB_NAME:$DB_USER:$DB_PASSWORD" > ~/.pgpass
chmod 600 ~/.pgpass

# 執行前備份
echo -e "\n${YELLOW}步驟 1: 備份現有資料庫...${NC}"
BACKUP_FILE="familytree_backup_$(date +%Y%m%d_%H%M%S).sql"
PGPASSWORD=$DB_PASSWORD pg_dump -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME > $BACKUP_FILE

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ 備份成功: $BACKUP_FILE${NC}"
else
    echo -e "${RED}✗ 備份失敗！${NC}"
    rm ~/.pgpass
    exit 1
fi

# 執行遷移腳本
echo -e "\n${YELLOW}步驟 2: 執行資料庫遷移...${NC}"

# 執行腳本 1
echo -e "\n執行 01_create_user_tables.sql..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f 01_create_user_tables.sql

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ 建立使用者資料表成功${NC}"
else
    echo -e "${RED}✗ 建立使用者資料表失敗！${NC}"
    rm ~/.pgpass
    exit 1
fi

# 執行腳本 2
echo -e "\n執行 02_alter_existing_tables.sql..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f 02_alter_existing_tables.sql

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ 修改現有資料表成功${NC}"
else
    echo -e "${RED}✗ 修改現有資料表失敗！${NC}"
    rm ~/.pgpass
    exit 1
fi

# 執行腳本 3
echo -e "\n執行 03_create_views_functions.sql..."
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -f 03_create_views_functions.sql

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ 建立檢視和函數成功${NC}"
else
    echo -e "${RED}✗ 建立檢視和函數失敗！${NC}"
    rm ~/.pgpass
    exit 1
fi

# 驗證遷移結果
echo -e "\n${YELLOW}步驟 3: 驗證遷移結果...${NC}"
PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME << EOF
-- 檢查新建立的資料表
SELECT 'users' as table_name, COUNT(*) as row_count FROM users
UNION ALL
SELECT 'user_tokens', COUNT(*) FROM user_tokens
UNION ALL
SELECT 'activity_logs', COUNT(*) FROM activity_logs;

-- 檢查 user_id 欄位
SELECT 
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE column_name = 'user_id'
AND table_name IN ('person_profile', 'favorites', 'field_mapping', 'analysis_results', 'analysis_sessions', 'missing_persons', 'relationship_layers')
ORDER BY table_name;
EOF

# 清理暫存檔案
rm ~/.pgpass

echo -e "\n${GREEN}========================================${NC}"
echo -e "${GREEN}資料庫遷移完成！${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "\n${YELLOW}重要提醒：${NC}"
echo -e "1. 請更新管理員密碼（預設帳號: admin）"
echo -e "2. 備份檔案: $BACKUP_FILE"
echo -e "3. 如遇問題可使用備份檔案還原"