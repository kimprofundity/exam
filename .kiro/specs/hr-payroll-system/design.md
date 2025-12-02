# 設計文件

## 概述

人事薪資系統是一個基於 .NET Core 8 和 Vue 3 的全端應用程式，部署於 Linux Docker 環境。系統採用前後端分離架構，後端提供 RESTful API，前端為單頁應用程式（SPA）。系統整合 Active Directory 進行身份驗證，使用 MS SQL Server 作為資料儲存，並支援薪資計算、通知、報表和年度結算等核心功能。

**設計目標：**
- 確保薪資計算的準確性和可追溯性
- 保護敏感薪資資料的安全性
- 提供靈活的權限控制機制
- 支援跨平台部署（Linux Docker）
- 保留未來雲端遷移的彈性

## 架構

### 系統架構圖

```
┌─────────────────────────────────────────────────────────────┐
│                         前端層 (Vue 3)                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ 員工介面 │  │ 管理介面 │  │ 報表介面 │  │ 設定介面 │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │ HTTPS/REST API
┌─────────────────────────────────────────────────────────────┐
│                    API 閘道層 (.NET Core 8)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ 身份驗證     │  │ 授權檢查     │  │ 請求路由     │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                    應用服務層 (.NET Core 8)                   │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ 薪資服務 │  │ 員工服務 │  │ 通知服務 │  │ 報表服務 │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                    領域層 (.NET Core 8)                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │ 薪資計算 │  │ 請假管理 │  │ 權限管理 │  │ 稅務計算 │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                    資料存取層 (.NET Core 8)                   │
│  ┌──────────────────┐  ┌──────────────────┐               │
│  │ Repository 模式  │  │ Entity Framework │               │
│  └──────────────────┘  └──────────────────┘               │
└─────────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────────┐
│                    資料層                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ MS SQL Server│  │ Active Dir.  │  │ 檔案系統     │     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

### 架構模式

**後端架構：**
- 採用分層架構（Layered Architecture）
- 使用 Clean Architecture 原則，確保業務邏輯獨立於基礎設施
- 應用 Repository 模式進行資料存取
- 使用 Dependency Injection 進行依賴管理

**前端架構：**
- 採用元件化架構（Component-Based Architecture）
- 使用 Vuex/Pinia 進行狀態管理
- 使用 Vue Router 進行路由管理
- 採用 Composition API 提升程式碼可維護性



## 元件和介面

### 後端核心元件

#### 1. 身份驗證與授權模組 (AuthenticationModule)

**職責：**
- 整合 Active Directory 進行 LDAP 身份驗證
- 產生和驗證 JWT 令牌
- 管理使用者會話

**介面：**
```csharp
public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(string username, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserInfo> GetUserInfoAsync(string username);
}

public interface ILdapService
{
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<LdapUser> GetUserDetailsAsync(string username);
    Task<List<string>> GetUserGroupsAsync(string username);
}
```

#### 2. 權限管理模組 (AuthorizationModule)

**職責：**
- 管理角色和權限
- 驗證使用者操作權限
- 控制資料存取範圍

**介面：**
```csharp
public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(string userId, string permission);
    Task<DataAccessScope> GetDataAccessScopeAsync(string userId);
    Task<List<Role>> GetUserRolesAsync(string userId);
}

