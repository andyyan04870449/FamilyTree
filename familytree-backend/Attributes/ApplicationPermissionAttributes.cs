using FamilyTree.Attributes;

namespace familytree_backend.Attributes
{
    // ===== 人員資料權限 =====
    /// <summary>
    /// 人員資料讀取權限
    /// </summary>
    public class PersonReadPermissionAttribute : HasPermissionAttribute
    {
        public PersonReadPermissionAttribute() : base("person:read") { }
    }

    /// <summary>
    /// 人員資料建立權限
    /// </summary>
    public class PersonCreatePermissionAttribute : HasPermissionAttribute
    {
        public PersonCreatePermissionAttribute() : base("person:create") { }
    }

    /// <summary>
    /// 人員資料更新權限
    /// </summary>
    public class PersonUpdatePermissionAttribute : HasPermissionAttribute
    {
        public PersonUpdatePermissionAttribute() : base("person:update") { }
    }

    /// <summary>
    /// 人員資料刪除權限
    /// </summary>
    public class PersonDeletePermissionAttribute : HasPermissionAttribute
    {
        public PersonDeletePermissionAttribute() : base("person:delete") { }
    }

    // ===== 搜尋權限 =====
    /// <summary>
    /// 搜尋執行權限
    /// </summary>
    public class SearchPerformPermissionAttribute : HasPermissionAttribute
    {
        public SearchPerformPermissionAttribute() : base("search:perform") { }
    }

    // ===== 專案權限 =====
    /// <summary>
    /// 專案讀取權限
    /// </summary>
    public class ProjectReadPermissionAttribute : HasPermissionAttribute
    {
        public ProjectReadPermissionAttribute() : base("project:read") { }
    }

    /// <summary>
    /// 專案建立權限
    /// </summary>
    public class ProjectCreatePermissionAttribute : HasPermissionAttribute
    {
        public ProjectCreatePermissionAttribute() : base("project:create") { }
    }

    // ===== 角色管理權限 =====
    /// <summary>
    /// 角色管理權限
    /// </summary>
    public class RoleManagePermissionAttribute : HasPermissionAttribute
    {
        public RoleManagePermissionAttribute() : base("role:manage") { }
    }

    // ===== 檔案權限 =====
    /// <summary>
    /// 檔案上傳權限
    /// </summary>
    public class FileUploadPermissionAttribute : HasPermissionAttribute
    {
        public FileUploadPermissionAttribute() : base("file:upload") { }
    }

    /// <summary>
    /// 檔案讀取權限
    /// </summary>
    public class FileReadPermissionAttribute : HasPermissionAttribute
    {
        public FileReadPermissionAttribute() : base("file:read") { }
    }

    /// <summary>
    /// 檔案刪除權限
    /// </summary>
    public class FileDeletePermissionAttribute : HasPermissionAttribute
    {
        public FileDeletePermissionAttribute() : base("file:delete") { }
    }

    // ===== 稽核權限 =====
    /// <summary>
    /// 稽核查詢權限
    /// </summary>
    public class AuditQueryPermissionAttribute : HasPermissionAttribute
    {
        public AuditQueryPermissionAttribute() : base("audit:query") { }
    }

    /// <summary>
    /// 稽核匯出權限
    /// </summary>
    public class AuditExportPermissionAttribute : HasPermissionAttribute
    {
        public AuditExportPermissionAttribute() : base("audit:export") { }
    }

    /// <summary>
    /// 稽核統計權限
    /// </summary>
    public class AuditStatisticsPermissionAttribute : HasPermissionAttribute
    {
        public AuditStatisticsPermissionAttribute() : base("audit:statistics") { }
    }

    // ===== 使用者管理權限 =====
    /// <summary>
    /// 使用者建立權限
    /// </summary>
    public class UserCreatePermissionAttribute : HasPermissionAttribute
    {
        public UserCreatePermissionAttribute() : base("user:create") { }
    }

    /// <summary>
    /// 使用者更新權限
    /// </summary>
    public class UserUpdatePermissionAttribute : HasPermissionAttribute
    {
        public UserUpdatePermissionAttribute() : base("user:update") { }
    }

    /// <summary>
    /// 使用者刪除權限
    /// </summary>
    public class UserDeletePermissionAttribute : HasPermissionAttribute
    {
        public UserDeletePermissionAttribute() : base("user:delete") { }
    }

    /// <summary>
    /// 使用者讀取權限（允許擁有者讀取自己的資料）
    /// </summary>
    public class UserReadPermissionAttribute : HasPermissionAttribute
    {
        public UserReadPermissionAttribute(bool allowOwner = false) : base("user:read", allowOwner, "user") { }
    }

    // ===== 收藏功能權限 =====
    /// <summary>
    /// 收藏管理權限（允許使用者管理自己的收藏）
    /// </summary>
    public class FavoritesManagePermissionAttribute : HasPermissionAttribute
    {
        public FavoritesManagePermissionAttribute() : base("favorites:manage", true, "favorites") { }
    }

    /// <summary>
    /// 視覺分析權限
    /// </summary>
    public class VisualAnalysisPermissionAttribute : HasPermissionAttribute
    {
        public VisualAnalysisPermissionAttribute() : base("analysis:visual") { }
    }

    /// <summary>
    /// 關係圖檢視權限
    /// </summary>
    public class RelationshipViewPermissionAttribute : HasPermissionAttribute
    {
        public RelationshipViewPermissionAttribute() : base("relationship:view") { }
    }
}