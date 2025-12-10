using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 薪資計算服務實作
/// </summary>
public class PayrollCalculationService : IPayrollCalculationService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<PayrollCalculationService> _logger;
    private readonly IRateTableService _rateTableService;
    private readonly ISalaryItemDefinitionService _salaryItemDefinitionService;
    private readonly ITaxCalculationService _taxCalculationService;

    public PayrollCalculationService(
        HRPayrollContext context,
        ILogger<PayrollCalculationService> logger,
        IRateTableService rateTableService,
        ISalaryItemDefinitionService salaryItemDefinitionService,
        ITaxCalculationService taxCalculationService)
    {
        _context = context;
        _logger = logger;
        _rateTableService = rateTableService;
        _salaryItemDefinitionService = salaryItemDefinitionService;
        _taxCalculationService = taxCalculationService;
    }

    /// <summary>
    /// 計算員工薪資
    /// </summary>
    public async Task<SalaryRecord> CalculateSalaryAsync(string employeeId, DateTime period, bool copyFromPreviousMonth = false)
    {
        try
        {
            _logger.LogInformation("開始計算員工薪資：員工ID {EmployeeId}，期間 {Period}", employeeId, period);

            // 1. 取得員工資料
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                throw new ArgumentException($"找不到員工：{employeeId}");
            }

            // 2. 載入上月薪資記錄（如果需要）
            SalaryRecord? previousMonthSalary = null;
            if (copyFromPreviousMonth)
            {
                previousMonthSalary = await LoadPreviousMonthSalaryAsync(employeeId, period);
            }

            // 3. 取得工作天數
            var totalWorkDays = await GetWorkingDaysInMonthAsync(period);
            var actualWorkDays = await CalculateActualWorkDaysAsync(employeeId, period);

            // 4. 計算基本薪資
            var baseSalary = await CalculateBaseSalaryAsync(employee, period, actualWorkDays, totalWorkDays);

            // 5. 計算請假扣款
            var leaveDeduction = await CalculateLeaveDeductionAsync(employeeId, period);

            // 6. 處理薪資項目（加項和減項）
            var salaryItems = await ProcessSalaryItemsAsync(employeeId, period, copyFromPreviousMonth);

            // 7. 計算加項和減項總額
            var totalAdditions = salaryItems.Where(x => x.Type == SalaryItemType.Addition).Sum(x => x.Amount);
            var totalDeductions = salaryItems.Where(x => x.Type == SalaryItemType.Deduction).Sum(x => x.Amount);

            // 8. 計算應發薪資
            var grossSalary = baseSalary + totalAdditions - leaveDeduction;

            // 9. 計算勞健保扣除額
            var (laborInsurance, healthInsurance) = await CalculateInsuranceDeductionsAsync(grossSalary, period);
            totalDeductions += laborInsurance + healthInsurance;

            // 10. 計算所得稅扣繳
            var incomeTax = await _taxCalculationService.CalculateIncomeTaxAsync(grossSalary, employeeId, period);
            totalDeductions += incomeTax;

            // 11. 計算實發薪資
            var netSalary = grossSalary - totalDeductions;

            // 12. 取得使用的費率表版本
            var rateTable = await _rateTableService.GetEffectiveRateTableAsync(period);
            var rateTableVersion = rateTable?.Version ?? "未知";

            // 13. 建立薪資記錄
            var salaryRecord = new SalaryRecord
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeId = employeeId,
                Period = new DateTime(period.Year, period.Month, 1), // 統一為月初
                BaseSalary = baseSalary,
                TotalAdditions = totalAdditions,
                TotalDeductions = totalDeductions,
                GrossSalary = EncryptDecimal(grossSalary), // 加密儲存
                NetSalary = EncryptDecimal(netSalary), // 加密儲存
                RateTableVersion = rateTableVersion,
                Status = SalaryStatus.Draft,
                IsYearEndClosed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "system", // TODO: 從當前使用者取得
                SalaryItems = new List<SalaryItem>()
            };

            // 14. 加入勞健保扣除項目
            if (laborInsurance > 0)
            {
                salaryItems.Add(new SalaryItem
                {
                    Id = Guid.NewGuid().ToString(),
                    SalaryRecordId = salaryRecord.Id,
                    ItemCode = "LABOR_INS",
                    ItemName = "勞保費",
                    Type = SalaryItemType.Deduction,
                    Amount = laborInsurance,
                    Description = $"勞保費扣除（費率表版本：{rateTableVersion}）"
                });
            }

            if (healthInsurance > 0)
            {
                salaryItems.Add(new SalaryItem
                {
                    Id = Guid.NewGuid().ToString(),
                    SalaryRecordId = salaryRecord.Id,
                    ItemCode = "HEALTH_INS",
                    ItemName = "健保費",
                    Type = SalaryItemType.Deduction,
                    Amount = healthInsurance,
                    Description = $"健保費扣除（費率表版本：{rateTableVersion}）"
                });
            }

            // 15. 加入請假扣款項目
            if (leaveDeduction > 0)
            {
                salaryItems.Add(new SalaryItem
                {
                    Id = Guid.NewGuid().ToString(),
                    SalaryRecordId = salaryRecord.Id,
                    ItemCode = "LEAVE_DEDUCTION",
                    ItemName = "請假扣款",
                    Type = SalaryItemType.Deduction,
                    Amount = leaveDeduction,
                    Description = $"請假扣款（實際出勤：{actualWorkDays}/{totalWorkDays}天）"
                });
            }

            // 16. 加入所得稅扣繳項目
            if (incomeTax > 0)
            {
                salaryItems.Add(new SalaryItem
                {
                    Id = Guid.NewGuid().ToString(),
                    SalaryRecordId = salaryRecord.Id,
                    ItemCode = "INCOME_TAX",
                    ItemName = "所得稅扣繳",
                    Type = SalaryItemType.Deduction,
                    Amount = incomeTax,
                    Description = $"所得稅扣繳（應發薪資：{grossSalary:N0}）"
                });
            }

            // 17. 設定薪資項目到薪資記錄
            salaryRecord.SalaryItems = salaryItems;

            _logger.LogInformation(
                "薪資計算完成：員工ID {EmployeeId}，基本薪資 {BaseSalary}，應發薪資 {GrossSalary}，實發薪資 {NetSalary}，所得稅 {IncomeTax}",
                employeeId, baseSalary, grossSalary, netSalary, incomeTax);

            return salaryRecord;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算員工薪資時發生錯誤：員工ID {EmployeeId}，期間 {Period}", employeeId, period);
            throw;
        }
    }

    /// <summary>
    /// 計算基本薪資（支援月薪/日薪/時薪）
    /// </summary>
    public async Task<decimal> CalculateBaseSalaryAsync(Employee employee, DateTime period, decimal workDays, decimal totalWorkDays)
    {
        decimal baseSalary = 0;

        switch (employee.SalaryType)
        {
            case SalaryType.Monthly:
                // 月薪：月薪 / 30 × 出勤天數
                var dailySalaryFromMonthly = employee.MonthlySalary / 30m;
                baseSalary = dailySalaryFromMonthly * workDays;
                _logger.LogDebug(
                    "月薪計算：員工 {EmployeeId}，月薪 {MonthlySalary}，日薪 {DailySalary}，出勤天數 {WorkDays}，基本薪資 {BaseSalary}",
                    employee.Id, employee.MonthlySalary, dailySalaryFromMonthly, workDays, baseSalary);
                break;

            case SalaryType.Daily:
                // 日薪：日薪 × 出勤天數
                var dailySalary = employee.DailySalary ?? 0;
                baseSalary = dailySalary * workDays;
                _logger.LogDebug(
                    "日薪計算：員工 {EmployeeId}，日薪 {DailySalary}，出勤天數 {WorkDays}，基本薪資 {BaseSalary}",
                    employee.Id, dailySalary, workDays, baseSalary);
                break;

            case SalaryType.Hourly:
                // 時薪：需要工時資料，這裡先用預設8小時/天
                var hourlySalary = employee.HourlySalary ?? 0;
                var defaultHoursPerDay = 8m; // 預設每天8小時
                baseSalary = hourlySalary * defaultHoursPerDay * workDays;
                _logger.LogDebug(
                    "時薪計算：員工 {EmployeeId}，時薪 {HourlySalary}，每日工時 {HoursPerDay}，出勤天數 {WorkDays}，基本薪資 {BaseSalary}",
                    employee.Id, hourlySalary, defaultHoursPerDay, workDays, baseSalary);
                break;

            default:
                throw new ArgumentException($"不支援的薪資類型：{employee.SalaryType}");
        }

        return await Task.FromResult(baseSalary);
    }

    /// <summary>
    /// 計算請假扣款
    /// </summary>
    public async Task<decimal> CalculateLeaveDeductionAsync(string employeeId, DateTime period)
    {
        try
        {
            // 取得當月請假記錄（僅計算事假，因為事假是無薪假）
            var startDate = new DateTime(period.Year, period.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var personalLeaves = await _context.LeaveRecords
                .Where(lr => lr.EmployeeId == employeeId &&
                           lr.Type == LeaveType.Personal &&
                           lr.Status == LeaveStatus.Approved &&
                           lr.StartDate >= startDate &&
                           lr.EndDate <= endDate)
                .ToListAsync();

            if (!personalLeaves.Any())
            {
                return 0;
            }

            // 取得員工資料
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return 0;
            }

            // 計算事假天數
            var totalPersonalLeaveDays = personalLeaves.Sum(x => x.Days);

            // 取得當月工作天數
            var totalWorkDays = await GetWorkingDaysInMonthAsync(period);

            // 根據薪資類型計算扣款金額
            decimal dailySalary = 0;
            switch (employee.SalaryType)
            {
                case SalaryType.Monthly:
                    dailySalary = employee.MonthlySalary / 30m;
                    break;
                case SalaryType.Daily:
                    dailySalary = employee.DailySalary ?? 0;
                    break;
                case SalaryType.Hourly:
                    var hourlySalary = employee.HourlySalary ?? 0;
                    dailySalary = hourlySalary * 8m; // 預設每天8小時
                    break;
            }
            
            var deduction = dailySalary * totalPersonalLeaveDays;

            _logger.LogDebug(
                "請假扣款計算：員工 {EmployeeId}，事假天數 {LeaveDays}，日薪 {DailySalary}，扣款 {Deduction}",
                employeeId, totalPersonalLeaveDays, dailySalary, deduction);

            return deduction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算請假扣款時發生錯誤：員工ID {EmployeeId}，期間 {Period}", employeeId, period);
            return 0;
        }
    }

    /// <summary>
    /// 計算加班費
    /// </summary>
    public async Task<decimal> CalculateOvertimePayAsync(string employeeId, DateTime period, decimal overtimeHours)
    {
        try
        {
            if (overtimeHours <= 0)
            {
                return 0;
            }

            // 取得員工資料
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
            {
                return 0;
            }

            // 取得加班費項目定義
            var overtimeDefinition = await _salaryItemDefinitionService.GetActiveItemsAsync();
            var overtimeItem = overtimeDefinition.FirstOrDefault(x => 
                x.ItemCode == "OVERTIME" && 
                x.Type == SalaryItemType.Addition &&
                x.CalculationMethod == CalculationMethod.Hourly);

            if (overtimeItem == null)
            {
                _logger.LogWarning("找不到加班費項目定義，使用預設費率");
                // 使用預設加班費率：時薪 × 1.34 倍
                var defaultHourlyRate = employee.MonthlySalary / 30 / 8; // 假設每月30天，每天8小時
                return defaultHourlyRate * 1.34m * overtimeHours;
            }

            // 使用定義的加班費率
            var hourlyRate = overtimeItem.HourlyRate ?? 0;
            var overtimePay = hourlyRate * overtimeHours;

            _logger.LogDebug(
                "加班費計算：員工 {EmployeeId}，加班時數 {OvertimeHours}，時薪 {HourlyRate}，加班費 {OvertimePay}",
                employeeId, overtimeHours, hourlyRate, overtimePay);

            return overtimePay;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算加班費時發生錯誤：員工ID {EmployeeId}，期間 {Period}", employeeId, period);
            return 0;
        }
    }

    /// <summary>
    /// 處理薪資加項和減項
    /// </summary>
    public async Task<List<SalaryItem>> ProcessSalaryItemsAsync(string employeeId, DateTime period, bool copyFromPreviousMonth)
    {
        var salaryItems = new List<SalaryItem>();

        try
        {
            if (copyFromPreviousMonth)
            {
                // 複製上月薪資項目
                var previousMonth = period.AddMonths(-1);
                var previousSalary = await LoadPreviousMonthSalaryAsync(employeeId, period);

                if (previousSalary != null)
                {
                    var previousItems = await _context.SalaryItems
                        .Where(si => si.SalaryRecordId == previousSalary.Id)
                        .ToListAsync();

                    foreach (var item in previousItems)
                    {
                        // 排除系統自動產生的項目（勞健保、請假扣款、所得稅）
                        if (item.ItemCode == "LABOR_INS" || 
                            item.ItemCode == "HEALTH_INS" || 
                            item.ItemCode == "LEAVE_DEDUCTION" ||
                            item.ItemCode == "INCOME_TAX")
                        {
                            continue;
                        }

                        salaryItems.Add(new SalaryItem
                        {
                            Id = Guid.NewGuid().ToString(),
                            SalaryRecordId = "", // 稍後設定
                            ItemCode = item.ItemCode,
                            ItemName = item.ItemName,
                            Type = item.Type,
                            Amount = item.Amount,
                            Description = $"複製自上月：{item.Description}"
                        });
                    }

                    _logger.LogDebug("複製上月薪資項目：員工 {EmployeeId}，項目數 {ItemCount}", employeeId, salaryItems.Count);
                }
            }

            // TODO: 這裡可以加入其他自動薪資項目的邏輯
            // 例如：固定津貼、績效獎金等

            return salaryItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "處理薪資項目時發生錯誤：員工ID {EmployeeId}，期間 {Period}", employeeId, period);
            return salaryItems;
        }
    }

    /// <summary>
    /// 載入上月薪資記錄作為參考
    /// </summary>
    public async Task<SalaryRecord?> LoadPreviousMonthSalaryAsync(string employeeId, DateTime currentPeriod)
    {
        try
        {
            var previousMonth = currentPeriod.AddMonths(-1);
            var previousMonthStart = new DateTime(previousMonth.Year, previousMonth.Month, 1);

            var previousSalary = await _context.SalaryRecords
                .Where(sr => sr.EmployeeId == employeeId && 
                           sr.Period.Year == previousMonth.Year && 
                           sr.Period.Month == previousMonth.Month)
                .OrderByDescending(sr => sr.CreatedAt)
                .FirstOrDefaultAsync();

            if (previousSalary != null)
            {
                _logger.LogDebug("載入上月薪資記錄：員工 {EmployeeId}，上月期間 {PreviousPeriod}", employeeId, previousMonth);
            }

            return previousSalary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "載入上月薪資記錄時發生錯誤：員工ID {EmployeeId}，期間 {Period}", employeeId, currentPeriod);
            return null;
        }
    }

    /// <summary>
    /// 計算勞健保扣除額
    /// </summary>
    public async Task<(decimal laborInsurance, decimal healthInsurance)> CalculateInsuranceDeductionsAsync(decimal grossSalary, DateTime period)
    {
        try
        {
            // 使用稅務計算服務計算勞健保費
            var laborInsurance = await _taxCalculationService.CalculateLaborInsuranceAsync(grossSalary, period);
            var healthInsurance = await _taxCalculationService.CalculateHealthInsuranceAsync(grossSalary, period);

            _logger.LogDebug(
                "勞健保計算：應發薪資 {GrossSalary}，勞保費 {LaborInsurance}，健保費 {HealthInsurance}",
                grossSalary, laborInsurance, healthInsurance);

            return (laborInsurance, healthInsurance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算勞健保扣除額時發生錯誤：應發薪資 {GrossSalary}，期間 {Period}", grossSalary, period);
            return (0, 0);
        }
    }

    /// <summary>
    /// 取得當月工作天數
    /// </summary>
    public async Task<decimal> GetWorkingDaysInMonthAsync(DateTime period)
    {
        try
        {
            // TODO: 這裡應該從系統參數或工作日曆取得
            // 目前使用簡化邏輯：假設每月 22 個工作天
            var defaultWorkDays = 22m;

            // 可以從系統參數表取得
            var workDaysParam = await _context.SystemParameters
                .Where(sp => sp.Key == "MONTHLY_WORK_DAYS" && 
                           sp.EffectiveDate <= period &&
                           (sp.ExpiryDate == null || sp.ExpiryDate >= period))
                .OrderByDescending(sp => sp.EffectiveDate)
                .FirstOrDefaultAsync();

            if (workDaysParam != null && decimal.TryParse(workDaysParam.Value, out var workDays))
            {
                return workDays;
            }

            return defaultWorkDays;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得當月工作天數時發生錯誤：期間 {Period}", period);
            return 22m; // 預設值
        }
    }

    /// <summary>
    /// 計算員工實際出勤天數
    /// </summary>
    public async Task<decimal> CalculateActualWorkDaysAsync(string employeeId, DateTime period)
    {
        try
        {
            // 取得當月總工作天數
            var totalWorkDays = await GetWorkingDaysInMonthAsync(period);

            // 取得當月已核准的請假記錄
            var startDate = new DateTime(period.Year, period.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var approvedLeaves = await _context.LeaveRecords
                .Where(lr => lr.EmployeeId == employeeId &&
                           lr.Status == LeaveStatus.Approved &&
                           lr.StartDate >= startDate &&
                           lr.EndDate <= endDate)
                .ToListAsync();

            // 計算請假天數
            var totalLeaveDays = approvedLeaves.Sum(x => x.Days);

            // 實際出勤天數 = 總工作天數 - 請假天數
            var actualWorkDays = Math.Max(0, totalWorkDays - totalLeaveDays);

            _logger.LogDebug(
                "出勤天數計算：員工 {EmployeeId}，總工作天數 {TotalWorkDays}，請假天數 {LeaveDays}，實際出勤 {ActualWorkDays}",
                employeeId, totalWorkDays, totalLeaveDays, actualWorkDays);

            return actualWorkDays;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "計算實際出勤天數時發生錯誤：員工ID {EmployeeId}，期間 {Period}", employeeId, period);
            // 發生錯誤時回傳總工作天數，避免薪資計算中斷
            return await GetWorkingDaysInMonthAsync(period);
        }
    }

    /// <summary>
    /// 加密 decimal 值（簡化實作，實際應使用 AES 加密）
    /// </summary>
    private byte[] EncryptDecimal(decimal value)
    {
        // TODO: 實作真正的 AES-256 加密
        // 目前使用簡化的方式：將 decimal 轉為 byte array
        var bytes = System.Text.Encoding.UTF8.GetBytes(value.ToString("F2"));
        return bytes;
    }
}