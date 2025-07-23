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
-- Name: analysis_results; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.analysis_results (
    id integer NOT NULL,
    person_id integer NOT NULL,
    analysis_result jsonb NOT NULL,
    analysis_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    progress_percentage integer DEFAULT 0,
    status character varying(50) DEFAULT 'pending'::character varying,
    current_step character varying(255),
    status_message text,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    project_id character varying(25) NOT NULL
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
    root_person_id integer NOT NULL,
    max_depth integer DEFAULT 3 NOT NULL,
    status character varying(20) DEFAULT 'processing'::character varying NOT NULL,
    total_relationships integer DEFAULT 0,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    completed_at timestamp without time zone,
    project_id character varying(25) NOT NULL
);


ALTER TABLE public.analysis_sessions OWNER TO "user";

--
-- Name: TABLE analysis_sessions; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.analysis_sessions IS '儲存分析會話資訊';


--
-- Name: COLUMN analysis_sessions.max_depth; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.analysis_sessions.max_depth IS '最大分析深度，預設為3層';


--
-- Name: field_mapping; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.field_mapping (
    id integer NOT NULL,
    excel_field_name character varying(100) NOT NULL,
    db_field_name character varying(100) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    project_id character varying(25)
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
-- Name: missing_persons; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.missing_persons (
    id integer NOT NULL,
    name character varying(255) NOT NULL,
    relation_type character varying(100) NOT NULL,
    source_person_id integer NOT NULL,
    source_field character varying(50) NOT NULL,
    analysis_session_id character varying(255) NOT NULL,
    layer_depth integer DEFAULT 1 NOT NULL,
    discovered_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    status character varying(50) DEFAULT 'pending'::character varying,
    resolved_person_id integer,
    notes text,
    project_id character varying(25) NOT NULL
);


ALTER TABLE public.missing_persons OWNER TO "user";

--
-- Name: TABLE missing_persons; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.missing_persons IS '記錄 AI 分析出但資料庫中不存在的人員';


--
-- Name: COLUMN missing_persons.name; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.name IS '人員姓名';


--
-- Name: COLUMN missing_persons.relation_type; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.relation_type IS '與來源人員的關係類型';


--
-- Name: COLUMN missing_persons.source_person_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.source_person_id IS '來源人員的 ID';


--
-- Name: COLUMN missing_persons.source_field; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.source_field IS '來源欄位（family_relationships, friends, activities）';


--
-- Name: COLUMN missing_persons.analysis_session_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.analysis_session_id IS '分析會話 ID';


--
-- Name: COLUMN missing_persons.layer_depth; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.layer_depth IS '分析層級深度';


--
-- Name: COLUMN missing_persons.discovered_at; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.discovered_at IS '發現時間';


--
-- Name: COLUMN missing_persons.status; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.status IS '狀態（pending: 待處理, resolved: 已解決, ignored: 忽略）';


--
-- Name: COLUMN missing_persons.resolved_person_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.resolved_person_id IS '解決後對應的人員 ID';


--
-- Name: COLUMN missing_persons.notes; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.missing_persons.notes IS '備註信息';


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
-- Name: person_data_backup; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.person_data_backup (
    id integer,
    file_md5 character varying(32),
    photo text,
    name character varying(100),
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
    created_at timestamp without time zone,
    created_by character varying(100),
    updated_at timestamp without time zone,
    updated_by character varying(100)
);


ALTER TABLE public.person_data_backup OWNER TO "user";

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
-- Name: person_profile_backup; Type: TABLE; Schema: public; Owner: user
--

CREATE TABLE public.person_profile_backup (
    id integer,
    photo_index real,
    name text,
    discovery_source text,
    gender text,
    birthday date,
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
    created_at timestamp without time zone,
    created_by text,
    updated_at timestamp without time zone,
    updated_by text,
    extra_data jsonb,
    source_id integer,
    source_table character varying(50),
    source_created_at timestamp without time zone,
    source_updated_at timestamp without time zone
);


ALTER TABLE public.person_profile_backup OWNER TO "user";

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
    CONSTRAINT projects_status_check CHECK (((status)::text = ANY ((ARRAY['active'::character varying, 'completed'::character varying, 'archived'::character varying, 'draft'::character varying])::text[])))
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
    layer_depth integer DEFAULT 1 NOT NULL,
    analysis_session_id character varying(100) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    project_id character varying(25)
);


ALTER TABLE public.relationship_layers OWNER TO "user";

--
-- Name: TABLE relationship_layers; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON TABLE public.relationship_layers IS '儲存遞迴分析的層級關係資料';


--
-- Name: COLUMN relationship_layers.layer_depth; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.relationship_layers.layer_depth IS '關係層級深度，1為直接關係，2為間接關係，以此類推';


--
-- Name: COLUMN relationship_layers.analysis_session_id; Type: COMMENT; Schema: public; Owner: user
--

COMMENT ON COLUMN public.relationship_layers.analysis_session_id IS '分析會話ID，用於區分不同的分析任務';


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
-- Name: analysis_results id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_results ALTER COLUMN id SET DEFAULT nextval('public.analysis_results_id_seq'::regclass);


--
-- Name: field_mapping id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping ALTER COLUMN id SET DEFAULT nextval('public.field_mapping_id_seq'::regclass);


--
-- Name: missing_persons id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.missing_persons ALTER COLUMN id SET DEFAULT nextval('public.missing_persons_id_seq'::regclass);


--
-- Name: person_profile id; Type: DEFAULT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.person_profile ALTER COLUMN id SET DEFAULT nextval('public.person_profile_id_seq'::regclass);


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
-- Data for Name: analysis_results; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.analysis_results (id, person_id, analysis_result, analysis_date, progress_percentage, status, current_step, status_message, created_at, updated_at, project_id) FROM stdin;
\.


--
-- Data for Name: analysis_sessions; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.analysis_sessions (id, root_person_id, max_depth, status, total_relationships, created_at, completed_at, project_id) FROM stdin;
\.


--
-- Data for Name: field_mapping; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.field_mapping (id, excel_field_name, db_field_name, created_at, updated_at, project_id) FROM stdin;
1	姓名	name	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
2	名字	name	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
3	Name	name	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
6	性別	gender	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
7	Gender	gender	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
9	生日	birthday	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
10	出生日期	birthday	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
11	Birthday	birthday	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
12	Date of Birth	birthday	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
13	出生地	birthplace	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
14	父母戶籍所在地	birthplace	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
15	Birthplace	birthplace	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
16	Place of Birth	birthplace	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
17	國籍	nationality	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
18	Nationality	nationality	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
19	民族	ethnicity	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
20	Ethnicity	ethnicity	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
24	黨派	political_party	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
25	Political Party	political_party	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
26	身分證號碼	id_number	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
27	身份證號碼	id_number	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
28	ID Number	id_number	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
29	Identity Number	id_number	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
30	護照號碼	passport_number	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
31	Passport Number	passport_number	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
32	電話	phone	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
33	Phone	phone	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
34	Telephone	phone	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
35	行動電話	mobile	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
36	手機	mobile	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
37	Mobile	mobile	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
38	Cell Phone	mobile	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
39	電子信箱	email	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
40	Email	email	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
41	E-mail	email	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
48	通訊地址	mailing_address	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
49	聯絡地址	mailing_address	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
50	Mailing Address	mailing_address	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
51	親屬關係	family_relationships	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
52	Family Relationships	family_relationships	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
53	經歷	experience	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
54	工作經歷	experience	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
55	Experience	experience	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
56	學歷	education	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
57	Education	education	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
58	網路帳號	online_accounts	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
59	Online Accounts	online_accounts	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
60	著作	publications	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
61	Publications	publications	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
62	參與活動	activities	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
63	Activities	activities	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
64	重要友人	important_friends	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
65	Important Friends	important_friends	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
73	發掘經過	discovery_process	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
74	Discovery Process	discovery_process	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
82	經歷(單位，職稱，任職期間)	experience	2025-07-20 19:58:37.138282	2025-07-23 19:21:27.127727	123456-20250723192127
91	親屬關係(職稱，姓名)	family_relationships	2025-07-20 20:09:06.730739	2025-07-23 19:21:27.127727	123456-20250723192127
92	著作(名稱，共同作者)	publications	2025-07-20 20:09:06.730739	2025-07-23 19:21:27.127727	123456-20250723192127
93	參與活動(活動名稱，參與人士)	activities	2025-07-20 20:09:06.730739	2025-07-23 19:21:27.127727	123456-20250723192127
94	重要友人(姓名，單位，關聯事件)	important_friends	2025-07-20 20:09:06.730739	2025-07-23 19:21:27.127727	123456-20250723192127
95	出生地(父母戶籍所在地)	birthplace	2025-07-20 20:15:42.191694	2025-07-23 19:21:27.127727	123456-20250723192127
21	籍貫	ancestral_origin	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
22	祖父戶籍所在地	ancestral_origin	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
23	Ancestral Home	ancestral_origin	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
96	籍貫(祖父戶籍所在地)	ancestral_origin	2025-07-20 20:15:42.191694	2025-07-23 19:21:27.127727	123456-20250723192127
42	現職單位	current_employer	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
43	工作單位	current_employer	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
44	Current Workplace	current_employer	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
45	現居地址	address	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
46	居住地址	address	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
47	Current Address	address	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
66	經常出入場所	frequent_locations	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
67	Frequent Places	frequent_locations	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
68	出國紀錄	travel_history	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
69	Travel Records	travel_history	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
70	備註	remarks	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
71	Notes	remarks	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
72	Remarks	remarks	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
75	照片	photo_index	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
76	Photo	photo_index	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
77	Picture	photo_index	2025-07-20 18:39:56.341682	2025-07-23 19:21:27.127727	123456-20250723192127
\.


--
-- Data for Name: missing_persons; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.missing_persons (id, name, relation_type, source_person_id, source_field, analysis_session_id, layer_depth, discovered_at, status, resolved_person_id, notes, project_id) FROM stdin;
\.


--
-- Data for Name: person_data_backup; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.person_data_backup (id, file_md5, photo, name, discovery_process, gender, birthday, birthplace, nationality, ethnicity, ancestral_home, political_party, id_number, passport_number, phone, mobile, email, current_workplace, current_address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, important_friends, frequent_places, travel_records, notes, created_at, created_by, updated_at, updated_by) FROM stdin;
61	4e2e6de8e658c9cc98c8051df9151c45	0001	范立	業務黃三三114年1月透由約聘人員汪一德轉介結識。	男	1988-06-07	上海市	中國	滿族	江蘇	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	父：范統\n母：吳春華	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	以運動經濟推进中国式现代化的决定策略，江南海	「中」日建交75周年慶祝茶會，中國駐日代表薛健、副代表孫一真、商務部駐日參讚王桂鍾、東京都知事川崎朗、東京都議員鈴木康一\n棒球社團，中國總商會青年會副會長張三、東京華為公司工程師黃文偉、陳亮、全家便利商店店員吳士達	張三，中國總商會東京華僑青年會副會長，中共東京使館中秋節慶祝活動工作人員\n李賜，早稻田大學政治系博士一年級，大學辯論社學長學弟\n佐藤真美子，住友不動產株式會社職員，女友	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-20 20:20:36.438472	\N	2025-07-20 20:20:36.438472	\N
62	4e2e6de8e658c9cc98c8051df9151c45	0002	趙威	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	男	1975-04-15	海南省三亞市	中國	漢	海南省三亞市	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	妻：項依潔      女：趙小惠     子：趙小偉	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	多功能菜刀	煙台大學2024文化欣賞活動，邀請趙馥樂任藝術講師	\N	\N	\N	\N	2025-07-20 20:20:36.453962	\N	2025-07-20 20:20:36.453962	\N
63	4e2e6de8e658c9cc98c8051df9151c45	0003	項依潔	114年5月透由區內徵信查獲資訊	女	1975-10-29	山東省煙臺市	中國	漢	山東省煙臺市	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	夫：趙威\n 女：趙小惠\n 子：趙小偉	煙臺大學，人文學院副教授， 2000迄今	\N	\N	《唐詩三百首》的價值，趙威\n 2013《論漢代詩歌中的防災救災主題》，趙威	\N	張濤 ，北京師範大學歷史學院（博士生導師）\n北京師範大學易學文化研究中心（主任）\n中國易學文化研究會（會長），2011至2018年間與項永琴共同發表《中國古代城市排洪防災解析與借鑒》、《產翁制·濤組與生殖崇拜的變化》、《中國傳統救災思想研究》、《秦漢齊魯經學》	\N	\N	\N	2025-07-20 20:20:36.455498	\N	2025-07-20 20:20:36.455499	\N
64	4e2e6de8e658c9cc98c8051df9151c45	0004	李光	114年4月30日博覽會發掘	男	1990-05-17	山東省	中國	漢	山東省	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	父：李立朝（1962.12.2）\n母：江彩芹（1961.11.11）	麗星郵輪海員	\N	\N	\N	\N	蕭大方，麗星郵輪維修部主任，蕭大方介紹李光入職	\N	\N	\N	2025-07-20 20:20:36.456741	\N	2025-07-20 20:20:36.456741	\N
65	4e2e6de8e658c9cc98c8051df9151c45	0005	沈家新	111年透由王大陸介紹	女	1978-09-18	\N	中國	漢	上海市	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	夫：蕭大方                     女：蕭圓圓	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-20 20:20:36.457941	\N	2025-07-20 20:20:36.457941	\N
66	4e2e6de8e658c9cc98c8051df9151c45	0006	唐伯虎	112年透由蔡怡萱介紹	男	1967-08-20	\N	中國	漢	湖南永州	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	姜大宇，新聞最嗨點主持人，於X上恭賀新年快樂。	\N	\N	\N	2025-07-20 20:20:36.459123	\N	2025-07-20 20:20:36.459123	\N
67	4e2e6de8e658c9cc98c8051df9151c45	0007	楊習五	112年透由蔡怡萱介紹	男	1983-04-23	\N	中國	漢	山東省	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	妻：姜恩為\n 岳母：賴月英\n 小叔：姜大宇	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	趙馥樂，具長時間北京工作經歷（實習生、總監），FB臺籍好友	\N	\N	\N	2025-07-20 20:20:36.460216	\N	2025-07-20 20:20:36.460216	\N
68	4e2e6de8e658c9cc98c8051df9151c45	0008	李青春	113年透由邱阿霞介紹	女	1983-07-10	四川省	中國	漢	四川省	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	夫：羅大佑                     女：羅亞璇        表妹：姜恩為	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	常雨，陸配閨蜜，FBUID:100005521629975\n楊玉皙，陸配閨蜜，FBUID:100004792441549\n武新莉，陸配閨蜜，FBUID:100006498932852                              \n邱還真，羅東鎮新住民關懷協會理事長，共同場域\n女神美婕美甲(1120701-FB貼文)\n黃心田，羅東鎮新住民關懷協會常務監事，共同場域\n女神美婕美甲(1120701-FB貼文)\n馬晶，鳳之韻旗袍協會理事長共同場域\n女神美婕美甲(1120701-FB貼文)	女神美甲美睫	\N	\N	2025-07-20 20:20:36.46206	\N	2025-07-20 20:20:36.462061	\N
69	4e2e6de8e658c9cc98c8051df9151c45	0009	邱還真	113年透由邱阿霞介紹	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	女：黃心田	\N	\N	\N	\N	20230423(台)鳳之韻新住民旗袍關懷協會餐敘\n20230808福建平潭綜合試驗區申辦區內銀行帳戶及手機門號\n20231203廣東廣州第十七屆世界海南香團聯誼大會「港灣大融合共享新機遇」，美、日、阿、越、德、柬、丹麥、奧地利等國同鄉會及商會\n20231205廣東世界婦女論壇\n20240815湖北恩師女兒會\n20241223雲南未來生物贏家論壇	馬晶，鳳之韻旗袍協會理事長，共同場域鳳之韻旗袍協會第一次會員大會暨餐敘聯誼 (1120423-FB貼文)	\N	\N	\N	2025-07-20 20:20:36.463525	\N	2025-07-20 20:20:36.463525	\N
70	4e2e6de8e658c9cc98c8051df9151c45	0010	黃心田	113年透由邱阿霞介紹	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	母：邱還真	\N	淡江大學	\N	\N	\N	羅亞璇，淡江大學同學	\N	\N	\N	2025-07-20 20:20:36.464643	\N	2025-07-20 20:20:36.464643	\N
71	4e2e6de8e658c9cc98c8051df9151c45	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-20 20:20:36.471423	\N	2025-07-20 20:20:36.471423	\N
72	4e2e6de8e658c9cc98c8051df9151c45	0012	李光	\N	女	1993-12-05	新北市蘆洲	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-20 20:20:36.472803	\N	2025-07-20 20:20:36.472803	\N
\.


