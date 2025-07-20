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
-- Name: update_updated_at_column(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_updated_at_column() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: analysis_results; Type: TABLE; Schema: public; Owner: -
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
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: analysis_results_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.analysis_results_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: analysis_results_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.analysis_results_id_seq OWNED BY public.analysis_results.id;


--
-- Name: analysis_sessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.analysis_sessions (
    id character varying(100) NOT NULL,
    root_person_id integer NOT NULL,
    max_depth integer DEFAULT 3 NOT NULL,
    status character varying(20) DEFAULT 'processing'::character varying NOT NULL,
    total_relationships integer DEFAULT 0,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    completed_at timestamp without time zone
);


--
-- Name: TABLE analysis_sessions; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.analysis_sessions IS '儲存分析會話資訊';


--
-- Name: COLUMN analysis_sessions.max_depth; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.analysis_sessions.max_depth IS '最大分析深度，預設為3層';


--
-- Name: field_mapping; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.field_mapping (
    id integer NOT NULL,
    excel_field_name character varying(100) NOT NULL,
    db_field_name character varying(100) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: field_mapping_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.field_mapping_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: field_mapping_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.field_mapping_id_seq OWNED BY public.field_mapping.id;


--
-- Name: missing_persons; Type: TABLE; Schema: public; Owner: -
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
    notes text
);


--
-- Name: TABLE missing_persons; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.missing_persons IS '記錄 AI 分析出但資料庫中不存在的人員';


--
-- Name: COLUMN missing_persons.name; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.name IS '人員姓名';


--
-- Name: COLUMN missing_persons.relation_type; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.relation_type IS '與來源人員的關係類型';


--
-- Name: COLUMN missing_persons.source_person_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.source_person_id IS '來源人員的 ID';


--
-- Name: COLUMN missing_persons.source_field; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.source_field IS '來源欄位（family_relationships, friends, activities）';


--
-- Name: COLUMN missing_persons.analysis_session_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.analysis_session_id IS '分析會話 ID';


--
-- Name: COLUMN missing_persons.layer_depth; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.layer_depth IS '分析層級深度';


--
-- Name: COLUMN missing_persons.discovered_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.discovered_at IS '發現時間';


--
-- Name: COLUMN missing_persons.status; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.status IS '狀態（pending: 待處理, resolved: 已解決, ignored: 忽略）';


--
-- Name: COLUMN missing_persons.resolved_person_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.resolved_person_id IS '解決後對應的人員 ID';


--
-- Name: COLUMN missing_persons.notes; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.missing_persons.notes IS '備註信息';


--
-- Name: missing_persons_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.missing_persons_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: missing_persons_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.missing_persons_id_seq OWNED BY public.missing_persons.id;


--
-- Name: person_data_backup; Type: TABLE; Schema: public; Owner: -
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


--
-- Name: person_profile; Type: TABLE; Schema: public; Owner: -
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
    important_friends text
);


--
-- Name: COLUMN person_profile.source_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.person_profile.source_id IS '來源資料的ID';


--
-- Name: COLUMN person_profile.source_table; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.person_profile.source_table IS '來源資料表名稱';


--
-- Name: COLUMN person_profile.source_created_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.person_profile.source_created_at IS '來源資料的創建時間';


--
-- Name: COLUMN person_profile.source_updated_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.person_profile.source_updated_at IS '來源資料的最後更新時間';


--
-- Name: person_profile_backup; Type: TABLE; Schema: public; Owner: -
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


--
-- Name: person_profile_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.person_profile_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: person_profile_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.person_profile_id_seq OWNED BY public.person_profile.id;


