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
-- Data for Name: field_mapping; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.field_mapping (id, excel_field_name, db_field_name, created_at, updated_at) FROM stdin;
1	姓名	name	2025-07-29 21:09:08.40895	2025-07-29 21:09:08.40895
2	名字	name	2025-07-29 21:09:08.413076	2025-07-29 21:09:08.413076
3	Name	name	2025-07-29 21:09:08.41335	2025-07-29 21:09:08.41335
4	性別	gender	2025-07-29 21:09:08.41351	2025-07-29 21:09:08.41351
5	Gender	gender	2025-07-29 21:09:08.413772	2025-07-29 21:09:08.413772
6	生日	birthday	2025-07-29 21:09:08.414401	2025-07-29 21:09:08.414401
7	出生日期	birthday	2025-07-29 21:09:08.41501	2025-07-29 21:09:08.41501
8	Birthday	birthday	2025-07-29 21:09:08.41595	2025-07-29 21:09:08.41595
9	Date of Birth	birthday	2025-07-29 21:09:08.417162	2025-07-29 21:09:08.417162
10	出生地	birthplace	2025-07-29 21:09:08.417892	2025-07-29 21:09:08.417892
11	父母戶籍所在地	birthplace	2025-07-29 21:09:08.41866	2025-07-29 21:09:08.41866
12	Birthplace	birthplace	2025-07-29 21:09:08.419663	2025-07-29 21:09:08.419663
13	Place of Birth	birthplace	2025-07-29 21:09:08.420134	2025-07-29 21:09:08.420134
14	國籍	nationality	2025-07-29 21:09:08.42059	2025-07-29 21:09:08.42059
15	Nationality	nationality	2025-07-29 21:09:08.420817	2025-07-29 21:09:08.420817
16	民族	ethnicity	2025-07-29 21:09:08.421015	2025-07-29 21:09:08.421015
17	Ethnicity	ethnicity	2025-07-29 21:09:08.421194	2025-07-29 21:09:08.421194
18	籍貫	ancestral_origin	2025-07-29 21:09:08.421349	2025-07-29 21:09:08.421349
19	祖父戶籍所在地	ancestral_origin	2025-07-29 21:09:08.421566	2025-07-29 21:09:08.421566
20	Ancestral Home	ancestral_origin	2025-07-29 21:09:08.42176	2025-07-29 21:09:08.42176
21	黨派	political_party	2025-07-29 21:09:08.421926	2025-07-29 21:09:08.421926
22	Political Party	political_party	2025-07-29 21:09:08.42216	2025-07-29 21:09:08.42216
23	身分證號碼	id_number	2025-07-29 21:09:08.422394	2025-07-29 21:09:08.422394
24	身份證號碼	id_number	2025-07-29 21:09:08.422772	2025-07-29 21:09:08.422772
25	ID Number	id_number	2025-07-29 21:09:08.423172	2025-07-29 21:09:08.423172
26	Identity Number	id_number	2025-07-29 21:09:08.423527	2025-07-29 21:09:08.423527
27	護照號碼	passport_number	2025-07-29 21:09:08.423931	2025-07-29 21:09:08.423931
28	Passport Number	passport_number	2025-07-29 21:09:08.42459	2025-07-29 21:09:08.42459
29	電話	phone	2025-07-29 21:09:08.425389	2025-07-29 21:09:08.425389
30	Phone	phone	2025-07-29 21:09:08.425951	2025-07-29 21:09:08.425951
31	Telephone	phone	2025-07-29 21:09:08.426755	2025-07-29 21:09:08.426755
32	行動電話	mobile	2025-07-29 21:09:08.427613	2025-07-29 21:09:08.427613
33	手機	mobile	2025-07-29 21:09:08.428519	2025-07-29 21:09:08.428519
34	Mobile	mobile	2025-07-29 21:09:08.428879	2025-07-29 21:09:08.428879
35	Cell Phone	mobile	2025-07-29 21:09:08.429112	2025-07-29 21:09:08.429112
36	電子信箱	email	2025-07-29 21:09:08.429283	2025-07-29 21:09:08.429283
37	Email	email	2025-07-29 21:09:08.430036	2025-07-29 21:09:08.430036
38	E-mail	email	2025-07-29 21:09:08.430363	2025-07-29 21:09:08.430363
39	現職單位	current_employer	2025-07-29 21:09:08.431393	2025-07-29 21:09:08.431393
40	工作單位	current_employer	2025-07-29 21:09:08.432622	2025-07-29 21:09:08.432622
41	Current Workplace	current_employer	2025-07-29 21:09:08.434006	2025-07-29 21:09:08.434006
42	現居地址	address	2025-07-29 21:09:08.434811	2025-07-29 21:09:08.434811
43	居住地址	address	2025-07-29 21:09:08.435497	2025-07-29 21:09:08.435497
44	Current Address	address	2025-07-29 21:09:08.43585	2025-07-29 21:09:08.43585
45	通訊地址	mailing_address	2025-07-29 21:09:08.436245	2025-07-29 21:09:08.436245
46	聯絡地址	mailing_address	2025-07-29 21:09:08.436717	2025-07-29 21:09:08.436717
47	Mailing Address	mailing_address	2025-07-29 21:09:08.43704	2025-07-29 21:09:08.43704
48	親屬關係	family_relationships	2025-07-29 21:09:08.437358	2025-07-29 21:09:08.437358
49	Family Relationships	family_relationships	2025-07-29 21:09:08.437705	2025-07-29 21:09:08.437705
50	經歷	experience	2025-07-29 21:09:08.439678	2025-07-29 21:09:08.439678
51	工作經歷	experience	2025-07-29 21:09:08.442615	2025-07-29 21:09:08.442615
52	Experience	experience	2025-07-29 21:09:08.446389	2025-07-29 21:09:08.446389
53	學歷	education	2025-07-29 21:09:08.446895	2025-07-29 21:09:08.446895
54	Education	education	2025-07-29 21:09:08.448061	2025-07-29 21:09:08.448061
55	網路帳號	online_accounts	2025-07-29 21:09:08.448556	2025-07-29 21:09:08.448556
56	Online Accounts	online_accounts	2025-07-29 21:09:08.449549	2025-07-29 21:09:08.449549
57	著作	publications	2025-07-29 21:09:08.450559	2025-07-29 21:09:08.450559
58	Publications	publications	2025-07-29 21:09:08.451613	2025-07-29 21:09:08.451613
59	參與活動	activities	2025-07-29 21:09:08.452232	2025-07-29 21:09:08.452232
60	Activities	activities	2025-07-29 21:09:08.452602	2025-07-29 21:09:08.452602
61	重要友人	important_friends	2025-07-29 21:09:08.453391	2025-07-29 21:09:08.453391
62	Important Friends	important_friends	2025-07-29 21:09:08.453848	2025-07-29 21:09:08.453848
63	經常出入場所	frequent_locations	2025-07-29 21:09:08.454096	2025-07-29 21:09:08.454096
64	Frequent Places	frequent_locations	2025-07-29 21:09:08.454268	2025-07-29 21:09:08.454268
65	出國紀錄	travel_history	2025-07-29 21:09:08.45441	2025-07-29 21:09:08.45441
66	Travel Records	travel_history	2025-07-29 21:09:08.454541	2025-07-29 21:09:08.454541
67	備註	remarks	2025-07-29 21:09:08.45466	2025-07-29 21:09:08.45466
68	Notes	remarks	2025-07-29 21:09:08.454806	2025-07-29 21:09:08.454806
69	Remarks	remarks	2025-07-29 21:09:08.454989	2025-07-29 21:09:08.454989
70	發掘經過	discovery_process	2025-07-29 21:09:08.455158	2025-07-29 21:09:08.455158
71	Discovery Process	discovery_process	2025-07-29 21:09:08.455308	2025-07-29 21:09:08.455308
72	照片	photo_index	2025-07-29 21:09:08.455505	2025-07-29 21:09:08.455505
73	Photo	photo_index	2025-07-29 21:09:08.455653	2025-07-29 21:09:08.455653
74	Picture	photo_index	2025-07-29 21:09:08.455776	2025-07-29 21:09:08.455776
75	經歷(單位，職稱，任職期間)	experience	2025-07-29 21:09:08.455899	2025-07-29 21:09:08.455899
76	親屬關係(職稱，姓名)	family_relationships	2025-07-29 21:09:08.456008	2025-07-29 21:09:08.456008
77	著作(名稱，共同作者)	publications	2025-07-29 21:09:08.456112	2025-07-29 21:09:08.456112
78	參與活動(活動名稱，參與人士)	activities	2025-07-29 21:09:08.456213	2025-07-29 21:09:08.456213
79	重要友人(姓名，單位，關聯事件)	important_friends	2025-07-29 21:09:08.456336	2025-07-29 21:09:08.456336
80	出生地(父母戶籍所在地)	birthplace	2025-07-29 21:09:08.456453	2025-07-29 21:09:08.456453
81	籍貫(祖父戶籍所在地)	ancestral_origin	2025-07-29 21:09:08.456563	2025-07-29 21:09:08.456563
\.


