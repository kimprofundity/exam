#!/usr/bin/env pwsh

# 任務 11：稅務計算模組測試腳本

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
        Write-Host "回應：$($response | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
        
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

# 3.1 計算所得稅
$incomeTaxRequest = @{
    GrossSalary = 50000
    EmployeeId = "test-employee-001"
    Period = "2024-12-01T00:00:00Z"
}

$incomeTaxResponse = Test-ApiEndpoint -TestName "計算所得稅" -Method "POST" -Url "$baseUrl/api/tax/calculate-income-tax" -Headers $headers -Body $incomeTaxRequest

# 4. 測試勞保費計算
Write-Host "`n--- 4. 測試勞保費計算 ---" -ForegroundColor Blue

$laborInsuranceRequest = @{
    Salary = 50000
    Period = "2024-12-01T00:00:00Z"
}

$laborInsuranceResponse = Test-ApiEndpoint -TestName "計算勞保費" -Method "POST" -Url "$baseUrl/api/tax/calculate-labor-insurance" -Headers $headers -Body $laborInsuranceRequest

# 5. 測試健保費計算
Write-Host "`n--- 5. 測試健保費計算 ---" -ForegroundColor Blue

$healthInsuranceRequest = @{
    Salary = 50000
    Period = "2024-12-01T00:00:00Z"
}

$healthInsuranceResponse = Test-ApiEndpoint -TestName "計算健保費" -Method "POST" -Url "$baseUrl/api/tax/calculate-health-insurance" -Headers $headers -Body $healthInsuranceRequest

# 6. 測試累進稅率計算
Write-Host "`n--- 6. 測試累進稅率計算 ---" -ForegroundColor Blue

$progressiveTaxRequest = @{
    AnnualIncome = 600000
    Deductions = 120000
    Exemptions = 92000
}

$progressiveTaxResponse = Test-ApiEndpoint -TestName "計算累進稅率所得稅" -Method "POST" -Url "$baseUrl/api/tax/calculate-progressive-tax" -Headers $headers -Body $progressiveTaxRequest

# 7. 測試取得員工扣除額
Write-Host "`n--- 7. 測試取得員工扣除額 ---" -ForegroundColor Blue

$deductionsResponse = Test-ApiEndpoint -TestName "取得員工扣除額" -Method "GET" -Url "$baseUrl/api/tax/employees/test-employee-001/deductions/2024" -Headers $headers

# 8. 測試取得員工免稅額
Write-Host "`n--- 8. 測試取得員工免稅額 ---" -ForegroundColor Blue

$exemptionsResponse = Test-ApiEndpoint -TestName "取得員工免稅額" -Method "GET" -Url "$baseUrl/api/tax/employees/test-employee-001/exemptions/2024" -Headers $headers

# 9. 測試取得累進稅率表
Write-Host "`n--- 9. 測試取得累進稅率表 ---" -ForegroundColor Blue

$taxBracketsResponse = Test-ApiEndpoint -TestName "取得累進稅率表" -Method "GET" -Url "$baseUrl/api/tax/progressive-tax-brackets/2024" -Headers $headers

# 10. 測試整合薪資計算（包含稅務計算）
Write-Host "`n--- 10. 測試整合薪資計算 ---" -ForegroundColor Blue

# 首先確保有員工資料
$createEmployeeRequest = @{
    EmployeeNumber = "TAX001"
    Name = "稅務測試員工"
    DepartmentId = "test-dept-001"
    Position = "測試職位"
    MonthlySalary = 50000
    BankCode = "012"
    BankAccount = "1234567890"
}

$employeeResponse = Test-ApiEndpoint -TestName "建立測試員工" -Method "POST" -Url "$baseUrl/api/employees" -Headers $headers -Body $createEmployeeRequest

if ($employeeResponse -ne $null) {
    $employeeId = $employeeResponse.id
    
    # 計算薪資（包含稅務計算）
    $calculateSalaryRequest = @{
        EmployeeId = $employeeId
        Period = "2024-12-01T00:00:00Z"
        CopyFromPreviousMonth = $false
    }
    
    $salaryResponse = Test-ApiEndpoint -TestName "計算薪資（含稅務）" -Method "POST" -Url "$baseUrl/api/payroll/calculate" -Headers $headers -Body $calculateSalaryRequest
    
    if ($salaryResponse -ne $null) {
        Write-Host "薪資計算結果：" -ForegroundColor Cyan
        Write-Host "  基本薪資：$($salaryResponse.baseSalary)" -ForegroundColor White
        Write-Host "  應發薪資：$(if ($salaryResponse.grossSalary) { [System.Text.Encoding]::UTF8.GetString($salaryResponse.grossSalary) } else { '加密資料' })" -ForegroundColor White
        Write-Host "  實發薪資：$(if ($salaryResponse.netSalary) { [System.Text.Encoding]::UTF8.GetString($salaryResponse.netSalary) } else { '加密資料' })" -ForegroundColor White
        Write-Host "  薪資項目數：$($salaryResponse.salaryItems.Count)" -ForegroundColor White
        
        # 顯示薪資項目詳情
        if ($salaryResponse.salaryItems -and $salaryResponse.salaryItems.Count -gt 0) {
            Write-Host "  薪資項目明細：" -ForegroundColor Cyan
            foreach ($item in $salaryResponse.salaryItems) {
                $typeText = if ($item.type -eq "Addition") { "加項" } else { "減項" }
                Write-Host "    - $($item.itemName) ($($item.itemCode)): $($item.amount) [$typeText]" -ForegroundColor White
            }
        }
    }
}

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