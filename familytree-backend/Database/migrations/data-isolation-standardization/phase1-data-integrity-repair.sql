-- Phase 1: 資料完整性修復
-- 統一採用 user_id 隔離策略
-- 執行日期: 2025-08-02
-- 相關 Issue: https://github.com/andyyan04870449/FamilyTree/issues/2

-- =====================================================
-- 第一步：備份當前狀態（安全措施）
-- =====================================================

-- 創建備份表
CREATE TABLE person_profile_backup_20250802 AS 
SELECT * FROM person_profile;

CREATE TABLE relationship_layers_backup_20250802 AS 
SELECT * FROM relationship_layers;

CREATE TABLE user_favorites_backup_20250802 AS 
SELECT * FROM user_favorites;

-- =====================================================
-- 第二步：分析和驗證資料
-- =====================================================

-- 檢查需要修復的記錄
SELECT 
    'person_profile' as table_name,
    COUNT(*) as total_records,
    COUNT(CASE WHEN user_id IS NULL AND project_id IS NOT NULL THEN 1 END) as needs_user_id_fix,
    COUNT(CASE WHEN user_id IS NOT NULL AND project_id IS NOT NULL THEN 1 END) as has_both,
    COUNT(CASE WHEN user_id IS NULL AND project_id IS NULL THEN 1 END) as orphaned_records
FROM person_profile;

-- 檢查 projects 表的完整性
SELECT 
    p.id as project_id,
    p.project_name,
    p.user_id,
    u.username
FROM projects p
LEFT JOIN users u ON p.user_id = u.id
ORDER BY p.created_at;

-- =====================================================
-- 第三步：修復缺失的 user_id
-- =====================================================

-- 修復 person_profile 表
UPDATE person_profile 
SET user_id = (
    SELECT p.user_id 
    FROM projects p 
    WHERE p.id = person_profile.project_id
)
WHERE user_id IS NULL 
  AND project_id IS NOT NULL
  AND EXISTS (
      SELECT 1 FROM projects p 
      WHERE p.id = person_profile.project_id 
        AND p.user_id IS NOT NULL
  );

-- 驗證修復結果
SELECT 
    'AFTER_FIX: person_profile' as status,
    COUNT(*) as total_records,
    COUNT(CASE WHEN user_id IS NULL THEN 1 END) as missing_user_id,
    COUNT(CASE WHEN project_id IS NULL THEN 1 END) as missing_project_id,
    COUNT(CASE WHEN user_id IS NOT NULL AND project_id IS NOT NULL THEN 1 END) as properly_isolated
FROM person_profile;

-- =====================================================
-- 第四步：處理孤立記錄（如果有的話）
-- =====================================================

-- 檢查是否有孤立記錄（沒有有效project_id的記錄）
SELECT 
    pp.id,
    pp.name,
    pp.project_id,
    pp.user_id,
    'ORPHANED - Invalid project_id' as issue
FROM person_profile pp
LEFT JOIN projects p ON pp.project_id = p.id
WHERE pp.project_id IS NOT NULL 
  AND p.id IS NULL;

-- 如果有孤立記錄，需要手動處理或分配給預設使用者
-- 這裡我們記錄但不自動修復，需要業務決策

-- =====================================================
-- 第五步：添加資料完整性約束（準備階段）
-- =====================================================

-- 檢查是否可以安全添加 NOT NULL 約束
SELECT 
    table_name,
    column_name,
    COUNT(*) as total_records,
    COUNT(CASE WHEN user_id IS NULL THEN 1 END) as null_user_ids
FROM (
    SELECT 'person_profile' as table_name, 'user_id' as column_name, user_id FROM person_profile
    UNION ALL
    SELECT 'relationship_layers', 'user_id', user_id FROM relationship_layers
    UNION ALL
    SELECT 'user_favorites', 'user_id', user_id FROM user_favorites
) combined
GROUP BY table_name, column_name;

-- =====================================================
-- 第六步：記錄修復日誌
-- =====================================================

-- 創建修復日誌表（如果不存在）
CREATE TABLE IF NOT EXISTS data_migration_log (
    id SERIAL PRIMARY KEY,
    migration_name VARCHAR(200) NOT NULL,
    executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    executed_by VARCHAR(100),
    status VARCHAR(50),
    records_affected INTEGER,
    notes TEXT
);

-- 記錄本次修復
INSERT INTO data_migration_log (migration_name, executed_by, status, records_affected, notes)
VALUES (
    'Phase1-DataIsolationStandardization', 
    'system', 
    'completed',
    (SELECT COUNT(*) FROM person_profile WHERE user_id IS NOT NULL),
    'Fixed missing user_id in person_profile table. Unified data isolation strategy to use user_id as primary isolation mechanism.'
);

-- =====================================================
-- 驗證和報告
-- =====================================================

-- 最終驗證報告
SELECT 
    '=== MIGRATION PHASE 1 SUMMARY ===' as report_section,
    '' as details
UNION ALL
SELECT 
    'person_profile records processed:',
    COUNT(*)::text
FROM person_profile
UNION ALL
SELECT 
    'Records with valid user_id:',
    COUNT(CASE WHEN user_id IS NOT NULL THEN 1 END)::text
FROM person_profile
UNION ALL
SELECT 
    'Records with both user_id and project_id:',
    COUNT(CASE WHEN user_id IS NOT NULL AND project_id IS NOT NULL THEN 1 END)::text
FROM person_profile
UNION ALL
SELECT 
    'Migration status:',
    CASE 
        WHEN (SELECT COUNT(*) FROM person_profile WHERE user_id IS NULL) = 0 
        THEN '✅ SUCCESS - All records have user_id'
        ELSE '❌ INCOMPLETE - Some records still missing user_id'
    END;

-- 顯示修復統計
SELECT 
    table_name,
    total_records,
    records_with_user_id,
    records_with_project_id,
    records_with_both,
    CASE 
        WHEN records_with_user_id = total_records THEN '✅ READY'
        ELSE '⚠️ NEEDS ATTENTION'
    END as status
FROM (
    SELECT 
        'person_profile' as table_name,
        COUNT(*) as total_records,
        COUNT(CASE WHEN user_id IS NOT NULL THEN 1 END) as records_with_user_id,
        COUNT(CASE WHEN project_id IS NOT NULL THEN 1 END) as records_with_project_id,
        COUNT(CASE WHEN user_id IS NOT NULL AND project_id IS NOT NULL THEN 1 END) as records_with_both
    FROM person_profile
    
    UNION ALL
    
    SELECT 
        'relationship_layers',
        COUNT(*),
        COUNT(CASE WHEN user_id IS NOT NULL THEN 1 END),
        COUNT(CASE WHEN project_id IS NOT NULL THEN 1 END),
        COUNT(CASE WHEN user_id IS NOT NULL AND project_id IS NOT NULL THEN 1 END)
    FROM relationship_layers
    
    UNION ALL
    
    SELECT 
        'user_favorites',
        COUNT(*),
        COUNT(CASE WHEN user_id IS NOT NULL THEN 1 END),
        COUNT(CASE WHEN project_id IS NOT NULL THEN 1 END),
        COUNT(CASE WHEN user_id IS NOT NULL AND project_id IS NOT NULL THEN 1 END)
    FROM user_favorites
) stats;