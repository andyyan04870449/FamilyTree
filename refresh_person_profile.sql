-- =====================================================
-- 🧬 AI關聯分析系統 - 重新整理 person_profile 資料表
-- 版本：v1.3
-- 說明：此腳本將清空現有資料並插入新的資料
-- =====================================================

-- 設定時區
SET timezone = 'Asia/Taipei';

-- 開始交易
BEGIN;

-- 清空現有資料
TRUNCATE TABLE person_profile RESTART IDENTITY CASCADE;

-- 重新插入資料
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

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '趙威',
    '男',
    '1975-04-15',
    '中國',
    '13884937455',
    '8613884937455',
    '460004197502151560',
    null,
    '妻：項依潔      女：趙小惠     子：趙小亮',
    null,
    '{"email": "vigor666@126.com", "residence": "山東省煙臺市文山區大江路", "mailing_address": "山東省煙臺市文山區大江路", "place_of_birth": "海南省三亞市", "ethnicity": "漢", "ancestral_origin": "海南省三亞市", "political_party": null, "current_employer": "煙台大學中文系", "education": null, "experience": "山東煙台大學，中文系教師， 2001.7-迄今", "social_account": null, "publications": "多功能菜刀\n", "activities": "煙台大學2024文化欣賞活動，邀請趙馥樂任藝術講師", "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '項依潔',
    '女',
    '1975-10-29',
    '中國',
    '13573590064',
    '8613573590064',
    '370629197510294987',
    null,
    ' 夫：趙威\n 女：趙小惠\n 子：趙小亮\n',
    ' 張濤 ，北京師範大學歷史學院（博士生導師）\n北京師範大學易學文化研究中心（主任）\n中國易學文化研究會（會長），2011年至2018年間與項永琴共同發表《中國古代城市防洪防災解析與借鑒》、《產權制·濤組與生態崇拜的變化》、《中國傳統救災思想研究》、《秦漢齊魯經學》\n',
    '{"email": "ruru215@163.com", "residence": "山東省煙臺市文山區大江路", "mailing_address": "山東省煙臺市文山區大江路", "place_of_birth": "山東省煙臺市", "ethnicity": "漢", "ancestral_origin": "山東省煙臺市", "political_party": null, "current_employer": "煙台大學文學與新聞傳播學系副教授", "education": null, "experience": " 煙臺大學，人文學院副教授， 2000迄今", "social_account": null, "publications": "《唐詩三百首》的價值，趙威\n 2013《論漢代詩歌中的防災救災主題》，趙威\n", "activities": null, "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '李光',
    '男',
    '1990-05-17',
    '中國',
    '+861335687453',
    '861335687453',
    '371082199005173613',
    null,
    '父：李立朝（1962.12.2）\n母：江彩芹（1961.11.11）\n',
    '蕭大方，麗星郵輪維修部主任，蕭大方介紹李光入職',
    '{"email": "erty@sina.com", "residence": "台北市中山區", "mailing_address": "台北市中山區", "place_of_birth": "山東省", "ethnicity": "漢", "ancestral_origin": "山東省", "political_party": null, "current_employer": "麗星郵輪總務", "education": null, "experience": "麗星郵輪海員", "social_account": null, "publications": null, "activities": null, "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '沈家新',
    '女',
    '1978-09-18',
    '中國',
    '+8613357912344\n0917851414\n',
    null,
    '32020419780918162X',
    null,
    '夫：蕭大方                     女：蕭圓圓',
    null,
    '{"email": null, "residence": "臺北市萬華區", "mailing_address": "臺北市萬華區", "place_of_birth": null, "ethnicity": "漢", "ancestral_origin": "上海市", "political_party": null, "current_employer": "永慶房屋仲介", "education": "北護專", "experience": null, "social_account": " FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827\n", "publications": null, "activities": null, "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '唐伯虎',
    '男',
    '1967-08-20',
    '中國',
    '17188407888',
    null,
    null,
    null,
    null,
    '姜大宇，新聞最前線主持人，於X上恭賀新年快樂。',
    '{"email": "bhtang@gmail.com\nloverongbh@yahoo.com\n", "residence": null, "mailing_address": null, "place_of_birth": null, "ethnicity": "漢", "ancestral_origin": "湖南永州", "political_party": null, "current_employer": "50藍西門店店長", "education": null, "experience": null, "social_account": "FB(未再使用) ：100063587324567\n", "publications": null, "activities": null, "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '楊習五',
    '男',
    '1983-04-23',
    '中國',
    '0933-549-070\n0988-206-038\n',
    null,
    '入台許可證號113330495794',
    null,
    ' 妻：姜恩為\n 岳母：賴月英\n 小叔：姜大宇',
    ' 趙馨樂，具長時間北京工作經歷（實習生、總監），FB台籍好友',
    '{"email": "hi@ailleurslab.com", "residence": "新北市新店區文化路", "mailing_address": "新北市新店區文化路", "place_of_birth": null, "ethnicity": "漢", "ancestral_origin": "山東省", "political_party": null, "current_employer": "momo藝術策畫經理", "education": "1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士\n", "experience": "1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年\n", "social_account": null, "publications": null, "activities": null, "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '李青春',
    '女',
    '1983-07-10',
    '中國',
    ' 0935-787-122',
    null,
    null,
    null,
    '夫：羅大佑                     女：羅亞瑟        表妹：姜恩為',
    '常雨，陸配關懷協會，FBUID:100005521629975\n楊玉玲，陸配關懷協會，FBUID:100004792441549\n武新莉，陸配關懷協會，FBUID:100006498932852                              \n邱還真，羅東鎮新住民關懷協會理事長，共同場域\n女神美甲美睫(1120701-FB貼文)\n黃心田，羅東鎮新住民關懷協會常務監事，共同場域\n女神美甲美睫(1120701-FB貼文)\n馬晶，鶯之韻旗袍協會理事長共同場域\n女神美甲美睫(1120701-FB貼文)   ',
    '{"email": null, "residence": null, "mailing_address": null, "place_of_birth": "四川省", "ethnicity": "漢", "ancestral_origin": "四川省", "political_party": null, "current_employer": "旭東廣告工程", "education": null, "experience": "社團法人羅東鎮新住民關懷服務協會總幹事，現職", "social_account": null, "publications": null, "activities": null, "frequent_places": "女神美甲美睫", "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '邱還真',
    '女',
    null,
    '中華民國',
    null,
    null,
    null,
    null,
    '女：黃心田',
    '馬晶，鶯之韻旗袍協會理事長，共同場域鶯之韻旗袍協會第一次會員大會餐敘聯誼 (1120423-FB貼文)\n',
    '{"email": null, "residence": "宜蘭", "mailing_address": null, "place_of_birth": null, "ethnicity": null, "ancestral_origin": null, "political_party": null, "current_employer": "羅東鎮新住民關懷協會理事長", "education": null, "experience": null, "social_account": null, "publications": null, "activities": "20230423(台)鳳之韻新住民旗袍關懷協會餐敘\n20230808福建平潭綜合試驗區申辦區內銀行帳戶及手機門號\n20231203廣東廣州第十七屆世界海南香團聯誼大會「港灣大融合共享新機遇」，美、日、阿、越、德、柬、丹麥、奧地利等國同鄉會及商會\n20231205廣東世界婦女論壇\n20240815湖北恩師女兒會\n20241223雲南未來生物贏家論壇\n\n\n\n\n\n\n\n\n\n\n\n\n\n      ", "frequent_places": null, "travel_history": null, "note": "\n\n", "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '黃心田',
    '女',
    null,
    '中華民國',
    null,
    null,
    null,
    null,
    '母：邱還真',
    '羅亞瑟，淡江大學同學',
    '{"email": null, "residence": "宜蘭", "mailing_address": null, "place_of_birth": null, "ethnicity": null, "ancestral_origin": null, "political_party": null, "current_employer": "羅東鎮新住民關懷協會常務監事", "education": "淡江大學", "experience": null, "social_account": null, "publications": null, "activities": null, "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '羅亞瑟',
    '女',
    null,
    null,
    null,
    null,
    '入台許可證號114665295794',
    null,
    null,
    null,
    '{"email": null, "residence": "台北市中正區", "mailing_address": null, "place_of_birth": null, "ethnicity": null, "ancestral_origin": null, "political_party": null, "current_employer": "大創有限公司副總經理", "education": null, "experience": null, "social_account": null, "publications": null, "activities": null, "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
);

INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES (
    '李光',
    '女',
    '1993-12-05',
    '中華民國',
    null,
    null,
    'F113456987',
    null,
    null,
    null,
    '{"email": null, "residence": "新北市蘆洲", "mailing_address": null, "place_of_birth": "新北市蘆洲", "ethnicity": null, "ancestral_origin": null, "political_party": null, "current_employer": "大創有限公司人事主管", "education": null, "experience": null, "social_account": null, "publications": null, "activities": null, "frequent_places": null, "travel_history": null, "note": null, "created_by": null, "updated_by": null}'::jsonb
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