using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Models
{
    /// <summary>
    /// 角色建立請求
    /// </summary>
    public class CreateRoleDto
    {
        [Required(ErrorMessage = "角色ID為必填")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "角色ID長度必須在3-50個字元之間")]
        [RegularExpression(@"^[a-z_]+$", ErrorMessage = "角色ID只能包含小寫字母和底線")]
        public string RoleId { get; set; }

        [Required(ErrorMessage = "顯示名稱為必填")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "顯示名稱長度必須在2-100個字元之間")]
        public string DisplayName { get; set; }

        [StringLength(500, ErrorMessage = "描述不能超過500個字元")]
        public string Description { get; set; }

        [Range(1, 100, ErrorMessage = "角色層級必須在1-100之間")]
        public int Level { get; set; } = 50;
    }

    /// <summary>
    /// 角色更新請求
    /// </summary>
    public class UpdateRoleDto
    {
        [Required(ErrorMessage = "顯示名稱為必填")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "顯示名稱長度必須在2-100個字元之間")]
        public string DisplayName { get; set; }

        [StringLength(500, ErrorMessage = "描述不能超過500個字元")]
        public string Description { get; set; }

        [Range(1, 100, ErrorMessage = "角色層級必須在1-100之間")]
        public int Level { get; set; }
    }

    /// <summary>
    /// 角色權限設定請求
    /// </summary>
    public class SetRolePermissionsDto
    {
        [Required(ErrorMessage = "權限列表為必填")]
        public List<string> Permissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 使用者角色指派請求
    /// </summary>
    public class AssignRoleDto
    {
        [Required(ErrorMessage = "使用者ID為必填")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "角色ID為必填")]
        public string RoleId { get; set; }
    }

    /// <summary>
    /// 使用者權限設定請求
    /// </summary>
    public class SetUserPermissionsDto
    {
        [Required(ErrorMessage = "權限列表為必填")]
        public List<string> Permissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 角色回應
    /// </summary>
    public class RoleDto
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public bool IsSystem { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 權限定義回應
    /// </summary>
    public class PermissionDto
    {
        public string Resource { get; set; }
        public string Action { get; set; }
        public string Permission { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsSystem { get; set; }
        public List<string> AssignedRoles { get; set; } = new List<string>();
    }

    /// <summary>
    /// 權限類別回應
    /// </summary>
    public class PermissionCategoryDto
    {
        public string Category { get; set; }
        public string DisplayName { get; set; }
        public int PermissionCount { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }

    /// <summary>
    /// 使用者權限回應
    /// </summary>
    public class UserPermissionsDto
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public List<RoleDto> Roles { get; set; } = new List<RoleDto>();
        public List<string> DirectPermissions { get; set; } = new List<string>();
        public List<string> AllPermissions { get; set; } = new List<string>();
        public Dictionary<string, ProjectPermissionDto> ProjectPermissions { get; set; } = new Dictionary<string, ProjectPermissionDto>();
    }

    /// <summary>
    /// 專案權限回應
    /// </summary>
    public class ProjectPermissionDto
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Role { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 權限檢查結果
    /// </summary>
    public class PermissionCheckResult
    {
        public bool HasPermission { get; set; }
        public string Reason { get; set; }
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        public List<string> UserPermissions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 角色複製請求
    /// </summary>
    public class CopyRoleDto
    {
        [Required(ErrorMessage = "來源角色ID為必填")]
        public string SourceRoleId { get; set; }

        [Required(ErrorMessage = "新角色ID為必填")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "角色ID長度必須在3-50個字元之間")]
        [RegularExpression(@"^[a-z_]+$", ErrorMessage = "角色ID只能包含小寫字母和底線")]
        public string NewRoleId { get; set; }

        [Required(ErrorMessage = "顯示名稱為必填")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "顯示名稱長度必須在2-100個字元之間")]
        public string DisplayName { get; set; }

        [StringLength(500, ErrorMessage = "描述不能超過500個字元")]
        public string Description { get; set; }
    }

    /// <summary>
    /// 批量角色指派請求
    /// </summary>
    public class BatchAssignRoleDto
    {
        [Required(ErrorMessage = "使用者ID列表為必填")]
        [MinLength(1, ErrorMessage = "至少需要一個使用者ID")]
        public List<string> UserIds { get; set; } = new List<string>();

        [Required(ErrorMessage = "角色ID為必填")]
        public string RoleId { get; set; }
    }

    /// <summary>
    /// 權限驗證請求
    /// </summary>
    public class ValidatePermissionDto
    {
        [Required(ErrorMessage = "使用者ID為必填")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "權限為必填")]
        public string Permission { get; set; }

        public string ProjectId { get; set; }
        public string ResourceId { get; set; }
        public string ResourceType { get; set; }
    }
}