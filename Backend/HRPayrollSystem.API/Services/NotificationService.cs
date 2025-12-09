using HRPayrollSystem.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 通知服務實作
/// 目前使用日誌記錄模擬通知發送，未來可整合實際的郵件服務
/// </summary>
public class NotificationService : INotificationService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        HRPayrollContext context,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 發送代理請假通知給被代理員工
    /// </summary>
    public async Task<bool> SendProxyLeaveNotificationAsync(
        string employeeId,
        string proxyUserId,
        string leaveId,
        string leaveType,
        DateTime startDate,
        DateTime endDate,
        decimal days)
    {
        try
        {
            // 取得員工和代理人資訊
            var employee = await _context.Employees.FindAsync(employeeId);
            var proxyUser = await _context.Employees.FindAsync(proxyUserId);

            if (employee == null || proxyUser == null)
            {
                _logger.LogWarning("無法發送通知：找不到員工或代理人資訊");
                return false;
            }

            // 記錄通知（模擬發送）
            _logger.LogInformation(
                "【代理請假通知】\n" +
                "收件人：{EmployeeName} ({EmployeeNumber})\n" +
                "代理人：{ProxyUserName} ({ProxyUserNumber})\n" +
                "請假類型：{LeaveType}\n" +
                "請假期間：{StartDate:yyyy-MM-dd} 至 {EndDate:yyyy-MM-dd}\n" +
                "請假天數：{Days} 天\n" +
                "請假記錄ID：{LeaveId}\n" +
                "請登入系統確認或拒絕此請假申請。",
                employee.Name, employee.EmployeeNumber,
                proxyUser.Name, proxyUser.EmployeeNumber,
                leaveType,
                startDate, endDate,
                days,
                leaveId);

            // TODO: 整合實際的郵件服務
            // await _emailService.SendEmailAsync(employee.Email, subject, body);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發送代理請假通知時發生錯誤");
            return false;
        }
    }

    /// <summary>
    /// 發送代理請假確認通知給代理人
    /// </summary>
    public async Task<bool> SendProxyLeaveConfirmationNotificationAsync(
        string proxyUserId,
        string employeeId,
        string leaveId,
        bool isApproved)
    {
        try
        {
            // 取得員工和代理人資訊
            var employee = await _context.Employees.FindAsync(employeeId);
            var proxyUser = await _context.Employees.FindAsync(proxyUserId);

            if (employee == null || proxyUser == null)
            {
                _logger.LogWarning("無法發送通知：找不到員工或代理人資訊");
                return false;
            }

            var status = isApproved ? "已確認" : "已拒絕";

            // 記錄通知（模擬發送）
            _logger.LogInformation(
                "【代理請假確認通知】\n" +
                "收件人：{ProxyUserName} ({ProxyUserNumber})\n" +
                "員工：{EmployeeName} ({EmployeeNumber})\n" +
                "請假記錄ID：{LeaveId}\n" +
                "處理結果：{Status}",
                proxyUser.Name, proxyUser.EmployeeNumber,
                employee.Name, employee.EmployeeNumber,
                leaveId,
                status);

            // TODO: 整合實際的郵件服務
            // await _emailService.SendEmailAsync(proxyUser.Email, subject, body);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發送代理請假確認通知時發生錯誤");
            return false;
        }
    }

    /// <summary>
    /// 發送薪資通知給員工
    /// </summary>
    public async Task<bool> SendSalaryNotificationAsync(
        string employeeId,
        string salaryPeriod,
        decimal totalAmount,
        DateTime paymentDate)
    {
        try
        {
            // 取得員工資訊
            var employee = await _context.Employees.FindAsync(employeeId);

            if (employee == null)
            {
                _logger.LogWarning("無法發送通知：找不到員工資訊");
                return false;
            }

            // 記錄通知（模擬發送）
            _logger.LogInformation(
                "【薪資通知】\n" +
                "收件人：{EmployeeName} ({EmployeeNumber})\n" +
                "薪資期間：{SalaryPeriod}\n" +
                "總金額：{TotalAmount:N0} 元\n" +
                "發放日期：{PaymentDate:yyyy-MM-dd}",
                employee.Name, employee.EmployeeNumber,
                salaryPeriod,
                totalAmount,
                paymentDate);

            // TODO: 整合實際的郵件服務
            // await _emailService.SendEmailAsync(employee.Email, subject, body);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "發送薪資通知時發生錯誤");
            return false;
        }
    }
}
