# Entity Framework Core Scaffold 腳本
# 此腳本會從現有資料庫產生 Entity 類別

Write-Host "開始 Entity Framework Core Scaffold..." -ForegroundColor Green

# 從 appsettings.json 讀取連線字串（簡化版本，實際使用時請確認連線字串）
$connectionString = "Server=localhost;Database=HRPayrollSystem;Trusted_Connection=True;TrustServerCertificate=True;"

Write-Host "連線字串: $connectionString" -ForegroundColor Yellow

# 執行 Scaffold 指令
Write-Host "正在產生 Entity 類別..." -ForegroundColor Cyan

dotnet ef dbcontext scaffold $connectionString `
    Microsoft.EntityFrameworkCore.SqlServer `
    --output-dir Models `
    --context HRPayrollContext `
    --context-dir Data `
    --force `
    --no-onconfiguring `
    --data-annotations

if ($LASTEXITCODE -eq 0) {
    Write-Host "Scaffold 完成！" -ForegroundColor Green
    Write-Host "Entity 類別已產生至 Models 目錄" -ForegroundColor Green
    Write-Host "DbContext 已更新至 Data 目錄" -ForegroundColor Green
} else {
    Write-Host "Scaffold 失敗！請檢查資料庫連線和錯誤訊息。" -ForegroundColor Red
    Write-Host "請確認：" -ForegroundColor Yellow
    Write-Host "1. SQL Server 正在運行" -ForegroundColor Yellow
    Write-Host "2. 資料庫 HRPayrollSystem 已建立" -ForegroundColor Yellow
    Write-Host "3. 連線字串正確" -ForegroundColor Yellow
}

Write-Host "`n按任意鍵繼續..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
