# 人事薪資系統 (HR Payroll System)

基於 .NET Core 8 和 Vue 3 的全端人事薪資管理系統，部署於 Linux Docker 環境。

## 技術堆疊

### 後端
- .NET Core 8 Web API
- Entity Framework Core
- MS SQL Server
- JWT 身份驗證
- LDAP/Active Directory 整合

### 前端
- Vue 3 (Composition API)
- TypeScript
- Vue Router
- Pinia (狀態管理)
- Axios

### 部署
- Docker & Docker Compose
- Linux 容器環境
- Nginx (前端伺服器)

## 功能特色

- ✅ 薪資計算與管理
- ✅ 銀行轉帳檔案產生
- ✅ Active Directory 整合
- ✅ 員工薪資查詢介面
- ✅ 請假管理
- ✅ 勞健保費率管理
- ✅ 報表產生與匯出
- ✅ 年度作業與結算
- ✅ 權限角色管理
- ✅ 稽核日誌

## 快速開始

### 前置需求

- .NET 8.0 SDK
- Node.js 20+
- Docker & Docker Compose
- MS SQL Server (或使用 Docker)

### 本地開發

1. **複製專案**
```bash
git clone <repository-url>
cd hr-payroll-system
```

2. **設定環境變數**
```bash
cp .env.example .env
# 編輯 .env 檔案，設定資料庫連線和其他配置
```

3. **建立資料庫**
```bash
# 執行 SQL 腳本
cd Backend/SQL
# 依序執行：
# 00_CreateDatabase.sql
# 01_CreateTables.sql
# 03_AddMissingRelations.sql
# 02_SeedData.sql
```

4. **啟動後端**
```bash
cd Backend/HRPayrollSystem.API
dotnet restore
dotnet run
```

5. **啟動前端**
```bash
cd Frontend/HRPayrollSystem.Web
npm install
npm run dev
```

### Docker 部署

```bash
# 啟動所有服務
docker-compose up -d

# 查看日誌
docker-compose logs -f

# 停止服務
docker-compose down
```

## 專案結構

```
.
├── Backend/
│   ├── HRPayrollSystem.API/          # Web API 專案
│   └── SQL/                           # 資料庫腳本
├── Frontend/
│   └── HRPayrollSystem.Web/          # Vue 3 前端專案
├── .kiro/
│   ├── specs/                         # 規格文件
│   └── steering/                      # 專案指導文件
├── docker-compose.yml                 # Docker Compose 配置
└── README.md
```

## API 文件

啟動後端後，訪問 Swagger UI：
- 開發環境：http://localhost:5000/swagger

## 開發指南

詳細的開發指南請參考：
- [需求文件](.kiro/specs/hr-payroll-system/requirements.md)
- [設計文件](.kiro/specs/hr-payroll-system/design.md)
- [任務清單](.kiro/specs/hr-payroll-system/tasks.md)
- [資料庫結構](Backend/SQL/DatabaseSchema.md)

## 授權

Copyright © 2024. All rights reserved.
