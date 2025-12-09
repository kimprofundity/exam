using HRPayrollSystem.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// AD 使用者資訊同步背景服務
/// 定期同步 AD 使用者資訊到系統
/// </summary>
public class AdSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdSyncBackgroundService> _logger;
    private readonly TimeSpan _syncInterval;

    public AdSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AdSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // 從配置讀取同步間隔（預設 1 小時）
        var intervalMinutes = int.Parse(configuration["AdSync:IntervalMinutes"] ?? "60");
        _syncInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AD 同步背景服務已啟動，同步間隔：{Interval} 分鐘", _syncInterval.TotalMinutes);

        // 延遲啟動，等待應用程式完全啟動
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAdUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AD 同步過程中發生錯誤");
            }

            // 等待下一次同步
            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // 正常關閉
                break;
            }
        }

        _logger.LogInformation("AD 同步背景服務已停止");
    }

    /// <summary>
    /// 同步 AD 使用者資訊
    /// </summary>
    private async Task SyncAdUsersAsync()
    {
        _logger.LogInformation("開始同步 AD 使用者資訊");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HRPayrollContext>();
            var ldapService = scope.ServiceProvider.GetRequiredService<ILdapService>();

            // 取得所有在職員工
            var employees = await context.Employees
                .Where(e => e.Status == Models.EmployeeStatus.Active)
                .ToListAsync();

            var syncedCount = 0;
            var errorCount = 0;

            foreach (var employee in employees)
            {
                try
                {
                    // 從 LDAP 取得最新資訊
                    var ldapUser = await ldapService.GetUserDetailsAsync(employee.EmployeeNumber);

                    if (ldapUser == null)
                    {
                        _logger.LogWarning("無法從 AD 取得員工 {EmployeeNumber} 的資訊", employee.EmployeeNumber);
                        errorCount++;
                        continue;
                    }

                    // 更新員工資訊
                    var hasChanges = false;

                    if (employee.Name != ldapUser.DisplayName)
                    {
                        employee.Name = ldapUser.DisplayName;
                        hasChanges = true;
                    }

                    // 檢查 AD 使用者是否被停用
                    if (!ldapUser.IsActive && employee.Status == Models.EmployeeStatus.Active)
                    {
                        employee.Status = Models.EmployeeStatus.Resigned;
                        employee.ResignationDate = DateTime.UtcNow;
                        hasChanges = true;
                        _logger.LogWarning("員工 {EmployeeNumber} 在 AD 中已被停用，已更新系統狀態", employee.EmployeeNumber);
                    }

                    if (hasChanges)
                    {
                        employee.UpdatedAt = DateTime.UtcNow;
                        syncedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "同步員工 {EmployeeNumber} 時發生錯誤", employee.EmployeeNumber);
                    errorCount++;
                }
            }

            if (syncedCount > 0)
            {
                await context.SaveChangesAsync();
            }

            _logger.LogInformation(
                "AD 同步完成：共 {Total} 位員工，更新 {Synced} 位，錯誤 {Errors} 位",
                employees.Count,
                syncedCount,
                errorCount
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AD 同步失敗");
            // 不要拋出異常，讓背景服務繼續運行
        }
    }
}
