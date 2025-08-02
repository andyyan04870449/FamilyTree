-- =============================================================================
-- 審計日誌使用者資訊回填腳本
-- 創建時間: 2025-08-02
-- 版本: 1.0.0
-- 描述: 批量更新現有審計日誌中缺失的使用者資訊
-- =============================================================================

-- 此腳本的目的：
-- 1. 為現有的審計日誌記錄補全缺失的 user_name 和 user_role 資訊
-- 2. 處理系統使用者的友好顯示名稱
-- 3. 確保資料一致性和完整性
-- 4. 提供回滾機制

-- 安全檢查：先備份當前狀態
DO $$
DECLARE
    backup_count INTEGER;
    null_username_count INTEGER;
    null_userrole_count INTEGER;
BEGIN
    -- 統計需要處理的記錄數量
    SELECT COUNT(*) INTO null_username_count 
    FROM audit_logs 
    WHERE user_id IS NOT NULL AND (user_name IS NULL OR user_name = '');
    
    SELECT COUNT(*) INTO null_userrole_count 
    FROM audit_logs 
    WHERE user_id IS NOT NULL AND (user_role IS NULL OR user_role = '');
    
    RAISE NOTICE '=== 審計日誌使用者資訊回填開始 ===';
    RAISE NOTICE '需要補全 user_name 的記錄數量: %', null_username_count;
    RAISE NOTICE '需要補全 user_role 的記錄數量: %', null_userrole_count;
    
    IF null_username_count = 0 AND null_userrole_count = 0 THEN
        RAISE NOTICE '所有記錄的使用者資訊都已完整，無需回填';
        RETURN;
    END IF;
END $$;

-- 1. 創建臨時備份表（如果需要回滾）
DROP TABLE IF EXISTS audit_logs_backup_before_user_info_backfill;
CREATE TABLE audit_logs_backup_before_user_info_backfill AS 
SELECT id, user_id, user_name, user_role, occurred_at 
FROM audit_logs 
WHERE user_id IS NOT NULL AND (
    (user_name IS NULL OR user_name = '') OR 
    (user_role IS NULL OR user_role = '')
);

-- 記錄備份完成
DO $$
DECLARE
    backup_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO backup_count FROM audit_logs_backup_before_user_info_backfill;
    RAISE NOTICE '已備份 % 條記錄到臨時表', backup_count;
END $$;

-- 2. 更新系統使用者的資訊（最高優先級）
UPDATE audit_logs 
SET 
    user_name = CASE 
        WHEN user_id = 'admin_default' THEN '系統管理員'
        WHEN user_id = 'system' THEN '系統'
        WHEN user_id = 'anonymous' THEN 'Anonymous'
        ELSE user_name
    END,
    user_role = CASE 
        WHEN user_id = 'admin_default' THEN 'admin'
        WHEN user_id = 'system' THEN 'system'
        WHEN user_id = 'anonymous' THEN 'anonymous'
        ELSE user_role
    END
WHERE user_id IN ('admin_default', 'system', 'anonymous')
    AND (
        (user_name IS NULL OR user_name = '') OR 
        (user_role IS NULL OR user_role = '')
    );

-- 記錄系統使用者更新結果
DO $$
DECLARE
    system_updated INTEGER;
BEGIN
    GET DIAGNOSTICS system_updated = ROW_COUNT;
    RAISE NOTICE '已更新 % 條系統使用者記錄', system_updated;
END $$;

-- 3. 從 users 表補全一般使用者的資訊
-- 先更新 user_name
UPDATE audit_logs al
SET user_name = COALESCE(u.full_name, u.username, al.user_id)
FROM users u
WHERE al.user_id = u.id
    AND al.user_id NOT IN ('admin_default', 'system', 'anonymous')
    AND (al.user_name IS NULL OR al.user_name = '')
    AND u.id IS NOT NULL;

-- 記錄 user_name 更新結果
DO $$
DECLARE
    username_updated INTEGER;
BEGIN
    GET DIAGNOSTICS username_updated = ROW_COUNT;
    RAISE NOTICE '已從 users 表補全 % 條記錄的 user_name', username_updated;
