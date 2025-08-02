-- =============================================================================
-- 稽核日誌效能優化索引
-- 創建時間: 2025-08-02
-- 版本: 1.0.0
-- 描述: 為稽核日誌系統創建效能優化索引
-- =============================================================================

-- 1. 為分區表創建額外的複合索引以提升查詢性能

-- 最常用的查詢模式索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_user_date_event 
    ON audit_logs (user_id, occurred_at DESC, event_type) 
    INCLUDE (action, success, security_level);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_resource_date 
    ON audit_logs (resource_type, resource_id, occurred_at DESC) 
    INCLUDE (action, user_id, success);

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_event_date 
    ON audit_logs (event_type, occurred_at DESC) 
    INCLUDE (user_id, action, success, security_level);

-- 安全監控相關索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_security_monitoring 
    ON audit_logs (security_level, is_suspicious, occurred_at DESC) 
    WHERE security_level IN ('HIGH', 'CRITICAL') OR is_suspicious = true;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_failed_operations 
    ON audit_logs (success, event_type, occurred_at DESC) 
    WHERE success = false;

CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_risk_score 
    ON audit_logs (risk_score DESC, occurred_at DESC) 
    WHERE risk_score > 50;

-- 合規性查詢索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_compliance 
    ON audit_logs USING GIN (compliance_flags) 
    WHERE compliance_flags IS NOT NULL AND array_length(compliance_flags, 1) > 0;

-- IP 地址和會話追蹤索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_ip_session 
    ON audit_logs (ip_address, session_id, occurred_at DESC) 
    WHERE ip_address IS NOT NULL;

-- 批次操作索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_batch 
    ON audit_logs (batch_id, occurred_at) 
    WHERE batch_id IS NOT NULL;

-- 2. 為變更詳情表創建索引

-- 基本查詢索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_change_details_log_field 
    ON audit_change_details (audit_log_id, field_name);

-- 敏感欄位查詢索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_change_details_sensitive 
    ON audit_change_details (is_sensitive, field_name, created_at DESC) 
    WHERE is_sensitive = true;

-- 變更類型索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_change_details_change_type 
    ON audit_change_details (change_type, field_name, created_at DESC);

-- 3. 為存取記錄表創建索引

-- 存取者查詢索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_log_access_accessor 
    ON audit_log_access (accessor_user_id, accessed_at DESC);

-- 被存取資源索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_log_access_resource 
    ON audit_log_access (accessed_resource_type, accessed_resource_id, accessed_at DESC);

-- 存取類型索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_log_access_type 
    ON audit_log_access (access_type, accessed_at DESC);

-- 審批相關索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_log_access_approval 
    ON audit_log_access (approval_required, approved_by, approved_at) 
    WHERE approval_required = true;

-- IP 存取模式索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_log_access_ip 
    ON audit_log_access (accessor_ip, accessed_at DESC) 
    WHERE accessor_ip IS NOT NULL;

-- 4. 為合規性報告表創建索引

-- 報告類型和狀態索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_compliance_reports_type_status 
    ON audit_compliance_reports (report_type, status, generated_at DESC);

-- 生成者索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_compliance_reports_generator 
    ON audit_compliance_reports (generated_by, generated_at DESC);

-- 日期範圍索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_compliance_reports_date_range 
    ON audit_compliance_reports (date_from, date_to, report_type);

-- 保留期索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_compliance_reports_retention 
    ON audit_compliance_reports (retention_until) 
    WHERE retention_until IS NOT NULL;

-- 5. 創建部分索引以節省空間和提升性能

-- 只為有錯誤的記錄創建索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_errors 
    ON audit_logs (error_code, event_type, occurred_at DESC) 
    WHERE error_code IS NOT NULL;

-- 只為有回應時間的記錄創建索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_response_time 
    ON audit_logs (response_time_ms DESC, occurred_at DESC) 
    WHERE response_time_ms IS NOT NULL AND response_time_ms > 1000;

-- 只為系統事件創建索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_system_events 
    ON audit_logs (event_type, occurred_at DESC, user_id) 
    WHERE event_type LIKE 'SYSTEM_%';

-- 只為認證事件創建索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_auth_events 
    ON audit_logs (event_type, user_id, occurred_at DESC, success) 
    WHERE event_type IN ('LOGIN', 'LOGIN_FAILED', 'LOGOUT', 'PASSWORD_CHANGE', 'PASSWORD_RESET');

-- 6. 創建函數索引以支援特殊查詢

-- 按小時分組的索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_hourly 
    ON audit_logs (date_trunc('hour', occurred_at), event_type);

-- 按日分組的索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_daily 
    ON audit_logs (date_trunc('day', occurred_at), event_type, success);

-- 用戶名小寫索引（支援不區分大小寫搜尋）
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_user_name_lower 
    ON audit_logs (lower(user_name), occurred_at DESC) 
    WHERE user_name IS NOT NULL;

-- 7. 為 JSONB 欄位創建特定的 GIN 索引

