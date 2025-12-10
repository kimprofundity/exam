# 薪資計算功能測試腳本

# 設定 API 基礎 URL
$baseUrl = "https://localhost:7001"

# 讀取認證令牌
$tokenFile = "Tests/token.txt"
if (Test-Path $tokenFile) {
    $token = Get-Content $tokenFile -Raw
    $token = $token.Trim()
    Write-Host "使用現有令牌進行測試" -ForegroundColor Green
} else {
    Write-Host "找不到令牌檔案，請先執行登入測試" -ForegroundColor Red
    exit 1
}

# 設定請求標頭
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "=== 薪資計算功能測試 ===" -ForegroundColor Cyan

try {
    # 1. 取得員工列表，選擇第一個員工進行測試
    Write-Host "`n1. 取得員工列表..." -ForegroundColor Yellow
    $employeesResponse = Invoke-RestMethod -Uri "$baseUrl/api/employees" -Method Get -Headers $headers
    
    if ($employeesResponse.data -and $employeesResponse.data.Count -gt 0) {
        $testEmployee = $employeesResponse.data[0]
        Write-Host "選擇測試員工：$($testEmployee.name) (ID: $($testEmployee.id))" -ForegroundColor Green
    } else {
        Write-Host "沒有找到員工資料，無法進行測試" -ForegroundColor Red
        exit 1
    }

    # 2. 取得當月工作天數
    Write-Host "`n2. 取得當月工作天數..." -ForegroundColor Yellow
    $currentPeriod = Get-Date -Format "yyyy-MM-01"
    $workingDaysResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/working-days/$currentPeriod" -Method Get -Headers $headers
    Write-Host "當月工作天數：$($workingDaysResponse.workingDays)" -ForegroundColor Green

    # 3. 計算實際出勤天數
    Write-Host "`n3. 計算實際出勤天數..." -ForegroundColor Yellow
    $actualWorkDaysResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/employees/$($testEmployee.id)/actual-work-days?period=$currentPeriod" -Method Get -Headers $headers
    Write-Host "實際出勤天數：$($actualWorkDaysResponse.actualWorkDays)" -ForegroundColor Green

    # 4. 計算請假扣款
    Write-Host "`n4. 計算請假扣款..." -ForegroundColor Yellow
    $leaveDeductionResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/employees/$($testEmployee.id)/leave-deduction?period=$currentPeriod" -Method Get -Headers $headers
    Write-Host "請假扣款：$($leaveDeductionResponse.leaveDeduction)" -ForegroundColor Green

    # 5. 計算基本薪資
    Write-Host "`n5. 計算基本薪資..." -ForegroundColor Yellow
    $baseSalaryRequest = @{
        employeeId = $testEmployee.id
        period = $currentPeriod
        workDays = $actualWorkDaysResponse.actualWorkDays
        totalWorkDays = $workingDaysResponse.workingDays
    }
    $baseSalaryResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/calculate-base-salary" -Method Post -Headers $headers -Body ($baseSalaryRequest | ConvertTo-Json)
    Write-Host "基本薪資：$($baseSalaryResponse.baseSalary)" -ForegroundColor Green

    # 6. 執行完整薪資計算
    Write-Host "`n6. 執行完整薪資計算..." -ForegroundColor Yellow
    $salaryCalculationRequest = @{
        employeeId = $testEmployee.id
        period = $currentPeriod
        copyFromPreviousMonth = $false
    }
    
    $salaryResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/calculate" -Method Post -Headers $headers -Body ($salaryCalculationRequest | ConvertTo-Json)
    Write-Host "薪資計算完成！薪資記錄 ID：$($salaryResponse.id)" -ForegroundColor Green

    # 7. 取得薪資記錄詳情
    Write-Host "`n7. 取得薪資記錄詳情..." -ForegroundColor Yellow
    $salaryRecordResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/salary-records/$($salaryResponse.id)" -Method Get -Headers $headers
    
    Write-Host "薪資記錄詳情：" -ForegroundColor Green
    Write-Host "  員工：$($salaryRecordResponse.employee.name)" -ForegroundColor White
    Write-Host "  期間：$($salaryRecordResponse.period)" -ForegroundColor White
    Write-Host "  基本薪資：$($salaryRecordResponse.baseSalary)" -ForegroundColor White
    Write-Host "  加項總額：$($salaryRecordResponse.totalAdditions)" -ForegroundColor White
    Write-Host "  減項總額：$($salaryRecordResponse.totalDeductions)" -ForegroundColor White
    Write-Host "  狀態：$($salaryRecordResponse.status)" -ForegroundColor White
    Write-Host "  費率表版本：$($salaryRecordResponse.rateTableVersion)" -ForegroundColor White
    
    if ($salaryRecordResponse.salaryItems -and $salaryRecordResponse.salaryItems.Count -gt 0) {
        Write-Host "  薪資項目：" -ForegroundColor White
        foreach ($item in $salaryRecordResponse.salaryItems) {
            $typeText = if ($item.type -eq "Addition") { "加項" } else { "減項" }
            Write-Host "    - $($item.itemName) ($typeText)：$($item.amount)" -ForegroundColor Gray
        }
    }

    # 8. 取得員工薪資記錄列表
    Write-Host "`n8. 取得員工薪資記錄列表..." -ForegroundColor Yellow
    $salaryRecordsResponse = Invoke-RestMethod -Uri "$baseUrl/api/payroll/employees/$($testEmployee.id)/salary-records" -Method Get -Headers $headers
    Write-Host "員工薪資記錄數量：$($salaryRecordsResponse.totalCount)" -ForegroundColor Green

    Write-Host "`n=== 薪資計算功能測試完成 ===" -ForegroundColor Cyan
    Write-Host "所有測試項目執行成功！" -ForegroundColor Green

} catch {
    Write-Host "`n測試失敗：$($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "HTTP 狀態碼：$statusCode" -ForegroundColor Red
        
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "錯誤詳情：$errorBody" -ForegroundColor Red
        } catch {
            Write-Host "無法讀取錯誤詳情" -ForegroundColor Red
        }
    }
    exit 1
}