# 工單 #003：資料庫查詢優化

## 工單資訊
- **工單編號**：TASK-003
- **優先級**：🟡 重要
- **預估時間**：8 小時
- **負責 Agent**：database-architect
- **創建日期**：2025-08-02
- **狀態**：待處理

## 問題描述
當前資料庫存在以下性能問題：
1. 缺少適當的索引導致全表掃描
2. N+1 查詢問題嚴重影響 API 響應時間
3. 大表缺少分區策略
4. 查詢計劃未優化

## 性能基準測試結果
```
當前狀況：
- 獲取人員列表 (1000筆)：平均 2.3 秒
- 獲取人員詳情含關係：平均 1.5 秒  
- 全文搜索：平均 3.8 秒
- 關係圖查詢：平均 4.2 秒
```

## 優化方案

### 1. 索引策略實施

#### 1.1 person_data 表索引
```sql
-- 基礎查詢索引
CREATE INDEX CONCURRENTLY idx_person_projectid ON person_data(project_id) WHERE deleted_at IS NULL;
CREATE INDEX CONCURRENTLY idx_person_familyname ON person_data(family_name) WHERE deleted_at IS NULL;
CREATE INDEX CONCURRENTLY idx_person_birthdate ON person_data(birth_date) WHERE deleted_at IS NULL;
CREATE INDEX CONCURRENTLY idx_person_status ON person_data(status) WHERE deleted_at IS NULL;

-- 複合索引（常用查詢組合）
CREATE INDEX CONCURRENTLY idx_person_project_family 
ON person_data(project_id, family_name) 
WHERE deleted_at IS NULL;

-- 全文搜索索引
CREATE INDEX CONCURRENTLY idx_person_fulltext 
ON person_data 
USING gin(to_tsvector('simple', 
    coalesce(family_name,'') || ' ' || 
    coalesce(given_name,'') || ' ' || 
    coalesce(other_names,'') || ' ' ||
    coalesce(biography,'')
));

-- 分析用索引
CREATE INDEX CONCURRENTLY idx_person_birth_year 
ON person_data(EXTRACT(YEAR FROM birth_date)) 
WHERE birth_date IS NOT NULL;
```

#### 1.2 relationships 表索引
```sql
-- 關係查詢索引
CREATE INDEX CONCURRENTLY idx_relationship_person ON relationships(person_id);
CREATE INDEX CONCURRENTLY idx_relationship_related ON relationships(related_person_id);
CREATE INDEX CONCURRENTLY idx_relationship_type ON relationships(relationship_type);

-- 複合索引用於關係圖查詢
CREATE INDEX CONCURRENTLY idx_relationship_person_type 
ON relationships(person_id, relationship_type);

-- 雙向關係查詢
CREATE INDEX CONCURRENTLY idx_relationship_bidirectional 
ON relationships(LEAST(person_id, related_person_id), GREATEST(person_id, related_person_id));
```

#### 1.3 file_metadata 表索引
```sql
-- 文件查詢索引
CREATE INDEX CONCURRENTLY idx_file_entity 
ON file_metadata(entity_type, entity_id) 
WHERE is_deleted = false;

CREATE INDEX CONCURRENTLY idx_file_uploaded 
ON file_metadata(uploaded_at DESC) 
WHERE is_deleted = false;

CREATE INDEX CONCURRENTLY idx_file_type 
ON file_metadata(file_type) 
WHERE is_deleted = false;
```

### 2. 解決 N+1 查詢問題

#### 2.1 問題代碼示例
```csharp
// 當前問題代碼 - DataAccessServiceV2.cs
public async Task<List<PersonDto>> GetPersonsWithDetails(string projectId)
{
    var persons = await GetPersonsByProject(projectId);
    
    // N+1 問題：每個人都單獨查詢
    foreach (var person in persons)
    {
        person.Relationships = await GetPersonRelationships(person.Id);
        person.Photos = await GetPersonPhotos(person.Id);
        person.Addresses = await GetPersonAddresses(person.Id);
    }
    
    return persons;
}
```

