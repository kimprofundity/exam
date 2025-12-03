namespace HRPayrollSystem.API.Models;

/// <summary>
/// 請假記錄實體
/// </summary>
public class LeaveRecord
{
    /// <summary>請假記錄唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>員工識別碼</summary>
    public string EmployeeId { get; set; } = null!;
    
    /// <summary>請假類型（事假/病假/特休）</summary>
    public LeaveType Type { get; set; }
    
    /// <summary>開始日期</summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>結束日期</summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>請假天數</summary>
    public decimal Days { get; set; }
    
    /// <summary>請假狀態（待審核/已核准/已拒絕）</summary>
    public LeaveStatus Status { get; set; }
    
    /// <summary>代理人使用者識別碼</summary>
    public string? ProxyUserId { get; set; }
    
    /// <summary>是否為代理請假</summary>
    public bool IsProxyRequest { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    // 導航屬性
    /// <summary>員工</summary>
    public Employee Employee { get; set; } = null!;
    
    /// <summary>代理人</summary>
    public Employee? ProxyUser { get; set; }
}
