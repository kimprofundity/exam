# Task 7: Leave Management Module Test Script
# Test leave requests, proxy leave, date overlap validation, etc.

$baseUrl = "http://localhost:5183"
$testResults = @()

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Task 7: Leave Management Module Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Login to get Token
Write-Host "Test 1: Login to get Token..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "testuser"
        password = "testpass123"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    
    if ($loginResponse.success -and $loginResponse.token) {
        $token = $loginResponse.token
        Write-Host "OK Login successful, Token: $($token.Substring(0, 20))..." -ForegroundColor Green
        $testResults += @{ Test = "Login"; Result = "Pass" }
    } else {
        Write-Host "FAIL Login failed" -ForegroundColor Red
        $testResults += @{ Test = "Login"; Result = "Fail" }
        exit 1
    }
} catch {
    Write-Host "FAIL Login request failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Login"; Result = "Fail" }
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""

# Test 2: Prepare test employee
Write-Host "Test 2: Prepare test employee..." -ForegroundColor Yellow
try {
    $departments = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Get -Headers $headers
    $testDeptId = $departments[0].id
    
    # Use unique employee number with timestamp
    $timestamp = (Get-Date).ToString("HHmmss")
    $empNumber = "E77$timestamp"
    
    $employeeBody = @{
        employeeNumber = $empNumber
        name = "Leave Test Employee"
        departmentId = $testDeptId
        position = "Tester"
        monthlySalary = 50000
        bankCode = "012"
        bankAccount = "1234567890"
    } | ConvertTo-Json

    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees" -Method Post -Body $employeeBody -Headers $headers
    $testEmployeeId = $createResponse.id
    Write-Host "OK Created test employee $empNumber, ID: $testEmployeeId" -ForegroundColor Green
    
    $testResults += @{ Test = "Prepare Employee"; Result = "Pass" }
} catch {
    Write-Host "FAIL Prepare employee failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Prepare Employee"; Result = "Fail" }
    exit 1
}

Write-Host ""

# Test 3: Create leave request (Personal)
Write-Host "Test 3: Create leave request (Personal)..." -ForegroundColor Yellow
try {
    $leaveBody = @{
        employeeId = $testEmployeeId
        type = "Personal"
        startDate = (Get-Date).AddDays(1).ToString("yyyy-MM-ddT00:00:00")
        endDate = (Get-Date).AddDays(3).ToString("yyyy-MM-ddT00:00:00")
        days = 3
        proxyUserId = $null
    } | ConvertTo-Json

    $leaveResponse = Invoke-RestMethod -Uri "$baseUrl/api/leaves" -Method Post -Body $leaveBody -Headers $headers
    $leaveId1 = $leaveResponse.id
    
    Write-Host "OK Created leave request, ID: $leaveId1" -ForegroundColor Green
    $testResults += @{ Test = "Create Leave"; Result = "Pass" }
} catch {
    Write-Host "FAIL Create leave failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Create Leave"; Result = "Fail" }
}

Write-Host ""

# Test 4: Get leave record details
Write-Host "Test 4: Get leave record details..." -ForegroundColor Yellow
try {
    $leave = Invoke-RestMethod -Uri "$baseUrl/api/leaves/$leaveId1" -Method Get -Headers $headers
    
    if ($leave.id -eq $leaveId1 -and $leave.type -eq "Personal" -and $leave.days -eq 3) {
        Write-Host "OK Got leave record successfully" -ForegroundColor Green
        Write-Host "  - Type: $($leave.type)" -ForegroundColor Gray
        Write-Host "  - Days: $($leave.days)" -ForegroundColor Gray
        Write-Host "  - Status: $($leave.status)" -ForegroundColor Gray
        $testResults += @{ Test = "Get Leave Record"; Result = "Pass" }
    } else {
        Write-Host "FAIL Leave record data incorrect" -ForegroundColor Red
        $testResults += @{ Test = "Get Leave Record"; Result = "Fail" }
    }
} catch {
    Write-Host "FAIL Get leave record failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Get Leave Record"; Result = "Fail" }
}

Write-Host ""