--
-- Name: search_keywords; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.search_keywords (
    id integer NOT NULL,
    keyword character varying(255) NOT NULL,
    search_count integer DEFAULT 1,
    search_type character varying(20) DEFAULT 'fuzzy'::character varying,
    last_search_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: TABLE search_keywords; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.search_keywords IS '搜索關鍵字記錄表：記錄用戶搜索的關鍵字和使用頻率';


--
-- Name: COLUMN search_keywords.keyword; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_keywords.keyword IS '搜索關鍵字';


--
-- Name: COLUMN search_keywords.search_count; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_keywords.search_count IS '搜索次數';


--
-- Name: COLUMN search_keywords.search_type; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_keywords.search_type IS '搜索類型：exact(精準) 或 fuzzy(模糊)';


--
-- Name: COLUMN search_keywords.last_search_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_keywords.last_search_time IS '最後搜索時間';


--
-- Name: COLUMN search_keywords.created_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_keywords.created_at IS '建立時間';


--
-- Name: COLUMN search_keywords.updated_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_keywords.updated_at IS '更新時間';


--
-- Name: popular_keywords; Type: VIEW; Schema: public; Owner: -
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


--
-- Name: relationship_layers; Type: TABLE; Schema: public; Owner: -
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
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: TABLE relationship_layers; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.relationship_layers IS '儲存遞迴分析的層級關係資料';


--
-- Name: COLUMN relationship_layers.layer_depth; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.relationship_layers.layer_depth IS '關係層級深度，1為直接關係，2為間接關係，以此類推';


--
-- Name: COLUMN relationship_layers.analysis_session_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.relationship_layers.analysis_session_id IS '分析會話ID，用於區分不同的分析任務';


--
-- Name: relationship_layers_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.relationship_layers_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: relationship_layers_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.relationship_layers_id_seq OWNED BY public.relationship_layers.id;


--
-- Name: search_keywords_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.search_keywords_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: search_keywords_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.search_keywords_id_seq OWNED BY public.search_keywords.id;


--
-- Name: search_logs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.search_logs (
    id integer NOT NULL,
    keyword character varying(255) NOT NULL,
    search_type character varying(20) NOT NULL,
    result_count integer DEFAULT 0,
    search_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    ip_address character varying(45),
    user_agent text
);


--
-- Name: TABLE search_logs; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.search_logs IS '搜索結果日誌表：記錄詳細的搜索行為，用於統計和分析';


--
-- Name: COLUMN search_logs.keyword; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_logs.keyword IS '搜索關鍵字';


--
-- Name: COLUMN search_logs.search_type; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_logs.search_type IS '搜索類型：exact 或 fuzzy';


--
-- Name: COLUMN search_logs.result_count; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_logs.result_count IS '搜索結果數量';


--
-- Name: COLUMN search_logs.search_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_logs.search_time IS '搜索時間';


--
-- Name: COLUMN search_logs.ip_address; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_logs.ip_address IS '搜索者IP地址';


--
-- Name: COLUMN search_logs.user_agent; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.search_logs.user_agent IS '用戶代理字符串';


--
-- Name: search_logs_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.search_logs_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: search_logs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.search_logs_id_seq OWNED BY public.search_logs.id;


--
-- Name: sync_error_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sync_error_log (
    id integer NOT NULL,
    source_id integer NOT NULL,
    source_table character varying(50) NOT NULL,
    error_message text NOT NULL,
    error_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: TABLE sync_error_log; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.sync_error_log IS '資料同步錯誤日誌表，記錄同步過程中的錯誤';


--
-- Name: sync_error_log_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.sync_error_log_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: sync_error_log_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.sync_error_log_id_seq OWNED BY public.sync_error_log.id;


--
-- Name: sync_log; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sync_log (
    id integer NOT NULL,
    source_id integer NOT NULL,
    source_table character varying(50) NOT NULL,
    status character varying(20) NOT NULL,
    sync_time timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: TABLE sync_log; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.sync_log IS '資料同步日誌表，記錄所有同步操作';


--
-- Name: sync_log_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.sync_log_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: sync_log_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.sync_log_id_seq OWNED BY public.sync_log.id;


--
-- Name: sync_status; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sync_status (
    id integer NOT NULL,
    last_sync_time timestamp without time zone NOT NULL,
    status character varying(20) NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: TABLE sync_status; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.sync_status IS '資料同步狀態表，記錄最後同步時間和狀態';


--
-- Name: sync_status_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.sync_status_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: sync_status_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.sync_status_id_seq OWNED BY public.sync_status.id;


--
-- Name: user_favorites; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.user_favorites (
    id integer NOT NULL,
    person_id integer NOT NULL,
    person_name character varying(100) NOT NULL,
    last_viewed_time timestamp without time zone,
    favorited_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: TABLE user_favorites; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.user_favorites IS '用戶收藏表：記錄用戶收藏的人員資料';


--
-- Name: COLUMN user_favorites.person_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.user_favorites.person_id IS '人員ID，關聯person_profile.id';


--
-- Name: COLUMN user_favorites.person_name; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.user_favorites.person_name IS '人員姓名（冗餘字段，提高查詢效能）';


--
-- Name: COLUMN user_favorites.last_viewed_time; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.user_favorites.last_viewed_time IS '最後查看時間';


--
-- Name: COLUMN user_favorites.favorited_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.user_favorites.favorited_at IS '收藏時間';


--
-- Name: COLUMN user_favorites.created_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.user_favorites.created_at IS '建立時間';


--
-- Name: COLUMN user_favorites.updated_at; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.user_favorites.updated_at IS '更新時間';


--
-- Name: user_favorites_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.user_favorites_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: user_favorites_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.user_favorites_id_seq OWNED BY public.user_favorites.id;


--
-- Name: user_update_file; Type: TABLE; Schema: public; Owner: -
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
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- Name: user_update_file_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.user_update_file_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: user_update_file_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.user_update_file_id_seq OWNED BY public.user_update_file.id;


--
-- Name: analysis_results id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.analysis_results ALTER COLUMN id SET DEFAULT nextval('public.analysis_results_id_seq'::regclass);


--
-- Name: field_mapping id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.field_mapping ALTER COLUMN id SET DEFAULT nextval('public.field_mapping_id_seq'::regclass);


--
-- Name: missing_persons id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.missing_persons ALTER COLUMN id SET DEFAULT nextval('public.missing_persons_id_seq'::regclass);


--
-- Name: person_profile id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.person_profile ALTER COLUMN id SET DEFAULT nextval('public.person_profile_id_seq'::regclass);


--
-- Name: relationship_layers id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers ALTER COLUMN id SET DEFAULT nextval('public.relationship_layers_id_seq'::regclass);


--
-- Name: search_keywords id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.search_keywords ALTER COLUMN id SET DEFAULT nextval('public.search_keywords_id_seq'::regclass);


--
-- Name: search_logs id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.search_logs ALTER COLUMN id SET DEFAULT nextval('public.search_logs_id_seq'::regclass);


--
-- Name: sync_error_log id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sync_error_log ALTER COLUMN id SET DEFAULT nextval('public.sync_error_log_id_seq'::regclass);


--
-- Name: sync_log id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sync_log ALTER COLUMN id SET DEFAULT nextval('public.sync_log_id_seq'::regclass);


--
-- Name: sync_status id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sync_status ALTER COLUMN id SET DEFAULT nextval('public.sync_status_id_seq'::regclass);


--
-- Name: user_favorites id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_favorites ALTER COLUMN id SET DEFAULT nextval('public.user_favorites_id_seq'::regclass);


--
-- Name: user_update_file id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_update_file ALTER COLUMN id SET DEFAULT nextval('public.user_update_file_id_seq'::regclass);


--
-- Name: analysis_results analysis_results_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.analysis_results
    ADD CONSTRAINT analysis_results_pkey PRIMARY KEY (id);


--
-- Name: analysis_sessions analysis_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.analysis_sessions
    ADD CONSTRAINT analysis_sessions_pkey PRIMARY KEY (id);


--
-- Name: field_mapping field_mapping_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.field_mapping
    ADD CONSTRAINT field_mapping_pkey PRIMARY KEY (id);


--
-- Name: missing_persons missing_persons_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT missing_persons_pkey PRIMARY KEY (id);


--
-- Name: person_profile person_profile_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.person_profile
    ADD CONSTRAINT person_profile_pkey PRIMARY KEY (id);


--
-- Name: relationship_layers relationship_layers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_pkey PRIMARY KEY (id);


--
-- Name: relationship_layers relationship_layers_source_person_id_target_person_id_analy_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_source_person_id_target_person_id_analy_key UNIQUE (source_person_id, target_person_id, analysis_session_id);


--
-- Name: search_keywords search_keywords_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.search_keywords
    ADD CONSTRAINT search_keywords_pkey PRIMARY KEY (id);


--
-- Name: search_logs search_logs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.search_logs
    ADD CONSTRAINT search_logs_pkey PRIMARY KEY (id);


--
-- Name: sync_error_log sync_error_log_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sync_error_log
    ADD CONSTRAINT sync_error_log_pkey PRIMARY KEY (id);


--
-- Name: sync_log sync_log_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sync_log
    ADD CONSTRAINT sync_log_pkey PRIMARY KEY (id);


--
-- Name: sync_status sync_status_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sync_status
    ADD CONSTRAINT sync_status_pkey PRIMARY KEY (id);


--
-- Name: field_mapping uk_field_mapping_excel_db; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.field_mapping
    ADD CONSTRAINT uk_field_mapping_excel_db UNIQUE (excel_field_name, db_field_name);


--
-- Name: search_keywords unique_keyword; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.search_keywords
    ADD CONSTRAINT unique_keyword UNIQUE (keyword);


--
-- Name: user_favorites unique_person_favorite; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_favorites
    ADD CONSTRAINT unique_person_favorite UNIQUE (person_id);


--
-- Name: relationship_layers unique_relationship_per_session; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT unique_relationship_per_session UNIQUE (analysis_session_id, source_person_id, target_person_id);


--
-- Name: user_favorites user_favorites_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_favorites
    ADD CONSTRAINT user_favorites_pkey PRIMARY KEY (id);


--
-- Name: user_update_file user_update_file_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.user_update_file
    ADD CONSTRAINT user_update_file_pkey PRIMARY KEY (id);


--
-- Name: idx_analysis_results_person_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_analysis_results_person_id ON public.analysis_results USING btree (person_id);


--
-- Name: idx_analysis_results_person_unique; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX idx_analysis_results_person_unique ON public.analysis_results USING btree (person_id) WHERE ((status)::text = ANY ((ARRAY['pending'::character varying, 'processing'::character varying])::text[]));


--
-- Name: idx_analysis_results_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_analysis_results_status ON public.analysis_results USING btree (status);


--
-- Name: idx_analysis_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_analysis_session ON public.relationship_layers USING btree (analysis_session_id);


--
-- Name: idx_favorited_at; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_favorited_at ON public.user_favorites USING btree (favorited_at DESC);


--
-- Name: idx_field_mapping_db_field; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_field_mapping_db_field ON public.field_mapping USING btree (db_field_name);


--
-- Name: idx_field_mapping_excel_field; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_field_mapping_excel_field ON public.field_mapping USING btree (excel_field_name);


--
-- Name: idx_keyword; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_keyword ON public.search_keywords USING btree (keyword);


--
-- Name: idx_keyword_log; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_keyword_log ON public.search_logs USING btree (keyword);


--
-- Name: idx_last_search_time; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_last_search_time ON public.search_keywords USING btree (last_search_time DESC);


--
-- Name: idx_last_viewed_time; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_last_viewed_time ON public.user_favorites USING btree (last_viewed_time DESC);


--
-- Name: idx_layer_depth; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_layer_depth ON public.relationship_layers USING btree (layer_depth);


--
-- Name: idx_missing_persons_name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_missing_persons_name ON public.missing_persons USING btree (name);


--
-- Name: idx_missing_persons_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_missing_persons_session ON public.missing_persons USING btree (analysis_session_id);


--
-- Name: idx_missing_persons_source_person; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_missing_persons_source_person ON public.missing_persons USING btree (source_person_id);


--
-- Name: idx_missing_persons_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_missing_persons_status ON public.missing_persons USING btree (status);


--
-- Name: idx_person_fulltext_search; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_fulltext_search ON public.person_profile USING btree (name, mobile, phone, id_number, passport_number);


--
-- Name: idx_person_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_id ON public.user_favorites USING btree (person_id);


--
-- Name: idx_person_id_number_search; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_id_number_search ON public.person_profile USING btree (id_number);


--
-- Name: idx_person_mobile_search; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_mobile_search ON public.person_profile USING btree (mobile);


--
-- Name: idx_person_name; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_name ON public.user_favorites USING btree (person_name);


--
-- Name: idx_person_name_search; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_name_search ON public.person_profile USING btree (name);


--
-- Name: idx_person_passport_search; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_passport_search ON public.person_profile USING btree (passport_number);


--
-- Name: idx_person_phone_search; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_phone_search ON public.person_profile USING btree (phone);


--
-- Name: idx_person_profile_file_md5; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_profile_file_md5 ON public.person_profile USING btree (file_md5);


--
-- Name: idx_person_profile_source; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_profile_source ON public.person_profile USING btree (source_id, source_table);


--
-- Name: idx_person_profile_source_file_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_person_profile_source_file_id ON public.person_profile USING btree (source_file_id);


--
-- Name: idx_relationship_layers_session_depth; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_relationship_layers_session_depth ON public.relationship_layers USING btree (analysis_session_id, layer_depth);


--
-- Name: idx_relationship_layers_session_source_target; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_relationship_layers_session_source_target ON public.relationship_layers USING btree (analysis_session_id, source_person_id, target_person_id);


--
-- Name: idx_result_count; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_result_count ON public.search_logs USING btree (result_count);


--
-- Name: idx_search_count; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_search_count ON public.search_keywords USING btree (search_count DESC);


--
-- Name: idx_search_time; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_search_time ON public.search_logs USING btree (search_time DESC);


--
-- Name: idx_source_person; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_source_person ON public.relationship_layers USING btree (source_person_id);


--
-- Name: idx_sync_error_source; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_sync_error_source ON public.sync_error_log USING btree (source_id, source_table);


--
-- Name: idx_sync_log_source; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_sync_log_source ON public.sync_log USING btree (source_id, source_table);


--
-- Name: idx_target_person; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_target_person ON public.relationship_layers USING btree (target_person_id);


--
-- Name: idx_user_update_file_md5; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_user_update_file_md5 ON public.user_update_file USING btree (md5_hash);


--
-- Name: idx_user_update_file_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_user_update_file_status ON public.user_update_file USING btree (status);


--
-- Name: idx_user_update_file_upload_time; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_user_update_file_upload_time ON public.user_update_file USING btree (upload_time);


--
-- Name: field_mapping update_field_mapping_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER update_field_mapping_updated_at BEFORE UPDATE ON public.field_mapping FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: search_keywords update_search_keywords_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER update_search_keywords_updated_at BEFORE UPDATE ON public.search_keywords FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: user_favorites update_user_favorites_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER update_user_favorites_updated_at BEFORE UPDATE ON public.user_favorites FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: user_update_file update_user_update_file_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER update_user_update_file_updated_at BEFORE UPDATE ON public.user_update_file FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: analysis_sessions analysis_sessions_root_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.analysis_sessions
    ADD CONSTRAINT analysis_sessions_root_person_id_fkey FOREIGN KEY (root_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- Name: missing_persons fk_resolved_person; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT fk_resolved_person FOREIGN KEY (resolved_person_id) REFERENCES public.person_profile(id) ON DELETE SET NULL;


--
-- Name: missing_persons fk_source_person; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.missing_persons
    ADD CONSTRAINT fk_source_person FOREIGN KEY (source_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- Name: relationship_layers relationship_layers_source_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_source_person_id_fkey FOREIGN KEY (source_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- Name: relationship_layers relationship_layers_target_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_target_person_id_fkey FOREIGN KEY (target_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

