# 資料庫遷移指南

## 檔案說明

1. **01_create_user_tables.sql** - 建立新的使用者管理相關資料表
   - users 表（使用者基本資料）
   - user_tokens 表（JWT Token 管理）
   - activity_logs 表（操作日誌）

2. **02_alter_existing_tables.sql** - 修改現有資料表
   - 為所有業務資料表加入 user_id 欄位
   - 將現有資料關聯到預設管理員

3. **03_create_views_functions.sql** - 建立輔助物件
   - 使用者統計檢視
   - 權限檢查函數
   - 活動日誌函數

4. **execute_migration.sh** - 自動執行腳本

## 執行步驟

### 方法一：使用自動腳本（推薦）

```bash
cd /Users/yangandy/FamilyTree/familytree-backend/Database/Scripts
./execute_migration.sh
```

腳本會自動：
- 備份現有資料庫
- 依序執行三個 SQL 檔案
- 驗證執行結果

### 方法二：手動執行

```bash
# 1. 備份資料庫
pg_dump -h localhost -U user -d familytree > backup_$(date +%Y%m%d_%H%M%S).sql

# 2. 執行 SQL 檔案
psql -h localhost -U user -d familytree -f 01_create_user_tables.sql
psql -h localhost -U user -d familytree -f 02_alter_existing_tables.sql
psql -h localhost -U user -d familytree -f 03_create_views_functions.sql
```

## 執行後檢查

### 1. 確認資料表建立
```sql
SELECT table_name FROM information_schema.tables 
WHERE table_name IN ('users', 'user_tokens', 'activity_logs');
```

### 2. 確認 user_id 欄位
```sql
SELECT table_name, column_name 
FROM information_schema.columns 
WHERE column_name = 'user_id' 
ORDER BY table_name;
```

### 3. 確認預設管理員
```sql
SELECT * FROM users WHERE username = 'admin';
```

## 重要提醒

1. **更新管理員密碼**
   - 預設管理員帳號：admin
   - 需要在應用程式中設定真實密碼

2. **資料遷移**
   - 所有現有資料會關聯到預設管理員（admin_default）
   - 可根據需要調整資料歸屬

3. **備份還原**
   ```bash
   # 如需還原
   psql -h localhost -U user -d familytree < backup_file.sql
   ```

## 預設帳號

- **使用者名稱**: admin
- **Email**: admin@familytree.com
- **角色**: admin
- **密碼**: 需要在應用程式中設定

## 注意事項

1. 執行前請確保有足夠的資料庫權限
2. 建議先在測試環境執行
3. 確認備份檔案正確產生
4. 如遇錯誤，使用備份檔案還原