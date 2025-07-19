# AI 目錄結構說明

## 整理後的檔案組織

### 原始位置 → 新位置

| 原始檔案 | 新位置 | 說明 |
|---------|--------|------|
| `LOGS_README.md` | `AI/Documentation/LOGS_README.md` | 圖譜分析日誌記錄說明 |
| `LOG_ENHANCEMENT_SUMMARY.md` | `AI/Documentation/LOG_ENHANCEMENT_SUMMARY.md` | 日誌增強功能總結 |
| `test-logs.sh` | `AI/Documentation/test-logs.sh` | 日誌測試腳本 |

### 目錄結構

```
familytree-backend/
├── AI/                                      # AI 功能目錄
│   ├── README.md                            # AI 功能總覽
│   ├── Templates/                           # AI 模板文件
│   │   ├── extract_name_relation_function.json    # 函數定義模板
│   │   └── extract_name_relation_prompt.json      # 提示詞模板
│   └── Documentation/                       # AI 功能說明文件
│       ├── DIRECTORY_STRUCTURE.md           # 本文件
│       ├── LOGS_README.md                   # 日誌記錄詳細說明
│       ├── LOG_ENHANCEMENT_SUMMARY.md       # 日誌功能實現總結
│       └── test-logs.sh                     # 日誌測試腳本
├── logs/                                    # 日誌檔案目錄（運行時生成）
├── Controllers/                             # 控制器
├── Services/                                # 服務層
└── ...                                      # 其他檔案
```

## 檔案說明

### AI/README.md
AI 功能的總覽文件，包含：
- 目錄結構說明
- 功能概述
- 使用方式
- 技術架構
- 注意事項

### AI/Templates/
包含 AI 分析所需的模板文件：
- **extract_name_relation_function.json**: OpenAI Function Calling 的函數結構定義
- **extract_name_relation_prompt.json**: AI 分析的提示詞模板

### AI/Documentation/
包含所有 AI 功能的說明文件和工具：

#### LOGS_README.md
詳細說明圖譜分析過程的日誌記錄功能：
- 日誌檔案位置和格式
- 日誌內容說明
- 查看日誌的方法
- 常見問題解答

#### LOG_ENHANCEMENT_SUMMARY.md
總結日誌增強功能的實現：
- 已完成的功能
- 技術實現細節
- 檔案結構
- 使用建議

#### test-logs.sh
用於測試和驗證日誌功能的腳本：
- 檢查日誌目錄和檔案
- 顯示日誌內容
- 提供查看指令

#### DIRECTORY_STRUCTURE.md
本文件，說明檔案組織結構

## 使用方式

### 查看日誌
```bash
# 從專案根目錄執行測試腳本
cd familytree-backend
./AI/Documentation/test-logs.sh

# 直接查看日誌檔案
tail -f logs/familytree-analysis-*.log

# 使用 API 查看
curl "http://localhost:5000/api/log/tail?lines=50"
```

### 查看說明文件
```bash
# 查看 AI 功能總覽
cat AI/README.md

# 查看日誌說明
cat AI/Documentation/LOGS_README.md

# 查看功能總結
cat AI/Documentation/LOG_ENHANCEMENT_SUMMARY.md
```

## 路徑引用

### 相對路徑
- 從 `AI/Documentation/` 到日誌檔案：`../../logs/`
- 從專案根目錄到日誌檔案：`logs/`
- 從 `AI/Documentation/` 到專案根目錄：`../../`

### 絕對路徑
- 日誌檔案：`/Users/user/FamilyTree/familytree-backend/logs/`
- AI 目錄：`/Users/user/FamilyTree/familytree-backend/AI/`

## 維護說明

### 新增 AI 相關文件
1. 如果是模板文件，放在 `AI/Templates/` 目錄
2. 如果是說明文件，放在 `AI/Documentation/` 目錄
3. 更新 `AI/README.md` 中的目錄結構

### 更新路徑引用
當移動文件時，需要更新：
1. 腳本中的相對路徑
2. 說明文件中的路徑引用
3. 相關文件中的交叉引用

### 保持一致性
- 所有 AI 相關文件都應該在 `AI/` 目錄下
- 使用一致的命名規範
- 保持文件結構的清晰性 