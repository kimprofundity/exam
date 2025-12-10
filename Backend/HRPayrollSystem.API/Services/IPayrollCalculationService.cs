using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 薪資計算服務介面
/// </summary>
public interface IPayrollCalculationService
{
    /// <summary>
    /// 計算員工薪資
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="period">薪資期間（年月）</param>
    /// <param name="copyFromPreviousMonth">是否複製上月資料</param>
    /// <returns>薪資記錄</returns>
    Task<SalaryRecord> CalculateSalaryAsync(string employeeId, DateTime period, bool copyFromPreviousMonth = false);

    /// <summary>
    /// 計算基本薪資（月薪 × 出勤比例）
    /// </summary>
    /// <param name="employee">員工資料</param>
    /// <param name="period">薪資期間</param>
    /// <param name="workDays">實際工作天數</param>
    /// <param name="totalWorkDays">當月總工作天數</param>
    /// <returns>基本薪資金額</returns>
    Task<decimal> CalculateBaseSalaryAsync(Employee employee, DateTime period, decimal workDays, decimal totalWorkDays);

    /// <summary>
    /// 計算請假扣款
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="period">薪資期間</param>
    /// <returns>請假扣款金額</returns>
    Task<decimal> CalculateLeaveDeductionAsync(string employeeId, DateTime period);

    /// <summary>
    /// 計算加班費
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="period">薪資期間</param>
    /// <param name="overtimeHours">加班小時數</param>
    /// <returns>加班費金額</returns>
    Task<decimal> CalculateOvertimePayAsync(string employeeId, DateTime period, decimal overtimeHours);

    /// <summary>
    /// 處理薪資加項和減項
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="period">薪資期間</param>
    /// <param name="copyFromPreviousMonth">是否複製上月資料</param>
    /// <returns>薪資項目列表</returns>
    Task<List<SalaryItem>> ProcessSalaryItemsAsync(string employeeId, DateTime period, bool copyFromPreviousMonth);

    /// <summary>
    /// 載入上月薪資記錄作為參考
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="currentPeriod">當前薪資期間</param>
    /// <returns>上月薪資記錄</returns>
    Task<SalaryRecord?> LoadPreviousMonthSalaryAsync(string employeeId, DateTime currentPeriod);

    /// <summary>
    /// 計算勞健保扣除額
    /// </summary>
    /// <param name="grossSalary">應發薪資</param>
    /// <param name="period">薪資期間</param>
    /// <returns>勞健保扣除額</returns>
    Task<(decimal laborInsurance, decimal healthInsurance)> CalculateInsuranceDeductionsAsync(decimal grossSalary, DateTime period);

    /// <summary>
    /// 取得當月工作天數
    /// </summary>
    /// <param name="period">薪資期間</param>
    /// <returns>工作天數</returns>
    Task<decimal> GetWorkingDaysInMonthAsync(DateTime period);

    /// <summary>
    /// 計算員工實際出勤天數
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="period">薪資期間</param>
    /// <returns>實際出勤天數</returns>
    Task<decimal> CalculateActualWorkDaysAsync(string employeeId, DateTime period);
}