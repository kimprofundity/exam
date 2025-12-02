# Git Commit 規範

## Commit Message 格式

所有 commit message 必須遵循以下格式：

```
[{{module}}] {{type}}: {{subject}}
```

### 組成部分

1. **[module]** - 模組名稱
   - Backend - 後端相關
   - Frontend - 前端相關
   - Database - 資料庫相關
   - Specs - 規格文件相關
   - Docs - 文件相關
   - Config - 配置相關
   - All - 跨模組或整體專案

2. **type** - 變更類型（只能使用以下類型）
   - **Feat** - 新增功能
   - **Update** - 修改邏輯
   - **Fix** - Bug 修復
   - **Optimize** - 效能優化
   - **Refactor** - 重構
   - **Docs** - 文件

3. **subject** - 簡潔描述
   - 必須簡潔明瞭
   - 不超過 50 字
   - 使用中文描述

## Type 判斷規則

根據變更內容自動推斷 type：

- 新增功能 → **Feat**
- 修改邏輯 → **Update**
- Bug 修復 → **Fix**
- 效能優化 → **Optimize**
- 重構 → **Refactor**
- 文件 → **Docs**

## 範例

```
[Backend] Feat: 新增員工資料 API 端點
[Frontend] Update: 修改登入頁面驗證邏輯
[Database] Fix: 修復薪資計算觸發器錯誤
[Backend] Optimize: 優化查詢效能
[Frontend] Refactor: 重構狀態管理架構
[Docs] Docs: 更新 API 文件
[All] Feat: 初始化 HR 薪資系統專案架構與規格文件
```

## 注意事項

- 每次 commit 前必須確認 message 符合此規範
- Subject 必須清楚說明變更內容
- 一個 commit 應該只做一件事
- 避免過大的 commit，適當拆分變更