-- 舊值特定路徑索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_old_values_user_info 
    ON audit_logs USING GIN ((old_values -> 'user')) 
    WHERE old_values ? 'user';

-- 新值特定路徑索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_new_values_user_info 
    ON audit_logs USING GIN ((new_values -> 'user')) 
    WHERE new_values ? 'user';

-- 額外資料中的特定欄位索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_audit_logs_additional_data_file_info 
    ON audit_logs USING GIN ((additional_data -> 'file')) 
    WHERE additional_data ? 'file';

-- 8. 統計資訊更新

-- 更新統計資訊以確保查詢規劃器有最新的資料分佈資訊
ANALYZE audit_logs;
ANALYZE audit_change_details;
ANALYZE audit_log_access;
ANALYZE audit_compliance_reports;
ANALYZE audit_event_types;

-- 9. 創建索引使用監控檢視

CREATE OR REPLACE VIEW audit_index_usage AS
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch,
    idx_scan,
    CASE WHEN idx_scan = 0 THEN 'UNUSED' 
         WHEN idx_scan < 100 THEN 'LOW_USAGE'
         WHEN idx_scan < 1000 THEN 'MEDIUM_USAGE'
         ELSE 'HIGH_USAGE' 
    END as usage_level
FROM pg_stat_user_indexes 
WHERE tablename LIKE 'audit_%'
ORDER BY idx_scan DESC, tablename, indexname;

-- 10. 創建索引大小監控檢視

CREATE OR REPLACE VIEW audit_index_sizes AS
SELECT 
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size,
    pg_size_pretty(pg_relation_size(indrelid)) as table_size,
    round(100.0 * pg_relation_size(indexrelid) / pg_relation_size(indrelid), 2) as index_ratio
FROM pg_stat_user_indexes 
WHERE tablename LIKE 'audit_%'
ORDER BY pg_relation_size(indexrelid) DESC;

-- 11. 創建慢查詢監控函數

CREATE OR REPLACE FUNCTION get_audit_slow_queries(
    min_duration_ms INTEGER DEFAULT 1000
)
RETURNS TABLE (
    query_text TEXT,
    calls BIGINT,
    total_time DOUBLE PRECISION,
    mean_time DOUBLE PRECISION,
    max_time DOUBLE PRECISION
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        pg_stat_statements.query,
        pg_stat_statements.calls,
        pg_stat_statements.total_exec_time,
        pg_stat_statements.mean_exec_time,
        pg_stat_statements.max_exec_time
    FROM pg_stat_statements 
    WHERE pg_stat_statements.query LIKE '%audit_logs%'
    AND pg_stat_statements.mean_exec_time > min_duration_ms
    ORDER BY pg_stat_statements.mean_exec_time DESC;
END;
$$ LANGUAGE plpgsql;

-- 12. 創建索引維護函數

CREATE OR REPLACE FUNCTION maintain_audit_indexes()
RETURNS TEXT AS $$
DECLARE
    result TEXT := '';
    rec RECORD;
BEGIN
    -- 重建統計資訊
    FOR rec IN 
        SELECT tablename FROM pg_tables WHERE tablename LIKE 'audit_%'
    LOOP
        EXECUTE 'ANALYZE ' || rec.tablename;
        result := result || 'Analyzed ' || rec.tablename || E'\n';
    END LOOP;
    
    -- 檢查索引膨脹
    FOR rec IN 
        SELECT schemaname, tablename, indexname
        FROM pg_stat_user_indexes 
        WHERE tablename LIKE 'audit_%'
        AND idx_scan < 10 
        AND pg_relation_size(indexrelid) > 1024 * 1024 -- 1MB
    LOOP
        result := result || 'Low usage index: ' || rec.indexname || ' on ' || rec.tablename || E'\n';
    END LOOP;
    
    RETURN result;
END;
$$ LANGUAGE plpgsql;

-- 13. 設定自動統計資訊更新

-- 設定較積極的統計資訊收集
ALTER TABLE audit_logs SET (autovacuum_analyze_scale_factor = 0.02);
ALTER TABLE audit_change_details SET (autovacuum_analyze_scale_factor = 0.05);
ALTER TABLE audit_log_access SET (autovacuum_analyze_scale_factor = 0.05);

-- 設定較積極的自動清理
ALTER TABLE audit_logs SET (autovacuum_vacuum_scale_factor = 0.1);
ALTER TABLE audit_change_details SET (autovacuum_vacuum_scale_factor = 0.1);

COMMENT ON INDEX idx_audit_logs_user_date_event IS '使用者、日期、事件類型複合索引，支援最常見的查詢模式';
COMMENT ON INDEX idx_audit_logs_security_monitoring IS '安全監控專用索引，用於檢測高風險和可疑活動';
COMMENT ON INDEX idx_audit_logs_compliance IS '合規性查詢專用索引，支援合規標記搜尋';
COMMENT ON FUNCTION maintain_audit_indexes() IS '稽核索引維護函數，定期執行以確保最佳性能';