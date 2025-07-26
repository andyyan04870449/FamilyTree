-- FamilyTree 專案 - field_mapping 表初始化資料
-- 此檔案包含系統運作必須的欄位對應資料
-- 使用方式: psql -U user -d familytree -h localhost -f FIELD_MAPPING_INIT.sql

-- 清空現有資料（可選）
-- TRUNCATE TABLE field_mapping;

-- 插入預設的欄位對應資料
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('姓名', 'name');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('名字', 'name');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Name', 'name');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('性別', 'gender');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Gender', 'gender');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('生日', 'birthday');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('出生日期', 'birthday');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Birthday', 'birthday');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Date of Birth', 'birthday');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('出生地', 'birthplace');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('父母戶籍所在地', 'birthplace');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Birthplace', 'birthplace');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Place of Birth', 'birthplace');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('國籍', 'nationality');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Nationality', 'nationality');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('民族', 'ethnicity');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Ethnicity', 'ethnicity');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('籍貫', 'ancestral_origin');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('祖父戶籍所在地', 'ancestral_origin');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Ancestral Home', 'ancestral_origin');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('黨派', 'political_party');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Political Party', 'political_party');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('身分證號碼', 'id_number');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('身份證號碼', 'id_number');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('ID Number', 'id_number');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Identity Number', 'id_number');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('護照號碼', 'passport_number');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Passport Number', 'passport_number');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('電話', 'phone');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Phone', 'phone');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Telephone', 'phone');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('行動電話', 'mobile');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('手機', 'mobile');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Mobile', 'mobile');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Cell Phone', 'mobile');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('電子信箱', 'email');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Email', 'email');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('E-mail', 'email');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('現職單位', 'current_employer');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('工作單位', 'current_employer');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Current Workplace', 'current_employer');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('現居地址', 'address');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('居住地址', 'address');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Current Address', 'address');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('通訊地址', 'mailing_address');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('聯絡地址', 'mailing_address');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Mailing Address', 'mailing_address');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('親屬關係', 'family_relationships');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Family Relationships', 'family_relationships');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('經歷', 'experience');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('工作經歷', 'experience');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Experience', 'experience');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('學歷', 'education');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Education', 'education');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('網路帳號', 'online_accounts');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Online Accounts', 'online_accounts');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('著作', 'publications');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Publications', 'publications');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('參與活動', 'activities');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Activities', 'activities');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('重要友人', 'important_friends');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Important Friends', 'important_friends');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('經常出入場所', 'frequent_locations');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Frequent Places', 'frequent_locations');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('出國紀錄', 'travel_history');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Travel Records', 'travel_history');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('備註', 'remarks');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Notes', 'remarks');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Remarks', 'remarks');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('發掘經過', 'discovery_process');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Discovery Process', 'discovery_process');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('照片', 'photo_index');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Photo', 'photo_index');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('Picture', 'photo_index');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('經歷(單位，職稱，任職期間)', 'experience');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('親屬關係(職稱，姓名)', 'family_relationships');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('著作(名稱，共同作者)', 'publications');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('參與活動(活動名稱，參與人士)', 'activities');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('重要友人(姓名，單位，關聯事件)', 'important_friends');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('出生地(父母戶籍所在地)', 'birthplace');
INSERT INTO field_mapping (excel_field_name, db_field_name) VALUES ('籍貫(祖父戶籍所在地)', 'ancestral_origin');

-- 檢查插入結果
SELECT COUNT(*) as total_mappings FROM field_mapping;
SELECT '完成 field_mapping 表初始化，共插入' || COUNT(*) || '筆欄位對應資料' as result FROM field_mapping; 