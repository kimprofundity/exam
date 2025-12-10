using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HRPayrollSystem.API.Data;
using HRPayrollSystem.API.Services;
using HRPayrollSystem.API.Middleware;
using HRPayrollSystem.API.Extensions;
using HRPayrollSystem.API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 配置資料庫連線
builder.Services.AddDbContext<HRPayrollContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 註冊服務
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<RoleMappingService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<ISalaryItemDefinitionService, SalaryItemDefinitionService>();
builder.Services.AddScoped<IRateTableService, RateTableService>();
builder.Services.AddScoped<IPayrollCalculationService, PayrollCalculationService>();
builder.Services.AddScoped<ITaxCalculationService, TaxCalculationService>();

// 註冊背景服務（暫時禁用以進行測試）
// builder.Services.AddHostedService<AdSyncBackgroundService>();

// 配置 JWT 認證
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey 未設定");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 配置 JSON 序列化選項
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// 配置 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vue 前端
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.

// 使用錯誤處理中介軟體
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 使用 CORS
app.UseCors();

// 使用認證和授權
app.UseAuthentication();
app.UseAuthorization();
app.UseCustomAuthorization();

// =============================================
// API 端點
// =============================================

// 簡單的健康檢查端點（不使用 DbContext）
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow,
        message = "API is running"
    });
})
.WithName("HealthCheck")
.WithOpenApi();

// 登入端點
app.MapPost("/api/auth/login", async (LoginRequest request, IAuthenticationService authService) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { success = false, message = "使用者名稱和密碼不能為空" });
    }

    var result = await authService.AuthenticateAsync(request.Username, request.Password);

    if (result.Success)
    {
        return Results.Ok(result);
    }

    return Results.Unauthorized();
})
.WithName("Login")
.WithOpenApi();

