#!/bin/bash

# 資料庫恢復腳本
set -e

echo "開始恢復 PostgreSQL 資料庫..."

# 設置資料庫連接資訊
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="familytree"
DB_USER="user"
DB_PASSWORD=""

# 檢查 PostgreSQL 是否運行
if ! pg_isready -h $DB_HOST -p $DB_PORT -U $DB_USER; then
    echo "PostgreSQL 未運行，請先啟動 PostgreSQL 服務"
    exit 1
fi

# 刪除現有資料庫（如果存在）
echo "刪除現有資料庫..."
PGPASSWORD=$DB_PASSWORD dropdb \
  -h $DB_HOST \
  -p $DB_PORT \
  -U $DB_USER \
  --if-exists \
  $DB_NAME || true

# 創建新資料庫
echo "創建新資料庫..."
PGPASSWORD=$DB_PASSWORD createdb \
  -h $DB_HOST \
  -p $DB_PORT \
  -U $DB_USER \
  $DB_NAME

# 恢復資料庫
echo "恢復資料庫資料..."
PGPASSWORD=$DB_PASSWORD psql \
  -h $DB_HOST \
  -p $DB_PORT \
  -U $DB_USER \
  -d $DB_NAME \
  -f familytree_backup.sql

echo "資料庫恢復完成！"
