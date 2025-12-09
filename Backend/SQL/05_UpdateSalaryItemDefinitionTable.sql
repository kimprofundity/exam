-- =============================================
-- 更新薪資項目定義資料表
-- =============================================

USE [HRPayrollSystem]
GO

-- 檢查並新增缺少的欄位
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'SalaryItemDefinitions' AND COLUMN_NAME = 'DefaultAmount')
BEGIN
    ALTER TABLE [dbo].[SalaryItemDefinitions]
    ADD [DefaultAmount] DECIMAL(18, 2) NULL;
    PRINT '新增 DefaultAmount 欄位';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'SalaryItemDefinitions' AND COLUMN_NAME = 'HourlyRate')
BEGIN
    ALTER TABLE [dbo].[SalaryItemDefinitions]
    ADD [HourlyRate] DECIMAL(18, 2) NULL;
    PRINT '新增 HourlyRate 欄位';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'SalaryItemDefinitions' AND COLUMN_NAME = 'PercentageRate')
BEGIN
    ALTER TABLE [dbo].[SalaryItemDefinitions]
    ADD [PercentageRate] DECIMAL(5, 4) NULL;
    PRINT '新增 PercentageRate 欄位';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'SalaryItemDefinitions' AND COLUMN_NAME = 'Description')
BEGIN
    ALTER TABLE [dbo].[SalaryItemDefinitions]
    ADD [Description] NVARCHAR(500) NULL;
    PRINT '新增 Description 欄位';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'SalaryItemDefinitions' AND COLUMN_NAME = 'CreatedBy')
BEGIN
    ALTER TABLE [dbo].[SalaryItemDefinitions]
    ADD [CreatedBy] NVARCHAR(50) NOT NULL DEFAULT 'system';
    PRINT '新增 CreatedBy 欄位';
END

-- 更新 CalculationMethod 欄位為 NOT NULL（如果是 NULL）
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'SalaryItemDefinitions' 
           AND COLUMN_NAME = 'CalculationMethod' 
           AND IS_NULLABLE = 'YES')
BEGIN
    -- 先更新現有的 NULL 值
    UPDATE [dbo].[SalaryItemDefinitions]
    SET [CalculationMethod] = 'Fixed'
    WHERE [CalculationMethod] IS NULL;
    
    -- 修改欄位為 NOT NULL
    ALTER TABLE [dbo].[SalaryItemDefinitions]
    ALTER COLUMN [CalculationMethod] NVARCHAR(50) NOT NULL;
    
    PRINT '更新 CalculationMethod 欄位為 NOT NULL';
END

PRINT '薪資項目定義資料表更新完成！'
GO
