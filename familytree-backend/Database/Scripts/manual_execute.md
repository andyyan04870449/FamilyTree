# 手動執行資料庫遷移

## 資料庫連線資訊
- Host: localhost
- Port: 5432
- Database: familytree
- Username: user
- Password: (請輸入您的密碼)

## 快速執行指令

### 1. 使用自動腳本
```bash
cd /Users/yangandy/FamilyTree/familytree-backend/Database/Scripts
./execute_migration.sh
# 執行時會提示輸入密碼
```

### 2. 手動執行（如果您知道密碼）

#### 備份資料庫
```bash
pg_dump -h localhost -p 5432 -U user -d familytree > backup_$(date +%Y%m%d_%H%M%S).sql
```

#### 執行遷移腳本
```bash
# 設定密碼環境變數
export PGPASSWORD='您的密碼'

# 執行 SQL 檔案
psql -h localhost -p 5432 -U user -d familytree -f 01_create_user_tables.sql
psql -h localhost -p 5432 -U user -d familytree -f 02_alter_existing_tables.sql
psql -h localhost -p 5432 -U user -d familytree -f 03_create_views_functions.sql

# 清除密碼環境變數
unset PGPASSWORD
```

### 3. 使用 psql 互動模式
```bash
psql -h localhost -p 5432 -U user -d familytree

# 在 psql 提示符下執行
\i 01_create_user_tables.sql
\i 02_alter_existing_tables.sql
\i 03_create_views_functions.sql
\q
```

## 執行後驗證

```bash
psql -h localhost -p 5432 -U user -d familytree -c "SELECT table_name FROM information_schema.tables WHERE table_name IN ('users', 'user_tokens', 'activity_logs');"
```

## 注意事項
1. 執行前請確認密碼正確
2. 建議先備份資料庫
3. 如果遇到權限問題，請確認使用者有建立表格的權限