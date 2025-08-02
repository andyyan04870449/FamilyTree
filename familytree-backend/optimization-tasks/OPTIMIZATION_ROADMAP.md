# FamilyTree 系統優化路線圖

## 概述
本文檔詳細記錄了 FamilyTree 系統需要進行的優化項目，包括安全性、性能、代碼質量和架構等方面的改進建議。

生成日期：2025-08-02  
優先級說明：🔴 緊急 | 🟡 重要 | 🟢 建議

---

## 🔴 第一階段：關鍵安全修復（第 1-2 週）

### 1.1 CORS 配置安全加固
**問題描述**：當前 CORS 配置允許所有來源，存在嚴重安全風險  
**影響範圍**：整個 API 安全性  
**修復位置**：`/Program.cs` (Line 30-36)  
**修復方案**：
```csharp
// 替換現有的 AllowAll 策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
            "https://familytree.yourdomain.com",
            "https://app.familytree.yourdomain.com"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
```
**負責 Agent**：dotnet-backend-api-developer

### 1.2 JWT 密鑰管理改進
**問題描述**：JWT 密鑰硬編碼在配置文件中  
**影響範圍**：認證安全性  
**修復位置**：`/appsettings.json`, `/Services/TokenService.cs`  
**修復方案**：
1. 使用 Azure Key Vault 或 AWS Secrets Manager
2. 本地開發使用 User Secrets
3. 實施密鑰輪換機制
**負責 Agent**：dotnet-backend-api-developer

### 1.3 強制 HTTPS 和安全標頭
**問題描述**：缺少 HTTPS 重定向和安全標頭  
**影響範圍**：傳輸安全性  
**修復位置**：`/Program.cs`  
**修復方案**：
```csharp
app.UseHttpsRedirection();
app.UseHsts();
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});
```
**負責 Agent**：dotnet-backend-api-developer

### 1.4 更新過期套件
**問題描述**：System.IdentityModel.Tokens.Jwt 7.0.3 存在已知漏洞  
**影響範圍**：依賴項安全性  
**修復位置**：`/familytree-backend.csproj`  
**修復方案**：更新至最新穩定版本  
**負責 Agent**：dotnet-backend-api-developer

---

## 🟡 第二階段：性能優化（第 3-4 週）

### 2.1 資料庫查詢優化
**問題描述**：缺少索引策略，存在 N+1 查詢問題  
**影響範圍**：API 響應時間  
**優化項目**：

#### 2.1.1 建立索引策略
**位置**：Database Scripts  
**需要建立的索引**：
```sql
-- person_data 表索引
CREATE INDEX idx_person_projectid ON person_data(project_id);
CREATE INDEX idx_person_familyname ON person_data(family_name);
CREATE INDEX idx_person_birthdate ON person_data(birth_date);
CREATE INDEX idx_person_fulltext ON person_data USING gin(to_tsvector('chinese', 
    coalesce(family_name,'') || ' ' || 
    coalesce(given_name,'') || ' ' || 
    coalesce(biography,'')
));

-- relationships 表索引
CREATE INDEX idx_relationship_person ON relationships(person_id);
CREATE INDEX idx_relationship_related ON relationships(related_person_id);
CREATE INDEX idx_relationship_type ON relationships(relationship_type);

-- file_metadata 表索引
CREATE INDEX idx_file_entity ON file_metadata(entity_type, entity_id);
CREATE INDEX idx_file_uploaded ON file_metadata(uploaded_at);
```
**負責 Agent**：database-architect

#### 2.1.2 優化 N+1 查詢
**位置**：`/Services/DataAccessServiceV2.cs`  
**問題代碼**：
```csharp
// 現有問題代碼
foreach (var person in persons)
{
    person.Relationships = await GetPersonRelationships(person.Id);
    person.Photos = await GetPersonPhotos(person.Id);
}
```
**優化方案**：使用批量查詢
**負責 Agent**：dotnet-backend-api-developer

### 2.2 前端性能優化

#### 2.2.1 實施延遲載入
**位置**：`/familytree-frontend/src/app/app.routes.ts`  
**優化方案**：
```typescript
const routes: Routes = [
  {
    path: 'person-data',
    loadChildren: () => import('./pages/person-data/person-data.module').then(m => m.PersonDataModule)
  },
  {
    path: 'visual-analysis',
    loadChildren: () => import('./pages/visual-analysis/visual-analysis.module').then(m => m.VisualAnalysisModule)
  }
];
```
**負責 Agent**：angular-frontend-developer

#### 2.2.2 圖片優化服務
**位置**：`/Services/PhotoUploadService.cs`  
**需求**：
- 實施圖片自動壓縮
- 生成多種尺寸縮圖
- 支援 WebP 格式
**負責 Agent**：dotnet-backend-api-developer

#### 2.2.3 修復記憶體洩漏
**位置**：多個 Angular 組件  
**問題**：未取消訂閱 Observable  
**修復方案**：實施 takeUntil 模式或使用 AsyncPipe  
**負責 Agent**：angular-frontend-developer

### 2.3 快取策略實施

#### 2.3.1 API 響應快取
**位置**：`/Controllers/` 各控制器  
**實施方案**：
```csharp
[HttpGet]
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
public async Task<IActionResult> GetPersonList()
```
**負責 Agent**：dotnet-backend-api-developer

#### 2.3.2 Redis 快取整合
**需求**：整合 Redis 作為分散式快取  
**負責 Agent**：dotnet-backend-api-developer

---

## 🟢 第三階段：代碼質量改進（第 5-6 週）