--
-- Data for Name: person_profile; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.person_profile (id, photo_index, name, discovery_source, gender, birthday, birthplace, nationality, ethnicity, ancestral_origin, political_party, id_number, passport_number, phone, mobile, email, current_employer, address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, friends, frequent_locations, travel_history, remarks, created_at, created_by, updated_at, updated_by, extra_data, source_id, source_table, source_created_at, source_updated_at, file_md5, source_file_id, source_file_name, discovery_process, important_friends, project_id) FROM stdin;
201	1	范立	\N	男	\N	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-20 18:33:51.918582	\N	2025-07-20 18:33:51.918604	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	123456-20250723192127
380	69	徐彥廷	\N	男	24392	\N	台灣	客家	\N	中國共產黨	Y782806479	ix95450090	090 65616556	(03) 23349114	chaoxia@yahoo.com	達廣電腦資訊有限公司	107 中和縣忠義路4號之7	49456 南投市中山路263號9樓	配偶，徐淑華	遊戲葡萄數位科技，硬體工程研發主管，2024-07-09 ~ 2006-02-20	發聯科技有限公司 大學，學士	@gang48	Innovative intermediate protocol，施郁婷	unleash viral networks年會，張雅玲	\N	新莊	俄羅斯 2006-04-02	\N	2025-07-23 14:01:35.707942	\N	2025-07-23 14:01:35.707943	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-07-03 透由 樊家銘 介紹	徐淑華，台灣雅萊（Y'ORÉAL）有限公司，配偶	782093-20250723205634
381	70	趙威廷	\N	男	28635	\N	中國	漢	\N	民進黨	K347786603	UF42044820	00 7015941	016 92319818	pdu@hotmail.com	隆豐大飯店（北台君悅）股份有限公司	436 台南松山街14號之8	45867 宜蘭縣昆陽街1號3樓	父親，張信宏	天中電視資訊有限公司，心理學研究人員，2013-06-25 ~ 2017-09-04	愛味之 大學，碩士	@qiangqiao	Advanced hybrid extranet，曹雅婷	deploy robust niches年會，薛佩君	\N	天母	馬利 1992-08-22	\N	2025-07-23 14:01:35.708526	\N	2025-07-23 14:01:35.708527	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-07-05 透由 薛雅婷 介紹	張信宏，王鼎餐飲集團有限公司，父親	782093-20250723205634
382	71	孫冠廷	\N	男	24870	\N	日本	客家	\N	\N	Q382965772	lz29543165	0993008931	0934206969	gangpeng@gmail.com	一統企業資訊有限公司	413 大里奇岩街7號9樓	336 宜蘭市中山路8段6號之1	母親，張宗翰	立三電視有限公司，電話及電報機裝修工，1991-12-31 ~ 2017-02-28	愛味之有限公司 大學，碩士	@bzou	User-centric radical extranet，李志豪	streamline open-source action-items年會，詹婉婷	\N	小碧潭	菲律賓 2018-04-15	\N	2025-07-23 14:01:35.709242	\N	2025-07-23 14:01:35.709242	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2025-06-15 透由 曹雅婷 介紹	張宗翰，品王餐飲資訊有限公司，母親	782093-20250723205634
383	72	李淑華	\N	男	24598	\N	中國	客家	\N	中國共產黨	Q150274564	Wj94856257	0970-320320	0942761707	fsun@gmail.com	天上雜誌股份有限公司	54228 基隆大智路61號4樓	259 澎湖市大坪巷14號之0	老闆，郭雅琪	風微廣場有限公司，媒體公關／宣傳採買，1973-04-25 ~ 2024-07-18	風微廣場 大學，學士	@xiaoxiuying	Multi-tiered background initiative，王羽	leverage integrated interfaces年會，蕭宜庭	\N	大同	喬治亞 1990-04-26	\N	2025-07-23 14:01:35.709978	\N	2025-07-23 14:01:35.709979	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-12-08 透由 劉淑惠 介紹	郭雅琪，石金堂資訊有限公司，老闆	782093-20250723205634
384	73	朱心怡	\N	男	35417	\N	中國	客家	\N	\N	F327881476	sK74224386	(08) 21081866	0994549180	yhao@he.tw	台灣人銀行資訊有限公司	897 新營市自強路5段916號0樓	70277 蘆竹縣芝山街3號3樓	老闆，臧志偉	品誠有限公司，電機設備裝配員，1971-10-29 ~ 1984-03-28	台灣BIM有限公司 大學，碩士	@pingyin	Public-key content-based array，陸建宏	syndicate cutting-edge web-readiness年會，張冠宇	\N	學府	墨西哥 2003-01-03	\N	2025-07-23 14:01:35.710576	\N	2025-07-23 14:01:35.710576	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-04-14 透由 李佩珊 介紹	臧志偉，平太洋崇光百貨有限公司，老闆	782093-20250723205634
385	74	楊承翰	\N	女	34308	\N	韓國	客家	\N	\N	F695814671	Ew95760243	0938395972	066 85729341	juanguo@yao.tw	丹味企業資訊有限公司	63800 楊梅市劍潭巷6號之1	68374 新竹縣興巷2段807號1樓	配偶，張淑娟	月日光半導體，物管／資材，1977-03-05 ~ 1981-05-14	美奧廣告股份有限公司 大學，學士	@lizou	Organic well-modulated middleware，盧俊傑	orchestrate bricks-and-clicks supply-chains年會，李懿	\N	南	新加坡 2022-05-07	\N	2025-07-23 14:01:35.711186	\N	2025-07-23 14:01:35.711186	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-07-15 透由 傅雅惠 介紹	張淑娟，Goagle資訊有限公司，配偶	782093-20250723205634
386	75	黃郁雯	\N	女	32539	\N	中國	漢	\N	\N	B208115562	Az06029084	08-3455048	06-6738997	lidong@gmail.com	全味食品工業有限公司	91884 金門市石牌巷5段5號之8	25485 屏東大橋頭巷45號5樓	同事，侯雅芳	大八電視有限公司，語文補習班老師，1995-09-13 ~ 2005-05-13	台灣鐵高股份有限公司 大學，博士	@mingsun	Cross-platform client-server structure，段志宏	evolve leading-edge e-commerce年會，馬怡君	\N	民權	聖文森及格瑞那丁 2016-01-22	\N	2025-07-23 14:01:35.711775	\N	2025-07-23 14:01:35.711776	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2025-05-09 透由 張佳穎 介紹	侯雅芳，達宏國際電子有限公司，同事	782093-20250723205634
387	76	薛建宏	\N	女	36147	\N	日本	客家	\N	\N	J961159408	Yj15454470	09-16044327	(08) 85106794	utang@hotmail.com	瑞輝大藥廠有限公司	672 褒忠建國街507號2樓	981 馬公德巷18號8樓	老闆，顧雅萍	品誠股份有限公司，醫事檢驗師，1984-01-03 ~ 1986-09-10	台灣人銀行股份有限公司 大學，碩士	@cyan	Pre-emptive 3rdgeneration Graphical User Interface，吳家豪	embrace mission-critical users年會，陳宗翰	\N	迴龍	巴林 1980-06-30	\N	2025-07-23 14:01:35.712366	\N	2025-07-23 14:01:35.712366	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-01-29 透由 呂柏翰 介紹	顧雅萍，邦富人壽保險有限公司，老闆	782093-20250723205634
203	3	項依潔	\N	女	27696	\N	中國	漢	\N	\N	370629197510294987	\N	8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.930751	\N	2025-07-20 18:33:51.930752	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	114年5月透由區內徵信查獲資訊	\N	123456-20250723192127
301	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-23 13:48:02.125644	\N	2025-07-23 13:48:02.125664	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	888888-20250723205343
302	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.141773	\N	2025-07-23 13:48:02.141777	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	888888-20250723205343
303	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.154036	\N	2025-07-23 13:48:02.154038	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	888888-20250723205343
304	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.156477	\N	2025-07-23 13:48:02.156479	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	888888-20250723205343
388	77	吳怡伶	\N	男	27116	\N	日本	漢	\N	中國共產黨	B578617063	KA48913824	0918325823	0968-700794	daitao@li.org	邦城文化事業有限公司	52891 阿里山雙連路5段93號之5	643 臺東縣民生巷809號0樓	老師，郝俊賢	冠智科技資訊有限公司，農藝／畜產研究人員，1996-09-23 ~ 2006-08-15	台灣BIM 大學，碩士	@zhaoqiang	Synergistic bottom-line help-desk，尹筱涵	synergize virtual synergies年會，孫沖	\N	南	阿爾及利亞 1985-01-27	\N	2025-07-23 14:01:35.713089	\N	2025-07-23 14:01:35.713090	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-07-06 透由 劉馨儀 介紹	郝俊賢，台灣酒菸有限公司，老師	782093-20250723205634
208	8	李青春	\N	女	30507	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-20 18:33:51.946441	\N	2025-07-20 18:33:51.946443	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	113年透由邱阿霞介紹	\N	123456-20250723192127
209	9	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.948068	\N	2025-07-20 18:33:51.948072	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	113年透由邱阿霞介紹	\N	123456-20250723192127
389	78	孟中山	\N	女	29398	\N	台灣	原住民	\N	民進黨	U192124938	aB43089487	00-8566418	0958030468	chao63@yahoo.com	樂可旅遊集團	413 新營民族巷8號之0	831 新營劍潭街4段8號7樓	老闆，邵宗翰	台灣人銀行，驗光師，1972-04-08 ~ 1995-09-07	邦富人壽保險 大學，博士	@fangsong	Centralized exuding pricing structure，王美琪	streamline scalable technologies年會，趙雅芳	\N	紅樹林	馬利 1985-11-12	\N	2025-07-23 14:01:35.713724	\N	2025-07-23 14:01:35.713724	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-03-14 透由 王婷婷 介紹	邵宗翰，品王餐飲有限公司，老闆	782093-20250723205634
210	10	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.949023	\N	2025-07-20 18:33:51.949025	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	113年透由邱阿霞介紹	\N	123456-20250723192127
211	11	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.955787	\N	2025-07-20 18:33:51.955789	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	\N	\N	123456-20250723192127
390	79	孫惠如	\N	男	38186	\N	日本	原住民	\N	國民黨	G232002496	Mv54982274	050 18920388	0953331537	xiulan95@hotmail.com	丹味企業股份有限公司	17073 竹北正義路75號6樓	15015 宜蘭南路6段1號3樓	同事，曹筱涵	一統超商有限公司，領班，1987-10-31 ~ 2018-12-01	石金堂有限公司 大學，碩士	@juancao	Enhanced empowering attitude，雷信宏	synergize one-to-one supply-chains年會，陳傑克	\N	太平	馬其頓 2014-12-22	\N	2025-07-23 14:01:35.714347	\N	2025-07-23 14:01:35.714347	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-08-21 透由 李雅惠 介紹	曹筱涵，輝燁企業有限公司，同事	782093-20250723205634
391	80	張佳玲	\N	女	32878	\N	日本	漢	\N	國民黨	K289731791	uT86857872	087 47749690	03-37363466	xia12@gmail.com	古太可口可樂股份有限公司	77895 牡丹縣太平街16號7樓	215 竹田市建國路6號8樓	兄弟，盧宜庭	見遠雜誌有限公司，藥學助理，1992-05-28 ~ 1987-01-06	聯燁鋼鐵股份有限公司 大學，博士	@yanyao	Devolved web-enabled structure，王美玲	envisioneer cross-media interfaces年會，閻宜庭	\N	中山	斯里蘭卡 2014-07-06	\N	2025-07-23 14:01:35.715135	\N	2025-07-23 14:01:35.715135	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-09-14 透由 虞雅萍 介紹	盧宜庭，瑞輝大藥廠有限公司，兄弟	782093-20250723205634
392	81	沈雅玲	\N	女	32043	\N	中國	客家	\N	國民黨	T075536668	KO59090225	09 2996636	0963-715974	guiyingzheng@xiao.com	邦富人壽保險資訊有限公司	43164 蘆洲大坪路602號之6	22867 臺東市府中街5號8樓	女兒，阮淑貞	橋子王生技有限公司，CNC電腦程式編排人員，1979-09-17 ~ 2000-05-01	旗花（台灣銀）行有限公司 大學，博士	@xiulan77	Persistent object-oriented analyzer，陳靜宜	seize seamless interfaces年會，趙冠霖	\N	正義	斯洛維尼亞 2001-02-26	\N	2025-07-23 14:01:35.715746	\N	2025-07-23 14:01:35.715747	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-09-23 透由 顧佩珊 介紹	阮淑貞，美奧廣告資訊有限公司，女兒	782093-20250723205634
393	82	文怡婷	\N	男	30830	\N	日本	客家	\N	民進黨	V438070176	uQ63600412	07-8771183	07-54220912	ehan@zhao.com	光新三越百貨資訊有限公司	83083 台南興街1號6樓	15331 桃園市劍潭路2號2樓	姊妹，李冠霖	丹味企業，金融理財專員，1992-04-06 ~ 1991-12-17	神漢名店百貨有限公司 大學，碩士	@hshen	Focused didactic benchmark，楊庭瑋	strategize turn-key synergies年會，張中山	\N	民族	智利 2014-03-25	\N	2025-07-23 14:01:35.716338	\N	2025-07-23 14:01:35.716338	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-07-26 透由 詹淑貞 介紹	李冠霖，中台信託商業銀行，姊妹	782093-20250723205634
394	83	呂雅雯	\N	女	27837	\N	韓國	原住民	\N	國民黨	O997683890	fd09143639	05-47432630	09-9752450	chao35@yahoo.com	美奧廣告	39685 基隆市民族路323號2樓	58167 頭份林森路4段84號4樓	姊妹，戚家瑋	台灣人銀行股份有限公司，樂器製造員，1995-09-15 ~ 1981-06-01	古太可口可樂資訊有限公司 大學，博士	@ygu	Polarized 3rdgeneration task-force，周志宏	engineer seamless metrics年會，羅冠霖	\N	迴龍	馬達加斯加 2001-07-21	\N	2025-07-23 14:01:35.716949	\N	2025-07-23 14:01:35.716950	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-02-21 透由 許郁婷 介紹	戚家瑋，台北邦富商業銀行有限公司，姊妹	782093-20250723205634
305	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.157597	\N	2025-07-23 13:48:02.157599	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	888888-20250723205343
306	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.158596	\N	2025-07-23 13:48:02.158598	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	888888-20250723205343
307	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.159417	\N	2025-07-23 13:48:02.159417	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	888888-20250723205343
308	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-23 13:48:02.160411	\N	2025-07-23 13:48:02.160412	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	888888-20250723205343
309	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.161410	\N	2025-07-23 13:48:02.161412	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	888888-20250723205343
310	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.162166	\N	2025-07-23 13:48:02.162168	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	888888-20250723205343
311	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.168068	\N	2025-07-23 13:48:02.168070	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	888888-20250723205343
312	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 13:48:02.168797	\N	2025-07-23 13:48:02.168798	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	888888-20250723205343
395	84	張沖	\N	男	23679	\N	台灣	原住民	\N	民進黨	T169236458	cx75066337	00-4249724	0992-011669	pinghan@shen.org	台北眾大捷運資訊有限公司	611 大里奇岩街3號之0	76864 太保自強巷8號8樓	配偶，蕭淑貞	榮長海運有限公司，地質與地球科學研究員，1983-09-04 ~ 1974-01-06	瑞輝大藥廠有限公司 大學，學士	@ping79	Customer-focused scalable concept，陳佳玲	embrace efficient partnerships年會，王傑克	\N	東興	蘇利南 2015-10-14	\N	2025-07-23 14:01:35.717621	\N	2025-07-23 14:01:35.717622	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-02-09 透由 朱郁雯 介紹	蕭淑貞，聯燁鋼鐵股份有限公司，配偶	782093-20250723205634
396	85	許雅文	\N	男	35653	\N	日本	漢	\N	\N	H510762091	Qm43579786	(05) 80505739	0991567164	weixu@wang.com	德汎	227 平鎮市新生巷7號2樓	543 屏東市信義巷2號之6	姊妹，包志宏	台灣BIM資訊有限公司，金融交易員，1985-11-18 ~ 1985-03-19	家宜家居（KIEA）股份有限公司 大學，學士	@fanggang	Mandatory user-facing parallelism，姜俊宏	maximize out-of-the-box channels年會，尤佳慧	\N	學府	阿爾巴尼亞 1991-02-24	\N	2025-07-23 14:01:35.718330	\N	2025-07-23 14:01:35.718330	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-01-10 透由 許思穎 介紹	包志宏，愛味之股份有限公司，姊妹	782093-20250723205634
397	86	陳怡萱	\N	男	29016	\N	韓國	客家	\N	國民黨	F460523302	RE64208656	066 21844386	05-89047292	jie33@hotmail.com	達台電子	12662 楊梅光復路759號6樓	51367 中和關渡街67號1樓	母親，陸建宏	台灣人銀行，營建主管，2013-08-25 ~ 2015-07-06	品誠資訊有限公司 大學，博士	@yan19	Enterprise-wide human-resource matrices，陳柏翰	transform world-class partnerships年會，趙志偉	\N	廣慈	突尼西亞 2006-07-03	\N	2025-07-23 14:01:35.719120	\N	2025-07-23 14:01:35.719120	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-04-01 透由 張欣怡 介紹	陸建宏，聯燁鋼鐵，母親	782093-20250723205634
398	87	覃宗翰	\N	男	23708	\N	韓國	漢	\N	中國共產黨	C845826034	Hw84872975	0988-310303	04-53298395	jding@lai.org	台灣BIM有限公司	714 臺東五福巷935號6樓	166 南投動物園巷286號之1	老師，孫惠雯	見遠雜誌股份有限公司，吊車／起重機設備操作員，2024-04-23 ~ 1985-05-13	一統企業 大學，博士	@yjiang	Customer-focused zero tolerance capability，郭淑玲	benchmark holistic platforms年會，蘇欣怡	\N	大安	查德 2011-05-04	\N	2025-07-23 14:01:35.719820	\N	2025-07-23 14:01:35.719820	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-09-19 透由 侯馨儀 介紹	孫惠雯，台灣力電有限公司，老師	782093-20250723205634
399	88	馬柏翰	\N	男	30014	\N	台灣	漢	\N	中國共產黨	T275784468	eU34146511	01-12901487	0982778816	chaoshen@zhu.tw	達台電子	68321 楊梅林森街6號5樓	54474 基隆松山街1段89號5樓	老師，張鈺婷	平太洋崇光百貨資訊有限公司，排版人員，1990-04-21 ~ 1994-01-31	品王餐飲股份有限公司 大學，博士	@namo	Advanced bandwidth-monitored Internet solution，張佳穎	deliver B2C e-markets年會，關宇軒	\N	大同	依朗 1985-05-21	\N	2025-07-23 14:01:35.720702	\N	2025-07-23 14:01:35.720703	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-01-30 透由 潘建宏 介紹	張鈺婷，塑台石化股份有限公司，老師	782093-20250723205634
400	89	花家瑜	\N	女	34496	\N	台灣	漢	\N	國民黨	F650237416	hg87256862	00 1225803	05-5690674	shenjun@hotmail.com	鮮爭有限公司	414 頭份縣自立巷3段3號1樓	404 北竿延平路7號之8	姊妹，尹筱涵	律理法律有限公司，客戶服務人員，1992-11-16 ~ 1998-12-30	榮長航空資訊有限公司 大學，學士	@liuguiying	Future-proofed high-level focus group，呂雅萍	harness scalable ROI年會，張依婷	\N	蘆洲	波蘭 1976-11-09	\N	2025-07-23 14:01:35.721320	\N	2025-07-23 14:01:35.721321	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-05-17 透由 黃靜怡 介紹	尹筱涵，華中郵政有限公司，姊妹	782093-20250723205634
212	12	李光	\N	女	34308	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.956699	\N	2025-07-20 18:33:51.956700	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	\N	\N	123456-20250723192127
313	1	范立	\N	男	32301	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-23 14:01:35.644155	\N	2025-07-23 14:01:35.644174	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	782093-20250723205634
314	2	趙威	\N	男	27499	\N	中國	漢	\N	\N	460004197502151560	\N	8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.654858	\N	2025-07-23 14:01:35.654860	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	782093-20250723205634
315	3	項依潔	\N	女	27696	\N	中國	漢	\N	\N	370629197510294987	\N	8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.656139	\N	2025-07-23 14:01:35.656140	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	114年5月透由區內徵信查獲資訊	\N	782093-20250723205634
316	4	李光	\N	男	33010	\N	中國	漢	\N	\N	371082199005173613	\N	861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.656803	\N	2025-07-23 14:01:35.656804	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	114年4月30日博覽會發掘	\N	782093-20250723205634
317	5	沈家新	\N	女	28751	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.657433	\N	2025-07-23 14:01:35.657434	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	111年透由王大陸介紹	\N	782093-20250723205634
318	6	唐伯虎	\N	男	24704	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.658053	\N	2025-07-23 14:01:35.658054	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	112年透由蔡怡萱介紹	\N	782093-20250723205634
319	7	楊習五	\N	男	30429	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.659163	\N	2025-07-23 14:01:35.659164	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	112年透由蔡怡萱介紹	\N	782093-20250723205634
320	8	李青春	\N	女	30507	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-23 14:01:35.659983	\N	2025-07-23 14:01:35.659983	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	113年透由邱阿霞介紹	\N	782093-20250723205634
321	9	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.660682	\N	2025-07-23 14:01:35.660682	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	113年透由邱阿霞介紹	\N	782093-20250723205634
322	10	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.661282	\N	2025-07-23 14:01:35.661283	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	113年透由邱阿霞介紹	\N	782093-20250723205634
323	11	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.667616	\N	2025-07-23 14:01:35.667618	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	\N	\N	782093-20250723205634
324	12	李光	\N	女	34308	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-23 14:01:35.668245	\N	2025-07-23 14:01:35.668246	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	\N	\N	782093-20250723205634
325	14	王詩婷	\N	男	29662	\N	台灣	漢	\N	中國共產黨	L890838637	Wv40265423	(01) 86155940	0916-184959	chengyong@jin.org	達宏國際電子	375 楊梅市自立巷428號7樓	683 竹北景美街41號之3	同事，趙威	遠西百貨資訊有限公司，可靠度工程師，1988-01-10 ~ 1979-07-03	聯燁鋼鐵股份有限公司 大學，學士	@zxue	Multi-lateral directional challenge，李佳樺	cultivate one-to-one eyeballs年會，夏佩珊	\N	新埔	奧地利 1996-03-08	\N	2025-07-23 14:01:35.669191	\N	2025-07-23 14:01:35.669191	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-11-24 透由 傅美琪 介紹	趙威，輝燁企業資訊有限公司，同事	782093-20250723205634
326	15	陸思穎	\N	男	26348	\N	台灣	漢	\N	\N	X288095701	eW43039117	0922-782489	0938346578	kongjun@gmail.com	品誠有限公司	11031 南投市小碧潭街829號7樓	71165 牡丹市復興街4號3樓	老師，沈家新	碩華電腦資訊有限公司，土木技師／土木工程師，1988-12-15 ~ 2001-06-25	華中航空資訊有限公司 大學，學士	@cuiyang	Quality-focused 24/7 array，張佩君	syndicate killer interfaces年會，張宇軒	\N	莒光	中非共和國 2023-05-11	\N	2025-07-23 14:01:35.669903	\N	2025-07-23 14:01:35.669903	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-06-30 透由 張宇軒 介紹	沈家新，台北眾大捷運有限公司，老師	782093-20250723205634
327	16	謝鈺婷	\N	男	25091	\N	中國	漢	\N	中國共產黨	Q193990916	qz99854353	04-27510799	(08) 63842513	qiangxiang@hotmail.com	平太洋崇光百貨	92411 大里永和路5號之4	66400 板橋學府街7段201號8樓	女兒，項依潔	中台信託商業銀行有限公司，水電工程繪圖人員，1978-09-03 ~ 1972-11-05	達友光電 大學，博士	@juan31	Multi-channeled zero-defect open architecture，商冠宇	re-contextualize web-enabled communities年會，文冠廷	\N	光復	克羅埃西亞 2023-03-30	\N	2025-07-23 14:01:35.670894	\N	2025-07-23 14:01:35.670895	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-09-01 透由 王詩涵 介紹	項依潔，榮長航空，女兒	782093-20250723205634
240	41	徐雅琪	\N	女	34263	\N	台灣	漢	\N	民進黨	K768321021	eY23785847	02-4961803	04-22605080	liujie@yahoo.com	星燦國際旅行社有限公司	477 太保大安巷2段3號1樓	69236 板橋復興街16號5樓	\N	榮長海運有限公司，軟韌體測試工程師，1978-08-31 ~ 2002-08-15	業商週刊有限公司 大學，學士	@xiuyingwen	\N	\N	\N	民富	尚比亞 1972-09-19	\N	2025-07-20 18:33:51.976211	\N	2025-07-20 18:33:51.976212	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-07-24 透由 王靜怡 推薦	\N	123456-20250723192127
328	17	白靜宜	\N	女	32266	\N	中國	客家	\N	\N	W142940196	qc56981693	06-2088356	05-7951484	szhu@hotmail.com	創群光電（奇原美電子）資訊有限公司	99946 蘆竹文山街6段87號8樓	61489 平鎮新店路1號7樓	女兒，陸思穎	達廣電腦資訊有限公司，領班，2012-11-08 ~ 2009-03-05	松黑資訊有限公司 大學，學士	@pqiu	Digitized solution-oriented solution，易承翰	revolutionize cutting-edge e-services年會，鄒瑋婷	\N	國凱八德	阿路巴 2007-12-07	\N	2025-07-23 14:01:35.671539	\N	2025-07-23 14:01:35.671540	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-08-06 透由 劉家瑜 介紹	陸思穎，旗花（台灣銀）行股份有限公司，女兒	782093-20250723205634
329	18	雷雅筑	\N	女	34659	\N	日本	漢	\N	民進黨	S455812236	Ad23166587	06-66909670	066 78893734	qiaomin@hotmail.com	台灣生資堂有限公司	19806 北竿國凱八德路66號7樓	764 雲林縣奇岩巷23號0樓	兄弟，李佳樺	台灣酒菸，總務主管，1980-05-15 ~ 1986-03-27	立三電視 大學，學士	@qiangzhu	Function-based zero administration hub，許俊宏	revolutionize granular content年會，謝淑玲	\N	忠義	聖多美及普林西比 1996-12-28	\N	2025-07-23 14:01:35.672145	\N	2025-07-23 14:01:35.672146	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-05-09 透由 徐雅萍 介紹	李佳樺，隆豐大飯店（北台君悅）資訊有限公司，兄弟	782093-20250723205634
330	19	方雅雯	\N	男	24596	\N	韓國	客家	\N	民進黨	R284987769	NQ53147379	0950752735	04 9549480	kangping@tian.net	業商週刊資訊有限公司	41436 永康縣林森巷6段57號之4	33518 斗六蘆洲巷9段441號之3	老闆，商冠宇	一統超商股份有限公司，IC封裝／測試工程師，2018-03-07 ~ 2006-05-07	橋子王生技 大學，學士	@xiaoli	Total clear-thinking productivity，李庭瑋	maximize front-end info-mediaries年會，徐宗翰	\N	仁愛	依朗 2002-07-06	\N	2025-07-23 14:01:35.672895	\N	2025-07-23 14:01:35.672895	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-12-14 透由 田威廷 介紹	商冠宇，一統企業資訊有限公司，老闆	782093-20250723205634
331	20	李雅萍	\N	女	26520	\N	台灣	漢	\N	民進黨	K334123281	sT06797403	071 43493618	02-5421024	tanyan@yahoo.com	遊戲葡萄數位科技資訊有限公司	59065 嘉義永寧巷1段149號7樓	729 嘉義縣中央街612號之7	老師，趙威	湖劍山世界資訊有限公司，金融營業員，2021-10-02 ~ 2007-06-08	達廣電腦 大學，博士	@lei51	Visionary impactful migration，潘鈺婷	target transparent markets年會，郭雅芳	\N	紅樹林	尼泊爾 2012-07-23	\N	2025-07-23 14:01:35.673496	\N	2025-07-23 14:01:35.673497	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-09-29 透由 許雅芳 介紹	趙威，資華粧業（生資堂）資訊有限公司，老師	782093-20250723205634
332	21	王郁婷	\N	女	29523	\N	中國	原住民	\N	中國共產黨	J484677378	vY63982146	0940-449972	0987558867	yankang@hotmail.com	達宏國際電子資訊有限公司	776 古坑淡水巷6段8號8樓	82621 新營縣文昌巷7段97號9樓	父親，夏佩珊	華晶國際酒店股份有限公司，家庭代工，2020-10-25 ~ 1976-02-25	海鴻精密 大學，碩士	@shan	Innovative 4thgeneration analyzer，方怡萱	seize out-of-the-box e-commerce年會，劉淑貞	\N	德	幾內亞 1981-07-26	\N	2025-07-23 14:01:35.674159	\N	2025-07-23 14:01:35.674159	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-05-07 透由 羅雅筑 介紹	夏佩珊，國中鋼鐵有限公司，父親	782093-20250723205634
333	22	孫惠雯	\N	男	34998	\N	中國	客家	\N	國民黨	W743671369	RX94406409	00-4974395	03-8942104	jie14@luo.tw	台灣酒菸有限公司	824 新營市民權路668號6樓	881 板橋縣新興巷90號之9	老師，潘鈺婷	業商週刊，美容類助理，2001-08-05 ~ 2017-09-16	台灣BIM資訊有限公司 大學，博士	@gaofang	Automated exuding methodology，李怡婷	enable integrated bandwidth年會，董鈺婷	\N	大智	阿拉伯聯合大公國 2020-07-06	\N	2025-07-23 14:01:35.674731	\N	2025-07-23 14:01:35.674731	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-03-04 透由 倪雅玲 介紹	潘鈺婷，台北邦富商業銀行有限公司，老師	782093-20250723205634
334	23	徐傑克	\N	女	32590	\N	中國	漢	\N	民進黨	L053293183	iI33529042	021 10205395	04-80268117	suwei@gmail.com	鮮爭股份有限公司	20766 卑南市大同巷3號之9	598 竹田縣石牌巷783號之3	老闆，項依潔	石金堂資訊有限公司，模特兒，1998-04-23 ~ 2019-08-26	麥當當資訊有限公司 大學，博士	@yong45	Distributed systemic leverage，李志宏	evolve user-centric platforms年會，秦淑華	\N	復興	阿拉伯聯合大公國 2008-03-20	\N	2025-07-23 14:01:35.675478	\N	2025-07-23 14:01:35.675478	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-11-04 透由 高雅婷 介紹	項依潔，榮長航空資訊有限公司，老闆	782093-20250723205634
335	24	黃佳慧	\N	男	36187	\N	中國	漢	\N	國民黨	L940244550	ny29612018	0975254599	(00) 72290147	rshao@gmail.com	邦城文化事業資訊有限公司	997 新竹市新埔路5號2樓	151 北港市水源巷285號之0	姊妹，唐伯虎	品誠資訊有限公司，麻醉醫師，1975-04-20 ~ 1991-04-22	樂可旅遊集團資訊有限公司 大學，碩士	@hlin	Open-architected 3rdgeneration Graphic Interface，侯雅芳	target bricks-and-clicks infrastructures年會，顧雅萍	\N	長春	阿拉伯聯合大公國 1997-04-02	\N	2025-07-23 14:01:35.676053	\N	2025-07-23 14:01:35.676053	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-10-22 透由 潘中山 介紹	唐伯虎，邦城文化事業股份有限公司，姊妹	782093-20250723205634
336	25	何沖	\N	女	32259	\N	中國	漢	\N	中國共產黨	E997995527	et77449058	0970054119	0986-798079	xiuying78@gu.com	台灣鐵高股份有限公司	90377 永和永安街19號之1	38644 雲林市府中巷2號之8	老闆，何沖	業商週刊股份有限公司，金融營業員，1978-09-12 ~ 2015-08-08	業商週刊資訊有限公司 大學，學士	@chao54	Enterprise-wide didactic Graphical User Interface，張信宏	orchestrate intuitive technologies年會，林鈺婷	\N	雙連	坦尚尼亞 1972-07-27	\N	2025-07-23 14:01:35.676612	\N	2025-07-23 14:01:35.676613	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-12-26 透由 劉建宏 介紹	何沖，台灣酒菸有限公司，老闆	782093-20250723205634
337	26	沈哲瑋	\N	女	33787	\N	韓國	漢	\N	民進黨	M977468862	TH92407581	01 3412478	06 1137506	jun61@yahoo.com	台灣電信股份有限公司	122 橫山市劍南巷1號之3	414 屏東市大智巷8段2號之9	母親，李青春	台灣士賓有限公司，調酒師／吧台人員，1979-10-09 ~ 1989-10-27	品王餐飲股份有限公司 大學，學士	@xiulanshao	Enterprise-wide static secured line，郭雅琪	optimize vertical architectures年會，王心怡	\N	太平	依朗 2020-11-08	\N	2025-07-23 14:01:35.677169	\N	2025-07-23 14:01:35.677170	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-02-05 透由 應琬婷 介紹	李青春，華聯電子資訊有限公司，母親	782093-20250723205634
338	27	王鈺婷	\N	女	31990	\N	韓國	漢	\N	民進黨	Q030548687	Zx03450541	0967652775	08 3416169	guiying15@hotmail.com	台灣來自水資訊有限公司	67570 鳳山市小碧潭巷38號2樓	62135 新竹市太平街3號之6	朋友，張宇軒	台灣BIM股份有限公司，小貨車司機，1977-09-20 ~ 1971-10-07	德汎資訊有限公司 大學，學士	@leilei	Triple-buffered local data-warehouse，杜佩君	aggregate 24/7 channels年會，孫雅涵	\N	信義	哥斯大黎加 2015-06-13	\N	2025-07-23 14:01:35.678004	\N	2025-07-23 14:01:35.678005	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2025-06-20 透由 雷雅玲 介紹	張宇軒，台灣士賓有限公司，朋友	782093-20250723205634
339	28	邱佳蓉	\N	男	29149	\N	日本	客家	\N	國民黨	P678669125	lX17785289	06-38018242	035 38414384	wangyang@gmail.com	鐵台股份有限公司	41094 桃園縣育英街7號之7	82767 新營福德巷356號之8	女兒，李庭瑋	麥當當有限公司，消防專業人員，1978-05-04 ~ 1988-03-15	秀威影城有限公司 大學，碩士	@cuili	Networked reciprocal instruction set，戚家瑋	transition world-class networks年會，張家瑋	\N	頂福州	新喀里多尼亞 2001-10-07	\N	2025-07-23 14:01:35.678644	\N	2025-07-23 14:01:35.678645	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-01-12 透由 夏傑克 介紹	李庭瑋，台灣電信股份有限公司，女兒	782093-20250723205634
340	29	王欣怡	\N	男	35876	\N	中國	漢	\N	\N	C983584168	xY84991216	085 12398680	(02) 35787298	jing69@hotmail.com	輝燁企業股份有限公司	670 高雄縣石牌巷474號9樓	48962 高雄民生街72號6樓	朋友，許俊宏	達台電子，記帳／出納／一般會計，1979-05-26 ~ 2005-04-18	月日光半導體股份有限公司 大學，學士	@ywu	Intuitive context-sensitive knowledgebase，高美琪	revolutionize back-end e-business年會，張雅萍	\N	蘆洲	突尼西亞 1990-04-25	\N	2025-07-23 14:01:35.679317	\N	2025-07-23 14:01:35.679317	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2025-01-30 透由 陳庭瑋 介紹	許俊宏，台灣雅萊（Y'ORÉAL）有限公司，朋友	782093-20250723205634
341	30	陳佳慧	\N	男	37077	\N	中國	客家	\N	\N	P384842199	vT30833016	0912082677	03-4454019	pinghao@chen.tw	碁宏股份有限公司	64991 梅山縣萬隆巷23號之2	620 馬公市劍潭街6段7號2樓	兒子，王鈺婷	星燦國際旅行社股份有限公司，小貨車司機，1970-06-19 ~ 1987-09-30	華晶國際酒店資訊有限公司 大學，碩士	@min49	Up-sized object-oriented access，蔡傑克	streamline 24/7 bandwidth年會，張淑惠	\N	民享	科克群島 2016-12-22	\N	2025-07-23 14:01:35.679923	\N	2025-07-23 14:01:35.679923	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-03-25 透由 江建宏 介紹	王鈺婷，一統星巴克股份有限公司，兒子	782093-20250723205634
342	31	趙雅筑	\N	女	35278	\N	韓國	原住民	\N	\N	T982258713	GA97187072	0990874064	00 2391065	tangfang@liao.tw	隆豐大飯店（北台君悅）有限公司	767 雲林市動物園路7段123號4樓	486 梅山大同街710號3樓	同事，項依潔	台灣印無品良，精密拋光技術人員，1972-04-14 ~ 2000-05-19	秀威影城 大學，博士	@osong	Quality-focused didactic system engine，李怡萱	enhance turn-key networks年會，劉建宏	\N	福安	紐西蘭 1985-01-16	\N	2025-07-23 14:01:35.680544	\N	2025-07-23 14:01:35.680545	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-07-28 透由 範雅惠 介紹	項依潔，雄遠建設事業資訊有限公司，同事	782093-20250723205634
343	32	路惠婷	\N	男	33049	\N	日本	客家	\N	國民黨	Y270866882	IU34351751	03 2046990	09-48373540	taojun@zhu.net	華晶國際酒店股份有限公司	678 板橋縣國凱八德路63號之6	72048 澎湖市建國街4段68號之7	母親，李怡婷	達廣電腦有限公司，資料輸入人員，2024-02-06 ~ 1993-12-19	碩華電腦資訊有限公司 大學，學士	@xiayuan	Centralized multi-tasking alliance，管惠婷	streamline clicks-and-mortar e-markets年會，賀飛	\N	民族	英國 1970-08-16	\N	2025-07-23 14:01:35.681155	\N	2025-07-23 14:01:35.681156	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-06-17 透由 杜淑貞 介紹	李怡婷，都亞緻麗資訊有限公司，母親	782093-20250723205634
344	33	王佳穎	\N	女	30404	\N	台灣	漢	\N	\N	S546156793	JE34101666	(00) 92176591	(03) 93974014	mincao@huang.com	華中航空股份有限公司	79685 雲林仁愛路2號之9	804 竹北縣長春路7段5號1樓	配偶，杜佩君	遊戲葡萄數位科技有限公司，木工，2018-06-06 ~ 2023-05-06	達台電子資訊有限公司 大學，博士	@ming11	Adaptive fault-tolerant intranet，範承翰	enable e-business ROI年會，徐淑華	\N	大勇	獅子山 1977-01-11	\N	2025-07-23 14:01:35.681974	\N	2025-07-23 14:01:35.681975	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-07-22 透由 王美琪 介紹	杜佩君，一統超商，配偶	782093-20250723205634
345	34	戴淑華	\N	女	29609	\N	中國	漢	\N	\N	M464715437	vN93712406	0957569245	0937-526547	na46@pan.com	輝燁企業有限公司	537 褒忠新生巷5號6樓	77979 新竹縣新興路1號之5	老闆，徐傑克	台灣鐵高，光電工程師，1987-08-10 ~ 1985-02-08	榮長海運股份有限公司 大學，博士	@xtan	Synergistic optimizing solution，楊冠廷	syndicate sticky technologies年會，包志宏	\N	新生	直布羅陀 1993-09-14	\N	2025-07-23 14:01:35.682535	\N	2025-07-23 14:01:35.682535	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-11-12 透由 葉家銘 介紹	徐傑克，遊戲葡萄數位科技股份有限公司，老闆	782093-20250723205634
346	35	楊美玲	\N	女	24686	\N	韓國	客家	\N	民進黨	M736226609	WT79660334	01 4918255	0914785969	mengming@yahoo.com	台北邦富商業銀行股份有限公司	25134 永康大勇街729號之2	38274 樹林正義路8號3樓	母親，楊美玲	月日光半導體資訊有限公司，機械設計／繪圖人員，2016-08-05 ~ 2003-06-18	心安食品服務（斯摩漢堡）資訊有限公司 大學，學士	@fhan	Re-contextualized 4thgeneration utilization，陳詩婷	enhance front-end communities年會，鄭婷婷	\N	大安	瑞士 1977-09-14	\N	2025-07-23 14:01:35.683173	\N	2025-07-23 14:01:35.683174	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-12-23 透由 郝雅婷 介紹	楊美玲，台北邦富商業銀行有限公司，母親	782093-20250723205634
347	36	盧宜庭	\N	男	29728	\N	中國	漢	\N	國民黨	K330958498	WY54494812	0904223253	06 8729466	qiuwei@liang.tw	衣優庫（Nuiqlo）股份有限公司	77518 北竿頂福州街95號9樓	200 新北市南街33號之2	母親，戴淑華	國中鋼鐵股份有限公司，居家服務督導員，1975-08-11 ~ 1995-04-22	台日積體電路資訊有限公司 大學，學士	@yan36	Streamlined background strategy，王詩涵	optimize end-to-end applications年會，郭雅涵	\N	中山	阿爾及利亞 2017-05-03	\N	2025-07-23 14:01:35.683868	\N	2025-07-23 14:01:35.683868	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-07-13 透由 徐怡如 介紹	戴淑華，古太可口可樂，母親	782093-20250723205634
348	37	姚承翰	\N	男	26141	\N	中國	漢	\N	民進黨	I532697311	rP19656523	(03) 48999404	0998-364037	kangtao@hotmail.com	雄遠建設事業	726 太保縣民生巷5號0樓	984 北竿縣華興巷3段51號8樓	配偶，王欣怡	光新三越百貨股份有限公司，核保／保險內勤人員，2009-11-27 ~ 2013-03-01	台灣來自水資訊有限公司 大學，博士	@fangkong	Cross-group intermediate budgetary management，鄧志豪	monetize virtual initiatives年會，程庭瑋	\N	三民	尼泊爾 1993-08-05	\N	2025-07-23 14:01:35.684479	\N	2025-07-23 14:01:35.684480	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-08-31 透由 施信宏 介紹	王欣怡，資華粧業（生資堂）有限公司，配偶	782093-20250723205634
349	38	郭嘉玲	\N	女	37908	\N	中國	原住民	\N	民進黨	E913800030	up56220863	0954195180	03-68061744	xcai@hu.org	達台電子有限公司	497 澎湖中央路650號6樓	83836 宜蘭信義路865號9樓	姊妹，戚家瑋	台北眾大捷運有限公司，美甲彩繪師，1996-11-01 ~ 1975-02-03	台灣雅萊（Y'ORÉAL）有限公司 大學，碩士	@lilei	Intuitive hybrid standardization，楊雅文	enhance intuitive solutions年會，戴雅慧	\N	新興	不丹 1977-03-11	\N	2025-07-23 14:01:35.685134	\N	2025-07-23 14:01:35.685134	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2025-05-03 透由 胡佳蓉 介紹	戚家瑋，榮長航空資訊有限公司，姊妹	782093-20250723205634
350	39	陳嘉玲	\N	女	26556	\N	韓國	漢	\N	中國共產黨	V804420678	dX83171210	02-23651994	(01) 83721510	xiulanshi@qin.tw	台北登來喜大飯店資訊有限公司	532 草屯市延平巷4段747號之0	762 臺中水源巷88號之9	朋友，王欣怡	全味食品工業股份有限公司，土地開發人員，1971-10-10 ~ 1991-09-13	台灣電信股份有限公司 大學，學士	@tangwei	Multi-channeled zero administration paradigm，刁俊傑	incentivize web-enabled niches年會，汪中山	\N	忠義	馬拉威 1994-02-22	\N	2025-07-23 14:01:35.687487	\N	2025-07-23 14:01:35.687487	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-10-07 透由 熊美玲 介紹	王欣怡，AYHOO!摩奇股份有限公司，朋友	782093-20250723205634
351	40	鄭沖	\N	男	35810	\N	日本	原住民	\N	中國共產黨	X252500292	vt18515691	022 46752895	09-1264422	hexiuying@yahoo.com	賓國大飯店	65135 嘉義劍南路4號5樓	70067 褒忠水源街7段8號2樓	女兒，夏佩珊	華中航空，建築師，1971-05-15 ~ 1991-09-09	台灣鐵高 大學，博士	@jing62	Upgradable asynchronous neural-net，楊琬婷	productize compelling web-readiness年會，遊家銘	\N	永和	茅利塔尼亞 1980-09-09	\N	2025-07-23 14:01:35.688411	\N	2025-07-23 14:01:35.688412	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-08-11 透由 張彥廷 介紹	夏佩珊，榮長航空有限公司，女兒	782093-20250723205634
352	41	李宜君	\N	男	37787	\N	台灣	漢	\N	中國共產黨	Q241714677	lC41862565	(04) 37743490	01 9158693	awen@xia.tw	立三電視股份有限公司	908 屏東光復路3號6樓	532 彰化市龍山寺街6段88號9樓	兒子，范立	家宜家居（KIEA）資訊有限公司，品牌宣傳及媒體公關，1980-02-01 ~ 2008-07-22	台灣鐵高 大學，學士	@xia56	Fundamental human-resource projection，梁依婷	integrate virtual niches年會，李婉婷	\N	育英	塞席爾 1972-08-21	\N	2025-07-23 14:01:35.689046	\N	2025-07-23 14:01:35.689046	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-09-03 透由 張雅琪 介紹	范立，發聯科技，兒子	782093-20250723205634
353	42	江美玲	\N	男	31304	\N	日本	漢	\N	民進黨	T367850821	md86289023	00-97053457	0910357972	ming24@hotmail.com	大八電視	42933 大里大勇路32號3樓	746 梅山縣國凱八德街7段910號之8	配偶，范立	都亞緻麗，照顧指導員，1974-05-06 ~ 1992-11-19	賓國大飯店股份有限公司 大學，博士	@lei43	Distributed multi-state task-force，莊雅惠	strategize turn-key methodologies年會，沈宜庭	\N	頂福州	哥倫比亞 1987-08-28	\N	2025-07-23 14:01:35.689667	\N	2025-07-23 14:01:35.689667	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-05-21 透由 嚴俊傑 介紹	范立，碩華電腦股份有限公司，配偶	782093-20250723205634
354	43	殷惠婷	\N	女	32486	\N	中國	原住民	\N	國民黨	S353465549	Em71709991	0916-846022	05-86972521	minyu@gmail.com	榮長海運股份有限公司	39271 北竿縣東興街9號5樓	183 臺中興街2號之8	兄弟，趙雅筑	鐵台，鍋爐操作技術人員，1973-02-10 ~ 1990-04-17	達廣電腦股份有限公司 大學，學士	@izhong	Mandatory mobile help-desk，張淑娟	utilize bleeding-edge e-services年會，張志偉	\N	自由	荷蘭 1981-07-02	\N	2025-07-23 14:01:35.690603	\N	2025-07-23 14:01:35.690604	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-04-25 透由 顧家瑜 介紹	趙雅筑，台灣業糖，兄弟	782093-20250723205634
355	44	唐佳樺	\N	女	26263	\N	韓國	漢	\N	國民黨	G618499261	mX91411739	083 45642087	07-8530011	weigang@duan.org	達廣電腦股份有限公司	301 蘆洲芝山巷21號0樓	55579 永康自強路57號之7	母親，陳嘉玲	台灣BIM有限公司，副教授，1979-01-07 ~ 2017-05-17	台灣軟微 大學，碩士	@eyan	Re-contextualized systematic intranet，鄧筱涵	re-intermediate proactive methodologies年會，朱佩君	\N	新生	馬利 1999-12-28	\N	2025-07-23 14:01:35.691491	\N	2025-07-23 14:01:35.691492	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-03-02 透由 蔣郁雯 介紹	陳嘉玲，台北登來喜大飯店，母親	782093-20250723205634
356	45	周雅萍	\N	女	35554	\N	韓國	原住民	\N	\N	P354083866	uh17797177	0901660880	01 8994883	fang37@yahoo.com	松黑股份有限公司	61305 永康市新生街561號3樓	437 台南縣華興巷2段97號7樓	父親，周雅萍	雄遠建設事業，產品行銷人員，1999-06-15 ~ 2009-10-26	共公電視有限公司 大學，學士	@jiefan	Networked stable policy，胡心怡	deploy interactive niches年會，景怡萱	\N	劍南	迦納 1979-05-28	\N	2025-07-23 14:01:35.692105	\N	2025-07-23 14:01:35.692105	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-12-10 透由 張淑華 介紹	周雅萍，一統企業有限公司，父親	782093-20250723205634
357	46	蔣美琪	\N	女	26873	\N	日本	漢	\N	中國共產黨	R062003424	Cg42043805	0983-863840	0966580043	yangzou@hotmail.com	華聯電子有限公司	272 太保水源路58號2樓	35720 苗栗縣石牌巷6段6號2樓	母親，顧雅萍	台北眾大捷運有限公司，櫃檯接待人員，2006-12-16 ~ 2006-07-19	國中鋼鐵股份有限公司 大學，學士	@juanzhang	Managed full-range hierarchy，劉雅芳	mesh collaborative schemas年會，李怡如	\N	劍南	加拿大 2013-09-15	\N	2025-07-23 14:01:35.692721	\N	2025-07-23 14:01:35.692722	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2025-07-12 透由 胡淑慧 介紹	顧雅萍，心安食品服務（斯摩漢堡），母親	782093-20250723205634
358	47	王瑋婷	\N	女	32895	\N	中國	客家	\N	國民黨	D361386120	eK50110248	03 6466766	(06) 21809190	linyong@yahoo.com	大八電視資訊有限公司	307 花蓮石牌巷624號8樓	70099 三重奇岩巷7號2樓	兄弟，何沖	台灣台油股份有限公司，壓鑄模具技術人員，1977-10-19 ~ 1982-05-05	華聯電子資訊有限公司 大學，學士	@linyong	Cloned eco-centric attitude，任佩君	facilitate synergistic channels年會，李雅芳	\N	民族	汶萊 1991-08-17	\N	2025-07-23 14:01:35.693375	\N	2025-07-23 14:01:35.693375	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-05-16 透由 朱怡萱 介紹	何沖，風微廣場，兄弟	782093-20250723205634
359	48	簡家銘	\N	男	23696	\N	韓國	漢	\N	\N	I478129142	Lz34110809	09 8308488	02-8563942	gpan@cai.net	華聯電子有限公司	82992 新竹市建國路13號8樓	177 太平民享巷949號6樓	配偶，簡家銘	達友光電，珠寶及貴金屬技術員，1987-02-14 ~ 1995-08-11	明陽海運資訊有限公司 大學，博士	@ming77	Robust 24hour middleware，徐飛	optimize efficient e-tailers年會，張雅琪	\N	永和	喬治亞 2014-12-27	\N	2025-07-23 14:01:35.694216	\N	2025-07-23 14:01:35.694216	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-08-31 透由 韋雅婷 介紹	簡家銘，橋子王生技股份有限公司，配偶	782093-20250723205634
360	49	蕭淑貞	\N	男	24133	\N	台灣	漢	\N	國民黨	M869904137	Xl77236929	0984-257991	06-76210388	haotao@ye.com	國中鋼鐵資訊有限公司	16865 竹北縣博愛巷2段52號之3	798 馬公市忠義巷406號之6	姊妹，張佩君	台灣鐵高有限公司，水電工，2014-05-05 ~ 2009-11-01	台灣五星電子資訊有限公司 大學，碩士	@xiuying46	Versatile demand-driven installation，張淑華	engage compelling portals年會，謝宜君	\N	學府	貝南 2014-11-12	\N	2025-07-23 14:01:35.694838	\N	2025-07-23 14:01:35.694839	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-01-27 透由 李慧君 介紹	張佩君，碩華電腦，姊妹	782093-20250723205634
361	50	張淑慧	\N	女	23707	\N	中國	漢	\N	中國共產黨	A698490277	So38430988	0920-530214	06 1566938	xiaoli@xie.tw	橋子王生技有限公司	169 太保縣龍山寺路5段91號之8	869 新營育英街1段51號6樓	老師，戴淑華	台北邦富商業銀行有限公司，醫藥研發人員，1991-06-27 ~ 1998-01-24	明陽海運 大學，碩士	@jun41	Open-architected systematic synergy，李冠霖	syndicate turn-key convergence年會，伍雅文	\N	自強	蓋亞那 2003-12-16	\N	2025-07-23 14:01:35.695658	\N	2025-07-23 14:01:35.695659	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-10-10 透由 馬雅惠 介紹	戴淑華，國中鋼鐵，老師	782093-20250723205634
362	51	張承翰	\N	男	26555	\N	韓國	客家	\N	國民黨	G392086780	Sa56271875	(03) 36915594	0943-479552	ahuang@hotmail.com	達友光電資訊有限公司	329 桃園芝山路7段33號7樓	96728 關山縣龍山寺巷31號3樓	朋友，張雅琪	立三電視，法務／智財主管，2020-09-01 ~ 1992-10-09	達友光電股份有限公司 大學，博士	@ming06	Decentralized grid-enabled service-desk，梁宜庭	visualize virtual technologies年會，楊佳蓉	\N	勝利	索馬利亞 1975-05-03	\N	2025-07-23 14:01:35.696271	\N	2025-07-23 14:01:35.696271	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-08-31 透由 馮鈺婷 介紹	張雅琪，星燦國際旅行社股份有限公司，朋友	782093-20250723205634
363	52	郝俊賢	\N	女	25542	\N	中國	漢	\N	\N	G202341326	Wf11780068	(04) 59216433	0922820055	rxiang@hotmail.com	碩華電腦有限公司	588 光復市南街34號之0	15478 光復縣民富街930號之3	同事，蔣美琪	月日光半導體資訊有限公司，業務支援工程師，1987-01-03 ~ 1981-02-10	台北登來喜大飯店有限公司 大學，博士	@gang63	Programmable well-modulated website，陳怡婷	exploit real-time e-markets年會，邵宗翰	\N	新生	牙買加 1977-07-05	\N	2025-07-23 14:01:35.696899	\N	2025-07-23 14:01:35.696900	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-05-18 透由 劉怡伶 介紹	蔣美琪，心安食品服務（斯摩漢堡）股份有限公司，同事	782093-20250723205634
364	53	臧志偉	\N	男	28197	\N	日本	原住民	\N	國民黨	R475705120	ah61872346	03 2673037	0937-645208	tao43@pan.com	家宜家居（KIEA）有限公司	502 阿里山中山路8段692號3樓	619 台中育英路2段8號5樓	父親，陸思穎	古太可口可樂有限公司，LCD製程工程師，2016-11-26 ~ 1995-09-16	台灣生資堂有限公司 大學，博士	@fang77	Realigned explicit moratorium，張宗翰	incubate cross-media vortals年會，陳雅惠	\N	大安	玻利維亞 1974-09-08	\N	2025-07-23 14:01:35.697503	\N	2025-07-23 14:01:35.697504	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-11-13 透由 劉雅涵 介紹	陸思穎，達宏國際電子，父親	782093-20250723205634
299	100	孔庭瑋	\N	女	26217	\N	日本	原住民	\N	民進黨	Z499290096	vd27026604	0956-321740	(03) 69101933	jieguo@liu.tw	國中鋼鐵有限公司	30608 關山市大仁街7號7樓	66438 台北建國街5號之1	\N	發聯科技有限公司，電機裝修工，1985-03-03 ~ 2015-10-26	美奧廣告有限公司 大學，學士	@afang	\N	\N	\N	士林	中國大陸 1977-11-30	\N	2025-07-20 18:33:52.015809	\N	2025-07-20 18:33:52.015810	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-02-21 透由 蕭家豪 推薦	\N	123456-20250723192127
207	7	楊習五	\N	男	30429	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.944882	\N	2025-07-20 18:33:51.944883	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	112年透由蔡怡萱介紹	\N	123456-20250723192127
365	54	盧志偉	\N	男	32625	\N	台灣	漢	\N	\N	F181022031	SL31096789	005 83910371	00 5586509	liugang@jia.com	台灣生資堂有限公司	33839 台南縣自立巷3號8樓	828 樹林縣中央巷459號6樓	母親，楊琬婷	達友光電資訊有限公司，法務助理，1997-01-08 ~ 1970-01-05	大八電視 大學，博士	@libai	Innovative directional data-warehouse，李靜宜	seize B2C channels年會，黃雅慧	\N	古亭	喀麥隆 1987-06-09	\N	2025-07-23 14:01:35.698227	\N	2025-07-23 14:01:35.698227	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2025-04-10 透由 楊威廷 介紹	楊琬婷，創群光電（奇原美電子）資訊有限公司，母親	782093-20250723205634
366	55	朱佳慧	\N	男	25946	\N	中國	原住民	\N	中國共產黨	U604805892	Gf83206799	06-14622517	0964-168622	taoqin@hotmail.com	月日光半導體有限公司	24659 褒忠光華街15號之2	892 竹北育英街111號9樓	母親，趙威	大八電視，攝影師，2017-03-19 ~ 2024-09-24	台灣來自水資訊有限公司 大學，博士	@vcao	Expanded user-facing monitoring，張瑋婷	engineer back-end schemas年會，韓建宏	\N	興	安哥拉 1995-08-18	\N	2025-07-23 14:01:35.698932	\N	2025-07-23 14:01:35.698932	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2025-01-23 透由 王佳慧 介紹	趙威，台灣人銀行，母親	782093-20250723205634
367	56	王家瑜	\N	男	30319	\N	台灣	原住民	\N	民進黨	F878003845	Gr62055543	00 3460843	062 31689363	xiuying57@hotmail.com	美奧廣告有限公司	34826 樹林龍山寺巷6號0樓	864 澎湖市淡水路494號3樓	兒子，楊冠廷	華中郵政，行銷企劃人員，2023-01-25 ~ 2002-06-08	台北眾大捷運資訊有限公司 大學，碩士	@jing67	Phased client-driven complexity，阮淑貞	enhance 24/365 e-tailers年會，張雅涵	\N	延平	諾魯 2016-10-22	\N	2025-07-23 14:01:35.699530	\N	2025-07-23 14:01:35.699531	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-01-24 透由 王宜庭 介紹	楊冠廷，達宏國際電子資訊有限公司，兒子	782093-20250723205634
368	57	吳佳慧	\N	男	38335	\N	日本	客家	\N	中國共產黨	D764472649	XL82771812	08-3266570	0993714521	zouqiang@yu.tw	雄遠建設事業資訊有限公司	186 新營三民路506號之7	51230 梅山頂福州巷473號之7	同事，方雅雯	古太可口可樂，紡織化學工程師，2010-11-02 ~ 1970-06-13	天中電視股份有限公司 大學，博士	@fanghe	Digitized analyzing projection，王彥廷	repurpose seamless supply-chains年會，王郁雯	\N	光復	玻利維亞 2015-09-26	\N	2025-07-23 14:01:35.700141	\N	2025-07-23 14:01:35.700142	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-07-23 透由 郭怡君 介紹	方雅雯，一統星巴克有限公司，同事	782093-20250723205634
369	58	黃佳玲	\N	女	25257	\N	韓國	漢	\N	國民黨	Q981794695	HU89952997	07-1729343	(00) 67139660	chaoyu@gmail.com	王鼎餐飲集團	688 高雄土城街1段4號之5	802 梅山國凱八德街649號3樓	母親，孫惠雯	華中郵政，品牌宣傳及媒體公關，1989-01-31 ~ 1972-02-14	台北登來喜大飯店有限公司 大學，博士	@mhe	Customizable zero tolerance system engine，魏怡伶	unleash robust niches年會，李傑克	\N	太平	摩爾多瓦 1990-08-05	\N	2025-07-23 14:01:35.700755	\N	2025-07-23 14:01:35.700755	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-08-20 透由 陳信宏 介紹	孫惠雯，邦富人壽保險股份有限公司，母親	782093-20250723205634
370	59	侯淑惠	\N	男	27405	\N	台灣	原住民	\N	民進黨	V768104091	On70184094	033 34485714	04-91098813	lei24@hotmail.com	鐵台	187 桃園永寧路20號之6	595 頭份市德路871號之7	姊妹，顧雅萍	達宏國際電子有限公司，可靠度工程師，2011-12-26 ~ 1987-07-24	遠西百貨有限公司 大學，學士	@min77	Assimilated asymmetric leverage，黃雅玲	iterate frictionless technologies年會，許柏翰	\N	大勇	法國 1997-08-10	\N	2025-07-23 14:01:35.701590	\N	2025-07-23 14:01:35.701590	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-08-13 透由 符佳穎 介紹	顧雅萍，資華粧業（生資堂）股份有限公司，姊妹	782093-20250723205634
371	60	韓雅筑	\N	女	27528	\N	台灣	客家	\N	中國共產黨	Z997581724	fn19196570	02 5902278	0999733549	fmeng@hotmail.com	平太洋崇光百貨資訊有限公司	905 基隆新生街7號之5	966 太保新生路7段36號之2	老闆，黃心田	一統企業，美髮類助理，1971-01-26 ~ 1979-10-20	湖劍山世界資訊有限公司 大學，碩士	@ganggao	Stand-alone intangible knowledgebase，韓靜宜	deliver cutting-edge schemas年會，吳中山	\N	自強	斯洛伐克 2011-02-21	\N	2025-07-23 14:01:35.702253	\N	2025-07-23 14:01:35.702254	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-02-07 透由 王羽 介紹	黃心田，台灣雅萊（Y'ORÉAL）股份有限公司，老闆	782093-20250723205634
372	61	魏俊宏	\N	女	37998	\N	台灣	原住民	\N	\N	A580778795	qd54420949	07-1929049	071 52814429	chao19@gmail.com	湖劍山世界資訊有限公司	94372 新營文化巷76號7樓	29504 連江大仁街48號之6	同事，陳詩婷	台灣五星電子有限公司，生產技術／製程工程師，1999-08-02 ~ 1970-10-22	榮長海運資訊有限公司 大學，學士	@wei01	Public-key scalable help-desk，鄧宜庭	iterate real-time supply-chains年會，劉俊傑	\N	迴龍	聖克里斯多福及尼維斯 2009-10-02	\N	2025-07-23 14:01:35.702998	\N	2025-07-23 14:01:35.702998	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-02-05 透由 李佳穎 介紹	陳詩婷，華福大飯店，同事	782093-20250723205634
373	62	雷庭瑋	\N	女	33797	\N	台灣	漢	\N	民進黨	Q939111926	OD92124471	065 64214066	(04) 74271739	jfan@xiang.tw	平太洋崇光百貨	210 八德民生街700號之2	990 光復縣萬隆巷730號之1	配偶，張宇軒	見遠雜誌股份有限公司，機械加工技術人員，1973-10-03 ~ 2013-01-10	達友光電股份有限公司 大學，碩士	@sunli	Stand-alone dedicated conglomeration，柏家瑜	aggregate seamless architectures年會，曹筱涵	\N	勝利	斐濟 2013-01-23	\N	2025-07-23 14:01:35.703604	\N	2025-07-23 14:01:35.703605	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-10-30 透由 葉怡如 介紹	張宇軒，台灣生資堂股份有限公司，配偶	782093-20250723205634
374	63	胡怡君	\N	男	23984	\N	日本	原住民	\N	民進黨	O707903202	gh04179291	0999313656	019 68253092	liangjie@shao.com	天中電視有限公司	79021 新北忠孝路2號之6	83248 平鎮市新埔路614號之0	兒子，董鈺婷	創群光電（奇原美電子），生產技術／製程工程師，1976-07-06 ~ 2012-02-13	瑞輝大藥廠有限公司 大學，博士	@qyin	Expanded even-keeled application，李冠宇	innovate collaborative models年會，張哲瑋	\N	仁愛	盧安達 1994-10-14	\N	2025-07-23 14:01:35.704219	\N	2025-07-23 14:01:35.704220	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-04-25 透由 王哲瑋 介紹	董鈺婷，天上雜誌有限公司，兒子	782093-20250723205634
205	5	沈家新	\N	女	28751	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.942823	\N	2025-07-20 18:33:51.942824	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	111年透由王大陸介紹	\N	123456-20250723192127
204	4	李光	\N	男	33010	\N	中國	漢	\N	\N	371082199005173613	\N	861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.931506	\N	2025-07-20 18:33:51.931507	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	114年4月30日博覽會發掘	\N	123456-20250723192127
202	2	趙威	\N	男	27499	\N	中國	漢	\N	\N	460004197502151560	\N	8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.929428	\N	2025-07-20 18:33:51.929429	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	123456-20250723192127
206	6	唐伯虎	\N	男	24704	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-20 18:33:51.944097	\N	2025-07-20 18:33:51.944099	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	112年透由蔡怡萱介紹	\N	123456-20250723192127
239	40	喬淑惠	\N	女	+026254-12-31		韓國	原住民			V471656094	ON65346997	0907-075504	07-6681699	qianfang@yahoo.com	立三電視股份有限公司	63176 橫山大同街8段7號0樓	38718 鳳山縣迴龍巷387號之4		一統星巴克股份有限公司，紡織化學工程師，1992-07-21 ~ 1999-01-15	愛味之有限公司 大學，碩士	@hutao			\N	土城	巴拉圭 2003-07-29		2025-07-20 18:33:51.975545	\N	2025-07-23 02:41:15.773324+08	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-08-16 透由 王怡萱 推薦		123456-20250723192127
375	64	張冠廷	\N	女	35632	\N	日本	原住民	\N	國民黨	L313636317	yx70623972	07-13543520	021 37762534	gangren@wen.tw	輝燁企業資訊有限公司	406 太平中和路3段106號7樓	886 草屯西門路1段363號之4	母親，張雅萍	業商週刊，ISO／品保人員，2002-08-26 ~ 2015-07-12	台灣軟微資訊有限公司 大學，碩士	@sunqiang	Visionary client-driven strategy，張怡安	whiteboard plug-and-play functionalities年會，賴俊賢	\N	莒光	克羅埃西亞 2006-02-17	\N	2025-07-23 14:01:35.704860	\N	2025-07-23 14:01:35.704860	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-05-18 透由 王傑克 介紹	張雅萍，輝燁企業股份有限公司，母親	782093-20250723205634
376	65	林美琪	\N	男	32623	\N	台灣	原住民	\N	民進黨	R381793355	FG86401557	0917-884795	01 5371162	hanlei@fang.com	光新三越百貨有限公司	368 汐止縣學府巷23號3樓	695 卑南縣德路5號7樓	兄弟，徐淑華	大八電視，呼吸治療師，1994-01-11 ~ 1989-05-09	鐵台有限公司 大學，學士	@jing22	Diverse disintermediate budgetary management，芮佳慧	enable visionary portals年會，田怡如	\N	文化	馬其頓 2000-07-07	\N	2025-07-23 14:01:35.705501	\N	2025-07-23 14:01:35.705503	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-06-09 透由 吳雅玲 介紹	徐淑華，達友光電股份有限公司，兄弟	782093-20250723205634
377	66	張鈺婷	\N	女	36187	\N	韓國	漢	\N	\N	D406937515	fI93983430	07 9186375	(05) 56265650	li78@gmail.com	鮮爭資訊有限公司	742 花蓮市文山街751號0樓	962 花蓮市雙連街570號之7	父親，殷惠婷	台灣台油，醫療器材研發工程師，1970-03-02 ~ 1980-06-25	都亞緻麗資訊有限公司 大學，博士	@zengyong	Horizontal intangible conglomeration，殷雅婷	incentivize proactive initiatives年會，胡威廷	\N	新埔	聖露西亞 2009-07-17	\N	2025-07-23 14:01:35.706057	\N	2025-07-23 14:01:35.706058	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-10-26 透由 馬怡萱 介紹	殷惠婷，塑台石化資訊有限公司，父親	782093-20250723205634
213	14	黃建宏	\N	男	24467	\N	日本	原住民	\N	\N	D670336268	ZW40850954	0930-596601	0964-876998	natan@hotmail.com	台灣力電	913 新營縣延平街14號4樓	827 卑南國凱八德路48號之0	\N	橋子王生技股份有限公司，人力資源助理，1999-12-22 ~ 1982-03-28	隆豐大飯店（北台君悅）股份有限公司 大學，博士	@tao23	\N	\N	\N	大安	冰島 1974-02-20	\N	2025-07-20 18:33:51.957553	\N	2025-07-20 18:33:51.957553	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-01-01 透由 陳志宏 推薦	\N	123456-20250723192127
214	15	李俊賢	\N	男	29466	\N	中國	原住民	\N	國民黨	G042901935	yW89536902	01-7139898	0918-097887	juan94@gmail.com	秀威影城股份有限公司	451 褒忠縣象山巷2段47號之6	38369 豐原縣動物園路38號之3	\N	達廣電腦，國內業務主管，1978-03-22 ~ 1998-06-27	瑞輝大藥廠資訊有限公司 大學，博士	@fufang	\N	\N	\N	土城	寮國 2010-04-02	\N	2025-07-20 18:33:51.958209	\N	2025-07-20 18:33:51.958210	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-01-24 透由 寇宜庭 推薦	\N	123456-20250723192127
215	16	廖信宏	\N	女	28135	\N	日本	原住民	\N	民進黨	H456522036	hs49951542	077 53244913	01-93882090	tianchao@peng.net	樂可旅遊集團資訊有限公司	87411 古坑縣新生街41號5樓	947 樹林長春路6段9號0樓	\N	碩華電腦資訊有限公司，電話及電報機裝修工，1984-12-26 ~ 2015-10-28	台灣五星電子資訊有限公司 大學，碩士	@taohe	\N	\N	\N	延平	布吉納法索 2018-04-25	\N	2025-07-20 18:33:51.958903	\N	2025-07-20 18:33:51.958904	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-01-01 透由 歐陽俊傑 推薦	\N	123456-20250723192127
216	17	溫飛	\N	女	30836	\N	日本	漢	\N	\N	D963044836	zq85456676	(04) 47350476	0928-319952	qiangang@zou.tw	隆豐大飯店（北台君悅）	95832 竹北縣太平街9號0樓	88186 金門大橋頭路530號6樓	\N	台灣力電股份有限公司，電玩程式設計師，1975-10-12 ~ 1975-03-18	台北眾大捷運資訊有限公司 大學，博士	@caixia	\N	\N	\N	大橋頭	以色列 2001-03-25	\N	2025-07-20 18:33:51.959496	\N	2025-07-20 18:33:51.959497	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-08-13 透由 張怡伶 推薦	\N	123456-20250723192127
217	18	米淑華	\N	男	30499	\N	韓國	客家	\N	民進黨	K791452118	EH37931269	09-6307464	02 5059960	fxiang@lu.net	華福大飯店資訊有限公司	16162 屏東公園街7號之2	58660 北港民治巷659號2樓	\N	明陽海運有限公司，量測／儀校人員，2011-10-14 ~ 2018-08-15	明陽海運股份有限公司 大學，學士	@ntan	\N	\N	\N	大坪	科威特 1976-06-12	\N	2025-07-20 18:33:51.960078	\N	2025-07-20 18:33:51.960079	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-05-08 透由 範承翰 推薦	\N	123456-20250723192127
218	19	陳佩珊	\N	男	38103	\N	中國	客家	\N	\N	Z289659994	ZN97320331	02-92705102	07 4675481	tqiao@yahoo.com	台灣來自水股份有限公司	92426 關山縣龍山寺街1號之7	939 中和光復巷8號之8	\N	品王餐飲股份有限公司，排版人員，1976-05-28 ~ 1995-02-07	台北眾大捷運 大學，學士	@fang47	\N	\N	\N	奇岩	盧安達 2021-01-28	\N	2025-07-20 18:33:51.960647	\N	2025-07-20 18:33:51.960648	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-06-16 透由 李冠宇 推薦	\N	123456-20250723192127
219	20	周馨儀	\N	女	24969	\N	日本	漢	\N	\N	A412704938	DK20448515	01 2971112	05-29020357	yong32@long.com	心安食品服務（斯摩漢堡）股份有限公司	629 員林市公園路57號7樓	858 新竹大同巷4段565號5樓	\N	國中鋼鐵有限公司，工地監工／主任，1984-11-07 ~ 2001-10-29	Goagle 大學，碩士	@taogao	\N	\N	\N	三民	馬爾他 2002-05-11	\N	2025-07-20 18:33:51.961539	\N	2025-07-20 18:33:51.961540	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-06-09 透由 郭惠婷 推薦	\N	123456-20250723192127
220	21	崔雅涵	\N	女	34921	\N	台灣	漢	\N	中國共產黨	I282498392	kD50142576	(02) 43244123	0913285072	tangxia@hotmail.com	律理法律有限公司	273 台東縣大仁街769號5樓	63923 新營市民權巷8號0樓	\N	都亞緻麗資訊有限公司，傳銷人員，1993-02-09 ~ 1996-02-20	發聯科技有限公司 大學，博士	@li67	\N	\N	\N	三民	哥斯大黎加 1978-10-05	\N	2025-07-20 18:33:51.962526	\N	2025-07-20 18:33:51.962528	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-07-21 透由 鄔飛 推薦	\N	123456-20250723192127
221	22	周淑慧	\N	女	26458	\N	中國	原住民	\N	國民黨	B017750954	Ab54084245	0954091508	06-3321812	wliang@yahoo.com	鮮爭資訊有限公司	25502 雲林縣林森街5號6樓	99449 苗栗忠義街34號之8	\N	豐兆國際商業銀行資訊有限公司，飯店餐廳主管，1982-05-03 ~ 2016-02-25	遊戲葡萄數位科技 大學，碩士	@bhuang	\N	\N	\N	新店	奧地利 2011-08-10	\N	2025-07-20 18:33:51.963797	\N	2025-07-20 18:33:51.963799	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-01-22 透由 喻慧君 推薦	\N	123456-20250723192127
222	23	楊怡伶	\N	女	32184	\N	中國	原住民	\N	中國共產黨	I966048736	hK26911633	022 56865369	051 85888636	qiang22@yahoo.com	品王餐飲股份有限公司	71696 馬公市新生巷9段7號5樓	19715 馬公市太平巷8號5樓	\N	台灣業糖，類講師，1972-02-07 ~ 1984-02-02	達廣電腦有限公司 大學，碩士	@ykang	\N	\N	\N	中正	阿根廷 2016-05-30	\N	2025-07-20 18:33:51.964418	\N	2025-07-20 18:33:51.964418	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-10-06 透由 陳怡伶 推薦	\N	123456-20250723192127
223	24	丁婉婷	\N	男	36852	\N	韓國	客家	\N	國民黨	S415177345	Wi75749490	08-54035954	02-3947742	na43@chang.net	雄豹旅遊	788 連江縣自由巷856號8樓	67601 三重新店路613號之7	\N	聯燁鋼鐵有限公司，資訊專業人員，1983-10-13 ~ 1994-09-28	旗花（台灣銀）行 大學，碩士	@wei14	\N	\N	\N	興	辛巴威 1986-09-05	\N	2025-07-20 18:33:51.965081	\N	2025-07-20 18:33:51.965082	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-12-06 透由 沈雅琪 推薦	\N	123456-20250723192127
224	25	翟柏翰	\N	男	36564	\N	中國	客家	\N	中國共產黨	R739970469	qI72587844	05-84884712	(05) 58772216	omao@zhong.com	資華粧業（生資堂）	94845 台南縣忠義巷42號之2	75879 竹北縣雙連街5號之5	\N	榮長海運股份有限公司，水產養殖工作者，2008-05-01 ~ 1996-10-12	品王餐飲資訊有限公司 大學，碩士	@renming	\N	\N	\N	大同	突尼西亞 2008-02-09	\N	2025-07-20 18:33:51.965689	\N	2025-07-20 18:33:51.965689	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-10-04 透由 馬俊傑 推薦	\N	123456-20250723192127
225	26	沈慧君	\N	男	35254	\N	日本	原住民	\N	民進黨	X857417215	fC62294938	05-39848840	01 8557325	vpan@hu.org	豐兆國際商業銀行	16667 永康德街858號之9	21918 嘉義延平巷7號之7	\N	達台電子有限公司，聲學／噪音工程師，1992-06-26 ~ 1974-12-21	業商週刊股份有限公司 大學，碩士	@xwu	\N	\N	\N	大同	伊拉克 1985-03-02	\N	2025-07-20 18:33:51.966921	\N	2025-07-20 18:33:51.966922	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-02-15 透由 粟慧君 推薦	\N	123456-20250723192127
226	27	陳俊傑	\N	男	24227	\N	韓國	客家	\N	\N	Z160736077	LU33691128	0912-317955	0933185748	acao@qian.tw	海鴻精密股份有限公司	516 雲林天母路350號2樓	18227 臺東太平巷7號2樓	\N	達台電子有限公司，文編／校對／文字工作者，1977-06-11 ~ 2023-07-11	台灣電信資訊有限公司 大學，博士	@liqiu	\N	\N	\N	中央	埃及 2006-09-17	\N	2025-07-20 18:33:51.967518	\N	2025-07-20 18:33:51.967518	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-07-30 透由 周雅文 推薦	\N	123456-20250723192127
227	28	全雅玲	\N	男	33892	\N	中國	原住民	\N	\N	G822311649	HH33424579	0942-565290	06-4032020	aluo@xu.tw	邦城文化事業有限公司	537 屏東縣土城路97號之1	715 台東建國街9號之2	\N	台灣來自水資訊有限公司，汽車／機車引擎技術人員，2007-08-27 ~ 1975-08-25	遠西百貨有限公司 大學，博士	@yongjiang	\N	\N	\N	新生	馬其頓 1980-10-20	\N	2025-07-20 18:33:51.968114	\N	2025-07-20 18:33:51.968115	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-03-09 透由 楊鈺婷 推薦	\N	123456-20250723192127
228	29	嚴詩婷	\N	女	24791	\N	韓國	原住民	\N	國民黨	M272362669	UV35134261	0923-724501	0948-197374	egu@mao.org	台北登來喜大飯店股份有限公司	369 蘆洲市信義巷6段8號之6	64448 八德縣中山路8號9樓	\N	合作庫金商業銀行股份有限公司，公共衛生人員，2023-02-21 ~ 2018-03-20	月日光半導體資訊有限公司 大學，學士	@mcheng	\N	\N	\N	博愛	盧安達 2023-08-20	\N	2025-07-20 18:33:51.968797	\N	2025-07-20 18:33:51.968797	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-08-20 透由 程惠婷 推薦	\N	123456-20250723192127
229	30	董冠廷	\N	女	28945	\N	韓國	原住民	\N	國民黨	V057423885	IV65271559	03 8541165	02-8634755	jingluo@gmail.com	創群光電（奇原美電子）股份有限公司	26913 白沙文化巷356號之1	45370 鳳山頂福州路4號之3	\N	一統企業股份有限公司，導演，1971-06-01 ~ 2011-08-30	古太可口可樂資訊有限公司 大學，碩士	@pinghe	\N	\N	\N	福德	多哥 1994-04-20	\N	2025-07-20 18:33:51.969385	\N	2025-07-20 18:33:51.969385	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-03-29 透由 郝惠雯 推薦	\N	123456-20250723192127
230	31	何承翰	\N	女	29238	\N	韓國	原住民	\N	\N	U572151494	tI09762637	04-9539862	09-2509059	wuping@guo.net	興復航空運輸有限公司	27151 中壢縣自立街22號之5	247 板橋公園路815號8樓	\N	品誠，發包人員，1974-01-10 ~ 2016-06-16	海鴻精密有限公司 大學，博士	@wuchao	\N	\N	\N	頂福州	幾內亞 2006-06-03	\N	2025-07-20 18:33:51.969954	\N	2025-07-20 18:33:51.969955	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-06-14 透由 馮婷婷 推薦	\N	123456-20250723192127
231	32	白雅涵	\N	女	24330	\N	韓國	漢	\N	中國共產黨	E523736050	SI91815899	0968-345273	066 66617071	flong@yahoo.com	雄遠建設事業	34998 苗栗市大橋頭街18號之8	203 朴子市民權巷13號6樓	\N	邦富人壽保險，廣告文案／企劃，1970-04-28 ~ 1986-05-11	一統超商資訊有限公司 大學，碩士	@mingqiu	\N	\N	\N	大智	馬利 1995-08-31	\N	2025-07-20 18:33:51.970557	\N	2025-07-20 18:33:51.970558	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-11-14 透由 童雅筑 推薦	\N	123456-20250723192127
232	33	杜怡君	\N	男	26779	\N	中國	客家	\N	\N	W771130621	MX13695727	(08) 63350902	04 7257306	wei13@he.net	湖劍山世界資訊有限公司	913 金門市復興路98號9樓	350 竹北水源路1段29號0樓	\N	品誠資訊有限公司，硬體研發工程師，2013-11-17 ~ 1996-04-09	愛味之資訊有限公司 大學，博士	@rkang	\N	\N	\N	新店	直布羅陀 1989-07-10	\N	2025-07-20 18:33:51.971138	\N	2025-07-20 18:33:51.971138	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-06-03 透由 李馨儀 推薦	\N	123456-20250723192127
233	34	王淑貞	\N	女	37035	\N	中國	客家	\N	民進黨	P393957865	tX43627187	09 3603358	06-7862703	yong92@yuan.tw	光新三越百貨資訊有限公司	832 新竹市忠孝路6號5樓	62341 草屯縣民享路91號2樓	\N	丹味企業，生產管理主管，2012-01-28 ~ 1997-11-17	月日光半導體資訊有限公司 大學，博士	@yan31	\N	\N	\N	自由	聖露西亞 2000-01-03	\N	2025-07-20 18:33:51.971764	\N	2025-07-20 18:33:51.971764	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-10-11 透由 孫思穎 推薦	\N	123456-20250723192127
234	35	余冠宇	\N	女	26334	\N	韓國	漢	\N	中國共產黨	R203447650	VK71580290	(04) 76376604	058 46652548	lei17@hotmail.com	合作庫金商業銀行股份有限公司	32131 彰化市關渡路1號之9	29751 花蓮奇岩路2號6樓	\N	美奧廣告，調酒師／吧台人員，1975-05-28 ~ 1996-04-30	台灣士賓有限公司 大學，學士	@thu	\N	\N	\N	光復	海地 1987-12-01	\N	2025-07-20 18:33:51.972393	\N	2025-07-20 18:33:51.972393	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-07-21 透由 黃惠雯 推薦	\N	123456-20250723192127
235	36	吳慧君	\N	女	30951	\N	韓國	漢	\N	國民黨	K170159591	WF45757574	083 83920567	067 37722660	jyin@yahoo.com	一統企業資訊有限公司	19231 橫山市土城路36號之8	73797 金門縣象山路57號3樓	\N	AYHOO!摩奇資訊有限公司，營建構造工，1970-11-19 ~ 1990-12-17	石金堂股份有限公司 大學，博士	@hye	\N	\N	\N	太平	新加坡 1986-10-01	\N	2025-07-20 18:33:51.972955	\N	2025-07-20 18:33:51.972955	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-02-20 透由 譚威廷 推薦	\N	123456-20250723192127
236	37	蘇柏翰	\N	女	31575	\N	日本	漢	\N	中國共產黨	N250733055	Im78245759	03 9991403	08 5436604	lichang@chang.tw	中台信託商業銀行資訊有限公司	368 樹林市大坪街597號9樓	101 光復縣福德巷606號之2	\N	平太洋崇光百貨，美術設計，1989-04-02 ~ 1992-01-19	共公電視 大學，博士	@taodeng	\N	\N	\N	紅樹林	愛沙尼亞 2003-07-07	\N	2025-07-20 18:33:51.973627	\N	2025-07-20 18:33:51.973628	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-04-05 透由 李佩君 推薦	\N	123456-20250723192127
237	38	徐俊宏	\N	女	30057	\N	韓國	原住民	\N	國民黨	F285566084	no06810390	0970-519582	0932809448	mguo@hotmail.com	見遠雜誌有限公司	561 桃園縣文山路7號3樓	539 牡丹市德街104號2樓	\N	美奧廣告資訊有限公司，調音技術員，1996-06-11 ~ 2018-12-29	達廣電腦資訊有限公司 大學，碩士	@taotao	\N	\N	\N	中正	剛果 2000-04-15	\N	2025-07-20 18:33:51.974431	\N	2025-07-20 18:33:51.974432	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-03-16 透由 彭中山 推薦	\N	123456-20250723192127
238	39	陳宜庭	\N	男	24132	\N	韓國	漢	\N	國民黨	L166007676	Zr09014473	(04) 93370096	07-6304519	kxiang@yahoo.com	台灣生資堂有限公司	762 牡丹縣文化巷686號之1	95577 新竹東湖巷358號3樓	\N	台灣人銀行資訊有限公司，韌體設計工程師，1977-08-12 ~ 2004-02-13	明陽海運 大學，學士	@laixia	\N	\N	\N	民有	塞內加爾 1984-11-28	\N	2025-07-20 18:33:51.974979	\N	2025-07-20 18:33:51.974979	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-12-30 透由 孔雅慧 推薦	\N	123456-20250723192127
241	42	甘依婷	\N	男	36287	\N	日本	漢	\N	\N	X358644963	nZ92435885	0998214339	05 5275367	jie49@gmail.com	天上雜誌資訊有限公司	81347 北斗市仁愛街717號之5	12150 鳳山大安巷2號之3	\N	湖劍山世界，FAE工程師，1977-08-01 ~ 1992-12-22	大八電視資訊有限公司 大學，碩士	@junjiang	\N	\N	\N	和街	匈牙利 1981-09-11	\N	2025-07-20 18:33:51.976829	\N	2025-07-20 18:33:51.976830	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-04-22 透由 唐飛 推薦	\N	123456-20250723192127
242	43	孟馨儀	\N	男	25954	\N	中國	漢	\N	中國共產黨	T996733116	Wr77269608	07-76855078	07 7935219	nhuang@lu.tw	橋子王生技有限公司	46970 草屯莒光巷16號之6	516 竹北縣中山街4號0樓	\N	達友光電，農藝作物栽培工作者，1985-07-22 ~ 1976-02-15	國中鋼鐵 大學，學士	@gang84	\N	\N	\N	平和	波札那 1989-08-13	\N	2025-07-20 18:33:51.977490	\N	2025-07-20 18:33:51.977491	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-05-29 透由 柳詩婷 推薦	\N	123456-20250723192127
243	44	金雅文	\N	女	28018	\N	韓國	客家	\N	中國共產黨	Y207223959	OM47372132	00 9162047	07 9688687	hlai@yahoo.com	台北眾大捷運資訊有限公司	53195 褒忠水源街6號之3	36279 阿里山市民生路77號之7	\N	美奧廣告資訊有限公司，化學研究員，1998-04-18 ~ 1973-06-30	雄豹旅遊 大學，學士	@ming05	\N	\N	\N	府中	荷蘭 1981-04-17	\N	2025-07-20 18:33:51.978213	\N	2025-07-20 18:33:51.978213	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-10-08 透由 汪佳蓉 推薦	\N	123456-20250723192127
244	45	王淑娟	\N	女	31878	\N	韓國	漢	\N	中國共產黨	A196463004	JK23693626	030 81204713	07-51175957	wei29@hotmail.com	興復航空運輸有限公司	96745 古坑縣自由街3號3樓	921 褒忠縣德街4號9樓	\N	湖劍山世界，遊戲企劃人員，1991-05-14 ~ 1973-11-15	旗花（台灣銀）行資訊有限公司 大學，碩士	@pingtang	\N	\N	\N	永安	印尼 2011-07-20	\N	2025-07-20 18:33:51.978904	\N	2025-07-20 18:33:51.978905	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-04-22 透由 尉冠宇 推薦	\N	123456-20250723192127
245	46	張佩珊	\N	女	35820	\N	中國	原住民	\N	民進黨	N671494981	gF42179428	0934-065277	084 44663453	yangu@gmail.com	榮長航空	57221 樹林市長春街589號7樓	426 彰化市新北投街8號之6	\N	榮長海運有限公司，金融理財專員，1992-03-31 ~ 2020-09-19	湖劍山世界股份有限公司 大學，學士	@chaohan	\N	\N	\N	蘆洲	保加利亞 2019-01-19	\N	2025-07-20 18:33:51.979647	\N	2025-07-20 18:33:51.979648	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-10-22 透由 王雅慧 推薦	\N	123456-20250723192127
246	47	張惠如	\N	男	36127	\N	中國	漢	\N	民進黨	O069176799	QL59374403	064 70830373	(06) 90148860	wanxiulan@gmail.com	資華粧業（生資堂）	385 台北忠孝街20號4樓	346 臺東市成功街281號1樓	\N	華晶國際酒店，運輸交通專業人員，2022-06-03 ~ 2024-01-23	一統超商 大學，碩士	@hmao	\N	\N	\N	光復	馬拉威 2024-11-21	\N	2025-07-20 18:33:51.980308	\N	2025-07-20 18:33:51.980309	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-03-17 透由 周哲瑋 推薦	\N	123456-20250723192127
247	48	郭怡婷	\N	女	28402	\N	中國	客家	\N	國民黨	D095055605	iE12198819	0988519053	086 74010424	min61@wu.tw	德汎股份有限公司	500 朴子市自由巷2段21號之5	22939 苗栗府中巷2段989號之8	\N	海鴻精密資訊有限公司，軟體專案管理師，1972-10-09 ~ 1991-03-24	國中鋼鐵股份有限公司 大學，碩士	@fang21	\N	\N	\N	龍山寺	維德角島 1975-08-01	\N	2025-07-20 18:33:51.980913	\N	2025-07-20 18:33:51.980913	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-03-15 透由 吳雅婷 推薦	\N	123456-20250723192127
248	49	傅佳蓉	\N	女	29506	\N	韓國	原住民	\N	民進黨	H269694439	mV64970169	029 42913203	00-1162341	zhuchao@ren.com	美奧廣告資訊有限公司	71556 卑南市文山街4號之7	104 牡丹市北投路743號4樓	\N	大八電視股份有限公司，國貿人員，1999-01-01 ~ 2022-05-02	立三電視 大學，學士	@tmeng	\N	\N	\N	關渡	印度 2013-11-28	\N	2025-07-20 18:33:51.981577	\N	2025-07-20 18:33:51.981577	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-08-05 透由 高志豪 推薦	\N	123456-20250723192127
249	50	蔣雅文	\N	女	36413	\N	日本	原住民	\N	民進黨	G586950732	KK46449369	016 67933505	0938573680	xiongjie@jin.tw	美奧廣告資訊有限公司	41872 斗六成功路7號4樓	21539 八德中興街820號6樓	\N	海鴻精密，物理治療師，2009-08-31 ~ 1985-08-10	品王餐飲資訊有限公司 大學，博士	@ming78	\N	\N	\N	學府	埃及 2019-11-04	\N	2025-07-20 18:33:51.982314	\N	2025-07-20 18:33:51.982315	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-11-28 透由 倪惠雯 推薦	\N	123456-20250723192127
250	51	劉信宏	\N	女	24358	\N	韓國	原住民	\N	民進黨	T312616324	MI88899572	09-5376297	049 83036468	zhoujie@cui.net	月日光半導體資訊有限公司	40828 平鎮大勇巷2段9號5樓	945 太保林森街4號8樓	\N	神漢名店百貨資訊有限公司，作曲家，1998-09-22 ~ 2019-01-22	達台電子股份有限公司 大學，博士	@rhan	\N	\N	\N	中和	巴哈馬 1975-08-16	\N	2025-07-20 18:33:51.982832	\N	2025-07-20 18:33:51.982832	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-05-09 透由 吳家銘 推薦	\N	123456-20250723192127
251	52	丁筱涵	\N	女	31351	\N	韓國	漢	\N	\N	G248905670	py67009004	051 25848266	06-62483550	xiulanqian@hotmail.com	天中電視	96336 楊梅市淡水路20號之4	84428 臺中市昆陽路2段44號1樓	\N	律理法律資訊有限公司，網頁設計師，2016-02-18 ~ 1990-02-01	明陽海運股份有限公司 大學，學士	@kliu	\N	\N	\N	忠孝	西班牙 1972-10-10	\N	2025-07-20 18:33:51.983408	\N	2025-07-20 18:33:51.983409	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-06-26 透由 張鈺婷 推薦	\N	123456-20250723192127
252	53	武哲瑋	\N	男	28213	\N	韓國	客家	\N	\N	N388824527	nn05040433	05 7384951	03-9211197	xiangfang@hotmail.com	華晶國際酒店	732 員林市和平巷9號之5	97868 台北文山路715號8樓	\N	興復航空運輸，導遊，2016-11-05 ~ 1995-09-04	立三電視資訊有限公司 大學，博士	@li91	\N	\N	\N	華興	巴布亞紐幾內亞 1989-07-09	\N	2025-07-20 18:33:51.984017	\N	2025-07-20 18:33:51.984018	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-09-19 透由 吳淑貞 推薦	\N	123456-20250723192127
253	54	廖淑貞	\N	女	28415	\N	台灣	原住民	\N	民進黨	X042886050	Sg53766608	(02) 66003083	0969106271	guiyinghao@gmail.com	石金堂股份有限公司	388 台南縣林森巷538號之0	84808 新營西門街9段7號3樓	\N	業商週刊股份有限公司，網路安全分析師，2019-04-05 ~ 1978-06-13	台灣鐵高股份有限公司 大學，博士	@szhou	\N	\N	\N	昆陽	摩爾多瓦 1987-05-16	\N	2025-07-20 18:33:51.984592	\N	2025-07-20 18:33:51.984592	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-04-20 透由 王怡君 推薦	\N	123456-20250723192127
254	55	段雅婷	\N	女	38496	\N	韓國	漢	\N	中國共產黨	X849932312	AN34266980	00-7872617	(07) 42348389	yang38@gmail.com	大八電視	712 桃園市自由街3段24號之9	461 光復成功巷9號之9	\N	華聯電子，居家服務督導員，1983-01-26 ~ 2022-11-21	旗花（台灣銀）行有限公司 大學，博士	@xiulanlong	\N	\N	\N	長春	菲律賓 1996-08-02	\N	2025-07-20 18:33:51.985141	\N	2025-07-20 18:33:51.985141	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-02-11 透由 曾雅雯 推薦	\N	123456-20250723192127
255	56	白惠如	\N	男	24209	\N	台灣	原住民	\N	中國共產黨	O283976205	zk44077794	(07) 12662925	0917-760299	chao68@zheng.org	創群光電（奇原美電子）有限公司	331 楊梅市博愛街28號之0	11192 高雄市西門巷8號之8	\N	台北登來喜大飯店，塑膠模具技術人員，1984-11-07 ~ 2000-06-14	合作庫金商業銀行股份有限公司 大學，博士	@lei90	\N	\N	\N	西門	摩爾多瓦 1983-12-11	\N	2025-07-20 18:33:51.985881	\N	2025-07-20 18:33:51.985882	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-07-07 透由 李郁婷 推薦	\N	123456-20250723192127
256	57	焦俊賢	\N	男	23924	\N	中國	原住民	\N	民進黨	I605397079	pz66177067	0920-506821	02-7235521	maxia@yahoo.com	愛味之股份有限公司	31821 阿里山縣龍山寺巷5段7號之8	42050 八德縣大橋頭巷998號1樓	\N	邦城文化事業資訊有限公司，總幹事，2007-02-19 ~ 2004-07-15	華聯電子 大學，學士	@wdu	\N	\N	\N	延平	奧地利 2018-12-16	\N	2025-07-20 18:33:51.986509	\N	2025-07-20 18:33:51.986510	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-02-27 透由 劉佩珊 推薦	\N	123456-20250723192127
257	58	林冠宇	\N	男	26794	\N	日本	漢	\N	中國共產黨	R566957900	ql12482554	0967-963259	023 92894633	na24@dong.tw	台灣迪奧汽車股份有限公司	35629 員林市自由巷4號8樓	850 中壢縣延平街444號3樓	\N	台灣士賓，電鍍／表面處理技術人員，2015-12-18 ~ 2004-11-11	家宜家居（KIEA）資訊有限公司 大學，博士	@dhan	\N	\N	\N	信義	象牙海岸 1989-12-25	\N	2025-07-20 18:33:51.987088	\N	2025-07-20 18:33:51.987088	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-06-21 透由 葉宇軒 推薦	\N	123456-20250723192127
258	59	馮龍	\N	女	34002	\N	中國	原住民	\N	\N	Z045238885	XL22586605	0967-874422	0958548596	yan94@gmail.com	都亞緻麗	350 新竹大同街439號3樓	15513 新營縣民有路521號之4	\N	全味食品工業資訊有限公司，可靠度工程師，2003-12-31 ~ 2016-12-24	發聯科技資訊有限公司 大學，博士	@plin	\N	\N	\N	民享	挪威 2024-12-19	\N	2025-07-20 18:33:51.987681	\N	2025-07-20 18:33:51.987682	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-07-04 透由 吳佳穎 推薦	\N	123456-20250723192127
259	60	曹雅芳	\N	女	25877	\N	韓國	漢	\N	\N	E257754584	fG26290529	07 9683160	02 4717217	kjia@zeng.com	風微廣場有限公司	79993 桃園芝山街2段943號3樓	953 樹林新莊街87號2樓	\N	台灣業糖有限公司，LCD製程工程師，2008-11-10 ~ 1992-08-20	王鼎餐飲集團資訊有限公司 大學，碩士	@heqiang	\N	\N	\N	新埔	摩洛哥 2021-09-13	\N	2025-07-20 18:33:51.988757	\N	2025-07-20 18:33:51.988758	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-09-13 透由 楊怡婷 推薦	\N	123456-20250723192127
260	61	李雅琪	\N	男	33446	\N	日本	客家	\N	國民黨	E436199517	kX52391029	09-5501437	08-7309309	huangyong@hotmail.com	國中鋼鐵	302 臺東動物園巷37號1樓	31661 新竹正義巷62號之3	\N	神漢名店百貨有限公司，RF通訊工程師，1989-12-24 ~ 2008-12-01	台灣雅萊（Y'ORÉAL）資訊有限公司 大學，學士	@juan80	\N	\N	\N	石牌	突尼西亞 2016-06-25	\N	2025-07-20 18:33:51.989395	\N	2025-07-20 18:33:51.989395	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-03-02 透由 張淑慧 推薦	\N	123456-20250723192127
261	62	李慧君	\N	男	28389	\N	韓國	原住民	\N	\N	W365858564	Ed23041202	0915927387	002 19907166	jiefu@yahoo.com	石金堂股份有限公司	40340 屏東延平街89號之6	65795 北竿市迴龍街5號之6	\N	家宜家居（KIEA），病理藥理研究人員，1974-01-16 ~ 2022-05-05	共公電視 大學，碩士	@huangchao	\N	\N	\N	明德	丹麥 2005-03-04	\N	2025-07-20 18:33:51.990526	\N	2025-07-20 18:33:51.990527	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-12-17 透由 薛俊傑 推薦	\N	123456-20250723192127
262	63	趙承翰	\N	女	29702	\N	台灣	漢	\N	國民黨	C009625885	Ja66947031	033 79627466	02 3159183	chenfang@yahoo.com	丹味企業資訊有限公司	159 高雄劍南巷571號0樓	58819 樹林小碧潭巷12號之9	\N	輝燁企業股份有限公司，美髮類助理，2010-09-09 ~ 2003-10-25	豐兆國際商業銀行資訊有限公司 大學，學士	@yangdeng	\N	\N	\N	大同	蒲隆地 2003-01-01	\N	2025-07-20 18:33:51.991128	\N	2025-07-20 18:33:51.991129	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-06-12 透由 臧詩婷 推薦	\N	123456-20250723192127
263	64	徐羽	\N	女	36386	\N	台灣	客家	\N	民進黨	X813599898	GO97211708	0948-018644	0922-175883	qiang08@gmail.com	台灣電信股份有限公司	875 朴子水源巷79號1樓	394 竹田市民治街8段104號之3	\N	台灣五星電子，醫療人員，2009-07-15 ~ 1998-10-27	華中郵政資訊有限公司 大學，博士	@wanxia	\N	\N	\N	中和	尚比亞 1997-02-04	\N	2025-07-20 18:33:51.991695	\N	2025-07-20 18:33:51.991696	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-12-02 透由 李淑貞 推薦	\N	123456-20250723192127
264	65	高嘉玲	\N	女	34246	\N	韓國	漢	\N	\N	K936488208	cB34868839	04-26728799	0909114131	yong92@huang.tw	達宏國際電子有限公司	305 新竹大同巷2段15號5樓	572 桃園福德巷5號之0	\N	旗花（台灣銀）行，清潔工，2021-06-02 ~ 2020-05-19	聯燁鋼鐵 大學，碩士	@yanlei	\N	\N	\N	萬隆	法國 2023-07-29	\N	2025-07-20 18:33:51.992286	\N	2025-07-20 18:33:51.992287	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-07-20 透由 王淑華 推薦	\N	123456-20250723192127
265	66	蘇雅慧	\N	女	33414	\N	台灣	原住民	\N	民進黨	M051171603	TP88636536	01-78945590	0900099424	fanglin@yahoo.com	愛味之股份有限公司	176 北港縣中山街85號之5	445 竹田林森路74號之2	\N	隆豐大飯店（北台君悅）資訊有限公司，推土機設備操作員，2017-09-04 ~ 1989-09-05	台灣鐵高資訊有限公司 大學，學士	@xiuying95	\N	\N	\N	和街	肯亞 2011-09-25	\N	2025-07-20 18:33:51.992851	\N	2025-07-20 18:33:51.992851	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-06-02 透由 李信宏 推薦	\N	123456-20250723192127
266	67	何瑋婷	\N	男	33225	\N	韓國	漢	\N	中國共產黨	E704726426	VW21203843	0953659680	0985254824	zhaoxia@zhou.net	王鼎餐飲集團股份有限公司	60644 牡丹縣公園路4號7樓	891 阿里山市小碧潭街12號8樓	\N	愛味之有限公司，銀行辦事員，1974-12-14 ~ 2002-04-22	松黑 大學，博士	@dengxiuying	\N	\N	\N	南	緬甸 2004-05-17	\N	2025-07-20 18:33:51.993430	\N	2025-07-20 18:33:51.993430	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-05-22 透由 朱婷婷 推薦	\N	123456-20250723192127
267	68	尹羽	\N	男	36875	\N	日本	漢	\N	民進黨	D999698829	CR45792714	04-56301986	04-92383972	xiulanfan@gu.com	台灣電信資訊有限公司	92880 連江動物園路3號2樓	71867 高雄市大勇巷9段276號8樓	\N	台灣業糖有限公司，星象占卜人員，1996-02-11 ~ 2024-03-24	大八電視 大學，碩士	@xialin	\N	\N	\N	育英	貝南 1983-04-09	\N	2025-07-20 18:33:51.993992	\N	2025-07-20 18:33:51.993993	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-07-15 透由 劉雅惠 推薦	\N	123456-20250723192127
268	69	孫淑娟	\N	女	28109	\N	韓國	客家	\N	國民黨	R407851699	Wm97089058	0968348268	09-8996269	xiashi@xie.com	雄遠建設事業資訊有限公司	705 永和萬隆路8段40號0樓	96578 中和大橋頭路1段252號之5	\N	共公電視股份有限公司，助理工程師，2001-05-05 ~ 2003-02-12	台灣業糖 大學，碩士	@taolin	\N	\N	\N	大仁	貝南 1999-08-27	\N	2025-07-20 18:33:51.994558	\N	2025-07-20 18:33:51.994558	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-04-14 透由 王雅婷 推薦	\N	123456-20250723192127
269	70	薛怡如	\N	女	30874	\N	日本	原住民	\N	國民黨	G407232472	FH28954001	08-3224731	0916850710	duguiying@hotmail.com	品王餐飲有限公司	21526 永和士林街123號7樓	195 光復自立巷994號之4	\N	業商週刊有限公司，醫藥業務代表，2008-08-13 ~ 2005-07-07	冠智科技有限公司 大學，博士	@imeng	\N	\N	\N	景美	科克群島 2025-03-08	\N	2025-07-20 18:33:51.995140	\N	2025-07-20 18:33:51.995141	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-01-01 透由 曲佳樺 推薦	\N	123456-20250723192127
270	71	金家瑜	\N	女	33599	\N	日本	漢	\N	中國共產黨	Y488384253	CC46280550	006 75795515	03-69209085	wanyang@zhu.org	合作庫金商業銀行	71178 新北文昌街360號0樓	535 太保忠孝巷8號之9	\N	榮長航空，哲學／歷史／政治研究人員，1998-06-04 ~ 2021-09-08	台灣人銀行股份有限公司 大學，碩士	@maojuan	\N	\N	\N	成功	墨西哥 2017-12-02	\N	2025-07-20 18:33:51.995807	\N	2025-07-20 18:33:51.995808	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-06-14 透由 苑靜怡 推薦	\N	123456-20250723192127
271	72	彭飛	\N	女	25210	\N	韓國	原住民	\N	中國共產黨	E203608941	lI84913599	053 25952056	(00) 76799006	xiangna@song.tw	一統星巴克有限公司	423 三重縣民生路77號5樓	23250 斗六縣迴龍路5號7樓	\N	碁宏，總機人員，1982-09-17 ~ 2015-01-06	台灣台油有限公司 大學，博士	@min11	\N	\N	\N	華興	多哥 2007-10-28	\N	2025-07-20 18:33:51.997191	\N	2025-07-20 18:33:51.997191	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-11-02 透由 金庭瑋 推薦	\N	123456-20250723192127
272	73	邱琬婷	\N	女	23737	\N	台灣	客家	\N	國民黨	D027558214	Zw70920711	02 3380236	(09) 82158189	xiulan16@fan.com	AYHOO!摩奇股份有限公司	89597 桃園龍山寺巷405號之8	98692 太保縣府中路822號0樓	\N	一統超商有限公司，網路安全分析師，1990-01-16 ~ 2006-07-09	台灣士賓有限公司 大學，博士	@yonggu	\N	\N	\N	忠義	烏拉圭 2018-05-09	\N	2025-07-20 18:33:51.998182	\N	2025-07-20 18:33:51.998183	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2025-01-18 透由 陳宜君 推薦	\N	123456-20250723192127
273	74	邵柏翰	\N	女	34485	\N	韓國	原住民	\N	民進黨	D630059377	Xe56919832	00-5291617	0990298310	xialiang@gmail.com	遠西百貨有限公司	13167 永和龍山寺街17號之3	61986 台中縣關渡街707號之3	\N	豐兆國際商業銀行有限公司，調酒師／吧台人員，1985-04-17 ~ 2008-08-15	邦城文化事業 大學，學士	@yang96	\N	\N	\N	迴龍	蒲隆地 2023-06-23	\N	2025-07-20 18:33:51.999002	\N	2025-07-20 18:33:51.999002	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-03-04 透由 王柏翰 推薦	\N	123456-20250723192127
274	75	何婷婷	\N	女	38342	\N	韓國	客家	\N	民進黨	Q334303115	XU87605457	0994757694	05-8353659	uqiao@hotmail.com	台灣印無品良資訊有限公司	321 豐原縣長安巷559號5樓	692 中和縣萬隆街4號7樓	\N	碩華電腦有限公司，日式廚師，2022-11-14 ~ 2018-10-27	遊戲葡萄數位科技有限公司 大學，學士	@sfeng	\N	\N	\N	昆陽	澳大利亞 2000-10-24	\N	2025-07-20 18:33:51.999581	\N	2025-07-20 18:33:51.999581	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-01-13 透由 沈欣怡 推薦	\N	123456-20250723192127
275	76	駱筱涵	\N	女	27727	\N	中國	客家	\N	民進黨	K865237772	hr66563370	(00) 65278105	08-3000272	jing57@zhang.net	發聯科技	99136 古坑和平路7號之5	320 臺中縣民治街6段333號2樓	\N	達宏國際電子有限公司，類講師，2005-07-15 ~ 2011-01-23	品王餐飲股份有限公司 大學，博士	@tao35	\N	\N	\N	莒光	波蘭 2003-11-17	\N	2025-07-20 18:33:52.000172	\N	2025-07-20 18:33:52.000172	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-04-19 透由 劉宜庭 推薦	\N	123456-20250723192127
276	77	張俊傑	\N	男	32380	\N	中國	原住民	\N	\N	W228019442	yl89222560	09 8535063	03 8118046	kangchao@yahoo.com	達宏國際電子股份有限公司	220 中壢大仁巷446號之5	65470 八德市五福巷71號之1	\N	塑台石化股份有限公司，排版人員，1999-02-22 ~ 1988-06-26	塑台石化 大學，學士	@nyan	\N	\N	\N	頂福州	孟加拉 1979-04-03	\N	2025-07-20 18:33:52.000824	\N	2025-07-20 18:33:52.000824	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-06-04 透由 張雅涵 推薦	\N	123456-20250723192127
277	78	孫雅雯	\N	女	34857	\N	台灣	客家	\N	國民黨	G319165430	hL95231202	(05) 80576449	04 9700026	tlong@hotmail.com	德汎資訊有限公司	597 北斗莒光街604號0樓	45537 中壢德街3段27號9樓	\N	碩華電腦資訊有限公司，室內設計／裝潢人員，1991-11-07 ~ 1984-01-05	邦富人壽保險股份有限公司 大學，博士	@lcai	\N	\N	\N	福德	葡萄牙 1989-09-25	\N	2025-07-20 18:33:52.001416	\N	2025-07-20 18:33:52.001417	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-03-28 透由 車佳慧 推薦	\N	123456-20250723192127
278	79	常怡萱	\N	女	29079	\N	韓國	客家	\N	\N	J394640882	UE30958629	03-4988941	01-80972021	tluo@wen.tw	大八電視資訊有限公司	379 新竹中山路79號之6	19390 楊梅新生路775號之8	\N	創群光電（奇原美電子）股份有限公司，空服員，1992-12-16 ~ 2012-09-26	品王餐飲資訊有限公司 大學，學士	@xuxiuying	\N	\N	\N	新店	多哥 2007-06-24	\N	2025-07-20 18:33:52.001986	\N	2025-07-20 18:33:52.001986	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-01-11 透由 梁婉婷 推薦	\N	123456-20250723192127
279	80	王雅萍	\N	女	28420	\N	台灣	客家	\N	中國共產黨	Q902177417	IY30435023	05 7022894	0952752332	chaogao@cao.com	旗花（台灣銀）行	40303 褒忠縣民族巷667號7樓	67995 台中蘆洲巷1號2樓	\N	達台電子股份有限公司，金融理財專員，2003-03-09 ~ 1981-08-11	台灣電信有限公司 大學，碩士	@yan20	\N	\N	\N	福安	貝里斯 1989-12-01	\N	2025-07-20 18:33:52.002623	\N	2025-07-20 18:33:52.002623	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-11-01 透由 胡美琪 推薦	\N	123456-20250723192127
280	81	彭婷婷	\N	女	31557	\N	中國	原住民	\N	國民黨	O684482373	fd37912121	(08) 17803586	01-3707403	yanxiuying@hotmail.com	邦富人壽保險股份有限公司	217 太保頂福州街4號之2	25422 太保縣育英街7段4號8樓	\N	台灣酒菸股份有限公司，建築師，1990-07-22 ~ 2000-02-19	台灣來自水資訊有限公司 大學，碩士	@chao47	\N	\N	\N	頂福州	開曼群島 2012-06-16	\N	2025-07-20 18:33:52.003190	\N	2025-07-20 18:33:52.003191	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-12-28 透由 楊威廷 推薦	\N	123456-20250723192127
281	82	趙雅文	\N	男	28173	\N	日本	漢	\N	民進黨	U115630295	lN96363726	04-5206615	0980-851331	chenglei@xu.net	台灣印無品良	51215 永康長春巷576號1樓	948 中壢府中路7號9樓	\N	古太可口可樂資訊有限公司，導遊，1991-05-07 ~ 2005-06-10	資華粧業（生資堂）資訊有限公司 大學，學士	@zliao	\N	\N	\N	昆陽	茅利塔尼亞 2025-01-11	\N	2025-07-20 18:33:52.003825	\N	2025-07-20 18:33:52.003825	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-05-25 透由 高建宏 推薦	\N	123456-20250723192127
282	83	方俊宏	\N	女	30570	\N	韓國	漢	\N	\N	M200053285	Zl86310505	038 33805826	09-7900768	pshi@jin.com	台北眾大捷運資訊有限公司	67618 白沙廣慈街7號之1	43282 澎湖永寧巷4段82號8樓	\N	風微廣場，機械操作員，2002-10-27 ~ 2003-07-20	海鴻精密 大學，碩士	@uyin	\N	\N	\N	中央	白俄羅斯 2001-05-07	\N	2025-07-20 18:33:52.004544	\N	2025-07-20 18:33:52.004544	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-09-10 透由 盧志偉 推薦	\N	123456-20250723192127
283	84	田冠霖	\N	女	23989	\N	日本	漢	\N	中國共產黨	O074299074	WC72966941	0996-708089	0947485994	gang83@hotmail.com	家宜家居（KIEA）資訊有限公司	44785 台南縣關渡巷1段64號9樓	305 新竹大仁路7號之4	\N	品誠有限公司，工程配管繪圖，2006-05-20 ~ 1987-03-12	明陽海運 大學，碩士	@chaoxie	\N	\N	\N	中山	巴貝多 2007-08-28	\N	2025-07-20 18:33:52.005157	\N	2025-07-20 18:33:52.005157	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-09-26 透由 馮慧君 推薦	\N	123456-20250723192127
284	85	焦佳樺	\N	女	23591	\N	日本	漢	\N	\N	K849378467	AP08701872	00 1013651	04-74442990	yong56@hotmail.com	中台信託商業銀行有限公司	66221 北港縣福德街3號9樓	533 桃園縣府中街58號之5	\N	興復航空運輸資訊有限公司，按摩／推拿師，1981-08-14 ~ 1976-07-23	台灣迪奧汽車有限公司 大學，博士	@chao71	\N	\N	\N	昆陽	斯里蘭卡 1986-02-12	\N	2025-07-20 18:33:52.005737	\N	2025-07-20 18:33:52.005737	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-06-07 透由 宋雅惠 推薦	\N	123456-20250723192127
285	86	呂淑芬	\N	女	37349	\N	日本	原住民	\N	中國共產黨	X774473535	wL24784171	097 43449305	0988033183	dengyan@gmail.com	業商週刊有限公司	38788 楊梅東興巷1號之8	25152 三重縣雙連巷83號1樓	\N	華中郵政資訊有限公司，行銷企劃主管，2002-07-21 ~ 1982-10-17	橋子王生技資訊有限公司 大學，碩士	@vdu	\N	\N	\N	文山	馬來西亞 1979-04-18	\N	2025-07-20 18:33:52.006753	\N	2025-07-20 18:33:52.006754	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-02-18 透由 徐志豪 推薦	\N	123456-20250723192127
286	87	蔣俊宏	\N	女	24774	\N	韓國	客家	\N	民進黨	C488499795	iH28575958	01-8902217	08-4912488	tanchao@yahoo.com	台灣五星電子股份有限公司	836 阿里山忠孝路903號之6	99397 鳳山忠義路399號7樓	\N	樂可旅遊集團，日文翻譯／口譯人員，1991-06-24 ~ 1980-01-22	發聯科技有限公司 大學，碩士	@mingdu	\N	\N	\N	廣慈	多明尼加 2020-10-10	\N	2025-07-20 18:33:52.007427	\N	2025-07-20 18:33:52.007428	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-12-27 透由 張志宏 推薦	\N	123456-20250723192127
287	88	邵依婷	\N	女	31215	\N	韓國	客家	\N	\N	Y373615873	zK62713589	03-65390058	06 6154731	lkang@yahoo.com	華中郵政股份有限公司	16436 平鎮勝利路3號之6	95342 中和迴龍路1號9樓	\N	風微廣場有限公司，採購助理，2006-06-08 ~ 2013-03-14	冠智科技股份有限公司 大學，博士	@pengguiying	\N	\N	\N	新生	瑞士 1976-12-21	\N	2025-07-20 18:33:52.007984	\N	2025-07-20 18:33:52.007985	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-07-20 透由 萬馨儀 推薦	\N	123456-20250723192127
288	89	梁佳慧	\N	男	24692	\N	中國	客家	\N	民進黨	X306881164	GF03439881	08-77758333	077 20413090	weiyu@yahoo.com	碁宏有限公司	55420 連江忠孝巷857號0樓	110 大里市三民街9號0樓	\N	松黑股份有限公司，光電工程師，2013-03-01 ~ 1979-10-31	華聯電子股份有限公司 大學，學士	@lchang	\N	\N	\N	新北投	日本 2023-12-30	\N	2025-07-20 18:33:52.008534	\N	2025-07-20 18:33:52.008534	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-12-25 透由 萬傑克 推薦	\N	123456-20250723192127
289	90	郭俊傑	\N	女	33057	\N	台灣	客家	\N	中國共產黨	U689005779	ab00173259	07 8397130	02-91138690	zengna@yahoo.com	碁宏	754 古坑德路2段80號之4	402 蘆洲市光華路360號之2	\N	輝燁企業股份有限公司，軟體設計工程師，1975-08-08 ~ 1979-08-29	信永藥品股份有限公司 大學，學士	@qiang72	\N	\N	\N	國凱八德	烏拉圭 1990-06-04	\N	2025-07-20 18:33:52.009087	\N	2025-07-20 18:33:52.009088	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2020-04-19 透由 唐傑克 推薦	\N	123456-20250723192127
290	91	李佳慧	\N	女	26321	\N	台灣	客家	\N	國民黨	Q760088447	Kc08378191	0949921640	(06) 69836923	xia48@shi.org	一統星巴克有限公司	59865 朴子市新生路8號0樓	403 台中民生巷7段5號之7	\N	輝燁企業，網站行銷企劃，1970-05-01 ~ 1970-03-25	天上雜誌股份有限公司 大學，博士	@qiuchao	\N	\N	\N	永寧	泰國 2022-08-17	\N	2025-07-20 18:33:52.009644	\N	2025-07-20 18:33:52.009644	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-06-11 透由 盧怡婷 推薦	\N	123456-20250723192127
291	92	劉佳樺	\N	男	24149	\N	韓國	漢	\N	民進黨	R034015353	Mq66075699	094 96672010	04-40281822	chao26@gmail.com	雄遠建設事業股份有限公司	345 竹北縣中央街6號之0	212 新營大安路3號之5	\N	大八電視資訊有限公司，加油員，1972-02-28 ~ 1974-02-20	豐兆國際商業銀行有限公司 大學，學士	@fanglei	\N	\N	\N	德	西薩摩亞 1999-01-27	\N	2025-07-20 18:33:52.010178	\N	2025-07-20 18:33:52.010178	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-06-14 透由 楊家瑜 推薦	\N	123456-20250723192127
292	93	崔中山	\N	女	30002	\N	韓國	漢	\N	\N	A724358617	zC82251272	(08) 56665622	(09) 57687849	qdong@gmail.com	賓國大飯店有限公司	50618 蘆竹市西門街5號4樓	282 嘉義石牌巷69號5樓	\N	樂可旅遊集團有限公司，ISO／品保人員，1997-03-13 ~ 2014-11-28	大八電視 大學，碩士	@duanyong	\N	\N	\N	林森	喀麥隆 1990-11-15	\N	2025-07-20 18:33:52.010753	\N	2025-07-20 18:33:52.010754	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-04-04 透由 李家豪 推薦	\N	123456-20250723192127
293	94	宋郁雯	\N	男	36919	\N	韓國	漢	\N	\N	T498603721	gM86003203	0989990114	(00) 23471317	xcheng@hotmail.com	台灣士賓資訊有限公司	990 桃園縣劍潭巷20號3樓	935 屏東天母路9段178號之2	\N	興復航空運輸，語言治療師，1985-06-05 ~ 2001-11-04	丹味企業 大學，博士	@jchang	\N	\N	\N	關渡	喬治亞 2006-06-10	\N	2025-07-20 18:33:52.011416	\N	2025-07-20 18:33:52.011417	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2022-05-16 透由 鄒宗翰 推薦	\N	123456-20250723192127
294	95	莊宇軒	\N	女	28101	\N	韓國	客家	\N	\N	R857467067	SV51691395	01-25016426	(07) 30335084	yanzhou@gmail.com	樂可旅遊集團有限公司	481 台北新興巷610號0樓	65167 蘆洲縣建國巷6段5號1樓	\N	風微廣場，銀行辦事員，1978-01-24 ~ 2021-10-16	德汎資訊有限公司 大學，博士	@jie78	\N	\N	\N	新生	喀麥隆 2010-04-22	\N	2025-07-20 18:33:52.011973	\N	2025-07-20 18:33:52.011973	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-02-23 透由 溫宗翰 推薦	\N	123456-20250723192127
295	96	範雅惠	\N	女	35055	\N	台灣	原住民	\N	民進黨	I308715647	oH37031754	(02) 37414857	0902925468	qianghe@hotmail.com	一統星巴克資訊有限公司	142 中和市東興路93號之7	95923 竹田縣中央巷98號之9	\N	一統超商，補習班老師，1992-10-08 ~ 2023-02-03	資華粧業（生資堂）有限公司 大學，學士	@chao50	\N	\N	\N	關渡	阿爾及利亞 1991-05-22	\N	2025-07-20 18:33:52.012519	\N	2025-07-20 18:33:52.012519	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-12-08 透由 李筱涵 推薦	\N	123456-20250723192127
296	97	劉靜怡	\N	男	25489	\N	中國	漢	\N	\N	S620429851	lg48698305	06 1903922	06-59801426	daiping@hotmail.com	華中航空資訊有限公司	83163 新營和平街4段3號之7	446 卑南德街93號8樓	\N	塑台石化，資訊專業人員，1997-01-23 ~ 2021-06-18	台灣五星電子 大學，博士	@mingye	\N	\N	\N	劍潭	馬拉威 1985-02-14	\N	2025-07-20 18:33:52.013405	\N	2025-07-20 18:33:52.013405	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2024-02-06 透由 李俊賢 推薦	\N	123456-20250723192127
297	98	孫雅婷	\N	女	31806	\N	台灣	客家	\N	民進黨	I406756020	Xe01929022	043 49044890	02-45333202	chengjie@yahoo.com	邦城文化事業	45450 竹田縣正義路3號之3	99620 汐止動物園路31號7樓	\N	華聯電子資訊有限公司，志願役軍官／士官／士兵，2011-08-18 ~ 2010-11-28	榮長航空 大學，博士	@junwan	\N	\N	\N	育英	芬蘭 2008-07-16	\N	2025-07-20 18:33:52.014497	\N	2025-07-20 18:33:52.014497	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2023-01-10 透由 宋嘉玲 推薦	\N	123456-20250723192127
298	99	李怡安	\N	女	29345	\N	日本	客家	\N	\N	R009928452	wa03211535	014 13219143	0977-972479	yan42@gu.com	心安食品服務（斯摩漢堡）股份有限公司	99204 古坑市頂福州巷433號9樓	115 新營昆陽巷14號之8	\N	達台電子有限公司，門市／店員／專櫃人員，2020-04-11 ~ 2006-11-06	資華粧業（生資堂）股份有限公司 大學，碩士	@yzou	\N	\N	\N	光華	約旦 1984-09-30	\N	2025-07-20 18:33:52.015215	\N	2025-07-20 18:33:52.015216	\N	\N	\N	\N	\N	\N	504de19c2d2f304e60a8ad34aee4e6a0	\N	\N	2021-04-14 透由 劉承翰 推薦	\N	123456-20250723192127
378	67	顧家瑋	\N	男	38033	\N	台灣	原住民	\N	中國共產黨	U818321928	ja54451895	08-44576521	08-23389888	hding@gmail.com	星燦國際旅行社股份有限公司	927 太保縣奇岩路3號4樓	22161 光復中興路9號7樓	女兒，孫惠雯	王鼎餐飲集團，人力／外勞仲介，1990-07-12 ~ 2015-06-06	華聯電子 大學，碩士	@yaojuan	Reverse-engineered dedicated contingency，王建宏	iterate efficient applications年會，黃淑惠	\N	關渡	委內瑞拉 1986-06-16	\N	2025-07-23 14:01:35.706589	\N	2025-07-23 14:01:35.706589	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-06-30 透由 楊信宏 介紹	孫惠雯，台灣軟微，女兒	782093-20250723205634
379	68	聶怡婷	\N	男	23683	\N	台灣	漢	\N	民進黨	I836151718	hy75480157	079 44738646	01 7752071	fengjie@yahoo.com	邦富人壽保險	75353 竹北民治街599號之5	578 梅山動物園街33號1樓	老闆，戴雅慧	台灣印無品良資訊有限公司，融資／信用業務人員，1971-10-27 ~ 1982-03-10	豐兆國際商業銀行股份有限公司 大學，學士	@liyi	Secured bandwidth-monitored task-force，田瑋婷	revolutionize rich bandwidth年會，陳婷婷	\N	勝利	荷蘭 1987-10-22	\N	2025-07-23 14:01:35.707305	\N	2025-07-23 14:01:35.707306	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2020-11-02 透由 包依婷 介紹	戴雅慧，律理法律有限公司，老闆	782093-20250723205634
401	90	葉柏翰	\N	女	31808	\N	日本	原住民	\N	國民黨	I910933711	im95473820	0984855716	(07) 36880294	xiongfang@hotmail.com	達台電子資訊有限公司	92786 白沙縣自強路139號之8	534 連江中興巷30號之2	朋友，潘鈺婷	台日積體電路有限公司，鍋爐操作技術人員，1984-04-15 ~ 2006-05-19	品王餐飲股份有限公司 大學，博士	@min18	Fundamental uniform challenge，倪怡萱	harness leading-edge functionalities年會，薛中山	\N	關渡	智利 1982-01-10	\N	2025-07-23 14:01:35.721943	\N	2025-07-23 14:01:35.721943	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-01-25 透由 鐘慧君 介紹	潘鈺婷，華晶國際酒店股份有限公司，朋友	782093-20250723205634
402	91	江中山	\N	女	31635	\N	中國	客家	\N	民進黨	T722397240	Bp22693121	0961820916	0901002314	caiguiying@xiang.tw	台灣酒菸有限公司	624 澎湖市明德巷181號1樓	48620 竹北莒光路743號7樓	朋友，呂雅雯	家宜家居（KIEA）有限公司，內業工程師，1989-03-06 ~ 1994-04-25	平太洋崇光百貨股份有限公司 大學，學士	@pangang	Balanced real-time collaboration，周惠如	expedite viral methodologies年會，張惠婷	\N	育英	剛果 2012-07-07	\N	2025-07-23 14:01:35.722879	\N	2025-07-23 14:01:35.722880	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-04-15 透由 謝宜君 介紹	呂雅雯，榮長海運有限公司，朋友	782093-20250723205634
403	92	賴冠宇	\N	女	27219	\N	韓國	客家	\N	國民黨	Z557676952	II36687887	03 4619689	01-9350305	guiyingdai@yahoo.com	資華粧業（生資堂）有限公司	728 臺東復興巷3號之0	201 金門市光華路2號9樓	姊妹，薛中山	光新三越百貨有限公司，清潔工，2020-08-07 ~ 1979-07-15	立三電視資訊有限公司 大學，學士	@qiang04	Customizable coherent approach，許冠霖	deliver granular synergies年會，雷靜宜	\N	大安	伊拉克 2008-12-05	\N	2025-07-23 14:01:35.723495	\N	2025-07-23 14:01:35.723495	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-09-22 透由 吳詩涵 介紹	薛中山，家宜家居（KIEA）資訊有限公司，姊妹	782093-20250723205634
404	93	佘柏翰	\N	女	28841	\N	中國	客家	\N	\N	B634111595	aW34281139	038 13034199	0970-051405	mwang@yahoo.com	台灣生資堂股份有限公司	95681 桃園東興巷32號之1	93228 草屯市四維街7段7號3樓	老闆，蘇欣怡	光新三越百貨資訊有限公司，光學工程師，2024-10-05 ~ 1971-01-07	隆豐大飯店（北台君悅）股份有限公司 大學，學士	@wanguiying	Persevering foreground concept，張詩婷	maximize bricks-and-clicks convergence年會，王靜宜	\N	中山	緬甸 1987-06-04	\N	2025-07-23 14:01:35.724138	\N	2025-07-23 14:01:35.724139	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-05-29 透由 劉宗翰 介紹	蘇欣怡，邦富人壽保險資訊有限公司，老闆	782093-20250723205634
405	94	徐佳蓉	\N	男	31371	\N	中國	原住民	\N	中國共產黨	Q462961367	lU15778711	(06) 86365155	07-31470827	xiulan09@qian.com	台灣雅萊（Y'ORÉAL）資訊有限公司	53064 雲林民族街5號之6	220 新營縣公園街97號6樓	兄弟，文怡婷	邦富人壽保險資訊有限公司，鍋爐操作技術人員，1970-09-04 ~ 2008-03-03	華中航空有限公司 大學，學士	@yong50	Distributed logistical circuit，洪俊宏	generate transparent portals年會，厲柏翰	\N	華興	摩爾多瓦 2013-06-24	\N	2025-07-23 14:01:35.724730	\N	2025-07-23 14:01:35.724730	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2023-03-06 透由 葉淑惠 介紹	文怡婷，台灣五星電子，兄弟	782093-20250723205634
406	95	黃雅琪	\N	女	23978	\N	韓國	客家	\N	國民黨	B276528928	fP64649111	(09) 96179817	0904-633786	taofeng@meng.tw	業商週刊資訊有限公司	538 板橋縣永寧路10號2樓	752 阿里山延平巷7號6樓	配偶，郭雅涵	旗花（台灣銀）行股份有限公司，珠寶及貴金屬技術員，1980-10-13 ~ 1999-10-07	海鴻精密有限公司 大學，學士	@yinxiulan	Object-based well-modulated product，龍家瑋	harness best-of-breed infrastructures年會，王庭瑋	\N	五福	秘魯 1980-06-01	\N	2025-07-23 14:01:35.725328	\N	2025-07-23 14:01:35.725329	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-07-27 透由 胡馨儀 介紹	郭雅涵，台灣BIM資訊有限公司，配偶	782093-20250723205634
407	96	劉中山	\N	男	30956	\N	韓國	原住民	\N	\N	E791124421	eF75361706	01-3366572	002 99901557	weilai@feng.net	台灣迪奧汽車	65130 員林關渡巷84號1樓	777 馬公縣公園巷51號3樓	女兒，閻宜庭	台灣雅萊（Y'ORÉAL）資訊有限公司，水利工程師，2010-07-31 ~ 2020-11-02	麥當當 大學，博士	@mshen	Re-contextualized heuristic hub，宋怡萱	expedite synergistic interfaces年會，李淑芬	\N	林森	巴西 1994-05-06	\N	2025-07-23 14:01:35.725957	\N	2025-07-23 14:01:35.725958	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-08-25 透由 任佳玲 介紹	閻宜庭，湖劍山世界，女兒	782093-20250723205634
408	97	張雅雯	\N	女	31568	\N	韓國	客家	\N	國民黨	A553307704	pK11729596	(04) 10907426	0980-501716	min56@gmail.com	德汎	277 台南市仁愛路6號之3	13580 新竹市大橋頭路79號之2	老闆，邱佳蓉	信永藥品資訊有限公司，助理教授，1978-08-05 ~ 1991-09-25	華聯電子資訊有限公司 大學，碩士	@hzhao	Programmable multi-tasking encoding，田詩婷	harness compelling e-services年會，劉怡萱	\N	昆陽	斐濟 1994-01-09	\N	2025-07-23 14:01:35.726798	\N	2025-07-23 14:01:35.726799	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2021-12-23 透由 吳婉婷 介紹	邱佳蓉，雄遠建設事業有限公司，老闆	782093-20250723205634
409	98	馬心怡	\N	男	28246	\N	韓國	客家	\N	國民黨	B679836314	pb24912711	0924078754	(08) 71563268	songlei@yahoo.com	台日積體電路資訊有限公司	54362 北港市育英路2號0樓	41814 阿里山縣新店巷33號5樓	同事，李志豪	心安食品服務（斯摩漢堡）股份有限公司，資料輸入人員，1970-03-19 ~ 1976-05-31	台北眾大捷運資訊有限公司 大學，學士	@yan32	Intuitive systemic architecture，張靜宜	orchestrate value-added web services年會，周志偉	\N	光復	中非共和國 1975-03-29	\N	2025-07-23 14:01:35.727518	\N	2025-07-23 14:01:35.727519	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2024-02-24 透由 楊慧君 介紹	李志豪，瑞輝大藥廠股份有限公司，同事	782093-20250723205634
410	99	王志偉	\N	女	37675	\N	日本	客家	\N	中國共產黨	T847457054	jA58971480	03 7257039	031 33614878	min69@yahoo.com	台灣來自水有限公司	791 汐止縣中山街92號之9	274 台北縣松山街2段8號之0	兄弟，呂雅雯	華中郵政，中醫師，1987-02-10 ~ 1979-05-17	大八電視有限公司 大學，博士	@lijie	Upgradable dedicated Internet solution，陳彥廷	syndicate synergistic action-items年會，周俊傑	\N	中山	剛果 2010-08-18	\N	2025-07-23 14:01:35.728180	\N	2025-07-23 14:01:35.728181	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-06-22 透由 郭家銘 介紹	呂雅雯，資華粧業（生資堂），兄弟	782093-20250723205634
411	100	李雅筑	\N	男	28535	\N	韓國	原住民	\N	國民黨	B857567667	LG80612943	029 18940626	0987010415	xiulan75@dai.com	律理法律有限公司	323 三重市正義巷2號2樓	13317 阿里山市大橋頭街4號之9	母親，田詩婷	湖劍山世界，汽車美容人員，1979-12-14 ~ 2013-04-17	碁宏有限公司 大學，學士	@jingwei	User-centric tangible moratorium，馬怡萱	envisioneer mission-critical applications年會，黃詩婷	\N	廣慈	沙烏地阿拉伯 2017-06-23	\N	2025-07-23 14:01:35.728827	\N	2025-07-23 14:01:35.728828	\N	\N	\N	\N	\N	\N	fb55aecf938abb381cdea8bd2addbfa8	\N	\N	2022-07-24 透由 李雅雯 介紹	田詩婷，台灣地土銀行有限公司，母親	782093-20250723205634
\.


