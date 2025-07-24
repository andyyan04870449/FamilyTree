#!/bin/bash

# 執行資料庫遷移腳本
# 為 relationship_layers 表添加 visual_analysis_graph_id 欄位

echo "🔄 開始執行資料庫遷移..."
echo "📊 為 relationship_layers 表添加 visual_analysis_graph_id 欄位"

# 載入環境變數
source ../load-env.sh

# 檢查是否有 PostgreSQL 連線資訊
if [ -z "$DATABASE_URL" ]; then
    echo "❌ 錯誤：DATABASE_URL 環境變數未設定"
    echo "請確保 load-env.sh 中包含資料庫連線資訊"
    exit 1
fi

echo "📍 資料庫連線：$DATABASE_URL"
echo ""

# 執行遷移腳本
echo "⚙️  執行遷移腳本..."
psql "$DATABASE_URL" -f add-visual-analysis-graph-id-to-relationships.sql

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ 資料庫遷移完成！"
    echo ""
    echo "📋 已完成的操作："
    echo "   1. 為 relationship_layers 表添加 visual_analysis_graph_id 欄位"
    echo "   2. 插入或確認'視覺化圖表分析'記錄存在"
    echo "   3. 將所有現有關聯資料關聯到'視覺化圖表分析'"
    echo "   4. 添加外鍵約束和索引"
    echo ""
    echo "🔍 驗證遷移結果..."
    psql "$DATABASE_URL" -c "SELECT COUNT(*) as total_relationships, COUNT(visual_analysis_graph_id) as updated_relationships FROM relationship_layers;"
    echo ""
    psql "$DATABASE_URL" -c "SELECT id, name FROM visual_analysis_graphs WHERE name = '視覺化圖表分析';"
else
    echo ""
    echo "❌ 資料庫遷移失敗！"
    echo "請檢查錯誤訊息並修正問題後重新執行"
    exit 1
fi 