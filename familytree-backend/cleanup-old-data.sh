#!/bin/bash

echo "🧹 開始清理舊的重複分析資料..."

# 檢查是否在正確的目錄
if [ ! -f "cleanup-old-data.sql" ]; then
    echo "❌ 錯誤：找不到 cleanup-old-data.sql 文件"
    echo "請確保你在 familytree-backend 目錄下運行此腳本"
    exit 1
fi

# 檢查資料庫連接
echo "📡 檢查資料庫連接..."

# 執行清理腳本
echo "🚀 執行資料清理..."
PGPASSWORD="" psql -h localhost -p 5432 -U user -d familytree -f cleanup-old-data.sql

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ 資料清理完成！"
    echo ""
    echo "📊 清理結果："
    echo "- 已刪除重複的分析會話"
    echo "- 已刪除對應的舊關係記錄"
    echo "- 只保留每個人員的最新分析結果"
    echo ""
    echo "🎯 現在黃心田的分析圖譜中應該不會有重複的馬晶了"
else
    echo "❌ 資料清理失敗"
    exit 1
fi

echo "✅ 清理完成！" 