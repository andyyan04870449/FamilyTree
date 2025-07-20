-- 建立 user_update_file 表格
-- 用於記錄用戶上傳的檔案資訊

CREATE TABLE IF NOT EXISTS user_update_file (
    id SERIAL PRIMARY KEY,
    filename VARCHAR(255) NOT NULL,
    original_filename VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    md5_hash VARCHAR(32) NOT NULL,
    upload_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_merged BOOLEAN DEFAULT FALSE,
    merge_time TIMESTAMP NULL,
    status VARCHAR(50) DEFAULT 'uploaded',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 建立索引以提高查詢效能
CREATE INDEX IF NOT EXISTS idx_user_update_file_md5 ON user_update_file(md5_hash);
CREATE INDEX IF NOT EXISTS idx_user_update_file_status ON user_update_file(status);
CREATE INDEX IF NOT EXISTS idx_user_update_file_upload_time ON user_update_file(upload_time);

-- 建立觸發器自動更新 updated_at 欄位
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_user_update_file_updated_at 
    BEFORE UPDATE ON user_update_file 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- 插入測試資料（可選）
-- INSERT INTO user_update_file (filename, original_filename, file_path, file_size, md5_hash, status) 
-- VALUES ('test.xlsx', 'test.xlsx', '/uploads/test.xlsx', 1024, 'd41d8cd98f00b204e9800998ecf8427e', 'uploaded'); 