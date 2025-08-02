-- 檔案上傳命名標準化 - Phase 1: 資料庫表遷移
-- 執行日期: 2025-08-02
-- 目標: 將 user_update_file 表重構為 file_uploads，並修正命名不一致問題

-- =====================================================
-- 第一步：備份現有資料
-- =====================================================

-- 創建備份表
CREATE TABLE user_update_file_backup_20250802 AS 
SELECT * FROM user_update_file;

-- 驗證備份
SELECT 
    'BACKUP VERIFICATION' as step,
    COUNT(*) as original_count,
    (SELECT COUNT(*) FROM user_update_file_backup_20250802) as backup_count,
    CASE 
        WHEN COUNT(*) = (SELECT COUNT(*) FROM user_update_file_backup_20250802) 
        THEN '✅ BACKUP SUCCESS' 
        ELSE '❌ BACKUP FAILED' 
    END as status
FROM user_update_file;

-- =====================================================
-- 第二步：創建新的 file_uploads 表
-- =====================================================

CREATE TABLE file_uploads (
    file_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id VARCHAR(50) NOT NULL,
    
    -- 檔案基本資訊
    filename VARCHAR(255) NOT NULL,
    original_filename VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    md5_hash VARCHAR(32) NOT NULL,
    file_type VARCHAR(50),
    mime_type VARCHAR(100),
    
    -- 關聯資訊 (取代原本的 project_id)
    associated_record_id VARCHAR(50),
    associated_record_type VARCHAR(50), -- 'person', 'project', 'analysis', etc.
    
    -- 狀態管理
    upload_status VARCHAR(50) DEFAULT 'uploaded',
    is_processed BOOLEAN DEFAULT FALSE,
    processed_at TIMESTAMP,
    
    -- 審計欄位
    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- 約束條件
    CONSTRAINT fk_file_uploads_user_id 
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,
    CONSTRAINT chk_file_uploads_status 
        CHECK (upload_status IN ('uploaded', 'processing', 'processed', 'failed', 'deleted')),
    CONSTRAINT chk_file_uploads_record_type 
        CHECK (associated_record_type IS NULL OR 
               associated_record_type IN ('person', 'project', 'analysis', 'photo', 'document'))
);

-- 創建必要索引
CREATE INDEX idx_file_uploads_user_id ON file_uploads(user_id);
CREATE INDEX idx_file_uploads_md5_hash ON file_uploads(md5_hash);
CREATE INDEX idx_file_uploads_status ON file_uploads(upload_status);
CREATE INDEX idx_file_uploads_uploaded_at ON file_uploads(uploaded_at DESC);
CREATE INDEX idx_file_uploads_associated_record ON file_uploads(associated_record_type, associated_record_id);

-- 創建複合索引
CREATE INDEX idx_file_uploads_user_status ON file_uploads(user_id, upload_status);
CREATE INDEX idx_file_uploads_user_type ON file_uploads(user_id, associated_record_type);

-- =====================================================
-- 第三步：資料遷移
-- =====================================================

-- 分析現有資料的 project_id 模式
SELECT 
    'DATA ANALYSIS' as step,
    COUNT(*) as total_files,
    COUNT(DISTINCT project_id) as unique_project_ids,
    COUNT(CASE WHEN project_id IS NULL THEN 1 END) as null_project_ids,
    STRING_AGG(DISTINCT 
        CASE 
            WHEN LENGTH(project_id) < 10 THEN 'short_id' 
            WHEN project_id LIKE '%-%' THEN 'uuid_format'
            ELSE 'other_format'
        END, ', ') as project_id_patterns
FROM user_update_file;

-- 遷移資料到新表
INSERT INTO file_uploads (
    user_id,
    filename,
    original_filename,
    file_path,
    file_size,
    md5_hash,
    file_type,
    associated_record_id,
    associated_record_type,
    upload_status,
    is_processed,
    processed_at,
    uploaded_at,
    created_at,
    updated_at
)
SELECT 
    -- 從 project_id 推導 user_id (如果 project_id 存在且為 UUID 格式)
    CASE 
        WHEN uf.project_id IS NOT NULL AND LENGTH(uf.project_id) = 36 AND uf.project_id LIKE '%-%-%-%-%'
        THEN (SELECT p.user_id FROM projects p WHERE p.id = uf.project_id LIMIT 1)
        ELSE 'admin_default' -- 預設管理員帳號
    END as user_id,
    
    uf.filename,
    uf.original_filename,
    uf.file_path,
    uf.file_size,
    uf.md5_hash,
    CASE 
        WHEN uf.original_filename LIKE '%.xlsx' THEN 'excel'
        WHEN uf.original_filename LIKE '%.xls' THEN 'excel'
        WHEN uf.original_filename LIKE '%.csv' THEN 'csv'
        WHEN uf.original_filename LIKE '%.pdf' THEN 'pdf'
        ELSE 'unknown'
    END as file_type,
    
    uf.project_id as associated_record_id,
    CASE 
        WHEN uf.project_id IS NOT NULL THEN 'project'
        ELSE NULL
    END as associated_record_type,
    
    COALESCE(uf.status, 'uploaded') as upload_status,
    COALESCE(uf.is_merged, FALSE) as is_processed,
    uf.merge_time as processed_at,
    uf.upload_time as uploaded_at,
    uf.created_at,
    uf.updated_at

FROM user_update_file uf;

