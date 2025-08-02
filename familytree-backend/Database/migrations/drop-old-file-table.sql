-- 刪除舊的檔案上傳表
-- 執行日期: 2025-08-02
-- 注意：執行前請確保所有資料已經遷移到 file_uploads 表

-- 先檢查遷移狀態
SELECT 
    'FINAL CHECK BEFORE DROP' as step,
    (SELECT COUNT(*) FROM user_update_file) as old_table_count,
    (SELECT COUNT(*) FROM file_uploads) as new_table_count,
    CASE 
        WHEN (SELECT COUNT(*) FROM user_update_file) = (SELECT COUNT(*) FROM file_uploads)
        THEN '✅ All data migrated'
        ELSE '❌ Data migration incomplete - DO NOT PROCEED'
    END as status;

-- 確認資料已經全部遷移
DO $$
DECLARE
    old_count INTEGER;
    new_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO old_count FROM user_update_file;
    SELECT COUNT(*) INTO new_count FROM file_uploads;
    
    IF old_count > new_count THEN
        RAISE EXCEPTION '資料遷移不完整！舊表有 % 筆記錄，新表只有 % 筆記錄', old_count, new_count;
    END IF;
END $$;

-- 刪除相容性檢視
DROP VIEW IF EXISTS user_update_file_compat;

-- 刪除舊表（如果你真的確定要刪除）
-- 註解掉以防止意外執行
-- DROP TABLE IF EXISTS user_update_file;

-- 為了安全起見，我們先重命名表而不是直接刪除
ALTER TABLE user_update_file RENAME TO user_update_file_archived_20250802;

-- 記錄操作
INSERT INTO data_migration_log (migration_name, executed_by, status, records_affected, notes)
VALUES (
    'DropOldFileTable-Archive', 
    'system', 
    'completed',
    (SELECT COUNT(*) FROM user_update_file_archived_20250802),
    'Archived user_update_file table to user_update_file_archived_20250802. All data migrated to file_uploads. Table can be dropped after verification.'
);

-- 顯示最終狀態
SELECT 
    'POST-ARCHIVE STATUS' as step,
    EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user_update_file') as old_table_exists,
    EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'user_update_file_archived_20250802') as archived_table_exists,
    EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'file_uploads') as new_table_exists;

-- 如果確定要永久刪除存檔表，可以執行：
-- DROP TABLE IF EXISTS user_update_file_archived_20250802;