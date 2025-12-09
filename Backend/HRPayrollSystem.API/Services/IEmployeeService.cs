using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 員工管理服務介面
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// 建立新員工
    /// </summary>
    /// <param name="employeeNumber">員工編號</param>
    /// <param name="name">員工姓名</param>
    /// <param name="departmentId">部門識別碼</param>
    /// <param name="position">職位</param>
    /// <param name="monthlySalary">固定月薪</param>
    /// <param name="bankCode">銀行代碼</param>
    /// <param name="bankAccount">銀行帳號</param>
    /// <returns>建立的員工識別碼</returns>
    Task<string> CreateEmployeeAsync(
        string employeeNumber,
        string name,
        string departmentId,
        string? position,
        decimal monthlySalary,
        string? bankCode,
        string? bankAccount);

    /// <summary>
    /// 更新員工資料
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="name">員工姓名</param>
    /// <param name="position">職位</param>
    /// <param name="monthlySalary">固定月薪</param>
    /// <param name="bankCode">銀行代碼</param>
    /// <param name="bankAccount">銀行帳號</param>
    Task UpdateEmployeeAsync(
        string employeeId,
        string name,
        string? position,
        decimal monthlySalary,
        string? bankCode,
        string? bankAccount);

    /// <summary>
    /// 員工部門調動
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="newDepartmentId">新部門識別碼</param>
    /// <param name="transferDate">調動日期</param>
    Task TransferDepartmentAsync(string employeeId, string newDepartmentId, DateTime transferDate);

    /// <summary>
    /// 員工離職
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="resignationDate">離職日期</param>
    Task ResignEmployeeAsync(string employeeId, DateTime resignationDate);

    /// <summary>
    /// 取得員工資料
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <returns>員工資料</returns>
    Task<EmployeeDto?> GetEmployeeAsync(string employeeId);

    /// <summary>
    /// 取得員工列表
    /// </summary>
    /// <param name="departmentId">部門識別碼（可選）</param>
    /// <param name="status">員工狀態（可選）</param>
    /// <param name="searchKeyword">搜尋關鍵字（可選）</param>
    /// <param name="pageNumber">頁碼</param>
    /// <param name="pageSize">每頁筆數</param>
    /// <returns>員工列表</returns>
    Task<PagedResult<EmployeeDto>> GetEmployeesAsync(
        string? departmentId = null,
        EmployeeStatus? status = null,
        string? searchKeyword = null,
        int pageNumber = 1,
        int pageSize = 20);

    /// <summary>
    /// 檢查員工編號是否已存在
    /// </summary>
    /// <param name="employeeNumber">員工編號</param>
    /// <returns>是否存在</returns>
    Task<bool> EmployeeNumberExistsAsync(string employeeNumber);
}

/// <summary>
/// 員工資料傳輸物件
/// </summary>
public class EmployeeDto
{
    public string Id { get; set; } = null!;
    public string EmployeeNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string DepartmentId { get; set; } = null!;
    public string DepartmentName { get; set; } = null!;
    public string? Position { get; set; }
    public decimal MonthlySalary { get; set; }
    public string? BankCode { get; set; }
    public string? BankAccount { get; set; }
    public EmployeeStatus Status { get; set; }
    public DateTime? ResignationDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 分頁結果
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
