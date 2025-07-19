-- =====================================================
-- 🧬 AI關聯分析系統 - 完整重新整理 person_profile 資料表
-- 版本：v1.3
-- 說明：此腳本將清空現有資料並插入所有新資料
-- =====================================================

-- 設定時區
SET timezone = 'Asia/Taipei';

-- 開始交易
BEGIN;

-- 清空現有資料
TRUNCATE TABLE person_profile RESTART IDENTITY CASCADE;

-- 重新插入所有資料
-- 注意：這裡只包含前幾筆資料作為範例，完整資料請使用 person_profile_insert.sql 檔案

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '范立',
    '男',
    '1988-06-07',
    '中國',
    '13357913171',
    '8113457839955',
    '320204197608071330',
    'EJ9804516',
    '父：范統\n母：吳春華',
    '張三，中國總商會東京華僑青年會副會長，中共東京使館中秋節慶祝活動工作人員\n李贄，早稻田大學政治系博士一年級，大學論社學長學弟\n佐藤真美子，住友不動產株式會社職員，女友',
    '{"email": "84313835435671@qq.com\nfanli555@gmail.com", "residence": "東京都品川", "mailing_address": "上海市惠暢里小區50號60室", "place_of_birth": "上海市", "ethnicity": "滿族", "ancestral_origin": "江蘇", "political_party": null, "current_employer": "東京大學經濟系研究生一年級", "education": "復旦大學經濟系", "experience": "上海華為公司國際商務部，實習生，2021年6-8月", "social_account": "FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349", "publications": "以運動經濟推进中国式现代化的决定策略，江南海", "activities": "「中」日建交75周年慶祝茶會，中國駐日代表薛健、副代表孫一真、商務部駐日參讚王桂鍾、東京都知事川崎朗、東京都議員鈴木康一\n棒球社團，中國總商會青年會副會長張三、東京華為公司工程師黃文偉、陳亮、全家便利商店店員吳士達", "frequent_places": null, "travel_history": "\n2015年1月，台灣2018年2月，美國\n2019年7月，泰國", "note": null, "created_by": null, "updated_by": null}'::jsonb
);

-- 提交交易
COMMIT;

-- 顯示插入結果
SELECT 
    COUNT(*) as total_records,
    COUNT(CASE WHEN gender = '男' THEN 1 END) as male_count,
    COUNT(CASE WHEN gender = '女' THEN 1 END) as female_count,
    COUNT(CASE WHEN gender IS NULL OR gender = '' THEN 1 END) as unknown_gender_count
FROM person_profile;

-- 顯示前10筆資料
SELECT 
    id,
    name,
    gender,
    birthday,
    nationality,
    mobile,
    created_at
FROM person_profile 
ORDER BY id 
LIMIT 10; 