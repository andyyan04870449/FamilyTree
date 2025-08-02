-- =============================================
-- 使用者帳號管理系統資料庫設計（簡化版）
-- =============================================

-- 1. 使用者表
CREATE TABLE users (
    id VARCHAR(50) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    username VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(200),
    role VARCHAR(20) DEFAULT 'user' CHECK (role IN ('admin', 'user')),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP
);

-- 建立索引
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);

-- 2. 專案成員表（控制專案存取權限）
CREATE TABLE project_members (
    project_id VARCHAR(25) NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    access_level VARCHAR(20) NOT NULL DEFAULT 'viewer' CHECK (access_level IN ('owner', 'editor', 'viewer')),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) REFERENCES users(id),
    PRIMARY KEY (project_id, user_id)
);

-- 建立索引
CREATE INDEX idx_project_members_user_id ON project_members(user_id);
CREATE INDEX idx_project_members_project_id ON project_members(project_id);

-- 3. 使用者 Token 表（JWT Refresh Token 儲存）
CREATE TABLE user_tokens (
    id VARCHAR(100) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_type VARCHAR(20) NOT NULL CHECK (token_type IN ('refresh')),
    token_hash VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    used_at TIMESTAMP
);

-- 建立索引
CREATE INDEX idx_user_tokens_user_id ON user_tokens(user_id);
CREATE INDEX idx_user_tokens_token_hash ON user_tokens(token_hash);

-- 4. 操作日誌表（簡單的審計追蹤）
CREATE TABLE activity_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(50) REFERENCES users(id),
    action VARCHAR(100) NOT NULL,
    resource_type VARCHAR(50),
    resource_id VARCHAR(100),
    details TEXT,
    ip_address VARCHAR(45),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 建立索引
CREATE INDEX idx_activity_logs_user_id ON activity_logs(user_id);
CREATE INDEX idx_activity_logs_created_at ON activity_logs(created_at);

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

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- Views
-- =============================================

-- 使用者專案權限檢視
CREATE VIEW v_user_project_access AS
SELECT 
    u.id as user_id,
    u.username,
    u.email,
    u.full_name,
    u.role as system_role,
    p.id as project_id,
    p.project_name,
    pm.access_level,
    pm.created_at as joined_at
FROM users u
JOIN project_members pm ON u.id = pm.user_id
JOIN projects p ON pm.project_id = p.id
WHERE u.status = 'active' AND p.status != 'deleted';

-- =============================================
-- Functions
-- =============================================

-- 檢查使用者對專案的存取權限
CREATE OR REPLACE FUNCTION check_project_access(
    p_user_id VARCHAR(50),
    p_project_id VARCHAR(25),
    p_required_level VARCHAR(20) DEFAULT 'viewer'
) RETURNS BOOLEAN AS $$
DECLARE
    user_role VARCHAR(20);
    user_access_level VARCHAR(20);
    access_hierarchy JSONB := '{"owner": 3, "editor": 2, "viewer": 1}'::jsonb;
    user_level INT;
    required_level INT;
BEGIN
    -- 檢查使用者系統角色
    SELECT role INTO user_role FROM users WHERE id = p_user_id AND status = 'active';
    
    -- 系統管理員有所有權限
    IF user_role = 'admin' THEN
        RETURN TRUE;
    END IF;
    
    -- 檢查專案存取權限
    SELECT access_level INTO user_access_level
    FROM project_members
    WHERE user_id = p_user_id AND project_id = p_project_id;
    
    IF user_access_level IS NULL THEN
        RETURN FALSE;
    END IF;
    
    -- 比較存取層級
    user_level := (access_hierarchy->>user_access_level)::INT;
    required_level := (access_hierarchy->>p_required_level)::INT;
    
    RETURN user_level >= required_level;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- 初始資料
-- =============================================

-- 建立預設管理員帳號（密碼需要在應用程式中設定）
INSERT INTO users (id, username, email, full_name, role) VALUES
('admin', 'admin', 'admin@familytree.com', '系統管理員', 'admin');