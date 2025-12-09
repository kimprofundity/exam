using Microsoft.EntityFrameworkCore;
using HRPayrollSystem.API.Models;

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

    // DbSet 屬性
    public DbSet<Employee> Employees { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<LeaveRecord> LeaveRecords { get; set; } = null!;
    public DbSet<SalaryRecord> SalaryRecords { get; set; } = null!;
    public DbSet<SalaryItem> SalaryItems { get; set; } = null!;
    public DbSet<RateTable> RateTables { get; set; } = null!;
    public DbSet<SystemParameter> SystemParameters { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 簡化配置 - 只保留基本的表映射，讓 EF Core 使用約定
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.Id);
            // 將枚舉轉換為字串儲存
            entity.Property(e => e.Status)
                .HasConversion<string>();
            // 忽略導航屬性，避免循環依賴
            entity.Ignore(e => e.Department);
            entity.Ignore(e => e.SalaryRecords);
            entity.Ignore(e => e.LeaveRecords);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(e => e.Id);
            // 忽略導航屬性
            entity.Ignore(e => e.Manager);
            entity.Ignore(e => e.ParentDepartment);
            entity.Ignore(e => e.SubDepartments);
            entity.Ignore(e => e.Employees);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            // 將枚舉轉換為字串儲存
            entity.Property(e => e.DataAccessScope)
                .HasConversion<string>();
            // 忽略導航屬性
            entity.Ignore(e => e.Permissions);
            entity.Ignore(e => e.UserRoles);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => e.Id);
            // 忽略導航屬性
            entity.Ignore(e => e.User);
            entity.Ignore(e => e.Role);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(e => e.Id);
            // 忽略導航屬性
            entity.Ignore(e => e.Role);
        });
        
        modelBuilder.Entity<LeaveRecord>(entity =>
        {
            entity.ToTable("LeaveRecords");
            entity.HasKey(e => e.Id);
            // 將枚舉轉換為字串儲存
            entity.Property(e => e.Type)
                .HasConversion<string>();
            entity.Property(e => e.Status)
                .HasConversion<string>();
            // 忽略導航屬性
            entity.Ignore(e => e.Employee);
            entity.Ignore(e => e.ProxyUser);
        });
        
        modelBuilder.Entity<SalaryRecord>(entity =>
        {
            entity.ToTable("SalaryRecords");
            entity.HasKey(e => e.Id);
            // 將枚舉轉換為字串儲存
            entity.Property(e => e.Status)
                .HasConversion<string>();
            // 忽略導航屬性
            entity.Ignore(e => e.Employee);
            entity.Ignore(e => e.SalaryItems);
        });
        
        modelBuilder.Entity<SalaryItem>(entity =>
        {
            entity.ToTable("SalaryItems");
            entity.HasKey(e => e.Id);
            // 將枚舉轉換為字串儲存
            entity.Property(e => e.Type)
                .HasConversion<string>();
            // 忽略導航屬性
            entity.Ignore(e => e.SalaryRecord);
        });
        
        modelBuilder.Entity<RateTable>(entity =>
        {
            entity.ToTable("RateTables");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<SystemParameter>(entity =>
        {
            entity.ToTable("SystemParameters");
            entity.HasKey(e => e.Id);
        });
        
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
        });
    }
}
