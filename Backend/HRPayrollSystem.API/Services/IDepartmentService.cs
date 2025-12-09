using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 部門管理服務介面
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// 建立新部門
    /// </summary>
    /// <param name="code">部門代碼</param>
    /// <param name="name">部門名稱</param>
    /// <param name="managerId">部門主管員工識別碼（可選）</param>
    /// <param name="parentDepartmentId">上級部門識別碼（可選）</param>
    /// <returns>建立的部門</returns>
    Task<Department> CreateDepartmentAsync(string code, string name, string? managerId = null, string? parentDepartmentId = null);

    /// <summary>
    /// 更新部門資訊
    /// </summary>
    /// <param name="departmentId">部門識別碼</param>
    /// <param name="code">部門代碼</param>
    /// <param name="name">部門名稱</param>
    /// <param name="managerId">部門主管員工識別碼（可選）</param>
    /// <param name="parentDepartmentId">上級部門識別碼（可選）</param>
    /// <returns>更新後的部門</returns>
    Task<Department> UpdateDepartmentAsync(string departmentId, string code, string name, string? managerId = null, string? parentDepartmentId = null);

    /// <summary>
    /// 取得部門資訊
    /// </summary>
    /// <param name="departmentId">部門識別碼</param>
    /// <returns>部門資訊，包含員工數量和主管資訊</returns>
    Task<Department?> GetDepartmentAsync(string departmentId);

    /// <summary>
    /// 取得所有部門列表
    /// </summary>
    /// <param name="includeInactive">是否包含已停用的部門</param>
    /// <returns>部門列表</returns>
    Task<List<Department>> GetAllDepartmentsAsync(bool includeInactive = false);

    /// <summary>
    /// 取得部門的組織階層
    /// </summary>
    /// <param name="departmentId">部門識別碼</param>
    /// <returns>包含上下級關係的部門階層</returns>
    Task<DepartmentHierarchy> GetDepartmentHierarchyAsync(string departmentId);

    /// <summary>
    /// 停用部門
    /// </summary>
    /// <param name="departmentId">部門識別碼</param>
    /// <returns>是否成功停用</returns>
    /// <exception cref="InvalidOperationException">當部門有在職員工時拋出</exception>
    Task<bool> DeactivateDepartmentAsync(string departmentId);

    /// <summary>
    /// 啟用部門
    /// </summary>
    /// <param name="departmentId">部門識別碼</param>
    /// <returns>是否成功啟用</returns>
    Task<bool> ActivateDepartmentAsync(string departmentId);

    /// <summary>
    /// 取得部門的員工數量
    /// </summary>
    /// <param name="departmentId">部門識別碼</param>
    /// <returns>在職員工數量</returns>
    Task<int> GetActiveEmployeeCountAsync(string departmentId);
}

/// <summary>
/// 部門階層資訊
/// </summary>
public class DepartmentHierarchy
{
    /// <summary>當前部門</summary>
    public Department Department { get; set; } = null!;

    /// <summary>上級部門列表（從根部門到當前部門）</summary>
    public List<Department> Ancestors { get; set; } = new();

    /// <summary>下級部門列表</summary>
    public List<Department> Children { get; set; } = new();

    /// <summary>在職員工數量</summary>
    public int ActiveEmployeeCount { get; set; }
}
