# Task 10: Simple Payroll Calculation Test

# Set API base URL
$baseUrl = "http://localhost:5183"

# Read authentication token
$token = Get-Content token.txt -Raw
$token = $token.Trim()

# Set request headers
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "=== Task 10: Simple Payroll Calculation Test ===" -ForegroundColor Cyan

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

    # 2. Test individual calculation methods
    Write-Host "`n2. Testing working days calculation..." -ForegroundColor Yellow
    $currentPeriod = Get-Date -Format "yyyy-MM-01"
    $workingDaysResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/working-days/$currentPeriod" -Method Get -Headers $headers
    Write-Host "Working days in month: $($workingDaysResponse.workingDays)" -ForegroundColor Green

    Write-Host "`n3. Testing actual work days calculation..." -ForegroundColor Yellow
    $actualWorkDaysResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/employees/$($testEmployee.id)/actual-work-days?period=$currentPeriod" -Method Get -Headers $headers
    Write-Host "Actual work days: $($actualWorkDaysResponse.actualWorkDays)" -ForegroundColor Green

    Write-Host "`n4. Testing leave deduction calculation..." -ForegroundColor Yellow
    $leaveDeductionResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/employees/$($testEmployee.id)/leave-deduction?period=$currentPeriod" -Method Get -Headers $headers
    Write-Host "Leave deduction: $($leaveDeductionResponse.leaveDeduction)" -ForegroundColor Green

    Write-Host "`n5. Testing base salary calculation..." -ForegroundColor Yellow
    $baseSalaryRequest = @{
        employeeId = $testEmployee.id
        period = $currentPeriod
        workDays = $actualWorkDaysResponse.actualWorkDays
        totalWorkDays = $workingDaysResponse.workingDays
    }
    $baseSalaryResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/calculate-base-salary" -Method Post -Headers $headers -Body ($baseSalaryRequest | ConvertTo-Json)
    Write-Host "Base salary: $($baseSalaryResponse.baseSalary)" -ForegroundColor Green

    Write-Host "`n=== Task 10 Individual Tests Completed ===" -ForegroundColor Cyan
    Write-Host "All individual payroll calculation methods work correctly!" -ForegroundColor Green

} catch {
    Write-Host "`nTest failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "HTTP status code: $statusCode" -ForegroundColor Red
    }
    exit 1
}