# 資料隔離策略不一致問題分析

## 問題識別

### 當前狀況
系統中存在**兩種不同的資料隔離策略**，導致資料架構不一致和潛在的安全隱憂：

1. **user_id隔離** - 以使用者為單位隔離資料
2. **project_id隔離** - 以專案為單位隔離資料  
3. **混合隔離** - 同時包含user_id和project_id

### 隔離策略分布統計

#### 1. 混合隔離表 (4個表)
| 表名 | 總記錄數 | user_id | project_id | 雙重填充 | 僅user_id | 僅project_id |
|------|----------|---------|------------|----------|-----------|------------|
| `person_profile` | 36 | 1 unique | 3 unique | 24 records | 0 | 12 |
| `relationship_layers` | 9 | 1 unique | 1 unique | 9 records | 0 | 0 |
| `user_favorites` | 4 | 1 unique | 2 unique | 4 records | 0 | 0 |
| `project_permissions` | 0 | 0 | 0 | 0 | 0 | 0 |

#### 2. 純user_id隔離表 (8個表)
| 表名 | 記錄數 | 用途 |
|------|--------|------|
| `activity_logs` | 4 | 使用者活動記錄 |
| `user_roles` | 2 | 使用者角色關聯 |
| `user_tokens` | 18 | 使用者認證Token |
| `field_mapping` | 81 | 欄位映射配置 |
| `analysis_results` | 0 | AI分析結果 |
| `analysis_sessions` | 0 | 分析會話 |
| `missing_persons` | 0 | 遺失人員記錄 |
| `user_permissions` | 0 | 使用者額外權限 |

#### 3. 純project_id隔離表 (7個表)
| 表名 | 記錄數 | 用途 |
|------|--------|------|
| `visual_analysis_nodes` | 36 | 視覺分析節點 |
| `search_keywords` | 55 | 搜尋關鍵字 |
| `user_update_file` | 3 | 使用者更新檔案 |
| `photos` | 1 | 照片檔案 |
| `mergedpersons` | 0 | 合併人員記錄 |
| `personmergelog` | 0 | 人員合併日誌 |
| `search_logs` | 0 | 搜尋日誌 |

### 主要問題

#### 1. **資料隔離邏輯不清晰**
- **問題**: 混合隔離導致資料歸屬不明確
- **範例**: `person_profile`中，36筆記錄有24筆同時有user_id和project_id，12筆只有project_id
- **影響**: 
  - 查詢條件複雜化
  - 權限控制邏輯混亂
  - 資料完整性風險

#### 2. **業務邏輯矛盾**
- **問題**: project_id和user_id的關係不一致
- **現況**: 所有專案都屬於同一個使用者(admin_default)，但資料卻分散在project_id中
- **矛盾點**:
  ```
  專案層級: 12個專案都屬於admin_default
  資料層級: person_profile按project_id分散，但實際都是同一個使用者的資料
  ```

#### 3. **權限控制複雜化**
- **問題**: 需要同時檢查兩種隔離機制
- **影響**:
  - API權限檢查邏輯複雜
  - 容易產生安全漏洞
  - 維護成本高

#### 4. **查詢效能影響**
- **問題**: 需要JOIN多個條件進行過濾
- **範例**:
  ```sql
  -- 複雜的混合查詢
  SELECT * FROM person_profile 
  WHERE (user_id = ? OR project_id IN (
      SELECT id FROM projects WHERE user_id = ?
  ))
  ```

## 根本原因分析

### 1. **設計階段缺乏統一規劃**
系統在不同時期採用了不同的隔離策略，缺乏整體架構規劃

### 2. **業務需求變化**
- 初期：以使用者為中心的設計
- 後期：引入專案概念，但未統一整合

### 3. **多層級權限需求**
- 使用者層級權限控制
- 專案層級資料隔離
- 兩者需求重疊但實現分離

## 解決方案

### 方案A：統一採用user_id隔離 ⭐ **推薦**

#### 原理
- 以使用者為最終的資料隔離單位
- project_id作為資料分類標籤，而非隔離機制
- 權限控制在使用者層級進行

#### 實施步驟

##### 1. 資料模型調整
```sql
-- person_profile 調整
-- 保留user_id作為主要隔離，project_id作為分類
ALTER TABLE person_profile 
    ALTER COLUMN user_id SET NOT NULL,
    ADD CONSTRAINT person_profile_user_id_not_null CHECK (user_id IS NOT NULL);

-- 更新現有資料
UPDATE person_profile 
SET user_id = (SELECT user_id FROM projects WHERE id = person_profile.project_id)
WHERE user_id IS NULL AND project_id IS NOT NULL;
```

##### 2. 權限控制簡化
```sql
-- 統一的資料查詢模式
SELECT * FROM person_profile WHERE user_id = @current_user_id;
-- 如果需要按專案過濾，在應用層處理
SELECT * FROM person_profile WHERE user_id = @current_user_id AND project_id = @project_id;
```

##### 3. API層級調整
```csharp
// 統一的權限檢查
public async Task<List<PersonProfile>> GetPersonsAsync(string userId, string? projectId = null)
{
    var query = "SELECT * FROM person_profile WHERE user_id = @UserId";
    var parameters = new { UserId = userId };
    
    if (!string.IsNullOrEmpty(projectId))
    {
        query += " AND project_id = @ProjectId";
        parameters = new { UserId = userId, ProjectId = projectId };
    }
    
    return await _db.QueryAsync<PersonProfile>(query, parameters);
}
```

