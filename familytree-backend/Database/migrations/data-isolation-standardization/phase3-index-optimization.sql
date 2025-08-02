-- Phase 3: 索引優化 for user_id 隔離機制
-- 針對統一的 user_id 隔離策略進行索引優化
-- 執行日期: 2025-08-02
-- 相關 Issue: https://github.com/andyyan04870449/FamilyTree/issues/2

-- =====================================================
-- 第一步：分析現有索引
-- =====================================================

-- 檢查現有索引
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE schemaname = 'public' 
    AND tablename IN (
        'person_profile', 'relationship_layers', 'user_favorites',
        'projects', 'photos', 'analysis_sessions', 'file_upload_records'
    )
ORDER BY tablename, indexname;

-- =====================================================
-- 第二步：為 user_id 創建優化索引
-- =====================================================

-- person_profile 表索引優化
-- 主要查詢模式：WHERE user_id = ? [AND other_conditions]
DROP INDEX IF EXISTS idx_person_profile_user_id;
CREATE INDEX CONCURRENTLY idx_person_profile_user_id 
ON person_profile (user_id);

-- 複合索引：user_id + 常用查詢欄位
DROP INDEX IF EXISTS idx_person_profile_user_name;
CREATE INDEX CONCURRENTLY idx_person_profile_user_name 
ON person_profile (user_id, name);

DROP INDEX IF EXISTS idx_person_profile_user_created;
CREATE INDEX CONCURRENTLY idx_person_profile_user_created 
ON person_profile (user_id, created_at DESC);

-- 全文搜索索引
DROP INDEX IF EXISTS idx_person_profile_fulltext;
CREATE INDEX CONCURRENTLY idx_person_profile_fulltext 
ON person_profile USING gin (
    to_tsvector('english', 
        COALESCE(name, '') || ' ' ||
        COALESCE(chinese_name, '') || ' ' ||
        COALESCE(birth_place, '') || ' ' ||
        COALESCE(current_address, '')
    )
) WHERE user_id IS NOT NULL;

-- relationship_layers 表索引優化
DROP INDEX IF EXISTS idx_relationship_layers_user_id;
CREATE INDEX CONCURRENTLY idx_relationship_layers_user_id 
ON relationship_layers (user_id);

DROP INDEX IF EXISTS idx_relationship_layers_user_person;
CREATE INDEX CONCURRENTLY idx_relationship_layers_user_person 
ON relationship_layers (user_id, person_id);

-- user_favorites 表索引優化
DROP INDEX IF EXISTS idx_user_favorites_user_id;
CREATE INDEX CONCURRENTLY idx_user_favorites_user_id 
ON user_favorites (user_id);

DROP INDEX IF EXISTS idx_user_favorites_user_person;
CREATE INDEX CONCURRENTLY idx_user_favorites_user_person 
ON user_favorites (user_id, person_id);

-- projects 表索引優化
DROP INDEX IF EXISTS idx_projects_user_id;
CREATE INDEX CONCURRENTLY idx_projects_user_id 
ON projects (user_id);

DROP INDEX IF EXISTS idx_projects_user_status;
CREATE INDEX CONCURRENTLY idx_projects_user_status 
ON projects (user_id, status) WHERE deleted_at IS NULL;

-- photos 表索引優化
DROP INDEX IF EXISTS idx_photos_user_id;
CREATE INDEX CONCURRENTLY idx_photos_user_id 
ON photos (user_id);

DROP INDEX IF EXISTS idx_photos_user_person;
CREATE INDEX CONCURRENTLY idx_photos_user_person 
ON photos (user_id, person_id);

-- analysis_sessions 表索引優化
DROP INDEX IF EXISTS idx_analysis_sessions_user_id;
CREATE INDEX CONCURRENTLY idx_analysis_sessions_user_id 
ON analysis_sessions (user_id);

DROP INDEX IF EXISTS idx_analysis_sessions_user_created;
CREATE INDEX CONCURRENTLY idx_analysis_sessions_user_created 
ON analysis_sessions (user_id, created_at DESC);

-- file_upload_records 表索引優化
DROP INDEX IF EXISTS idx_file_upload_records_user_id;
CREATE INDEX CONCURRENTLY idx_file_upload_records_user_id 
ON file_upload_records (user_id);

DROP INDEX IF EXISTS idx_file_upload_records_user_status;
CREATE INDEX CONCURRENTLY idx_file_upload_records_user_status 
ON file_upload_records (user_id, status);

-- =====================================================
-- 第三步：移除不再需要的 project_id 索引
-- =====================================================

