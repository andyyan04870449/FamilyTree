-- 003_migrate_activity_logs_to_audit_logs.sql
-- 將舊的 activity_logs 資料遷移到新的 audit_logs 表

BEGIN;

-- 1. 遷移現有的 activity_logs 資料到 audit_logs
INSERT INTO audit_logs (
    event_id,
    user_id,
    event_type,
    action,
    resource_type,
    resource_id,
    changes_summary,
    ip_address,
    success,
    occurred_at,
    created_at
)
SELECT 
    gen_random_uuid() as event_id,
    user_id,
    'LEGACY_ACTIVITY' as event_type,
    action,
    resource_type,
    resource_id,
    details as changes_summary,
    ip_address::inet,
    true as success,
    created_at as occurred_at,
    created_at
FROM activity_logs
WHERE NOT EXISTS (
    -- 避免重複遷移
    SELECT 1 FROM audit_logs 
    WHERE audit_logs.event_type = 'LEGACY_ACTIVITY'
);

-- 2. 顯示遷移結果
DO $$
DECLARE
    migrated_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO migrated_count 
    FROM audit_logs 
    WHERE event_type = 'LEGACY_ACTIVITY';
    
    RAISE NOTICE '已成功遷移 % 筆記錄從 activity_logs 到 audit_logs', migrated_count;
END $$;

-- 3. 重新命名舊表（保留備份）
ALTER TABLE activity_logs RENAME TO activity_logs_backup;

-- 4. 創建視圖以保持向後相容（如果有程式碼還在使用 activity_logs）
CREATE OR REPLACE VIEW activity_logs AS
SELECT 
    id::bigint as id,
    user_id,
    action,
    resource_type,
    resource_id,
    changes_summary as details,
    ip_address::varchar(45) as ip_address,
    created_at
FROM audit_logs
WHERE event_type = 'LEGACY_ACTIVITY';

-- 5. 添加註解說明
COMMENT ON VIEW activity_logs IS '向後相容視圖，實際資料已遷移至 audit_logs 表';
COMMENT ON TABLE activity_logs_backup IS '原始 activity_logs 表備份，資料已遷移至 audit_logs';

COMMIT;

-- 顯示遷移後的統計
SELECT 
    'audit_logs' as table_name,
    COUNT(*) as record_count,
    COUNT(DISTINCT event_type) as event_types
FROM audit_logs
UNION ALL
SELECT 
    'activity_logs_backup' as table_name,
    COUNT(*) as record_count,
    1 as event_types
FROM activity_logs_backup;