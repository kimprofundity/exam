-- =============================================
-- 人事薪資系統 - 資料表建立腳本
-- Database First Approach
-- =============================================

USE [HRPayrollSystem]
GO

-- =============================================
-- 1. 部門資料表
-- =============================================
CREATE TABLE [dbo].[Departments] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [Code] NVARCHAR(20) NOT NULL UNIQUE,
    [Name] NVARCHAR(100) NOT NULL,
    [ManagerId] NVARCHAR(50) NULL,
    [ParentDepartmentId] NVARCHAR(50) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_Departments_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Employees]([Id]),
    CONSTRAINT [FK_Departments_Parent] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [dbo].[Departments]([Id])
);
GO

CREATE INDEX [IX_Departments_Code] ON [dbo].[Departments]([Code]);
CREATE INDEX [IX_Departments_ParentId] ON [dbo].[Departments]([ParentDepartmentId]);
GO

-- =============================================
-- 2. 員工資料表
-- =============================================
CREATE TABLE [dbo].[Employees] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [EmployeeNumber] NVARCHAR(20) NOT NULL UNIQUE,
    [Name] NVARCHAR(100) NOT NULL,
    [DepartmentId] NVARCHAR(50) NOT NULL,
    [Position] NVARCHAR(100) NULL,
    [MonthlySalary] DECIMAL(18, 2) NOT NULL,
    [BankCode] NVARCHAR(10) NULL,
    [BankAccount] NVARCHAR(500) NULL, -- 加密儲存
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Active', -- Active, Resigned
    [ResignationDate] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_Employees_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments]([Id])
);
GO

CREATE UNIQUE INDEX [IX_Employees_EmployeeNumber] ON [dbo].[Employees]([EmployeeNumber]);
CREATE INDEX [IX_Employees_DepartmentId] ON [dbo].[Employees]([DepartmentId]);
CREATE INDEX [IX_Employees_Status] ON [dbo].[Employees]([Status]);
GO


-- =============================================
-- 3. 角色資料表
-- =============================================
CREATE TABLE [dbo].[Roles] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [Code] NVARCHAR(20) NOT NULL UNIQUE,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [DataAccessScope] NVARCHAR(20) NOT NULL DEFAULT 'Self', -- Self, Department, Company
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE INDEX [IX_Roles_Code] ON [dbo].[Roles]([Code]);
GO

-- =============================================
-- 4. 使用者角色關聯表
-- =============================================
CREATE TABLE [dbo].[UserRoles] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(50) NOT NULL,
    [RoleId] NVARCHAR(50) NOT NULL,
    [EffectiveDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ExpiryDate] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_UserRoles_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id])
);
GO

CREATE INDEX [IX_UserRoles_UserId] ON [dbo].[UserRoles]([UserId]);
CREATE INDEX [IX_UserRoles_RoleId] ON [dbo].[UserRoles]([RoleId]);
GO

-- =============================================
-- 5. 角色權限表
-- =============================================
CREATE TABLE [dbo].[RolePermissions] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [RoleId] NVARCHAR(50) NOT NULL,
    [Permission] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id])
);
GO

CREATE INDEX [IX_RolePermissions_RoleId] ON [dbo].[RolePermissions]([RoleId]);
GO

-- =============================================
-- 6. 請假記錄表
-- =============================================
CREATE TABLE [dbo].[LeaveRecords] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [EmployeeId] NVARCHAR(50) NOT NULL,
    [Type] NVARCHAR(20) NOT NULL, -- Personal, Sick, Annual
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NOT NULL,
    [Days] DECIMAL(5, 2) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Approved, Rejected
    [ProxyUserId] NVARCHAR(50) NULL,
    [IsProxyRequest] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_LeaveRecords_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id])
);
GO

CREATE INDEX [IX_LeaveRecords_EmployeeId] ON [dbo].[LeaveRecords]([EmployeeId]);
CREATE INDEX [IX_LeaveRecords_StartDate] ON [dbo].[LeaveRecords]([StartDate]);
CREATE INDEX [IX_LeaveRecords_Status] ON [dbo].[LeaveRecords]([Status]);
GO