#### 優點
- ✅ 權限控制邏輯簡化
- ✅ 查詢效能提升
- ✅ 資料隔離清晰
- ✅ 符合現有使用者權限架構
- ✅ 實施風險較低

#### 缺點
- ❌ 專案層級共享功能需額外設計
- ❌ 部分表需要結構調整

### 方案B：統一採用project_id隔離

#### 原理
- 以專案為資料隔離單位
- 使用者通過專案權限訪問資料
- 需要建立完整的專案權限體系

#### 實施複雜度
- 需要重新設計權限系統
- 大量資料遷移工作
- 應用層邏輯大幅修改

#### 風險評估
- 🔴 **高風險** - 影響範圍大，實施複雜

### 方案C：雙重隔離機制 ❌ **不推薦**

保持現狀但規範化兩種機制的使用場景

#### 問題
- 複雜度持續增加
- 維護成本高
- 潛在安全風險

## 推薦實施計畫

### 第一階段：資料完整性修復 (週1)

#### 1. 修復missing user_id
```sql
-- 為所有缺少user_id的記錄填充正確的user_id
UPDATE person_profile 
SET user_id = (
    SELECT p.user_id 
    FROM projects p 
    WHERE p.id = person_profile.project_id
)
WHERE user_id IS NULL AND project_id IS NOT NULL;

-- 為其他混合表執行類似修復
UPDATE relationship_layers 
SET user_id = (
    SELECT p.user_id 
    FROM projects p 
    WHERE p.id = relationship_layers.project_id
)
WHERE user_id IS NULL AND project_id IS NOT NULL;
```

#### 2. 添加約束確保資料完整性
```sql
-- 確保user_id不為空
ALTER TABLE person_profile 
    ALTER COLUMN user_id SET NOT NULL;

ALTER TABLE relationship_layers 
    ALTER COLUMN user_id SET NOT NULL;

ALTER TABLE user_favorites 
    ALTER COLUMN user_id SET NOT NULL;
```

### 第二階段：API層級統一 (週2)

#### 1. 更新所有資料存取方法
- 統一使用user_id作為主要過濾條件
- project_id作為可選的次級過濾條件

#### 2. 權限檢查簡化
```csharp
// 統一的權限檢查模式
public async Task<bool> CanAccessData(string userId, string resourceUserId)
{
    // 簡單的使用者匹配檢查
    return userId == resourceUserId || await IsAdminUser(userId);
}
```

### 第三階段：查詢優化 (週3)

#### 1. 索引策略調整
```sql
-- 優化索引以user_id為主
CREATE INDEX idx_person_profile_user_id_project_id ON person_profile(user_id, project_id);
CREATE INDEX idx_relationship_layers_user_id_project_id ON relationship_layers(user_id, project_id);
CREATE INDEX idx_user_favorites_user_id_project_id ON user_favorites(user_id, project_id);
```

#### 2. 移除過時的索引
```sql
-- 移除純project_id索引（如果存在更好的複合索引）
-- 分析查詢模式後決定
```

### 第四階段：文件和測試 (週4)

#### 1. 更新技術文件
- 資料隔離策略說明
- API使用指南
- 權限模型文件

#### 2. 全面測試
- 權限控制測試
- 資料隔離測試  
- 效能測試

## 預期效益

### 1. 安全性提升
- ✅ 統一的權限控制邏輯
- ✅ 減少安全漏洞風險
- ✅ 資料隔離保證

### 2. 效能改善
- ✅ 查詢邏輯簡化 → 15-25% 效能提升
- ✅ 索引效率提升
- ✅ 減少複雜JOIN操作

### 3. 維護性改善
- ✅ 統一的資料存取模式
- ✅ 簡化的API設計
- ✅ 降低開發複雜度

### 4. 擴展性增強
- ✅ 便於實施多租戶架構
- ✅ 支援未來的權限需求擴展

## 風險評估

### 🟡 中風險
- **資料完整性**: 大量資料更新操作
- **業務邏輯調整**: API層級修改

### 🟢 低風險  
- **效能暫時下降**: 遷移期間可能的效能影響
- **學習曲線**: 開發團隊需要適應新模式

## 監控指標

### 實施前後對比
1. **查詢效能**: 平均回應時間改善15-25%
2. **程式碼複雜度**: 權限檢查邏輯行數減少30-40%
3. **安全性**: 權限漏洞數量降為0
4. **維護效率**: 新功能開發時間減少20%

## 相關影響

### 需要調整的組件
1. **API Controllers** - 權限檢查邏輯
2. **Data Access Layer** - 查詢條件調整
3. **Frontend Services** - API呼叫參數調整
4. **權限系統** - 簡化權限檢查流程

### 不受影響的組件
1. **使用者介面** - 基本功能不變
2. **核心業務邏輯** - 資料處理邏輯保持不變
3. **外部整合** - API介面保持相容

這個優化將顯著提升系統的安全性、效能和維護性，是建構穩固多租戶架構的重要基礎。