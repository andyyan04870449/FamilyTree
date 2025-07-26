# FamilyTree 專案 - 資料庫建立指南

本文件說明如何在新環境中建立和初始化 FamilyTree 專案所需的 PostgreSQL 資料庫。

## 先決條件

- 已安裝 PostgreSQL (建議版本 15 或更高)。
- 已安裝 `psql` 命令列工具。
- 您擁有建立新資料庫的權限。

## 步驟

### 1. 建立新的資料庫

首先，您需要建立一個名為 `familytree` 的空資料庫。您可以使用 `createdb` 指令，或透過 `psql` 來執行。

**方法一：使用 `createdb`**
```bash
createdb -U <您的使用者名稱> -h localhost familytree
```

**方法二：使用 `psql`**
```bash
psql -U <您的使用者名稱> -h localhost -d postgres
```
然後在 psql 提示字元中執行：
```sql
CREATE DATABASE familytree;
\q
```

### 2. 建立專案使用者 (可選，但建議)

為了安全起見，建議為此專案建立一個專屬的使用者 `user` 並設定密碼。

```bash
psql -U <您的使用者名稱> -h localhost -d familytree
```
在 psql 提示字元中執行：
```sql
CREATE USER user WITH PASSWORD 'password123';
GRANT ALL PRIVILEGES ON DATABASE familytree TO user;
```
若要將所有現有資料表的權限賦予新使用者，可以執行以下指令：
```sql
GRANT ALL ON ALL TABLES IN SCHEMA public TO user;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO user;
GRANT ALL ON ALL FUNCTIONS IN SCHEMA public TO user;
\q
```
**注意**: `<您的使用者名稱>` 通常是 `postgres`。

### 3. 匯入資料庫結構

現在，我們將使用此目錄下的 `FULL_SCHEMA.sql` 檔案來建立所有的資料表和結構。

執行以下指令，它會將 `FULL_SCHEMA.sql` 的內容匯入到您剛剛建立的 `familytree` 資料庫中。

```bash
psql -U user -d familytree -h localhost -f FULL_SCHEMA.sql
```
系統可能會提示您輸入 `user` 的密碼 (`password123`)。

### 4. 驗證

匯入完成後，您可以連線到資料庫並檢查資料表是否都已成功建立。

```bash
psql -U user -d familytree -h localhost
```
在 psql 提示字元中執行 `\dt` 來列出所有資料表：
```
\dt
```
如果您能看到如 `person_profile`, `relationships`, `projects` 等資料表，代表設定已成功完成！ 