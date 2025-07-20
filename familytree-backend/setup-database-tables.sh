#!/bin/bash

echo "🔧 建立檔案上傳功能所需的資料庫表格..."

# 檢查是否在正確的目錄
if [ ! -f "familytree-backend.csproj" ]; then
    echo "❌ 錯誤：找不到 familytree-backend.csproj 文件"
    echo "請確保你在 familytree-backend 目錄下運行此腳本"
    exit 1
fi

echo "📁 進入後端目錄: $(pwd)"

# 檢查 appsettings.json 是否存在
if [ ! -f "appsettings.json" ]; then
    echo "❌ 錯誤：找不到 appsettings.json 文件"
    exit 1
fi

# 從 appsettings.json 讀取資料庫連線字串
DB_CONNECTION=$(grep -o '"DefaultConnection": "[^"]*"' appsettings.json | cut -d'"' -f4)

if [ -z "$DB_CONNECTION" ]; then
    echo "❌ 錯誤：無法從 appsettings.json 讀取資料庫連線字串"
    exit 1
fi

echo "✅ 找到資料庫連線字串"

# 執行 SQL 檔案
echo "📊 建立 user_update_file 表格..."
psql "$DB_CONNECTION" -f create_user_update_file_table.sql

echo "📊 建立 person_data 表格..."
psql "$DB_CONNECTION" -f create_person_data_table.sql

echo "📊 建立 field_mapping 表格..."
psql "$DB_CONNECTION" -f create_field_mapping_table.sql

echo "✅ 資料庫表格建立完成！"
echo ""
echo "📋 已建立的表格："
echo "  - user_update_file: 檔案上傳記錄"
echo "  - person_data: 人員資料"
echo "  - field_mapping: 欄位對應"
echo ""
echo "🚀 現在可以開始使用檔案上傳功能了！" 