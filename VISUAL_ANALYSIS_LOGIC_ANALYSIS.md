# 視覺分析邏輯對比分析報告

## 用戶需求分析

### 1. 用戶按下視覺分析後的通知機制
**需求**：當用戶按下視覺分析之後會跳出一個滑動訊息匡通知用戶已開始進行這個人的視覺分析工程。

**現有實現**：
✅ **已實現** - 在 `family-tree.ts` 的 `startVisualAnalysis()` 方法中：
```typescript
this.showNotification('視覺分析已開始，正在背景處理中...', 'success');
```

### 2. 背景工作建立機制
**需求**：系統會建立一個背景工作，必須要具體從一個人開始，這個人在資料庫的PK會變成唯一識別ID，用來避免重複對同一個人提出分析的請求。

**現有實現**：
✅ **已實現** - 在 `AnalysisBackgroundService.cs` 中：
- 使用 `Dictionary<int, RecursiveAnalysisTask> _activeTasks` 來追蹤活躍任務
- 在 `StartAnalysis()` 方法中檢查是否已有相同人員的進行中任務
- 資料庫層面有唯一約束：`CREATE UNIQUE INDEX idx_analysis_results_person_unique ON analysis_results(person_id) WHERE status IN ('pending', 'processing')`

### 3. 資料查詢邏輯
**需求**：從這個人的資料庫中分別查詢、親屬關係、參與活動、重要友人這些欄位。

**現有實現**：
⚠️ **部分實現** - 在 `RecursiveAnalysisTask.cs` 的 `GetPersonData()` 方法中：
```csharp
public string? family_relationships { get; set; }
public string? friends { get; set; }
```
❌ **缺少**：參與活動欄位的查詢

### 4. OpenAI 4o Mini 模型整合
**需求**：將上述查出來的資料，逐一丟向openai 4omini模型進行提問，提問的相關提示詞語function call模板可以從AI目錄底下的設定檔取得，目的是要取得與此人有關的所有關係人。

**現有實現**：
❌ **未實現** - 現有代碼中：
- `AIService.cs` 只有簡單的文字解析邏輯，沒有實際調用 OpenAI API
- 有模板文件：`extract_name_relation_prompt.json` 和 `extract_name_relation_function.json`
- 但沒有實際的 OpenAI 4o Mini 模型調用邏輯

### 5. 資料庫儲存結構
**需求**：在資料庫中建立一個新的資料表來儲存一個人與其他人的關係，這個表會有1、用戶ID，圖普分析結果(JSON)，分析日期。JSON中會以陣列保存多筆以下欄位：1、與對象之關係，4、對象的名字，5、對象在資料庫中的ID(如果這個人不在資料庫中就空白)。

**現有實現**：
✅ **已實現** - `analysis_results` 表結構：
```sql
CREATE TABLE analysis_results (
    id SERIAL PRIMARY KEY,
    person_id INTEGER NOT NULL,
    analysis_result JSONB NOT NULL,
    analysis_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    progress_percentage INTEGER DEFAULT 0,
    status VARCHAR(50) DEFAULT 'pending',
    current_step VARCHAR(255),
    status_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

⚠️ **部分符合** - JSON 結構在 `RelationshipData` 類中定義：
```csharp
public class RelationshipData
{
    public string Relation { get; set; } = "";
    public string TargetName { get; set; } = "";
    public int? TargetId { get; set; }
    public string SourceField { get; set; } = "";
}
```

### 6. 進度查詢機制
**需求**：由於分析過程涉及多次ai訪問與多次資料庫操作，因此這個背景工作要提供百分比的進度查詢，會呈現在介面上讓用戶知道進度。

**現有實現**：
✅ **已實現** - 完整的進度查詢機制：
- 後端：`GetAnalysisProgress()` 方法提供進度查詢
- 前端：`monitorAnalysisProgress()` 方法每3秒查詢一次進度
- 進度顯示：`updateProgressDisplay()` 方法更新UI

## 主要缺失項目

### 1. OpenAI 4o Mini 模型整合
**問題**：現有代碼沒有實際調用 OpenAI API
**需要實現**：
- 在 `AIService.cs` 中添加 OpenAI 4o Mini 模型調用邏輯
- 使用現有的模板文件進行 function calling
- 處理 API 響應和錯誤

### 2. 參與活動欄位查詢
**問題**：缺少參與活動資料的查詢
**需要實現**：
- 在 `PersonData` 類中添加活動欄位
- 在資料庫查詢中包含活動資料
- 在分析邏輯中處理活動資料

### 3. 完整的關係分析流程
**問題**：現有的 `ExtractNameRelationsAsync` 方法只是簡單的文字解析
**需要實現**：
- 將文字資料發送給 OpenAI 進行智能分析
- 使用 function calling 來結構化提取關係
- 處理多種關係類型的識別

## 建議的實現方案

### 1. 完善 AIService
```csharp
public async Task<List<NameRelationPair>> ExtractNameRelationsWithAIAsync(string textContent)
{
    // 調用 OpenAI 4o Mini 模型
    // 使用 function calling 提取關係
    // 返回結構化的關係數據
}
```

### 2. 擴展資料查詢
```csharp
public class PersonData
{
    public int Id { get; set; }
    public string? family_relationships { get; set; }
    public string? friends { get; set; }
    public string? activities { get; set; } // 新增活動欄位
}
```

### 3. 完善分析流程
```csharp
private async Task<List<RelationshipData>> AnalyzePersonRelationships(PersonData personData)
{
    var relationships = new List<RelationshipData>();
    
    // 分析家族關係
    if (!string.IsNullOrEmpty(personData.family_relationships))
    {
        var familyRelations = await _aiService.ExtractNameRelationsWithAIAsync(personData.family_relationships);
        // 處理結果...
    }
    
    // 分析朋友關係
    if (!string.IsNullOrEmpty(personData.friends))
    {
        var friendRelations = await _aiService.ExtractNameRelationsWithAIAsync(personData.friends);
        // 處理結果...
    }
    
    // 分析活動關係
    if (!string.IsNullOrEmpty(personData.activities))
    {
        var activityRelations = await _aiService.ExtractNameRelationsWithAIAsync(personData.activities);
        // 處理結果...
    }
    
    return relationships;
}
```

## 總結

現有代碼已經實現了大部分基礎架構，包括：
- ✅ 通知機制
- ✅ 背景工作管理
- ✅ 進度查詢
- ✅ 資料庫儲存結構

主要缺失的是：
- ❌ OpenAI 4o Mini 模型整合
- ❌ 參與活動資料查詢
- ❌ 智能關係分析邏輯

需要重點完善 AI 服務的整合和資料查詢的完整性。 