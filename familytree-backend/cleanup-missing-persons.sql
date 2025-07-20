-- 清理 missing_persons 表中的重複記錄
-- 只保留每個人員的最新記錄

-- 1. 顯示清理前的統計
SELECT 
    '清理前統計' as info,
    COUNT(*) as total_missing_persons,
    COUNT(DISTINCT name) as unique_names,
    COUNT(DISTINCT analysis_session_id) as session_count
FROM missing_persons;

-- 2. 顯示重複的人員
SELECT 
    name,
    relation_type,
    COUNT(*) as duplicate_count,
    STRING_AGG(analysis_session_id, ', ' ORDER BY discovered_at DESC) as session_ids
FROM missing_persons
GROUP BY name, relation_type
HAVING COUNT(*) > 1
ORDER BY duplicate_count DESC;

-- 3. 刪除重複記錄，只保留每個人員的最新記錄
DELETE FROM missing_persons 
WHERE id NOT IN (
    SELECT DISTINCT ON (name, relation_type, source_person_id) id
    FROM missing_persons
    ORDER BY name, relation_type, source_person_id, discovered_at DESC
);

-- 4. 顯示清理後的統計
SELECT 
    '清理後統計' as info,
    COUNT(*) as total_missing_persons,
    COUNT(DISTINCT name) as unique_names,
    COUNT(DISTINCT analysis_session_id) as session_count
FROM missing_persons;

-- 5. 顯示剩餘的 missing persons
SELECT 
    name,
    relation_type,
    source_person_id,
    layer_depth,
    discovered_at,
    status
FROM missing_persons
ORDER BY discovered_at DESC; 