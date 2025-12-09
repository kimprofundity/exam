namespace HRPayrollSystem.API.Models.DTOs;

/// <summary>
/// 薪資項目定義 DTO
/// </summary>
public class SalaryItemDefinitionDto
{
    /// <summary>項目代碼</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>項目名稱</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>項目類型（Addition/Deduction）</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>計算方式（Fixed/Hourly/Percentage）</summary>
    public string CalculationMethod { get; set; } = string.Empty;

    /// <summary>預設金額（固定金額時使用）</summary>
    public decimal? DefaultAmount { get; set; }

    /// <summary>小時費率（按小時計算時使用）</summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>比例費率（按比例計算時使用）</summary>
    public decimal? PercentageRate { get; set; }

    /// <summary>項目說明</summary>
    public string? Description { get; set; }

    /// <summary>生效日期</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>失效日期</summary>
    public DateTime? ExpiryDate { get; set; }
}

/// <summary>
/// 薪資項目定義回應 DTO
/// </summary>
public class SalaryItemDefinitionResponseDto
{
    /// <summary>識別碼</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>項目代碼</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>項目名稱</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>項目類型</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>計算方式</summary>
    public string CalculationMethod { get; set; } = string.Empty;

    /// <summary>預設金額</summary>
    public decimal? DefaultAmount { get; set; }

    /// <summary>小時費率</summary>
    public decimal? HourlyRate { get; set; }

    /// <summary>比例費率</summary>
    public decimal? PercentageRate { get; set; }

    /// <summary>項目說明</summary>
    public string? Description { get; set; }

    /// <summary>是否啟用</summary>
    public bool IsActive { get; set; }

    /// <summary>生效日期</summary>
    public DateTime EffectiveDate { get; set; }

    /// <summary>失效日期</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>建立者</summary>
    public string CreatedBy { get; set; } = string.Empty;
}