# Test 5: Check date overlap (should overlap)
Write-Host "Test 5: Check date overlap validation..." -ForegroundColor Yellow
try {
    $overlapBody = @{
        employeeId = $testEmployeeId
        startDate = (Get-Date).AddDays(2).ToString("yyyy-MM-ddT00:00:00")
        endDate = (Get-Date).AddDays(4).ToString("yyyy-MM-ddT00:00:00")
        excludeLeaveId = $null
    } | ConvertTo-Json

    $overlapResponse = Invoke-RestMethod -Uri "$baseUrl/api/leaves/check-overlap" -Method Post -Body $overlapBody -Headers $headers
    
    if ($overlapResponse.hasOverlap -eq $true) {
        Write-Host "OK Correctly detected date overlap" -ForegroundColor Green
        $testResults += @{ Test = "Date Overlap Check"; Result = "Pass" }
    } else {
        Write-Host "FAIL Failed to detect date overlap" -ForegroundColor Red
        $testResults += @{ Test = "Date Overlap Check"; Result = "Fail" }
    }
} catch {
    Write-Host "FAIL Check overlap failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Date Overlap Check"; Result = "Fail" }
}

Write-Host ""

# Test 6: Try to create overlapping leave (should fail)
Write-Host "Test 6: Try to create overlapping leave (should fail)..." -ForegroundColor Yellow
try {
    $overlapLeaveBody = @{
        employeeId = $testEmployeeId
        type = "Sick"
        startDate = (Get-Date).AddDays(2).ToString("yyyy-MM-ddT00:00:00")
        endDate = (Get-Date).AddDays(4).ToString("yyyy-MM-ddT00:00:00")
        days = 3
        proxyUserId = $null
    } | ConvertTo-Json

    $overlapLeaveResponse = Invoke-RestMethod -Uri "$baseUrl/api/leaves" -Method Post -Body $overlapLeaveBody -Headers $headers
    Write-Host "FAIL System allowed overlapping leave" -ForegroundColor Red
    $testResults += @{ Test = "Reject Overlap"; Result = "Fail" }
} catch {
    if ($_.Exception.Message -like "*400*" -or $_.Exception.Message -like "*overlap*" -or $_.Exception.Message -like "*重疊*") {
        Write-Host "OK System correctly rejected overlapping leave" -ForegroundColor Green
        $testResults += @{ Test = "Reject Overlap"; Result = "Pass" }
    } else {
        Write-Host "FAIL Wrong error message: $($_.Exception.Message)" -ForegroundColor Red
        $testResults += @{ Test = "Reject Overlap"; Result = "Fail" }
    }
}

Write-Host ""

# Test 7: Create proxy leave
Write-Host "Test 7: Create proxy leave..." -ForegroundColor Yellow
try {
    $proxyEmpNumber = "E78$timestamp"
    $proxyEmployeeBody = @{
        employeeNumber = $proxyEmpNumber
        name = "Proxy Test"
        departmentId = $testDeptId
        position = "Tester"
        monthlySalary = 50000
    } | ConvertTo-Json

    $proxyResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees" -Method Post -Body $proxyEmployeeBody -Headers $headers
    $proxyEmployeeId = $proxyResponse.id

    $proxyLeaveBody = @{
        employeeId = $testEmployeeId
        type = "Sick"
        startDate = (Get-Date).AddDays(10).ToString("yyyy-MM-ddT00:00:00")
        endDate = (Get-Date).AddDays(12).ToString("yyyy-MM-ddT00:00:00")
        days = 3
        proxyUserId = $proxyEmployeeId
    } | ConvertTo-Json

    $proxyLeaveResponse = Invoke-RestMethod -Uri "$baseUrl/api/leaves" -Method Post -Body $proxyLeaveBody -Headers $headers
    $proxyLeaveId = $proxyLeaveResponse.id
    
    Write-Host "OK Created proxy leave, ID: $proxyLeaveId" -ForegroundColor Green
    $testResults += @{ Test = "Create Proxy Leave"; Result = "Pass" }
} catch {
    Write-Host "FAIL Create proxy leave failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Create Proxy Leave"; Result = "Fail" }
}

Write-Host ""

# Test 8: Confirm proxy leave
Write-Host "Test 8: Confirm proxy leave..." -ForegroundColor Yellow
try {
    $confirmResponse = Invoke-RestMethod -Uri "$baseUrl/api/leaves/$proxyLeaveId/confirm" -Method Post -Headers $headers
    
    if ($confirmResponse.success) {
        Write-Host "OK Confirmed proxy leave" -ForegroundColor Green
        
        $updatedLeave = Invoke-RestMethod -Uri "$baseUrl/api/leaves/$proxyLeaveId" -Method Get -Headers $headers
        if ($updatedLeave.status -eq "Approved") {
            Write-Host "  - Status updated to: Approved" -ForegroundColor Gray
            $testResults += @{ Test = "Confirm Proxy Leave"; Result = "Pass" }
        } else {
            Write-Host "FAIL Status not updated correctly" -ForegroundColor Red
            $testResults += @{ Test = "Confirm Proxy Leave"; Result = "Fail" }
        }
    } else {
        Write-Host "FAIL Confirm proxy leave failed" -ForegroundColor Red
        $testResults += @{ Test = "Confirm Proxy Leave"; Result = "Fail" }
    }
} catch {
    Write-Host "FAIL Confirm proxy leave failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Confirm Proxy Leave"; Result = "Fail" }
}

