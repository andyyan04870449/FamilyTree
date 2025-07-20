# Missing Persons 視覺化功能

## 📋 功能概述

為了讓用戶能夠在圖譜中看到 AI 分析過程中發現但未在資料庫中找到的人員，我們實現了 Missing Persons 視覺化功能。這些人員會以特殊的視覺樣式顯示在圖譜中，讓用戶能夠直觀地看到分析結果。

## 🎨 視覺設計

### 節點樣式
- **顏色**: 橙色 (#FF9800)
- **邊框**: 深橙色 (#E65100) 虛線邊框
- **邊框樣式**: 虛線 (stroke-dasharray: 5,5)
- **邊框寬度**: 2px

### 連線樣式
- **顏色**: 橙色 (#FF9800)
- **線條樣式**: 虛線 (stroke-dasharray: 10,5)
- **線條寬度**: 2px
- **透明度**: 0.7

### 圖例
- 添加了 "找不到的人員" 和 "找不到人員的關係" 的圖例項目
- 使用虛線邊框和虛線連線來區分

## 🔧 技術實現

### 1. 前端服務擴展

**檔案**: `src/app/services/person.service.ts`

添加了 Missing Persons 相關的介面和 API 方法：

```typescript
export interface MissingPerson {
  id: number;
  name: string;
  relationType: string;
  sourcePersonId: number;
  sourceField: string;
  analysisSessionId: string;
  layerDepth: number;
  discoveredAt: string;
  status: string;
  resolvedPersonId?: number;
  notes?: string;
  sourcePersonName?: string;
  resolvedPersonName?: string;
}

// API 方法
getMissingPersons(status?: string, sourcePersonId?: number): Observable<MissingPerson[]>
getMissingPerson(id: number): Observable<MissingPerson>
updateMissingPerson(id: number, updateData: any): Observable<any>
deleteMissingPerson(id: number): Observable<void>
getMissingPersonStats(): Observable<MissingPersonStats>
```

### 2. 圖譜組件擴展

**檔案**: `src/app/pages/family-tree/family-tree.page.ts`

#### 新增屬性
```typescript
missingPersons: MissingPerson[] = [];
showMissingPersonsModal = false;
selectedMissingPerson: MissingPerson | null = null;
```

#### 新增方法
```typescript
loadMissingPersons(personId?: number) // 載入 missing persons
addMissingPersonsToGraph() // 將 missing persons 添加到圖譜
viewMissingPersonDetails(missingPerson: MissingPerson) // 查看詳情
closeMissingPersonsModal() // 關閉詳情視窗
updateMissingPersonStatus(missingPerson: MissingPerson, status: string) // 更新狀態
```

### 3. 視覺樣式實現

#### 節點樣式
```typescript
.style("fill", (d: any) => {
  if (d.id.startsWith('missing_')) {
    return "#FF9800"; // 橙色
  }
  return d.gender === 'male' ? "#42A5F5" : "#F48FB1";
})
.style("stroke", (d: any) => {
  if (d.id.startsWith('missing_')) {
    return "#E65100"; // 深橙色邊框
  }
  // ... 其他邏輯
})
.style("stroke-dasharray", (d: any) => {
  if (d.id.startsWith('missing_')) {
    return "5,5"; // 虛線邊框
  }
  return "none";
})
```

#### 連線樣式
```typescript
.style("stroke", (d: any) => {
  const isMissingPersonLink = d.target.startsWith('missing_') || d.source.startsWith('missing_');
  if (isMissingPersonLink) {
    return "#FF9800"; // 橙色
  }
  return d.isFamily ? "#ff6b35" : "#666";
})
.style("stroke-dasharray", (d: any) => {
  const isMissingPersonLink = d.target.startsWith('missing_') || d.source.startsWith('missing_');
  if (isMissingPersonLink) {
    return "10,5"; // 虛線
  }
  return "none";
})
```

### 4. CSS 樣式

**檔案**: `src/app/pages/family-tree/family-tree.page.scss`

```scss
.legend-color {
  &.missing {
    background: #FF9800; // 橙色
    border: 2px dashed #E65100; // 虛線邊框
  }
}

.legend-line {
  &.missing {
    background: #FF9800; // 橙色
    border-top: 2px dashed #FF9800; // 虛線樣式
  }
}
```

## 🔄 工作流程

### 1. 載入 Missing Persons
當用戶開始視覺分析或載入分析結果時，系統會自動調用 `loadMissingPersons()` 方法：

```typescript
// 在 startVisualAnalysis 和 loadAnalysisResult 中
this.loadMissingPersons(personId);
```

### 2. 添加到圖譜
`addMissingPersonsToGraph()` 方法會：
- 檢查每個 missing person 的狀態
- 只顯示狀態為 'pending' 的人員
- 為每個 missing person 創建節點
- 創建與來源人員的連線
- 更新圖譜顯示

### 3. 節點識別
Missing persons 的節點 ID 格式為：`missing_{id}`，這樣可以：
- 與正常人員節點區分
- 在樣式應用時進行識別
- 避免與現有節點衝突

## 📊 使用場景

### 1. 分析過程中
- 當 AI 分析發現關係但找不到對應人員時
- 這些人員會以橙色虛線節點顯示
- 連線也會使用虛線樣式

### 2. 結果查看
- 用戶可以點擊 missing person 節點查看詳情
- 可以更新狀態（如標記為已解決）
- 可以添加備註

### 3. 後續處理
- 管理員可以根據 missing persons 列表添加新人員
- 可以將 missing person 與現有人員關聯
- 可以標記為已解決或忽略

## ✅ 優勢

1. **視覺化**: 直觀地顯示分析結果中的缺失人員
2. **區分性**: 使用特殊樣式與正常人員區分
3. **可操作性**: 提供查看詳情和更新狀態的功能
4. **完整性**: 讓用戶看到完整的關係網路，包括未找到的人員
5. **可追蹤**: 記錄發現時間、來源等資訊，便於後續處理

## 🔮 未來擴展

1. **批量操作**: 批量更新 missing persons 狀態
2. **篩選功能**: 根據狀態、來源等條件篩選顯示
3. **統計資訊**: 顯示 missing persons 的統計資訊
4. **自動關聯**: 當添加新人員時，自動關聯相關的 missing persons
5. **通知功能**: 當發現新的 missing persons 時發送通知 