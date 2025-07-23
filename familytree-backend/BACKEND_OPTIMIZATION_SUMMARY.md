# 後端代碼優化總結

## 概述

本次優化專注於移除硬編碼邏輯、改善物件導向設計、提高代碼可讀性，並加入詳細的中文註解。

## 主要優化項目

### 1. 建立應用程式常數管理 (`ApplicationConstants.cs`)

**目的：**移除散佈在各處的硬編碼值，集中管理所有常數

**改善內容：**
- **檔案相關常數**：支援的 MIME 類型、檔案副檔名、大小限制、狀態定義
- **資料庫相關常數**：分頁設定、欄位長度限制、專案 ID 格式
- **API 回應常數**：標準成功/錯誤訊息
- **Excel 處理常數**：工作表索引、資料開始行數、最大處理行數
- **日誌常數**：檔案名稱格式、時間格式
- **搜尋常數**：搜尋類型、結果限制
- **關係圖譜常數**：分析深度、節點限制

**設計理念：**
```csharp
// 舊做法：硬編碼
if (pageSize > 100) pageSize = 20;

// 新做法：使用常數
if (pageSize > ApplicationConstants.Database.MaxPageSize) 
    pageSize = ApplicationConstants.Database.DefaultPageSize;
```

### 2. 配置管理服務 (`ConfigurationService.cs`)

**目的：**統一管理應用程式配置，提供型別安全的配置存取

**核心功能：**
- **強型別配置模型**：`FileUploadConfiguration`、`PaginationConfiguration`、`LoggingConfiguration`、`SearchConfiguration`
- **環境適應性**：根據開發/生產環境動態調整設定
- **設定驗證**：確保必要配置項目存在
- **預設值處理**：當配置缺失時使用合理預設值

**設計模式：**
```csharp
// 採用依賴注入模式，便於單元測試
public interface IConfigurationService
{
    FileUploadConfiguration GetFileUploadConfiguration();
    PaginationConfiguration GetPaginationConfiguration();
    // ...
}
```

### 3. 基礎控制器 (`BaseController.cs`)

**目的：**提供所有控制器的通用功能，遵循 DRY 原則

**統一功能：**
- **專案參數驗證**：統一的專案 ID 格式檢查和隔離邏輯
- **分頁參數處理**：自動正規化頁碼和頁面大小
- **回應格式化**：標準化的成功、錯誤、分頁回應格式
- **例外處理**：統一的錯誤捕獲和日誌記錄
- **日誌輔助**：統一的請求開始/完成日誌格式

**設計優勢：**
```csharp
// 所有控制器都能使用統一的回應格式
protected IActionResult CreateSuccessResponse<T>(T data, string? message = null)
protected IActionResult CreateErrorResponse(string message, object? details = null)
protected IActionResult CreatePagedResponse<T>(...)
```

### 4. FileUploadController 重構

**主要改善：**
- **繼承 BaseController**：獲得統一的錯誤處理和回應格式
- **配置驅動驗證**：使用 `FileUploadConfiguration` 進行檔案驗證
- **詳細日誌記錄**：每個步驟都有清楚的日誌追蹤
- **統一例外處理**：使用 `ExecuteWithExceptionHandling` 方法

**改善對比：**
```csharp
// 舊做法：硬編碼檔案大小檢查
if (file.Length > 50 * 1024 * 1024)
{
    return BadRequest("檔案太大");
}

// 新做法：使用配置和統一錯誤處理
if (file.Length > _fileUploadConfig.MaxFileSizeBytes)
{
    Logger.LogWarning("檔案驗證失敗：檔案大小超過限制 ({FileSize} > {MaxSize})", 
        file.Length, _fileUploadConfig.MaxFileSizeBytes);
    return CreateErrorResponse(ApplicationConstants.ApiResponse.ErrorMessages.FileSizeExceeded);
}
```

### 5. PersonController 重構

**核心改善：**
- **移除硬編碼日誌**：使用標準 ILogger 和 BaseController 的日誌方法
- **統一 SQL 建構**：將 SQL 查詢邏輯封裝成私有方法
- **配置化分頁**：使用 `PaginationConfiguration` 管理分頁參數
- **完整的 CRUD 驗證**：每個操作都有詳細的參數和業務邏輯驗證

**SQL 建構改善：**
```csharp
// 舊做法：重複的 SQL 字串
var sql = "SELECT id, name, ... FROM person_profile WHERE ...";

// 新做法：統一的 SQL 建構方法
private static string BuildPersonSelectQuery(string? whereClause = null, int? offset = null, int? limit = null)
{
    var sql = @"SELECT id, name, ... FROM person_profile";
    // 統一處理 WHERE、ORDER BY、LIMIT、OFFSET
    return sql;
}
```

