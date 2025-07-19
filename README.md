# FamilyTree AI 關聯分析系統

一個基於 Angular 和 .NET Core 的 AI 關聯分析系統，用於視覺化家族關係和人員管理。

## 🌟 功能特色

- **AI 關聯分析** - 智能分析人員關係
- **視覺化圖譜** - 互動式家族關係圖
- **關鍵字查詢** - 多種搜尋方式
- **人員管理** - 完整的 CRUD 操作
- **自動部署** - CI/CD 自動化部署

## 🚀 快速開始

### 本地開發

1. **克隆專案**
```bash
git clone https://github.com/yourusername/FamilyTree.git
cd FamilyTree
```

2. **啟動後端**
```bash
cd familytree-backend
dotnet run
```

3. **啟動前端**
```bash
cd familytree-frontend
npm install
ng serve
```

4. **訪問應用程式**
- 前端: http://localhost:4200
- 後端: http://localhost:5087

### 自動部署

本專案已配置完整的 CI/CD 自動化部署流程。

#### 設置步驟

1. **運行設置腳本**
```bash
./setup-cicd.sh
```

2. **設置 GitHub Secrets**
   - 進入 GitHub 倉庫設置
   - 添加 `EC2_SSH_KEY` Secret（SSH 私鑰內容）

3. **推送代碼**
```bash
git remote add origin https://github.com/yourusername/FamilyTree.git
git push -u origin main
```

#### 部署流程

- ✅ 推送到 `main` 分支自動觸發部署
- ✅ 自動構建 Angular 應用程式
- ✅ 自動部署到 EC2 實例
- ✅ 自動健康檢查
- ✅ 自動備份舊版本

## 📁 專案結構

```
FamilyTree/
├── familytree-backend/          # .NET Core 後端
│   ├── Controllers/            # API 控制器
│   ├── Models/                 # 數據模型
│   ├── Services/               # 業務邏輯
│   └── AI/                     # AI 分析模組
├── familytree-frontend/         # Angular 前端
│   ├── src/app/               # 應用程式源碼
│   │   ├── components/        # 共用組件
│   │   ├── pages/            # 頁面組件
│   │   └── services/         # 服務層
│   └── src/styles/           # 樣式檔案
├── .github/workflows/         # GitHub Actions
├── scripts/                   # 部署腳本
├── docker-compose.yml         # Docker 配置
└── CI_CD_SETUP.md            # CI/CD 設置指南
```

## 🛠 技術棧

### 前端
- **Angular 20** - 現代化前端框架
- **TypeScript** - 類型安全的 JavaScript
- **SCSS** - 樣式預處理器
- **D3.js** - 數據視覺化

### 後端
- **.NET Core 8** - 高性能後端框架
- **Entity Framework** - ORM 框架
- **PostgreSQL** - 關係型數據庫
- **Docker** - 容器化部署

### 部署
- **GitHub Actions** - CI/CD 自動化
- **AWS EC2** - 雲端伺服器
- **Docker Compose** - 容器編排
- **Nginx** - 反向代理

## 🔧 開發指南

### 環境要求
- Node.js 20+
- .NET Core 8 SDK
- Docker & Docker Compose
- Git

### 開發命令

```bash
# 前端開發
cd familytree-frontend
npm install
ng serve

# 後端開發
cd familytree-backend
dotnet run

# Docker 部署
docker-compose up -d

# 手動部署
./scripts/deploy-on-server.sh
```

## 📊 監控和管理

### 查看部署狀態
- GitHub Actions 頁面查看部署進度
- EC2 實例查看服務狀態

### 服務管理
```bash
# SSH 到 EC2 實例
ssh -i TreeTest.pem ubuntu@16.176.220.138

# 查看服務狀態
cd /home/ubuntu/FamilyTree
docker-compose ps

# 查看日誌
docker-compose logs

# 重啟服務
docker-compose restart
```

## 🔒 安全注意事項

- SSH 金鑰應妥善保管
- 定期更新安全群組規則
- 監控部署日誌
- 定期備份數據

## 📞 支援

如有問題，請檢查：
1. GitHub Actions 日誌
2. EC2 實例日誌
3. Docker 容器日誌
4. 應用程式日誌

## 📄 授權

本專案採用 MIT 授權條款。 