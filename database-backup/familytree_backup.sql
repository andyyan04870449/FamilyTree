--
-- PostgreSQL database dump
--

-- Dumped from database version 14.18 (Homebrew)
-- Dumped by pg_dump version 14.18 (Homebrew)

-- Started on 2025-07-19 20:35:39 CST

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

DROP DATABASE IF EXISTS familytree;
--
-- TOC entry 3730 (class 1262 OID 16439)
-- Name: familytree; Type: DATABASE; Schema: -; Owner: -
--

CREATE DATABASE familytree WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE = 'C';


\connect familytree

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

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 212 (class 1259 OID 16523)
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
-- TOC entry 211 (class 1259 OID 16522)
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
-- TOC entry 3731 (class 0 OID 0)
-- Dependencies: 211
-- Name: analysis_results_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.analysis_results_id_seq OWNED BY public.analysis_results.id;


--
-- TOC entry 213 (class 1259 OID 16539)
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
-- TOC entry 3732 (class 0 OID 0)
-- Dependencies: 213
-- Name: TABLE analysis_sessions; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.analysis_sessions IS '儲存分析會話資訊';


--
-- TOC entry 3733 (class 0 OID 0)
-- Dependencies: 213
-- Name: COLUMN analysis_sessions.max_depth; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.analysis_sessions.max_depth IS '最大分析深度，預設為3層';


--
-- TOC entry 210 (class 1259 OID 16441)
-- Name: person_profile; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.person_profile (
    id integer NOT NULL,
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
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    created_by text,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_by text,
    extra_data jsonb
);


--
-- TOC entry 209 (class 1259 OID 16440)
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
-- TOC entry 3734 (class 0 OID 0)
-- Dependencies: 209
-- Name: person_profile_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.person_profile_id_seq OWNED BY public.person_profile.id;


--
-- TOC entry 215 (class 1259 OID 16554)
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
-- TOC entry 3735 (class 0 OID 0)
-- Dependencies: 215
-- Name: TABLE relationship_layers; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON TABLE public.relationship_layers IS '儲存遞迴分析的層級關係資料';


--
-- TOC entry 3736 (class 0 OID 0)
-- Dependencies: 215
-- Name: COLUMN relationship_layers.layer_depth; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.relationship_layers.layer_depth IS '關係層級深度，1為直接關係，2為間接關係，以此類推';


--
-- TOC entry 3737 (class 0 OID 0)
-- Dependencies: 215
-- Name: COLUMN relationship_layers.analysis_session_id; Type: COMMENT; Schema: public; Owner: -
--

COMMENT ON COLUMN public.relationship_layers.analysis_session_id IS '分析會話ID，用於區分不同的分析任務';


--
-- TOC entry 214 (class 1259 OID 16553)
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
-- TOC entry 3738 (class 0 OID 0)
-- Dependencies: 214
-- Name: relationship_layers_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.relationship_layers_id_seq OWNED BY public.relationship_layers.id;


--
-- TOC entry 3545 (class 2604 OID 16526)
-- Name: analysis_results id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.analysis_results ALTER COLUMN id SET DEFAULT nextval('public.analysis_results_id_seq'::regclass);


--
-- TOC entry 3542 (class 2604 OID 16444)
-- Name: person_profile id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.person_profile ALTER COLUMN id SET DEFAULT nextval('public.person_profile_id_seq'::regclass);


--
-- TOC entry 3555 (class 2604 OID 16557)
-- Name: relationship_layers id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers ALTER COLUMN id SET DEFAULT nextval('public.relationship_layers_id_seq'::regclass);


--
-- TOC entry 3721 (class 0 OID 16523)
-- Dependencies: 212
-- Data for Name: analysis_results; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.analysis_results (id, person_id, analysis_result, analysis_date, progress_percentage, status, current_step, status_message, created_at, updated_at) FROM stdin;
90	7	{}	2025-07-19 20:10:42.170436	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:10:42.170436	2025-07-19 20:10:42.172037
91	8	{}	2025-07-19 20:10:58.603514	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:10:58.603514	2025-07-19 20:10:58.60821
92	19	{}	2025-07-19 20:11:08.970574	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:11:08.970574	2025-07-19 20:11:08.973501
94	16	{}	2025-07-19 20:11:33.935166	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:11:33.935166	2025-07-19 20:11:33.94889
95	15	{}	2025-07-19 20:12:01.198323	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:12:01.198323	2025-07-19 20:12:01.201545
96	17	{}	2025-07-19 20:12:12.536235	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:12:12.536235	2025-07-19 20:12:12.537271
97	18	{}	2025-07-19 20:12:24.306712	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:12:24.306712	2025-07-19 20:12:24.308099
98	13	{}	2025-07-19 20:12:38.519631	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:12:38.519631	2025-07-19 20:12:38.520747
80	9	{}	2025-07-19 19:42:16.9788	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 19:42:16.9788	2025-07-19 19:42:16.987157
99	14	{}	2025-07-19 20:12:51.194636	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:12:51.194636	2025-07-19 20:12:51.195665
43	11	{}	2025-07-19 15:10:00.413769	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 15:10:00.413769	2025-07-19 15:10:00.415742
45	12	{}	2025-07-19 15:11:28.918882	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 15:11:28.918882	2025-07-19 15:11:28.922959
102	4	{}	2025-07-19 20:13:30.975175	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:13:30.975175	2025-07-19 20:13:30.975917
103	5	{}	2025-07-19 20:13:44.068318	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:13:44.068318	2025-07-19 20:13:44.069137
104	6	{}	2025-07-19 20:13:57.352752	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:13:57.352752	2025-07-19 20:13:57.353493
105	3	{}	2025-07-19 20:14:10.344967	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:14:10.344967	2025-07-19 20:14:10.346004
106	20	{}	2025-07-19 20:20:27.162379	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:20:27.162379	2025-07-19 20:20:27.164529
88	10	{}	2025-07-19 19:53:38.109661	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 19:53:38.109661	2025-07-19 19:53:38.112468
107	1	{}	2025-07-19 20:22:17.141175	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:22:17.141175	2025-07-19 20:22:17.146657
108	2	{}	2025-07-19 20:22:28.603543	5	開始遞迴分析...	開始遞迴分析...	開始遞迴分析...	2025-07-19 20:22:28.603543	2025-07-19 20:22:28.60485
\.