#### 2.2 優化後代碼
```csharp
// 優化方案 - 批量查詢
public async Task<List<PersonDto>> GetPersonsWithDetailsOptimized(string projectId)
{
    var persons = await GetPersonsByProject(projectId);
    var personIds = persons.Select(p => p.Id).ToList();
    
    // 批量查詢所有相關數據
    var relationshipsTask = GetRelationshipsByPersonIds(personIds);
    var photosTask = GetPhotosByPersonIds(personIds);
    var addressesTask = GetAddressesByPersonIds(personIds);
    
    await Task.WhenAll(relationshipsTask, photosTask, addressesTask);
    
    // 建立查找字典
    var relationshipsLookup = relationshipsTask.Result
        .GroupBy(r => r.PersonId)
        .ToDictionary(g => g.Key, g => g.ToList());
        
    var photosLookup = photosTask.Result
        .GroupBy(p => p.EntityId)
        .ToDictionary(g => g.Key, g => g.ToList());
        
    var addressesLookup = addressesTask.Result
        .GroupBy(a => a.PersonId)
        .ToDictionary(g => g.Key, g => g.ToList());
    
    // 組裝數據
    foreach (var person in persons)
    {
        person.Relationships = relationshipsLookup.GetValueOrDefault(person.Id, new List<RelationshipDto>());
        person.Photos = photosLookup.GetValueOrDefault(person.Id, new List<PhotoDto>());
        person.Addresses = addressesLookup.GetValueOrDefault(person.Id, new List<AddressDto>());
    }
    
    return persons;
}

// 批量查詢方法
private async Task<List<RelationshipDto>> GetRelationshipsByPersonIds(List<string> personIds)
{
    const string sql = @"
        SELECT r.*, p1.family_name, p1.given_name, p2.family_name as related_family_name, p2.given_name as related_given_name
        FROM relationships r
        JOIN person_data p1 ON r.person_id = p1.id
        JOIN person_data p2 ON r.related_person_id = p2.id
        WHERE r.person_id = ANY(@PersonIds)";
        
    return await _db.QueryAsync<RelationshipDto>(sql, new { PersonIds = personIds });
}
```

### 3. 查詢優化建議

#### 3.1 使用 EXPLAIN ANALYZE
```sql
-- 分析慢查詢
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM person_data 
WHERE project_id = 'xxx' 
AND family_name LIKE '王%';
```

#### 3.2 實施查詢結果快取
```csharp
// 使用 MemoryCache 快取常用查詢
public async Task<List<PersonDto>> GetPersonsByProjectCached(string projectId)
{
    var cacheKey = $"persons_project_{projectId}";
    
    if (_cache.TryGetValue(cacheKey, out List<PersonDto> cached))
    {
        return cached;
    }
    
    var persons = await GetPersonsByProjectOptimized(projectId);
    
    _cache.Set(cacheKey, persons, new MemoryCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(5),
        Size = persons.Count
    });
    
    return persons;
}
```

### 4. 資料庫維護計劃
```sql
-- 定期執行 VACUUM 和 ANALYZE
VACUUM ANALYZE person_data;
VACUUM ANALYZE relationships;
VACUUM ANALYZE file_metadata;

-- 更新統計信息
ANALYZE person_data;
ANALYZE relationships;
```

## 測試要求
1. 性能基準測試
   - 測試每個查詢的響應時間
   - 對比優化前後的性能

2. 索引效果驗證
   - 使用 EXPLAIN 確認索引被使用
   - 監控索引使用率

3. 並發測試
   - 模擬多用戶同時查詢
   - 確保無死鎖問題

## 預期改進
```
優化後目標：
- 獲取人員列表 (1000筆)：< 500ms (↓78%)
- 獲取人員詳情含關係：< 300ms (↓80%)
- 全文搜索：< 800ms (↓79%)
- 關係圖查詢：< 1000ms (↓76%)
```

## 驗收標準
- [ ] 所有索引創建完成
- [ ] N+1 查詢問題解決
- [ ] 查詢響應時間達標
- [ ] 資料庫維護計劃實施
- [ ] 監控告警配置完成
- [ ] 性能測試報告

## 風險和注意事項
1. 創建索引可能暫時影響寫入性能
2. 使用 CONCURRENTLY 避免鎖表
3. 監控磁碟空間（索引會佔用額外空間）
4. 準備索引回滾腳本
5. 在低峰期執行索引創建

## 監控指標
- 查詢響應時間 (P50, P95, P99)
- 慢查詢日誌
- 索引使用率
- 資料庫連接數
- 快取命中率