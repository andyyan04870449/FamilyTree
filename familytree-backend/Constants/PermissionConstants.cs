using System.Collections.Generic;

namespace FamilyTree.Constants
{
    /// <summary>
    /// 系統權限常數定義
    /// 
    /// 用途：統一管理所有權限相關的常數，避免硬代碼權限字串散布在各處
    /// 重構目標：替換所有硬代碼的權限字串檢查
    /// </summary>
    public static class PermissionConstants
    {
        /// <summary>
        /// 權限動作類型
        /// </summary>
        public static class Actions
        {
            public const string CREATE = "create";
            public const string READ = "read";
            public const string UPDATE = "update";
            public const string DELETE = "delete";
            public const string MANAGE = "manage";
            public const string VIEW = "view";
            public const string EXPORT = "export";
            public const string IMPORT = "import";
            public const string ALL = "*";
        }

        /// <summary>
        /// 權限資源類型
        /// </summary>
        public static class Resources
        {
            public const string SYSTEM = "system";
            public const string USER = "user";
            public const string PROJECT = "project";
            public const string PERSON = "person";
            public const string FILE = "file";
            public const string REPORT = "report";
            public const string SEARCH = "search";
            public const string AUDIT = "audit";
            public const string ALL = "*";
        }

        /// <summary>
        /// 具體權限定義
        /// </summary>
        public static class Permissions
        {
            // 系統管理權限
            public static class System
            {
                public const string MANAGE = "system:manage";
                public const string VIEW_SETTINGS = "system:view_settings";
                public const string UPDATE_SETTINGS = "system:update_settings";
                public const string ALL = "system:*";
            }

            // 使用者管理權限
            public static class User
            {
                public const string CREATE = "user:create";
                public const string READ = "user:read";
                public const string UPDATE = "user:update";
                public const string DELETE = "user:delete";
                public const string MANAGE_ROLES = "user:manage_roles";
                public const string VIEW_LIST = "user:view_list";
                public const string ALL = "user:*";
            }

            // 專案管理權限
            public static class Project
            {
                public const string CREATE = "project:create";
                public const string READ = "project:read";
                public const string UPDATE = "project:update";
                public const string DELETE = "project:delete";
                public const string MANAGE_MEMBERS = "project:manage_members";
                public const string VIEW_LIST = "project:view_list";
                public const string EXPORT = "project:export";
                public const string ALL = "project:*";
            }

            // 人員資料權限
            public static class Person
            {
                public const string CREATE = "person:create";
                public const string READ = "person:read";
                public const string UPDATE = "person:update";
                public const string DELETE = "person:delete";
                public const string VIEW_DETAILS = "person:view_details";
                public const string MANAGE_PHOTOS = "person:manage_photos";
                public const string ALL = "person:*";
            }

            // 檔案管理權限
            public static class File
            {
                public const string UPLOAD = "file:upload";
                public const string DOWNLOAD = "file:download";
                public const string DELETE = "file:delete";
                public const string MANAGE = "file:manage";
                public const string VIEW_LIST = "file:view_list";
                public const string ALL = "file:*";
            }

            // 報表權限
            public static class Report
            {
                public const string VIEW = "report:view";
                public const string GENERATE = "report:generate";
                public const string EXPORT = "report:export";
                public const string MANAGE = "report:manage";
                public const string ALL = "report:*";
            }

            // 搜尋權限
            public static class Search
            {
                public const string BASIC = "search:basic";
                public const string ADVANCED = "search:advanced";
                public const string FULLTEXT = "search:fulltext";
                public const string ALL = "search:*";
            }

            // 稽核日誌權限
            public static class Audit
            {
                public const string VIEW = "audit:view";
                public const string MANAGE = "audit:manage";
                public const string EXPORT = "audit:export";
                public const string ALL = "audit:*";
            }
        }

