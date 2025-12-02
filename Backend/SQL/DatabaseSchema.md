# 資料庫結構關聯圖

## 資料表關聯總覽

```
Departments (部門)
├── FK_Departments_Manager → Employees (部門主管)
├── FK_Departments_Parent → Departments (上級部門)
└── ← FK_Employees_Department (部門員工)

Employees (員工)
├── FK_Employees_Department → Departments (所屬部門)
├── ← FK_Departments_Manager (擔任部門主管)
├── ← FK_UserRoles_Employee (使用者角色)
├── ← FK_LeaveRecords_Employee (請假記錄)
├── ← FK_LeaveRecords_ProxyUser (代理請假)
├── ← FK_SalaryRecords_Employee (薪資記錄)
├── ← FK_SalaryRecords_CreatedBy (建立薪資記錄)
├── ← FK_NotificationLogs_Employee (通知記錄)
├── ← FK_AuditLogs_User (稽核日誌)
├── ← FK_RateTables_CreatedBy (建立費率表)
├── ← FK_SystemParameters_CreatedBy (建立系統參數)
├── ← FK_TransferBatches_GeneratedBy (產生轉帳批次)
└── ← FK_AnomalyAlerts_AcknowledgedBy (確認異常警示)

Roles (角色)
├── ← FK_UserRoles_Role (使用者角色關聯)
└── ← FK_RolePermissions_Role (角色權限)

UserRoles (使用者角色關聯)
├── FK_UserRoles_Employee → Employees (使用者)
└── FK_UserRoles_Role → Roles (角色)

RolePermissions (角色權限)
└── FK_RolePermissions_Role → Roles (角色)

LeaveRecords (請假記錄)
├── FK_LeaveRecords_Employee → Employees (員工)
└── FK_LeaveRecords_ProxyUser → Employees (代理人)

SalaryRecords (薪資記錄)
├── FK_SalaryRecords_Employee → Employees (員工)
├── FK_SalaryRecords_RateTable → RateTables (費率表版本)
├── FK_SalaryRecords_CreatedBy → Employees (建立者)
├── ← FK_SalaryItems_SalaryRecord (薪資項目明細)
└── ← FK_NotificationLogs_SalaryRecord (通知記錄)

SalaryItems (薪資項目明細)
├── FK_SalaryItems_SalaryRecord → SalaryRecords (薪資記錄)
└── FK_SalaryItems_Definition → SalaryItemDefinitions (項目定義)

SalaryItemDefinitions (薪資項目定義)
└── ← FK_SalaryItems_Definition (薪資項目明細)

RateTables (費率表)
├── FK_RateTables_CreatedBy → Employees (建立者)
└── ← FK_SalaryRecords_RateTable (薪資記錄)

SystemParameters (系統參數)
└── FK_SystemParameters_CreatedBy → Employees (建立者)

AuditLogs (稽核日誌)
└── FK_AuditLogs_User → Employees (操作使用者)

TransferBatches (銀行轉帳批次)
└── FK_TransferBatches_GeneratedBy → Employees (產生者)

NotificationLogs (通知記錄)
├── FK_NotificationLogs_Employee → Employees (員工)
└── FK_NotificationLogs_SalaryRecord → SalaryRecords (薪資記錄)

AnomalyAlerts (異常警示)
└── FK_AnomalyAlerts_AcknowledgedBy → Employees (確認者)
```

## 核心關聯說明

### 1. 組織架構關聯
- **Departments ↔ Employees**: 部門與員工的雙向關聯
  - 一個部門有多個員工
  - 一個員工屬於一個部門
  - 一個部門有一個主管（也是員工）

- **Departments ↔ Departments**: 部門階層關聯
  - 一個部門可以有上級部門
  - 一個部門可以有多個下級部門

### 2. 權限管理關聯
- **Employees → UserRoles → Roles**: 使用者角色關聯
  - 一個員工可以有多個角色
  - 一個角色可以指派給多個員工
  - 透過 UserRoles 中介表實現多對多關聯

- **Roles → RolePermissions**: 角色權限關聯
  - 一個角色有多個權限
  - 權限以字串形式儲存

### 3. 薪資核心關聯
- **Employees → SalaryRecords**: 員工薪資記錄
  - 一個員工有多筆薪資記錄（按月）
  - 每筆薪資記錄屬於一個員工
  - 唯一約束：(EmployeeId, Period)

- **SalaryRecords → SalaryItems**: 薪資明細
  - 一筆薪資記錄有多個薪資項目
  - 級聯刪除：刪除薪資記錄時自動刪除相關項目

- **SalaryItems → SalaryItemDefinitions**: 項目定義
  - 薪資項目參照項目定義
  - 確保項目代碼的一致性

- **SalaryRecords → RateTables**: 費率表版本
  - 每筆薪資記錄使用特定版本的費率表
  - 保證歷史薪資計算的可追溯性

### 4. 請假管理關聯
- **Employees → LeaveRecords**: 員工請假記錄
  - 一個員工有多筆請假記錄
  - 支援代理請假（ProxyUserId）

### 5. 通知與稽核關聯
- **Employees → NotificationLogs**: 通知記錄
  - 記錄發送給員工的所有通知
  - 可關聯到特定薪資記錄

- **Employees → AuditLogs**: 稽核日誌
  - 記錄員工的所有操作
  - 用於安全稽核和問題追查

### 6. 建立者追蹤
多個資料表都有 CreatedBy 欄位關聯到 Employees：
- SalaryRecords (建立薪資記錄的人)
- RateTables (建立費率表的人)
- SystemParameters (建立系統參數的人)
- TransferBatches (產生轉帳批次的人)

## 關聯類型統計

| 關聯類型 | 數量 | 說明 |
|---------|------|------|
| 一對多 (1:N) | 12 | 如 Employees → SalaryRecords |
| 多對多 (M:N) | 1 | Employees ↔ Roles (透過 UserRoles) |
| 自我關聯 | 1 | Departments → Departments |
| 循環關聯 | 1 | Departments ↔ Employees (部門主管) |

## 級聯刪除設定

| 父資料表 | 子資料表 | 級聯動作 |
|---------|---------|---------|
| SalaryRecords | SalaryItems | CASCADE (級聯刪除) |
| 其他 | - | NO ACTION (預設) |

## 注意事項

1. **循環關聯處理**
   - Departments.ManagerId 和 Employees.DepartmentId 形成循環
   - 建議先建立部門，再指派主管

2. **軟刪除建議**
   - Employees 建議使用軟刪除（Status = 'Resigned'）
   - 避免因刪除員工導致歷史資料遺失

3. **效能考量**
   - 所有外鍵欄位都已建立索引
   - 複合唯一索引：SalaryRecords (EmployeeId + Period)

4. **資料完整性**
   - 所有關聯都使用外鍵約束
   - 確保參照完整性
