namespace HRPayrollSystem.API.Models;

/// <summary>
/// 部門實體
/// </summary>
public class Department
{
    /// <summary>部門唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>部門代碼</summary>
    public string Code { get; set; } = null!;
    
    /// <summary>部門名稱</summary>
    public string Name { get; set; } = null!;
    
    /// <summary>部門主管員工識別碼</summary>
    public string? ManagerId { get; set; }
    
    /// <summary>上級部門識別碼</summary>
    public string? ParentDepartmentId { get; set; }
    
    /// <summary>是否啟用</summary>
    public bool IsActive { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    /// <summary>部門主管</summary>
    public Employee? Manager { get; set; }
    
    /// <summary>上級部門</summary>
    public Department? ParentDepartment { get; set; }
    
    /// <summary>下級部門列表</summary>
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    
    /// <summary>部門員工列表</summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
