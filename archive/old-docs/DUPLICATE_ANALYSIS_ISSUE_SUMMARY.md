# 重複分析問題總結

## 🎯 問題描述

在黃心田的分析圖譜中，馬晶這個人員在多層關聯中重複出現，導致圖譜視覺化混亂。

## 🔍 根本原因分析

### 1. 資料庫層面
- **81個分析會話**：每次分析都在創建新的會話，而不是重用現有會話
- **336個關係記錄**：每個會話都包含相同的關係，但會話ID不同
- **缺少唯一約束**：`relationship_layers` 表沒有適當的約束防止重複

### 2. 後端邏輯問題
- **會話管理**：每次分析都生成新的 `analysis_session_id`
- **去重邏輯缺失**：在保存關係時沒有檢查是否已存在相同關係
- **遞迴分析**：雖然有 `_analyzedPersons` HashSet，但沒有跨會話的去重

### 3. 前端視覺化問題
- **節點去重缺失**：在處理分層資料時沒有對節點進行去重
- **連線重複**：相同的連線可能被重複添加

## 📊 資料統計

### 修復前
- **分析會話數量**：81個
- **關係記錄總數**：336個
- **重複關係**：每個會話包含相同的關係（黃心田 -> 馬晶等）

### 修復後
- **資料庫約束**：已添加唯一約束防止未來重複
- **去重邏輯**：後端和前端都添加了去重處理
- **會話重用**：需要修改會話管理邏輯

## 🛠️ 已實施的修復

### 1. 資料庫修復 ✅
```sql
-- 添加唯一約束
ALTER TABLE relationship_layers 
ADD CONSTRAINT unique_relationship_per_session 
UNIQUE (analysis_session_id, source_person_id, target_person_id);

-- 添加索引
CREATE INDEX idx_relationship_layers_session_source_target 
ON relationship_layers (analysis_session_id, source_person_id, target_person_id);
```

### 2. 後端修復 ✅
- **AnalysisController.cs**：在 `GetRelationshipLayers` 方法中添加去重邏輯
- **AnalysisBackgroundService.cs**：在 `SaveRelationship` 方法中加強重複檢查
- **保留最淺層級**：當同一個人出現在多個層級時，只保留最淺層級的關係

### 3. 前端修復 ✅
- **family-tree.page.ts**：在 `processAllHierarchicalData` 和 `processHierarchicalData` 方法中添加去重邏輯
- **使用 Set 追蹤**：用 `processedNodes` 和 `processedLinks` 避免重複處理

## 🚨 剩餘問題

### 1. 會話管理問題
**問題**：每次分析都創建新的會話，導致關係記錄累積
**影響**：資料庫記錄越來越多，查詢效能下降

**解決方案**：
```csharp
// 在 StartAnalysis 方法中檢查是否有現有的完成會話
var existingSession = await connection.QueryFirstOrDefaultAsync(
    @"SELECT id FROM analysis_sessions 
      WHERE root_person_id = @PersonId AND status = 'completed'
      ORDER BY created_at DESC LIMIT 1",
    new { PersonId = personId });

if (existingSession != null)
{
    // 重用現有會話，而不是創建新會話
    return existingSession.id;
}
```

### 2. 資料清理問題
**問題**：歷史的重複資料仍然存在
**影響**：查詢時可能返回多個相同的關係

**解決方案**：
```sql
-- 清理重複的會話，只保留最新的
DELETE FROM analysis_sessions 
WHERE id NOT IN (
    SELECT DISTINCT ON (root_person_id) id 
    FROM analysis_sessions 
    WHERE status = 'completed'
    ORDER BY root_person_id, created_at DESC
);

-- 清理對應的關係記錄
DELETE FROM relationship_layers 
WHERE analysis_session_id NOT IN (
    SELECT id FROM analysis_sessions
);
```

## 📋 建議的後續步驟

### 1. 立即執行
- [ ] 執行資料清理腳本，刪除重複的會話和關係
- [ ] 修改會話管理邏輯，重用現有會話
- [ ] 測試新的分析流程

### 2. 中期改進
- [ ] 添加分析結果快取機制
- [ ] 實現增量分析（只分析新增的關係）
- [ ] 添加分析結果版本控制

### 3. 長期優化
- [ ] 實現分析結果的定期清理
- [ ] 添加分析效能監控
- [ ] 實現分析結果的備份和恢復

## 🔧 測試驗證

### 測試步驟
1. **清理資料庫**：執行資料清理腳本
2. **重新分析**：對黃心田執行新的分析
3. **檢查結果**：驗證圖譜中沒有重複的人員
4. **監控日誌**：確認去重邏輯正常工作

### 預期結果
- 每個人在圖譜中只出現一次
- 關係連線唯一且清晰
- 分析效能提升
- 資料庫記錄數量減少

---

**問題發現時間**：2025年1月27日  
**修復狀態**：部分完成（代碼修復完成，資料清理待執行）  
**影響範圍**：所有分析結果的視覺化顯示 