--
-- Data for Name: mergedpersons; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.mergedpersons (id, project_id, photo, name, discovery_process, gender, birthday, birthplace, nationality, ethnicity, ancestral_home, political_party, id_number, passport_number, phone, mobile, email, current_workplace, current_address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, important_friends, frequent_places, travel_records, notes, created_at, created_by, updated_at, updated_by, source_person_a_id, source_person_b_id) FROM stdin;
\.


--
-- Data for Name: person_profile; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.person_profile (id, photo_index, name, discovery_source, gender, birthday, birthplace, nationality, ethnicity, ancestral_origin, political_party, id_number, passport_number, phone, mobile, email, current_employer, address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, friends, frequent_locations, travel_history, remarks, created_at, created_by, updated_at, updated_by, extra_data, source_id, source_table, source_created_at, source_updated_at, file_md5, source_file_id, source_file_name, discovery_process, important_friends, project_id) FROM stdin;
1	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-29 13:15:46.924786	\N	2025-07-29 13:15:46.924796	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	968348-20250729211539
2	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.933122	\N	2025-07-29 13:15:46.933123	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	968348-20250729211539
3	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.934327	\N	2025-07-29 13:15:46.934328	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	968348-20250729211539
4	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.934925	\N	2025-07-29 13:15:46.934925	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	968348-20250729211539
5	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.935457	\N	2025-07-29 13:15:46.935457	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	968348-20250729211539
6	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.935980	\N	2025-07-29 13:15:46.935980	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	968348-20250729211539
7	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.936502	\N	2025-07-29 13:15:46.936502	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	968348-20250729211539
8	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-29 13:15:46.937096	\N	2025-07-29 13:15:46.937096	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539
9	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.937681	\N	2025-07-29 13:15:46.937681	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539
10	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938157	\N	2025-07-29 13:15:46.938158	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	968348-20250729211539
11	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938540	\N	2025-07-29 13:15:46.938541	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	968348-20250729211539
12	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-29 13:15:46.938809	\N	2025-07-29 13:15:46.938809	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	968348-20250729211539
13	0001	范立	\N	男	1988-06-07	\N	中國	滿族	\N	\N	320204197608071330	EJ9804516	+8113457839955	13357913171	84313835435671@qq.com\nfanli555@gmail.com	東京大學經濟系研究生一年級	東京都品川	上海市惠暢里小區50號60室	\N	上海華為公司國際商務部，實習生，2021年6-8月	復旦大學經濟系	FB：100009163673467\n抖音：2160535467\n微博：lilifzn111（用戶名：鋤禾不苦）\n微信：wxid_f7g78ox8z21349	\N	\N	\N	\N	2015年1月，台灣2018年2月，美國\n2019年7月，泰國	\N	2025-07-30 23:39:00.331737+08	\N	2025-07-30 23:39:00.331748+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃三三114年1月透由約聘人員汪一德轉介結識。	\N	636216-20250730233734
14	0002	趙威	\N	男	1975-04-15	\N	中國	漢	\N	\N	460004197502151560	\N	+8613884937455	13884937455	vigor666@126.com	煙台大學中文系	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	山東煙台大學，中文系教師， 2001.7-迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.338127+08	\N	2025-07-30 23:39:00.338127+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	業務黃二二114年4月透由約聘人員蘇妮轉介結識。	\N	636216-20250730233734
15	0003	項依潔	\N	女	1975-10-29	\N	中國	漢	\N	\N	370629197510294987	\N	+8613573590064	13573590064	ruru215@163.com	煙台大學文學與新聞傳播學系副教授	山東省煙臺市文山區大江路	山東省煙臺市文山區大江路	\N	煙臺大學，人文學院副教授， 2000迄今	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.339134+08	\N	2025-07-30 23:39:00.339134+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年5月透由區內徵信查獲資訊	\N	636216-20250730233734
16	0004	李光	\N	男	1990-05-17	\N	中國	漢	\N	\N	371082199005173613	\N	+861335687453	+861335687453	erty@sina.com	麗星郵輪總務	台北市中山區	台北市中山區	\N	麗星郵輪海員	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.340256+08	\N	2025-07-30 23:39:00.340256+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	114年4月30日博覽會發掘	\N	636216-20250730233734
17	0005	沈家新	\N	女	1978-09-18	\N	中國	漢	\N	\N	32020419780918162X	\N	\N	+8613357912344\n0917851414	\N	永慶房屋仲介	臺北市萬華區	臺北市萬華區	\N	\N	北護專	FB：100009163384924\n、100032652349399（沈員幫女兒蕭圓圓創的）\n、100092577066960（蕭大方）\n 抖音：2160535827	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.341044+08	\N	2025-07-30 23:39:00.341044+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	111年透由王大陸介紹	\N	636216-20250730233734
18	0006	唐伯虎	\N	男	1967-08-20	\N	中國	漢	\N	\N	\N	\N	\N	17188407888	bhtang@gmail.com\nloverongbh@yahoo.com	50藍西門店店長	\N	\N	\N	\N	\N	FB(未再使用) ：100063587324567	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.34179+08	\N	2025-07-30 23:39:00.34179+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	636216-20250730233734
19	0007	楊習五	\N	男	1983-04-23	\N	中國	漢	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\n0988-206-038	hi@ailleurslab.com	momo藝術策畫經理	新北市新店區文化路	新北市新店區文化路	\N	1. 法國貝桑松高等美術學院客座教授，待查\n2. 法國留尼旺高等美術學院客座教授，待查\n3. ACEA藝術文化教育協會 創辦人，2015年       瀋陽市「別處」美術館館長，2018年\n4. 蓓蔻城堡國際藝術駐留工作， 2020年\n5. 臺灣巫登益美術館新北館館長，2022年	1. 法國貝桑松高等美術學院，學士\n2. 英國哈德斯菲爾德大學，碩士\n3. 法國博艮第大學藝術史與藝術管理 ，博士	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.342643+08	\N	2025-07-30 23:39:00.342643+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	112年透由蔡怡萱介紹	\N	636216-20250730233734
20	0008	李青春	\N	女	1983-07-10	\N	中國	漢	\N	\N	\N	\N	\N	0935-787-122	\N	旭東廣告工程	\N	\N	\N	社團法人羅東鎮新住民關懷服務協會總幹事，現職	\N	\N	\N	\N	\N	女神美甲美睫	\N	\N	2025-07-30 23:39:00.345285+08	\N	2025-07-30 23:39:00.345285+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734
21	0009	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會理事長	宜蘭	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.346183+08	\N	2025-07-30 23:39:00.346183+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734
22	0010	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	羅東鎮新住民關懷協會常務監事	宜蘭	\N	\N	\N	淡江大學	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.347081+08	\N	2025-07-30 23:39:00.347081+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	113年透由邱阿霞介紹	\N	636216-20250730233734
23	0011	羅亞璇	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	大創有限公司副總經理	台北市中正區	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.347804+08	\N	2025-07-30 23:39:00.347804+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	636216-20250730233734
24	0012	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	大創有限公司人事主管	新北市蘆洲	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-30 23:39:00.348341+08	\N	2025-07-30 23:39:00.348341+08	\N	\N	\N	\N	\N	\N	4e2e6de8e658c9cc98c8051df9151c45	\N	\N	\N	\N	636216-20250730233734
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
-- Data for Name: projects; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.projects (id, user_id, project_name, project_description, status, created_at, completed_at, updated_at) FROM stdin;
303334-20250729211457	303334	test	test	deleted	2025-07-29 21:14:57.434749	\N	2025-07-29 21:15:25.85044
test01-20250730004405	test01	測試專案（已更新）	這是一個已更新的API測試專案	deleted	2025-07-30 00:44:05.964717	2025-07-30 00:44:13.586968	2025-07-30 00:44:29.655937
968348-20250729211539	968348	123	\N	\N	2025-07-29 21:15:39.289037	\N	2025-07-30 23:30:01.126062
636216-20250730233734	636216	test	test	\N	2025-07-30 23:37:34.873419	\N	2025-07-31 13:27:33.557869
499307-20250731132801	499307	test	tett	\N	2025-07-31 13:28:01.452892	\N	2025-07-31 13:28:03.669852
922142-20250731132807	922142	testt	testt	\N	2025-07-31 13:28:07.829965	\N	2025-07-31 13:54:06.951596
883487-20250731135414	883487	text	test	\N	2025-07-31 13:54:14.652295	\N	2025-07-31 13:55:18.425069
897217-20250731135535	897217	ㄅㄅ	ㄅ	\N	2025-07-31 13:55:35.142551	\N	2025-07-31 14:00:26.724618
934545-20250731140318	934545	123	2222	\N	2025-07-31 14:03:18.145623	\N	2025-07-31 14:03:20.724959
123456-20250731141338	123456	測試專案 2025-07-31T06:13:38.023Z (已更新)	這是更新後的描述	deleted	2025-07-31 14:13:38.079053	\N	2025-07-31 14:13:38.114002
123456-20250731140902	123456	測試專案 2025-07-31T06:09:02.039Z	這是一個測試專案ㄅ1	\N	2025-07-31 14:09:02.097685	\N	2025-07-31 14:15:05.481039
577050-20250731141511	577050	123	123	active	2025-07-31 14:15:11.431322	\N	2025-07-31 14:15:11.431322
\.


