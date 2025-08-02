-- =============================================
-- 使用者帳號管理系統資料庫設計
-- =============================================

-- 1. 使用者基本資料表
CREATE TABLE users (
    id VARCHAR(50) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    username VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(200),
    phone VARCHAR(50),
    avatar_url VARCHAR(500),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive', 'suspended', 'deleted')),
    email_verified BOOLEAN DEFAULT FALSE,
    phone_verified BOOLEAN DEFAULT FALSE,
    two_factor_enabled BOOLEAN DEFAULT FALSE,
    two_factor_secret VARCHAR(255),
    language VARCHAR(10) DEFAULT 'zh-TW',
    timezone VARCHAR(50) DEFAULT 'Asia/Taipei',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP,
    last_login_ip VARCHAR(45),
    failed_login_attempts INT DEFAULT 0,
    locked_until TIMESTAMP,
    password_changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    must_change_password BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP,
    metadata JSONB DEFAULT '{}'::jsonb
);

-- 建立索引
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_status ON users(status);
CREATE INDEX idx_users_created_at ON users(created_at);

-- 2. 角色定義表
CREATE TABLE roles (
    id VARCHAR(50) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    name VARCHAR(100) UNIQUE NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    description TEXT,
    is_system BOOLEAN DEFAULT FALSE, -- 系統內建角色不可刪除
    is_default BOOLEAN DEFAULT FALSE, -- 新使用者預設角色
    level INT DEFAULT 0, -- 角色層級，數字越大權限越高
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50),
    metadata JSONB DEFAULT '{}'::jsonb
);

-- 建立索引
CREATE INDEX idx_roles_name ON roles(name);
CREATE INDEX idx_roles_level ON roles(level);

-- 插入預設角色
INSERT INTO roles (id, name, display_name, description, is_system, level) VALUES
('role_superadmin', 'superadmin', '超級管理員', '擁有系統所有權限', true, 100),
('role_admin', 'admin', '管理員', '擁有大部分管理權限', true, 80),
('role_user', 'user', '一般使用者', '一般使用者權限', true, 50),
('role_guest', 'guest', '訪客', '只有基本查看權限', true, 10);

-- 3. 使用者角色關聯表
CREATE TABLE user_roles (
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id VARCHAR(50) NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by VARCHAR(50) REFERENCES users(id),
    expires_at TIMESTAMP, -- 角色過期時間（可選）
    PRIMARY KEY (user_id, role_id)
);

-- 建立索引
CREATE INDEX idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);

-- 4. 權限定義表
CREATE TABLE permissions (
    id VARCHAR(50) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    resource VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    description TEXT,
    is_system BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(resource, action)
);

-- 建立索引
CREATE INDEX idx_permissions_resource ON permissions(resource);
CREATE INDEX idx_permissions_action ON permissions(action);

-- 插入基本權限
INSERT INTO permissions (resource, action, display_name, description, is_system) VALUES
-- 使用者管理權限
('user', 'create', '建立使用者', '可以建立新使用者', true),
('user', 'read', '查看使用者', '可以查看使用者資料', true),
('user', 'update', '更新使用者', '可以更新使用者資料', true),
('user', 'delete', '刪除使用者', '可以刪除使用者', true),
-- 專案管理權限
('project', 'create', '建立專案', '可以建立新專案', true),
('project', 'read', '查看專案', '可以查看專案資料', true),
('project', 'update', '更新專案', '可以更新專案資料', true),
('project', 'delete', '刪除專案', '可以刪除專案', true),
-- 人員資料權限
('person', 'create', '建立人員', '可以建立新人員資料', true),
('person', 'read', '查看人員', '可以查看人員資料', true),
('person', 'update', '更新人員', '可以更新人員資料', true),
('person', 'delete', '刪除人員', '可以刪除人員資料', true),
('person', 'export', '匯出人員', '可以匯出人員資料', true),
-- 檔案管理權限
('file', 'upload', '上傳檔案', '可以上傳檔案', true),
('file', 'download', '下載檔案', '可以下載檔案', true),
('file', 'delete', '刪除檔案', '可以刪除檔案', true),
-- 報表權限
('report', 'view', '查看報表', '可以查看各種報表', true),
('report', 'export', '匯出報表', '可以匯出報表', true),
-- 系統管理權限
('system', 'manage', '系統管理', '可以管理系統設定', true),
('role', 'manage', '角色管理', '可以管理角色和權限', true),
('log', 'view', '查看日誌', '可以查看系統日誌', true);

-- 5. 角色權限關聯表
CREATE TABLE role_permissions (
    role_id VARCHAR(50) NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id VARCHAR(50) NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    granted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    granted_by VARCHAR(50) REFERENCES users(id),
    PRIMARY KEY (role_id, permission_id)
);

-- 建立索引
CREATE INDEX idx_role_permissions_role_id ON role_permissions(role_id);
CREATE INDEX idx_role_permissions_permission_id ON role_permissions(permission_id);

