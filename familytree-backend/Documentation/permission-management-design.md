# 權限管理系統設計文件

## 專案概述

本文件詳細描述家譜系統的權限管理功能設計，包括現有系統分析、權限模型設計、UI 介面規劃和實作策略。

---

## 1. 現有系統分析

### 1.1 當前認證機制
- **JWT Token 認證**：使用 Access Token 和 Refresh Token 機制
- **角色驗證**：基於簡單的 role 字段（admin/user）
- **中介軟體**：JWT 認證中介軟體處理 Token 驗證
- **密碼安全**：使用 BCrypt 加密

### 1.2 當前授權架構
- **角色系統**：二層角色（admin, user）
- **權限檢查**：
  - 控制器層級：`[Authorize(Roles = "admin")]`
  - 基礎類別：`IsAdmin()`, `GetCurrentUserRole()`
  - 服務層級：`AccessControlService` 提供統一權限檢查

### 1.3 資料庫結構
- **users** 表：基本使用者資料
- **完整權限架構**：已設計完整的 RBAC 架構（roles, permissions, role_permissions 等）
- **專案權限**：project_members 表處理專案層級權限

### 1.4 現有問題分析
1. **權限粒度不足**：只有 admin/user 二層權限
2. **缺乏權限管理介面**：無法動態管理角色和權限
3. **權限邏輯分散**：權限檢查邏輯散佈在各個控制器
4. **資源權限未實作**：完整的 RBAC 資料結構未充分利用

---

## 2. 權限管理系統設計

### 2.1 權限模型架構

#### 權限層級結構
```
系統權限（全域）
├── 角色管理權限
├── 使用者管理權限
├── 系統設定權限
└── 審計日誌權限

專案權限（專案隔離）
├── 專案管理權限
├── 人員資料權限
├── 檔案管理權限
└── 報表權限

資源權限（細粒度）
├── 讀取權限（read）
├── 建立權限（create）
├── 更新權限（update）
├── 刪除權限（delete）
└── 管理權限（manage）
```

#### 預設角色定義
```json
{
  "superadmin": {
    "name": "超級管理員",
    "level": 100,
    "description": "擁有系統所有權限",
    "permissions": ["*"]
  },
  "admin": {
    "name": "管理員", 
    "level": 80,
    "description": "擁有大部分管理權限",
    "permissions": [
      "user:*", "project:*", "person:*", 
      "file:*", "report:*"
    ]
  },
  "user": {
    "name": "一般使用者",
    "level": 50, 
    "description": "基本使用權限",
    "permissions": [
      "project:read", "project:create", 
      "person:*", "file:upload", "file:download"
    ]
  },
  "guest": {
    "name": "訪客",
    "level": 10,
    "description": "只有查看權限", 
    "permissions": [
      "project:read", "person:read", "report:view"
    ]
  }
}
```

### 2.2 權限類別分類

#### 系統管理權限
- `system:manage` - 系統設定管理
- `role:manage` - 角色權限管理
- `user:create` - 建立使用者
- `user:update` - 更新使用者
- `user:delete` - 刪除使用者
- `log:view` - 查看系統日誌

#### 專案管理權限
- `project:create` - 建立專案
- `project:read` - 查看專案
- `project:update` - 更新專案
- `project:delete` - 刪除專案
- `project:manage_members` - 管理專案成員

#### 人員資料權限
- `person:create` - 建立人員
- `person:read` - 查看人員
- `person:update` - 更新人員
- `person:delete` - 刪除人員
- `person:export` - 匯出人員資料

#### 檔案管理權限
- `file:upload` - 上傳檔案
- `file:download` - 下載檔案
- `file:delete` - 刪除檔案
- `file:manage` - 檔案管理

#### 報表權限
- `report:view` - 查看報表
- `report:export` - 匯出報表
- `report:create` - 建立報表

### 2.3 權限檢查邏輯

#### 權限檢查優先順序
1. **系統角色檢查**：superadmin 擁有所有權限
2. **角色權限檢查**：檢查使用者角色是否有對應權限
3. **專案權限檢查**：檢查專案層級權限
4. **資源擁有權檢查**：檢查資源擁有權

#### 權限檢查流程
```typescript
async checkPermission(userId: string, resource: string, action: string, context?: any): Promise<boolean> {
  // 1. 檢查使用者是否存在且啟用
  const user = await getUserById(userId);
  if (!user || user.status !== 'active') return false;

  // 2. 超級管理員擁有所有權限
  if (await hasRole(userId, 'superadmin')) return true;

  // 3. 檢查角色權限
  const hasRolePermission = await checkRolePermission(userId, resource, action);
  if (hasRolePermission) return true;

  // 4. 檢查專案權限（如果是專案相關資源）
  if (context?.projectId) {
    const hasProjectPermission = await checkProjectPermission(userId, context.projectId, action);
    if (hasProjectPermission) return true;
  }

  // 5. 檢查資源擁有權
  if (context?.resourceId) {
    const isOwner = await checkResourceOwnership(userId, resource, context.resourceId);
    if (isOwner) return true;
  }

  return false;
}
```