-- =============================================
-- 7. 薪資記錄表（按年度分區）
-- =============================================
CREATE TABLE [dbo].[SalaryRecords] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [EmployeeId] NVARCHAR(50) NOT NULL,
    [Period] DATE NOT NULL, -- 薪資期間（年月）
    [BaseSalary] DECIMAL(18, 2) NOT NULL,
    [TotalAdditions] DECIMAL(18, 2) NOT NULL DEFAULT 0,
    [TotalDeductions] DECIMAL(18, 2) NOT NULL DEFAULT 0,
    [GrossSalary] VARBINARY(500) NOT NULL, -- 加密儲存
    [NetSalary] VARBINARY(500) NOT NULL, -- 加密儲存
    [RateTableVersion] NVARCHAR(50) NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft, Approved, Paid
    [IsYearEndClosed] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(50) NOT NULL,
    CONSTRAINT [FK_SalaryRecords_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id])
);
GO

CREATE UNIQUE INDEX [IX_SalaryRecords_EmployeeId_Period] ON [dbo].[SalaryRecords]([EmployeeId], [Period]);
CREATE INDEX [IX_SalaryRecords_Period] ON [dbo].[SalaryRecords]([Period]);
CREATE INDEX [IX_SalaryRecords_Status] ON [dbo].[SalaryRecords]([Status]);
GO

-- =============================================
-- 8. 薪資項目表
-- =============================================
CREATE TABLE [dbo].[SalaryItems] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [SalaryRecordId] NVARCHAR(50) NOT NULL,
    [ItemCode] NVARCHAR(20) NOT NULL,
    [ItemName] NVARCHAR(100) NOT NULL,
    [Type] NVARCHAR(20) NOT NULL, -- Addition, Deduction
    [Amount] DECIMAL(18, 2) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    CONSTRAINT [FK_SalaryItems_SalaryRecord] FOREIGN KEY ([SalaryRecordId]) REFERENCES [dbo].[SalaryRecords]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_SalaryItems_SalaryRecordId] ON [dbo].[SalaryItems]([SalaryRecordId]);
CREATE INDEX [IX_SalaryItems_ItemCode] ON [dbo].[SalaryItems]([ItemCode]);
GO

-- =============================================
-- 9. 薪資項目定義表
-- =============================================
CREATE TABLE [dbo].[SalaryItemDefinitions] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [ItemCode] NVARCHAR(20) NOT NULL UNIQUE,
    [ItemName] NVARCHAR(100) NOT NULL,
    [Type] NVARCHAR(20) NOT NULL, -- Addition, Deduction
    [CalculationMethod] NVARCHAR(50) NULL, -- Fixed, Hourly, Percentage
    [IsActive] BIT NOT NULL DEFAULT 1,
    [EffectiveDate] DATETIME2 NOT NULL,
    [ExpiryDate] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE INDEX [IX_SalaryItemDefinitions_ItemCode] ON [dbo].[SalaryItemDefinitions]([ItemCode]);
GO


-- =============================================
-- 10. 費率表
-- =============================================
CREATE TABLE [dbo].[RateTables] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [Version] NVARCHAR(20) NOT NULL UNIQUE,
    [EffectiveDate] DATE NOT NULL,
    [ExpiryDate] DATE NULL,
    [LaborInsuranceRate] DECIMAL(10, 6) NOT NULL,
    [HealthInsuranceRate] DECIMAL(10, 6) NOT NULL,
    [Source] NVARCHAR(20) NOT NULL, -- Manual, API, File
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(50) NOT NULL
);
GO

CREATE INDEX [IX_RateTables_EffectiveDate] ON [dbo].[RateTables]([EffectiveDate]);
CREATE INDEX [IX_RateTables_Version] ON [dbo].[RateTables]([Version]);
GO

