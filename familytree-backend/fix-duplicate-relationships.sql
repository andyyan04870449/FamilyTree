-- 修復重複關係問題的 SQL 腳本
-- 目的：防止在黃心田的分析圖譜中馬晶重複出現的問題

-- 1. 首先清理現有的重複關係記錄
-- 保留每個分析會話中每個關係對的第一條記錄（最淺層級）
DELETE FROM relationship_layers 
WHERE id NOT IN (
    SELECT MIN(id) 
    FROM relationship_layers 
    GROUP BY analysis_session_id, source_person_id, target_person_id
);

-- 2. 添加唯一約束防止未來重複
-- 注意：如果約束已存在，這個語句會失敗，這是正常的
DO $$
BEGIN
    -- 嘗試添加唯一約束
    BEGIN
        ALTER TABLE relationship_layers 
        ADD CONSTRAINT unique_relationship_per_session 
        UNIQUE (analysis_session_id, source_person_id, target_person_id);
        RAISE NOTICE '成功添加唯一約束';
    EXCEPTION
        WHEN duplicate_object THEN
            RAISE NOTICE '唯一約束已存在，跳過';
    END;
END $$;

-- 3. 添加索引以提升查詢效能
CREATE INDEX IF NOT EXISTS idx_relationship_layers_session_source_target 
ON relationship_layers (analysis_session_id, source_person_id, target_person_id);

CREATE INDEX IF NOT EXISTS idx_relationship_layers_session_depth 
ON relationship_layers (analysis_session_id, layer_depth);

-- 4. 顯示修復結果
SELECT 
    '修復完成' as status,
    COUNT(*) as total_relationships,
    COUNT(DISTINCT analysis_session_id) as total_sessions,
    COUNT(DISTINCT source_person_id) as unique_source_persons,
    COUNT(DISTINCT target_person_id) as unique_target_persons
FROM relationship_layers;

-- 5. 顯示每個分析會話的關係統計
SELECT 
    analysis_session_id,
    COUNT(*) as relationship_count,
    COUNT(DISTINCT source_person_id) as unique_sources,
    COUNT(DISTINCT target_person_id) as unique_targets,
    MIN(layer_depth) as min_depth,
    MAX(layer_depth) as max_depth
FROM relationship_layers 
GROUP BY analysis_session_id 
ORDER BY analysis_session_id; 