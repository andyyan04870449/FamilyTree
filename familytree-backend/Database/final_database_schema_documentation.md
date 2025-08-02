# FamilyTree 系統資料庫架構 - 完整欄位說明

## 一、使用者管理系統資料表（新增 3 個表）

### 1. users 表（使用者帳號資料）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | VARCHAR(50) | PRIMARY KEY | 使用者唯一識別碼（UUID格式） |
| username | VARCHAR(100) | UNIQUE, NOT NULL | 登入帳號名稱 |
| email | VARCHAR(255) | UNIQUE, NOT NULL | 電子郵件地址 |
| password_hash | VARCHAR(255) | NOT NULL | BCrypt 加密的密碼雜湊值 |
| full_name | VARCHAR(200) | | 使用者真實姓名 |
| role | VARCHAR(20) | DEFAULT 'user' | 系統角色：admin 或 user |
| status | VARCHAR(20) | DEFAULT 'active' | 帳號狀態：active 或 inactive |
| created_at | TIMESTAMP | DEFAULT NOW() | 帳號建立時間 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 最後更新時間 |
| last_login_at | TIMESTAMP | | 最後登入時間 |

**權限說明**：
- `admin`：可查看系統內所有使用者的資料
- `user`：只能查看自己上傳的資料

---

### 2. user_tokens 表（Token 管理）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | VARCHAR(100) | PRIMARY KEY | Token 記錄唯一識別碼 |
| user_id | VARCHAR(50) | FK, NOT NULL | 關聯的使用者 ID |
| token_type | VARCHAR(20) | NOT NULL | Token 類型（目前只有 refresh） |
| token_hash | VARCHAR(255) | NOT NULL | Token 的雜湊值（不存明文） |
| expires_at | TIMESTAMP | NOT NULL | Token 過期時間 |
| created_at | TIMESTAMP | DEFAULT NOW() | Token 建立時間 |
| used_at | TIMESTAMP | | 最後使用時間（可選） |

**用途**：儲存 JWT Refresh Token，用於更新 Access Token

---

### 3. activity_logs 表（操作日誌）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | BIGSERIAL | PRIMARY KEY | 自動遞增的日誌編號 |
| user_id | VARCHAR(50) | FK | 執行操作的使用者 ID |
| action | VARCHAR(100) | NOT NULL | 操作名稱（如：login, create_person） |
| resource_type | VARCHAR(50) | | 資源類型（如：person, file） |
| resource_id | VARCHAR(100) | | 資源的 ID |
| details | TEXT | | 操作詳情（可存 JSON 格式） |
| ip_address | VARCHAR(45) | | 操作者的 IP 位址 |
| created_at | TIMESTAMP | DEFAULT NOW() | 操作時間 |

**記錄範例**：
- 登入：`action='user_login'`
- 新增人員：`action='create_person', resource_type='person', resource_id='123'`

---

## 二、原有業務資料表（修改後）

### 4. person_profile 表（人員基本資料）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 人員唯一編號 |
| **user_id** | **VARCHAR(50)** | **FK（新增）** | **資料擁有者的使用者 ID** |
| photo_index | TEXT | | 照片索引/檔名 |
| name | TEXT | | 姓名 |
| discovery_source | TEXT | | 發現來源 |
| gender | TEXT | | 性別 |
| birthday | TEXT | | 生日 |
| birthplace | TEXT | | 出生地 |
| nationality | TEXT | | 國籍 |
| ethnicity | TEXT | | 民族 |
| ancestral_origin | TEXT | | 祖籍 |
| political_party | TEXT | | 政黨 |
| id_number | TEXT | | 身分證號碼 |
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

**注意**：移除了 project_id，改用 user_id 做資料隔離

---

### 5. favorites 表（我的最愛）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 最愛記錄編號 |
| **user_id** | **VARCHAR(50)** | **FK（更新）** | **使用者 ID** |
| person_id | INTEGER | FK, NOT NULL | 人員 ID |
| created_at | TIMESTAMP | DEFAULT NOW() | 加入最愛時間 |

**注意**：user_id 欄位更新為新的使用者系統格式

---

### 6. field_mapping 表（Excel 欄位對應）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 對應記錄編號 |
| **user_id** | **VARCHAR(50)** | **FK（新增）** | **資料擁有者的使用者 ID** |
| excel_field_name | VARCHAR(100) | NOT NULL | Excel 中的欄位名稱 |
| db_field_name | VARCHAR(100) | NOT NULL | 資料庫中的欄位名稱 |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 更新時間 |

