# Department Management Test Script
$baseUrl = "http://localhost:5183"
$tokenFile = "token.txt"

Write-Host "Department Management Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if (-not (Test-Path $tokenFile)) {
    Write-Host "Error: Token file not found" -ForegroundColor Red
    exit 1
}

$token = (Get-Content $tokenFile -Raw).Trim()
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 1: Create Department
Write-Host "`nTest 1: Create Department" -ForegroundColor Yellow
$createBody = @{
    code = "HR"
    name = "HR Department"
    managerId = $null
    parentDepartmentId = $null
} | ConvertTo-Json

try {
    $dept = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Post -Headers $headers -Body $createBody
    Write-Host "Success: Department created with ID $($dept.id)" -ForegroundColor Green
    $deptId = $dept.id
    $deptId | Out-File "department_id.txt"
}
catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# Test 2: Get All Departments
Write-Host "`nTest 2: Get All Departments" -ForegroundColor Yellow
try {
    $depts = Invoke-RestMethod -Uri "$baseUrl/api/departments" -Method Get -Headers $headers
    Write-Host "Success: Found $($depts.Count) departments" -ForegroundColor Green
}
catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get Department by ID
if ($deptId) {
    Write-Host "`nTest 3: Get Department by ID" -ForegroundColor Yellow
    try {
        $dept = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId" -Method Get -Headers $headers
        Write-Host "Success: Retrieved department $($dept.name)" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 4: Update Department
if ($deptId) {
    Write-Host "`nTest 4: Update Department" -ForegroundColor Yellow
    $updateBody = @{
        code = "HR"
        name = "HR Department Updated"
        managerId = $null
        parentDepartmentId = $null
    } | ConvertTo-Json
    
    try {
        $dept = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId" -Method Put -Headers $headers -Body $updateBody
        Write-Host "Success: Department updated to $($dept.name)" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 5: Get Employee Count
if ($deptId) {
    Write-Host "`nTest 5: Get Employee Count" -ForegroundColor Yellow
    try {
        $result = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/employee-count" -Method Get -Headers $headers
        Write-Host "Success: Department has $($result.activeEmployeeCount) active employees" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 6: Deactivate Department
if ($deptId) {
    Write-Host "`nTest 6: Deactivate Department" -ForegroundColor Yellow
    try {
        $result = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/deactivate" -Method Post -Headers $headers
        Write-Host "Success: $($result.message)" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 7: Activate Department
if ($deptId) {
    Write-Host "`nTest 7: Activate Department" -ForegroundColor Yellow
    try {
        $result = Invoke-RestMethod -Uri "$baseUrl/api/departments/$deptId/activate" -Method Post -Headers $headers
        Write-Host "Success: $($result.message)" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Tests Completed" -ForegroundColor Cyan
