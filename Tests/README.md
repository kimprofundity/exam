# 測試資料夾

此資料夾包含所有測試相關的檔案和報告。

## 資料夾結構

```
Tests/
├── README.md                      # 本檔案
├── TASK3_TEST_REPORT.md          # 任務三測試報告（LDAP 整合和身份驗證）
├── TASK4_COMPLETION_REPORT.md    # 任務四完成報告（權限管理模組）
├── test-login.ps1                # 登入 API 測試腳本
└── token.txt                     # JWT 令牌（測試用）
```

## 測試腳本

### test-login.ps1
測試登入 API 的 PowerShell 腳本。

**使用方式**:
```powershell
cd Tests
.\test-login.ps1
```

**功能**:
- 測試 POST /api/auth/login 端點
- 使用測試帳號（testuser / testpass123）
- 顯示登入結果
- 將 JWT 令牌儲存到 token.txt

## 測試報告

### TASK3_TEST_REPORT.md
任務三（設定 LDAP 整合和身份驗證）的詳細測試報告。

**包含內容**:
- 健康檢查端點測試
- 登入功能測試
- 首次登入自動建立員工記錄測試
- 取得使用者資訊端點測試
- 錯誤處理測試
- 已解決的問題記錄

### TASK4_COMPLETION_REPORT.md
任務四（實作權限管理模組）的完成報告。

**包含內容**:
- 實作內容詳細說明
- 核心功能列表
- 安全特性
- 編譯狀態
- 驗證需求對照
- 檔案清單

## 測試資料

### token.txt
儲存最近一次登入測試產生的 JWT 令牌。

**用途**:
- 用於測試需要認證的 API 端點
- 可以使用此令牌測試 /api/auth/me 等端點

**使用範例**:
```powershell
$token = Get-Content Tests/token.txt
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod -Uri "http://localhost:5183/api/auth/me" -Method Get -Headers $headers
```

## 未來測試

此資料夾將用於存放：
- 單元測試報告
- 整合測試報告
- 屬性測試報告
- 效能測試報告
- 測試腳本
- 測試資料

## 注意事項

1. **token.txt** 包含敏感資訊，不應提交到版本控制系統
2. 測試腳本應該使用測試環境，不要在生產環境執行
3. 測試報告應該定期更新，反映最新的測試結果
