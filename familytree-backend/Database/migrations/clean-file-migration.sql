-- 檔案系統優化 - 資料遷移腳本
-- 執行日期: 2025-08-02
-- 目標: 將 user_update_file 表資料正確遷移到 file_uploads

-- 檢查現有資料
SELECT 
    'BEFORE MIGRATION' as step,
    (SELECT COUNT(*) FROM user_update_file) as old_table_count,
    (SELECT COUNT(*) FROM file_uploads) as new_table_count;

-- 遷移資料到新表
INSERT INTO file_uploads (
    user_id,
    filename,
    original_filename,
    file_path,
    file_size,
    md5_hash,
    file_type,
    mime_type,
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
    -- 目前所有檔案都使用預設用戶，因為 project_id 格式不是 UUID
    'admin_default' as user_id,
    
    uf.filename,
    uf.original_filename,
    uf.file_path,
    uf.file_size,
    uf.md5_hash,
    
    -- 根據檔案擴展名判斷檔案類型
    CASE 
        WHEN uf.original_filename LIKE '%.xlsx' THEN 'excel'
        WHEN uf.original_filename LIKE '%.xls' THEN 'excel'
        WHEN uf.original_filename LIKE '%.csv' THEN 'csv'
        WHEN uf.original_filename LIKE '%.pdf' THEN 'pdf'
        WHEN uf.original_filename LIKE '%.jpg' OR uf.original_filename LIKE '%.jpeg' THEN 'image'
        WHEN uf.original_filename LIKE '%.png' THEN 'image'
        WHEN uf.original_filename LIKE '%.zip' THEN 'archive'
        WHEN uf.original_filename LIKE '%.7z' THEN 'archive'
        ELSE 'unknown'
    END as file_type,
    
    -- 設定 MIME 類型
    CASE 
        WHEN uf.original_filename LIKE '%.xlsx' THEN 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
        WHEN uf.original_filename LIKE '%.xls' THEN 'application/vnd.ms-excel'
        WHEN uf.original_filename LIKE '%.csv' THEN 'text/csv'
        WHEN uf.original_filename LIKE '%.pdf' THEN 'application/pdf'
        WHEN uf.original_filename LIKE '%.jpg' OR uf.original_filename LIKE '%.jpeg' THEN 'image/jpeg'
        WHEN uf.original_filename LIKE '%.png' THEN 'image/png'
        WHEN uf.original_filename LIKE '%.zip' THEN 'application/zip'
        WHEN uf.original_filename LIKE '%.7z' THEN 'application/x-7z-compressed'
        ELSE 'application/octet-stream'
    END as mime_type,
    
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

FROM user_update_file uf
WHERE NOT EXISTS (
    SELECT 1 FROM file_uploads fu 
    WHERE fu.md5_hash = uf.md5_hash 
    AND fu.original_filename = uf.original_filename
);

-- 驗證遷移結果
SELECT 
    'AFTER MIGRATION' as step,
    (SELECT COUNT(*) FROM user_update_file) as old_table_count,
    (SELECT COUNT(*) FROM file_uploads) as new_table_count,
    (SELECT COUNT(*) FROM file_uploads WHERE user_id = 'admin_default') as default_user_count,
    (SELECT COUNT(*) FROM file_uploads WHERE associated_record_type = 'project') as project_associated_count;

-- 顯示遷移的資料
SELECT 
    file_id,
    user_id,
    original_filename,
    file_type,
    mime_type,
    file_size,
    upload_status,
    associated_record_type,
    associated_record_id,
    uploaded_at
FROM file_uploads
ORDER BY uploaded_at DESC;