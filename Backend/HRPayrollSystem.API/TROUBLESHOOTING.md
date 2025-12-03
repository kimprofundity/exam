# 疑難排解指南

## 常見問題

### 問題：編譯失敗 - 檔案被其他程序使用

**錯誤訊息：**
```
error MSB3027: 無法將 "apphost.exe" 複製到 "HRPayrollSystem.API.exe"。
檔案鎖定者: "HRPayrollSystem.API (6408)"
```

**原因：**
應用程式正在運行中（可能在背景執行），導致執行檔被鎖定，無法重新編譯。

**解決方案：**

#### 方法 1：停止正在運行的應用程式
```powershell
# 查找正在運行的 dotnet 程序
Get-Process | Where-Object {$_.ProcessName -like "*HRPayrollSystem*"}

# 停止特定程序（使用 PID）
Stop-Process -Id 6408 -Force

# 或停止所有相關的 dotnet 程序
Get-Process | Where-Object {$_.ProcessName -eq "HRPayrollSystem.API"} | Stop-Process -Force
```

#### 方法 2：使用工作管理員
1. 開啟工作管理員（Ctrl + Shift + Esc）
2. 找到 `HRPayrollSystem.API.exe` 或 `dotnet.exe`
3. 右鍵點擊 → 結束工作

#### 方法 3：重新啟動終端機
如果上述方法無效，關閉所有終端機視窗並重新開啟。

### 問題：資料庫連線失敗

**錯誤訊息：**
```
Sqlcmd: Error: Microsoft ODBC Driver 17 for SQL Server : 無法開啟 SQL Server 連線
```

**原因：**
本地沒有 SQL Server 運行。

**解決方案：**

#### 選項 A：使用 Docker SQL Server（推薦）
```powershell
# 啟動 SQL Server 容器
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

# 檢查容器狀態
docker ps

# 查看容器日誌
docker logs sqlserver
```

#### 選項 B：安裝本地 SQL Server
1. 下載 SQL Server Express：https://www.microsoft.com/sql-server/sql-server-downloads
2. 安裝並啟動 SQL Server 服務
3. 確認服務正在運行

### 問題：套件還原失敗

**解決方案：**
```powershell
# 清除 NuGet 快取
dotnet nuget locals all --clear

# 重新還原套件
dotnet restore

# 重新編譯
dotnet build
```

## 驗證任務2完成狀態

### 檢查清單

#### ✅ 已完成項目

1. **套件安裝**
   ```powershell
   # 檢查專案檔案
   cat HRPayrollSystem.API.csproj
   ```
   應該包含：
   - Microsoft.EntityFrameworkCore.Design (8.0.0)
   - Microsoft.EntityFrameworkCore.SqlServer (8.0.0)
   - Microsoft.EntityFrameworkCore.Tools (8.0.0)

2. **DbContext 建立**
   ```powershell
   # 檢查檔案是否存在
   Test-Path Data/HRPayrollContext.cs
   ```
   應該回傳 `True`

3. **Program.cs 配置**
   ```powershell
   # 檢查 Program.cs 內容
   Select-String -Path Program.cs -Pattern "AddDbContext"
   ```
   應該找到 DbContext 註冊程式碼

4. **程式碼診斷**
   ```powershell
   # 檢查是否有編譯錯誤（需要停止運行中的應用程式）
   dotnet build --no-incremental
   ```
   應該成功編譯（如果沒有程序鎖定檔案）

#### ⏳ 待執行項目（需要資料庫環境）

5. **資料庫設定**
   - 啟動 SQL Server
   - 執行資料庫腳本

6. **Scaffold Entity 類別**
   - 執行 scaffold-database.ps1
   - 或手動執行 dotnet ef dbcontext scaffold

7. **驗證資料庫連線**
   - 啟動應用程式
   - 測試 /health 端點

## 快速驗證腳本

建立一個 PowerShell 腳本來快速驗證任務2的狀態：

