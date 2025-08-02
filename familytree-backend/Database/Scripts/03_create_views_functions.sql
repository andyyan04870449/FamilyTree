-- =============================================
-- 使用者帳號管理系統 - 建立檢視和函數
-- 執行順序：3
-- 功能：建立輔助檢視和權限檢查函數
-- =============================================

-- 1. 建立使用者統計檢視
DROP VIEW IF EXISTS v_user_statistics;
CREATE VIEW v_user_statistics AS
SELECT 
    u.id as user_id,
    u.username,
    u.email,
    u.full_name,
    u.role,
    u.status,
    u.created_at,
    u.last_login_at,
    COALESCE((SELECT COUNT(*) FROM person_profile WHERE user_id = u.id), 0) as person_count,
    COALESCE((SELECT COUNT(*) FROM favorites WHERE user_id = u.id), 0) as favorite_count,
    COALESCE((SELECT COUNT(*) FROM analysis_sessions WHERE user_id = u.id), 0) as analysis_count,
    COALESCE((SELECT COUNT(*) FROM activity_logs WHERE user_id = u.id AND created_at > CURRENT_DATE - INTERVAL '30 days'), 0) as recent_activities
FROM users u
WHERE u.status = 'active';

-- 新增註解
COMMENT ON VIEW v_user_statistics IS '使用者統計資料檢視';

-- 2. 建立使用者活動摘要檢視
DROP VIEW IF EXISTS v_user_activity_summary;
CREATE VIEW v_user_activity_summary AS
SELECT 
    u.id as user_id,
    u.username,
    al.action,
    al.resource_type,
    al.resource_id,
    al.created_at,
    al.ip_address
FROM users u
JOIN activity_logs al ON u.id = al.user_id
ORDER BY al.created_at DESC;

-- 新增註解
COMMENT ON VIEW v_user_activity_summary IS '使用者活動摘要檢視';

-- 3. 建立資料擁有權檢查函數
CREATE OR REPLACE FUNCTION check_data_access(
    p_user_id VARCHAR(50),
    p_resource_type VARCHAR(50),
    p_resource_id VARCHAR(100),
    p_owner_user_id VARCHAR(50)
) RETURNS BOOLEAN AS $$
DECLARE
    user_role VARCHAR(20);
BEGIN
    -- 檢查使用者是否存在且啟用
    SELECT role INTO user_role 
    FROM users 
    WHERE id = p_user_id AND status = 'active';
    
    -- 使用者不存在或未啟用
    IF user_role IS NULL THEN
        RETURN FALSE;
    END IF;
    
    -- 系統管理員可以存取所有資料
    IF user_role = 'admin' THEN
        RETURN TRUE;
    END IF;
    
    -- 一般使用者只能存取自己的資料
    RETURN p_user_id = p_owner_user_id;
END;
$$ LANGUAGE plpgsql;

-- 新增註解
COMMENT ON FUNCTION check_data_access IS '檢查使用者是否可以存取特定資料';

-- 4. 建立使用者權限檢查函數
CREATE OR REPLACE FUNCTION check_user_permission(
    p_user_id VARCHAR(50),
    p_action VARCHAR(100)
) RETURNS BOOLEAN AS $$
DECLARE
    user_role VARCHAR(20);
    user_status VARCHAR(20);
BEGIN
    -- 取得使用者角色和狀態
    SELECT role, status INTO user_role, user_status
    FROM users 
    WHERE id = p_user_id;
    
    -- 使用者不存在或未啟用
    IF user_status IS NULL OR user_status != 'active' THEN
        RETURN FALSE;
    END IF;
    
    -- 管理員可以執行所有操作
    IF user_role = 'admin' THEN
        RETURN TRUE;
    END IF;
    
    -- 一般使用者的權限檢查
    CASE p_action
        -- 允許的操作
        WHEN 'view_own_data', 'create_person', 'update_person', 'delete_person',
             'create_analysis', 'view_analysis', 'export_data' THEN
            RETURN TRUE;
        -- 不允許的操作
        WHEN 'manage_users', 'view_all_data', 'system_admin' THEN
            RETURN FALSE;
        ELSE
            -- 預設不允許
            RETURN FALSE;
    END CASE;
END;
$$ LANGUAGE plpgsql;

-- 新增註解
COMMENT ON FUNCTION check_user_permission IS '檢查使用者是否有權限執行特定操作';

-- 5. 建立記錄活動日誌的函數
CREATE OR REPLACE FUNCTION log_activity(
    p_user_id VARCHAR(50),
    p_action VARCHAR(100),
    p_resource_type VARCHAR(50) DEFAULT NULL,
    p_resource_id VARCHAR(100) DEFAULT NULL,
    p_details TEXT DEFAULT NULL,
    p_ip_address VARCHAR(45) DEFAULT NULL
) RETURNS VOID AS $$
BEGIN
    INSERT INTO activity_logs (user_id, action, resource_type, resource_id, details, ip_address)
    VALUES (p_user_id, p_action, p_resource_type, p_resource_id, p_details, p_ip_address);
END;
$$ LANGUAGE plpgsql;

-- 新增註解
COMMENT ON FUNCTION log_activity IS '記錄使用者活動到日誌表';

-- 6. 建立清理過期 Token 的函數
CREATE OR REPLACE FUNCTION cleanup_expired_tokens() RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM user_tokens 
    WHERE expires_at < CURRENT_TIMESTAMP;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    -- 記錄清理結果
    IF deleted_count > 0 THEN
        PERFORM log_activity(
            'system',
            'cleanup_tokens',
            'system',
            NULL,
            format('Deleted %s expired tokens', deleted_count)
        );
    END IF;
    
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- 新增註解
COMMENT ON FUNCTION cleanup_expired_tokens IS '清理過期的 Token';

-- 7. 建立取得使用者資料摘要的函數
CREATE OR REPLACE FUNCTION get_user_data_summary(p_user_id VARCHAR(50))
RETURNS TABLE (
    data_type VARCHAR(50),
    count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 'persons'::VARCHAR(50), COUNT(*)::BIGINT FROM person_profile WHERE user_id = p_user_id
    UNION ALL
    SELECT 'favorites'::VARCHAR(50), COUNT(*)::BIGINT FROM favorites WHERE user_id = p_user_id
    UNION ALL
    SELECT 'analyses'::VARCHAR(50), COUNT(*)::BIGINT FROM analysis_sessions WHERE user_id = p_user_id
    UNION ALL
    SELECT 'field_mappings'::VARCHAR(50), COUNT(*)::BIGINT FROM field_mapping WHERE user_id = p_user_id;
END;
$$ LANGUAGE plpgsql;

-- 新增註解
COMMENT ON FUNCTION get_user_data_summary IS '取得使用者資料摘要統計';

-- 8. 記錄執行日誌
INSERT INTO activity_logs (user_id, action, details) 
VALUES (
    'admin_default',
    'database_migration',
    '建立檢視和函數'
);

-- 顯示建立結果
SELECT 
    'Views and Functions created successfully' as result,
    (SELECT COUNT(*) FROM information_schema.views WHERE table_name LIKE 'v_user%') as view_count,
    (SELECT COUNT(*) FROM information_schema.routines WHERE routine_type = 'FUNCTION' AND routine_name IN ('check_data_access', 'check_user_permission', 'log_activity', 'cleanup_expired_tokens', 'get_user_data_summary')) as function_count;