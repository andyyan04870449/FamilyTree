# GitHub Issue: 資料隔離策略標準化

**Issue Title**: 資料隔離策略不一致：統一採用user_id隔離機制

**Labels**: `database`, `security`, `architecture`, `high-priority`

**Priority**: 🔥 HIGH

---

## 問題描述

系統中存在**三種不同的資料隔離策略**，導致架構不一致、權限控制複雜化和潛在的安全風險：

### 當前隔離策略分布

#### 🔄 混合隔離表 (4個)
| 表名 | 記錄數 | user_id | project_id | 問題 |
|------|-------|---------|------------|------|
| `person_profile` | 36 | ✅ | ✅ | 12筆記錄只有project_id |
| `relationship_layers` | 9 | ✅ | ✅ | 雙重隔離邏輯 |
| `user_favorites` | 4 | ✅ | ✅ | 權限檢查複雜 |
| `project_permissions` | 0 | ✅ | ✅ | 新表，待規範 |

#### 👤 純user_id隔離 (8個表)
```
activity_logs, user_roles, user_tokens, field_mapping, 
analysis_results, analysis_sessions, missing_persons, user_permissions
```

#### 📁 純project_id隔離 (7個表)  
```
visual_analysis_nodes, search_keywords, user_update_file, photos,
mergedpersons, personmergelog, search_logs
```

### 核心問題

#### 1. **資料歸屬不明確**
```sql
-- 當前複雜的查詢邏輯
SELECT * FROM person_profile 
WHERE (user_id = @userId OR project_id IN (
    SELECT id FROM projects WHERE user_id = @userId
))
```

#### 2. **權限控制邏輯混亂**
- 需要同時檢查user_id和project_id權限
- 容易產生安全漏洞
- API權限檢查複雜化

#### 3. **業務邏輯矛盾**
```
現況: 12個專案都屬於同一使用者(admin_default)
問題: 為什麼要用project_id隔離同一使用者的資料？
結果: 邏輯不一致，維護困難
```

#### 4. **查詢效能影響**
- 複雜的JOIN條件
- 多重權限檢查
- 索引效率不佳

## 解決方案：統一user_id隔離 ⭐

### 設計原則
- **user_id**: 主要隔離機制，確保資料安全
- **project_id**: 資料分類標籤，便於組織管理
- **權限**: 統一在使用者層級控制

### 實施計畫

#### Phase 1 (週1): 資料完整性修復
```sql
-- 修復缺失的user_id
UPDATE person_profile 
SET user_id = (
    SELECT p.user_id 
    FROM projects p 
    WHERE p.id = person_profile.project_id
)
WHERE user_id IS NULL AND project_id IS NOT NULL;

-- 添加NOT NULL約束
ALTER TABLE person_profile 
    ALTER COLUMN user_id SET NOT NULL;
```

#### Phase 2 (週2): API層級統一
```csharp
// 新的統一權限檢查模式
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

// 簡化的權限檢查
public async Task<bool> CanAccessData(string userId, string resourceUserId)
{
    return userId == resourceUserId || await IsAdminUser(userId);
}
```

#### Phase 3 (週3): 索引優化
```sql
-- 新的複合索引策略
CREATE INDEX idx_person_profile_user_id_project_id ON person_profile(user_id, project_id);
CREATE INDEX idx_relationship_layers_user_id_project_id ON relationship_layers(user_id, project_id);
CREATE INDEX idx_user_favorites_user_id_project_id ON user_favorites(user_id, project_id);
```

#### Phase 4 (週4): 測試和文件
- [ ] 權限控制測試
- [ ] 資料隔離測試  
- [ ] 效能基準測試
- [ ] API文件更新

## 預期效益

### 🔒 安全性提升
- ✅ **統一權限邏輯**: 減少安全漏洞風險
- ✅ **清晰的資料隔離**: 確保使用者資料不會洩露
- ✅ **簡化存取控制**: 降低權限配置錯誤機率