-- 驗證遷移結果
SELECT 
    'MIGRATION VERIFICATION' as step,
    (SELECT COUNT(*) FROM user_update_file) as original_count,
    (SELECT COUNT(*) FROM file_uploads) as migrated_count,
    (SELECT COUNT(*) FROM file_uploads WHERE user_id = 'admin_default') as default_user_count,
    (SELECT COUNT(*) FROM file_uploads WHERE associated_record_type = 'project') as project_associated_count,
    CASE 
        WHEN (SELECT COUNT(*) FROM user_update_file) = (SELECT COUNT(*) FROM file_uploads)
        THEN '✅ MIGRATION SUCCESS'
        ELSE '❌ MIGRATION FAILED'
    END as status;

-- =====================================================
-- 第四步：創建檢視以維持向後相容性 (臨時)
-- =====================================================

-- 創建相容性檢視 (供舊代碼使用)
CREATE VIEW user_update_file_compat AS
SELECT 
    ROW_NUMBER() OVER (ORDER BY uploaded_at) as id,
    filename,
    original_filename,
    file_path,
    file_size,
    md5_hash,
    uploaded_at as upload_time,
    is_processed as is_merged,
    processed_at as merge_time,
    upload_status as status,
    created_at,
    updated_at,
    associated_record_id as project_id
FROM file_uploads
ORDER BY uploaded_at DESC;

-- =====================================================
-- 第五步：創建管理函數
-- =====================================================

-- 檔案查詢函數 (基於 user_id)
CREATE OR REPLACE FUNCTION get_user_files(
    p_user_id VARCHAR(50),
    p_file_type VARCHAR(50) DEFAULT NULL,
    p_status VARCHAR(50) DEFAULT NULL,
    p_limit INTEGER DEFAULT 50,
    p_offset INTEGER DEFAULT 0
)
RETURNS TABLE (
    file_id UUID,
    filename VARCHAR(255),
    original_filename VARCHAR(255),
    file_size BIGINT,
    upload_status VARCHAR(50),
    associated_record_type VARCHAR(50),
    uploaded_at TIMESTAMP
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        fu.file_id,
        fu.filename,
        fu.original_filename,
        fu.file_size,
        fu.upload_status,
        fu.associated_record_type,
        fu.uploaded_at
    FROM file_uploads fu
    WHERE fu.user_id = p_user_id
        AND (p_file_type IS NULL OR fu.file_type = p_file_type)
        AND (p_status IS NULL OR fu.upload_status = p_status)
    ORDER BY fu.uploaded_at DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

-- 檔案統計函數
CREATE OR REPLACE FUNCTION get_file_statistics(p_user_id VARCHAR(50) DEFAULT NULL)
RETURNS JSON AS $$
DECLARE
    result JSON;
BEGIN
    SELECT json_build_object(
        'total_files', COUNT(*),
        'total_size_mb', ROUND(SUM(file_size)::NUMERIC / 1024 / 1024, 2),
        'by_type', json_object_agg(
            COALESCE(file_type, 'unknown'), 
            type_count
        ),
        'by_status', json_object_agg(
            upload_status,
            status_count
        )
    ) INTO result
    FROM (
        SELECT 
            file_type,
            upload_status,
            COUNT(*) OVER (PARTITION BY file_type) as type_count,
            COUNT(*) OVER (PARTITION BY upload_status) as status_count,
            file_size
        FROM file_uploads
        WHERE p_user_id IS NULL OR user_id = p_user_id
    ) stats;
    
    RETURN result;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 第六步：觸發器設定
-- =====================================================

-- 自動更新 updated_at 觸發器
CREATE OR REPLACE FUNCTION update_file_uploads_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_file_uploads_updated_at
    BEFORE UPDATE ON file_uploads
    FOR EACH ROW
    EXECUTE FUNCTION update_file_uploads_updated_at();

-- =====================================================
-- 第七步：記錄遷移日誌
-- =====================================================

INSERT INTO data_migration_log (migration_name, executed_by, status, records_affected, notes)
VALUES (
    'FileNamingStandardization-Phase1', 
    'system', 
    'completed',
    (SELECT COUNT(*) FROM file_uploads),
    'Migrated user_update_file table to file_uploads with proper naming conventions. Replaced project_id with user_id + associated_record_id/type pattern. Created compatibility view for backward compatibility.'
);

-- =====================================================
-- 驗證和報告
-- =====================================================

-- 最終驗證報告
SELECT 
    '=== FILE NAMING STANDARDIZATION PHASE 1 SUMMARY ===' as report_section,
    '' as details
UNION ALL
SELECT 
    'Original table records:',
    (SELECT COUNT(*)::text FROM user_update_file)
UNION ALL
SELECT 
    'Migrated records:',
    (SELECT COUNT(*)::text FROM file_uploads)
UNION ALL
SELECT 
    'Records with proper user_id:',
    (SELECT COUNT(*)::text FROM file_uploads WHERE user_id != 'admin_default')
UNION ALL
SELECT 
    'Records with project association:',
    (SELECT COUNT(*)::text FROM file_uploads WHERE associated_record_type = 'project')
UNION ALL
SELECT 
    'Migration status:',
    CASE 
        WHEN (SELECT COUNT(*) FROM user_update_file) = (SELECT COUNT(*) FROM file_uploads)
        THEN '✅ SUCCESS - All files migrated successfully'
        ELSE '❌ INCOMPLETE - Some files not migrated'
    END;

-- 顯示新表統計
SELECT 
    'file_uploads' as table_name,
    user_id,
    associated_record_type,
    upload_status,
    COUNT(*) as file_count,
    ROUND(SUM(file_size)::NUMERIC / 1024 / 1024, 2) as total_size_mb
FROM file_uploads
GROUP BY user_id, associated_record_type, upload_status
ORDER BY user_id, associated_record_type;