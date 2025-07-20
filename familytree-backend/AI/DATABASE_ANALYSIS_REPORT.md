# FamilyTree 資料庫結構說明

## 📊 系統概覽

**系統名稱**: FamilyTree AI 關聯分析系統  
**資料庫**: PostgreSQL  
**版本**: v1.0  

---

## 🗂️ 資料表結構

### 1. person_profile (人員基本資料表)

#### 用途
儲存所有人員的基本資訊和關係資料

#### 表結構
```sql
CREATE TABLE person_profile (
    id SERIAL PRIMARY KEY,                    -- 人員唯一識別碼
    name VARCHAR(255) NOT NULL,               -- 姓名
    gender VARCHAR(10),                       -- 性別 (男/女)
    birthday DATE,                            -- 生日
    nationality VARCHAR(100),                 -- 國籍
    mobile VARCHAR(20),                       -- 行動電話
    phone VARCHAR(20),                        -- 市話
    id_number VARCHAR(20),                    -- 身分證號
    passport_number VARCHAR(20),              -- 護照號碼
    family_relationships TEXT,                -- 家庭關係描述
    friends TEXT,                             -- 朋友關係描述
    extra_data JSONB,                         -- 額外資料 (JSON格式)
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### 設計特點
- **JSONB 欄位**: `extra_data` 用於儲存結構化的額外資訊
- **文字欄位**: `family_relationships` 和 `friends` 用於儲存自然語言描述的關係
- **時間戳記**: 完整的資料建立和更新時間追蹤

### 2. analysis_results (傳統分析結果表)

#### 用途
儲存 AI 分析人員關係的結果和進度

#### 表結構
```sql
CREATE TABLE analysis_results (
    id SERIAL PRIMARY KEY,                    -- 分析記錄唯一識別碼
    person_id INTEGER NOT NULL,               -- 被分析的人員ID
    analysis_result JSONB NOT NULL,           -- 分析結果 (JSON格式)
    analysis_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    progress_percentage INTEGER DEFAULT 0,    -- 分析進度百分比
    status VARCHAR(50) DEFAULT 'pending',     -- 狀態 (pending/processing/completed/failed)
    current_step VARCHAR(255),                -- 當前執行步驟
    status_message TEXT,                      -- 狀態詳細訊息
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### 索引設計
```sql
CREATE INDEX idx_analysis_results_person_id ON analysis_results(person_id);
CREATE INDEX idx_analysis_results_status ON analysis_results(status);
CREATE UNIQUE INDEX idx_analysis_results_person_unique 
ON analysis_results(person_id) WHERE status IN ('pending', 'processing');
```

#### 設計特點
- **狀態管理**: 完整的分析狀態追蹤
- **進度追蹤**: 百分比進度顯示
- **唯一約束**: 避免同一人員重複分析
- **JSONB 結果**: 靈活的結果儲存格式

### 3. analysis_sessions (遞迴分析會話表)

#### 用途
管理多層級遞迴關係分析的會話

#### 表結構
```sql
CREATE TABLE analysis_sessions (
    id SERIAL PRIMARY KEY,                    -- 會話唯一識別碼
    root_person_id INTEGER NOT NULL,          -- 根節點人員ID
    max_depth INTEGER DEFAULT 10,             -- 最大分析深度
    status VARCHAR(50) DEFAULT 'processing',  -- 會話狀態
    total_relationships INTEGER DEFAULT 0,    -- 發現的關係總數
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP                    -- 完成時間
);
```

#### 設計特點
- **深度控制**: 可設定最大分析深度
- **關係統計**: 記錄發現的關係數量
- **會話管理**: 支援長時間運行的分析任務

### 4. relationship_layers (關係層級表)

#### 用途
儲存遞迴分析發現的層級關係資料

#### 表結構
```sql
CREATE TABLE relationship_layers (
    id SERIAL PRIMARY KEY,                    -- 關係記錄唯一識別碼
    analysis_session_id INTEGER NOT NULL,     -- 所屬分析會話ID
    source_person_id INTEGER NOT NULL,        -- 來源人員ID
    target_person_id INTEGER NOT NULL,        -- 目標人員ID
    relation_type VARCHAR(100),               -- 關係類型 (父/母/子/女/朋友等)
    source_field VARCHAR(50),                 -- 關係來源欄位 (family_relationships/friends)
    layer_depth INTEGER DEFAULT 1,            -- 關係層級深度
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### 設計特點
- **層級追蹤**: 記錄關係的層級深度
- **來源標記**: 區分關係來源 (家庭/朋友)
- **會話關聯**: 與分析會話關聯

---

## 🔗 資料表關聯

```
person_profile (人員資料)
    ↓ (1:N)
analysis_results (分析結果)
    ↓ (1:1)
analysis_sessions (分析會話)
    ↓ (1:N)
relationship_layers (關係層級)
```

### 關聯說明
1. **person_profile** 是核心資料表，儲存所有人員資訊
2. **analysis_results** 記錄每個人員的分析結果
3. **analysis_sessions** 管理遞迴分析會話
4. **relationship_layers** 儲存具體的關係資料

---

## 📊 資料統計

### 當前資料量
- **人員資料**: 24 筆
- **分析會話**: 2 筆
- **關係記錄**: 5 筆 (黃心田分析結果)

### 資料完整性
- **基本資料**: 100% 有姓名
- **性別資料**: 87.5% 有性別資訊
- **朋友關係**: 66.7% 有朋友資料
- **額外資料**: 87.5% 有 profileData

---

## 🎯 設計理念

### 1. 靈活性
- 使用 JSONB 欄位儲存複雜資料結構
- 支援自然語言描述的關係資料
- 可擴展的欄位設計

### 2. 效能優化
- 關鍵欄位建立索引
- 避免資料冗餘的正規化設計
- 支援大量資料的查詢優化

### 3. 可追蹤性
- 完整的時間戳記
- 分析狀態和進度追蹤
- 資料建立和更新記錄

### 4. 擴展性
- 支援多種關係類型
- 可設定分析深度
- 模組化的表結構設計

---

**文件版本**: v2.0  
**更新時間**: 2025年1月27日 