namespace HRPayrollSystem.API.Models;

/// <summary>
/// 使用者角色關聯實體
/// </summary>
public class UserRole
{
    /// <summary>唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>使用者識別碼</summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>角色識別碼</summary>
    public string RoleId { get; set; } = null!;
    
    /// <summary>生效日期</summary>
    public DateTime EffectiveDate { get; set; }
    
    /// <summary>失效日期</summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    // 導航屬性
    /// <summary>使用者（員工）</summary>
    public Employee User { get; set; } = null!;
    
    /// <summary>角色</summary>
    public Role Role { get; set; } = null!;
}