-- 6. 專案成員表（使用者與專案的關聯）
CREATE TABLE project_members (
    project_id VARCHAR(25) NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL CHECK (role IN ('owner', 'admin', 'editor', 'viewer')),
    joined_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    invited_by VARCHAR(50) REFERENCES users(id),
    invitation_accepted BOOLEAN DEFAULT FALSE,
    invitation_token VARCHAR(255),
    invitation_expires_at TIMESTAMP,
    last_accessed_at TIMESTAMP,
    permissions JSONB DEFAULT '{}'::jsonb, -- 專案特定權限覆蓋
    PRIMARY KEY (project_id, user_id)
);

-- 建立索引
CREATE INDEX idx_project_members_project_id ON project_members(project_id);
CREATE INDEX idx_project_members_user_id ON project_members(user_id);
CREATE INDEX idx_project_members_role ON project_members(role);

-- 7. 使用者 Token 表（用於認證）
CREATE TABLE user_tokens (
    id VARCHAR(100) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_type VARCHAR(20) NOT NULL CHECK (token_type IN ('access', 'refresh', 'reset_password', 'verify_email', 'api_key')),
    token_hash VARCHAR(255) NOT NULL, -- 儲存 hash 值而非明文
    device_info JSONB DEFAULT '{}'::jsonb, -- 裝置資訊
    ip_address VARCHAR(45),
    user_agent TEXT,
    last_used_at TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMP,
    revoked_by VARCHAR(50) REFERENCES users(id),
    revoke_reason VARCHAR(255)
);

-- 建立索引
CREATE INDEX idx_user_tokens_user_id ON user_tokens(user_id);
CREATE INDEX idx_user_tokens_token_type ON user_tokens(token_type);
CREATE INDEX idx_user_tokens_token_hash ON user_tokens(token_hash);
CREATE INDEX idx_user_tokens_expires_at ON user_tokens(expires_at);

-- 8. 使用者會話表（用於追蹤登入狀態）
CREATE TABLE user_sessions (
    id VARCHAR(100) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    session_token VARCHAR(255) UNIQUE NOT NULL,
    ip_address VARCHAR(45),
    user_agent TEXT,
    device_type VARCHAR(50),
    device_name VARCHAR(200),
    location JSONB DEFAULT '{}'::jsonb, -- 地理位置資訊
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_activity_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    ended_at TIMESTAMP,
    end_reason VARCHAR(50) -- logout, timeout, revoked, etc.
);

-- 建立索引
CREATE INDEX idx_user_sessions_user_id ON user_sessions(user_id);
CREATE INDEX idx_user_sessions_session_token ON user_sessions(session_token);
CREATE INDEX idx_user_sessions_expires_at ON user_sessions(expires_at);

-- 9. 審計日誌表
CREATE TABLE audit_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(50) REFERENCES users(id),
    action VARCHAR(100) NOT NULL,
    resource_type VARCHAR(50),
    resource_id VARCHAR(100),
    old_values JSONB,
    new_values JSONB,
    details JSONB DEFAULT '{}'::jsonb,
    ip_address VARCHAR(45),
    user_agent TEXT,
    session_id VARCHAR(100),
    status VARCHAR(20) DEFAULT 'success', -- success, failed, error
    error_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 建立索引
CREATE INDEX idx_audit_logs_user_id ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_action ON audit_logs(action);
CREATE INDEX idx_audit_logs_resource ON audit_logs(resource_type, resource_id);
CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at);