public interface IRoleService
{
    Task<Role> CreateRoleAsync(RoleDto roleDto);
    Task<Role> UpdateRoleAsync(string roleId, RoleDto roleDto);
    Task AssignRoleToUserAsync(string userId, string roleId);
}
```

#### 3. 員工管理模組 (EmployeeModule)

**職責：**
- 管理員工基本資料
- 處理員工部門調動
- 維護員工狀態

**介面：**
```csharp
public interface IEmployeeService
{
    Task<Employee> CreateEmployeeAsync(EmployeeDto employeeDto);
    Task<Employee> UpdateEmployeeAsync(string employeeId, EmployeeDto employeeDto);
    Task<Employee> GetEmployeeAsync(string employeeId);
    Task<List<Employee>> GetEmployeesByDepartmentAsync(string departmentId);
    Task TransferEmployeeAsync(string employeeId, string newDepartmentId);
}
```



#### 4. 薪資計算模組 (PayrollCalculationModule)

**職責：**
- 執行薪資計算邏輯
- 處理月薪和日薪計算
- 計算各項加項和減項
- 計算勞健保和所得稅

**介面：**
```csharp
public interface IPayrollCalculationService
{
    Task<SalaryRecord> CalculateSalaryAsync(string employeeId, DateTime period);
    Task<decimal> CalculateBaseSalaryAsync(Employee employee, AttendanceRecord attendance);
    Task<decimal> CalculateOvertimePayAsync(Employee employee, List<OvertimeRecord> overtimes);
    Task<decimal> CalculateDeductionsAsync(Employee employee, SalaryRecord salary);
}

public interface ITaxCalculationService
{
    Task<decimal> CalculateIncomeTaxAsync(decimal grossSalary, TaxSettings settings);
    Task<decimal> CalculateLaborInsuranceAsync(decimal salary, RateTable rateTable);
    Task<decimal> CalculateHealthInsuranceAsync(decimal salary, RateTable rateTable);
}
```

#### 5. 請假管理模組 (LeaveManagementModule)

**職責：**
- 處理請假申請
- 管理代理請假流程
- 計算假期額度
- 驗證請假規則

**介面：**
```csharp
public interface ILeaveService
{
    Task<LeaveRecord> CreateLeaveRequestAsync(LeaveRequestDto request);
    Task<LeaveRecord> CreateProxyLeaveRequestAsync(string proxyUserId, LeaveRequestDto request);
    Task<LeaveRecord> ApproveProxyLeaveAsync(string leaveId, string employeeId);
    Task RejectProxyLeaveAsync(string leaveId, string employeeId, string reason);
    Task<AttendanceRecord> GetAttendanceRecordAsync(string employeeId, DateTime period);
}
```

#### 6. 薪資通知模組 (NotificationModule)

**職責：**
- 發送薪資通知郵件
- 產生加密附件
- 管理通知範本
- 處理發送失敗重試

**介面：**
```csharp
public interface INotificationService
{
    Task SendSalaryNotificationAsync(string employeeId, SalaryRecord salary);
    Task<byte[]> GenerateEncryptedAttachmentAsync(SalaryRecord salary, string password);
    Task<bool> SendEmailAsync(EmailMessage message);
}

public interface IEmailService
{
    Task<bool> SendAsync(string to, string subject, string body, List<Attachment> attachments);
    Task<EmailTemplate> GetTemplateAsync(string templateName);
}
```



#### 7. 報表產生模組 (ReportGenerationModule)

**職責：**
- 產生各類薪資報表
- 支援多種匯出格式（PDF、Excel、CSV）
- 產生年度報表和扣繳憑單

**介面：**
```csharp
public interface IReportService
{
    Task<byte[]> GenerateSalaryReportAsync(ReportCriteria criteria, ReportFormat format);
    Task<byte[]> GenerateAnnualReportAsync(int year, string departmentId);
    Task<byte[]> GenerateWithholdingStatementAsync(string employeeId, int year);
}

