# 日誌系統遷移計劃書

> **計劃目的**: 將關鍵業務事件從舊LoggingService遷移至新IAuditLogService
> 
> **計劃建立**: 2025-08-02
> 
> **預計執行時間**: 2-3小時
> 
> **風險等級**: 🟡 中等風險

## 📋 遷移總覽

### 🎯 遷移目標
1. 修復LOGIN、LOGIN_FAILED、LOGOUT事件記錄問題
2. 將認證相關事件正確記錄到audit_logs資料庫
3. 建立可回滾的遷移機制
4. 確保系統穩定性和功能完整性

### 📊 遷移範圍

| 檔案 | 修改內容 | 影響範圍 | 風險等級 |
|------|----------|----------|----------|
| AuthService.cs | 注入IAuditLogService，替換4個LogActivityAsync呼叫 | 所有認證功能 | 🟡 中等 |
| Program.cs | 確保IAuditLogService已註冊 | DI容器 | 🟢 低 |

## 🔄 執行階段

### 階段1: 準備工作 (15分鐘)
- [x] 建立遷移計劃文檔
- [ ] 備份原始檔案
- [ ] 確認測試環境
- [ ] 驗證資料庫連接

### 階段2: 程式碼修改 (30分鐘)
- [ ] 修改AuthService建構子
- [ ] 替換LOGIN事件記錄
- [ ] 替換LOGIN_FAILED事件記錄
- [ ] 替換LOGOUT事件記錄
- [ ] 替換PASSWORD_RESET事件記錄

### 階段3: 編譯測試 (15分鐘)
- [ ] 編譯後端專案
- [ ] 執行單元測試
- [ ] 檢查依賴注入

### 階段4: 功能測試 (30分鐘)
- [ ] 測試登入功能
- [ ] 測試登入失敗場景
- [ ] 測試登出功能
- [ ] 驗證稽核日誌記錄

### 階段5: 回歸測試 (30分鐘)
- [ ] 測試其他認證相關功能
- [ ] 檢查系統穩定性
- [ ] 驗證前端功能正常

## 💾 備份與回滾計劃

### 檔案備份清單
```bash
# 備份要修改的檔案
cp familytree-backend/Services/AuthService.cs familytree-backend/Services/AuthService.cs.backup.2025-08-02
cp familytree-backend/Program.cs familytree-backend/Program.cs.backup.2025-08-02
```

### 回滾步驟
如果遷移失敗，按以下步驟回滾：
1. 停止應用程式
2. 還原備份檔案
3. 重新編譯
4. 重啟應用程式
5. 驗證功能正常

```bash
# 回滾命令
mv familytree-backend/Services/AuthService.cs.backup.2025-08-02 familytree-backend/Services/AuthService.cs
mv familytree-backend/Program.cs.backup.2025-08-02 familytree-backend/Program.cs
```

## 🔧 技術實施細節

### AuthService修改清單

#### 1. 建構子修改
```csharp
// 修改前
public AuthService(
    IUserService userService,
    ITokenService tokenService,
    ILoggingService loggingService,
    IOptions<JwtSettings> jwtSettings,
    ILogger<AuthService> logger)

// 修改後  
public AuthService(
    IUserService userService,
    ITokenService tokenService,
    ILoggingService loggingService,
    IAuditLogService auditLogService,  // 新增
    IOptions<JwtSettings> jwtSettings,
    ILogger<AuthService> logger)
```

#### 2. 欄位宣告修改
```csharp
// 新增私有欄位
private readonly IAuditLogService _auditLogService;

// 在建構子中初始化
_auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
```

#### 3. LOGIN事件修改 (第99-104行)
```csharp
// 修改前
await _loggingService.LogActivityAsync(
    user.Id,
    "login_success",
    "User logged in successfully",
    ipAddress
);

// 修改後
await _auditLogService.LogEventAsync(
    AuditEventTypes.LOGIN,
    AuditActions.LOGIN,
    new {
        UserId = user.Id,
        Username = user.Username,
        IpAddress = ipAddress,
        LoginTime = DateTime.UtcNow
    }
);
```

#### 4. LOGIN_FAILED事件修改 (第73-78行)
```csharp
// 修改前
await _loggingService.LogActivityAsync(
    user.Id,
    "login_failed",
    "Invalid password",
    ipAddress
);

// 修改後
await _auditLogService.LogEventAsync(
    AuditEventTypes.LOGIN_FAILED,
    AuditActions.LOGIN,
    new {
        UserId = user.Id,
        Username = user.Username,
        Reason = "Invalid password",
        IpAddress = ipAddress,
        AttemptTime = DateTime.UtcNow
    }
);
```

