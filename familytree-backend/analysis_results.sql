-- 建立分析結果資料表
CREATE TABLE IF NOT EXISTS analysis_results (
    id SERIAL PRIMARY KEY,
    person_id INTEGER NOT NULL,
    analysis_result JSONB NOT NULL,
    analysis_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    progress_percentage INTEGER DEFAULT 0,
    status VARCHAR(50) DEFAULT 'pending', -- pending, processing, completed, failed
    current_step VARCHAR(255), -- 當前執行的步驟
    status_message TEXT, -- 詳細的狀態信息
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_analysis_results_person_id ON analysis_results(person_id);
CREATE INDEX IF NOT EXISTS idx_analysis_results_status ON analysis_results(status);

-- 建立唯一約束，避免重複分析
CREATE UNIQUE INDEX IF NOT EXISTS idx_analysis_results_person_unique 
ON analysis_results(person_id) WHERE status IN ('pending', 'processing'); 