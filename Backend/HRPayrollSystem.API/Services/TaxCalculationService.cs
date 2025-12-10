using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 稅務計算服務實作
/// </summary>
public class TaxCalculationService : ITaxCalculationService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<TaxCalculationService> _logger;
    private readonly IRateTableService _rateTableService;

    public TaxCalculationService(
        HRPayrollContext context,
        ILogger<TaxCalculationService> logger,
        IRateTableService rateTableService)
    {
        _context = context;
        _logger = logger;
        _rateTableService = rateTableService;
    }

    /// <summary>
    /// 計算所得稅扣繳金額
    /// </summary>
    public async Task<decimal> CalculateIncomeTaxAsync(decimal grossSalary, string employeeId, DateTime period)
    {
        try
        {
            _logger.LogInformation("開始計算員工 {EmployeeId} 於 {Period} 的所得稅", employeeId, period.ToString("yyyy-MM"));

            // 取得年度累計所得
            var year = period.Year;
            var annualIncome = await GetAnnualIncomeAsync(employeeId, year, period);
            
            // 取得扣除額和免稅額
            var deductions = await GetEmployeeDeductionsAsync(employeeId, year);
            var exemptions = await GetEmployeeExemptionsAsync(employeeId, year);
            
            // 計算年度應稅所得
            var taxableIncome = Math.Max(0, annualIncome - deductions - exemptions);
            
            // 計算年度所得稅
            var annualTax = await CalculateProgressiveTaxAsync(taxableIncome, 0, 0);
            
            // 計算已扣繳稅額
            var paidTax = await GetPaidTaxAsync(employeeId, year, period);
            
            // 計算當月應扣繳稅額
            var monthlyTax = (annualTax / 12) - (paidTax / (period.Month - 1 == 0 ? 1 : period.Month - 1));
            monthlyTax = Math.Max(0, monthlyTax);

            _logger.LogInformation("員工 {EmployeeId} 當月所得稅扣繳金額: {Tax}", employeeId, monthlyTax);
            return Math.Round(monthlyTax, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算員工 {EmployeeId} 所得稅時發生錯誤", employeeId);
            throw;
        }
    }

    /// <summary>
    /// 計算勞保費扣除額
    /// </summary>
    public async Task<decimal> CalculateLaborInsuranceAsync(decimal salary, DateTime period)
    {
        try
        {
            _logger.LogInformation("開始計算勞保費，薪資: {Salary}, 期間: {Period}", salary, period.ToString("yyyy-MM"));

            // 取得生效的費率表
            var rateTable = await _rateTableService.GetEffectiveRateTableAsync(period);
            if (rateTable == null)
            {
                _logger.LogWarning("找不到 {Period} 生效的費率表，使用預設費率", period.ToString("yyyy-MM"));
                return salary * 0.105m * 0.2m; // 預設勞保費率 10.5%，員工負擔 20%
            }

            // 勞保投保薪資級距對照
            var insuranceSalary = GetLaborInsuranceSalary(salary);
            
            // 計算勞保費（員工負擔部分，通常為 20%）
            var laborInsurance = insuranceSalary * rateTable.LaborInsuranceRate * 0.2m;

            _logger.LogInformation("勞保費計算結果: 投保薪資 {InsuranceSalary}, 費率 {Rate}, 扣除額 {Amount}", 
                insuranceSalary, rateTable.LaborInsuranceRate, laborInsurance);

            return Math.Round(laborInsurance, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算勞保費時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 計算健保費扣除額
    /// </summary>
    public async Task<decimal> CalculateHealthInsuranceAsync(decimal salary, DateTime period)
    {
        try
        {
            _logger.LogInformation("開始計算健保費，薪資: {Salary}, 期間: {Period}", salary, period.ToString("yyyy-MM"));

            // 取得生效的費率表
            var rateTable = await _rateTableService.GetEffectiveRateTableAsync(period);
            if (rateTable == null)
            {
                _logger.LogWarning("找不到 {Period} 生效的費率表，使用預設費率", period.ToString("yyyy-MM"));
                return salary * 0.0517m * 0.3m; // 預設健保費率 5.17%，員工負擔 30%
            }

            // 健保投保金額級距對照
            var insuranceAmount = GetHealthInsuranceAmount(salary);
            
            // 計算健保費（員工負擔部分，通常為 30%）
            var healthInsurance = insuranceAmount * rateTable.HealthInsuranceRate * 0.3m;

            _logger.LogInformation("健保費計算結果: 投保金額 {InsuranceAmount}, 費率 {Rate}, 扣除額 {Amount}", 
                insuranceAmount, rateTable.HealthInsuranceRate, healthInsurance);

            return Math.Round(healthInsurance, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算健保費時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 計算累進稅率的所得稅
    /// </summary>
    public async Task<decimal> CalculateProgressiveTaxAsync(decimal annualIncome, decimal deductions, decimal exemptions)
    {
        try
        {
            _logger.LogInformation("開始計算累進稅率所得稅，年度所得: {Income}", annualIncome);

            var taxableIncome = Math.Max(0, annualIncome - deductions - exemptions);
            if (taxableIncome <= 0)
            {
                return 0;
            }

            // 取得累進稅率表（使用當年度）
            var taxBrackets = await GetProgressiveTaxBracketsAsync(DateTime.Now.Year);
            
            decimal totalTax = 0;
            decimal remainingIncome = taxableIncome;

            foreach (var bracket in taxBrackets.OrderBy(b => b.MinIncome))
            {
                if (remainingIncome <= 0) break;

                var bracketIncome = bracket.MaxIncome.HasValue 
                    ? Math.Min(remainingIncome, bracket.MaxIncome.Value - bracket.MinIncome)
                    : remainingIncome;

                if (bracketIncome > 0)
                {
                    var bracketTax = bracketIncome * (bracket.TaxRate / 100);
                    totalTax += bracketTax;
                    remainingIncome -= bracketIncome;

                    _logger.LogDebug("稅率級距計算: 所得 {Income}, 稅率 {Rate}%, 稅額 {Tax}", 
                        bracketIncome, bracket.TaxRate, bracketTax);
                }
            }

            _logger.LogInformation("累進稅率計算結果: 應稅所得 {TaxableIncome}, 總稅額 {TotalTax}", 
                taxableIncome, totalTax);

            return Math.Round(totalTax, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算累進稅率所得稅時發生錯誤");
            throw;
        }
    }

    /// <summary>
    /// 取得員工的扣除額設定
    /// </summary>
    public async Task<decimal> GetEmployeeDeductionsAsync(string employeeId, int year)
    {
        try
        {
            // 從系統參數取得標準扣除額
            var standardDeduction = await GetSystemParameterAsync($"StandardDeduction_{year}", 120000m);
            
            // 可以擴展為從員工設定取得個人扣除額
            // 例如：房貸利息、保險費、醫療費等
            
            return standardDeduction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得員工 {EmployeeId} 扣除額時發生錯誤", employeeId);
            return 120000m; // 預設標準扣除額
        }
    }

    /// <summary>
    /// 取得員工的免稅額設定
    /// </summary>
    public async Task<decimal> GetEmployeeExemptionsAsync(string employeeId, int year)
    {
        try
        {
            // 從系統參數取得個人免稅額
            var personalExemption = await GetSystemParameterAsync($"PersonalExemption_{year}", 92000m);
            
            // 可以擴展為從員工設定取得眷屬免稅額
            
            return personalExemption;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得員工 {EmployeeId} 免稅額時發生錯誤", employeeId);
            return 92000m; // 預設個人免稅額
        }
    }

    /// <summary>
    /// 取得累進稅率表
    /// </summary>
    public async Task<List<TaxBracket>> GetProgressiveTaxBracketsAsync(int year)
    {
        try
        {
            // 台灣 2024 年度綜合所得稅累進稅率表
            return new List<TaxBracket>
            {
                new TaxBracket { MinIncome = 0, MaxIncome = 560000, TaxRate = 5, CumulativeDifference = 0 },
                new TaxBracket { MinIncome = 560000, MaxIncome = 1260000, TaxRate = 12, CumulativeDifference = 39200 },
                new TaxBracket { MinIncome = 1260000, MaxIncome = 2520000, TaxRate = 20, CumulativeDifference = 140000 },
                new TaxBracket { MinIncome = 2520000, MaxIncome = 4720000, TaxRate = 30, CumulativeDifference = 392000 },
                new TaxBracket { MinIncome = 4720000, MaxIncome = null, TaxRate = 40, CumulativeDifference = 864000 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得累進稅率表時發生錯誤");
            throw;
        }
    }

    #region 私有方法

    /// <summary>
    /// 取得年度累計所得
    /// </summary>
    private async Task<decimal> GetAnnualIncomeAsync(string employeeId, int year, DateTime currentPeriod)
    {
        var startOfYear = new DateTime(year, 1, 1);
        var endOfCurrentMonth = new DateTime(currentPeriod.Year, currentPeriod.Month, 1).AddMonths(1).AddDays(-1);

        var salaryRecords = await _context.SalaryRecords
            .Where(sr => sr.EmployeeId == employeeId && 
                        sr.Period >= startOfYear && 
                        sr.Period <= endOfCurrentMonth &&
                        sr.Status != SalaryStatus.Draft)
            .ToListAsync();

        decimal totalIncome = 0;
        foreach (var record in salaryRecords)
        {
            // 解密薪資金額
            var grossSalary = DecryptSalary(record.GrossSalary);
            totalIncome += grossSalary;
        }

        return totalIncome;
    }

    /// <summary>
    /// 取得已扣繳稅額
    /// </summary>
    private async Task<decimal> GetPaidTaxAsync(string employeeId, int year, DateTime currentPeriod)
    {
        var startOfYear = new DateTime(year, 1, 1);
        var endOfPreviousMonth = new DateTime(currentPeriod.Year, currentPeriod.Month, 1).AddDays(-1);

        var taxItems = await _context.SalaryItems
            .Where(si => si.SalaryRecord.EmployeeId == employeeId &&
                        si.SalaryRecord.Period >= startOfYear &&
                        si.SalaryRecord.Period <= endOfPreviousMonth &&
                        si.ItemCode == "INCOME_TAX" &&
                        si.Type == SalaryItemType.Deduction)
            .SumAsync(si => si.Amount);

        return taxItems;
    }

    /// <summary>
    /// 取得勞保投保薪資級距
    /// </summary>
    private decimal GetLaborInsuranceSalary(decimal salary)
    {
        // 勞保投保薪資分級表（簡化版）
        var brackets = new[]
        {
            (min: 0m, max: 25200m, insurance: 25200m),
            (min: 25200m, max: 26400m, insurance: 26400m),
            (min: 26400m, max: 27600m, insurance: 27600m),
            (min: 27600m, max: 28800m, insurance: 28800m),
            (min: 28800m, max: 30300m, insurance: 30300m),
            (min: 30300m, max: 31800m, insurance: 31800m),
            (min: 31800m, max: 33300m, insurance: 33300m),
            (min: 33300m, max: 34800m, insurance: 34800m),
            (min: 34800m, max: 36300m, insurance: 36300m),
            (min: 36300m, max: 38200m, insurance: 38200m),
            (min: 38200m, max: 40100m, insurance: 40100m),
            (min: 40100m, max: 42000m, insurance: 42000m),
            (min: 42000m, max: 43900m, insurance: 43900m),
            (min: 43900m, max: decimal.MaxValue, insurance: 45800m)
        };

        var bracket = brackets.FirstOrDefault(b => salary >= b.min && salary < b.max);
        return bracket.insurance > 0 ? bracket.insurance : 45800m; // 最高級距
    }

    /// <summary>
    /// 取得健保投保金額級距
    /// </summary>
    private decimal GetHealthInsuranceAmount(decimal salary)
    {
        // 健保投保金額分級表（簡化版）
        var brackets = new[]
        {
            (min: 0m, max: 25200m, insurance: 25200m),
            (min: 25200m, max: 26400m, insurance: 26400m),
            (min: 26400m, max: 27600m, insurance: 27600m),
            (min: 27600m, max: 28800m, insurance: 28800m),
            (min: 28800m, max: 30300m, insurance: 30300m),
            (min: 30300m, max: 31800m, insurance: 31800m),
            (min: 31800m, max: 33300m, insurance: 33300m),
            (min: 33300m, max: 34800m, insurance: 34800m),
            (min: 34800m, max: 36300m, insurance: 36300m),
            (min: 36300m, max: 38200m, insurance: 38200m),
            (min: 38200m, max: 40100m, insurance: 40100m),
            (min: 40100m, max: 42000m, insurance: 42000m),
            (min: 42000m, max: 43900m, insurance: 43900m),
            (min: 43900m, max: decimal.MaxValue, insurance: 182000m)
        };

        var bracket = brackets.FirstOrDefault(b => salary >= b.min && salary < b.max);
        return bracket.insurance > 0 ? bracket.insurance : 182000m; // 最高級距
    }

    /// <summary>
    /// 取得系統參數
    /// </summary>
    private async Task<decimal> GetSystemParameterAsync(string key, decimal defaultValue)
    {
        try
        {
            var parameter = await _context.SystemParameters
                .Where(sp => sp.Key == key && 
                           sp.EffectiveDate <= DateTime.Now &&
                           (sp.ExpiryDate == null || sp.ExpiryDate > DateTime.Now))
                .OrderByDescending(sp => sp.EffectiveDate)
                .FirstOrDefaultAsync();

            if (parameter != null && decimal.TryParse(parameter.Value, out var value))
            {
                return value;
            }

            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得系統參數 {Key} 時發生錯誤", key);
            return defaultValue;
        }
    }

    /// <summary>
    /// 解密薪資金額
    /// </summary>
    private decimal DecryptSalary(byte[] encryptedSalary)
    {
        // 這裡應該實作實際的解密邏輯
        // 暫時使用簡化的轉換
        if (encryptedSalary == null || encryptedSalary.Length == 0)
            return 0;

        try
        {
            // 假設加密的資料可以直接轉換為 decimal
            // 實際實作時需要使用適當的解密演算法
            var decimalBytes = new byte[16];
            Array.Copy(encryptedSalary, 0, decimalBytes, 0, Math.Min(encryptedSalary.Length, 16));
            
            // 簡化處理：假設前 4 個位元組是 decimal 的整數部分
            if (encryptedSalary.Length >= 4)
            {
                return BitConverter.ToInt32(encryptedSalary, 0);
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密薪資金額時發生錯誤");
            return 0;
        }
    }

    #endregion
}