-- 將舊系統的角色遷移到新的角色系統

-- 1. 為所有現有用戶分配對應的角色
INSERT INTO user_roles (user_id, role_id, assigned_at, assigned_by)
SELECT 
    u.id,
    CASE 
        WHEN u.role = 'admin' THEN 'admin'
        WHEN u.role = 'user' THEN 'user'
        ELSE 'guest'
    END as role_id,
    CURRENT_TIMESTAMP,
    u.id -- 自己分配給自己
FROM users u
WHERE NOT EXISTS (
    SELECT 1 FROM user_roles ur 
    WHERE ur.user_id = u.id
);

-- 2. 顯示遷移結果
SELECT 
    u.username,
    u.email,
    u.role as old_role,
    ur.role_id as new_role,
    r.display_name as role_display_name
FROM users u
LEFT JOIN user_roles ur ON u.id = ur.user_id
LEFT JOIN roles r ON ur.role_id = r.id
ORDER BY u.created_at DESC;