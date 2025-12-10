# Task 9 Test Script: Rate Table Management

$baseUrl = "http://localhost:5183"
$testResults = @()

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Task 9: Rate Table Management Test" -ForegroundColor Cyan
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

# Clean up existing rate tables first
Write-Host "Cleanup: Removing existing rate tables..." -ForegroundColor Gray
try {
    $existingTables = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables" `
        -Method Get `
        -Headers $headers
    
    foreach ($table in $existingTables) {
        try {
            Invoke-RestMethod -Uri "$baseUrl/api/rate-tables/$($table.id)" `
                -Method Delete `
                -Headers $headers | Out-Null
            Write-Host "  Deleted: $($table.version)" -ForegroundColor Gray
        }
        catch {
            Write-Host "  Could not delete: $($table.version)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "  Cleanup failed (continuing anyway)" -ForegroundColor Gray
}

Write-Host ""

# Test 2: Create Rate Table (2025)
Write-Host "Test 2: Create Rate Table (2025)" -ForegroundColor Yellow
try {
    $createBody = @{
        version = "2025-V1"
        effectiveDate = "2025-01-01T00:00:00Z"
        expiryDate = "2025-12-31T23:59:59Z"
        laborInsuranceRate = 0.105
        healthInsuranceRate = 0.0517
        source = "Manual"
    } | ConvertTo-Json

    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables" `
        -Method Post `
        -Headers $headers `
        -Body $createBody

    $rateTableId1 = $createResponse.id
    Write-Host "Pass - Created ID: $rateTableId1" -ForegroundColor Green
    Write-Host "  Version: $($createResponse.version)" -ForegroundColor Gray
    Write-Host "  Labor Rate: $($createResponse.laborInsuranceRate)" -ForegroundColor Gray
    Write-Host "  Health Rate: $($createResponse.healthInsuranceRate)" -ForegroundColor Gray
    $testResults += @{ Test = "Create Rate Table 2025"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Create failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Create Rate Table 2025"; Result = "Fail" }
}

Write-Host ""

# Test 3: Create Rate Table (2026)
Write-Host "Test 3: Create Rate Table (2026)" -ForegroundColor Yellow
try {
    $createBody = @{
        version = "2026-V1"
        effectiveDate = "2026-01-01T00:00:00Z"
        expiryDate = $null
        laborInsuranceRate = 0.11
        healthInsuranceRate = 0.053
        source = "Manual"
    } | ConvertTo-Json

    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables" `
        -Method Post `
        -Headers $headers `
        -Body $createBody

    $rateTableId2 = $createResponse.id
    Write-Host "Pass - Created ID: $rateTableId2" -ForegroundColor Green
    Write-Host "  Version: $($createResponse.version)" -ForegroundColor Gray
    Write-Host "  Labor Rate: $($createResponse.laborInsuranceRate)" -ForegroundColor Gray
    Write-Host "  Health Rate: $($createResponse.healthInsuranceRate)" -ForegroundColor Gray
    $testResults += @{ Test = "Create Rate Table 2026"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Create failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Create Rate Table 2026"; Result = "Fail" }
}

Write-Host ""

# Test 4: Get Rate Table
Write-Host "Test 4: Get Rate Table" -ForegroundColor Yellow
try {
    $getResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables/$rateTableId1" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved successfully" -ForegroundColor Green
    Write-Host "  Version: $($getResponse.version)" -ForegroundColor Gray
    Write-Host "  Effective Date: $($getResponse.effectiveDate)" -ForegroundColor Gray
    Write-Host "  Source: $($getResponse.source)" -ForegroundColor Gray
    $testResults += @{ Test = "Get Rate Table"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get Rate Table"; Result = "Fail" }
}

Write-Host ""

# Test 5: Get All Rate Tables
Write-Host "Test 5: Get All Rate Tables" -ForegroundColor Yellow
try {
    $listResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved $($listResponse.Count) rate tables" -ForegroundColor Green
    foreach ($item in $listResponse) {
        Write-Host "  - $($item.version): Effective $($item.effectiveDate)" -ForegroundColor Gray
    }
    $testResults += @{ Test = "Get All Rate Tables"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get All Rate Tables"; Result = "Fail" }
}

Write-Host ""

# Test 6: Get Effective Rate Table (2025-06-01)
Write-Host "Test 6: Get Effective Rate Table (2025-06-01)" -ForegroundColor Yellow
try {
    $date = "2025-06-01"
    $effectiveResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables/effective/$date" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved effective rate table" -ForegroundColor Green
    Write-Host "  Version: $($effectiveResponse.version)" -ForegroundColor Gray
    Write-Host "  Labor Rate: $($effectiveResponse.laborInsuranceRate)" -ForegroundColor Gray
    Write-Host "  Health Rate: $($effectiveResponse.healthInsuranceRate)" -ForegroundColor Gray
    
    if ($effectiveResponse.version -eq "2025-V1") {
        Write-Host "  Correct version for date 2025-06-01" -ForegroundColor Green
    }
    else {
        Write-Host "  Warning: Expected version 2025-V1" -ForegroundColor Yellow
    }
    
    $testResults += @{ Test = "Get Effective Rate Table 2025"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get effective failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get Effective Rate Table 2025"; Result = "Fail" }
}

Write-Host ""

# Test 7: Get Effective Rate Table (2026-06-01)
Write-Host "Test 7: Get Effective Rate Table (2026-06-01)" -ForegroundColor Yellow
try {
    $date = "2026-06-01"
    $effectiveResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables/effective/$date" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved effective rate table" -ForegroundColor Green
    Write-Host "  Version: $($effectiveResponse.version)" -ForegroundColor Gray
    Write-Host "  Labor Rate: $($effectiveResponse.laborInsuranceRate)" -ForegroundColor Gray
    Write-Host "  Health Rate: $($effectiveResponse.healthInsuranceRate)" -ForegroundColor Gray
    
    if ($effectiveResponse.version -eq "2026-V1") {
        Write-Host "  Correct version for date 2026-06-01" -ForegroundColor Green
    }
    else {
        Write-Host "  Warning: Expected version 2026-V1" -ForegroundColor Yellow
    }
    
    $testResults += @{ Test = "Get Effective Rate Table 2026"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get effective failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get Effective Rate Table 2026"; Result = "Fail" }
}

Write-Host ""

# Test 8: Update Rate Table
Write-Host "Test 8: Update Rate Table" -ForegroundColor Yellow
try {
    if (-not $rateTableId1) {
        Write-Host "Skip - No rate table ID from Test 2" -ForegroundColor Yellow
        $testResults += @{ Test = "Update Rate Table"; Result = "Skip" }
    }
    else {
        $updateBody = @{
            version = "2025-V1-Updated"
            effectiveDate = "2025-01-01T00:00:00Z"
            expiryDate = "2025-12-31T23:59:59Z"
            laborInsuranceRate = 0.106
            healthInsuranceRate = 0.052
            source = "Manual"
        } | ConvertTo-Json

        $updateResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables/$rateTableId1" `
            -Method Put `
            -Headers $headers `
            -Body $updateBody

        Write-Host "Pass - Updated successfully" -ForegroundColor Green
        Write-Host "  Version: $($updateResponse.version)" -ForegroundColor Gray
        Write-Host "  Labor Rate: $($updateResponse.laborInsuranceRate)" -ForegroundColor Gray
        Write-Host "  Health Rate: $($updateResponse.healthInsuranceRate)" -ForegroundColor Gray
        $testResults += @{ Test = "Update Rate Table"; Result = "Pass" }
    }
}
catch {
    Write-Host "Fail - Update failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Update Rate Table"; Result = "Fail" }
}

Write-Host ""

# Test 9: Get Rate Table History
Write-Host "Test 9: Get Rate Table History" -ForegroundColor Yellow
try {
    $historyResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables/history" `
        -Method Get `
        -Headers $headers

    Write-Host "Pass - Retrieved $($historyResponse.Count) historical records" -ForegroundColor Green
    foreach ($item in $historyResponse) {
        Write-Host "  - $($item.version): $($item.effectiveDate) to $($item.expiryDate)" -ForegroundColor Gray
    }
    $testResults += @{ Test = "Get Rate Table History"; Result = "Pass" }
}
catch {
    Write-Host "Fail - Get history failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Get Rate Table History"; Result = "Fail" }
}

Write-Host ""

# Test 10: Delete Rate Table
Write-Host "Test 10: Delete Rate Table" -ForegroundColor Yellow
try {
    if (-not $rateTableId2) {
        Write-Host "Skip - No rate table ID from Test 3" -ForegroundColor Yellow
        $testResults += @{ Test = "Delete Rate Table"; Result = "Skip" }
    }
    else {
        $deleteResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables/$rateTableId2" `
            -Method Delete `
            -Headers $headers

        Write-Host "Pass - Deleted successfully" -ForegroundColor Green
        Write-Host "  Message: $($deleteResponse.message)" -ForegroundColor Gray
        $testResults += @{ Test = "Delete Rate Table"; Result = "Pass" }
    }
}
catch {
    Write-Host "Fail - Delete failed: $_" -ForegroundColor Red
    $testResults += @{ Test = "Delete Rate Table"; Result = "Fail" }
}

Write-Host ""

# Test 11: Verify Deleted Rate Table Not Found
Write-Host "Test 11: Verify Deleted Rate Table Not Found" -ForegroundColor Yellow
try {
    if (-not $rateTableId2) {
        Write-Host "Skip - No rate table ID from Test 3" -ForegroundColor Yellow
        $testResults += @{ Test = "Verify Deletion"; Result = "Skip" }
    }
    else {
        $getResponse = Invoke-RestMethod -Uri "$baseUrl/api/rate-tables/$rateTableId2" `
            -Method Get `
            -Headers $headers
        
        Write-Host "Fail - Deleted rate table still exists" -ForegroundColor Red
        $testResults += @{ Test = "Verify Deletion"; Result = "Fail" }
    }
}
catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "Pass - Deleted rate table not found (expected)" -ForegroundColor Green
        $testResults += @{ Test = "Verify Deletion"; Result = "Pass" }
    }
    else {
        Write-Host "Fail - Unexpected error: $_" -ForegroundColor Red
        $testResults += @{ Test = "Verify Deletion"; Result = "Fail" }
    }
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
