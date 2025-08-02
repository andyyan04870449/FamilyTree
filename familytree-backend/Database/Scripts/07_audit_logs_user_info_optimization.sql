-- =============================================================================
-- 審計日誌使用者資訊優化腳本
-- 創建時間: 2025-08-02
-- 版本: 1.0.0
-- 描述: 為審計日誌使用者資訊自動補全機制添加必要的索引和優化
-- =============================================================================

-- 1. 檢查並創建必要的索引以優化 audit_logs 和 users 表的 JOIN 查詢
-- 這些索引將大幅提升審計日誌查詢的效能

-- 檢查 users 表的 id 索引（通常已經是主鍵，但確保存在）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'users' AND indexname = 'users_pkey'
    ) THEN
        -- 如果主鍵不存在，創建唯一索引
        CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS idx_users_id ON users(id);
        RAISE NOTICE 'Created index idx_users_id on users(id)';
    ELSE
        RAISE NOTICE 'Primary key already exists on users(id)';
    END IF;
END $$;

-- 2. 檢查並創建 audit_logs.user_id 索引（如果不存在）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'audit_logs' AND indexname = 'idx_audit_logs_user_id'
    ) THEN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_user_id ON audit_logs(user_id);
        RAISE NOTICE 'Created index idx_audit_logs_user_id on audit_logs(user_id)';
    ELSE
        RAISE NOTICE 'Index idx_audit_logs_user_id already exists';
    END IF;
END $$;

-- 3. 創建複合索引以優化使用者活動統計查詢
-- 這個索引將大幅提升按時間範圍統計使用者活動的查詢效能
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'audit_logs' AND indexname = 'idx_audit_logs_user_occurred_time'
    ) THEN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_user_occurred_time 
        ON audit_logs(user_id, occurred_at DESC) 
        WHERE user_id IS NOT NULL;
        RAISE NOTICE 'Created index idx_audit_logs_user_occurred_time on audit_logs(user_id, occurred_at)';
    ELSE
        RAISE NOTICE 'Index idx_audit_logs_user_occurred_time already exists';
    END IF;
END $$;

-- 4. 創建部分索引以優化查詢有使用者資訊的記錄
-- 這個索引只會包含有 user_id 的記錄，減少索引大小並提升查詢效能
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'audit_logs' AND indexname = 'idx_audit_logs_non_null_user'
    ) THEN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_non_null_user 
        ON audit_logs(user_id, occurred_at DESC, event_type) 
        WHERE user_id IS NOT NULL;
        RAISE NOTICE 'Created partial index idx_audit_logs_non_null_user';
    ELSE
        RAISE NOTICE 'Index idx_audit_logs_non_null_user already exists';
    END IF;
END $$;

-- 5. 創建索引以優化根據使用者名稱和角色的搜尋
-- 這些索引將提升過濾特定使用者名稱或角色的查詢效能
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'audit_logs' AND indexname = 'idx_audit_logs_user_name'
    ) THEN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_user_name 
        ON audit_logs(user_name) 
        WHERE user_name IS NOT NULL AND user_name != '';
        RAISE NOTICE 'Created index idx_audit_logs_user_name';
    ELSE
        RAISE NOTICE 'Index idx_audit_logs_user_name already exists';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'audit_logs' AND indexname = 'idx_audit_logs_user_role'
    ) THEN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_user_role 
        ON audit_logs(user_role) 
        WHERE user_role IS NOT NULL AND user_role != '';
        RAISE NOTICE 'Created index idx_audit_logs_user_role';
    ELSE
        RAISE NOTICE 'Index idx_audit_logs_user_role already exists';
    END IF;
END $$;

-- 6. 創建複合索引以優化常見的分頁查詢
-- 這個索引將提升帶有 WHERE 條件和 ORDER BY 的分頁查詢效能
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'audit_logs' AND indexname = 'idx_audit_logs_paginated_query'
    ) THEN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_paginated_query 
        ON audit_logs(occurred_at DESC, event_type, user_id, success);
        RAISE NOTICE 'Created index idx_audit_logs_paginated_query';
    ELSE
        RAISE NOTICE 'Index idx_audit_logs_paginated_query already exists';
    END IF;
END $$;