--
-- TOC entry 3722 (class 0 OID 16539)
-- Dependencies: 213
-- Data for Name: analysis_sessions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.analysis_sessions (id, root_person_id, max_depth, status, total_relationships, created_at, completed_at) FROM stdin;
cd32deae-edf5-406a-9a22-6084744e2a94	10	3	completed	0	2025-07-19 14:32:28.845903	2025-07-19 14:32:29.887142
e67f02da-d7ee-4fd0-81c7-208ac4831f02	10	3	completed	0	2025-07-19 14:36:15.622245	2025-07-19 14:36:16.671624
402b2617-bc24-4b13-a5e5-a6fd9019bf38	10	3	completed	0	2025-07-19 14:36:59.118619	2025-07-19 14:37:00.13128
536f242d-f7bb-4130-af8a-e19012bf9e7b	10	3	completed	1	2025-07-19 14:41:06.108187	2025-07-19 14:41:06.152715
1ab38b75-b0f1-4da9-8174-f76e8d62a891	10	3	completed	1	2025-07-19 14:45:05.033378	2025-07-19 14:45:05.084558
cac0eb91-8745-409e-86cd-b17116bf0ab6	10	3	completed	1	2025-07-19 14:45:26.302589	2025-07-19 14:45:26.328839
8d8edca3-36e2-4233-b759-820a1f616e0a	10	3	completed	1	2025-07-19 14:47:32.064868	2025-07-19 14:47:32.125972
8b525ea3-5e33-4f11-bb1c-fc53e38b0fef	10	3	completed	3	2025-07-19 15:04:56.05032	2025-07-19 15:04:56.090235
185bce1c-98f2-46bf-a47e-c06e6407e59d	10	3	completed	3	2025-07-19 15:06:12.873276	2025-07-19 15:06:12.964631
7e4510a5-6c8f-48aa-83a4-31f47e5a3313	11	2	completed	0	2025-07-19 15:10:00.408944	2025-07-19 15:10:00.420022
b7e8696d-fb91-4590-8133-b23dc7dd5228	10	3	completed	3	2025-07-19 15:10:23.703836	2025-07-19 15:10:23.724371
22c3a9ac-e2cf-4108-8fbb-78b21bf3798c	12	2	completed	0	2025-07-19 15:11:28.916607	2025-07-19 15:11:28.941055
36c58a88-9e6c-461c-a1bd-7312679ebae1	13	2	completed	0	2025-07-19 15:11:54.550685	2025-07-19 15:11:54.565696
b07ff5e3-a6de-4898-9e26-9a4076af876c	13	2	completed	0	2025-07-19 15:12:40.243693	2025-07-19 15:12:40.279041
0e19f48b-9cbe-4650-807e-8c1cf5beada8	13	2	completed	0	2025-07-19 15:12:55.097172	2025-07-19 15:12:55.102741
9bc14f21-ec51-4126-868a-f29190b8c35e	10	3	completed	3	2025-07-19 15:17:08.22332	2025-07-19 15:17:08.251422
ea95f285-3288-420e-9e76-4ab386df4680	13	2	completed	0	2025-07-19 15:18:29.329956	2025-07-19 15:18:29.365085
e0e10b69-c3ec-4139-84eb-f0a57257cbcb	13	2	completed	0	2025-07-19 15:19:20.078566	2025-07-19 15:19:20.112124
b636b7cd-deb9-49d5-b46a-7aeddf765f8d	13	2	completed	0	2025-07-19 15:20:47.70125	2025-07-19 15:20:47.728237
b344c787-e330-47df-abcc-b85df7924e0e	10	2	completed	3	2025-07-19 15:21:23.62525	2025-07-19 15:21:23.636784
208ee596-fe1e-4133-9dcd-e20c1ab60156	4	3	completed	0	2025-07-19 15:21:55.867435	2025-07-19 15:21:55.872961
0bf26b8a-506c-4869-ba5b-07fd8b513644	10	3	completed	3	2025-07-19 15:22:34.358476	2025-07-19 15:22:34.435585
4aee27ac-3182-43e5-b1a7-22083f9beb7d	10	3	completed	3	2025-07-19 15:24:42.80341	2025-07-19 15:24:42.870977
91804b53-00b4-4487-bc8e-d51909e5bbda	10	3	completed	3	2025-07-19 15:25:42.568906	2025-07-19 15:25:42.604171
7e3835d1-d2c1-47bd-95b0-2d550d8d9819	10	2	completed	3	2025-07-19 15:26:59.742728	2025-07-19 15:26:59.755646
0a3fcb1d-50e5-47d3-a53a-19f220fc1a0c	10	3	completed	3	2025-07-19 15:27:22.551518	2025-07-19 15:27:22.580999
c74a898a-e03f-4bbe-8778-33d4cc70173c	10	3	completed	3	2025-07-19 15:29:14.504262	2025-07-19 15:29:14.521476
27fe3bdd-0a60-4189-937c-dd730ff57f3e	4	4	completed	0	2025-07-19 15:30:19.303431	2025-07-19 15:30:19.309685
2ba6a225-0e8f-452a-8812-11602d8b902d	2	3	completed	0	2025-07-19 15:30:34.654613	2025-07-19 15:30:34.658803
f53337b6-ca5a-41e1-be06-dd8c519bcf57	1	2	completed	0	2025-07-19 15:30:50.18887	2025-07-19 15:30:50.204987
b66fa96a-7e8c-4f8e-8b61-2ee3b65b65df	1	2	completed	0	2025-07-19 15:31:30.508733	2025-07-19 15:31:30.512268
6c41aa6f-551b-406d-b273-b4555ba18a16	2	3	completed	0	2025-07-19 15:32:04.886219	2025-07-19 15:32:04.891992
4b1279ba-6c63-48f8-b0a4-818f89799c0c	3	2	completed	0	2025-07-19 15:32:27.607448	2025-07-19 15:32:27.611535
b16dc77d-db19-4867-af9e-6fd83f2fa9b3	10	2	completed	3	2025-07-19 15:32:55.94836	2025-07-19 15:32:55.955706
ec0fda89-9ea4-4fb5-99cb-844b059f7aab	10	3	completed	3	2025-07-19 15:34:25.479985	2025-07-19 15:34:25.496683
a37e3612-2538-4849-b3ee-bc366c703caa	10	3	completed	3	2025-07-19 15:36:02.692809	2025-07-19 15:36:02.702762
6bd0d487-b9a5-4327-bcfa-e4a40dd418fd	10	3	completed	3	2025-07-19 15:36:20.844564	2025-07-19 15:36:20.862144
f95091b1-20ff-4a49-a396-c6c8231cc88b	10	3	completed	3	2025-07-19 19:22:46.312952	2025-07-19 19:22:46.370859
06b5e8cd-6778-4491-acc5-38d7a95c5f3a	10	3	completed	3	2025-07-19 19:24:59.225679	2025-07-19 19:24:59.25336
5a194c0e-c448-4fa0-94f2-1c745bd5e9b4	10	3	completed	3	2025-07-19 19:25:34.157718	2025-07-19 19:25:34.185353
fda6eeda-eca1-435b-819b-e2d7f388bad6	10	3	completed	3	2025-07-19 19:28:13.014247	2025-07-19 19:28:13.040584
dd728074-f442-4fd8-86fa-08f77a7a4cb6	10	3	completed	3	2025-07-19 19:28:46.676382	2025-07-19 19:28:46.701637
f73741f9-b8b6-47c8-857d-2c02969a7abe	10	3	completed	3	2025-07-19 19:29:06.214033	2025-07-19 19:29:06.228899
4b72fd2b-7acb-4acf-a883-9ddacfbf3010	10	3	completed	3	2025-07-19 19:29:58.061492	2025-07-19 19:29:58.079591
3f59682b-ff01-4f6a-8981-013f56ff9acb	9	3	completed	3	2025-07-19 19:30:16.095302	2025-07-19 19:30:16.10925
fa28f827-ef22-4703-9c35-0f29b16dad63	10	3	completed	3	2025-07-19 19:30:55.862007	2025-07-19 19:30:55.905937
b3ff6304-b871-4d33-b642-9ba3aea55576	9	3	completed	3	2025-07-19 19:42:16.971868	2025-07-19 19:42:17.020231
cc67da56-d045-4fcc-9bba-7e954a5d7f06	10	3	completed	3	2025-07-19 19:45:07.337419	2025-07-19 19:45:07.363283
3b278df0-5151-45f2-8c1b-047b6e2e27f6	10	3	completed	3	2025-07-19 19:45:12.371428	2025-07-19 19:45:12.384194
ae2d99a0-e0a0-414c-86f5-5797d1df168c	10	3	completed	3	2025-07-19 19:47:32.855016	2025-07-19 19:47:32.870768
33213b8b-f43b-40fb-bb5c-56b66950c27c	3	3	completed	0	2025-07-19 19:49:40.134834	2025-07-19 19:49:40.144163
6535fe21-94fa-4771-a9ef-21f375e78da0	10	3	completed	3	2025-07-19 19:49:56.315266	2025-07-19 19:49:56.339623
8729361d-8c3f-4ca9-a6d9-a0ea58ce9970	3	3	completed	0	2025-07-19 19:51:12.311678	2025-07-19 19:51:12.33384
0f008689-b97a-4930-a2a2-230b141978bf	10	3	completed	3	2025-07-19 19:51:25.116113	2025-07-19 19:51:25.131825
755ef462-555b-4582-821f-5d579b9754b1	10	3	completed	3	2025-07-19 19:53:38.106667	2025-07-19 19:53:38.134779
fb8b0fe2-ba53-4146-853d-196ebdc34350	5	3	completed	0	2025-07-19 19:54:11.147546	2025-07-19 19:54:11.15446
f3862ec7-a977-457b-9e74-ad92aeea83fa	7	4	completed	0	2025-07-19 20:10:42.16459	2025-07-19 20:10:42.179074
6cd19ed5-6c0b-4b4e-8073-53b038d7cbc6	8	4	completed	0	2025-07-19 20:10:58.6003	2025-07-19 20:10:58.612005
ba05e7b7-cf12-49f0-a982-04e8f3865dcd	19	4	completed	0	2025-07-19 20:11:08.965214	2025-07-19 20:11:08.97618
15f8edf9-6b59-4960-9d88-e80acb1fe3b5	20	4	completed	4	2025-07-19 20:11:22.832251	2025-07-19 20:11:22.848259
ae05077d-0189-45e5-9d05-ab7b231f95d4	16	4	completed	0	2025-07-19 20:11:33.932206	2025-07-19 20:11:33.951586
6465444a-6e96-48b5-9ef0-cd4101fb6121	15	4	completed	0	2025-07-19 20:12:01.195533	2025-07-19 20:12:01.222123
187b512c-7309-4b68-bf7d-cfa73bddc484	17	4	completed	0	2025-07-19 20:12:12.534887	2025-07-19 20:12:12.540995
7c019374-bb97-4b14-ab82-aaa1c538e89c	18	4	completed	0	2025-07-19 20:12:24.305533	2025-07-19 20:12:24.310716
587e3eae-6245-4672-b3e7-1fd48a2b8abc	13	4	completed	0	2025-07-19 20:12:38.518266	2025-07-19 20:12:38.524746
3b32e395-1d7e-427d-9b7a-2113e904961c	14	4	completed	0	2025-07-19 20:12:51.177187	2025-07-19 20:12:51.198667
28dbeb3a-05c7-4edc-9d3e-3da60796e080	1	4	completed	0	2025-07-19 20:13:03.281608	2025-07-19 20:13:03.285008
c92fc475-a008-4bf2-98a5-e2420ff60b60	2	4	completed	0	2025-07-19 20:13:17.242525	2025-07-19 20:13:17.246174
ee880e8e-7c30-4b22-9d35-73c06d302274	4	4	completed	0	2025-07-19 20:13:30.974091	2025-07-19 20:13:30.980079
58850613-9117-4fec-82ea-fb4323a8eabb	5	4	completed	0	2025-07-19 20:13:44.067323	2025-07-19 20:13:44.070919
fe7b09a8-600a-480e-98d7-8bd297e017b8	6	4	completed	0	2025-07-19 20:13:57.351326	2025-07-19 20:13:57.357124
bdd1b4f7-60a4-4e2a-8a5d-c238ea334b2d	3	4	completed	0	2025-07-19 20:14:10.343021	2025-07-19 20:14:10.347977
e91192c3-152d-4d20-9111-621c8cb4b150	20	3	completed	4	2025-07-19 20:20:27.154346	2025-07-19 20:20:27.200712
75846c8c-7baa-4409-971f-604931743e8f	1	10	completed	0	2025-07-19 20:22:17.138317	2025-07-19 20:22:17.169737
7e8db332-8a4f-46df-9830-fd1bd821ea80	2	10	completed	0	2025-07-19 20:22:28.60145	2025-07-19 20:22:28.607396
\.


