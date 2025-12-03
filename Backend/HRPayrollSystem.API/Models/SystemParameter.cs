namespace HRPayrollSystem.API.Models;

/// <summary>
/// 系統參數實體
/// </summary>
public class SystemParameter
{
    /// <summary>系統參數唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>參數鍵值</summary>
    public string Key { get; set; } = null!;
    
    /// <summary>參數值</summary>
    public string Value { get; set; } = null!;
    
    /// <summary>資料類型</summary>
    public string DataType { get; set; } = null!;
    
    /// <summary>生效日期</summary>
    public DateTime EffectiveDate { get; set; }
    
    /// <summary>失效日期</summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>參數說明</summary>
    public string? Description { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>建立者識別碼</summary>
    public string CreatedBy { get; set; } = null!;
    
    // 導航屬性
    /// <summary>建立者</summary>
    public Employee CreatedByEmployee { get; set; } = null!;
}
