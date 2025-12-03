using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Data;

/// <summary>
/// HR 薪資系統資料庫上下文
/// </summary>
public class HRPayrollContext : DbContext
{
    public HRPayrollContext(DbContextOptions<HRPayrollContext> options)
        : base(options)
    {
    }

    // DbSet 屬性將在 Scaffold 後自動產生
    // 目前先建立基礎架構

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 模型配置將在 Scaffold 後補充
    }
}
