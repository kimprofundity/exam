# Comprehensive Department Management Test
$baseUrl = "http://localhost:5183"
$token = (Get-Content "token.txt" -Raw).Trim()
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "Comprehensive Department Management Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$testsPassed = 0
$testsFailed = 0

# Generate unique codes using timestamp
$timestamp = (Get-Date).ToString("HHmmss")

# Test 1: Create Parent Department
Write-Host "`nTest 1: Create Parent Department" -ForegroundColor Yellow
$createBody = @{
    code = "SALES-$timestamp"
    name = "Sales Department"
    managerId = $null
    parentDepartmentId = $null
} | ConvertTo-Json

try {
    $parentDept = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Post -Headers $headers -Body $createBody
    Write-Host "PASS: Parent department created with ID $($parentDept.id)" -ForegroundColor Green
    $parentDeptId = $parentDept.id
    $testsPassed++
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "Details: $($errorObj.message)" -ForegroundColor Red
    }
    $testsFailed++
}

# Test 2: Create Child Department
if ($parentDeptId) {
    Write-Host "`nTest 2: Create Child Department" -ForegroundColor Yellow
    $createChildBody = @{
        code = "SALES-NORTH-$timestamp"
        name = "Sales North Region"
        managerId = $null
        parentDepartmentId = $parentDeptId
    } | ConvertTo-Json

    try {
        $childDept = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Post -Headers $headers -Body $createChildBody
        Write-Host "PASS: Child department created with ID $($childDept.id)" -ForegroundColor Green
        $childDeptId = $childDept.id
        $testsPassed++
    }
    catch {
        Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
        $testsFailed++
    }
}

# Test 3: Get Department Hierarchy
if ($childDeptId) {
    Write-Host "`nTest 3: Get Department Hierarchy" -ForegroundColor Yellow
    try {
        $hierarchy = Invoke-RestMethod -Uri "$baseUrl/api/departments/$childDeptId/hierarchy" -Method Get -Headers $headers
        Write-Host "PASS: Retrieved hierarchy" -ForegroundColor Green
        Write-Host "  Current: $($hierarchy.department.name)" -ForegroundColor Gray
        Write-Host "  Ancestors: $($hierarchy.ancestors.Count)" -ForegroundColor Gray
        Write-Host "  Children: $($hierarchy.children.Count)" -ForegroundColor Gray
        Write-Host "  Active Employees: $($hierarchy.activeEmployeeCount)" -ForegroundColor Gray
        $testsPassed++
    }
    catch {
        Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
        $testsFailed++
    }
}

# Test 4: Update Department
if ($parentDeptId) {
    Write-Host "`nTest 4: Update Department" -ForegroundColor Yellow
    $updateBody = @{
        code = "SALES-$timestamp"
        name = "Sales Department (Updated)"
        managerId = $null
        parentDepartmentId = $null
    } | ConvertTo-Json

    try {
        $updated = Invoke-RestMethod -Uri "$baseUrl/api/departments/$parentDeptId" -Method Put -Headers $headers -Body $updateBody
        Write-Host "PASS: Department updated to $($updated.name)" -ForegroundColor Green
        $testsPassed++
    }
    catch {
        Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
        $testsFailed++
    }
}

# Test 5: Deactivate Department without Employees
if ($childDeptId) {
    Write-Host "`nTest 5: Deactivate Department (No Employees)" -ForegroundColor Yellow
    try {
        $result = Invoke-RestMethod -Uri "$baseUrl/api/departments/$childDeptId/deactivate" -Method Post -Headers $headers
        Write-Host "PASS: $($result.message)" -ForegroundColor Green
        $testsPassed++
    }
    catch {
        Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
        $testsFailed++
    }
}

# Test 6: Activate Department
if ($childDeptId) {
    Write-Host "`nTest 6: Activate Department" -ForegroundColor Yellow
    try {
        $result = Invoke-RestMethod -Uri "$baseUrl/api/departments/$childDeptId/activate" -Method Post -Headers $headers
        Write-Host "PASS: $($result.message)" -ForegroundColor Green
        $testsPassed++
    }
    catch {
        Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
        $testsFailed++
    }
}

# Test 7: Verify Department Code Uniqueness
Write-Host "`nTest 7: Verify Department Code Uniqueness" -ForegroundColor Yellow
$duplicateBody = @{
    code = "SALES-$timestamp"
    name = "Duplicate Sales"
    managerId = $null
    parentDepartmentId = $null
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Post -Headers $headers -Body $duplicateBody
    Write-Host "FAIL: Should have rejected duplicate code" -ForegroundColor Red
    $testsFailed++
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        $reader.Close()
        
        $errorObj = $responseBody | ConvertFrom-Json
        
        # Check if error message contains expected text
        $isValidError = $errorObj.message.Contains("SALES") -and $statusCode -eq 400
        
        if ($isValidError) {
            Write-Host "PASS: Correctly rejected duplicate code (Status: $statusCode)" -ForegroundColor Green
            Write-Host "  Error message: $($errorObj.message)" -ForegroundColor Gray
            $testsPassed++
        }
        else {
            Write-Host "FAIL: Wrong error message" -ForegroundColor Red
            Write-Host "  Received: $($errorObj.message)" -ForegroundColor Gray
            $testsFailed++
        }
    }
    catch {
        Write-Host "FAIL: Could not parse error response" -ForegroundColor Red
        Write-Host "  Exception: $($_.Exception.Message)" -ForegroundColor Gray
        $testsFailed++
    }
}

# Test 8: Get All Departments
Write-Host "`nTest 8: Get All Departments" -ForegroundColor Yellow
try {
    $depts = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Get -Headers $headers
    Write-Host "PASS: Retrieved $($depts.Count) departments" -ForegroundColor Green
    $testsPassed++
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Test 9: Get All Departments Including Inactive
Write-Host "`nTest 9: Get All Departments (Including Inactive)" -ForegroundColor Yellow
try {
    $depts = Invoke-RestMethod -Uri "$baseUrl/api/departments?includeInactive=true" -Method Get -Headers $headers
    Write-Host "PASS: Retrieved $($depts.Count) departments (including inactive)" -ForegroundColor Green
    $testsPassed++
}
catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Passed: $testsPassed" -ForegroundColor Green
Write-Host "Failed: $testsFailed" -ForegroundColor Red
Write-Host "Total: $($testsPassed + $testsFailed)" -ForegroundColor Cyan
