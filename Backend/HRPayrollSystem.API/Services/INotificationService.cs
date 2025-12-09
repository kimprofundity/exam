namespace HRPayrollSystem.API.Services;

/// <summary>
/// 通知服務介面
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 發送代理請假通知給被代理員工
    /// </summary>
    /// <param name="employeeId">被代理員工識別碼</param>
    /// <param name="proxyUserId">代理人識別碼</param>
    /// <param name="leaveId">請假記錄識別碼</param>
    /// <param name="leaveType">請假類型</param>
    /// <param name="startDate">開始日期</param>
    /// <param name="endDate">結束日期</param>
    /// <param name="days">請假天數</param>
    /// <returns>是否成功發送</returns>
    Task<bool> SendProxyLeaveNotificationAsync(
        string employeeId,
        string proxyUserId,
        string leaveId,
        string leaveType,
        DateTime startDate,
        DateTime endDate,
        decimal days);

    /// <summary>
    /// 發送代理請假確認通知給代理人
    /// </summary>
    /// <param name="proxyUserId">代理人識別碼</param>
    /// <param name="employeeId">被代理員工識別碼</param>
    /// <param name="leaveId">請假記錄識別碼</param>
    /// <param name="isApproved">是否核准</param>
    /// <returns>是否成功發送</returns>
    Task<bool> SendProxyLeaveConfirmationNotificationAsync(
        string proxyUserId,
        string employeeId,
        string leaveId,
        bool isApproved);

    /// <summary>
    /// 發送薪資通知給員工
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="salaryPeriod">薪資期間</param>
    /// <param name="totalAmount">總金額</param>
    /// <param name="paymentDate">發放日期</param>
    /// <returns>是否成功發送</returns>
    Task<bool> SendSalaryNotificationAsync(
        string employeeId,
        string salaryPeriod,
        decimal totalAmount,
        DateTime paymentDate);
}