### ⚡ 效能改善
- ✅ **查詢簡化**: 預計15-25%效能提升
- ✅ **索引效率**: 單一主鍵過濾效率更高
- ✅ **減少JOIN**: 避免複雜的多表關聯

### 🛠️ 維護性改善
- ✅ **程式碼簡化**: 權限檢查邏輯減少30-40%行數
- ✅ **統一模式**: 所有表遵循相同的隔離策略
- ✅ **開發效率**: 新功能開發時間減少20%

### 🚀 擴展性增強
- ✅ **多租戶準備**: 為未來多租戶架構奠定基礎
- ✅ **權限系統**: 支援更複雜的權限需求
- ✅ **資料治理**: 便於實施資料治理策略

## 詳細工作項目

### 🔧 資料庫層級
- [ ] 分析所有混合隔離表的資料分布
- [ ] 執行user_id修復腳本
- [ ] 添加NOT NULL約束
- [ ] 優化索引策略
- [ ] 驗證資料完整性

### 💻 後端API層級
- [ ] 更新所有DataAccessService方法
- [ ] 簡化權限檢查邏輯
- [ ] 調整Controller權限驗證
- [ ] 更新Repository模式
- [ ] 添加統一的資料過濾器

### 🎯 前端調整
- [ ] 更新Service層API呼叫
- [ ] 調整權限檢查邏輯
- [ ] 測試使用者資料隔離

### 📋 測試驗證
- [ ] 單元測試：權限控制
- [ ] 整合測試：資料隔離
- [ ] 效能測試：查詢速度
- [ ] 安全測試：資料洩露防護

## 影響評估

### 🔄 需要調整的組件
| 組件 | 影響程度 | 調整內容 |
|------|----------|----------|
| **Data Access Layer** | 🔴 高 | 查詢條件和權限檢查 |
| **API Controllers** | 🟡 中 | 權限驗證邏輯 |
| **Frontend Services** | 🟡 中 | API呼叫參數 |
| **權限系統** | 🟢 低 | 簡化現有邏輯 |

### ✅ 不受影響的組件
- 使用者介面基本功能
- 核心業務邏輯處理
- 外部API介面相容性
- 現有資料內容

## 風險評估

### 🟡 中風險
- **資料完整性**: 大量UPDATE操作
  - **緩解**: 完整備份 + 分批執行 + 驗證腳本
- **業務邏輯調整**: API行為變更
  - **緩解**: 漸進式部署 + 回滾計畫

### 🟢 低風險
- **效能暫時下降**: 遷移期間影響
  - **緩解**: 非高峰期執行
- **學習曲線**: 開發團隊適應
  - **緩解**: 培訓 + 文件更新

## 驗收標準

### 功能驗收
- [ ] 所有混合隔離表的user_id填充完整
- [ ] 權限檢查邏輯統一且正確
- [ ] 資料隔離100%有效（無跨使用者資料洩露）
- [ ] 所有現有功能正常運作

### 效能驗收
- [ ] 查詢效能提升 >= 15%
- [ ] API回應時間改善
- [ ] 資料庫CPU使用率降低

### 安全驗收
- [ ] 權限控制測試通過
- [ ] 資料隔離測試通過  
- [ ] 安全掃描無新增漏洞

## 相關文件

- 📄 **詳細分析**: `Documentation/data-isolation-inconsistency-analysis.md`
- 📊 **資料庫架構報告**: `Documentation/database-architecture-analysis.md`
- 🔧 **遷移腳本**: `Database/migrations/data-isolation-standardization/`

## 後續改善機會

1. **多租戶架構**: 基於統一user_id隔離實施
2. **資料治理**: 建立完整的資料分類和存取策略
3. **效能監控**: 實施自動化效能監控
4. **權限細化**: 支援更精細的資源層級權限

---

**Priority**: 🔥 **HIGH** - 安全性和架構統一的重要基礎

**Estimated effort**: 3-4週

**Dependencies**: 
- person_profile優化 (Issue #1)
- 權限系統穩定性

**Success metrics**:
- 權限檢查邏輯複雜度降低30%
- 查詢效能提升15-25%  
- 資料隔離安全性100%