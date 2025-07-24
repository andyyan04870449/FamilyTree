-- 查詢視覺化分析圖表資料
-- 查找名為"視覺化圖表分析"的記錄

SELECT id, name, project_ids, updated_by, updated_at 
FROM visual_analysis_graphs 
WHERE name LIKE '%視覺化圖表分析%' OR name LIKE '%視覺化%'
ORDER BY updated_at DESC;

-- 如果沒有找到，顯示所有記錄
SELECT 'ALL RECORDS:' as note;
SELECT id, name, project_ids, updated_by, updated_at 
FROM visual_analysis_graphs 
ORDER BY id;

-- 查看 relationship_layers 表的結構
SELECT 'RELATIONSHIP_LAYERS TABLE STRUCTURE:' as note;
\d relationship_layers; 