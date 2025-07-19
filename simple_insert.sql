-- =====================================================
-- 🧬 AI關聯分析系統 - 簡單資料插入腳本
-- 版本：v1.3
-- 說明：此腳本將插入基本資料，JSON 資料稍後處理
-- =====================================================

-- 設定時區
SET timezone = 'Asia/Taipei';

-- 開始交易
BEGIN;

-- 清空現有資料
TRUNCATE TABLE person_profile RESTART IDENTITY CASCADE;

-- 插入基本資料（前 20 筆作為範例）
INSERT INTO person_profile 
(name, gender, birthday, nationality, mobile, phone, id_number, passport_number, family_relationships, friends, extra_data) 
VALUES 
('范立', '男', '1988-06-07', '中國', '13357913171', '8113457839955', '320204197608071330', 'EJ9804516', 
 '父：范統\n母：吳春華', 
 '張三，中國總商會東京華僑青年會副會長，中共東京使館中秋節慶祝活動工作人員\n李贄，早稻田大學政治系博士一年級，大學論社學長學弟\n佐藤真美子，住友不動產株式會社職員，女友',
 '{"email": "84313835435671@qq.com", "residence": "東京都品川", "current_employer": "東京大學經濟系研究生一年級"}'::jsonb),

('趙威', '男', '1975-04-15', '中國', '13884937455', '8613884937455', '460004197502151560', NULL, 
 '妻：項依潔\n女：趙小惠\n子：趙小亮', 
 NULL,
 '{"email": "vigor666@126.com", "residence": "山東省煙臺市文山區大江路", "current_employer": "煙台大學中文系"}'::jsonb),

('項依潔', '女', '1975-10-29', '中國', '13573590064', '8613573590064', '370629197510294987', NULL, 
 '夫：趙威\n女：趙小惠\n子：趙小亮', 
 '張濤，北京師範大學歷史學院（博士生導師）',
 '{"email": "ruru215@163.com", "residence": "山東省煙臺市文山區大江路", "current_employer": "煙台大學文學與新聞傳播學系副教授"}'::jsonb),

('李光', '男', '1990-05-17', '中國', '+861335687453', '861335687453', '371082199005173613', NULL, 
 '父：李立朝（1962.12.2）\n母：江彩芹（1961.11.11）', 
 '謝大方，麗星郵輪維修部主任，謝大方介紹李光入職',
 '{"email": "erty@sina.com", "residence": "台北市中山區", "current_employer": "麗星郵輪總務"}'::jsonb),

('沈家新', '女', '1978-09-18', '中國', '+8613357912344\n0917851414', NULL, '32020419780918162X', NULL, 
 '夫：謝大方\n女：謝圓圓', 
 NULL,
 '{"residence": "臺北市萬華區", "current_employer": "永慶房屋仲介"}'::jsonb),

('唐伯虎', '男', '1967-08-20', '中國', '17188407888', NULL, NULL, NULL, 
 NULL, 
 '姜大宇，新聞最前線主持人，於X上恭賀新年快樂。',
 '{"email": "bhtang@gmail.com", "current_employer": "50藍西門店店長"}'::jsonb),

('楊習五', '男', '1983-04-23', '中國', '0933-549-070\n0988-206-038', NULL, '入台許可證號113330495794', NULL, 
 '妻：姜恩為\n岳母：諶月英\n小叔：姜大宇', 
 '趙馥樂，具長時間北京工作經歷（實習生、總監），FB台籍好友',
 '{"email": "hi@ailleurslab.com", "residence": "新北市新店區文化路", "current_employer": "momo藝術策畫經理"}'::jsonb),

('李青春', '女', '1983-07-10', '中國', '0935-787-122', NULL, NULL, NULL, 
 '夫：羅大佑\n女：羅亞瑟\n表妹：姜恩為', 
 '常雨，羅東鎮新住民關懷協會理事長，共同場域\n女神美甲美睫(1120701-FB貼文)',
 '{"place_of_birth": "四川省", "current_employer": "旭東廣告工程"}'::jsonb),

('邱還真', '女', NULL, '中華民國', NULL, NULL, NULL, NULL, 
 '女：黃心田', 
 '馬晶，鶯之韻旗袍協會理事長，共同場域鶯之韻旗袍協會第一次會員大會餐敘聯誼 (1120423-FB貼文)',
 '{"residence": "宜蘭", "current_employer": "羅東鎮新住民關懷協會理事長"}'::jsonb),

('黃心田', '女', NULL, '中華民國', NULL, NULL, NULL, NULL, 
 '母：邱還真', 
 '羅亞瑟，淡江大學同學',
 '{"residence": "宜蘭", "current_employer": "羅東鎮新住民關懷協會常務監事"}'::jsonb),

