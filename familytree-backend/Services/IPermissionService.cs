using System.Collections.Generic;
using System.Threading.Tasks;

namespace FamilyTree.Services
{
    public interface IPermissionService
    {
        /// <summary>
        /// 檢查使用者是否擁有指定權限
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="permission">權限字串 (例如: "user:create")</param>
        /// <returns>是否擁有權限</returns>
        Task<bool> HasPermissionAsync(string userId, string permission);

        /// <summary>
        /// 檢查使用者是否擁有任一指定權限
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="permissions">權限字串陣列</param>
        /// <returns>是否擁有任一權限</returns>
        Task<bool> HasAnyPermissionAsync(string userId, params string[] permissions);

        /// <summary>
        /// 檢查使用者是否擁有所有指定權限
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="permissions">權限字串陣列</param>
        /// <returns>是否擁有所有權限</returns>
        Task<bool> HasAllPermissionsAsync(string userId, params string[] permissions);

        /// <summary>
        /// 取得使用者的所有權限
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <returns>權限字串列表</returns>
        Task<List<string>> GetUserPermissionsAsync(string userId);

        /// <summary>
        /// 取得角色的所有權限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>權限字串列表</returns>
        Task<List<string>> GetRolePermissionsAsync(string roleId);

        /// <summary>
        /// 為角色設定權限
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="permissions">權限字串列表</param>
        /// <returns>操作是否成功</returns>
        Task<bool> SetRolePermissionsAsync(string roleId, List<string> permissions);

        /// <summary>
        /// 為使用者設定額外權限
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="permissions">權限字串列表</param>
        /// <returns>操作是否成功</returns>
        Task<bool> SetUserPermissionsAsync(string userId, List<string> permissions);

        /// <summary>
        /// 檢查專案層級權限
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="projectId">專案ID</param>
        /// <param name="permission">權限字串</param>
        /// <returns>是否擁有權限</returns>
        Task<bool> HasProjectPermissionAsync(string userId, string projectId, string permission);

        /// <summary>
        /// 檢查資源擁有權
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="resourceType">資源類型</param>
        /// <param name="resourceId">資源ID</param>
        /// <returns>是否為資源擁有者</returns>
        Task<bool> IsResourceOwnerAsync(string userId, string resourceType, string resourceId);

        /// <summary>
        /// 取得所有權限定義
        /// </summary>
        /// <returns>權限定義列表</returns>
        Task<List<PermissionDefinition>> GetAllPermissionDefinitionsAsync();

        /// <summary>
        /// 取得所有角色
        /// </summary>
        /// <returns>角色列表</returns>
        Task<List<RoleInfo>> GetAllRolesAsync();

        /// <summary>
        /// 建立新角色
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="displayName">顯示名稱</param>
        /// <param name="description">描述</param>
        /// <param name="level">角色層級</param>
        /// <returns>操作是否成功</returns>
        Task<bool> CreateRoleAsync(string roleId, string displayName, string description, int level);

        /// <summary>
        /// 更新角色資訊
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="displayName">顯示名稱</param>
        /// <param name="description">描述</param>
        /// <param name="level">角色層級</param>
        /// <returns>操作是否成功</returns>
        Task<bool> UpdateRoleAsync(string roleId, string displayName, string description, int level);

        /// <summary>
        /// 刪除角色
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> DeleteRoleAsync(string roleId);

        /// <summary>
        /// 為使用者指派角色
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="roleId">角色ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> AssignRoleToUserAsync(string userId, string roleId);

        /// <summary>
        /// 移除使用者的角色
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <param name="roleId">角色ID</param>
        /// <returns>操作是否成功</returns>
        Task<bool> RemoveRoleFromUserAsync(string userId, string roleId);

        /// <summary>
        /// 取得使用者的所有角色
        /// </summary>
        /// <param name="userId">使用者ID</param>
        /// <returns>角色列表</returns>
        Task<List<RoleInfo>> GetUserRolesAsync(string userId);
    }

    public class PermissionDefinition
    {
        public string Resource { get; set; }
        public string Action { get; set; }
        public string Permission { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public bool IsSystem { get; set; }
    }

    public class RoleInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public bool IsSystem { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}