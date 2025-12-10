using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 稅務計算服務介面
/// </summary>
public interface ITaxCalculationService
{
    /// <summary>
    /// 計算所得稅扣繳金額
    /// </summary>
    /// <param name="grossSalary">應發薪資</param>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="period">薪資期間</param>
    /// <returns>所得稅扣繳金額</returns>
    Task<decimal> CalculateIncomeTaxAsync(decimal grossSalary, string employeeId, DateTime period);

    /// <summary>
    /// 計算勞保費扣除額
    /// </summary>
    /// <param name="salary">薪資金額</param>
    /// <param name="period">薪資期間</param>
    /// <returns>勞保費扣除額</returns>
    Task<decimal> CalculateLaborInsuranceAsync(decimal salary, DateTime period);

    /// <summary>
    /// 計算健保費扣除額
    /// </summary>
    /// <param name="salary">薪資金額</param>
    /// <param name="period">薪資期間</param>
    /// <returns>健保費扣除額</returns>
    Task<decimal> CalculateHealthInsuranceAsync(decimal salary, DateTime period);

    /// <summary>
    /// 計算累進稅率的所得稅
    /// </summary>
    /// <param name="annualIncome">年度所得</param>
    /// <param name="deductions">扣除額</param>
    /// <param name="exemptions">免稅額</param>
    /// <returns>年度所得稅金額</returns>
    Task<decimal> CalculateProgressiveTaxAsync(decimal annualIncome, decimal deductions, decimal exemptions);

    /// <summary>
    /// 取得員工的扣除額設定
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="year">年度</param>
    /// <returns>扣除額金額</returns>
    Task<decimal> GetEmployeeDeductionsAsync(string employeeId, int year);

    /// <summary>
    /// 取得員工的免稅額設定
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="year">年度</param>
    /// <returns>免稅額金額</returns>
    Task<decimal> GetEmployeeExemptionsAsync(string employeeId, int year);

    /// <summary>
    /// 取得累進稅率表
    /// </summary>
    /// <param name="year">年度</param>
    /// <returns>累進稅率表</returns>
    Task<List<TaxBracket>> GetProgressiveTaxBracketsAsync(int year);
}

/// <summary>
/// 稅率級距
/// </summary>
public class TaxBracket
{
    /// <summary>級距下限</summary>
    public decimal MinIncome { get; set; }
    
    /// <summary>級距上限（null 表示無上限）</summary>
    public decimal? MaxIncome { get; set; }
    
    /// <summary>稅率（百分比）</summary>
    public decimal TaxRate { get; set; }
    
    /// <summary>累進差額</summary>
    public decimal CumulativeDifference { get; set; }
}