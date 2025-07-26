# Missing Persons 顯示功能測試指南

## 🎯 測試目標

驗證系統是否正確將找不到的人員（Missing Persons）顯示在家族圖譜中。

## 🔍 測試步驟

### 1. 檢查後端數據

首先確認後端有 Missing Persons 數據：

```bash
# 在 familytree-backend 目錄下執行
curl -s "http://localhost:5087/api/missingperson" | jq .
```

**預期結果：**
- 應該看到包含 `sourcePersonId` 不為 0 的記錄
- `status` 應該為 `"pending"`
- 有具體的 `name` 和 `relationType`

### 2. 檢查前端調試信息

打開瀏覽器開發者工具（F12），查看 Console 標籤頁。

**預期看到的日誌：**

```
🔄 開始載入 Missing Persons, personId: [數字]
📥 收到 Missing Persons 數據: [數組]
✅ 成功載入 X 個 Missing Persons
  1. [姓名] ([關係]) - 來源: [來源ID]
🔍 開始添加 Missing Persons 到圖譜
SVG 狀態: true
Missing Persons 數量: X
當前節點數量: Y
處理 Missing Person: [對象]
✅ 添加 Missing Person 節點: [節點對象]
✅ 添加 Missing Person 連線: [連線對象]
📊 添加結果: X 個節點, X 個連線
```

### 3. 視覺化檢查

在圖譜中應該看到：

- **橙色虛線節點**：代表找不到的人員
- **橙色虛線連線**：連接到來源人員
- **圖例說明**：顯示 "找不到的人員" 的樣式說明

## 🐛 常見問題排查

### 問題 1：沒有看到 Missing Persons

**可能原因：**
- 後端沒有 Missing Persons 數據
- `sourcePersonId` 為 0，找不到對應的來源節點
- 圖譜還沒有初始化就嘗試添加節點

**解決方案：**
1. 檢查後端 API 返回的數據
2. 確認 `sourcePersonId` 對應的節點存在
3. 查看 Console 中的錯誤信息

### 問題 2：Missing Persons 顯示但沒有連線

**可能原因：**
- 來源節點 ID 不匹配
- 連線創建失敗

**解決方案：**
1. 檢查 Console 中的 "找不到來源節點" 警告
2. 確認節點 ID 格式是否一致

### 問題 3：樣式不正確

**可能原因：**
- CSS 樣式沒有正確應用
- D3.js 連線樣式設置有問題

**解決方案：**
1. 檢查瀏覽器開發者工具的 Elements 標籤頁
2. 查看 SVG 元素的 style 屬性

## 📊 測試數據準備

如果需要測試數據，可以：

1. **手動插入測試數據：**
```sql
INSERT INTO missing_persons (
    name, relation_type, source_person_id, source_field, 
    analysis_session_id, layer_depth, discovered_at, status
) VALUES (
    '測試人員', '朋友', 1, 'friends', 
    'test-session', 1, CURRENT_TIMESTAMP, 'pending'
);
```

2. **觸發 AI 分析：**
- 在系統中為某個人員添加包含不存在人員姓名的關係數據
- 啟動 AI 分析，讓系統自動識別並記錄 Missing Persons

## ✅ 成功標準

測試成功的標誌：

1. **數據層面：**
   - 後端 API 返回有效的 Missing Persons 數據
   - `sourcePersonId` 不為 0 且對應的節點存在

2. **前端層面：**
   - Console 顯示正確的載入和添加日誌
   - 沒有 JavaScript 錯誤

3. **視覺層面：**
   - 圖譜中顯示橙色虛線節點
   - 有對應的橙色虛線連線
   - 圖例正確顯示樣式說明

## 🔧 調試技巧

### 1. 使用瀏覽器開發者工具
- **Console 標籤頁**：查看詳細的調試日誌
- **Network 標籤頁**：檢查 API 請求是否成功
- **Elements 標籤頁**：檢查 SVG 元素和樣式

### 2. 添加臨時調試代碼
在關鍵位置添加 `console.log` 來追蹤執行流程：

```typescript
console.log('當前節點:', this.nodes);
console.log('當前連線:', this.links);
console.log('SVG 元素:', this.svg);
```

### 3. 檢查數據流
確保數據從後端到前端的完整流程：
```
後端 API → 前端 Service → Component → D3.js 渲染
```

## 📝 測試記錄

建議記錄以下信息：

- **測試時間**：YYYY-MM-DD HH:MM:SS
- **測試環境**：瀏覽器版本、系統版本
- **後端數據**：API 返回的原始數據
- **前端日誌**：Console 中的關鍵日誌
- **視覺效果**：截圖記錄
- **發現問題**：詳細描述問題現象
- **解決方案**：採取的修復措施

## 🎉 測試完成

當所有檢查項目都通過時，Missing Persons 顯示功能就成功實現了！

**最終驗證：**
- ✅ 後端數據正確
- ✅ 前端載入成功
- ✅ 圖譜顯示正確
- ✅ 樣式應用正確
- ✅ 無錯誤日誌 