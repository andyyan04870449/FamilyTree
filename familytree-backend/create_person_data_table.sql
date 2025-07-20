-- 建立人員資料表格
-- 用於儲存用戶上傳的Excel檔案中的人員資料

CREATE TABLE IF NOT EXISTS person_data (
    id SERIAL PRIMARY KEY,
    file_md5 VARCHAR(32) NOT NULL,  -- 檔案MD5(唯一PK)
    photo TEXT,                      -- 照片
    name VARCHAR(100) NOT NULL,      -- 姓名
    discovery_process TEXT,          -- 發掘經過
    gender VARCHAR(10),              -- 性別
    birthday DATE,                   -- 生日
    birthplace VARCHAR(200),         -- 出生地（父母戶籍所在地）
    nationality VARCHAR(50),         -- 國籍
    ethnicity VARCHAR(50),           -- 民族
    ancestral_home VARCHAR(200),     -- 籍貫（祖父戶籍所在地）
    political_party VARCHAR(100),    -- 黨派
    id_number VARCHAR(50),           -- 身分證號碼
    passport_number VARCHAR(50),     -- 護照號碼
    phone VARCHAR(50),               -- 電話
    mobile VARCHAR(50),              -- 行動電話
    email VARCHAR(100),              -- 電子信箱
    current_workplace VARCHAR(200),  -- 現職單位
    current_address TEXT,            -- 現居地址
    mailing_address TEXT,            -- 通訊地址
    family_relationships TEXT,       -- 親屬關係（職稱，姓名）
    experience TEXT,                 -- 經歷（單位，職稱，任職期間）
    education TEXT,                  -- 學歷
    online_accounts TEXT,            -- 網路帳號
    publications TEXT,               -- 著作（名稱，共同作者）
    activities TEXT,                 -- 參與活動（活動名稱，參與人士）
    important_friends TEXT,          -- 重要友人（姓名，單位，關聯事件）
    frequent_places TEXT,            -- 經常出入場所
    travel_records TEXT,             -- 出國紀錄
    notes TEXT,                      -- 備註
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,  -- 建檔時間
    created_by VARCHAR(100),         -- 建檔人
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,  -- 最後更新時間
    updated_by VARCHAR(100)          -- 最後更新人
);

-- 建立索引以提高查詢效能
CREATE INDEX IF NOT EXISTS idx_person_data_file_md5 ON person_data(file_md5);
CREATE INDEX IF NOT EXISTS idx_person_data_name ON person_data(name);
CREATE INDEX IF NOT EXISTS idx_person_data_created_at ON person_data(created_at);

-- 建立觸發器自動更新 updated_at 欄位
CREATE TRIGGER update_person_data_updated_at 
    BEFORE UPDATE ON person_data 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column(); 