--
-- TOC entry 3719 (class 0 OID 16441)
-- Dependencies: 210
-- Data for Name: person_profile; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.person_profile (id, photo_index, name, discovery_source, gender, birthday, birthplace, nationality, ethnicity, ancestral_origin, political_party, id_number, passport_number, phone, mobile, email, current_employer, address, mailing_address, family_relationships, experience, education, online_accounts, publications, activities, friends, frequent_locations, travel_history, remarks, created_at, created_by, updated_at, updated_by, extra_data) FROM stdin;
1	\N	范立	\N	男	1988-06-07	\N	中國	\N	\N	\N	320204197608071330	EJ9804516	8113457839955	13357913171	\N	\N	\N	\N	父：范統\\n母：吳春華	\N	\N	\N	\N	\N	張三，中國總商會東京華僑青年會副會長，中共東京使館中秋節慶祝活動工作人員\\n李贄，早稻田大學政治系博士一年級，大學論社學長學弟\\n佐藤真美子，住友不動產株式會社職員，女友	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "84313835435671@qq.com", "residence": "東京都品川", "current_employer": "東京大學經濟系研究生一年級"}
2	\N	趙威	\N	男	1975-04-15	\N	中國	\N	\N	\N	460004197502151560	\N	8613884937455	13884937455	\N	\N	\N	\N	妻：項依潔\\n女：趙小惠\\n子：趙小亮	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "vigor666@126.com", "residence": "山東省煙臺市文山區大江路", "current_employer": "煙台大學中文系"}
3	\N	項依潔	\N	女	1975-10-29	\N	中國	\N	\N	\N	370629197510294987	\N	8613573590064	13573590064	\N	\N	\N	\N	夫：趙威\\n女：趙小惠\\n子：趙小亮	\N	\N	\N	\N	\N	張濤，北京師範大學歷史學院（博士生導師）	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "ruru215@163.com", "residence": "山東省煙臺市文山區大江路", "current_employer": "煙台大學文學與新聞傳播學系副教授"}
4	\N	李光	\N	男	1990-05-17	\N	中國	\N	\N	\N	371082199005173613	\N	861335687453	+861335687453	\N	\N	\N	\N	父：李立朝（1962.12.2）\\n母：江彩芹（1961.11.11）	\N	\N	\N	\N	\N	謝大方，麗星郵輪維修部主任，謝大方介紹李光入職	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "erty@sina.com", "residence": "台北市中山區", "current_employer": "麗星郵輪總務"}
5	\N	沈家新	\N	女	1978-09-18	\N	中國	\N	\N	\N	32020419780918162X	\N	\N	+8613357912344\\n0917851414	\N	\N	\N	\N	夫：謝大方\\n女：謝圓圓	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"residence": "臺北市萬華區", "current_employer": "永慶房屋仲介"}
6	\N	唐伯虎	\N	男	1967-08-20	\N	中國	\N	\N	\N	\N	\N	\N	17188407888	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	姜大宇，新聞最前線主持人，於X上恭賀新年快樂。	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "bhtang@gmail.com", "current_employer": "50藍西門店店長"}
7	\N	楊習五	\N	男	1983-04-23	\N	中國	\N	\N	\N	入台許可證號113330495794	\N	\N	0933-549-070\\n0988-206-038	\N	\N	\N	\N	妻：姜恩為\\n岳母：諶月英\\n小叔：姜大宇	\N	\N	\N	\N	\N	趙馥樂，具長時間北京工作經歷（實習生、總監），FB台籍好友	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "hi@ailleurslab.com", "residence": "新北市新店區文化路", "current_employer": "momo藝術策畫經理"}
8	\N	李青春	\N	女	1983-07-10	\N	中國	\N	\N	\N	\N	\N	\N	0935-787-122	\N	\N	\N	\N	夫：羅大佑\\n女：羅亞瑟\\n表妹：姜恩為	\N	\N	\N	\N	\N	常雨，羅東鎮新住民關懷協會理事長，共同場域\\n女神美甲美睫(1120701-FB貼文)	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"place_of_birth": "四川省", "current_employer": "旭東廣告工程"}
9	\N	邱還真	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	女：黃心田	\N	\N	\N	\N	\N	馬晶，鶯之韻旗袍協會理事長，共同場域鶯之韻旗袍協會第一次會員大會餐敘聯誼 (1120423-FB貼文)	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"residence": "宜蘭", "current_employer": "羅東鎮新住民關懷協會理事長"}
10	\N	黃心田	\N	女	\N	\N	中華民國	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	母：邱還真	\N	\N	\N	\N	\N	羅亞瑟，淡江大學同學	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"residence": "宜蘭", "current_employer": "羅東鎮新住民關懷協會常務監事"}
11	\N	羅亞瑟	\N	女	\N	\N	\N	\N	\N	\N	入台許可證號114665295794	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"residence": "台北市中正區", "current_employer": "大創有限公司副總經理"}
12	\N	李光	\N	女	1993-12-05	\N	中華民國	\N	\N	\N	F113456987	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"residence": "新北市蘆洲", "place_of_birth": "新北市蘆洲", "current_employer": "大創有限公司人事主管"}
13	\N	黃建宏	\N	男	1966-12-26	\N	日本	\N	\N	\N	D670336268	ZW40850954	0930-596601	0964-876998	\N	\N	\N	\N	妹妹，李惠如；妹妹，秦美玲	\N	\N	\N	\N	\N	周雅，律理法律資訊有限公司；程佩珊，品誠資訊有限公司	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "natan@hotmail.com", "residence": "913 新營縣延平街14號4樓", "current_employer": "台灣力電"}
14	\N	李俊謙	\N	男	1980-09-02	\N	中國	\N	\N	\N	G042901935	yW89536902	01-7139898	0918-097887	\N	\N	\N	\N	配偶，姜詩涵	\N	\N	\N	\N	\N	陳濤，風微廣場股份有限公司；王家豪，大八電視有限公司	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "juan94@gmail.com", "residence": "451 褒忠縣象山巷2段47號之6", "current_employer": "秀威影城股份有限公司"}
15	\N	廖信宏	\N	女	1977-01-10	\N	日本	\N	\N	\N	H456522036	hs49951542	077 53244913	01-93882090	\N	\N	\N	\N	母親，項依潔；姐姐，周雅娟	\N	\N	\N	\N	\N	符志宏，光新三越百貨，Reverse-engineered optimizing hub；莫慧君，台北登來喜大飯店股份有限公司，Organic context-sensitive migration	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "tianchao@peng.net", "residence": "87411 古坑縣新生街41號5樓", "current_employer": "樂可旅遊集團資訊有限公司"}
16	\N	溫馨	\N	女	1984-06-03	\N	日本	\N	\N	\N	D963044836	zq85456676	(04) 47350476	0928-319952	\N	\N	\N	\N	女兒，史佳穗；妹妹，史詩涵	\N	\N	\N	\N	\N	趙雅娟，衣優庫（Nuiqlo）資訊有限公司，Right-sized heuristic moratorium；莫家豪，台灣業糖，Face-to-face bifurcated system engine；黎嘉玲，丹即企業資訊有限公司，Inverse dynamic protocol	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "qiangang@zou.tw", "residence": "95832 竹北縣太平街9號0樓", "current_employer": "隆豐大飯店（北台君悅）"}
17	\N	米淑華	\N	男	1983-07-02	\N	韓國	\N	\N	\N	K791452118	EH37931269	09-6307464	02 5059960	\N	\N	\N	\N	女兒，冉俊宏；哥哥，王家豪	\N	\N	\N	\N	\N	李惠如，大八電視有限公司，Multi-tiered 3rdgeneration methodology	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "fxiang@lu.net", "residence": "16162 屏東公園街7號之2", "current_employer": "華福大飯店資訊有限公司"}
18	\N	陳佩珊	\N	男	2004-04-26	\N	中國	\N	\N	\N	Z289659994	ZN97320331	02-92705102	07 4675481	\N	\N	\N	\N	妹妹，王怡如；弟弟，牛冠宇	\N	\N	\N	\N	\N	黃淑貞，中台信託商業銀行資訊有限公司，Function-based next generation moratorium	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "tqiao@yahoo.com", "residence": "92426 關山縣龍山寺街1號之7", "current_employer": "台灣來自水股份有限公司"}
19	\N	周馨怡	\N	女	1968-05-11	\N	日本	\N	\N	\N	A412704938	DK20448515	01 2971112	05-29020357	\N	\N	\N	\N	兒子，李輝；父親，唐佩君；弟弟，賴郁飯	\N	\N	\N	\N	\N	唐淑芬，饗國大飯店，Intuitive bandwidth-monitored moratorium；溫怡安，創群光電（奇原美電子），Managed systematic functionalities	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "yong32@long.com", "residence": "629 員林市公園路57號7樓", "current_employer": "心安食品服務（斯摩漢堡）股份有限公司"}
20	\N	崔雅涵	\N	女	1995-08-10	\N	台灣	\N	\N	\N	I282498392	kD50142576	(02) 43244123	0913285072	\N	\N	\N	\N	母親，周馨怡	\N	\N	\N	\N	\N	黃雅芳，禮爭資訊有限公司，Phased dynamic extranet；黃心田，興復航空運輸股份有限公司，Polarized systematic initiative	\N	\N	\N	2025-07-19 04:50:29.901924	\N	2025-07-19 04:50:29.901924	\N	{"email": "tangxia@hotmail.com", "residence": "273 台東縣大仁街769號5樓", "current_employer": "律理法律有限公司"}
\.


