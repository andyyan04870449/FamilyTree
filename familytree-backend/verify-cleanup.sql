-- 驗證資料清理效果
-- 確認重複問題已解決

-- 1. 整體統計
SELECT 
    '整體統計' as info,
    COUNT(DISTINCT analysis_session_id) as session_count,
    COUNT(*) as total_relationships,
    COUNT(DISTINCT a.root_person_id) as unique_persons
FROM relationship_layers rl
JOIN analysis_sessions a ON rl.analysis_session_id = a.id;

-- 2. 每個人員的分析會話數量
SELECT 
    pp.name as person_name,
    COUNT(DISTINCT a.id) as session_count,
    COUNT(rl.*) as relationship_count
FROM analysis_sessions a
JOIN person_profile pp ON a.root_person_id = pp.id
LEFT JOIN relationship_layers rl ON a.id = rl.analysis_session_id
GROUP BY pp.id, pp.name
ORDER BY pp.name;

-- 3. 黃心田的詳細關係（確認沒有重複）
SELECT 
    '黃心田的關係圖譜' as info,
    pp1.name as source_person,
    pp2.name as target_person,
    rl.relation_type,
    rl.layer_depth
FROM relationship_layers rl
JOIN analysis_sessions a ON rl.analysis_session_id = a.id
JOIN person_profile pp1 ON rl.source_person_id = pp1.id
JOIN person_profile pp2 ON rl.target_person_id = pp2.id
WHERE a.root_person_id = (SELECT id FROM person_profile WHERE name = '黃心田')
ORDER BY rl.layer_depth, pp1.name, pp2.name;

-- 4. 檢查是否有重複的關係（同一會話內）
SELECT 
    '重複關係檢查' as info,
    analysis_session_id,
    source_person_id,
    target_person_id,
    COUNT(*) as duplicate_count
FROM relationship_layers
GROUP BY analysis_session_id, source_person_id, target_person_id
HAVING COUNT(*) > 1
ORDER BY duplicate_count DESC;

-- 5. 檢查是否有重複的人員出現在多個層級
SELECT 
    '多層級人員檢查' as info,
    analysis_session_id,
    target_person_id,
    COUNT(DISTINCT layer_depth) as depth_count,
    STRING_AGG(layer_depth::text, ', ' ORDER BY layer_depth) as depths
FROM relationship_layers
GROUP BY analysis_session_id, target_person_id
HAVING COUNT(DISTINCT layer_depth) > 1
ORDER BY depth_count DESC; 