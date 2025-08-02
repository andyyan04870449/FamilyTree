-- =============================================
-- 使用者帳號管理系統資料庫設計（簡化版 - 單一使用者資料隔離）
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

-- 2. 使用者 Token 表（JWT Refresh Token 儲存）
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

-- 3. 操作日誌表（簡單的審計追蹤）
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
-- 修改現有資料表，加入 user_id 欄位
-- =============================================

-- 修改 person_profile 表，加入 user_id
ALTER TABLE person_profile 
ADD COLUMN user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX idx_person_profile_user_id ON person_profile(user_id);

-- 修改 favorites 表，更新 user_id 類型
ALTER TABLE favorites 
DROP COLUMN user_id;

ALTER TABLE favorites 
ADD COLUMN user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX idx_favorites_user_id ON favorites(user_id);

-- 修改 field_mapping 表，加入 user_id
ALTER TABLE field_mapping 
ADD COLUMN user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX idx_field_mapping_user_id ON field_mapping(user_id);

-- 修改 analysis_results 表，加入 user_id
ALTER TABLE analysis_results 
ADD COLUMN user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX idx_analysis_results_user_id ON analysis_results(user_id);

-- 修改 analysis_sessions 表，加入 user_id
ALTER TABLE analysis_sessions 
ADD COLUMN user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX idx_analysis_sessions_user_id ON analysis_sessions(user_id);

-- 修改 missing_persons 表，加入 user_id
ALTER TABLE missing_persons 
ADD COLUMN user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX idx_missing_persons_user_id ON missing_persons(user_id);

-- 修改 relationship_layers 表，加入 user_id
ALTER TABLE relationship_layers 
ADD COLUMN user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX idx_relationship_layers_user_id ON relationship_layers(user_id);

-- =============================================
-- 移除 projects 相關表格和欄位
-- =============================================

-- 從各表移除 project_id 欄位
ALTER TABLE person_profile DROP COLUMN IF EXISTS project_id;
ALTER TABLE field_mapping DROP COLUMN IF EXISTS project_id;
ALTER TABLE analysis_results DROP COLUMN IF EXISTS project_id;
ALTER TABLE analysis_sessions DROP COLUMN IF EXISTS project_id;
ALTER TABLE missing_persons DROP COLUMN IF EXISTS project_id;
ALTER TABLE relationship_layers DROP COLUMN IF EXISTS project_id;
ALTER TABLE favorites DROP COLUMN IF EXISTS project_id;

-- 移除 projects 表（如果確定不需要）
-- DROP TABLE IF EXISTS projects CASCADE;

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

-- 使用者資料統計檢視
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
    (SELECT COUNT(*) FROM person_profile WHERE user_id = u.id) as person_count,
    (SELECT COUNT(*) FROM favorites WHERE user_id = u.id) as favorite_count,
    (SELECT COUNT(*) FROM analysis_sessions WHERE user_id = u.id) as analysis_count
FROM users u
WHERE u.status = 'active';

-- =============================================
-- Functions
-- =============================================

-- 檢查使用者是否可以存取資料
CREATE OR REPLACE FUNCTION check_data_access(
    p_user_id VARCHAR(50),
    p_resource_type VARCHAR(50),
    p_resource_id VARCHAR(100),
    p_owner_user_id VARCHAR(50)
) RETURNS BOOLEAN AS $$
DECLARE
    user_role VARCHAR(20);
BEGIN
    -- 取得使用者角色
    SELECT role INTO user_role FROM users WHERE id = p_user_id AND status = 'active';
    
    -- 系統管理員可以存取所有資料
    IF user_role = 'admin' THEN
        RETURN TRUE;
    END IF;
    
    -- 一般使用者只能存取自己的資料
    RETURN p_user_id = p_owner_user_id;
END;
$$ LANGUAGE plpgsql;

-- =============================================
-- 初始資料
-- =============================================

-- 建立預設管理員帳號（密碼需要在應用程式中設定）
INSERT INTO users (id, username, email, full_name, role) VALUES
('admin', 'admin', 'admin@familytree.com', '系統管理員', 'admin');