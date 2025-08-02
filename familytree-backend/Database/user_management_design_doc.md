# 使用者帳號管理系統 - 資料庫設計文件

## 資料表總覽
1. **users** - 使用者基本資料表
2. **project_members** - 專案成員權限表
3. **user_tokens** - 使用者 Token 管理表
4. **activity_logs** - 操作日誌表

---

## 1. users 表（使用者基本資料）

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

### 索引
- idx_users_email (email)
- idx_users_username (username)

---

## 2. project_members 表（專案成員權限）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| project_id | VARCHAR(25) | FK, NOT NULL | 專案 ID，關聯到 projects 表 |
| user_id | VARCHAR(50) | FK, NOT NULL | 使用者 ID，關聯到 users 表 |
| access_level | VARCHAR(20) | NOT NULL, DEFAULT 'viewer' | 專案內權限等級（見下方說明） |
| created_at | TIMESTAMP | DEFAULT NOW() | 加入專案時間 |
| created_by | VARCHAR(50) | FK | 邀請者/加入者的使用者 ID |

### 主鍵
- PRIMARY KEY (project_id, user_id)

### 權限等級說明
- **owner**：專案擁有者，可管理成員、刪除專案
- **editor**：編輯者，可新增/修改/刪除專案內資料
- **viewer**：檢視者，只能查看資料

### 索引
- idx_project_members_user_id (user_id)
- idx_project_members_project_id (project_id)

---

## 3. user_tokens 表（Token 管理）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | VARCHAR(100) | PRIMARY KEY | Token 唯一識別碼 |
| user_id | VARCHAR(50) | FK, NOT NULL | 使用者 ID |
| token_type | VARCHAR(20) | NOT NULL | Token 類型：refresh（更新用） |
| token_hash | VARCHAR(255) | NOT NULL | Token 雜湊值，不存明文 |
| expires_at | TIMESTAMP | NOT NULL | Token 過期時間 |
| created_at | TIMESTAMP | DEFAULT NOW() | Token 建立時間 |
| used_at | TIMESTAMP | | Token 使用時間（用於追蹤） |

### 索引
- idx_user_tokens_user_id (user_id)
- idx_user_tokens_token_hash (token_hash)

---

## 4. activity_logs 表（操作日誌）

| 欄位名稱 | 資料類型 | 限制條件 | 說明 |
|---------|---------|---------|------|
| id | BIGSERIAL | PRIMARY KEY | 自動遞增的日誌 ID |
| user_id | VARCHAR(50) | FK | 執行操作的使用者 ID |
| action | VARCHAR(100) | NOT NULL | 執行的動作（如：login, create_project, update_person） |
| resource_type | VARCHAR(50) | | 資源類型（如：project, person, file） |
| resource_id | VARCHAR(100) | | 資源 ID |
| details | TEXT | | 操作詳細資訊 |
| ip_address | VARCHAR(45) | | 操作者 IP 位址 |
| created_at | TIMESTAMP | DEFAULT NOW() | 操作時間 |

### 索引
- idx_activity_logs_user_id (user_id)
- idx_activity_logs_created_at (created_at)

---

## 權限架構說明

### 1. 系統層級權限
- **admin**：
  - 可以管理所有使用者
  - 可以查看所有專案
  - 可以執行系統管理功能
  - 可以查看系統日誌

- **user**：
  - 只能管理自己的資料
  - 只能查看自己參與的專案
  - 需要被邀請才能加入專案

### 2. 專案層級權限（繼承關係）
```
owner > editor > viewer

owner 擁有 editor 和 viewer 的所有權限
editor 擁有 viewer 的所有權限
```

### 3. 權限檢查邏輯
1. 先檢查系統角色（admin 有最高權限）
2. 再檢查專案成員權限
3. 根據操作類型判斷是否允許

---

## 資料庫 Views

### v_user_project_access（使用者專案權限檢視）
結合 users、project_members 和 projects 表，方便查詢使用者的專案權限：
- user_id, username, email, full_name
- system_role（系統角色）
- project_id, project_name
- access_level（專案權限）
- joined_at（加入時間）

---

## 資料庫 Functions

### check_project_access()
檢查使用者對特定專案的存取權限：
```sql
check_project_access(
    p_user_id VARCHAR(50),
    p_project_id VARCHAR(25),
    p_required_level VARCHAR(20)
) RETURNS BOOLEAN
```

邏輯：
1. admin 角色直接返回 true
2. 檢查 project_members 表中的權限
3. 比較使用者權限是否滿足要求

---

## 使用範例

### 1. 建立新使用者
```sql
INSERT INTO users (username, email, password_hash, full_name, role)
VALUES ('john_doe', 'john@example.com', '$2b$10$...', '張三', 'user');
```

### 2. 指派專案權限
```sql
INSERT INTO project_members (project_id, user_id, access_level, created_by)
VALUES ('proj_123', 'user_456', 'editor', 'admin_user_id');
```

### 3. 記錄操作日誌
```sql
INSERT INTO activity_logs (user_id, action, resource_type, resource_id, ip_address)
VALUES ('user_456', 'update_person', 'person', 'person_789', '192.168.1.1');
```

### 4. 檢查權限
```sql
SELECT check_project_access('user_456', 'proj_123', 'editor');
-- 返回 true 或 false
```

---

## 安全性考量

1. **密碼安全**：
   - 使用 BCrypt 加密
   - 不存儲明文密碼

2. **Token 安全**：
   - Token 以雜湊形式存儲
   - 設定過期時間
   - 使用後可標記

3. **權限隔離**：
   - 專案資料完全隔離
   - 使用者只能存取授權的專案

4. **審計追蹤**：
   - 所有重要操作都記錄在 activity_logs
   - 包含操作者、時間、IP 等資訊