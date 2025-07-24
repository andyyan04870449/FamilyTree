# FamilyTree AWS 資源文檔總覽

本目錄包含 FamilyTree 項目在 AWS 上的完整連線資訊和管理文檔。

## 📁 文件說明

### 📊 `aws-connection-info.md`
**主要連線資訊文檔**
- AWS 基本配置 (區域、認證)
- EC2 實例詳細資訊 (IP、安全組、網絡)
- S3 存儲桶內容
- 訪問方式和故障排除指南

### 🔧 `aws-cli-commands.md`
**AWS CLI 命令參考手冊**
- 常用的 EC2 管理命令
- S3 存儲桶操作
- 安全組配置
- 監控和日誌查詢
- 故障排除命令

### ⚡ `quick-aws-check.sh`
**可執行的快速檢查腳本**
- 自動檢查 AWS 連接狀態
- 驗證 EC2 實例運行狀態
- 測試服務端口連通性
- 生成狀態總結報告

## 🚀 快速開始

### 1. 檢查當前狀態
```bash
# 運行快速檢查腳本
./quick-aws-check.sh
```

### 2. 連接到服務器
```bash
# SSH 連接 (需要 TreeTest.pem 密鑰)
ssh -i TreeTest.pem ec2-user@16.176.220.138
```

### 3. 訪問應用
- **前端**: http://16.176.220.138:4200
- **後端API**: http://16.176.220.138:5088
- **HTTP**: http://16.176.220.138

## 🔍 當前狀態概覽

### ✅ 正常運行的服務
- AWS CLI 連接
- EC2 實例 (running)
- S3 存儲桶
- SSH 端口 (22)
- 前端服務 (4200)

### ⚠️ 需要檢查的服務
- HTTP 端口 (80) - 無響應
- 後端API (5088) - 404錯誤

## 🔧 常見操作

### 重啟服務
```bash
# SSH 到服務器後
sudo systemctl restart familytree-backend
sudo systemctl restart familytree-frontend
```

### 檢查服務狀態
```bash
sudo systemctl status familytree-backend
sudo systemctl status familytree-frontend
```

### 查看服務日誌
```bash
sudo journalctl -u familytree-backend -f
sudo journalctl -u familytree-frontend -f
```

### 部署更新
```bash
# 下載最新部署包
aws s3 cp s3://familytree-deploy-1564/familytree-deploy.tar.gz ./
tar -xzf familytree-deploy.tar.gz
# 然後按部署流程進行更新
```

## 📞 支援資訊

- **AWS 區域**: ap-southeast-2 (澳洲悉尼)
- **實例ID**: i-07e8d754a2a75b2b1
- **公網IP**: 16.176.220.138
- **存儲桶**: familytree-deploy-1564

## 🕒 文檔更新時間

**最後更新**: 2025-01-27 23:39  
**檢查者**: AI Assistant  
**狀態**: 所有資源正常，部分服務需要調試

---

*提示：建議定期運行 `./quick-aws-check.sh` 來監控服務狀態* 