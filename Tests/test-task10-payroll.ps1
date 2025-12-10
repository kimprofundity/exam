# Task 10: Payroll Calculation Engine Test Script

# Set API base URL
$baseUrl = "http://localhost:5183"

# Read authentication token
$tokenFile = "token.txt"
if (Test-Path $tokenFile) {
    $token = Get-Content $tokenFile -Raw
    $token = $token.Trim()
    Write-Host "Using existing token for testing" -ForegroundColor Green
} else {
    Write-Host "Token file not found, please run login test first" -ForegroundColor Red
    exit 1
}

# Set request headers
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "=== Task 10: Payroll Calculation Engine Test ===" -ForegroundColor Cyan

try {
    # 1. Get employee list
    Write-Host "`n1. Getting employee list..." -ForegroundColor Yellow
    $employeesResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees" -Method Get -Headers $headers
    
    if ($employeesResponse.items -and $employeesResponse.items.Count -gt 0) {
        $testEmployee = $employeesResponse.items[0]
        Write-Host "Selected test employee: $($testEmployee.name) (ID: $($testEmployee.id))" -ForegroundColor Green
    } else {
        Write-Host "No employee data found, cannot proceed with test" -ForegroundColor Red
        exit 1
    }

    # 2. Get working days in month
    Write-Host "`n2. Getting working days in month..." -ForegroundColor Yellow
    $currentPeriod = Get-Date -Format "yyyy-MM-01"
    $workingDaysResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/working-days/$currentPeriod" -Method Get -Headers $headers
    Write-Host "Working days in month: $($workingDaysResponse.workingDays)" -ForegroundColor Green

    # 3. Calculate actual work days
    Write-Host "`n3. Calculating actual work days..." -ForegroundColor Yellow
    $actualWorkDaysResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/employees/$($testEmployee.id)/actual-work-days?period=$currentPeriod" -Method Get -Headers $headers
    Write-Host "Actual work days: $($actualWorkDaysResponse.actualWorkDays)" -ForegroundColor Green

    # 4. Execute full salary calculation
    Write-Host "`n4. Executing full salary calculation..." -ForegroundColor Yellow
    $salaryCalculationRequest = @{
        employeeId = $testEmployee.id
        period = $currentPeriod
        copyFromPreviousMonth = $false
    }
    
    $salaryResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/calculate" -Method Post -Headers $headers -Body ($salaryCalculationRequest | ConvertTo-Json)
    Write-Host "Salary calculation completed! Salary record ID: $($salaryResponse.id)" -ForegroundColor Green

    # 5. Get salary record details
    Write-Host "`n5. Getting salary record details..." -ForegroundColor Yellow
    $salaryRecordResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/salary-records/$($salaryResponse.id)" -Method Get -Headers $headers
    
    Write-Host "Salary record details:" -ForegroundColor Green
    Write-Host "  Employee: $($salaryRecordResponse.employee.name)" -ForegroundColor White
    Write-Host "  Period: $($salaryRecordResponse.period)" -ForegroundColor White
    Write-Host "  Base salary: $($salaryRecordResponse.baseSalary)" -ForegroundColor White
    Write-Host "  Total additions: $($salaryRecordResponse.totalAdditions)" -ForegroundColor White
    Write-Host "  Total deductions: $($salaryRecordResponse.totalDeductions)" -ForegroundColor White
    Write-Host "  Status: $($salaryRecordResponse.status)" -ForegroundColor White

    Write-Host "`n=== Task 10 Test Completed ===" -ForegroundColor Cyan
    Write-Host "Payroll calculation engine implementation successful!" -ForegroundColor Green

} catch {
    Write-Host "`nTest failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "HTTP status code: $statusCode" -ForegroundColor Red
    }
    exit 1
}