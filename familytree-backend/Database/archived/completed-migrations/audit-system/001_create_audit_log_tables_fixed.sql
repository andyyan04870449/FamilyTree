-- =============================================================================
-- 家族樹系統稽核日誌資料庫架構 (修正版)
-- 創建時間: 2025-08-02
-- 版本: 1.0.1
-- 描述: 完整的稽核日誌系統，支援多種事件類型、變更追蹤、批次操作關聯
-- =============================================================================

-- 1. 稽核日誌主表 (簡化版，不使用分區以避免外鍵問題)
DROP TABLE IF EXISTS audit_logs CASCADE;

CREATE TABLE audit_logs (
    id BIGSERIAL PRIMARY KEY,
    
    -- 基本資訊
    event_id UUID NOT NULL DEFAULT gen_random_uuid(),           -- 唯一事件識別碼
    batch_id UUID,                                             -- 批次操作關聯ID（用於關聯多個相關操作）
    session_id VARCHAR(128),                                   -- 會話ID
    
    -- 使用者資訊
    user_id VARCHAR(128),                                      -- 執行操作的使用者ID
    user_name VARCHAR(256),                                    -- 使用者名稱（冗餘，便於查詢）
    user_role VARCHAR(64),                                     -- 使用者角色
    impersonator_id VARCHAR(128),                              -- 代理操作者ID（如果有的話）
    
    -- 操作資訊
    event_type VARCHAR(64) NOT NULL,                           -- 事件類型（LOGIN, DATA_ACCESS, DATA_MODIFY等）
    action VARCHAR(128) NOT NULL,                              -- 具體操作（CREATE, UPDATE, DELETE, VIEW等）
    resource_type VARCHAR(128),                                -- 資源類型（Person, File, Setting等）
    resource_id VARCHAR(256),                                  -- 資源ID
    resource_name VARCHAR(512),                                -- 資源名稱
    
    -- 變更詳情
    old_values JSONB,                                          -- 變更前的值
    new_values JSONB,                                          -- 變更後的值
    changes_summary TEXT,                                      -- 變更摘要（人類可讀）
    
    -- 技術資訊
    ip_address INET,                                           -- 客戶端IP地址
    user_agent TEXT,                                           -- 用戶代理資訊
    request_method VARCHAR(16),                                -- HTTP方法
    request_url TEXT,                                          -- 請求URL
    request_id VARCHAR(128),                                   -- 請求追蹤ID
    
    -- 結果資訊
    success BOOLEAN NOT NULL DEFAULT true,                     -- 操作是否成功
    error_message TEXT,                                        -- 錯誤訊息（如果失敗）
    error_code VARCHAR(32),                                    -- 錯誤代碼
    response_time_ms INTEGER,                                  -- 響應時間（毫秒）
    
    -- 安全資訊
    security_level VARCHAR(32) DEFAULT 'NORMAL',               -- 安全等級（LOW, NORMAL, HIGH, CRITICAL）
    risk_score INTEGER DEFAULT 0,                             -- 風險評分（0-100）
    is_suspicious BOOLEAN DEFAULT false,                       -- 是否可疑操作
    
    -- 時間資訊
    occurred_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- 額外資訊
    additional_data JSONB,                                     -- 額外的上下文資料
    tags TEXT[],                                               -- 標籤陣列（便於分類和搜尋）
    compliance_flags TEXT[]                                    -- 合規標記
);

-- 創建索引
CREATE INDEX idx_audit_logs_event_id ON audit_logs (event_id);
CREATE INDEX idx_audit_logs_batch_id ON audit_logs (batch_id) WHERE batch_id IS NOT NULL;
CREATE INDEX idx_audit_logs_user_id ON audit_logs (user_id);
CREATE INDEX idx_audit_logs_event_type ON audit_logs (event_type);
CREATE INDEX idx_audit_logs_action ON audit_logs (action);
CREATE INDEX idx_audit_logs_resource ON audit_logs (resource_type, resource_id);
CREATE INDEX idx_audit_logs_security ON audit_logs (security_level, is_suspicious);
CREATE INDEX idx_audit_logs_success ON audit_logs (success);
CREATE INDEX idx_audit_logs_ip ON audit_logs (ip_address);
CREATE INDEX idx_audit_logs_session ON audit_logs (session_id) WHERE session_id IS NOT NULL;
CREATE INDEX idx_audit_logs_occurred_at ON audit_logs (occurred_at DESC);

