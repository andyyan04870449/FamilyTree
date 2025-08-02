--
-- PostgreSQL database dump
--

-- Dumped from database version 15.13 (Homebrew)
-- Dumped by pg_dump version 15.13 (Homebrew)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: yangandy
--

-- *not* creating schema, since initdb creates it


ALTER SCHEMA public OWNER TO yangandy;

--
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: yangandy
--

COMMENT ON SCHEMA public IS '';


--
-- Name: check_data_access(character varying, character varying, character varying, character varying); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.check_data_access(p_user_id character varying, p_resource_type character varying, p_resource_id character varying, p_owner_user_id character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    user_role VARCHAR(20);
BEGIN
    -- 檢查使用者是否存在且啟用
    SELECT role INTO user_role 
    FROM users 
    WHERE id = p_user_id AND status = 'active';
    
    -- 使用者不存在或未啟用
    IF user_role IS NULL THEN
        RETURN FALSE;
    END IF;
    
    -- 系統管理員可以存取所有資料
    IF user_role = 'admin' THEN
        RETURN TRUE;
    END IF;
    
    -- 一般使用者只能存取自己的資料
    RETURN p_user_id = p_owner_user_id;
END;
$$;


ALTER FUNCTION public.check_data_access(p_user_id character varying, p_resource_type character varying, p_resource_id character varying, p_owner_user_id character varying) OWNER TO "user";

--
-- Name: FUNCTION check_data_access(p_user_id character varying, p_resource_type character varying, p_resource_id character varying, p_owner_user_id character varying); Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON FUNCTION public.check_data_access(p_user_id character varying, p_resource_type character varying, p_resource_id character varying, p_owner_user_id character varying) IS '檢查使用者是否可以存取特定資料';


--
-- Name: check_user_permission(character varying, character varying); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.check_user_permission(p_user_id character varying, p_action character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    user_role VARCHAR(20);
    user_status VARCHAR(20);
BEGIN
    -- 取得使用者角色和狀態
    SELECT role, status INTO user_role, user_status
    FROM users 
    WHERE id = p_user_id;
    
    -- 使用者不存在或未啟用
    IF user_status IS NULL OR user_status != 'active' THEN
        RETURN FALSE;
    END IF;
    
    -- 管理員可以執行所有操作
    IF user_role = 'admin' THEN
        RETURN TRUE;
    END IF;
    
    -- 一般使用者的權限檢查
    CASE p_action
        -- 允許的操作
        WHEN 'view_own_data', 'create_person', 'update_person', 'delete_person',
             'create_analysis', 'view_analysis', 'export_data' THEN
            RETURN TRUE;
        -- 不允許的操作
        WHEN 'manage_users', 'view_all_data', 'system_admin' THEN
            RETURN FALSE;
        ELSE
            -- 預設不允許
            RETURN FALSE;
    END CASE;
END;
$$;


ALTER FUNCTION public.check_user_permission(p_user_id character varying, p_action character varying) OWNER TO "user";

--
-- Name: FUNCTION check_user_permission(p_user_id character varying, p_action character varying); Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON FUNCTION public.check_user_permission(p_user_id character varying, p_action character varying) IS '檢查使用者是否有權限執行特定操作';


--
-- Name: cleanup_deleted_project_photos(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.cleanup_deleted_project_photos() RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    -- 軟刪除關聯到已刪除專案的照片
    UPDATE photos 
    SET deleted_at = NOW() 
    WHERE deleted_at IS NULL 
    AND project_id IN (
        SELECT id FROM projects WHERE status = 'deleted'
    );
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$;


ALTER FUNCTION public.cleanup_deleted_project_photos() OWNER TO "user";

--
-- Name: cleanup_expired_tokens(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.cleanup_expired_tokens() RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM user_tokens 
    WHERE expires_at < CURRENT_TIMESTAMP;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    -- 記錄清理結果
    IF deleted_count > 0 THEN
        PERFORM log_activity(
            'system',
            'cleanup_tokens',
            'system',
            NULL,
            format('Deleted %s expired tokens', deleted_count)
        );
    END IF;
    
    RETURN deleted_count;
END;
$$;


ALTER FUNCTION public.cleanup_expired_tokens() OWNER TO "user";

--
-- Name: FUNCTION cleanup_expired_tokens(); Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON FUNCTION public.cleanup_expired_tokens() IS '清理過期的 Token';


--
-- Name: cleanup_old_audit_logs(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.cleanup_old_audit_logs() RETURNS integer
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public.cleanup_old_audit_logs() OWNER TO "user";

--
-- Name: create_monthly_partition(date); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.create_monthly_partition(target_date date) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    partition_name TEXT;
    start_date DATE;
    end_date DATE;
BEGIN
    start_date := date_trunc('month', target_date);
    end_date := start_date + interval '1 month';
    partition_name := 'audit_logs_' || to_char(start_date, 'YYYY_MM');
    
    EXECUTE format('CREATE TABLE IF NOT EXISTS %I PARTITION OF audit_logs
                    FOR VALUES FROM (%L) TO (%L)',
                   partition_name, start_date, end_date);
END;
$$;


ALTER FUNCTION public.create_monthly_partition(target_date date) OWNER TO "user";

--
-- Name: get_audit_slow_queries(integer); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.get_audit_slow_queries(min_duration_ms integer DEFAULT 1000) RETURNS TABLE(query_text text, calls bigint, total_time double precision, mean_time double precision, max_time double precision)
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public.get_audit_slow_queries(min_duration_ms integer) OWNER TO "user";

--
-- Name: get_file_statistics(character varying); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.get_file_statistics(p_user_id character varying DEFAULT NULL::character varying) RETURNS json
    LANGUAGE plpgsql
    AS $$
DECLARE
    result JSON;
BEGIN
    SELECT json_build_object(
        'total_files', COUNT(*),
        'total_size_mb', ROUND(SUM(file_size)::NUMERIC / 1024 / 1024, 2),
        'by_type', json_object_agg(
            COALESCE(file_type, 'unknown'), 
            type_count
        ),
        'by_status', json_object_agg(
            upload_status,
            status_count
        )
    ) INTO result
    FROM (
        SELECT 
            file_type,
            upload_status,
            COUNT(*) OVER (PARTITION BY file_type) as type_count,
            COUNT(*) OVER (PARTITION BY upload_status) as status_count,
            file_size
        FROM file_uploads
        WHERE p_user_id IS NULL OR user_id = p_user_id
    ) stats;
    
    RETURN result;
END;
$$;


ALTER FUNCTION public.get_file_statistics(p_user_id character varying) OWNER TO "user";

--
-- Name: get_user_data_summary(character varying); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.get_user_data_summary(p_user_id character varying) RETURNS TABLE(data_type character varying, count bigint)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 'persons'::VARCHAR(50), COUNT(*)::BIGINT FROM person_profile WHERE user_id = p_user_id
    UNION ALL
    SELECT 'favorites'::VARCHAR(50), COUNT(*)::BIGINT FROM favorites WHERE user_id = p_user_id
    UNION ALL
    SELECT 'analyses'::VARCHAR(50), COUNT(*)::BIGINT FROM analysis_sessions WHERE user_id = p_user_id
    UNION ALL
    SELECT 'field_mappings'::VARCHAR(50), COUNT(*)::BIGINT FROM field_mapping WHERE user_id = p_user_id;
END;
$$;


ALTER FUNCTION public.get_user_data_summary(p_user_id character varying) OWNER TO "user";

--
-- Name: FUNCTION get_user_data_summary(p_user_id character varying); Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON FUNCTION public.get_user_data_summary(p_user_id character varying) IS '取得使用者資料摘要統計';


--
-- Name: get_user_files(character varying, character varying, character varying, integer, integer); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.get_user_files(p_user_id character varying, p_file_type character varying DEFAULT NULL::character varying, p_status character varying DEFAULT NULL::character varying, p_limit integer DEFAULT 50, p_offset integer DEFAULT 0) RETURNS TABLE(file_id uuid, filename character varying, original_filename character varying, file_size bigint, upload_status character varying, associated_record_type character varying, uploaded_at timestamp without time zone)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        fu.file_id,
        fu.filename,
        fu.original_filename,
        fu.file_size,
        fu.upload_status,
        fu.associated_record_type,
        fu.uploaded_at
    FROM file_uploads fu
    WHERE fu.user_id = p_user_id
        AND (p_file_type IS NULL OR fu.file_type = p_file_type)
        AND (p_status IS NULL OR fu.upload_status = p_status)
    ORDER BY fu.uploaded_at DESC
    LIMIT p_limit OFFSET p_offset;
END;
$$;


ALTER FUNCTION public.get_user_files(p_user_id character varying, p_file_type character varying, p_status character varying, p_limit integer, p_offset integer) OWNER TO "user";

--
-- Name: log_activity(character varying, character varying, character varying, character varying, text, character varying); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.log_activity(p_user_id character varying, p_action character varying, p_resource_type character varying DEFAULT NULL::character varying, p_resource_id character varying DEFAULT NULL::character varying, p_details text DEFAULT NULL::text, p_ip_address character varying DEFAULT NULL::character varying) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO activity_logs (user_id, action, resource_type, resource_id, details, ip_address)
    VALUES (p_user_id, p_action, p_resource_type, p_resource_id, p_details, p_ip_address);
END;
$$;


ALTER FUNCTION public.log_activity(p_user_id character varying, p_action character varying, p_resource_type character varying, p_resource_id character varying, p_details text, p_ip_address character varying) OWNER TO "user";

--
-- Name: FUNCTION log_activity(p_user_id character varying, p_action character varying, p_resource_type character varying, p_resource_id character varying, p_details text, p_ip_address character varying); Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON FUNCTION public.log_activity(p_user_id character varying, p_action character varying, p_resource_type character varying, p_resource_id character varying, p_details text, p_ip_address character varying) IS '記錄使用者活動到日誌表';


--
-- Name: maintain_audit_indexes(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.maintain_audit_indexes() RETURNS text
    LANGUAGE plpgsql
    AS $$
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
$$;


ALTER FUNCTION public.maintain_audit_indexes() OWNER TO "user";

--
-- Name: FUNCTION maintain_audit_indexes(); Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON FUNCTION public.maintain_audit_indexes() IS '稽核索引維護函數，定期執行以確保最佳性能';


--
-- Name: update_audit_log_timestamp(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.update_audit_log_timestamp() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.created_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.update_audit_log_timestamp() OWNER TO "user";

--
-- Name: update_file_uploads_updated_at(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.update_file_uploads_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.update_file_uploads_updated_at() OWNER TO "user";

--
-- Name: update_mergedpersons_updated_at_column(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.update_mergedpersons_updated_at_column() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.update_mergedpersons_updated_at_column() OWNER TO "user";

--
-- Name: update_photos_updated_at(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.update_photos_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.update_photos_updated_at() OWNER TO "user";

--
-- Name: update_updated_at_column(); Type: FUNCTION; Schema: public; Owner: user
--

CREATE FUNCTION public.update_updated_at_column() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.update_updated_at_column() OWNER TO "user";

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: audit_logs; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.audit_logs (
    id bigint NOT NULL,
    event_id uuid DEFAULT gen_random_uuid() NOT NULL,
    batch_id uuid,
    session_id character varying(128),
    user_id character varying(128),
    user_name character varying(256),
    user_role character varying(64),
    impersonator_id character varying(128),
    event_type character varying(64) NOT NULL,
    action character varying(128) NOT NULL,
    resource_type character varying(128),
    resource_id character varying(256),
    resource_name character varying(512),
    old_values jsonb,
    new_values jsonb,
    changes_summary text,
    ip_address inet,
    user_agent text,
    request_method character varying(16),
    request_url text,
    request_id character varying(128),
    success boolean DEFAULT true NOT NULL,
    error_message text,
    error_code character varying(32),
    response_time_ms integer,
    security_level character varying(32) DEFAULT 'NORMAL'::character varying,
    risk_score integer DEFAULT 0,
    is_suspicious boolean DEFAULT false,
    occurred_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    additional_data jsonb,
    tags text[],
    compliance_flags text[]
)
WITH (autovacuum_analyze_scale_factor='0.02', autovacuum_vacuum_scale_factor='0.1');


ALTER TABLE public.audit_logs OWNER TO "user";

--
-- Name: TABLE audit_logs; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.audit_logs IS '主要稽核日誌表，記錄所有系統操作和事件';


--
-- Name: activity_logs; Type: VIEW; Schema: public; Owner: user
--

CREATE VIEW public.activity_logs AS
 SELECT audit_logs.id,
    audit_logs.user_id,
    audit_logs.action,
    audit_logs.resource_type,
    audit_logs.resource_id,
    audit_logs.changes_summary AS details,
    (audit_logs.ip_address)::character varying(45) AS ip_address,
    audit_logs.created_at
   FROM public.audit_logs
  WHERE ((audit_logs.event_type)::text = 'LEGACY_ACTIVITY'::text);


ALTER TABLE public.activity_logs OWNER TO "user";

--
-- Name: VIEW activity_logs; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON VIEW public.activity_logs IS '向後相容視圖，實際資料已遷移至 audit_logs 表';


--
-- Name: activity_logs_backup; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.activity_logs_backup (
    id bigint NOT NULL,
    user_id character varying(50),
    action character varying(100) NOT NULL,
    resource_type character varying(50),
    resource_id character varying(100),
    details text,
    ip_address character varying(45),
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.activity_logs_backup OWNER TO "user";

--
-- Name: TABLE activity_logs_backup; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.activity_logs_backup IS '原始 activity_logs 表備份，資料已遷移至 audit_logs';


--
-- Name: COLUMN activity_logs_backup.user_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.activity_logs_backup.user_id IS '執行操作的使用者 ID';


--
-- Name: COLUMN activity_logs_backup.action; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.activity_logs_backup.action IS '操作名稱（如：login, create_person）';


--
-- Name: COLUMN activity_logs_backup.resource_type; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.activity_logs_backup.resource_type IS '資源類型（如：person, file）';


--
-- Name: COLUMN activity_logs_backup.resource_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.activity_logs_backup.resource_id IS '資源的 ID';


--
-- Name: COLUMN activity_logs_backup.details; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.activity_logs_backup.details IS '操作詳情（可存 JSON 格式）';


--
-- Name: COLUMN activity_logs_backup.ip_address; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.activity_logs_backup.ip_address IS '操作者的 IP 位址';


--
-- Name: activity_logs_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.activity_logs_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.activity_logs_id_seq OWNER TO "user";

--
-- Name: activity_logs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.activity_logs_id_seq OWNED BY public.activity_logs_backup.id;


--
-- Name: analysis_results; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.analysis_results (
    id integer NOT NULL,
    user_id character varying(50),
    person_id integer NOT NULL,
    analysis_result jsonb NOT NULL,
    analysis_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    progress_percentage integer DEFAULT 0,
    status character varying(50) DEFAULT 'pending'::character varying,
    current_step character varying(255),
    status_message text,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.analysis_results OWNER TO "user";

--
-- Name: analysis_results_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.analysis_results_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.analysis_results_id_seq OWNER TO "user";

--
-- Name: analysis_results_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.analysis_results_id_seq OWNED BY public.analysis_results.id;


--
-- Name: analysis_sessions; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.analysis_sessions (
    id character varying(100) NOT NULL,
    user_id character varying(50),
    root_person_id integer NOT NULL,
    max_depth integer DEFAULT 3 NOT NULL,
    status character varying(20) DEFAULT 'processing'::character varying NOT NULL,
    total_relationships integer DEFAULT 0,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    completed_at timestamp without time zone
);


ALTER TABLE public.analysis_sessions OWNER TO "user";

--
-- Name: audit_change_details; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.audit_change_details (
    id bigint NOT NULL,
    audit_log_id bigint NOT NULL,
    field_name character varying(256) NOT NULL,
    field_type character varying(64),
    old_value text,
    new_value text,
    change_type character varying(32),
    is_sensitive boolean DEFAULT false,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
)
WITH (autovacuum_analyze_scale_factor='0.05', autovacuum_vacuum_scale_factor='0.1');


ALTER TABLE public.audit_change_details OWNER TO "user";

--
-- Name: TABLE audit_change_details; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.audit_change_details IS '詳細變更記錄表，追蹤欄位級變更';


--
-- Name: audit_change_details_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.audit_change_details_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.audit_change_details_id_seq OWNER TO "user";

--
-- Name: audit_change_details_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.audit_change_details_id_seq OWNED BY public.audit_change_details.id;


--
-- Name: audit_compliance_reports; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.audit_compliance_reports (
    id bigint NOT NULL,
    report_id uuid DEFAULT gen_random_uuid() NOT NULL,
    report_type character varying(128) NOT NULL,
    report_name character varying(512) NOT NULL,
    date_from timestamp with time zone NOT NULL,
    date_to timestamp with time zone NOT NULL,
    generated_by character varying(128) NOT NULL,
    generated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    status character varying(64) DEFAULT 'PENDING'::character varying,
    total_events bigint,
    critical_events bigint,
    security_events bigint,
    compliance_violations bigint,
    file_path text,
    file_size_bytes bigint,
    checksum character varying(128),
    retention_until timestamp with time zone,
    additional_metadata jsonb
);


ALTER TABLE public.audit_compliance_reports OWNER TO "user";

--
-- Name: TABLE audit_compliance_reports; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.audit_compliance_reports IS '合規性報告表，支援法規遵循';


--
-- Name: audit_compliance_reports_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.audit_compliance_reports_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.audit_compliance_reports_id_seq OWNER TO "user";

--
-- Name: audit_compliance_reports_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.audit_compliance_reports_id_seq OWNED BY public.audit_compliance_reports.id;


--
-- Name: audit_event_types; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.audit_event_types (
    id integer NOT NULL,
    code character varying(64) NOT NULL,
    name character varying(256) NOT NULL,
    description text,
    category character varying(64),
    severity character varying(32) DEFAULT 'INFO'::character varying,
    retention_days integer DEFAULT 90,
    requires_approval boolean DEFAULT false,
    compliance_required boolean DEFAULT false,
    is_active boolean DEFAULT true,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.audit_event_types OWNER TO "user";

--
-- Name: TABLE audit_event_types; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.audit_event_types IS '事件類型定義表，標準化事件分類';


--
-- Name: audit_event_types_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.audit_event_types_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.audit_event_types_id_seq OWNER TO "user";

--
-- Name: audit_event_types_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.audit_event_types_id_seq OWNED BY public.audit_event_types.id;


--
-- Name: audit_log_access; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.audit_log_access (
    id bigint NOT NULL,
    accessor_user_id character varying(128) NOT NULL,
    accessor_user_name character varying(256),
    accessor_ip inet,
    accessed_log_id bigint,
    accessed_resource_type character varying(128),
    accessed_resource_id character varying(256),
    access_type character varying(64),
    search_criteria jsonb,
    records_returned integer,
    purpose text,
    approval_required boolean DEFAULT false,
    approved_by character varying(128),
    approved_at timestamp with time zone,
    accessed_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
)
WITH (autovacuum_analyze_scale_factor='0.05');


ALTER TABLE public.audit_log_access OWNER TO "user";

--
-- Name: TABLE audit_log_access; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.audit_log_access IS '稽核日誌存取記錄表，實現元稽核功能';


--
-- Name: audit_log_access_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.audit_log_access_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.audit_log_access_id_seq OWNER TO "user";

--
-- Name: audit_log_access_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.audit_log_access_id_seq OWNED BY public.audit_log_access.id;


--
-- Name: audit_logs_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.audit_logs_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.audit_logs_id_seq OWNER TO "user";

--
-- Name: audit_logs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.audit_logs_id_seq OWNED BY public.audit_logs.id;


--
-- Name: audit_logs_public; Type: VIEW; Schema: public; Owner: user
--

CREATE VIEW public.audit_logs_public AS
 SELECT audit_logs.id,
    audit_logs.event_id,
    audit_logs.batch_id,
    audit_logs.user_id,
    audit_logs.user_name,
    audit_logs.user_role,
    audit_logs.event_type,
    audit_logs.action,
    audit_logs.resource_type,
    audit_logs.resource_id,
    audit_logs.resource_name,
        CASE
            WHEN ((audit_logs.security_level)::text = ANY ((ARRAY['HIGH'::character varying, 'CRITICAL'::character varying])::text[])) THEN '***MASKED***'::text
            ELSE audit_logs.changes_summary
        END AS changes_summary,
    audit_logs.success,
    audit_logs.error_code,
    audit_logs.response_time_ms,
    audit_logs.security_level,
    audit_logs.is_suspicious,
    audit_logs.occurred_at,
    audit_logs.tags
   FROM public.audit_logs;


ALTER TABLE public.audit_logs_public OWNER TO "user";

--
-- Name: audit_logs_summary; Type: VIEW; Schema: public; Owner: user
--

CREATE VIEW public.audit_logs_summary AS
 SELECT date_trunc('day'::text, audit_logs.occurred_at) AS log_date,
    audit_logs.event_type,
    audit_logs.action,
    audit_logs.resource_type,
    count(*) AS event_count,
    count(
        CASE
            WHEN (audit_logs.success = false) THEN 1
            ELSE NULL::integer
        END) AS failed_count,
    count(
        CASE
            WHEN (audit_logs.is_suspicious = true) THEN 1
            ELSE NULL::integer
        END) AS suspicious_count,
    avg(audit_logs.response_time_ms) AS avg_response_time,
    count(DISTINCT audit_logs.user_id) AS unique_users
   FROM public.audit_logs
  WHERE (audit_logs.occurred_at >= (CURRENT_DATE - '30 days'::interval))
  GROUP BY (date_trunc('day'::text, audit_logs.occurred_at)), audit_logs.event_type, audit_logs.action, audit_logs.resource_type
  ORDER BY (date_trunc('day'::text, audit_logs.occurred_at)) DESC, (count(*)) DESC;


ALTER TABLE public.audit_logs_summary OWNER TO "user";

--
-- Name: data_migration_log; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.data_migration_log (
    id integer NOT NULL,
    migration_name character varying(200) NOT NULL,
    executed_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    executed_by character varying(100),
    status character varying(50),
    records_affected integer,
    notes text
);


ALTER TABLE public.data_migration_log OWNER TO "user";

--
-- Name: data_migration_log_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.data_migration_log_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.data_migration_log_id_seq OWNER TO "user";

--
-- Name: data_migration_log_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.data_migration_log_id_seq OWNED BY public.data_migration_log.id;


--
-- Name: field_mapping; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.field_mapping (
    id integer NOT NULL,
    excel_field_name character varying(100) NOT NULL,
    db_field_name character varying(100) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    user_id character varying(50)
);


ALTER TABLE public.field_mapping OWNER TO "user";

--
-- Name: COLUMN field_mapping.user_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.field_mapping.user_id IS '資料擁有者的使用者 ID';


--
-- Name: field_mapping_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.field_mapping_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.field_mapping_id_seq OWNER TO "user";

--
-- Name: field_mapping_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.field_mapping_id_seq OWNED BY public.field_mapping.id;


--
-- Name: file_uploads; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.file_uploads (
    file_id uuid DEFAULT gen_random_uuid() NOT NULL,
    user_id character varying(50) NOT NULL,
    filename character varying(255) NOT NULL,
    original_filename character varying(255) NOT NULL,
    file_path character varying(500) NOT NULL,
    file_size bigint NOT NULL,
    md5_hash character varying(32) NOT NULL,
    file_type character varying(50),
    mime_type character varying(100),
    associated_record_id character varying(50),
    associated_record_type character varying(50),
    upload_status character varying(50) DEFAULT 'uploaded'::character varying,
    is_processed boolean DEFAULT false,
    processed_at timestamp without time zone,
    uploaded_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_file_uploads_record_type CHECK (((associated_record_type IS NULL) OR ((associated_record_type)::text = ANY ((ARRAY['person'::character varying, 'project'::character varying, 'analysis'::character varying, 'photo'::character varying, 'document'::character varying])::text[])))),
    CONSTRAINT chk_file_uploads_status CHECK (((upload_status)::text = ANY ((ARRAY['uploaded'::character varying, 'processing'::character varying, 'processed'::character varying, 'failed'::character varying, 'deleted'::character varying, 'merged'::character varying])::text[])))
);


ALTER TABLE public.file_uploads OWNER TO "user";

--
-- Name: mergedpersons; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.mergedpersons (
    id integer NOT NULL,
    project_id character varying(255) NOT NULL,
    photo text,
    name character varying(100) NOT NULL,
    discovery_process text,
    gender character varying(10),
    birthday date,
    birthplace character varying(200),
    nationality character varying(50),
    ethnicity character varying(50),
    ancestral_home character varying(200),
    political_party character varying(100),
    id_number character varying(50),
    passport_number character varying(50),
    phone character varying(50),
    mobile character varying(50),
    email character varying(100),
    current_workplace character varying(200),
    current_address text,
    mailing_address text,
    family_relationships text,
    experience text,
    education text,
    online_accounts text,
    publications text,
    activities text,
    important_friends text,
    frequent_places text,
    travel_records text,
    notes text,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    created_by character varying(100),
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_by character varying(100),
    source_person_a_id integer NOT NULL,
    source_person_b_id integer NOT NULL
);


ALTER TABLE public.mergedpersons OWNER TO "user";

--
-- Name: TABLE mergedpersons; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.mergedpersons IS '儲存合併後的人員資料';


--
-- Name: COLUMN mergedpersons.id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.mergedpersons.id IS '合併後人員資料的唯一ID';


--
-- Name: COLUMN mergedpersons.project_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.mergedpersons.project_id IS '此筆資料所屬的專案ID';


--
-- Name: COLUMN mergedpersons.source_person_a_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.mergedpersons.source_person_a_id IS '合併來源人員A的ID (來自 person_profile)';


--
-- Name: COLUMN mergedpersons.source_person_b_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.mergedpersons.source_person_b_id IS '合併來源人員B的ID (來自 person_profile)';


--
-- Name: mergedpersons_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.mergedpersons_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.mergedpersons_id_seq OWNER TO "user";

--
-- Name: mergedpersons_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.mergedpersons_id_seq OWNED BY public.mergedpersons.id;


--
-- Name: missing_persons; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.missing_persons (
    id integer NOT NULL,
    user_id character varying(50),
    name character varying(255) NOT NULL,
    relation_type character varying(100) NOT NULL,
    source_person_id integer NOT NULL,
    source_field character varying(50) NOT NULL,
    analysis_session_id character varying(255) NOT NULL,
    layer_depth integer DEFAULT 1 NOT NULL,
    discovered_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    status character varying(50) DEFAULT 'pending'::character varying,
    resolved_person_id integer,
    notes text
);


ALTER TABLE public.missing_persons OWNER TO "user";

--
-- Name: missing_persons_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.missing_persons_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.missing_persons_id_seq OWNER TO "user";

--
-- Name: missing_persons_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.missing_persons_id_seq OWNED BY public.missing_persons.id;


--
-- Name: permissions; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.permissions (
    id integer NOT NULL,
    permission character varying(100) NOT NULL,
    display_name character varying(200) NOT NULL,
    description text,
    category character varying(50) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.permissions OWNER TO "user";

--
-- Name: permissions_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.permissions_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.permissions_id_seq OWNER TO "user";

--
-- Name: permissions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.permissions_id_seq OWNED BY public.permissions.id;


--
-- Name: person_profile; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.person_profile (
    id integer NOT NULL,
    photo_index text,
    name text,
    discovery_source text,
    gender text,
    birthday text,
    birthplace text,
    nationality text,
    ethnicity text,
    ancestral_origin text,
    political_party text,
    id_number text,
    passport_number text,
    phone text,
    mobile text,
    email text,
    current_employer text,
    address text,
    mailing_address text,
    family_relationships text,
    experience text,
    education text,
    online_accounts text,
    publications text,
    activities text,
    friends text,
    frequent_locations text,
    travel_history text,
    remarks text,
    created_at text DEFAULT CURRENT_TIMESTAMP,
    created_by text,
    updated_at text DEFAULT CURRENT_TIMESTAMP,
    updated_by text,
    extra_data jsonb,
    source_id integer,
    source_table character varying(50),
    source_created_at timestamp without time zone,
    source_updated_at timestamp without time zone,
    file_md5 character varying(32),
    source_file_id integer,
    source_file_name character varying(200),
    discovery_process text,
    important_friends text,
    project_id character varying(25),
    user_id character varying(50) NOT NULL
);


ALTER TABLE public.person_profile OWNER TO "user";

--
-- Name: COLUMN person_profile.source_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.person_profile.source_id IS '來源資料的ID';


--
-- Name: COLUMN person_profile.source_table; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.person_profile.source_table IS '來源資料表名稱';


--
-- Name: COLUMN person_profile.source_created_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.person_profile.source_created_at IS '來源資料的創建時間';


--
-- Name: COLUMN person_profile.source_updated_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.person_profile.source_updated_at IS '來源資料的最後更新時間';


--
-- Name: COLUMN person_profile.user_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.person_profile.user_id IS '資料擁有者的使用者 ID';


--
-- Name: person_profile_backup_20250802; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.person_profile_backup_20250802 (
    id integer,
    photo_index text,
    name text,
    discovery_source text,
    gender text,
    birthday text,
    birthplace text,
    nationality text,
    ethnicity text,
    ancestral_origin text,
    political_party text,
    id_number text,
    passport_number text,
    phone text,
    mobile text,
    email text,
    current_employer text,
    address text,
    mailing_address text,
    family_relationships text,
    experience text,
    education text,
    online_accounts text,
    publications text,
    activities text,
    friends text,
    frequent_locations text,
    travel_history text,
    remarks text,
    created_at text,
    created_by text,
    updated_at text,
    updated_by text,
    extra_data jsonb,
    source_id integer,
    source_table character varying(50),
    source_created_at timestamp without time zone,
    source_updated_at timestamp without time zone,
    file_md5 character varying(32),
    source_file_id integer,
    source_file_name character varying(200),
    discovery_process text,
    important_friends text,
    project_id character varying(25),
    user_id character varying(50)
);


ALTER TABLE public.person_profile_backup_20250802 OWNER TO "user";

--
-- Name: person_profile_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.person_profile_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.person_profile_id_seq OWNER TO "user";

--
-- Name: person_profile_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.person_profile_id_seq OWNED BY public.person_profile.id;


--
-- Name: personmergelog; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.personmergelog (
    id integer NOT NULL,
    merged_person_id integer NOT NULL,
    source_person_a_id integer NOT NULL,
    source_person_b_id integer NOT NULL,
    merged_by character varying(100),
    merged_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    project_id character varying(255) NOT NULL
);


ALTER TABLE public.personmergelog OWNER TO "user";

--
-- Name: TABLE personmergelog; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.personmergelog IS '記錄人員資料合併操作的日誌';


--
-- Name: COLUMN personmergelog.id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.personmergelog.id IS '日誌記錄的唯一ID';


--
-- Name: COLUMN personmergelog.merged_person_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.personmergelog.merged_person_id IS '合併後新產生的 MergedPersons 資料ID';


--
-- Name: COLUMN personmergelog.source_person_a_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.personmergelog.source_person_a_id IS '被合併的來源人員A的ID';


--
-- Name: COLUMN personmergelog.source_person_b_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.personmergelog.source_person_b_id IS '被合併的來源人員B的ID';


--
-- Name: COLUMN personmergelog.merged_by; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.personmergelog.merged_by IS '執行合併操作的使用者';


--
-- Name: COLUMN personmergelog.merged_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.personmergelog.merged_at IS '合併操作發生的時間';


--
-- Name: COLUMN personmergelog.project_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.personmergelog.project_id IS '操作所屬的專案ID';


--
-- Name: personmergelog_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.personmergelog_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.personmergelog_id_seq OWNER TO "user";

--
-- Name: personmergelog_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.personmergelog_id_seq OWNED BY public.personmergelog.id;


--
-- Name: photos; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.photos (
    id integer NOT NULL,
    original_filename character varying(255) NOT NULL,
    saved_filename character varying(255) NOT NULL,
    file_path text NOT NULL,
    file_size bigint NOT NULL,
    md5_hash character varying(32) NOT NULL,
    project_id character varying(50) NOT NULL,
    upload_time timestamp with time zone DEFAULT now(),
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now(),
    deleted_at timestamp without time zone
);


ALTER TABLE public.photos OWNER TO "user";

--
-- Name: photos_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.photos_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.photos_id_seq OWNER TO "user";

--
-- Name: photos_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.photos_id_seq OWNED BY public.photos.id;


--
-- Name: project_permissions; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.project_permissions (
    id integer NOT NULL,
    user_id character varying(50) NOT NULL,
    project_id character varying(50) NOT NULL,
    role character varying(50) NOT NULL,
    granted_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    granted_by character varying(50)
);


ALTER TABLE public.project_permissions OWNER TO "user";

--
-- Name: project_permissions_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.project_permissions_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.project_permissions_id_seq OWNER TO "user";

--
-- Name: project_permissions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.project_permissions_id_seq OWNED BY public.project_permissions.id;


--
-- Name: projects; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.projects (
    id character varying(25) NOT NULL,
    user_id character varying(50) NOT NULL,
    project_name character varying(200) NOT NULL,
    project_description text,
    status character varying(20) DEFAULT 'active'::character varying,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    completed_at timestamp without time zone,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT projects_status_check CHECK (((status)::text = ANY (ARRAY[('active'::character varying)::text, ('completed'::character varying)::text, ('archived'::character varying)::text, ('draft'::character varying)::text, ('deleted'::character varying)::text])))
);


ALTER TABLE public.projects OWNER TO "user";

--
-- Name: relationship_layers; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.relationship_layers (
    id integer NOT NULL,
    source_person_id integer NOT NULL,
    target_person_id integer NOT NULL,
    relation_type character varying(100) NOT NULL,
    source_field character varying(50) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    project_id character varying(25),
    visual_analysis_graph_id integer,
    user_id character varying(50) NOT NULL
);


ALTER TABLE public.relationship_layers OWNER TO "user";

--
-- Name: TABLE relationship_layers; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.relationship_layers IS '儲存遞迴分析的層級關係資料';


--
-- Name: COLUMN relationship_layers.visual_analysis_graph_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.relationship_layers.visual_analysis_graph_id IS '視覺化分析圖表ID，關聯到 visual_analysis_graphs.id';


--
-- Name: COLUMN relationship_layers.user_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.relationship_layers.user_id IS '資料擁有者的使用者 ID';


--
-- Name: relationship_layers_backup; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.relationship_layers_backup (
    id integer,
    source_person_id integer,
    target_person_id integer,
    relation_type character varying(100),
    source_field character varying(50),
    layer_depth integer,
    analysis_session_id character varying(100),
    created_at timestamp without time zone,
    updated_at timestamp without time zone,
    project_id character varying(25),
    visual_analysis_graph_id integer
);


ALTER TABLE public.relationship_layers_backup OWNER TO "user";

--
-- Name: relationship_layers_backup_20250802; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.relationship_layers_backup_20250802 (
    id integer,
    source_person_id integer,
    target_person_id integer,
    relation_type character varying(100),
    source_field character varying(50),
    created_at timestamp without time zone,
    updated_at timestamp without time zone,
    project_id character varying(25),
    visual_analysis_graph_id integer,
    user_id character varying(50)
);


ALTER TABLE public.relationship_layers_backup_20250802 OWNER TO "user";

--
-- Name: relationship_layers_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.relationship_layers_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.relationship_layers_id_seq OWNER TO "user";

--
-- Name: relationship_layers_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.relationship_layers_id_seq OWNED BY public.relationship_layers.id;


--
-- Name: role_permissions; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.role_permissions (
    role_id character varying(50) NOT NULL,
    permission_id integer NOT NULL,
    granted_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    granted_by character varying(50)
);


ALTER TABLE public.role_permissions OWNER TO "user";

--
-- Name: roles; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.roles (
    id character varying(50) NOT NULL,
    display_name character varying(100) NOT NULL,
    description text,
    level integer DEFAULT 0 NOT NULL,
    is_system boolean DEFAULT false NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.roles OWNER TO "user";

--
-- Name: search_keywords; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.search_keywords (
    id integer NOT NULL,
    keyword character varying(255) NOT NULL,
    search_count integer DEFAULT 1,
    search_type character varying(20) DEFAULT 'fuzzy'::character varying,
    last_search_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    project_id character varying(25)
);


ALTER TABLE public.search_keywords OWNER TO "user";

--
-- Name: TABLE search_keywords; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.search_keywords IS '搜索關鍵字記錄表：記錄用戶搜索的關鍵字和使用頻率';


--
-- Name: COLUMN search_keywords.keyword; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_keywords.keyword IS '搜索關鍵字';


--
-- Name: COLUMN search_keywords.search_count; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_keywords.search_count IS '搜索次數';


--
-- Name: COLUMN search_keywords.search_type; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_keywords.search_type IS '搜索類型：exact(精準) 或 fuzzy(模糊)';


--
-- Name: COLUMN search_keywords.last_search_time; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_keywords.last_search_time IS '最後搜索時間';


--
-- Name: COLUMN search_keywords.created_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_keywords.created_at IS '建立時間';


--
-- Name: COLUMN search_keywords.updated_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_keywords.updated_at IS '更新時間';


--
-- Name: search_keywords_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.search_keywords_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.search_keywords_id_seq OWNER TO "user";

--
-- Name: search_keywords_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.search_keywords_id_seq OWNED BY public.search_keywords.id;


--
-- Name: search_logs; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.search_logs (
    id integer NOT NULL,
    keyword character varying(255) NOT NULL,
    search_type character varying(20) NOT NULL,
    result_count integer DEFAULT 0,
    search_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    ip_address character varying(45),
    user_agent text,
    project_id character varying(25)
);


ALTER TABLE public.search_logs OWNER TO "user";

--
-- Name: TABLE search_logs; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.search_logs IS '搜索結果日誌表：記錄詳細的搜索行為，用於統計和分析';


--
-- Name: COLUMN search_logs.keyword; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_logs.keyword IS '搜索關鍵字';


--
-- Name: COLUMN search_logs.search_type; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_logs.search_type IS '搜索類型：exact 或 fuzzy';


--
-- Name: COLUMN search_logs.result_count; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_logs.result_count IS '搜索結果數量';


--
-- Name: COLUMN search_logs.search_time; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_logs.search_time IS '搜索時間';


--
-- Name: COLUMN search_logs.ip_address; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_logs.ip_address IS '搜索者IP地址';


--
-- Name: COLUMN search_logs.user_agent; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.search_logs.user_agent IS '用戶代理字符串';


--
-- Name: search_logs_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.search_logs_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.search_logs_id_seq OWNER TO "user";

--
-- Name: search_logs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.search_logs_id_seq OWNED BY public.search_logs.id;


--
-- Name: sync_error_log; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.sync_error_log (
    id integer NOT NULL,
    source_id integer NOT NULL,
    source_table character varying(50) NOT NULL,
    error_message text NOT NULL,
    error_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.sync_error_log OWNER TO "user";

--
-- Name: TABLE sync_error_log; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.sync_error_log IS '資料同步錯誤日誌表，記錄同步過程中的錯誤';


--
-- Name: sync_error_log_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.sync_error_log_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.sync_error_log_id_seq OWNER TO "user";

--
-- Name: sync_error_log_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.sync_error_log_id_seq OWNED BY public.sync_error_log.id;


--
-- Name: sync_log; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.sync_log (
    id integer NOT NULL,
    source_id integer NOT NULL,
    source_table character varying(50) NOT NULL,
    status character varying(20) NOT NULL,
    sync_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.sync_log OWNER TO "user";

--
-- Name: TABLE sync_log; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.sync_log IS '資料同步日誌表，記錄所有同步操作';


--
-- Name: sync_log_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.sync_log_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.sync_log_id_seq OWNER TO "user";

--
-- Name: sync_log_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.sync_log_id_seq OWNED BY public.sync_log.id;


--
-- Name: sync_status; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.sync_status (
    id integer NOT NULL,
    last_sync_time timestamp without time zone NOT NULL,
    status character varying(20) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


ALTER TABLE public.sync_status OWNER TO "user";

--
-- Name: TABLE sync_status; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.sync_status IS '資料同步狀態表，記錄最後同步時間和狀態';


--
-- Name: sync_status_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.sync_status_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.sync_status_id_seq OWNER TO "user";

--
-- Name: sync_status_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.sync_status_id_seq OWNED BY public.sync_status.id;


--
-- Name: user_favorites; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.user_favorites (
    id integer NOT NULL,
    person_id integer NOT NULL,
    person_name character varying(100) NOT NULL,
    last_viewed_time timestamp without time zone,
    favorited_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    project_id character varying(25),
    user_id character varying(50) NOT NULL
);


ALTER TABLE public.user_favorites OWNER TO "user";

--
-- Name: TABLE user_favorites; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.user_favorites IS '用戶收藏表：記錄用戶收藏的人員資料';


--
-- Name: COLUMN user_favorites.person_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_favorites.person_id IS '人員ID，關聯person_profile.id';


--
-- Name: COLUMN user_favorites.person_name; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_favorites.person_name IS '人員姓名（冗餘字段，提高查詢效能）';


--
-- Name: COLUMN user_favorites.last_viewed_time; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_favorites.last_viewed_time IS '最後查看時間';


--
-- Name: COLUMN user_favorites.favorited_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_favorites.favorited_at IS '收藏時間';


--
-- Name: COLUMN user_favorites.created_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_favorites.created_at IS '建立時間';


--
-- Name: COLUMN user_favorites.updated_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_favorites.updated_at IS '更新時間';


--
-- Name: user_favorites_backup_20250802; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.user_favorites_backup_20250802 (
    id integer,
    person_id integer,
    person_name character varying(100),
    last_viewed_time timestamp without time zone,
    favorited_at timestamp without time zone,
    created_at timestamp without time zone,
    updated_at timestamp without time zone,
    project_id character varying(25),
    user_id character varying(50)
);


ALTER TABLE public.user_favorites_backup_20250802 OWNER TO "user";

--
-- Name: user_favorites_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.user_favorites_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.user_favorites_id_seq OWNER TO "user";

--
-- Name: user_favorites_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.user_favorites_id_seq OWNED BY public.user_favorites.id;


--
-- Name: user_permissions; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.user_permissions (
    user_id character varying(50) NOT NULL,
    permission_id integer NOT NULL,
    granted_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    granted_by character varying(50),
    expires_at timestamp without time zone
);


ALTER TABLE public.user_permissions OWNER TO "user";

--
-- Name: user_roles; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.user_roles (
    user_id character varying(50) NOT NULL,
    role_id character varying(50) NOT NULL,
    assigned_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    assigned_by character varying(50)
);


ALTER TABLE public.user_roles OWNER TO "user";

--
-- Name: user_tokens; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.user_tokens (
    id character varying(100) DEFAULT (gen_random_uuid())::text NOT NULL,
    user_id character varying(50) NOT NULL,
    token_type character varying(20) NOT NULL,
    token_hash character varying(255) NOT NULL,
    expires_at timestamp without time zone NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    used_at timestamp without time zone,
    CONSTRAINT user_tokens_token_type_check CHECK (((token_type)::text = 'refresh'::text))
);


ALTER TABLE public.user_tokens OWNER TO "user";

--
-- Name: TABLE user_tokens; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.user_tokens IS 'JWT Token 管理表';


--
-- Name: COLUMN user_tokens.token_type; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_tokens.token_type IS 'Token 類型，目前只有 refresh';


--
-- Name: COLUMN user_tokens.token_hash; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_tokens.token_hash IS 'Token 的雜湊值（不存明文）';


--
-- Name: COLUMN user_tokens.expires_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_tokens.expires_at IS 'Token 過期時間';


--
-- Name: COLUMN user_tokens.used_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.user_tokens.used_at IS '最後使用時間';


--
-- Name: user_update_file_archived_20250802; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.user_update_file_archived_20250802 (
    id integer NOT NULL,
    filename character varying(255) NOT NULL,
    original_filename character varying(255) NOT NULL,
    file_path character varying(500) NOT NULL,
    file_size bigint NOT NULL,
    md5_hash character varying(32) NOT NULL,
    upload_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    is_merged boolean DEFAULT false,
    merge_time timestamp without time zone,
    status character varying(50) DEFAULT 'uploaded'::character varying,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    project_id character varying(25)
);


ALTER TABLE public.user_update_file_archived_20250802 OWNER TO "user";

--
-- Name: TABLE user_update_file_archived_20250802; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.user_update_file_archived_20250802 IS 'DEPRECATED: 此表已被 file_uploads 取代。請使用新的檔案管理 API (/api/file)。此表將在 v2.1 移除，目前保留僅供相容性使用。遷移日期: 2025-08-02';


--
-- Name: user_update_file_backup_20250802; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.user_update_file_backup_20250802 (
    id integer,
    filename character varying(255),
    original_filename character varying(255),
    file_path character varying(500),
    file_size bigint,
    md5_hash character varying(32),
    upload_time timestamp without time zone,
    is_merged boolean,
    merge_time timestamp without time zone,
    status character varying(50),
    created_at timestamp without time zone,
    updated_at timestamp without time zone,
    project_id character varying(25)
);


ALTER TABLE public.user_update_file_backup_20250802 OWNER TO "user";

--
-- Name: user_update_file_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.user_update_file_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.user_update_file_id_seq OWNER TO "user";

--
-- Name: user_update_file_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.user_update_file_id_seq OWNED BY public.user_update_file_archived_20250802.id;


--
-- Name: users; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.users (
    id character varying(50) DEFAULT (gen_random_uuid())::text NOT NULL,
    username character varying(100) NOT NULL,
    email character varying(255) NOT NULL,
    password_hash character varying(255) NOT NULL,
    full_name character varying(200),
    role character varying(20) DEFAULT 'user'::character varying,
    status character varying(20) DEFAULT 'active'::character varying,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    last_login_at timestamp without time zone,
    CONSTRAINT users_role_check CHECK (((role)::text = ANY ((ARRAY['admin'::character varying, 'user'::character varying])::text[]))),
    CONSTRAINT users_status_check CHECK (((status)::text = ANY ((ARRAY['active'::character varying, 'inactive'::character varying])::text[])))
);


ALTER TABLE public.users OWNER TO "user";

--
-- Name: TABLE users; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.users IS '使用者帳號資料表';


--
-- Name: COLUMN users.id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.users.id IS '使用者唯一識別碼 (UUID)';


--
-- Name: COLUMN users.username; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.users.username IS '登入帳號名稱';


--
-- Name: COLUMN users.email; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.users.email IS '電子郵件地址';


--
-- Name: COLUMN users.password_hash; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.users.password_hash IS 'BCrypt 加密的密碼雜湊值';


--
-- Name: COLUMN users.full_name; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.users.full_name IS '使用者真實姓名';


--
-- Name: COLUMN users.role; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.users.role IS '系統角色：admin(管理員) 或 user(一般使用者)';


--
-- Name: COLUMN users.status; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.users.status IS '帳號狀態：active(啟用) 或 inactive(停用)';


--
-- Name: COLUMN users.last_login_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.users.last_login_at IS '最後登入時間';


--
-- Name: v_user_activity_summary; Type: VIEW; Schema: public; Owner: user
--

CREATE VIEW public.v_user_activity_summary AS
 SELECT u.id AS user_id,
    u.username,
    al.action,
    al.resource_type,
    al.resource_id,
    al.created_at,
    al.ip_address
   FROM (public.users u
     JOIN public.activity_logs_backup al ON (((u.id)::text = (al.user_id)::text)))
  ORDER BY al.created_at DESC;


ALTER TABLE public.v_user_activity_summary OWNER TO "user";

--
-- Name: VIEW v_user_activity_summary; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON VIEW public.v_user_activity_summary IS '使用者活動摘要檢視';


--
-- Name: v_user_statistics; Type: VIEW; Schema: public; Owner: user
--

CREATE VIEW public.v_user_statistics AS
 SELECT u.id AS user_id,
    u.username,
    u.email,
    u.full_name,
    u.role,
    u.status,
    u.created_at,
    u.last_login_at,
    COALESCE(( SELECT count(*) AS count
           FROM public.person_profile
          WHERE ((person_profile.user_id)::text = (u.id)::text)), (0)::bigint) AS person_count,
    COALESCE(( SELECT count(*) AS count
           FROM public.user_favorites
          WHERE ((user_favorites.user_id)::text = (u.id)::text)), (0)::bigint) AS favorite_count,
    COALESCE(( SELECT count(*) AS count
           FROM public.analysis_sessions
          WHERE ((analysis_sessions.user_id)::text = (u.id)::text)), (0)::bigint) AS analysis_count,
    COALESCE(( SELECT count(*) AS count
           FROM public.activity_logs_backup
          WHERE (((activity_logs_backup.user_id)::text = (u.id)::text) AND (activity_logs_backup.created_at > (CURRENT_DATE - '30 days'::interval)))), (0)::bigint) AS recent_activities
   FROM public.users u
  WHERE ((u.status)::text = 'active'::text);


ALTER TABLE public.v_user_statistics OWNER TO "user";

--
-- Name: visual_analysis_graphs; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.visual_analysis_graphs (
    id integer NOT NULL,
    name character varying(255) NOT NULL,
    project_ids text,
    updated_by character varying(100) DEFAULT 'user'::character varying,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.visual_analysis_graphs OWNER TO "user";

--
-- Name: visual_analysis_graphs_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.visual_analysis_graphs_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.visual_analysis_graphs_id_seq OWNER TO "user";

--
-- Name: visual_analysis_graphs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.visual_analysis_graphs_id_seq OWNED BY public.visual_analysis_graphs.id;


--
-- Name: visual_analysis_nodes; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.visual_analysis_nodes (
    id integer NOT NULL,
    graph_id integer NOT NULL,
    project_id character varying(50) NOT NULL,
    person_id integer NOT NULL,
    is_visible boolean DEFAULT true,
    node_x double precision DEFAULT 0,
    node_y double precision DEFAULT 0,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.visual_analysis_nodes OWNER TO "user";

--
-- Name: TABLE visual_analysis_nodes; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.visual_analysis_nodes IS '視覺化分析圖表節點資料表';


--
-- Name: COLUMN visual_analysis_nodes.graph_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.visual_analysis_nodes.graph_id IS '關聯的視覺化分析圖表ID';


--
-- Name: COLUMN visual_analysis_nodes.project_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.visual_analysis_nodes.project_id IS '專案ID';


--
-- Name: COLUMN visual_analysis_nodes.person_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.visual_analysis_nodes.person_id IS '人員ID';


--
-- Name: COLUMN visual_analysis_nodes.is_visible; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.visual_analysis_nodes.is_visible IS '是否在圖表中顯示該節點';


--
-- Name: COLUMN visual_analysis_nodes.node_x; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.visual_analysis_nodes.node_x IS '節點在畫布上的X座標';


--
-- Name: COLUMN visual_analysis_nodes.node_y; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.visual_analysis_nodes.node_y IS '節點在畫布上的Y座標';


--
-- Name: visual_analysis_nodes_id_seq; Type: SEQUENCE; Schema: public; Owner: user
--

CREATE SEQUENCE public.visual_analysis_nodes_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER TABLE public.visual_analysis_nodes_id_seq OWNER TO "user";

--
-- Name: visual_analysis_nodes_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: user
--

ALTER SEQUENCE public.visual_analysis_nodes_id_seq OWNED BY public.visual_analysis_nodes.id;


--
-- Name: activity_logs_backup id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.activity_logs_backup ALTER COLUMN id SET DEFAULT nextval('public.activity_logs_id_seq'::regclass);


--
-- Name: analysis_results id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_results ALTER COLUMN id SET DEFAULT nextval('public.analysis_results_id_seq'::regclass);


--
-- Name: audit_change_details id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_change_details ALTER COLUMN id SET DEFAULT nextval('public.audit_change_details_id_seq'::regclass);


--
-- Name: audit_compliance_reports id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_compliance_reports ALTER COLUMN id SET DEFAULT nextval('public.audit_compliance_reports_id_seq'::regclass);


--
-- Name: audit_event_types id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_event_types ALTER COLUMN id SET DEFAULT nextval('public.audit_event_types_id_seq'::regclass);


--
-- Name: audit_log_access id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_log_access ALTER COLUMN id SET DEFAULT nextval('public.audit_log_access_id_seq'::regclass);


--
-- Name: audit_logs id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_logs ALTER COLUMN id SET DEFAULT nextval('public.audit_logs_id_seq'::regclass);


--
-- Name: data_migration_log id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.data_migration_log ALTER COLUMN id SET DEFAULT nextval('public.data_migration_log_id_seq'::regclass);


--
-- Name: field_mapping id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping ALTER COLUMN id SET DEFAULT nextval('public.field_mapping_id_seq'::regclass);


--
-- Name: mergedpersons id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.mergedpersons ALTER COLUMN id SET DEFAULT nextval('public.mergedpersons_id_seq'::regclass);


--
-- Name: missing_persons id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.missing_persons ALTER COLUMN id SET DEFAULT nextval('public.missing_persons_id_seq'::regclass);


--
-- Name: permissions id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.permissions ALTER COLUMN id SET DEFAULT nextval('public.permissions_id_seq'::regclass);


--
-- Name: person_profile id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.person_profile ALTER COLUMN id SET DEFAULT nextval('public.person_profile_id_seq'::regclass);


--
-- Name: personmergelog id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.personmergelog ALTER COLUMN id SET DEFAULT nextval('public.personmergelog_id_seq'::regclass);


--
-- Name: photos id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.photos ALTER COLUMN id SET DEFAULT nextval('public.photos_id_seq'::regclass);


--
-- Name: project_permissions id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.project_permissions ALTER COLUMN id SET DEFAULT nextval('public.project_permissions_id_seq'::regclass);


--
-- Name: relationship_layers id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers ALTER COLUMN id SET DEFAULT nextval('public.relationship_layers_id_seq'::regclass);


--
-- Name: search_keywords id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.search_keywords ALTER COLUMN id SET DEFAULT nextval('public.search_keywords_id_seq'::regclass);


--
-- Name: search_logs id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.search_logs ALTER COLUMN id SET DEFAULT nextval('public.search_logs_id_seq'::regclass);


--
-- Name: sync_error_log id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.sync_error_log ALTER COLUMN id SET DEFAULT nextval('public.sync_error_log_id_seq'::regclass);


--
-- Name: sync_log id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.sync_log ALTER COLUMN id SET DEFAULT nextval('public.sync_log_id_seq'::regclass);


--
-- Name: sync_status id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.sync_status ALTER COLUMN id SET DEFAULT nextval('public.sync_status_id_seq'::regclass);


--
-- Name: user_favorites id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_favorites ALTER COLUMN id SET DEFAULT nextval('public.user_favorites_id_seq'::regclass);


--
-- Name: user_update_file_archived_20250802 id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_update_file_archived_20250802 ALTER COLUMN id SET DEFAULT nextval('public.user_update_file_id_seq'::regclass);


--
-- Name: visual_analysis_graphs id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.visual_analysis_graphs ALTER COLUMN id SET DEFAULT nextval('public.visual_analysis_graphs_id_seq'::regclass);


--
-- Name: visual_analysis_nodes id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.visual_analysis_nodes ALTER COLUMN id SET DEFAULT nextval('public.visual_analysis_nodes_id_seq'::regclass);


--
-- Data for Name: activity_logs_backup; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.activity_logs_backup (id, user_id, action, resource_type, resource_id, details, ip_address, created_at) FROM stdin;
1	admin_default	database_migration	\N	\N	建立使用者管理系統資料表	\N	2025-08-02 01:34:47.379366
2	admin_default	database_migration	\N	\N	修改現有資料表加入 user_id 欄位	\N	2025-08-02 01:34:47.431553
3	admin_default	database_migration	\N	\N	建立檢視和函數	\N	2025-08-02 01:34:47.456399
4	admin_default	database_migration_fix	\N	\N	修正缺失的資料表關聯	\N	2025-08-02 01:35:32.210256
\.


--
-- Data for Name: analysis_results; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.analysis_results (id, user_id, person_id, analysis_result, analysis_date, progress_percentage, status, current_step, status_message, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: analysis_sessions; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.analysis_sessions (id, user_id, root_person_id, max_depth, status, total_relationships, created_at, completed_at) FROM stdin;
\.


--
-- Data for Name: audit_change_details; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.audit_change_details (id, audit_log_id, field_name, field_type, old_value, new_value, change_type, is_sensitive, created_at) FROM stdin;
\.


--
-- Data for Name: audit_compliance_reports; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.audit_compliance_reports (id, report_id, report_type, report_name, date_from, date_to, generated_by, generated_at, status, total_events, critical_events, security_events, compliance_violations, file_path, file_size_bytes, checksum, retention_until, additional_metadata) FROM stdin;
\.


--
-- Data for Name: audit_event_types; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.audit_event_types (id, code, name, description, category, severity, retention_days, requires_approval, compliance_required, is_active, created_at, updated_at) FROM stdin;
1	LOGIN	使用者登入	使用者成功登入系統	SECURITY	INFO	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
2	LOGIN_FAILED	登入失敗	使用者登入失敗	SECURITY	WARN	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
3	LOGOUT	使用者登出	使用者登出系統	SECURITY	INFO	90	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
4	PASSWORD_CHANGE	密碼變更	使用者變更密碼	SECURITY	INFO	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
5	PASSWORD_RESET	密碼重設	管理員重設使用者密碼	SECURITY	WARN	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
6	PERMISSION_GRANTED	權限授予	使用者獲得新權限	SECURITY	WARN	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
7	PERMISSION_REVOKED	權限撤銷	使用者權限被撤銷	SECURITY	WARN	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
8	ROLE_CHANGED	角色變更	使用者角色被變更	SECURITY	WARN	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
9	ACCESS_DENIED	存取拒絕	使用者嘗試存取無權限資源	SECURITY	WARN	180	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
10	DATA_VIEW	資料檢視	使用者檢視資料	BUSINESS	DEBUG	30	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
11	DATA_CREATE	資料建立	建立新資料記錄	BUSINESS	INFO	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
12	DATA_UPDATE	資料更新	更新現有資料記錄	BUSINESS	INFO	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
13	DATA_DELETE	資料刪除	刪除資料記錄	BUSINESS	WARN	2555	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
14	DATA_EXPORT	資料匯出	匯出資料	BUSINESS	INFO	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
15	DATA_IMPORT	資料匯入	匯入資料	BUSINESS	INFO	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
16	FILE_UPLOAD	檔案上傳	上傳檔案	BUSINESS	INFO	365	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
17	FILE_DOWNLOAD	檔案下載	下載檔案	BUSINESS	INFO	90	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
18	FILE_DELETE	檔案刪除	刪除檔案	BUSINESS	WARN	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
19	FILE_MODIFY	檔案修改	修改檔案內容或屬性	BUSINESS	INFO	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
20	SYSTEM_CONFIG	系統配置	變更系統配置	SYSTEM	WARN	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
21	SYSTEM_BACKUP	系統備份	執行系統備份	SYSTEM	INFO	90	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
22	SYSTEM_RESTORE	系統還原	執行系統還原	SYSTEM	CRITICAL	2555	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
23	SYSTEM_MAINTENANCE	系統維護	執行系統維護操作	SYSTEM	INFO	180	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
24	SECURITY_BREACH	安全入侵	檢測到安全入侵	SECURITY	CRITICAL	2555	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
25	SUSPICIOUS_ACTIVITY	可疑活動	檢測到可疑活動	SECURITY	WARN	365	f	t	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
26	SECURITY_SCAN	安全掃描	執行安全掃描	SECURITY	INFO	90	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
27	API_CALL	API 呼叫	API 端點被呼叫	TECHNICAL	DEBUG	30	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
28	API_ERROR	API 錯誤	API 呼叫發生錯誤	TECHNICAL	ERROR	90	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
29	API_RATE_LIMIT	API 限流	API 呼叫觸發限流	TECHNICAL	WARN	30	f	f	t	2025-08-02 20:30:38.749524+08	2025-08-02 20:30:38.749524+08
\.


--
-- Data for Name: audit_log_access; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.audit_log_access (id, accessor_user_id, accessor_user_name, accessor_ip, accessed_log_id, accessed_resource_type, accessed_resource_id, access_type, search_criteria, records_returned, purpose, approval_required, approved_by, approved_at, accessed_at) FROM stdin;
\.


--
-- Data for Name: audit_logs; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.audit_logs (id, event_id, batch_id, session_id, user_id, user_name, user_role, impersonator_id, event_type, action, resource_type, resource_id, resource_name, old_values, new_values, changes_summary, ip_address, user_agent, request_method, request_url, request_id, success, error_message, error_code, response_time_ms, security_level, risk_score, is_suspicious, occurred_at, created_at, additional_data, tags, compliance_flags) FROM stdin;
1	16ff4c65-798e-427a-8c3c-5cc31c17cebc	\N	\N	test_user	Test User	admin	\N	DATA_CREATE	CREATE	Person	12345	Test Person	\N	\N	Created test audit log entry	127.0.0.1	Test Browser	\N	\N	\N	t	\N	\N	\N	NORMAL	0	f	2025-08-02 20:30:38.76826+08	2025-08-02 20:30:38.76826+08	\N	\N	\N
2	f8e3f776-523e-4cfc-a1e8-100978284e6e	\N	\N	admin_default	\N	\N	\N	LEGACY_ACTIVITY	database_migration	\N	\N	\N	\N	\N	建立使用者管理系統資料表	\N	\N	\N	\N	\N	t	\N	\N	\N	NORMAL	0	f	2025-08-02 01:34:47.379366+08	2025-08-02 01:34:47.379366+08	\N	\N	\N
3	e56f51d4-8f3a-43df-a1be-31aa09068957	\N	\N	admin_default	\N	\N	\N	LEGACY_ACTIVITY	database_migration	\N	\N	\N	\N	\N	修改現有資料表加入 user_id 欄位	\N	\N	\N	\N	\N	t	\N	\N	\N	NORMAL	0	f	2025-08-02 01:34:47.431553+08	2025-08-02 01:34:47.431553+08	\N	\N	\N
4	65fdc06f-283f-46c1-94a8-e44ea67389c0	\N	\N	admin_default	\N	\N	\N	LEGACY_ACTIVITY	database_migration	\N	\N	\N	\N	\N	建立檢視和函數	\N	\N	\N	\N	\N	t	\N	\N	\N	NORMAL	0	f	2025-08-02 01:34:47.456399+08	2025-08-02 01:34:47.456399+08	\N	\N	\N
5	b152dab5-2535-4d52-b101-7a3440fc0aa0	\N	\N	admin_default	\N	\N	\N	LEGACY_ACTIVITY	database_migration_fix	\N	\N	\N	\N	\N	修正缺失的資料表關聯	\N	\N	\N	\N	\N	t	\N	\N	\N	NORMAL	0	f	2025-08-02 01:35:32.210256+08	2025-08-02 01:35:32.210256+08	\N	\N	\N
6	9a2b09af-32a7-493b-9549-c957e9528bad	\N	0HNEHO9VA352Q:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHO9VA352Q:00000001	t	\N	\N	32	NORMAL	0	f	2025-08-02 20:45:46.285775+08	2025-08-02 20:45:46.285775+08	\N	\N	\N
7	59aaeb74-b8d6-4049-954d-28c99d327d75	\N	0HNEHO9VA352R:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA352R:00000001	f	\N	\N	50	NORMAL	0	f	2025-08-02 20:45:46.356158+08	2025-08-02 20:45:46.356158+08	\N	\N	\N
8	d1e10ad2-4701-4567-b464-1aa4beba8378	\N	0HNEHO9VA352S:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA352S:00000001	f	\N	\N	11	NORMAL	0	f	2025-08-02 20:45:46.814161+08	2025-08-02 20:45:46.814161+08	\N	\N	\N
9	1d3a3c04-c3f7-4b6b-9cc0-cc53022cb44c	\N	0HNEHO9VA352U:00000001	\N	\N	\N	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auth/login	0HNEHO9VA352U:00000001	t	\N	\N	246	NORMAL	0	f	2025-08-02 20:45:56.593633+08	2025-08-02 20:45:56.593633+08	\N	\N	\N
10	38ea7ecb-2ea0-43a7-ace4-d8bfbec677a4	\N	0HNEHO9VA3532:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA3532:00000001	f	\N	\N	12	NORMAL	0	f	2025-08-02 20:46:01.431578+08	2025-08-02 20:46:01.431578+08	\N	\N	\N
11	a8f6aa1c-afa8-4057-a273-5f75c5b08fc2	\N	0HNEHO9VA3533:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA3533:00000001	f	\N	\N	16	NORMAL	0	f	2025-08-02 20:46:01.939445+08	2025-08-02 20:46:01.939445+08	\N	\N	\N
12	0b12290e-479b-4b58-b7fa-029399b5cbf6	\N	0HNEHO9VA3534:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA3534:00000001	f	\N	\N	11	NORMAL	0	f	2025-08-02 20:46:34.291787+08	2025-08-02 20:46:34.291787+08	\N	\N	\N
13	ddae6a33-fc77-4b00-a15d-15913fbbf625	\N	0HNEHO9VA3537:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHO9VA3537:00000001	t	\N	\N	37	NORMAL	0	f	2025-08-02 20:47:05.984735+08	2025-08-02 20:47:05.984735+08	\N	\N	\N
14	679c45a5-e668-4cf5-8b9a-bef8cc578017	\N	0HNEHO9VA3539:00000001	admin_default	admin	admin	\N	API_CALL	DELETE 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	DELETE	/api/File/863a3c45-a823-4df7-bc2f-01aa98c14b65	0HNEHO9VA3539:00000001	t	\N	\N	27	NORMAL	0	f	2025-08-02 20:47:09.585776+08	2025-08-02 20:47:09.585776+08	\N	\N	\N
15	89508047-a5a1-4822-84ac-a4d8659bdbd6	\N	0HNEHO9VA353A:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHO9VA353A:00000001	t	\N	\N	3	NORMAL	0	f	2025-08-02 20:47:09.599988+08	2025-08-02 20:47:09.599988+08	\N	\N	\N
16	a75066c4-d4d3-47b4-8d46-6f8e7eae7f82	\N	0HNEHO9VA353B:00000001	admin_default	admin	admin	\N	API_CALL	DELETE 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	DELETE	/api/File/ed74a632-4d07-4081-8811-072c498e75bb	0HNEHO9VA353B:00000001	t	\N	\N	7	NORMAL	0	f	2025-08-02 20:47:12.070993+08	2025-08-02 20:47:12.070993+08	\N	\N	\N
17	c4401345-9f41-4dff-bfbf-c5cd806cd541	\N	0HNEHO9VA353C:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHO9VA353C:00000001	t	\N	\N	2	NORMAL	0	f	2025-08-02 20:47:12.083887+08	2025-08-02 20:47:12.083887+08	\N	\N	\N
18	471003a6-3d4c-4599-8959-6cc070b7a0c6	\N	0HNEHO9VA353D:00000001	admin_default	admin	admin	\N	API_CALL	DELETE 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	DELETE	/api/File/2e2ec457-81eb-4afc-99e6-de5223f9ae5b	0HNEHO9VA353D:00000001	t	\N	\N	8	NORMAL	0	f	2025-08-02 20:47:14.814714+08	2025-08-02 20:47:14.814714+08	\N	\N	\N
19	cdaac048-b0a5-4864-8202-066a87e87637	\N	0HNEHO9VA353E:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHO9VA353E:00000001	t	\N	\N	2	NORMAL	0	f	2025-08-02 20:47:14.825036+08	2025-08-02 20:47:14.825036+08	\N	\N	\N
20	300c09b0-7c42-49a5-9ecb-115b753e372e	\N	0HNEHO9VA353F:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/File/upload	0HNEHO9VA353F:00000001	t	\N	\N	29	NORMAL	0	f	2025-08-02 20:47:23.520717+08	2025-08-02 20:47:23.520717+08	\N	\N	\N
21	5fcb91ad-e2b2-49bc-9535-8e9bb14e751f	\N	0HNEHO9VA353G:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHO9VA353G:00000001	t	\N	\N	4	NORMAL	0	f	2025-08-02 20:47:23.538326+08	2025-08-02 20:47:23.538326+08	\N	\N	\N
22	10b4cb26-3d14-41ab-9d1b-b3e5a7338811	\N	0HNEHO9VA353K:00000001	\N	\N	\N	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auth/login	0HNEHO9VA353K:00000001	t	\N	\N	147	NORMAL	0	f	2025-08-02 20:52:48.348918+08	2025-08-02 20:52:48.348918+08	\N	\N	\N
23	44eda346-5df9-4210-af93-5104b8b86191	\N	0HNEHO9VA353R:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA353R:00000001	f	\N	\N	17	NORMAL	0	f	2025-08-02 20:53:21.946333+08	2025-08-02 20:53:21.946333+08	\N	\N	\N
24	0aa4d9c3-8196-4973-a8b2-2b42948a916e	\N	0HNEHO9VA353S:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA353S:00000001	f	\N	\N	12	NORMAL	0	f	2025-08-02 20:53:22.446345+08	2025-08-02 20:53:22.446345+08	\N	\N	\N
25	6f28cbe3-3002-4e76-af5b-6ea41fb69861	\N	0HNEHO9VA353T:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHO9VA353T:00000001	t	\N	\N	2	NORMAL	0	f	2025-08-02 20:53:24.993313+08	2025-08-02 20:53:24.993313+08	\N	\N	\N
26	66839003-a021-4b60-bdd3-37802345f533	\N	0HNEHO9VA353U:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA353U:00000001	f	\N	\N	17	NORMAL	0	f	2025-08-02 20:53:25.035386+08	2025-08-02 20:53:25.035386+08	\N	\N	\N
27	054b8dad-8851-4e8b-a83c-a5602002a67d	\N	0HNEHO9VA353V:00000001	admin_default	admin	admin	\N	API_ERROR	POST 500	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHO9VA353V:00000001	f	\N	\N	15	NORMAL	0	f	2025-08-02 20:53:25.532839+08	2025-08-02 20:53:25.532839+08	\N	\N	\N
28	e0bb2805-7ef6-4d36-817c-123159840c95	\N	0HNEHOEEBC8D8:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHOEEBC8D8:00000001	t	\N	\N	29	NORMAL	0	f	2025-08-02 20:53:51.42604+08	2025-08-02 20:53:51.42604+08	\N	\N	\N
29	5ca1eff1-0a22-4f3c-b1f3-dd192272e211	\N	0HNEHOEEBC8D9:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOEEBC8D9:00000001	t	\N	\N	49	NORMAL	0	f	2025-08-02 20:53:51.490479+08	2025-08-02 20:53:51.490479+08	\N	\N	\N
30	9c0ebeeb-e89c-4bd6-b3c1-f19c5900cca1	\N	0HNEHOEEBC8DA:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOEEBC8DA:00000001	t	\N	\N	5	NORMAL	0	f	2025-08-02 20:53:51.946427+08	2025-08-02 20:53:51.946427+08	\N	\N	\N
31	b4bdfcb4-aac3-400f-a581-f2066143f789	\N	0HNEHOEEBC8DC:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOEEBC8DC:00000001	t	\N	\N	7	NORMAL	0	f	2025-08-02 20:53:54.634679+08	2025-08-02 20:53:54.634679+08	\N	\N	\N
32	0766ed2f-c518-48cd-a0f0-3a38d139a260	\N	0HNEHOEEBC8DG:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOEEBC8DG:00000001	t	\N	\N	4	NORMAL	0	f	2025-08-02 20:54:03.373895+08	2025-08-02 20:54:03.373895+08	\N	\N	\N
33	6ee218db-b8b4-4d5d-bda3-c011265caa32	\N	0HNEHOEEBC8DH:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOEEBC8DH:00000001	t	\N	\N	7	NORMAL	0	f	2025-08-02 20:54:03.880035+08	2025-08-02 20:54:03.880035+08	\N	\N	\N
34	69f704a1-29c7-4ed9-8c8c-e46b1a197b51	\N	0HNEHOEEBC8DM:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOEEBC8DM:00000001	t	\N	\N	26	NORMAL	0	f	2025-08-02 20:56:36.186807+08	2025-08-02 20:56:36.186807+08	\N	\N	\N
35	e0b2922b-7c17-4bac-b02c-8ae8a320ba62	\N	0HNEHOHCUH67E:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHOHCUH67E:00000001	t	\N	\N	13	NORMAL	0	f	2025-08-02 21:02:53.94201+08	2025-08-02 21:02:53.94201+08	\N	\N	\N
36	a58fd6ad-9766-4bad-b262-34c390542b9d	\N	0HNEHOHCUH67F:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOHCUH67F:00000001	t	\N	\N	46	NORMAL	0	f	2025-08-02 21:02:54.009505+08	2025-08-02 21:02:54.009505+08	\N	\N	\N
37	9faa46d8-7832-4263-8d69-93cb72db0033	\N	0HNEHOHCUH67G:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOHCUH67G:00000001	t	\N	\N	4	NORMAL	0	f	2025-08-02 21:02:54.468659+08	2025-08-02 21:02:54.468659+08	\N	\N	\N
38	bfc10663-3347-4cf1-b3db-11ce2722636f	\N	0HNEHOHCUH67H:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOHCUH67H:00000001	t	\N	\N	25	NORMAL	0	f	2025-08-02 21:02:59.765834+08	2025-08-02 21:02:59.765834+08	\N	\N	\N
39	863b5b66-76f4-4e37-8c3d-1deb95b72acb	\N	0HNEHOHCUH67J:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHOHCUH67J:00000001	t	\N	\N	4	NORMAL	0	f	2025-08-02 21:03:41.908391+08	2025-08-02 21:03:41.908391+08	\N	\N	\N
40	8e462eaa-61dc-484b-a537-d74b8de6aee4	\N	0HNEHOHCUH67K:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOHCUH67K:00000001	t	\N	\N	61	NORMAL	0	f	2025-08-02 21:03:41.97803+08	2025-08-02 21:03:41.97803+08	\N	\N	\N
41	fd13440a-5ed9-4f5d-afb5-9e75343067fb	\N	0HNEHOHCUH67L:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOHCUH67L:00000001	t	\N	\N	2	NORMAL	0	f	2025-08-02 21:03:42.424359+08	2025-08-02 21:03:42.424359+08	\N	\N	\N
42	c292ec91-62a2-4402-8e8b-dd7f1f558d8b	\N	0HNEHOHCUH67M:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOHCUH67M:00000001	t	\N	\N	2	NORMAL	0	f	2025-08-02 21:03:47.79211+08	2025-08-02 21:03:47.79211+08	\N	\N	\N
43	3643f7c1-6776-4ddd-9b96-ff3c098c240e	\N	0HNEHOHCUH67N:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOHCUH67N:00000001	t	\N	\N	2	NORMAL	0	f	2025-08-02 21:03:48.443474+08	2025-08-02 21:03:48.443474+08	\N	\N	\N
44	8b57dc28-37de-4bde-a5fa-542b5e037e1f	\N	0HNEHOHCUH67O:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOHCUH67O:00000001	t	\N	\N	2	NORMAL	0	f	2025-08-02 21:03:50.320741+08	2025-08-02 21:03:50.320741+08	\N	\N	\N
45	148e42d7-2cf2-4f5c-a7f2-f733442f79cb	\N	0HNEHONP4PUT8:00000001	anonymous	Anonymous	unknown	\N	API_ERROR	POST 401	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auth/refresh	0HNEHONP4PUT8:00000001	f	\N	\N	114	NORMAL	0	f	2025-08-02 21:10:53.202673+08	2025-08-02 21:10:53.202673+08	\N	\N	\N
46	e72de72e-4a29-49b4-a304-a3f6f50e639f	\N	0HNEHONP4PUT9:00000001	anonymous	Anonymous	unknown	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auth/login	0HNEHONP4PUT9:00000001	t	\N	\N	248	NORMAL	0	f	2025-08-02 21:10:55.151677+08	2025-08-02 21:10:55.151677+08	\N	\N	\N
47	a59bd42d-713e-407c-b49b-aeb6f5f743e6	\N	0HNEHOOOURJOU:00000001	anonymous	Anonymous	unknown	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auth/login	0HNEHOOOURJOU:00000001	t	\N	\N	240	NORMAL	0	f	2025-08-02 21:12:17.462676+08	2025-08-02 21:12:17.462676+08	\N	\N	\N
48	6e93252d-bef0-4c00-96af-3a9238fec741	\N	0HNEHOOOURJP6:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOOOURJP6:00000001	t	\N	\N	68	NORMAL	0	f	2025-08-02 21:12:26.803669+08	2025-08-02 21:12:26.803669+08	\N	\N	\N
49	326e7f29-f635-4eea-bb8f-d4b50f8b7327	\N	0HNEHOOOURJP8:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/File/upload	0HNEHOOOURJP8:00000001	t	\N	\N	29	NORMAL	0	f	2025-08-02 21:12:33.288677+08	2025-08-02 21:12:33.288677+08	\N	\N	\N
50	ed4638aa-6504-407d-94af-147aa5f53ac9	\N	0HNEHOOOURJP9:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOOOURJP9:00000001	t	\N	\N	8	NORMAL	0	f	2025-08-02 21:12:33.310615+08	2025-08-02 21:12:33.310615+08	\N	\N	\N
51	8a5e09f8-65de-47f2-8d01-fd4c426ad12b	\N	0HNEHOOOURJPC:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOOOURJPC:00000001	t	\N	\N	53	NORMAL	0	f	2025-08-02 21:12:39.783872+08	2025-08-02 21:12:39.783872+08	\N	\N	\N
52	e245a41c-6e9e-4ade-8181-d6934674f105	\N	0HNEHOOOURJPH:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOOOURJPH:00000001	t	\N	\N	24	NORMAL	0	f	2025-08-02 21:13:13.38933+08	2025-08-02 21:13:13.38933+08	\N	\N	\N
53	e19aea7e-03de-4806-82a1-374148d54888	\N	0HNEHOQRPDBA8:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOQRPDBA8:00000001	t	\N	\N	80	NORMAL	0	f	2025-08-02 21:16:01.860399+08	2025-08-02 21:16:01.860399+08	\N	\N	\N
54	93024746-6f19-4d13-8b15-9c826ff5d0b7	\N	0HNEHOQRPDBAD:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHOQRPDBAD:00000001	t	\N	\N	10	NORMAL	0	f	2025-08-02 21:16:10.283672+08	2025-08-02 21:16:10.283672+08	\N	\N	\N
55	738695eb-8767-4127-88d8-3c60b9b392bb	\N	0HNEHOQRPDBAE:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOQRPDBAE:00000001	t	\N	\N	55	NORMAL	0	f	2025-08-02 21:16:10.369399+08	2025-08-02 21:16:10.369399+08	\N	\N	\N
56	7bba58cf-d800-4306-aa88-d556a1f2d086	\N	0HNEHOQRPDBAF:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOQRPDBAF:00000001	t	\N	\N	6	NORMAL	0	f	2025-08-02 21:16:10.820462+08	2025-08-02 21:16:10.820462+08	\N	\N	\N
57	6e482753-a6f8-4507-8cd0-f6083716d389	\N	0HNEHOTTQR8L2:00000001	anonymous	Anonymous	unknown	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auth/login	0HNEHOTTQR8L2:00000001	t	\N	\N	280	NORMAL	0	f	2025-08-02 21:21:29.058483+08	2025-08-02 21:21:29.058483+08	\N	\N	\N
58	586f4a08-3a32-429f-b6e2-2da90e6e03a9	\N	0HNEHOTTQR8L3:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHOTTQR8L3:00000001	t	\N	\N	23	NORMAL	0	f	2025-08-02 21:21:29.179335+08	2025-08-02 21:21:29.179335+08	\N	\N	\N
59	2f985b0f-8b67-4afb-8d2e-643c46165557	\N	0HNEHOTTQR8L4:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOTTQR8L4:00000001	t	\N	\N	45	NORMAL	0	f	2025-08-02 21:21:29.243507+08	2025-08-02 21:21:29.243507+08	\N	\N	\N
60	5db60912-0fc0-4ef3-8b7b-543cd19c487b	\N	0HNEHOTTQR8L5:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOTTQR8L5:00000001	t	\N	\N	5	NORMAL	0	f	2025-08-02 21:21:29.699075+08	2025-08-02 21:21:29.699075+08	\N	\N	\N
61	d011b04a-5e62-49b9-91f1-fff2313fc914	\N	0HNEHOTTQR8L7:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOTTQR8L7:00000001	t	\N	\N	8	NORMAL	0	f	2025-08-02 21:21:42.046707+08	2025-08-02 21:21:42.046707+08	\N	\N	\N
62	ead17532-0901-4574-9bbb-493a1a785d96	\N	0HNEHOTTQR8L8:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOTTQR8L8:00000001	t	\N	\N	5	NORMAL	0	f	2025-08-02 21:21:44.653958+08	2025-08-02 21:21:44.653958+08	\N	\N	\N
63	203230c8-2bbd-4e84-9fb6-999a961e5735	\N	0HNEHOTTQR8LD:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOTTQR8LD:00000001	t	\N	\N	25	NORMAL	0	f	2025-08-02 21:21:56.027939+08	2025-08-02 21:21:56.027939+08	\N	\N	\N
64	ebb95223-1988-41bd-971b-d30c142ff47c	\N	0HNEHOTTQR8LF:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/File/upload	0HNEHOTTQR8LF:00000001	t	\N	\N	33	NORMAL	0	f	2025-08-02 21:22:05.171098+08	2025-08-02 21:22:05.171098+08	\N	\N	\N
65	19cbc9cc-1b0c-4f37-9097-6591439f785c	\N	0HNEHOTTQR8LG:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOTTQR8LG:00000001	t	\N	\N	5	NORMAL	0	f	2025-08-02 21:22:05.185804+08	2025-08-02 21:22:05.185804+08	\N	\N	\N
66	e45b492b-9ddd-4e93-9a7d-f88ed49ac523	\N	0HNEHOTTQR8LH:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOTTQR8LH:00000001	t	\N	\N	10	NORMAL	0	f	2025-08-02 21:22:47.736038+08	2025-08-02 21:22:47.736038+08	\N	\N	\N
67	b5613750-a959-431e-9ee5-47f6f649f646	\N	0HNEHOTTQR8LJ:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOTTQR8LJ:00000001	t	\N	\N	23	NORMAL	0	f	2025-08-02 21:26:32.520138+08	2025-08-02 21:26:32.520138+08	\N	\N	\N
68	87b98122-b629-48d4-b848-4a76655f311d	\N	0HNEHOTTQR8LO:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHOTTQR8LO:00000001	t	\N	\N	0	NORMAL	0	f	2025-08-02 21:26:45.547667+08	2025-08-02 21:26:45.547667+08	\N	\N	\N
69	f3c103ca-56a8-4bc6-9cbd-59208a80e707	\N	0HNEHOTTQR8LP:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOTTQR8LP:00000001	t	\N	\N	6	NORMAL	0	f	2025-08-02 21:26:45.570178+08	2025-08-02 21:26:45.570178+08	\N	\N	\N
70	5c434449-c2a9-45df-95a7-1d10faeb7e27	\N	0HNEHOTTQR8LQ:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHOTTQR8LQ:00000001	t	\N	\N	3	NORMAL	0	f	2025-08-02 21:26:46.068703+08	2025-08-02 21:26:46.068703+08	\N	\N	\N
71	8e22a602-4b24-4108-b59d-8dd7a80c0328	\N	0HNEHOTTQR8LT:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOTTQR8LT:00000001	t	\N	\N	18	NORMAL	0	f	2025-08-02 21:27:32.328231+08	2025-08-02 21:27:32.328231+08	\N	\N	\N
72	837512b8-beef-4789-9343-d5fe2e545bb5	\N	0HNEHOTTQR8LV:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHOTTQR8LV:00000001	t	\N	\N	3	NORMAL	0	f	2025-08-02 21:27:34.69934+08	2025-08-02 21:27:34.69934+08	\N	\N	\N
73	cdf6b6d2-915a-4681-9955-a29d5163166c	\N	0HNEHP1HD0AN3:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHP1HD0AN3:00000001	t	\N	\N	64	NORMAL	0	f	2025-08-02 21:27:54.209673+08	2025-08-02 21:27:54.209673+08	\N	\N	\N
74	c8a8fd40-f56c-4ea4-980e-92bd3fceeb56	\N	0HNEHP1HD0AN6:00000001	anonymous	Anonymous	unknown	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auth/login	0HNEHP1HD0AN6:00000001	t	\N	\N	246	NORMAL	0	f	2025-08-02 21:30:41.602309+08	2025-08-02 21:30:41.602309+08	\N	\N	\N
75	fb323cd7-ccf4-486a-919e-366789d2d1e0	\N	0HNEHP1HD0AND:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHP1HD0AND:00000001	t	\N	\N	32	NORMAL	0	f	2025-08-02 21:35:59.619795+08	2025-08-02 21:35:59.619795+08	\N	\N	\N
76	49f59ff8-a462-4ebb-b4a8-b3cefbffe6fe	\N	0HNEHP1HD0ANF:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/File/upload	0HNEHP1HD0ANF:00000001	t	\N	\N	34	NORMAL	0	f	2025-08-02 21:36:05.745033+08	2025-08-02 21:36:05.745033+08	\N	\N	\N
77	dfc8bf31-7f38-48fa-84d8-162c05e4c74c	\N	0HNEHP1HD0ANG:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/File/list	0HNEHP1HD0ANG:00000001	t	\N	\N	5	NORMAL	0	f	2025-08-02 21:36:05.758322+08	2025-08-02 21:36:05.758322+08	\N	\N	\N
78	69eb9810-b389-43bc-a799-b0fdda86c5ae	\N	0HNEHP1HD0ANK:00000001	admin_default	admin	admin	\N	API_CALL	GET 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	GET	/api/auditlog/event-types	0HNEHP1HD0ANK:00000001	t	\N	\N	7	NORMAL	0	f	2025-08-02 21:37:31.817483+08	2025-08-02 21:37:31.817483+08	\N	\N	\N
79	ef485319-c82b-4a70-a738-c146a60f0227	\N	0HNEHP1HD0ANL:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHP1HD0ANL:00000001	t	\N	\N	36	NORMAL	0	f	2025-08-02 21:37:31.869386+08	2025-08-02 21:37:31.869386+08	\N	\N	\N
80	368801a3-0bee-4055-90cf-37f1933fbd24	\N	0HNEHP1HD0ANM:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHP1HD0ANM:00000001	t	\N	\N	8	NORMAL	0	f	2025-08-02 21:37:32.348276+08	2025-08-02 21:37:32.348276+08	\N	\N	\N
81	26f6b4ed-792b-45e0-a863-245b7c995de2	\N	0HNEHP1HD0ANQ:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHP1HD0ANQ:00000001	t	\N	\N	6	NORMAL	0	f	2025-08-02 21:37:39.477715+08	2025-08-02 21:37:39.477715+08	\N	\N	\N
82	9ae3f36b-0157-4f91-b414-cc0379e5813a	\N	0HNEHP1HD0ANR:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHP1HD0ANR:00000001	t	\N	\N	5	NORMAL	0	f	2025-08-02 21:37:39.983975+08	2025-08-02 21:37:39.983975+08	\N	\N	\N
83	d2ed3a3d-908d-4da4-ab08-51bf6ddf62c2	\N	0HNEHP1HD0ANS:00000001	admin_default	admin	admin	\N	API_CALL	POST 200	\N	\N	\N	\N	\N	\N	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15	POST	/api/auditlog/query	0HNEHP1HD0ANS:00000001	t	\N	\N	44	NORMAL	0	f	2025-08-02 21:37:48.990069+08	2025-08-02 21:37:48.990069+08	\N	\N	\N
\.


--
-- Data for Name: data_migration_log; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.data_migration_log (id, migration_name, executed_at, executed_by, status, records_affected, notes) FROM stdin;
1	Phase1-DataIsolationStandardization	2025-08-02 14:19:17.917879	system	completed	36	Fixed missing user_id in person_profile table. Unified data isolation strategy to use user_id as primary isolation mechanism.
2	FileNamingStandardization-Phase1	2025-08-02 14:59:10.105394	system	completed	3	Successfully migrated user_update_file table to file_uploads with proper naming conventions. Replaced project_id with user_id + associated_record_id/type pattern. Created compatibility view for backward compatibility. All 3 files migrated successfully.
3	FileNamingStandardization-Phase1	2025-08-02 20:04:46.138836	system	completed	13	Migrated user_update_file table to file_uploads with proper naming conventions. Replaced project_id with user_id + associated_record_id/type pattern. Created compatibility view for backward compatibility.
4	DropOldFileTable-Archive	2025-08-02 20:12:58.848376	system	completed	4	Archived user_update_file table to user_update_file_archived_20250802. All data migrated to file_uploads. Table can be dropped after verification.
\.


--
-- Data for Name: field_mapping; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.field_mapping (id, excel_field_name, db_field_name, created_at, updated_at, user_id) FROM stdin;
1	姓名	name	2025-07-29 21:09:08.40895	2025-08-02 01:34:47.424863	admin_default
2	名字	name	2025-07-29 21:09:08.413076	2025-08-02 01:34:47.424863	admin_default
3	Name	name	2025-07-29 21:09:08.41335	2025-08-02 01:34:47.424863	admin_default
4	性別	gender	2025-07-29 21:09:08.41351	2025-08-02 01:34:47.424863	admin_default
5	Gender	gender	2025-07-29 21:09:08.413772	2025-08-02 01:34:47.424863	admin_default
6	生日	birthday	2025-07-29 21:09:08.414401	2025-08-02 01:34:47.424863	admin_default
7	出生日期	birthday	2025-07-29 21:09:08.41501	2025-08-02 01:34:47.424863	admin_default
8	Birthday	birthday	2025-07-29 21:09:08.41595	2025-08-02 01:34:47.424863	admin_default
9	Date of Birth	birthday	2025-07-29 21:09:08.417162	2025-08-02 01:34:47.424863	admin_default
10	出生地	birthplace	2025-07-29 21:09:08.417892	2025-08-02 01:34:47.424863	admin_default
11	父母戶籍所在地	birthplace	2025-07-29 21:09:08.41866	2025-08-02 01:34:47.424863	admin_default
12	Birthplace	birthplace	2025-07-29 21:09:08.419663	2025-08-02 01:34:47.424863	admin_default
13	Place of Birth	birthplace	2025-07-29 21:09:08.420134	2025-08-02 01:34:47.424863	admin_default
14	國籍	nationality	2025-07-29 21:09:08.42059	2025-08-02 01:34:47.424863	admin_default
15	Nationality	nationality	2025-07-29 21:09:08.420817	2025-08-02 01:34:47.424863	admin_default
16	民族	ethnicity	2025-07-29 21:09:08.421015	2025-08-02 01:34:47.424863	admin_default
17	Ethnicity	ethnicity	2025-07-29 21:09:08.421194	2025-08-02 01:34:47.424863	admin_default
18	籍貫	ancestral_origin	2025-07-29 21:09:08.421349	2025-08-02 01:34:47.424863	admin_default
19	祖父戶籍所在地	ancestral_origin	2025-07-29 21:09:08.421566	2025-08-02 01:34:47.424863	admin_default
20	Ancestral Home	ancestral_origin	2025-07-29 21:09:08.42176	2025-08-02 01:34:47.424863	admin_default
21	黨派	political_party	2025-07-29 21:09:08.421926	2025-08-02 01:34:47.424863	admin_default
22	Political Party	political_party	2025-07-29 21:09:08.42216	2025-08-02 01:34:47.424863	admin_default
23	身分證號碼	id_number	2025-07-29 21:09:08.422394	2025-08-02 01:34:47.424863	admin_default
24	身份證號碼	id_number	2025-07-29 21:09:08.422772	2025-08-02 01:34:47.424863	admin_default
25	ID Number	id_number	2025-07-29 21:09:08.423172	2025-08-02 01:34:47.424863	admin_default
26	Identity Number	id_number	2025-07-29 21:09:08.423527	2025-08-02 01:34:47.424863	admin_default
27	護照號碼	passport_number	2025-07-29 21:09:08.423931	2025-08-02 01:34:47.424863	admin_default
28	Passport Number	passport_number	2025-07-29 21:09:08.42459	2025-08-02 01:34:47.424863	admin_default
29	電話	phone	2025-07-29 21:09:08.425389	2025-08-02 01:34:47.424863	admin_default
30	Phone	phone	2025-07-29 21:09:08.425951	2025-08-02 01:34:47.424863	admin_default
31	Telephone	phone	2025-07-29 21:09:08.426755	2025-08-02 01:34:47.424863	admin_default
32	行動電話	mobile	2025-07-29 21:09:08.427613	2025-08-02 01:34:47.424863	admin_default
33	手機	mobile	2025-07-29 21:09:08.428519	2025-08-02 01:34:47.424863	admin_default
34	Mobile	mobile	2025-07-29 21:09:08.428879	2025-08-02 01:34:47.424863	admin_default
35	Cell Phone	mobile	2025-07-29 21:09:08.429112	2025-08-02 01:34:47.424863	admin_default
36	電子信箱	email	2025-07-29 21:09:08.429283	2025-08-02 01:34:47.424863	admin_default
37	Email	email	2025-07-29 21:09:08.430036	2025-08-02 01:34:47.424863	admin_default
38	E-mail	email	2025-07-29 21:09:08.430363	2025-08-02 01:34:47.424863	admin_default
39	現職單位	current_employer	2025-07-29 21:09:08.431393	2025-08-02 01:34:47.424863	admin_default
40	工作單位	current_employer	2025-07-29 21:09:08.432622	2025-08-02 01:34:47.424863	admin_default
41	Current Workplace	current_employer	2025-07-29 21:09:08.434006	2025-08-02 01:34:47.424863	admin_default
42	現居地址	address	2025-07-29 21:09:08.434811	2025-08-02 01:34:47.424863	admin_default
43	居住地址	address	2025-07-29 21:09:08.435497	2025-08-02 01:34:47.424863	admin_default
44	Current Address	address	2025-07-29 21:09:08.43585	2025-08-02 01:34:47.424863	admin_default
45	通訊地址	mailing_address	2025-07-29 21:09:08.436245	2025-08-02 01:34:47.424863	admin_default
46	聯絡地址	mailing_address	2025-07-29 21:09:08.436717	2025-08-02 01:34:47.424863	admin_default
47	Mailing Address	mailing_address	2025-07-29 21:09:08.43704	2025-08-02 01:34:47.424863	admin_default
48	親屬關係	family_relationships	2025-07-29 21:09:08.437358	2025-08-02 01:34:47.424863	admin_default
49	Family Relationships	family_relationships	2025-07-29 21:09:08.437705	2025-08-02 01:34:47.424863	admin_default
50	經歷	experience	2025-07-29 21:09:08.439678	2025-08-02 01:34:47.424863	admin_default
51	工作經歷	experience	2025-07-29 21:09:08.442615	2025-08-02 01:34:47.424863	admin_default
52	Experience	experience	2025-07-29 21:09:08.446389	2025-08-02 01:34:47.424863	admin_default
53	學歷	education	2025-07-29 21:09:08.446895	2025-08-02 01:34:47.424863	admin_default
54	Education	education	2025-07-29 21:09:08.448061	2025-08-02 01:34:47.424863	admin_default
55	網路帳號	online_accounts	2025-07-29 21:09:08.448556	2025-08-02 01:34:47.424863	admin_default
56	Online Accounts	online_accounts	2025-07-29 21:09:08.449549	2025-08-02 01:34:47.424863	admin_default
57	著作	publications	2025-07-29 21:09:08.450559	2025-08-02 01:34:47.424863	admin_default
58	Publications	publications	2025-07-29 21:09:08.451613	2025-08-02 01:34:47.424863	admin_default
59	參與活動	activities	2025-07-29 21:09:08.452232	2025-08-02 01:34:47.424863	admin_default
60	Activities	activities	2025-07-29 21:09:08.452602	2025-08-02 01:34:47.424863	admin_default
61	重要友人	important_friends	2025-07-29 21:09:08.453391	2025-08-02 01:34:47.424863	admin_default
62	Important Friends	important_friends	2025-07-29 21:09:08.453848	2025-08-02 01:34:47.424863	admin_default
63	經常出入場所	frequent_locations	2025-07-29 21:09:08.454096	2025-08-02 01:34:47.424863	admin_default
64	Frequent Places	frequent_locations	2025-07-29 21:09:08.454268	2025-08-02 01:34:47.424863	admin_default
65	出國紀錄	travel_history	2025-07-29 21:09:08.45441	2025-08-02 01:34:47.424863	admin_default
66	Travel Records	travel_history	2025-07-29 21:09:08.454541	2025-08-02 01:34:47.424863	admin_default
67	備註	remarks	2025-07-29 21:09:08.45466	2025-08-02 01:34:47.424863	admin_default
68	Notes	remarks	2025-07-29 21:09:08.454806	2025-08-02 01:34:47.424863	admin_default
69	Remarks	remarks	2025-07-29 21:09:08.454989	2025-08-02 01:34:47.424863	admin_default
70	發掘經過	discovery_process	2025-07-29 21:09:08.455158	2025-08-02 01:34:47.424863	admin_default
71	Discovery Process	discovery_process	2025-07-29 21:09:08.455308	2025-08-02 01:34:47.424863	admin_default
72	照片	photo_index	2025-07-29 21:09:08.455505	2025-08-02 01:34:47.424863	admin_default
73	Photo	photo_index	2025-07-29 21:09:08.455653	2025-08-02 01:34:47.424863	admin_default
74	Picture	photo_index	2025-07-29 21:09:08.455776	2025-08-02 01:34:47.424863	admin_default
75	經歷(單位，職稱，任職期間)	experience	2025-07-29 21:09:08.455899	2025-08-02 01:34:47.424863	admin_default
76	親屬關係(職稱，姓名)	family_relationships	2025-07-29 21:09:08.456008	2025-08-02 01:34:47.424863	admin_default
77	著作(名稱，共同作者)	publications	2025-07-29 21:09:08.456112	2025-08-02 01:34:47.424863	admin_default
78	參與活動(活動名稱，參與人士)	activities	2025-07-29 21:09:08.456213	2025-08-02 01:34:47.424863	admin_default
79	重要友人(姓名，單位，關聯事件)	important_friends	2025-07-29 21:09:08.456336	2025-08-02 01:34:47.424863	admin_default
80	出生地(父母戶籍所在地)	birthplace	2025-07-29 21:09:08.456453	2025-08-02 01:34:47.424863	admin_default
81	籍貫(祖父戶籍所在地)	ancestral_origin	2025-07-29 21:09:08.456563	2025-08-02 01:34:47.424863	admin_default
\.


--
-- Data for Name: file_uploads; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.file_uploads (file_id, user_id, filename, original_filename, file_path, file_size, md5_hash, file_type, mime_type, associated_record_id, associated_record_type, upload_status, is_processed, processed_at, uploaded_at, created_at, updated_at) FROM stdin;
2e2ec457-81eb-4afc-99e6-de5223f9ae5b	admin_default	分公司客戶基資表-廠商測試版_20250729_131546_ac400430.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree-1/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250729_131546_ac400430.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	excel	application/vnd.openxmlformats-officedocument.spreadsheetml.sheet	968348-20250729211539	project	deleted	t	2025-08-02 18:38:34.702097	2025-07-29 21:15:46.787473	2025-07-29 21:15:46.789843	2025-08-02 20:47:14.812414
30514cd2-2415-4c0e-afcb-f404dc4d3f4f	admin_default	分公司客戶基資表-廠商測試版_20250802_124723_6855b1a7.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250802_124723_6855b1a7.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	excel	application/vnd.openxmlformats-officedocument.spreadsheetml.sheet	p250802183809016115	project	uploaded	f	\N	2025-08-02 20:47:23.512938	2025-08-02 20:47:23.512938	2025-08-02 20:47:23.512938
20c0c320-d182-4200-ad41-652310ea6eb3	admin_default	分公司客戶基資表-廠商測試版_20250802_103834_3209086d.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250802_103834_3209086d.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	excel	application/vnd.openxmlformats-officedocument.spreadsheetml.sheet	p250802183809016115	project	deleted	t	2025-08-02 18:38:34.702097	2025-08-02 18:38:34.504623	2025-08-02 18:38:34.507034	2025-08-02 20:44:01.444077
863a3c45-a823-4df7-bc2f-01aa98c14b65	admin_default	分公司客戶基資表-廠商測試版_20250802_054819_4c6befd5.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250802_054819_4c6befd5.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	excel	application/vnd.openxmlformats-officedocument.spreadsheetml.sheet	577050-20250731141511	project	deleted	t	2025-08-02 18:38:34.702097	2025-08-02 13:48:19.918347	2025-08-02 13:48:19.921969	2025-08-02 20:47:09.575874
ed74a632-4d07-4081-8811-072c498e75bb	admin_default	分公司客戶基資表-廠商測試版_20250730_153900_e87377f3.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250730_153900_e87377f3.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	excel	application/vnd.openxmlformats-officedocument.spreadsheetml.sheet	636216-20250730233734	project	deleted	t	2025-08-02 18:38:34.702097	2025-07-30 23:39:00.198591	2025-07-30 23:39:00.201013	2025-08-02 20:47:12.069058
\.


--
-- Data for Name: mergedpersons; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.mergedpersons (id, project_id, photo, name, discovery_process, gender, birthday, birthplace, nationality, ethnicity, ancestral_home, political_party, id_number, passport_number, phone, mobile, email, current_workplace, current_address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, important_friends, frequent_places, travel_records, notes, created_at, created_by, updated_at, updated_by, source_person_a_id, source_person_b_id) FROM stdin;
\.


--
-- Data for Name: missing_persons; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.missing_persons (id, user_id, name, relation_type, source_person_id, source_field, analysis_session_id, layer_depth, discovered_at, status, resolved_person_id, notes) FROM stdin;
\.


--
-- Data for Name: permissions; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.permissions (id, permission, display_name, description, category, created_at) FROM stdin;
1	user:create	建立使用者	建立新使用者帳號	系統管理	2025-08-02 13:32:29.594345
2	user:read	檢視使用者	檢視使用者資訊	系統管理	2025-08-02 13:32:29.594345
3	user:update	更新使用者	更新使用者資訊	系統管理	2025-08-02 13:32:29.594345
4	user:delete	刪除使用者	刪除使用者帳號	系統管理	2025-08-02 13:32:29.594345
5	role:manage	管理角色	管理角色和權限	系統管理	2025-08-02 13:32:29.594345
6	project:create	建立專案	建立新專案	專案管理	2025-08-02 13:32:29.594345
7	project:read	檢視專案	檢視專案資訊	專案管理	2025-08-02 13:32:29.594345
8	project:update	更新專案	更新專案資訊	專案管理	2025-08-02 13:32:29.594345
9	project:delete	刪除專案	刪除專案	專案管理	2025-08-02 13:32:29.594345
10	person:create	建立人員	建立人員資料	人員管理	2025-08-02 13:32:29.594345
11	person:read	檢視人員	檢視人員資料	人員管理	2025-08-02 13:32:29.594345
12	person:update	更新人員	更新人員資料	人員管理	2025-08-02 13:32:29.594345
13	person:delete	刪除人員	刪除人員資料	人員管理	2025-08-02 13:32:29.594345
14	file:upload	上傳檔案	上傳檔案	檔案管理	2025-08-02 13:32:29.594345
15	file:read	檢視檔案	檢視檔案	檔案管理	2025-08-02 13:32:29.594345
16	file:delete	刪除檔案	刪除檔案	檔案管理	2025-08-02 13:32:29.594345
17	file:download	下載檔案	下載檔案	檔案管理	2025-08-02 13:32:29.594345
18	file:process	處理檔案	處理檔案內容	檔案管理	2025-08-02 13:32:29.594345
19	search:perform	執行搜尋	執行搜尋功能	搜尋功能	2025-08-02 13:32:29.594345
20	report:view	檢視報表	檢視報表	報表功能	2025-08-02 13:32:29.594345
21	report:export	匯出報表	匯出報表	報表功能	2025-08-02 13:32:29.594345
\.


--
-- Data for Name: person_profile; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.person_profile (id, photo_index, name, discovery_source, gender, birthday, birthplace, nationality, ethnicity, ancestral_origin, political_party, id_number, passport_number, phone, mobile, email, current_employer, address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, friends, frequent_locations, travel_history, remarks, created_at, created_by, updated_at, updated_by, extra_data, source_id, source_table, source_created_at, source_updated_at, file_md5, source_file_id, source_file_name, discovery_process, important_friends, project_id, user_id) FROM stdin;
9	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.937681	\N	2025-07-29 13:15:46.937681	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539	admin_default
1	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-29 13:15:46.924786	\N	2025-07-29 13:15:46.924796	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	968348-20250729211539	admin_default
2	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.933122	\N	2025-07-29 13:15:46.933123	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	968348-20250729211539	admin_default
3	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.934327	\N	2025-07-29 13:15:46.934328	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	968348-20250729211539	admin_default
4	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.934925	\N	2025-07-29 13:15:46.934925	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	968348-20250729211539	admin_default
5	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.935457	\N	2025-07-29 13:15:46.935457	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	968348-20250729211539	admin_default
6	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.935980	\N	2025-07-29 13:15:46.935980	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	968348-20250729211539	admin_default
7	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.936502	\N	2025-07-29 13:15:46.936502	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	968348-20250729211539	admin_default
8	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-29 13:15:46.937096	\N	2025-07-29 13:15:46.937096	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539	admin_default
10	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938157	\N	2025-07-29 13:15:46.938158	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539	admin_default
11	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938540	\N	2025-07-29 13:15:46.938541	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	968348-20250729211539	admin_default
12	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938809	\N	2025-07-29 13:15:46.938809	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	968348-20250729211539	admin_default
13	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-30 23:39:00.331737+08	\N	2025-07-30 23:39:00.331748+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	636216-20250730233734	admin_default
14	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.338127+08	\N	2025-07-30 23:39:00.338127+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	636216-20250730233734	admin_default
15	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.339134+08	\N	2025-07-30 23:39:00.339134+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	636216-20250730233734	admin_default
16	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.340256+08	\N	2025-07-30 23:39:00.340256+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	636216-20250730233734	admin_default
17	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.341044+08	\N	2025-07-30 23:39:00.341044+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	636216-20250730233734	admin_default
18	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.34179+08	\N	2025-07-30 23:39:00.34179+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	636216-20250730233734	admin_default
19	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.342643+08	\N	2025-07-30 23:39:00.342643+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	636216-20250730233734	admin_default
20	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-30 23:39:00.345285+08	\N	2025-07-30 23:39:00.345285+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734	admin_default
21	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.346183+08	\N	2025-07-30 23:39:00.346183+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734	admin_default
22	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.347081+08	\N	2025-07-30 23:39:00.347081+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734	admin_default
23	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.347804+08	\N	2025-07-30 23:39:00.347804+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	636216-20250730233734	admin_default
24	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.348341+08	\N	2025-07-30 23:39:00.348341+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	636216-20250730233734	admin_default
25	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-08-02 13:48:20.048777+08	\N	2025-08-02 13:48:20.048787+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	577050-20250731141511	admin_default
26	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.057253+08	\N	2025-08-02 13:48:20.057253+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	577050-20250731141511	admin_default
27	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.058849+08	\N	2025-08-02 13:48:20.058849+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	577050-20250731141511	admin_default
28	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.060062+08	\N	2025-08-02 13:48:20.060062+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	577050-20250731141511	admin_default
29	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.061212+08	\N	2025-08-02 13:48:20.061212+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	577050-20250731141511	admin_default
30	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.062086+08	\N	2025-08-02 13:48:20.062086+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	577050-20250731141511	admin_default
31	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.062777+08	\N	2025-08-02 13:48:20.062777+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	577050-20250731141511	admin_default
32	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-08-02 13:48:20.06379+08	\N	2025-08-02 13:48:20.06379+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	577050-20250731141511	admin_default
33	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.064561+08	\N	2025-08-02 13:48:20.064561+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	577050-20250731141511	admin_default
34	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.065137+08	\N	2025-08-02 13:48:20.065137+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	577050-20250731141511	admin_default
35	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.069324+08	\N	2025-08-02 13:48:20.069324+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	577050-20250731141511	admin_default
36	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.081319+08	\N	2025-08-02 13:48:20.081319+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	577050-20250731141511	admin_default
\.


--
-- Data for Name: person_profile_backup_20250802; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.person_profile_backup_20250802 (id, photo_index, name, discovery_source, gender, birthday, birthplace, nationality, ethnicity, ancestral_origin, political_party, id_number, passport_number, phone, mobile, email, current_employer, address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, friends, frequent_locations, travel_history, remarks, created_at, created_by, updated_at, updated_by, extra_data, source_id, source_table, source_created_at, source_updated_at, file_md5, source_file_id, source_file_name, discovery_process, important_friends, project_id, user_id) FROM stdin;
9	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.937681	\N	2025-07-29 13:15:46.937681	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539	admin_default
1	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-29 13:15:46.924786	\N	2025-07-29 13:15:46.924796	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	968348-20250729211539	admin_default
2	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.933122	\N	2025-07-29 13:15:46.933123	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	968348-20250729211539	admin_default
3	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.934327	\N	2025-07-29 13:15:46.934328	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	968348-20250729211539	admin_default
4	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.934925	\N	2025-07-29 13:15:46.934925	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	968348-20250729211539	admin_default
5	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.935457	\N	2025-07-29 13:15:46.935457	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	968348-20250729211539	admin_default
6	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.935980	\N	2025-07-29 13:15:46.935980	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	968348-20250729211539	admin_default
7	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.936502	\N	2025-07-29 13:15:46.936502	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	968348-20250729211539	admin_default
8	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-29 13:15:46.937096	\N	2025-07-29 13:15:46.937096	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539	admin_default
10	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938157	\N	2025-07-29 13:15:46.938158	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539	admin_default
11	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938540	\N	2025-07-29 13:15:46.938541	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	968348-20250729211539	admin_default
12	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938809	\N	2025-07-29 13:15:46.938809	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	968348-20250729211539	admin_default
13	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-30 23:39:00.331737+08	\N	2025-07-30 23:39:00.331748+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	636216-20250730233734	admin_default
14	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.338127+08	\N	2025-07-30 23:39:00.338127+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	636216-20250730233734	admin_default
15	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.339134+08	\N	2025-07-30 23:39:00.339134+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	636216-20250730233734	admin_default
16	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.340256+08	\N	2025-07-30 23:39:00.340256+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	636216-20250730233734	admin_default
17	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.341044+08	\N	2025-07-30 23:39:00.341044+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	636216-20250730233734	admin_default
18	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.34179+08	\N	2025-07-30 23:39:00.34179+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	636216-20250730233734	admin_default
19	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.342643+08	\N	2025-07-30 23:39:00.342643+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	636216-20250730233734	admin_default
20	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-30 23:39:00.345285+08	\N	2025-07-30 23:39:00.345285+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734	admin_default
21	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.346183+08	\N	2025-07-30 23:39:00.346183+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734	admin_default
22	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.347081+08	\N	2025-07-30 23:39:00.347081+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734	admin_default
23	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.347804+08	\N	2025-07-30 23:39:00.347804+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	636216-20250730233734	admin_default
24	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.348341+08	\N	2025-07-30 23:39:00.348341+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	636216-20250730233734	admin_default
25	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-08-02 13:48:20.048777+08	\N	2025-08-02 13:48:20.048787+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	577050-20250731141511	\N
26	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.057253+08	\N	2025-08-02 13:48:20.057253+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	577050-20250731141511	\N
27	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.058849+08	\N	2025-08-02 13:48:20.058849+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	577050-20250731141511	\N
28	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.060062+08	\N	2025-08-02 13:48:20.060062+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	577050-20250731141511	\N
29	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.061212+08	\N	2025-08-02 13:48:20.061212+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	577050-20250731141511	\N
30	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.062086+08	\N	2025-08-02 13:48:20.062086+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	577050-20250731141511	\N
31	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.062777+08	\N	2025-08-02 13:48:20.062777+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	577050-20250731141511	\N
32	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-08-02 13:48:20.06379+08	\N	2025-08-02 13:48:20.06379+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	577050-20250731141511	\N
33	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.064561+08	\N	2025-08-02 13:48:20.064561+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	577050-20250731141511	\N
34	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.065137+08	\N	2025-08-02 13:48:20.065137+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	577050-20250731141511	\N
35	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.069324+08	\N	2025-08-02 13:48:20.069324+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	577050-20250731141511	\N
36	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-08-02 13:48:20.081319+08	\N	2025-08-02 13:48:20.081319+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	577050-20250731141511	\N
\.


--
-- Data for Name: personmergelog; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.personmergelog (id, merged_person_id, source_person_a_id, source_person_b_id, merged_by, merged_at, project_id) FROM stdin;
\.


--
-- Data for Name: photos; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.photos (id, original_filename, saved_filename, file_path, file_size, md5_hash, project_id, upload_time, created_at, updated_at, deleted_at) FROM stdin;
1	系統登入頁-T1.png	系統登入頁-T1.png	/Users/yangandy/FamilyTree-1/familytree-backend/photos/303334-20250729211457/系統登入頁-T1.png	2994841	5a9b797f1ce72bce106a927e9eb04dcc	303334-20250729211457	2025-07-29 21:15:04.430248+08	2025-07-29 21:15:04.43166+08	2025-07-29 21:15:04.43166+08	\N
\.


--
-- Data for Name: project_permissions; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.project_permissions (id, user_id, project_id, role, granted_at, granted_by) FROM stdin;
\.


--
-- Data for Name: projects; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.projects (id, user_id, project_name, project_description, status, created_at, completed_at, updated_at) FROM stdin;
303334-20250729211457	admin_default	test	test	deleted	2025-07-29 21:14:57.434749	\N	2025-08-02 01:34:47.429043
test01-20250730004405	admin_default	測試專案（已更新）	這是一個已更新的API測試專案	deleted	2025-07-30 00:44:05.964717	2025-07-30 00:44:13.586968	2025-08-02 01:34:47.429043
968348-20250729211539	admin_default	123	\N	\N	2025-07-29 21:15:39.289037	\N	2025-08-02 01:34:47.429043
636216-20250730233734	admin_default	test	test	\N	2025-07-30 23:37:34.873419	\N	2025-08-02 01:34:47.429043
499307-20250731132801	admin_default	test	tett	\N	2025-07-31 13:28:01.452892	\N	2025-08-02 01:34:47.429043
922142-20250731132807	admin_default	testt	testt	\N	2025-07-31 13:28:07.829965	\N	2025-08-02 01:34:47.429043
883487-20250731135414	admin_default	text	test	\N	2025-07-31 13:54:14.652295	\N	2025-08-02 01:34:47.429043
897217-20250731135535	admin_default	ㄅㄅ	ㄅ	\N	2025-07-31 13:55:35.142551	\N	2025-08-02 01:34:47.429043
934545-20250731140318	admin_default	123	2222	\N	2025-07-31 14:03:18.145623	\N	2025-08-02 01:34:47.429043
123456-20250731141338	admin_default	測試專案 2025-07-31T06:13:38.023Z (已更新)	這是更新後的描述	deleted	2025-07-31 14:13:38.079053	\N	2025-08-02 01:34:47.429043
123456-20250731140902	admin_default	測試專案 2025-07-31T06:09:02.039Z	這是一個測試專案ㄅ1	\N	2025-07-31 14:09:02.097685	\N	2025-08-02 01:34:47.429043
577050-20250731141511	admin_default	123	123	deleted	2025-07-31 14:15:11.431322	\N	2025-08-02 16:32:59.2052
p25080218375669e5bd	admin_default	1231	23	deleted	2025-08-02 18:37:56.968581	\N	2025-08-02 18:38:05.963351
p250802183809016115	admin_default	1	1	active	2025-08-02 18:38:09.229463	\N	2025-08-02 18:38:09.229463
\.


--
-- Data for Name: relationship_layers; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.relationship_layers (id, source_person_id, target_person_id, relation_type, source_field, created_at, updated_at, project_id, visual_analysis_graph_id, user_id) FROM stdin;
1	1	2	朋友	manual	2025-07-30 02:02:47.098392	2025-07-30 02:02:47.098392	968348-20250729211539	2	admin_default
2	1	3	朋友	manual	2025-07-30 02:03:32.880944	2025-07-30 02:03:32.880944	968348-20250729211539	2	admin_default
5	1	6	朋友	manual	2025-07-30 02:07:00.045372	2025-07-30 02:07:00.045372	968348-20250729211539	2	admin_default
6	5	4	ㄇ	manual	2025-07-30 02:07:23.622036	2025-07-30 02:07:23.622036	968348-20250729211539	2	admin_default
7	4	2	造	manual	2025-07-30 02:07:31.052867	2025-07-30 02:07:31.052867	968348-20250729211539	2	admin_default
8	3	4	朝	manual	2025-07-30 02:07:35.969011	2025-07-30 02:07:35.969011	968348-20250729211539	2	admin_default
9	4	11	照	manual	2025-07-30 02:07:39.302771	2025-07-30 02:07:39.302771	968348-20250729211539	2	admin_default
10	11	1	母女	manual	2025-07-31 01:21:11.309977	2025-07-31 01:21:11.309977	968348-20250729211539	2	admin_default
11	1	4	母女	manual	2025-07-31 01:21:20.910533	2025-07-31 01:21:20.910533	968348-20250729211539	2	admin_default
\.


--
-- Data for Name: relationship_layers_backup; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.relationship_layers_backup (id, source_person_id, target_person_id, relation_type, source_field, layer_depth, analysis_session_id, created_at, updated_at, project_id, visual_analysis_graph_id) FROM stdin;
\.


--
-- Data for Name: relationship_layers_backup_20250802; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.relationship_layers_backup_20250802 (id, source_person_id, target_person_id, relation_type, source_field, created_at, updated_at, project_id, visual_analysis_graph_id, user_id) FROM stdin;
1	1	2	朋友	manual	2025-07-30 02:02:47.098392	2025-07-30 02:02:47.098392	968348-20250729211539	2	admin_default
2	1	3	朋友	manual	2025-07-30 02:03:32.880944	2025-07-30 02:03:32.880944	968348-20250729211539	2	admin_default
5	1	6	朋友	manual	2025-07-30 02:07:00.045372	2025-07-30 02:07:00.045372	968348-20250729211539	2	admin_default
6	5	4	ㄇ	manual	2025-07-30 02:07:23.622036	2025-07-30 02:07:23.622036	968348-20250729211539	2	admin_default
7	4	2	造	manual	2025-07-30 02:07:31.052867	2025-07-30 02:07:31.052867	968348-20250729211539	2	admin_default
8	3	4	朝	manual	2025-07-30 02:07:35.969011	2025-07-30 02:07:35.969011	968348-20250729211539	2	admin_default
9	4	11	照	manual	2025-07-30 02:07:39.302771	2025-07-30 02:07:39.302771	968348-20250729211539	2	admin_default
10	11	1	母女	manual	2025-07-31 01:21:11.309977	2025-07-31 01:21:11.309977	968348-20250729211539	2	admin_default
11	1	4	母女	manual	2025-07-31 01:21:20.910533	2025-07-31 01:21:20.910533	968348-20250729211539	2	admin_default
\.


--
-- Data for Name: role_permissions; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.role_permissions (role_id, permission_id, granted_at, granted_by) FROM stdin;
superadmin	1	2025-08-02 13:36:39.277776	\N
superadmin	2	2025-08-02 13:36:39.277776	\N
superadmin	3	2025-08-02 13:36:39.277776	\N
superadmin	4	2025-08-02 13:36:39.277776	\N
superadmin	5	2025-08-02 13:36:39.277776	\N
superadmin	6	2025-08-02 13:36:39.277776	\N
superadmin	7	2025-08-02 13:36:39.277776	\N
superadmin	8	2025-08-02 13:36:39.277776	\N
superadmin	9	2025-08-02 13:36:39.277776	\N
superadmin	10	2025-08-02 13:36:39.277776	\N
superadmin	11	2025-08-02 13:36:39.277776	\N
superadmin	12	2025-08-02 13:36:39.277776	\N
superadmin	13	2025-08-02 13:36:39.277776	\N
superadmin	14	2025-08-02 13:36:39.277776	\N
superadmin	15	2025-08-02 13:36:39.277776	\N
superadmin	16	2025-08-02 13:36:39.277776	\N
superadmin	17	2025-08-02 13:36:39.277776	\N
superadmin	18	2025-08-02 13:36:39.277776	\N
superadmin	19	2025-08-02 13:36:39.277776	\N
superadmin	20	2025-08-02 13:36:39.277776	\N
superadmin	21	2025-08-02 13:36:39.277776	\N
admin	1	2025-08-02 13:36:39.28021	\N
admin	2	2025-08-02 13:36:39.28021	\N
admin	3	2025-08-02 13:36:39.28021	\N
admin	4	2025-08-02 13:36:39.28021	\N
admin	6	2025-08-02 13:36:39.28021	\N
admin	7	2025-08-02 13:36:39.28021	\N
admin	8	2025-08-02 13:36:39.28021	\N
admin	9	2025-08-02 13:36:39.28021	\N
admin	10	2025-08-02 13:36:39.28021	\N
admin	11	2025-08-02 13:36:39.28021	\N
admin	12	2025-08-02 13:36:39.28021	\N
admin	13	2025-08-02 13:36:39.28021	\N
admin	14	2025-08-02 13:36:39.28021	\N
admin	15	2025-08-02 13:36:39.28021	\N
admin	16	2025-08-02 13:36:39.28021	\N
admin	17	2025-08-02 13:36:39.28021	\N
admin	18	2025-08-02 13:36:39.28021	\N
admin	19	2025-08-02 13:36:39.28021	\N
admin	20	2025-08-02 13:36:39.28021	\N
admin	21	2025-08-02 13:36:39.28021	\N
user	6	2025-08-02 13:36:39.281445	\N
user	7	2025-08-02 13:36:39.281445	\N
user	10	2025-08-02 13:36:39.281445	\N
user	11	2025-08-02 13:36:39.281445	\N
user	12	2025-08-02 13:36:39.281445	\N
user	13	2025-08-02 13:36:39.281445	\N
user	14	2025-08-02 13:36:39.281445	\N
user	15	2025-08-02 13:36:39.281445	\N
user	17	2025-08-02 13:36:39.281445	\N
user	19	2025-08-02 13:36:39.281445	\N
user	20	2025-08-02 13:36:39.281445	\N
guest	7	2025-08-02 13:36:39.282202	\N
guest	11	2025-08-02 13:36:39.282202	\N
guest	20	2025-08-02 13:36:39.282202	\N
\.


--
-- Data for Name: roles; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.roles (id, display_name, description, level, is_system, created_at, updated_at) FROM stdin;
superadmin	超級管理員	系統最高權限，可以管理所有功能	100	t	2025-08-02 13:32:29.592741	2025-08-02 13:32:29.592741
admin	管理員	可以管理使用者、專案和大部分系統功能	90	t	2025-08-02 13:32:29.592741	2025-08-02 13:32:29.592741
user	一般使用者	可以使用基本功能	50	t	2025-08-02 13:32:29.592741	2025-08-02 13:32:29.592741
guest	訪客	只能查看公開資料	0	t	2025-08-02 13:32:29.592741	2025-08-02 13:32:29.592741
\.


--
-- Data for Name: search_keywords; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.search_keywords (id, keyword, search_count, search_type, last_search_time, created_at, updated_at, project_id) FROM stdin;
4	范立	1	fuzzy	2025-07-30 02:16:25.662919	2025-07-30 02:16:25.663116	2025-07-30 02:16:25.663116	968348-20250729211539
5	趙威	1	fuzzy	2025-07-30 02:16:34.063927	2025-07-30 02:16:34.063999	2025-07-30 02:16:34.063999	968348-20250729211539
74	東京華僑	6	exact	2025-07-31 13:10:12.697702	2025-07-31 02:21:42.325478	2025-07-31 13:10:12.697989	636216-20250730233734
55	東京華僑	4	fuzzy	2025-07-31 13:10:16.812164	2025-07-30 23:37:50.148487	2025-07-31 13:10:16.812245	636216-20250730233734
56	中國總商會	2	fuzzy	2025-07-31 13:10:18.213358	2025-07-30 23:37:50.555717	2025-07-31 13:10:18.213475	636216-20250730233734
54	煙台大學	10	fuzzy	2025-07-31 13:10:18.964216	2025-07-30 23:37:49.613557	2025-07-31 13:10:18.964361	636216-20250730233734
109	上海浦東	1	fuzzy	2025-07-31 22:59:40.466044	2025-07-31 22:59:40.466281	2025-07-31 22:59:40.466281	897217-20250731135535
110	台北101	1	fuzzy	2025-07-31 22:59:41.369389	2025-07-31 22:59:41.369616	2025-07-31 22:59:41.369616	897217-20250731135535
111	台灣大學	1	fuzzy	2025-07-31 22:59:42.050062	2025-07-31 22:59:42.050488	2025-07-31 22:59:42.050488	897217-20250731135535
112	北大	1	fuzzy	2025-07-31 22:59:42.585094	2025-07-31 22:59:42.585196	2025-07-31 22:59:42.585196	897217-20250731135535
113	中華民國	1	fuzzy	2025-07-31 22:59:43.24957	2025-07-31 22:59:43.249825	2025-07-31 22:59:43.249825	897217-20250731135535
114	中國總商會	1	fuzzy	2025-07-31 22:59:44.127585	2025-07-31 22:59:44.127758	2025-07-31 22:59:44.127758	897217-20250731135535
115	東京華僑	1	fuzzy	2025-07-31 22:59:44.465416	2025-07-31 22:59:44.465626	2025-07-31 22:59:44.465626	897217-20250731135535
116	煙台大學	1	fuzzy	2025-07-31 22:59:44.827898	2025-07-31 22:59:44.82804	2025-07-31 22:59:44.82804	897217-20250731135535
117	上海市	1	fuzzy	2025-07-31 22:59:45.422611	2025-07-31 22:59:45.422808	2025-07-31 22:59:45.422808	897217-20250731135535
19	台灣大學	1	exact	2025-07-30 21:31:49.013432	2025-07-30 21:31:49.013653	2025-07-30 21:31:49.013653	968348-20250729211539
118	台科大	1	fuzzy	2025-07-31 22:59:45.819511	2025-07-31 22:59:45.819783	2025-07-31 22:59:45.819783	897217-20250731135535
12	上海市	2	fuzzy	2025-07-30 21:31:51.487724	2025-07-30 21:26:58.368446	2025-07-30 21:31:51.488073	968348-20250729211539
121	台北101	1	fuzzy	2025-08-01 00:54:24.621884	2025-08-01 00:54:24.622163	2025-08-01 00:54:24.622163	577050-20250731141511
123	北大	1	fuzzy	2025-08-01 00:54:25.286392	2025-08-01 00:54:25.286611	2025-08-01 00:54:25.286611	577050-20250731141511
126	中國總商會	1	fuzzy	2025-08-01 00:54:26.796329	2025-08-01 00:54:26.796573	2025-08-01 00:54:26.796573	577050-20250731141511
120	東京華僑	2	fuzzy	2025-08-01 00:54:27.370622	2025-08-01 00:54:23.564305	2025-08-01 00:54:27.370856	577050-20250731141511
128	煙台大學	1	fuzzy	2025-08-01 00:54:27.695645	2025-08-01 00:54:27.696043	2025-08-01 00:54:27.696043	577050-20250731141511
129	台科大	1	fuzzy	2025-08-01 00:54:28.385475	2025-08-01 00:54:28.385709	2025-08-01 00:54:28.385709	577050-20250731141511
130	上海市	1	fuzzy	2025-08-01 00:54:28.693173	2025-08-01 00:54:28.693452	2025-08-01 00:54:28.693452	577050-20250731141511
11	台科大	4	fuzzy	2025-07-30 23:23:41.608835	2025-07-30 21:26:57.173406	2025-07-30 23:23:41.609001	968348-20250729211539
23	北大	2	fuzzy	2025-07-30 23:23:42.323474	2025-07-30 21:31:52.456111	2025-07-30 23:23:42.323921	968348-20250729211539
131	北大	2	exact	2025-08-01 10:56:10.929526	2025-08-01 10:56:10.215496	2025-08-01 10:56:10.929739	577050-20250731141511
122	上海浦東	3	fuzzy	2025-08-01 10:56:14.936221	2025-08-01 00:54:24.958365	2025-08-01 10:56:14.936452	577050-20250731141511
124	台灣大學	2	fuzzy	2025-08-01 10:56:16.223486	2025-08-01 00:54:25.641487	2025-08-01 10:56:16.223637	577050-20250731141511
6	0	9	fuzzy	2025-07-30 23:23:57.5067	2025-07-30 02:22:23.882377	2025-07-30 23:23:57.506848	968348-20250729211539
125	中華民國	2	fuzzy	2025-08-01 10:56:17.261311	2025-08-01 00:54:25.91421	2025-08-01 10:56:17.261496	577050-20250731141511
137	台科大	1	exact	2025-08-01 11:24:28.642604	2025-08-01 11:24:28.642877	2025-08-01 11:24:28.642877	577050-20250731141511
20	北大	5	exact	2025-07-30 23:25:23.105773	2025-07-30 21:31:49.437053	2025-07-30 23:25:23.106044	968348-20250729211539
15	煙台大學	2	exact	2025-07-30 23:25:24.366465	2025-07-30 21:31:46.249846	2025-07-30 23:25:24.366667	968348-20250729211539
16	東京華僑	3	exact	2025-07-30 23:25:24.995008	2025-07-30 21:31:47.424667	2025-07-30 23:25:24.995268	968348-20250729211539
18	台科大	2	exact	2025-07-30 23:25:25.454978	2025-07-30 21:31:48.321033	2025-07-30 23:25:25.455176	968348-20250729211539
138	上海市	1	exact	2025-08-01 11:24:29.273584	2025-08-01 11:24:29.273877	2025-08-01 11:24:29.273877	577050-20250731141511
139	煙台大學	1	exact	2025-08-01 11:24:29.903708	2025-08-01 11:24:29.904002	2025-08-01 11:24:29.904002	577050-20250731141511
7	0	9	exact	2025-07-30 23:25:26.86377	2025-07-30 02:22:30.629939	2025-07-30 23:25:26.863828	968348-20250729211539
14	上海市	4	exact	2025-07-30 23:25:53.620487	2025-07-30 21:31:45.692807	2025-07-30 23:25:53.623115	968348-20250729211539
52	東京華僑	1	fuzzy	2025-07-30 23:25:55.599732	2025-07-30 23:25:55.600128	2025-07-30 23:25:55.600128	968348-20250729211539
10	煙台大學	4	fuzzy	2025-07-30 23:25:55.976251	2025-07-30 21:26:29.196093	2025-07-30 23:25:55.976396	968348-20250729211539
119	東京華僑	2	exact	2025-08-01 11:24:30.753056	2025-08-01 00:54:21.25422	2025-08-01 11:24:30.753219	577050-20250731141511
57	上海市	2	fuzzy	2025-07-31 01:35:08.940989	2025-07-30 23:37:50.981215	2025-07-31 01:35:08.943417	636216-20250730233734
58	台科大	2	fuzzy	2025-07-31 01:35:09.377292	2025-07-30 23:37:51.293586	2025-07-31 01:35:09.377539	636216-20250730233734
141	北大	13	exact	2025-08-02 20:52:52.103217	2025-08-02 19:34:38.662021	2025-08-02 20:52:52.104503	p250802183809016115
147	北大	2	fuzzy	2025-08-02 20:56:49.420977	2025-08-02 19:52:59.817917	2025-08-02 20:56:49.422384	p250802183809016115
84	中華民國	1	exact	2025-07-31 02:21:46.882475	2025-07-31 02:21:46.882733	2025-07-31 02:21:46.882733	636216-20250730233734
85	台灣大學	1	exact	2025-07-31 02:21:47.261553	2025-07-31 02:21:47.261842	2025-07-31 02:21:47.261842	636216-20250730233734
62	北大	6	exact	2025-07-31 02:21:47.661699	2025-07-31 01:39:23.539365	2025-07-31 02:21:47.661956	636216-20250730233734
82	上海浦東	2	exact	2025-07-31 02:21:48.02583	2025-07-31 02:21:46.107965	2025-07-31 02:21:48.026079	636216-20250730233734
88	台北101	1	exact	2025-07-31 02:21:48.573576	2025-07-31 02:21:48.57376	2025-07-31 02:21:48.57376	636216-20250730233734
68	台科大	4	exact	2025-07-31 02:22:05.315174	2025-07-31 02:10:13.936478	2025-07-31 02:22:05.315358	636216-20250730233734
76	上海市	5	exact	2025-07-31 02:22:06.078526	2025-07-31 02:21:43.141982	2025-07-31 02:22:06.078714	636216-20250730233734
75	煙台大學	5	exact	2025-07-31 02:22:06.604345	2025-07-31 02:21:42.756746	2025-07-31 02:22:06.604637	636216-20250730233734
73	中國總商會	4	exact	2025-07-31 02:22:08.241201	2025-07-31 02:21:41.868248	2025-07-31 02:22:08.241429	636216-20250730233734
\.


--
-- Data for Name: search_logs; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.search_logs (id, keyword, search_type, result_count, search_time, ip_address, user_agent, project_id) FROM stdin;
\.


--
-- Data for Name: sync_error_log; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.sync_error_log (id, source_id, source_table, error_message, error_time, created_at) FROM stdin;
\.


--
-- Data for Name: sync_log; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.sync_log (id, source_id, source_table, status, sync_time, created_at) FROM stdin;
\.


--
-- Data for Name: sync_status; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.sync_status (id, last_sync_time, status, created_at) FROM stdin;
\.


--
-- Data for Name: user_favorites; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_favorites (id, person_id, person_name, last_viewed_time, favorited_at, created_at, updated_at, project_id, user_id) FROM stdin;
1	3	項依潔	\N	2025-07-30 01:50:23.387636	2025-07-30 01:50:23.387636	2025-08-02 01:35:32.184665	968348-20250729211539	admin_default
2	2	趙威	\N	2025-07-30 01:50:24.137089	2025-07-30 01:50:24.137089	2025-08-02 01:35:32.184665	968348-20250729211539	admin_default
5	4	李光	\N	2025-07-30 23:24:08.632393	2025-07-30 23:24:08.632393	2025-08-02 01:35:32.184665	968348-20250729211539	admin_default
6	15	項依潔	\N	2025-07-31 01:35:19.077568	2025-07-31 01:35:19.077568	2025-08-02 01:35:32.184665	636216-20250730233734	admin_default
\.


--
-- Data for Name: user_favorites_backup_20250802; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_favorites_backup_20250802 (id, person_id, person_name, last_viewed_time, favorited_at, created_at, updated_at, project_id, user_id) FROM stdin;
1	3	項依潔	\N	2025-07-30 01:50:23.387636	2025-07-30 01:50:23.387636	2025-08-02 01:35:32.184665	968348-20250729211539	admin_default
2	2	趙威	\N	2025-07-30 01:50:24.137089	2025-07-30 01:50:24.137089	2025-08-02 01:35:32.184665	968348-20250729211539	admin_default
5	4	李光	\N	2025-07-30 23:24:08.632393	2025-07-30 23:24:08.632393	2025-08-02 01:35:32.184665	968348-20250729211539	admin_default
6	15	項依潔	\N	2025-07-31 01:35:19.077568	2025-07-31 01:35:19.077568	2025-08-02 01:35:32.184665	636216-20250730233734	admin_default
\.


--
-- Data for Name: user_permissions; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_permissions (user_id, permission_id, granted_at, granted_by, expires_at) FROM stdin;
\.


--
-- Data for Name: user_roles; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_roles (user_id, role_id, assigned_at, assigned_by) FROM stdin;
b4a60dce-0ad1-4994-bd8a-26f9444595c0	user	2025-08-02 13:36:43.375839	b4a60dce-0ad1-4994-bd8a-26f9444595c0
admin_default	admin	2025-08-02 13:36:43.375839	admin_default
\.


--
-- Data for Name: user_tokens; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_tokens (id, user_id, token_type, token_hash, expires_at, created_at, used_at) FROM stdin;
04401d1b-f510-406e-8d8b-c263281d989b	admin_default	refresh	d13eca9dac1323196dd5d8d71b924d9c9c827fc1973e94621d6e4440d31807ea	2025-08-09 02:30:29.026381	2025-08-02 02:30:29.028873	\N
a523e4b4-10ee-48d2-a41a-90ea35e75c21	admin_default	refresh	910452e5b997288a619b950bebfcd6c46515aed3c9d25d2535691ed93a0cc40c	2025-08-09 02:31:17.962719	2025-08-02 02:31:17.963028	\N
2366016f-8aa2-4eae-a507-bb0e7f62829c	admin_default	refresh	9cd4c964af5542885fb65121742b256c4d70ef6d9611da01d73ee7cae91f31db	2025-08-09 02:31:41.897043	2025-08-02 02:31:41.897335	2025-08-02 10:50:16.000465
c44484f5-12e0-4471-9bbf-d30f535c90cc	admin_default	refresh	f772951bc2a1efcec98450036e892d93db63729c9d10b4c4ece3d2a744344531	2025-08-09 10:50:29.674515	2025-08-02 10:50:29.675069	\N
9abb2695-f3c5-4ea1-a216-5fb979fc261e	admin_default	refresh	010fc78d89bc2d8f6a810b45bac17322a4dfd54396de64fe297389edd92c75e6	2025-08-09 10:51:03.870062	2025-08-02 10:51:03.870374	2025-08-02 11:05:03.918329
7cb9ccbd-81bc-4cc0-a05f-c2d5832a3914	admin_default	refresh	e3b5f353ba93bd2931ab159ada5c7c67db8b5cba4613ca85beaf0d22ed1407bc	2025-08-09 11:22:44.26223	2025-08-02 11:22:44.262552	\N
aa9265c6-42d4-407f-9778-a8b2fbc00c9a	b4a60dce-0ad1-4994-bd8a-26f9444595c0	refresh	dafda68c60458b8cd1ec551e521450c202baa8743a42fcf986f8955191ab7214	2025-08-09 11:25:01.266729	2025-08-02 11:25:01.266967	\N
576a8f6c-731f-4cee-b38b-26ba6dd2d725	admin_default	refresh	26dfa4ce0dc6644a9456cca30ff0a9a5d9d7233ed24ff4931091f1035b10745b	2025-08-09 11:25:20.878099	2025-08-02 11:25:20.878353	2025-08-02 11:40:39.942232
063802ea-d462-4155-8547-8aff1b43631e	admin_default	refresh	4e9c39bfdcc42ff26bb6993acdf853a816340638938842dd2fba21e8475873d2	2025-08-09 11:40:44.538303	2025-08-02 11:40:44.538721	2025-08-02 12:19:37.869248
99be0f63-a905-4c26-9098-de13a223ff86	admin_default	refresh	ab14f29d134e6d508df7a256e47c04b493fc122e4077de20079c7e1c22eaa4a8	2025-08-09 12:20:35.395551	2025-08-02 12:20:35.395849	\N
76668a2b-d533-4139-9a0b-fd1c83e9a15c	admin_default	refresh	da6489ce605bf539487fa24695486d8624b0e6e15d88e11113abb72f3c5f5d97	2025-08-09 12:24:08.115165	2025-08-02 12:24:08.115586	\N
517ecd5d-7c8c-4c09-89e6-c911e1b0836f	admin_default	refresh	ae8ba84d38a20fbb4b3bb81c9231f730967a6963faf314ace46b44065a7c1b44	2025-08-09 12:26:22.726672	2025-08-02 12:26:22.727102	\N
ab12968e-320b-46c9-adc7-fb55c4e0b269	admin_default	refresh	964c5eceeba955607848a324926eea8c3039f040f7c188ae91a703e673325259	2025-08-09 12:33:31.054642	2025-08-02 12:33:31.057043	\N
f14a9e32-5528-4a8f-8ebe-b02c8bf79d43	admin_default	refresh	a220d974cd9ccf2f690b6cb2919f72e453b0385944e542d8a53718d2b42d37e0	2025-08-09 12:35:32.459628	2025-08-02 12:35:32.459962	\N
dc303e1b-a9b4-469f-8736-1d15854cd1a7	admin_default	refresh	cfc3e114978c50bf43b30cbf65a044ab4c9d6b68e9da17ce676cf06a5a74a6c9	2025-08-09 12:35:59.963376	2025-08-02 12:35:59.965708	\N
d5e6b88c-12e1-46c2-bb06-568a9ee86727	admin_default	refresh	f7794050a6c57e5860ec8e65e26a9e685c4a11fba69a1064f7173b62271d5e14	2025-08-09 12:40:50.507344	2025-08-02 12:40:50.50766	\N
7750cc47-5383-4779-b5a7-4fddb3bb291c	admin_default	refresh	dd9229d3f4518f9591681175b6498b8ac8abbd2bd3278e1d3a31b1ab23c19f2c	2025-08-09 12:51:28.038939	2025-08-02 12:51:28.041071	\N
3a8de602-ed37-46c0-ae6b-f3b848b585d7	admin_default	refresh	9f4b8b212a2df3cf2f0a29b0a126fb7a374084ebd8bdaac651e0e6e54d026c38	2025-08-09 13:41:36.126692	2025-08-02 13:41:36.128627	2025-08-02 14:34:16.479251
7e29795b-fafd-48eb-a751-2969ef6e95da	admin_default	refresh	247d561231b00b23517cec7cacbd71a2b7bc502701056011b9c9ecb13c81a241	2025-08-09 14:34:50.414673	2025-08-02 14:34:50.416603	2025-08-02 14:52:18.927966
67ada374-d400-4c6a-8970-2d3c739ec60a	admin_default	refresh	1fc710e611d0a4b51fdb8ad1aab7106462013199840b4b48c980600d041e5002	2025-08-09 15:02:08.302941	2025-08-02 15:02:08.303118	\N
65547ab7-6467-4283-8647-3095e57aa6a1	admin_default	refresh	89e6fc23ab2662f87c44d4deefa97c4b98530e839f94087ed73dd79ec8b25d07	2025-08-09 15:09:36.60378	2025-08-02 15:09:36.604013	\N
3c277cde-bb27-4d35-a886-4a5b6f1a2573	admin_default	refresh	3fe866bf9f4a80084fb9298f8b746483806056758f106a33005197e626256c82	2025-08-09 15:11:24.954288	2025-08-02 15:11:24.954456	\N
a43ea5f4-db14-4904-bcc9-0d1bbb1b9f01	admin_default	refresh	14edb875c90c112aa127564133553af37bfd2e61964083c6421571c45bdf79b0	2025-08-09 15:15:39.806823	2025-08-02 15:15:39.807113	\N
73de67bd-f149-40b3-83dd-bd61d176e598	admin_default	refresh	6d8f9e61d36eb343f261a248a51198bd06898ce9dfc92c4402922641abdfbc26	2025-08-09 16:21:39.021387	2025-08-02 16:21:39.023312	\N
4025975c-fd24-4540-b18d-9da0fae1b5a1	admin_default	refresh	3e43e76383eba01c3a9daa4871d7200b19317ad3b6900f454a6e5f9e3eb9db20	2025-08-09 16:21:56.84634	2025-08-02 16:21:56.846535	\N
6d960dab-b33a-47ff-9b83-531ae33b9e9f	admin_default	refresh	b20259981a0aac648daad16b86bbd4b431dfcbed023446157495a86eef8cdf52	2025-08-09 16:22:27.989984	2025-08-02 16:22:27.990156	\N
7e250ac4-3f19-43bb-9142-2f35b1c2f65e	admin_default	refresh	c1d83be1eff91cc3051fdce2ce5121eab6a5bdf016de572df059cabb2d9a63e3	2025-08-09 16:22:41.982465	2025-08-02 16:22:41.9827	\N
ac4b4b1a-def1-457c-973b-b2f399648f21	admin_default	refresh	5afe4ecc98307855cf723ae78633e890a7204a32ffa0e9b680f3a9482750ff1c	2025-08-09 16:27:47.136316	2025-08-02 16:27:47.136498	\N
866cd8ea-893a-4fa7-bb79-2d8b246524b8	admin_default	refresh	b20bb152eba36d705f6cffd71a9b4bf23e3d54eb98bf96874fcb6ab344562869	2025-08-09 16:32:55.396911	2025-08-02 16:32:55.398928	2025-08-02 17:13:21.867069
bb8f9ee7-a93b-40bd-803f-c02087f30132	admin_default	refresh	227c7e4ae3411768d79dc80bf0bfe6c19f22b9cfdf9012abe2c8fe00799590a4	2025-08-09 18:01:55.266663	2025-08-02 18:01:55.267055	\N
25a48912-1ead-4142-8541-84f9185110a5	admin_default	refresh	c3e5e1fe756ca825c78913fe816bf9e54aae2f1fb2fdbfccf26f8e03dde2dbc8	2025-08-09 18:02:08.034102	2025-08-02 18:02:08.034222	\N
c7f3ebed-ab8f-4cb4-9b02-44c7fceacab6	admin_default	refresh	b08b18b5d1c8a170fd72f445ca139037c2600732508a5ca8b144fe53c1a5fb44	2025-08-09 18:11:50.692249	2025-08-02 18:11:50.694126	\N
02c2d10c-fbf7-4078-8220-59dbcb42b38e	admin_default	refresh	85a05cf2ae22781473ada03d95082597dd5338e54d66a75350582a928089c60e	2025-08-09 18:17:12.111098	2025-08-02 18:17:12.113091	\N
dde9f436-cfcd-49da-bcd3-e40cc9ddb4f2	admin_default	refresh	5d5bf6e1a44edcf1e34d96c3cbdab9de6b06e95853c9c275c56e716396b7ff5b	2025-08-09 18:25:16.127343	2025-08-02 18:25:16.129275	\N
8b787984-f2e2-465d-83c1-00fe8599f23e	admin_default	refresh	abcd495b9c88dad062bbb1c4b9fa692adddb9fbc6c49dccae6bdda4fb3da338d	2025-08-09 18:29:47.986835	2025-08-02 18:29:47.987168	2025-08-02 18:48:03.101774
c72ce2c6-7b7e-4e45-aac1-090a1c8dd6e5	admin_default	refresh	9d35b791ac9fd9e0089209eaf13ed157deaae8f1d8d024496bf0840331078d93	2025-08-09 18:48:08.159282	2025-08-02 18:48:08.160406	2025-08-02 19:02:53.020065
207825d4-9d3e-46bf-a368-ec66193b052b	admin_default	refresh	82c0eb1e7e8c52026481cdb3cd89bc14997b14d2fe7a06b2a205115603255674	2025-08-09 19:02:55.977509	2025-08-02 19:02:55.978659	2025-08-02 19:16:55.932766
20140bbe-a67e-475b-a675-ea9dd7afd6b7	admin_default	refresh	a9e14401a37a006d5cc1b7a6f3207fafbfcedbec52109817ba3ba764200f82ee	2025-08-09 19:17:06.613735	2025-08-02 19:17:06.614934	\N
83efc140-ef8a-47b2-af2e-0d21d5a6f454	admin_default	refresh	9ef43668408d0cd686f69bce97a022334aa14aeed468d7ad36bddf21472e3715	2025-08-09 19:23:56.657935	2025-08-02 19:23:56.659785	2025-08-02 19:43:13.697632
c6352fd3-b486-468a-8d10-1e48bd6106e6	admin_default	refresh	23e10e50e71831233d6ef348087e41a248649f8c927256ed2863eb8b627eb1ce	2025-08-09 19:43:16.637626	2025-08-02 19:43:16.638796	2025-08-02 19:58:30.630383
8b08ed1a-1d15-4a5d-9fec-cb010e536894	admin_default	refresh	10add0ba90e100120f5834fb0b758fe77c6af5974f9850b66e9f10472e75ee1b	2025-08-09 19:58:33.456154	2025-08-02 19:58:33.456841	2025-08-02 20:12:33.508027
07894553-daf3-4539-9285-97834473742b	admin_default	refresh	eff5914fc5aeb64c69df45fc897db72fe683a9a8f38f1f6a572c43d74607e68d	2025-08-09 20:23:26.11531	2025-08-02 20:23:26.117282	2025-08-02 20:43:48.715791
e2570ed8-1a29-490f-9a6c-de323d64a2d5	admin_default	refresh	f94b3ca44cad00dca387ce19de1c3e28e5208da7ca2d905f5097cab488d1aff8	2025-08-09 20:43:55.871976	2025-08-02 20:43:55.872127	\N
ddf93ad2-eae9-432a-8bc5-3f003b706dac	admin_default	refresh	2c0cc2ece25397459eded9a0f548775cf6c7cd1ff1c45155b37defcd379f3b29	2025-08-09 20:45:56.582781	2025-08-02 20:45:56.583833	\N
63ebf20f-7d12-4e0f-953c-8c3df4d09c67	admin_default	refresh	f7dbeb0e6191351a6b6eea77be05e6821b38464455bc5f8a3a9c3a80a8014964	2025-08-09 20:52:48.34059	2025-08-02 20:52:48.340825	2025-08-02 21:10:53.025467
ff5c3d01-5603-4252-8704-30e1cf00675c	admin_default	refresh	4ed8f5bb3310dd75d596851b68a099240ea1b7f7709bd703b80b64c3f4a3880f	2025-08-09 21:10:55.137161	2025-08-02 21:10:55.13839	\N
e6bb844c-dc5a-450f-8465-977f63f8c24d	admin_default	refresh	7048d94eb669fb2738a23a1c6687b9c3eb838571d88f55ff2b3911672d8ddd74	2025-08-09 21:12:17.445825	2025-08-02 21:12:17.447834	\N
55a20621-e501-4efa-af03-b3a9925e82ef	admin_default	refresh	8b9e105c83f8569c3e26e44ad992c87cfbca0230141664fc527273a17dd52e90	2025-08-09 21:21:29.023828	2025-08-02 21:21:29.0258	\N
146c824f-864c-4d97-8481-659b25a7d473	admin_default	refresh	448edda8496e257400fc67fbda2a21fff582ea3db276e3cbfd5bb3c0ac9526e4	2025-08-09 21:30:41.589426	2025-08-02 21:30:41.590644	\N
\.


--
-- Data for Name: user_update_file_archived_20250802; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_update_file_archived_20250802 (id, filename, original_filename, file_path, file_size, md5_hash, upload_time, is_merged, merge_time, status, created_at, updated_at, project_id) FROM stdin;
1	分公司客戶基資表-廠商測試版_20250729_131546_ac400430.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree-1/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250729_131546_ac400430.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-07-29 21:15:46.787473	t	2025-08-02 18:38:34.702097	merged	2025-07-29 21:15:46.789843	2025-08-02 18:38:34.702505	968348-20250729211539
2	分公司客戶基資表-廠商測試版_20250730_153900_e87377f3.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250730_153900_e87377f3.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-07-30 23:39:00.198591	t	2025-08-02 18:38:34.702097	merged	2025-07-30 23:39:00.201013	2025-08-02 18:38:34.702505	636216-20250730233734
3	分公司客戶基資表-廠商測試版_20250802_054819_4c6befd5.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250802_054819_4c6befd5.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-08-02 13:48:19.918347	t	2025-08-02 18:38:34.702097	merged	2025-08-02 13:48:19.921969	2025-08-02 18:38:34.702505	577050-20250731141511
4	分公司客戶基資表-廠商測試版_20250802_103834_3209086d.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250802_103834_3209086d.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-08-02 18:38:34.504623	t	2025-08-02 18:38:34.702097	merged	2025-08-02 18:38:34.507034	2025-08-02 18:38:34.702505	p250802183809016115
\.


--
-- Data for Name: user_update_file_backup_20250802; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_update_file_backup_20250802 (id, filename, original_filename, file_path, file_size, md5_hash, upload_time, is_merged, merge_time, status, created_at, updated_at, project_id) FROM stdin;
1	分公司客戶基資表-廠商測試版_20250729_131546_ac400430.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree-1/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250729_131546_ac400430.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-07-29 21:15:46.787473	t	2025-08-02 13:48:20.082628	merged	2025-07-29 21:15:46.789843	2025-08-02 13:48:20.083042	968348-20250729211539
2	分公司客戶基資表-廠商測試版_20250730_153900_e87377f3.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250730_153900_e87377f3.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-07-30 23:39:00.198591	t	2025-08-02 13:48:20.082628	merged	2025-07-30 23:39:00.201013	2025-08-02 13:48:20.083042	636216-20250730233734
3	分公司客戶基資表-廠商測試版_20250802_054819_4c6befd5.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250802_054819_4c6befd5.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-08-02 13:48:19.918347	t	2025-08-02 13:48:20.082628	merged	2025-08-02 13:48:19.921969	2025-08-02 13:48:20.083042	577050-20250731141511
\.


--
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.users (id, username, email, password_hash, full_name, role, status, created_at, updated_at, last_login_at) FROM stdin;
b4a60dce-0ad1-4994-bd8a-26f9444595c0	user	andyyan04870449@yahoo.com.tw	$2a$11$nP.OB3Wif6e.ef3oRH1n0OJwX4vf/eusVa.JorGq489VESZSmP1Ze	Andy	user	active	2025-08-02 11:23:12.750533	2025-08-02 11:25:01.269102	2025-08-02 11:25:01.269102
admin_default	admin	admin@familytree.com	$2a$11$xxZ5aAl1UnpG2pvpofnFUuHgGftaxKJbUdZRV8iIOHZh4Zfb9vzIi	系統管理員	admin	active	2025-08-02 01:34:47.377729	2025-08-02 21:30:41.593909	2025-08-02 21:30:41.593909
\.


--
-- Data for Name: visual_analysis_graphs; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.visual_analysis_graphs (id, name, project_ids, updated_by, updated_at) FROM stdin;
2	test	968348-20250729211539	user	2025-07-30 01:57:06.162035
4	123	636216-20250730233734	user	2025-07-31 00:17:12.074707
\.


--
-- Data for Name: visual_analysis_nodes; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.visual_analysis_nodes (id, graph_id, project_id, person_id, is_visible, node_x, node_y, created_at, updated_at) FROM stdin;
25	4	636216-20250730233734	18	t	0	0	2025-07-31 22:56:25.331213	2025-07-31 22:56:25.331213
26	4	636216-20250730233734	24	t	0	0	2025-07-31 22:56:25.33805	2025-07-31 22:56:25.33805
27	4	636216-20250730233734	16	t	0	0	2025-07-31 22:56:25.338432	2025-07-31 22:56:25.338432
28	4	636216-20250730233734	20	t	0	0	2025-07-31 22:56:25.338748	2025-07-31 22:56:25.338748
29	4	636216-20250730233734	19	t	0	0	2025-07-31 22:56:25.340216	2025-07-31 22:56:25.340216
30	4	636216-20250730233734	17	t	0	0	2025-07-31 22:56:25.340568	2025-07-31 22:56:25.340568
31	4	636216-20250730233734	23	t	0	0	2025-07-31 22:56:25.340925	2025-07-31 22:56:25.340925
32	4	636216-20250730233734	13	t	0	0	2025-07-31 22:56:25.341272	2025-07-31 22:56:25.341272
33	4	636216-20250730233734	14	t	0	0	2025-07-31 22:56:25.341576	2025-07-31 22:56:25.341576
34	4	636216-20250730233734	21	t	0	0	2025-07-31 22:56:25.341906	2025-07-31 22:56:25.341906
35	4	636216-20250730233734	15	t	0	0	2025-07-31 22:56:25.342782	2025-07-31 22:56:25.342782
36	4	636216-20250730233734	22	t	0	0	2025-07-31 22:56:25.343124	2025-07-31 22:56:25.343124
1	2	968348-20250729211539	6	t	0	0	2025-07-30 01:57:08.57471	2025-07-30 21:30:07.315944
2	2	968348-20250729211539	12	t	0	0	2025-07-30 01:57:08.59708	2025-07-30 21:30:07.316846
3	2	968348-20250729211539	4	t	0	0	2025-07-30 01:57:08.597546	2025-07-30 21:30:07.317296
4	2	968348-20250729211539	8	t	0	0	2025-07-30 01:57:08.597834	2025-07-30 21:30:07.31764
5	2	968348-20250729211539	7	t	0	0	2025-07-30 01:57:08.59806	2025-07-30 21:30:07.318015
6	2	968348-20250729211539	5	t	0	0	2025-07-30 01:57:08.598322	2025-07-30 21:30:07.318381
7	2	968348-20250729211539	11	t	0	0	2025-07-30 01:57:08.598544	2025-07-30 21:30:07.318727
8	2	968348-20250729211539	1	t	0	0	2025-07-30 01:57:08.598735	2025-07-30 21:30:07.319038
9	2	968348-20250729211539	2	t	0	0	2025-07-30 01:57:08.598903	2025-07-30 21:30:07.319259
10	2	968348-20250729211539	9	t	0	0	2025-07-30 01:57:08.599126	2025-07-30 21:30:07.319549
11	2	968348-20250729211539	3	t	0	0	2025-07-30 01:57:08.599322	2025-07-30 21:30:07.319757
12	2	968348-20250729211539	10	t	0	0	2025-07-30 01:57:08.599524	2025-07-30 21:30:07.31994
\.


--
-- Name: activity_logs_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.activity_logs_id_seq', 4, true);


--
-- Name: analysis_results_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.analysis_results_id_seq', 1, false);


--
-- Name: audit_change_details_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.audit_change_details_id_seq', 1, false);


--
-- Name: audit_compliance_reports_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.audit_compliance_reports_id_seq', 1, false);


--
-- Name: audit_event_types_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.audit_event_types_id_seq', 29, true);


--
-- Name: audit_log_access_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.audit_log_access_id_seq', 1, false);


--
-- Name: audit_logs_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.audit_logs_id_seq', 83, true);


--
-- Name: data_migration_log_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.data_migration_log_id_seq', 4, true);


--
-- Name: field_mapping_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.field_mapping_id_seq', 81, true);


--
-- Name: mergedpersons_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.mergedpersons_id_seq', 1, false);


--
-- Name: missing_persons_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.missing_persons_id_seq', 1, false);


--
-- Name: permissions_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.permissions_id_seq', 63, true);


--
-- Name: person_profile_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.person_profile_id_seq', 48, true);


--
-- Name: personmergelog_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.personmergelog_id_seq', 1, false);


--
-- Name: photos_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.photos_id_seq', 1, true);


--
-- Name: project_permissions_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.project_permissions_id_seq', 1, false);


--
-- Name: relationship_layers_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.relationship_layers_id_seq', 11, true);


--
-- Name: search_keywords_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.search_keywords_id_seq', 155, true);


--
-- Name: search_logs_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.search_logs_id_seq', 1, false);


--
-- Name: sync_error_log_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.sync_error_log_id_seq', 1, false);


--
-- Name: sync_log_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.sync_log_id_seq', 1, false);


--
-- Name: sync_status_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.sync_status_id_seq', 1, false);


--
-- Name: user_favorites_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.user_favorites_id_seq', 6, true);


--
-- Name: user_update_file_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.user_update_file_id_seq', 4, true);


--
-- Name: visual_analysis_graphs_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.visual_analysis_graphs_id_seq', 5, true);


--
-- Name: visual_analysis_nodes_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.visual_analysis_nodes_id_seq', 36, true);


--
-- Name: activity_logs_backup activity_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.activity_logs_backup
    ADD CONSTRAINT activity_logs_pkey PRIMARY KEY (id);


--
-- Name: analysis_results analysis_results_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_results
    ADD CONSTRAINT analysis_results_pkey PRIMARY KEY (id);


--
-- Name: analysis_sessions analysis_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_sessions
    ADD CONSTRAINT analysis_sessions_pkey PRIMARY KEY (id);


--
-- Name: audit_change_details audit_change_details_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_change_details
    ADD CONSTRAINT audit_change_details_pkey PRIMARY KEY (id);


--
-- Name: audit_compliance_reports audit_compliance_reports_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_compliance_reports
    ADD CONSTRAINT audit_compliance_reports_pkey PRIMARY KEY (id);


--
-- Name: audit_event_types audit_event_types_code_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_event_types
    ADD CONSTRAINT audit_event_types_code_key UNIQUE (code);


--
-- Name: audit_event_types audit_event_types_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_event_types
    ADD CONSTRAINT audit_event_types_pkey PRIMARY KEY (id);


--
-- Name: audit_log_access audit_log_access_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_log_access
    ADD CONSTRAINT audit_log_access_pkey PRIMARY KEY (id);


--
-- Name: audit_logs audit_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_logs
    ADD CONSTRAINT audit_logs_pkey PRIMARY KEY (id);


--
-- Name: data_migration_log data_migration_log_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.data_migration_log
    ADD CONSTRAINT data_migration_log_pkey PRIMARY KEY (id);


--
-- Name: field_mapping field_mapping_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping
    ADD CONSTRAINT field_mapping_pkey PRIMARY KEY (id);


--
-- Name: file_uploads file_uploads_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.file_uploads
    ADD CONSTRAINT file_uploads_pkey PRIMARY KEY (file_id);


--
-- Name: mergedpersons mergedpersons_id_number_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.mergedpersons
    ADD CONSTRAINT mergedpersons_id_number_key UNIQUE (id_number);


--
-- Name: mergedpersons mergedpersons_passport_number_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.mergedpersons
    ADD CONSTRAINT mergedpersons_passport_number_key UNIQUE (passport_number);


--
-- Name: mergedpersons mergedpersons_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.mergedpersons
    ADD CONSTRAINT mergedpersons_pkey PRIMARY KEY (id);


--
-- Name: missing_persons missing_persons_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT missing_persons_pkey PRIMARY KEY (id);


--
-- Name: permissions permissions_permission_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT permissions_permission_key UNIQUE (permission);


--
-- Name: permissions permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.permissions
    ADD CONSTRAINT permissions_pkey PRIMARY KEY (id);


--
-- Name: person_profile person_profile_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.person_profile
    ADD CONSTRAINT person_profile_pkey PRIMARY KEY (id);


--
-- Name: personmergelog personmergelog_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.personmergelog
    ADD CONSTRAINT personmergelog_pkey PRIMARY KEY (id);


--
-- Name: photos photos_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.photos
    ADD CONSTRAINT photos_pkey PRIMARY KEY (id);


--
-- Name: project_permissions project_permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.project_permissions
    ADD CONSTRAINT project_permissions_pkey PRIMARY KEY (id);


--
-- Name: project_permissions project_permissions_user_id_project_id_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.project_permissions
    ADD CONSTRAINT project_permissions_user_id_project_id_key UNIQUE (user_id, project_id);


--
-- Name: projects projects_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.projects
    ADD CONSTRAINT projects_pkey PRIMARY KEY (id);


--
-- Name: relationship_layers relationship_layers_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_pkey PRIMARY KEY (id);


--
-- Name: role_permissions role_permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT role_permissions_pkey PRIMARY KEY (role_id, permission_id);


--
-- Name: roles roles_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (id);


--
-- Name: search_keywords search_keywords_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.search_keywords
    ADD CONSTRAINT search_keywords_pkey PRIMARY KEY (id);


--
-- Name: search_logs search_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.search_logs
    ADD CONSTRAINT search_logs_pkey PRIMARY KEY (id);


--
-- Name: sync_error_log sync_error_log_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.sync_error_log
    ADD CONSTRAINT sync_error_log_pkey PRIMARY KEY (id);


--
-- Name: sync_log sync_log_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.sync_log
    ADD CONSTRAINT sync_log_pkey PRIMARY KEY (id);


--
-- Name: sync_status sync_status_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.sync_status
    ADD CONSTRAINT sync_status_pkey PRIMARY KEY (id);


--
-- Name: field_mapping uk_field_mapping_excel_db; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping
    ADD CONSTRAINT uk_field_mapping_excel_db UNIQUE (excel_field_name, db_field_name);


--
-- Name: photos uk_photos_project_md5; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.photos
    ADD CONSTRAINT uk_photos_project_md5 UNIQUE (project_id, md5_hash);


--
-- Name: search_keywords unique_keyword_project; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.search_keywords
    ADD CONSTRAINT unique_keyword_project UNIQUE (keyword, search_type, project_id);


--
-- Name: user_favorites unique_person_favorite; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_favorites
    ADD CONSTRAINT unique_person_favorite UNIQUE (person_id);


--
-- Name: user_favorites user_favorites_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_favorites
    ADD CONSTRAINT user_favorites_pkey PRIMARY KEY (id);


--
-- Name: user_permissions user_permissions_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_permissions
    ADD CONSTRAINT user_permissions_pkey PRIMARY KEY (user_id, permission_id);


--
-- Name: user_roles user_roles_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT user_roles_pkey PRIMARY KEY (user_id, role_id);


--
-- Name: user_tokens user_tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_tokens
    ADD CONSTRAINT user_tokens_pkey PRIMARY KEY (id);


--
-- Name: user_update_file_archived_20250802 user_update_file_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_update_file_archived_20250802
    ADD CONSTRAINT user_update_file_pkey PRIMARY KEY (id);


--
-- Name: users users_email_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_email_key UNIQUE (email);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: users users_username_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_username_key UNIQUE (username);


--
-- Name: visual_analysis_graphs visual_analysis_graphs_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.visual_analysis_graphs
    ADD CONSTRAINT visual_analysis_graphs_pkey PRIMARY KEY (id);


--
-- Name: visual_analysis_nodes visual_analysis_nodes_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.visual_analysis_nodes
    ADD CONSTRAINT visual_analysis_nodes_pkey PRIMARY KEY (id);


--
-- Name: idx_activity_logs_action; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_activity_logs_action ON public.activity_logs_backup USING btree (action);


--
-- Name: idx_activity_logs_created_at; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_activity_logs_created_at ON public.activity_logs_backup USING btree (created_at);


--
-- Name: idx_activity_logs_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_activity_logs_user_id ON public.activity_logs_backup USING btree (user_id);


--
-- Name: idx_analysis_results_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_analysis_results_user_id ON public.analysis_results USING btree (user_id);


--
-- Name: idx_analysis_sessions_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_analysis_sessions_user_id ON public.analysis_sessions USING btree (user_id);


--
-- Name: idx_audit_change_details_change_type; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_change_details_change_type ON public.audit_change_details USING btree (change_type, field_name, created_at DESC);


--
-- Name: idx_audit_change_details_field; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_change_details_field ON public.audit_change_details USING btree (field_name);


--
-- Name: idx_audit_change_details_log_field; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_change_details_log_field ON public.audit_change_details USING btree (audit_log_id, field_name);


--
-- Name: idx_audit_change_details_log_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_change_details_log_id ON public.audit_change_details USING btree (audit_log_id);


--
-- Name: idx_audit_change_details_sensitive; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_change_details_sensitive ON public.audit_change_details USING btree (is_sensitive);


--
-- Name: idx_audit_compliance_reports_date; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_compliance_reports_date ON public.audit_compliance_reports USING btree (date_from, date_to);


--
-- Name: idx_audit_compliance_reports_date_range; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_compliance_reports_date_range ON public.audit_compliance_reports USING btree (date_from, date_to, report_type);


--
-- Name: idx_audit_compliance_reports_generated; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_compliance_reports_generated ON public.audit_compliance_reports USING btree (generated_by, generated_at DESC);


--
-- Name: idx_audit_compliance_reports_generator; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_compliance_reports_generator ON public.audit_compliance_reports USING btree (generated_by, generated_at DESC);


--
-- Name: idx_audit_compliance_reports_retention; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_compliance_reports_retention ON public.audit_compliance_reports USING btree (retention_until) WHERE (retention_until IS NOT NULL);


--
-- Name: idx_audit_compliance_reports_type; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_compliance_reports_type ON public.audit_compliance_reports USING btree (report_type);


--
-- Name: idx_audit_compliance_reports_type_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_compliance_reports_type_status ON public.audit_compliance_reports USING btree (report_type, status, generated_at DESC);


--
-- Name: idx_audit_log_access_accessor; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_log_access_accessor ON public.audit_log_access USING btree (accessor_user_id, accessed_at DESC);


--
-- Name: idx_audit_log_access_approval; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_log_access_approval ON public.audit_log_access USING btree (approval_required, approved_by);


--
-- Name: idx_audit_log_access_ip; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_log_access_ip ON public.audit_log_access USING btree (accessor_ip, accessed_at DESC) WHERE (accessor_ip IS NOT NULL);


--
-- Name: idx_audit_log_access_resource; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_log_access_resource ON public.audit_log_access USING btree (accessed_resource_type, accessed_resource_id);


--
-- Name: idx_audit_log_access_type; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_log_access_type ON public.audit_log_access USING btree (access_type, accessed_at DESC);


--
-- Name: idx_audit_log_access_user; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_log_access_user ON public.audit_log_access USING btree (accessor_user_id, accessed_at DESC);


--
-- Name: idx_audit_logs_action; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_action ON public.audit_logs USING btree (action);


--
-- Name: idx_audit_logs_additional_data; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_additional_data ON public.audit_logs USING gin (additional_data);


--
-- Name: idx_audit_logs_additional_data_file_info; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_additional_data_file_info ON public.audit_logs USING gin (((additional_data -> 'file'::text))) WHERE (additional_data ? 'file'::text);


--
-- Name: idx_audit_logs_auth_events; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_auth_events ON public.audit_logs USING btree (event_type, user_id, occurred_at DESC, success) WHERE ((event_type)::text = ANY ((ARRAY['LOGIN'::character varying, 'LOGIN_FAILED'::character varying, 'LOGOUT'::character varying, 'PASSWORD_CHANGE'::character varying, 'PASSWORD_RESET'::character varying])::text[]));


--
-- Name: idx_audit_logs_batch; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_batch ON public.audit_logs USING btree (batch_id, occurred_at) WHERE (batch_id IS NOT NULL);


--
-- Name: idx_audit_logs_batch_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_batch_id ON public.audit_logs USING btree (batch_id) WHERE (batch_id IS NOT NULL);


--
-- Name: idx_audit_logs_compliance; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_compliance ON public.audit_logs USING gin (compliance_flags) WHERE ((compliance_flags IS NOT NULL) AND (array_length(compliance_flags, 1) > 0));


--
-- Name: INDEX idx_audit_logs_compliance; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON INDEX public.idx_audit_logs_compliance IS '合規性查詢專用索引，支援合規標記搜尋';


--
-- Name: idx_audit_logs_errors; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_errors ON public.audit_logs USING btree (error_code, event_type, occurred_at DESC) WHERE (error_code IS NOT NULL);


--
-- Name: idx_audit_logs_event_date; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_event_date ON public.audit_logs USING btree (event_type, occurred_at DESC) INCLUDE (user_id, action, success, security_level);


--
-- Name: idx_audit_logs_event_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_event_id ON public.audit_logs USING btree (event_id);


--
-- Name: idx_audit_logs_event_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_event_time ON public.audit_logs USING btree (event_type, occurred_at DESC);


--
-- Name: idx_audit_logs_event_type; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_event_type ON public.audit_logs USING btree (event_type);


--
-- Name: idx_audit_logs_failed_operations; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_failed_operations ON public.audit_logs USING btree (success, event_type, occurred_at DESC) WHERE (success = false);


--
-- Name: idx_audit_logs_ip; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_ip ON public.audit_logs USING btree (ip_address);


--
-- Name: idx_audit_logs_ip_session; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_ip_session ON public.audit_logs USING btree (ip_address, session_id, occurred_at DESC) WHERE (ip_address IS NOT NULL);


--
-- Name: idx_audit_logs_new_values; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_new_values ON public.audit_logs USING gin (new_values);


--
-- Name: idx_audit_logs_new_values_user_info; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_new_values_user_info ON public.audit_logs USING gin (((new_values -> 'user'::text))) WHERE (new_values ? 'user'::text);


--
-- Name: idx_audit_logs_occurred_at; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_occurred_at ON public.audit_logs USING btree (occurred_at DESC);


--
-- Name: idx_audit_logs_old_values; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_old_values ON public.audit_logs USING gin (old_values);


--
-- Name: idx_audit_logs_old_values_user_info; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_old_values_user_info ON public.audit_logs USING gin (((old_values -> 'user'::text))) WHERE (old_values ? 'user'::text);


--
-- Name: idx_audit_logs_resource; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_resource ON public.audit_logs USING btree (resource_type, resource_id);


--
-- Name: idx_audit_logs_resource_date; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_resource_date ON public.audit_logs USING btree (resource_type, resource_id, occurred_at DESC) INCLUDE (action, user_id, success);


--
-- Name: idx_audit_logs_resource_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_resource_time ON public.audit_logs USING btree (resource_type, resource_id, occurred_at DESC);


--
-- Name: idx_audit_logs_response_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_response_time ON public.audit_logs USING btree (response_time_ms DESC, occurred_at DESC) WHERE ((response_time_ms IS NOT NULL) AND (response_time_ms > 1000));


--
-- Name: idx_audit_logs_risk_score; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_risk_score ON public.audit_logs USING btree (risk_score DESC, occurred_at DESC) WHERE (risk_score > 50);


--
-- Name: idx_audit_logs_security; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_security ON public.audit_logs USING btree (security_level, is_suspicious);


--
-- Name: idx_audit_logs_security_monitoring; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_security_monitoring ON public.audit_logs USING btree (security_level, is_suspicious, occurred_at DESC) WHERE (((security_level)::text = ANY ((ARRAY['HIGH'::character varying, 'CRITICAL'::character varying])::text[])) OR (is_suspicious = true));


--
-- Name: INDEX idx_audit_logs_security_monitoring; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON INDEX public.idx_audit_logs_security_monitoring IS '安全監控專用索引，用於檢測高風險和可疑活動';


--
-- Name: idx_audit_logs_session; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_session ON public.audit_logs USING btree (session_id) WHERE (session_id IS NOT NULL);


--
-- Name: idx_audit_logs_success; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_success ON public.audit_logs USING btree (success);


--
-- Name: idx_audit_logs_system_events; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_system_events ON public.audit_logs USING btree (event_type, occurred_at DESC, user_id) WHERE ((event_type)::text ~~ 'SYSTEM_%'::text);


--
-- Name: idx_audit_logs_tags; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_tags ON public.audit_logs USING gin (tags);


--
-- Name: idx_audit_logs_user_date_event; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_user_date_event ON public.audit_logs USING btree (user_id, occurred_at DESC, event_type) INCLUDE (action, success, security_level);


--
-- Name: INDEX idx_audit_logs_user_date_event; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON INDEX public.idx_audit_logs_user_date_event IS '使用者、日期、事件類型複合索引，支援最常見的查詢模式';


--
-- Name: idx_audit_logs_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_user_id ON public.audit_logs USING btree (user_id);


--
-- Name: idx_audit_logs_user_name_lower; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_user_name_lower ON public.audit_logs USING btree (lower((user_name)::text), occurred_at DESC) WHERE (user_name IS NOT NULL);


--
-- Name: idx_audit_logs_user_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_audit_logs_user_time ON public.audit_logs USING btree (user_id, occurred_at DESC);


--
-- Name: idx_favorited_at; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_favorited_at ON public.user_favorites USING btree (favorited_at DESC);


--
-- Name: idx_field_mapping_db_field; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_field_mapping_db_field ON public.field_mapping USING btree (db_field_name);


--
-- Name: idx_field_mapping_excel_field; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_field_mapping_excel_field ON public.field_mapping USING btree (excel_field_name);


--
-- Name: idx_field_mapping_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_field_mapping_user_id ON public.field_mapping USING btree (user_id);


--
-- Name: idx_file_uploads_associated_record; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_file_uploads_associated_record ON public.file_uploads USING btree (associated_record_type, associated_record_id);


--
-- Name: idx_file_uploads_md5_hash; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_file_uploads_md5_hash ON public.file_uploads USING btree (md5_hash);


--
-- Name: idx_file_uploads_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_file_uploads_status ON public.file_uploads USING btree (upload_status);


--
-- Name: idx_file_uploads_uploaded_at; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_file_uploads_uploaded_at ON public.file_uploads USING btree (uploaded_at DESC);


--
-- Name: idx_file_uploads_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_file_uploads_user_id ON public.file_uploads USING btree (user_id);


--
-- Name: idx_file_uploads_user_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_file_uploads_user_status ON public.file_uploads USING btree (user_id, upload_status);


--
-- Name: idx_file_uploads_user_type; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_file_uploads_user_type ON public.file_uploads USING btree (user_id, associated_record_type);


--
-- Name: idx_keyword; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_keyword ON public.search_keywords USING btree (keyword);


--
-- Name: idx_keyword_log; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_keyword_log ON public.search_logs USING btree (keyword);


--
-- Name: idx_last_search_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_last_search_time ON public.search_keywords USING btree (last_search_time DESC);


--
-- Name: idx_last_viewed_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_last_viewed_time ON public.user_favorites USING btree (last_viewed_time DESC);


--
-- Name: idx_missing_persons_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_missing_persons_user_id ON public.missing_persons USING btree (user_id);


--
-- Name: idx_person_fulltext_search; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_fulltext_search ON public.person_profile USING btree (name, mobile, phone, id_number, passport_number);


--
-- Name: idx_person_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_id ON public.user_favorites USING btree (person_id);


--
-- Name: idx_person_id_number_search; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_id_number_search ON public.person_profile USING btree (id_number);


--
-- Name: idx_person_mobile_search; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_mobile_search ON public.person_profile USING btree (mobile);


--
-- Name: idx_person_name; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_name ON public.user_favorites USING btree (person_name);


--
-- Name: idx_person_name_search; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_name_search ON public.person_profile USING btree (name);


--
-- Name: idx_person_passport_search; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_passport_search ON public.person_profile USING btree (passport_number);


--
-- Name: idx_person_phone_search; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_phone_search ON public.person_profile USING btree (phone);


--
-- Name: idx_person_profile_file_md5; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_profile_file_md5 ON public.person_profile USING btree (file_md5);


--
-- Name: idx_person_profile_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_profile_project_id ON public.person_profile USING btree (project_id);


--
-- Name: idx_person_profile_source; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_profile_source ON public.person_profile USING btree (source_id, source_table);


--
-- Name: idx_person_profile_source_file_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_profile_source_file_id ON public.person_profile USING btree (source_file_id);


--
-- Name: idx_person_profile_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_person_profile_user_id ON public.person_profile USING btree (user_id);


--
-- Name: idx_photos_deleted_at; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_photos_deleted_at ON public.photos USING btree (deleted_at);


--
-- Name: idx_photos_md5_hash; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_photos_md5_hash ON public.photos USING btree (md5_hash);


--
-- Name: idx_photos_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_photos_project_id ON public.photos USING btree (project_id);


--
-- Name: idx_photos_project_md5; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_photos_project_md5 ON public.photos USING btree (project_id, md5_hash);


--
-- Name: idx_photos_upload_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_photos_upload_time ON public.photos USING btree (upload_time);


--
-- Name: idx_project_permissions_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_project_permissions_project_id ON public.project_permissions USING btree (project_id);


--
-- Name: idx_project_permissions_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_project_permissions_user_id ON public.project_permissions USING btree (user_id);


--
-- Name: idx_projects_created_at; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_projects_created_at ON public.projects USING btree (created_at);


--
-- Name: idx_projects_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_projects_status ON public.projects USING btree (status);


--
-- Name: idx_projects_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_projects_user_id ON public.projects USING btree (user_id);


--
-- Name: idx_relationship_layers_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_relationship_layers_project_id ON public.relationship_layers USING btree (project_id);


--
-- Name: idx_relationship_layers_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_relationship_layers_user_id ON public.relationship_layers USING btree (user_id);


--
-- Name: idx_relationship_layers_visual_analysis_graph_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_relationship_layers_visual_analysis_graph_id ON public.relationship_layers USING btree (visual_analysis_graph_id);


--
-- Name: idx_result_count; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_result_count ON public.search_logs USING btree (result_count);


--
-- Name: idx_role_permissions_role_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_role_permissions_role_id ON public.role_permissions USING btree (role_id);


--
-- Name: idx_search_count; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_search_count ON public.search_keywords USING btree (search_count DESC);


--
-- Name: idx_search_keywords_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_search_keywords_project_id ON public.search_keywords USING btree (project_id);


--
-- Name: idx_search_logs_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_search_logs_project_id ON public.search_logs USING btree (project_id);


--
-- Name: idx_search_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_search_time ON public.search_logs USING btree (search_time DESC);


--
-- Name: idx_source_person; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_source_person ON public.relationship_layers USING btree (source_person_id);


--
-- Name: idx_sync_error_source; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_sync_error_source ON public.sync_error_log USING btree (source_id, source_table);


--
-- Name: idx_sync_log_source; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_sync_log_source ON public.sync_log USING btree (source_id, source_table);


--
-- Name: idx_target_person; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_target_person ON public.relationship_layers USING btree (target_person_id);


--
-- Name: idx_user_favorites_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_favorites_project_id ON public.user_favorites USING btree (project_id);


--
-- Name: idx_user_favorites_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_favorites_user_id ON public.user_favorites USING btree (user_id);


--
-- Name: idx_user_permissions_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_permissions_user_id ON public.user_permissions USING btree (user_id);


--
-- Name: idx_user_roles_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_roles_user_id ON public.user_roles USING btree (user_id);


--
-- Name: idx_user_tokens_expires_at; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_tokens_expires_at ON public.user_tokens USING btree (expires_at);


--
-- Name: idx_user_tokens_token_hash; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_tokens_token_hash ON public.user_tokens USING btree (token_hash);


--
-- Name: idx_user_tokens_user_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_tokens_user_id ON public.user_tokens USING btree (user_id);


--
-- Name: idx_user_update_file_md5; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_update_file_md5 ON public.user_update_file_archived_20250802 USING btree (md5_hash);


--
-- Name: idx_user_update_file_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_update_file_project_id ON public.user_update_file_archived_20250802 USING btree (project_id);


--
-- Name: idx_user_update_file_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_update_file_status ON public.user_update_file_archived_20250802 USING btree (status);


--
-- Name: idx_user_update_file_upload_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_update_file_upload_time ON public.user_update_file_archived_20250802 USING btree (upload_time);


--
-- Name: idx_users_email; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_users_email ON public.users USING btree (email);


--
-- Name: idx_users_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_users_status ON public.users USING btree (status);


--
-- Name: idx_users_username; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_users_username ON public.users USING btree (username);


--
-- Name: idx_visual_analysis_name; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_visual_analysis_name ON public.visual_analysis_graphs USING btree (name);


--
-- Name: idx_visual_analysis_nodes_graph_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_visual_analysis_nodes_graph_id ON public.visual_analysis_nodes USING btree (graph_id);


--
-- Name: idx_visual_analysis_nodes_person_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_visual_analysis_nodes_person_id ON public.visual_analysis_nodes USING btree (person_id);


--
-- Name: idx_visual_analysis_nodes_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_visual_analysis_nodes_project_id ON public.visual_analysis_nodes USING btree (project_id);


--
-- Name: idx_visual_analysis_nodes_unique; Type: INDEX; Schema: public; Owner: user
--

CREATE UNIQUE INDEX idx_visual_analysis_nodes_unique ON public.visual_analysis_nodes USING btree (graph_id, project_id, person_id);


--
-- Name: idx_visual_analysis_updated_at; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_visual_analysis_updated_at ON public.visual_analysis_graphs USING btree (updated_at);


--
-- Name: file_uploads tr_file_uploads_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER tr_file_uploads_updated_at BEFORE UPDATE ON public.file_uploads FOR EACH ROW EXECUTE FUNCTION public.update_file_uploads_updated_at();


--
-- Name: photos trigger_photos_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER trigger_photos_updated_at BEFORE UPDATE ON public.photos FOR EACH ROW EXECUTE FUNCTION public.update_photos_updated_at();


--
-- Name: field_mapping update_field_mapping_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_field_mapping_updated_at BEFORE UPDATE ON public.field_mapping FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: mergedpersons update_mergedpersons_modtime; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_mergedpersons_modtime BEFORE UPDATE ON public.mergedpersons FOR EACH ROW EXECUTE FUNCTION public.update_mergedpersons_updated_at_column();


--
-- Name: projects update_projects_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_projects_updated_at BEFORE UPDATE ON public.projects FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: search_keywords update_search_keywords_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_search_keywords_updated_at BEFORE UPDATE ON public.search_keywords FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: user_favorites update_user_favorites_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_user_favorites_updated_at BEFORE UPDATE ON public.user_favorites FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: user_update_file_archived_20250802 update_user_update_file_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_user_update_file_updated_at BEFORE UPDATE ON public.user_update_file_archived_20250802 FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: users update_users_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON public.users FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: activity_logs_backup activity_logs_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.activity_logs_backup
    ADD CONSTRAINT activity_logs_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE SET NULL;


--
-- Name: analysis_results analysis_results_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_results
    ADD CONSTRAINT analysis_results_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: analysis_sessions analysis_sessions_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_sessions
    ADD CONSTRAINT analysis_sessions_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: audit_change_details audit_change_details_audit_log_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_change_details
    ADD CONSTRAINT audit_change_details_audit_log_id_fkey FOREIGN KEY (audit_log_id) REFERENCES public.audit_logs(id) ON DELETE CASCADE;


--
-- Name: audit_log_access audit_log_access_accessed_log_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.audit_log_access
    ADD CONSTRAINT audit_log_access_accessed_log_id_fkey FOREIGN KEY (accessed_log_id) REFERENCES public.audit_logs(id) ON DELETE SET NULL;


--
-- Name: field_mapping field_mapping_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping
    ADD CONSTRAINT field_mapping_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: file_uploads fk_file_uploads_user_id; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.file_uploads
    ADD CONSTRAINT fk_file_uploads_user_id FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE RESTRICT;


--
-- Name: personmergelog fk_log_source_person_a; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.personmergelog
    ADD CONSTRAINT fk_log_source_person_a FOREIGN KEY (source_person_a_id) REFERENCES public.person_profile(id);


--
-- Name: personmergelog fk_log_source_person_b; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.personmergelog
    ADD CONSTRAINT fk_log_source_person_b FOREIGN KEY (source_person_b_id) REFERENCES public.person_profile(id);


--
-- Name: personmergelog fk_merged_person; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.personmergelog
    ADD CONSTRAINT fk_merged_person FOREIGN KEY (merged_person_id) REFERENCES public.mergedpersons(id);


--
-- Name: person_profile fk_person_profile_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.person_profile
    ADD CONSTRAINT fk_person_profile_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: photos fk_photos_project_id; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.photos
    ADD CONSTRAINT fk_photos_project_id FOREIGN KEY (project_id) REFERENCES public.projects(id);


--
-- Name: projects fk_projects_user_id; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.projects
    ADD CONSTRAINT fk_projects_user_id FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: relationship_layers fk_relationship_layers_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT fk_relationship_layers_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: relationship_layers fk_relationship_layers_visual_analysis_graph; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT fk_relationship_layers_visual_analysis_graph FOREIGN KEY (visual_analysis_graph_id) REFERENCES public.visual_analysis_graphs(id) ON DELETE SET NULL;


--
-- Name: search_keywords fk_search_keywords_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.search_keywords
    ADD CONSTRAINT fk_search_keywords_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: search_logs fk_search_logs_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.search_logs
    ADD CONSTRAINT fk_search_logs_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: mergedpersons fk_source_person_a; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.mergedpersons
    ADD CONSTRAINT fk_source_person_a FOREIGN KEY (source_person_a_id) REFERENCES public.person_profile(id);


--
-- Name: mergedpersons fk_source_person_b; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.mergedpersons
    ADD CONSTRAINT fk_source_person_b FOREIGN KEY (source_person_b_id) REFERENCES public.person_profile(id);


--
-- Name: user_favorites fk_user_favorites_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_favorites
    ADD CONSTRAINT fk_user_favorites_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: user_update_file_archived_20250802 fk_user_update_file_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_update_file_archived_20250802
    ADD CONSTRAINT fk_user_update_file_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: visual_analysis_nodes fk_visual_analysis_nodes_graph_id; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.visual_analysis_nodes
    ADD CONSTRAINT fk_visual_analysis_nodes_graph_id FOREIGN KEY (graph_id) REFERENCES public.visual_analysis_graphs(id) ON DELETE CASCADE;


--
-- Name: missing_persons missing_persons_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT missing_persons_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: person_profile person_profile_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.person_profile
    ADD CONSTRAINT person_profile_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: project_permissions project_permissions_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.project_permissions
    ADD CONSTRAINT project_permissions_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: relationship_layers relationship_layers_source_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_source_person_id_fkey FOREIGN KEY (source_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- Name: relationship_layers relationship_layers_target_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_target_person_id_fkey FOREIGN KEY (target_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- Name: relationship_layers relationship_layers_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: role_permissions role_permissions_permission_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT role_permissions_permission_id_fkey FOREIGN KEY (permission_id) REFERENCES public.permissions(id);


--
-- Name: role_permissions role_permissions_role_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.role_permissions
    ADD CONSTRAINT role_permissions_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);


--
-- Name: user_favorites user_favorites_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_favorites
    ADD CONSTRAINT user_favorites_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: user_permissions user_permissions_permission_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_permissions
    ADD CONSTRAINT user_permissions_permission_id_fkey FOREIGN KEY (permission_id) REFERENCES public.permissions(id);


--
-- Name: user_permissions user_permissions_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_permissions
    ADD CONSTRAINT user_permissions_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: user_roles user_roles_role_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT user_roles_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);


--
-- Name: user_roles user_roles_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_roles
    ADD CONSTRAINT user_roles_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);


--
-- Name: user_tokens user_tokens_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_tokens
    ADD CONSTRAINT user_tokens_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE;


--
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: yangandy
--

REVOKE USAGE ON SCHEMA public FROM PUBLIC;
GRANT ALL ON SCHEMA public TO "user";


--
-- PostgreSQL database dump complete
--

