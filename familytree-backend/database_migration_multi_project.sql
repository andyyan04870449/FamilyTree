-- ==========================================
-- 多專案架構資料庫遷移腳本
-- ==========================================

-- Step 1: 創建專案管理表
CREATE TABLE IF NOT EXISTS projects (
    id VARCHAR(20) PRIMARY KEY,  -- 格式: userID-YYYYMMDDHHMMSS
    user_id VARCHAR(6) NOT NULL,  -- 建立者的用戶ID (6位數字)
    project_name VARCHAR(200) NOT NULL,  -- 專案名稱
    project_description TEXT,  -- 專案描述
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'completed', 'archived', 'draft')),  -- 專案狀態
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,  -- 建立時間
    completed_at TIMESTAMP,  -- 結案時間
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,  -- 最後更新時間
    
    -- 索引
    INDEX idx_projects_user_id (user_id),
    INDEX idx_projects_status (status),
    INDEX idx_projects_created_at (created_at)
);

-- 添加更新時間觸發器
CREATE OR REPLACE TRIGGER update_projects_updated_at
    BEFORE UPDATE ON projects
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Step 2: 生成預設專案資料
-- 使用隨機6位數用戶ID和今天的時間戳
DO $$
DECLARE
    default_user_id VARCHAR(6) := LPAD(FLOOR(RANDOM() * 1000000)::TEXT, 6, '0');  -- 生成6位隨機數字
    default_project_id VARCHAR(20);
    current_timestamp_str VARCHAR(14);
BEGIN
    -- 生成時間戳字串 (YYYYMMDDHHMMSS)
    current_timestamp_str := TO_CHAR(NOW(), 'YYYYMMDDHH24MISS');
    
    -- 組合專案ID
    default_project_id := default_user_id || '-' || current_timestamp_str;
    
    -- 插入預設專案
    INSERT INTO projects (
        id, 
        user_id, 
        project_name, 
        project_description, 
        status, 
        created_at
    ) VALUES (
        default_project_id,
        default_user_id,
        '家族樹系統預設專案',
        '系統升級前的原有資料專案，包含所有現有的人員資料和關係網絡',
        'active',
        NOW()
    );
    
    -- 儲存預設專案ID到臨時變數（供後續步驟使用）
    PERFORM set_config('myapp.default_project_id', default_project_id, true);
    
    RAISE NOTICE '已建立預設專案: ID = %, User ID = %', default_project_id, default_user_id;
END $$;

-- Step 3: 為現有表格增加專案ID欄位
-- 3.1 person_profile 表
ALTER TABLE person_profile ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.2 analysis_results 表
ALTER TABLE analysis_results ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.3 analysis_sessions 表
ALTER TABLE analysis_sessions ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.4 relationship_layers 表
ALTER TABLE relationship_layers ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.5 missing_persons 表
ALTER TABLE missing_persons ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.6 user_update_file 表
ALTER TABLE user_update_file ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.7 field_mapping 表
ALTER TABLE field_mapping ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.8 search_keywords 表
ALTER TABLE search_keywords ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.9 user_favorites 表
ALTER TABLE user_favorites ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- 3.10 search_logs 表
ALTER TABLE search_logs ADD COLUMN IF NOT EXISTS project_id VARCHAR(20);

-- Step 4: 更新現有資料，設定為預設專案
DO $$
DECLARE
    default_project_id VARCHAR(20);
BEGIN
    -- 獲取預設專案ID
    default_project_id := current_setting('myapp.default_project_id');
    
    -- 更新所有相關表格的專案ID
    UPDATE person_profile SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE analysis_results SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE analysis_sessions SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE relationship_layers SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE missing_persons SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE user_update_file SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE field_mapping SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE search_keywords SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE user_favorites SET project_id = default_project_id WHERE project_id IS NULL;
    UPDATE search_logs SET project_id = default_project_id WHERE project_id IS NULL;
    
    RAISE NOTICE '已將現有資料更新至預設專案: %', default_project_id;
END $$;

-- Step 5: 添加外鍵約束
ALTER TABLE person_profile ADD CONSTRAINT fk_person_profile_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE analysis_results ADD CONSTRAINT fk_analysis_results_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE analysis_sessions ADD CONSTRAINT fk_analysis_sessions_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE relationship_layers ADD CONSTRAINT fk_relationship_layers_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE missing_persons ADD CONSTRAINT fk_missing_persons_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE user_update_file ADD CONSTRAINT fk_user_update_file_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE field_mapping ADD CONSTRAINT fk_field_mapping_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE search_keywords ADD CONSTRAINT fk_search_keywords_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE user_favorites ADD CONSTRAINT fk_user_favorites_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

ALTER TABLE search_logs ADD CONSTRAINT fk_search_logs_project 
    FOREIGN KEY (project_id) REFERENCES projects(id) ON DELETE CASCADE;

-- Step 6: 添加索引以提升查詢效能
CREATE INDEX IF NOT EXISTS idx_person_profile_project_id ON person_profile(project_id);
CREATE INDEX IF NOT EXISTS idx_analysis_results_project_id ON analysis_results(project_id);
CREATE INDEX IF NOT EXISTS idx_analysis_sessions_project_id ON analysis_sessions(project_id);
CREATE INDEX IF NOT EXISTS idx_relationship_layers_project_id ON relationship_layers(project_id);
CREATE INDEX IF NOT EXISTS idx_missing_persons_project_id ON missing_persons(project_id);
CREATE INDEX IF NOT EXISTS idx_user_update_file_project_id ON user_update_file(project_id);
CREATE INDEX IF NOT EXISTS idx_field_mapping_project_id ON field_mapping(project_id);
CREATE INDEX IF NOT EXISTS idx_search_keywords_project_id ON search_keywords(project_id);
CREATE INDEX IF NOT EXISTS idx_user_favorites_project_id ON user_favorites(project_id);
CREATE INDEX IF NOT EXISTS idx_search_logs_project_id ON search_logs(project_id);

-- Step 7: 設定專案ID為必填欄位（在資料更新後）
ALTER TABLE person_profile ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE analysis_results ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE analysis_sessions ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE relationship_layers ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE missing_persons ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE user_update_file ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE field_mapping ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE search_keywords ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE user_favorites ALTER COLUMN project_id SET NOT NULL;
ALTER TABLE search_logs ALTER COLUMN project_id SET NOT NULL;

-- 完成通知
DO $$
BEGIN
    RAISE NOTICE '==========================================';
    RAISE NOTICE '多專案架構資料庫遷移完成！';
    RAISE NOTICE '==========================================';
    RAISE NOTICE '已創建 projects 表';
    RAISE NOTICE '已為所有相關表格增加 project_id 欄位';
    RAISE NOTICE '已建立預設專案並更新現有資料';
    RAISE NOTICE '已添加外鍵約束和索引';
    RAISE NOTICE '==========================================';
END $$; 