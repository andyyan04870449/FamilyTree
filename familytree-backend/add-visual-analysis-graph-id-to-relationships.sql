-- 資料庫遷移腳本：為 relationship_layers 表添加 visual_analysis_graph_id 欄位
-- 目的：將關聯資料與視覺化分析圖表關聯起來
-- 日期：2024-12-23

BEGIN;

-- 步驟1：檢查並插入"視覺化圖表分析"記錄（如果不存在）
INSERT INTO visual_analysis_graphs (name, project_ids, updated_by, updated_at) 
VALUES ('視覺化圖表分析', '', 'system', CURRENT_TIMESTAMP)
ON CONFLICT (name) DO NOTHING;

-- 步驟2：獲取"視覺化圖表分析"的ID（用於後續更新）
-- 這個ID會在後面的UPDATE語句中使用

-- 步驟3：為 relationship_layers 表添加新欄位
ALTER TABLE relationship_layers 
ADD COLUMN IF NOT EXISTS visual_analysis_graph_id INTEGER;

-- 步驟4：添加註釋
COMMENT ON COLUMN relationship_layers.visual_analysis_graph_id 
IS '視覺化分析圖表ID，關聯到 visual_analysis_graphs.id';

-- 步驟5：更新所有現有的關聯資料，將它們關聯到"視覺化圖表分析"
UPDATE relationship_layers 
SET visual_analysis_graph_id = (
    SELECT id FROM visual_analysis_graphs 
    WHERE name = '視覺化圖表分析' 
    LIMIT 1
)
WHERE visual_analysis_graph_id IS NULL;

-- 步驟6：添加外鍵約束（設為可為NULL，因為某些舊資料可能沒有關聯圖表）
ALTER TABLE relationship_layers 
ADD CONSTRAINT fk_relationship_layers_visual_analysis_graph
FOREIGN KEY (visual_analysis_graph_id) 
REFERENCES visual_analysis_graphs(id) 
ON DELETE SET NULL;

-- 步驟7：創建索引以提升查詢效能
CREATE INDEX IF NOT EXISTS idx_relationship_layers_visual_analysis_graph_id 
ON relationship_layers(visual_analysis_graph_id);

-- 步驟8：顯示更新結果
SELECT 
    'visual_analysis_graph_id欄位已添加' as status,
    COUNT(*) as total_relationships,
    COUNT(visual_analysis_graph_id) as updated_relationships,
    (SELECT name FROM visual_analysis_graphs WHERE name = '視覺化圖表分析') as default_graph_name,
    (SELECT id FROM visual_analysis_graphs WHERE name = '視覺化圖表分析') as default_graph_id
FROM relationship_layers;

-- 步驟9：顯示視覺化分析圖表列表
SELECT 'VISUAL ANALYSIS GRAPHS:' as note;
SELECT id, name, project_ids, updated_by, updated_at 
FROM visual_analysis_graphs 
ORDER BY id;

COMMIT; 