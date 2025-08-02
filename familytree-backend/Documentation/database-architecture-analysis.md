# 家族樹管理系統資料庫架構分析報告

**分析日期：** 2025年8月2日  
**資料庫版本：** PostgreSQL 14+  
**分析範圍：** 完整系統資料庫架構  
**資料庫名稱：** familytree  

---

## 一、資料庫概覽

### 1.1 基本資訊
- **資料庫類型：** PostgreSQL
- **表格總數：** 29個資料表
- **主要資料量：**
  - 使用者：2筆記錄
  - 人員資料：36筆記錄
  - 專案：12筆記錄
- **主要功能模組：**
  - 使用者認證與權限管理
  - 人員資料管理
  - 專案管理
  - AI分析功能
  - 檔案上傳與管理
  - 關係圖分析

### 1.2 架構特點
- **混合式設計：** 同時支援專案導向(project_id)和使用者導向(user_id)的資料隔離
- **權限系統：** 實現角色基礎存取控制(RBAC)
- **審計追蹤：** 完整的操作日誌記錄
- **彈性欄位：** 使用TEXT和JSONB支援非結構化資料

---

## 二、資料表詳細分析

### 2.1 核心使用者管理系統

#### users（使用者基本資料）
```sql
主鍵：id (VARCHAR(50)) - UUID格式
唯一約束：username, email
檢查約束：role IN ('admin', 'user'), status IN ('active', 'inactive')
觸發器：自動更新updated_at
```

**功能分析：**
- 支援使用者名稱和郵件雙重登入方式
- BCrypt密碼雜湊保護
- 角色分級權限控制
- 帳號狀態管理

**索引策略：**
- 主鍵索引：id
- 唯一索引：username, email
- 查詢索引：status

#### user_tokens（JWT Token管理）
```sql
主鍵：id (VARCHAR(100))
外鍵：user_id → users(id) ON DELETE CASCADE
約束：token_type IN ('refresh')
```

**功能分析：**
- JWT Refresh Token安全儲存
- Token過期時間管理
- 串聯刪除保護

#### activity_logs（操作日誌）
```sql
主鍵：id (BIGSERIAL)
外鍵：user_id → users(id) ON DELETE SET NULL
```

**功能分析：**
- 完整操作審計追蹤
- 支援JSON格式詳細資訊
- IP地址記錄
- 軟刪除保護（SET NULL）

### 2.2 權限管理系統

#### roles（角色定義）
```sql
主鍵：id (VARCHAR(50))
層級：level (INTEGER) - 支援角色階層
系統標記：is_system (BOOLEAN)
```

#### permissions（權限定義）
```sql
複合主鍵：(resource, action)
分類管理：category分組
系統權限標記：is_system
```

#### role_permissions（角色權限關聯）
```sql
複合主鍵：(role_id, permission_id)
外鍵約束：雙重關聯確保完整性
```

#### user_permissions（使用者直接權限）
```sql
複合主鍵：(user_id, permission_id)
設計目的：特殊權限例外管理
```

### 2.3 核心業務資料

#### person_profile（人員資料）
```sql
主鍵：id (INTEGER) AUTO_INCREMENT
雙重關聯：project_id, user_id
資料完整性：44個詳細欄位
```

**設計分析：**
- **優點：** 欄位豐富，支援複雜人員資料
- **問題：** 大量TEXT欄位可能影響查詢效能
- **冗餘設計：** 同時保留project_id和user_id

**重要欄位分析：**
- 識別資訊：name, id_number, passport_number
- 聯絡資訊：phone, mobile, email
- 關係資訊：family_relationships, friends
- 追蹤資訊：source_id, file_md5

#### projects（專案管理）
```sql
主鍵：id (VARCHAR(25))
外鍵：user_id → users(id)
狀態約束：5種專案狀態
```

**功能分析：**
- 專案生命週期管理
- 建立者追蹤
- 狀態流程控制

### 2.4 AI分析功能

#### analysis_sessions（分析會話）
```sql
主鍵：id (VARCHAR(100))
控制參數：max_depth, status
效能追蹤：total_relationships
```

#### analysis_results（分析結果）
```sql
JSONB儲存：analysis_result
進度追蹤：progress_percentage, status
多狀態：pending/processing/completed/failed
```

#### missing_persons（缺失人員）
```sql
關聯追蹤：source_person_id, analysis_session_id
層級管理：layer_depth
解析狀態：resolved_person_id
```

#### relationship_layers（關係層級）
```sql
階層結構：layer_number, parent_person_id
關係類型：relation_type, relation_field
視覺化支援：visual_analysis_graph_id
```

