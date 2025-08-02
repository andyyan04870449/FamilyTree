/**
 * 系統權限常數定義
 * 
 * 用途：統一管理所有權限相關的常數，避免硬代碼權限字串散布在各處
 * 重構目標：替換所有硬代碼的權限字串檢查
 */

/**
 * 權限動作類型
 */
export const PERMISSION_ACTIONS = {
  CREATE: 'create',
  READ: 'read',
  UPDATE: 'update',
  DELETE: 'delete',
  MANAGE: 'manage',
  VIEW: 'view',
  EXPORT: 'export',
  IMPORT: 'import',
  ALL: '*'
} as const;

/**
 * 權限資源類型
 */
export const PERMISSION_RESOURCES = {
  SYSTEM: 'system',
  USER: 'user',
  PROJECT: 'project',
  PERSON: 'person',
  FILE: 'file',
  REPORT: 'report',
  SEARCH: 'search',
  AUDIT: 'audit',
  ALL: '*'
} as const;

/**
 * 具體權限定義
 */
export const PERMISSIONS = {
  // 系統管理權限
  SYSTEM: {
    MANAGE: 'system:manage',
    VIEW_SETTINGS: 'system:view_settings',
    UPDATE_SETTINGS: 'system:update_settings',
    ALL: 'system:*'
  },
  
  // 使用者管理權限
  USER: {
    CREATE: 'user:create',
    READ: 'user:read',
    UPDATE: 'user:update',
    DELETE: 'user:delete',
    MANAGE_ROLES: 'user:manage_roles',
    VIEW_LIST: 'user:view_list',
    ALL: 'user:*'
  },
  
  // 專案管理權限
  PROJECT: {
    CREATE: 'project:create',
    READ: 'project:read',
    UPDATE: 'project:update',
    DELETE: 'project:delete',
    MANAGE_MEMBERS: 'project:manage_members',
    VIEW_LIST: 'project:view_list',
    EXPORT: 'project:export',
    ALL: 'project:*'
  },
  
  // 人員資料權限
  PERSON: {
    CREATE: 'person:create',
    READ: 'person:read',
    UPDATE: 'person:update',
    DELETE: 'person:delete',
    VIEW_DETAILS: 'person:view_details',
    MANAGE_PHOTOS: 'person:manage_photos',
    ALL: 'person:*'
  },
  
  // 檔案管理權限
  FILE: {
    UPLOAD: 'file:upload',
    DOWNLOAD: 'file:download',
    DELETE: 'file:delete',
    MANAGE: 'file:manage',
    VIEW_LIST: 'file:view_list',
    ALL: 'file:*'
  },
  
  // 報表權限
  REPORT: {
    VIEW: 'report:view',
    GENERATE: 'report:generate',
    EXPORT: 'report:export',
    MANAGE: 'report:manage',
    ALL: 'report:*'
  },
  
  // 搜尋權限
  SEARCH: {
    BASIC: 'search:basic',
    ADVANCED: 'search:advanced',
    FULLTEXT: 'search:fulltext',
    ALL: 'search:*'
  },
  
  // 稽核日誌權限
  AUDIT: {
    VIEW: 'audit:view',
    MANAGE: 'audit:manage',
    EXPORT: 'audit:export',
    ALL: 'audit:*'
  }
} as const;

/**
 * 角色預設權限對應
 * 定義每個角色預設擁有的權限
 */
export const ROLE_DEFAULT_PERMISSIONS = {
  superadmin: [PERMISSIONS.SYSTEM.ALL], // 超級管理員擁有所有權限
  
  admin: [
    PERMISSIONS.USER.ALL,
    PERMISSIONS.PROJECT.ALL,
    PERMISSIONS.PERSON.ALL,
    PERMISSIONS.FILE.ALL,
    PERMISSIONS.REPORT.ALL,
    PERMISSIONS.SEARCH.ALL,
    PERMISSIONS.AUDIT.VIEW,
    PERMISSIONS.SYSTEM.VIEW_SETTINGS
  ],
  
  user: [
    PERMISSIONS.PROJECT.CREATE,
    PERMISSIONS.PROJECT.READ,
    PERMISSIONS.PROJECT.UPDATE,
    PERMISSIONS.PERSON.ALL,
    PERMISSIONS.FILE.UPLOAD,
    PERMISSIONS.FILE.DOWNLOAD,
    PERMISSIONS.REPORT.VIEW,
    PERMISSIONS.SEARCH.BASIC,
    PERMISSIONS.SEARCH.ADVANCED
  ],
  
  guest: [
    PERMISSIONS.PROJECT.READ,
    PERMISSIONS.PERSON.READ,
    PERMISSIONS.PERSON.VIEW_DETAILS,
    PERMISSIONS.REPORT.VIEW,
    PERMISSIONS.SEARCH.BASIC
  ]
} as const;

/**
 * 權限工具函數
 */
export class PermissionHelper {
  /**
   * 建構權限字串
   */
  static buildPermission(resource: string, action: string): string {
    return `${resource}:${action}`;
  }

  /**
   * 解析權限字串
   */
  static parsePermission(permission: string): { resource: string; action: string } {
    const [resource, action] = permission.split(':');
    return { resource, action };
  }

  /**
   * 檢查權限是否匹配
   * 支援萬用字元 (*) 權限檢查
   */
  static matchesPermission(userPermissions: string[], requiredPermission: string): boolean {
    // 檢查是否有完全匹配的權限
    if (userPermissions.includes(requiredPermission)) {
      return true;
    }

    // 檢查是否有萬用字元權限
    if (userPermissions.includes('*')) {
      return true;
    }

    const { resource } = this.parsePermission(requiredPermission);
    
    // 檢查資源層級的萬用字元權限
    if (userPermissions.includes(`${resource}:*`)) {
      return true;
    }

    return false;
  }

  /**
   * 取得角色的預設權限
   */
  static getRolePermissions(role: string): string[] {
    return [...(ROLE_DEFAULT_PERMISSIONS[role as keyof typeof ROLE_DEFAULT_PERMISSIONS] || [])];
  }

  /**
   * 檢查角色是否擁有特定權限
   */
  static roleHasPermission(role: string, permission: string): boolean {
    const rolePermissions = this.getRolePermissions(role);
    return this.matchesPermission(rolePermissions, permission);
  }

  /**
   * 取得所有可用權限列表
   */
  static getAllPermissions(): string[] {
    const allPermissions: string[] = [];
    
    Object.values(PERMISSIONS).forEach(resourcePermissions => {
      Object.values(resourcePermissions).forEach(permission => {
        allPermissions.push(permission);
      });
    });
    
    return allPermissions;
  }

  /**
   * 依資源分組權限
   */
  static getPermissionsByResource(): Record<string, string[]> {
    const permissionsByResource: Record<string, string[]> = {};
    
    Object.entries(PERMISSIONS).forEach(([resourceKey, resourcePermissions]) => {
      const resource = resourceKey.toLowerCase();
      permissionsByResource[resource] = Object.values(resourcePermissions);
    });
    
    return permissionsByResource;
  }
}