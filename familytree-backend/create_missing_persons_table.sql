-- 創建記錄找不到人員的表格
-- 這個表格用於記錄 AI 分析出但資料庫中不存在的人員

CREATE TABLE IF NOT EXISTS missing_persons (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    relation_type VARCHAR(100) NOT NULL,
    source_person_id INTEGER NOT NULL,
    source_field VARCHAR(50) NOT NULL, -- 'family_relationships', 'friends', 'activities'
    analysis_session_id VARCHAR(255) NOT NULL,
    layer_depth INTEGER NOT NULL DEFAULT 1,
    discovered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending', -- 'pending', 'resolved', 'ignored'
    resolved_person_id INTEGER NULL, -- 如果後來找到對應人員，記錄其 ID
    notes TEXT NULL,
    
    -- 索引
    CONSTRAINT fk_source_person FOREIGN KEY (source_person_id) REFERENCES person_profile(id) ON DELETE CASCADE,
    CONSTRAINT fk_resolved_person FOREIGN KEY (resolved_person_id) REFERENCES person_profile(id) ON DELETE SET NULL
);

-- 創建索引以提高查詢性能
CREATE INDEX IF NOT EXISTS idx_missing_persons_name ON missing_persons(name);
CREATE INDEX IF NOT EXISTS idx_missing_persons_source_person ON missing_persons(source_person_id);
CREATE INDEX IF NOT EXISTS idx_missing_persons_session ON missing_persons(analysis_session_id);
CREATE INDEX IF NOT EXISTS idx_missing_persons_status ON missing_persons(status);

-- 添加註釋
COMMENT ON TABLE missing_persons IS '記錄 AI 分析出但資料庫中不存在的人員';
COMMENT ON COLUMN missing_persons.name IS '人員姓名';
COMMENT ON COLUMN missing_persons.relation_type IS '與來源人員的關係類型';
COMMENT ON COLUMN missing_persons.source_person_id IS '來源人員的 ID';
COMMENT ON COLUMN missing_persons.source_field IS '來源欄位（family_relationships, friends, activities）';
COMMENT ON COLUMN missing_persons.analysis_session_id IS '分析會話 ID';
COMMENT ON COLUMN missing_persons.layer_depth IS '分析層級深度';
COMMENT ON COLUMN missing_persons.discovered_at IS '發現時間';
COMMENT ON COLUMN missing_persons.status IS '狀態（pending: 待處理, resolved: 已解決, ignored: 忽略）';
COMMENT ON COLUMN missing_persons.resolved_person_id IS '解決後對應的人員 ID';
COMMENT ON COLUMN missing_persons.notes IS '備註信息'; 