--
-- Data for Name: relationship_layers; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.relationship_layers (id, source_person_id, target_person_id, relation_type, source_field, created_at, updated_at, project_id, visual_analysis_graph_id) FROM stdin;
1	1	2	朋友	manual	2025-07-30 02:02:47.098392	2025-07-30 02:02:47.098392	968348-20250729211539	2
2	1	3	朋友	manual	2025-07-30 02:03:32.880944	2025-07-30 02:03:32.880944	968348-20250729211539	2
5	1	6	朋友	manual	2025-07-30 02:07:00.045372	2025-07-30 02:07:00.045372	968348-20250729211539	2
6	5	4	ㄇ	manual	2025-07-30 02:07:23.622036	2025-07-30 02:07:23.622036	968348-20250729211539	2
7	4	2	造	manual	2025-07-30 02:07:31.052867	2025-07-30 02:07:31.052867	968348-20250729211539	2
8	3	4	朝	manual	2025-07-30 02:07:35.969011	2025-07-30 02:07:35.969011	968348-20250729211539	2
9	4	11	照	manual	2025-07-30 02:07:39.302771	2025-07-30 02:07:39.302771	968348-20250729211539	2
10	11	1	母女	manual	2025-07-31 01:21:11.309977	2025-07-31 01:21:11.309977	968348-20250729211539	2
11	1	4	母女	manual	2025-07-31 01:21:20.910533	2025-07-31 01:21:20.910533	968348-20250729211539	2
\.


