using HRPayrollSystem.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 角色對應服務
/// 將 AD 群組對應到系統角色
/// </summary>
public class RoleMappingService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<RoleMappingService> _logger;
    private readonly Dictionary<string, string> _groupToRoleMapping;

    public RoleMappingService(
        HRPayrollContext context,
        ILogger<RoleMappingService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        
        // 從配置讀取 AD 群組到系統角色的對應
        // 格式：AD群組名稱:系統角色代碼
        _groupToRoleMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "HR-Admins", "ADMIN" },
            { "HR-Managers", "HR" },
            { "Finance-Team", "FINANCE" },
            { "Department-Managers", "MANAGER" }
        };

        // 從配置檔讀取自訂對應（如果有）
        var customMappings = configuration.GetSection("AdRoleMapping").Get<Dictionary<string, string>>();
        if (customMappings != null)
        {
            foreach (var mapping in customMappings)
            {
                _groupToRoleMapping[mapping.Key] = mapping.Value;
            }
        }
    }

    /// <summary>
    /// 根據 AD 群組同步使用者角色
    /// </summary>
    /// <param name="employeeId">員工 ID</param>
    /// <param name="adGroups">AD 群組列表</param>
    public async Task SyncUserRolesAsync(string employeeId, List<string> adGroups)
    {
        try
        {
            // 取得應該擁有的角色
            var targetRoleCodes = adGroups
                .Where(g => _groupToRoleMapping.ContainsKey(g))
                .Select(g => _groupToRoleMapping[g])
                .Distinct()
                .ToList();

            // 如果沒有對應的角色，至少給予一般員工角色
            if (!targetRoleCodes.Any())
            {
                targetRoleCodes.Add("EMPLOYEE");
            }

            // 取得目標角色的 ID
            var targetRoles = await _context.Roles
                .Where(r => targetRoleCodes.Contains(r.Code))
                .ToListAsync();

            // 取得使用者目前的角色
            var currentUserRoles = await _context.UserRoles
                .Where(ur => ur.UserId == employeeId)
                .ToListAsync();

            var currentRoleIds = currentUserRoles.Select(ur => ur.RoleId).ToHashSet();
            var targetRoleIds = targetRoles.Select(r => r.Id).ToHashSet();

            // 移除不再擁有的角色
            var rolesToRemove = currentUserRoles
                .Where(ur => !targetRoleIds.Contains(ur.RoleId))
                .ToList();

            if (rolesToRemove.Any())
            {
                _context.UserRoles.RemoveRange(rolesToRemove);
                _logger.LogInformation(
                    "移除員工 {EmployeeId} 的 {Count} 個角色",
                    employeeId,
                    rolesToRemove.Count
                );
            }

            // 新增新的角色
            var rolesToAdd = targetRoles
                .Where(r => !currentRoleIds.Contains(r.Id))
                .Select(r => new Models.UserRole
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = employeeId,
                    RoleId = r.Id,
                    EffectiveDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (rolesToAdd.Any())
            {
                _context.UserRoles.AddRange(rolesToAdd);
                _logger.LogInformation(
                    "為員工 {EmployeeId} 新增 {Count} 個角色",
                    employeeId,
                    rolesToAdd.Count
                );
            }

            if (rolesToRemove.Any() || rolesToAdd.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("員工 {EmployeeId} 的角色同步完成", employeeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步員工 {EmployeeId} 的角色時發生錯誤", employeeId);
            throw;
        }
    }

    /// <summary>
    /// 取得 AD 群組對應的系統角色
    /// </summary>
    /// <param name="adGroups">AD 群組列表</param>
    /// <returns>系統角色代碼列表</returns>
    public List<string> MapAdGroupsToRoles(List<string> adGroups)
    {
        var roles = adGroups
            .Where(g => _groupToRoleMapping.ContainsKey(g))
            .Select(g => _groupToRoleMapping[g])
            .Distinct()
            .ToList();

        // 如果沒有對應的角色，至少給予一般員工角色
        if (!roles.Any())
        {
            roles.Add("EMPLOYEE");
        }

        return roles;
    }

    /// <summary>
    /// 取得所有角色對應規則
    /// </summary>
    public Dictionary<string, string> GetAllMappings()
    {
        return new Dictionary<string, string>(_groupToRoleMapping);
    }
}
