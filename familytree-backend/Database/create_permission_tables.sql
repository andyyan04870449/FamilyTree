-- 創建角色表
CREATE TABLE IF NOT EXISTS roles (
    id VARCHAR(50) PRIMARY KEY,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    level INTEGER NOT NULL DEFAULT 0,
    is_system BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 創建使用者角色關聯表
CREATE TABLE IF NOT EXISTS user_roles (
    user_id VARCHAR(50) NOT NULL,
    role_id VARCHAR(50) NOT NULL,
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    assigned_by VARCHAR(50),
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (role_id) REFERENCES roles(id)
);

-- 創建權限表
CREATE TABLE IF NOT EXISTS permissions (
    id SERIAL PRIMARY KEY,
    permission VARCHAR(100) NOT NULL UNIQUE,
    display_name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(50) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 創建角色權限關聯表
CREATE TABLE IF NOT EXISTS role_permissions (
    role_id VARCHAR(50) NOT NULL,
    permission_id INTEGER NOT NULL,
    granted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    granted_by VARCHAR(50),
    PRIMARY KEY (role_id, permission_id),
    FOREIGN KEY (role_id) REFERENCES roles(id),
    FOREIGN KEY (permission_id) REFERENCES permissions(id)
);

-- 創建使用者額外權限表
CREATE TABLE IF NOT EXISTS user_permissions (
    user_id VARCHAR(50) NOT NULL,
    permission_id INTEGER NOT NULL,
    granted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    granted_by VARCHAR(50),
    expires_at TIMESTAMP,
    PRIMARY KEY (user_id, permission_id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (permission_id) REFERENCES permissions(id)
);

-- 創建專案權限表
CREATE TABLE IF NOT EXISTS project_permissions (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(50) NOT NULL,
    project_id VARCHAR(50) NOT NULL,
    role VARCHAR(50) NOT NULL,
    granted_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    granted_by VARCHAR(50),
    FOREIGN KEY (user_id) REFERENCES users(id),
    UNIQUE(user_id, project_id)
);

-- 插入預設角色
INSERT INTO roles (id, display_name, description, level, is_system) VALUES
    ('superadmin', '超級管理員', '系統最高權限，可以管理所有功能', 100, true),
    ('admin', '管理員', '可以管理使用者、專案和大部分系統功能', 90, true),
    ('user', '一般使用者', '可以使用基本功能', 50, true),
    ('guest', '訪客', '只能查看公開資料', 0, true)
ON CONFLICT (id) DO NOTHING;

-- 插入預設權限
INSERT INTO permissions (permission, display_name, description, category) VALUES
    -- 使用者管理
    ('user:create', '建立使用者', '建立新使用者帳號', '系統管理'),
    ('user:read', '檢視使用者', '檢視使用者資訊', '系統管理'),
    ('user:update', '更新使用者', '更新使用者資訊', '系統管理'),
    ('user:delete', '刪除使用者', '刪除使用者帳號', '系統管理'),
    
    -- 角色管理
    ('role:manage', '管理角色', '管理角色和權限', '系統管理'),
    
    -- 專案管理
    ('project:create', '建立專案', '建立新專案', '專案管理'),
    ('project:read', '檢視專案', '檢視專案資訊', '專案管理'),
    ('project:update', '更新專案', '更新專案資訊', '專案管理'),
    ('project:delete', '刪除專案', '刪除專案', '專案管理'),
    
    -- 人員管理
    ('person:create', '建立人員', '建立人員資料', '人員管理'),
    ('person:read', '檢視人員', '檢視人員資料', '人員管理'),
    ('person:update', '更新人員', '更新人員資料', '人員管理'),
    ('person:delete', '刪除人員', '刪除人員資料', '人員管理'),
    
    -- 檔案管理
    ('file:upload', '上傳檔案', '上傳檔案', '檔案管理'),
    ('file:read', '檢視檔案', '檢視檔案', '檔案管理'),
    ('file:delete', '刪除檔案', '刪除檔案', '檔案管理'),
    ('file:download', '下載檔案', '下載檔案', '檔案管理'),
    ('file:process', '處理檔案', '處理檔案內容', '檔案管理'),
    
    -- 搜尋功能
    ('search:perform', '執行搜尋', '執行搜尋功能', '搜尋功能'),
    
    -- 報表功能
    ('report:view', '檢視報表', '檢視報表', '報表功能'),
    ('report:export', '匯出報表', '匯出報表', '報表功能')
ON CONFLICT (permission) DO NOTHING;

-- 為預設角色分配權限
-- superadmin 擁有所有權限
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'superadmin', id FROM permissions
ON CONFLICT DO NOTHING;

-- admin 擁有大部分權限
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'admin', id FROM permissions
WHERE permission IN (
    'user:create', 'user:read', 'user:update', 'user:delete',
    'project:create', 'project:read', 'project:update', 'project:delete',
    'person:create', 'person:read', 'person:update', 'person:delete',
    'file:upload', 'file:read', 'file:delete', 'file:download', 'file:process',
    'search:perform', 'report:view', 'report:export'
)
ON CONFLICT DO NOTHING;

-- user 擁有基本權限
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'user', id FROM permissions
WHERE permission IN (
    'project:read', 'project:create',
    'person:create', 'person:read', 'person:update', 'person:delete',
    'file:upload', 'file:read', 'file:download',
    'search:perform', 'report:view'
)
ON CONFLICT DO NOTHING;

-- guest 只有查看權限
INSERT INTO role_permissions (role_id, permission_id)
SELECT 'guest', id FROM permissions
WHERE permission IN (
    'project:read',
    'person:read',
    'report:view'
)
ON CONFLICT DO NOTHING;

-- 創建索引以提升效能
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_role_permissions_role_id ON role_permissions(role_id);
CREATE INDEX IF NOT EXISTS idx_user_permissions_user_id ON user_permissions(user_id);
CREATE INDEX IF NOT EXISTS idx_project_permissions_user_id ON project_permissions(user_id);
CREATE INDEX IF NOT EXISTS idx_project_permissions_project_id ON project_permissions(project_id);