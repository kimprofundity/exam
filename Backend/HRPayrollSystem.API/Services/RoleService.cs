using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 角色服務實作
/// </summary>
public class RoleService : IRoleService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        HRPayrollContext context,
        ILogger<RoleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 建立新角色
    /// </summary>
    public async Task<Role> CreateRoleAsync(RoleDto roleDto)
    {
        try
        {
            // 檢查角色代碼是否已存在
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Code == roleDto.Code);

            if (existingRole != null)
            {
                throw new InvalidOperationException($"角色代碼 {roleDto.Code} 已存在");
            }

            var role = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Code = roleDto.Code,
                Name = roleDto.Name,
                Description = roleDto.Description ?? string.Empty,
                DataAccessScope = ParseDataAccessScope(roleDto.DataAccessScope),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // 新增權限
            if (roleDto.Permissions.Any())
            {
                await UpdateRolePermissionsAsync(role.Id, roleDto.Permissions);
            }

            _logger.LogInformation("成功建立角色 {RoleCode}", roleDto.Code);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立角色 {RoleCode} 時發生錯誤", roleDto.Code);
            throw;
        }
    }

    /// <summary>
    /// 更新角色
    /// </summary>
    public async Task<Role> UpdateRoleAsync(string roleId, RoleDto roleDto)
    {
        try
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                throw new InvalidOperationException($"找不到角色 {roleId}");
            }

            role.Name = roleDto.Name;
            role.Description = roleDto.Description ?? string.Empty;
            role.DataAccessScope = ParseDataAccessScope(roleDto.DataAccessScope);
            role.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 更新權限
            await UpdateRolePermissionsAsync(roleId, roleDto.Permissions);

            _logger.LogInformation("成功更新角色 {RoleId}", roleId);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新角色 {RoleId} 時發生錯誤", roleId);
            throw;
        }
    }

    /// <summary>
    /// 取得角色
    /// </summary>
    public async Task<Role?> GetRoleAsync(string roleId)
    {
        try
        {
            return await _context.Roles.FindAsync(roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得角色 {RoleId} 時發生錯誤", roleId);
            return null;
        }
    }

    /// <summary>
    /// 取得所有角色
    /// </summary>
    public async Task<List<Role>> GetAllRolesAsync()
    {
        try
        {
            return await _context.Roles.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得所有角色時發生錯誤");
            return new List<Role>();
        }
    }

    /// <summary>
    /// 刪除角色
    /// </summary>
    public async Task DeleteRoleAsync(string roleId)
    {
        try
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                throw new InvalidOperationException($"找不到角色 {roleId}");
            }

            // 檢查是否有使用者使用此角色
            var hasUsers = await _context.UserRoles
                .AnyAsync(ur => ur.RoleId == roleId);

            if (hasUsers)
            {
                throw new InvalidOperationException($"角色 {roleId} 仍有使用者使用，無法刪除");
            }

            // 刪除角色權限
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(permissions);

            // 刪除角色
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("成功刪除角色 {RoleId}", roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除角色 {RoleId} 時發生錯誤", roleId);
            throw;
        }
    }

    /// <summary>
    /// 指派角色給使用者
    /// </summary>
    public async Task AssignRoleToUserAsync(string userId, string roleId)
    {
        try
        {
            // 檢查角色是否存在
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                throw new InvalidOperationException($"找不到角色 {roleId}");
            }

            // 檢查使用者是否已有此角色
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existingUserRole != null)
            {
                _logger.LogWarning("使用者 {UserId} 已擁有角色 {RoleId}", userId, roleId);
                return;
            }

            var userRole = new UserRole
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                RoleId = roleId,
                EffectiveDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("成功指派角色 {RoleId} 給使用者 {UserId}", roleId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "指派角色 {RoleId} 給使用者 {UserId} 時發生錯誤", roleId, userId);
            throw;
        }
    }

    /// <summary>
    /// 移除使用者的角色
    /// </summary>
    public async Task RemoveRoleFromUserAsync(string userId, string roleId)
    {
        try
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
            {
                _logger.LogWarning("找不到使用者 {UserId} 的角色 {RoleId}", userId, roleId);
                return;
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            _logger.LogInformation("成功移除使用者 {UserId} 的角色 {RoleId}", userId, roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除使用者 {UserId} 的角色 {RoleId} 時發生錯誤", userId, roleId);
            throw;
        }
    }

    /// <summary>
    /// 更新角色權限
    /// </summary>
    public async Task UpdateRolePermissionsAsync(string roleId, List<string> permissions)
    {
        try
        {
            // 刪除現有權限
            var existingPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(existingPermissions);

            // 新增新權限
            var newPermissions = permissions.Select(p => new RolePermission
            {
                Id = Guid.NewGuid().ToString(),
                RoleId = roleId,
                Permission = p,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.RolePermissions.AddRange(newPermissions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("成功更新角色 {RoleId} 的權限", roleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新角色 {RoleId} 權限時發生錯誤", roleId);
            throw;
        }
    }

    /// <summary>
    /// 解析資料存取範圍字串
    /// </summary>
    private DataAccessScope ParseDataAccessScope(string scopeString)
    {
        return scopeString?.ToLower() switch
        {
            "company" => DataAccessScope.Company,
            "department" => DataAccessScope.Department,
            "self" => DataAccessScope.Self,
            _ => DataAccessScope.Self
        };
    }
}
