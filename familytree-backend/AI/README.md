# AI 功能目錄

本目錄包含 FamilyTree 系統中所有 AI 相關的功能和文件。

## 目錄結構

```
AI/
├── README.md                    # 本文件
├── Templates/                   # AI 模板文件
│   ├── extract_name_relation_function.json    # 人名關係提取函數定義
│   └── extract_name_relation_prompt.json      # 人名關係提取提示詞模板
└── Documentation/               # AI 功能說明文件
    ├── LOGS_README.md           # 圖譜分析日誌記錄說明
    ├── LOG_ENHANCEMENT_SUMMARY.md  # 日誌增強功能總結
    └── test-logs.sh             # 日誌測試腳本
```

## 功能說明

### 1. Templates 目錄
包含 AI 分析所需的模板文件：
- **extract_name_relation_function.json**: 定義 OpenAI Function Calling 的函數結構
- **extract_name_relation_prompt.json**: 定義 AI 分析的提示詞模板

### 2. Documentation 目錄
包含 AI 功能的說明文件和工具：
- **LOGS_README.md**: 詳細說明圖譜分析過程的日誌記錄功能
- **LOG_ENHANCEMENT_SUMMARY.md**: 總結日誌增強功能的實現
- **test-logs.sh**: 用於測試和驗證日誌功能的腳本

## AI 功能概述

### 人名關係提取
系統使用 OpenAI GPT-4o Mini 模型來分析文字內容，提取人名和關係信息：

1. **輸入處理**: 接收家庭關係、朋友關係、活動參與等文字描述
2. **AI 分析**: 使用結構化提示詞和函數定義進行分析
3. **結果解析**: 提取人名和對應的關係類型
4. **回退機制**: 當 AI 分析失敗時，使用簡單的規則解析

### 日誌記錄
為了追蹤 AI 分析過程，系統實現了詳細的日誌記錄：

1. **分析過程追蹤**: 記錄每個分析步驟的詳細信息
2. **AI 調用記錄**: 記錄 OpenAI API 的請求和回應
3. **錯誤處理**: 記錄分析過程中的錯誤和異常
4. **性能監控**: 記錄分析時間和統計信息

## 使用方式

### 查看日誌
```bash
# 使用測試腳本
./AI/Documentation/test-logs.sh

# 直接查看日誌文件
tail -f logs/familytree-analysis-*.log

# 使用 API 查看
curl "http://localhost:5000/api/log/tail?lines=50"
```

### 配置 AI 服務
在 `appsettings.json` 中配置 OpenAI API Key：
```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  }
}
```

### 自定義模板
可以修改 `Templates/` 目錄下的文件來自定義 AI 分析行為：
- 修改提示詞模板以改變分析風格
- 調整函數定義以改變輸出格式

## 技術架構

### 核心組件
- **AIService**: 負責 AI 分析的核心服務
- **AnalysisBackgroundService**: 負責圖譜分析的背景服務
- **LogController**: 提供日誌查看的 API 端點

### 日誌框架
- **Serilog**: 結構化日誌記錄
- **檔案輸出**: 每日滾動的日誌文件
- **控制台輸出**: 即時查看分析進度

### 錯誤處理
- **AI 回退**: 當 AI 分析失敗時自動使用規則解析
- **詳細錯誤記錄**: 記錄所有錯誤的詳細信息
- **異常恢復**: 確保分析過程的穩定性

## 注意事項

1. **API 限制**: 注意 OpenAI API 的使用限制和成本
2. **日誌管理**: 定期清理舊的日誌文件以節省空間
3. **隱私保護**: 日誌中可能包含敏感信息，請妥善保管
4. **性能影響**: 詳細日誌會略微影響性能，但影響很小

## 未來改進

1. **多模型支援**: 支援其他 AI 模型
2. **智能快取**: 快取常見的分析結果
3. **批量處理**: 支援批量分析以提高效率
4. **自學習**: 根據歷史數據改進分析準確性 