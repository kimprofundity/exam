namespace HRPayrollSystem.API.Services;

/// <summary>
/// 身份驗證服務介面
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="username">使用者名稱</param>
    /// <param name="password">密碼</param>
    /// <returns>驗證結果</returns>
    Task<AuthenticationResult> AuthenticateAsync(string username, string password);

    /// <summary>
    /// 驗證令牌
    /// </summary>
    /// <param name="token">JWT 令牌</param>
    /// <returns>令牌是否有效</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// 取得使用者資訊
    /// </summary>
    /// <param name="username">使用者名稱</param>
    /// <returns>使用者資訊</returns>
    Task<UserInfo?> GetUserInfoAsync(string username);
}

/// <summary>
/// 驗證結果
/// </summary>
public class AuthenticationResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserInfo? User { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 使用者資訊
/// </summary>
public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
