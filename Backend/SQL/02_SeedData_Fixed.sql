-- =============================================
-- 人事薪資系統 - 初始資料腳本（修正版）
-- =============================================

USE [HRPayrollSystem]
GO

-- =============================================
-- 1. 插入預設角色
-- =============================================
INSERT INTO [dbo].[Roles] ([Id], [Code], [Name], [Description], [DataAccessScope])
VALUES 
    (NEWID(), 'ADMIN', N'系統管理員', N'擁有完整系統存取權限', 'Company'),
    (NEWID(), 'HR', N'人資管理員', N'管理員工資料和薪資計算', 'Company'),
    (NEWID(), 'FINANCE', N'財務人員', N'處理薪資發放和報表', 'Company'),
    (NEWID(), 'MANAGER', N'部門主管', N'查看本部門員工薪資', 'Department'),
    (NEWID(), 'EMPLOYEE', N'一般員工', N'查看自己的薪資資料', 'Self');
GO

-- =============================================
-- 2. 插入預設系統參數
-- =============================================
INSERT INTO [dbo].[SystemParameters] ([Id], [Key], [Value], [DataType], [EffectiveDate], [Description], [CreatedBy])
VALUES 
    (NEWID(), 'DefaultWorkDays', '22', 'Int', GETUTCDATE(), N'每月預設工作天數', 'SYSTEM'),
    (NEWID(), 'OvertimeRateWeekday', '1.34', 'Decimal', GETUTCDATE(), N'平日加班費率倍數', 'SYSTEM'),
    (NEWID(), 'OvertimeRateWeekend', '1.67', 'Decimal', GETUTCDATE(), N'假日加班費率倍數', 'SYSTEM'),
    (NEWID(), 'OvertimeRateHoliday', '2.0', 'Decimal', GETUTCDATE(), N'國定假日加班費率倍數', 'SYSTEM'),
    (NEWID(), 'SalaryAnomalyThreshold', '0.3', 'Decimal', GETUTCDATE(), N'薪資異常變動門檻（30%）', 'SYSTEM'),
    (NEWID(), 'NotificationRetryLimit', '3', 'Int', GETUTCDATE(), N'通知發送重試次數上限', 'SYSTEM'),
    (NEWID(), 'EmailServer', 'smtp.example.com', 'String', GETUTCDATE(), N'郵件伺服器位址', 'SYSTEM'),
    (NEWID(), 'EmailPort', '587', 'Int', GETUTCDATE(), N'郵件伺服器埠號', 'SYSTEM');
GO

-- =============================================
-- 3. 插入預設薪資項目定義
-- =============================================
INSERT INTO [dbo].[SalaryItemDefinitions] ([Id], [ItemCode], [ItemName], [Type], [CalculationMethod], [EffectiveDate])
VALUES 
    (NEWID(), 'BASE', N'基本薪資', 'Addition', 'Fixed', GETUTCDATE()),
    (NEWID(), 'OT_WD', N'平日加班費', 'Addition', 'Hourly', GETUTCDATE()),
    (NEWID(), 'OT_WE', N'假日加班費', 'Addition', 'Hourly', GETUTCDATE()),
    (NEWID(), 'OT_HOL', N'國定假日加班費', 'Addition', 'Hourly', GETUTCDATE()),
    (NEWID(), 'MEAL', N'伙食津貼', 'Addition', 'Fixed', GETUTCDATE()),
    (NEWID(), 'TRANS', N'交通津貼', 'Addition', 'Fixed', GETUTCDATE()),
    (NEWID(), 'LABOR', N'勞保費', 'Deduction', 'Percentage', GETUTCDATE()),
    (NEWID(), 'HEALTH', N'健保費', 'Deduction', 'Percentage', GETUTCDATE()),
    (NEWID(), 'TAX', N'所得稅', 'Deduction', 'Percentage', GETUTCDATE()),
    (NEWID(), 'LEAVE', N'請假扣款', 'Deduction', 'Fixed', GETUTCDATE()),
    (NEWID(), 'MEAL_FEE', N'代收餐費', 'Deduction', 'Fixed', GETUTCDATE()),
    (NEWID(), 'PARKING', N'代收停車費', 'Deduction', 'Fixed', GETUTCDATE());
GO

-- =============================================
-- 4. 插入初始費率表
-- =============================================
INSERT INTO [dbo].[RateTables] ([Id], [Version], [EffectiveDate], [LaborInsuranceRate], [HealthInsuranceRate], [Source], [CreatedBy])
VALUES 
    (NEWID(), '2024-01', '2024-01-01', 0.115, 0.0517, 'Manual', 'SYSTEM');
GO

PRINT '初始資料插入完成！'
GO
