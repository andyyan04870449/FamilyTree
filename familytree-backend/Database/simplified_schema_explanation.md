# 簡化後的資料庫設計說明

## 核心概念變更

### 原設計問題
- 有 projects 表和 project_members 表
- 允許使用者參與多個專案
- 資料以專案為單位隔離
- 過於複雜，不符合需求

### 新設計方案
- **移除專案概念**
- **每個使用者只能看到自己的資料**
- **資料直接以 user_id 隔離**
- **簡單直接的權限控制**

## 資料表變更說明

### 1. 保留的資料表（3個）

#### users 表
- 保持不變，儲存使用者基本資料
- role 欄位：admin（可看所有資料）或 user（只看自己資料）

#### user_tokens 表
- 保持不變，管理 JWT refresh token

#### activity_logs 表
- 保持不變，記錄操作日誌

### 2. 移除的資料表
- ~~project_members~~ - 不需要專案成員管理
- ~~projects~~ - 不需要專案概念（可考慮保留或刪除）

### 3. 修改的資料表
所有業務資料表都加入 `user_id` 欄位：
- person_profile - 人員資料歸屬於特定使用者
- favorites - 我的最愛歸屬於特定使用者
- field_mapping - 欄位對應歸屬於特定使用者
- analysis_results - 分析結果歸屬於特定使用者
- analysis_sessions - 分析會話歸屬於特定使用者
- missing_persons - 缺失人員歸屬於特定使用者
- relationship_layers - 關係層級歸屬於特定使用者

## 權限控制邏輯

### 簡化後的權限規則
1. **admin 角色**：可以查看和管理所有使用者的資料
2. **user 角色**：只能查看和管理自己的資料

### 實作範例

```csharp
// 在 API 中檢查權限
public async Task<IActionResult> GetPersonProfile(int personId)
{
    var currentUserId = GetCurrentUserId();
    var currentUserRole = GetCurrentUserRole();
    
    // 查詢人員資料
    var person = await GetPersonById(personId);
    
    // 權限檢查
    if (currentUserRole != "admin" && person.user_id != currentUserId)
    {
        return Forbid("您沒有權限查看此資料");
    }
    
    return Ok(person);
}
```

### 查詢範例

```sql
-- 一般使用者查詢自己的資料
SELECT * FROM person_profile 
WHERE user_id = @currentUserId;

-- 管理員查詢所有資料
SELECT * FROM person_profile;

-- 使用 Function 檢查權限
SELECT * FROM person_profile p
WHERE check_data_access(@currentUserId, 'person', p.id::text, p.user_id);
```

## 優點

1. **簡單明瞭**：沒有複雜的專案權限管理
2. **資料隔離**：每個使用者的資料完全獨立
3. **易於實作**：權限檢查邏輯簡單
4. **效能更好**：少了專案關聯查詢

## 遷移步驟

1. 執行 SQL 腳本加入 user_id 欄位
2. 將現有資料關聯到對應的使用者
3. 移除 project_id 相關欄位
4. 更新應用程式的查詢邏輯

## 應用程式調整

### Before (使用專案隔離)
```csharp
// 查詢需要專案 ID
var persons = await db.QueryAsync(
    "SELECT * FROM person_profile WHERE project_id = @projectId",
    new { projectId });
```

### After (使用使用者隔離)
```csharp
// 直接使用使用者 ID
var persons = await db.QueryAsync(
    "SELECT * FROM person_profile WHERE user_id = @userId",
    new { userId = currentUserId });
```