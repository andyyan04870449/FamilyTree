# 檔案刪除功能修復測試指南

## 修復內容總結

### 1. Controller 層修復
- **新增彈性路由**: 支援 GUID 和字串格式的 fileId
- **改進錯誤處理**: 詳細的GUID驗證和錯誤日誌
- **增強日誌記錄**: 追蹤整個刪除流程

### 2. Service 層修復
- **改進軟刪除邏輯**: 更好的實體檔案和資料庫狀態管理
- **增強錯誤處理**: 分別處理實體檔案和資料庫操作的錯誤
- **修復列表查詢**: 排除已刪除檔案，避免重複顯示

### 3. 關鍵修復點
```csharp
// 新增的路由支援
[HttpDelete("{fileId:guid}")]     // 原有的 GUID 路由
[HttpDelete("{fileId}")]          // 新增的字串路由（會驗證GUID）

// 修復的檔案列表查詢
var whereConditions = new List<string> { 
    "user_id = @userId", 
    "upload_status != @deletedStatus"   // 新增：排除已刪除檔案
};
```

## 測試步驟

### 前置準備
1. 確保應用程式已重新編譯和部署
2. 檢查資料庫中有一些測試檔案
3. 確認前端檔案ID提取邏輯正確

### 測試案例

#### 測試案例 1: 正常刪除流程
1. **操作**: 上傳一個Excel檔案
2. **驗證**: 檔案出現在列表中
3. **操作**: 點擊刪除按鈕
4. **預期結果**: 
   - 前端顯示刪除成功訊息
   - 檔案從列表中消失
   - 後端日誌顯示完整刪除流程
   - 實體檔案被刪除
   - 資料庫記錄狀態更新為 'deleted'

#### 測試案例 2: GUID 格式驗證
1. **操作**: 手動發送無效的 fileId 格式 (例如: "invalid-id")
2. **預期結果**: 
   - 返回 400 Bad Request
   - 錯誤訊息: "無效的檔案ID格式: invalid-id"

#### 測試案例 3: 不存在的檔案
1. **操作**: 嘗試刪除不存在的檔案ID
2. **預期結果**: 
   - 返回 400 Bad Request
   - 錯誤訊息: "找不到指定的檔案"

#### 測試案例 4: 權限驗證
1. **操作**: 嘗試刪除其他使用者的檔案
2. **預期結果**: 
   - 返回 400 Bad Request
   - 錯誤訊息: "找不到指定的檔案"

### 檢查要點

#### 後端日誌檢查
查看以下日誌訊息：
```
- "開始刪除檔案: FileId={FileId}, UserId={UserId}"
- "找到要刪除的檔案: {FileName}, 路徑: {FilePath}"
- "實體檔案刪除成功: {FilePath}"
- "檔案刪除操作完成: {FileName}, FileId={FileId}"
```

#### 資料庫檢查
```sql
-- 檢查檔案狀態是否正確更新
SELECT file_id, original_filename, upload_status, updated_at 
FROM file_uploads 
WHERE upload_status = 'deleted' 
ORDER BY updated_at DESC;

-- 確認檔案列表查詢不包含已刪除檔案
SELECT COUNT(*) FROM file_uploads 
WHERE user_id = 'your-user-id' AND upload_status != 'deleted';
```

#### 檔案系統檢查
```bash
# 檢查上傳目錄，確認檔案已被實際刪除
ls -la user_upload/
```

### 常見問題排除

#### 問題1: 前端仍然顯示檔案刪除失敗
- **檢查**: fileId 格式是否正確
- **解決**: 確認前端從 "excel_321321" 中提取的是有效的GUID

#### 問題2: 檔案從列表消失但實體檔案仍存在
- **檢查**: 檔案路徑權限和錯誤日誌
- **解決**: 確認應用程式對檔案目錄有寫入權限

#### 問題3: 資料庫更新失敗
- **檢查**: 使用者權限和檔案所有權
- **解決**: 確認 userId 匹配和檔案存在

## API 端點測試

### 使用 Postman 或 curl 測試

```bash
# 測試刪除 API (需要有效的 JWT token)
curl -X DELETE "http://localhost:5000/api/File/{valid-file-id}" \
  -H "Authorization: Bearer {your-jwt-token}" \
  -H "Content-Type: application/json"

# 預期回應
{
  "success": true,
  "message": "成功刪除檔案: filename.xlsx"
}
```

## 修復檔案位置

- **Controller**: `/Users/yangandy/FamilyTree/familytree-backend/Controllers/FileController.cs`
- **Service**: `/Users/yangandy/FamilyTree/familytree-backend/Services/FileUploadService.cs`

## 後續建議

1. **監控**: 部署後監控刪除操作的日誌，確保沒有異常
2. **測試**: 在不同環境中測試，特別是檔案權限不同的情況
3. **文檔**: 更新API文檔，說明檔案刪除的行為（軟刪除 + 實體檔案刪除）
4. **前端**: 考慮在前端添加刪除確認對話框，提高用戶體驗