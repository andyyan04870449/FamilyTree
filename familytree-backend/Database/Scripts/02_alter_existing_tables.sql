-- =============================================
-- 使用者帳號管理系統 - 修改現有資料表
-- 執行順序：2
-- 功能：為現有資料表加入 user_id 欄位
-- =============================================

-- 1. 修改 person_profile 表
ALTER TABLE person_profile 
ADD COLUMN IF NOT EXISTS user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_person_profile_user_id ON person_profile(user_id);

-- 新增註解
COMMENT ON COLUMN person_profile.user_id IS '資料擁有者的使用者 ID';

-- 2. 修改 favorites 表
-- 先檢查是否有舊的 user_id 欄位需要處理
DO $$ 
BEGIN
    -- 如果 user_id 欄位存在但類型不對，先刪除
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'favorites' 
        AND column_name = 'user_id'
        AND data_type != 'character varying'
    ) THEN
        ALTER TABLE favorites DROP COLUMN user_id;
    END IF;
END $$;

-- 新增正確的 user_id 欄位
ALTER TABLE favorites 
ADD COLUMN IF NOT EXISTS user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_favorites_user_id ON favorites(user_id);

-- 新增註解
COMMENT ON COLUMN favorites.user_id IS '使用者 ID';

-- 3. 修改 field_mapping 表
ALTER TABLE field_mapping 
ADD COLUMN IF NOT EXISTS user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_field_mapping_user_id ON field_mapping(user_id);

-- 新增註解
COMMENT ON COLUMN field_mapping.user_id IS '資料擁有者的使用者 ID';

-- 4. 修改 analysis_results 表
ALTER TABLE analysis_results 
ADD COLUMN IF NOT EXISTS user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_analysis_results_user_id ON analysis_results(user_id);

-- 新增註解
COMMENT ON COLUMN analysis_results.user_id IS '資料擁有者的使用者 ID';

-- 5. 修改 analysis_sessions 表
ALTER TABLE analysis_sessions 
ADD COLUMN IF NOT EXISTS user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_analysis_sessions_user_id ON analysis_sessions(user_id);

-- 新增註解
COMMENT ON COLUMN analysis_sessions.user_id IS '發起分析的使用者 ID';

-- 6. 修改 missing_persons 表
ALTER TABLE missing_persons 
ADD COLUMN IF NOT EXISTS user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_missing_persons_user_id ON missing_persons(user_id);

-- 新增註解
COMMENT ON COLUMN missing_persons.user_id IS '資料擁有者的使用者 ID';

-- 7. 修改 relationship_layers 表
ALTER TABLE relationship_layers 
ADD COLUMN IF NOT EXISTS user_id VARCHAR(50) REFERENCES users(id);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_relationship_layers_user_id ON relationship_layers(user_id);

-- 新增註解
COMMENT ON COLUMN relationship_layers.user_id IS '資料擁有者的使用者 ID';

-- 8. 資料遷移：將現有資料關聯到預設管理員
-- 只有在 user_id 為 NULL 的情況下才更新
UPDATE person_profile SET user_id = 'admin_default' WHERE user_id IS NULL;
UPDATE favorites SET user_id = 'admin_default' WHERE user_id IS NULL;
UPDATE field_mapping SET user_id = 'admin_default' WHERE user_id IS NULL;
UPDATE analysis_results SET user_id = 'admin_default' WHERE user_id IS NULL;
UPDATE analysis_sessions SET user_id = 'admin_default' WHERE user_id IS NULL;
UPDATE missing_persons SET user_id = 'admin_default' WHERE user_id IS NULL;
UPDATE relationship_layers SET user_id = 'admin_default' WHERE user_id IS NULL;

-- 9. 修改 projects 表的 user_id 欄位（如果需要保留 projects 表）
-- 檢查 projects 表是否存在
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'projects') THEN
        -- 修改 user_id 欄位類型
        ALTER TABLE projects 
        ALTER COLUMN user_id TYPE VARCHAR(50);
        
        -- 更新現有資料
        UPDATE projects SET user_id = 'admin_default' WHERE user_id IS NOT NULL;
        
        -- 加入外鍵約束
        ALTER TABLE projects 
        ADD CONSTRAINT fk_projects_user_id 
        FOREIGN KEY (user_id) REFERENCES users(id);
    END IF;
END $$;

-- 10. 記錄執行日誌
INSERT INTO activity_logs (user_id, action, details) 
VALUES (
    'admin_default',
    'database_migration',
    '修改現有資料表加入 user_id 欄位'
);

-- 顯示修改結果
SELECT 
    table_name,
    COUNT(*) as affected_rows
FROM (
    SELECT 'person_profile' as table_name, COUNT(*) FROM person_profile WHERE user_id = 'admin_default'
    UNION ALL
    SELECT 'favorites', COUNT(*) FROM favorites WHERE user_id = 'admin_default'
    UNION ALL
    SELECT 'field_mapping', COUNT(*) FROM field_mapping WHERE user_id = 'admin_default'
    UNION ALL
    SELECT 'analysis_results', COUNT(*) FROM analysis_results WHERE user_id = 'admin_default'
    UNION ALL
    SELECT 'analysis_sessions', COUNT(*) FROM analysis_sessions WHERE user_id = 'admin_default'
    UNION ALL
    SELECT 'missing_persons', COUNT(*) FROM missing_persons WHERE user_id = 'admin_default'
    UNION ALL
    SELECT 'relationship_layers', COUNT(*) FROM relationship_layers WHERE user_id = 'admin_default'
) as migration_results
GROUP BY table_name
ORDER BY table_name;