--
-- Data for Name: person_profile_backup; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.person_profile_backup (id, photo_index, name, discovery_source, gender, birthday, birthplace, nationality, ethnicity, ancestral_origin, political_party, id_number, passport_number, phone, mobile, email, current_employer, address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, friends, frequent_locations, travel_history, remarks, created_at, created_by, updated_at, updated_by, extra_data, source_id, source_table, source_created_at, source_updated_at) FROM stdin;
1	\N	王小明	\N	男	\N	\N	台灣	\N	\N	\N	A123456789	G123456789	02-12345678	0912345678	\N	台科大研究所	\N	\N	父親：王大華，母親：李美麗	\N	北大畢業	\N	\N	\N	張三、李四	\N	\N	\N	2025-07-20 21:18:46.419392	\N	2025-07-20 21:18:46.419392	\N	\N	\N	\N	\N	\N
2	\N	李美美	\N	女	\N	\N	台灣	\N	\N	\N	B987654321	G987654321	03-87654321	0987654321	\N	上海科技公司	\N	\N	丈夫：陳大明，兒子：陳小強	\N	台科大畢業	\N	\N	\N	王小明、張小美	\N	\N	\N	2025-07-20 21:18:46.419392	\N	2025-07-20 21:18:46.419392	\N	\N	\N	\N	\N	\N
3	\N	張志強	\N	男	\N	\N	中國	\N	\N	\N	C456789123	H456789123	021-87654321	021-12345678	\N	北京科技公司	\N	\N	妻子：劉小芳，女兒：張小花	\N	上海大學畢業	\N	\N	\N	李美美、王大華	\N	\N	\N	2025-07-20 21:18:46.419392	\N	2025-07-20 21:18:46.419392	\N	\N	\N	\N	\N	\N
4	\N	陳雅文	\N	女	\N	\N	台灣	\N	\N	\N	D111222333	G111222333	04-33333333	0933333333	\N	東京電信股份有限公司	\N	\N	父親：陳大雄，母親：王美玉	\N	廣告大學畢業	\N	\N	\N	李美美、張志強	\N	\N	\N	2025-07-20 21:18:46.419392	\N	2025-07-20 21:18:46.419392	\N	\N	\N	\N	\N	\N
5	\N	劉建國	\N	男	\N	\N	台灣	\N	\N	\N	E999888777	G999888777	07-22222222	0922222222	\N	中國總商會台灣分部	\N	\N	妻子：林小慧，兒子：劉小偉	\N	高雄大學畢業	\N	\N	\N	王小明、陳雅文	\N	\N	\N	2025-07-20 21:18:46.419392	\N	2025-07-20 21:18:46.419392	\N	\N	\N	\N	\N	\N
\.