-- GIN 索引用於 JSONB 資料和陣列搜尋
CREATE INDEX idx_audit_logs_old_values ON audit_logs USING GIN (old_values);
CREATE INDEX idx_audit_logs_new_values ON audit_logs USING GIN (new_values);
CREATE INDEX idx_audit_logs_additional_data ON audit_logs USING GIN (additional_data);
CREATE INDEX idx_audit_logs_tags ON audit_logs USING GIN (tags);

-- 複合索引用於常見查詢模式
CREATE INDEX idx_audit_logs_user_time ON audit_logs (user_id, occurred_at DESC);
CREATE INDEX idx_audit_logs_resource_time ON audit_logs (resource_type, resource_id, occurred_at DESC);
CREATE INDEX idx_audit_logs_event_time ON audit_logs (event_type, occurred_at DESC);

-- 2. 事件類型定義表
DROP TABLE IF EXISTS audit_event_types CASCADE;

CREATE TABLE audit_event_types (
    id SERIAL PRIMARY KEY,
    code VARCHAR(64) UNIQUE NOT NULL,                          -- 事件類型代碼
    name VARCHAR(256) NOT NULL,                                -- 事件類型名稱
    description TEXT,                                          -- 描述
    category VARCHAR(64),                                      -- 分類（SECURITY, BUSINESS, SYSTEM等）
    severity VARCHAR(32) DEFAULT 'INFO',                       -- 嚴重性（DEBUG, INFO, WARN, ERROR, CRITICAL）
    retention_days INTEGER DEFAULT 90,                        -- 保留天數
    requires_approval BOOLEAN DEFAULT false,                  -- 是否需要審批
    compliance_required BOOLEAN DEFAULT false,                -- 是否為合規必要事件
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 插入預定義的事件類型
INSERT INTO audit_event_types (code, name, description, category, severity, retention_days, compliance_required) VALUES
-- 認證相關
('LOGIN', '使用者登入', '使用者成功登入系統', 'SECURITY', 'INFO', 365, true),
('LOGIN_FAILED', '登入失敗', '使用者登入失敗', 'SECURITY', 'WARN', 365, true),
('LOGOUT', '使用者登出', '使用者登出系統', 'SECURITY', 'INFO', 90, false),
('PASSWORD_CHANGE', '密碼變更', '使用者變更密碼', 'SECURITY', 'INFO', 365, true),
('PASSWORD_RESET', '密碼重設', '管理員重設使用者密碼', 'SECURITY', 'WARN', 365, true),

-- 授權相關
('PERMISSION_GRANTED', '權限授予', '使用者獲得新權限', 'SECURITY', 'WARN', 365, true),
('PERMISSION_REVOKED', '權限撤銷', '使用者權限被撤銷', 'SECURITY', 'WARN', 365, true),
('ROLE_CHANGED', '角色變更', '使用者角色被變更', 'SECURITY', 'WARN', 365, true),
('ACCESS_DENIED', '存取拒絕', '使用者嘗試存取無權限資源', 'SECURITY', 'WARN', 180, true),

-- 資料操作
('DATA_VIEW', '資料檢視', '使用者檢視資料', 'BUSINESS', 'DEBUG', 30, false),
('DATA_CREATE', '資料建立', '建立新資料記錄', 'BUSINESS', 'INFO', 365, true),
('DATA_UPDATE', '資料更新', '更新現有資料記錄', 'BUSINESS', 'INFO', 365, true),
('DATA_DELETE', '資料刪除', '刪除資料記錄', 'BUSINESS', 'WARN', 2555, true), -- 7年保存
('DATA_EXPORT', '資料匯出', '匯出資料', 'BUSINESS', 'INFO', 365, true),
('DATA_IMPORT', '資料匯入', '匯入資料', 'BUSINESS', 'INFO', 365, true),

-- 檔案操作
('FILE_UPLOAD', '檔案上傳', '上傳檔案', 'BUSINESS', 'INFO', 365, false),
('FILE_DOWNLOAD', '檔案下載', '下載檔案', 'BUSINESS', 'INFO', 90, false),
('FILE_DELETE', '檔案刪除', '刪除檔案', 'BUSINESS', 'WARN', 365, true),
('FILE_MODIFY', '檔案修改', '修改檔案內容或屬性', 'BUSINESS', 'INFO', 365, true),

-- 系統操作
('SYSTEM_CONFIG', '系統配置', '變更系統配置', 'SYSTEM', 'WARN', 365, true),
('SYSTEM_BACKUP', '系統備份', '執行系統備份', 'SYSTEM', 'INFO', 90, false),
('SYSTEM_RESTORE', '系統還原', '執行系統還原', 'SYSTEM', 'CRITICAL', 2555, true),
('SYSTEM_MAINTENANCE', '系統維護', '執行系統維護操作', 'SYSTEM', 'INFO', 180, false),

-- 安全事件
('SECURITY_BREACH', '安全入侵', '檢測到安全入侵', 'SECURITY', 'CRITICAL', 2555, true),
('SUSPICIOUS_ACTIVITY', '可疑活動', '檢測到可疑活動', 'SECURITY', 'WARN', 365, true),
('SECURITY_SCAN', '安全掃描', '執行安全掃描', 'SECURITY', 'INFO', 90, false),

-- API 操作
('API_CALL', 'API 呼叫', 'API 端點被呼叫', 'TECHNICAL', 'DEBUG', 30, false),
('API_ERROR', 'API 錯誤', 'API 呼叫發生錯誤', 'TECHNICAL', 'ERROR', 90, false),
('API_RATE_LIMIT', 'API 限流', 'API 呼叫觸發限流', 'TECHNICAL', 'WARN', 30, false);

-- 3. 資料變更詳情表（用於複雜的變更追蹤）
DROP TABLE IF EXISTS audit_change_details CASCADE;

CREATE TABLE audit_change_details (
    id BIGSERIAL PRIMARY KEY,
    audit_log_id BIGINT NOT NULL,                             -- 關聯到主日誌記錄
    field_name VARCHAR(256) NOT NULL,                         -- 變更的欄位名稱
    field_type VARCHAR(64),                                   -- 欄位類型
    old_value TEXT,                                           -- 舊值
    new_value TEXT,                                           -- 新值
    change_type VARCHAR(32),                                  -- 變更類型（INSERT, UPDATE, DELETE）
    is_sensitive BOOLEAN DEFAULT false,                       -- 是否為敏感欄位
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (audit_log_id) REFERENCES audit_logs(id) ON DELETE CASCADE
);

CREATE INDEX idx_audit_change_details_log_id ON audit_change_details (audit_log_id);
CREATE INDEX idx_audit_change_details_field ON audit_change_details (field_name);
CREATE INDEX idx_audit_change_details_sensitive ON audit_change_details (is_sensitive);

-- 4. 稽核日誌存取記錄表（元稽核：誰查看了稽核日誌）
DROP TABLE IF EXISTS audit_log_access CASCADE;

CREATE TABLE audit_log_access (
    id BIGSERIAL PRIMARY KEY,
    accessor_user_id VARCHAR(128) NOT NULL,                   -- 存取者用戶ID
    accessor_user_name VARCHAR(256),                          -- 存取者用戶名稱
    accessor_ip INET,                                         -- 存取者IP
    accessed_log_id BIGINT,                                   -- 被存取的日誌ID
    accessed_resource_type VARCHAR(128),                      -- 被存取的資源類型
    accessed_resource_id VARCHAR(256),                        -- 被存取的資源ID
    access_type VARCHAR(64),                                  -- 存取類型（VIEW, EXPORT, SEARCH）
    search_criteria JSONB,                                    -- 搜尋條件
    records_returned INTEGER,                                 -- 返回的記錄數
    purpose TEXT,                                             -- 存取目的
    approval_required BOOLEAN DEFAULT false,                  -- 是否需要審批
    approved_by VARCHAR(128),                                 -- 審批者
    approved_at TIMESTAMP WITH TIME ZONE,                     -- 審批時間
    accessed_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    FOREIGN KEY (accessed_log_id) REFERENCES audit_logs(id) ON DELETE SET NULL
);

CREATE INDEX idx_audit_log_access_user ON audit_log_access (accessor_user_id, accessed_at DESC);
CREATE INDEX idx_audit_log_access_resource ON audit_log_access (accessed_resource_type, accessed_resource_id);
CREATE INDEX idx_audit_log_access_approval ON audit_log_access (approval_required, approved_by);

-- 5. 合規性報告表
DROP TABLE IF EXISTS audit_compliance_reports CASCADE;

CREATE TABLE audit_compliance_reports (
    id BIGSERIAL PRIMARY KEY,
    report_id UUID NOT NULL DEFAULT gen_random_uuid(),
    report_type VARCHAR(128) NOT NULL,                        -- 報告類型（GDPR, HIPAA, SOX等）
    report_name VARCHAR(512) NOT NULL,                        -- 報告名稱
    date_from TIMESTAMP WITH TIME ZONE NOT NULL,              -- 報告起始日期
    date_to TIMESTAMP WITH TIME ZONE NOT NULL,                -- 報告結束日期
    generated_by VARCHAR(128) NOT NULL,                       -- 生成者
    generated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(64) DEFAULT 'PENDING',                     -- 狀態（PENDING, GENERATING, COMPLETED, FAILED）
    total_events BIGINT,                                      -- 總事件數
    critical_events BIGINT,                                   -- 重要事件數
    security_events BIGINT,                                   -- 安全事件數
    compliance_violations BIGINT,                             -- 合規違規數
    file_path TEXT,                                           -- 報告檔案路徑
    file_size_bytes BIGINT,                                   -- 檔案大小
    checksum VARCHAR(128),                                    -- 檔案校驗和
    retention_until TIMESTAMP WITH TIME ZONE,                 -- 保留至
    additional_metadata JSONB
);

CREATE INDEX idx_audit_compliance_reports_type ON audit_compliance_reports (report_type);
CREATE INDEX idx_audit_compliance_reports_date ON audit_compliance_reports (date_from, date_to);
CREATE INDEX idx_audit_compliance_reports_generated ON audit_compliance_reports (generated_by, generated_at DESC);

-- 6. 建立觸發器函數用於自動化處理
CREATE OR REPLACE FUNCTION update_audit_log_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.created_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 7. 建立檢視以簡化常見查詢
CREATE OR REPLACE VIEW audit_logs_summary AS
SELECT 
    date_trunc('day', occurred_at) as log_date,
    event_type,
    action,
    resource_type,
    COUNT(*) as event_count,
    COUNT(CASE WHEN success = false THEN 1 END) as failed_count,
    COUNT(CASE WHEN is_suspicious = true THEN 1 END) as suspicious_count,
    AVG(response_time_ms) as avg_response_time,
    COUNT(DISTINCT user_id) as unique_users
FROM audit_logs
WHERE occurred_at >= CURRENT_DATE - interval '30 days'
GROUP BY date_trunc('day', occurred_at), event_type, action, resource_type
ORDER BY log_date DESC, event_count DESC;

-- 8. 建立安全性檢視（過濾敏感資料）
CREATE OR REPLACE VIEW audit_logs_public AS
SELECT 
    id,
    event_id,
    batch_id,
    user_id,
    user_name,
    user_role,
    event_type,
    action,
    resource_type,
    resource_id,
    resource_name,
    -- 敏感資料遮罩
    CASE 
        WHEN security_level IN ('HIGH', 'CRITICAL') THEN '***MASKED***'
        ELSE changes_summary
    END as changes_summary,
    success,
    error_code,
    response_time_ms,
    security_level,
    is_suspicious,
    occurred_at,
    tags
FROM audit_logs;

-- 9. 建立清理函數
CREATE OR REPLACE FUNCTION cleanup_old_audit_logs()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER := 0;
    retention_date TIMESTAMP WITH TIME ZONE;
BEGIN
    -- 根據事件類型的保留策略刪除過期日誌
    FOR retention_date IN
        SELECT DISTINCT 
            CURRENT_TIMESTAMP - (retention_days || ' days')::INTERVAL as cutoff_date
        FROM audit_event_types 
        WHERE is_active = true
    LOOP
        DELETE FROM audit_logs 
        WHERE occurred_at < retention_date
        AND event_type IN (
            SELECT code FROM audit_event_types 
            WHERE CURRENT_TIMESTAMP - (retention_days || ' days')::INTERVAL = retention_date
        );
        
        GET DIAGNOSTICS deleted_count = ROW_COUNT;
    END LOOP;
    
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- 10. 插入測試資料以驗證表結構
INSERT INTO audit_logs (
    user_id, user_name, user_role, event_type, action, 
    resource_type, resource_id, resource_name, success, 
    ip_address, user_agent, changes_summary
) VALUES (
    'test_user', 'Test User', 'admin', 'DATA_CREATE', 'CREATE',
    'Person', '12345', 'Test Person', true,
    '127.0.0.1', 'Test Browser', 'Created test audit log entry'
);

-- 添加表註釋
COMMENT ON TABLE audit_logs IS '主要稽核日誌表，記錄所有系統操作和事件';
COMMENT ON TABLE audit_event_types IS '事件類型定義表，標準化事件分類';
COMMENT ON TABLE audit_change_details IS '詳細變更記錄表，追蹤欄位級變更';
COMMENT ON TABLE audit_log_access IS '稽核日誌存取記錄表，實現元稽核功能';
COMMENT ON TABLE audit_compliance_reports IS '合規性報告表，支援法規遵循';