namespace HRPayrollSystem.API.Models;

/// <summary>
/// 薪資記錄實體
/// </summary>
public class SalaryRecord
{
    /// <summary>薪資記錄唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>員工識別碼</summary>
    public string EmployeeId { get; set; } = null!;
    
    /// <summary>薪資期間（年月）</summary>
    public DateTime Period { get; set; }
    
    /// <summary>基本薪資</summary>
    public decimal BaseSalary { get; set; }
    
    /// <summary>加項總額</summary>
    public decimal TotalAdditions { get; set; }
    
    /// <summary>減項總額</summary>
    public decimal TotalDeductions { get; set; }
    
    /// <summary>應發薪資（加密儲存）</summary>
    public byte[] GrossSalary { get; set; } = null!;
    
    /// <summary>實發薪資（加密儲存）</summary>
    public byte[] NetSalary { get; set; } = null!;
    
    /// <summary>使用的費率表版本</summary>
    public string? RateTableVersion { get; set; }
    
    /// <summary>薪資狀態（草稿/已核准/已發放）</summary>
    public SalaryStatus Status { get; set; }
    
    /// <summary>是否已年度結算</summary>
    public bool IsYearEndClosed { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>建立者識別碼</summary>
    public string CreatedBy { get; set; } = null!;
    
    // 導航屬性
    /// <summary>員工</summary>
    public Employee Employee { get; set; } = null!;
    
    /// <summary>薪資項目列表</summary>
    public ICollection<SalaryItem> SalaryItems { get; set; } = new List<SalaryItem>();
}