---

## 3. UI 介面設計

### 3.1 權限管理主頁面

#### 頁面結構
```
權限管理
├── 角色管理標籤
│   ├── 角色列表表格
│   ├── 新增角色按鈕
│   └── 角色操作（編輯、刪除、複製）
├── 權限設定標籤
│   ├── 權限類別樹狀結構
│   ├── 權限描述
│   └── 權限狀態切換
└── 使用者權限標籤
    ├── 使用者搜尋
    ├── 使用者權限表格
    └── 批量權限操作
```

#### 介面佈局（與現有使用者管理頁面一致）
```html
<div class="permission-management-page">
  <!-- 頁面標題和操作區域 -->
  <div class="page-header">
    <div class="header-top">
      <div class="header-left">
        <h1>權限管理</h1>
      </div>
      <div class="header-right">
        <button class="btn-export">匯出權限設定</button>
        <button class="btn-new">新增角色</button>
      </div>
    </div>
  </div>

  <!-- 標籤頁導航 -->
  <div class="tab-navigation">
    <button class="tab-btn active">角色管理</button>
    <button class="tab-btn">權限設定</button>
    <button class="tab-btn">使用者權限</button>
  </div>

  <!-- 內容區域 -->
  <div class="tab-content">
    <!-- 角色管理標籤內容 -->
    <!-- 權限設定標籤內容 -->
    <!-- 使用者權限標籤內容 -->
  </div>
</div>
```

### 3.2 角色管理介面

#### 角色列表表格
| 角色名稱 | 顯示名稱 | 層級 | 使用者數量 | 最後更新 | 操作 |
|---------|---------|------|----------|---------|------|
| admin | 管理員 | 80 | 3 | 2024-08-01 | 編輯、複製、刪除 |
| user | 一般使用者 | 50 | 25 | 2024-07-30 | 編輯、複製 |

#### 角色編輯對話框
```html
<div class="role-edit-dialog">
  <div class="dialog-header">
    <h3>編輯角色 - 管理員</h3>
  </div>
  
  <div class="dialog-body">
    <div class="form-group">
      <label>角色名稱</label>
      <input type="text" value="admin" disabled />
    </div>
    
    <div class="form-group">
      <label>顯示名稱</label>
      <input type="text" value="管理員" />
    </div>
    
    <div class="form-group">
      <label>角色層級 (1-100)</label>
      <input type="number" value="80" min="1" max="100" />
    </div>
    
    <div class="form-group">
      <label>描述</label>
      <textarea>擁有大部分管理權限的角色</textarea>
    </div>
    
    <!-- 權限設定區域 -->
    <div class="permissions-section">
      <h4>權限設定</h4>
      <div class="permission-categories">
        <!-- 系統管理權限 -->
        <div class="permission-category">
          <div class="category-header">
            <input type="checkbox" id="system-all" />
            <label for="system-all">系統管理</label>
          </div>
          <div class="permission-items">
            <label><input type="checkbox" /> 系統設定管理</label>
            <label><input type="checkbox" /> 角色權限管理</label>
            <label><input type="checkbox" /> 查看系統日誌</label>
          </div>
        </div>
        
        <!-- 使用者管理權限 -->
        <div class="permission-category">
          <div class="category-header">
            <input type="checkbox" id="user-all" />
            <label for="user-all">使用者管理</label>
          </div>
          <div class="permission-items">
            <label><input type="checkbox" checked /> 建立使用者</label>
            <label><input type="checkbox" checked /> 查看使用者</label>
            <label><input type="checkbox" checked /> 更新使用者</label>
            <label><input type="checkbox" /> 刪除使用者</label>
          </div>
        </div>
        
        <!-- 專案管理權限 -->
        <div class="permission-category">
          <div class="category-header">
            <input type="checkbox" id="project-all" checked />
            <label for="project-all">專案管理</label>
          </div>
          <div class="permission-items">
            <label><input type="checkbox" checked /> 建立專案</label>
            <label><input type="checkbox" checked /> 查看專案</label>
            <label><input type="checkbox" checked /> 更新專案</label>
            <label><input type="checkbox" checked /> 刪除專案</label>
          </div>
        </div>
      </div>
    </div>
  </div>
  
  <div class="dialog-footer">
    <button class="btn btn-cancel">取消</button>
    <button class="btn btn-save">儲存</button>
  </div>
</div>
```

### 3.3 使用者權限管理介面

