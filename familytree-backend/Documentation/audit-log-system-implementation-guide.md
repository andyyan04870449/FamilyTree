# 家族樹管理系統 - 稽核日誌系統實施指南

## 概述

本文件描述家族樹管理系統稽核日誌系統的完整實施方案，包括資料庫設計、後端API、前端介面、安全性控制和效能優化等方面。

## 系統架構

### 1. 資料庫層

#### 主要表格結構

- **audit_logs**: 主稽核日誌表（分區表，按月分區）
- **audit_event_types**: 事件類型定義表
- **audit_change_details**: 變更詳情表
- **audit_log_access**: 日誌存取記錄表（元稽核）
- **audit_compliance_reports**: 合規性報告表

#### 關鍵設計特點

1. **分區策略**: 按月分區，提升查詢性能和資料管理效率
2. **索引優化**: 針對常見查詢模式創建複合索引
3. **JSONB支援**: 靈活存儲變更前後的值和額外資料
4. **元稽核**: 記錄誰查看了稽核日誌
5. **自動清理**: 根據保留政策自動清理過期資料

### 2. 應用程式層

#### 服務架構

- **AuditLogService**: 核心稽核日誌服務
- **AuditSecurityService**: 安全性和權限控制服務
- **AuditLogBackgroundService**: 背景處理服務

#### 主要功能

1. **非同步記錄**: 使用 Channel 進行高性能非同步處理
2. **批次操作**: 支援關聯多個操作的批次記錄
3. **風險評估**: 自動計算操作風險評分
4. **資料遮罩**: 根據使用者權限遮罩敏感資料
5. **合規性檢查**: 支援 GDPR、HIPAA、SOX 等法規

### 3. API層

#### 控制器功能

- **查詢和過濾**: 支援多條件查詢和分頁
- **統計分析**: 提供詳細的使用統計和趨勢分析
- **資料匯出**: 支援 CSV 和 JSON 格式匯出
- **合規性報告**: 自動生成法規遵循報告
- **系統管理**: 清理、歸檔和系統狀態監控

#### 安全性控制

1. **角色為基礎的存取控制** (RBAC)
2. **資料列層級安全性** (RLS)
3. **敏感資料自動遮罩**
4. **稽核存取的元稽核**

### 4. 前端層

#### 使用者介面組件

- **稽核日誌檢視器**: 主要的日誌查詢和顯示介面
- **過濾器面板**: 多條件過濾功能
- **統計儀表板**: 視覺化統計資料
- **詳情模態框**: 單一日誌記錄的詳細檢視

## 實施步驟

### 階段 1: 資料庫設置

1. **執行遷移腳本**
   ```bash
   # 在 PostgreSQL 中執行
   psql -d familytree -f Database/migrations/001_create_audit_log_tables.sql
   psql -d familytree -f Database/migrations/002_create_audit_indexes.sql
   ```

2. **驗證表格建立**
   ```sql
   -- 檢查主要表格
   SELECT tablename FROM pg_tables WHERE tablename LIKE 'audit_%';
   
   -- 檢查分區
   SELECT schemaname, tablename, tableowner 
   FROM pg_tables 
   WHERE tablename LIKE 'audit_logs_%';
   
   -- 檢查索引
   SELECT indexname, tablename 
   FROM pg_indexes 
   WHERE tablename LIKE 'audit_%';
   ```

3. **權限設置**
   ```sql
   -- 為應用程式使用者授予權限
   GRANT audit_writer TO your_app_user;
   
   -- 為稽核管理員授予權限
   GRANT audit_admin TO your_audit_admin;
   
   -- 為稽核讀取者授予權限
   GRANT audit_reader TO your_audit_reader;
   ```

### 階段 2: 後端服務整合

1. **註冊服務**

   在 `Program.cs` 或 `Startup.cs` 中：
   ```csharp
   using familytree_backend.Extensions;
   
   // 在 ConfigureServices 方法中
   services.AddAuditLogServices(configuration);
   
   // 在 Configure 方法中（在 Authentication 之後）
   app.UseAuthentication();
   app.UseAuditLogMiddleware(); // 新增這行
   app.UseAuthorization();
   ```

2. **更新現有控制器**

   在需要稽核的控制器中注入服務：
   ```csharp
   private readonly IAuditLogService _auditLogService;
   
   public YourController(IAuditLogService auditLogService)
   {
       _auditLogService = auditLogService;
   }
   
   // 在操作方法中記錄稽核
   await _auditLogService.LogDataOperationAsync(
       "CREATE", "Person", personId, null, newPerson, "建立新人員記錄");
   ```

3. **配置檔案更新**

   確保 `appsettings.json` 包含稽核配置：
   ```json
   {
     "AuditLog": {
       "EnableAutoAudit": true,
       "LogApiCalls": true,
       // ... 其他配置
     }
   }
   ```

### 階段 3: 前端介面整合

1. **路由配置**

   在 `app.routes.ts` 中新增路由：
   ```typescript
   {
     path: 'audit-logs',
     component: AuditLogViewerComponent,
     canActivate: [AuthGuard, PermissionGuard],
     data: { requiredPermission: 'audit:read' }
   }
   ```