--
-- Data for Name: relationship_layers_backup; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.relationship_layers_backup (id, source_person_id, target_person_id, relation_type, source_field, layer_depth, analysis_session_id, created_at, updated_at, project_id, visual_analysis_graph_id) FROM stdin;
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

COPY public.user_favorites (id, person_id, person_name, last_viewed_time, favorited_at, created_at, updated_at, project_id) FROM stdin;
1	3	項依潔	\N	2025-07-30 01:50:23.387636	2025-07-30 01:50:23.387636	2025-07-30 01:50:23.387636	968348-20250729211539
2	2	趙威	\N	2025-07-30 01:50:24.137089	2025-07-30 01:50:24.137089	2025-07-30 01:50:24.137089	968348-20250729211539
5	4	李光	\N	2025-07-30 23:24:08.632393	2025-07-30 23:24:08.632393	2025-07-30 23:24:08.632393	968348-20250729211539
6	15	項依潔	\N	2025-07-31 01:35:19.077568	2025-07-31 01:35:19.077568	2025-07-31 01:35:19.077568	636216-20250730233734
\.


--
-- Data for Name: user_update_file; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.user_update_file (id, filename, original_filename, file_path, file_size, md5_hash, upload_time, is_merged, merge_time, status, created_at, updated_at, project_id) FROM stdin;
1	分公司客戶基資表-廠商測試版_20250729_131546_ac400430.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree-1/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250729_131546_ac400430.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-07-29 21:15:46.787473	t	2025-07-30 23:39:00.349194	merged	2025-07-29 21:15:46.789843	2025-07-30 23:39:00.349591	968348-20250729211539
2	分公司客戶基資表-廠商測試版_20250730_153900_e87377f3.xlsx	分公司客戶基資表-廠商測試版.xlsx	/Users/yangandy/FamilyTree/familytree-backend/user_upload/分公司客戶基資表-廠商測試版_20250730_153900_e87377f3.xlsx	16247	4e2e6de8e658c9cc98c8051df9151c45	2025-07-30 23:39:00.198591	t	2025-07-30 23:39:00.349194	merged	2025-07-30 23:39:00.201013	2025-07-30 23:39:00.349591	636216-20250730233734
\.


