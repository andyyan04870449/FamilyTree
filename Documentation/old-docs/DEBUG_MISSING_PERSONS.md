# Missing Persons 調試指南

## 🔍 如何檢查 Console 日誌

### 步驟 1：打開瀏覽器開發者工具
1. 按 **F12** 鍵
2. 點擊 **Console** 標籤頁
3. 清空現有日誌（按 **Ctrl+L** 或點擊清空按鈕）

### 步驟 2：開始測試
1. 進入家族圖譜頁面
2. 選擇一個人員
3. 點擊"開始視覺分析"
4. 等待分析完成，點擊"查看結果"

### 步驟 3：觀察關鍵日誌

**應該看到的日誌順序：**

```
開始視覺分析，設置當前人員ID: [數字]
視覺分析完成！正在載入結果...
載入分析結果，personId: [數字]
🔄 開始載入 Missing Persons 和關係數據
📥 收到 Missing Persons 數據: [數組]
✅ 成功載入 X 個 Missing Persons
  1. 馬晶 () - 來源: 0
  2. 馬晶 () - 來源: 0
開始載入所有層級關係: personId = [數字], maxDepth = 10
所有層級資料: [對象]
初始化主角節點: [對象]
主角節點已添加: [對象]
處理所有層級資料: [對象]
添加目標節點: [對象]
添加連線: [對象]
🎯 開始添加 Missing Persons 到圖譜
🔍 開始添加 Missing Persons 到圖譜
SVG 狀態: true
Missing Persons 數量: X
當前節點數量: Y
當前節點列表: [
  { id: "1", name: "主角姓名" },
  { id: "2", name: "相關人員" }
]
處理 Missing Person: [對象]
✅ 添加 Missing Person 節點: [對象]
🔗 使用主角節點作為來源: 主角姓名
✅ 添加 Missing Person 連線: [對象]
📊 添加結果: X 個節點, X 個連線
```

## 🐛 常見問題診斷

### 問題 1：沒有看到 "🔄 開始載入 Missing Persons 和關係數據"
**可能原因：**
- `loadAnalysisResult` 沒有被調用
- 分析沒有完成

**解決方案：**
- 確認分析狀態為 "completed"
- 檢查是否有 "視覺分析完成！正在載入結果..." 的日誌

### 問題 2：沒有看到 "📥 收到 Missing Persons 數據"
**可能原因：**
- 後端 API 沒有返回數據
- API 調用失敗

**解決方案：**
- 檢查 Network 標籤頁中的 API 請求
- 手動測試 API：`curl -s "http://localhost:5087/api/missingperson" | jq .`

### 問題 3：看到 "ℹ️ 沒有找到 Missing Persons"
**可能原因：**
- 後端沒有 Missing Persons 數據
- 查詢條件不正確

**解決方案：**
- 檢查後端數據庫中是否有 missing_persons 記錄
- 確認 `status` 為 "pending"

### 問題 4：沒有看到 "🎯 開始添加 Missing Persons 到圖譜"
**可能原因：**
- `processAllHierarchicalData` 沒有被調用
- 關係數據載入失敗

**解決方案：**
- 檢查是否有 "處理所有層級資料" 的日誌
- 確認關係數據載入成功

### 問題 5：看到 "⚠️ 找不到主角節點"
**可能原因：**
- 主角節點沒有正確創建
- `layerDepth` 設置不正確

**解決方案：**
- 檢查是否有 "主角節點已添加" 的日誌
- 確認主角節點的 `layerDepth` 為 0

### 問題 6：看到 "📊 添加結果: 0 個節點, 0 個連線"
**可能原因：**
- Missing Persons 狀態不是 "pending"
- 節點已存在

**解決方案：**
- 檢查 Missing Persons 的 `status` 字段
- 確認節點 ID 是否重複

## 📊 預期的 Console 輸出

如果一切正常，您應該看到類似這樣的完整日誌：

```
開始視覺分析，設置當前人員ID: 1
視覺分析完成！正在載入結果...
載入分析結果，personId: 1
🔄 開始載入 Missing Persons 和關係數據
📥 收到 Missing Persons 數據: [
  {
    "id": 6,
    "name": "馬晶",
    "relationType": "",
    "sourcePersonId": 0,
    "status": "pending"
  }
]
✅ 成功載入 1 個 Missing Persons
  1. 馬晶 () - 來源: 0
開始載入所有層級關係: personId = 1, maxDepth = 10
所有層級資料: {
  "rootPerson": {"id": 1, "name": "張三"},
  "relationships": [...],
  "relatedPersons": [...]
}
初始化主角節點: {"id": 1, "name": "張三"}
主角節點已添加: {"id": "1", "name": "張三", "layerDepth": 0}
處理所有層級資料: [對象]
添加目標節點: [對象]
添加連線: [對象]
🎯 開始添加 Missing Persons 到圖譜
🔍 開始添加 Missing Persons 到圖譜
SVG 狀態: true
Missing Persons 數量: 1
當前節點數量: 3
當前節點列表: [
  {"id": "1", "name": "張三"},
  {"id": "2", "name": "李四"},
  {"id": "3", "name": "王五"}
]
處理 Missing Person: {
  "id": 6,
  "name": "馬晶",
  "sourcePersonId": 0,
  "status": "pending"
}
✅ 添加 Missing Person 節點: {
  "id": "missing_6",
  "name": "馬晶",
  "layerDepth": 1
}
🔗 使用主角節點作為來源: 張三
✅ 添加 Missing Person 連線: {
  "source": "1",
  "target": "missing_6",
  "type": "關係"
}
📊 添加結果: 1 個節點, 1 個連線
```

## 🔧 如果仍然沒有顯示

請將完整的 Console 日誌複製給我，包括：

1. **所有日誌內容**：從開始到結束的完整日誌
2. **錯誤信息**：任何紅色的錯誤信息
3. **警告信息**：任何黃色的警告信息
4. **Network 請求**：在 Network 標籤頁中查看 API 請求的狀態

這樣我就能準確診斷問題所在並提供解決方案。 