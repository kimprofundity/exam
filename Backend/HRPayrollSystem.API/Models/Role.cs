namespace HRPayrollSystem.API.Models;

/// <summary>
/// 角色實體
/// </summary>
public class Role
{
    /// <summary>角色唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>角色代碼</summary>
    public string Code { get; set; } = null!;
    
    /// <summary>角色名稱</summary>
    public string Name { get; set; } = null!;
    
    /// <summary>角色描述</summary>
    public string? Description { get; set; }
    
    /// <summary>資料存取範圍（僅自己/本部門/全公司）</summary>
    public DataAccessScope DataAccessScope { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    /// <summary>角色權限列表</summary>
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    
    /// <summary>使用者角色關聯列表</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