// 取得使用者資訊端點（需要認證）
app.MapGet("/api/auth/me", async (IAuthenticationService authService, HttpContext context) =>
{
    var username = context.User.Identity?.Name;
    
    if (string.IsNullOrEmpty(username))
    {
        return Results.Unauthorized();
    }

    var userInfo = await authService.GetUserInfoAsync(username);
    
    if (userInfo == null)
    {
        return Results.NotFound(new { success = false, message = "找不到使用者資訊" });
    }

    return Results.Ok(userInfo);
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithOpenApi();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// =============================================
// 員工管理 API
// =============================================

// 建立員工
app.MapPost("/api/employees", async (CreateEmployeeRequest request, IEmployeeService employeeService) =>
{
    try
    {
        var employeeId = await employeeService.CreateEmployeeAsync(
            request.EmployeeNumber,
            request.Name,
            request.DepartmentId,
            request.Position,
            request.MonthlySalary,
            request.BankCode,
            request.BankAccount);

        return Results.Created($"/api/employees/{employeeId}", new { id = employeeId });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CreateEmployee")
.WithOpenApi();

// 建立員工（支援薪資類型）
app.MapPost("/api/employees/with-salary-type", async (CreateEmployeeWithSalaryTypeRequest request, IEmployeeService employeeService) =>
{
    try
    {
        var salaryType = Enum.Parse<SalaryType>(request.SalaryType, true);
        
        var employeeId = await employeeService.CreateEmployeeWithSalaryTypeAsync(
            request.EmployeeNumber,
            request.Name,
            request.DepartmentId,
            request.Position,
            salaryType,
            request.MonthlySalary,
            request.DailySalary,
            request.HourlySalary,
            request.BankCode,
            request.BankAccount);

        return Results.Created($"/api/employees/{employeeId}", new { id = employeeId });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CreateEmployeeWithSalaryType")
.WithOpenApi();

// 更新員工
app.MapPut("/api/employees/{id}", async (string id, UpdateEmployeeRequest request, IEmployeeService employeeService) =>
{
    try
    {
        await employeeService.UpdateEmployeeAsync(
            id,
            request.Name,
            request.Position,
            request.MonthlySalary,
            request.BankCode,
            request.BankAccount);

        return Results.Ok(new { success = true, message = "員工資料更新成功" });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("UpdateEmployee")
.WithOpenApi();

// 員工部門調動
app.MapPost("/api/employees/{id}/transfer", async (string id, TransferDepartmentRequest request, IEmployeeService employeeService) =>
{
    try
    {
        await employeeService.TransferDepartmentAsync(id, request.NewDepartmentId, request.TransferDate);
        return Results.Ok(new { success = true, message = "員工部門調動成功" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("TransferEmployee")
.WithOpenApi();

// 員工離職
app.MapPost("/api/employees/{id}/resign", async (string id, ResignEmployeeRequest request, IEmployeeService employeeService) =>
{
    try
    {
        await employeeService.ResignEmployeeAsync(id, request.ResignationDate);
        return Results.Ok(new { success = true, message = "員工離職處理完成" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("ResignEmployee")
.WithOpenApi();

// 取得員工資料
app.MapGet("/api/employees/{id}", async (string id, IEmployeeService employeeService) =>
{
    var employee = await employeeService.GetEmployeeAsync(id);
    if (employee == null)
    {
        return Results.NotFound(new { success = false, message = "找不到員工資料" });
    }
    return Results.Ok(employee);
})
.RequireAuthorization()
.WithName("GetEmployee")
.WithOpenApi();

// 取得員工列表
app.MapGet("/api/employees", async (
    IEmployeeService employeeService,
    string? departmentId = null,
    EmployeeStatus? status = null,
    string? searchKeyword = null,
    int pageNumber = 1,
    int pageSize = 20) =>
{
    var result = await employeeService.GetEmployeesAsync(
        departmentId,
        status,
        searchKeyword,
        pageNumber,
        pageSize);

    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("GetEmployees")
.WithOpenApi();

// =============================================
// 部門管理 API
// =============================================

// 建立部門
app.MapPost("/api/departments", async (CreateDepartmentRequest request, IDepartmentService departmentService) =>
{
    try
    {
        var department = await departmentService.CreateDepartmentAsync(
            request.Code,
            request.Name,
            request.ManagerId,
            request.ParentDepartmentId);

        return Results.Created($"/api/departments/{department.Id}", department);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CreateDepartment")
.WithOpenApi();

// 更新部門
app.MapPut("/api/departments/{id}", async (string id, UpdateDepartmentRequest request, IDepartmentService departmentService) =>
{
    try
    {
        var department = await departmentService.UpdateDepartmentAsync(
            id,
            request.Code,
            request.Name,
            request.ManagerId,
            request.ParentDepartmentId);

        return Results.Ok(department);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("UpdateDepartment")
.WithOpenApi();

// 取得部門資料
app.MapGet("/api/departments/{id}", async (string id, IDepartmentService departmentService) =>
{
    var department = await departmentService.GetDepartmentAsync(id);
    if (department == null)
    {
        return Results.NotFound(new { success = false, message = "找不到部門資料" });
    }
    return Results.Ok(department);
})
.RequireAuthorization()
.WithName("GetDepartment")
.WithOpenApi();

// 取得所有部門列表
app.MapGet("/api/departments", async (IDepartmentService departmentService, bool includeInactive = false) =>
{
    var departments = await departmentService.GetAllDepartmentsAsync(includeInactive);
    return Results.Ok(departments);
})
.RequireAuthorization()
.WithName("GetAllDepartments")
.WithOpenApi();

// 取得部門階層
app.MapGet("/api/departments/{id}/hierarchy", async (string id, IDepartmentService departmentService) =>
{
    try
    {
        var hierarchy = await departmentService.GetDepartmentHierarchyAsync(id);
        return Results.Ok(hierarchy);
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("GetDepartmentHierarchy")
.WithOpenApi();

// 停用部門
app.MapPost("/api/departments/{id}/deactivate", async (string id, IDepartmentService departmentService) =>
{
    try
    {
        await departmentService.DeactivateDepartmentAsync(id);
        return Results.Ok(new { success = true, message = "部門已停用" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("DeactivateDepartment")
.WithOpenApi();

// 啟用部門
app.MapPost("/api/departments/{id}/activate", async (string id, IDepartmentService departmentService) =>
{
    try
    {
        await departmentService.ActivateDepartmentAsync(id);
        return Results.Ok(new { success = true, message = "部門已啟用" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("ActivateDepartment")
.WithOpenApi();

// 取得部門員工數量
app.MapGet("/api/departments/{id}/employee-count", async (string id, IDepartmentService departmentService) =>
{
    var count = await departmentService.GetActiveEmployeeCountAsync(id);
    return Results.Ok(new { departmentId = id, activeEmployeeCount = count });
})
.RequireAuthorization()
.WithName("GetDepartmentEmployeeCount")
.WithOpenApi();

// =============================================
// 請假管理 API
// =============================================

// 建立請假申請
app.MapPost("/api/leaves", async (CreateLeaveRequest request, ILeaveService leaveService) =>
{
    try
    {
        var leaveId = await leaveService.CreateLeaveRequestAsync(
            request.EmployeeId,
            request.Type,
            request.StartDate,
            request.EndDate,
            request.Days,
            request.ProxyUserId);

        return Results.Created($"/api/leaves/{leaveId}", new { id = leaveId });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CreateLeaveRequest")
.WithOpenApi();

// 確認代理請假
app.MapPost("/api/leaves/{id}/confirm", async (string id, ILeaveService leaveService) =>
{
    try
    {
        await leaveService.ConfirmProxyLeaveAsync(id);
        return Results.Ok(new { success = true, message = "代理請假已確認" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("ConfirmProxyLeave")
.WithOpenApi();

// 拒絕代理請假
app.MapPost("/api/leaves/{id}/reject", async (string id, ILeaveService leaveService) =>
{
    try
    {
        await leaveService.RejectProxyLeaveAsync(id);
        return Results.Ok(new { success = true, message = "代理請假已拒絕" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("RejectProxyLeave")
.WithOpenApi();

// 取得請假記錄詳情
app.MapGet("/api/leaves/{id}", async (string id, ILeaveService leaveService) =>
{
    var leave = await leaveService.GetLeaveRecordAsync(id);
    if (leave == null)
    {
        return Results.NotFound(new { success = false, message = "找不到請假記錄" });
    }
    return Results.Ok(leave);
})
.RequireAuthorization()
.WithName("GetLeaveRecord")
.WithOpenApi();

// 取得員工請假記錄
app.MapGet("/api/employees/{employeeId}/leaves", async (
    string employeeId,
    ILeaveService leaveService,
    DateTime? startDate = null,
    DateTime? endDate = null) =>
{
    var leaves = await leaveService.GetEmployeeLeaveRecordsAsync(employeeId, startDate, endDate);
    return Results.Ok(leaves);
})
.RequireAuthorization()
.WithName("GetEmployeeLeaveRecords")
.WithOpenApi();

// 取得員工剩餘假期額度
app.MapGet("/api/employees/{employeeId}/leave-balance", async (
    string employeeId,
    LeaveType type,
    ILeaveService leaveService) =>
{
    try
    {
        var balance = await leaveService.GetRemainingLeaveBalanceAsync(employeeId, type);
        return Results.Ok(new { employeeId, leaveType = type.ToString(), remainingDays = balance });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("GetLeaveBalance")
.WithOpenApi();

// 檢查請假日期是否重疊
app.MapPost("/api/leaves/check-overlap", async (CheckOverlapRequest request, ILeaveService leaveService) =>
{
    var hasOverlap = await leaveService.HasOverlappingLeaveAsync(
        request.EmployeeId,
        request.StartDate,
        request.EndDate,
        request.ExcludeLeaveId);

    return Results.Ok(new { hasOverlap });
})
.RequireAuthorization()
.WithName("CheckLeaveOverlap")
.WithOpenApi();

// =============================================
// 薪資項目定義管理 API
// =============================================

// 建立薪資項目定義
app.MapPost("/api/salary-item-definitions", async (
    CreateSalaryItemDefinitionRequest request,
    ISalaryItemDefinitionService service,
    HttpContext context) =>
{
    try
    {
        var username = context.User.Identity?.Name ?? "system";
        
        var definition = new SalaryItemDefinition
        {
            ItemCode = request.ItemCode,
            ItemName = request.ItemName,
            Type = Enum.Parse<SalaryItemType>(request.Type),
            CalculationMethod = Enum.Parse<CalculationMethod>(request.CalculationMethod),
            DefaultAmount = request.DefaultAmount,
            HourlyRate = request.HourlyRate,
            PercentageRate = request.PercentageRate,
            Description = request.Description,
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate
        };

        var result = await service.CreateAsync(definition, username);
        return Results.Created($"/api/salary-item-definitions/{result.Id}", result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CreateSalaryItemDefinition")
.WithOpenApi();

// 更新薪資項目定義
app.MapPut("/api/salary-item-definitions/{id}", async (
    string id,
    UpdateSalaryItemDefinitionRequest request,
    ISalaryItemDefinitionService service) =>
{
    try
    {
        var definition = new SalaryItemDefinition
        {
            ItemName = request.ItemName,
            Type = Enum.Parse<SalaryItemType>(request.Type),
            CalculationMethod = Enum.Parse<CalculationMethod>(request.CalculationMethod),
            DefaultAmount = request.DefaultAmount,
            HourlyRate = request.HourlyRate,
            PercentageRate = request.PercentageRate,
            Description = request.Description,
            IsActive = request.IsActive,
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate
        };

        var result = await service.UpdateAsync(id, definition);
        return Results.Ok(result);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { success = false, message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("UpdateSalaryItemDefinition")
.WithOpenApi();

// 取得薪資項目定義
app.MapGet("/api/salary-item-definitions/{id}", async (string id, ISalaryItemDefinitionService service) =>
{
    var definition = await service.GetByIdAsync(id);
    if (definition == null)
    {
        return Results.NotFound(new { success = false, message = "找不到薪資項目定義" });
    }
    return Results.Ok(definition);
})
.RequireAuthorization()
.WithName("GetSalaryItemDefinition")
.WithOpenApi();

// 取得所有啟用的薪資項目定義
app.MapGet("/api/salary-item-definitions", async (ISalaryItemDefinitionService service) =>
{
    var definitions = await service.GetActiveItemsAsync();
    return Results.Ok(definitions);
})
.RequireAuthorization()
.WithName("GetActiveSalaryItemDefinitions")
.WithOpenApi();

// 取得所有薪資項目定義（含停用）
app.MapGet("/api/salary-item-definitions/all", async (ISalaryItemDefinitionService service) =>
{
    var definitions = await service.GetAllItemsAsync();
    return Results.Ok(definitions);
})
.RequireAuthorization()
.WithName("GetAllSalaryItemDefinitions")
.WithOpenApi();

// 取得特定類型的薪資項目定義
app.MapGet("/api/salary-item-definitions/by-type/{type}", async (
    string type,
    ISalaryItemDefinitionService service) =>
{
    try
    {
        var itemType = Enum.Parse<SalaryItemType>(type, true);
        var definitions = await service.GetItemsByTypeAsync(itemType);
        return Results.Ok(definitions);
    }
    catch (ArgumentException)
    {
        return Results.BadRequest(new { success = false, message = "無效的項目類型" });
    }
})
.RequireAuthorization()
.WithName("GetSalaryItemDefinitionsByType")
.WithOpenApi();

// 停用薪資項目定義
app.MapPost("/api/salary-item-definitions/{id}/deactivate", async (
    string id,
    ISalaryItemDefinitionService service) =>
{
    var success = await service.DeactivateAsync(id);
    if (!success)
    {
        return Results.NotFound(new { success = false, message = "找不到薪資項目定義" });
    }
    return Results.Ok(new { success = true, message = "薪資項目定義已停用" });
})
.RequireAuthorization()
.WithName("DeactivateSalaryItemDefinition")
.WithOpenApi();

// 取得薪資項目定義的歷史版本
app.MapGet("/api/salary-item-definitions/history/{itemCode}", async (
    string itemCode,
    ISalaryItemDefinitionService service) =>
{
    var history = await service.GetItemHistoryAsync(itemCode);
    return Results.Ok(history);
})
.RequireAuthorization()
.WithName("GetSalaryItemDefinitionHistory")
.WithOpenApi();

// =============================================
// 費率表管理 API
// =============================================

// 建立費率表
app.MapPost("/api/rate-tables", async (
    CreateRateTableRequest request,
    IRateTableService service,
    HttpContext context) =>
{
    try
    {
        var username = context.User.Identity?.Name ?? "system";
        
        var rateTable = new RateTable
        {
            Version = request.Version,
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate,
            LaborInsuranceRate = request.LaborInsuranceRate,
            HealthInsuranceRate = request.HealthInsuranceRate,
            Source = request.Source
        };

        var result = await service.CreateAsync(rateTable, username);
        return Results.Created($"/api/rate-tables/{result.Id}", result);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CreateRateTable")
.WithOpenApi();

// 更新費率表
app.MapPut("/api/rate-tables/{id}", async (
    string id,
    UpdateRateTableRequest request,
    IRateTableService service) =>
{
    try
    {
        var rateTable = new RateTable
        {
            Version = request.Version,
            EffectiveDate = request.EffectiveDate,
            ExpiryDate = request.ExpiryDate,
            LaborInsuranceRate = request.LaborInsuranceRate,
            HealthInsuranceRate = request.HealthInsuranceRate,
            Source = request.Source
        };

        var result = await service.UpdateAsync(id, rateTable);
        return Results.Ok(result);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { success = false, message = ex.Message });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("UpdateRateTable")
.WithOpenApi();

// 取得費率表
app.MapGet("/api/rate-tables/{id}", async (string id, IRateTableService service) =>
{
    var rateTable = await service.GetByIdAsync(id);
    if (rateTable == null)
    {
        return Results.NotFound(new { success = false, message = "找不到費率表" });
    }
    return Results.Ok(rateTable);
})
.RequireAuthorization()
.WithName("GetRateTable")
.WithOpenApi();

// 取得生效的費率表
app.MapGet("/api/rate-tables/effective/{date}", async (DateTime date, IRateTableService service) =>
{
    var rateTable = await service.GetEffectiveRateTableAsync(date);
    if (rateTable == null)
    {
        return Results.NotFound(new { success = false, message = "找不到生效的費率表" });
    }
    return Results.Ok(rateTable);
})
.RequireAuthorization()
.WithName("GetEffectiveRateTable")
.WithOpenApi();

// 取得所有費率表
app.MapGet("/api/rate-tables", async (IRateTableService service) =>
{
    var rateTables = await service.GetAllRateTablesAsync();
    return Results.Ok(rateTables);
})
.RequireAuthorization()
.WithName("GetAllRateTables")
.WithOpenApi();

// 取得費率表歷史
app.MapGet("/api/rate-tables/history", async (IRateTableService service) =>
{
    var history = await service.GetRateTableHistoryAsync();
    return Results.Ok(history);
})
.RequireAuthorization()
.WithName("GetRateTableHistory")
.WithOpenApi();

// 匯入費率表
app.MapPost("/api/rate-tables/import", async (
    HttpRequest request,
    IRateTableService service,
    HttpContext context) =>
{
    try
    {
        var username = context.User.Identity?.Name ?? "system";
        
        if (!request.HasFormContentType || request.Form.Files.Count == 0)
        {
            return Results.BadRequest(new { success = false, message = "請上傳檔案" });
        }

        var file = request.Form.Files[0];
        using var stream = file.OpenReadStream();
        
        var result = await service.ImportFromFileAsync(stream, file.FileName, username);
        return Results.Created($"/api/rate-tables/{result.Id}", result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("ImportRateTable")
.WithOpenApi()
.DisableAntiforgery();

// 刪除費率表
app.MapDelete("/api/rate-tables/{id}", async (string id, IRateTableService service) =>
{
    var success = await service.DeleteAsync(id);
    if (!success)
    {
        return Results.NotFound(new { success = false, message = "找不到費率表" });
    }
    return Results.Ok(new { success = true, message = "費率表已刪除" });
})
.RequireAuthorization()
.WithName("DeleteRateTable")
.WithOpenApi();

// =============================================
// 薪資計算 API
// =============================================

// 計算員工薪資
app.MapPost("/api/payroll/calculate", async (
    CalculateSalaryRequest request,
    IPayrollCalculationService service,
    HRPayrollContext context) =>
{
    try
    {
        var salaryRecord = await service.CalculateSalaryAsync(
            request.EmployeeId,
            request.Period,
            request.CopyFromPreviousMonth);

        // 儲存薪資記錄到資料庫
        context.SalaryRecords.Add(salaryRecord);
        await context.SaveChangesAsync();

        return Results.Created($"/api/payroll/salary-records/{salaryRecord.Id}", salaryRecord);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CalculateSalary")
.WithOpenApi();

// 取得薪資記錄
app.MapGet("/api/payroll/salary-records/{id}", async (string id, HRPayrollContext context) =>
{
    var salaryRecord = await context.SalaryRecords
        .Include(sr => sr.Employee)
        .Include(sr => sr.SalaryItems)
        .FirstOrDefaultAsync(sr => sr.Id == id);

    if (salaryRecord == null)
    {
        return Results.NotFound(new { success = false, message = "找不到薪資記錄" });
    }

    return Results.Ok(salaryRecord);
})
.RequireAuthorization()
.WithName("GetSalaryRecord")
.WithOpenApi();

// 取得員工薪資記錄列表
app.MapGet("/api/payroll/employees/{employeeId}/salary-records", async (
    string employeeId,
    HRPayrollContext context,
    int? year = null,
    int? month = null,
    int pageNumber = 1,
    int pageSize = 20) =>
{
    var query = context.SalaryRecords
        .Include(sr => sr.Employee)
        .Where(sr => sr.EmployeeId == employeeId);

    if (year.HasValue)
    {
        query = query.Where(sr => sr.Period.Year == year.Value);
    }

    if (month.HasValue)
    {
        query = query.Where(sr => sr.Period.Month == month.Value);
    }

    var totalCount = await query.CountAsync();
    var records = await query
        .OrderByDescending(sr => sr.Period)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new
    {
        data = records,
        totalCount,
        pageNumber,
        pageSize,
        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
    });
})
.RequireAuthorization()
.WithName("GetEmployeeSalaryRecords")
.WithOpenApi();

// 載入上月薪資記錄
app.MapGet("/api/payroll/employees/{employeeId}/previous-month-salary", async (
    string employeeId,
    DateTime currentPeriod,
    IPayrollCalculationService service) =>
{
    var previousSalary = await service.LoadPreviousMonthSalaryAsync(employeeId, currentPeriod);
    
    if (previousSalary == null)
    {
        return Results.NotFound(new { success = false, message = "找不到上月薪資記錄" });
    }

    return Results.Ok(previousSalary);
})
.RequireAuthorization()
.WithName("GetPreviousMonthSalary")
.WithOpenApi();

// 計算基本薪資
app.MapPost("/api/payroll/calculate-base-salary", async (
    CalculateBaseSalaryRequest request,
    IPayrollCalculationService service,
    HRPayrollContext context) =>
{
    try
    {
        var employee = await context.Employees.FindAsync(request.EmployeeId);
        if (employee == null)
        {
            return Results.NotFound(new { success = false, message = "找不到員工資料" });
        }

        var baseSalary = await service.CalculateBaseSalaryAsync(
            employee,
            request.Period,
            request.WorkDays,
            request.TotalWorkDays);

        return Results.Ok(new { baseSalary });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CalculateBaseSalary")
.WithOpenApi();

// 計算請假扣款
app.MapGet("/api/payroll/employees/{employeeId}/leave-deduction", async (
    string employeeId,
    DateTime period,
    IPayrollCalculationService service) =>
{
    var deduction = await service.CalculateLeaveDeductionAsync(employeeId, period);
    return Results.Ok(new { leaveDeduction = deduction });
})
.RequireAuthorization()
.WithName("CalculateLeaveDeduction")
.WithOpenApi();

// 計算加班費
app.MapPost("/api/payroll/calculate-overtime", async (
    CalculateOvertimeRequest request,
    IPayrollCalculationService service) =>
{
    var overtimePay = await service.CalculateOvertimePayAsync(
        request.EmployeeId,
        request.Period,
        request.OvertimeHours);

    return Results.Ok(new { overtimePay });
})
.RequireAuthorization()
.WithName("CalculateOvertimePay")
.WithOpenApi();

// 取得當月工作天數
app.MapGet("/api/payroll/working-days/{period}", async (
    DateTime period,
    IPayrollCalculationService service) =>
{
    var workingDays = await service.GetWorkingDaysInMonthAsync(period);
    return Results.Ok(new { workingDays });
})
.RequireAuthorization()
.WithName("GetWorkingDays")
.WithOpenApi();

// 計算實際出勤天數
app.MapGet("/api/payroll/employees/{employeeId}/actual-work-days", async (
    string employeeId,
    DateTime period,
    IPayrollCalculationService service) =>
{
    var actualWorkDays = await service.CalculateActualWorkDaysAsync(employeeId, period);
    return Results.Ok(new { actualWorkDays });
})
.RequireAuthorization()
.WithName("CalculateActualWorkDays")
.WithOpenApi();

// =============================================
// 稅務計算 API
// =============================================

// 計算所得稅
app.MapPost("/api/tax/calculate-income-tax", async (
    CalculateIncomeTaxRequest request,
    ITaxCalculationService service) =>
{
    try
    {
        var incomeTax = await service.CalculateIncomeTaxAsync(
            request.GrossSalary,
            request.EmployeeId,
            request.Period);

        return Results.Ok(new { incomeTax });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CalculateIncomeTax")
.WithOpenApi();

// 計算勞保費
app.MapPost("/api/tax/calculate-labor-insurance", async (
    CalculateInsuranceRequest request,
    ITaxCalculationService service) =>
{
    try
    {
        var laborInsurance = await service.CalculateLaborInsuranceAsync(
            request.Salary,
            request.Period);

        return Results.Ok(new { laborInsurance });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CalculateLaborInsurance")
.WithOpenApi();

// 計算健保費
app.MapPost("/api/tax/calculate-health-insurance", async (
    CalculateInsuranceRequest request,
    ITaxCalculationService service) =>
{
    try
    {
        var healthInsurance = await service.CalculateHealthInsuranceAsync(
            request.Salary,
            request.Period);

        return Results.Ok(new { healthInsurance });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CalculateHealthInsurance")
.WithOpenApi();

// 計算累進稅率所得稅
app.MapPost("/api/tax/calculate-progressive-tax", async (
    CalculateProgressiveTaxRequest request,
    ITaxCalculationService service) =>
{
    try
    {
        var progressiveTax = await service.CalculateProgressiveTaxAsync(
            request.AnnualIncome,
            request.Deductions,
            request.Exemptions);

        return Results.Ok(new { progressiveTax });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
})
.RequireAuthorization()
.WithName("CalculateProgressiveTax")
.WithOpenApi();

// 取得員工扣除額
app.MapGet("/api/tax/employees/{employeeId}/deductions/{year}", async (
    string employeeId,
    int year,
    ITaxCalculationService service) =>
{
    var deductions = await service.GetEmployeeDeductionsAsync(employeeId, year);
    return Results.Ok(new { employeeId, year, deductions });
})
.RequireAuthorization()
.WithName("GetEmployeeDeductions")
.WithOpenApi();

// 取得員工免稅額
app.MapGet("/api/tax/employees/{employeeId}/exemptions/{year}", async (
    string employeeId,
    int year,
    ITaxCalculationService service) =>
{
    var exemptions = await service.GetEmployeeExemptionsAsync(employeeId, year);
    return Results.Ok(new { employeeId, year, exemptions });
})
.RequireAuthorization()
.WithName("GetEmployeeExemptions")
.WithOpenApi();

// 取得累進稅率表
app.MapGet("/api/tax/progressive-tax-brackets/{year}", async (
    int year,
    ITaxCalculationService service) =>
{
    var brackets = await service.GetProgressiveTaxBracketsAsync(year);
    return Results.Ok(brackets);
})
.RequireAuthorization()
.WithName("GetProgressiveTaxBrackets")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


// =============================================
// DTOs
// =============================================

/// <summary>
/// 登入請求
/// </summary>
record LoginRequest(string Username, string Password);

/// <summary>
/// 建立員工請求
/// </summary>
record CreateEmployeeRequest(
    string EmployeeNumber,
    string Name,
    string DepartmentId,
    string? Position,
    decimal MonthlySalary,
    string? BankCode,
    string? BankAccount);

/// <summary>
/// 建立員工請求（支援薪資類型）
/// </summary>
record CreateEmployeeWithSalaryTypeRequest(
    string EmployeeNumber,
    string Name,
    string DepartmentId,
    string? Position,
    string SalaryType, // "Monthly", "Daily", "Hourly"
    decimal? MonthlySalary,
    decimal? DailySalary,
    decimal? HourlySalary,
    string? BankCode,
    string? BankAccount);

/// <summary>
/// 更新員工請求
/// </summary>
record UpdateEmployeeRequest(
    string Name,
    string? Position,
    decimal MonthlySalary,
    string? BankCode,
    string? BankAccount);

/// <summary>
/// 部門調動請求
/// </summary>
record TransferDepartmentRequest(string NewDepartmentId, DateTime TransferDate);

/// <summary>
/// 員工離職請求
/// </summary>
record ResignEmployeeRequest(DateTime ResignationDate);

/// <summary>
/// 建立部門請求
/// </summary>
record CreateDepartmentRequest(
    string Code,
    string Name,
    string? ManagerId,
    string? ParentDepartmentId);

/// <summary>
/// 更新部門請求
/// </summary>
record UpdateDepartmentRequest(
    string Code,
    string Name,
    string? ManagerId,
    string? ParentDepartmentId);

/// <summary>
/// 建立請假請求
/// </summary>
record CreateLeaveRequest(
    string EmployeeId,
    LeaveType Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Days,
    string? ProxyUserId);

/// <summary>
/// 檢查請假重疊請求
/// </summary>
record CheckOverlapRequest(
    string EmployeeId,
    DateTime StartDate,
    DateTime EndDate,
    string? ExcludeLeaveId);

/// <summary>
/// 建立薪資項目定義請求
/// </summary>
record CreateSalaryItemDefinitionRequest(
    string ItemCode,
    string ItemName,
    string Type,
    string CalculationMethod,
    decimal? DefaultAmount,
    decimal? HourlyRate,
    decimal? PercentageRate,
    string? Description,
    DateTime EffectiveDate,
    DateTime? ExpiryDate);

/// <summary>
/// 更新薪資項目定義請求
/// </summary>
record UpdateSalaryItemDefinitionRequest(
    string ItemName,
    string Type,
    string CalculationMethod,
    decimal? DefaultAmount,
    decimal? HourlyRate,
    decimal? PercentageRate,
    string? Description,
    bool IsActive,
    DateTime EffectiveDate,
    DateTime? ExpiryDate);

/// <summary>
/// 建立費率表請求
/// </summary>
record CreateRateTableRequest(
    string Version,
    DateTime EffectiveDate,
    DateTime? ExpiryDate,
    decimal LaborInsuranceRate,
    decimal HealthInsuranceRate,
    string Source);

/// <summary>
/// 更新費率表請求
/// </summary>
record UpdateRateTableRequest(
    string Version,
    DateTime EffectiveDate,
    DateTime? ExpiryDate,
    decimal LaborInsuranceRate,
    decimal HealthInsuranceRate,
    string Source);

/// <summary>
/// 計算薪資請求
/// </summary>
record CalculateSalaryRequest(
    string EmployeeId,
    DateTime Period,
    bool CopyFromPreviousMonth = false);

/// <summary>
/// 計算基本薪資請求
/// </summary>
record CalculateBaseSalaryRequest(
    string EmployeeId,
    DateTime Period,
    decimal WorkDays,
    decimal TotalWorkDays);

/// <summary>
/// 計算加班費請求
/// </summary>
record CalculateOvertimeRequest(
    string EmployeeId,
    DateTime Period,
    decimal OvertimeHours);

/// <summary>
/// 計算所得稅請求
/// </summary>
record CalculateIncomeTaxRequest(
    decimal GrossSalary,
    string EmployeeId,
    DateTime Period);

/// <summary>
/// 計算保險費請求
/// </summary>
record CalculateInsuranceRequest(
    decimal Salary,
    DateTime Period);

/// <summary>
/// 計算累進稅率所得稅請求
/// </summary>
record CalculateProgressiveTaxRequest(
    decimal AnnualIncome,
    decimal Deductions,
    decimal Exemptions);