### 3.1 統一錯誤處理機制
**問題描述**：錯誤處理不一致，多處使用 NotImplementedException  
**修復位置**：
- `/Services/DataAccessService.cs`
- `/Services/DataAccessServiceV2.cs`
- 其他服務層

**實施方案**：
1. 創建全局異常處理中介軟體
2. 定義標準錯誤響應格式
3. 實施所有 NotImplementedException 的功能

**負責 Agent**：dotnet-backend-api-developer

### 3.2 消除代碼重複

#### 3.2.1 控制器基礎類優化
**位置**：各控制器  
**問題**：相似的驗證和錯誤處理邏輯  
**方案**：擴展 BaseController 功能  
**負責 Agent**：dotnet-backend-api-developer

#### 3.2.2 前端服務重構
**位置**：`/familytree-frontend/src/app/services/`  
**問題**：HTTP 請求處理代碼重複  
**方案**：創建統一的 HTTP 攔截器和基礎服務類  
**負責 Agent**：angular-frontend-developer

### 3.3 型別安全改進

#### 3.3.1 處理 C# Nullable 警告
**影響檔案數**：約 50+ 個檔案  
**修復策略**：
- 使用 nullable reference types
- 添加適當的 null 檢查
- 使用 required 修飾符

**負責 Agent**：dotnet-backend-api-developer

#### 3.3.2 消除 TypeScript any 型別
**位置**：前端各組件和服務  
**策略**：定義明確的介面和型別  
**負責 Agent**：angular-frontend-developer

---

## 🏗️ 第四階段：架構優化（第 7-8 週）

### 4.1 權限系統統一

#### 4.1.1 移除舊權限系統
**現狀**：新舊權限系統並存  
**目標**：完全遷移到基於角色的權限系統  
**步驟**：
1. 確保所有用戶都有對應的角色
2. 更新所有權限檢查邏輯
3. 移除舊的權限代碼

**負責 Agent**：system-analyst-tech-debt, dotnet-backend-api-developer

#### 4.1.2 實施權限中介層
**位置**：創建新的 Middleware  
**功能**：統一的權限檢查和審計  
**負責 Agent**：dotnet-backend-api-developer

### 4.2 資料庫架構優化

#### 4.2.1 person_data 表重構
**問題**：單表包含 70+ 欄位  
**方案**：
```sql
-- 拆分為多個表
CREATE TABLE person_basic (
    id UUID PRIMARY KEY,
    family_name VARCHAR(100),
    given_name VARCHAR(100),
    birth_date DATE,
    death_date DATE,
    -- 基本信息
);

CREATE TABLE person_details (
    person_id UUID REFERENCES person_basic(id),
    biography TEXT,
    occupation VARCHAR(200),
    -- 詳細信息
);

CREATE TABLE person_addresses (
    person_id UUID REFERENCES person_basic(id),
    address_type VARCHAR(50),
    address TEXT,
    -- 地址信息
);
```
**負責 Agent**：database-architect

#### 4.2.2 實施資料分區
**目標**：按專案 ID 分區提高查詢性能  
**負責 Agent**：database-architect

### 4.3 API 設計標準化

#### 4.3.1 統一 RESTful 規範
**問題**：API 端點命名不一致  
**標準**：
- GET /api/persons (列表)
- GET /api/persons/{id} (單個)
- POST /api/persons (創建)
- PUT /api/persons/{id} (更新)
- DELETE /api/persons/{id} (刪除)

**負責 Agent**：dotnet-backend-api-developer

#### 4.3.2 統一響應格式
**標準格式**：
```json
{
  "success": true,
  "data": {},
  "message": "",
  "errors": [],
  "pagination": {}
}
```
**負責 Agent**：dotnet-backend-api-developer

---

## 📊 第五階段：用戶體驗改進（第 9-10 週）

### 5.1 功能完善

#### 5.1.1 批次操作功能
**需求**：
- 批次匯入優化
- 批次更新
- 批次刪除（含回收站）

**負責 Agent**：product-manager-tech-spec, angular-frontend-developer

#### 5.1.2 操作歷史和撤銷
**需求**：
- 實施 Command 模式
- 操作歷史記錄
- 撤銷/重做功能

**負責 Agent**：dotnet-backend-api-developer, angular-frontend-developer

### 5.2 UI/UX 優化

#### 5.2.1 載入狀態統一
**位置**：所有前端組件  
**需求**：
- 統一的載入動畫組件
- 骨架屏實施
- 進度條顯示

**負責 Agent**：ui-ux-designer, angular-frontend-developer

#### 5.2.2 錯誤訊息優化
**需求**：
- 友好的錯誤提示
- 多語言支援
- 操作建議

**負責 Agent**：ui-ux-designer, angular-frontend-developer

#### 5.2.3 鍵盤快捷鍵
**需求**：
- Ctrl+S 保存
- Ctrl+Z 撤銷
- Ctrl+F 搜尋
- 其他常用快捷鍵

**負責 Agent**：angular-frontend-developer

---

## 📋 追蹤和監控

### 監控指標
1. API 響應時間 < 200ms (P95)
2. 前端首屏載入時間 < 3s
3. 錯誤率 < 0.1%
4. 代碼覆蓋率 > 80%

### 每週進度檢查
- 週一：計劃會議
- 週三：進度檢查
- 週五：代碼審查

### 風險管理
1. 資料庫遷移需要詳細測試
2. 權限系統變更需要回滾計劃
3. 性能優化需要基準測試

---

## 總結
本優化計劃預計需要 10 週完成，將顯著提升系統的安全性、性能和用戶體驗。建議按照優先級順序執行，並在每個階段進行充分的測試。

最後更新：2025-08-02