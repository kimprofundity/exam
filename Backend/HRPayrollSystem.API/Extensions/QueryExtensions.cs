using HRPayrollSystem.API.Models;
using HRPayrollSystem.API.Services;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Extensions;

/// <summary>
/// 查詢擴充方法
/// 提供資料存取範圍過濾功能
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// 根據使用者的資料存取範圍過濾員工查詢
    /// </summary>
    /// <param name="query">員工查詢</param>
    /// <param name="userId">使用者識別碼</param>
    /// <param name="authorizationService">授權服務</param>
    /// <returns>過濾後的查詢</returns>
    public static async Task<IQueryable<Employee>> FilterByDataAccessScopeAsync(
        this IQueryable<Employee> query,
        string userId,
        IAuthorizationService authorizationService)
    {
        var scope = await authorizationService.GetDataAccessScopeAsync(userId);

        return scope switch
        {
            DataAccessScope.Company => query, // 可存取所有員工
            DataAccessScope.Department => await FilterByDepartmentAsync(query, userId),
            DataAccessScope.Self => query.Where(e => e.Id == userId), // 只能存取自己
            _ => query.Where(e => e.Id == userId)
        };
    }

    /// <summary>
    /// 根據使用者的資料存取範圍過濾薪資記錄查詢
    /// </summary>
    /// <param name="query">薪資記錄查詢</param>
    /// <param name="userId">使用者識別碼</param>
    /// <param name="authorizationService">授權服務</param>
    /// <returns>過濾後的查詢</returns>
    public static async Task<IQueryable<SalaryRecord>> FilterByDataAccessScopeAsync(
        this IQueryable<SalaryRecord> query,
        string userId,
        IAuthorizationService authorizationService)
    {
        var accessibleEmployeeIds = await authorizationService.GetAccessibleEmployeeIdsAsync(userId);
        return query.Where(sr => accessibleEmployeeIds.Contains(sr.EmployeeId));
    }

    /// <summary>
    /// 根據使用者的資料存取範圍過濾請假記錄查詢
    /// </summary>
    /// <param name="query">請假記錄查詢</param>
    /// <param name="userId">使用者識別碼</param>
    /// <param name="authorizationService">授權服務</param>
    /// <returns>過濾後的查詢</returns>
    public static async Task<IQueryable<LeaveRecord>> FilterByDataAccessScopeAsync(
        this IQueryable<LeaveRecord> query,
        string userId,
        IAuthorizationService authorizationService)
    {
        var accessibleEmployeeIds = await authorizationService.GetAccessibleEmployeeIdsAsync(userId);
        return query.Where(lr => accessibleEmployeeIds.Contains(lr.EmployeeId));
    }

    /// <summary>
    /// 過濾同部門的員工
    /// </summary>
    private static async Task<IQueryable<Employee>> FilterByDepartmentAsync(
        IQueryable<Employee> query,
        string userId)
    {
        // 取得使用者的部門 ID
        var userEmployee = await query.FirstOrDefaultAsync(e => e.Id == userId);
        if (userEmployee == null)
        {
            return query.Where(e => e.Id == userId);
        }

        // 過濾同部門員工
        return query.Where(e => e.DepartmentId == userEmployee.DepartmentId);
    }
}