        /// <summary>
        /// 角色預設權限對應
        /// 定義每個角色預設擁有的權限
        /// </summary>
        public static readonly Dictionary<string, string[]> RoleDefaultPermissions = new()
        {
            [RoleConstants.SUPER_ADMIN] = new[] { Permissions.System.ALL }, // 超級管理員擁有所有權限

            [RoleConstants.ADMIN] = new[]
            {
                Permissions.User.ALL,
                Permissions.Project.ALL,
                Permissions.Person.ALL,
                Permissions.File.ALL,
                Permissions.Report.ALL,
                Permissions.Search.ALL,
                Permissions.Audit.VIEW,
                Permissions.System.VIEW_SETTINGS
            },

            [RoleConstants.USER] = new[]
            {
                Permissions.Project.CREATE,
                Permissions.Project.READ,
                Permissions.Project.UPDATE,
                Permissions.Person.ALL,
                Permissions.File.UPLOAD,
                Permissions.File.DOWNLOAD,
                Permissions.Report.VIEW,
                Permissions.Search.BASIC,
                Permissions.Search.ADVANCED
            },

            [RoleConstants.GUEST] = new[]
            {
                Permissions.Project.READ,
                Permissions.Person.READ,
                Permissions.Person.VIEW_DETAILS,
                Permissions.Report.VIEW,
                Permissions.Search.BASIC
            }
        };
    }

    /// <summary>
    /// 權限工具類別
    /// </summary>
    public static class PermissionHelper
    {
        /// <summary>
        /// 建構權限字串
        /// </summary>
        /// <param name="resource">資源</param>
        /// <param name="action">動作</param>
        /// <returns>權限字串</returns>
        public static string BuildPermission(string resource, string action)
        {
            return $"{resource}:{action}";
        }

        /// <summary>
        /// 解析權限字串
        /// </summary>
        /// <param name="permission">權限字串</param>
        /// <returns>資源和動作的元組</returns>
        public static (string Resource, string Action) ParsePermission(string permission)
        {
            var parts = permission.Split(':');
            return parts.Length == 2 ? (parts[0], parts[1]) : (permission, string.Empty);
        }

        /// <summary>
        /// 檢查權限是否匹配
        /// 支援萬用字元 (*) 權限檢查
        /// </summary>
        /// <param name="userPermissions">使用者權限列表</param>
        /// <param name="requiredPermission">需要的權限</param>
        /// <returns>是否匹配</returns>
        public static bool MatchesPermission(IEnumerable<string> userPermissions, string requiredPermission)
        {
            var permissions = userPermissions.ToArray();

            // 檢查是否有完全匹配的權限
            if (permissions.Contains(requiredPermission))
            {
                return true;
            }

            // 檢查是否有萬用字元權限
            if (permissions.Contains("*"))
            {
                return true;
            }

            var (resource, _) = ParsePermission(requiredPermission);

            // 檢查資源層級的萬用字元權限
            if (permissions.Contains($"{resource}:*"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 取得角色的預設權限
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <returns>權限列表</returns>
        public static string[] GetRolePermissions(string role)
        {
            return PermissionConstants.RoleDefaultPermissions.TryGetValue(role, out string[] permissions) 
                ? permissions 
                : Array.Empty<string>();
        }

        /// <summary>
        /// 檢查角色是否擁有特定權限
        /// </summary>
        /// <param name="role">角色名稱</param>
        /// <param name="permission">權限字串</param>
        /// <returns>是否擁有權限</returns>
        public static bool RoleHasPermission(string role, string permission)
        {
            var rolePermissions = GetRolePermissions(role);
            return MatchesPermission(rolePermissions, permission);
        }

        /// <summary>
        /// 取得所有可用權限列表
        /// </summary>
        /// <returns>所有權限的列表</returns>
        public static List<string> GetAllPermissions()
        {
            var permissions = new List<string>();

            // 使用反射取得所有權限常數
            var permissionClasses = new[]
            {
                typeof(PermissionConstants.Permissions.System),
                typeof(PermissionConstants.Permissions.User),
                typeof(PermissionConstants.Permissions.Project),
                typeof(PermissionConstants.Permissions.Person),
                typeof(PermissionConstants.Permissions.File),
                typeof(PermissionConstants.Permissions.Report),
                typeof(PermissionConstants.Permissions.Search),
                typeof(PermissionConstants.Permissions.Audit)
            };

            foreach (var permissionClass in permissionClasses)
            {
                var fields = permissionClass.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(string) && field.GetValue(null) is string permission)
                    {
                        permissions.Add(permission);
                    }
                }
            }

            return permissions;
        }
    }
}