# Person Profile 表設計優化分析

## 問題識別

### 當前狀況
- **表名**: `person_profile`
- **總欄位數**: 45個
- **TEXT欄位數**: 34個 (75.6%)
- **資料記錄數**: 36筆
- **影響範圍**: 人員資料管理核心功能

### 主要問題

#### 1. 過度使用TEXT資料類型
- **問題**: 34個欄位全部使用TEXT，包括應該使用專用類型的欄位
- **影響**: 
  - 查詢效能降低
  - 儲存空間浪費
  - 資料驗證困難
  - 索引效果不佳

#### 2. 單一巨型表設計
- **問題**: 所有人員相關資料塞在一個表中
- **影響**:
  - 查詢時載入不必要的欄位
  - 更新操作鎖定整個記錄
  - 難以針對不同用途進行優化

#### 3. 資料類型不當使用
具體問題欄位：

| 欄位名 | 當前類型 | 建議類型 | 理由 |
|--------|----------|----------|------|
| `birthday` | TEXT | DATE | 日期計算和驗證 |
| `gender` | TEXT | ENUM/VARCHAR(10) | 有限選項，節省空間 |
| `phone`/`mobile` | TEXT | VARCHAR(20) | 長度限制，國際格式 |
| `email` | TEXT | VARCHAR(255) | 標準email長度 |
| `created_at`/`updated_at` | TEXT | TIMESTAMP | 時間運算和排序 |
| `nationality`/`ethnicity` | TEXT | VARCHAR(50) | 有限長度，便於索引 |

## 解決方案

### 第一階段：垂直分割表結構

將`person_profile`分割為多個專門的表：

#### 1. 核心人員表 (`persons`)
```sql
CREATE TABLE persons (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    gender person_gender_enum,
    birthday DATE,
    birthplace VARCHAR(100),
    nationality VARCHAR(50),
    ethnicity VARCHAR(50),
    id_number VARCHAR(50),
    passport_number VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    user_id VARCHAR(50) REFERENCES users(id),
    project_id VARCHAR(25) REFERENCES projects(id)
);
```

