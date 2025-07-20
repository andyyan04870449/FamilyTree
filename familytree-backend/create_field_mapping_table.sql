-- 建立欄位對應表格
-- 用於mapping用戶上傳的Excel欄位名稱到資料庫欄位名稱

CREATE TABLE IF NOT EXISTS field_mapping (
    id SERIAL PRIMARY KEY,
    excel_field_name VARCHAR(100) NOT NULL,    -- Excel表格上的欄位名稱
    db_field_name VARCHAR(100) NOT NULL,       -- 資料庫中欄位的名稱
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 建立唯一約束
ALTER TABLE field_mapping ADD CONSTRAINT uk_field_mapping_excel_db UNIQUE (excel_field_name, db_field_name);

-- 建立索引以提高查詢效能
CREATE INDEX IF NOT EXISTS idx_field_mapping_excel_field ON field_mapping(excel_field_name);
CREATE INDEX IF NOT EXISTS idx_field_mapping_db_field ON field_mapping(db_field_name);

-- 建立觸發器自動更新 updated_at 欄位
CREATE TRIGGER update_field_mapping_updated_at 
    BEFORE UPDATE ON field_mapping 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();

-- 插入預設的欄位對應資料
-- 一個資料庫欄位可以擁有多個excel欄位名稱，供用戶提供不同種規格的excel檔案
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES
-- 姓名相關
('姓名', 'name'),
('名字', 'name'),
('Name', 'name'),
('姓名', 'name'),
('姓名', 'name'),

-- 性別相關
('性別', 'gender'),
('Gender', 'gender'),
('性別', 'gender'),

-- 生日相關
('生日', 'birthday'),
('出生日期', 'birthday'),
('Birthday', 'birthday'),
('Date of Birth', 'birthday'),

-- 出生地相關
('出生地', 'birthplace'),
('父母戶籍所在地', 'birthplace'),
('Birthplace', 'birthplace'),
('Place of Birth', 'birthplace'),

-- 國籍相關
('國籍', 'nationality'),
('Nationality', 'nationality'),

-- 民族相關
('民族', 'ethnicity'),
('Ethnicity', 'ethnicity'),

-- 籍貫相關
('籍貫', 'ancestral_home'),
('祖父戶籍所在地', 'ancestral_home'),
('Ancestral Home', 'ancestral_home'),

-- 黨派相關
('黨派', 'political_party'),
('Political Party', 'political_party'),

-- 身分證號碼相關
('身分證號碼', 'id_number'),
('身份證號碼', 'id_number'),
('ID Number', 'id_number'),
('Identity Number', 'id_number'),

-- 護照號碼相關
('護照號碼', 'passport_number'),
('Passport Number', 'passport_number'),

-- 電話相關
('電話', 'phone'),
('Phone', 'phone'),
('Telephone', 'phone'),

-- 行動電話相關
('行動電話', 'mobile'),
('手機', 'mobile'),
('Mobile', 'mobile'),
('Cell Phone', 'mobile'),

-- 電子信箱相關
('電子信箱', 'email'),
('Email', 'email'),
('E-mail', 'email'),

-- 現職單位相關
('現職單位', 'current_workplace'),
('工作單位', 'current_workplace'),
('Current Workplace', 'current_workplace'),

-- 現居地址相關
('現居地址', 'current_address'),
('居住地址', 'current_address'),
('Current Address', 'current_address'),

-- 通訊地址相關
('通訊地址', 'mailing_address'),
('聯絡地址', 'mailing_address'),
('Mailing Address', 'mailing_address'),

-- 親屬關係相關
('親屬關係', 'family_relationships'),
('Family Relationships', 'family_relationships'),

-- 經歷相關
('經歷', 'experience'),
('工作經歷', 'experience'),
('Experience', 'experience'),

-- 學歷相關
('學歷', 'education'),
('Education', 'education'),

-- 網路帳號相關
('網路帳號', 'online_accounts'),
('Online Accounts', 'online_accounts'),

-- 著作相關
('著作', 'publications'),
('Publications', 'publications'),

-- 參與活動相關
('參與活動', 'activities'),
('Activities', 'activities'),

-- 重要友人相關
('重要友人', 'important_friends'),
('Important Friends', 'important_friends'),

-- 經常出入場所相關
('經常出入場所', 'frequent_places'),
('Frequent Places', 'frequent_places'),

-- 出國紀錄相關
('出國紀錄', 'travel_records'),
('Travel Records', 'travel_records'),

-- 備註相關
('備註', 'notes'),
('Notes', 'notes'),
('Remarks', 'notes'),

-- 發掘經過相關
('發掘經過', 'discovery_process'),
('Discovery Process', 'discovery_process'),

-- 照片相關
('照片', 'photo'),
('Photo', 'photo'),
('Picture', 'photo')
ON CONFLICT (excel_field_name, db_field_name) DO NOTHING; 