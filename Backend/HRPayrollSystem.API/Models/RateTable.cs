namespace HRPayrollSystem.API.Models;

/// <summary>
/// 費率表實體
/// </summary>
public class RateTable
{
    /// <summary>費率表唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>版本號</summary>
    public string Version { get; set; } = null!;
    
    /// <summary>生效日期</summary>
    public DateTime EffectiveDate { get; set; }
    
    /// <summary>失效日期</summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>勞保費率</summary>
    public decimal LaborInsuranceRate { get; set; }
    
    /// <summary>健保費率</summary>
    public decimal HealthInsuranceRate { get; set; }
    
    /// <summary>資料來源（手動/API/檔案）</summary>
    public string Source { get; set; } = null!;
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>建立者識別碼</summary>
    public string CreatedBy { get; set; } = null!;
    
    // 導航屬性
    /// <summary>建立者</summary>
    public Employee CreatedByEmployee { get; set; } = null!;
}