--
-- Data for Name: projects; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.projects (id, user_id, project_name, project_description, status, created_at, completed_at, updated_at) FROM stdin;
123456-20250723192127	123456	家族樹系統預設專案	系統升級前的原有資料專案，包含所有現有的人員資料和關係網絡	active	2025-07-23 19:21:27.127727	\N	2025-07-23 19:21:27.127727
888888-20250723205343	888888	測試專案	這是一個用於測試的專案	active	2025-07-23 20:53:43.461475	\N	2025-07-23 20:53:43.461475
782093-20250723205634	782093	測試2		active	2025-07-23 20:56:34.497244	\N	2025-07-23 20:56:34.497244
\.


--
-- Data for Name: relationship_layers; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.relationship_layers (id, source_person_id, target_person_id, relation_type, source_field, layer_depth, analysis_session_id, created_at, updated_at, project_id) FROM stdin;
1	219	206	母女	manual	1	manual_20250721031604_42133ee330f840df95fedc6be24bbb3d	2025-07-21 03:16:04.168161	2025-07-21 03:16:04.168161	123456-20250723192127
2	266	234	母女	manual	1	manual_20250721032104_fdf6236a76d246928e273690c72bd6fc	2025-07-21 03:21:04.66324	2025-07-21 03:21:04.66324	123456-20250723192127
3	248	234	朋友	manual	1	manual_20250721032824_2c9baad53d01432d98f629e0a99a5508	2025-07-21 03:28:24.907682	2025-07-21 03:28:24.907682	123456-20250723192127
4	250	234	朋友	manual	1	manual_20250721032841_92b9176d85054c16add462b2106fe6ed	2025-07-21 03:28:41.595805	2025-07-21 03:28:41.595805	123456-20250723192127
5	227	248	朋友	manual	1	manual_20250721032854_8e9a4fba5d6744ebb169c313d8f897eb	2025-07-21 03:28:54.26156	2025-07-21 03:28:54.26156	123456-20250723192127
6	230	234	朋友	manual	1	manual_20250721032903_0e69c41868fd4131a44f8c0050c87547	2025-07-21 03:29:03.403771	2025-07-21 03:29:03.403771	123456-20250723192127
7	291	248	朋友	manual	1	manual_20250721032924_f53c6d86058a468c8e4929119a83784a	2025-07-21 03:29:24.68747	2025-07-21 03:29:24.68747	123456-20250723192127
8	212	254	母子	manual	1	manual_20250721040853_e291e27ebc104c02b9878e6b93ccfcfc	2025-07-21 04:08:53.605733	2025-07-21 04:08:53.605733	123456-20250723192127
9	204	263	父女	manual	1	manual_20250721040912_1fa8c7e6d12e4ee2ad390c849cbdb730	2025-07-21 04:09:12.064134	2025-07-21 04:09:12.064134	123456-20250723192127
10	296	279	兄弟	manual	1	manual_20250721130636_988b4670e86e49238b5690bae77a57c6	2025-07-21 13:06:36.678	2025-07-21 13:06:36.678	123456-20250723192127
11	226	273	死黨	manual	1	manual_20250722124129_da4189fc8065447bbd1d38fd4d266b28	2025-07-22 12:41:29.144917	2025-07-22 12:41:29.144917	123456-20250723192127
12	269	249	父子	manual	1	manual_20250723152141_dbaed26662ba4d65b5fa110aee7d66af	2025-07-23 15:21:42.001809	2025-07-23 15:21:42.001809	123456-20250723192127
13	225	205	母子	manual	1	manual_20250723160407_e2c79b4be2804534b5ceac8bf7e15e58	2025-07-23 16:04:07.052193	2025-07-23 16:04:07.052193	123456-20250723192127
14	256	216	父子	manual	1	manual_20250723160430_a055be8df3124f4ca849c4d6543f9afe	2025-07-23 16:04:30.115098	2025-07-23 16:04:30.115098	123456-20250723192127
15	244	241	母子	manual	1	manual_20250723160604_230484d5651e4374a29e2509df4e6285	2025-07-23 16:06:04.797686	2025-07-23 16:06:04.797686	123456-20250723192127
16	231	201	朋友	manual	1	manual_20250723160625_a30c3eb4a08d4449ace62d053e78e2b5	2025-07-23 16:06:25.509191	2025-07-23 16:06:25.509191	123456-20250723192127
17	255	218	朋友	manual	1	manual_20250723160645_c230ca1ad3b547c1a321d08768f36281	2025-07-23 16:06:45.878278	2025-07-23 16:06:45.878278	123456-20250723192127
18	238	203	朋友	manual	1	manual_20250723160722_0b26bf5b02334951b91b17d54c08e4ec	2025-07-23 16:07:22.931059	2025-07-23 16:07:22.931059	123456-20250723192127
19	283	224	朋友	manual	1	manual_20250723160854_c7430a59e815477ead3ad3a8a9c28812	2025-07-23 16:08:54.284137	2025-07-23 16:08:54.284137	123456-20250723192127
20	202	262	朋友	manual	1	manual_20250723160909_e1a1edca27f6435198acdb473f39c0f3	2025-07-23 16:09:09.105584	2025-07-23 16:09:09.105584	123456-20250723192127
21	233	284	朋友	manual	1	manual_20250723161104_5a26100e872548959ea865e84430e5f0	2025-07-23 16:11:04.479613	2025-07-23 16:11:04.479613	123456-20250723192127
22	295	252	123	manual	1	manual_20250723172027_837d9786fdf24b3d97d1ea77db99c5c5	2025-07-23 17:20:27.546714	2025-07-23 17:20:27.546714	123456-20250723192127
\.


