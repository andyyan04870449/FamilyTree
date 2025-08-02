# 檔案命名標準化 - 舊系統清理計畫

## 📋 清理階段規劃

### Phase 1: 立即清理 (✅ 建議立即執行)
這些是相對安全的清理項目，不會影響系統運作：

#### 1.1 程式碼層面
- [ ] 標記舊 FileUploadController 為 [Obsolete]
- [ ] 標記舊 FileUploadService 為 [Obsolete]  
- [ ] 標記舊 FileUploadModel 為 [Obsolete]

#### 1.2 文檔更新
- [ ] 更新 API 文檔指向新端點
- [ ] 新增遷移說明文檔

### Phase 2: 謹慎清理 (⚠️ 需要測試後執行)
這些需要確認沒有依賴後才能清理：

#### 2.1 資料庫清理準備
```sql
-- 檢查還有哪些程式碼在使用舊表
SELECT 
    schemaname, 
    viewname, 
    definition 
FROM pg_views 
WHERE definition LIKE '%user_update_file%' 
AND schemaname = 'public';
```

#### 2.2 相依性檢查
- [ ] ExcelProcessingService.cs 中的 user_update_file 參考
- [ ] PhotoUploadService.cs 中的 user_update_file 參考  
- [ ] FileUploadService.cs 中的 user_update_file 參考

### Phase 3: 完全移除 (🚨 最後階段，需要充分測試)
這些只有在確認新系統完全穩定後才執行：

#### 3.1 資料庫物件移除
```sql
-- 移除舊的外鍵約束
ALTER TABLE user_update_file DROP CONSTRAINT IF EXISTS fk_user_update_file_project;

-- 移除舊表 (保留備份表)
DROP TABLE IF EXISTS user_update_file;

-- 清理舊的序列
DROP SEQUENCE IF EXISTS user_update_file_id_seq;
```

#### 3.2 程式碼完全移除
- [ ] 刪除 FileUploadController.cs (舊版)
- [ ] 刪除 FileUploadService.cs (舊版)
- [ ] 刪除 FileUploadModel.cs (舊版)

## 🎯 建議的清理順序

### 第一步：立即執行 (現在可以做)
```csharp
// 在舊控制器上新增 Obsolete 標記
[Obsolete("請使用新的 /api/file 端點。此端點將在 v2.1 中移除。", false)]
[Route("api/file-upload")]
public class FileUploadController : ControllerBase
```

### 第二步：程式碼相依性更新 (需要修改程式碼)
更新以下服務以使用新的 FileService：
1. ExcelProcessingService
2. PhotoUploadService  
3. 其他使用 FileUploadService 的地方

### 第三步：測試期 (2-4 週)
- 新 API 完全測試
- 監控系統穩定性
- 確認沒有舊 API 的呼叫

### 第四步：完全移除 (測試通過後)
- 移除舊資料表
- 刪除舊程式碼
- 清理相關檔案

## 📊 目前狀況分析

### 安全保留項目 (建議保留)
- `user_update_file_backup_20250802` - 備份表，建議保留至少 3 個月
- `user_update_file_compat` - 相容性檢視，在確認沒有舊程式碼使用前保留

### 可以立即清理的項目
- 無，目前所有項目都建議保留直到新系統穩定

### 需要程式碼更新的項目
1. **ExcelProcessingService.cs** - 需要更新以使用新的檔案 ID
2. **PhotoUploadService.cs** - 需要檢查是否使用舊的檔案參考
3. **FileUploadService.cs** - 需要標記為過時或完全移除

## 🔧 清理腳本

### 標記舊程式碼為過時 (立即可執行)
```sql
-- 新增註解到舊表說明其狀態
COMMENT ON TABLE user_update_file IS 'DEPRECATED: 此表已被 file_uploads 取代。請使用新的檔案管理 API。計畫在 v2.1 移除。';
```

### 程式碼更新檢查清單
- [ ] 更新所有 `user_update_file` 的 SQL 查詢
- [ ] 更新所有使用整數檔案 ID 的程式碼
- [ ] 更新所有 `project_id` 參數為 `associated_record_id/type`
- [ ] 測試所有檔案相關功能

### 最終清理腳本 (稍後執行)
```sql
-- 在確認新系統穩定後執行 (建議 4-8 週後)
-- DROP TABLE user_update_file CASCADE;
-- DROP VIEW user_update_file_compat;
-- 保留備份表 user_update_file_backup_20250802 至少 6 個月
```

## ⚡ 立即行動項目

目前建議立即執行的安全清理：

1. **標記舊程式碼為過時**
2. **更新程式碼註解和文檔**  
3. **開始更新相依程式碼**
4. **進行新系統的完整測試**

## 🚨 重要提醒

- **不要立即刪除舊表** - 需要確認新系統完全穩定
- **保留備份表至少 3 個月**
- **分階段執行，每階段充分測試**
- **監控新系統的性能和穩定性**
- **準備回滾計畫以防萬一**

現階段重點應該放在**程式碼更新**和**測試**，而不是急於刪除舊的資料庫物件。