public interface IExportService
{
    Task<byte[]> ExportToPdfAsync(object data, string templateName);
    Task<byte[]> ExportToExcelAsync(object data);
    Task<byte[]> ExportToCsvAsync(object data);
}
```

#### 8. 銀行轉帳檔案產生模組 (BankTransferModule)

**職責：**
- 產生銀行轉帳檔案
- 驗證銀行帳號格式
- 支援多種銀行格式

**介面：**
```csharp
public interface IBankTransferService
{
    Task<byte[]> GenerateTransferFileAsync(List<SalaryRecord> salaries, BankFormat format);
    Task<bool> ValidateBankAccountAsync(string bankCode, string accountNumber);
    Task<TransferBatch> CreateTransferBatchAsync(DateTime period);
}
```

#### 9. 年度作業模組 (YearEndModule)

**職責：**
- 執行年度結算
- 產生年度統計報表
- 封存歷史資料
- 重置年度累計數據

**介面：**
```csharp
public interface IYearEndService
{
    Task<YearEndResult> ExecuteYearEndClosingAsync(int year);
    Task<AnnualSummary> CalculateAnnualSummaryAsync(string employeeId, int year);
    Task ArchiveYearDataAsync(int year);
    Task ResetAnnualCountersAsync(int newYear);
}
```

#### 10. 系統設定模組 (SystemConfigurationModule)

**職責：**
- 管理系統參數
- 維護工作日曆
- 管理費率表版本

**介面：**
```csharp
public interface ISystemConfigService
{
    Task<SystemParameter> GetParameterAsync(string key, DateTime effectiveDate);
    Task UpdateParameterAsync(string key, object value, DateTime effectiveDate);
    Task<WorkCalendar> GetWorkCalendarAsync(int year);
    Task<RateTable> GetEffectiveRateTableAsync(DateTime date);
}
```



## 資料模型

### 核心實體

#### Employee (員工)
```csharp
public class Employee
{
    /// <summary>員工唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>員工編號</summary>
    public string EmployeeNumber { get; set; }
    
    /// <summary>員工姓名</summary>
    public string Name { get; set; }
    
    /// <summary>所屬部門識別碼</summary>
    public string DepartmentId { get; set; }
    
    /// <summary>職位</summary>
    public string Position { get; set; }
    
    /// <summary>固定月薪金額</summary>
    public decimal MonthlySalary { get; set; }
    
    /// <summary>銀行代碼</summary>
    public string BankCode { get; set; }
    
    /// <summary>銀行帳號（加密儲存）</summary>
    public string BankAccount { get; set; }
    
    /// <summary>員工狀態（在職/離職）</summary>
    public EmployeeStatus Status { get; set; } // Active, Resigned
    
    /// <summary>離職日期</summary>
    public DateTime? ResignationDate { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    /// <summary>所屬部門</summary>
    public Department Department { get; set; }
    
    /// <summary>薪資記錄列表</summary>
    public List<SalaryRecord> SalaryRecords { get; set; }
    
    /// <summary>請假記錄列表</summary>
    public List<LeaveRecord> LeaveRecords { get; set; }
}
```

#### Department (部門)
```csharp
public class Department
{
    /// <summary>部門唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>部門代碼</summary>
    public string Code { get; set; }
    
    /// <summary>部門名稱</summary>
    public string Name { get; set; }
    
    /// <summary>部門主管員工識別碼</summary>
    public string ManagerId { get; set; }
    
    /// <summary>上級部門識別碼</summary>
    public string ParentDepartmentId { get; set; }
    
    /// <summary>是否啟用</summary>
    public bool IsActive { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    /// <summary>部門主管</summary>
    public Employee Manager { get; set; }
    
    /// <summary>上級部門</summary>
    public Department ParentDepartment { get; set; }
    
    /// <summary>下級部門列表</summary>
    public List<Department> SubDepartments { get; set; }
    
    /// <summary>部門員工列表</summary>
    public List<Employee> Employees { get; set; }
}
```

#### SalaryRecord (薪資記錄)
```csharp
public class SalaryRecord
{
    /// <summary>薪資記錄唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>員工識別碼</summary>
    public string EmployeeId { get; set; }
    
    /// <summary>薪資期間（年月）</summary>
    public DateTime Period { get; set; }
    
    /// <summary>基本薪資</summary>
    public decimal BaseSalary { get; set; }
    
    /// <summary>加項總額</summary>
    public decimal TotalAdditions { get; set; }
    
