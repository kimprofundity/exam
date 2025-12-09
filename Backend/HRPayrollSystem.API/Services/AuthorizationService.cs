using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 授權服務實作
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(
        HRPayrollContext context,
        ILogger<AuthorizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 檢查使用者是否擁有指定權限
    /// </summary>
    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        try
        {
            // 取得使用者的所有角色
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            if (!userRoles.Any())
            {
                _logger.LogWarning("使用者 {UserId} 沒有任何角色", userId);
                return false;
            }

            // 取得角色的權限
            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
            var rolePermissions = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .ToListAsync();

            // 檢查是否有指定權限
            var hasPermission = rolePermissions.Any(rp => rp.Permission == permission);

            if (!hasPermission)
            {
                _logger.LogWarning(
                    "使用者 {UserId} 嘗試存取權限 {Permission} 但被拒絕",
                    userId,
                    permission
                );
            }

            return hasPermission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "檢查使用者 {UserId} 權限 {Permission} 時發生錯誤", userId, permission);
            return false;
        }
    }

    /// <summary>
    /// 取得使用者的資料存取範圍
    /// </summary>
    public async Task<DataAccessScope> GetDataAccessScopeAsync(string userId)
    {
        try
        {
            // 取得使用者的所有角色
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            if (!userRoles.Any())
            {
                _logger.LogWarning("使用者 {UserId} 沒有任何角色，預設為 Self 範圍", userId);
                return DataAccessScope.Self;
            }

            // 取得角色詳細資訊
            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
            var roles = await _context.Roles
                .Where(r => roleIds.Contains(r.Id))
                .ToListAsync();

            // 取得最大的資料存取範圍
            // Company > Department > Self
            var maxScope = DataAccessScope.Self;
            foreach (var role in roles)
            {
                // DataAccessScope 已經是枚舉類型，直接使用
                if (role.DataAccessScope > maxScope)
                {
                    maxScope = role.DataAccessScope;
                }
            }

            _logger.LogInformation("使用者 {UserId} 的資料存取範圍為 {Scope}", userId, maxScope);
            return maxScope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得使用者 {UserId} 資料存取範圍時發生錯誤", userId);
            return DataAccessScope.Self;
        }
    }

    /// <summary>
    /// 取得使用者的所有角色
    /// </summary>
    public async Task<List<Role>> GetUserRolesAsync(string userId)
    {
        try
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
            var roles = await _context.Roles
                .Where(r => roleIds.Contains(r.Id))
                .ToListAsync();

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得使用者 {UserId} 角色時發生錯誤", userId);
            return new List<Role>();
        }
    }

    /// <summary>
    /// 檢查使用者是否可以存取指定員工的資料
    /// </summary>
    public async Task<bool> CanAccessEmployeeDataAsync(string userId, string targetEmployeeId)
    {
        try
        {
            // 如果是存取自己的資料，永遠允許
            if (userId == targetEmployeeId)
            {
                return true;
            }

            // 取得使用者的資料存取範圍
            var scope = await GetDataAccessScopeAsync(userId);

            // 如果是公司範圍，可以存取所有員工
            if (scope == DataAccessScope.Company)
            {
                return true;
            }

            // 如果是部門範圍，檢查是否在同一部門
            if (scope == DataAccessScope.Department)
            {
                var userEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == userId);

                var targetEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == targetEmployeeId);

                if (userEmployee != null && targetEmployee != null)
                {
                    return userEmployee.DepartmentId == targetEmployee.DepartmentId;
                }
            }

            // Self 範圍只能存取自己
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "檢查使用者 {UserId} 是否可存取員工 {TargetEmployeeId} 資料時發生錯誤",
                userId,
                targetEmployeeId
            );
            return false;
        }
    }

    /// <summary>
    /// 取得使用者可存取的員工識別碼列表
    /// </summary>
    public async Task<List<string>> GetAccessibleEmployeeIdsAsync(string userId)
    {
        try
        {
            var scope = await GetDataAccessScopeAsync(userId);

            // 公司範圍：所有員工
            if (scope == DataAccessScope.Company)
            {
                return await _context.Employees
                    .Select(e => e.Id)
                    .ToListAsync();
            }

            // 部門範圍：同部門員工
            if (scope == DataAccessScope.Department)
            {
                var userEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == userId);

                if (userEmployee != null)
                {
                    return await _context.Employees
                        .Where(e => e.DepartmentId == userEmployee.DepartmentId)
                        .Select(e => e.Id)
                        .ToListAsync();
                }
            }

            // Self 範圍：只有自己
            return new List<string> { userId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得使用者 {UserId} 可存取的員工列表時發生錯誤", userId);
            return new List<string> { userId };
        }
    }
}
