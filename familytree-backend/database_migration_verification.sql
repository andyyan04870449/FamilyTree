-- ==========================================
-- 多專案架構資料庫遷移驗證腳本
-- ==========================================

-- 驗證 1: 檢查 projects 表結構和資料
\echo '=== 驗證 1: projects 表結構和資料 ==='
\d projects;
SELECT * FROM projects;

-- 驗證 2: 檢查所有相關表格是否都有 project_id 欄位
\echo '=== 驗證 2: 檢查表格結構中的 project_id 欄位 ==='
SELECT 
    table_name,
    column_name,
    data_type,
    character_maximum_length,
    is_nullable
FROM information_schema.columns 
WHERE column_name = 'project_id' 
    AND table_schema = 'public'
ORDER BY table_name;

-- 驗證 3: 檢查各表格資料分佈
\echo '=== 驗證 3: 各表格資料分佈統計 ==='
SELECT 'projects' as table_name, COUNT(*) as total_records, COUNT(DISTINCT id) as unique_projects FROM projects
UNION ALL
SELECT 'person_profile', COUNT(*), COUNT(DISTINCT project_id) FROM person_profile
UNION ALL  
SELECT 'analysis_results', COUNT(*), COUNT(DISTINCT project_id) FROM analysis_results
UNION ALL
SELECT 'analysis_sessions', COUNT(*), COUNT(DISTINCT project_id) FROM analysis_sessions
UNION ALL
SELECT 'relationship_layers', COUNT(*), COUNT(DISTINCT project_id) FROM relationship_layers
UNION ALL
SELECT 'missing_persons', COUNT(*), COUNT(DISTINCT project_id) FROM missing_persons
UNION ALL
SELECT 'user_update_file', COUNT(*), COUNT(DISTINCT project_id) FROM user_update_file
UNION ALL
SELECT 'field_mapping', COUNT(*), COUNT(DISTINCT project_id) FROM field_mapping
UNION ALL
SELECT 'search_keywords', COUNT(*), COUNT(DISTINCT project_id) FROM search_keywords
UNION ALL
SELECT 'user_favorites', COUNT(*), COUNT(DISTINCT project_id) FROM user_favorites
UNION ALL
SELECT 'search_logs', COUNT(*), COUNT(DISTINCT project_id) FROM search_logs
ORDER BY table_name;

-- 驗證 4: 檢查外鍵約束
\echo '=== 驗證 4: 外鍵約束檢查 ==='
SELECT 
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
    AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
    AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY' 
    AND ccu.table_name = 'projects'
ORDER BY tc.table_name;

-- 驗證 5: 檢查索引
\echo '=== 驗證 5: project_id 相關索引檢查 ==='
SELECT 
    indexname,
    tablename,
    indexdef
FROM pg_indexes 
WHERE indexname LIKE '%project_id%'
ORDER BY tablename;

-- 驗證 6: 檢查是否有 NULL 值
\echo '=== 驗證 6: 檢查 project_id 欄位是否有 NULL 值 ==='
DO $$
DECLARE
    rec RECORD;
    null_count INTEGER;
    table_list TEXT[] := ARRAY[
        'person_profile', 'analysis_results', 'analysis_sessions', 
        'relationship_layers', 'missing_persons', 'user_update_file',
        'field_mapping', 'search_keywords', 'user_favorites', 'search_logs'
    ];
    sql_text TEXT;
BEGIN
    FOR i IN 1..array_length(table_list, 1) LOOP
        sql_text := 'SELECT COUNT(*) FROM ' || table_list[i] || ' WHERE project_id IS NULL';
        EXECUTE sql_text INTO null_count;
        RAISE NOTICE '表格 %: % 筆 NULL 值', table_list[i], null_count;
    END LOOP;
END $$;

-- 驗證 7: 測試外鍵約束是否有效
\echo '=== 驗證 7: 測試外鍵約束 ==='
DO $$
BEGIN
    -- 嘗試插入無效的專案ID，應該會失敗
    BEGIN
        INSERT INTO person_profile (name, project_id) VALUES ('測試人員', 'invalid-project-id');
        RAISE NOTICE '❌ 外鍵約束失效：允許插入無效專案ID';
    EXCEPTION WHEN foreign_key_violation THEN
        RAISE NOTICE '✅ 外鍵約束正常：拒絕無效專案ID';
    END;
END $$;

-- 驗證 8: 專案資料完整性檢查
\echo '=== 驗證 8: 專案資料完整性檢查 ==='
SELECT 
    p.id as project_id,
    p.project_name,
    p.user_id,
    p.status,
    (SELECT COUNT(*) FROM person_profile pp WHERE pp.project_id = p.id) as person_count,
    (SELECT COUNT(*) FROM relationship_layers rl WHERE rl.project_id = p.id) as relationship_count,
    (SELECT COUNT(*) FROM field_mapping fm WHERE fm.project_id = p.id) as mapping_count,
    (SELECT COUNT(*) FROM search_logs sl WHERE sl.project_id = p.id) as search_count
FROM projects p;

\echo '==========================================';
\echo '多專案架構資料庫遷移驗證完成！';
\echo '==========================================';
\echo '請檢查上述結果，確認：';
\echo '1. projects 表已正確建立且有預設專案';
\echo '2. 所有相關表格都有 project_id 欄位';
\echo '3. 所有現有資料都已分配到預設專案';
\echo '4. 外鍵約束正常運作';
\echo '5. 索引已建立';
\echo '6. 沒有 NULL 值';
\echo '=========================================='; 