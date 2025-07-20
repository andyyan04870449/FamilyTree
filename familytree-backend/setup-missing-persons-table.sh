#!/bin/bash

echo "🔧 創建 missing_persons 表格..."

# 檢查是否在正確的目錄
if [ ! -f "create_missing_persons_table.sql" ]; then
    echo "❌ 錯誤：找不到 create_missing_persons_table.sql 檔案"
    echo "請確保你在 familytree-backend 目錄下運行此腳本"
    exit 1
fi

# 檢查 PostgreSQL 是否安裝
if ! command -v psql &> /dev/null; then
    echo "❌ 錯誤：找不到 psql 命令"
    echo "請確保已安裝 PostgreSQL 客戶端"
    exit 1
fi

# 從環境變數或配置文件讀取資料庫連接信息
DB_HOST=${DB_HOST:-"localhost"}
DB_PORT=${DB_PORT:-"5432"}
DB_NAME=${DB_NAME:-"familytree"}
DB_USER=${DB_USER:-"user"}

echo "📋 資料庫連接信息："
echo "  - 主機: $DB_HOST"
echo "  - 端口: $DB_PORT"
echo "  - 資料庫: $DB_NAME"
echo "  - 用戶: $DB_USER"

# 執行 SQL 腳本
echo "🚀 執行 SQL 腳本..."
psql -h $DB_HOST -p $DB_PORT -d $DB_NAME -U $DB_USER -f create_missing_persons_table.sql

if [ $? -eq 0 ]; then
    echo "✅ missing_persons 表格創建成功！"
    echo ""
    echo "📊 表格結構："
    echo "  - id: 主鍵"
    echo "  - name: 人員姓名"
    echo "  - relation_type: 關係類型"
    echo "  - source_person_id: 來源人員 ID"
    echo "  - source_field: 來源欄位"
    echo "  - analysis_session_id: 分析會話 ID"
    echo "  - layer_depth: 層級深度"
    echo "  - discovered_at: 發現時間"
    echo "  - status: 狀態 (pending/resolved/ignored)"
    echo "  - resolved_person_id: 解決後的人員 ID"
    echo "  - notes: 備註"
    echo ""
    echo "🎯 現在系統會自動記錄 AI 分析出但找不到的人員！"
else
    echo "❌ 表格創建失敗"
    exit 1
fi 