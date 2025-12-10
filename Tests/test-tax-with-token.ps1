# 使用現有 token 測試稅務計算

$baseUrl = "http://localhost:5183"
$token = Get-Content "token.txt" -Raw
$token = $token.Trim()
$headers = @{ "Authorization" = "Bearer $token" }

Write-Host "Using existing token: $($token.Substring(0,20))..."

# 測試所得稅計算
$taxBody = @{
    GrossSalary = 50000
    EmployeeId = "test-employee-001"
    Period = "2024-12-01T00:00:00Z"
} | ConvertTo-Json

try {
    $taxResponse = Invoke-RestMethod -Uri "$baseUrl/api/tax/calculate-income-tax" -Method POST -Body $taxBody -ContentType "application/json" -Headers $headers
    Write-Host "✅ Income tax calculation successful: $($taxResponse.incomeTax)"
} catch {
    Write-Host "❌ Income tax calculation failed: $($_.Exception.Message)"
}

# 測試勞保費計算
$laborBody = @{
    Salary = 50000
    Period = "2024-12-01T00:00:00Z"
} | ConvertTo-Json

try {
    $laborResponse = Invoke-RestMethod -Uri "$baseUrl/api/tax/calculate-labor-insurance" -Method POST -Body $laborBody -ContentType "application/json" -Headers $headers
    Write-Host "✅ Labor insurance calculation successful: $($laborResponse.laborInsurance)"
} catch {
    Write-Host "❌ Labor insurance calculation failed: $($_.Exception.Message)"
}

# 測試健保費計算
$healthBody = @{
    Salary = 50000
    Period = "2024-12-01T00:00:00Z"
} | ConvertTo-Json

try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/api/tax/calculate-health-insurance" -Method POST -Body $healthBody -ContentType "application/json" -Headers $headers
    Write-Host "✅ Health insurance calculation successful: $($healthResponse.healthInsurance)"
} catch {
    Write-Host "❌ Health insurance calculation failed: $($_.Exception.Message)"
}

# 測試累進稅率計算
$progressiveBody = @{
    AnnualIncome = 600000
    Deductions = 120000
    Exemptions = 92000
} | ConvertTo-Json

try {
    $progressiveResponse = Invoke-RestMethod -Uri "$baseUrl/api/tax/calculate-progressive-tax" -Method POST -Body $progressiveBody -ContentType "application/json" -Headers $headers
    Write-Host "✅ Progressive tax calculation successful: $($progressiveResponse.progressiveTax)"
} catch {
    Write-Host "❌ Progressive tax calculation failed: $($_.Exception.Message)"
}

# 測試取得累進稅率表
try {
    $bracketsResponse = Invoke-RestMethod -Uri "$baseUrl/api/tax/progressive-tax-brackets/2024" -Method GET -Headers $headers
    Write-Host "✅ Tax brackets retrieval successful: $($bracketsResponse.Count) brackets"
    foreach ($bracket in $bracketsResponse) {
        Write-Host "  - $($bracket.minIncome) - $($bracket.maxIncome): $($bracket.taxRate)%"
    }
} catch {
    Write-Host "❌ Tax brackets retrieval failed: $($_.Exception.Message)"
}

Write-Host "Tax calculation tests completed."