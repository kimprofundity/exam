-- =============================================
-- 人事薪資系統 - 補充缺少的外鍵關聯
-- =============================================

USE [HRPayrollSystem]
GO

-- =============================================
-- 1. UserRoles 與 Employees 的關聯
-- =============================================
-- 假設 UserId 對應到 Employees.Id
ALTER TABLE [dbo].[UserRoles]
ADD CONSTRAINT [FK_UserRoles_Employee] 
FOREIGN KEY ([UserId]) 
REFERENCES [dbo].[Employees]([Id]);
GO

-- =============================================
-- 2. LeaveRecords 與 ProxyUser 的關聯
-- =============================================
-- ProxyUserId 也對應到 Employees.Id
ALTER TABLE [dbo].[LeaveRecords]
ADD CONSTRAINT [FK_LeaveRecords_ProxyUser] 
FOREIGN KEY ([ProxyUserId]) 
REFERENCES [dbo].[Employees]([Id]);
GO

-- =============================================
-- 3. SalaryRecords 與 RateTables 的關聯
-- =============================================
ALTER TABLE [dbo].[SalaryRecords]
ADD CONSTRAINT [FK_SalaryRecords_RateTable] 
FOREIGN KEY ([RateTableVersion]) 
REFERENCES [dbo].[RateTables]([Version]);
GO

-- =============================================
-- 4. SalaryRecords 與 CreatedBy (Employee) 的關聯
-- =============================================
ALTER TABLE [dbo].[SalaryRecords]
ADD CONSTRAINT [FK_SalaryRecords_CreatedBy] 
FOREIGN KEY ([CreatedBy]) 
REFERENCES [dbo].[Employees]([Id]);
GO

-- =============================================
-- 5. SalaryItems 與 SalaryItemDefinitions 的關聯
-- =============================================
ALTER TABLE [dbo].[SalaryItems]
ADD CONSTRAINT [FK_SalaryItems_Definition] 
FOREIGN KEY ([ItemCode]) 
REFERENCES [dbo].[SalaryItemDefinitions]([ItemCode]);
GO

-- =============================================
-- 6. RateTables 與 CreatedBy (Employee) 的關聯
-- =============================================
ALTER TABLE [dbo].[RateTables]
ADD CONSTRAINT [FK_RateTables_CreatedBy] 
FOREIGN KEY ([CreatedBy]) 
REFERENCES [dbo].[Employees]([Id]);
GO

-- =============================================
-- 7. SystemParameters 與 CreatedBy (Employee) 的關聯
-- =============================================
ALTER TABLE [dbo].[SystemParameters]
ADD CONSTRAINT [FK_SystemParameters_CreatedBy] 
FOREIGN KEY ([CreatedBy]) 
REFERENCES [dbo].[Employees]([Id]);
GO

-- =============================================
-- 8. AuditLogs 與 UserId (Employee) 的關聯
-- =============================================
ALTER TABLE [dbo].[AuditLogs]
ADD CONSTRAINT [FK_AuditLogs_User] 
FOREIGN KEY ([UserId]) 
REFERENCES [dbo].[Employees]([Id]);
GO

-- =============================================
-- 9. TransferBatches 與 GeneratedBy (Employee) 的關聯
-- =============================================
ALTER TABLE [dbo].[TransferBatches]
ADD CONSTRAINT [FK_TransferBatches_GeneratedBy] 
FOREIGN KEY ([GeneratedBy]) 
REFERENCES [dbo].[Employees]([Id]);
GO

-- =============================================
-- 10. AnomalyAlerts 與 AcknowledgedBy (Employee) 的關聯
-- =============================================
ALTER TABLE [dbo].[AnomalyAlerts]
ADD CONSTRAINT [FK_AnomalyAlerts_AcknowledgedBy] 
FOREIGN KEY ([AcknowledgedBy]) 
REFERENCES [dbo].[Employees]([Id]);
GO

PRINT '外鍵關聯補充完成！'
GO