END $$;

-- 更新 user_role
UPDATE audit_logs al
SET user_role = COALESCE(u.role, 'user')
FROM users u
WHERE al.user_id = u.id
    AND al.user_id NOT IN ('admin_default', 'system', 'anonymous')
    AND (al.user_role IS NULL OR al.user_role = '')
    AND u.id IS NOT NULL;

-- 記錄 user_role 更新結果
DO $$
DECLARE
    userrole_updated INTEGER;
BEGIN
    GET DIAGNOSTICS userrole_updated = ROW_COUNT;
    RAISE NOTICE '已從 users 表補全 % 條記錄的 user_role', userrole_updated;
END $$;

-- 4. 處理仍然缺失資訊的記錄（使用 user_id 作為 fallback）
UPDATE audit_logs 
SET 
    user_name = COALESCE(NULLIF(user_name, ''), user_id, 'Unknown'),
    user_role = COALESCE(NULLIF(user_role, ''), 'unknown')
WHERE user_id IS NOT NULL 
    AND (
        (user_name IS NULL OR user_name = '') OR 
        (user_role IS NULL OR user_role = '')
    );

-- 記錄 fallback 更新結果
DO $$
DECLARE
    fallback_updated INTEGER;
BEGIN
    GET DIAGNOSTICS fallback_updated = ROW_COUNT;
    RAISE NOTICE '已使用 fallback 值更新 % 條記錄', fallback_updated;
END $$;

-- 5. 資料完整性檢查
DO $$
DECLARE
    remaining_null_username INTEGER;
    remaining_null_userrole INTEGER;
    total_records INTEGER;
    complete_records INTEGER;
BEGIN
    -- 檢查剩餘的空值
    SELECT COUNT(*) INTO remaining_null_username 
    FROM audit_logs 
    WHERE user_id IS NOT NULL AND (user_name IS NULL OR user_name = '');
    
    SELECT COUNT(*) INTO remaining_null_userrole 
    FROM audit_logs 
    WHERE user_id IS NOT NULL AND (user_role IS NULL OR user_role = '');
    
    -- 統計總記錄和完整記錄
    SELECT COUNT(*) INTO total_records FROM audit_logs WHERE user_id IS NOT NULL;
    SELECT COUNT(*) INTO complete_records 
    FROM audit_logs 
    WHERE user_id IS NOT NULL 
        AND user_name IS NOT NULL AND user_name != ''
        AND user_role IS NOT NULL AND user_role != '';
    
    RAISE NOTICE '=== 回填完成統計 ===';
    RAISE NOTICE '總記錄數（有 user_id）: %', total_records;
    RAISE NOTICE '完整記錄數: %', complete_records;
    RAISE NOTICE '剩餘空 user_name: %', remaining_null_username;
    RAISE NOTICE '剩餘空 user_role: %', remaining_null_userrole;
    RAISE NOTICE '完整率: %.2f%%', (complete_records::NUMERIC / NULLIF(total_records, 0) * 100);
    
    IF remaining_null_username > 0 OR remaining_null_userrole > 0 THEN
        RAISE WARNING '仍有記錄包含空的使用者資訊，可能需要人工檢查';
    ELSE
        RAISE NOTICE '所有記錄的使用者資訊已完整補全';
    END IF;
END $$;

-- 6. 創建回填報告
CREATE TEMP TABLE backfill_report AS
SELECT 
    al.user_id,
    al.user_name,
    al.user_role,
    u.username as users_table_username,
    u.full_name as users_table_full_name,
    u.role as users_table_role,
    COUNT(*) as log_count,
    MIN(al.occurred_at) as first_log,
    MAX(al.occurred_at) as last_log
FROM audit_logs al
LEFT JOIN users u ON al.user_id = u.id
WHERE al.user_id IS NOT NULL
GROUP BY al.user_id, al.user_name, al.user_role, u.username, u.full_name, u.role
ORDER BY log_count DESC;

-- 顯示前10名最活躍使用者的回填結果
DO $$
DECLARE
    report_record RECORD;
    counter INTEGER := 0;
