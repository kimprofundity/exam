# 任務 2：設定資料庫和 Entity Framework Core

## 已完成項目 ✅

### 1. 安裝必要的 NuGet 套件
- ✅ Microsoft.EntityFrameworkCore.Design (8.0.0)
- ✅ Microsoft.EntityFrameworkCore.SqlServer (8.0.0)
- ✅ Microsoft.EntityFrameworkCore.Tools (8.0.0)

### 2. 建立基礎架構
- ✅ 建立 `HRPayrollContext` 基礎類別（`Data/HRPayrollContext.cs`）
- ✅ 更新 `Program.cs` 註冊 DbContext 服務
- ✅ 配置資料庫連線字串（含重試機制）
- ✅ 建立健康檢查端點 (`/health`)

### 3. 建立輔助文件
- ✅ `DATABASE_SETUP.md` - 資料庫設定完整指南
- ✅ `scaffold-database.ps1` - Scaffold 自動化腳本

## 待執行項目 ⏳

### 需要資料庫環境

以下步驟需要 SQL Server 資料庫環境才能執行：

1. **設定 SQL Server**
   - 選項 A：使用本地 SQL Server
   - 選項 B：使用 Docker SQL Server（推薦）

2. **執行資料庫腳本**
   ```bash
   # 依序執行以下腳本
   cd Backend/SQL
   sqlcmd -S localhost -U sa -P YourPassword -i 00_CreateDatabase.sql
   sqlcmd -S localhost -U sa -P YourPassword -i 01_CreateTables.sql
   sqlcmd -S localhost -U sa -P YourPassword -i 03_AddMissingRelations.sql
   sqlcmd -S localhost -U sa -P YourPassword -i 02_SeedData.sql
   ```

3. **執行 Scaffold 產生 Entity 類別**
   ```bash
   cd Backend/HRPayrollSystem.API
   # 方式 1：使用 PowerShell 腳本
   .\scaffold-database.ps1
   
   # 方式 2：手動執行
   dotnet ef dbcontext scaffold "Server=localhost;Database=HRPayrollSystem;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c HRPayrollContext --context-dir Data --force
   ```

4. **驗證設定**
   ```bash
   # 啟動應用程式
   dotnet run
   
   # 測試健康檢查端點
   curl http://localhost:5000/health
   ```

## 快速開始（使用 Docker）

如果您想快速開始，可以使用以下指令：

```bash
# 1. 啟動 SQL Server Docker 容器
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# 2. 等待容器啟動（約 10-15 秒）
Start-Sleep -Seconds 15

# 3. 執行資料庫腳本
cd Backend/SQL
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Password123" -i 00_CreateDatabase.sql
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Password123" -i 01_CreateTables.sql
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Password123" -i 03_AddMissingRelations.sql
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Password123" -i 02_SeedData.sql

# 4. 更新連線字串（在 appsettings.json）
# "Server=localhost,1433;Database=HRPayrollSystem;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;"

# 5. 執行 Scaffold
cd ../HRPayrollSystem.API
dotnet ef dbcontext scaffold "Server=localhost,1433;Database=HRPayrollSystem;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -c HRPayrollContext --context-dir Data --force

# 6. 啟動應用程式
dotnet run
```

## 專案結構

```
Backend/HRPayrollSystem.API/
├── Data/
│   └── HRPayrollContext.cs          # DbContext 基礎類別
├── Models/                           # Entity 類別（Scaffold 後產生）
├── Program.cs                        # 已配置 DbContext 註冊
├── appsettings.json                  # 包含資料庫連線字串
├── DATABASE_SETUP.md                 # 資料庫設定指南
├── scaffold-database.ps1             # Scaffold 自動化腳本
└── TASK2_README.md                   # 本文件
```

## 配置說明

### DbContext 註冊（Program.cs）

```csharp
builder.Services.AddDbContext<HRPayrollContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    )
);
```

特點：
- 從配置檔讀取連線字串
- 啟用自動重試機制（最多 3 次）
- 每次重試間隔最多 5 秒

### 健康檢查端點

```
GET /health
```

回應範例：
```json
{
  "status": "Healthy",
  "database": "Connected",
  "timestamp": "2024-12-03T10:30:00Z"
}
```

## 注意事項

1. **連線字串安全性**
   - 開發環境：可使用 `appsettings.Development.json`
   - 生產環境：使用環境變數或 Azure Key Vault

2. **Scaffold 選項說明**
   - `--force`: 覆蓋現有檔案
   - `--no-onconfiguring`: 不產生 OnConfiguring 方法（使用 DI）
   - `--data-annotations`: 使用 Data Annotations 而非 Fluent API

3. **資料庫腳本執行順序**
   - 必須按照編號順序執行（00 → 01 → 03 → 02）
   - 03 必須在 02 之前執行（外鍵關聯）

## 疑難排解

### 問題：無法連接到 SQL Server
**解決方案：**
- 確認 SQL Server 服務正在運行
- 檢查防火牆設定
- 驗證連線字串中的伺服器名稱和埠號

### 問題：Scaffold 失敗
**解決方案：**
- 確認資料庫已建立且資料表存在
- 檢查連線字串格式
- 確認有足夠的資料庫權限

### 問題：Docker 容器無法啟動
**解決方案：**
- 確認 Docker Desktop 正在運行
- 檢查埠號 1433 是否被占用
- 查看容器日誌：`docker logs sqlserver`

## 下一步

完成任務 2 後，可以繼續執行：
- **任務 3**：設定 LDAP 整合和身份驗證
- 開始實作核心業務邏輯

## 參考資料

- [Entity Framework Core 文件](https://docs.microsoft.com/ef/core/)
- [SQL Server Docker 映像](https://hub.docker.com/_/microsoft-mssql-server)
- [Database First 開發方式](https://docs.microsoft.com/ef/core/managing-schemas/scaffolding)