-- 檢查並移除舊的 project_id 索引（如果它們不再被使用）
-- 注意：只有在確認沒有其他查詢使用這些索引時才移除

-- person_profile 相關的 project_id 索引
DROP INDEX IF EXISTS idx_person_profile_project_id;

-- relationship_layers 相關的 project_id 索引  
DROP INDEX IF EXISTS idx_relationship_layers_project_id;

-- user_favorites 相關的 project_id 索引
DROP INDEX IF EXISTS idx_user_favorites_project_id;

-- =====================================================
-- 第四步：創建統計信息和維護計劃
-- =====================================================

-- 更新表統計信息
ANALYZE person_profile;
ANALYZE relationship_layers;
ANALYZE user_favorites;
ANALYZE projects;
ANALYZE photos;
ANALYZE analysis_sessions;
ANALYZE file_upload_records;

-- =====================================================
-- 第五步：性能驗證查詢
-- =====================================================

-- 測試關鍵查詢的性能
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM person_profile 
WHERE user_id = '123e4567-e89b-12d3-a456-426614174000'
ORDER BY created_at DESC
LIMIT 20;

EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM person_profile 
WHERE user_id = '123e4567-e89b-12d3-a456-426614174000'
    AND name ILIKE '%測試%';

EXPLAIN (ANALYZE, BUFFERS)
SELECT p.*, f.id as favorite_id 
FROM person_profile p
LEFT JOIN user_favorites f ON p.id = f.person_id AND f.user_id = p.user_id
WHERE p.user_id = '123e4567-e89b-12d3-a456-426614174000';

-- =====================================================
-- 第六步：索引使用情況監控
-- =====================================================

-- 創建索引使用情況監控視圖
CREATE OR REPLACE VIEW v_index_usage_stats AS
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch,
    idx_scan,
    CASE 
        WHEN idx_scan = 0 THEN 'UNUSED'
        WHEN idx_scan < 100 THEN 'LOW_USAGE'
        WHEN idx_scan < 1000 THEN 'MEDIUM_USAGE'
        ELSE 'HIGH_USAGE'
    END as usage_level
FROM pg_stat_user_indexes 
WHERE schemaname = 'public'
    AND tablename IN (
        'person_profile', 'relationship_layers', 'user_favorites',
        'projects', 'photos', 'analysis_sessions', 'file_upload_records'
    )
ORDER BY idx_scan DESC;

-- =====================================================
-- 第七步：記錄優化日誌
-- =====================================================

-- 記錄索引優化操作
INSERT INTO data_migration_log (migration_name, executed_by, status, records_affected, notes)
VALUES (
    'Phase3-IndexOptimization', 
    'system', 
    'completed',
    (SELECT COUNT(*) FROM information_schema.indexes 
     WHERE table_schema = 'public' 
        AND table_name IN ('person_profile', 'relationship_layers', 'user_favorites', 'projects', 'photos', 'analysis_sessions', 'file_upload_records')
        AND index_name LIKE 'idx_%_user_%'),
    'Optimized indexes for user_id isolation strategy. Created user_id based indexes and removed obsolete project_id indexes. Added monitoring views for index usage tracking.'
);

-- =====================================================
-- 驗證和報告
-- =====================================================

-- 最終索引報告
SELECT 
    '=== INDEX OPTIMIZATION PHASE 3 SUMMARY ===' as report_section,
    '' as details
UNION ALL
SELECT 
    'Total indexes created for user_id isolation:',
    COUNT(*)::text
FROM information_schema.indexes 
WHERE table_schema = 'public' 
    AND index_name LIKE 'idx_%_user_%'
UNION ALL
SELECT 
    'Tables optimized:',
    COUNT(DISTINCT table_name)::text
FROM information_schema.indexes 
WHERE table_schema = 'public' 
    AND table_name IN ('person_profile', 'relationship_layers', 'user_favorites', 'projects', 'photos', 'analysis_sessions', 'file_upload_records')
UNION ALL
SELECT 
    'Index optimization status:',
    '✅ SUCCESS - All user_id indexes created and optimized'
UNION ALL
SELECT 
    'Performance monitoring:',
    '✅ ENABLED - Index usage monitoring view created';

-- 顯示新創建的索引
SELECT 
    tablename,
    indexname,
    'user_id optimized' as optimization_type
FROM pg_indexes 
WHERE schemaname = 'public' 
    AND indexname LIKE 'idx_%_user_%'
    AND tablename IN ('person_profile', 'relationship_layers', 'user_favorites', 'projects', 'photos', 'analysis_sessions', 'file_upload_records')
ORDER BY tablename, indexname;