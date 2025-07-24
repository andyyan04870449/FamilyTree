# FamilyTree AWS 連線資訊 - 最新更新

## 📋 基本配置資訊

**更新時間**: 2025-01-27 00:12  
**區域**: ap-southeast-2 (澳洲悉尼)  
**AWS CLI版本**: aws-cli/2.27.55 Python/3.13.5 Darwin/23.1.0 source/arm64

## 🔐 認證狀態

```
Access Key: ****************BR5H (從共享憑證文件)
Secret Key: ****************H00b (從共享憑證文件)
區域: ap-southeast-2
```

## 🖥️ EC2 實例資訊

### 當前實例 (POC)
- **實例ID**: `i-04a08ac869c2c20af`
- **狀態**: running (運行中)
- **類型**: `t3.small`
- **公網IP**: `52.62.99.253`
- **私有IP**: `172.31.20.65`
- **密鑰文件**: `POC.pem`
- **SSH用戶名**: ubuntu
- **系統**: Ubuntu 24.04.2 LTS
- **主機名**: ip-172-31-17-89.ap-southeast-2.compute.internal
- **運行時間**: 全新實例 (2025-07-25啟動)

### SSH連線命令
```bash
ssh -i POC.pem ubuntu@52.62.99.253
```

### 密鑰文件設定
```bash
chmod 400 POC.pem
```

## 🔧 實例狀態

### ✅ 已安裝
- Git
- SSH服務
- 基本Ubuntu系統

### ❌ 需要安裝
- Docker
- Node.js
- npm
- FamilyTree應用
- 資料庫 (PostgreSQL/SQLite)
- 網頁服務器

## 🗄️ S3 存儲桶

### familytree-deploy-1564
- **創建時間**: 2025-07-19 20:38:46
- **內容**:
  - `familytree-deploy.tar.gz` (40,661 bytes)
  - 最後更新: 2025-07-19 21:01:42

## 🚀 應用訪問URL (待部署)

- **前端**: http://52.62.99.253:4200 (未部署)
- **後端API**: http://52.62.99.253:5088 (未部署)
- **HTTP**: http://52.62.99.253 (未部署)

## 🔧 連線測試結果

- ✅ AWS CLI 認證成功
- ✅ S3 存儲桶可訪問
- ✅ SSH連線成功
- ❌ 應用服務未部署
- ❌ Web端口無響應

## 📝 部署狀態

**當前狀態**: 全新EC2實例，需要完整部署流程
**下一步**: 安裝Docker和部署應用

---

### 舊實例 (已終止)
- **實例ID**: i-07e8d754a2a75b2b1 (terminated)
- **密鑰文件**: TreeTest.pem (舊)