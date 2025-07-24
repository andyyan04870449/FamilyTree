#!/bin/bash

# 驗證視覺化分析關聯遷移結果

echo "🔍 驗證視覺化分析關聯遷移結果..."
echo ""

# 載入環境變數
source ../load-env.sh

# 檢查資料庫連線
if [ -z "$DATABASE_URL" ]; then
    echo "❌ 錯誤：DATABASE_URL 環境變數未設定"
    exit 1
fi

echo "📍 資料庫連線：$DATABASE_URL"
echo ""

# 1. 檢查資料表結構
echo "1️⃣ 檢查 relationship_layers 表結構..."
psql "$DATABASE_URL" -c "\d relationship_layers" | grep "visual_analysis_graph_id"

if [ $? -eq 0 ]; then
    echo "✅ visual_analysis_graph_id 欄位存在"
else
    echo "❌ visual_analysis_graph_id 欄位不存在"
fi
echo ""

# 2. 檢查約束和索引
echo "2️⃣ 檢查外鍵約束..."
psql "$DATABASE_URL" -c "
SELECT conname, contype 
FROM pg_constraint 
WHERE conrelid = 'relationship_layers'::regclass 
AND conname = 'fk_relationship_layers_visual_analysis_graph';"

echo ""
echo "3️⃣ 檢查索引..."
psql "$DATABASE_URL" -c "
SELECT indexname 
FROM pg_indexes 
WHERE tablename = 'relationship_layers' 
AND indexname = 'idx_relationship_layers_visual_analysis_graph_id';"

echo ""

# 3. 檢查預設圖表
echo "4️⃣ 檢查預設視覺化圖表分析記錄..."
psql "$DATABASE_URL" -c "
SELECT id, name, project_ids, updated_by, updated_at 
FROM visual_analysis_graphs 
WHERE name = '視覺化圖表分析';"

echo ""

# 4. 檢查關聯資料統計
echo "5️⃣ 檢查關聯資料統計..."
psql "$DATABASE_URL" -c "
SELECT 
    COUNT(*) as total_relationships,
    COUNT(visual_analysis_graph_id) as relationships_with_graph_id,
    ROUND(COUNT(visual_analysis_graph_id) * 100.0 / COUNT(*), 2) as percentage_updated
FROM relationship_layers;"

echo ""

# 5. 檢查關聯資料與圖表的連接
echo "6️⃣ 檢查關聯資料與圖表的連接..."
psql "$DATABASE_URL" -c "
SELECT 
    vag.id as graph_id,
    vag.name as graph_name,
    COUNT(rl.id) as relationship_count
FROM visual_analysis_graphs vag
LEFT JOIN relationship_layers rl ON vag.id = rl.visual_analysis_graph_id
GROUP BY vag.id, vag.name
ORDER BY vag.id;"

echo ""

# 6. 最新的幾筆關聯資料
echo "7️⃣ 最新的關聯資料範例..."
psql "$DATABASE_URL" -c "
SELECT 
    rl.id,
    rl.source_person_id,
    rl.target_person_id,
    rl.relation_type,
    rl.visual_analysis_graph_id,
    vag.name as graph_name,
    rl.created_at
FROM relationship_layers rl
LEFT JOIN visual_analysis_graphs vag ON rl.visual_analysis_graph_id = vag.id
ORDER BY rl.id DESC
LIMIT 5;"

echo ""
echo "🎉 驗證完成！" 