```powershell
# verify-task2.ps1

Write-Host "=== 任務2驗證腳本 ===" -ForegroundColor Cyan

# 1. 檢查套件
Write-Host "`n1. 檢查 NuGet 套件..." -ForegroundColor Yellow
$packages = @(
    "Microsoft.EntityFrameworkCore.Design",
    "Microsoft.EntityFrameworkCore.SqlServer",
    "Microsoft.EntityFrameworkCore.Tools"
)

foreach ($pkg in $packages) {
    if (Select-String -Path "HRPayrollSystem.API.csproj" -Pattern $pkg -Quiet) {
        Write-Host "  ✓ $pkg" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $pkg" -ForegroundColor Red
    }
}

# 2. 檢查檔案
Write-Host "`n2. 檢查必要檔案..." -ForegroundColor Yellow
$files = @(
    "Data/HRPayrollContext.cs",
    "Program.cs",
    "DATABASE_SETUP.md",
    "scaffold-database.ps1",
    "TASK2_README.md"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file" -ForegroundColor Red
    }
}

# 3. 檢查 Program.cs 配置
Write-Host "`n3. 檢查 Program.cs 配置..." -ForegroundColor Yellow
if (Select-String -Path "Program.cs" -Pattern "AddDbContext" -Quiet) {
    Write-Host "  ✓ DbContext 已註冊" -ForegroundColor Green
} else {
    Write-Host "  ✗ DbContext 未註冊" -ForegroundColor Red
}

if (Select-String -Path "Program.cs" -Pattern "/health" -Quiet) {
    Write-Host "  ✓ 健康檢查端點已建立" -ForegroundColor Green
} else {
    Write-Host "  ✗ 健康檢查端點未建立" -ForegroundColor Red
}

# 4. 檢查是否有程序鎖定
Write-Host "`n4. 檢查程序狀態..." -ForegroundColor Yellow
$process = Get-Process | Where-Object {$_.ProcessName -like "*HRPayrollSystem*"}
if ($process) {
    Write-Host "  ⚠ 警告：應用程式正在運行中 (PID: $($process.Id))" -ForegroundColor Yellow
    Write-Host "    請先停止應用程式再進行編譯" -ForegroundColor Yellow
} else {
    Write-Host "  ✓ 沒有程序鎖定" -ForegroundColor Green
}

# 5. 檢查程式碼診斷
Write-Host "`n5. 檢查程式碼..." -ForegroundColor Yellow
if (-not $process) {
    Write-Host "  正在檢查編譯錯誤..." -ForegroundColor Gray
    $buildResult = dotnet build --no-incremental 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ 程式碼編譯成功" -ForegroundColor Green
    } else {
        Write-Host "  ✗ 程式碼編譯失敗" -ForegroundColor Red
    }
} else {
    Write-Host "  ⊘ 跳過（應用程式正在運行）" -ForegroundColor Gray
}

Write-Host "`n=== 驗證完成 ===" -ForegroundColor Cyan
Write-Host "`n下一步：" -ForegroundColor Yellow
Write-Host "1. 如果應用程式正在運行，請先停止" -ForegroundColor White
Write-Host "2. 設定 SQL Server（參考 DATABASE_SETUP.md）" -ForegroundColor White
Write-Host "3. 執行資料庫腳本" -ForegroundColor White
Write-Host "4. 執行 Scaffold（參考 TASK2_README.md）" -ForegroundColor White
```

## 使用驗證腳本

```powershell
# 儲存上述腳本為 verify-task2.ps1
# 然後執行：
.\verify-task2.ps1
```

## 總結

**任務2的核心工作已完成：**
- ✅ Entity Framework Core 套件已安裝
- ✅ DbContext 基礎架構已建立
- ✅ Program.cs 已正確配置
- ✅ 健康檢查端點已建立
- ✅ 輔助文件和腳本已準備

**目前的錯誤不是程式碼問題，而是：**
- 應用程式正在背景運行，導致檔案被鎖定
- 需要停止應用程式後才能重新編譯

**後續步驟需要：**
- SQL Server 環境（本地或 Docker）
- 執行資料庫腳本
- 執行 Scaffold 產生 Entity 類別
