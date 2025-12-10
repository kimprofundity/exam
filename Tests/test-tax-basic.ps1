# 基本稅務計算測試

$baseUrl = "http://localhost:5183"

# 登入
$loginBody = @{
    Username = "testuser"
    Password = "testpass123"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
$headers = @{ "Authorization" = "Bearer $token" }

Write-Host "Login successful, token: $($token.Substring(0,20))..."

# 測試所得稅計算
$taxBody = @{
    GrossSalary = 50000
    EmployeeId = "test-employee-001"
    Period = "2024-12-01T00:00:00Z"
} | ConvertTo-Json

try {
    $taxResponse = Invoke-RestMethod -Uri "$baseUrl/api/tax/calculate-income-tax" -Method POST -Body $taxBody -ContentType "application/json" -Headers $headers
    Write-Host "Income tax calculation successful: $($taxResponse.incomeTax)"
} catch {
    Write-Host "Income tax calculation failed: $($_.Exception.Message)"
}

# 測試勞保費計算
$laborBody = @{
    Salary = 50000
    Period = "2024-12-01T00:00:00Z"
} | ConvertTo-Json

try {
    $laborResponse = Invoke-RestMethod -Uri "$baseUrl/api/tax/calculate-labor-insurance" -Method POST -Body $laborBody -ContentType "application/json" -Headers $headers
    Write-Host "Labor insurance calculation successful: $($laborResponse.laborInsurance)"
} catch {
    Write-Host "Labor insurance calculation failed: $($_.Exception.Message)"
}

# 測試健保費計算
$healthBody = @{
    Salary = 50000
    Period = "2024-12-01T00:00:00Z"
} | ConvertTo-Json

try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/api/tax/calculate-health-insurance" -Method POST -Body $healthBody -ContentType "application/json" -Headers $headers
    Write-Host "Health insurance calculation successful: $($healthResponse.healthInsurance)"
} catch {
    Write-Host "Health insurance calculation failed: $($_.Exception.Message)"
}

Write-Host "Tax calculation tests completed."