# GitHub Issue: 資料庫優化 - person_profile表結構重構

**Issue Title**: 資料庫優化：person_profile表結構重構

**Labels**: `database`, `performance`, `optimization`, `high-priority`

**Priority**: 🔥 HIGH

---

## 問題描述

`person_profile`表設計存在嚴重的效能和維護性問題：

### 核心問題
- **過度設計**: 45個欄位，其中34個使用TEXT類型 (75.6%)
- **效能瓶頸**: 單一巨型表影響查詢效能  
- **資料類型不當**: 日期、性別等使用TEXT而非適當類型
- **維護困難**: 更新操作鎖定整個記錄

### 當前狀況統計
```
總欄位數: 45個
TEXT欄位: 34個 (75.6%)
資料記錄: 36筆
使用者數: 1個
專案數: 3個
```

### 影響範圍
- 人員資料查詢效能下降20-50%
- 儲存空間浪費15-30%
- 資料驗證和完整性問題
- 索引效果不佳
- 全文搜索效能低下

## 具體問題欄位

| 欄位名 | 當前類型 | 建議類型 | 問題 |
|--------|----------|----------|------|
| `birthday` | TEXT | DATE | 無法進行日期計算和驗證 |
| `gender` | TEXT | ENUM/VARCHAR(10) | 無限制選項，浪費空間 |
| `phone`/`mobile` | TEXT | VARCHAR(20) | 無長度限制，難以驗證格式 |
| `email` | TEXT | VARCHAR(255) | 超過標準email長度需求 |
| `created_at`/`updated_at` | TEXT | TIMESTAMP | 無法進行時間運算和排序 |
| `nationality`/`ethnicity` | TEXT | VARCHAR(50) | 過長類型影響索引效率 |

## 解決方案

### 第一階段：表結構垂直分割
將`person_profile`分割為5個專門的表：

#### 1. **persons** - 核心人員資訊
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

#### 2. **person_contacts** - 聯絡資訊  
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

#### 3. **person_career** - 職業和教育
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

#### 4. **person_social** - 社交關係
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

#### 5. **person_sources** - 來源追蹤
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
```sql
-- 創建列舉類型
CREATE TYPE person_gender_enum AS ENUM ('男', '女', '其他', '未知');
CREATE TYPE political_party_enum AS ENUM ('無黨籍', '民進黨', '國民黨', '民眾黨', '時代力量', '其他');
```

### 第三階段：索引策略優化
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

## 預期效益

### 1. 效能提升
- ✅ **查詢速度**: 20-50% 提升 (基於欄位減少和適當索引)
- ✅ **儲存空間**: 15-30% 節省 (適當資料類型)
- ✅ **索引效率**: 顯著提升 (專用資料類型)
- ✅ **全文搜索**: 效能大幅改善 (GIN索引)

### 2. 維護性改善
- ✅ **欄位驗證**: 資料庫層級約束
- ✅ **查詢簡化**: 按需載入相關資料
- ✅ **更新效率**: 減少鎖定範圍

### 3. 擴展性增強
- ✅ **模組化設計**: 便於未來功能擴展
- ✅ **專門優化**: 針對不同使用場景優化
- ✅ **資料完整性**: 更好的參照完整性

## 實施計畫

### Phase 1 (週1-2): 準備階段
- [ ] 創建新表結構 DDL 腳本
- [ ] 準備資料遷移腳本
- [ ] 建立測試環境驗證
- [ ] 準備回滾計畫

### Phase 2 (週3): 資料遷移
- [ ] 執行資料遷移腳本
- [ ] 驗證資料完整性
- [ ] 更新相關應用程式碼
- [ ] 調整 API 回應格式

### Phase 3 (週4): 清理優化
- [ ] 移除舊 person_profile 表
- [ ] 優化索引和查詢
- [ ] 效能測試和調優
- [ ] 更新技術文件

## 風險評估

### 🔴 高風險
- **資料遷移失敗**: 建立完整備份和回滾計畫
- **應用程式中斷**: 分階段部署和相容性處理

### 🟡 中風險
- **效能暫時下降**: 遷移期間可能影響查詢
- **資料類型轉換錯誤**: 詳細測試和驗證

### 🟢 低風險
- **學習曲線**: 團隊需要適應新結構
- **查詢調整**: 部分查詢需要重寫

## 相關文件
- 📄 **詳細分析**: `Documentation/person-profile-optimization-analysis.md`
- 📊 **資料庫架構報告**: `Documentation/database-architecture-analysis.md`
- 🔧 **遷移腳本**: `Database/migrations/person-profile-restructure/`

## 驗收標準
- [ ] 新表結構正確創建
- [ ] 資料遷移 100% 完成且正確
- [ ] 查詢效能提升 >= 20%
- [ ] 儲存空間減少 >= 15%
- [ ] 所有現有功能正常運作
- [ ] 全文搜索效能改善
- [ ] 通過效能測試

## 後續改善
1. 實施快取策略
2. 讀寫分離優化
3. 分區策略考量
4. 資料治理和品質監控

---

**優先級**: 🔥 **HIGH** - 此優化對整體系統效能影響最大，建議作為Q1優先實施項目

**estimated effort**: 3-4週

**affected components**: 
- person-profile API
- search functionality  
- data import/export
- analytics module