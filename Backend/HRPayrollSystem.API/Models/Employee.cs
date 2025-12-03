namespace HRPayrollSystem.API.Models;

/// <summary>
/// 員工實體
/// </summary>
public class Employee
{
    /// <summary>員工唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>員工編號</summary>
    public string EmployeeNumber { get; set; } = null!;
    
    /// <summary>員工姓名</summary>
    public string Name { get; set; } = null!;
    
    /// <summary>所屬部門識別碼</summary>
    public string DepartmentId { get; set; } = null!;
    
    /// <summary>職位</summary>
    public string? Position { get; set; }
    
    /// <summary>固定月薪金額</summary>
    public decimal MonthlySalary { get; set; }
    
    /// <summary>銀行代碼</summary>
    public string? BankCode { get; set; }
    
    /// <summary>銀行帳號（加密儲存）</summary>
    public string? BankAccount { get; set; }
    
    /// <summary>員工狀態（在職/離職）</summary>
    public EmployeeStatus Status { get; set; }
    
    /// <summary>離職日期</summary>
    public DateTime? ResignationDate { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    /// <summary>所屬部門</summary>
    public Department Department { get; set; } = null!;
    
    /// <summary>薪資記錄列表</summary>
    public ICollection<SalaryRecord> SalaryRecords { get; set; } = new List<SalaryRecord>();
    
    /// <summary>請假記錄列表</summary>
    public ICollection<LeaveRecord> LeaveRecords { get; set; } = new List<LeaveRecord>();
}