### 2.5 輔助功能表

#### user_favorites（使用者收藏）
```sql
唯一約束：unique_person_favorite (person_id)
冗餘欄位：person_name（效能優化）
追蹤時間：last_viewed_time, favorited_at
```

#### field_mapping（欄位對應）
```sql
Excel整合：excel_field_name → db_field_name
使用者隔離：user_id
```

#### photos（照片管理）
```sql
檔案追蹤：file_path, file_size, mime_type
安全驗證：md5_hash
```

---

## 三、資料庫關聯分析

### 3.1 主要外鍵關係

```
users (1) ──→ (M) user_tokens [CASCADE DELETE]
users (1) ──→ (M) activity_logs [SET NULL]
users (1) ──→ (M) projects
users (1) ──→ (M) person_profile
users (1) ──→ (M) analysis_sessions
users (1) ──→ (M) user_favorites

projects (1) ──→ (M) person_profile [CASCADE DELETE]
projects (1) ──→ (M) photos
projects (1) ──→ (M) user_favorites [CASCADE DELETE]

person_profile (1) ──→ (M) relationship_layers [CASCADE DELETE]
person_profile (1) ──→ (M) mergedpersons

roles (1) ──→ (M) role_permissions
roles (1) ──→ (M) user_roles

permissions (1) ──→ (M) role_permissions
permissions (1) ──→ (M) user_permissions
```

### 3.2 關聯完整性評估

**強一致性關聯：**
- users → user_tokens (CASCADE DELETE) ✓
- projects → person_profile (CASCADE DELETE) ✓
- person_profile → relationship_layers (CASCADE DELETE) ✓

**軟刪除保護：**
- users → activity_logs (SET NULL) ✓

**潛在問題：**
- person_profile同時關聯projects和users，可能產生資料不一致
- 部分關聯缺乏CASCADE設定，可能產生孤立記錄

---

## 四、正規化程度分析

### 4.1 正規化優點

**符合第一正規化（1NF）：**
- 所有欄位均為原子值
- 每個欄位僅包含單一資料類型

**符合第二正規化（2NF）：**
- 消除部分函數依賴
- 非鍵欄位完全依賴主鍵

**符合第三正規化（3NF）：**
- 消除傳遞依賴
- 權限系統正確分離角色和權限

### 4.2 正規化問題

**冗餘設計識別：**

1. **user_favorites.person_name**
   - 問題：重複存儲person_profile.name
   - 風險：資料不同步
   - 目的：查詢效能優化

2. **雙重資料隔離**
   - 問題：person_profile同時包含project_id和user_id
   - 風險：資料歸屬不明確
   - 建議：選擇單一隔離策略

3. **TEXT欄位過多**
   - 問題：person_profile包含44個TEXT欄位
   - 風險：查詢效能低下
   - 建議：考慮垂直分割

### 4.3 反正規化策略

**合理的反正規化：**
- user_favorites.person_name（查詢頻繁）
- analysis_sessions.total_relationships（統計快取）

**需要檢討的反正規化：**
- person_profile的massive字段設計
- 重複的地址和聯絡資訊欄位

---

## 五、索引設計評估

### 5.1 現有索引分析

#### 優秀的索引設計：

**users表：**
```sql
✓ idx_users_email (查詢頻繁)
✓ idx_users_username (登入使用)
✓ idx_users_status (管理員查詢)
```

**person_profile表：**
```sql
✓ idx_person_name_search (基本查詢)
✓ idx_person_mobile_search (聯絡資訊查詢)
✓ idx_person_id_number_search (身分驗證)
✓ idx_person_fulltext_search (複合查詢)
✓ idx_person_profile_user_id (資料隔離)
```

**user_favorites表：**
```sql
✓ idx_favorited_at (時間排序)
✓ idx_user_favorites_user_id (使用者查詢)
✓ unique_person_favorite (防重複)
```

#### 索引效能問題：

1. **缺少複合索引：**
   - person_profile缺少(user_id, name)複合索引
   - analysis_results缺少(user_id, status)索引

2. **過度索引：**
   - person_profile的單欄位索引過多
   - 考慮整合為複合索引

### 5.2 索引優化建議

**建議新增索引：**
```sql
-- 常用查詢組合
CREATE INDEX idx_person_profile_user_name ON person_profile(user_id, name);
CREATE INDEX idx_analysis_results_user_status ON analysis_results(user_id, status);
CREATE INDEX idx_activity_logs_user_action ON activity_logs(user_id, action);

-- 時間範圍查詢
CREATE INDEX idx_person_profile_created_at ON person_profile(created_at DESC);
CREATE INDEX idx_projects_user_status ON projects(user_id, status);
```

