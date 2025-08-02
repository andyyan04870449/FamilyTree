-- =============================================
-- 修正缺失的資料表關聯
-- =============================================

-- 1. 處理 favorites 表（實際名稱是 user_favorites）
ALTER TABLE user_favorites 
ADD COLUMN IF NOT EXISTS user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_user_favorites_user_id ON user_favorites(user_id);

-- 更新現有資料
UPDATE user_favorites SET user_id = 'admin_default' WHERE user_id IS NULL;

-- 2. 處理 analysis 相關表（這些表可能不存在，我們建立它們）

-- 建立 analysis_results 表（如果不存在）
CREATE TABLE IF NOT EXISTS analysis_results (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(50) REFERENCES users(id),
    person_id INTEGER NOT NULL,
    analysis_result JSONB NOT NULL,
    analysis_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    progress_percentage INTEGER DEFAULT 0,
    status VARCHAR(50) DEFAULT 'pending',
    current_step VARCHAR(255),
    status_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_analysis_results_user_id ON analysis_results(user_id);

-- 建立 analysis_sessions 表（如果不存在）
CREATE TABLE IF NOT EXISTS analysis_sessions (
    id VARCHAR(100) PRIMARY KEY,
    user_id VARCHAR(50) REFERENCES users(id),
    root_person_id INTEGER NOT NULL,
    max_depth INTEGER DEFAULT 3 NOT NULL,
    status VARCHAR(20) DEFAULT 'processing' NOT NULL,
    total_relationships INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_analysis_sessions_user_id ON analysis_sessions(user_id);

-- 建立 missing_persons 表（如果不存在）
CREATE TABLE IF NOT EXISTS missing_persons (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(50) REFERENCES users(id),
    name VARCHAR(255) NOT NULL,
    relation_type VARCHAR(100) NOT NULL,
    source_person_id INTEGER NOT NULL,
    source_field VARCHAR(50) NOT NULL,
    analysis_session_id VARCHAR(255) NOT NULL,
    layer_depth INTEGER DEFAULT 1 NOT NULL,
    discovered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending',
    resolved_person_id INTEGER,
    notes TEXT
);

CREATE INDEX IF NOT EXISTS idx_missing_persons_user_id ON missing_persons(user_id);

-- 3. 更新 relationship_layers 表（已存在）
-- 檢查是否已有 user_id 欄位
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'relationship_layers' 
        AND column_name = 'user_id'
    ) THEN
        ALTER TABLE relationship_layers 
        ADD COLUMN user_id VARCHAR(50) REFERENCES users(id);
        
        CREATE INDEX idx_relationship_layers_user_id ON relationship_layers(user_id);
        
        UPDATE relationship_layers SET user_id = 'admin_default' WHERE user_id IS NULL;
    END IF;
END $$;

-- 4. 重新建立使用者統計檢視（修正表名）
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
    COALESCE((SELECT COUNT(*) FROM user_favorites WHERE user_id = u.id), 0) as favorite_count,
    COALESCE((SELECT COUNT(*) FROM analysis_sessions WHERE user_id = u.id), 0) as analysis_count,
    COALESCE((SELECT COUNT(*) FROM activity_logs WHERE user_id = u.id AND created_at > CURRENT_DATE - INTERVAL '30 days'), 0) as recent_activities
FROM users u
WHERE u.status = 'active';

-- 5. 記錄修正日誌
INSERT INTO activity_logs (user_id, action, details) 
VALUES (
    'admin_default',
    'database_migration_fix',
    '修正缺失的資料表關聯'
);

-- 顯示修正結果
SELECT 
    'Table fixes completed' as result,
    (SELECT COUNT(*) FROM information_schema.columns WHERE column_name = 'user_id') as user_id_columns;