--
-- Data for Name: search_keywords; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.search_keywords (id, keyword, search_count, search_type, last_search_time, created_at, updated_at, project_id) FROM stdin;
146	09	11	fuzzy	2025-07-23 22:46:41.762417	2025-07-21 15:56:01.393332	2025-07-23 22:46:41.767706	123456-20250723192127
210	李	4	fuzzy	2025-07-23 22:54:20.6173	2025-07-23 22:50:09.518913	2025-07-23 22:54:20.618191	\N
5	東京電信	2	fuzzy	2025-07-20 21:17:44.021763	2025-07-20 21:17:44.021763	2025-07-23 19:21:27.127727	123456-20250723192127
12	王	2	fuzzy	2025-07-21 04:10:09.289313	2025-07-20 21:24:16.372206	2025-07-23 19:21:27.127727	123456-20250723192127
105	傅佳蓉	11	fuzzy	2025-07-21 04:17:41.868179	2025-07-21 03:30:35.373828	2025-07-23 19:21:27.127727	123456-20250723192127
104	漢神	3	fuzzy	2025-07-21 04:17:46.729426	2025-07-21 03:30:08.253524	2025-07-23 19:21:27.127727	123456-20250723192127
92	台積電	4	fuzzy	2025-07-21 04:18:59.692167	2025-07-21 02:17:35.50507	2025-07-23 19:21:27.127727	123456-20250723192127
2	台科大	11	fuzzy	2025-07-23 02:35:00.897532	2025-07-20 21:17:44.021763	2025-07-23 19:21:27.127727	123456-20250723192127
85	范立\\	6	fuzzy	2025-07-23 15:49:18.266625	2025-07-21 01:59:25.700927	2025-07-23 19:21:27.127727	123456-20250723192127
86	范立	19	fuzzy	2025-07-23 16:43:27.558968	2025-07-21 01:59:29.554895	2025-07-23 19:21:27.127727	123456-20250723192127
13	李光	87	fuzzy	2025-07-23 18:29:00.796797	2025-07-20 21:28:43.468548	2025-07-23 19:21:27.127727	123456-20250723192127
6	中國總商會	6	fuzzy	2025-07-23 18:29:03.343877	2025-07-20 21:17:44.021763	2025-07-23 19:21:27.127727	123456-20250723192127
4	廣告大學	10	fuzzy	2025-07-23 18:29:04.102851	2025-07-20 21:17:44.021763	2025-07-23 19:21:27.127727	123456-20250723192127
3	上海	19	fuzzy	2025-07-23 18:29:04.798507	2025-07-20 21:17:44.021763	2025-07-23 19:21:27.127727	123456-20250723192127
1	北大	27	fuzzy	2025-07-23 18:29:08.754355	2025-07-20 21:17:44.021763	2025-07-23 19:21:27.127727	123456-20250723192127
205	junjiang	1	fuzzy	2025-07-23 18:36:18.327642	2025-07-23 18:36:18.327642	2025-07-23 19:21:27.127727	123456-20250723192127
206	民進黨	2	fuzzy	2025-07-23 18:39:06.214429	2025-07-23 18:38:52.750185	2025-07-23 19:21:27.127727	123456-20250723192127
208	中國共產黨	1	fuzzy	2025-07-23 18:39:19.626358	2025-07-23 18:39:19.626358	2025-07-23 19:21:27.127727	123456-20250723192127
\.