--
-- TOC entry 3724 (class 0 OID 16554)
-- Dependencies: 215
-- Data for Name: relationship_layers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.relationship_layers (id, source_person_id, target_person_id, relation_type, source_field, layer_depth, analysis_session_id, created_at, updated_at) FROM stdin;
1	10	11	朋友	friends	1	536f242d-f7bb-4130-af8a-e19012bf9e7b	2025-07-19 14:41:06.142752	2025-07-19 14:41:06.142752
2	10	11	朋友	friends	1	1ab38b75-b0f1-4da9-8174-f76e8d62a891	2025-07-19 14:45:05.078211	2025-07-19 14:45:05.078211
3	10	11	朋友	friends	1	cac0eb91-8745-409e-86cd-b17116bf0ab6	2025-07-19 14:45:26.312978	2025-07-19 14:45:26.312978
4	10	11	淡江大學同學	friends	1	8d8edca3-36e2-4233-b759-820a1f616e0a	2025-07-19 14:47:32.117962	2025-07-19 14:47:32.117962
5	10	9	母	family_relationships	1	8b525ea3-5e33-4f11-bb1c-fc53e38b0fef	2025-07-19 15:04:56.081121	2025-07-19 15:04:56.081121
6	10	11	淡江大學同學	friends	1	8b525ea3-5e33-4f11-bb1c-fc53e38b0fef	2025-07-19 15:04:56.083111	2025-07-19 15:04:56.083111
7	9	10	女	family_relationships	2	8b525ea3-5e33-4f11-bb1c-fc53e38b0fef	2025-07-19 15:04:56.084709	2025-07-19 15:04:56.084709
8	10	9	母	family_relationships	1	185bce1c-98f2-46bf-a47e-c06e6407e59d	2025-07-19 15:06:12.95055	2025-07-19 15:06:12.95055
9	10	11	淡江大學同學	friends	1	185bce1c-98f2-46bf-a47e-c06e6407e59d	2025-07-19 15:06:12.9543	2025-07-19 15:06:12.9543
10	9	10	女	family_relationships	2	185bce1c-98f2-46bf-a47e-c06e6407e59d	2025-07-19 15:06:12.958269	2025-07-19 15:06:12.958269
11	10	9	母	family_relationships	1	b7e8696d-fb91-4590-8133-b23dc7dd5228	2025-07-19 15:10:23.718744	2025-07-19 15:10:23.718744
12	10	11	淡江大學同學	friends	1	b7e8696d-fb91-4590-8133-b23dc7dd5228	2025-07-19 15:10:23.720398	2025-07-19 15:10:23.720398
13	9	10	女	family_relationships	2	b7e8696d-fb91-4590-8133-b23dc7dd5228	2025-07-19 15:10:23.722196	2025-07-19 15:10:23.722196
14	10	9	母	family_relationships	1	9bc14f21-ec51-4126-868a-f29190b8c35e	2025-07-19 15:17:08.239766	2025-07-19 15:17:08.239766
15	10	11	淡江大學同學	friends	1	9bc14f21-ec51-4126-868a-f29190b8c35e	2025-07-19 15:17:08.243747	2025-07-19 15:17:08.243747
16	9	10	女	family_relationships	2	9bc14f21-ec51-4126-868a-f29190b8c35e	2025-07-19 15:17:08.246373	2025-07-19 15:17:08.246373
17	10	9	母	family_relationships	1	b344c787-e330-47df-abcc-b85df7924e0e	2025-07-19 15:21:23.630821	2025-07-19 15:21:23.630821
18	10	11	淡江大學同學	friends	1	b344c787-e330-47df-abcc-b85df7924e0e	2025-07-19 15:21:23.632191	2025-07-19 15:21:23.632191
19	9	10	女	family_relationships	2	b344c787-e330-47df-abcc-b85df7924e0e	2025-07-19 15:21:23.633514	2025-07-19 15:21:23.633514
20	10	9	母	family_relationships	1	0bf26b8a-506c-4869-ba5b-07fd8b513644	2025-07-19 15:22:34.36611	2025-07-19 15:22:34.36611
21	10	11	淡江大學同學	friends	1	0bf26b8a-506c-4869-ba5b-07fd8b513644	2025-07-19 15:22:34.368659	2025-07-19 15:22:34.368659
22	9	10	女	family_relationships	2	0bf26b8a-506c-4869-ba5b-07fd8b513644	2025-07-19 15:22:34.421102	2025-07-19 15:22:34.421102
23	10	9	母	family_relationships	1	4aee27ac-3182-43e5-b1a7-22083f9beb7d	2025-07-19 15:24:42.862382	2025-07-19 15:24:42.862382
24	10	11	淡江大學同學	friends	1	4aee27ac-3182-43e5-b1a7-22083f9beb7d	2025-07-19 15:24:42.864218	2025-07-19 15:24:42.864218
25	9	10	女	family_relationships	2	4aee27ac-3182-43e5-b1a7-22083f9beb7d	2025-07-19 15:24:42.86586	2025-07-19 15:24:42.86586
26	10	9	母	family_relationships	1	91804b53-00b4-4487-bc8e-d51909e5bbda	2025-07-19 15:25:42.580026	2025-07-19 15:25:42.580026
27	10	11	淡江大學同學	friends	1	91804b53-00b4-4487-bc8e-d51909e5bbda	2025-07-19 15:25:42.58302	2025-07-19 15:25:42.58302
28	9	10	女	family_relationships	2	91804b53-00b4-4487-bc8e-d51909e5bbda	2025-07-19 15:25:42.585593	2025-07-19 15:25:42.585593
29	10	9	母	family_relationships	1	7e3835d1-d2c1-47bd-95b0-2d550d8d9819	2025-07-19 15:26:59.748871	2025-07-19 15:26:59.748871
30	10	11	淡江大學同學	friends	1	7e3835d1-d2c1-47bd-95b0-2d550d8d9819	2025-07-19 15:26:59.751116	2025-07-19 15:26:59.751116
31	9	10	女	family_relationships	2	7e3835d1-d2c1-47bd-95b0-2d550d8d9819	2025-07-19 15:26:59.752979	2025-07-19 15:26:59.752979
32	10	9	母	family_relationships	1	0a3fcb1d-50e5-47d3-a53a-19f220fc1a0c	2025-07-19 15:27:22.562751	2025-07-19 15:27:22.562751
33	10	11	淡江大學同學	friends	1	0a3fcb1d-50e5-47d3-a53a-19f220fc1a0c	2025-07-19 15:27:22.564656	2025-07-19 15:27:22.564656
34	9	10	女	family_relationships	2	0a3fcb1d-50e5-47d3-a53a-19f220fc1a0c	2025-07-19 15:27:22.578356	2025-07-19 15:27:22.578356
35	10	9	母	family_relationships	1	c74a898a-e03f-4bbe-8778-33d4cc70173c	2025-07-19 15:29:14.51188	2025-07-19 15:29:14.51188
36	10	11	淡江大學同學	friends	1	c74a898a-e03f-4bbe-8778-33d4cc70173c	2025-07-19 15:29:14.514939	2025-07-19 15:29:14.514939
37	9	10	女	family_relationships	2	c74a898a-e03f-4bbe-8778-33d4cc70173c	2025-07-19 15:29:14.517226	2025-07-19 15:29:14.517226
38	10	9	母	family_relationships	1	b16dc77d-db19-4867-af9e-6fd83f2fa9b3	2025-07-19 15:32:55.951542	2025-07-19 15:32:55.951542
39	10	11	淡江大學同學	friends	1	b16dc77d-db19-4867-af9e-6fd83f2fa9b3	2025-07-19 15:32:55.952454	2025-07-19 15:32:55.952454
40	9	10	女	family_relationships	2	b16dc77d-db19-4867-af9e-6fd83f2fa9b3	2025-07-19 15:32:55.953715	2025-07-19 15:32:55.953715
41	10	9	母	family_relationships	1	ec0fda89-9ea4-4fb5-99cb-844b059f7aab	2025-07-19 15:34:25.487287	2025-07-19 15:34:25.487287
42	10	11	淡江大學同學	friends	1	ec0fda89-9ea4-4fb5-99cb-844b059f7aab	2025-07-19 15:34:25.49043	2025-07-19 15:34:25.49043
43	9	10	女	family_relationships	2	ec0fda89-9ea4-4fb5-99cb-844b059f7aab	2025-07-19 15:34:25.493321	2025-07-19 15:34:25.493321
44	10	9	母	family_relationships	1	a37e3612-2538-4849-b3ee-bc366c703caa	2025-07-19 15:36:02.697753	2025-07-19 15:36:02.697753
45	10	11	淡江大學同學	friends	1	a37e3612-2538-4849-b3ee-bc366c703caa	2025-07-19 15:36:02.698767	2025-07-19 15:36:02.698767
46	9	10	女	family_relationships	2	a37e3612-2538-4849-b3ee-bc366c703caa	2025-07-19 15:36:02.700269	2025-07-19 15:36:02.700269
47	10	9	母	family_relationships	1	6bd0d487-b9a5-4327-bcfa-e4a40dd418fd	2025-07-19 15:36:20.853735	2025-07-19 15:36:20.853735
48	10	11	淡江大學同學	friends	1	6bd0d487-b9a5-4327-bcfa-e4a40dd418fd	2025-07-19 15:36:20.855702	2025-07-19 15:36:20.855702
49	9	10	女	family_relationships	2	6bd0d487-b9a5-4327-bcfa-e4a40dd418fd	2025-07-19 15:36:20.857185	2025-07-19 15:36:20.857185
50	10	9	母	family_relationships	1	f95091b1-20ff-4a49-a396-c6c8231cc88b	2025-07-19 19:22:46.352	2025-07-19 19:22:46.352
51	10	11	淡江大學同學	friends	1	f95091b1-20ff-4a49-a396-c6c8231cc88b	2025-07-19 19:22:46.353517	2025-07-19 19:22:46.353517
52	9	10	女	family_relationships	2	f95091b1-20ff-4a49-a396-c6c8231cc88b	2025-07-19 19:22:46.354816	2025-07-19 19:22:46.354816
53	10	9	母	family_relationships	1	06b5e8cd-6778-4491-acc5-38d7a95c5f3a	2025-07-19 19:24:59.234482	2025-07-19 19:24:59.234482
54	10	11	淡江大學同學	friends	1	06b5e8cd-6778-4491-acc5-38d7a95c5f3a	2025-07-19 19:24:59.23676	2025-07-19 19:24:59.23676
55	9	10	女	family_relationships	2	06b5e8cd-6778-4491-acc5-38d7a95c5f3a	2025-07-19 19:24:59.238006	2025-07-19 19:24:59.238006
56	10	9	母	family_relationships	1	5a194c0e-c448-4fa0-94f2-1c745bd5e9b4	2025-07-19 19:25:34.16463	2025-07-19 19:25:34.16463
57	10	11	淡江大學同學	friends	1	5a194c0e-c448-4fa0-94f2-1c745bd5e9b4	2025-07-19 19:25:34.178008	2025-07-19 19:25:34.178008
58	9	10	女	family_relationships	2	5a194c0e-c448-4fa0-94f2-1c745bd5e9b4	2025-07-19 19:25:34.18039	2025-07-19 19:25:34.18039
59	10	9	母	family_relationships	1	fda6eeda-eca1-435b-819b-e2d7f388bad6	2025-07-19 19:28:13.032531	2025-07-19 19:28:13.032531
60	10	11	淡江大學同學	friends	1	fda6eeda-eca1-435b-819b-e2d7f388bad6	2025-07-19 19:28:13.033866	2025-07-19 19:28:13.033866
61	9	10	女	family_relationships	2	fda6eeda-eca1-435b-819b-e2d7f388bad6	2025-07-19 19:28:13.038651	2025-07-19 19:28:13.038651
62	10	9	母	family_relationships	1	dd728074-f442-4fd8-86fa-08f77a7a4cb6	2025-07-19 19:28:46.687904	2025-07-19 19:28:46.687904
63	10	11	淡江大學同學	friends	1	dd728074-f442-4fd8-86fa-08f77a7a4cb6	2025-07-19 19:28:46.692841	2025-07-19 19:28:46.692841
64	9	10	女	family_relationships	2	dd728074-f442-4fd8-86fa-08f77a7a4cb6	2025-07-19 19:28:46.699172	2025-07-19 19:28:46.699172
65	10	9	母	family_relationships	1	f73741f9-b8b6-47c8-857d-2c02969a7abe	2025-07-19 19:29:06.220216	2025-07-19 19:29:06.220216
66	10	11	淡江大學同學	friends	1	f73741f9-b8b6-47c8-857d-2c02969a7abe	2025-07-19 19:29:06.222797	2025-07-19 19:29:06.222797
67	9	10	女	family_relationships	2	f73741f9-b8b6-47c8-857d-2c02969a7abe	2025-07-19 19:29:06.225513	2025-07-19 19:29:06.225513
68	10	9	母	family_relationships	1	4b72fd2b-7acb-4acf-a883-9ddacfbf3010	2025-07-19 19:29:58.068666	2025-07-19 19:29:58.068666
69	10	11	淡江大學同學	friends	1	4b72fd2b-7acb-4acf-a883-9ddacfbf3010	2025-07-19 19:29:58.071888	2025-07-19 19:29:58.071888
70	9	10	女	family_relationships	2	4b72fd2b-7acb-4acf-a883-9ddacfbf3010	2025-07-19 19:29:58.076207	2025-07-19 19:29:58.076207
71	9	10	女	family_relationships	1	3f59682b-ff01-4f6a-8981-013f56ff9acb	2025-07-19 19:30:16.099713	2025-07-19 19:30:16.099713
72	10	9	母	family_relationships	2	3f59682b-ff01-4f6a-8981-013f56ff9acb	2025-07-19 19:30:16.103833	2025-07-19 19:30:16.103833
73	10	11	淡江大學同學	friends	2	3f59682b-ff01-4f6a-8981-013f56ff9acb	2025-07-19 19:30:16.104998	2025-07-19 19:30:16.104998
74	10	9	母	family_relationships	1	fa28f827-ef22-4703-9c35-0f29b16dad63	2025-07-19 19:30:55.866303	2025-07-19 19:30:55.866303
75	10	11	淡江大學同學	friends	1	fa28f827-ef22-4703-9c35-0f29b16dad63	2025-07-19 19:30:55.867495	2025-07-19 19:30:55.867495
76	9	10	女	family_relationships	2	fa28f827-ef22-4703-9c35-0f29b16dad63	2025-07-19 19:30:55.870744	2025-07-19 19:30:55.870744
77	9	10	女	family_relationships	1	b3ff6304-b871-4d33-b642-9ba3aea55576	2025-07-19 19:42:17.010873	2025-07-19 19:42:17.010873
78	10	9	母	family_relationships	2	b3ff6304-b871-4d33-b642-9ba3aea55576	2025-07-19 19:42:17.013758	2025-07-19 19:42:17.013758
79	10	11	淡江大學同學	friends	2	b3ff6304-b871-4d33-b642-9ba3aea55576	2025-07-19 19:42:17.014534	2025-07-19 19:42:17.014534
80	10	9	母	family_relationships	1	cc67da56-d045-4fcc-9bba-7e954a5d7f06	2025-07-19 19:45:07.356956	2025-07-19 19:45:07.356956
81	10	11	淡江大學同學	friends	1	cc67da56-d045-4fcc-9bba-7e954a5d7f06	2025-07-19 19:45:07.358745	2025-07-19 19:45:07.358745
82	9	10	女	family_relationships	2	cc67da56-d045-4fcc-9bba-7e954a5d7f06	2025-07-19 19:45:07.360579	2025-07-19 19:45:07.360579
83	10	9	母	family_relationships	1	3b278df0-5151-45f2-8c1b-047b6e2e27f6	2025-07-19 19:45:12.376555	2025-07-19 19:45:12.376555
84	10	11	淡江大學同學	friends	1	3b278df0-5151-45f2-8c1b-047b6e2e27f6	2025-07-19 19:45:12.378741	2025-07-19 19:45:12.378741
85	9	10	女	family_relationships	2	3b278df0-5151-45f2-8c1b-047b6e2e27f6	2025-07-19 19:45:12.380634	2025-07-19 19:45:12.380634
86	10	9	母	family_relationships	1	ae2d99a0-e0a0-414c-86f5-5797d1df168c	2025-07-19 19:47:32.86116	2025-07-19 19:47:32.86116
87	10	11	淡江大學同學	friends	1	ae2d99a0-e0a0-414c-86f5-5797d1df168c	2025-07-19 19:47:32.865527	2025-07-19 19:47:32.865527
88	9	10	女	family_relationships	2	ae2d99a0-e0a0-414c-86f5-5797d1df168c	2025-07-19 19:47:32.867956	2025-07-19 19:47:32.867956
89	10	9	母	family_relationships	1	6535fe21-94fa-4771-a9ef-21f375e78da0	2025-07-19 19:49:56.320455	2025-07-19 19:49:56.320455
90	10	11	淡江大學同學	friends	1	6535fe21-94fa-4771-a9ef-21f375e78da0	2025-07-19 19:49:56.322734	2025-07-19 19:49:56.322734
91	9	10	女	family_relationships	2	6535fe21-94fa-4771-a9ef-21f375e78da0	2025-07-19 19:49:56.33668	2025-07-19 19:49:56.33668
92	10	9	母	family_relationships	1	0f008689-b97a-4930-a2a2-230b141978bf	2025-07-19 19:51:25.12284	2025-07-19 19:51:25.12284
93	10	11	淡江大學同學	friends	1	0f008689-b97a-4930-a2a2-230b141978bf	2025-07-19 19:51:25.12595	2025-07-19 19:51:25.12595
94	9	10	女	family_relationships	2	0f008689-b97a-4930-a2a2-230b141978bf	2025-07-19 19:51:25.128885	2025-07-19 19:51:25.128885
95	10	9	母	family_relationships	1	755ef462-555b-4582-821f-5d579b9754b1	2025-07-19 19:53:38.114592	2025-07-19 19:53:38.114592
96	10	11	淡江大學同學	friends	1	755ef462-555b-4582-821f-5d579b9754b1	2025-07-19 19:53:38.128181	2025-07-19 19:53:38.128181
97	9	10	女	family_relationships	2	755ef462-555b-4582-821f-5d579b9754b1	2025-07-19 19:53:38.13125	2025-07-19 19:53:38.13125
98	20	10	興復航空運輸股份有限公司	friends	1	15f8edf9-6b59-4960-9d88-e80acb1fe3b5	2025-07-19 20:11:22.838116	2025-07-19 20:11:22.838116
99	10	9	母	family_relationships	2	15f8edf9-6b59-4960-9d88-e80acb1fe3b5	2025-07-19 20:11:22.842148	2025-07-19 20:11:22.842148
100	10	11	淡江大學同學	friends	2	15f8edf9-6b59-4960-9d88-e80acb1fe3b5	2025-07-19 20:11:22.843422	2025-07-19 20:11:22.843422
101	9	10	女	family_relationships	3	15f8edf9-6b59-4960-9d88-e80acb1fe3b5	2025-07-19 20:11:22.844758	2025-07-19 20:11:22.844758
102	20	10	興復航空運輸股份有限公司	friends	1	e91192c3-152d-4d20-9111-621c8cb4b150	2025-07-19 20:20:27.178528	2025-07-19 20:20:27.178528
103	10	9	母	family_relationships	2	e91192c3-152d-4d20-9111-621c8cb4b150	2025-07-19 20:20:27.196298	2025-07-19 20:20:27.196298
104	10	11	淡江大學同學	friends	2	e91192c3-152d-4d20-9111-621c8cb4b150	2025-07-19 20:20:27.197205	2025-07-19 20:20:27.197205
105	9	10	女	family_relationships	3	e91192c3-152d-4d20-9111-621c8cb4b150	2025-07-19 20:20:27.198434	2025-07-19 20:20:27.198434
\.