-- =============================================
-- 11. 系統參數表
-- =============================================
CREATE TABLE [dbo].[SystemParameters] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL,
    [Value] NVARCHAR(MAX) NOT NULL,
    [DataType] NVARCHAR(20) NOT NULL, -- String, Int, Decimal, Boolean, Json
    [EffectiveDate] DATETIME2 NOT NULL,
    [ExpiryDate] DATETIME2 NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(50) NOT NULL
);
GO

CREATE INDEX [IX_SystemParameters_Key] ON [dbo].[SystemParameters]([Key]);
CREATE INDEX [IX_SystemParameters_EffectiveDate] ON [dbo].[SystemParameters]([EffectiveDate]);
GO

-- =============================================
-- 12. 稽核日誌表（按月份分區）
-- =============================================
CREATE TABLE [dbo].[AuditLogs] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(50) NOT NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EntityId] NVARCHAR(50) NULL,
    [OldValue] NVARCHAR(MAX) NULL,
    [NewValue] NVARCHAR(MAX) NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IpAddress] NVARCHAR(50) NULL
);
GO

CREATE INDEX [IX_AuditLogs_UserId] ON [dbo].[AuditLogs]([UserId]);
CREATE INDEX [IX_AuditLogs_Timestamp] ON [dbo].[AuditLogs]([Timestamp]);
CREATE INDEX [IX_AuditLogs_EntityType] ON [dbo].[AuditLogs]([EntityType]);
GO

-- =============================================
-- 13. 銀行轉帳批次表
-- =============================================
CREATE TABLE [dbo].[TransferBatches] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [Period] DATE NOT NULL,
    [TotalAmount] DECIMAL(18, 2) NOT NULL,
    [TotalCount] INT NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Generated, Completed
    [FileName] NVARCHAR(255) NULL,
    [GeneratedAt] DATETIME2 NULL,
    [GeneratedBy] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE INDEX [IX_TransferBatches_Period] ON [dbo].[TransferBatches]([Period]);
CREATE INDEX [IX_TransferBatches_Status] ON [dbo].[TransferBatches]([Status]);
GO

-- =============================================
-- 14. 通知記錄表
-- =============================================
CREATE TABLE [dbo].[NotificationLogs] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [EmployeeId] NVARCHAR(50) NOT NULL,
    [SalaryRecordId] NVARCHAR(50) NULL,
    [Type] NVARCHAR(20) NOT NULL, -- Email, SMS, System
    [Status] NVARCHAR(20) NOT NULL, -- Pending, Sent, Failed
    [RetryCount] INT NOT NULL DEFAULT 0,
    [SentAt] DATETIME2 NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_NotificationLogs_Employee] FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees]([Id]),
    CONSTRAINT [FK_NotificationLogs_SalaryRecord] FOREIGN KEY ([SalaryRecordId]) REFERENCES [dbo].[SalaryRecords]([Id])
);
GO

CREATE INDEX [IX_NotificationLogs_EmployeeId] ON [dbo].[NotificationLogs]([EmployeeId]);
CREATE INDEX [IX_NotificationLogs_Status] ON [dbo].[NotificationLogs]([Status]);
CREATE INDEX [IX_NotificationLogs_CreatedAt] ON [dbo].[NotificationLogs]([CreatedAt]);
GO

-- =============================================
-- 15. 異常警示表
-- =============================================
CREATE TABLE [dbo].[AnomalyAlerts] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [Type] NVARCHAR(50) NOT NULL, -- SalaryVariation, DuplicateAccount, InvalidAmount
    [Severity] NVARCHAR(20) NOT NULL, -- Low, Medium, High
    [EntityType] NVARCHAR(100) NOT NULL,
    [EntityId] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Acknowledged, Resolved
    [AcknowledgedBy] NVARCHAR(50) NULL,
    [AcknowledgedAt] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE INDEX [IX_AnomalyAlerts_Status] ON [dbo].[AnomalyAlerts]([Status]);
CREATE INDEX [IX_AnomalyAlerts_Type] ON [dbo].[AnomalyAlerts]([Type]);
CREATE INDEX [IX_AnomalyAlerts_CreatedAt] ON [dbo].[AnomalyAlerts]([CreatedAt]);
GO

PRINT '資料表建立完成！'
GO
