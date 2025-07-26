# 視覺化分析關聯遷移指南

## 📋 概覽
此遷移為 `relationship_layers` 表添加 `visual_analysis_graph_id` 欄位，將關聯資料與視覺化分析圖表關聯起來。

## 🎯 目標
- 將關聯資料歸屬到特定的視覺化分析圖表
- 將現有的關聯資料都關聯到名為"視覺化圖表分析"的預設圖表
- 支援在視覺化分析編輯器中建立新的關聯

## 📊 資料庫變更

### 1. relationship_layers 表變更
```sql
-- 添加新欄位
ALTER TABLE relationship_layers 
ADD COLUMN visual_analysis_graph_id INTEGER;

-- 添加外鍵約束
ALTER TABLE relationship_layers 
ADD CONSTRAINT fk_relationship_layers_visual_analysis_graph
FOREIGN KEY (visual_analysis_graph_id) 
REFERENCES visual_analysis_graphs(id) 
ON DELETE SET NULL;

-- 創建索引
CREATE INDEX idx_relationship_layers_visual_analysis_graph_id 
ON relationship_layers(visual_analysis_graph_id);
```

### 2. 資料遷移
```sql
-- 插入預設的視覺化圖表分析記錄
INSERT INTO visual_analysis_graphs (name, project_ids, updated_by, updated_at) 
VALUES ('視覺化圖表分析', '', 'system', CURRENT_TIMESTAMP)
ON CONFLICT (name) DO NOTHING;

-- 將所有現有關聯資料關聯到預設圖表
UPDATE relationship_layers 
SET visual_analysis_graph_id = (
    SELECT id FROM visual_analysis_graphs 
    WHERE name = '視覺化圖表分析' 
    LIMIT 1
)
WHERE visual_analysis_graph_id IS NULL;
```

## 🔧 後端程式碼變更

### 1. RelationshipGraphController.cs
#### CreateRelationshipRequest 模型
```csharp
public class CreateRelationshipRequest
{
    public int SourcePersonId { get; set; }
    public int TargetPersonId { get; set; }
    public string RelationshipType { get; set; } = "";
    public int? VisualAnalysisGraphId { get; set; } // 新增欄位
}
```

#### INSERT 語句更新
```csharp
var insertSql = @"
    INSERT INTO relationship_layers 
    (source_person_id, target_person_id, relation_type, source_field, layer_depth, analysis_session_id, visual_analysis_graph_id)
    VALUES (@SourceId, @TargetId, @Type, 'manual', 1, @SessionId, @VisualAnalysisGraphId)";
```

## 🌐 前端程式碼變更

### 1. relationship-graph.service.ts
```typescript
export interface CreateRelationshipRequest {
  sourcePersonId: number;
  targetPersonId: number;
  relationshipType: string;
  visualAnalysisGraphId?: number; // 新增欄位
}
```

### 2. visual-analysis.service.ts
```typescript
// 新增介面
export interface CreateVisualAnalysisRelationshipRequest {
  sourcePersonId: number;
  targetPersonId: number;
  relationshipType: string;
  visualAnalysisGraphId: number;
}

// 新增方法
createRelationship(request: CreateVisualAnalysisRelationshipRequest): Observable<VisualAnalysisApiResponse> {
  return this.http.post<VisualAnalysisApiResponse>(`${AppConstants.API_BASE_URL}/RelationshipGraph/create-relationship`, request);
}
```

### 3. visual-analysis-editor.page.ts
```typescript
// 更新 confirmRelationshipCreation 方法
confirmRelationshipCreation(): void {
  const request: CreateVisualAnalysisRelationshipRequest = {
    sourcePersonId: parseInt(this.firstSelectedNode.id),
    targetPersonId: parseInt(this.secondSelectedNode.id),
    relationshipType: this.relationshipType.trim(),
    visualAnalysisGraphId: this.graphId // 關聯到當前圖表
  };

  this.visualAnalysisService.createRelationship(request).subscribe({
    // ... 處理回應
  });
}
```

## 🚀 執行遷移

### 1. 執行資料庫遷移
```bash
cd familytree-backend
./run-migration.sh
```

### 2. 重新啟動後端服務
```bash
cd familytree-backend
dotnet run
```

### 3. 重新啟動前端服務
```bash
cd familytree-frontend
ng serve --port 53101
```

## ✅ 驗證遷移

### 1. 檢查資料庫結構
```sql
\d relationship_layers
```

### 2. 檢查現有資料
```sql
SELECT COUNT(*) as total_relationships, 
       COUNT(visual_analysis_graph_id) as updated_relationships 
FROM relationship_layers;
```

### 3. 檢查預設圖表
```sql
SELECT id, name FROM visual_analysis_graphs 
WHERE name = '視覺化圖表分析';
```

## 📈 功能測試

### 1. 視覺化分析編輯器
1. 開啟視覺化分析編輯器
2. 點擊節點選單中的"建立關係"
3. 選擇第二個節點
4. 輸入關係類型並確認
5. 檢查是否成功建立關係

### 2. 資料庫驗證
```sql
SELECT rl.*, vag.name as graph_name 
FROM relationship_layers rl
LEFT JOIN visual_analysis_graphs vag ON rl.visual_analysis_graph_id = vag.id
ORDER BY rl.id DESC
LIMIT 5;
```

## 📝 注意事項
- `visual_analysis_graph_id` 欄位設為可為 NULL，確保向後相容性
- 所有現有關聯資料會自動關聯到"視覺化圖表分析"預設圖表
- 新建立的關聯會根據當前編輯的圖表ID進行關聯
- 外鍵約束設為 `ON DELETE SET NULL`，刪除圖表時不會影響關聯資料

## 🔄 回滾計劃
如需回滾此遷移：
```sql
-- 移除外鍵約束
ALTER TABLE relationship_layers 
DROP CONSTRAINT IF EXISTS fk_relationship_layers_visual_analysis_graph;

-- 移除索引
DROP INDEX IF EXISTS idx_relationship_layers_visual_analysis_graph_id;

-- 移除欄位
ALTER TABLE relationship_layers 
DROP COLUMN IF EXISTS visual_analysis_graph_id;
``` 