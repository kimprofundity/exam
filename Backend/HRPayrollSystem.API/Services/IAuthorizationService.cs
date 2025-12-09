using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 授權服務介面
/// 負責驗證使用者權限和資料存取範圍
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// 檢查使用者是否擁有指定權限
    /// </summary>
    /// <param name="userId">使用者識別碼</param>
    /// <param name="permission">權限名稱</param>
    /// <returns>是否擁有權限</returns>
    Task<bool> HasPermissionAsync(string userId, string permission);

    /// <summary>
    /// 取得使用者的資料存取範圍
    /// </summary>
    /// <param name="userId">使用者識別碼</param>
    /// <returns>資料存取範圍</returns>
    Task<DataAccessScope> GetDataAccessScopeAsync(string userId);

    /// <summary>
    /// 取得使用者的所有角色
    /// </summary>
    /// <param name="userId">使用者識別碼</param>
    /// <returns>角色列表</returns>
    Task<List<Role>> GetUserRolesAsync(string userId);

    /// <summary>
    /// 檢查使用者是否可以存取指定員工的資料
    /// </summary>
    /// <param name="userId">使用者識別碼</param>
    /// <param name="targetEmployeeId">目標員工識別碼</param>
    /// <returns>是否可以存取</returns>
    Task<bool> CanAccessEmployeeDataAsync(string userId, string targetEmployeeId);

    /// <summary>
    /// 取得使用者可存取的員工識別碼列表
    /// </summary>
    /// <param name="userId">使用者識別碼</param>
    /// <returns>員工識別碼列表</returns>
    Task<List<string>> GetAccessibleEmployeeIdsAsync(string userId);
}
