-- 添加薪資類型欄位到 Employees 表

-- 添加薪資類型欄位
ALTER TABLE Employees 
ADD SalaryType NVARCHAR(20) NOT NULL DEFAULT 'Monthly';

-- 添加日薪欄位
ALTER TABLE Employees 
ADD DailySalary DECIMAL(18,2) NULL;

-- 添加時薪欄位
ALTER TABLE Employees 
ADD HourlySalary DECIMAL(18,2) NULL;

-- 添加約束檢查
ALTER TABLE Employees 
ADD CONSTRAINT CK_Employees_SalaryType 
CHECK (SalaryType IN ('Monthly', 'Daily', 'Hourly'));

-- 添加約束：月薪類型必須有月薪金額
ALTER TABLE Employees 
ADD CONSTRAINT CK_Employees_MonthlySalary 
CHECK (
    (SalaryType = 'Monthly' AND MonthlySalary > 0) OR
    (SalaryType != 'Monthly')
);

-- 添加約束：日薪類型必須有日薪金額
ALTER TABLE Employees 
ADD CONSTRAINT CK_Employees_DailySalary 
CHECK (
    (SalaryType = 'Daily' AND DailySalary > 0) OR
    (SalaryType != 'Daily')
);

-- 添加約束：時薪類型必須有時薪金額
ALTER TABLE Employees 
ADD CONSTRAINT CK_Employees_HourlySalary 
CHECK (
    (SalaryType = 'Hourly' AND HourlySalary > 0) OR
    (SalaryType != 'Hourly')
);

-- 更新現有員工為月薪類型（如果還沒設定）
UPDATE Employees 
SET SalaryType = 'Monthly' 
WHERE SalaryType IS NULL OR SalaryType = '';

PRINT '薪資類型欄位添加完成';