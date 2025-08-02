# 資料庫性能優化實施報告

## 概述

本文檔記錄了 FamilyTree 後端應用程式的資料庫性能優化實施過程，包含索引優化、N+1 查詢問題解決、快取機制實施和維護腳本建立。

## 優化目標

基於性能基準測試，設定以下優化目標：

| 查詢類型 | 優化前 | 目標性能 | 改善幅度 |
|---------|--------|----------|----------|
| 獲取人員列表 (1000筆) | 2.3s | < 500ms | ↓78% |
| 獲取人員詳情含關係 | 1.5s | < 300ms | ↓80% |
| 全文搜索 | 3.8s | < 800ms | ↓79% |
| 關係圖查詢 | 4.2s | < 1000ms | ↓76% |

## 實施的優化措施

### 1. 資料庫索引優化

#### 1.1 person_profile 表索引

建立了以下索引以提升查詢性能：

```sql
-- 基礎查詢索引
CREATE INDEX CONCURRENTLY idx_person_profile_user_id_active ON person_profile(user_id) WHERE user_id IS NOT NULL;
CREATE INDEX CONCURRENTLY idx_person_profile_name ON person_profile(name) WHERE name IS NOT NULL;
CREATE INDEX CONCURRENTLY idx_person_profile_birthday ON person_profile(birthday) WHERE birthday IS NOT NULL;

-- 複合索引（常用查詢組合）
CREATE INDEX CONCURRENTLY idx_person_profile_user_name ON person_profile(user_id, name) WHERE user_id IS NOT NULL AND name IS NOT NULL;

-- 全文搜索索引
CREATE INDEX CONCURRENTLY idx_person_profile_fulltext ON person_profile USING gin(to_tsvector('simple', ...));

-- 分析用索引
CREATE INDEX CONCURRENTLY idx_person_profile_birth_year ON person_profile(EXTRACT(YEAR FROM birthday));
```

#### 1.2 relationships 表索引

```sql
-- 關係查詢索引
CREATE INDEX CONCURRENTLY idx_relationships_person ON relationships(person_id);
CREATE INDEX CONCURRENTLY idx_relationships_related ON relationships(related_person_id);
CREATE INDEX CONCURRENTLY idx_relationships_type ON relationships(relationship_type);

-- 複合索引用於關係圖查詢
CREATE INDEX CONCURRENTLY idx_relationships_person_type ON relationships(person_id, relationship_type);

-- 雙向關係查詢優化
CREATE INDEX CONCURRENTLY idx_relationships_bidirectional ON relationships(LEAST(person_id, related_person_id), GREATEST(person_id, related_person_id));
```

#### 1.3 file_metadata 表索引

```sql
-- 文件查詢索引
CREATE INDEX CONCURRENTLY idx_file_metadata_entity ON file_metadata(entity_type, entity_id) WHERE is_deleted = false;
CREATE INDEX CONCURRENTLY idx_file_metadata_uploaded_at ON file_metadata(uploaded_at DESC) WHERE is_deleted = false;
CREATE INDEX CONCURRENTLY idx_file_metadata_md5 ON file_metadata(md5_hash) WHERE md5_hash IS NOT NULL;
```

### 2. N+1 查詢問題解決

#### 2.1 問題分析

原始代碼存在嚴重的 N+1 查詢問題：

```csharp
// 問題代碼：每個人員都單獨查詢關聯資料
foreach (var person in persons)
{
    person.Relationships = await GetPersonRelationships(person.Id);
    person.Photos = await GetPersonPhotos(person.Id);
}
```

#### 2.2 解決方案

實施批量查詢策略：

```csharp
// 優化後：批量查詢所有相關資料
var personIds = persons.Select(p => p.Id).ToList();

var relationshipsTask = GetRelationshipsByPersonIdsAsync(personIds);
var photosTask = GetPhotosByPersonIdsAsync(personIds);

await Task.WhenAll(relationshipsTask, photosTask);

// 建立查找字典進行高效組裝
var relationshipsLookup = relationshipsTask.Result
    .GroupBy(r => r.PersonId)
    .ToDictionary(g => g.Key, g => g.ToList());
```

#### 2.3 新增的批量查詢方法

- `GetPersonDataListOptimizedAsync()` - 優化版人員列表查詢
- `GetRelationshipsByPersonIdsAsync()` - 批量查詢人員關係
- `GetPhotosByPersonIdsAsync()` - 批量查詢人員照片

### 3. 記憶體快取實施

#### 3.1 快取策略

實施了三層快取策略：

- **短期快取** (2-5分鐘)：人員列表、搜尋結果
- **中期快取** (5-15分鐘)：人員詳情、關係資料
- **長期快取** (15分鐘-1小時)：照片資料、分析結果

#### 3.2 快取實施