--
-- Data for Name: visual_analysis_graphs; Type: TABLE DATA; Schema: public; Owner: user
--

COPY public.visual_analysis_graphs (id, name, project_ids, updated_by, updated_at) FROM stdin;
2	test	968348-20250729211539	user	2025-07-30 01:57:06.162035
4	123	636216-20250730233734	user	2025-07-31 00:17:12.074707
5	321321	636216-20250730233734	user	2025-07-31 00:17:21.241882
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
13	5	636216-20250730233734	18	t	0	0	2025-07-31 00:30:11.45132	2025-07-31 00:30:11.45132
14	5	636216-20250730233734	24	t	0	0	2025-07-31 00:30:11.455016	2025-07-31 00:30:11.455016
15	5	636216-20250730233734	16	t	0	0	2025-07-31 00:30:11.455499	2025-07-31 00:30:11.455499
16	5	636216-20250730233734	20	t	0	0	2025-07-31 00:30:11.455749	2025-07-31 00:30:11.455749
17	5	636216-20250730233734	19	t	0	0	2025-07-31 00:30:11.455978	2025-07-31 00:30:11.455978
18	5	636216-20250730233734	17	t	0	0	2025-07-31 00:30:11.456198	2025-07-31 00:30:11.456198
19	5	636216-20250730233734	23	t	0	0	2025-07-31 00:30:11.456407	2025-07-31 00:30:11.456407
20	5	636216-20250730233734	13	t	0	0	2025-07-31 00:30:11.45656	2025-07-31 00:30:11.45656
21	5	636216-20250730233734	14	t	0	0	2025-07-31 00:30:11.456747	2025-07-31 00:30:11.456747
22	5	636216-20250730233734	21	t	0	0	2025-07-31 00:30:11.456988	2025-07-31 00:30:11.456988
23	5	636216-20250730233734	15	t	0	0	2025-07-31 00:30:11.457167	2025-07-31 00:30:11.457167
24	5	636216-20250730233734	22	t	0	0	2025-07-31 00:30:11.457342	2025-07-31 00:30:11.457342
\.


--
-- Name: field_mapping_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.field_mapping_id_seq', 81, true);


--
-- Name: mergedpersons_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.mergedpersons_id_seq', 1, false);


--
-- Name: person_profile_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.person_profile_id_seq', 24, true);


--
-- Name: personmergelog_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.personmergelog_id_seq', 1, false);


--
-- Name: photos_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.photos_id_seq', 1, true);


--
-- Name: relationship_layers_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.relationship_layers_id_seq', 11, true);


--
-- Name: search_keywords_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.search_keywords_id_seq', 140, true);


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

SELECT pg_catalog.setval('public.user_update_file_id_seq', 2, true);


--
-- Name: visual_analysis_graphs_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.visual_analysis_graphs_id_seq', 5, true);


--
-- Name: visual_analysis_nodes_id_seq; Type: SEQUENCE SET; Schema: public; Owner: user
--

SELECT pg_catalog.setval('public.visual_analysis_nodes_id_seq', 36, true);


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
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: yangandy
--

REVOKE USAGE ON SCHEMA public FROM PUBLIC;
GRANT ALL ON SCHEMA public TO "user";


--
-- PostgreSQL database dump complete
--

