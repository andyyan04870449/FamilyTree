# FamilyTree 系統完整資料庫架構

## 一、使用者管理系統資料表（新增）

### 1. users 表（使用者基本資料）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | VARCHAR(50) | PRIMARY KEY | 使用者唯一識別碼，使用 UUID |
| username | VARCHAR(100) | UNIQUE, NOT NULL | 使用者名稱，用於登入 |
| email | VARCHAR(255) | UNIQUE, NOT NULL | 電子郵件，用於登入和識別 |
| password_hash | VARCHAR(255) | NOT NULL | 密碼雜湊值（BCrypt） |
| full_name | VARCHAR(200) | | 使用者全名 |
| role | VARCHAR(20) | DEFAULT 'user' | 系統角色：admin（管理員）或 user（一般使用者） |
| status | VARCHAR(20) | DEFAULT 'active' | 帳號狀態：active（啟用）或 inactive（停用） |
| created_at | TIMESTAMP | DEFAULT NOW() | 帳號建立時間 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 最後更新時間 |
| last_login_at | TIMESTAMP | | 最後登入時間 |

### 2. project_members 表（專案成員權限）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| project_id | VARCHAR(25) | FK, NOT NULL, PK | 專案 ID，關聯到 projects 表 |
| user_id | VARCHAR(50) | FK, NOT NULL, PK | 使用者 ID，關聯到 users 表 |
| access_level | VARCHAR(20) | NOT NULL, DEFAULT 'viewer' | 權限：owner、editor、viewer |
| created_at | TIMESTAMP | DEFAULT NOW() | 加入專案時間 |
| created_by | VARCHAR(50) | FK | 邀請者的使用者 ID |

### 3. user_tokens 表（Token 管理）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | VARCHAR(100) | PRIMARY KEY | Token 唯一識別碼 |
| user_id | VARCHAR(50) | FK, NOT NULL | 使用者 ID |
| token_type | VARCHAR(20) | NOT NULL | Token 類型：refresh |
| token_hash | VARCHAR(255) | NOT NULL | Token 雜湊值 |
| expires_at | TIMESTAMP | NOT NULL | Token 過期時間 |
| created_at | TIMESTAMP | DEFAULT NOW() | Token 建立時間 |
| used_at | TIMESTAMP | | Token 使用時間 |

### 4. activity_logs 表（操作日誌）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | BIGSERIAL | PRIMARY KEY | 自動遞增的日誌 ID |
| user_id | VARCHAR(50) | FK | 執行操作的使用者 ID |
| action | VARCHAR(100) | NOT NULL | 執行的動作 |
| resource_type | VARCHAR(50) | | 資源類型 |
| resource_id | VARCHAR(100) | | 資源 ID |
| details | TEXT | | 操作詳細資訊 |
| ip_address | VARCHAR(45) | | 操作者 IP 位址 |
| created_at | TIMESTAMP | DEFAULT NOW() | 操作時間 |

---

## 二、原有系統資料表

### 5. projects 表（專案管理）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | VARCHAR(25) | PRIMARY KEY | 專案唯一識別碼 |
| user_id | VARCHAR(6) | NOT NULL | 建立者 ID（需要更新為關聯到 users.id） |
| project_name | VARCHAR(200) | NOT NULL | 專案名稱 |
| project_description | TEXT | | 專案描述 |
| status | VARCHAR(20) | DEFAULT 'active' | 狀態：active、completed、archived、draft |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| completed_at | TIMESTAMP | | 完成時間 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 更新時間 |

### 6. person_profile 表（人員資料）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 人員 ID |
| project_id | VARCHAR(25) | FK, NOT NULL | 所屬專案 |
| photo_index | TEXT | | 照片索引 |
| name | TEXT | | 姓名 |
| discovery_source | TEXT | | 發現來源 |
| gender | TEXT | | 性別 |
| birthday | TEXT | | 生日 |
| birthplace | TEXT | | 出生地 |
| nationality | TEXT | | 國籍 |
| ethnicity | TEXT | | 民族 |
| ancestral_origin | TEXT | | 祖籍 |
| political_party | TEXT | | 政黨 |
| id_number | TEXT | | 身分證號 |
| passport_number | TEXT | | 護照號碼 |
| phone | TEXT | | 電話 |
| mobile | TEXT | | 手機 |
| email | TEXT | | 電子郵件 |
| current_employer | TEXT | | 現職單位 |
| address | TEXT | | 地址 |
| mailing_address | TEXT | | 通訊地址 |
| family_relationships | TEXT | | 家庭關係 |
| work_experience | TEXT | | 工作經歷 |
| education | TEXT | | 學歷 |
| criminal_record | TEXT | | 犯罪記錄 |
| financial_status | TEXT | | 財務狀況 |
| health_status | TEXT | | 健康狀況 |
| military_service | TEXT | | 兵役狀況 |
| social_relations | TEXT | | 社會關係 |
| international_relations | TEXT | | 國際關係 |
| travel_history | TEXT | | 出入境記錄 |
| communication_records | TEXT | | 通聯記錄 |
| social_media | TEXT | | 社群媒體 |
| notes | TEXT | | 備註 |
| tag | TEXT | | 標籤 |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 更新時間 |

