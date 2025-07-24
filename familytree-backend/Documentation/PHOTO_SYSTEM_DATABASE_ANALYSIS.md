# 照片上傳系統資料庫架構分析
# 文件目的：記錄照片相關資料表的完整結構與現狀，作為系統開發參考
# 主要功能：提供資料表架構、索引設計、約束條件和現有資料概況

## 📊 資料表概覽

### 1. photos (主要照片資料表)
**用途**：儲存照片檔案的元數據資訊

#### 表格結構
```sql
CREATE TABLE photos (
    id                INTEGER PRIMARY KEY,               -- 自動遞增主鍵
    original_filename VARCHAR(255) NOT NULL,             -- 原始檔名 (如: 0004.PNG)
    saved_filename    VARCHAR(255) NOT NULL,             -- 正規化檔名 (如: 000004.PNG)
    file_path         TEXT NOT NULL,                     -- 完整檔案路徑
    file_size         BIGINT NOT NULL,                   -- 檔案大小 (bytes)
    md5_hash          VARCHAR(32) NOT NULL,              -- MD5雜湊值 (重複檢測)
    project_id        VARCHAR(50) NOT NULL,              -- 專案ID (專案分離)
    upload_time       TIMESTAMP WITH TIME ZONE DEFAULT NOW(), -- 上傳時間
    created_at        TIMESTAMP WITH TIME ZONE DEFAULT NOW(), -- 建立時間
    updated_at        TIMESTAMP WITH TIME ZONE DEFAULT NOW()  -- 更新時間
);
```

#### 索引設計
- `photos_pkey`: PRIMARY KEY (id)
- `idx_photos_project_id`: 專案查詢優化
- `idx_photos_md5_hash`: MD5查詢優化  
- `idx_photos_project_md5`: 複合索引 (project_id, md5_hash)
- `idx_photos_upload_time`: 時間排序優化
- `uk_photos_project_md5`: 唯一約束 (project_id, md5_hash) - 防止重複上傳

#### 觸發器
- `trigger_photos_updated_at`: 自動更新 updated_at 欄位

### 2. photo_tags (照片標籤擴展表)
**用途**：儲存照片與其他實體的關聯標籤 (如人員照片關聯)

#### 表格結構
```sql
CREATE TABLE photo_tags (
    id         INTEGER PRIMARY KEY,                      -- 自動遞增主鍵
    photo_id   INTEGER NOT NULL,                         -- 照片ID (外鍵)
    tag_type   VARCHAR(50) NOT NULL,                     -- 標籤類型 (如: person_id, location)
    tag_value  VARCHAR(100) NOT NULL,                    -- 標籤值 (如: 人員ID、地點名稱)
    project_id VARCHAR(50) NOT NULL,                     -- 專案ID
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()    -- 建立時間
);
```

#### 索引設計
- `photo_tags_pkey`: PRIMARY KEY (id)
- `idx_photo_tags_photo_id`: 照片查詢優化
- `idx_photo_tags_project_id`: 專案查詢優化
- `idx_photo_tags_type_value`: 標籤查詢優化 (tag_type, tag_value)
- `uk_photo_tags_unique`: 唯一約束 (photo_id, tag_type, tag_value) - 防止重複標籤

#### 外鍵約束
- `photo_tags_photo_id_fkey`: FOREIGN KEY (photo_id) REFERENCES photos(id) ON DELETE CASCADE

## 📈 目前資料狀況

### photos 表格資料分析
- **總照片數量**: 8 張
- **活躍專案數**: 3 個專案
- **專案ID分布**:
  - `782093-20250723205634`: 1 張照片
  - `888888-20250723205343`: 1 張照片  
  - `893148-20250724022612`: 6 張照片

### 照片資料詳情
```
ID  原始檔名      正規化檔名    專案ID                  檔案大小   上傳時間
8   0004.PNG     000004.PNG   893148-20250724022612   24041     2025-07-24 03:05
7   0012.png     000012.png   893148-20250724022612   24070     2025-07-24 03:02
6   0011.jpg     000011.jpg   893148-20250724022612   67993     2025-07-24 02:54
5   0005.PNG     000005.PNG   893148-20250724022612   28409     2025-07-24 02:54
4   0003.PNG     000003.PNG   893148-20250724022612   28809     2025-07-24 02:51
3   0010.PNG     000010.PNG   893148-20250724022612   34124     2025-07-24 02:49
2   0004.PNG     000004.PNG   782093-20250723205634   24041     2025-07-24 02:34
1   0004.PNG     000004.PNG   888888-20250723205343   24041     2025-07-24 02:25
```

### photo_tags 表格資料分析
- **總標籤數量**: 0 (表格為空，功能尚未使用)

## 🗂️ 檔案儲存結構

### 實際檔案路徑範例
```
/Users/user/FamilyTree/familytree-backend/photos/
├── 782093-20250723205634/
│   └── 000004.PNG
├── 888888-20250723205343/
│   └── 000004.PNG
└── 893148-20250724022612/
    ├── 000003.PNG
    ├── 000004.PNG
    ├── 000005.PNG
    ├── 000010.PNG
    ├── 000011.jpg
    └── 000012.png
```

## 🔧 核心功能特性

### 1. 專案分離
- 透過 `project_id` 實現多專案照片隔離
- 檔案實際儲存在獨立目錄下
- 查詢和操作都按專案進行

### 2. 重複檢測機制
- 使用 MD5 雜湊值進行重複檢測
- 唯一約束：`(project_id, md5_hash)` 防止同專案內重複上傳
- 支援跨專案的相同檔案儲存

### 3. 檔名正規化
- 數字檔名自動補零至6位數 (如: 4.PNG → 000004.PNG)
- 處理重複檔名衝突 (檔名-1, 檔名-2...)
- 保留原始檔名紀錄

### 4. 擴展標籤系統
- photo_tags 表格支援照片與其他實體的關聯
- 支援多種標籤類型 (person_id, location 等)
- 防重複標籤機制

## 📊 效能最佳化

### 索引策略
1. **查詢最佳化**: 針對常用查詢建立複合索引
2. **唯一性約束**: 防止資料重複的同時提升查詢速度
3. **時間排序**: upload_time 索引支援時間順序查詢

### 資料完整性
1. **CASCADE 刪除**: 刪除照片時自動清理相關標籤
2. **觸發器**: 自動維護時間戳記欄位
3. **外鍵約束**: 確保資料關聯完整性

## 🚀 開發建議

### 即將開發的系統考量
1. **照片瀏覽功能**: 可按專案、時間、標籤進行查詢
2. **標籤系統**: 考慮啟用 photo_tags 表格實現照片分類
3. **批量操作**: 支援批量標籤、移動、刪除等操作
4. **搜尋優化**: 利用現有索引進行高效查詢
5. **快取策略**: 考慮對照片列表和標籤進行快取

### 資料表擴展建議
1. **照片縮圖**: 考慮新增 thumbnail_path 欄位
2. **照片描述**: 考慮新增 description 欄位
3. **拍攝資訊**: 考慮新增 EXIF 資料欄位 (拍攝時間、地點等)
4. **檔案版本**: 考慮新增版本控制機制

---

*最後更新時間: 2025-01-24*
*資料統計時間: 2025-01-24 03:05 (UTC+8)* 