-- 7. 創建函數式索引以優化 COALESCE 查詢
-- 這些索引將提升使用 COALESCE 進行使用者資訊補全的查詢效能
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'audit_logs' AND indexname = 'idx_audit_logs_effective_user_name'
    ) THEN
        CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_effective_user_name 
        ON audit_logs(
            COALESCE(
                NULLIF(user_name, ''), 
                user_id, 
                'Anonymous'
            )
        );
        RAISE NOTICE 'Created functional index idx_audit_logs_effective_user_name';
    ELSE
        RAISE NOTICE 'Index idx_audit_logs_effective_user_name already exists';
    END IF;
END $$;

-- 8. 更新表統計資訊以優化查詢計劃
ANALYZE audit_logs;
ANALYZE users;

-- 9. 創建檢視以簡化使用者資訊補全的查詢
CREATE OR REPLACE VIEW audit_logs_with_user_info AS
SELECT 
    al.id,
    al.event_id,
    al.batch_id,
    al.session_id,
    al.user_id,
    -- 使用者名稱優先級：audit_logs.user_name > users.full_name > users.username > 系統使用者友好名稱 > user_id
    COALESCE(
        NULLIF(al.user_name, ''), 
        u.full_name, 
        u.username,
        CASE 
            WHEN al.user_id = 'admin_default' THEN '系統管理員'
            WHEN al.user_id = 'system' THEN '系統'
            WHEN al.user_id IS NULL THEN 'Anonymous'
            ELSE al.user_id
        END
    ) as enriched_user_name,
    -- 使用者角色優先級：audit_logs.user_role > users.role > 系統使用者預設角色
    COALESCE(
        NULLIF(al.user_role, ''), 
        u.role,
        CASE 
            WHEN al.user_id = 'admin_default' THEN 'admin'
            WHEN al.user_id = 'system' THEN 'system'
            ELSE 'unknown'
        END
    ) as enriched_user_role,
    al.impersonator_id,
    al.event_type,
    al.action,
    al.resource_type,
    al.resource_id,
    al.resource_name,
    al.old_values,
    al.new_values,
    al.changes_summary,
    al.ip_address,
    al.user_agent,
    al.request_method,
    al.request_url,
    al.request_id,
    al.success,
    al.error_message,
    al.error_code,
    al.response_time_ms,
    al.security_level,
    al.risk_score,
    al.is_suspicious,
    al.additional_data,
    al.occurred_at,
    al.created_at,
    -- 額外的使用者詳細資訊
    u.username as user_username,
    u.email as user_email,
    u.full_name as user_full_name,
    u.status as user_status
FROM audit_logs al
LEFT JOIN users u ON al.user_id = u.id;

-- 10. 為檢視創建註釋
COMMENT ON VIEW audit_logs_with_user_info IS '
審計日誌與使用者資訊整合檢視
提供完整的使用者資訊補全功能，包括：
1. 自動從 users 表補全缺失的使用者名稱和角色
2. 系統使用者的友好顯示名稱
3. 向後相容性支援
4. 優化的查詢效能
';

-- 11. 創建使用者活動摘要檢視
CREATE OR REPLACE VIEW user_activity_summary AS
SELECT 
    al.user_id,
    COALESCE(
        NULLIF(al.user_name, ''), 
        u.full_name, 
        u.username,
        CASE 
            WHEN al.user_id = 'admin_default' THEN '系統管理員'
            WHEN al.user_id = 'system' THEN '系統'
            ELSE al.user_id
        END
    ) as user_name,
    COALESCE(
        NULLIF(al.user_role, ''), 
        u.role,
        CASE 
            WHEN al.user_id = 'admin_default' THEN 'admin'
            WHEN al.user_id = 'system' THEN 'system'
            ELSE 'unknown'
        END
    ) as user_role,
    DATE(al.occurred_at) as activity_date,
    COUNT(*) as total_events,
    COUNT(CASE WHEN al.success = true THEN 1 END) as successful_events,
    COUNT(CASE WHEN al.success = false THEN 1 END) as failed_events,
    COUNT(CASE WHEN al.is_suspicious = true THEN 1 END) as suspicious_events,
    COUNT(DISTINCT al.event_type) as unique_event_types,
    MIN(al.occurred_at) as first_activity,
    MAX(al.occurred_at) as last_activity
