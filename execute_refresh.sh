#!/bin/bash

# =====================================================
# 🧬 AI關聯分析系統 - 資料庫重新整理執行腳本
# 版本：v1.3
# 說明：此腳本將清空 person_profile 表格並重新插入資料
# =====================================================

echo "🧬 開始重新整理 person_profile 資料表..."

# 檢查資料庫連線設定
DB_HOST=${DB_HOST:-"localhost"}
DB_PORT=${DB_PORT:-"5432"}
DB_NAME=${DB_NAME:-"familytree"}
DB_USER=${DB_USER:-"user"}
DB_PASSWORD=${DB_PASSWORD:-""}

echo "📊 資料庫連線資訊："
echo "  主機：$DB_HOST"
echo "  端口：$DB_PORT"
echo "  資料庫：$DB_NAME"
echo "  使用者：$DB_USER"

# 檢查 PostgreSQL 是否安裝
if ! command -v psql &> /dev/null; then
    echo "❌ 錯誤：找不到 psql 命令"
    echo "請確保已安裝 PostgreSQL 客戶端工具"
    exit 1
fi

echo "✅ PostgreSQL 客戶端已安裝"

# 測試資料庫連線
echo "🔗 測試資料庫連線..."
if ! PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" > /dev/null 2>&1; then
    echo "❌ 錯誤：無法連接到資料庫"
    echo "請檢查資料庫連線設定"
    exit 1
fi

echo "✅ 資料庫連線成功"

# 檢查資料檔案是否存在
if [ ! -f "person_profile_insert.sql" ]; then
    echo "❌ 錯誤：找不到 person_profile_insert.sql 檔案"
    echo "請確保資料檔案存在於當前目錄"
    exit 1
fi

echo "✅ 找到資料檔案：person_profile_insert.sql"

# 備份現有資料（可選）
echo "💾 建立資料備份..."
BACKUP_FILE="person_profile_backup_$(date +%Y%m%d_%H%M%S).sql"
PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "\COPY (SELECT * FROM person_profile) TO STDOUT WITH CSV HEADER" > "$BACKUP_FILE" 2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ 資料備份已建立：$BACKUP_FILE"
else
    echo "⚠️  警告：無法建立資料備份（可能是表格為空）"
fi

# 執行清空和重新插入
echo "🔄 開始清空並重新插入資料..."

# 先清空表格
echo "🗑️  清空現有資料..."
PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "TRUNCATE TABLE person_profile RESTART IDENTITY CASCADE;" 2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ 資料表已清空"
else
    echo "❌ 錯誤：清空資料表失敗"
    exit 1
fi

# 插入新資料
echo "📥 插入新資料..."
PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "person_profile_insert.sql" 2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ 新資料插入成功"
else
    echo "❌ 錯誤：插入新資料失敗"
    exit 1
fi

# 顯示結果統計
echo "📊 資料更新結果："
PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "
SELECT 
    COUNT(*) as total_records,
    COUNT(CASE WHEN gender = '男' THEN 1 END) as male_count,
    COUNT(CASE WHEN gender = '女' THEN 1 END) as female_count,
    COUNT(CASE WHEN gender IS NULL OR gender = '' THEN 1 END) as unknown_gender_count
FROM person_profile;
" 2>/dev/null

# 顯示前5筆資料
echo "📋 前5筆資料："
PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "
SELECT 
    id,
    name,
    gender,
    birthday,
    nationality,
    mobile
FROM person_profile 
ORDER BY id 
LIMIT 5;
" 2>/dev/null

echo ""
echo "🎉 資料庫重新整理完成！"
echo ""
echo "📝 注意事項："
echo "  - 舊資料已備份至：$BACKUP_FILE"
echo "  - 如需恢復舊資料，請使用備份檔案"
echo "  - 建議重新啟動應用程式以確保資料同步" 