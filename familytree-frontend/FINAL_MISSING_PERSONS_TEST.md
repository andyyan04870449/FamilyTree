# Missing Persons 顯示功能最終測試指南

## 🎯 測試目標

驗證修改後的代碼是否正確顯示 Missing Persons 在家族圖譜中。

## 🔍 關鍵修改

### 1. **解決 sourcePersonId 為 0 的問題**
- 當 `sourcePersonId` 為 0 時，自動連接到主角節點（layerDepth = 0）
- 避免因為找不到來源節點而無法創建連線

### 2. **增強調試功能**
- 顯示當前所有節點的列表
- 詳細記錄每個步驟的執行情況
- 顯示連線創建的詳細信息

## 🧪 測試步驟

### 步驟 1：檢查後端數據
```bash
# 在 familytree-backend 目錄下執行
curl -s "http://localhost:5087/api/missingperson" | jq '.[0:2]'
```

**預期結果：**
```json
[
  {
    "id": 6,
    "name": "馬晶",
    "relationType": "",
    "sourcePersonId": 0,
    "sourceField": "",
    "status": "pending"
  }
]
```

### 步驟 2：打開瀏覽器開發者工具
1. 按 F12 打開開發者工具
2. 切換到 **Console** 標籤頁
3. 清空現有的日誌（按 Ctrl+L 或點擊清空按鈕）

### 步驟 3：進入家族圖譜頁面
1. 打開前端應用
2. 進入家族圖譜頁面
3. 選擇一個人員開始視覺分析

### 步驟 4：觀察 Console 日誌

**應該看到的關鍵日誌：**

```
🔄 開始載入 Missing Persons, personId: [數字]
📥 收到 Missing Persons 數據: [數組]
✅ 成功載入 X 個 Missing Persons
  1. 馬晶 () - 來源: 0
  2. 馬晶 () - 來源: 0

處理所有層級資料: [對象]
添加目標節點: [節點對象]
添加連線: [連線對象]

🎯 開始添加 Missing Persons 到圖譜
🔍 開始添加 Missing Persons 到圖譜
SVG 狀態: true
Missing Persons 數量: X
當前節點數量: Y
當前節點列表: [
  { id: "1", name: "主角姓名" },
  { id: "2", name: "相關人員" }
]

處理 Missing Person: {
  id: 6,
  name: "馬晶",
  sourcePersonId: 0,
  status: "pending"
}

✅ 添加 Missing Person 節點: {
  id: "missing_6",
  name: "馬晶",
  layerDepth: 1
}

🔗 使用主角節點作為來源: 主角姓名
✅ 添加 Missing Person 連線: {
  source: "1",
  target: "missing_6",
  type: "關係"
}

📊 添加結果: X 個節點, X 個連線
```

### 步驟 5：檢查圖譜顯示

在圖譜中應該看到：

1. **橙色虛線節點**：代表 "馬晶" 等找不到的人員
2. **橙色虛線連線**：從主角節點連接到 Missing Persons
3. **圖例說明**：顯示 "找不到的人員" 的樣式

## 🐛 故障排除

### 問題 1：沒有看到 Console 日誌
**可能原因：**
- 頁面沒有正確載入
- JavaScript 錯誤阻止了執行

**解決方案：**
- 檢查瀏覽器控制台是否有錯誤
- 重新載入頁面

### 問題 2：看到 "找不到主角節點" 警告
**可能原因：**
- 主角節點沒有正確創建
- `layerDepth` 設置不正確

**解決方案：**
- 檢查 `initializeRootPerson` 方法是否被調用
- 確認主角節點的 `layerDepth` 為 0

### 問題 3：Missing Persons 載入但沒有添加到圖譜
**可能原因：**
- `addMissingPersonsToGraph` 沒有被調用
- SVG 未初始化

**解決方案：**
- 檢查 Console 中是否有 "🎯 開始添加 Missing Persons 到圖譜" 日誌
- 確認 SVG 狀態為 true

### 問題 4：節點顯示但沒有連線
**可能原因：**
- 連線創建失敗
- 樣式沒有正確應用

**解決方案：**
- 檢查是否有 "✅ 添加 Missing Person 連線" 日誌
- 檢查 SVG 元素的 style 屬性

## ✅ 成功標準

測試成功的標誌：

1. **Console 日誌完整**：
   - 顯示 Missing Persons 載入成功
   - 顯示節點和連線添加成功
   - 沒有錯誤或警告

2. **視覺效果正確**：
   - 橙色虛線節點可見
   - 橙色虛線連線可見
   - 圖例正確顯示

3. **數據正確**：
   - 節點數量正確
   - 連線數量正確
   - 節點名稱正確

## 📊 預期結果

如果一切正常，您應該看到：

- ✅ **Console 日誌**：完整的執行流程記錄
- ✅ **圖譜節點**：主角節點 + 相關人員節點 + Missing Persons 節點
- ✅ **圖譜連線**：正常關係連線 + Missing Persons 連線
- ✅ **視覺樣式**：Missing Persons 使用橙色虛線樣式

## 🔧 如果仍然沒有顯示

如果按照以上步驟測試後仍然沒有看到 Missing Persons，請：

1. **截圖 Console 日誌**：保存完整的 Console 輸出
2. **檢查網絡請求**：在 Network 標籤頁查看 API 請求
3. **檢查元素**：在 Elements 標籤頁查看 SVG 結構

然後將這些信息提供給我，我可以進一步診斷問題。

## 🎉 測試完成

當您看到橙色虛線節點和連線時，Missing Persons 顯示功能就成功實現了！

**最終驗證清單：**
- [ ] 後端 API 返回 Missing Persons 數據
- [ ] Console 顯示完整的載入和添加日誌
- [ ] 圖譜中顯示橙色虛線節點
- [ ] 圖譜中顯示橙色虛線連線
- [ ] 沒有 JavaScript 錯誤
- [ ] 圖例正確顯示樣式說明 