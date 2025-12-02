# HR Payroll System - Frontend

人事薪資系統前端專案，使用 Vue 3 + TypeScript + Vite 建構。

## 技術堆疊

- Vue 3 (Composition API)
- TypeScript
- Vite
- Vue Router
- Pinia (狀態管理)
- Axios (HTTP 客戶端)

## 開發指令

```bash
# 安裝依賴
npm install

# 啟動開發伺服器
npm run dev

# 建置生產版本
npm run build

# 預覽生產版本
npm run preview
```

## 專案結構

```
src/
├── assets/          # 靜態資源
├── components/      # 可重用元件
├── router/          # 路由配置
├── services/        # API 服務
├── stores/          # Pinia 狀態管理
├── types/           # TypeScript 類型定義
├── views/           # 頁面元件
├── App.vue          # 根元件
└── main.ts          # 入口檔案
```

## 環境變數

建立 `.env.local` 檔案：

```
VITE_API_BASE_URL=http://localhost:5000
```
