-- =============================================
-- 薪資項目定義資料表建立腳本
-- =============================================

USE [HRPayrollSystem]
GO

-- =============================================
-- 薪資項目定義表
-- =============================================
CREATE TABLE [dbo].[SalaryItemDefinitions] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [ItemCode] NVARCHAR(20) NOT NULL,
    [ItemName] NVARCHAR(100) NOT NULL,
    [Type] NVARCHAR(20) NOT NULL, -- Addition, Deduction
    [CalculationMethod] NVARCHAR(50) NOT NULL, -- Fixed, Hourly, Percentage
    [DefaultAmount] DECIMAL(18, 2) NULL,
    [HourlyRate] DECIMAL(18, 2) NULL,
    [PercentageRate] DECIMAL(5, 4) NULL,
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [EffectiveDate] DATE NOT NULL,
    [ExpiryDate] DATE NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(50) NOT NULL
);
GO

-- 建立索引
CREATE UNIQUE INDEX [IX_SalaryItemDefinitions_ItemCode_EffectiveDate] 
ON [dbo].[SalaryItemDefinitions]([ItemCode], [EffectiveDate]);

CREATE INDEX [IX_SalaryItemDefinitions_IsActive] 
ON [dbo].[SalaryItemDefinitions]([IsActive]);

CREATE INDEX [IX_SalaryItemDefinitions_Type] 
ON [dbo].[SalaryItemDefinitions]([Type]);

CREATE INDEX [IX_SalaryItemDefinitions_EffectiveDate] 
ON [dbo].[SalaryItemDefinitions]([EffectiveDate]);
GO

PRINT '薪資項目定義資料表建立完成！'
GO
