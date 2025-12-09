# Cleanup test data script
$baseUrl = "http://localhost:5183"

Write-Host "Cleaning up test data..." -ForegroundColor Yellow

# Login
$loginBody = @{
    username = "testuser"
    password = "testpass123"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get test employees
$employees = Invoke-RestMethod -Uri "$baseUrl/api/employees?searchKeyword=E777" -Method Get -Headers $headers

foreach ($emp in $employees.items) {
    Write-Host "Found test employee: $($emp.employeeNumber) - $($emp.name)" -ForegroundColor Gray
    
    # Resign employee to clean up
    try {
        $resignBody = @{
            resignationDate = (Get-Date).ToString("yyyy-MM-ddT00:00:00")
        } | ConvertTo-Json
        
        Invoke-RestMethod -Uri "$baseUrl/api/employees/$($emp.id)/resign" -Method Post -Body $resignBody -Headers $headers | Out-Null
        Write-Host "  - Resigned employee $($emp.employeeNumber)" -ForegroundColor Green
    } catch {
        Write-Host "  - Already resigned or error: $($_.Exception.Message)" -ForegroundColor Gray
    }
}

Write-Host "Cleanup completed!" -ForegroundColor Green