--
-- PostgreSQL database dump
--

-- Dumped from database version 14.18 (Homebrew)
-- Dumped by pg_dump version 14.18 (Homebrew)

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
-- Name: field_mapping; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.field_mapping (
    id integer NOT NULL,
    excel_field_name character varying(100) NOT NULL,
    db_field_name character varying(100) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.field_mapping OWNER TO "user";

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
    project_id character varying(25)
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
-- Name: popular_keywords; Type: VIEW; Schema: public; Owner: user
--

CREATE VIEW public.popular_keywords AS
 SELECT search_keywords.keyword,
    search_keywords.search_count,
    search_keywords.last_search_time,
        CASE
            WHEN (search_keywords.search_count >= 10) THEN '熱門'::text
            WHEN (search_keywords.search_count >= 5) THEN '常用'::text
            ELSE '一般'::text
        END AS popularity_level
   FROM public.search_keywords
  WHERE (search_keywords.search_count > 0)
  ORDER BY search_keywords.search_count DESC, search_keywords.last_search_time DESC
 LIMIT 20;


ALTER TABLE public.popular_keywords OWNER TO "user";

--
-- Name: projects; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.projects (
    id character varying(25) NOT NULL,
    user_id character varying(6) NOT NULL,
    project_name character varying(200) NOT NULL,
    project_description text,
    status character varying(20) DEFAULT 'active'::character varying,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    completed_at timestamp without time zone,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT projects_status_check CHECK (((status)::text = ANY ((ARRAY['active'::character varying, 'completed'::character varying, 'archived'::character varying, 'draft'::character varying, 'deleted'::character varying])::text[])))
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
    visual_analysis_graph_id integer
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
    project_id character varying(25)
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
-- Name: user_update_file; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.user_update_file (
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


ALTER TABLE public.user_update_file OWNER TO "user";

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

ALTER SEQUENCE public.user_update_file_id_seq OWNED BY public.user_update_file.id;


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
-- Name: field_mapping id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping ALTER COLUMN id SET DEFAULT nextval('public.field_mapping_id_seq'::regclass);


--
-- Name: mergedpersons id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.mergedpersons ALTER COLUMN id SET DEFAULT nextval('public.mergedpersons_id_seq'::regclass);


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
-- Name: user_update_file id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_update_file ALTER COLUMN id SET DEFAULT nextval('public.user_update_file_id_seq'::regclass);


--
-- Name: visual_analysis_graphs id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.visual_analysis_graphs ALTER COLUMN id SET DEFAULT nextval('public.visual_analysis_graphs_id_seq'::regclass);


--
-- Name: visual_analysis_nodes id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.visual_analysis_nodes ALTER COLUMN id SET DEFAULT nextval('public.visual_analysis_nodes_id_seq'::regclass);


--
-- Name: field_mapping field_mapping_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping
    ADD CONSTRAINT field_mapping_pkey PRIMARY KEY (id);


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
-- Name: search_keywords unique_keyword; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.search_keywords
    ADD CONSTRAINT unique_keyword UNIQUE (keyword);


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
-- Name: user_update_file user_update_file_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_update_file
    ADD CONSTRAINT user_update_file_pkey PRIMARY KEY (id);


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
-- Name: idx_relationship_layers_visual_analysis_graph_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_relationship_layers_visual_analysis_graph_id ON public.relationship_layers USING btree (visual_analysis_graph_id);


--
-- Name: idx_result_count; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_result_count ON public.search_logs USING btree (result_count);


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
-- Name: idx_user_update_file_md5; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_update_file_md5 ON public.user_update_file USING btree (md5_hash);


--
-- Name: idx_user_update_file_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_update_file_project_id ON public.user_update_file USING btree (project_id);


--
-- Name: idx_user_update_file_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_update_file_status ON public.user_update_file USING btree (status);


--
-- Name: idx_user_update_file_upload_time; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_user_update_file_upload_time ON public.user_update_file USING btree (upload_time);


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
-- Name: user_update_file update_user_update_file_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_user_update_file_updated_at BEFORE UPDATE ON public.user_update_file FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


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
-- Name: user_update_file fk_user_update_file_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.user_update_file
    ADD CONSTRAINT fk_user_update_file_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: visual_analysis_nodes fk_visual_analysis_nodes_graph_id; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.visual_analysis_nodes
    ADD CONSTRAINT fk_visual_analysis_nodes_graph_id FOREIGN KEY (graph_id) REFERENCES public.visual_analysis_graphs(id) ON DELETE CASCADE;


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
-- PostgreSQL database dump complete
--