#### 使用者權限表格
| 使用者 | 角色 | 專案權限 | 特殊權限 | 最後登入 | 操作 |
|-------|------|---------|---------|---------|------|
| admin@example.com | 管理員 | 3個專案 | 無 | 2024-08-02 | 編輯權限 |
| user@example.com | 一般使用者 | 1個專案 | 檔案管理 | 2024-08-01 | 編輯權限 |

#### 權限編輯對話框
```html
<div class="user-permission-dialog">
  <div class="dialog-header">
    <h3>編輯使用者權限 - user@example.com</h3>
  </div>
  
  <div class="dialog-body">
    <div class="user-info">
      <h4>使用者資訊</h4>
      <p>姓名：張三</p>
      <p>電子信箱：user@example.com</p>
      <p>狀態：啟用</p>
    </div>
    
    <div class="role-assignment">
      <h4>角色指派</h4>
      <div class="role-list">
        <label>
          <input type="radio" name="role" value="admin" />
          <span class="role-badge role-admin">管理員</span>
          <span class="role-description">擁有大部分管理權限</span>
        </label>
        <label>
          <input type="radio" name="role" value="user" checked />
          <span class="role-badge role-user">一般使用者</span>
          <span class="role-description">基本使用權限</span>
        </label>
        <label>
          <input type="radio" name="role" value="guest" />
          <span class="role-badge role-guest">訪客</span>
          <span class="role-description">只有查看權限</span>
        </label>
      </div>
    </div>
    
    <div class="additional-permissions">
      <h4>額外權限</h4>
      <div class="permission-toggles">
        <label class="permission-toggle">
          <input type="checkbox" />
          <span>檔案管理權限</span>
          <small>允許刪除和管理檔案</small>
        </label>
        <label class="permission-toggle">
          <input type="checkbox" checked />
          <span>報表匯出權限</span>
          <small>允許匯出各種報表</small>
        </label>
      </div>
    </div>
    
    <div class="project-permissions">
      <h4>專案權限</h4>
      <div class="project-list">
        <div class="project-item">
          <span class="project-name">家族族譜專案</span>
          <select class="project-role">
            <option value="viewer">檢視者</option>
            <option value="editor" selected>編輯者</option>
            <option value="admin">管理員</option>
            <option value="owner">擁有者</option>
          </select>
        </div>
      </div>
    </div>
  </div>
  
  <div class="dialog-footer">
    <button class="btn btn-cancel">取消</button>
    <button class="btn btn-save">儲存</button>
  </div>
</div>
```

### 3.4 權限設定介面

#### 權限類別樹狀結構
```html
<div class="permission-tree">
  <div class="permission-category expanded">
    <div class="category-header">
      <span class="expand-icon">▼</span>
      <span class="category-name">系統管理</span>
      <span class="permission-count">(3個權限)</span>
    </div>
    <div class="category-items">
      <div class="permission-item">
        <span class="permission-name">system:manage</span>
        <span class="permission-description">系統設定管理</span>
        <div class="role-assignments">
          <span class="role-badge">superadmin</span>
        </div>
      </div>
      <div class="permission-item">
        <span class="permission-name">role:manage</span>
        <span class="permission-description">角色權限管理</span>
        <div class="role-assignments">
          <span class="role-badge">superadmin</span>
        </div>
      </div>
    </div>
  </div>
  
  <div class="permission-category expanded">
    <div class="category-header">
      <span class="expand-icon">▼</span>
      <span class="category-name">使用者管理</span>
      <span class="permission-count">(4個權限)</span>
    </div>
    <div class="category-items">
      <div class="permission-item">
        <span class="permission-name">user:create</span>
        <span class="permission-description">建立使用者</span>
        <div class="role-assignments">
          <span class="role-badge">superadmin</span>
          <span class="role-badge">admin</span>
        </div>
      </div>
      <!-- 更多權限項目 -->
    </div>
  </div>
</div>
```

---

## 4. 實作策略

### 4.1 開發階段規劃

#### 第一階段：後端基礎建設
1. **完善 PermissionService**
   - 實作完整的 RBAC 邏輯
   - 與現有資料庫整合
   - 完善權限檢查方法

2. **更新 Controllers**
   - 統一使用權限檢查
   - 移除硬編碼角色檢查
   - 實作細粒度權限控制

3. **建立 Permission Controllers**
   - RoleController（角色管理）
   - PermissionController（權限管理）
   - UserPermissionController（使用者權限）

#### 第二階段：前端介面開發
1. **建立權限管理頁面**
   - 複用現有使用者管理頁面樣式
   - 實作標籤頁導航
   - 建立各種對話框組件

2. **整合權限檢查**
   - 前端路由權限守衛
   - 按鈕和功能權限控制
   - 動態選單生成

