# AI 回應格式標準化

## 📋 修改概述

為了簡化 AI 回應解析邏輯並提高系統穩定性，我們將 AI 回應格式標準化為單一的函數調用格式。

## 🔄 修改內容

### 1. AI 提示詞模板修改

**檔案**: `AI/Templates/extract_name_relation_prompt.json`

**修改前**:
```json
{
  "system_message": {
    "role": "system",
    "content": "你是一個擅長資訊抽取的助理。請從使用者提供的文字中，辨識所有出現的「人名」與其對應的「關係」。\n\n你可以使用以下兩種格式之一來返回結果：\n\n1. JSON 數組格式：\n```json\n[\n  {\"name\": \"人名\", \"relation\": \"關係\"},\n  {\"name\": \"人名2\", \"relation\": \"關係2\"}\n]\n```\n\n2. 函數調用格式：\n```\nextract_name_relation([(\"人名\", \"關係\"), (\"人名2\", \"關係2\")])\n```\n\n⚠️ 請注意：\n- 你可以選擇使用上述兩種格式中的任意一種\n- 忽略與人名無關的資訊\n- 若同一人名關係出現多次，只保留一次\n- 確保人名和關係的準確性\n- 如果沒有找到任何人名關係，返回空數組"
  }
}
```

**修改後**:
```json
{
  "system_message": {
    "role": "system",
    "content": "你是一個擅長資訊抽取的助理。請從使用者提供的文字中，辨識所有出現的「人名」與其對應的「關係」。\n\n你必須使用 extract_name_relation 函數來返回結果，格式如下：\n\nextract_name_relation([(\"人名1\", \"關係1\"), (\"人名2\", \"關係2\"), ...])\n\n⚠️ 重要要求：\n- 必須使用 extract_name_relation 函數調用格式\n- 不要使用 JSON 格式或其他格式\n- 人名用雙引號包圍\n- 關係用雙引號包圍\n- 每個配對用括號包圍\n- 多個配對用逗號分隔\n- 整個列表用方括號包圍\n- 忽略與人名無關的資訊\n- 若同一人名關係出現多次，只保留一次\n- 確保人名和關係的準確性\n- 如果沒有找到任何人名關係，返回：extract_name_relation([])\n\n範例格式：\nextract_name_relation([(\"張三\", \"朋友\"), (\"李四\", \"同事\")])"
  }
}
```

### 2. AIService 解析邏輯簡化

**檔案**: `Services/AIService.cs`

**修改內容**:
- 移除了 `ParseJsonArrayFormat` 方法
- 簡化了 `ExtractNameRelationsWithAIAsync` 方法中的解析邏輯
- 只保留函數調用格式的解析
- 改進了錯誤處理和日誌記錄

**修改前**:
```csharp
// 嘗試解析 JSON 數組格式：[{"name": "名字", "relation": "關係"}, ...]
if (content?.Contains("[") == true && content?.Contains("]") == true)
{
    var pairs = ParseJsonArrayFormat(content);
    // ... 處理邏輯
}

// 嘗試解析函數調用格式：extract_name_relation([("名字", "關係"), ...])
if (content?.Contains("extract_name_relation") == true)
{
    var pairs = ParseFunctionCallFormat(content);
    // ... 處理邏輯
}
```

**修改後**:
```csharp
// 解析函數調用格式：extract_name_relation([("名字", "關係"), ...])
if (content?.Contains("extract_name_relation") == true)
{
    var pairs = ParseFunctionCallFormat(content);
    if (pairs.Count > 0)
    {
        // ... 成功處理邏輯
    }
    else
    {
        // ... 空結果處理邏輯
    }
}
else
{
    throw new Exception("AI 回應格式不符合預期，應包含 extract_name_relation 函數調用");
}
```

## 🎯 標準化格式

### 唯一支持的格式：函數調用格式

```
extract_name_relation([("人名1", "關係1"), ("人名2", "關係2"), ...])
```

**格式規則**:
- 函數名：`extract_name_relation`
- 參數：方括號包圍的元組列表
- 元組格式：`("人名", "關係")`
- 人名和關係都用雙引號包圍
- 多個元組用逗號分隔
- 空結果：`extract_name_relation([])`

**範例**:
```
extract_name_relation([("范統", "父"), ("吳春華", "母")])
extract_name_relation([("張三", "朋友"), ("李四", "同事")])
extract_name_relation([])
```

## ✅ 優勢

1. **簡化解析邏輯**：只需要處理一種格式，減少複雜性
2. **提高穩定性**：避免多種格式解析的潛在錯誤
3. **明確要求**：AI 知道必須使用特定格式
4. **更好的錯誤處理**：當格式不符合預期時，有明確的錯誤訊息
5. **減少維護成本**：只需要維護一套解析邏輯

## 🔧 測試建議

1. **測試正常情況**：確保 AI 回傳正確的函數調用格式
2. **測試空結果**：確保空結果也能正確處理
3. **測試錯誤格式**：確保錯誤格式有適當的錯誤處理
4. **測試特殊字符**：確保包含特殊字符的人名和關係能正確處理

## 📊 預期效果

- AI 回應格式統一為函數調用格式
- 解析成功率提高
- 系統穩定性增強
- 日誌更清晰，便於調試
- 維護成本降低 