### 6. PersonDataController 重構

**進階功能：**
- **多欄位搜尋**：支援姓名、身分證、手機、電子郵件等多欄位搜尋
- **排序功能**：支援多種欄位的昇序/降序排列
- **統計功能**：提供專案內人員資料的詳細統計資訊
- **進階搜尋**：涵蓋更多欄位的全文檢索功能

**搜尋條件建構：**
```csharp
// 使用建構者模式建立複雜查詢條件
private static (string whereClause, DynamicParameters parameters) BuildAdvancedSearchConditions(
    string? projectId, 
    string keyword)
{
    var conditions = new List<string>();
    var parameters = new DynamicParameters();
    
    // 專案隔離 + 多欄位搜尋
    conditions.Add("project_id = @project_id");
    conditions.Add("(name ILIKE @keyword OR id_number ILIKE @keyword OR ...)");
    
    return (string.Join(" AND ", conditions), parameters);
}
```

## 配置檔案優化

### appsettings.json 新增配置項目

```json
{
  "FileUpload": {
    "MaxFileSizeBytes": 52428800,
    "AllowOverwrite": false
  },
  "Pagination": {
    "DefaultPageSize": 20,
    "MaxPageSize": 100
  },
  "Search": {
    "DefaultPageSize": 10,
    "MaxResults": 1000,
    "TimeoutSeconds": 30,
    "EnableFuzzySearch": true
  },
  "Logging": {
    "EnableVerboseLogging": false,
    "RetentionDays": 30
  }
}
```

## 物件導向設計改善

### 1. 單一職責原則 (SRP)
- **ConfigurationService**：專責配置管理
- **BaseController**：專責通用控制器功能
- **ApplicationConstants**：專責常數定義

### 2. 開放封閉原則 (OCP)
- 透過配置檔案擴展功能，無需修改代碼
- 介面設計便於功能擴展

### 3. 依賴反轉原則 (DIP)
- 控制器依賴 `IConfigurationService` 介面，而非具體實作
- 便於單元測試和模擬

### 4. 不重複原則 (DRY)
- 統一的回應格式、錯誤處理、參數驗證
- 共用的 SQL 建構邏輯

## 可讀性提升

### 1. 有意義的變數命名
```csharp
// 舊做法
var data = await connection.QueryAsync<PersonDataModel>(sql, new { id });

// 新做法
var personData = await connection.QueryFirstOrDefaultAsync<PersonDataModel>(sql, new { id, project_id });
```

### 2. 邏輯拆分與結構清晰
```csharp
public async Task<IActionResult> GetPersons(...)
{
    return await ExecuteWithExceptionHandling(async () =>
    {
        // 步驟 1：驗證專案 ID
        var projectValidationResult = ValidateProjectId(project_id, allowNull: false);
        
        // 步驟 2：正規化分頁參數
        var (normalizedPage, normalizedPageSize) = ValidateAndNormalizePagination(page, pageSize);
        
        // 步驟 3：建立資料庫連接
        // 步驟 4：建構查詢條件
        // 步驟 5：執行查詢
        // 步驟 6：建立回應
        
    }, "獲取人員列表");
}
```

### 3. 詳細的中文註解
每個關鍵邏輯都有詳細的中文註解說明：
- **設計理念**：說明為什麼這樣設計
- **職責說明**：明確每個類別和方法的職責
- **參數說明**：詳細的參數用途和格式
- **實作考量**：解釋特殊處理的原因

## 效益總結

### 1. 維護性提升
- **集中化管理**：常數和配置統一管理，修改時只需改一處
- **統一邏輯**：通用功能在 BaseController 統一實作
- **清楚的代碼結構**：每個檔案和方法職責明確

### 2. 擴展性增強
- **配置驅動**：新增功能時優先考慮配置化
- **介面設計**：便於替換實作和功能擴展
- **模組化架構**：各層次分離，便於獨立測試和部署

### 3. 可靠性提高
- **統一錯誤處理**：減少遺漏的錯誤情況
- **參數驗證**：防止無效數據進入系統
- **詳細日誌**：便於問題追蹤和除錯

### 4. 開發效率改善
- **代碼重用**：BaseController 提供可重用的通用功能
- **清楚的註解**：新開發者容易理解代碼意圖
- **標準化流程**：統一的開發模式和最佳實務

## 未來改善方向

1. **增加單元測試**：為新的架構增加完整的測試覆蓋
2. **效能監控**：加入 APM 工具監控系統效能
3. **快取策略**：對頻繁查詢的資料加入快取機制
4. **API 文件**：使用 Swagger 產生完整的 API 文件
5. **安全強化**：加入 JWT 驗證和角色權限控制 