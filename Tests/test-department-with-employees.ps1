# Test Department Deactivation with Active Employees
$baseUrl = "http://localhost:5183"
$token = (Get-Content "token.txt" -Raw).Trim()
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "Test: Department Deactivation with Active Employees" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Create a test department
Write-Host "`nStep 1: Create Test Department" -ForegroundColor Yellow
$createDeptBody = @{
    code = "TEST-DEPT"
    name = "Test Department"
    managerId = $null
    parentDepartmentId = $null
} | ConvertTo-Json

try {
    $dept = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Post -Headers $headers -Body $createDeptBody
    Write-Host "Success: Department created with ID $($dept.id)" -ForegroundColor Green
    $deptId = $dept.id
}
catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "Details: $($errorObj.message)" -ForegroundColor Red
    }
    exit 1
}

# Step 2: Create an employee in this department
Write-Host "`nStep 2: Create Employee in Department" -ForegroundColor Yellow
$createEmpBody = @{
    employeeNumber = "TEST001"
    name = "Test Employee"
    departmentId = $deptId
    position = "Test Position"
    monthlySalary = 50000
    bankCode = "001"
    bankAccount = "1234567890"
} | ConvertTo-Json

try {
    $empResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees" -Method Post -Headers $headers -Body $createEmpBody
    Write-Host "Success: Employee created with ID $($empResponse.id)" -ForegroundColor Green
    $empId = $empResponse.id
}
catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# Step 3: Verify employee count
Write-Host "`nStep 3: Verify Employee Count" -ForegroundColor Yellow
try {
    $countResult = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/employee-count" -Method Get -Headers $headers
    Write-Host "Success: Department has $($countResult.activeEmployeeCount) active employees" -ForegroundColor Green
}
catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Try to deactivate department (should fail)
Write-Host "`nStep 4: Try to Deactivate Department with Active Employees" -ForegroundColor Yellow
try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/deactivate" -Method Post -Headers $headers
    Write-Host "FAIL: Department should not be deactivated when it has active employees" -ForegroundColor Red
}
catch {
    if ($_.ErrorDetails.Message) {
        $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
        if ($errorObj.message -like "*在職員工*") {
            Write-Host "PASS: Correctly prevented deactivation" -ForegroundColor Green
            Write-Host "Error message: $($errorObj.message)" -ForegroundColor Gray
        }
        else {
            Write-Host "FAIL: Wrong error message" -ForegroundColor Red
            Write-Host "Error: $($errorObj.message)" -ForegroundColor Red
        }
    }
    else {
        Write-Host "FAIL: No error details" -ForegroundColor Red
    }
}

# Step 5: Resign the employee
if ($empId) {
    Write-Host "`nStep 5: Resign Employee" -ForegroundColor Yellow
    $resignBody = @{
        resignationDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
    } | ConvertTo-Json

    try {
        $result = Invoke-RestMethod -Uri "$baseUrl/api/employees/$empId/resign" -Method Post -Headers $headers -Body $resignBody
        Write-Host "Success: Employee resigned" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 6: Verify employee count after resignation
Write-Host "`nStep 6: Verify Employee Count After Resignation" -ForegroundColor Yellow
try {
    $countResult = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/employee-count" -Method Get -Headers $headers
    Write-Host "Success: Department has $($countResult.activeEmployeeCount) active employees" -ForegroundColor Green
}
catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 7: Try to deactivate department again (should succeed)
Write-Host "`nStep 7: Deactivate Department After Employee Resignation" -ForegroundColor Yellow
try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/deactivate" -Method Post -Headers $headers
    Write-Host "PASS: Department deactivated successfully" -ForegroundColor Green
    Write-Host "Message: $($result.message)" -ForegroundColor Gray
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Completed" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