--
-- TOC entry 3739 (class 0 OID 0)
-- Dependencies: 211
-- Name: analysis_results_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.analysis_results_id_seq', 108, true);


--
-- TOC entry 3740 (class 0 OID 0)
-- Dependencies: 209
-- Name: person_profile_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.person_profile_id_seq', 20, true);


--
-- TOC entry 3741 (class 0 OID 0)
-- Dependencies: 214
-- Name: relationship_layers_id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public.relationship_layers_id_seq', 105, true);


--
-- TOC entry 3562 (class 2606 OID 16535)
-- Name: analysis_results analysis_results_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.analysis_results
    ADD CONSTRAINT analysis_results_pkey PRIMARY KEY (id);


--
-- TOC entry 3567 (class 2606 OID 16547)
-- Name: analysis_sessions analysis_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.analysis_sessions
    ADD CONSTRAINT analysis_sessions_pkey PRIMARY KEY (id);


--
-- TOC entry 3560 (class 2606 OID 16450)
-- Name: person_profile person_profile_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.person_profile
    ADD CONSTRAINT person_profile_pkey PRIMARY KEY (id);


--
-- TOC entry 3573 (class 2606 OID 16562)
-- Name: relationship_layers relationship_layers_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_pkey PRIMARY KEY (id);


--
-- TOC entry 3575 (class 2606 OID 16564)
-- Name: relationship_layers relationship_layers_source_person_id_target_person_id_analy_key; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_source_person_id_target_person_id_analy_key UNIQUE (source_person_id, target_person_id, analysis_session_id);