#### 第三階段：測試和優化
1. **權限測試**
   - 單元測試
   - 整合測試
   - 安全性測試

2. **效能優化**
   - 權限快取機制
   - 批量權限檢查
   - 資料庫查詢優化

### 4.2 技術實作要點

#### 後端權限檢查統一化
```csharp
// 統一的權限檢查屬性
[RequirePermission("user:create")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
{
    // 控制器邏輯
}

// 權限檢查中介軟體
public class PermissionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var permissionAttribute = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>();
        
        if (permissionAttribute != null)
        {
            var userId = GetCurrentUserId(context);
            var hasPermission = await _permissionService.CheckPermissionAsync(
                userId, permissionAttribute.Permission);
                
            if (!hasPermission)
            {
                context.Response.StatusCode = 403;
                return;
            }
        }
        
        await _next(context);
    }
}
```

#### 前端權限守衛
```typescript
// 路由權限守衛
export class PermissionGuard implements CanActivate {
  canActivate(route: ActivatedRouteSnapshot): Observable<boolean> {
    const requiredPermission = route.data['permission'];
    return this.permissionService.hasPermission(requiredPermission);
  }
}

// 指令權限控制
@Directive({
  selector: '[hasPermission]'
})
export class HasPermissionDirective {
  @Input() set hasPermission(permission: string) {
    this.checkPermission(permission);
  }
  
  private async checkPermission(permission: string) {
    const hasPermission = await this.permissionService.hasPermission(permission);
    if (!hasPermission) {
      this.viewContainer.clear();
    }
  }
}
```

### 4.3 資料遷移策略

#### 現有使用者角色遷移
```sql
-- 將現有的 role 欄位遷移到新的角色系統
INSERT INTO user_roles (user_id, role_id)
SELECT 
    id as user_id,
    CASE 
        WHEN role = 'admin' THEN 'role_admin'
        WHEN role = 'user' THEN 'role_user'
        ELSE 'role_guest'
    END as role_id
FROM users
WHERE status = 'active';
```

#### 權限資料初始化
```sql
-- 初始化基本權限資料
INSERT INTO permissions (resource, action, display_name, description, is_system) VALUES
('user', 'create', '建立使用者', '可以建立新使用者帳號', true),
('user', 'read', '查看使用者', '可以查看使用者資料', true),
('user', 'update', '更新使用者', '可以修改使用者資料', true),
('user', 'delete', '刪除使用者', '可以刪除使用者帳號', true);

-- 為角色指派權限
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'role_admin', id FROM permissions WHERE resource = 'user';
```

---

## 5. 安全性考量

### 5.1 權限檢查機制
- **雙重驗證**：前端和後端都進行權限檢查
- **最小權限原則**：預設拒絕，明確授權
- **權限繼承**：角色層級權限繼承
- **審計日誌**：記錄所有權限變更

### 5.2 安全防護措施
- **防止權限提升**：嚴格控制角色指派
- **會話權限檢查**：每次請求都驗證權限
- **敏感操作確認**：重要權限變更需要二次確認
- **權限快取失效**：權限變更時立即失效快取

### 5.3 監控和警告
- **異常權限操作警告**
- **權限變更通知**
- **定期權限審查提醒**
- **未授權存取嘗試記錄**

---

## 6. 使用者體驗設計

### 6.1 易用性設計
- **直觀的權限分類**：按功能模組分類權限
- **批量操作支援**：支援批量角色指派
- **權限搜尋功能**：快速找到特定權限
- **權限預覽**：顯示權限影響範圍

### 6.2 視覺設計一致性
- **沿用現有設計系統**：與使用者管理頁面保持一致
- **權限狀態指示**：清楚的已授權/未授權狀態
- **角色層級視覺化**：不同層級角色使用不同顏色
- **響應式設計**：支援各種裝置尺寸

### 6.3 錯誤處理和提示
- **友善的錯誤訊息**：避免技術術語
- **操作確認對話框**：重要操作需要確認
- **即時驗證回饋**：輸入驗證即時顯示
- **操作結果通知**：成功/失敗明確提示

---

## 7. 總結

本設計文件提供了完整的權限管理系統架構，包括：

1. **完善的權限模型**：基於 RBAC 的多層權限架構
2. **統一的權限檢查**：前後端一致的權限驗證機制
3. **直觀的管理介面**：符合現有設計風格的管理頁面
4. **安全的實作策略**：考慮各種安全性和效能問題

該設計充分利用現有的資料庫架構和 UI 組件，確保在提供強大權限管理功能的同時，維持系統的一致性和使用者體驗。

實作時建議按照分階段策略進行，先完善後端基礎設施，再開發前端介面，最後進行整合測試和優化，確保系統的穩定性和安全性。