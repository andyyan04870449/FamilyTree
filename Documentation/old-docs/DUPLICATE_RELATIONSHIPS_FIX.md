# 重複關係問題修復方案

## 🎯 問題描述

在黃心田的分析圖譜中，馬晶這個人員在多層關聯中重複出現，這不是正常的行為。重複出現會導致：
- 圖譜視覺化混亂
- 資料冗餘
- 分析結果不準確

## 🔍 問題根源分析

### 1. 後端分析服務問題
- **遞迴分析邏輯**：雖然有 `_analyzedPersons` HashSet 避免重複分析同一個人，但在保存關係時沒有檢查是否已存在相同關係
- **資料庫約束缺失**：`relationship_layers` 表缺少唯一約束來防止重複關係記錄
- **層級深度處理**：不同層級的相同關係被當作不同記錄保存

### 2. 前端視覺化問題
- **節點去重缺失**：在處理分層資料時，沒有對節點進行去重處理
- **連線重複**：相同的連線可能被重複添加

## 🛠️ 修復方案

### 1. 後端修復

#### A. 分析控制器 (`AnalysisController.cs`)
- **修改 `GetRelationshipLayers` 方法**：在返回結果前進行去重處理
- **保留最淺層級**：當同一個人出現在多個層級時，只保留最淺層級的關係
- **添加統計資訊**：返回原始數量和解重後的數量

#### B. 分析背景服務 (`AnalysisBackgroundService.cs`)
- **加強 `SaveRelationship` 方法**：在保存前檢查是否已存在相同或反向關係
- **移除 ON CONFLICT**：改用手動檢查避免資料庫約束衝突

### 2. 前端修復

#### A. 家族樹組件 (`family-tree.page.ts`)
- **修改 `processAllHierarchicalData` 方法**：添加節點和連線去重邏輯
- **修改 `processHierarchicalData` 方法**：單層分析時也進行去重
- **使用 Set 追蹤**：用 `processedNodes` 和 `processedLinks` 避免重複處理

### 3. 資料庫修復

#### A. SQL 修復腳本 (`fix-duplicate-relationships.sql`)
- **清理重複記錄**：保留每個分析會話中每個關係對的第一條記錄
- **添加唯一約束**：防止未來重複關係的產生
- **添加索引**：提升查詢效能

## 📋 修復步驟

### 步驟 1：執行資料庫修復
```bash
cd familytree-backend
./fix-duplicates.sh
```

### 步驟 2：重新啟動服務
```bash
# 停止現有服務
# 重新啟動後端和前端
./start-all.sh
```

### 步驟 3：重新分析黃心田
1. 進入前端應用程式
2. 查詢黃心田
3. 重新執行分析
4. 查看圖譜結果

## ✅ 預期效果

### 修復前
- 馬晶在圖譜中出現多次
- 關係連線重複
- 視覺化混亂

### 修復後
- 每個人員在圖譜中只出現一次
- 關係連線唯一
- 清晰的層級結構
- 準確的分析結果

## 🔧 技術細節

### 去重邏輯
```typescript
// 前端去重邏輯
const processedNodes = new Set<string>();
const processedLinks = new Set<string>();

// 檢查是否已處理
if (processedNodes.has(nodeId)) {
    continue; // 跳過重複節點
}
```

### 資料庫約束
```sql
-- 唯一約束防止重複關係
ALTER TABLE relationship_layers 
ADD CONSTRAINT unique_relationship_per_session 
UNIQUE (analysis_session_id, source_person_id, target_person_id);
```

### 後端檢查
```csharp
// 檢查是否已存在相同關係
var existingRelation = await connection.QueryFirstOrDefaultAsync(
    @"SELECT id FROM relationship_layers 
      WHERE source_person_id = @SourceId AND target_person_id = @TargetId 
      AND analysis_session_id = @SessionId",
    new { SourceId = sourceId, TargetId = targetId, SessionId = _sessionId });
```

## 📊 監控指標

修復後可以監控以下指標：
- **關係記錄數量**：應該減少
- **唯一人員數量**：保持不變
- **圖譜節點數量**：應該等於唯一人員數量
- **分析效能**：應該提升

## 🚀 未來改進

1. **即時去重**：在分析過程中即時去重，而不是事後修復
2. **視覺化優化**：添加去重統計顯示
3. **效能監控**：添加分析效能監控
4. **自動修復**：定期檢查和自動修復重複資料

---

**修復時間**：2025年1月27日  
**修復版本**：v1.0  
**影響範圍**：黃心田分析圖譜及所有未來分析 