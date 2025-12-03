# 資料庫設定指南

## 前置需求

1. **SQL Server**：需要 MS SQL Server（本地安裝或 Docker 容器）
2. **連線權限**：確保有建立資料庫的權限

## 設定步驟

### 選項 1：使用本地 SQL Server

1. 確認 SQL Server 服務正在運行
2. 更新 `appsettings.json` 中的連線字串（如需要）
3. 執行資料庫腳本

### 選項 2：使用 Docker SQL Server

1. 啟動 SQL Server 容器：
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

2. 更新 `appsettings.json` 連線字串：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=HRPayrollSystem;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;"
  }
}
```

## 執行資料庫腳本

依序執行以下 SQL 腳本（位於 `Backend/SQL/` 目錄）：

### 1. 建立資料庫
```bash
sqlcmd -S localhost -U sa -P YourStrong@Password -i 00_CreateDatabase.sql
```

### 2. 建立資料表
```bash
sqlcmd -S localhost -U sa -P YourStrong@Password -i 01_CreateTables.sql
```

### 3. 補充外鍵關聯
```bash
sqlcmd -S localhost -U sa -P YourStrong@Password -i 03_AddMissingRelations.sql
```

### 4. 插入初始資料
```bash
sqlcmd -S localhost -U sa -P YourStrong@Password -i 02_SeedData.sql
```

## 使用 Entity Framework Core Scaffold

資料庫建立完成後，執行以下指令產生 Entity 類別：

```bash
cd Backend/HRPayrollSystem.API
dotnet ef dbcontext scaffold "Server=localhost;Database=HRPayrollSystem;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c HRPayrollContext --context-dir Data --force
```

## 驗證設定

執行以下指令測試資料庫連線：

```bash
cd Backend/HRPayrollSystem.API
dotnet ef database update
```

## 常見問題

### 連線失敗
- 確認 SQL Server 服務正在運行
- 檢查防火牆設定
- 驗證連線字串正確性

### 權限錯誤
- 確認使用者有建立資料庫的權限
- 檢查 SQL Server 驗證模式（混合模式）

### Docker 容器問題
- 確認 Docker Desktop 正在運行
- 檢查容器狀態：`docker ps`
- 查看容器日誌：`docker logs sqlserver`
