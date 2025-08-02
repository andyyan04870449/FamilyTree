-- Phase 1b: 添加資料完整性約束
-- 確保所有混合隔離表的user_id不為空
-- 執行日期: 2025-08-02

-- =====================================================
-- 添加 NOT NULL 約束
-- =====================================================

-- person_profile 表
ALTER TABLE person_profile 
    ALTER COLUMN user_id SET NOT NULL;

-- relationship_layers 表
ALTER TABLE relationship_layers 
    ALTER COLUMN user_id SET NOT NULL;

-- user_favorites 表
ALTER TABLE user_favorites 
    ALTER COLUMN user_id SET NOT NULL;

-- =====================================================
-- 添加檢查約束（確保邏輯一致性）
-- =====================================================

-- 確保 user_id 和 project_id 的關聯正確
ALTER TABLE person_profile 
ADD CONSTRAINT chk_person_profile_user_project_consistency 
CHECK (
    user_id IS NOT NULL AND 
    project_id IS NOT NULL AND
    EXISTS (
        SELECT 1 FROM projects p 
        WHERE p.id = project_id AND p.user_id = person_profile.user_id
    )
);

-- 為 relationship_layers 添加類似約束
ALTER TABLE relationship_layers 
ADD CONSTRAINT chk_relationship_layers_user_project_consistency 
CHECK (
    user_id IS NOT NULL AND 
    project_id IS NOT NULL AND
    EXISTS (
        SELECT 1 FROM projects p 
        WHERE p.id = project_id AND p.user_id = relationship_layers.user_id
    )
);

-- 為 user_favorites 添加類似約束
ALTER TABLE user_favorites 
ADD CONSTRAINT chk_user_favorites_user_project_consistency 
CHECK (
    user_id IS NOT NULL AND 
    project_id IS NOT NULL AND
    EXISTS (
        SELECT 1 FROM projects p 
        WHERE p.id = project_id AND p.user_id = user_favorites.user_id
    )
);

-- =====================================================
-- 驗證約束
-- =====================================================

-- 測試約束是否正常工作
SELECT 
    'Constraint validation:' as test_type,
    'All tables now have NOT NULL user_id' as result;

-- 顯示新增的約束
SELECT 
    tc.table_name,
    tc.constraint_name,
    tc.constraint_type,
    cc.check_clause
FROM information_schema.table_constraints tc
LEFT JOIN information_schema.check_constraints cc 
    ON tc.constraint_name = cc.constraint_name
WHERE tc.table_schema = 'public' 
    AND tc.table_name IN ('person_profile', 'relationship_layers', 'user_favorites')
    AND (tc.constraint_type = 'CHECK' OR 
         (tc.constraint_type = 'NOT NULL' AND tc.constraint_name LIKE '%user_id%'))
ORDER BY tc.table_name, tc.constraint_type;