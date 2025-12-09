# Test Login API

$loginUrl = "http://localhost:5183/api/auth/login"

# Test data
$loginData = @{
    username = "testuser"
    password = "testpass123"
} | ConvertTo-Json

Write-Host "Testing Login API..." -ForegroundColor Cyan
Write-Host "URL: $loginUrl" -ForegroundColor Yellow
Write-Host "Request Data: $loginData" -ForegroundColor Yellow
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginData -ContentType "application/json"
    
    Write-Host "Login Successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response Data:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10 | Write-Host
    
    # Save token for future tests
    if ($response.token) {
        $response.token | Out-File -FilePath "token.txt" -Encoding UTF8
        Write-Host ""
        Write-Host "Token saved to token.txt" -ForegroundColor Green
    }
}
catch {
    Write-Host "Login Failed!" -ForegroundColor Red
    Write-Host "Error Message: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "Error Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