    /// <summary>減項總額</summary>
    public decimal TotalDeductions { get; set; }
    
    /// <summary>應發薪資（加密儲存）</summary>
    public decimal GrossSalary { get; set; }
    
    /// <summary>實發薪資（加密儲存）</summary>
    public decimal NetSalary { get; set; }
    
    /// <summary>使用的費率表版本</summary>
    public string RateTableVersion { get; set; }
    
    /// <summary>薪資狀態（草稿/已核准/已發放）</summary>
    public SalaryStatus Status { get; set; } // Draft, Approved, Paid
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>建立者識別碼</summary>
    public string CreatedBy { get; set; }
    
    // 導航屬性
    /// <summary>員工</summary>
    public Employee Employee { get; set; }
    
    /// <summary>薪資項目列表</summary>
    public List<SalaryItem> SalaryItems { get; set; }
    
    /// <summary>扣除項目列表</summary>
    public List<SalaryDeduction> Deductions { get; set; }
}
```

#### SalaryItem (薪資項目)
```csharp
public class SalaryItem
{
    /// <summary>薪資項目唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>所屬薪資記錄識別碼</summary>
    public string SalaryRecordId { get; set; }
    
    /// <summary>項目代碼</summary>
    public string ItemCode { get; set; }
    
    /// <summary>項目名稱</summary>
    public string ItemName { get; set; }
    
    /// <summary>項目類型（加項/減項）</summary>
    public SalaryItemType Type { get; set; } // Addition, Deduction
    
    /// <summary>金額</summary>
    public decimal Amount { get; set; }
    
    /// <summary>說明</summary>
    public string Description { get; set; }
    
    // 導航屬性
    /// <summary>所屬薪資記錄</summary>
    public SalaryRecord SalaryRecord { get; set; }
}
```



#### LeaveRecord (請假記錄)
```csharp
public class LeaveRecord
{
    /// <summary>請假記錄唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>員工識別碼</summary>
    public string EmployeeId { get; set; }
    
    /// <summary>請假類型（事假/病假/特休）</summary>
    public LeaveType Type { get; set; } // Personal, Sick, Annual
    
    /// <summary>開始日期</summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>結束日期</summary>
    public DateTime EndDate { get; set; }
    
    /// <summary>請假天數</summary>
    public decimal Days { get; set; }
    
    /// <summary>請假狀態（待審核/已核准/已拒絕）</summary>
    public LeaveStatus Status { get; set; } // Pending, Approved, Rejected
    
    /// <summary>代理人使用者識別碼</summary>
    public string ProxyUserId { get; set; }
    
    /// <summary>是否為代理請假</summary>
    public bool IsProxyRequest { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    // 導航屬性
    /// <summary>員工</summary>
    public Employee Employee { get; set; }
    
    /// <summary>代理人</summary>
    public User ProxyUser { get; set; }
}
```

#### Role (角色)
```csharp
public class Role
{
    /// <summary>角色唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>角色代碼</summary>
    public string Code { get; set; }
    
    /// <summary>角色名稱</summary>
    public string Name { get; set; }
    
    /// <summary>角色描述</summary>
    public string Description { get; set; }
    
    /// <summary>資料存取範圍（僅自己/本部門/全公司）</summary>
    public DataAccessScope DataAccessScope { get; set; } // Self, Department, Company
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>更新時間</summary>
    public DateTime UpdatedAt { get; set; }
    
    // 導航屬性
    /// <summary>角色權限列表</summary>
    public List<RolePermission> Permissions { get; set; }
    
    /// <summary>使用者角色關聯列表</summary>
    public List<UserRole> UserRoles { get; set; }
}
```

#### RateTable (費率表)
```csharp
public class RateTable
{
    /// <summary>費率表唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>版本號</summary>
    public string Version { get; set; }
    
    /// <summary>生效日期</summary>
    public DateTime EffectiveDate { get; set; }
    