### 7. field_mapping 表（欄位對應）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 對應 ID |
| excel_field_name | VARCHAR(100) | NOT NULL | Excel 欄位名稱 |
| db_field_name | VARCHAR(100) | NOT NULL | 資料庫欄位名稱 |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 更新時間 |
| project_id | VARCHAR(25) | | 專案 ID |

### 8. analysis_results 表（分析結果）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 分析結果 ID |
| person_id | INTEGER | FK, NOT NULL | 人員 ID |
| analysis_result | JSONB | NOT NULL | 分析結果（JSON） |
| analysis_date | TIMESTAMP | DEFAULT NOW() | 分析日期 |
| progress_percentage | INTEGER | DEFAULT 0 | 進度百分比 |
| status | VARCHAR(50) | DEFAULT 'pending' | 狀態 |
| current_step | VARCHAR(255) | | 目前步驟 |
| status_message | TEXT | | 狀態訊息 |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 更新時間 |
| project_id | VARCHAR(25) | NOT NULL | 專案 ID |

### 9. analysis_sessions 表（分析會話）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | VARCHAR(100) | PRIMARY KEY | 會話 ID |
| root_person_id | INTEGER | NOT NULL | 根人員 ID |
| max_depth | INTEGER | DEFAULT 3, NOT NULL | 最大分析深度 |
| status | VARCHAR(20) | DEFAULT 'processing' | 狀態 |
| total_relationships | INTEGER | DEFAULT 0 | 關係總數 |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| completed_at | TIMESTAMP | | 完成時間 |
| project_id | VARCHAR(25) | NOT NULL | 專案 ID |

### 10. missing_persons 表（缺失人員）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | ID |
| name | VARCHAR(255) | NOT NULL | 姓名 |
| relation_type | VARCHAR(100) | NOT NULL | 關係類型 |
| source_person_id | INTEGER | NOT NULL | 來源人員 ID |
| source_field | VARCHAR(50) | NOT NULL | 來源欄位 |
| analysis_session_id | VARCHAR(255) | NOT NULL | 分析會話 ID |
| layer_depth | INTEGER | DEFAULT 1, NOT NULL | 層級深度 |
| discovered_at | TIMESTAMP | DEFAULT NOW() | 發現時間 |
| status | VARCHAR(50) | DEFAULT 'pending' | 狀態 |
| resolved_person_id | INTEGER | | 解決的人員 ID |
| notes | TEXT | | 備註 |
| project_id | VARCHAR(25) | NOT NULL | 專案 ID |

### 11. relationship_layers 表（關係層級）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | ID |
| analysis_session_id | VARCHAR(100) | FK, NOT NULL | 分析會話 ID |
| layer_number | INTEGER | NOT NULL | 層級編號 |
| person_id | INTEGER | FK, NOT NULL | 人員 ID |
| parent_person_id | INTEGER | FK | 父層人員 ID |
| relation_type | VARCHAR(100) | | 關係類型 |
| relation_field | VARCHAR(50) | | 關係欄位 |
| processed_at | TIMESTAMP | DEFAULT NOW() | 處理時間 |
| project_id | VARCHAR(25) | NOT NULL | 專案 ID |

### 12. favorites 表（我的最愛）
| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | ID |
| user_id | VARCHAR(50) | FK, NOT NULL | 使用者 ID（需更新關聯） |
| person_id | INTEGER | FK, NOT NULL | 人員 ID |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| project_id | VARCHAR(25) | NOT NULL | 專案 ID |

---

## 需要注意的資料庫調整

1. **projects.user_id** 欄位：
   - 目前是 VARCHAR(6)
   - 需要改為 VARCHAR(50) 以配合新的 users.id

2. **favorites.user_id** 欄位：
   - 需要更新為關聯到新的 users 表

3. **外鍵關係**：
   - 需要建立 projects.user_id → users.id 的外鍵
   - 需要建立 favorites.user_id → users.id 的外鍵

4. **資料遷移**：
   - 現有的 user_id 資料需要對應到新的使用者系統
   - 或者為現有資料建立預設使用者

---

## 索引清單

### 使用者管理系統索引
- users: idx_users_email, idx_users_username
- project_members: idx_project_members_user_id, idx_project_members_project_id
- user_tokens: idx_user_tokens_user_id, idx_user_tokens_token_hash
- activity_logs: idx_activity_logs_user_id, idx_activity_logs_created_at

### 原有系統索引
- 需要檢查和建立適當的索引以優化查詢效能