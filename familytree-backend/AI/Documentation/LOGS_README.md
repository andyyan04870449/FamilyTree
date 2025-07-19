# 圖譜分析日誌記錄說明

## 概述

本系統已增強圖譜分析過程的日誌記錄功能，可以詳細追蹤整個分析流程，包括：
- 分析啟動和參數
- 人員資料查詢
- AI 關係解析過程
- 關係保存到資料庫
- 遞迴分析進度
- 錯誤和異常處理

## 日誌檔案位置

日誌檔案位於：`familytree-backend/logs/` 目錄下
- 檔案名稱格式：`familytree-analysis-YYYYMMDD.log`
- 每日自動滾動
- 最大檔案大小：10MB
- 保留檔案數量：10個

**注意**: 本文件位於 `AI/Documentation/` 目錄下，相對於日誌檔案的路徑為 `../../logs/`

## 日誌內容說明

### 1. 分析啟動階段
```
=== 收到分析啟動請求 ===
請求參數: PersonId = 1, MaxDepth = 3
請求時間: 2024-01-15 10:30:00
✅ 分析啟動成功: PersonId = 1, MaxDepth = 3
```

### 2. 遞迴分析開始
```
=== 開始執行遞迴分析任務 ===
分析參數: PersonId = 1, SessionId = abc123, MaxDepth = 3
分析開始時間: 2024-01-15 10:30:01
開始遞迴分析根節點: PersonId = 1
```

### 3. 人員分析過程
```
=== 開始分析人員 (深度 1) ===
分析目標: PersonId = 1
✅ 開始分析人員: PersonId = 1, 當前深度 = 1, 已分析人數 = 1
📋 正在查詢人員資料: PersonId = 1
✅ 成功獲取人員資料:
  - ID: 1
  - 家庭關係: '母：邱還真'
  - 朋友關係: '羅亞瑟，淡江大學同學'
  - 參與活動: '無'
```

### 4. AI 關係解析
```
🔍 開始分析家庭關係...
=== 開始使用 OpenAI 4o Mini 模型解析人名關係 ===
輸入文字內容: 母：邱還真
開始時間: 2024-01-15 10:30:02
📋 開始載入 AI 模板...
✅ 提示詞模板載入成功
✅ 函數定義載入成功
🚀 發送請求到 OpenAI 4o Mini 模型...
📨 收到 AI 回應
✅ AI 解析成功:
  - 找到關係配對: 1 個
📋 關係配對詳情:
    - 邱還真: 母親
```

### 5. 關係保存
```
💾 正在保存關係到資料庫...
  - 來源人員: 1
  - 目標人員: 2
  - 關係類型: 母親
  - 來源欄位: family_relationships
  - 層級深度: 1
  - 分析會話: abc123
✅ 關係保存成功: 1 -> 2 (母親) at layer 1
```

### 6. 遞迴分析
```
🔄 開始遞迴分析發現的人員...
  - 遞迴分析: PersonId = 2 (深度 2)
=== 開始分析人員 (深度 2) ===
```

### 7. 分析完成
```
=== 遞迴分析任務完成 ===
完成時間: 2024-01-15 10:35:00
總共分析 5 個人員
總共找到 8 個關係
分析深度: 1-3 層
```

## 查看日誌的方法

### 1. 直接查看檔案
```bash
# 查看最新的日誌檔案
tail -f familytree-backend/logs/familytree-analysis-20240115.log

# 查看最近的 100 行
tail -n 100 familytree-backend/logs/familytree-analysis-20240115.log

# 從 AI/Documentation 目錄查看
tail -f ../../logs/familytree-analysis-20240115.log
```

### 2. 使用 API 端點
```bash
# 查看最近的 100 行日誌
curl "http://localhost:5000/api/log/analysis?lines=100"

# 查看特定日期的日誌
curl "http://localhost:5000/api/log/analysis?lines=100&date=20240115"

# 查看日誌檔案列表
curl "http://localhost:5000/api/log/files"

# 查看日誌尾部
curl "http://localhost:5000/api/log/tail?lines=50"
```

### 3. 在瀏覽器中查看
- 訪問：`http://localhost:5000/api/log/analysis`
- 訪問：`http://localhost:5000/api/log/tail`

## 日誌級別說明

- **Information (ℹ️)**: 一般資訊，如分析進度、成功操作
- **Warning (⚠️)**: 警告資訊，如找不到人員、格式不匹配
- **Error (❌)**: 錯誤資訊，如資料庫連接失敗、AI 解析失敗
- **Success (✅)**: 成功操作，如關係保存成功、分析完成

## 常見問題

### Q: 日誌檔案太大怎麼辦？
A: 系統會自動滾動日誌檔案，每個檔案最大 10MB，最多保留 10 個檔案。

### Q: 如何只查看錯誤日誌？
A: 可以使用 grep 命令過濾：
```bash
grep "❌\|ERROR" familytree-backend/logs/familytree-analysis-20240115.log
```

### Q: 如何查看特定人員的分析日誌？
A: 可以使用 grep 命令過濾：
```bash
grep "PersonId = 1" familytree-backend/logs/familytree-analysis-20240115.log
```

### Q: 日誌檔案在哪裡？
A: 日誌檔案位於 `familytree-backend/logs/` 目錄下，如果目錄不存在，系統會自動創建。

### Q: 如何從 AI/Documentation 目錄查看日誌？
A: 可以使用相對路徑：`../../logs/familytree-analysis-*.log`

## 注意事項

1. 日誌檔案會記錄敏感資訊，請妥善保管
2. 在生產環境中，建議調整日誌級別以減少檔案大小
3. 定期清理舊的日誌檔案以節省磁碟空間
4. 日誌記錄會影響系統性能，但影響很小 