**用途**：記錄每個使用者的 Excel 欄位對應設定

---

### 7. analysis_results 表（AI 分析結果）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 分析結果編號 |
| **user_id** | **VARCHAR(50)** | **FK（新增）** | **資料擁有者的使用者 ID** |
| person_id | INTEGER | FK, NOT NULL | 被分析的人員 ID |
| analysis_result | JSONB | NOT NULL | 分析結果（JSON 格式） |
| analysis_date | TIMESTAMP | DEFAULT NOW() | 分析日期 |
| progress_percentage | INTEGER | DEFAULT 0 | 進度百分比（0-100） |
| status | VARCHAR(50) | DEFAULT 'pending' | 狀態（pending/processing/completed/failed） |
| current_step | VARCHAR(255) | | 目前執行步驟 |
| status_message | TEXT | | 狀態訊息 |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 更新時間 |

---

### 8. analysis_sessions 表（分析會話）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | VARCHAR(100) | PRIMARY KEY | 會話唯一識別碼 |
| **user_id** | **VARCHAR(50)** | **FK（新增）** | **發起分析的使用者 ID** |
| root_person_id | INTEGER | NOT NULL | 分析起始人員 ID |
| max_depth | INTEGER | DEFAULT 3 | 最大分析深度（層數） |
| status | VARCHAR(20) | DEFAULT 'processing' | 會話狀態 |
| total_relationships | INTEGER | DEFAULT 0 | 發現的關係總數 |
| created_at | TIMESTAMP | DEFAULT NOW() | 開始時間 |
| completed_at | TIMESTAMP | | 完成時間 |

---

### 9. missing_persons 表（缺失人員記錄）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 記錄編號 |
| **user_id** | **VARCHAR(50)** | **FK（新增）** | **資料擁有者的使用者 ID** |
| name | VARCHAR(255) | NOT NULL | 缺失人員姓名 |
| relation_type | VARCHAR(100) | NOT NULL | 關係類型（父親/母親/配偶等） |
| source_person_id | INTEGER | NOT NULL | 來源人員 ID |
| source_field | VARCHAR(50) | NOT NULL | 來源欄位名稱 |
| analysis_session_id | VARCHAR(255) | NOT NULL | 關聯的分析會話 ID |
| layer_depth | INTEGER | DEFAULT 1 | 在關係網中的層級深度 |
| discovered_at | TIMESTAMP | DEFAULT NOW() | 發現時間 |
| status | VARCHAR(50) | DEFAULT 'pending' | 處理狀態 |
| resolved_person_id | INTEGER | | 已解決時對應的人員 ID |
| notes | TEXT | | 備註 |

---

### 10. relationship_layers 表（關係層級）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | INTEGER | PRIMARY KEY | 記錄編號 |
| **user_id** | **VARCHAR(50)** | **FK（新增）** | **資料擁有者的使用者 ID** |
| analysis_session_id | VARCHAR(100) | FK, NOT NULL | 分析會話 ID |
| layer_number | INTEGER | NOT NULL | 層級編號（1,2,3...） |
| person_id | INTEGER | FK, NOT NULL | 人員 ID |
| parent_person_id | INTEGER | FK | 上一層的人員 ID |
| relation_type | VARCHAR(100) | | 與上層的關係類型 |
| relation_field | VARCHAR(50) | | 關係來源欄位 |
| processed_at | TIMESTAMP | DEFAULT NOW() | 處理時間 |

---

## 三、已移除的欄位

所有資料表都已移除 `project_id` 欄位，因為：
- 不再使用專案概念
- 改為直接使用 `user_id` 進行資料隔離

---

## 四、資料隔離邏輯

### 查詢範例

```sql
-- 一般使用者查詢（只能看自己的資料）
SELECT * FROM person_profile WHERE user_id = '當前使用者ID';

-- 管理員查詢（可以看所有資料）
SELECT p.*, u.username as owner_name 
FROM person_profile p
JOIN users u ON p.user_id = u.id;
```

### 權限檢查函數

```sql
-- 使用 check_data_access 函數
SELECT * FROM person_profile p
WHERE check_data_access('當前使用者ID', 'person', p.id::text, p.user_id);
```

---

## 五、索引設計

每個資料表的 `user_id` 欄位都建立了索引，確保查詢效能：
- idx_person_profile_user_id
- idx_favorites_user_id
- idx_field_mapping_user_id
- idx_analysis_results_user_id
- idx_analysis_sessions_user_id
- idx_missing_persons_user_id
- idx_relationship_layers_user_id