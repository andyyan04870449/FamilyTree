# 審計日誌使用者資訊自動補全機制

## 概述

此增強功能解決了審計日誌中使用者名稱和角色顯示 N/A 的問題，通過以下機制提供完整的使用者資訊：

1. **自動查詢補全**：在查詢審計日誌時，自動 LEFT JOIN users 表獲取使用者資訊
2. **記錄時補全**：在記錄審計日誌時，自動補全缺失的使用者資訊
3. **系統使用者友好顯示**：為系統使用者提供中文友好名稱
4. **效能優化**：添加適當的索引確保查詢效能
5. **向後相容性**：不影響現有的審計日誌記錄

## 架構設計

### 資料優先級策略

**使用者名稱優先級：**
1. `audit_logs.user_name`（如果不為空）
2. `users.full_name`
3. `users.username`
4. 系統使用者友好名稱
5. `user_id` 作為 fallback

**使用者角色優先級：**
1. `audit_logs.user_role`（如果不為空）
2. `users.role`
3. 系統使用者預設角色
4. 'unknown' 作為 fallback

### 系統使用者映射

| User ID | 友好名稱 | 角色 |
|---------|----------|------|
| admin_default | 系統管理員 | admin |
| system | 系統 | system |
| anonymous | Anonymous | anonymous |

## 實作的檔案修改

### 1. AuditLogService.cs
- 新增 `EnrichUserInfoAsync()` 方法
- 新增 `GetSystemUserInfo()` 方法  
- 修改所有查詢方法使用 LEFT JOIN
- 修改所有記錄方法自動補全使用者資訊

### 2. AuditLogModels.cs
- 新增 `EnrichedUserInfo` 模型

### 3. SqlQueries.cs
- 新增 `AuditLogs` 查詢類別
- 包含使用者資訊補全的標準化 SQL 查詢

## 資料庫腳本

### 1. 07_audit_logs_user_info_optimization.sql
**功能：**
- 建立效能優化索引
- 建立查詢檢視
- 建立效能監控函數

**主要索引：**
```sql
-- 使用者活動統計優化
idx_audit_logs_user_occurred_time ON audit_logs(user_id, occurred_at DESC)

-- 分頁查詢優化  
idx_audit_logs_paginated_query ON audit_logs(occurred_at DESC, event_type, user_id, success)

-- 函數式索引（COALESCE 查詢優化）
idx_audit_logs_effective_user_name ON audit_logs(COALESCE(...))
```

**建立的檢視：**
- `audit_logs_with_user_info`：包含完整使用者資訊的審計日誌檢視
- `user_activity_summary`：使用者活動摘要檢視

### 2. 08_backfill_audit_logs_user_info.sql
**功能：**
- 批量更新現有審計日誌的使用者資訊
- 建立備份表以支援回滾
- 提供資料完整性檢查
- 建立一致性檢查函數

## 使用方式

### 部署步驟

1. **執行優化腳本**
   ```bash
   psql -d familytree -f 07_audit_logs_user_info_optimization.sql
   ```

2. **執行回填腳本**（可選，用於更新現有資料）
   ```bash
   psql -d familytree -f 08_backfill_audit_logs_user_info.sql
   ```

3. **重新編譯並部署應用程式**

### 驗證方式

1. **檢查索引建立**
   ```sql
   SELECT indexname, tablename FROM pg_indexes 
   WHERE tablename IN ('audit_logs', 'users') 
   ORDER BY tablename, indexname;
   ```

2. **檢查使用者資訊一致性**
   ```sql
   SELECT * FROM check_audit_logs_user_info_consistency() 
   WHERE is_consistent = false;
   ```

3. **檢查效能統計**
   ```sql
   SELECT * FROM get_audit_logs_performance_stats();
   ```

4. **測試查詢效能**
   ```sql
   EXPLAIN ANALYZE
   SELECT * FROM audit_logs_with_user_info 
   WHERE occurred_at >= CURRENT_DATE - INTERVAL '7 days'
   ORDER BY occurred_at DESC 
   LIMIT 100;
   ```

### API 測試

1. **查詢審計日誌**
   ```
   GET /api/audit-logs?page=1&pageSize=20
   ```
   確認返回的結果中 `userName` 和 `userRole` 不再顯示 N/A

2. **查看使用者活動統計**
   ```
   GET /api/audit-logs/statistics?fromDate=2025-07-01&toDate=2025-08-02
   ```
   確認 `TopActiveUsers` 包含正確的使用者名稱

## 效能考量

### 索引策略
- **複合索引**：優化常見查詢模式
- **部分索引**：只為有 user_id 的記錄建立索引
- **函數式索引**：優化 COALESCE 查詢

### 查詢優化
- **LEFT JOIN**：避免遺失沒有對應 users 記錄的審計日誌
- **COALESCE**：提供多層 fallback 機制
- **檢視物件**：簡化複雜查詢

### 記憶體使用
- 使用 `QueryFirstOrDefaultAsync` 而非 `QueryAsync` 來節省記憶體
- 在 `EnrichUserInfoAsync` 中實作快取機制（未來改進）

## 錯誤處理

### 常見問題

1. **使用者不存在於 users 表**
   - 解決：使用 user_id 作為 fallback 顯示名稱

2. **系統使用者未正確顯示**
   - 解決：檢查 `GetSystemUserInfo()` 方法的映射

3. **查詢效能變慢**
   - 解決：確認索引已正確建立，執行 `ANALYZE` 更新統計資訊

4. **資料不一致**
   - 解決：執行一致性檢查函數，必要時重新執行回填腳本

### 監控指標

```sql
-- 監控查詢效能
SELECT 
    query,
    calls,
    total_time,
    mean_time,
    stddev_time
FROM pg_stat_statements 
WHERE query LIKE '%audit_logs%'
ORDER BY total_time DESC;

-- 監控索引使用情況
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
WHERE tablename = 'audit_logs'
ORDER BY idx_tup_read DESC;
```

## 回滾機制

如果需要回滾更改：

1. **回滾程式碼更改**
   ```bash
   git revert <commit-hash>
   ```

2. **從備份表復原資料**（如果執行了回填腳本）
   ```sql
   UPDATE audit_logs al
   SET 
       user_name = b.user_name,
       user_role = b.user_role
   FROM audit_logs_backup_before_user_info_backfill b
   WHERE al.id = b.id;
   ```

3. **移除建立的索引**（可選）
   ```sql
   DROP INDEX IF EXISTS idx_audit_logs_user_occurred_time;
   DROP INDEX IF EXISTS idx_audit_logs_paginated_query;
   -- ... 其他索引
   ```

## 未來改進

1. **快取機制**：在 `EnrichUserInfoAsync` 中添加記憶體快取
2. **非同步處理**：對於大量審計日誌寫入場景，考慮非同步補全
3. **使用者資訊變更追蹤**：當 users 表中的資訊變更時，更新相關審計日誌
4. **多租戶支援**：為多租戶環境調整使用者資訊補全邏輯

## 聯絡資訊

如有問題或建議，請聯絡資料庫架構師團隊。