-- =============================================
-- 使用者帳號管理系統 - 資料庫建立腳本
-- 執行順序：1
-- 功能：建立新的使用者相關資料表
-- =============================================

-- 1. 建立 users 表（使用者基本資料）
CREATE TABLE IF NOT EXISTS users (
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
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_status ON users(status);

-- 新增註解
COMMENT ON TABLE users IS '使用者帳號資料表';
COMMENT ON COLUMN users.id IS '使用者唯一識別碼 (UUID)';
COMMENT ON COLUMN users.username IS '登入帳號名稱';
COMMENT ON COLUMN users.email IS '電子郵件地址';
COMMENT ON COLUMN users.password_hash IS 'BCrypt 加密的密碼雜湊值';
COMMENT ON COLUMN users.full_name IS '使用者真實姓名';
COMMENT ON COLUMN users.role IS '系統角色：admin(管理員) 或 user(一般使用者)';
COMMENT ON COLUMN users.status IS '帳號狀態：active(啟用) 或 inactive(停用)';
COMMENT ON COLUMN users.last_login_at IS '最後登入時間';

-- 2. 建立 user_tokens 表（JWT Refresh Token 儲存）
CREATE TABLE IF NOT EXISTS user_tokens (
    id VARCHAR(100) PRIMARY KEY DEFAULT gen_random_uuid()::text,
    user_id VARCHAR(50) NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_type VARCHAR(20) NOT NULL CHECK (token_type IN ('refresh')),
    token_hash VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    used_at TIMESTAMP
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_user_tokens_user_id ON user_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_user_tokens_token_hash ON user_tokens(token_hash);
CREATE INDEX IF NOT EXISTS idx_user_tokens_expires_at ON user_tokens(expires_at);

-- 新增註解
COMMENT ON TABLE user_tokens IS 'JWT Token 管理表';
COMMENT ON COLUMN user_tokens.token_type IS 'Token 類型，目前只有 refresh';
COMMENT ON COLUMN user_tokens.token_hash IS 'Token 的雜湊值（不存明文）';
COMMENT ON COLUMN user_tokens.expires_at IS 'Token 過期時間';
COMMENT ON COLUMN user_tokens.used_at IS '最後使用時間';

-- 3. 建立 activity_logs 表（操作日誌）
CREATE TABLE IF NOT EXISTS activity_logs (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(50) REFERENCES users(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    resource_type VARCHAR(50),
    resource_id VARCHAR(100),
    details TEXT,
    ip_address VARCHAR(45),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_activity_logs_user_id ON activity_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_activity_logs_created_at ON activity_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_activity_logs_action ON activity_logs(action);

-- 新增註解
COMMENT ON TABLE activity_logs IS '系統操作日誌表';
COMMENT ON COLUMN activity_logs.user_id IS '執行操作的使用者 ID';
COMMENT ON COLUMN activity_logs.action IS '操作名稱（如：login, create_person）';
COMMENT ON COLUMN activity_logs.resource_type IS '資源類型（如：person, file）';
COMMENT ON COLUMN activity_logs.resource_id IS '資源的 ID';
COMMENT ON COLUMN activity_logs.details IS '操作詳情（可存 JSON 格式）';
COMMENT ON COLUMN activity_logs.ip_address IS '操作者的 IP 位址';

-- 4. 建立自動更新 updated_at 的觸發器函數（如果不存在）
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 5. 套用觸發器到 users 表
DROP TRIGGER IF EXISTS update_users_updated_at ON users;
CREATE TRIGGER update_users_updated_at 
    BEFORE UPDATE ON users
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- 6. 插入預設管理員帳號
-- 預設密碼: Admin@123 (需要在應用程式中使用 BCrypt 加密後更新)
INSERT INTO users (id, username, email, full_name, role, password_hash) 
VALUES (
    'admin_default',
    'admin',
    'admin@familytree.com',
    '系統管理員',
    'admin',
    '$2b$10$dummyHashNeedsToBeUpdated' -- 這個需要用真實的 BCrypt hash 替換
) ON CONFLICT (username) DO NOTHING;

-- 7. 記錄執行日誌
INSERT INTO activity_logs (user_id, action, details) 
VALUES (
    'admin_default',
    'database_migration',
    '建立使用者管理系統資料表'
);

-- 顯示建立結果
SELECT 'User management tables created successfully' as result;