-- 10. 登入歷史表
CREATE TABLE login_history (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(50) REFERENCES users(id) ON DELETE CASCADE,
    login_type VARCHAR(20) NOT NULL, -- password, oauth, api_key, etc.
    ip_address VARCHAR(45),
    user_agent TEXT,
    device_info JSONB DEFAULT '{}'::jsonb,
    location JSONB DEFAULT '{}'::jsonb,
    status VARCHAR(20) NOT NULL, -- success, failed, blocked
    failure_reason VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 建立索引
CREATE INDEX idx_login_history_user_id ON login_history(user_id);
CREATE INDEX idx_login_history_created_at ON login_history(created_at);
CREATE INDEX idx_login_history_status ON login_history(status);

-- 11. 密碼歷史表（防止重複使用密碼）
CREATE TABLE password_history (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 建立索引
CREATE INDEX idx_password_history_user_id ON password_history(user_id);

-- 12. API Keys 表（用於程式化存取）
CREATE TABLE api_keys (
    id VARCHAR(50) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name VARCHAR(200) NOT NULL,
    key_hash VARCHAR(255) NOT NULL,
    permissions JSONB DEFAULT '[]'::jsonb, -- 允許的 API 端點
    rate_limit INT DEFAULT 1000, -- 每小時請求限制
    last_used_at TIMESTAMP,
    expires_at TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMP
);

-- 建立索引
CREATE INDEX idx_api_keys_user_id ON api_keys(user_id);
CREATE INDEX idx_api_keys_key_hash ON api_keys(key_hash);

-- 13. 使用者偏好設定表
CREATE TABLE user_preferences (
    user_id VARCHAR(50) PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    theme VARCHAR(20) DEFAULT 'light',
    notification_email BOOLEAN DEFAULT TRUE,
    notification_push BOOLEAN DEFAULT TRUE,
    notification_sms BOOLEAN DEFAULT FALSE,
    privacy_settings JSONB DEFAULT '{}'::jsonb,
    ui_settings JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 14. 通知訂閱表
CREATE TABLE notification_subscriptions (
    id VARCHAR(50) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    channel VARCHAR(50) NOT NULL, -- email, push, sms
    event_type VARCHAR(100) NOT NULL, -- login, project_update, etc.
    is_enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, channel, event_type)
);

-- 建立索引
CREATE INDEX idx_notification_subscriptions_user_id ON notification_subscriptions(user_id);

-- =============================================
-- Triggers
-- =============================================

-- 自動更新 updated_at 時間戳
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 套用 trigger 到需要的表
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_roles_updated_at BEFORE UPDATE ON roles
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_user_preferences_updated_at BEFORE UPDATE ON user_preferences
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- Views
-- =============================================

-- 使用者完整資訊檢視
CREATE VIEW v_user_details AS
SELECT 
    u.id,
    u.username,
    u.email,
    u.full_name,
    u.status,
    u.created_at,
    u.last_login_at,
    COALESCE(
        json_agg(
            DISTINCT jsonb_build_object(
                'role_id', r.id,
                'role_name', r.name,
                'role_display_name', r.display_name,
                'role_level', r.level
            )
        ) FILTER (WHERE r.id IS NOT NULL), 
        '[]'::json
    ) as roles,
    COALESCE(pm.project_count, 0) as project_count
FROM users u
LEFT JOIN user_roles ur ON u.id = ur.user_id
LEFT JOIN roles r ON ur.role_id = r.id
LEFT JOIN (
    SELECT user_id, COUNT(*) as project_count 
    FROM project_members 
    WHERE invitation_accepted = true
    GROUP BY user_id
) pm ON u.id = pm.user_id
WHERE u.status != 'deleted'
GROUP BY u.id, u.username, u.email, u.full_name, u.status, u.created_at, u.last_login_at, pm.project_count;

-- 角色權限檢視
CREATE VIEW v_role_permissions AS
SELECT 
    r.id as role_id,
    r.name as role_name,
    r.display_name as role_display_name,
    p.id as permission_id,
    p.resource,
    p.action,
    p.display_name as permission_display_name
FROM roles r
JOIN role_permissions rp ON r.id = rp.role_id
JOIN permissions p ON rp.permission_id = p.id
ORDER BY r.level DESC, p.resource, p.action;

-- =============================================
-- Functions
-- =============================================

-- 檢查使用者是否有特定權限
CREATE OR REPLACE FUNCTION check_user_permission(
    p_user_id VARCHAR(50),
    p_resource VARCHAR(100),
    p_action VARCHAR(50)
) RETURNS BOOLEAN AS $$
DECLARE
    has_permission BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1
        FROM users u
        JOIN user_roles ur ON u.id = ur.user_id
        JOIN role_permissions rp ON ur.role_id = rp.role_id
        JOIN permissions p ON rp.permission_id = p.id
        WHERE u.id = p_user_id
        AND u.status = 'active'
        AND p.resource = p_resource
        AND p.action = p_action
    ) INTO has_permission;
    
    RETURN has_permission;
END;
$$ LANGUAGE plpgsql;

-- 檢查使用者對專案的存取權限
CREATE OR REPLACE FUNCTION check_project_access(
    p_user_id VARCHAR(50),
    p_project_id VARCHAR(25),
    p_required_role VARCHAR(20)
) RETURNS BOOLEAN AS $$
DECLARE
    user_project_role VARCHAR(20);
    role_levels JSONB := '{"owner": 4, "admin": 3, "editor": 2, "viewer": 1}'::jsonb;
    user_level INT;
    required_level INT;
BEGIN
    -- 檢查是否為超級管理員
    IF EXISTS (
        SELECT 1 FROM user_roles ur
        JOIN roles r ON ur.role_id = r.id
        WHERE ur.user_id = p_user_id AND r.name = 'superadmin'
    ) THEN
        RETURN TRUE;
    END IF;
    
    -- 獲取使用者在專案中的角色
    SELECT role INTO user_project_role
    FROM project_members
    WHERE user_id = p_user_id 
    AND project_id = p_project_id
    AND invitation_accepted = true;
    
    IF user_project_role IS NULL THEN
        RETURN FALSE;
    END IF;
    
    -- 比較角色層級
    user_level := (role_levels->>user_project_role)::INT;
    required_level := (role_levels->>p_required_role)::INT;
    
    RETURN user_level >= required_level;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- 初始資料
-- =============================================

-- 為超級管理員角色賦予所有權限
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'role_superadmin', id FROM permissions;

-- 為管理員角色賦予大部分權限（排除系統管理）
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'role_admin', id FROM permissions
WHERE NOT (resource = 'system' OR resource = 'role');

-- 為一般使用者角色賦予基本權限
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'role_user', id FROM permissions
WHERE resource IN ('project', 'person', 'file', 'report')
AND action IN ('create', 'read', 'update', 'export', 'upload', 'download', 'view');

-- 為訪客角色賦予查看權限
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'role_guest', id FROM permissions
WHERE action IN ('read', 'view', 'download');