#### 5. LOGOUT事件修改 (第137-141行)
```csharp
// 修改前
await _loggingService.LogActivityAsync(
    userId,
    "logout",
    "User logged out"
);

// 修改後
await _auditLogService.LogEventAsync(
    AuditEventTypes.LOGOUT,
    AuditActions.LOGOUT,
    new {
        UserId = userId,
        LogoutTime = DateTime.UtcNow
    }
);
```

#### 6. PASSWORD_RESET事件修改 (第242-246行)
```csharp
// 修改前
await _loggingService.LogActivityAsync(
    adminUserId,
    "reset_password",
    $"Password reset by admin for user {userId}"
);

// 修改後
await _auditLogService.LogEventAsync(
    AuditEventTypes.PASSWORD_RESET,
    AuditActions.UPDATE,
    new {
        AdminUserId = adminUserId,
        TargetUserId = userId,
        ResetTime = DateTime.UtcNow,
        ResetMethod = "AdminReset"
    }
);
```

## 🧪 測試檢查清單

### 功能測試
- [ ] **登入成功測試**
  - 使用正確帳密登入
  - 檢查audit_logs表是否有LOGIN記錄
  - 驗證記錄內容正確性

- [ ] **登入失敗測試**
  - 使用錯誤密碼登入
  - 檢查audit_logs表是否有LOGIN_FAILED記錄
  - 驗證失敗原因記錄

- [ ] **登出測試**
  - 正常登出流程
  - 檢查audit_logs表是否有LOGOUT記錄
  - 驗證登出時間記錄

- [ ] **密碼重設測試**
  - 管理員重設使用者密碼
  - 檢查audit_logs表是否有PASSWORD_RESET記錄
  - 驗證管理員和目標使用者記錄

### 資料庫驗證查詢
```sql
-- 檢查最近的認證事件
SELECT 
    event_type,
    action,
    user_id,
    user_name,
    ip_address,
    additional_data,
    occurred_at
FROM audit_logs 
WHERE event_type IN ('LOGIN', 'LOGIN_FAILED', 'LOGOUT', 'PASSWORD_RESET')
ORDER BY occurred_at DESC 
LIMIT 10;

-- 檢查今日登入統計
SELECT 
    event_type,
    COUNT(*) as count,
    COUNT(DISTINCT user_id) as unique_users
FROM audit_logs 
WHERE event_type IN ('LOGIN', 'LOGIN_FAILED', 'LOGOUT')
AND DATE(occurred_at) = CURRENT_DATE
GROUP BY event_type;
```

## 🚨 風險控制

### 高風險情況及處理
1. **編譯錯誤**
   - 原因: 依賴注入問題
   - 處理: 檢查Program.cs中IAuditLogService註冊
   - 回滾: 還原AuthService.cs

2. **執行時錯誤**
   - 原因: 資料庫連接問題
   - 處理: 檢查audit_logs表存在性
   - 回滾: 立即還原並重啟

3. **效能問題**
   - 原因: 資料庫寫入延遲
   - 處理: 監控登入響應時間
   - 緩解: 考慮異步處理

### 成功標準
- [ ] 編譯無錯誤
- [ ] 登入功能正常
- [ ] 稽核記錄正確寫入
- [ ] 無明顯效能下降
- [ ] 前端功能正常

### 失敗標準 (觸發回滾)
- [ ] 編譯失敗
- [ ] 登入功能異常
- [ ] 資料庫錯誤
- [ ] 系統當機
- [ ] 效能嚴重下降

## 📞 緊急聯絡資訊

### 遇到問題時的處理順序
1. **停止當前操作**
2. **檢查錯誤日誌**
3. **執行回滾程序**
4. **驗證系統恢復**
5. **記錄問題詳情**

### 後續支援
- 遷移完成後持續監控24小時
- 記錄任何異常情況
- 準備進一步優化計劃

---

**⚠️ 重要提醒**: 
- 在生產環境執行前，務必在測試環境完整驗證
- 保持與使用者的溝通，告知可能的短暫影響
- 準備好立即回滾的能力

*計劃建立完成 - 2025-08-02*
*執行人員: Claude AI Assistant*