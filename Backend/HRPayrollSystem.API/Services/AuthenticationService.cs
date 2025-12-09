using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HRPayrollSystem.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 身份驗證服務實作
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILdapService _ldapService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HRPayrollContext _context;
    private readonly RoleMappingService _roleMappingService;
    private readonly string _jwtSecretKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpirationMinutes;

    public AuthenticationService(
        ILdapService ldapService,
        ILogger<AuthenticationService> logger,
        IConfiguration configuration,
        HRPayrollContext context,
        RoleMappingService roleMappingService)
    {
        _ldapService = ldapService;
        _logger = logger;
        _configuration = configuration;
        _context = context;
        _roleMappingService = roleMappingService;
        
        // 讀取 JWT 設定
        _jwtSecretKey = _configuration["JwtSettings:SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey 未設定");
        _jwtIssuer = _configuration["JwtSettings:Issuer"] ?? "HRPayrollSystem";
        _jwtAudience = _configuration["JwtSettings:Audience"] ?? "HRPayrollSystem";
        _jwtExpirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "480");
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password)
    {
        try
        {
            // 1. 透過 LDAP 驗證使用者憑證
            var isValid = await _ldapService.ValidateCredentialsAsync(username, password);
            
            if (!isValid)
            {
                _logger.LogWarning("使用者 {Username} 登入失敗：憑證無效", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "使用者名稱或密碼錯誤"
                };
            }

            // 2. 從 LDAP 取得使用者詳細資訊
            var ldapUser = await _ldapService.GetUserDetailsAsync(username);
            
            if (ldapUser == null)
            {
                _logger.LogWarning("使用者 {Username} 登入失敗：無法取得使用者資訊", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "無法取得使用者資訊"
                };
            }

            // 3. 檢查使用者是否被停用
            if (!ldapUser.IsActive)
            {
                _logger.LogWarning("使用者 {Username} 登入失敗：帳號已停用", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "帳號已停用"
                };
            }

            // 4. 檢查或建立員工記錄（首次登入自動建立）
            var employee = await EnsureEmployeeExistsAsync(ldapUser);
            
            if (employee == null)
            {
                _logger.LogError("無法為使用者 {Username} 建立或取得員工記錄", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "無法建立使用者記錄"
                };
            }

            // 5. 取得使用者角色和權限
            var userInfo = await GetUserInfoAsync(username);
            
            if (userInfo == null)
            {
                _logger.LogError("無法取得使用者 {Username} 的角色和權限", username);
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "無法取得使用者權限"
                };
            }

            // 6. 產生 JWT 令牌
            var token = GenerateJwtToken(userInfo);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

            _logger.LogInformation("使用者 {Username} 登入成功", username);

            return new AuthenticationResult
            {
                Success = true,
                Token = token,
                ExpiresAt = expiresAt,
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "使用者 {Username} 登入時發生錯誤", username);
            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "登入時發生錯誤，請稍後再試"
            };
        }
    }

    /// <summary>
    /// 驗證令牌
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            await Task.Run(() => tokenHandler.ValidateToken(token, validationParameters, out _));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "令牌驗證失敗");
            return false;
        }
    }

    /// <summary>
    /// 取得使用者資訊
    /// </summary>
    public async Task<UserInfo?> GetUserInfoAsync(string username)
    {
        try
        {
            // 從資料庫取得員工資訊
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == username);

            if (employee == null)
            {
                _logger.LogWarning("找不到員工記錄：{Username}", username);
                return null;
            }

            // 取得部門資訊
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == employee.DepartmentId);

            // 取得使用者角色
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == employee.Id)
                .ToListAsync();

            // 取得角色詳細資訊
            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
            var roles = await _context.Roles
                .Where(r => roleIds.Contains(r.Id))
                .ToListAsync();

            // 取得權限
            var rolePermissions = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .ToListAsync();

            var roleNames = roles.Select(r => r.Name).ToList();
            var permissions = rolePermissions
                .Select(p => p.Permission)
                .Distinct()
                .ToList();

            // 從 LDAP 取得最新資訊
            var ldapUser = await _ldapService.GetUserDetailsAsync(username);

            return new UserInfo
            {
                UserId = employee.Id,
                Username = employee.EmployeeNumber,
                DisplayName = ldapUser?.DisplayName ?? employee.Name,
                Email = ldapUser?.Email ?? string.Empty,
                Department = department?.Name ?? string.Empty,
                Roles = roleNames,
                Permissions = permissions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得使用者 {Username} 資訊時發生錯誤", username);
            return null;
        }
    }

    /// <summary>
    /// 確保員工記錄存在（首次登入自動建立）
    /// </summary>
    private async Task<Models.Employee?> EnsureEmployeeExistsAsync(LdapUser ldapUser)
    {
        try
        {
            // 檢查員工是否已存在
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeNumber == ldapUser.Username);

            if (employee != null)
            {
                // 更新員工資訊（同步 AD 資料）
                employee.Name = ldapUser.DisplayName;
                employee.Status = ldapUser.IsActive ? Models.EmployeeStatus.Active : Models.EmployeeStatus.Resigned;
                employee.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("已更新員工 {Username} 的資訊", ldapUser.Username);
                
                return employee;
            }

            // 首次登入：建立新員工記錄
            _logger.LogInformation("首次登入：為使用者 {Username} 建立員工記錄", ldapUser.Username);

            // 取得或建立預設部門
            var defaultDepartment = await GetOrCreateDefaultDepartmentAsync(ldapUser.Department);

            employee = new Models.Employee
            {
                Id = Guid.NewGuid().ToString(),
                EmployeeNumber = ldapUser.Username,
                Name = ldapUser.DisplayName,
                DepartmentId = defaultDepartment.Id,
                Position = ldapUser.Title,
                MonthlySalary = 0, // 預設為 0，需要 HR 後續設定
                Status = ldapUser.IsActive ? Models.EmployeeStatus.Active : Models.EmployeeStatus.Resigned,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // 根據 AD 群組同步角色
            await _roleMappingService.SyncUserRolesAsync(employee.Id, ldapUser.Groups);

            _logger.LogInformation("成功為使用者 {Username} 建立員工記錄並同步角色", ldapUser.Username);
            return employee;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "確保員工記錄存在時發生錯誤：{Username}", ldapUser.Username);
            return null;
        }
    }

    /// <summary>
    /// 取得或建立預設部門
    /// </summary>
    private async Task<Models.Department> GetOrCreateDefaultDepartmentAsync(string departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
        {
            departmentName = "未分類";
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Name == departmentName);

        if (department == null)
        {
            department = new Models.Department
            {
                Id = Guid.NewGuid().ToString(),
                Code = $"DEPT{DateTime.UtcNow.Ticks}",
                Name = departmentName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
        }

        return department;
    }

    /// <summary>
    /// 指派預設角色（一般員工）
    /// </summary>
    private async Task AssignDefaultRoleAsync(string employeeId)
    {
        var employeeRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Code == "EMPLOYEE");

        if (employeeRole != null)
        {
            var userRole = new Models.UserRole
            {
                Id = Guid.NewGuid().ToString(),
                UserId = employeeId,
                RoleId = employeeRole.Id,
                EffectiveDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 產生 JWT 令牌
    /// </summary>
    private string GenerateJwtToken(UserInfo userInfo)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(ClaimTypes.Name, userInfo.Username),
            new Claim(ClaimTypes.Email, userInfo.Email),
            new Claim("DisplayName", userInfo.DisplayName),
            new Claim("Department", userInfo.Department)
        };

        // 加入角色
        foreach (var role in userInfo.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // 加入權限
        foreach (var permission in userInfo.Permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
