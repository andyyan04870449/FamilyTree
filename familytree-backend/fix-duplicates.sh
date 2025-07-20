#!/bin/bash

echo "🔧 開始修復重複關係問題..."

# 檢查是否在正確的目錄
if [ ! -f "fix-duplicate-relationships.sql" ]; then
    echo "❌ 錯誤：找不到 fix-duplicate-relationships.sql 文件"
    echo "請確保你在 familytree-backend 目錄下運行此腳本"
    exit 1
fi

# 檢查 PostgreSQL 連接
echo "📡 檢查資料庫連接..."

# 使用環境變數方式連接資料庫
echo "✅ 使用環境變數連接資料庫"

# 執行 SQL 修復腳本
echo "🚀 執行修復腳本..."
PGPASSWORD="" psql -h localhost -p 5432 -U user -d familytree -f fix-duplicate-relationships.sql

if [ $? -eq 0 ]; then
    echo "✅ 修復腳本執行成功"
    echo ""
    echo "📊 修復結果："
    echo "- 已清理重複的關係記錄"
    echo "- 已添加唯一約束防止未來重複"
    echo "- 已添加索引提升查詢效能"
    echo ""
    echo "🎯 現在黃心田的分析圖譜中馬晶應該不會重複出現了"
else
    echo "❌ 修復腳本執行失敗"
    exit 1
fi

echo "✅ 修復完成！" 