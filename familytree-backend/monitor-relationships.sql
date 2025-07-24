-- 監控關係資料表的SQL查詢腳本
-- 用於測試關係建立是否正確存入資料庫

-- 1. 查看當前所有關係資料
\echo '🔍 目前所有關係資料:'
SELECT 
    rl.id,
    rl.source_person_id,
    rl.target_person_id,
    rl.relation_type,
    rl.visual_analysis_graph_id,
    rl.created_at,
    p1.name as source_name,
    p2.name as target_name,
    vag.name as graph_name
FROM relationship_layers rl
LEFT JOIN person_profile p1 ON rl.source_person_id = p1.id
LEFT JOIN person_profile p2 ON rl.target_person_id = p2.id
LEFT JOIN visual_analysis_graphs vag ON rl.visual_analysis_graph_id = vag.id
ORDER BY rl.id DESC;

-- 2. 按圖表統計關係數量
\echo '📊 按圖表統計關係數量:'
SELECT 
    vag.id as graph_id,
    vag.name as graph_name,
    COUNT(rl.id) as relationship_count
FROM visual_analysis_graphs vag
LEFT JOIN relationship_layers rl ON vag.id = rl.visual_analysis_graph_id
GROUP BY vag.id, vag.name
ORDER BY vag.id;

-- 3. 顯示最近新增的關係（最新5筆）
\echo '🆕 最近新增的關係（最新5筆）:'
SELECT 
    rl.id,
    rl.source_person_id || ' (' || p1.name || ')' as source,
    rl.target_person_id || ' (' || p2.name || ')' as target,
    rl.relation_type,
    vag.name as graph_name,
    rl.created_at
FROM relationship_layers rl
LEFT JOIN person_profile p1 ON rl.source_person_id = p1.id
LEFT JOIN person_profile p2 ON rl.target_person_id = p2.id
LEFT JOIN visual_analysis_graphs vag ON rl.visual_analysis_graph_id = vag.id
ORDER BY rl.created_at DESC
LIMIT 5;

\echo '📝 監控完成 - 請建立新關係後重新執行此腳本檢查結果' 