    /// <summary>失效日期</summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>勞保費率</summary>
    public decimal LaborInsuranceRate { get; set; }
    
    /// <summary>健保費率</summary>
    public decimal HealthInsuranceRate { get; set; }
    
    /// <summary>資料來源（手動/API/檔案）</summary>
    public string Source { get; set; } // Manual, API, File
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>建立者識別碼</summary>
    public string CreatedBy { get; set; }
}
```

#### SystemParameter (系統參數)
```csharp
public class SystemParameter
{
    /// <summary>系統參數唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>參數鍵值</summary>
    public string Key { get; set; }
    
    /// <summary>參數值</summary>
    public string Value { get; set; }
    
    /// <summary>資料類型</summary>
    public string DataType { get; set; }
    
    /// <summary>生效日期</summary>
    public DateTime EffectiveDate { get; set; }
    
    /// <summary>失效日期</summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>參數說明</summary>
    public string Description { get; set; }
    
    /// <summary>建立時間</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>建立者識別碼</summary>
    public string CreatedBy { get; set; }
}
```

#### AuditLog (稽核日誌)
```csharp
public class AuditLog
{
    /// <summary>稽核日誌唯一識別碼</summary>
    public string Id { get; set; }
    
    /// <summary>使用者識別碼</summary>
    public string UserId { get; set; }
    
    /// <summary>操作動作</summary>
    public string Action { get; set; }
    
    /// <summary>實體類型</summary>
    public string EntityType { get; set; }
    
    /// <summary>實體識別碼</summary>
    public string EntityId { get; set; }
    
    /// <summary>修改前的值（JSON 格式）</summary>
    public string OldValue { get; set; }
    
    /// <summary>修改後的值（JSON 格式）</summary>
    public string NewValue { get; set; }
    
