# Scaffold database script
$connectionString = "Server=localhost,1433;Database=HRPayrollSystem;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;"

Write-Host "Scaffolding database..." -ForegroundColor Cyan

# Remove old files
if (Test-Path "Models") {
    Remove-Item "Models\*.cs" -Force -ErrorAction SilentlyContinue
}
if (Test-Path "Data\HRPayrollContext.cs") {
    Remove-Item "Data\HRPayrollContext.cs" -Force -ErrorAction SilentlyContinue
}

# Run scaffold
dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer `
    --output-dir Models `
    --context-dir Data `
    --context HRPayrollContext `
    --force `
    --data-annotations `
    --no-onconfiguring

Write-Host "Scaffold completed!" -ForegroundColor Green
