# 使用者帳號管理系統 - 開發計劃（修訂版）

## 一、系統需求確認

### 核心功能
1. **使用者註冊與登入**
   - 使用 username 或 email 登入
   - JWT token 認證機制
   - 密碼加密儲存（BCrypt）

2. **權限控制**
   - 兩層權限：admin（看所有資料）、user（只看自己資料）
   - 所有 API 都需要驗證身份
   - 資料自動過濾（依 user_id）

3. **帳號管理**
   - 管理員可以管理所有使用者
   - 使用者可以修改自己的資料
   - 管理員可以重設使用者密碼

4. **審計日誌**
   - 記錄所有重要操作
   - 包含操作者、時間、IP等資訊

## 二、四階段開發計劃

### 第一階段：資料庫建置（2-3天）

#### 1.1 執行資料庫變更
```sql
-- 執行順序
1. 建立新資料表（users, user_tokens, activity_logs）
2. 修改現有資料表（加入 user_id 欄位）
3. 建立索引和外鍵關係
4. 建立 trigger 和 function
```

#### 1.2 資料遷移
- 建立預設管理員帳號
- 為現有資料建立預設使用者（如需要）
- 將現有資料關聯到對應使用者

#### 1.3 交付項目
- 完整的資料庫 Schema
- 資料遷移腳本
- 資料庫變更記錄

### 第二階段：後端 API 開發（5-7天）

#### 2.1 認證系統開發

**JWT 設定（appsettings.json）**
```json
{
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "FamilyTreeAPI",
    "Audience": "FamilyTreeClient",
    "AccessTokenExpiration": 15,
    "RefreshTokenExpiration": 10080
  }
}
```

**核心服務實作**
1. UserService - 使用者 CRUD 操作
2. AuthService - 登入驗證邏輯
3. TokenService - JWT Token 產生與驗證
4. PasswordService - 密碼加密與驗證

**認證 API 端點**
```
POST /api/auth/register     - 註冊新使用者
POST /api/auth/login        - 登入
POST /api/auth/logout       - 登出
POST /api/auth/refresh      - 更新 Token
```

#### 2.2 使用者管理開發

**使用者管理 API 端點**
```
GET    /api/users           - 取得使用者列表（僅 admin）
GET    /api/users/me        - 取得當前使用者資料
GET    /api/users/{id}      - 取得特定使用者資料
PUT    /api/users/{id}      - 更新使用者資料
POST   /api/users/{id}/change-password    - 變更密碼
POST   /api/users/{id}/reset-password     - 重設密碼（僅 admin）
DELETE /api/users/{id}      - 停用使用者（僅 admin）
```

#### 2.3 認證中介軟體
- JWT Token 驗證
- 自動解析使用者資訊
- 權限檢查

### 第三階段：整合現有功能（3-4天）

#### 3.1 修改 BaseController

```csharp
public abstract class BaseController : ControllerBase
{
    // 取得當前使用者 ID
    protected string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
    
    // 取得當前使用者角色
    protected string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
    
    // 檢查是否為管理員
    protected bool IsAdmin()
    {
        return GetCurrentUserRole() == "admin";
    }
}
```

#### 3.2 修改現有 Controllers
所有 Controller 需要：
1. 加入 `[Authorize]` 屬性
2. 查詢時加入 user_id 過濾
3. 新增/更新時自動設定 user_id

**範例：PersonDataController**
```csharp
[Authorize]
[Route("api/[controller]")]
public class PersonDataController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetPersonList()
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        
        // 管理員看所有，一般使用者只看自己的
        var persons = await _personService.GetListAsync(userId, userRole);
        return Ok(persons);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreatePerson(PersonCreateDto dto)
    {
        dto.UserId = GetCurrentUserId(); // 自動設定擁有者
        var result = await _personService.CreateAsync(dto);
        return Ok(result);
    }
}
```

#### 3.3 更新資料存取層
修改所有查詢方法，加入 user_id 過濾邏輯：

```csharp
public async Task<IEnumerable<PersonDataModel>> GetPersonListAsync(
    string userId, string userRole)
{
    var sql = userRole == "admin" 
        ? "SELECT * FROM person_profile ORDER BY created_at DESC"
        : "SELECT * FROM person_profile WHERE user_id = @UserId ORDER BY created_at DESC";
    
    return await connection.QueryAsync<PersonDataModel>(sql, new { UserId = userId });
}
```

### 第四階段：前端整合與測試（3-4天）

#### 4.1 前端整合

**登入功能**
- 登入頁面 UI
- Token 儲存機制（localStorage）
- 自動更新 Token
- 登出清理

**使用者介面**
- 個人資料查看/編輯
- 變更密碼
- 使用者管理（admin only）

**權限控制**
- 路由守衛
- 功能按鈕顯示控制
- API 錯誤處理（401/403）

#### 4.2 API 功能測試

**測試清單**
1. **認證流程測試**
   - 註冊新使用者
   - 登入（正確/錯誤密碼）
   - Token 更新
   - 登出

2. **權限測試**
   - 一般使用者只能看自己的資料
   - 管理員可以看所有資料
   - 未登入無法存取 API

3. **資料隔離測試**
   - 建立兩個測試使用者
   - 確認資料互相看不到
   - 管理員可以看到兩者資料

4. **整合測試**
   - 完整使用流程
   - 資料 CRUD 操作
   - 日誌記錄確認

**測試工具**
- Postman 或 Swagger UI
- 準備測試資料集
- 測試案例文件

## 三、實作重點

### 密碼安全
```csharp
// 密碼加密
var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

// 密碼驗證
var isValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);

// 產生臨時密碼
var tempPassword = $"Tmp{DateTime.Now:yyyyMMdd}!{Random.Next(1000, 9999)}";
```

### Token 管理
```csharp
// Access Token Payload
{
  "sub": "user_id",
  "name": "username",
  "role": "user",
  "exp": 1234567890
}

// Refresh Token 儲存
INSERT INTO user_tokens (user_id, token_type, token_hash, expires_at)
VALUES (@userId, 'refresh', @tokenHash, @expiresAt)
```

### 操作日誌
```csharp
// 記錄重要操作
await LogActivity(
    userId: currentUserId,
    action: "create_person",
    resourceType: "person",
    resourceId: newPersonId,
    details: JsonSerializer.Serialize(createDto),
    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
);
```

## 四、時程估計

| 階段 | 工作項目 | 預估時間 |
|------|---------|----------|
| 第一階段 | 資料庫建置 | 2-3 天 |
| 第二階段 | 後端 API 開發 | 5-7 天 |
| 第三階段 | 整合現有功能 | 3-4 天 |
| 第四階段 | 前端整合與測試 | 3-4 天 |
| **總計** | | **13-18 天** |

## 五、風險管理

### 1. 資料遷移
- **風險**：現有資料關聯錯誤
- **對策**：先備份，小批次遷移

### 2. 效能問題
- **風險**：user_id 過濾造成查詢變慢
- **對策**：確保索引建立正確

### 3. 權限漏洞
- **風險**：忘記加入權限檢查
- **對策**：統一在 BaseController 處理

## 六、交付標準

### 必須完成
1. 所有使用者都必須登入才能使用系統
2. 一般使用者只能看到自己的資料
3. 管理員可以管理使用者和查看所有資料
4. 所有操作都有日誌記錄

### 成功指標
1. 無法繞過登入存取 API
2. 使用者 A 看不到使用者 B 的資料
3. Token 過期後自動更新或要求重新登入
4. 管理員可以重設使用者密碼