    /// <summary>操作時間戳記</summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>來源 IP 位址</summary>
    public string IpAddress { get; set; }
}
```

### 資料庫設計考量

**加密欄位：**
- Employee.BankAccount - 使用 AES-256 加密
- SalaryRecord.NetSalary - 使用 AES-256 加密
- SalaryRecord.GrossSalary - 使用 AES-256 加密

**索引策略：**
- Employee.EmployeeNumber - 唯一索引
- Employee.DepartmentId - 非唯一索引
- SalaryRecord.EmployeeId + Period - 複合唯一索引
- LeaveRecord.EmployeeId + StartDate - 複合索引
- AuditLog.Timestamp - 非唯一索引（用於日誌查詢）

**分區策略：**
- SalaryRecord 按年度分區，提升查詢效能
- AuditLog 按月份分區，便於歷史資料管理



## 正確性屬性

*屬性是一個特徵或行為，應該在系統的所有有效執行中保持為真——本質上是關於系統應該做什麼的正式陳述。屬性作為人類可讀規範和機器可驗證正確性保證之間的橋樑。*

### 薪資通知屬性

**屬性 1：通知完整性**
*對於任何*符合條件的員工集合，當觸發薪資通知流程時，所有符合條件的員工都應該收到通知
**驗證：需求 1.1**

**屬性 2：附件加密一致性**
*對於任何*包含薪資明細附件的通知，該附件應該被加密保護
**驗證：需求 1.6**

**屬性 3：密碼產生正確性**
*對於任何*員工的加密附件，使用該員工的身份證字號或員工編號應該能成功解密附件
**驗證：需求 1.7**

### 銀行轉帳屬性

**屬性 4：帳號驗證完整性**
*對於任何*轉帳檔案產生請求，所有無效的銀行帳號都應該被識別並拒絕
**驗證：需求 2.2**

**屬性 5：檔案格式一致性**
*對於任何*產生的轉帳檔案，解析該檔案應該能正確還原所有薪資記錄資訊
**驗證：需求 2.3**

### 資料安全屬性

**屬性 6：敏感資料加密**
*對於任何*儲存的薪資記錄，直接查詢資料庫時敏感欄位（銀行帳號、薪資金額）應該是加密狀態
**驗證：需求 5.2**

### 部門管理屬性

**屬性 7：部門停用安全性**
*對於任何*部門，只有當該部門沒有在職員工時，才能被成功停用
**驗證：需求 6.4**

### 權限控制屬性

**屬性 8：功能存取控制**
*對於任何*一般員工角色的使用者，嘗試存取管理功能應該被拒絕並記錄
**驗證：需求 7.5**

**屬性 9：資料範圍過濾**
*對於任何*使用者的資料查詢，回傳的結果應該只包含該使用者資料存取範圍內的資料
**驗證：需求 7.7**



### 請假管理屬性

**屬性 10：請假日期不重疊**
*對於任何*員工的新請假申請，如果與現有請假記錄日期重疊，應該被拒絕
**驗證：需求 9.4**

**屬性 11：代理請假記錄完整性**
*對於任何*被確認的代理請假，正式建立的請假記錄應該包含代理人資訊
**驗證：需求 9.6**

### 薪資計算屬性

**屬性 12：上月資料載入**
*對於任何*當月薪資計算，如果上個月有薪資記錄，該記錄應該被載入作為參考
**驗證：需求 12.1**

**屬性 13：薪資計算正確性**
*對於任何*員工，計算的薪資應該等於固定月薪乘以實際出勤天數與預設工作天數的比例
**驗證：需求 12.2**

**屬性 14：薪資項目複製一致性**
*對於任何*選擇帶入上月資料的薪資計算，當月薪資項目應該包含上月所有的加項和減項
**驗證：需求 12.8**

**屬性 15：費率表版本正確性**
*對於任何*薪資計算，使用的費率表版本應該是薪資期間生效的版本
**驗證：需求 12.9**

### 異常偵測屬性

**屬性 16：薪資異常變動偵測**
*對於任何*員工的當月薪資，如果與上月相比變動超過 30%，應該被標記為異常
**驗證：需求 17.1**

**屬性 17：重複帳號偵測**
*對於任何*員工資料集合，如果存在重複的銀行帳號，應該發出警示
**驗證：需求 17.2**

### 稅務計算屬性

**屬性 18：累進稅率計算正確性**
*對於任何*薪資金額，計算的所得稅應該符合累進稅率表的級距和稅率
**驗證：需求 18.1**

### 年度作業屬性

**屬性 19：年度累計重置**
*對於任何*年度結算作業，執行後所有年度累計數據（如年假額度、累計所得）應該被重置
**驗證：需求 19.3**

**屬性 20：已結算資料不可修改**
*對於任何*已結算年度的薪資記錄，嘗試修改應該被拒絕
**驗證：需求 19.7**



## 錯誤處理

### 錯誤分類

**1. 業務邏輯錯誤 (Business Logic Errors)**
- 請假日期重疊
- 部門有在職員工無法停用
- 薪資計算結果異常
- 處理方式：回傳明確的錯誤訊息，記錄日誌，不重試

**2. 驗證錯誤 (Validation Errors)**
- 無效的銀行帳號格式
- 必填欄位缺失
- 資料格式不正確
- 處理方式：回傳驗證錯誤詳情，HTTP 400 狀態碼

**3. 權限錯誤 (Authorization Errors)**
- 未授權的功能存取
- 超出資料存取範圍
- 處理方式：記錄嘗試，回傳 HTTP 403 狀態碼

**4. 系統錯誤 (System Errors)**
- 資料庫連線失敗
- AD 伺服器無法連接
- 檔案系統錯誤
- 處理方式：記錄完整錯誤堆疊，回傳 HTTP 500 狀態碼，通知管理員

**5. 外部服務錯誤 (External Service Errors)**
- 郵件發送失敗
- 政府 API 無法存取
- 處理方式：實施重試機制（最多 3 次），記錄失敗原因

### 錯誤處理策略

**全域異常處理器：**
```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async Task<IResult> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException => (400, exception.Message),
            UnauthorizedException => (403, "Access denied"),
            NotFoundException => (404, "Resource not found"),
            BusinessLogicException => (422, exception.Message),
            _ => (500, "Internal server error")
        };
        