建立了 `CachedDataAccessServiceV2` 作為原始服務的包裝器：

```csharp
public async Task<PersonDataModel?> GetPersonDataByIdAsync(int id, string userId, string userRole)
{
    var cacheKey = $"person_{id}_{userId}_{userRole}";
    
    if (_cache.TryGetValue(cacheKey, out PersonDataModel? cached))
    {
        return cached; // 快取命中
    }

    var result = await _baseService.GetPersonDataByIdAsync(id, userId, userRole);
    
    if (result != null)
    {
        _cache.Set(cacheKey, result, _mediumCacheOptions);
    }
    
    return result;
}
```

#### 3.3 快取失效策略

- 資料新增/更新/刪除時自動清除相關快取
- 實施智能快取鍵管理避免記憶體洩漏

### 4. 資料庫維護自動化

#### 4.1 維護腳本

建立了自動化維護腳本：

- `daily_maintenance()` - 每日維護程序
- `weekly_index_maintenance()` - 週期性索引重建
- `get_table_sizes()` - 表格大小監控
- `get_index_usage_stats()` - 索引使用率分析

#### 4.2 監控視圖

- `database_health_view` - 資料庫健康狀況
- `performance_metrics_view` - 性能指標

#### 4.3 維護排程

| 任務類型 | 執行頻率 | 執行時間 | 說明 |
|---------|----------|----------|------|
| 日常維護 | 每日 | 02:00 | VACUUM ANALYZE 小型表，清理日誌 |
| 索引重建 | 每週 | 週日 03:00 | 重建可能有 bloat 的索引 |
| 統計更新 | 每月 | 1號 04:00 | 完整 VACUUM 和統計更新 |

### 5. 性能測試框架

#### 5.1 測試工具

建立了 `DatabasePerformanceTest` 類別提供：

- 完整性能測試套件
- 個別查詢性能測試
- 索引使用率分析
- 自動化測試報告生成

#### 5.2 測試端點

透過 `PerformanceTestController` 提供 API 端點：

- `POST /api/performancetest/run-full-test` - 執行完整測試
- `GET /api/performancetest/database-health` - 資料庫健康檢查
- `GET /api/performancetest/index-stats` - 索引使用統計
- `POST /api/performancetest/analyze-query` - 查詢計劃分析

## 實施檔案清單

### 資料庫腳本
- `Database/Scripts/05_performance_optimization_indexes.sql` - 性能優化索引
- `Database/Scripts/06_database_maintenance.sql` - 維護腳本和監控函數

### 服務層
- `Services/DataAccessServiceV2.cs` - 擴展批量查詢方法
- `Services/CachedDataAccessServiceV2.cs` - 快取服務包裝器
- `Models/OptimizationModels.cs` - 優化相關的資料模型

### 測試工具
- `Tools/DatabasePerformanceTest.cs` - 性能測試工具
- `Controllers/PerformanceTestController.cs` - 性能測試 API

### 配置變更
- `Program.cs` - 更新服務註冊以使用快取版本

## 使用建議

### 1. 部署步驟

1. **執行資料庫腳本**：
   ```bash
   # 按順序執行
   psql -d familytree -f Database/Scripts/05_performance_optimization_indexes.sql
   psql -d familytree -f Database/Scripts/06_database_maintenance.sql
   ```

2. **部署應用程式**：
   - 確保 MemoryCache 服務已註冊
   - 驗證 CachedDataAccessServiceV2 正確注入

3. **驗證優化效果**：
   ```bash
   curl -X POST https://yourapi/api/performancetest/run-full-test
   ```

### 2. 監控建議

- 定期檢查索引使用率
- 監控快取命中率
- 觀察查詢執行時間變化
- 設定效能警報閾值

### 3. 維護建議

- 每週檢查 `database_health_view`
- 每月執行完整性能測試
- 根據使用情況調整快取策略
- 定期檢查並清理無用索引

## 預期效果

根據實施的優化措施，預期可達成以下效果：

1. **查詢性能提升 70-80%**
2. **記憶體使用效率提升**
3. **資料庫負載降低**
4. **使用者體驗顯著改善**
5. **系統可擴展性增強**

## 注意事項

1. **索引維護**：新建索引會佔用額外儲存空間，需定期監控
2. **快取一致性**：資料變更時確保快取正確失效
3. **記憶體管理**：監控快取記憶體使用量，避免記憶體洩漏
4. **備份策略**：重要變更前建立資料庫備份
5. **漸進部署**：建議在測試環境驗證後再部署到生產環境

## 結論

透過系統性的資料庫優化，包含索引策略、查詢優化、快取機制和自動化維護，預期能夠大幅提升應用程式性能，為使用者提供更佳的體驗。建議持續監控和調整優化策略，以因應系統成長和使用模式變化。