--
-- TOC entry 3563 (class 1259 OID 16536)
-- Name: idx_analysis_results_person_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_analysis_results_person_id ON public.analysis_results USING btree (person_id);


--
-- TOC entry 3564 (class 1259 OID 16538)
-- Name: idx_analysis_results_person_unique; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX idx_analysis_results_person_unique ON public.analysis_results USING btree (person_id) WHERE ((status)::text = ANY ((ARRAY['pending'::character varying, 'processing'::character varying])::text[]));


--
-- TOC entry 3565 (class 1259 OID 16537)
-- Name: idx_analysis_results_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_analysis_results_status ON public.analysis_results USING btree (status);


--
-- TOC entry 3568 (class 1259 OID 16577)
-- Name: idx_analysis_session; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_analysis_session ON public.relationship_layers USING btree (analysis_session_id);


--
-- TOC entry 3569 (class 1259 OID 16578)
-- Name: idx_layer_depth; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_layer_depth ON public.relationship_layers USING btree (layer_depth);


--
-- TOC entry 3570 (class 1259 OID 16575)
-- Name: idx_source_person; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_source_person ON public.relationship_layers USING btree (source_person_id);


--
-- TOC entry 3571 (class 1259 OID 16576)
-- Name: idx_target_person; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_target_person ON public.relationship_layers USING btree (target_person_id);


--
-- TOC entry 3576 (class 2606 OID 16548)
-- Name: analysis_sessions analysis_sessions_root_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.analysis_sessions
    ADD CONSTRAINT analysis_sessions_root_person_id_fkey FOREIGN KEY (root_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- TOC entry 3577 (class 2606 OID 16565)
-- Name: relationship_layers relationship_layers_source_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_source_person_id_fkey FOREIGN KEY (source_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


--
-- TOC entry 3578 (class 2606 OID 16570)
-- Name: relationship_layers relationship_layers_target_person_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.relationship_layers
    ADD CONSTRAINT relationship_layers_target_person_id_fkey FOREIGN KEY (target_person_id) REFERENCES public.person_profile(id) ON DELETE CASCADE;


-- Completed on 2025-07-19 20:35:39 CST

--
-- PostgreSQL database dump complete
--

