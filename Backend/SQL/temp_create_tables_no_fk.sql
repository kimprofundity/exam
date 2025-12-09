-- 臨時腳本：先建立表結構，不含循環外鍵
USE [HRPayrollSystem]
GO

-- 1. 部門資料表（不含 ManagerId 外鍵）
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

-- 2. 員工資料表（不含 DepartmentId 外鍵）
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

-- 現在補上循環外鍵
ALTER TABLE [dbo].[Departments]
ADD CONSTRAINT [FK_Departments_Manager] FOREIGN KEY ([ManagerId]) REFERENCES [dbo].[Employees]([Id]);
GO

ALTER TABLE [dbo].[Employees]
ADD CONSTRAINT [FK_Employees_Department] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments]([Id]);
GO

PRINT '部門和員工表建立完成'
GO