('羅亞瑟', '女', NULL, NULL, NULL, NULL, '入台許可證號114665295794', NULL, 
 NULL, 
 NULL,
 '{"residence": "台北市中正區", "current_employer": "大創有限公司副總經理"}'::jsonb),

('李光', '女', '1993-12-05', '中華民國', NULL, NULL, 'F113456987', NULL, 
 NULL, 
 NULL,
 '{"residence": "新北市蘆洲", "place_of_birth": "新北市蘆洲", "current_employer": "大創有限公司人事主管"}'::jsonb),

('黃建宏', '男', '1966-12-26', '日本', '0964-876998', '0930-596601', 'D670336268', 'ZW40850954', 
 '妹妹，李惠如；妹妹，秦美玲', 
 '周雅，律理法律資訊有限公司；程佩珊，品誠資訊有限公司',
 '{"email": "natan@hotmail.com", "residence": "913 新營縣延平街14號4樓", "current_employer": "台灣力電"}'::jsonb),

('李俊謙', '男', '1980-09-02', '中國', '0918-097887', '01-7139898', 'G042901935', 'yW89536902', 
 '配偶，姜詩涵', 
 '陳濤，風微廣場股份有限公司；王家豪，大八電視有限公司',
 '{"email": "juan94@gmail.com", "residence": "451 褒忠縣象山巷2段47號之6", "current_employer": "秀威影城股份有限公司"}'::jsonb),

('廖信宏', '女', '1977-01-10', '日本', '01-93882090', '077 53244913', 'H456522036', 'hs49951542', 
 '母親，項依潔；姐姐，周雅娟', 
 '符志宏，光新三越百貨，Reverse-engineered optimizing hub；莫慧君，台北登來喜大飯店股份有限公司，Organic context-sensitive migration',
 '{"email": "tianchao@peng.net", "residence": "87411 古坑縣新生街41號5樓", "current_employer": "樂可旅遊集團資訊有限公司"}'::jsonb),

('溫馨', '女', '1984-06-03', '日本', '0928-319952', '(04) 47350476', 'D963044836', 'zq85456676', 
 '女兒，史佳穗；妹妹，史詩涵', 
 '趙雅娟，衣優庫（Nuiqlo）資訊有限公司，Right-sized heuristic moratorium；莫家豪，台灣業糖，Face-to-face bifurcated system engine；黎嘉玲，丹即企業資訊有限公司，Inverse dynamic protocol',
 '{"email": "qiangang@zou.tw", "residence": "95832 竹北縣太平街9號0樓", "current_employer": "隆豐大飯店（北台君悅）"}'::jsonb),

('米淑華', '男', '1983-07-02', '韓國', '02 5059960', '09-6307464', 'K791452118', 'EH37931269', 
 '女兒，冉俊宏；哥哥，王家豪', 
 '李惠如，大八電視有限公司，Multi-tiered 3rdgeneration methodology',
 '{"email": "fxiang@lu.net", "residence": "16162 屏東公園街7號之2", "current_employer": "華福大飯店資訊有限公司"}'::jsonb),

('陳佩珊', '男', '2004-04-26', '中國', '07 4675481', '02-92705102', 'Z289659994', 'ZN97320331', 
 '妹妹，王怡如；弟弟，牛冠宇', 
 '黃淑貞，中台信託商業銀行資訊有限公司，Function-based next generation moratorium',
 '{"email": "tqiao@yahoo.com", "residence": "92426 關山縣龍山寺街1號之7", "current_employer": "台灣來自水股份有限公司"}'::jsonb),

('周馨怡', '女', '1968-05-11', '日本', '05-29020357', '01 2971112', 'A412704938', 'DK20448515', 
 '兒子，李輝；父親，唐佩君；弟弟，賴郁飯', 
 '唐淑芬，饗國大飯店，Intuitive bandwidth-monitored moratorium；溫怡安，創群光電（奇原美電子），Managed systematic functionalities',
 '{"email": "yong32@long.com", "residence": "629 員林市公園路57號7樓", "current_employer": "心安食品服務（斯摩漢堡）股份有限公司"}'::jsonb),

('崔雅涵', '女', '1995-08-10', '台灣', '0913285072', '(02) 43244123', 'I282498392', 'kD50142576', 
 '母親，周馨怡', 
 '黃雅芳，禮爭資訊有限公司，Phased dynamic extranet；黃心田，興復航空運輸股份有限公司，Polarized systematic initiative',
 '{"email": "tangxia@hotmail.com", "residence": "273 台東縣大仁街769號5樓", "current_employer": "律理法律有限公司"}'::jsonb),

('王先生', '男', NULL, '中華民國', NULL, NULL, NULL, NULL, 
 '母：邱還真', 
 NULL,
 '{"residence": "宜蘭", "current_employer": "待業中"}'::jsonb);

-- 提交交易
COMMIT;

-- 顯示插入結果
SELECT COUNT(*) as total_records FROM person_profile; 