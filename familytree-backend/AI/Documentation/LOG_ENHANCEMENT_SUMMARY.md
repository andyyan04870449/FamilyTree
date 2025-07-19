# 圖譜分析日誌增強功能總結

## 已完成的功能

### 1. 日誌記錄增強
- ✅ 在 `AnalysisBackgroundService` 中增加詳細的分析過程日誌
- ✅ 在 `AIService` 中增加 AI 解析過程的詳細日誌
- ✅ 在 `AnalysisController` 中增加 API 請求的日誌記錄
- ✅ 使用表情符號和結構化格式提高日誌可讀性

### 2. 日誌配置
- ✅ 配置 Serilog 作為日誌框架
- ✅ 設定檔案日誌記錄（每日滾動，最大 10MB，保留 10 個檔案）
- ✅ 設定控制台日誌輸出
- ✅ 配置不同組件的日誌級別

### 3. 日誌查看工具
- ✅ 創建 `LogController` 提供 API 端點查看日誌
- ✅ 支援查看指定行數的日誌
- ✅ 支援查看特定日期的日誌
- ✅ 支援查看日誌檔案列表
- ✅ 支援查看日誌尾部

### 4. 日誌內容詳情

#### 分析啟動階段
```
=== 收到分析啟動請求 ===
請求參數: PersonId = 1, MaxDepth = 3
請求時間: 2024-01-15 10:30:00
✅ 分析啟動成功: PersonId = 1, MaxDepth = 3
```

#### 遞迴分析過程
```
=== 開始執行遞迴分析任務 ===
分析參數: PersonId = 1, SessionId = abc123, MaxDepth = 3
分析開始時間: 2024-01-15 10:30:01
開始遞迴分析根節點: PersonId = 1
```

#### 人員分析詳情
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

#### AI 解析過程
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

#### 關係保存過程
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

#### 分析完成統計
```
=== 遞迴分析任務完成 ===
完成時間: 2024-01-15 10:35:00
總共分析 5 個人員
總共找到 8 個關係
分析深度: 1-3 層
```

### 5. 日誌查看方法

#### 直接查看檔案
```bash
# 即時查看日誌
tail -f logs/familytree-analysis-20240115.log

# 查看最近 100 行
tail -n 100 logs/familytree-analysis-20240115.log

# 查看錯誤日誌
grep "❌\|ERROR" logs/familytree-analysis-20240115.log

# 查看特定人員
grep "PersonId = 1" logs/familytree-analysis-20240115.log
```

#### 使用 API 端點
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

### 6. 檔案結構
```
familytree-backend/
├── logs/                                    # 日誌目錄
│   ├── familytree-analysis-20240115.log    # 日誌檔案
│   └── familytree-analysis-20240116.log    # 日誌檔案
├── AI/                                      # AI 功能目錄
│   ├── README.md                            # AI 功能說明
│   ├── Templates/                           # AI 模板文件
│   │   ├── extract_name_relation_function.json
│   │   └── extract_name_relation_prompt.json
│   └── Documentation/                       # AI 功能說明文件
│       ├── LOGS_README.md                   # 日誌說明文件
│       ├── LOG_ENHANCEMENT_SUMMARY.md       # 本文件
│       └── test-logs.sh                     # 日誌測試腳本
├── Controllers/
│   ├── AnalysisController.cs               # 分析控制器（已增強日誌）
│   └── LogController.cs                    # 日誌控制器（新增）
├── Services/
│   ├── AnalysisBackgroundService.cs        # 分析服務（已增強日誌）
│   └── AIService.cs                        # AI 服務（已增強日誌）
├── Program.cs                              # 程式入口（已配置 Serilog）
├── appsettings.json                        # 配置檔案（已更新）
├── appsettings.Development.json            # 開發配置（已更新）
└── familytree-backend.csproj               # 專案檔案（已添加 Serilog 套件）
```

### 7. 技術實現

#### 使用的技術
- **Serilog**: 結構化日誌框架
- **Serilog.Sinks.File**: 檔案日誌輸出
- **Serilog.Sinks.Console**: 控制台日誌輸出
- **表情符號**: 提高日誌可讀性
- **結構化日誌**: 便於搜尋和分析

#### 日誌級別
- **Information (ℹ️)**: 一般資訊
- **Warning (⚠️)**: 警告資訊
- **Error (❌)**: 錯誤資訊
- **Success (✅)**: 成功操作

#### 日誌配置
- 每日滾動檔案
- 最大檔案大小：10MB
- 保留檔案數量：10個
- 即時寫入磁碟（1秒間隔）

### 8. 使用建議

1. **開發階段**: 使用詳細日誌進行除錯和監控
2. **生產階段**: 調整日誌級別以減少檔案大小
3. **監控**: 定期檢查錯誤日誌
4. **維護**: 定期清理舊的日誌檔案

### 9. 下一步改進

1. 添加日誌搜尋功能
2. 實現日誌分析工具
3. 添加日誌告警機制
4. 實現日誌壓縮存檔

## 總結

圖譜分析過程的日誌記錄功能已經完全實現，可以詳細追蹤整個分析流程，包括：
- 分析啟動和參數記錄
- 人員資料查詢過程
- AI 關係解析詳細過程
- 關係保存到資料庫
- 遞迴分析進度和統計
- 錯誤和異常處理

所有日誌都使用結構化格式記錄，便於搜尋和分析，並提供了多種查看方式。 