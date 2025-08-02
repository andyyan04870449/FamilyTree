using System;
using System.Collections.Generic;

namespace FamilyTree.Constants
{
    /// <summary>
    /// 系統角色常數定義
    /// 
    /// 用途：統一管理所有角色相關的常數，避免硬代碼散布在各處
    /// 重構目標：替換所有硬代碼的角色字串和權限等級檢查
    /// </summary>
    public static class RoleConstants
    {
        /// <summary>
        /// 系統角色定義
        /// </summary>
        public const string SUPER_ADMIN = "superadmin";
        public const string ADMIN = "admin";
        public const string USER = "user";
        public const string GUEST = "guest";
        
        /// <summary>
        /// 特殊角色定義 - 稽核與安全相關
        /// </summary>
        public const string AUDITOR = "auditor";
        public const string SECURITY_ADMIN = "security_admin";
        public const string AUDIT_READER = "AuditReader";
        public const string SECURITY_OFFICER = "SecurityOfficer";

        /// <summary>
        /// 角色權限等級定義
        /// 數值越高代表權限越大
        /// </summary>
        public static readonly Dictionary<string, int> RoleLevels = new()
        {
            [SUPER_ADMIN] = 100,
            [ADMIN] = 90,
            [SECURITY_ADMIN] = 85,
            [AUDITOR] = 70,
            [SECURITY_OFFICER] = 65,
            [AUDIT_READER] = 60,
            [USER] = 50,
            [GUEST] = 10
        };

        /// <summary>
        /// 角色顯示名稱對應
        /// </summary>
        public static readonly Dictionary<string, string> RoleDisplayNames = new()
        {
            [SUPER_ADMIN] = "超級管理員",
            [ADMIN] = "管理員",
            [SECURITY_ADMIN] = "安全管理員",
            [AUDITOR] = "稽核員",
            [SECURITY_OFFICER] = "資安官",
            [AUDIT_READER] = "稽核查看者",
            [USER] = "使用者",
            [GUEST] = "訪客"
        };

        /// <summary>
        /// 角色描述對應
        /// </summary>
        public static readonly Dictionary<string, string> RoleDescriptions = new()
        {
            [SUPER_ADMIN] = "擁有系統最高權限，可以管理所有功能和使用者",
            [ADMIN] = "擁有管理權限，可以管理專案、使用者和大部分系統功能",
            [SECURITY_ADMIN] = "負責系統安全配置與管理，可查看和分析所有安全相關記錄",
            [AUDITOR] = "可查看和分析系統稽核日誌，進行合規性檢查",
            [SECURITY_OFFICER] = "負責監控系統安全事件，可查看安全報告和警示",
            [AUDIT_READER] = "可查看稽核日誌和報告，但無法進行修改操作",
            [USER] = "一般使用者，可以建立和管理自己的專案",
            [GUEST] = "訪客使用者，僅能查看公開內容"
        };

        /// <summary>
        /// 預設角色設定
        /// </summary>
        public const string DEFAULT_ROLE = USER;

        /// <summary>
        /// 管理員級別角色列表
        /// </summary>
        public static readonly string[] AdminRoles = { SUPER_ADMIN, ADMIN, SECURITY_ADMIN };
        
        /// <summary>
        /// 稽核相關角色列表
        /// </summary>
        public static readonly string[] AuditRoles = { AUDITOR, SECURITY_OFFICER, AUDIT_READER };

        /// <summary>
        /// 所有可用角色列表
        /// </summary>
        public static readonly string[] AllRoles = { SUPER_ADMIN, ADMIN, SECURITY_ADMIN, AUDITOR, SECURITY_OFFICER, AUDIT_READER, USER, GUEST };
    }

    /// <summary>
    /// 角色工具類別
    /// </summary>
    public static class RoleHelper
    {
        /// <summary>
        /// 檢查角色是否為管理員級別
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <returns>是否為管理員級別</returns>
        public static bool IsAdminLevel(string role)
        {
            return Array.Exists(RoleConstants.AdminRoles, r => r == role);
        }

        /// <summary>
        /// 檢查角色是否達到最低權限等級
        /// </summary>
        /// <param name="userRole">使用者角色</param>
        /// <param name="requiredLevel">要求的權限等級</param>
        /// <returns>是否達到要求等級</returns>
        public static bool HasMinimumLevel(string userRole, int requiredLevel)
        {
            if (!RoleConstants.RoleLevels.TryGetValue(userRole, out int userLevel))
            {
                return false;
            }
            return userLevel >= requiredLevel;
        }

        /// <summary>
        /// 檢查角色是否達到指定角色的權限等級
        /// </summary>
        /// <param name="userRole">使用者角色</param>
        /// <param name="requiredRole">要求的角色</param>
        /// <returns>是否達到要求角色等級</returns>
        public static bool HasMinimumRole(string userRole, string requiredRole)
        {
            if (!RoleConstants.RoleLevels.TryGetValue(requiredRole, out int requiredLevel))
            {
                return false;
            }
            return HasMinimumLevel(userRole, requiredLevel);
        }

        /// <summary>
        /// 取得角色顯示名稱
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <returns>顯示名稱</returns>
        public static string GetDisplayName(string role)
        {
            return RoleConstants.RoleDisplayNames.TryGetValue(role, out string displayName) 
                ? displayName 
                : role;
        }

        /// <summary>
        /// 取得角色描述
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <returns>角色描述</returns>
        public static string GetDescription(string role)
        {
            return RoleConstants.RoleDescriptions.TryGetValue(role, out string description) 
                ? description 
                : string.Empty;
        }

        /// <summary>
        /// 驗證角色是否有效
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <returns>是否為有效角色</returns>
        public static bool IsValidRole(string role)
        {
            return Array.Exists(RoleConstants.AllRoles, r => r == role);
        }

        /// <summary>
        /// 取得角色權限等級
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <returns>權限等級，如果角色不存在則返回0</returns>
        public static int GetRoleLevel(string role)
        {
            return RoleConstants.RoleLevels.TryGetValue(role, out int level) ? level : 0;
        }
        
        /// <summary>
        /// 檢查角色是否為稽核相關角色
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <returns>是否為稽核相關角色</returns>
        public static bool IsAuditRole(string role)
        {
            return Array.Exists(RoleConstants.AuditRoles, r => r == role);
        }
        
        /// <summary>
        /// 檢查角色是否有稽核權限（包含管理員和稽核角色）
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <returns>是否有稽核權限</returns>
        public static bool HasAuditPermission(string role)
        {
            return IsAdminLevel(role) || IsAuditRole(role);
        }
    }
}