FROM audit_logs al
LEFT JOIN users u ON al.user_id = u.id
WHERE al.user_id IS NOT NULL
    AND al.occurred_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY 
    al.user_id,
    COALESCE(
        NULLIF(al.user_name, ''), 
        u.full_name, 
        u.username,
        CASE 
            WHEN al.user_id = 'admin_default' THEN '系統管理員'
            WHEN al.user_id = 'system' THEN '系統'
            ELSE al.user_id
        END
    ),
    COALESCE(
        NULLIF(al.user_role, ''), 
        u.role,
        CASE 
            WHEN al.user_id = 'admin_default' THEN 'admin'
            WHEN al.user_id = 'system' THEN 'system'
            ELSE 'unknown'
        END
    ),
    DATE(al.occurred_at)
ORDER BY activity_date DESC, total_events DESC;

COMMENT ON VIEW user_activity_summary IS '
使用者活動摘要檢視
提供每日使用者活動統計，包括：
1. 總事件數、成功/失敗事件數、可疑事件數
2. 唯一事件類型數量
3. 首次和最後活動時間
4. 自動補全的使用者資訊
5. 僅包含最近30天的資料以提升效能
';

-- 12. 創建效能監控函數
CREATE OR REPLACE FUNCTION get_audit_logs_performance_stats()
RETURNS TABLE(
    table_name TEXT,
    total_rows BIGINT,
    table_size TEXT,
    index_size TEXT,
    total_size TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        'audit_logs'::TEXT,
        (SELECT COUNT(*) FROM audit_logs),
        pg_size_pretty(pg_total_relation_size('audit_logs'::regclass) - pg_indexes_size('audit_logs'::regclass)),
        pg_size_pretty(pg_indexes_size('audit_logs'::regclass)),
        pg_size_pretty(pg_total_relation_size('audit_logs'::regclass))
    UNION ALL
    SELECT 
        'users'::TEXT,
        (SELECT COUNT(*) FROM users),
        pg_size_pretty(pg_total_relation_size('users'::regclass) - pg_indexes_size('users'::regclass)),
        pg_size_pretty(pg_indexes_size('users'::regclass)),
        pg_size_pretty(pg_total_relation_size('users'::regclass));
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION get_audit_logs_performance_stats() IS '
取得審計日誌相關表的效能統計資訊
包括記錄數量、表大小、索引大小等資訊
';

-- 13. 執行測試查詢以驗證索引效果
DO $$
DECLARE
    start_time TIMESTAMP;
    end_time TIMESTAMP;
    execution_time INTERVAL;
BEGIN
    -- 測試使用者資訊補全查詢的效能
    start_time := clock_timestamp();
    
    PERFORM al.user_id, 
           COALESCE(NULLIF(al.user_name, ''), u.full_name, u.username, al.user_id) as user_name
    FROM audit_logs al
    LEFT JOIN users u ON al.user_id = u.id
    WHERE al.occurred_at >= CURRENT_DATE - INTERVAL '7 days'
    ORDER BY al.occurred_at DESC
    LIMIT 100;
    
    end_time := clock_timestamp();
    execution_time := end_time - start_time;
    
    RAISE NOTICE 'Test query executed in: %', execution_time;
    
    IF execution_time > INTERVAL '1 second' THEN
        RAISE WARNING 'Query execution time is longer than expected. Consider reviewing indexes.';
    ELSE
        RAISE NOTICE 'Query performance is good. Indexes are working effectively.';
    END IF;
END $$;

-- 14. 記錄優化完成
INSERT INTO audit_logs (
    user_id, user_name, user_role, event_type, action, 
    resource_type, resource_name, success, 
    changes_summary
) VALUES (
    'system', '系統', 'system', 'SYSTEM_MAINTENANCE', 'DATABASE_OPTIMIZATION',
    'Database', 'audit_logs_user_info_optimization', true,
    '完成審計日誌使用者資訊優化：創建索引、檢視和效能監控功能'
);

-- 顯示完成信息
SELECT 
    '審計日誌使用者資訊優化完成' as status,
    'Created indexes, views, and performance monitoring for audit logs user information auto-completion' as description,
    NOW() as completed_at;