#### 2. 聯絡資訊表 (`person_contacts`)
```sql
CREATE TABLE person_contacts (
    id SERIAL PRIMARY KEY,
    person_id INTEGER REFERENCES persons(id) ON DELETE CASCADE,
    phone VARCHAR(20),
    mobile VARCHAR(20),
    email VARCHAR(255),
    address TEXT,
    mailing_address TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### 3. 職業和教育表 (`person_career`)
```sql
CREATE TABLE person_career (
    id SERIAL PRIMARY KEY,
    person_id INTEGER REFERENCES persons(id) ON DELETE CASCADE,
    current_employer VARCHAR(200),
    experience TEXT,
    education TEXT,
    publications TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### 4. 社交關係表 (`person_social`)
```sql
CREATE TABLE person_social (
    id SERIAL PRIMARY KEY,
    person_id INTEGER REFERENCES persons(id) ON DELETE CASCADE,
    family_relationships JSONB,
    friends TEXT,
    important_friends TEXT,
    online_accounts JSONB,
    activities TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

#### 5. 來源和追蹤表 (`person_sources`)
```sql
CREATE TABLE person_sources (
    id SERIAL PRIMARY KEY,
    person_id INTEGER REFERENCES persons(id) ON DELETE CASCADE,
    discovery_source VARCHAR(100),
    discovery_process TEXT,
    source_id INTEGER,
    source_table VARCHAR(50),
    source_file_id INTEGER,
    source_file_name VARCHAR(200),
    file_md5 VARCHAR(32),
    source_created_at TIMESTAMP,
    source_updated_at TIMESTAMP
);
```

### 第二階段：資料類型優化

#### 1. 創建列舉類型
```sql
-- 性別列舉
CREATE TYPE person_gender_enum AS ENUM ('男', '女', '其他', '未知');

-- 政治傾向列舉
CREATE TYPE political_party_enum AS ENUM ('無黨籍', '民進黨', '國民黨', '民眾黨', '時代力量', '其他');
```

#### 2. 索引策略優化
```sql
-- 核心搜尋索引
CREATE INDEX idx_persons_name_gin ON persons USING gin(to_tsvector('chinese', name));
CREATE INDEX idx_persons_id_number ON persons(id_number);
CREATE INDEX idx_persons_birthday ON persons(birthday);
CREATE INDEX idx_persons_user_project ON persons(user_id, project_id);

-- 聯絡資訊索引
CREATE INDEX idx_person_contacts_phone ON person_contacts(phone);
CREATE INDEX idx_person_contacts_mobile ON person_contacts(mobile);
CREATE INDEX idx_person_contacts_email ON person_contacts(email);

-- JSONB索引
CREATE INDEX idx_person_social_family_gin ON person_social USING gin(family_relationships);
CREATE INDEX idx_person_social_accounts_gin ON person_social USING gin(online_accounts);
```

### 第三階段：資料遷移計畫

#### 1. 資料遷移腳本
```sql
-- 遷移到新的persons表
INSERT INTO persons (name, gender, birthday, birthplace, nationality, ethnicity, 
                    id_number, passport_number, user_id, project_id, created_at, updated_at)
SELECT 
    name,
    CASE 
        WHEN gender = '男' THEN '男'::person_gender_enum
        WHEN gender = '女' THEN '女'::person_gender_enum
        ELSE '未知'::person_gender_enum
    END,
    CASE 
        WHEN birthday ~ '^\d{4}-\d{2}-\d{2}$' THEN birthday::DATE
        ELSE NULL
    END,
    birthplace,
    nationality,
    ethnicity,
    id_number,
    passport_number,
    user_id,
    project_id,
    CASE 
        WHEN created_at ~ '^\d{4}-\d{2}-\d{2}' THEN created_at::TIMESTAMP
        ELSE CURRENT_TIMESTAMP
    END,
    CASE 
        WHEN updated_at ~ '^\d{4}-\d{2}-\d{2}' THEN updated_at::TIMESTAMP
        ELSE CURRENT_TIMESTAMP
    END
FROM person_profile;
```

## 預期效益

### 1. 效能提升
- **查詢速度**: 20-50% 提升 (基於欄位減少和適當索引)
- **儲存空間**: 15-30% 節省 (適當資料類型)
- **索引效率**: 顯著提升 (專用資料類型)

### 2. 維護性改善
- **欄位驗證**: 資料庫層級約束
- **查詢簡化**: 按需載入相關資料
- **更新效率**: 減少鎖定範圍

### 3. 擴展性增強
- **模組化設計**: 便於未來功能擴展
- **專門優化**: 針對不同使用場景優化
- **資料完整性**: 更好的參照完整性

## 實施時程

### Phase 1 (週1-2): 準備階段
- 創建新表結構
- 準備資料遷移腳本
- 建立測試環境驗證

### Phase 2 (週3): 資料遷移
- 執行資料遷移
- 驗證資料完整性
- 更新相關應用程式碼

### Phase 3 (週4): 清理和優化
- 移除舊表
- 優化索引和查詢
- 效能測試和調優

## 風險評估

### 高風險
- **資料遷移失敗**: 完整備份和回滾計畫
- **應用程式中斷**: 分階段部署和相容性處理

### 中風險
- **效能暫時下降**: 遷移期間可能影響查詢
- **資料類型轉換錯誤**: 詳細測試和驗證

### 低風險
- **學習曲線**: 團隊需要適應新結構
- **查詢調整**: 部分查詢需要重寫

## 建議

1. **優先實施**: 此優化對整體系統效能影響最大
2. **分階段進行**: 降低風險，確保穩定性
3. **充分測試**: 在測試環境完整驗證後再部署
4. **監控效能**: 實施後持續監控效能指標
5. **文件更新**: 及時更新相關技術文件

這個優化將為系統帶來顯著的效能提升和維護性改善，是當前最值得投資的資料庫優化項目。