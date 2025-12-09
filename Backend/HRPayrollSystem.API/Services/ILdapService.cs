using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// LDAP 服務介面
/// </summary>
public interface ILdapService
{
    /// <summary>
    /// 驗證使用者憑證
    /// </summary>
    /// <param name="username">使用者名稱</param>
    /// <param name="password">密碼</param>
    /// <returns>驗證是否成功</returns>
    Task<bool> ValidateCredentialsAsync(string username, string password);

    /// <summary>
    /// 取得使用者詳細資訊
    /// </summary>
    /// <param name="username">使用者名稱</param>
    /// <returns>LDAP 使用者資訊</returns>
    Task<LdapUser?> GetUserDetailsAsync(string username);

    /// <summary>
    /// 取得使用者所屬群組
    /// </summary>
    /// <param name="username">使用者名稱</param>
    /// <returns>群組列表</returns>
    Task<List<string>> GetUserGroupsAsync(string username);
}

/// <summary>
/// LDAP 使用者資訊
/// </summary>
public class LdapUser
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Groups { get; set; } = new();
}
