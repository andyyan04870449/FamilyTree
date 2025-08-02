# FamilyTree 系統資料庫架構完整分析報告

## 📋 目錄
1. [系統概述](#系統概述)
2. [資料庫架構總覽](#資料庫架構總覽)
3. [核心資料表分析](#核心資料表分析)
4. [關聯關係分析](#關聯關係分析)
5. [權限與安全架構](#權限與安全架構)
6. [資料隔離策略](#資料隔離策略)
7. [索引與性能優化](#索引與性能優化)
8. [視圖與函數](#視圖與函數)
9. [架構評估與建議](#架構評估與建議)
10. [最佳實踐建議](#最佳實踐建議)

---

## 系統概述

FamilyTree 是一個家族譜系管理系統，支援多租戶資料隔離、權限管理、檔案上傳、AI 分析等功能。系統採用 PostgreSQL 資料庫，具備完整的使用者認證授權機制。

### 技術規格
- **資料庫引擎**: PostgreSQL
- **ORM框架**: Entity Framework Core (預計)
- **認證方式**: JWT Token 機制
- **資料隔離**: 基於 user_id 的多租戶架構
- **權限模式**: RBAC (角色基礎存取控制)

---

## 資料庫架構總覽

### 表分類結構

#### 🏢 核心使用者管理 (5 個表)
- `users` - 使用者基本資料
- `user_tokens` - JWT Token 管理
- `activity_logs` - 操作日誌
- `roles` - 角色定義 (新式系統)
- `user_roles` - 使用者角色關聯

#### 👥 權限管理系統 (4 個表)
- `permissions` - 權限定義
- `role_permissions` - 角色權限關聯
- `user_permissions` - 使用者直接權限
- `project_permissions` - 專案權限 (待廢除)

#### 👤 人員資料管理 (3 個表)
- `person_profile` - 人員基本資料 (主表)
- `user_favorites` - 使用者最愛
- `relationships` - 人員關係網

#### 🔬 AI 分析功能 (4 個表)
- `analysis_sessions` - 分析會話
- `analysis_results` - 分析結果
- `missing_persons` - 缺失人員記錄
- `relationship_layers` - 關係層級

#### 📁 檔案管理 (2 個表)
- `file_metadata` - 檔案元資料
- `file_uploads` - 檔案上傳記錄

#### ⚙️ 系統輔助 (2 個表)
- `field_mapping` - Excel 欄位對應
- `projects` - 專案表 (逐步廢除中)

---

## 核心資料表分析

### 1. users 表 (使用者基本資料)

| 欄位名稱 | 資料類型 | 約束條件 | 業務用途 | 設計評價 |
|---------|---------|---------|---------|---------|
| id | VARCHAR(50) | PRIMARY KEY, UUID | 使用者唯一識別碼 | ✅ 優秀：使用 UUID 提供更好的分散式支援 |
| username | VARCHAR(100) | UNIQUE, NOT NULL | 登入帳號名稱 | ✅ 優秀：長度適中，支援多語言 |
| email | VARCHAR(255) | UNIQUE, NOT NULL | 電子郵件地址 | ✅ 優秀：標準 Email 長度限制 |
| password_hash | VARCHAR(255) | NOT NULL | BCrypt 密碼雜湊 | ✅ 優秀：安全的密碼儲存方式 |
| full_name | VARCHAR(200) | NULLABLE | 使用者真實姓名 | ✅ 合理：允許空值的彈性設計 |
| role | VARCHAR(20) | DEFAULT 'user' | 系統角色 (舊式) | ⚠️ 待改進：建議完全移轉到新式 RBAC |
| status | VARCHAR(20) | DEFAULT 'active' | 帳號狀態 | ✅ 優秀：狀態管理完整 |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 | ✅ 標準：時間戳記錄完整 |
| updated_at | TIMESTAMP | DEFAULT NOW() | 更新時間 | ✅ 標準：具備自動更新觸發器 |
| last_login_at | TIMESTAMP | NULLABLE | 最後登入時間 | ✅ 優秀：用戶活躍度追蹤 |

**索引設計**:
- `idx_users_email` - Email 查詢優化
- `idx_users_username` - 使用者名稱查詢優化
- `idx_users_status` - 狀態篩選優化

**設計優點**:
- UUID 主鍵提供良好的分散式擴展性
- 完整的約束條件確保資料一致性
- 觸發器自動維護 updated_at 欄位

**改進建議**:
- 考慮新增 email_verified、phone 等欄位
- 建議完全移除 role 欄位，統一使用 RBAC 系統

### 2. person_profile 表 (人員基本資料)

這是系統的核心業務表，包含豐富的人員資訊欄位：

#### 基本資訊欄位
| 欄位群組 | 欄位名稱 | 資料類型 | 業務用途 |
|---------|---------|---------|---------|
| 識別資訊 | id, file_md5, photo_index | INTEGER, TEXT | 人員唯一識別與照片索引 |
| 基本資料 | name, gender, birthday, birthplace | TEXT | 基本個人資訊 |
| 身份資訊 | nationality, ethnicity, ancestral_origin | TEXT | 身份背景資訊 |
| 聯絡資訊 | phone, mobile, email, address | TEXT | 聯絡方式 |

#### 進階資訊欄位
| 欄位群組 | 包含欄位數量 | 主要用途 |
|---------|-------------|---------|
| 政治資訊 | 2 欄 | political_party, id_number |
| 工作資訊 | 3 欄 | current_employer, current_workplace, experience |
| 教育資訊 | 1 欄 | education |
| 社交資訊 | 4 欄 | family_relationships, friends, social_relations |
| 活動資訊 | 3 欄 | activities, frequent_places, travel_records |
| 其他資訊 | 5 欄 | publications, online_accounts, notes, remarks |

#### 系統控制欄位
- `user_id` - 資料擁有者 (關鍵的多租戶隔離欄位)
- `project_id` - 專案關聯 (逐步廢除中)
- `created_at/updated_at` - 時間戳記錄
- `created_by/updated_by` - 操作者記錄

**設計優點**:
- 欄位設計涵蓋全面，滿足複雜的人員資料需求
- 彈性的 TEXT 類型允許不同長度的內容
- 完整的審計欄位追蹤資料變更

**設計缺點**:
- 部分欄位命名不一致 (如 current_employer vs current_workplace)
- 缺乏正規化，可能導致資料冗餘
- 某些欄位 (如 travel_records vs travel_history) 重複

### 3. user_tokens 表 (JWT Token 管理)

| 欄位名稱 | 資料類型 | 約束條件 | 業務用途 |
|---------|---------|---------|---------|
| id | VARCHAR(100) | PRIMARY KEY | Token 記錄唯一識別碼 |
| user_id | VARCHAR(50) | FK, NOT NULL | 關聯使用者 |
| token_type | VARCHAR(20) | CHECK 約束 | Token 類型 (refresh) |
| token_hash | VARCHAR(255) | NOT NULL | Token 雜湊值 |
| expires_at | TIMESTAMP | NOT NULL | 過期時間 |
| created_at | TIMESTAMP | DEFAULT NOW() | 建立時間 |
| used_at | TIMESTAMP | NULLABLE | 最後使用時間 |

**安全特性**:
- 僅儲存 Token 雜湊值，不存明文
- 完整的過期時間管理
- 支援 Token 撤銷機制

### 4. 權限管理系統表群

#### roles 表 (角色定義)
```sql
CREATE TABLE roles (
    id VARCHAR(50) PRIMARY KEY,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    level INTEGER DEFAULT 0,  -- 角色層級
    is_system BOOLEAN DEFAULT FALSE,  -- 系統內建角色
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### permissions 表 (權限定義)
```sql
CREATE TABLE permissions (
    id SERIAL PRIMARY KEY,
    permission VARCHAR(100) UNIQUE NOT NULL,  -- 如 'user:create'
    display_name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(50) NOT NULL,  -- 權限分類
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**權限命名規範**: `resource:action` 格式
- `user:create` - 建立使用者
- `person:read` - 查看人員資料
- `file:upload` - 上傳檔案

---

## 關聯關係分析

### 核心關聯圖

```
users (1) ←→ (M) user_roles (M) ←→ (1) roles
  ↓                                    ↓
  └─ user_permissions ←─ permissions ←─ role_permissions
  ↓
  ├─ person_profile (1:M)
  ├─ user_favorites (1:M)
  ├─ analysis_sessions (1:M)
  ├─ activity_logs (1:M)
  └─ file_uploads (1:M)
```

### 關聯關係詳細分析

#### 1. 使用者 ←→ 權限關係 (多對多)
```sql
-- 直接關聯：使用者 ←→ 角色 ←→ 權限
users → user_roles → roles → role_permissions → permissions

-- 額外權限：使用者 ←→ 權限 (直接關聯)
users → user_permissions → permissions
```

#### 2. 使用者 ←→ 業務資料關係 (一對多)
- 一個使用者可以擁有多筆 person_profile
- 一個使用者可以有多個 user_favorites
- 一個使用者可以進行多次 analysis_sessions

#### 3. 人員關係網 (多對多)
```sql
person_profile (1) ←→ (M) relationships (M) ←→ (1) person_profile
```

### 外鍵約束分析

| 子表 | 父表 | 外鍵欄位 | 級聯行為 | 評價 |
|------|------|----------|----------|------|
| user_tokens | users | user_id | CASCADE | ✅ 正確：使用者刪除時清理 Token |
| person_profile | users | user_id | RESTRICT | ✅ 正確：保護使用者資料不被誤刪 |
| activity_logs | users | user_id | SET NULL | ✅ 正確：保留日誌記錄 |
| user_roles | users/roles | user_id/role_id | CASCADE | ✅ 正確：角色變更時清理關聯 |

---

## 權限與安全架構

### RBAC 權限模型

系統採用基於角色的存取控制 (RBAC) 模型：

#### 預設角色層級
| 角色 | 層級 | 權限範圍 | 說明 |
|------|------|----------|------|
| superadmin | 100 | 所有權限 | 系統最高權限 |
| admin | 90 | 大部分管理權限 | 一般管理員 |
| user | 50 | 基本使用權限 | 一般使用者 |
| guest | 0 | 僅查看權限 | 訪客用戶 |

#### 權限分類系統
```sql
-- 權限類別
系統管理: user:*, role:*, system:*
專案管理: project:*
人員管理: person:*
檔案管理: file:*
報表功能: report:*
搜尋功能: search:*
```

#### 權限檢查函數
```sql
-- 基本權限檢查
check_user_permission(user_id, action) → BOOLEAN

-- 資料存取權限檢查  
check_data_access(user_id, resource_type, resource_id, owner_user_id) → BOOLEAN
```

### 安全特性

#### 1. 密碼安全
- 使用 BCrypt 進行密碼雜湊
- 密碼長度最少 8 字元
- 不儲存明文密碼

#### 2. Token 安全
- JWT 雙 Token 機制 (Access + Refresh)
- Token 雜湊儲存，不存明文
- 過期時間管理
- 自動清理過期 Token

#### 3. 資料隔離
- 基於 user_id 的多租戶隔離
- 管理員可存取所有資料
- 一般使用者僅存取自有資料

---

## 資料隔離策略

### 多租戶架構設計

#### 隔離層級
1. **應用層隔離**: 通過 user_id 欄位實現
2. **查詢層隔離**: 所有業務查詢強制帶入 user_id 條件
3. **權限層隔離**: 通過 RBAC 系統控制存取

#### 隔離實施方式

```sql
-- 一般使用者查詢 (僅查看自己的資料)
SELECT * FROM person_profile WHERE user_id = '當前使用者ID';

-- 管理員查詢 (可查看所有資料，顯示擁有者)
SELECT p.*, u.username as owner_name 
FROM person_profile p
JOIN users u ON p.user_id = u.id;

-- 權限檢查查詢
SELECT * FROM person_profile p
WHERE check_data_access('當前使用者ID', 'person', p.id::text, p.user_id);
```

#### 資料遷移策略
- 現有資料統一歸屬於預設管理員 (`admin_default`)
- 新資料自動關聯到建立者的 user_id
- 支援資料擁有權轉移

### 隔離評估

| 層面 | 實施狀況 | 安全等級 | 建議 |
|------|----------|----------|------|
| 業務邏輯隔離 | ✅ 完整 | 高 | 持續維護查詢條件 |
| 權限控制隔離 | ✅ 完整 | 高 | 定期審核權限設定 |
| 資料庫層隔離 | ⚠️ 部分 | 中 | 考慮實施 RLS (Row Level Security) |

---

## 索引與性能優化

### 索引策略分析

#### 主要表索引設計

##### person_profile 表索引
```sql
-- 基礎查詢索引
idx_person_profile_user_id_active    -- 使用者資料查詢 (主要)
idx_person_profile_name              -- 姓名查詢
idx_person_profile_birthday          -- 生日查詢
idx_person_profile_gender            -- 性別篩選

-- 複合索引 (組合查詢)
idx_person_profile_user_name         -- 使用者 + 姓名
idx_person_profile_user_gender       -- 使用者 + 性別

-- 全文搜索索引
idx_person_profile_fulltext          -- GIN 索引支援全文搜索

-- 分析用索引
idx_person_profile_birth_year        -- 年份統計分析
idx_person_profile_created_at        -- 時間序列查詢
```

##### 權限系統索引
```sql
idx_user_roles_user_id               -- 使用者角色查詢
idx_role_permissions_role_id         -- 角色權限查詢
idx_user_permissions_user_id         -- 直接權限查詢
```

##### 日誌系統索引
```sql
idx_activity_logs_user_id            -- 使用者活動查詢
idx_activity_logs_created_at         -- 時間範圍查詢
idx_activity_logs_action             -- 操作類型查詢
```

### 性能優化特性

#### 1. 並發索引建立
```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_name ON table_name(column);
```
- 使用 `CONCURRENTLY` 避免阻塞
- 適合生產環境的索引建立

#### 2. 條件索引 (Partial Index)
```sql
-- 僅為有效資料建立索引
CREATE INDEX idx_person_profile_user_id_active 
ON person_profile(user_id) 
WHERE user_id IS NOT NULL;
```

#### 3. 全文搜索優化
```sql
-- GIN 索引支援多欄位全文搜索
CREATE INDEX idx_person_profile_fulltext 
ON person_profile 
USING gin(to_tsvector('simple', 
    coalesce(name,'') || ' ' || 
    coalesce(email,'') || ' ' || 
    coalesce(mobile,'') || ' ' ||
    coalesce(family_relationships,'') || ' ' ||
    coalesce(friends,'') || ' ' ||
    coalesce(remarks,'')
));
```

#### 4. 關係查詢優化
```sql
-- 雙向關係查詢索引
idx_relationships_bidirectional 
ON relationships(LEAST(person_id, related_person_id), GREATEST(person_id, related_person_id))

-- 防重複唯一索引
idx_relationships_unique 
ON relationships(person_id, related_person_id, relationship_type)
```

### 性能評估

| 查詢類型 | 索引支援 | 預期性能 | 建議優化 |
|---------|----------|----------|----------|
| 使用者資料查詢 | ✅ 完整 | 優秀 | 無 |
| 姓名模糊搜索 | ✅ 全文索引 | 良好 | 考慮 trigram 索引 |
| 關係網查詢 | ✅ 雙向索引 | 良好 | 考慮圖形資料庫 |
| 統計分析查詢 | ✅ 部分索引 | 中等 | 新增聚合表 |
| 日誌查詢 | ✅ 時間索引 | 良好 | 考慮分區表 |

---

## 視圖與函數

### 系統視圖

#### 1. v_user_statistics (使用者統計)
```sql
-- 提供使用者資料摘要統計
SELECT 
    u.id, u.username, u.email, u.status,
    COUNT(person_profile) as person_count,
    COUNT(user_favorites) as favorite_count,
    COUNT(analysis_sessions) as analysis_count,
    最近30天活動數量
FROM users u LEFT JOIN 各業務表...
```

#### 2. v_user_activity_summary (活動摘要)
```sql
-- 使用者活動記錄檢視
SELECT u.username, al.action, al.resource_type, al.created_at
FROM users u JOIN activity_logs al...
ORDER BY al.created_at DESC
```

### 系統函數

#### 1. 權限檢查函數

```sql
-- 資料存取權限檢查
check_data_access(user_id, resource_type, resource_id, owner_user_id) → BOOLEAN

-- 使用者操作權限檢查
check_user_permission(user_id, action) → BOOLEAN
```

#### 2. 日誌管理函數

```sql
-- 記錄活動日誌
log_activity(user_id, action, resource_type, resource_id, details, ip_address)

-- 清理過期 Token
cleanup_expired_tokens() → INTEGER
```

#### 3. 統計查詢函數

```sql
-- 使用者資料摘要
get_user_data_summary(user_id) → TABLE(data_type, count)
```

### 觸發器系統

#### 自動時間戳更新
```sql
-- 自動更新 updated_at 欄位
CREATE TRIGGER update_users_updated_at 
BEFORE UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
```

---

## 架構評估與建議

### 優點分析

#### 1. 安全性設計 ✅
- **完整的認證授權**: JWT + RBAC 雙重保障
- **資料隔離**: 多租戶架構設計完善
- **密碼安全**: BCrypt 雜湊 + 複雜度要求
- **審計功能**: 完整的操作日誌記錄

#### 2. 擴展性設計 ✅
- **UUID 主鍵**: 支援分散式擴展
- **模組化表結構**: 功能分離明確
- **索引策略**: 性能優化充分考慮
- **視圖抽象**: 簡化複雜查詢

#### 3. 維護性設計 ✅
- **完整註解**: 表和欄位說明詳細
- **觸發器**: 自動化資料維護
- **函數封裝**: 複雜邏輯統一管理
- **遷移腳本**: 版本控制良好

### 缺點分析

#### 1. 資料模型設計 ⚠️

**person_profile 表設計問題**:
- 欄位過於扁平化，缺乏正規化
- 部分欄位命名不一致
- 某些欄位功能重複
- TEXT 類型缺乏長度限制

**改進建議**:
```sql
-- 考慮拆分為多個關聯表
person_basic_info     -- 基本資料
person_contact_info   -- 聯絡資訊
person_work_info      -- 工作資訊
person_education_info -- 教育資訊
person_notes          -- 備註和額外資料
```

#### 2. 權限系統複雜度 ⚠️

**問題**:
- 新舊權限系統並存 (role 欄位 vs RBAC)
- project_permissions 表功能不明確
- 權限繼承關係複雜

**改進建議**:
- 完全移除 users.role 欄位
- 清理 project_permissions 表
- 簡化權限檢查邏輯

#### 3. 性能瓶頸風險 ⚠️

**潛在問題**:
- person_profile 表可能成為性能瓶頸
- 缺乏資料分區策略
- 全文搜索可能影響寫入性能

**建議**:
- 考慮按 user_id 進行表分區
- 實施讀寫分離
- 為大量資料場景準備快取策略

### 技術債務分析

#### 1. 高優先級技術債務
- [ ] 移除舊式 role 欄位
- [ ] 清理 project_id 相關欄位
- [ ] 統一欄位命名規範

#### 2. 中優先級技術債務
- [ ] person_profile 表正規化
- [ ] 實施 Row Level Security
- [ ] 新增資料備份策略

#### 3. 低優先級技術債務
- [ ] 考慮引入時序資料庫記錄日誌
- [ ] 評估圖形資料庫處理關係網
- [ ] 實施資料壓縮策略

---

## 最佳實踐建議

### 1. 立即執行建議 (高優先級)

#### 資料一致性強化
```sql
-- 新增必要的 CHECK 約束
ALTER TABLE users ADD CONSTRAINT chk_users_status 
CHECK (status IN ('active', 'inactive', 'suspended'));

-- 新增資料長度限制
ALTER TABLE person_profile ALTER COLUMN name TYPE VARCHAR(200);
ALTER TABLE person_profile ALTER COLUMN email TYPE VARCHAR(255);
```

#### 權限系統清理
```sql
-- 移除舊式 role 欄位 (在完全遷移到 RBAC 後)
-- ALTER TABLE users DROP COLUMN role;

-- 清理無用的 project_permissions 表
-- DROP TABLE IF EXISTS project_permissions;
```

### 2. 短期改進建議 (3-6個月)

#### 實施 Row Level Security
```sql
-- 啟用 RLS
ALTER TABLE person_profile ENABLE ROW LEVEL SECURITY;

-- 建立安全策略
CREATE POLICY person_profile_policy ON person_profile
FOR ALL TO authenticated_users
USING (user_id = current_setting('app.current_user_id') 
       OR check_user_permission(current_setting('app.current_user_id'), 'admin'));
```

#### 資料分區策略
```sql
-- 按 user_id 範圍分區 (適用於大量使用者場景)
CREATE TABLE person_profile_partitioned (
    LIKE person_profile INCLUDING ALL
) PARTITION BY HASH (user_id);

-- 建立分區表
CREATE TABLE person_profile_p0 PARTITION OF person_profile_partitioned
FOR VALUES WITH (modulus 4, remainder 0);
```

### 3. 長期改進建議 (6-12個月)

#### 引入事件溯源架構
```sql
-- 事件儲存表
CREATE TABLE domain_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aggregate_type VARCHAR(50) NOT NULL,
    aggregate_id VARCHAR(50) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_data JSONB NOT NULL,
    event_version INTEGER NOT NULL,
    occurred_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    user_id VARCHAR(50) REFERENCES users(id)
);
```

#### 讀寫分離架構
```sql
-- 建立物化檢視用於查詢優化
CREATE MATERIALIZED VIEW mv_person_search AS
SELECT 
    p.id, p.user_id, p.name, p.gender, p.birthday,
    to_tsvector('simple', 
        coalesce(p.name,'') || ' ' || 
        coalesce(p.email,'') || ' ' || 
        coalesce(p.notes,'')
    ) as search_vector
FROM person_profile p
WHERE p.user_id IS NOT NULL;

-- 建立自動重新整理
CREATE UNIQUE INDEX ON mv_person_search (id);
```

### 4. 監控與維護建議

#### 性能監控
```sql
-- 建立性能監控檢視
CREATE VIEW v_performance_metrics AS
SELECT 
    schemaname,
    tablename,
    n_tup_ins as inserts,
    n_tup_upd as updates,
    n_tup_del as deletes,
    n_live_tup as live_rows,
    n_dead_tup as dead_rows,
    last_vacuum,
    last_autovacuum,
    last_analyze,
    last_autoanalyze
FROM pg_stat_user_tables
ORDER BY n_live_tup DESC;
```

#### 定期維護作業
```sql
-- 清理過期 Token 的排程作業
SELECT cleanup_expired_tokens();

-- 更新統計資訊
ANALYZE;

-- 重新整理物化檢視
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_person_search;
```

### 5. 安全強化建議

#### 連線安全
```sql
-- 限制連線來源
-- 在 postgresql.conf 中設定
listen_addresses = 'localhost,10.0.0.0/8'

-- 在 pg_hba.conf 中設定
hostssl all all 10.0.0.0/8 md5
```

#### 資料加密
```sql
-- 敏感欄位加密 (使用 pgcrypto 擴展)
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 加密函數範例
CREATE OR REPLACE FUNCTION encrypt_sensitive_data(data TEXT)
RETURNS TEXT AS $$
BEGIN
    RETURN encode(encrypt(data::bytea, 'encryption_key', 'aes'), 'base64');
END;
$$ LANGUAGE plpgsql;
```

---

## 總結

FamilyTree 系統的資料庫架構整體設計良好，具備以下特點：

### 優勢總結
1. **安全性**: 完整的認證授權機制，多層級資料隔離
2. **擴展性**: UUID 主鍵、模組化設計、完善的索引策略
3. **維護性**: 詳細的文檔、觸發器自動化、版本控制良好
4. **功能性**: 涵蓋完整的業務需求，支援複雜的人員關係管理

### 關鍵改進領域
1. **資料模型正規化**: person_profile 表需要拆分重構
2. **權限系統簡化**: 移除新舊系統並存的複雜度
3. **性能優化**: 為大數據場景準備分區和快取策略

### 實施優先級
1. **立即**: 資料一致性約束、權限系統清理
2. **短期**: RLS 實施、資料分區規劃
3. **長期**: 事件溯源、讀寫分離、監控體系

這個資料庫架構為 FamilyTree 系統提供了堅實的基礎，通過建議的改進措施，可以進一步提升系統的性能、安全性和可維護性。

---

**文檔版本**: v1.0  
**最後更新**: 2025-08-02  
**分析範圍**: 完整資料庫架構 (20 個表, 40+ 索引, 8 個函數, 2 個檢視)