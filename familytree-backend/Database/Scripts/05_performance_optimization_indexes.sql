-- =============================================
-- 資料庫性能優化 - 索引建立腳本
-- 執行順序：5
-- 功能：建立高性能查詢索引
-- =============================================

-- 1. person_profile 表索引優化
-- 基礎查詢索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_user_id_active 
ON person_profile(user_id) 
WHERE user_id IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_name 
ON person_profile(name) 
WHERE name IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_birthday 
ON person_profile(birthday) 
WHERE birthday IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_gender 
ON person_profile(gender) 
WHERE gender IS NOT NULL;

-- 複合索引（常用查詢組合）
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_user_name 
ON person_profile(user_id, name) 
WHERE user_id IS NOT NULL AND name IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_user_gender 
ON person_profile(user_id, gender) 
WHERE user_id IS NOT NULL AND gender IS NOT NULL;

-- 全文搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_fulltext 
ON person_profile 
USING gin(to_tsvector('simple', 
    coalesce(name,'') || ' ' || 
    coalesce(email,'') || ' ' || 
    coalesce(mobile,'') || ' ' ||
    coalesce(family_relationships,'') || ' ' ||
    coalesce(friends,'') || ' ' ||
    coalesce(remarks,'')
));

-- 分析用索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_birth_year 
ON person_profile(EXTRACT(YEAR FROM birthday)) 
WHERE birthday IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_created_at 
ON person_profile(created_at DESC);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_person_profile_updated_at 
ON person_profile(updated_at DESC);

-- 2. 建立 relationships 表（如果不存在）
CREATE TABLE IF NOT EXISTS relationships (
    id BIGSERIAL PRIMARY KEY,
    person_id INTEGER NOT NULL,
    related_person_id INTEGER NOT NULL,
    relationship_type VARCHAR(50) NOT NULL,
    description TEXT,
    is_confirmed BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- relationships 表索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_relationships_person 
ON relationships(person_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_relationships_related 
ON relationships(related_person_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_relationships_type 
ON relationships(relationship_type);

-- 複合索引用於關係圖查詢
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_relationships_person_type 
ON relationships(person_id, relationship_type);

-- 雙向關係查詢
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_relationships_bidirectional 
ON relationships(LEAST(person_id, related_person_id), GREATEST(person_id, related_person_id));

-- 防重複索引
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS idx_relationships_unique 
ON relationships(person_id, related_person_id, relationship_type);

-- 3. 建立 file_metadata 表（如果不存在）
CREATE TABLE IF NOT EXISTS file_metadata (
    id BIGSERIAL PRIMARY KEY,
    entity_type VARCHAR(50) NOT NULL,
    entity_id INTEGER NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_type VARCHAR(50),
    file_size BIGINT,
    mime_type VARCHAR(100),
    md5_hash VARCHAR(32),
    uploaded_by VARCHAR(50),
    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN DEFAULT false
);

-- file_metadata 表索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_file_metadata_entity 
ON file_metadata(entity_type, entity_id) 
WHERE is_deleted = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_file_metadata_uploaded_at 
ON file_metadata(uploaded_at DESC) 
WHERE is_deleted = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_file_metadata_type 
ON file_metadata(file_type) 
WHERE is_deleted = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_file_metadata_md5 
ON file_metadata(md5_hash) 
WHERE md5_hash IS NOT NULL;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_file_metadata_uploader 
ON file_metadata(uploaded_by) 
WHERE uploaded_by IS NOT NULL;

-- 4. 優化現有表的索引
-- user_favorites 表優化
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_user_favorites_user_person 
ON user_favorites(user_id, person_id);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_user_favorites_created_at 
ON user_favorites(created_at DESC);

-- analysis_sessions 表優化
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_analysis_sessions_user_created 
ON analysis_sessions(user_id, created_at DESC);

-- analysis_results 表優化
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_analysis_results_session 
ON analysis_results(session_id);

-- file_uploads 表優化
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_file_uploads_user_time 
ON file_uploads(user_id, upload_time DESC);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_file_uploads_md5 
ON file_uploads(md5_hash);

-- 5. 建立觸發器更新 updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 套用到 relationships 表
DROP TRIGGER IF EXISTS update_relationships_updated_at ON relationships;
CREATE TRIGGER update_relationships_updated_at 
    BEFORE UPDATE ON relationships
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- 6. 新增註解
COMMENT ON INDEX idx_person_profile_user_id_active IS '用戶查詢人員列表的主要索引';
COMMENT ON INDEX idx_person_profile_fulltext IS '全文搜索性能優化索引';
COMMENT ON INDEX idx_relationships_bidirectional IS '雙向關係查詢優化索引';
COMMENT ON INDEX idx_file_metadata_entity IS '文件關聯查詢主要索引';

-- 記錄執行日誌
INSERT INTO activity_logs (user_id, action, details) 
VALUES (
    'admin_default',
    'database_optimization',
    '建立性能優化索引 - person_profile, relationships, file_metadata'
);

-- 顯示建立結果
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename IN ('person_profile', 'relationships', 'file_metadata', 'user_favorites', 'analysis_sessions', 'file_uploads')
ORDER BY tablename, indexname;