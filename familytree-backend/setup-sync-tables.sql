-- 同步日誌表
CREATE TABLE IF NOT EXISTS sync_log (
    id SERIAL PRIMARY KEY,
    source_id INTEGER NOT NULL,
    source_table VARCHAR(50) NOT NULL,
    status VARCHAR(20) NOT NULL,
    sync_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 同步錯誤日誌表
CREATE TABLE IF NOT EXISTS sync_error_log (
    id SERIAL PRIMARY KEY,
    source_id INTEGER NOT NULL,
    source_table VARCHAR(50) NOT NULL,
    error_message TEXT NOT NULL,
    error_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 同步狀態表
CREATE TABLE IF NOT EXISTS sync_status (
    id SERIAL PRIMARY KEY,
    last_sync_time TIMESTAMP NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 在 person_profile 表中添加來源追踪欄位
ALTER TABLE person_profile 
ADD COLUMN IF NOT EXISTS source_id INTEGER,
ADD COLUMN IF NOT EXISTS source_table VARCHAR(50),
ADD COLUMN IF NOT EXISTS source_created_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS source_updated_at TIMESTAMP;

-- 創建索引
CREATE INDEX IF NOT EXISTS idx_sync_log_source ON sync_log(source_id, source_table);
CREATE INDEX IF NOT EXISTS idx_sync_error_source ON sync_error_log(source_id, source_table);
CREATE INDEX IF NOT EXISTS idx_person_profile_source ON person_profile(source_id, source_table);

-- 添加表註釋
COMMENT ON TABLE sync_log IS '資料同步日誌表，記錄所有同步操作';
COMMENT ON TABLE sync_error_log IS '資料同步錯誤日誌表，記錄同步過程中的錯誤';
COMMENT ON TABLE sync_status IS '資料同步狀態表，記錄最後同步時間和狀態';

-- 添加欄位註釋
COMMENT ON COLUMN person_profile.source_id IS '來源資料的ID';
COMMENT ON COLUMN person_profile.source_table IS '來源資料表名稱';
COMMENT ON COLUMN person_profile.source_created_at IS '來源資料的創建時間';
COMMENT ON COLUMN person_profile.source_updated_at IS '來源資料的最後更新時間'; 