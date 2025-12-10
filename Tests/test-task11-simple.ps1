#!/usr/bin/env pwsh

# 任務 11：稅務計算模組簡化測試腳本

Write-Host "=== 任務 11：稅務計算模組測試 ===" -ForegroundColor Green

# API 基礎 URL
$baseUrl = "http://localhost:5183"

# 測試結果統計
$testResults = @()

function Test-ApiEndpoint {
    param(
        [string]$TestName,
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [object]$Body = $null
    )
    
    try {
        Write-Host "測試：$TestName" -ForegroundColor Yellow
        
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
        }
        
        if ($Body -ne $null) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params
        Write-Host "✅ $TestName - 成功" -ForegroundColor Green
        
        $script:testResults += @{
            Test = $TestName
            Status = "通過"
            Response = $response
        }
        
        return $response
    }
    catch {
        Write-Host "❌ $TestName - 失敗" -ForegroundColor Red
        Write-Host "錯誤：$($_.Exception.Message)" -ForegroundColor Red
        
        $script:testResults += @{
            Test = $TestName
            Status = "失敗"
            Error = $_.Exception.Message
        }
        
        return $null
    }
}

# 1. 健康檢查
Write-Host "`n--- 1. 健康檢查 ---" -ForegroundColor Blue
Test-ApiEndpoint -TestName "健康檢查" -Method "GET" -Url "$baseUrl/health"

# 2. 登入取得 Token
Write-Host "`n--- 2. 登入取得 Token ---" -ForegroundColor Blue
$loginResponse = Test-ApiEndpoint -TestName "登入" -Method "POST" -Url "$baseUrl/api/auth/login" -Body @{
    Username = "testuser"
    Password = "testpass123"
}

if ($loginResponse -eq $null) {
    Write-Host "❌ 無法取得認證令牌，停止測試" -ForegroundColor Red
    exit 1
}

$token = $loginResponse.token
$headers = @{
    "Authorization" = "Bearer $token"
}

Write-Host "✅ 成功取得令牌" -ForegroundColor Green

# 3. 測試所得稅計算
Write-Host "`n--- 3. 測試所得稅計算 ---" -ForegroundColor Blue

$incomeTaxRequest = @{
    GrossSalary = 50000
    EmployeeId = "test-employee-001"
    Period = "2024-12-01T00:00:00Z"
}

Test-ApiEndpoint -TestName "計算所得稅" -Method "POST" -Url "$baseUrl/api/tax/calculate-income-tax" -Headers $headers -Body $incomeTaxRequest

# 4. 測試勞保費計算
Write-Host "`n--- 4. 測試勞保費計算 ---" -ForegroundColor Blue

$laborInsuranceRequest = @{
    Salary = 50000
    Period = "2024-12-01T00:00:00Z"
}

Test-ApiEndpoint -TestName "計算勞保費" -Method "POST" -Url "$baseUrl/api/tax/calculate-labor-insurance" -Headers $headers -Body $laborInsuranceRequest

# 5. 測試健保費計算
Write-Host "`n--- 5. 測試健保費計算 ---" -ForegroundColor Blue

$healthInsuranceRequest = @{
    Salary = 50000
    Period = "2024-12-01T00:00:00Z"
}

Test-ApiEndpoint -TestName "計算健保費" -Method "POST" -Url "$baseUrl/api/tax/calculate-health-insurance" -Headers $headers -Body $healthInsuranceRequest

# 6. 測試累進稅率計算
Write-Host "`n--- 6. 測試累進稅率計算 ---" -ForegroundColor Blue

$progressiveTaxRequest = @{
    AnnualIncome = 600000
    Deductions = 120000
    Exemptions = 92000
}

Test-ApiEndpoint -TestName "計算累進稅率所得稅" -Method "POST" -Url "$baseUrl/api/tax/calculate-progressive-tax" -Headers $headers -Body $progressiveTaxRequest

# 7. 測試取得累進稅率表
Write-Host "`n--- 7. 測試取得累進稅率表 ---" -ForegroundColor Blue

Test-ApiEndpoint -TestName "取得累進稅率表" -Method "GET" -Url "$baseUrl/api/tax/progressive-tax-brackets/2024" -Headers $headers

# 測試結果統計
Write-Host "`n=== 測試結果統計 ===" -ForegroundColor Green
$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Status -eq "通過" }).Count
$failedTests = $totalTests - $passedTests

Write-Host "總測試數：$totalTests" -ForegroundColor White
Write-Host "通過：$passedTests" -ForegroundColor Green
Write-Host "失敗：$failedTests" -ForegroundColor Red
Write-Host "成功率：$([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor Yellow

# 顯示失敗的測試
if ($failedTests -gt 0) {
    Write-Host "`n失敗的測試：" -ForegroundColor Red
    $testResults | Where-Object { $_.Status -eq "失敗" } | ForEach-Object {
        Write-Host "  - $($_.Test): $($_.Error)" -ForegroundColor Red
    }
}

Write-Host "`n=== 任務 11 測試完成 ===" -ForegroundColor Green

# 回傳結果
if ($failedTests -eq 0) {
    Write-Host "🎉 所有測試通過！稅務計算模組實作成功！" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  有 $failedTests 個測試失敗，請檢查實作" -ForegroundColor Yellow
    exit 1
}