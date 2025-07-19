-- 創建層級關係表
CREATE TABLE IF NOT EXISTS relationship_layers (
    id SERIAL PRIMARY KEY,
    source_person_id INTEGER NOT NULL,
    target_person_id INTEGER NOT NULL,
    relation_type VARCHAR(100) NOT NULL,
    source_field VARCHAR(50) NOT NULL,
    layer_depth INTEGER NOT NULL DEFAULT 1,
    analysis_session_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- 外鍵約束
    FOREIGN KEY (source_person_id) REFERENCES person_profile(id) ON DELETE CASCADE,
    FOREIGN KEY (target_person_id) REFERENCES person_profile(id) ON DELETE CASCADE,
    
    -- 唯一約束：避免重複關係
    UNIQUE(source_person_id, target_person_id, analysis_session_id)
);

-- 創建索引
CREATE INDEX IF NOT EXISTS idx_source_person ON relationship_layers (source_person_id);
CREATE INDEX IF NOT EXISTS idx_target_person ON relationship_layers (target_person_id);
CREATE INDEX IF NOT EXISTS idx_analysis_session ON relationship_layers (analysis_session_id);
CREATE INDEX IF NOT EXISTS idx_layer_depth ON relationship_layers (layer_depth);

-- 創建分析會話表
CREATE TABLE IF NOT EXISTS analysis_sessions (
    id VARCHAR(100) PRIMARY KEY,
    root_person_id INTEGER NOT NULL,
    max_depth INTEGER NOT NULL DEFAULT 3,
    status VARCHAR(20) NOT NULL DEFAULT 'processing',
    total_relationships INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP,
    
    -- 外鍵約束
    FOREIGN KEY (root_person_id) REFERENCES person_profile(id) ON DELETE CASCADE
);

-- 添加註釋
COMMENT ON TABLE relationship_layers IS '儲存遞迴分析的層級關係資料';
COMMENT ON TABLE analysis_sessions IS '儲存分析會話資訊';
COMMENT ON COLUMN relationship_layers.layer_depth IS '關係層級深度，1為直接關係，2為間接關係，以此類推';
COMMENT ON COLUMN relationship_layers.analysis_session_id IS '分析會話ID，用於區分不同的分析任務';
COMMENT ON COLUMN analysis_sessions.max_depth IS '最大分析深度，預設為3層'; 