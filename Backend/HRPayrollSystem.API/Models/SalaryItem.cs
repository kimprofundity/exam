namespace HRPayrollSystem.API.Models;

/// <summary>
/// 薪資項目實體
/// </summary>
public class SalaryItem
{
    /// <summary>薪資項目唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>所屬薪資記錄識別碼</summary>
    public string SalaryRecordId { get; set; } = null!;
    
    /// <summary>項目代碼</summary>
    public string ItemCode { get; set; } = null!;
    
    /// <summary>項目名稱</summary>
    public string ItemName { get; set; } = null!;
    
    /// <summary>項目類型（加項/減項）</summary>
    public SalaryItemType Type { get; set; }
    
    /// <summary>金額</summary>
    public decimal Amount { get; set; }
    
    /// <summary>說明</summary>
    public string? Description { get; set; }
    
    // 導航屬性
    /// <summary>所屬薪資記錄</summary>
    public SalaryRecord SalaryRecord { get; set; } = null!;
}
