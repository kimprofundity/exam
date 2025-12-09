# Test Authorization API

$baseUrl = "http://localhost:5183"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "權限管理模組測試" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. 測試登入並取得令牌
Write-Host "1. 測試登入..." -ForegroundColor Yellow
$loginUrl = "$baseUrl/api/auth/login"
$loginData = @{
    username = "testuser"
    password = "testpass123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginData -ContentType "application/json"
    
    if ($loginResponse.success) {
        Write-Host "   ✓ 登入成功" -ForegroundColor Green
        $token = $loginResponse.token
        $userId = $loginResponse.user.userId
        Write-Host "   使用者 ID: $userId" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "   ✗ 登入失敗" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "   ✗ 登入失敗: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. 測試取得使用者資訊（需要認證）
Write-Host "2. 測試取得使用者資訊（需要認證）..." -ForegroundColor Yellow
$headers = @{ Authorization = "Bearer $token" }

try {
    $userInfo = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" -Method Get -Headers $headers
    Write-Host "   ✓ 成功取得使用者資訊" -ForegroundColor Green
    Write-Host "   使用者名稱: $($userInfo.username)" -ForegroundColor Gray
    Write-Host "   顯示名稱: $($userInfo.displayName)" -ForegroundColor Gray
    Write-Host "   部門: $($userInfo.department)" -ForegroundColor Gray
    Write-Host "   角色數量: $($userInfo.roles.Count)" -ForegroundColor Gray
    Write-Host "   權限數量: $($userInfo.permissions.Count)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "   ✗ 取得使用者資訊失敗: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. 測試未認證存取（應該失敗）
Write-Host "3. 測試未認證存取（應該返回 401）..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" -Method Get
    Write-Host "   ✗ 未認證存取應該失敗，但成功了" -ForegroundColor Red
}
catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "   ✓ 正確返回 401 Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "   ✗ 返回錯誤的狀態碼: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}
Write-Host ""

# 4. 測試無效令牌（應該失敗）
Write-Host "4. 測試無效令牌（應該返回 401）..." -ForegroundColor Yellow
$invalidHeaders = @{ Authorization = "Bearer invalid_token_12345" }

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" -Method Get -Headers $invalidHeaders
    Write-Host "   ✗ 無效令牌應該失敗，但成功了" -ForegroundColor Red
}
catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "   ✓ 正確返回 401 Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "   ✗ 返回錯誤的狀態碼: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}
Write-Host ""

# 5. 測試健康檢查（不需要認證）
Write-Host "5. 測試健康檢查（不需要認證）..." -ForegroundColor Yellow

try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
    Write-Host "   ✓ 健康檢查成功" -ForegroundColor Green
    Write-Host "   狀態: $($health.status)" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host "   ✗ 健康檢查失敗: $($_.Exception.Message)" -ForegroundColor Red
}

# 總結
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "測試完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "注意事項：" -ForegroundColor Yellow
Write-Host "- 權限管理模組已實作完成" -ForegroundColor Gray
Write-Host "- 需要建立角色管理 API 才能完整測試權限功能" -ForegroundColor Gray
Write-Host "- 目前測試使用者沒有角色，因此沒有權限" -ForegroundColor Gray
Write-Host "- 建議下一步：實作角色管理 API 端點" -ForegroundColor Gray
Write-Host ""
