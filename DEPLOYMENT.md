# FamilyTree 專案部署指南

## 自動化部署設置

### 1. GitHub Secrets 設置

在您的 GitHub 專案中設置以下 Secrets：

- `AWS_ACCESS_KEY_ID`: 您的 AWS Access Key ID
- `AWS_SECRET_ACCESS_KEY`: 您的 AWS Secret Access Key

### 2. EC2 實例設置

#### 2.1 安裝必要軟體
```bash
# 在 EC2 實例上執行
chmod +x ec2-setup.sh
./ec2-setup.sh
```

#### 2.2 設置 Git 憑證
```bash
# 設置 Git 用戶資訊
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"

# 克隆專案
cd /home/ubuntu
git clone https://github.com/your-username/FamilyTree.git
```

#### 2.3 設置 SSH 金鑰（可選）
```bash
# 生成 SSH 金鑰
ssh-keygen -t rsa -b 4096 -C "your.email@example.com"

# 將公鑰添加到 GitHub
cat ~/.ssh/id_rsa.pub
```

### 3. AWS IAM 權限

確保您的 EC2 實例具有以下 IAM 權限：

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ssm:SendCommand",
                "ssm:DescribeInstanceInformation",
                "ssm:ListCommands",
                "ssm:ListCommandInvocations"
            ],
            "Resource": "*"
        }
    ]
}
```

### 4. 安全群組設置

確保 EC2 安全群組允許以下端口：
- 22 (SSH)
- 80 (HTTP)
- 443 (HTTPS)
- 5088 (後端 API)

### 5. 部署流程

1. 推送代碼到 `main` 分支
2. GitHub Actions 自動觸發部署
3. 構建 Docker 映像
4. 通過 SSM 在 EC2 上執行部署腳本
5. 重啟服務

### 6. 監控和日誌

#### 查看容器狀態
```bash
docker-compose ps
```

#### 查看日誌
```bash
# 後端日誌
docker-compose logs familytree-backend

# 前端日誌
docker-compose logs familytree-frontend
```

#### 重啟服務
```bash
docker-compose restart
```

### 7. 故障排除

#### 檢查服務狀態
```bash
# 檢查後端健康狀態
curl http://localhost:5088/ping

# 檢查前端
curl http://localhost:4200
```

#### 重新部署
```bash
cd /home/ubuntu/FamilyTree
./deploy.sh
```

### 8. 環境變數

在 `docker-compose.yml` 中設置必要的環境變數：

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:80
  - DATABASE_CONNECTION_STRING=your_connection_string
```

## 注意事項

1. **安全**: 請立即輪換 AWS 憑證
2. **備份**: 定期備份資料庫和重要檔案
3. **監控**: 設置 CloudWatch 監控
4. **日誌**: 配置集中式日誌管理 