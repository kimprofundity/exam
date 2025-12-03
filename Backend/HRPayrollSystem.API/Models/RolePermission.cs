namespace HRPayrollSystem.API.Models;

/// <summary>
/// 角色權限實體
/// </summary>
public class RolePermission
{
    /// <summary>唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>角色識別碼</summary>
    public string RoleId { get; set; } = null!;
    
    /// <summary>權限名稱</summary>
    public string Permission { get; set; } = null!;
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    // 導航屬性
    /// <summary>角色</summary>
    public Role Role { get; set; } = null!;
}