2. **選單整合**

   在導覽選單中新增稽核日誌選項（僅管理員可見）：
   ```html
   <li *ngIf="hasPermission('audit:read')">
     <a routerLink="/audit-logs">稽核日誌</a>
   </li>
   ```

3. **服務註冊**

   確保 `AuditLogService` 在應用程式模組中提供：
   ```typescript
   @NgModule({
     providers: [
       AuditLogService,
       // ... 其他服務
     ]
   })
   ```

### 階段 4: 測試和驗證

1. **功能測試**
   - 使用者登入/登出記錄
   - 資料CRUD操作記錄
   - 檔案上傳/下載記錄
   - 權限變更記錄

2. **權限測試**
   - 不同角色的存取權限
   - 敏感資料遮罩功能
   - 跨使用者資料存取限制

3. **效能測試**
   - 大量日誌查詢效能
   - 分區查詢效能
   - 索引使用效果

4. **安全性測試**
   - SQL 注入防護
   - 權限繞過測試
   - 敏感資料洩露檢查

## 運營維護

### 定期維護任務

1. **每日任務**
   ```sql
   -- 檢查系統狀態
   SELECT * FROM audit_logs_summary WHERE log_date = CURRENT_DATE;
   
   -- 檢查可疑活動
   SELECT * FROM audit_logs 
   WHERE is_suspicious = true 
   AND occurred_at >= CURRENT_DATE;
   ```

2. **每週任務**
   ```sql
   -- 執行索引維護
   SELECT maintain_audit_indexes();
   
   -- 檢查索引使用情況
   SELECT * FROM audit_index_usage WHERE usage_level = 'UNUSED';
   ```

3. **每月任務**
   ```sql
   -- 清理過期日誌
   SELECT cleanup_old_audit_logs();
   
   -- 創建下個月的分區
   SELECT create_monthly_partition(CURRENT_DATE + INTERVAL '1 month');
   
   -- 生成合規性報告
   -- 通過 API 或管理介面執行
   ```

### 監控和警報

1. **關鍵指標監控**
   - 每小時日誌記錄數量
   - 失敗操作比例
   - 可疑活動數量
   - 系統回應時間

2. **警報設置**
   - 可疑活動激增
   - 系統錯誤率過高
   - 磁碟空間不足
   - 分區建立失敗

### 故障排除

1. **常見問題**

   **問題**: 稽核日誌記錄失敗
   ```sql
   -- 檢查權限
   SELECT has_table_privilege('your_app_user', 'audit_logs', 'INSERT');
   
   -- 檢查分區
   SELECT tablename FROM pg_tables WHERE tablename LIKE 'audit_logs_%';
   ```

   **問題**: 查詢效能差
   ```sql
   -- 檢查索引使用
   EXPLAIN (ANALYZE, BUFFERS) 
   SELECT * FROM audit_logs 
   WHERE user_id = 'user123' 
   AND occurred_at >= '2025-01-01';
   
   -- 檢查統計資訊
   SELECT last_analyze FROM pg_stat_user_tables WHERE relname = 'audit_logs';
   ```

2. **效能調優**
   - 檢查慢查詢日誌
   - 優化索引策略
   - 調整分區策略
   - 考慮歸檔舊資料

## 安全性考量

### 資料保護

1. **傳輸加密**: 使用 HTTPS/TLS
2. **靜態加密**: 考慮資料庫層加密
3. **存取控制**: 嚴格的角色權限管理
4. **資料遮罩**: 自動遮罩 PII 和敏感資料

### 合規性

1. **GDPR 遵循**
   - 資料主體權利
   - 資料處理透明度
   - 資料最小化原則

2. **其他法規**
   - HIPAA (醫療資料)
   - SOX (財務資料)
   - PCI DSS (支付資料)

### 稽核稽核系統

1. **元稽核**: 記錄誰存取了稽核日誌
2. **完整性檢查**: 防止日誌竄改
3. **備份策略**: 定期備份重要稽核資料

## 擴展和客製化

### 自定義事件類型

1. **新增事件類型**
   ```sql
   INSERT INTO audit_event_types 
   (code, name, description, category, severity, retention_days)
   VALUES 
   ('CUSTOM_EVENT', '自定義事件', '描述', 'BUSINESS', 'INFO', 90);
   ```

2. **程式碼中使用**
   ```csharp
   await _auditLogService.LogEventAsync("CUSTOM_EVENT", "CUSTOM_ACTION", context);
   ```

### 自定義報告

1. **建立報告檢視**
   ```sql
   CREATE VIEW custom_audit_report AS
   SELECT ...
   FROM audit_logs
   WHERE ...;
   ```

2. **API 端點擴展**
   ```csharp
   [HttpGet("custom-report")]
   public async Task<IActionResult> GetCustomReport(...)
   {
       // 實作自定義報告邏輯
   }
   ```

## 總結

此稽核日誌系統提供了：

1. **完整的追蹤能力**: 記錄所有重要的系統操作
2. **強大的查詢功能**: 支援複雜的過濾和搜尋
3. **安全性保護**: 防止資料洩露和未授權存取
4. **合規性支援**: 滿足各種法規要求
5. **高效能設計**: 支援大量資料和高併發

透過遵循本實施指南，您的家族樹管理系統將具備企業級的稽核追蹤能力，確保資料安全性和法規遵循。