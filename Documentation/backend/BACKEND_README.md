# 
# FamilyTree 後端系統說明文件

本文檔詳細說明 FamilyTree 後端系統的架構、核心功能、API 設計和技術細節。文件內容以系統的**最終實現成果**為基礎，並整合了開發過程中的分析與優化總結。

## 1. 系統架構與設計原則

後端系統採用 **.NET 6** 框架，基於 **RESTful API** 風格設計。資料庫使用 **PostgreSQL**，並透過 **Dapper** 進行高效能的資料操作。

### 1.1. 核心設計原則

-   **專案隔離 (Project Isolation)**: 這是系統的**核心安全機制**。所有核心資料 (人員、照片、檔案、搜尋歷史等) 都與 `project_id` 嚴格綁定。所有 API 端點都會驗證 `project_id`，確保使用者只能存取其所屬專案的資料。
-   **配置驅動 (Configuration-Driven)**: 系統的關鍵參數 (例如：檔案上傳大小、分頁筆數、AI 模型名稱等) 都透過 `appsettings.json` 進行配置，並由 `ConfigurationService` 統一管理，提高了系統的靈活性與可維護性。
-   **共用基礎建設 (Shared Infrastructure)**: `BaseController` 提供了所有控制器的共通功能，包含統一的**日誌記錄**、**參數驗證**、**標準化 API 回應格式**和**例外處理**，遵循了 DRY (Don't Repeat Yourself) 原則。
-   **背景任務處理 (Background Task Processing)**: 對於耗時的操作 (例如：AI 關係分析)，系統採用 `BackgroundService` 在背景執行，避免阻塞 API 請求，提升了系統的回應速度與使用者體驗。
-   **詳細日誌記錄 (Verbose Logging)**: 整個後端系統整合了 **Serilog**，對所有關鍵操作、API 請求、背景任務和錯誤都進行了詳細的日誌記錄，便於系統監控、除錯與問題追蹤。

## 2. 核心功能模組

### 2.1. 人員資料管理 (`PersonDataController`)

這是後端系統最核心的模組，負責管理所有人員的資料。它整合了舊有的 `PersonController`，提供了統一且強大的功能。

-   **API 端點**: `api/PersonData`
-   **核心功能**:
    -   **完整的 CRUD 操作**: 提供人員資料的建立 (`POST`)、讀取 (`GET`)、更新 (`PUT`)、刪除 (`DELETE`)。
    -   **強大的查詢功能**:
        -   **分頁查詢**: `GET /?page=1&pageSize=20`
        -   **多欄位搜尋**: `GET /search?query={keyword}`，可搜尋姓名、身分證、手機、Email 等多個欄位。
        -   **排序**: 可根據 `id`, `name`, `birthday` 等欄位進行排序。
    -   **資料統計**: `GET /statistics`，提供專案內人員的詳細統計資訊 (例如：男女比例、有生日的比例等)。
-   **資料表**: `person_profile`

### 2.2. 檔案上傳與處理 (`FileUploadController`, `FileUploadService`, `ExcelProcessingService`)

這個模組提供了從檔案上傳到資料匯入的完整自動化流程。

-   **API 端點**: `api/FileUpload`
-   **核心功能**:
    -   **Excel 檔案上傳**: 支援 `.xls` 和 `.xlsx` 格式的檔案上傳。
    -   **MD5 重複檢測**: 在同一個專案內，系統會透過 MD5 雜湊值防止內容完全相同的檔案被重複上傳。
    -   **自動化資料匯入**:
        1.  檔案上傳成功後，系統會自動呼叫 `ExcelProcessingService`。
        2.  服務會根據 `field_mapping` 資料表的設定，動態解析 Excel 中的欄位。
        3.  解析後的資料會自動轉換並批次寫入 `person_profile` 資料表。
        4.  每筆匯入的資料都會記錄來源檔案的 MD5，方便追溯。
    -   **安全的刪除機制**: `DELETE /{id}`，刪除檔案時，會一併刪除實體檔案、資料庫記錄，以及所有由該檔案匯入的人員資料。
    -   **刪除影響分析**: `GET /{id}/impact`，在刪除前提供分析，告知使用者該操作會影響多少筆人員資料。
-   **資料表**: `user_update_file`, `person_profile`, `field_mapping`
-   **檔案儲存位置**: `user_upload/`

### 2.3. 照片管理 (`PhotoUploadController`, `PhotoUploadService`)

提供了強大且靈活的照片管理功能，專為多專案環境設計。

-   **API 端點**: `api/PhotoUpload`
-   **核心功能**:
    -   **多格式支援**:
        -   **單張圖片**: 支援 `.jpg`, `.jpeg`, `.png`。
        -   **壓縮檔**: 支援 `.zip`, `.7z`，後端會自動解壓縮並匯入其中的所有圖片。
    -   **專案分離儲存**: 照片會根據 `projectId` 儲存在獨立的資料夾下 (`photos/{projectId}/`)。
    -   **檔名處理**:
        -   **唯一化**: 自動處理重複檔名 (例如：`photo-1.jpg`)。
        -   **正規化**: 純數字檔名會自動補零至六位數 (例如：`123.jpg` -> `000123.jpg`)。
    -   **MD5 重複檢測**: 防止在同專案內上傳內容相同的照片。
    -   **多樣化的查詢 API**:
        -   `GET /list`: 查詢專案的照片列表。
        -   `GET /{id}/file`: 根據 ID 獲取照片。
        -   `GET /photo-by-index/{photoIndex}`: 根據索引號 (例如：`0001`) 獲取照片。
        -   `GET /file/{filename}`: 根據檔名獲取照片。
-   **資料表**: `photos`
-   **檔案儲存位置**: `photos/`

### 2.4. AI 關係分析 (`AnalysisController`, `AIService`, `AnalysisBackgroundService`)

這是後端系統的智慧核心，能夠在背景自動化地分析與建立人員之間的關係網絡。

-   **API 端點**: `api/Analysis`
-   **核心功能**:
    -   **背景遞迴分析**:
        1.  使用者透過 `POST /start/{personId}` 觸發分析。
        2.  `AnalysisBackgroundService` 會以指定人員為起點，開始在背景進行遞迴分析。
        3.  服務會讀取人員資料中的 `family_relationships`, `friends`, `activities` 等文字欄位。
        4.  呼叫 `AIService` 將文字內容傳送給 **OpenAI GPT-4o-mini** 模型，要求其解析出人名與關係。
        5.  根據 AI 的回應，在 `relationship_layers` 表中建立關係連結。
        6.  持續遞迴分析新發現的人員，直到達到設定的最大深度 (`maxDepth`)。
    -   **任務管理**:
        -   `POST /stop/{personId}`: 終止正在進行的分析任務。
        -   `GET /progress/{personId}`: 查詢分析進度。
        -   `GET /jobs`: 獲取所有分析工作的列表。
    -   **容錯機制**: 如果 AI 模型失效或回應格式錯誤，`AIService` 會自動降級，採用簡單的規則式解析，確保系統的基本功能不受影響。
-   **資料表**: `relationship_layers`
-   **AI 模板**: `AI/Templates/`

### 2.5. 關係圖譜 (`RelationshipGraphController`)

為前端的關係圖譜視覺化提供結構化的資料。

-   **API 端點**: `api/RelationshipGraph`
-   **核心功能**:
    -   **多維度關係整合**:
        -   整合從 `person_profile` 的文字欄位中解析出的**家族關係**和**朋友關係**。
        -   整合從 `relationship_layers` 表中讀取的**手動建立的關係**和 **AI 分析出的關係**。
    -   **動態圖譜生成**:
        -   `POST /analyze-all`: 分析專案內所有人員的關係。
        -   `POST /analyze-selected`: 只分析指定人員及其 N 度以內的關聯。
    -   **結構化輸出**: 回傳前端 G6 圖引擎所需的 `nodes` (節點) 和 `links` (連結) JSON 格式。

### 2.6. 全文檢索 (`FullTextSearchController`)

提供企業級的全文檢索功能，並整合了多種搜尋輔助與分析工具。

-   **API 端點**: `api/FullTextSearch`
-   **核心功能**:
    -   **專案隔離搜尋**: 所有搜尋都嚴格限制在指定的 `project_id` 內。
    -   **多模式搜尋**:
        -   **精準搜尋 (`exact`)**: 完全匹配。
        -   **模糊搜尋 (`fuzzy`)**: 使用 `ILIKE` 進行模糊匹配。
    -   **豐富的輔助功能**:
        -   **熱門關鍵字**: `GET /popular-keywords`
        -   **搜尋歷史**: `GET /search-history`
        -   **收藏狀態**: 搜尋結果會標示出已收藏的人員。
    -   **搜尋分析**:
        -   在 `search_logs` 表中記錄詳細的搜尋活動。
        -   提供 `GET /statistics` API 來獲取搜尋統計資料。
-   **資料表**: `person_profile`, `search_keywords`, `search_logs`, `user_favorites`

## 3. 資料庫設計

-   **核心資料表**:
    -   `person_profile`: 儲存所有人員的詳細資料。
    -   `user_update_file`: 記錄檔案上傳歷史。
    -   `photos`: 記錄照片的元資料。
    -   `relationship_layers`: 儲存人員之間的關係連結。
    
-   **輔助資料表**:
    -   `field_mapping`: Excel 欄位與資料庫欄位的對應關係。
    -   `search_keywords`, `search_logs`, `user_favorites`: 全文檢索相關功能。

詳細的 Schema 請參考 `database_schema.sql`。

## 4. API 端點總覽

-   `api/PersonData`: 人員資料管理 (CRUD, 搜尋, 統計)
-   `api/FileUpload`: 檔案上傳與處理
-   `api/PhotoUpload`: 照片上傳與管理
-   `api/Analysis`: AI 背景分析任務管理
-   `api/RelationshipGraph`: 關係圖譜資料生成
-   `api/FullTextSearch`: 全文檢索
-   `api/Favorites`: 收藏功能
-   `api/Project`: 專案管理
-   `api/Log`: 日誌查詢

-   `api/VisualAnalysis`: 視覺化分析

---
*文件最後更新時間: {CURRENT_DATE}* 