BEGIN
    RAISE NOTICE '=== 前10名最活躍使用者的回填結果 ===';
    
    FOR report_record IN 
        SELECT * FROM backfill_report LIMIT 10
    LOOP
        counter := counter + 1;
        RAISE NOTICE '%| UserID: % | UserName: % | UserRole: % | LogCount: %', 
            counter, report_record.user_id, report_record.user_name, 
            report_record.user_role, report_record.log_count;
    END LOOP;
END $$;

-- 7. 創建使用者資訊一致性檢查函數
CREATE OR REPLACE FUNCTION check_audit_logs_user_info_consistency()
RETURNS TABLE(
    user_id TEXT,
    audit_user_name TEXT,
    audit_user_role TEXT,
    users_full_name TEXT,
    users_role TEXT,
    is_consistent BOOLEAN,
    log_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        al.user_id::TEXT,
        al.user_name::TEXT,
        al.user_role::TEXT,
        u.full_name::TEXT,
        u.role::TEXT,
        CASE 
            WHEN al.user_id IN ('admin_default', 'system', 'anonymous') THEN true
            WHEN u.id IS NULL THEN false -- 使用者不存在於 users 表
            WHEN al.user_name = COALESCE(u.full_name, u.username) 
                 AND al.user_role = u.role THEN true
            ELSE false
        END as is_consistent,
        COUNT(*)::BIGINT as log_count
    FROM (
        SELECT DISTINCT user_id, user_name, user_role 
        FROM audit_logs 
        WHERE user_id IS NOT NULL
    ) al
    LEFT JOIN users u ON al.user_id = u.id
    GROUP BY al.user_id, al.user_name, al.user_role, u.full_name, u.role
    ORDER BY log_count DESC;
END;
$$ LANGUAGE plpgsql;

-- 8. 建立清理函數（清理備份表）
CREATE OR REPLACE FUNCTION cleanup_audit_logs_backfill_backup()
RETURNS VOID AS $$
BEGIN
    DROP TABLE IF EXISTS audit_logs_backup_before_user_info_backfill;
    RAISE NOTICE '已清理回填前的備份表';
END;
$$ LANGUAGE plpgsql;

-- 9. 記錄回填操作到審計日誌
INSERT INTO audit_logs (
    user_id, user_name, user_role, event_type, action, 
    resource_type, resource_name, success, 
    changes_summary, additional_data
) VALUES (
    'system', '系統', 'system', 'SYSTEM_MAINTENANCE', 'DATA_BACKFILL',
    'Database', 'audit_logs_user_info_backfill', true,
    '批量回填審計日誌中缺失的使用者資訊',
    jsonb_build_object(
        'script_version', '1.0.0',
        'execution_time', NOW(),
        'affected_tables', ARRAY['audit_logs'],
        'backup_table', 'audit_logs_backup_before_user_info_backfill'
    )
);

-- 10. 提供使用指南
DO $$
BEGIN
    RAISE NOTICE '=== 回填操作完成 ===';
    RAISE NOTICE '1. 如需檢查一致性，執行: SELECT * FROM check_audit_logs_user_info_consistency();';
    RAISE NOTICE '2. 如需清理備份表，執行: SELECT cleanup_audit_logs_backfill_backup();';
    RAISE NOTICE '3. 備份表名稱: audit_logs_backup_before_user_info_backfill';
    RAISE NOTICE '4. 如需回滾，可從備份表復原對應記錄的 user_name 和 user_role';
END $$;

-- 最終統計
SELECT 
    '審計日誌使用者資訊回填完成' as status,
    COUNT(*) as total_audit_logs,
    COUNT(CASE WHEN user_id IS NOT NULL THEN 1 END) as logs_with_user_id,
    COUNT(CASE WHEN user_id IS NOT NULL AND user_name IS NOT NULL AND user_name != '' THEN 1 END) as logs_with_user_name,
    COUNT(CASE WHEN user_id IS NOT NULL AND user_role IS NOT NULL AND user_role != '' THEN 1 END) as logs_with_user_role,
    NOW() as completed_at
FROM audit_logs;