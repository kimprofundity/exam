using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 角色服務介面
/// 負責角色的 CRUD 操作和角色指派
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// 建立新角色
    /// </summary>
    /// <param name="roleDto">角色資料傳輸物件</param>
    /// <returns>建立的角色</returns>
    Task<Role> CreateRoleAsync(RoleDto roleDto);

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="roleId">角色識別碼</param>
    /// <param name="roleDto">角色資料傳輸物件</param>
    /// <returns>更新後的角色</returns>
    Task<Role> UpdateRoleAsync(string roleId, RoleDto roleDto);

    /// <summary>
    /// 取得角色
    /// </summary>
    /// <param name="roleId">角色識別碼</param>
    /// <returns>角色</returns>
    Task<Role?> GetRoleAsync(string roleId);

    /// <summary>
    /// 取得所有角色
    /// </summary>
    /// <returns>角色列表</returns>
    Task<List<Role>> GetAllRolesAsync();

    /// <summary>
    /// 刪除角色
    /// </summary>
    /// <param name="roleId">角色識別碼</param>
    Task DeleteRoleAsync(string roleId);

    /// <summary>
    /// 指派角色給使用者
    /// </summary>
    /// <param name="userId">使用者識別碼</param>
    /// <param name="roleId">角色識別碼</param>
    Task AssignRoleToUserAsync(string userId, string roleId);

    /// <summary>
    /// 移除使用者的角色
    /// </summary>
    /// <param name="userId">使用者識別碼</param>
    /// <param name="roleId">角色識別碼</param>
    Task RemoveRoleFromUserAsync(string userId, string roleId);

    /// <summary>
    /// 更新角色權限
    /// </summary>
    /// <param name="roleId">角色識別碼</param>
    /// <param name="permissions">權限列表</param>
    Task UpdateRolePermissionsAsync(string roleId, List<string> permissions);
}

/// <summary>
/// 角色資料傳輸物件
/// </summary>
public class RoleDto
{
    /// <summary>角色代碼</summary>
    public string Code { get; set; } = null!;

    /// <summary>角色名稱</summary>
    public string Name { get; set; } = null!;

    /// <summary>角色描述</summary>
    public string? Description { get; set; }

    /// <summary>資料存取範圍</summary>
    public string DataAccessScope { get; set; } = "Self";

    /// <summary>權限列表</summary>
    public List<string> Permissions { get; set; } = new();
}
