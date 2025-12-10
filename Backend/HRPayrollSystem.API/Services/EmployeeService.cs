using Microsoft.EntityFrameworkCore;
using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Models;

namespace HRPayrollSystem.API.Services;

/// <summary>
/// 員工管理服務實作
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly HRPayrollContext _context;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(HRPayrollContext context, ILogger<EmployeeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> CreateEmployeeAsync(
        string employeeNumber,
        string name,
        string departmentId,
        string? position,
        decimal monthlySalary,
        string? bankCode,
        string? bankAccount)
    {
        _logger.LogInformation("建立員工：{EmployeeNumber} - {Name}", employeeNumber, name);

        // 驗證必填欄位
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("員工編號不能為空", nameof(employeeNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("員工姓名不能為空", nameof(name));
        if (string.IsNullOrWhiteSpace(departmentId))
            throw new ArgumentException("部門識別碼不能為空", nameof(departmentId));
        if (monthlySalary < 0)
            throw new ArgumentException("月薪不能為負數", nameof(monthlySalary));

        // 檢查員工編號是否已存在
        if (await EmployeeNumberExistsAsync(employeeNumber))
        {
            throw new InvalidOperationException($"員工編號 {employeeNumber} 已存在");
        }

        // 檢查部門是否存在
        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
        {
            throw new InvalidOperationException($"部門 {departmentId} 不存在");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeNumber = employeeNumber,
            Name = name,
            DepartmentId = departmentId,
            Position = position,
            MonthlySalary = monthlySalary,
            BankCode = bankCode,
            BankAccount = bankAccount,
            Status = EmployeeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("員工建立成功：{EmployeeId}", employee.Id);
        return employee.Id;
    }

    public async Task<string> CreateEmployeeWithSalaryTypeAsync(
        string employeeNumber,
        string name,
        string departmentId,
        string? position,
        SalaryType salaryType,
        decimal? monthlySalary,
        decimal? dailySalary,
        decimal? hourlySalary,
        string? bankCode,
        string? bankAccount)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("員工編號不能為空", nameof(employeeNumber));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("員工姓名不能為空", nameof(name));

        if (string.IsNullOrWhiteSpace(departmentId))
            throw new ArgumentException("部門識別碼不能為空", nameof(departmentId));

        // 根據薪資類型驗證對應的薪資欄位
        switch (salaryType)
        {
            case SalaryType.Monthly:
                if (!monthlySalary.HasValue || monthlySalary <= 0)
                    throw new ArgumentException("月薪類型員工必須設定有效的月薪金額", nameof(monthlySalary));
                break;
            case SalaryType.Daily:
                if (!dailySalary.HasValue || dailySalary <= 0)
                    throw new ArgumentException("日薪類型員工必須設定有效的日薪金額", nameof(dailySalary));
                break;
            case SalaryType.Hourly:
                if (!hourlySalary.HasValue || hourlySalary <= 0)
                    throw new ArgumentException("時薪類型員工必須設定有效的時薪金額", nameof(hourlySalary));
                break;
        }

        // 檢查員工編號是否已存在
        if (await EmployeeNumberExistsAsync(employeeNumber))
        {
            throw new InvalidOperationException($"員工編號 {employeeNumber} 已存在");
        }

        // 檢查部門是否存在
        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
        {
            throw new InvalidOperationException($"部門 {departmentId} 不存在");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeNumber = employeeNumber,
            Name = name,
            DepartmentId = departmentId,
            Position = position,
            SalaryType = salaryType,
            MonthlySalary = monthlySalary ?? 0,
            DailySalary = dailySalary,
            HourlySalary = hourlySalary,
            BankCode = bankCode,
            BankAccount = bankAccount,
            Status = EmployeeStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("員工建立成功：{EmployeeId}，薪資類型：{SalaryType}", employee.Id, salaryType);
        return employee.Id;
    }

    public async Task UpdateEmployeeAsync(
        string employeeId,
        string name,
        string? position,
        decimal monthlySalary,
        string? bankCode,
        string? bankAccount)
    {
        _logger.LogInformation("更新員工資料：{EmployeeId}", employeeId);

        // 驗證必填欄位
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("員工姓名不能為空", nameof(name));
        if (monthlySalary < 0)
            throw new ArgumentException("月薪不能為負數", nameof(monthlySalary));

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            throw new InvalidOperationException($"員工 {employeeId} 不存在");
        }

        // 記錄變更歷史（簡化版本，實際應該有專門的變更記錄表）
        _logger.LogInformation(
            "員工資料變更：{EmployeeId}, 姓名: {OldName} -> {NewName}, 月薪: {OldSalary} -> {NewSalary}",
            employeeId, employee.Name, name, employee.MonthlySalary, monthlySalary);

        employee.Name = name;
        employee.Position = position;
        employee.MonthlySalary = monthlySalary;
        employee.BankCode = bankCode;
        employee.BankAccount = bankAccount;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("員工資料更新成功：{EmployeeId}", employeeId);
    }

    public async Task TransferDepartmentAsync(string employeeId, string newDepartmentId, DateTime transferDate)
    {
        _logger.LogInformation("員工部門調動：{EmployeeId} -> {NewDepartmentId}", employeeId, newDepartmentId);

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            throw new InvalidOperationException($"員工 {employeeId} 不存在");
        }

        // 檢查新部門是否存在
        var newDepartment = await _context.Departments.FindAsync(newDepartmentId);
        if (newDepartment == null)
        {
            throw new InvalidOperationException($"部門 {newDepartmentId} 不存在");
        }

        var oldDepartmentId = employee.DepartmentId;
        employee.DepartmentId = newDepartmentId;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "員工部門調動成功：{EmployeeId}, {OldDepartmentId} -> {NewDepartmentId}, 調動日期: {TransferDate}",
            employeeId, oldDepartmentId, newDepartmentId, transferDate);
    }

    public async Task ResignEmployeeAsync(string employeeId, DateTime resignationDate)
    {
        _logger.LogInformation("員工離職：{EmployeeId}, 離職日期: {ResignationDate}", employeeId, resignationDate);

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            throw new InvalidOperationException($"員工 {employeeId} 不存在");
        }

        if (employee.Status == EmployeeStatus.Resigned)
        {
            throw new InvalidOperationException($"員工 {employeeId} 已經離職");
        }

        employee.Status = EmployeeStatus.Resigned;
        employee.ResignationDate = resignationDate;
        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("員工離職處理完成：{EmployeeId}", employeeId);
    }

    public async Task<EmployeeDto?> GetEmployeeAsync(string employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
        {
            return null;
        }

        // 獨立查詢部門資訊
        var department = await _context.Departments.FindAsync(employee.DepartmentId);

        return new EmployeeDto
        {
            Id = employee.Id,
            EmployeeNumber = employee.EmployeeNumber,
            Name = employee.Name,
            DepartmentId = employee.DepartmentId,
            DepartmentName = department?.Name ?? "未知部門",
            Position = employee.Position,
            MonthlySalary = employee.MonthlySalary,
            BankCode = employee.BankCode,
            BankAccount = employee.BankAccount,
            Status = employee.Status,
            ResignationDate = employee.ResignationDate,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    public async Task<PagedResult<EmployeeDto>> GetEmployeesAsync(
        string? departmentId = null,
        EmployeeStatus? status = null,
        string? searchKeyword = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.Employees.AsQueryable();

        // 篩選條件
        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            query = query.Where(e =>
                e.Name.Contains(searchKeyword) ||
                e.EmployeeNumber.Contains(searchKeyword));
        }

        // 計算總數
        var totalCount = await query.CountAsync();

        // 分頁
        var employees = await query
            .OrderBy(e => e.EmployeeNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 取得所有部門資訊（批次查詢）
        var departmentIds = employees.Select(e => e.DepartmentId).Distinct().ToList();
        var departments = await _context.Departments
            .Where(d => departmentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name);

        var employeeDtos = employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            EmployeeNumber = e.EmployeeNumber,
            Name = e.Name,
            DepartmentId = e.DepartmentId,
            DepartmentName = departments.GetValueOrDefault(e.DepartmentId, "未知部門"),
            Position = e.Position,
            MonthlySalary = e.MonthlySalary,
            BankCode = e.BankCode,
            BankAccount = e.BankAccount,
            Status = e.Status,
            ResignationDate = e.ResignationDate,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        }).ToList();

        return new PagedResult<EmployeeDto>
        {
            Items = employeeDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<bool> EmployeeNumberExistsAsync(string employeeNumber)
    {
        return await _context.Employees.AnyAsync(e => e.EmployeeNumber == employeeNumber);
    }
}
