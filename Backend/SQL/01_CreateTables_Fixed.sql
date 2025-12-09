-- =============================================
-- 人事薪資系統 - 資料表建立腳本（修正版）
-- 階段 1：建立所有表結構（不含循環外鍵）
-- =============================================

USE [HRPayrollSystem]
GO

-- =============================================
-- 1. 部門資料表（暫不建立 ManagerId 外鍵）
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
    CONSTRAINT [FK_Departments_Parent] FOREIGN KEY ([ParentDepartmentId]) REFERENCES [dbo].[Departments]([Id])
);
GO

CREATE INDEX [IX_Departments_Code] ON [dbo].[Departments]([Code]);
CREATE INDEX [IX_Departments_ParentId] ON [dbo].[Departments]([ParentDepartmentId]);
GO

-- =============================================
-- 2. 員工資料表（暫不建立 DepartmentId 外鍵）
-- =============================================
CREATE TABLE [dbo].[Employees] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [EmployeeNumber] NVARCHAR(20) NOT NULL UNIQUE,
    [Name] NVARCHAR(100) NOT NULL,
    [DepartmentId] NVARCHAR(50) NOT NULL,
    [Position] NVARCHAR(100) NULL,
    [MonthlySalary] DECIMAL(18, 2) NOT NULL,
    [BankCode] NVARCHAR(10) NULL,
    [BankAccount] NVARCHAR(500) NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Active',
    [ResignationDate] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE UNIQUE INDEX [IX_Employees_EmployeeNumber] ON [dbo].[Employees]([EmployeeNumber]);
CREATE INDEX [IX_Employees_DepartmentId] ON [dbo].[Employees]([DepartmentId]);
CREATE INDEX [IX_Employees_Status] ON [dbo].[Employees]([Status]);
GO

-- =============================================
-- 3. 請假記錄表
-- =============================================
CREATE TABLE [dbo].[LeaveRecords] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [EmployeeId] NVARCHAR(50) NOT NULL,
    [Type] NVARCHAR(20) NOT NULL,
    [StartDate] DATE NOT NULL,
    [EndDate] DATE NOT NULL,
    [Days] DECIMAL(5, 2) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
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
-- 4. 薪資記錄表
-- =============================================
CREATE TABLE [dbo].[SalaryRecords] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [EmployeeId] NVARCHAR(50) NOT NULL,
    [Period] DATE NOT NULL,
    [BaseSalary] DECIMAL(18, 2) NOT NULL,
    [TotalAdditions] DECIMAL(18, 2) NOT NULL DEFAULT 0,
    [TotalDeductions] DECIMAL(18, 2) NOT NULL DEFAULT 0,
    [GrossSalary] VARBINARY(500) NOT NULL,
    [NetSalary] VARBINARY(500) NOT NULL,
    [RateTableVersion] NVARCHAR(50) NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Draft',
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
-- 5. 薪資項目表
-- =============================================
CREATE TABLE [dbo].[SalaryItems] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [SalaryRecordId] NVARCHAR(50) NOT NULL,
    [ItemCode] NVARCHAR(20) NOT NULL,
    [ItemName] NVARCHAR(100) NOT NULL,
    [Type] NVARCHAR(20) NOT NULL,
    [Amount] DECIMAL(18, 2) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    CONSTRAINT [FK_SalaryItems_SalaryRecord] FOREIGN KEY ([SalaryRecordId]) REFERENCES [dbo].[SalaryRecords]([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_SalaryItems_SalaryRecordId] ON [dbo].[SalaryItems]([SalaryRecordId]);
CREATE INDEX [IX_SalaryItems_ItemCode] ON [dbo].[SalaryItems]([ItemCode]);
GO

-- =============================================
-- 6. 通知記錄表
-- =============================================
CREATE TABLE [dbo].[NotificationLogs] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [EmployeeId] NVARCHAR(50) NOT NULL,
    [SalaryRecordId] NVARCHAR(50) NULL,
    [Type] NVARCHAR(20) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL,
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

PRINT '階段 1 完成：核心資料表建立完成！'
GO

-- =============================================
-- 階段 2：補上循環外鍵
-- =============================================

-- 補上 Departments.ManagerId 外鍵
ALTER TABLE [dbo].[Departments]
ADD CONSTRAINT [FK_Departments_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Employees]([Id]);
GO

-- 補上 Employees.DepartmentId 外鍵
ALTER TABLE [dbo].[Employees]
ADD CONSTRAINT [FK_Employees_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments]([Id]);
GO

PRINT '階段 2 完成：循環外鍵建立完成！'
PRINT '所有資料表建立完成！'
GO
