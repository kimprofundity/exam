using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 請假管理服務實作
/// </summary>
public class LeaveService : ILeaveService
{
    private readonly HRPayrollContext _context;
    private readonly INotificationService _notificationService;

    public LeaveService(
        HRPayrollContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    /// <summary>
    /// 建立請假申請
    /// </summary>
    public async Task<string> CreateLeaveRequestAsync(
        string employeeId,
        LeaveType type,
        DateTime startDate,
        DateTime endDate,
        decimal days,
        string? proxyUserId = null)
    {
        // 驗證員工是否存在
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            throw new InvalidOperationException("找不到員工資料");
        }

        // 驗證員工狀態
        if (employee.Status != EmployeeStatus.Active)
        {
            throw new InvalidOperationException("員工狀態不是在職，無法申請請假");
        }

        // 驗證日期
        if (startDate > endDate)
        {
            throw new ArgumentException("開始日期不能晚於結束日期");
        }

        if (days <= 0)
        {
            throw new ArgumentException("請假天數必須大於 0");
        }

        // 驗證請假日期是否重疊
        var hasOverlap = await HasOverlappingLeaveAsync(employeeId, startDate, endDate);
        if (hasOverlap)
        {
            throw new InvalidOperationException("請假日期與現有請假記錄重疊");
        }

        // 驗證代理人
        bool isProxyRequest = !string.IsNullOrEmpty(proxyUserId);
        if (isProxyRequest)
        {
            var proxyUser = await _context.Employees.FindAsync(proxyUserId);
            if (proxyUser == null)
            {
                throw new InvalidOperationException("找不到代理人資料");
            }
        }

        // 建立請假記錄
        var leaveRecord = new LeaveRecord
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = employeeId,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            Days = days,
            Status = isProxyRequest ? LeaveStatus.PendingConfirmation : LeaveStatus.Approved,
            ProxyUserId = proxyUserId,
            IsProxyRequest = isProxyRequest,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRecords.Add(leaveRecord);
        await _context.SaveChangesAsync();

        // 如果是代理請假，發送通知給被代理員工
        if (isProxyRequest && !string.IsNullOrEmpty(proxyUserId))
        {
            await _notificationService.SendProxyLeaveNotificationAsync(
                employeeId,
                proxyUserId,
                leaveRecord.Id,
                type.ToString(),
                startDate,
                endDate,
                days);
        }

        return leaveRecord.Id;
    }

    /// <summary>
    /// 確認代理請假
    /// </summary>
    public async Task<bool> ConfirmProxyLeaveAsync(string leaveId)
    {
        var leaveRecord = await _context.LeaveRecords.FindAsync(leaveId);
        if (leaveRecord == null)
        {
            throw new InvalidOperationException("找不到請假記錄");
        }

        if (!leaveRecord.IsProxyRequest)
        {
            throw new InvalidOperationException("此請假記錄不是代理請假");
        }

        if (leaveRecord.Status != LeaveStatus.PendingConfirmation)
        {
            throw new InvalidOperationException("此請假記錄狀態不是待確認");
        }

        leaveRecord.Status = LeaveStatus.Approved;
        await _context.SaveChangesAsync();

        // 發送確認通知給代理人
        if (!string.IsNullOrEmpty(leaveRecord.ProxyUserId))
        {
            await _notificationService.SendProxyLeaveConfirmationNotificationAsync(
                leaveRecord.ProxyUserId,
                leaveRecord.EmployeeId,
                leaveId,
                true);
        }

        return true;
    }

    /// <summary>
    /// 拒絕代理請假
    /// </summary>
    public async Task<bool> RejectProxyLeaveAsync(string leaveId)
    {
        var leaveRecord = await _context.LeaveRecords.FindAsync(leaveId);
        if (leaveRecord == null)
        {
            throw new InvalidOperationException("找不到請假記錄");
        }

        if (!leaveRecord.IsProxyRequest)
        {
            throw new InvalidOperationException("此請假記錄不是代理請假");
        }

        if (leaveRecord.Status != LeaveStatus.PendingConfirmation)
        {
            throw new InvalidOperationException("此請假記錄狀態不是待確認");
        }

        leaveRecord.Status = LeaveStatus.Rejected;
        await _context.SaveChangesAsync();

        // 發送拒絕通知給代理人
        if (!string.IsNullOrEmpty(leaveRecord.ProxyUserId))
        {
            await _notificationService.SendProxyLeaveConfirmationNotificationAsync(
                leaveRecord.ProxyUserId,
                leaveRecord.EmployeeId,
                leaveId,
                false);
        }

        return true;
    }

    /// <summary>
    /// 取得員工請假記錄
    /// </summary>
    public async Task<List<LeaveRecord>> GetEmployeeLeaveRecordsAsync(
        string employeeId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.LeaveRecords
            .Where(lr => lr.EmployeeId == employeeId);

        if (startDate.HasValue)
        {
            query = query.Where(lr => lr.EndDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(lr => lr.StartDate <= endDate.Value);
        }

        return await query
            .OrderByDescending(lr => lr.StartDate)
            .ToListAsync();
    }

    /// <summary>
    /// 取得請假記錄詳情
    /// </summary>
    public async Task<LeaveRecord?> GetLeaveRecordAsync(string leaveId)
    {
        return await _context.LeaveRecords.FindAsync(leaveId);
    }

    /// <summary>
    /// 計算員工剩餘假期額度
    /// </summary>
    public async Task<decimal> GetRemainingLeaveBalanceAsync(string employeeId, LeaveType leaveType)
    {
        // 取得員工資料
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            throw new InvalidOperationException("找不到員工資料");
        }

        // 取得當年度已核准的請假記錄
        var currentYear = DateTime.UtcNow.Year;
        var approvedLeaves = await _context.LeaveRecords
            .Where(lr => lr.EmployeeId == employeeId
                && lr.Type == leaveType
                && lr.Status == LeaveStatus.Approved
                && lr.StartDate.Year == currentYear)
            .ToListAsync();

        var usedDays = approvedLeaves.Sum(lr => lr.Days);

        // 根據請假類型計算額度
        decimal totalAllowance = leaveType switch
        {
            LeaveType.Annual => 14, // 預設年假 14 天
            LeaveType.Sick => 30,   // 預設病假 30 天
            LeaveType.Personal => decimal.MaxValue, // 事假無限制
            _ => 0
        };

        return Math.Max(0, totalAllowance - usedDays);
    }

    /// <summary>
    /// 驗證請假日期是否重疊
    /// </summary>
    public async Task<bool> HasOverlappingLeaveAsync(
        string employeeId,
        DateTime startDate,
        DateTime endDate,
        string? excludeLeaveId = null)
    {
        var query = _context.LeaveRecords
            .Where(lr => lr.EmployeeId == employeeId
                && lr.Status != LeaveStatus.Rejected
                && (
                    // 新請假的開始日期在現有請假期間內
                    (lr.StartDate <= startDate && startDate <= lr.EndDate)
                    // 新請假的結束日期在現有請假期間內
                    || (lr.StartDate <= endDate && endDate <= lr.EndDate)
                    // 新請假完全包含現有請假
                    || (startDate <= lr.StartDate && lr.EndDate <= endDate)
                ));

        if (!string.IsNullOrEmpty(excludeLeaveId))
        {
            query = query.Where(lr => lr.Id != excludeLeaveId);
        }

        return await query.AnyAsync();
    }
}