--
-- Data for Name: search_logs; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.search_logs (id, keyword, search_type, result_count, search_time, ip_address, user_agent, project_id) FROM stdin;
185	09	fuzzy	146	2025-07-23 22:46:41.89382	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	\N
186	李	fuzzy	38	2025-07-23 22:50:09.545242	127.0.0.1	curl/8.1.2	\N
187	李	fuzzy	38	2025-07-23 22:50:19.571626	127.0.0.1	curl/8.1.2	\N
188	李	fuzzy	38	2025-07-23 22:54:09.629692	127.0.0.1	curl/8.1.2	\N
189	李	fuzzy	38	2025-07-23 22:54:20.639207	127.0.0.1	curl/8.1.2	\N
132	09	exact	0	2025-07-21 16:02:27.105644	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
134	09	fuzzy	68	2025-07-21 16:02:30.725944	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
176	北大	fuzzy	0	2025-07-23 18:29:07.691591	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
179	北大	fuzzy	0	2025-07-23 18:29:08.611712	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
177	北大	fuzzy	0	2025-07-23 18:29:08.240581	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
180	北大	fuzzy	0	2025-07-23 18:29:08.761617	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
1	李光	fuzzy	0	2025-07-20 21:28:43.488546	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
2	李光	exact	0	2025-07-20 21:29:07.597366	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
3	李光	fuzzy	0	2025-07-20 21:31:56.067538	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
4	李光	fuzzy	0	2025-07-20 21:33:53.482901	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
5	李光	fuzzy	0	2025-07-20 21:34:01.183787	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
6	北大	fuzzy	0	2025-07-20 21:34:07.727933	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
7	李光	fuzzy	0	2025-07-20 21:34:10.620634	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
8	李光	fuzzy	0	2025-07-20 21:34:34.339491	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
9	李光	fuzzy	0	2025-07-20 21:35:33.881253	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
10	李光	fuzzy	0	2025-07-20 21:35:35.654823	127.0.0.1	curl/8.1.2	123456-20250723192127
11	李光	fuzzy	0	2025-07-20 21:36:28.809576	127.0.0.1	curl/8.1.2	123456-20250723192127
12	李光	fuzzy	0	2025-07-20 21:36:32.696619	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
13	李光	fuzzy	0	2025-07-20 21:37:01.936713	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
14	李光	fuzzy	0	2025-07-20 21:37:03.681009	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
15	李光	fuzzy	0	2025-07-20 21:37:04.781392	127.0.0.1	curl/8.1.2	123456-20250723192127
16	李光	fuzzy	0	2025-07-20 21:37:06.989919	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
17	李光	fuzzy	0	2025-07-20 21:38:59.613836	127.0.0.1	curl/8.1.2	123456-20250723192127
18	李光	fuzzy	0	2025-07-20 21:39:44.393528	127.0.0.1	curl/8.1.2	123456-20250723192127
19	李光	fuzzy	0	2025-07-20 21:40:49.241326	127.0.0.1	curl/8.1.2	123456-20250723192127
20	李光	fuzzy	0	2025-07-20 21:41:22.263367	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
21	李光	fuzzy	0	2025-07-20 21:41:23.664375	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
22	李光	fuzzy	0	2025-07-20 21:41:32.454701	127.0.0.1	curl/8.1.2	123456-20250723192127
23	李光	fuzzy	2	2025-07-20 21:43:30.851925	127.0.0.1	curl/8.1.2	123456-20250723192127
24	李光	fuzzy	2	2025-07-20 21:43:44.427612	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
25	李光	fuzzy	2	2025-07-20 21:47:07.286084	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
26	李光	fuzzy	2	2025-07-20 21:49:53.327151	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
27	李光	fuzzy	2	2025-07-20 21:49:54.202175	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
28	北大	fuzzy	0	2025-07-20 21:49:56.150149	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
29	李光	fuzzy	2	2025-07-20 21:49:58.203593	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
30	李光	fuzzy	2	2025-07-20 21:50:45.49805	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
31	李光	fuzzy	2	2025-07-20 21:52:32.140774	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
32	李光	fuzzy	2	2025-07-20 21:52:41.108489	127.0.0.1	curl/8.1.2	123456-20250723192127
33	李光	fuzzy	2	2025-07-20 21:52:56.534806	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
34	李光	fuzzy	2	2025-07-20 21:52:59.687764	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
35	李光	fuzzy	2	2025-07-20 21:55:50.74317	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
36	李光	fuzzy	2	2025-07-20 21:55:55.751792	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
37	李光	fuzzy	2	2025-07-20 21:56:05.872983	::1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
38	李光	fuzzy	2	2025-07-20 22:09:30.615422	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
39	李光	fuzzy	2	2025-07-20 22:10:17.603937	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
40	李光	fuzzy	2	2025-07-20 22:12:07.728044	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
41	李光	fuzzy	2	2025-07-20 22:12:40.400054	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
42	李光	fuzzy	2	2025-07-20 22:12:47.651637	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
43	李光	fuzzy	2	2025-07-20 22:12:48.818538	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
44	李光	fuzzy	2	2025-07-20 22:12:49.844557	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
45	李光	fuzzy	2	2025-07-20 22:12:50.65611	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
46	李光	fuzzy	2	2025-07-20 22:12:51.135506	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
47	李光	fuzzy	2	2025-07-20 22:12:57.072284	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
48	李光	fuzzy	2	2025-07-20 22:14:41.042467	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
49	李光	fuzzy	2	2025-07-20 22:15:43.82929	127.0.0.1	curl/8.1.2	123456-20250723192127
50	李光	fuzzy	2	2025-07-20 22:15:43.925434	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
51	李光	fuzzy	2	2025-07-20 22:15:46.677167	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
52	李光	fuzzy	2	2025-07-20 22:16:08.590492	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
53	李光	fuzzy	2	2025-07-20 22:16:57.91062	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
54	李光	fuzzy	2	2025-07-20 22:19:36.363122	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
55	李光	fuzzy	2	2025-07-20 22:20:51.320465	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
56	李光	fuzzy	2	2025-07-21 01:58:48.212121	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
57	李光	fuzzy	2	2025-07-21 01:58:52.29757	127.0.0.1	curl/8.1.2	123456-20250723192127
58	北大	fuzzy	0	2025-07-21 01:59:02.06101	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
59	北大	fuzzy	0	2025-07-21 01:59:03.388403	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
60	李光	fuzzy	2	2025-07-21 01:59:04.479345	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
61	范立\\	fuzzy	0	2025-07-21 01:59:25.705993	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
62	范立	fuzzy	1	2025-07-21 01:59:29.563728	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
63	李光	fuzzy	2	2025-07-21 02:00:06.164772	127.0.0.1	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
64	李光	fuzzy	2	2025-07-21 02:00:56.486107	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
65	范立	fuzzy	1	2025-07-21 02:02:29.959848	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
66	李光	fuzzy	2	2025-07-21 02:03:26.146297	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
67	范立	exact	1	2025-07-21 02:17:22.824597	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
68	台積電	fuzzy	1	2025-07-21 02:17:35.513842	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
69	台積電	fuzzy	0	2025-07-21 02:34:29.084174	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
70	北大	fuzzy	0	2025-07-21 02:34:30.968676	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
71	台科大	fuzzy	0	2025-07-21 02:34:31.570915	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
72	范立	fuzzy	1	2025-07-21 02:34:32.163958	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
73	上海	fuzzy	1	2025-07-21 02:34:32.531465	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
74	上海	fuzzy	1	2025-07-21 02:34:33.100155	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
75	范立	fuzzy	1	2025-07-21 02:54:19.097639	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
76	台科大	fuzzy	0	2025-07-21 03:29:38.64199	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
77	廣告大學	fuzzy	0	2025-07-21 03:29:40.262725	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
78	范立	fuzzy	1	2025-07-21 03:29:41.124531	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
79	范立	fuzzy	1	2025-07-21 03:29:53.283296	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
80	漢神	fuzzy	0	2025-07-21 03:30:08.268643	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
81	傅佳蓉	fuzzy	1	2025-07-21 03:30:35.39243	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
82	傅佳蓉	fuzzy	1	2025-07-21 03:52:45.19612	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
83	傅佳蓉	fuzzy	1	2025-07-21 03:57:18.553763	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
84	傅佳蓉	fuzzy	1	2025-07-21 03:57:21.767347	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
85	傅佳蓉	fuzzy	1	2025-07-21 03:57:22.358947	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
86	傅佳蓉	fuzzy	1	2025-07-21 03:58:23.554845	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
87	范立	fuzzy	1	2025-07-21 03:58:26.022851	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
88	范立	fuzzy	1	2025-07-21 04:04:12.464418	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
89	傅佳蓉	fuzzy	1	2025-07-21 04:04:14.791268	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
90	傅佳蓉	fuzzy	1	2025-07-21 04:04:38.909423	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
91	北大	fuzzy	0	2025-07-21 04:06:30.319171	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
92	台積電	fuzzy	0	2025-07-21 04:06:31.66482	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
93	李光	fuzzy	2	2025-07-21 04:06:32.423998	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
94	范立\\	fuzzy	0	2025-07-21 04:06:33.072439	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
95	台科大	fuzzy	0	2025-07-21 04:06:33.593634	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
96	李光	fuzzy	2	2025-07-21 04:06:36.297393	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
97	李光	fuzzy	2	2025-07-21 04:08:11.548671	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
98	傅佳蓉	fuzzy	1	2025-07-21 04:08:14.80325	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
99	李光	fuzzy	2	2025-07-21 04:08:42.193875	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
100	李光	fuzzy	2	2025-07-21 04:09:15.779638	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
101	王	fuzzy	15	2025-07-21 04:10:09.29824	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
102	台科大	fuzzy	0	2025-07-21 04:11:26.334274	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
103	傅佳蓉	fuzzy	1	2025-07-21 04:16:47.529426	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
104	漢神	fuzzy	0	2025-07-21 04:16:49.763241	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
105	李光	fuzzy	2	2025-07-21 04:17:16.810773	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
106	傅佳蓉	fuzzy	1	2025-07-21 04:17:41.876495	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
107	漢神	fuzzy	0	2025-07-21 04:17:46.746428	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
108	李光	fuzzy	2	2025-07-21 04:17:48.517256	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
109	上海	fuzzy	1	2025-07-21 04:17:53.434255	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
110	台積電	fuzzy	0	2025-07-21 04:18:59.727751	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
111	廣告大學	fuzzy	0	2025-07-21 04:19:00.526148	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
112	上海	fuzzy	1	2025-07-21 04:19:01.183049	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
113	台科大	fuzzy	0	2025-07-21 04:19:01.69506	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
114	上海	fuzzy	1	2025-07-21 04:19:02.529454	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
115	上海	fuzzy	1	2025-07-21 04:20:19.174779	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
116	李光	fuzzy	2	2025-07-21 04:20:22.639677	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
117	廣告大學	fuzzy	0	2025-07-21 04:25:27.460923	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
118	台科大	fuzzy	0	2025-07-21 04:25:28.636836	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
119	上海	fuzzy	1	2025-07-21 04:25:29.387334	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
120	上海	fuzzy	1	2025-07-21 13:06:09.288732	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
121	北大	fuzzy	0	2025-07-21 15:54:36.707477	2401:e180:8d23:c30f:7951:cfd1:6bd6:272a	Mozilla/5.0 (iPhone; CPU iPhone OS 18_4_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.4 Mobile/15E148 Safari/604.1	123456-20250723192127
122	09	fuzzy	68	2025-07-21 15:56:01.522284	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
123	北大	fuzzy	0	2025-07-21 15:59:01.632573	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
124	北大	fuzzy	0	2025-07-21 15:59:02.89096	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
125	上海	fuzzy	1	2025-07-21 15:59:05.266182	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
126	廣告大學	fuzzy	0	2025-07-21 16:02:07.699213	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
127	中國總商會	fuzzy	0	2025-07-21 16:02:14.087444	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
128	范立	fuzzy	1	2025-07-21 16:02:16.763157	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
129	范立\\	fuzzy	0	2025-07-21 16:02:21.62598	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
130	范立	fuzzy	1	2025-07-21 16:02:23.176439	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
131	09	fuzzy	68	2025-07-21 16:02:24.126875	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
133	09	exact	0	2025-07-21 16:02:28.267921	2402:7500:a6e:86d3:6ce9:a429:12b0:aee4	Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1	123456-20250723192127
135	范立	fuzzy	1	2025-07-22 12:40:13.795997	60.249.176.12	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
136	李光	fuzzy	2	2025-07-22 12:40:20.531954	60.249.176.12	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
137	范立	fuzzy	1	2025-07-23 02:14:49.37646	125.229.25.98	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
138	09	fuzzy	68	2025-07-23 02:14:52.575081	125.229.25.98	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
139	范立	fuzzy	1	2025-07-23 02:17:36.647107	125.229.25.98	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
140	范立	fuzzy	1	2025-07-23 02:22:22.926449	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
141	范立\\	fuzzy	0	2025-07-23 02:23:00.322809	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
142	李光	fuzzy	2	2025-07-23 02:23:01.976564	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
143	上海	fuzzy	1	2025-07-23 02:23:06.672078	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
144	李光	fuzzy	2	2025-07-23 02:23:12.940758	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
145	上海	fuzzy	1	2025-07-23 02:30:09.545973	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
146	范立	fuzzy	1	2025-07-23 02:30:14.756163	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
147	09	fuzzy	68	2025-07-23 02:30:16.155067	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
148	李光	fuzzy	2	2025-07-23 02:31:21.260567	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
149	李光	fuzzy	2	2025-07-23 02:31:28.052471	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
150	李光	fuzzy	2	2025-07-23 02:34:47.947277	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
151	北大	fuzzy	0	2025-07-23 02:34:59.484804	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
152	台科大	fuzzy	0	2025-07-23 02:35:00.909285	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
153	廣告大學	fuzzy	0	2025-07-23 02:35:02.054729	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
154	中國總商會	fuzzy	0	2025-07-23 02:35:03.104333	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
155	范立\\	fuzzy	0	2025-07-23 02:35:03.982674	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
156	上海	fuzzy	1	2025-07-23 02:35:04.905353	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
157	09	fuzzy	68	2025-07-23 02:35:07.338182	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
158	09	fuzzy	68	2025-07-23 02:35:12.613519	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
159	上海	fuzzy	1	2025-07-23 02:39:47.406368	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
160	09	fuzzy	68	2025-07-23 02:40:51.788946	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
161	上海	fuzzy	1	2025-07-23 15:49:18.037194	125.227.141.223	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
162	范立\\	fuzzy	0	2025-07-23 15:49:18.274794	125.227.141.223	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
163	中國總商會	fuzzy	0	2025-07-23 15:49:19.001116	125.227.141.223	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
164	廣告大學	fuzzy	0	2025-07-23 15:49:19.673131	125.227.141.223	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
165	北大	fuzzy	0	2025-07-23 15:49:22.87623	125.227.141.223	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
166	上海	fuzzy	1	2025-07-23 15:49:23.319115	125.227.141.223	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
167	范立	fuzzy	1	2025-07-23 15:49:23.695368	125.227.141.223	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
168	范立	fuzzy	1	2025-07-23 15:49:26.767784	125.227.141.223	Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
169	北大	fuzzy	0	2025-07-23 16:43:26.51478	122.116.44.6	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
170	范立	fuzzy	1	2025-07-23 16:43:27.568663	122.116.44.6	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
171	李光	fuzzy	2	2025-07-23 16:43:28.322552	122.116.44.6	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
172	李光	fuzzy	2	2025-07-23 18:29:00.852794	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
173	中國總商會	fuzzy	0	2025-07-23 18:29:03.352439	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
174	廣告大學	fuzzy	0	2025-07-23 18:29:04.114328	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
175	上海	fuzzy	1	2025-07-23 18:29:04.806972	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
178	北大	fuzzy	0	2025-07-23 18:29:08.241823	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
181	junjiang	fuzzy	1	2025-07-23 18:36:18.341485	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
182	民進黨	fuzzy	25	2025-07-23 18:38:52.784564	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
183	民進黨	fuzzy	25	2025-07-23 18:39:06.226328	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
184	中國共產黨	fuzzy	19	2025-07-23 18:39:19.636537	125.229.25.98	Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36	123456-20250723192127
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
1	73	person_data	success	2025-07-20 22:21:19.779159	2025-07-20 22:21:19.760996
2	74	person_data	success	2025-07-20 22:21:19.784007	2025-07-20 22:21:19.783223
3	75	person_data	success	2025-07-20 22:21:19.784967	2025-07-20 22:21:19.784425
4	76	person_data	success	2025-07-20 22:21:19.785978	2025-07-20 22:21:19.785354
5	77	person_data	success	2025-07-20 22:21:19.786868	2025-07-20 22:21:19.786329
6	78	person_data	success	2025-07-20 22:21:19.787718	2025-07-20 22:21:19.787228
7	79	person_data	success	2025-07-20 22:21:19.788561	2025-07-20 22:21:19.788081
8	80	person_data	success	2025-07-20 22:21:19.789432	2025-07-20 22:21:19.788894
9	81	person_data	success	2025-07-20 22:21:19.790447	2025-07-20 22:21:19.789883
10	82	person_data	success	2025-07-20 22:21:19.797878	2025-07-20 22:21:19.790801
11	83	person_data	success	2025-07-20 22:21:19.799441	2025-07-20 22:21:19.798749
12	84	person_data	success	2025-07-20 22:21:19.800279	2025-07-20 22:21:19.799803
\.


--
-- Data for Name: sync_status; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.sync_status (id, last_sync_time, status, created_at) FROM stdin;
1	2025-07-20 22:21:19.800983	success	2025-07-20 22:21:19.801276
\.


--
-- Data for Name: user_favorites; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_favorites (id, person_id, person_name, last_viewed_time, favorited_at, created_at, updated_at, project_id) FROM stdin;
7	201	范立	\N	2025-07-23 02:23:08.443262	2025-07-23 02:23:08.443262	2025-07-23 19:21:27.127727	123456-20250723192127
8	212	李光	\N	2025-07-23 02:23:14.087987	2025-07-23 02:23:14.087987	2025-07-23 19:21:27.127727	123456-20250723192127
9	204	李光	\N	2025-07-23 02:23:14.53377	2025-07-23 02:23:14.53377	2025-07-23 19:21:27.127727	123456-20250723192127
10	239	喬淑惠	\N	2025-07-23 02:40:54.111348	2025-07-23 02:40:54.111348	2025-07-23 19:21:27.127727	123456-20250723192127
11	221	周淑慧	\N	2025-07-23 02:40:54.162092	2025-07-23 02:40:54.162092	2025-07-23 19:21:27.127727	123456-20250723192127
\.


--
-- Data for Name: user_update_file; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_update_file (id, filename, original_filename, file_path, file_size, md5_hash, upload_time, is_merged, merge_time, status, created_at, updated_at, project_id) FROM stdin;
39	分公司客戶基資表_親友補全多筆關聯_20250720_183351_f8b1756b.xlsx	分公司客戶基資表_親友補全多筆關聯.xlsx	/Users/user/FamilyTree/familytree-backend/user_upload/分公司客戶基資表_親友補全多筆關聯_20250720_183351_f8b1756b.xlsx	48397	504de19c2d2f304e60a8ad34aee4e6a0	2025-07-21 02:33:51.759516	t	2025-07-21 02:33:52.017279	merged	2025-07-21 02:33:51.765577	2025-07-23 19:21:27.127727	123456-20250723192127
40	分公司客戶基資表-廠商測試版_20250723_134801_9ca1476a.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/user/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250723_134801_9ca1476a.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-07-23 21:48:01.939071	t	2025-07-23 21:48:02.170363	merged	2025-07-23 21:48:01.942129	2025-07-23 21:48:02.171062	888888-20250723205343
41	分公司客戶基資表_測試用100筆_20250723_140135_b8c532f0.xlsx	分公司客戶基資表_測試用100筆.xlsx	/Users/user/FamilyTree/familytree-backend/user_upload/分公司客戶基資表_測試用100筆_20250723_140135_b8c532f0.xlsx	45673	fb55aecf938abb381cdea8bd2addbfa8	2025-07-23 22:01:35.474654	t	2025-07-23 22:01:35.730035	merged	2025-07-23 22:01:35.479777	2025-07-23 22:01:35.73059	782093-20250723205634
\.


--
-- Name: analysis_results_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.analysis_results_id_seq', 1, false);


--
-- Name: field_mapping_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.field_mapping_id_seq', 96, true);


--
-- Name: missing_persons_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.missing_persons_id_seq', 1, false);


--
-- Name: person_profile_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.person_profile_id_seq', 411, true);


--
-- Name: relationship_layers_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.relationship_layers_id_seq', 22, true);


--
-- Name: search_keywords_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.search_keywords_id_seq', 213, true);


--
-- Name: search_logs_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.search_logs_id_seq', 189, true);


--
-- Name: sync_error_log_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.sync_error_log_id_seq', 1, false);


--
-- Name: sync_log_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.sync_log_id_seq', 12, true);


--
-- Name: sync_status_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.sync_status_id_seq', 1, true);


--
-- Name: user_favorites_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.user_favorites_id_seq', 11, true);


--
-- Name: user_update_file_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.user_update_file_id_seq', 41, true);


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
-- Name: field_mapping field_mapping_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping
    ADD CONSTRAINT field_mapping_pkey PRIMARY KEY (id);


--
-- Name: missing_persons missing_persons_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT missing_persons_pkey PRIMARY KEY (id);


--
-- Name: person_profile person_profile_pkey; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.person_profile
    ADD CONSTRAINT person_profile_pkey PRIMARY KEY (id);


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
-- Name: relationship_layers relationship_layers_source_person_id_target_person_id_analy_key; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_source_person_id_target_person_id_analy_key UNIQUE (source_person_id, target_person_id, analysis_session_id);


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
-- Name: relationship_layers unique_relationship_per_session; Type: CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT unique_relationship_per_session UNIQUE (analysis_session_id, source_person_id, target_person_id);


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
-- Name: idx_analysis_results_person_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_analysis_results_person_id ON public.analysis_results USING btree (person_id);


--
-- Name: idx_analysis_results_person_unique; Type: INDEX; Schema: public; Owner: user
--

CREATE UNIQUE INDEX idx_analysis_results_person_unique ON public.analysis_results USING btree (person_id) WHERE ((status)::text = ANY ((ARRAY['pending'::character varying, 'processing'::character varying])::text[]));


--
-- Name: idx_analysis_results_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_analysis_results_project_id ON public.analysis_results USING btree (project_id);


--
-- Name: idx_analysis_results_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_analysis_results_status ON public.analysis_results USING btree (status);


--
-- Name: idx_analysis_session; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_analysis_session ON public.relationship_layers USING btree (analysis_session_id);


--
-- Name: idx_analysis_sessions_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_analysis_sessions_project_id ON public.analysis_sessions USING btree (project_id);


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
-- Name: idx_field_mapping_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_field_mapping_project_id ON public.field_mapping USING btree (project_id);


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
-- Name: idx_layer_depth; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_layer_depth ON public.relationship_layers USING btree (layer_depth);


--
-- Name: idx_missing_persons_name; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_missing_persons_name ON public.missing_persons USING btree (name);


--
-- Name: idx_missing_persons_project_id; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_missing_persons_project_id ON public.missing_persons USING btree (project_id);


--
-- Name: idx_missing_persons_session; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_missing_persons_session ON public.missing_persons USING btree (analysis_session_id);


--
-- Name: idx_missing_persons_source_person; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_missing_persons_source_person ON public.missing_persons USING btree (source_person_id);


--
-- Name: idx_missing_persons_status; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_missing_persons_status ON public.missing_persons USING btree (status);


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
-- Name: idx_relationship_layers_session_depth; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_relationship_layers_session_depth ON public.relationship_layers USING btree (analysis_session_id, layer_depth);


--
-- Name: idx_relationship_layers_session_source_target; Type: INDEX; Schema: public; Owner: user
--

CREATE INDEX idx_relationship_layers_session_source_target ON public.relationship_layers USING btree (analysis_session_id, source_person_id, target_person_id);


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
-- Name: field_mapping update_field_mapping_updated_at; Type: TRIGGER; Schema: public; Owner: user
--

CREATE TRIGGER update_field_mapping_updated_at BEFORE UPDATE ON public.field_mapping FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


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
-- Name: analysis_sessions analysis_sessions_root_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_sessions
    ADD CONSTRAINT analysis_sessions_root_person_id_fkey FOREIGN KEY (root_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- Name: analysis_results fk_analysis_results_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_results
    ADD CONSTRAINT fk_analysis_results_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: analysis_sessions fk_analysis_sessions_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.analysis_sessions
    ADD CONSTRAINT fk_analysis_sessions_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: field_mapping fk_field_mapping_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.field_mapping
    ADD CONSTRAINT fk_field_mapping_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: missing_persons fk_missing_persons_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT fk_missing_persons_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: person_profile fk_person_profile_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.person_profile
    ADD CONSTRAINT fk_person_profile_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: relationship_layers fk_relationship_layers_project; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT fk_relationship_layers_project FOREIGN KEY (project_id) REFERENCES public.projects(id) ON DELETE CASCADE;


--
-- Name: missing_persons fk_resolved_person; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT fk_resolved_person FOREIGN KEY (resolved_person_id) REFERENCES public.person_profile(id) ON DELETE SET NULL;


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
-- Name: missing_persons fk_source_person; Type: FK CONSTRAINT; Schema: public; Owner: user
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT fk_source_person FOREIGN KEY (source_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


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

