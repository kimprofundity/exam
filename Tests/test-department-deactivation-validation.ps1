# Test Department Deactivation Validation
$baseUrl = "http://localhost:5183"
$token = (Get-Content "token.txt" -Raw).Trim()
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "Test: Department Deactivation Validation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Create a new department
Write-Host "`nCreating new department..." -ForegroundColor Yellow
$timestamp = (Get-Date).ToString("HHmmss")
$deptBody = @{
    code = "VAL-$timestamp"
    name = "Validation Test Department"
    managerId = $null
    parentDepartmentId = $null
} | ConvertTo-Json

$dept = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Post -Headers $headers -Body $deptBody
Write-Host "Department created: $($dept.id)" -ForegroundColor Green
$deptId = $dept.id

# Create an employee
Write-Host "`nCreating employee in department..." -ForegroundColor Yellow
$empBody = @{
    employeeNumber = "VAL-$timestamp"
    name = "Validation Test Employee"
    departmentId = $deptId
    position = "Tester"
    monthlySalary = 50000
    bankCode = "001"
    bankAccount = "9999999999"
} | ConvertTo-Json

$empResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees" -Method Post -Headers $headers -Body $empBody
Write-Host "Employee created: $($empResponse.id)" -ForegroundColor Green

# Check employee count
Write-Host "`nChecking employee count..." -ForegroundColor Yellow
$count = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/employee-count" -Method Get -Headers $headers
Write-Host "Active employees: $($count.activeEmployeeCount)" -ForegroundColor Gray

# Try to deactivate (should fail)
Write-Host "`nAttempting to deactivate department with active employees..." -ForegroundColor Yellow
try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/deactivate" -Method Post -Headers $headers
    Write-Host "FAIL: Department was deactivated despite having active employees!" -ForegroundColor Red
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "Status Code: $statusCode" -ForegroundColor Gray
    
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $responseBody = $reader.ReadToEnd()
    $reader.Close()
    
    Write-Host "Response: $responseBody" -ForegroundColor Gray
    
    $errorObj = $responseBody | ConvertFrom-Json
    Write-Host "PASS: Correctly prevented deactivation!" -ForegroundColor Green
    Write-Host "Error message: $($errorObj.message)" -ForegroundColor Gray
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Completed" -ForegroundColor Cyan
