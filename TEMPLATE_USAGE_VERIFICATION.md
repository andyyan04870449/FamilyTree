# 提示模板使用驗證報告

## ✅ **模板使用情況檢查**

### 1. **提示詞模板 (`extract_name_relation_prompt.json`)**

**✅ 正確使用**：
- 模板被 `LoadPromptTemplateAsync("extract_name_relation")` 載入
- 系統消息被正確提取：`promptTemplate["system_message"]?["content"]`
- 用戶消息模板被正確處理：`promptTemplate["user_message_template"]?["content"]?.Replace("{text_content}", textContent)`

**📝 模板內容**：
```json
{
  "system_message": {
    "role": "system",
    "content": "你是一個擅長資訊抽取的助理。請從使用者提供的文字中，辨識所有出現的「人名」與其對應的「關係」。\n\n你需要使用 extract_name_relation 函數來返回結果，該函數會自動格式化輸出。\n\n⚠️ 請注意：\n- 使用提供的函數來返回結果\n- 忽略與人名無關的資訊\n- 若同一人名關係出現多次，只保留一次\n- 確保人名和關係的準確性"
  },
  "user_message_template": {
    "role": "user",
    "content": "{text_content}"
  }
}
```

### 2. **函數定義模板 (`extract_name_relation_function.json`)**

**✅ 正確使用**：
- 模板被 `LoadFunctionSchemaAsync("extract_name_relation")` 載入
- 函數名稱被正確提取：`functionSchema["name"]`
- 函數描述被正確提取：`functionSchema["description"]`
- 參數定義被正確序列化：`BinaryData.FromObjectAsJson(functionSchema["parameters"])`

**📝 模板內容**：
```json
{
  "name": "extract_name_relation",
  "description": "根據提供的文字，抽取其中所有的人名與其關係。支持多種關係類型，包括家族關係、朋友關係、工作關係等。",
  "parameters": {
    "type": "object",
    "properties": {
      "pairs": {
        "type": "array",
        "description": "人名與關係的配對列表",
        "items": {
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "description": "人物的完整姓名"
            },
            "relation": {
              "type": "string",
              "description": "此人與文本中主角或敘述者的關係，例如：父親、母親、兒子、女兒、朋友、同事、同學等"
            }
          },
          "required": ["name", "relation"]
        }
      }
    },
    "required": ["pairs"]
  }
}
```

## 🔧 **代碼中的使用流程**

### 1. **模板載入**
```csharp
// 載入提示詞模板
var promptTemplate = await LoadPromptTemplateAsync("extract_name_relation");

// 載入函數定義
var functionSchema = await LoadFunctionSchemaAsync("extract_name_relation");
```

### 2. **消息準備**
```csharp
// 準備系統消息和用戶消息
var systemMessage = promptTemplate["system_message"]?["content"]?.ToString() ?? "";
var userMessage = promptTemplate["user_message_template"]?["content"]?.ToString()?.Replace("{text_content}", textContent) ?? textContent;

var messages = new List<ChatMessage>
{
    new ChatMessage(ChatRole.System, systemMessage),
    new ChatMessage(ChatRole.User, userMessage)
};
```

### 3. **函數定義**
```csharp
// 準備函數調用選項
var functionCall = new ChatFunctionCall("extract_name_relation");
var functionDefinition = new ChatFunctionDefinition
{
    Name = functionSchema["name"]?.ToString() ?? "extract_name_relation",
    Description = functionSchema["description"]?.ToString() ?? "根據提供的文字，抽取其中所有的人名與其關係",
    Parameters = BinaryData.FromObjectAsJson(functionSchema["parameters"])
};
```

### 4. **結果處理**
```csharp
// 反序列化 AI 返回的結果
var result = JsonSerializer.Deserialize<ExtractNameRelationResult>(arguments);
return result?.Pairs ?? new List<NameRelationPair>();
```

## 🧪 **測試建議**

### 1. **測試不同類型的關係文字**

**家族關係測試**：
```
母：邱還真
父：王先生
妹妹，李惠如
```

**朋友關係測試**：
```
羅亞瑟，淡江大學同學
周雅，律理法律資訊有限公司
```

**活動關係測試**：
```
20230423(台)鳳之韻新住民旗袍關懷協會餐敘，邀請趙馥樂任藝術講師
棒球社團，中國總商會青年會副會長張三
```

### 2. **驗證 AI 輸出格式**

AI 應該返回如下格式：
```json
{
  "pairs": [
    {
      "name": "邱還真",
      "relation": "母親"
    },
    {
      "name": "王先生",
      "relation": "父親"
    },
    {
      "name": "李惠如",
      "relation": "妹妹"
    }
  ]
}
```

### 3. **檢查日誌輸出**

啟動應用後，檢查日誌中是否有：
- ✅ "OpenAI 客戶端已初始化"
- ✅ "發送請求到 OpenAI 4o Mini 模型..."
- ✅ "AI 解析結果: X 個關係配對"
- ✅ "AI 解析完成"

## 🔍 **潛在問題和解決方案**

### 1. **API Key 配置問題**
**症狀**：日誌顯示 "OpenAI API Key 未配置，將使用簡單解析模式"
**解決**：檢查 `appsettings.Development.json` 中的 API Key 配置

### 2. **模板文件路徑問題**
**症狀**：拋出 `FileNotFoundException`
**解決**：確認 `AI/Templates/` 目錄下的模板文件存在

### 3. **函數調用失敗**
**症狀**：日誌顯示 "AI 模型未返回函數調用結果，回退到簡單解析"
**解決**：檢查 API Key 是否有效，網路連接是否正常

### 4. **反序列化錯誤**
**症狀**：拋出 JSON 反序列化異常
**解決**：檢查 AI 返回的 JSON 格式是否符合預期

## 📊 **預期效果**

使用正確配置的模板後，系統應該能夠：

1. **智能識別關係**：比簡單文字解析更準確地識別人名和關係
2. **處理複雜格式**：處理各種不同格式的關係描述
3. **結構化輸出**：返回標準化的關係數據結構
4. **錯誤回退**：在 AI 服務不可用時自動回退到簡單解析

## 🎯 **下一步測試**

1. **啟動應用**：確保後端服務正常運行
2. **配置 API Key**：在配置文件中設置有效的 OpenAI API Key
3. **觸發分析**：在介面中點擊「視覺分析」按鈕
4. **檢查日誌**：觀察 AI 調用和結果處理的日誌
5. **驗證結果**：確認分析結果的準確性和完整性 