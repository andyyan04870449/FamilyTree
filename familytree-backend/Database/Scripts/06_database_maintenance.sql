-- =============================================
-- 資料庫維護腳本
-- 執行順序：6
-- 功能：資料庫效能維護和監控
-- =============================================

-- 1. 手動維護腳本

-- 1.1 VACUUM 和 ANALYZE 所有主要表格
-- 建議在低峰期執行
DO $$
DECLARE
    table_name TEXT;
    table_list TEXT[] := ARRAY[
        'person_profile', 'relationships', 'file_metadata', 
        'user_favorites', 'analysis_sessions', 'analysis_results',
        'file_uploads', 'users', 'activity_logs'
    ];
BEGIN
    FOREACH table_name IN ARRAY table_list
    LOOP
        RAISE NOTICE '正在維護表格: %', table_name;
        EXECUTE format('VACUUM ANALYZE %I', table_name);
        RAISE NOTICE '表格 % 維護完成', table_name;
    END LOOP;
END $$;

-- 1.2 更新資料庫統計資訊
ANALYZE person_profile;
ANALYZE relationships;
ANALYZE file_metadata;
ANALYZE user_favorites;
ANALYZE analysis_sessions;
ANALYZE file_uploads;

-- 2. 建立自動維護函數

-- 2.1 表格大小監控函數
CREATE OR REPLACE FUNCTION get_table_sizes()
RETURNS TABLE (
    table_name TEXT,
    size_bytes BIGINT,
    size_mb NUMERIC,
    row_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        t.table_name::TEXT,
        pg_total_relation_size(t.table_name::regclass) as size_bytes,
        ROUND(pg_total_relation_size(t.table_name::regclass) / 1024.0 / 1024.0, 2) as size_mb,
        (SELECT n_tup_ins + n_tup_upd + n_tup_del FROM pg_stat_user_tables WHERE relname = t.table_name) as row_count
    FROM (
        VALUES 
        ('person_profile'),
        ('relationships'),
        ('file_metadata'),
        ('user_favorites'),
        ('analysis_sessions'),
        ('file_uploads'),
        ('users'),
        ('activity_logs')
    ) AS t(table_name)
    WHERE EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = t.table_name
    )
    ORDER BY size_bytes DESC;
END;
$$ LANGUAGE plpgsql;

