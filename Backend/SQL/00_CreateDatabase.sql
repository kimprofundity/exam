-- =============================================
-- 人事薪資系統 - 資料庫建立腳本
-- =============================================

-- 檢查資料庫是否存在，如果存在則刪除（開發環境用）
-- 生產環境請移除此段
/*
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'HRPayrollSystem')
BEGIN
    ALTER DATABASE [HRPayrollSystem] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [HRPayrollSystem];
END
GO
*/

-- 建立資料庫
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HRPayrollSystem')
BEGIN
    CREATE DATABASE [HRPayrollSystem]
    COLLATE Chinese_Taiwan_Stroke_CI_AS;
END
GO

USE [HRPayrollSystem]
GO

-- 設定資料庫選項
ALTER DATABASE [HRPayrollSystem] SET RECOVERY SIMPLE;
ALTER DATABASE [HRPayrollSystem] SET AUTO_CLOSE OFF;
ALTER DATABASE [HRPayrollSystem] SET AUTO_SHRINK OFF;
GO

PRINT '資料庫建立完成！'
PRINT '請執行 01_CreateTables.sql 建立資料表'
PRINT '然後執行 02_SeedData.sql 插入初始資料'
GO
