# D3.js 連線錯誤修復

## 🐛 問題描述

在實現 Missing Persons 視覺化功能時，遇到了以下錯誤：

```
ERROR TypeError: d.target.startsWith is not a function
    at SVGLineElement.<anonymous> (family-tree.page.ts:912:46)
```

## 🔍 問題分析

### 錯誤原因
在 D3.js 的力導向圖中，連線的 `source` 和 `target` 屬性可能是：
1. **字符串**：當連線數據是靜態的字符串 ID 時
2. **對象**：當 D3.js 的力導向模擬運行時，會將字符串 ID 轉換為節點對象

### 具體情況
- 初始狀態：`d.source = "1"`, `d.target = "missing_123"`
- 運行後：`d.source = {id: "1", x: 100, y: 200, ...}`, `d.target = {id: "missing_123", x: 150, y: 250, ...}`

當 D3.js 將連線綁定到節點對象時，`source` 和 `target` 就變成了對象，不再有 `startsWith` 方法。

## ✅ 解決方案

### 修復前
```typescript
.style("stroke", (d: any) => {
  const isMissingPersonLink = d.target.startsWith('missing_') || d.source.startsWith('missing_');
  // 錯誤：當 d.target 是對象時，沒有 startsWith 方法
})
```

### 修復後
```typescript
.style("stroke", (d: any) => {
  const targetId = typeof d.target === 'string' ? d.target : d.target.id;
  const sourceId = typeof d.source === 'string' ? d.source : d.source.id;
  const isMissingPersonLink = targetId.startsWith('missing_') || sourceId.startsWith('missing_');
  // 正確：先檢查類型，再獲取 ID
})
```

## 🔧 修復範圍

修復了所有連線樣式相關的方法：

1. **stroke 顏色**
2. **stroke-width 寬度**
3. **stroke-dasharray 虛線樣式**
4. **opacity 透明度**

每個方法都使用了相同的修復邏輯：

```typescript
const targetId = typeof d.target === 'string' ? d.target : d.target.id;
const sourceId = typeof d.source === 'string' ? d.source : d.source.id;
```

## 📊 修復效果

### 修復前
- ❌ 圖譜載入時出現 JavaScript 錯誤
- ❌ 連線樣式無法正確應用
- ❌ 瀏覽器控制台顯示錯誤訊息

### 修復後
- ✅ 圖譜正常載入和顯示
- ✅ Missing persons 的連線正確顯示為橙色虛線
- ✅ 正常人員的連線保持原有樣式
- ✅ 無 JavaScript 錯誤

## 🎯 技術要點

### 1. 類型檢查
使用 `typeof` 運算符檢查變數類型：
```typescript
typeof d.target === 'string'
```

### 2. 條件運算符
使用三元運算符進行條件賦值：
```typescript
const targetId = typeof d.target === 'string' ? d.target : d.target.id;
```

### 3. 安全訪問
確保在訪問對象屬性前進行類型檢查，避免運行時錯誤。

## 🔮 預防措施

### 1. 類型定義
可以考慮為 D3.js 的連線數據定義更精確的 TypeScript 介面：

```typescript
interface D3Link {
  source: string | { id: string; x: number; y: number };
  target: string | { id: string; x: number; y: number };
  // 其他屬性...
}
```

### 2. 工具函數
可以創建一個工具函數來安全地獲取節點 ID：

```typescript
function getNodeId(node: string | { id: string }): string {
  return typeof node === 'string' ? node : node.id;
}
```

### 3. 測試覆蓋
為不同狀態的連線數據添加測試，確保修復的穩定性。

## 📝 總結

這個修復解決了 D3.js 力導向圖中連線數據類型變化的問題，確保了 Missing Persons 視覺化功能的正常運行。修復方案具有：

- **兼容性**：同時支援字符串和對象類型的連線數據
- **穩定性**：避免了運行時錯誤
- **可維護性**：代碼清晰易懂，易於後續維護
- **擴展性**：可以應用於其他類似的 D3.js 場景 