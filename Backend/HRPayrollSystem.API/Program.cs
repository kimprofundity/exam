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
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    )
);

// 註冊服務
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<RoleMappingService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();

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
