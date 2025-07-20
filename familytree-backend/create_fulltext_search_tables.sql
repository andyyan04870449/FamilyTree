-- 全文檢索功能資料庫表結構設計 (PostgreSQL版本)
-- 此檔案的目的：建立全文檢索功能所需的資料表，包括搜索歷史和收藏功能

-- 1. 搜索關鍵字記錄表
CREATE TABLE IF NOT EXISTS search_keywords (
    id SERIAL PRIMARY KEY,
    keyword VARCHAR(255) NOT NULL,
    search_count INTEGER DEFAULT 1,
    search_type VARCHAR(20) DEFAULT 'fuzzy',
    last_search_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- 確保關鍵字的唯一性
    CONSTRAINT unique_keyword UNIQUE (keyword)
);

-- 為搜索關鍵字表添加註釋
COMMENT ON TABLE search_keywords IS '搜索關鍵字記錄表：記錄用戶搜索的關鍵字和使用頻率';
COMMENT ON COLUMN search_keywords.keyword IS '搜索關鍵字';
COMMENT ON COLUMN search_keywords.search_count IS '搜索次數';
COMMENT ON COLUMN search_keywords.search_type IS '搜索類型：exact(精準) 或 fuzzy(模糊)';
COMMENT ON COLUMN search_keywords.last_search_time IS '最後搜索時間';
COMMENT ON COLUMN search_keywords.created_at IS '建立時間';
COMMENT ON COLUMN search_keywords.updated_at IS '更新時間';

-- 2. 用戶收藏表
CREATE TABLE IF NOT EXISTS user_favorites (
    id SERIAL PRIMARY KEY,
    person_id INTEGER NOT NULL,
    person_name VARCHAR(100) NOT NULL,
    last_viewed_time TIMESTAMP NULL,
    favorited_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- 確保同一人員不會重複收藏
    CONSTRAINT unique_person_favorite UNIQUE (person_id)
);

-- 為用戶收藏表添加註釋
COMMENT ON TABLE user_favorites IS '用戶收藏表：記錄用戶收藏的人員資料';
COMMENT ON COLUMN user_favorites.person_id IS '人員ID，關聯person_profile.id';
COMMENT ON COLUMN user_favorites.person_name IS '人員姓名（冗餘字段，提高查詢效能）';
COMMENT ON COLUMN user_favorites.last_viewed_time IS '最後查看時間';
COMMENT ON COLUMN user_favorites.favorited_at IS '收藏時間';
COMMENT ON COLUMN user_favorites.created_at IS '建立時間';
COMMENT ON COLUMN user_favorites.updated_at IS '更新時間';

-- 3. 搜索結果日誌表（用於統計和分析）
CREATE TABLE IF NOT EXISTS search_logs (
    id SERIAL PRIMARY KEY,
    keyword VARCHAR(255) NOT NULL,
    search_type VARCHAR(20) NOT NULL,
    result_count INTEGER DEFAULT 0,
    search_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ip_address VARCHAR(45) NULL,
    user_agent TEXT NULL
);

-- 為搜索日誌表添加註釋
COMMENT ON TABLE search_logs IS '搜索結果日誌表：記錄詳細的搜索行為，用於統計和分析';
COMMENT ON COLUMN search_logs.keyword IS '搜索關鍵字';
COMMENT ON COLUMN search_logs.search_type IS '搜索類型：exact 或 fuzzy';
COMMENT ON COLUMN search_logs.result_count IS '搜索結果數量';
COMMENT ON COLUMN search_logs.search_time IS '搜索時間';
COMMENT ON COLUMN search_logs.ip_address IS '搜索者IP地址';
COMMENT ON COLUMN search_logs.user_agent IS '用戶代理字符串';

-- 4. 建立索引以提高查詢效能

-- 搜索關鍵字表索引
CREATE INDEX IF NOT EXISTS idx_keyword ON search_keywords (keyword);
CREATE INDEX IF NOT EXISTS idx_search_count ON search_keywords (search_count DESC);
CREATE INDEX IF NOT EXISTS idx_last_search_time ON search_keywords (last_search_time DESC);

-- 用戶收藏表索引
CREATE INDEX IF NOT EXISTS idx_person_id ON user_favorites (person_id);
CREATE INDEX IF NOT EXISTS idx_person_name ON user_favorites (person_name);
CREATE INDEX IF NOT EXISTS idx_last_viewed_time ON user_favorites (last_viewed_time DESC);
CREATE INDEX IF NOT EXISTS idx_favorited_at ON user_favorites (favorited_at DESC);

-- 搜索日誌表索引
CREATE INDEX IF NOT EXISTS idx_keyword_log ON search_logs (keyword);
CREATE INDEX IF NOT EXISTS idx_search_time ON search_logs (search_time DESC);
CREATE INDEX IF NOT EXISTS idx_result_count ON search_logs (result_count);

-- 5. 為person_profile表添加全文搜索索引（如果需要）
CREATE INDEX IF NOT EXISTS idx_person_name_search ON person_profile (name);
CREATE INDEX IF NOT EXISTS idx_person_mobile_search ON person_profile (mobile);
CREATE INDEX IF NOT EXISTS idx_person_phone_search ON person_profile (phone);
CREATE INDEX IF NOT EXISTS idx_person_id_number_search ON person_profile (id_number);
CREATE INDEX IF NOT EXISTS idx_person_passport_search ON person_profile (passport_number);

-- 為全文搜索建立複合索引
CREATE INDEX IF NOT EXISTS idx_person_fulltext_search ON person_profile (name, mobile, phone, id_number, passport_number);

-- 6. 插入一些初始的熱門關鍵字示例（可選）
INSERT INTO search_keywords (keyword, search_count, search_type, last_search_time) VALUES
('北大', 5, 'fuzzy', CURRENT_TIMESTAMP),
('台科大', 4, 'fuzzy', CURRENT_TIMESTAMP),
('上海', 3, 'fuzzy', CURRENT_TIMESTAMP),
('廣告大學', 3, 'fuzzy', CURRENT_TIMESTAMP),
('東京電信', 2, 'fuzzy', CURRENT_TIMESTAMP),
('中國總商會', 2, 'fuzzy', CURRENT_TIMESTAMP)
ON CONFLICT (keyword) 
DO UPDATE SET 
    search_count = EXCLUDED.search_count,
    last_search_time = EXCLUDED.last_search_time;

-- 7. 建立視圖：熱門關鍵字（取最常搜索的關鍵字）
CREATE OR REPLACE VIEW popular_keywords AS
SELECT 
    keyword,
    search_count,
    last_search_time,
    CASE 
        WHEN search_count >= 10 THEN '熱門'
        WHEN search_count >= 5 THEN '常用'
        ELSE '一般'
    END as popularity_level
FROM search_keywords 
WHERE search_count > 0
ORDER BY search_count DESC, last_search_time DESC
LIMIT 20;

-- 8. 為更新時間添加觸發器
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 為搜索關鍵字表添加更新觸發器
DROP TRIGGER IF EXISTS update_search_keywords_updated_at ON search_keywords;
CREATE TRIGGER update_search_keywords_updated_at
    BEFORE UPDATE ON search_keywords
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 為用戶收藏表添加更新觸發器
DROP TRIGGER IF EXISTS update_user_favorites_updated_at ON user_favorites;
CREATE TRIGGER update_user_favorites_updated_at
    BEFORE UPDATE ON user_favorites
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column(); 