**建議移除索引：**
```sql
-- 低使用率索引
DROP INDEX IF EXISTS idx_person_profile_project_id; -- 如果不再使用project隔離
```

---

## 六、效能瓶頸識別

### 6.1 查詢效能問題

**問題1：person_profile表設計**
- **問題：** 44個TEXT欄位導致每列佔用空間大
- **影響：** 全表掃描效能低下
- **建議：** 垂直分割為基本資料和詳細資料表

**問題2：全文搜索**
- **問題：** TEXT欄位的LIKE查詢效能差
- **影響：** 搜索回應時間長
- **建議：** 實施PostgreSQL全文搜索(FTS)

**問題3：關係分析查詢**
- **問題：** 多層關係遞歸查詢複雜度高
- **影響：** AI分析功能回應慢
- **建議：** 實施查詢結果快取

### 6.2 資料庫連接效能

**連接池設置：**
```json
{
  "MaxPoolSize": 100,
  "MinPoolSize": 5,
  "ConnectionTimeout": 30
}
```

**評估：** 連接池配置合理，支援高並發存取

### 6.3 儲存效能

**JSONB使用評估：**
- analysis_results.analysis_result：適當使用 ✓
- person_profile.extra_data：適當使用 ✓

**大型TEXT欄位：**
- 建議採用TOAST壓縮
- 考慮外部檔案儲存

---

## 七、安全性分析

### 7.1 認證安全

**密碼保護：** ✓ BCrypt雜湊
**Token管理：** ✓ JWT + Refresh Token
**會話安全：** ✓ Token過期機制

### 7.2 授權安全

**角色基礎存取控制（RBAC）：** ✓ 完整實現
**資料隔離：** ✓ user_id和project_id雙重保護
**權限細粒度：** ✓ 資源級權限控制

### 7.3 資料保護

**審計追蹤：** ✓ activity_logs完整記錄
**軟刪除：** ⚠️ 部分表格未實現
**備份策略：** ⚠️ 需要確認定期備份計畫

### 7.4 安全建議

1. **實施資料加密**
   - 敏感欄位（id_number, passport_number）加密儲存
   - 資料庫連接SSL加密

2. **強化存取控制**
   - 資料庫使用者權限最小化
   - 實施資料列級安全(RLS)

3. **審計增強**
   - 紀錄敏感資料存取
   - 實施異常存取偵測

---

## 八、設計問題與冗餘識別

### 8.1 架構設計問題

#### 問題1：資料隔離策略不一致
**現狀：**
- person_profile同時包含project_id和user_id
- 部分表格只有user_id，部分只有project_id

**風險：**
- 資料歸屬不明確
- 查詢邏輯複雜
- 權限檢查困難

**建議：**
- 統一採用user_id隔離策略
- 淘汰project_id欄位
- 實施資料遷移計畫

#### 問題2：person_profile表過度設計
**現狀：**
- 44個欄位集中在單一表格
- 大量TEXT欄位影響效能

**建議重構：**
```sql
-- 基本資料表
CREATE TABLE person_basic (
    id INTEGER PRIMARY KEY,
    user_id VARCHAR(50) NOT NULL,
    name TEXT NOT NULL,
    gender TEXT,
    birthday TEXT,
    -- 基本識別資訊
);

-- 聯絡資訊表
CREATE TABLE person_contact (
    person_id INTEGER PRIMARY KEY,
    phone TEXT,
    mobile TEXT,
    email TEXT,
    -- 聯絡方式
);

-- 詳細資料表
CREATE TABLE person_details (
    person_id INTEGER PRIMARY KEY,
    family_relationships TEXT,
    experience TEXT,
    education TEXT,
    -- 詳細描述資訊
);
```

### 8.2 資料冗餘問題

#### 冗餘1：user_favorites.person_name
**評估：** 可接受的效能優化冗餘
**維護：** 需要觸發器保持同步

#### 冗餘2：地址欄位重複
**問題：**
- address vs current_address vs mailing_address
- birthplace vs ancestral_origin

**建議：**
- 標準化地址欄位命名
- 考慮地址表獨立設計

### 8.3 資料類型問題

#### TEXT欄位濫用
**問題：**
- birthday, phone使用TEXT而非DATE, VARCHAR
- 失去資料類型驗證和索引優化

