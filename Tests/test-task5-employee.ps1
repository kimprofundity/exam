# Task 5: Employee Management Module Test Script
$baseUrl = "http://localhost:5183"
$testResults = @()

Write-Host "========================================"
Write-Host "Task 5: Employee Management Test"
Write-Host "========================================"
Write-Host ""

# Get Department ID from database
Write-Host "Getting department ID from database..."
$deptQuery = docker exec hrpayroll-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password123" -d HRPayrollSystem -C -Q "SELECT TOP 1 Id FROM Departments" -h -1 -W 2>$null
$departmentId = ($deptQuery | Where-Object { $_ -match "^[0-9A-F]{8}-" } | Select-Object -First 1).Trim()
Write-Host "Department ID: $departmentId"
Write-Host ""

# Test 1: Login
Write-Host "Test 1: Login and get token..."
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body (@{
        username = "testuser"
        password = "testpass123"
    } | ConvertTo-Json) -ContentType "application/json"
    
    $token = $loginResponse.token
    $headers = @{ "Authorization" = "Bearer $token" }
    
    Write-Host "Pass: Login successful"
    $testResults += @{ Test = "Login"; Result = "Pass" }
} catch {
    Write-Host "Fail: Login failed - $($_.Exception.Message)"
    $testResults += @{ Test = "Login"; Result = "Fail" }
    exit 1
}

Write-Host ""

# Test 2: Create Employee
Write-Host "Test 2: Create new employee..."
try {
    $newEmployee = @{
        employeeNumber = "E9999"
        name = "Test Employee"
        departmentId = $departmentId
        position = "Test Engineer"
        monthlySalary = 50000
        bankCode = "012"
        bankAccount = "1234567890"
    }
    
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees" -Method Post -Body ($newEmployee | ConvertTo-Json) -ContentType "application/json" -Headers $headers
    
    $script:employeeId = $createResponse.id
    Write-Host "Pass: Employee created with ID: $employeeId"
    $testResults += @{ Test = "Create Employee"; Result = "Pass" }
} catch {
    Write-Host "Fail: Create employee failed - $($_.Exception.Message)"
    $testResults += @{ Test = "Create Employee"; Result = "Fail" }
}

Write-Host ""

# Test 3: Get Employee
Write-Host "Test 3: Get employee data..."
try {
    $employee = Invoke-RestMethod -Uri "$baseUrl/api/employees/$employeeId" -Method Get -Headers $headers
    
    if ($employee.name -eq "Test Employee" -and $employee.monthlySalary -eq 50000) {
        Write-Host "Pass: Employee data is correct"
        Write-Host "  Name: $($employee.name)"
        Write-Host "  Salary: $($employee.monthlySalary)"
        $testResults += @{ Test = "Get Employee"; Result = "Pass" }
    } else {
        Write-Host "Fail: Employee data is incorrect"
        $testResults += @{ Test = "Get Employee"; Result = "Fail" }
    }
} catch {
    Write-Host "Fail: Get employee failed - $($_.Exception.Message)"
    $testResults += @{ Test = "Get Employee"; Result = "Fail" }
}

Write-Host ""

# Test 4: Update Employee
Write-Host "Test 4: Update employee data..."
try {
    $updateEmployee = @{
        name = "Test Employee Updated"
        position = "Senior Test Engineer"
        monthlySalary = 60000
        bankCode = "012"
        bankAccount = "1234567890"
    }
    
    $updateResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees/$employeeId" -Method Put -Body ($updateEmployee | ConvertTo-Json) -ContentType "application/json" -Headers $headers
    
    $updatedEmployee = Invoke-RestMethod -Uri "$baseUrl/api/employees/$employeeId" -Method Get -Headers $headers
    
    if ($updatedEmployee.name -eq "Test Employee Updated" -and $updatedEmployee.monthlySalary -eq 60000) {
        Write-Host "Pass: Employee updated successfully"
        $testResults += @{ Test = "Update Employee"; Result = "Pass" }
    } else {
        Write-Host "Fail: Employee update failed"
        $testResults += @{ Test = "Update Employee"; Result = "Fail" }
    }
} catch {
    Write-Host "Fail: Update employee failed - $($_.Exception.Message)"
    $testResults += @{ Test = "Update Employee"; Result = "Fail" }
}

Write-Host ""

# Test 5: Get Employee List
Write-Host "Test 5: Get employee list..."
try {
    $employeeList = Invoke-RestMethod -Uri "$baseUrl/api/employees?pageNumber=1&pageSize=10" -Method Get -Headers $headers
    
    if ($employeeList.items -and $employeeList.items.Count -gt 0) {
        Write-Host "Pass: Employee list retrieved"
        Write-Host "  Total: $($employeeList.totalCount)"
        $testResults += @{ Test = "Get Employee List"; Result = "Pass" }
    } else {
        Write-Host "Fail: Employee list is empty"
        $testResults += @{ Test = "Get Employee List"; Result = "Fail" }
    }
} catch {
    Write-Host "Fail: Get employee list failed - $($_.Exception.Message)"
    $testResults += @{ Test = "Get Employee List"; Result = "Fail" }
}

Write-Host ""

# Test 6: Resign Employee
Write-Host "Test 6: Resign employee..."
try {
    $resignRequest = @{
        resignationDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
    }
    
    $resignResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees/$employeeId/resign" -Method Post -Body ($resignRequest | ConvertTo-Json) -ContentType "application/json" -Headers $headers
    
    $resignedEmployee = Invoke-RestMethod -Uri "$baseUrl/api/employees/$employeeId" -Method Get -Headers $headers
    
    if ($resignedEmployee.status -eq 1 -or $resignedEmployee.status -eq "Resigned") {
        Write-Host "Pass: Employee resigned successfully"
        Write-Host "  Status: $($resignedEmployee.status)"
        $testResults += @{ Test = "Resign Employee"; Result = "Pass" }
    } else {
        Write-Host "Fail: Employee resign failed (Status: $($resignedEmployee.status))"
        $testResults += @{ Test = "Resign Employee"; Result = "Fail" }
    }
} catch {
    Write-Host "Fail: Resign employee failed - $($_.Exception.Message)"
    $testResults += @{ Test = "Resign Employee"; Result = "Fail" }
}

Write-Host ""
Write-Host "========================================"
Write-Host "Test Summary"
Write-Host "========================================"

$passCount = ($testResults | Where-Object { $_.Result -eq "Pass" }).Count
$failCount = ($testResults | Where-Object { $_.Result -eq "Fail" }).Count
$totalCount = $testResults.Count

Write-Host ""
foreach ($result in $testResults) {
    Write-Host "$($result.Test): $($result.Result)"
}

Write-Host ""
Write-Host "Total: $totalCount"
Write-Host "Pass: $passCount"
Write-Host "Fail: $failCount"
Write-Host "Pass Rate: $([math]::Round($passCount / $totalCount * 100, 2))%"
Write-Host ""

if ($failCount -eq 0) {
    Write-Host "All tests passed!"
    exit 0
} else {
    Write-Host "Some tests failed"
    exit 1
}
