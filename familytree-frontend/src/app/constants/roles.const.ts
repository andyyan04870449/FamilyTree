/**
 * 系統角色常數定義
 * 
 * 用途：統一管理所有角色相關的常數，避免硬代碼散布在各處
 * 重構目標：替換所有硬代碼的角色字串和權限等級檢查
 */

export const SYSTEM_ROLES = {
  SUPER_ADMIN: 'superadmin',
  ADMIN: 'admin',
  USER: 'user',
  GUEST: 'guest'
} as const;

export type SystemRole = typeof SYSTEM_ROLES[keyof typeof SYSTEM_ROLES];

/**
 * 角色權限等級定義
 * 數值越高代表權限越大
 */
export const ROLE_LEVELS = {
  [SYSTEM_ROLES.SUPER_ADMIN]: 100,
  [SYSTEM_ROLES.ADMIN]: 90,
  [SYSTEM_ROLES.USER]: 50,
  [SYSTEM_ROLES.GUEST]: 10
} as const;

/**
 * 角色顯示名稱對應
 */
export const ROLE_DISPLAY_NAMES = {
  [SYSTEM_ROLES.SUPER_ADMIN]: '超級管理員',
  [SYSTEM_ROLES.ADMIN]: '管理員',
  [SYSTEM_ROLES.USER]: '使用者',
  [SYSTEM_ROLES.GUEST]: '訪客'
} as const;

/**
 * 角色描述對應
 */
export const ROLE_DESCRIPTIONS = {
  [SYSTEM_ROLES.SUPER_ADMIN]: '擁有系統最高權限，可以管理所有功能和使用者',
  [SYSTEM_ROLES.ADMIN]: '擁有管理權限，可以管理專案、使用者和大部分系統功能',
  [SYSTEM_ROLES.USER]: '一般使用者，可以建立和管理自己的專案',
  [SYSTEM_ROLES.GUEST]: '訪客使用者，僅能查看公開內容'
} as const;

/**
 * 預設角色設定
 */
export const DEFAULT_ROLE = SYSTEM_ROLES.USER;

/**
 * 管理員級別角色列表
 */
export const ADMIN_ROLES = [
  SYSTEM_ROLES.SUPER_ADMIN,
  SYSTEM_ROLES.ADMIN
] as const;

/**
 * 角色工具函數
 */
export class RoleHelper {
  /**
   * 檢查角色是否為管理員級別
   */
  static isAdminLevel(role: string): boolean {
    return ADMIN_ROLES.includes(role as 'superadmin' | 'admin');
  }

  /**
   * 檢查角色是否達到最低權限等級
   */
  static hasMinimumLevel(userRole: string, requiredLevel: number): boolean {
    const userLevel = ROLE_LEVELS[userRole as SystemRole];
    return userLevel >= requiredLevel;
  }

  /**
   * 檢查角色是否達到指定角色的權限等級
   */
  static hasMinimumRole(userRole: string, requiredRole: string): boolean {
    const requiredLevel = ROLE_LEVELS[requiredRole as SystemRole];
    return this.hasMinimumLevel(userRole, requiredLevel);
  }

  /**
   * 取得角色顯示名稱
   */
  static getDisplayName(role: string): string {
    return ROLE_DISPLAY_NAMES[role as SystemRole] || role;
  }

  /**
   * 取得角色描述
   */
  static getDescription(role: string): string {
    return ROLE_DESCRIPTIONS[role as SystemRole] || '';
  }

  /**
   * 取得所有可用角色列表
   */
  static getAllRoles(): SystemRole[] {
    return Object.values(SYSTEM_ROLES);
  }

  /**
   * 驗證角色是否有效
   */
  static isValidRole(role: string): role is SystemRole {
    return Object.values(SYSTEM_ROLES).includes(role as SystemRole);
  }
}