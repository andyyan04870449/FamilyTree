-- 清理舊的重複分析資料
-- 目的：刪除重複的分析會話，只保留每個人員的最新分析結果

-- 1. 顯示清理前的統計資訊
SELECT 
    '清理前統計' as status,
    COUNT(DISTINCT analysis_session_id) as session_count,
    COUNT(*) as total_relationships,
    COUNT(DISTINCT root_person_id) as unique_persons
FROM relationship_layers rl
JOIN analysis_sessions a ON rl.analysis_session_id = a.id;

-- 2. 找出要保留的會話（每個人員的最新完成會話）
WITH latest_sessions AS (
    SELECT DISTINCT ON (root_person_id) 
        id,
        root_person_id,
        created_at
    FROM analysis_sessions 
    WHERE status = 'completed'
    ORDER BY root_person_id, created_at DESC
)
SELECT 
    '要保留的會話' as info,
    COUNT(*) as session_count,
    STRING_AGG(root_person_id::text, ', ') as person_ids
FROM latest_sessions;

-- 3. 刪除舊的會話（保留最新的）
DELETE FROM analysis_sessions 
WHERE id NOT IN (
    SELECT DISTINCT ON (root_person_id) id 
    FROM analysis_sessions 
    WHERE status = 'completed'
    ORDER BY root_person_id, created_at DESC
);

-- 4. 刪除對應的舊關係記錄
DELETE FROM relationship_layers 
WHERE analysis_session_id NOT IN (
    SELECT id FROM analysis_sessions
);

-- 5. 顯示清理後的統計資訊
SELECT 
    '清理後統計' as status,
    COUNT(DISTINCT analysis_session_id) as session_count,
    COUNT(*) as total_relationships,
    COUNT(DISTINCT root_person_id) as unique_persons
FROM relationship_layers rl
JOIN analysis_sessions a ON rl.analysis_session_id = a.id;

-- 6. 顯示每個人員的關係數量
SELECT 
    p.name as person_name,
    COUNT(rl.*) as relationship_count,
    MIN(rl.layer_depth) as min_depth,
    MAX(rl.layer_depth) as max_depth
FROM relationship_layers rl
JOIN analysis_sessions a ON rl.analysis_session_id = a.id
JOIN persons p ON a.root_person_id = p.id
GROUP BY p.id, p.name
ORDER BY p.name;

-- 7. 顯示黃心田的具體關係（用於驗證）
SELECT 
    '黃心田的關係' as info,
    p1.name as source_person,
    p2.name as target_person,
    rl.relation_type,
    rl.layer_depth
FROM relationship_layers rl
JOIN analysis_sessions a ON rl.analysis_session_id = a.id
JOIN persons p1 ON rl.source_person_id = p1.id
JOIN persons p2 ON rl.target_person_id = p2.id
WHERE a.root_person_id = (SELECT id FROM persons WHERE name = '黃心田')
ORDER BY rl.layer_depth, p1.name, p2.name; 