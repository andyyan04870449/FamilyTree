using Microsoft.AspNetCore.Authorization;
using FamilyTree.Constants;

namespace FamilyTree.Attributes
{
    /// <summary>
    /// 自定義角色授權屬性
    /// 使用角色常數而非硬代碼字串，提供更好的類型安全性和可維護性
    /// </summary>
    public class RequireRoleAttribute : AuthorizeAttribute
    {
        public RequireRoleAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }
    }

    /// <summary>
    /// 要求管理員權限的授權屬性
    /// </summary>
    public class RequireAdminAttribute : RequireRoleAttribute
    {
        public RequireAdminAttribute() : base(RoleConstants.ADMIN, RoleConstants.SUPER_ADMIN)
        {
        }
    }

    /// <summary>
    /// 要求超級管理員權限的授權屬性
    /// </summary>
    public class RequireSuperAdminAttribute : RequireRoleAttribute
    {
        public RequireSuperAdminAttribute() : base(RoleConstants.SUPER_ADMIN)
        {
        }
    }

    /// <summary>
    /// 要求使用者權限（包含管理員）的授權屬性
    /// </summary>
    public class RequireUserAttribute : RequireRoleAttribute
    {
        public RequireUserAttribute() : base(RoleConstants.USER, RoleConstants.ADMIN, RoleConstants.SUPER_ADMIN)
        {
        }
    }

    /// <summary>
    /// 要求稽核讀取權限的授權屬性
    /// </summary>
    public class RequireAuditReaderAttribute : RequireRoleAttribute
    {
        public RequireAuditReaderAttribute() : base(RoleConstants.ADMIN, RoleConstants.SUPER_ADMIN, "AuditReader")
        {
        }
    }
}