**建議改善：**
```sql
ALTER TABLE person_profile 
ALTER COLUMN birthday TYPE DATE USING birthday::DATE;

ALTER TABLE person_profile 
ALTER COLUMN phone TYPE VARCHAR(20);
```

---

## 九、優化建議

### 9.1 短期優化（1-2週）

#### 索引優化
```sql
-- 新增高頻查詢索引
CREATE INDEX CONCURRENTLY idx_person_user_name 
ON person_profile(user_id, name);

CREATE INDEX CONCURRENTLY idx_analysis_user_status 
ON analysis_results(user_id, status);

-- 全文搜索索引
CREATE INDEX CONCURRENTLY idx_person_fulltext_gin 
ON person_profile 
USING gin(to_tsvector('simple', name || ' ' || COALESCE(mobile,'') || ' ' || COALESCE(email,'')));
```

#### 查詢優化
```sql
-- 新增查詢函數
CREATE OR REPLACE FUNCTION get_user_persons(user_id_param VARCHAR(50))
RETURNS TABLE(id INTEGER, name TEXT, mobile TEXT) AS $$
BEGIN
    RETURN QUERY
    SELECT p.id, p.name, p.mobile
    FROM person_profile p
    WHERE p.user_id = user_id_param
    ORDER BY p.name;
END;
$$ LANGUAGE plpgsql;
```

### 9.2 中期優化（1-2個月）

#### 表格重構
1. **person_profile垂直分割**
   - 分離基本資料和詳細資料
   - 保持向後相容性

2. **統一資料隔離策略**
   - 移除project_id依賴
   - 全面採用user_id隔離

3. **資料類型標準化**
   - 日期欄位改為DATE類型
   - 電話號碼標準化格式

#### 效能監控
```sql
-- 慢查詢監控
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- 索引使用率監控
SELECT schemaname, tablename, indexname, idx_tup_read, idx_tup_fetch
FROM pg_stat_user_indexes
ORDER BY idx_tup_read DESC;
```

### 9.3 長期優化（3-6個月）

#### 架構升級
1. **實施讀寫分離**
   - 主庫負責寫入操作
   - 從庫負責查詢和分析

2. **快取層實施**
   - Redis快取熱門查詢
   - 分析結果快取

3. **分片策略**
   - 按使用者分片person_profile
   - 地理分佈式部署

#### 資料治理
1. **資料品質監控**
   - 重複資料檢測
   - 資料完整性檢查

2. **自動備份和復原**
   - 定期全量備份
   - 增量備份策略
   - 災難復原計畫

---

## 十、總結與建議

### 10.1 架構優勢

1. **完整的權限系統**：RBAC實現良好，支援細粒度權限控制
2. **靈活的資料模型**：支援複雜人員資料和關係分析
3. **審計追蹤完整**：操作日誌完善，滿足合規要求
4. **索引策略合理**：核心查詢路徑已有適當索引

### 10.2 主要問題

1. **資料隔離不一致**：project_id和user_id雙重隔離造成複雜性
2. **person_profile過度設計**：44個欄位影響查詢效能
3. **資料類型使用不當**：過度使用TEXT類型
4. **缺少垂直分割**：大表查詢效能問題

### 10.3 優先改善項目

#### 高優先級（立即執行）
1. 新增關鍵複合索引
2. 實施全文搜索優化
3. 統一資料隔離策略

#### 中優先級（1-2個月）
1. person_profile表重構
2. 資料類型標準化
3. 查詢效能監控

#### 低優先級（長期規劃）
1. 讀寫分離架構
2. 分片和分散式部署
3. 進階資料治理

### 10.4 架構評分

| 評估項目 | 得分 | 滿分 | 評語 |
|---------|------|------|------|
| 資料正規化 | 7 | 10 | 基本符合但有冗餘問題 |
| 索引設計 | 8 | 10 | 核心索引完善，需要微調 |
| 關聯完整性 | 8 | 10 | 外鍵約束良好 |
| 安全性 | 9 | 10 | 認證授權機制完整 |
| 效能設計 | 6 | 10 | 存在查詢瓶頸 |
| 可維護性 | 7 | 10 | 架構清晰但複雜度高 |
| **總體評分** | **7.5** | **10** | **良好，需要優化** |

這個資料庫架構在安全性和功能完整性方面表現優秀，但在效能優化和設計簡化方面還有改善空間。建議按照優先級逐步實施優化措施，特別關注查詢效能和資料一致性問題。

---

**報告完成時間：** 2025年8月2日  
**下次檢討建議：** 3個月後進行效能評估  
**負責架構師：** Database Architecture AI Agent