using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 請假管理服務介面
/// </summary>
public interface ILeaveService
{
    /// <summary>
    /// 建立請假申請
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="type">請假類型</param>
    /// <param name="startDate">開始日期</param>
    /// <param name="endDate">結束日期</param>
    /// <param name="days">請假天數</param>
    /// <param name="proxyUserId">代理人識別碼（可選）</param>
    /// <returns>請假記錄識別碼</returns>
    Task<string> CreateLeaveRequestAsync(
        string employeeId,
        LeaveType type,
        DateTime startDate,
        DateTime endDate,
        decimal days,
        string? proxyUserId = null);

    /// <summary>
    /// 確認代理請假
    /// </summary>
    /// <param name="leaveId">請假記錄識別碼</param>
    /// <returns>是否成功</returns>
    Task<bool> ConfirmProxyLeaveAsync(string leaveId);

    /// <summary>
    /// 拒絕代理請假
    /// </summary>
    /// <param name="leaveId">請假記錄識別碼</param>
    /// <returns>是否成功</returns>
    Task<bool> RejectProxyLeaveAsync(string leaveId);

    /// <summary>
    /// 取得員工請假記錄
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="startDate">開始日期（可選）</param>
    /// <param name="endDate">結束日期（可選）</param>
    /// <returns>請假記錄列表</returns>
    Task<List<LeaveRecord>> GetEmployeeLeaveRecordsAsync(
        string employeeId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// 取得請假記錄詳情
    /// </summary>
    /// <param name="leaveId">請假記錄識別碼</param>
    /// <returns>請假記錄</returns>
    Task<LeaveRecord?> GetLeaveRecordAsync(string leaveId);

    /// <summary>
    /// 計算員工剩餘假期額度
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="leaveType">請假類型</param>
    /// <returns>剩餘天數</returns>
    Task<decimal> GetRemainingLeaveBalanceAsync(string employeeId, LeaveType leaveType);

    /// <summary>
    /// 驗證請假日期是否重疊
    /// </summary>
    /// <param name="employeeId">員工識別碼</param>
    /// <param name="startDate">開始日期</param>
    /// <param name="endDate">結束日期</param>
    /// <param name="excludeLeaveId">排除的請假記錄識別碼（用於更新時）</param>
    /// <returns>是否重疊</returns>
    Task<bool> HasOverlappingLeaveAsync(
        string employeeId,
        DateTime startDate,
        DateTime endDate,
        string? excludeLeaveId = null);
}
