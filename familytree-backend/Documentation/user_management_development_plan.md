# 使用者帳號管理系統 - 開發計劃

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

## 二、開發階段規劃

### 第一階段：資料庫建置（2-3天）

#### 1.1 執行資料庫變更
```sql
-- 執行順序
1. 建立新資料表（users, user_tokens, activity_logs）
2. 修改現有資料表（加入 user_id 欄位）
3. 建立索引和外鍵關係
4. 建立 trigger 和 function
```

#### 1.2 資料遷移計劃
- 為現有資料建立預設使用者
- 將現有資料關聯到對應使用者
- 移除 project 相關欄位

### 第二階段：後端 API 開發（5-7天）

#### 2.1 建立服務類別

**UserService.cs** - 使用者管理
```csharp
public interface IUserService
{
    Task<UserModel> GetByIdAsync(string userId);
    Task<UserModel> GetByUsernameAsync(string username);
    Task<UserModel> CreateAsync(CreateUserDto dto);
    Task<UserModel> UpdateAsync(string userId, UpdateUserDto dto);
    Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
    Task<PagedResult<UserModel>> GetUsersAsync(int page, int pageSize);
}
```

**AuthService.cs** - 認證服務
```csharp
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginDto dto);
    Task LogoutAsync(string userId, string token);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> ValidateTokenAsync(string token);
}
```

**TokenService.cs** - JWT Token 管理
```csharp
public interface ITokenService
{
    string GenerateAccessToken(UserModel user);
    string GenerateRefreshToken();
    Task<bool> SaveRefreshTokenAsync(string userId, string token);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
}
```

#### 2.2 建立 API Controllers

**AuthController.cs** - 認證相關 API
```csharp
[Route("api/auth")]
public class AuthController : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto);
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto);
    
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout();
    
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(RefreshTokenDto dto);
}
```

**UserController.cs** - 使用者管理 API
```csharp
[Route("api/users")]
[Authorize]
public class UserController : BaseController
{
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 20);
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(string id);
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserDto dto);
    
    [HttpPost("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(string id, ChangePasswordDto dto);
    
    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ResetPassword(string id);
}
```

#### 2.3 實作認證中介軟體

**JwtAuthenticationMiddleware.cs**
```csharp
public class JwtAuthenticationMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context.Request);
        if (!string.IsNullOrEmpty(token))
        {
            var principal = ValidateToken(token);
            context.User = principal;
        }
        await _next(context);
    }
}
```

### 第三階段：整合現有功能（3-4天）

#### 3.1 修改現有 API
- 所有 API 加入 [Authorize] 屬性
- 查詢時自動加入 user_id 過濾
- 新增資料時自動設定 user_id

#### 3.2 修改 BaseController
```csharp
public abstract class BaseController : ControllerBase
{
    protected string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
    
    protected string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
    
    protected IQueryable<T> ApplyUserFilter<T>(IQueryable<T> query) where T : IUserOwned
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentUserRole();
        
        if (role != "admin")
        {
            query = query.Where(x => x.UserId == userId);
        }
        
        return query;
    }
}
```

#### 3.3 修改 DataAccessService
```csharp
// 範例：修改查詢方法
public async Task<IEnumerable<PersonDataModel>> GetPersonListAsync(string userId, string userRole)
{
    var sql = @"
        SELECT * FROM person_profile 
        WHERE (@UserRole = 'admin' OR user_id = @UserId)
        ORDER BY created_at DESC";
    
    return await connection.QueryAsync<PersonDataModel>(sql, 
        new { UserId = userId, UserRole = userRole });
}
```

### 第四階段：前端整合（3-4天）

#### 4.1 登入頁面
- 建立登入表單
- 儲存 JWT token 到 localStorage
- 實作自動更新 token 機制

#### 4.2 使用者管理介面
- 使用者列表（管理員）
- 個人資料編輯
- 變更密碼功能

#### 4.3 權限控制
- 路由守衛
- 根據角色顯示/隱藏功能
- API 錯誤處理（401/403）

### 第五階段：測試與部署（2-3天）

#### 5.1 單元測試
- 認證服務測試
- 權限檢查測試
- Token 管理測試

#### 5.2 整合測試
- 完整登入流程測試
- 資料隔離測試
- 權限控制測試

#### 5.3 部署準備
- 環境變數設定（JWT Secret 等）
- 資料庫遷移腳本
- 部署文件更新

## 三、技術實作細節

### JWT 設定
```json
{
  "Jwt": {
    "Secret": "your-256-bit-secret-key-here",
    "Issuer": "FamilyTreeAPI",
    "Audience": "FamilyTreeClient",
    "AccessTokenExpiration": 15,  // 分鐘
    "RefreshTokenExpiration": 10080  // 分鐘 (7天)
  }
}
```

### 密碼政策
- 最少 8 個字元
- 包含大小寫字母和數字
- 管理員重設密碼為臨時密碼

### API 回應格式
```json
// 成功
{
  "success": true,
  "data": { ... },
  "message": "操作成功"
}

// 失敗
{
  "success": false,
  "error": "錯誤訊息",
  "errorCode": "ERROR_CODE"
}
```

## 四、風險與注意事項

### 1. 資料遷移風險
- 備份現有資料庫
- 先在測試環境執行
- 準備回滾方案

### 2. 效能考量
- user_id 索引很重要
- 考慮資料量大時的分頁
- Token 定期清理機制

### 3. 安全性考量
- JWT Secret 必須安全保管
- 實作登入失敗鎖定
- 敏感操作記錄日誌

## 五、時程估計

| 階段 | 工作項目 | 預估時間 |
|------|---------|----------|
| 第一階段 | 資料庫建置 | 2-3 天 |
| 第二階段 | 後端 API 開發 | 5-7 天 |
| 第三階段 | 整合現有功能 | 3-4 天 |
| 第四階段 | 前端整合 | 3-4 天 |
| 第五階段 | 測試與部署 | 2-3 天 |
| **總計** | | **15-21 天** |

## 六、交付項目

1. **資料庫**
   - 更新後的資料庫結構
   - 資料遷移腳本
   - 資料庫文件

2. **後端 API**
   - 認證相關 API
   - 使用者管理 API
   - 更新後的業務 API

3. **前端功能**
   - 登入/登出頁面
   - 使用者管理介面
   - 權限控制機制

4. **文件**
   - API 文件
   - 部署指南
   - 使用手冊

## 七、後續優化建議

1. **進階功能**
   - 多因素認證（2FA）
   - 單一登入（SSO）
   - API Rate Limiting

2. **管理功能**
   - 使用者活動報表
   - 系統使用統計
   - 自動化備份機制

3. **使用者體驗**
   - 記住我功能
   - 登入歷史查看
   - 安全提醒通知