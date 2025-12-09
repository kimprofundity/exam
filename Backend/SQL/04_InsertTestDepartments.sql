-- =============================================
-- 插入測試部門資料
-- =============================================

USE [HRPayrollSystem]
GO

-- 檢查是否已有部門資料
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments])
BEGIN
    PRINT '插入測試部門資料...'
    
    INSERT INTO [dbo].[Departments] ([Id], [Code], [Name], [IsActive], [CreatedAt], [UpdatedAt])
    VALUES 
        (NEWID(), 'IT', N'資訊部', 1, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'HR', N'人資部', 1, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'FIN', N'財務部', 1, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'SALES', N'業務部', 1, GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'MKT', N'行銷部', 1, GETUTCDATE(), GETUTCDATE());
    
    PRINT '測試部門資料插入完成！'
    
    -- 顯示插入的部門
    SELECT [Id], [Code], [Name], [IsActive] FROM [dbo].[Departments];
END
ELSE
BEGIN
    PRINT '部門資料已存在，跳過插入'
    SELECT [Id], [Code], [Name], [IsActive] FROM [dbo].[Departments];
END
GO
