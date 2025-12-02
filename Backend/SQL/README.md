# 資料庫腳本說明

## Database First 方式

本專案採用 Database First 開發方式，先建立資料庫結構，再透過 Entity Framework Core 的 Scaffold 功能產生實體類別。

## 執行順序

請依照以下順序執行 SQL 腳本：

### 1. 建立資料庫
```sql
00_CreateDatabase.sql
```
- 建立 `HRPayrollSystem` 資料庫
- 設定資料庫選項和定序

### 2. 建立資料表
```sql
01_CreateTables.sql
```
- 建立所有資料表
- 建立基本外鍵關聯
- 建立索引

### 3. 補充外鍵關聯
```sql
03_AddMissingRelations.sql
```
- 補充所有資料表之間的外鍵關聯
- 建立完整的參照完整性約束
- **重要**：此腳本必須在建立資料表後執行

### 4. 插入初始資料
```sql
02_SeedData.sql
```
- 插入預設角色
- 插入系統參數
- 插入薪資項目定義
- 插入初始費率表

### 5. 查看資料庫結構
```
DatabaseSchema.md
```
- 完整的資料表關聯圖
- 關聯說明和注意事項

## 資料表說明

### 核心資料表

| 資料表名稱 | 說明 | 重要欄位 |
|-----------|------|---------|
| Departments | 部門資料 | Code, Name, ManagerId |
| Employees | 員工資料 | EmployeeNumber, MonthlySalary, BankAccount (加密) |
| SalaryRecords | 薪資記錄 | Period, GrossSalary (加密), NetSalary (加密) |
| SalaryItems | 薪資項目明細 | ItemCode, Type, Amount |
| LeaveRecords | 請假記錄 | Type, StartDate, EndDate, Days |

### 權限管理

| 資料表名稱 | 說明 |
|-----------|------|
| Roles | 角色定義 |
| UserRoles | 使用者角色關聯 |
| RolePermissions | 角色權限 |

### 系統設定

| 資料表名稱 | 說明 |
|-----------|------|
| SystemParameters | 系統參數 |
| RateTables | 勞健保費率表 |
| SalaryItemDefinitions | 薪資項目定義 |

### 稽核與日誌

| 資料表名稱 | 說明 |
|-----------|------|
| AuditLogs | 稽核日誌 |
| NotificationLogs | 通知記錄 |
| AnomalyAlerts | 異常警示 |

## 加密欄位

以下欄位使用 VARBINARY 儲存加密資料：
- `Employees.BankAccount` - 銀行帳號
- `SalaryRecords.GrossSalary` - 應發薪資
- `SalaryRecords.NetSalary` - 實發薪資

## 索引策略

- **唯一索引**：EmployeeNumber, Department.Code, Role.Code
- **複合索引**：SalaryRecords (EmployeeId + Period)
- **查詢索引**：DepartmentId, Status, Period, Timestamp

## Scaffold 指令

建立資料庫後，使用以下指令產生 Entity Framework Core 實體類別：

```bash
cd Backend
dotnet ef dbcontext scaffold "Server=localhost;Database=HRPayrollSystem;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c HRPayrollContext --force
```

## 注意事項

1. **加密欄位**：BankAccount, GrossSalary, NetSalary 需要在應用層實作加密/解密
2. **分區策略**：SalaryRecords 和 AuditLogs 建議按年度/月份分區（生產環境）
3. **備份策略**：建議每日備份，保留至少 30 天
4. **效能優化**：定期重建索引，更新統計資訊

## 資料庫連線字串範例

### 開發環境
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HRPayrollSystem;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Docker 環境
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=HRPayrollSystem;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;"
  }
}
```
