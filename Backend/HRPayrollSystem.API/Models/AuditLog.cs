namespace HRPayrollSystem.API.Models;

/// <summary>
/// 稽核日誌實體
/// </summary>
public class AuditLog
{
    /// <summary>稽核日誌唯一識別碼</summary>
    public string Id { get; set; } = null!;
    
    /// <summary>使用者識別碼</summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>操作動作</summary>
    public string Action { get; set; } = null!;
    
    /// <summary>實體類型</summary>
    public string EntityType { get; set; } = null!;
    
    /// <summary>實體識別碼</summary>
    public string? EntityId { get; set; }
    
    /// <summary>修改前的值（JSON 格式）</summary>
    public string? OldValue { get; set; }
    
    /// <summary>修改後的值（JSON 格式）</summary>
    public string? NewValue { get; set; }
    
    /// <summary>操作時間戳記</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>來源 IP 位址</summary>
    public string? IpAddress { get; set; }
}