        await LogExceptionAsync(exception, httpContext);
        
        return Results.Problem(
            statusCode: statusCode,
            title: message,
            detail: exception.Message
        );
    }
}
```

**重試策略（使用 Polly）：**
```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            _logger.LogWarning(
                "Retry {RetryCount} after {Delay}s due to {Exception}",
                retryCount, timeSpan.TotalSeconds, exception.Message);
        });
```



## 測試策略

### 測試方法

系統採用雙重測試方法，結合單元測試和屬性基礎測試（Property-Based Testing）：

**單元測試：**
- 驗證特定範例和邊界情況
- 測試整合點
- 覆蓋錯誤處理路徑

**屬性基礎測試：**
- 驗證應該在所有輸入中保持的通用屬性
- 使用隨機產生的測試資料
- 提供更廣泛的測試覆蓋

### 屬性基礎測試框架

**選擇的框架：** FsCheck for .NET

**配置：**
- 每個屬性測試執行最少 100 次迭代
- 使用自訂產生器確保測試資料的有效性
- 每個屬性測試必須明確標記對應的設計文件屬性

**標記格式：**
```csharp
// Feature: hr-payroll-system, Property 13: 月薪計算正確性
[Property(MaxTest = 100)]
public Property MonthlySalaryCalculationCorrectness()
{
    return Prop.ForAll(
        GenerateMonthlyEmployee(),
        GenerateAttendanceRecord(),
        (employee, attendance) =>
        {
            var result = _salaryCalculator.Calculate(employee, attendance);
            var expected = employee.MonthlySalary * 
                          (attendance.WorkDays / _defaultWorkDays);
            return Math.Abs(result - expected) < 0.01m;
        });
}
```

### 測試資料產生器

**員工產生器：**
```csharp
public static Arbitrary<Employee> GenerateEmployee()
{
    return Arb.From(
        from salary in Gen.Choose(30000, 100000)
        from employeeNumber in Gen.Choose(1000, 9999)
        select new Employee
        {
            EmployeeNumber = $"EMP{employeeNumber}",
            MonthlySalary = salary,
            Status = EmployeeStatus.Active
        });
}
```

**薪資記錄產生器：**
```csharp
public static Arbitrary<SalaryRecord> GenerateSalaryRecord()
{
    return Arb.From(
        from baseSalary in Gen.Choose(20000, 150000)
        from additions in Gen.Choose(0, 20000)
        from deductions in Gen.Choose(0, 10000)
        select new SalaryRecord
        {
            BaseSalary = baseSalary,
            TotalAdditions = additions,
            TotalDeductions = deductions,
            GrossSalary = baseSalary + additions,
            NetSalary = baseSalary + additions - deductions
        });
}
```

### 測試覆蓋目標

**程式碼覆蓋率：**
- 業務邏輯層：> 90%
- 服務層：> 85%
- API 控制器：> 80%

**屬性測試覆蓋：**
- 每個正確性屬性必須有對應的屬性基礎測試
- 關鍵計算邏輯（薪資、稅務）必須有多個屬性測試

**整合測試：**
- API 端點測試
- 資料庫整合測試
- AD 整合測試（使用測試 LDAP 伺服器）

### 測試環境

**開發環境：**
- 使用 Docker Compose 建立本地測試環境
- 包含 MS SQL Server、測試 LDAP 伺服器
- 使用測試資料種子

**CI/CD 環境：**
- 自動執行所有單元測試和屬性測試
- 程式碼覆蓋率報告
- 測試失敗時阻止部署

