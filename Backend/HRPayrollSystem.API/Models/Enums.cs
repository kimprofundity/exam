namespace HRPayrollSystem.API.Models;

/// <summary>
/// 員工狀態
/// </summary>
public enum EmployeeStatus
{
    /// <summary>在職</summary>
    Active,
    
    /// <summary>離職</summary>
    Resigned
}

/// <summary>
/// 薪資狀態
/// </summary>
public enum SalaryStatus
{
    /// <summary>草稿</summary>
    Draft,
    
    /// <summary>已核准</summary>
    Approved,
    
    /// <summary>已發放</summary>
    Paid
}

/// <summary>
/// 薪資項目類型
/// </summary>
public enum SalaryItemType
{
    /// <summary>加項</summary>
    Addition,
    
    /// <summary>減項</summary>
    Deduction
}

/// <summary>
/// 請假類型
/// </summary>
public enum LeaveType
{
    /// <summary>事假</summary>
    Personal,
    
    /// <summary>病假</summary>
    Sick,
    
    /// <summary>特休/年假</summary>
    Annual
}

/// <summary>
/// 請假狀態
/// </summary>
public enum LeaveStatus
{
    /// <summary>待審核</summary>
    Pending,
    
    /// <summary>待確認（代理請假）</summary>
    PendingConfirmation,
    
    /// <summary>已核准</summary>
    Approved,
    
    /// <summary>已拒絕</summary>
    Rejected
}

/// <summary>
/// 資料存取範圍
/// </summary>
public enum DataAccessScope
{
    /// <summary>僅自己</summary>
    Self,
    
    /// <summary>本部門</summary>
    Department,
    
    /// <summary>全公司</summary>
    Company
}

/// <summary>
/// 薪資項目計算方式
/// </summary>
public enum CalculationMethod
{
    /// <summary>固定金額</summary>
    Fixed,
    
    /// <summary>按小時計算</summary>
    Hourly,
    
    /// <summary>按比例計算</summary>
    Percentage
}
