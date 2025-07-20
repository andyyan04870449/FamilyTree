# 資料同步機制說明文檔

## 資料表設計

### 1. 主要資料表

#### person_data（原始資料表）
- 用途：存儲從外部來源（如檔案、API）導入的原始人員資料
- 特點：
  - 有 `file_md5` 欄位追踪資料來源
  - `discovery_process` 欄位記錄資料發現/收集過程
  - 欄位類型較為嚴格（如 `character varying` 有長度限制）
- 主要用於：
  - 資料導入
  - 全文搜索
  - 資料來源追踪

#### person_profile（整合資料表）
- 用途：系統主要人員資料表，用於關係圖譜分析和系統功能
- 特點：
  - 被其他功能表引用（分析會話、失蹤人口、關係層級）
  - 有 `extra_data` JSONB 欄位，更靈活
  - 欄位類型較為寬鬆（多用 `text`）
  - 有更多的搜索索引
- 主要用於：
  - 關係圖譜分析
  - 人員資料管理
  - 系統功能整合

### 2. 同步相關資料表

#### sync_log（同步日誌表）
- 記錄所有同步操作
- 包含：來源ID、來源表、狀態、同步時間

#### sync_error_log（同步錯誤日誌表）
- 記錄同步過程中的錯誤
- 包含：來源ID、來源表、錯誤訊息、錯誤時間

#### sync_status（同步狀態表）
- 記錄最後同步時間和狀態
- 用於追踪同步進度

## 同步機制

### 1. 同步觸發
- 自動同步：每30分鐘執行一次
- 條件：
  - 資料更新時間晚於最後同步時間
  - 或資料尚未同步到 person_profile

### 2. 同步流程
1. 檢查最後同步時間
2. 獲取需要同步的資料
3. 對每筆資料：
   - 檢查是否存在對應記錄
   - 存在則更新，不存在則創建
   - 記錄同步日誌
4. 更新同步時間

### 3. 錯誤處理
- 記錄錯誤到 sync_error_log
- 單筆資料同步失敗不影響其他資料
- 使用事務確保資料一致性

## 欄位對應關係

| person_data         | person_profile      | 說明               |
|--------------------|---------------------|-------------------|
| id                 | source_id           | 來源ID            |
| 'person_data'      | source_table        | 來源表名           |
| created_at         | source_created_at   | 來源創建時間        |
| updated_at         | source_updated_at   | 來源更新時間        |
| current_workplace  | current_employer    | 工作單位           |
| important_friends  | friends            | 重要朋友           |
| frequent_places    | frequent_locations  | 常去地點           |
| travel_records     | travel_history     | 旅行記錄           |
| ancestral_home     | ancestral_origin    | 祖籍              |
| notes              | remarks            | 備註              |

## 使用建議

### 1. 資料維護
- 原始資料修改應在 person_data 表進行
- 系統功能應使用 person_profile 表
- 避免直接修改 person_profile 表的資料

### 2. 搜索功能
- 全文搜索應使用 person_data 表
- 關係圖譜相關功能使用 person_profile 表
- 詳情頁面應顯示兩個表的組合資料

### 3. 監控和維護
- 定期檢查 sync_error_log
- 監控同步狀態和效能
- 必要時手動觸發同步

## 開發指南

### 1. 添加新的資料來源
1. 創建新的資料表
2. 在 person_profile 中添加對應欄位
3. 實現資料轉換邏輯
4. 更新同步服務

### 2. 修改同步邏輯
1. 修改 DataSyncService
2. 更新欄位對應關係
3. 測試同步功能
4. 更新文檔

### 3. 故障排除
1. 檢查 sync_error_log
2. 驗證資料一致性
3. 必要時手動修復資料
4. 更新同步時間 