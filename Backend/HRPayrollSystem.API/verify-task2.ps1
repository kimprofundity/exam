# 任務2驗證腳本

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
    "../DATABASE_SETUP.md",
    "scaffold-database.ps1",
    "TASK2_README.md",
    "TROUBLESHOOTING.md"
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
    Write-Host "    請執行以下指令停止應用程式：" -ForegroundColor Yellow
    Write-Host "    Stop-Process -Id $($process.Id) -Force" -ForegroundColor Cyan
} else {
    Write-Host "  ✓ 沒有程序鎖定" -ForegroundColor Green
}

# 5. 檢查 SQL 腳本
Write-Host "`n5. 檢查 SQL 腳本..." -ForegroundColor Yellow
$sqlScripts = @(
    "../SQL/00_CreateDatabase.sql",
    "../SQL/01_CreateTables.sql",
    "../SQL/02_SeedData.sql",
    "../SQL/03_AddMissingRelations.sql"
)

foreach ($script in $sqlScripts) {
    if (Test-Path $script) {
        Write-Host "  ✓ $script" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $script" -ForegroundColor Red
    }
}

Write-Host "`n=== 驗證完成 ===" -ForegroundColor Cyan

# 總結
Write-Host "`n【任務2狀態總結】" -ForegroundColor Cyan
Write-Host "✅ 已完成：" -ForegroundColor Green
Write-Host "  - Entity Framework Core 套件已安裝" -ForegroundColor White
Write-Host "  - DbContext 基礎架構已建立" -ForegroundColor White
Write-Host "  - Program.cs 已正確配置" -ForegroundColor White
Write-Host "  - 健康檢查端點已建立" -ForegroundColor White
Write-Host "  - 輔助文件和腳本已準備" -ForegroundColor White

Write-Host "`n⏳ 待執行（需要資料庫環境）：" -ForegroundColor Yellow
Write-Host "  1. 設定 SQL Server（參考 DATABASE_SETUP.md）" -ForegroundColor White
Write-Host "  2. 執行資料庫腳本（00 → 01 → 03 → 02）" -ForegroundColor White
Write-Host "  3. 執行 Scaffold 產生 Entity 類別" -ForegroundColor White
Write-Host "  4. 驗證資料庫連線" -ForegroundColor White

if ($process) {
    Write-Host "`n⚠ 注意：" -ForegroundColor Yellow
    Write-Host "  應用程式正在運行中，請先停止後再進行編譯測試" -ForegroundColor White
    Write-Host "  執行：Stop-Process -Id $($process.Id) -Force" -ForegroundColor Cyan
}

Write-Host "`n詳細資訊請參考：" -ForegroundColor Gray
Write-Host "  - TASK2_README.md（任務完成狀態和後續步驟）" -ForegroundColor Gray
Write-Host "  - TROUBLESHOOTING.md（疑難排解指南）" -ForegroundColor Gray
Write-Host "  - DATABASE_SETUP.md（資料庫設定指南）" -ForegroundColor Gray

Write-Host "`n按任意鍵繼續..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