Write-Host ""

# Test 9: Get employee leave records
Write-Host "Test 9: Get employee leave records..." -ForegroundColor Yellow
try {
    $leaves = Invoke-RestMethod -Uri "$baseUrl/api/employees/$testEmployeeId/leaves" -Method Get -Headers $headers
    
    if ($leaves.Count -ge 2) {
        Write-Host "OK Got leave records, total: $($leaves.Count)" -ForegroundColor Green
        $testResults += @{ Test = "Get Leave List"; Result = "Pass" }
    } else {
        Write-Host "FAIL Leave record count incorrect" -ForegroundColor Red
        $testResults += @{ Test = "Get Leave List"; Result = "Fail" }
    }
} catch {
    Write-Host "FAIL Get leave list failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Get Leave List"; Result = "Fail" }
}

Write-Host ""

# Test 10: Query leave balance
Write-Host "Test 10: Query leave balance..." -ForegroundColor Yellow
try {
    $balance = Invoke-RestMethod -Uri "$baseUrl/api/employees/$testEmployeeId/leave-balance?type=Annual" -Method Get -Headers $headers
    
    Write-Host "OK Queried leave balance" -ForegroundColor Green
    Write-Host "  - Annual leave remaining: $($balance.remainingDays) days" -ForegroundColor Gray
    $testResults += @{ Test = "Query Leave Balance"; Result = "Pass" }
} catch {
    Write-Host "FAIL Query balance failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Query Leave Balance"; Result = "Fail" }
}

Write-Host ""

# Test 11: Create and reject proxy leave
Write-Host "Test 11: Create and reject proxy leave..." -ForegroundColor Yellow
try {
    $rejectLeaveBody = @{
        employeeId = $testEmployeeId
        type = "Personal"
        startDate = (Get-Date).AddDays(20).ToString("yyyy-MM-ddT00:00:00")
        endDate = (Get-Date).AddDays(21).ToString("yyyy-MM-ddT00:00:00")
        days = 2
        proxyUserId = $proxyEmployeeId
    } | ConvertTo-Json

    $rejectLeaveResponse = Invoke-RestMethod -Uri "$baseUrl/api/leaves" -Method Post -Body $rejectLeaveBody -Headers $headers
    $rejectLeaveId = $rejectLeaveResponse.id
    
    $rejectResponse = Invoke-RestMethod -Uri "$baseUrl/api/leaves/$rejectLeaveId/reject" -Method Post -Headers $headers
    
    if ($rejectResponse.success) {
        $rejectedLeave = Invoke-RestMethod -Uri "$baseUrl/api/leaves/$rejectLeaveId" -Method Get -Headers $headers
        if ($rejectedLeave.status -eq "Rejected") {
            Write-Host "OK Rejected proxy leave successfully" -ForegroundColor Green
            $testResults += @{ Test = "Reject Proxy Leave"; Result = "Pass" }
        } else {
            Write-Host "FAIL Status not updated to Rejected" -ForegroundColor Red
            $testResults += @{ Test = "Reject Proxy Leave"; Result = "Fail" }
        }
    } else {
        Write-Host "FAIL Reject proxy leave failed" -ForegroundColor Red
        $testResults += @{ Test = "Reject Proxy Leave"; Result = "Fail" }
    }
} catch {
    Write-Host "FAIL Reject proxy leave failed: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += @{ Test = "Reject Proxy Leave"; Result = "Fail" }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$passCount = ($testResults | Where-Object { $_.Result -eq "Pass" }).Count
$totalCount = $testResults.Count

Write-Host ""
foreach ($result in $testResults) {
    $color = if ($result.Result -eq "Pass") { "Green" } else { "Red" }
    $symbol = if ($result.Result -eq "Pass") { "OK" } else { "FAIL" }
    Write-Host "$symbol $($result.Test): $($result.Result)" -ForegroundColor $color
}

Write-Host ""
Write-Host "Passed: $passCount / $totalCount" -ForegroundColor $(if ($passCount -eq $totalCount) { "Green" } else { "Yellow" })
Write-Host ""

if ($passCount -eq $totalCount) {
    Write-Host "All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some tests failed" -ForegroundColor Yellow
    exit 1
}