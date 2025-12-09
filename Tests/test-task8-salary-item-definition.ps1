# Task 8 Test Script: Salary Item Definition Management

$baseUrl = "http://localhost:5183"
$testResults = @()

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Task 8: Salary Item Definition Management Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Login
Write-Host "Test 1: Login" -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "testuser"
        password = "testpass123"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"

    $token = $loginResponse.token
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    Write-Host "Pass - Login successful" -ForegroundColor Green
    $testResults += @{ Test = "Login"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Login failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Login"; Result = "Fail" }
    exit 1
}

Write-Host ""

# Test 2: Create Salary Item Definition (Hourly - Overtime)
Write-Host "Test 2: Create Salary Item Definition (Hourly - Overtime)" -ForegroundColor Yellow
try {
    $createBody = @{
        itemCode = "OT001"
        itemName = "Weekday Overtime"
        type = "Addition"
        calculationMethod = "Hourly"
        hourlyRate = 200.00
        description = "Weekday overtime pay, 200 per hour"
        effectiveDate = "2025-01-01T00:00:00Z"
        expiryDate = $null
    } | ConvertTo-Json

    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions" `
        -Method Post `
        -Headers $headers `
        -Body $createBody

    $itemId1 = $createResponse.id
    Write-Host "Pass - Created ID: $itemId1" -ForegroundColor Green
    Write-Host "  Code: $($createResponse.itemCode)" -ForegroundColor Gray
    Write-Host "  Name: $($createResponse.itemName)" -ForegroundColor Gray
    Write-Host "  Method: $($createResponse.calculationMethod)" -ForegroundColor Gray
    Write-Host "  Hourly Rate: $($createResponse.hourlyRate)" -ForegroundColor Gray
    $testResults += @{ Test = "Create Overtime Item"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Create failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Create Overtime Item"; Result = "Fail" }
}

Write-Host ""

# Test 3: Create Salary Item Definition (Fixed - Allowance)
Write-Host "Test 3: Create Salary Item Definition (Fixed - Allowance)" -ForegroundColor Yellow
try {
    $createBody = @{
        itemCode = "ALLOW001"
        itemName = "Transportation Allowance"
        type = "Addition"
        calculationMethod = "Fixed"
        defaultAmount = 2000.00
        description = "Monthly transportation allowance"
        effectiveDate = "2025-01-01T00:00:00Z"
        expiryDate = $null
    } | ConvertTo-Json

    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions" `
        -Method Post `
        -Headers $headers `
        -Body $createBody

    $itemId2 = $createResponse.id
    Write-Host "Pass - Created ID: $itemId2" -ForegroundColor Green
    Write-Host "  Code: $($createResponse.itemCode)" -ForegroundColor Gray
    Write-Host "  Name: $($createResponse.itemName)" -ForegroundColor Gray
    Write-Host "  Method: $($createResponse.calculationMethod)" -ForegroundColor Gray
    Write-Host "  Amount: $($createResponse.defaultAmount)" -ForegroundColor Gray
    $testResults += @{ Test = "Create Allowance Item"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Create failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Create Allowance Item"; Result = "Fail" }
}

Write-Host ""

# Test 4: Create Salary Item Definition (Percentage - Deduction)
Write-Host "Test 4: Create Salary Item Definition (Percentage - Deduction)" -ForegroundColor Yellow
try {
    $createBody = @{
        itemCode = "DED001"
        itemName = "Meal Deduction"
        type = "Deduction"
        calculationMethod = "Percentage"
        percentageRate = 0.05
        description = "Meal deduction, 5% of salary"
        effectiveDate = "2025-01-01T00:00:00Z"
        expiryDate = $null
    } | ConvertTo-Json

    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions" `
        -Method Post `
        -Headers $headers `
        -Body $createBody

    $itemId3 = $createResponse.id
    Write-Host "Pass - Created ID: $itemId3" -ForegroundColor Green
    Write-Host "  Code: $($createResponse.itemCode)" -ForegroundColor Gray
    Write-Host "  Name: $($createResponse.itemName)" -ForegroundColor Gray
    Write-Host "  Method: $($createResponse.calculationMethod)" -ForegroundColor Gray
    Write-Host "  Rate: $($createResponse.percentageRate)" -ForegroundColor Gray
    $testResults += @{ Test = "Create Deduction Item"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Create failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Create Deduction Item"; Result = "Fail" }
}

Write-Host ""

# Test 5: Get Salary Item Definition
Write-Host "Test 5: Get Salary Item Definition" -ForegroundColor Yellow
try {
    $getResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions/$itemId1" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved successfully" -ForegroundColor Green
    Write-Host "  Code: $($getResponse.itemCode)" -ForegroundColor Gray
    Write-Host "  Name: $($getResponse.itemName)" -ForegroundColor Gray
    Write-Host "  Active: $($getResponse.isActive)" -ForegroundColor Gray
    $testResults += @{ Test = "Get Item Definition"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get Item Definition"; Result = "Fail" }
}

Write-Host ""

# Test 6: Get All Active Items
Write-Host "Test 6: Get All Active Items" -ForegroundColor Yellow
try {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved $($listResponse.Count) items" -ForegroundColor Green
    foreach ($item in $listResponse) {
        Write-Host "  - $($item.itemCode): $($item.itemName) ($($item.type))" -ForegroundColor Gray
    }
    $testResults += @{ Test = "Get All Active Items"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get All Active Items"; Result = "Fail" }
}

Write-Host ""

# Test 7: Get Items By Type (Addition)
Write-Host "Test 7: Get Items By Type (Addition)" -ForegroundColor Yellow
try {
    $typeResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions/by-type/Addition" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved $($typeResponse.Count) addition items" -ForegroundColor Green
    foreach ($item in $typeResponse) {
        Write-Host "  - $($item.itemCode): $($item.itemName)" -ForegroundColor Gray
    }
    $testResults += @{ Test = "Get Items By Type"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get Items By Type"; Result = "Fail" }
}

Write-Host ""

# Test 8: Update Salary Item Definition
Write-Host "Test 8: Update Salary Item Definition" -ForegroundColor Yellow
try {
    $updateBody = @{
        itemName = "Weekday Overtime (Updated)"
        type = "Addition"
        calculationMethod = "Hourly"
        hourlyRate = 250.00
        description = "Weekday overtime pay, 250 per hour (adjusted)"
        isActive = $true
        effectiveDate = "2025-01-01T00:00:00Z"
        expiryDate = $null
    } | ConvertTo-Json

    $updateResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions/$itemId1" `
        -Method Put `
        -Headers $headers `
        -Body $updateBody

    Write-Host "Pass - Updated successfully" -ForegroundColor Green
    Write-Host "  Name: $($updateResponse.itemName)" -ForegroundColor Gray
    Write-Host "  Hourly Rate: $($updateResponse.hourlyRate)" -ForegroundColor Gray
    $testResults += @{ Test = "Update Item Definition"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Update failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Update Item Definition"; Result = "Fail" }
}

Write-Host ""

# Test 9: Deactivate Salary Item Definition
Write-Host "Test 9: Deactivate Salary Item Definition" -ForegroundColor Yellow
try {
    $deactivateResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions/$itemId3/deactivate" `
        -Method Post `
        -Headers $headers

    Write-Host "Pass - Deactivated successfully" -ForegroundColor Green
    Write-Host "  Message: $($deactivateResponse.message)" -ForegroundColor Gray
    $testResults += @{ Test = "Deactivate Item"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Deactivate failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Deactivate Item"; Result = "Fail" }
}

Write-Host ""

# Test 10: Verify Deactivated Item Not In Active List
Write-Host "Test 10: Verify Deactivated Item Not In Active List" -ForegroundColor Yellow
try {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions" `
        -Method Get `
        -Headers $headers

    $deactivatedItem = $listResponse | Where-Object { $_.id -eq $itemId3 }
    
    if ($null -eq $deactivatedItem) {
        Write-Host "Pass - Deactivated item not in active list" -ForegroundColor Green
        $testResults += @{ Test = "Verify Deactivation"; Result = "Pass" }
    }
    else {
        Write-Host "Fail - Deactivated item still in active list" -ForegroundColor Red
        $testResults += @{ Test = "Verify Deactivation"; Result = "Fail" }
    }
}
catch {
    Write-Host "Fail - Verification failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Verify Deactivation"; Result = "Fail" }
}

Write-Host ""

# Test 11: Get Item History
Write-Host "Test 11: Get Item History" -ForegroundColor Yellow
try {
    $historyResponse = Invoke-RestMethod -Uri "$baseUrl/api/salary-item-definitions/history/OT001" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved $($historyResponse.Count) versions" -ForegroundColor Green
    foreach ($item in $historyResponse) {
        Write-Host "  - Effective: $($item.effectiveDate), Rate: $($item.hourlyRate)" -ForegroundColor Gray
    }
    $testResults += @{ Test = "Get Item History"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get history failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get Item History"; Result = "Fail" }
}

Write-Host ""

# Test Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$passedTests = ($testResults | Where-Object { $_.Result -eq "Pass" }).Count
$totalTests = $testResults.Count

Write-Host ""
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $($totalTests - $passedTests)" -ForegroundColor Red
Write-Host "Pass Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor Yellow
Write-Host ""

foreach ($result in $testResults) {
    $color = if ($result.Result -eq "Pass") { "Green" } else { "Red" }
    $symbol = if ($result.Result -eq "Pass") { "[PASS]" } else { "[FAIL]" }
    Write-Host "$symbol $($result.Test): $($result.Result)" -ForegroundColor $color
}

Write-Host ""
Write-Host "Test Complete!" -ForegroundColor Cyan