-- 2.2 索引使用率監控函數
CREATE OR REPLACE FUNCTION get_index_usage_stats()
RETURNS TABLE (
    table_name TEXT,
    index_name TEXT,
    index_scans BIGINT,
    index_tup_read BIGINT,
    index_tup_fetch BIGINT,
    size_mb NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        schemaname || '.' || tablename as table_name,
        indexrelname as index_name,
        idx_scan as index_scans,
        idx_tup_read as index_tup_read,
        idx_tup_fetch as index_tup_fetch,
        ROUND(pg_relation_size(indexrelname::regclass) / 1024.0 / 1024.0, 2) as size_mb
    FROM pg_stat_user_indexes 
    WHERE schemaname = 'public'
    AND tablename IN (
        'person_profile', 'relationships', 'file_metadata', 
        'user_favorites', 'analysis_sessions', 'file_uploads'
    )
    ORDER BY idx_scan DESC;
END;
$$ LANGUAGE plpgsql;

-- 2.3 查詢性能監控函數
CREATE OR REPLACE FUNCTION get_slow_queries()
RETURNS TABLE (
    query_text TEXT,
    calls BIGINT,
    total_time_ms NUMERIC,
    mean_time_ms NUMERIC,
    max_time_ms NUMERIC
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        SUBSTRING(query, 1, 100) as query_text,
        calls,
        ROUND(total_exec_time::NUMERIC, 2) as total_time_ms,
        ROUND(mean_exec_time::NUMERIC, 2) as mean_time_ms,
        ROUND(max_exec_time::NUMERIC, 2) as max_time_ms
    FROM pg_stat_statements 
    WHERE query LIKE '%person_profile%' 
    OR query LIKE '%relationships%'
    OR query LIKE '%file_metadata%'
    ORDER BY mean_exec_time DESC
    LIMIT 20;
EXCEPTION
    WHEN undefined_table THEN
        RAISE NOTICE 'pg_stat_statements extension is not installed';
        RETURN;
END;
$$ LANGUAGE plpgsql;

-- 2.4 自動維護程序（每日執行）
CREATE OR REPLACE FUNCTION daily_maintenance()
RETURNS TEXT AS $$
DECLARE
    start_time TIMESTAMP := clock_timestamp();
    end_time TIMESTAMP;
    result_message TEXT := '';
BEGIN
    -- 記錄開始
    INSERT INTO activity_logs (user_id, action, details) 
    VALUES ('system', 'daily_maintenance_start', 'Starting daily database maintenance');
    
    -- 執行 VACUUM ANALYZE（僅限小型表）
    VACUUM ANALYZE user_favorites;
    VACUUM ANALYZE analysis_sessions;
    
    -- 更新統計資訊
    ANALYZE person_profile;
    ANALYZE relationships;
    ANALYZE file_metadata;
    
    -- 清理舊的活動日誌（保留30天）
    DELETE FROM activity_logs 
    WHERE created_at < NOW() - INTERVAL '30 days';
    
    -- 計算執行時間
    end_time := clock_timestamp();
    result_message := 'Daily maintenance completed in ' || 
                     EXTRACT(EPOCH FROM (end_time - start_time))::TEXT || ' seconds';
    
    -- 記錄完成
    INSERT INTO activity_logs (user_id, action, details) 
    VALUES ('system', 'daily_maintenance_complete', result_message);
    
    RETURN result_message;
END;
$$ LANGUAGE plpgsql;

-- 2.5 週期性重建索引函數（每週執行）
CREATE OR REPLACE FUNCTION weekly_index_maintenance()
RETURNS TEXT AS $$
DECLARE
    index_name TEXT;
    result_message TEXT := '';
    bloat_ratio NUMERIC;
BEGIN
    -- 記錄開始
    INSERT INTO activity_logs (user_id, action, details) 
    VALUES ('system', 'weekly_index_maintenance_start', 'Starting weekly index maintenance');
    
    -- 重建可能有bloat的索引
    FOR index_name IN 
        SELECT indexname 
        FROM pg_indexes 
        WHERE tablename IN ('person_profile', 'relationships', 'file_metadata')
        AND indexname LIKE 'idx_%'
    LOOP
        BEGIN
            EXECUTE format('REINDEX INDEX CONCURRENTLY %I', index_name);
            result_message := result_message || 'Rebuilt index: ' || index_name || E'\n';
        EXCEPTION
            WHEN OTHERS THEN
                result_message := result_message || 'Failed to rebuild index: ' || index_name || E'\n';
        END;
    END LOOP;
    
    -- 記錄完成
    INSERT INTO activity_logs (user_id, action, details) 
    VALUES ('system', 'weekly_index_maintenance_complete', result_message);
    
    RETURN result_message;
END;
$$ LANGUAGE plpgsql;

-- 3. 建立監控視圖

-- 3.1 資料庫健康狀況視圖
CREATE OR REPLACE VIEW database_health_view AS
SELECT 
    'table_count' as metric,
    COUNT(*)::TEXT as value,
    'Number of user tables' as description
FROM information_schema.tables 
WHERE table_schema = 'public'
UNION ALL
SELECT 
    'index_count' as metric,
    COUNT(*)::TEXT as value,
    'Number of indexes' as description
FROM pg_indexes 
WHERE schemaname = 'public'
UNION ALL
SELECT 
    'database_size' as metric,
    pg_size_pretty(pg_database_size(current_database())) as value,
    'Total database size' as description
UNION ALL
SELECT 
    'largest_table' as metric,
    (SELECT table_name FROM get_table_sizes() LIMIT 1) as value,
    'Largest table by size' as description;

-- 3.2 性能指標視圖
CREATE OR REPLACE VIEW performance_metrics_view AS
SELECT 
    'active_connections' as metric,
    COUNT(*)::TEXT as value
FROM pg_stat_activity 
WHERE state = 'active'
UNION ALL
SELECT 
    'total_queries' as metric,
    SUM(calls)::TEXT as value
FROM pg_stat_statements
WHERE query NOT LIKE '%pg_stat%'
UNION ALL
SELECT 
    'cache_hit_ratio' as metric,
    ROUND(
        100.0 * SUM(blks_hit) / NULLIF(SUM(blks_hit) + SUM(blks_read), 0), 2
    )::TEXT || '%' as value
FROM pg_stat_database
WHERE datname = current_database();

-- 4. 建立維護任務排程提醒

-- 4.1 建立維護提醒表
CREATE TABLE IF NOT EXISTS maintenance_schedule (
    id SERIAL PRIMARY KEY,
    task_name VARCHAR(100) NOT NULL,
    task_type VARCHAR(50) NOT NULL,
    schedule_expression VARCHAR(100) NOT NULL,
    last_run TIMESTAMP,
    next_run TIMESTAMP,
    is_enabled BOOLEAN DEFAULT true,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 插入預設維護任務
INSERT INTO maintenance_schedule (task_name, task_type, schedule_expression, description, next_run)
VALUES 
    ('Daily Maintenance', 'daily', '0 2 * * *', 'Daily VACUUM ANALYZE and cleanup', CURRENT_DATE + INTERVAL '1 day' + INTERVAL '2 hours'),
    ('Weekly Index Rebuild', 'weekly', '0 3 * * 0', 'Weekly index maintenance', DATE_TRUNC('week', CURRENT_DATE) + INTERVAL '1 week' + INTERVAL '3 hours'),
    ('Monthly Statistics Update', 'monthly', '0 4 1 * *', 'Monthly full VACUUM and statistics update', DATE_TRUNC('month', CURRENT_DATE) + INTERVAL '1 month' + INTERVAL '4 hours')
ON CONFLICT DO NOTHING;

-- 5. 建立性能基準測試函數

CREATE OR REPLACE FUNCTION performance_benchmark()
RETURNS TABLE (
    test_name TEXT,
    execution_time_ms NUMERIC,
    result_count BIGINT,
    timestamp TIMESTAMP
) AS $$
DECLARE
    start_time TIMESTAMP;
    end_time TIMESTAMP;
    test_count BIGINT;
BEGIN
    -- 測試 1: 人員列表查詢
    start_time := clock_timestamp();
    SELECT COUNT(*) INTO test_count FROM person_profile LIMIT 1000;
    end_time := clock_timestamp();
    
    RETURN QUERY SELECT 
        'Person List Query'::TEXT as test_name,
        EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 as execution_time_ms,
        test_count as result_count,
        start_time as timestamp;
    
    -- 測試 2: 複雜查詢（如果 relationships 表有資料）
    start_time := clock_timestamp();
    SELECT COUNT(*) INTO test_count 
    FROM person_profile p 
    LEFT JOIN relationships r ON p.id = r.person_id 
    LIMIT 100;
    end_time := clock_timestamp();
    
    RETURN QUERY SELECT 
        'Complex Join Query'::TEXT as test_name,
        EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 as execution_time_ms,
        test_count as result_count,
        start_time as timestamp;
    
    -- 測試 3: 全文搜索
    start_time := clock_timestamp();
    SELECT COUNT(*) INTO test_count 
    FROM person_profile 
    WHERE to_tsvector('simple', coalesce(name,'') || ' ' || coalesce(email,'')) @@ to_tsquery('simple', 'test')
    LIMIT 100;
    end_time := clock_timestamp();
    
    RETURN QUERY SELECT 
        'Full Text Search'::TEXT as test_name,
        EXTRACT(EPOCH FROM (end_time - start_time)) * 1000 as execution_time_ms,
        test_count as result_count,
        start_time as timestamp;
END;
$$ LANGUAGE plpgsql;

-- 6. 記錄維護腳本執行
INSERT INTO activity_logs (user_id, action, details) 
VALUES (
    'admin_default',
    'database_maintenance_setup',
    '建立資料庫維護腳本和監控函數'
);

-- 顯示結果
SELECT 'Database maintenance scripts created successfully' as result;

-- 執行初始性能基準測試
SELECT * FROM performance_benchmark();