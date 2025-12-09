using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 部門管理服務實作
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(HRPayrollContext context, ILogger<DepartmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Department> CreateDepartmentAsync(string code, string name, string? managerId = null, string? parentDepartmentId = null)
    {
        _logger.LogInformation("建立部門: Code={Code}, Name={Name}", code, name);

        // 驗證部門代碼唯一性
        var existingDepartment = await _context.Departments
            .FirstOrDefaultAsync(d => d.Code == code);

        if (existingDepartment != null)
        {
            throw new InvalidOperationException($"部門代碼 {code} 已存在");
        }

        // 驗證主管是否存在
        if (!string.IsNullOrEmpty(managerId))
        {
            var manager = await _context.Employees.FindAsync(managerId);
            if (manager == null)
            {
                throw new InvalidOperationException($"找不到員工 {managerId}");
            }
        }

        // 驗證上級部門是否存在
        if (!string.IsNullOrEmpty(parentDepartmentId))
        {
            var parentDepartment = await _context.Departments.FindAsync(parentDepartmentId);
            if (parentDepartment == null)
            {
                throw new InvalidOperationException($"找不到上級部門 {parentDepartmentId}");
            }
        }

        var department = new Department
        {
            Id = Guid.NewGuid().ToString(),
            Code = code,
            Name = name,
            ManagerId = managerId,
            ParentDepartmentId = parentDepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        _logger.LogInformation("部門建立成功: Id={Id}, Code={Code}", department.Id, department.Code);

        return department;
    }

    /// <inheritdoc/>
    public async Task<Department> UpdateDepartmentAsync(string departmentId, string code, string name, string? managerId = null, string? parentDepartmentId = null)
    {
        _logger.LogInformation("更新部門: Id={Id}", departmentId);

        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
        {
            throw new InvalidOperationException($"找不到部門 {departmentId}");
        }

        // 驗證部門代碼唯一性（排除自己）
        var existingDepartment = await _context.Departments
            .FirstOrDefaultAsync(d => d.Code == code && d.Id != departmentId);

        if (existingDepartment != null)
        {
            throw new InvalidOperationException($"部門代碼 {code} 已存在");
        }

        // 驗證主管是否存在
        if (!string.IsNullOrEmpty(managerId))
        {
            var manager = await _context.Employees.FindAsync(managerId);
            if (manager == null)
            {
                throw new InvalidOperationException($"找不到員工 {managerId}");
            }
        }

        // 驗證上級部門是否存在且不是自己
        if (!string.IsNullOrEmpty(parentDepartmentId))
        {
            if (parentDepartmentId == departmentId)
            {
                throw new InvalidOperationException("部門不能設定自己為上級部門");
            }

            var parentDepartment = await _context.Departments.FindAsync(parentDepartmentId);
            if (parentDepartment == null)
            {
                throw new InvalidOperationException($"找不到上級部門 {parentDepartmentId}");
            }

            // 檢查是否會造成循環依賴
            if (await WouldCreateCircularDependencyAsync(departmentId, parentDepartmentId))
            {
                throw new InvalidOperationException("設定此上級部門會造成循環依賴");
            }
        }

        department.Code = code;
        department.Name = name;
        department.ManagerId = managerId;
        department.ParentDepartmentId = parentDepartmentId;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("部門更新成功: Id={Id}, Code={Code}", department.Id, department.Code);

        return department;
    }

    /// <inheritdoc/>
    public async Task<Department?> GetDepartmentAsync(string departmentId)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == departmentId);
    }

    /// <inheritdoc/>
    public async Task<List<Department>> GetAllDepartmentsAsync(bool includeInactive = false)
    {
        var query = _context.Departments.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(d => d.IsActive);
        }

        return await query.OrderBy(d => d.Code).ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<DepartmentHierarchy> GetDepartmentHierarchyAsync(string departmentId)
    {
        var department = await GetDepartmentAsync(departmentId);
        if (department == null)
        {
            throw new InvalidOperationException($"找不到部門 {departmentId}");
        }

        // 取得上級部門列表
        var ancestors = await GetAncestorsAsync(departmentId);

        // 取得下級部門列表
        var children = await _context.Departments
            .Where(d => d.ParentDepartmentId == departmentId)
            .OrderBy(d => d.Code)
            .ToListAsync();

        // 取得在職員工數量
        var activeEmployeeCount = await GetActiveEmployeeCountAsync(departmentId);

        return new DepartmentHierarchy
        {
            Department = department,
            Ancestors = ancestors,
            Children = children,
            ActiveEmployeeCount = activeEmployeeCount
        };
    }

    /// <inheritdoc/>
    public async Task<bool> DeactivateDepartmentAsync(string departmentId)
    {
        _logger.LogInformation("停用部門: Id={Id}", departmentId);

        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
        {
            throw new InvalidOperationException($"找不到部門 {departmentId}");
        }

        // 驗證部門是否有在職員工
        var activeEmployeeCount = await GetActiveEmployeeCountAsync(departmentId);
        if (activeEmployeeCount > 0)
        {
            throw new InvalidOperationException($"部門 {department.Name} 有 {activeEmployeeCount} 位在職員工，無法停用");
        }

        department.IsActive = false;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("部門停用成功: Id={Id}, Code={Code}", department.Id, department.Code);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ActivateDepartmentAsync(string departmentId)
    {
        _logger.LogInformation("啟用部門: Id={Id}", departmentId);

        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
        {
            throw new InvalidOperationException($"找不到部門 {departmentId}");
        }

        department.IsActive = true;
        department.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("部門啟用成功: Id={Id}, Code={Code}", department.Id, department.Code);

        return true;
    }

    /// <inheritdoc/>
    public async Task<int> GetActiveEmployeeCountAsync(string departmentId)
    {
        return await _context.Employees
            .CountAsync(e => e.DepartmentId == departmentId && e.Status == EmployeeStatus.Active);
    }

    /// <summary>
    /// 取得部門的所有上級部門（從根部門到當前部門的父部門）
    /// </summary>
    private async Task<List<Department>> GetAncestorsAsync(string departmentId)
    {
        var ancestors = new List<Department>();
        var currentDepartment = await GetDepartmentAsync(departmentId);

        while (currentDepartment?.ParentDepartmentId != null)
        {
            var parent = await GetDepartmentAsync(currentDepartment.ParentDepartmentId);
            if (parent == null) break;

            ancestors.Insert(0, parent); // 插入到列表開頭，保持從根到當前的順序
            currentDepartment = parent;
        }

        return ancestors;
    }

    /// <summary>
    /// 檢查設定上級部門是否會造成循環依賴
    /// </summary>
    private async Task<bool> WouldCreateCircularDependencyAsync(string departmentId, string parentDepartmentId)
    {
        var currentId = parentDepartmentId;
        var visited = new HashSet<string> { departmentId };

        while (currentId != null)
        {
            if (visited.Contains(currentId))
            {
                return true; // 發現循環
            }

            visited.Add(currentId);

            var department = await GetDepartmentAsync(currentId);
            currentId = department?.ParentDepartmentId;
        }

        return false;
    }
}
