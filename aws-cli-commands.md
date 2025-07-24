# AWS CLI 常用命令參考

## 🔧 基本配置

```bash
# 檢查AWS配置
aws configure list

# 檢查當前用戶身份
aws sts get-caller-identity

# 檢查可用區域
aws ec2 describe-regions --output table
```

## 🖥️ EC2 管理

```bash
# 列出所有實例
aws ec2 describe-instances --output table

# 檢查特定實例狀態
aws ec2 describe-instances --instance-ids i-07e8d754a2a75b2b1

# 啟動實例
aws ec2 start-instances --instance-ids i-07e8d754a2a75b2b1

# 停止實例
aws ec2 stop-instances --instance-ids i-07e8d754a2a75b2b1

# 重啟實例
aws ec2 reboot-instances --instance-ids i-07e8d754a2a75b2b1

# 檢查實例狀態
aws ec2 describe-instance-status --instance-ids i-07e8d754a2a75b2b1
```

## 🗄️ S3 管理

```bash
# 列出所有存儲桶
aws s3 ls

# 列出存儲桶內容
aws s3 ls s3://familytree-deploy-1564/

# 下載文件
aws s3 cp s3://familytree-deploy-1564/familytree-deploy.tar.gz ./

# 上傳文件
aws s3 cp ./familytree-deploy.tar.gz s3://familytree-deploy-1564/

# 同步目錄
aws s3 sync ./dist/ s3://familytree-deploy-1564/dist/

# 設置文件權限
aws s3 cp s3://familytree-deploy-1564/familytree-deploy.tar.gz s3://familytree-deploy-1564/familytree-deploy.tar.gz --acl public-read
```

## 🔒 安全組管理

```bash
# 檢查安全組
aws ec2 describe-security-groups --group-ids sg-08626692e93c8f272

# 添加入站規則
aws ec2 authorize-security-group-ingress \
  --group-id sg-08626692e93c8f272 \
  --protocol tcp \
  --port 8080 \
  --cidr 0.0.0.0/0

# 移除入站規則
aws ec2 revoke-security-group-ingress \
  --group-id sg-08626692e93c8f272 \
  --protocol tcp \
  --port 8080 \
  --cidr 0.0.0.0/0
```

## 🔍 監控和日誌

```bash
# 檢查CloudWatch日誌
aws logs describe-log-groups

# 檢查特定日誌流
aws logs describe-log-streams --log-group-name /aws/ec2/familytree

# 獲取日誌事件
aws logs get-log-events \
  --log-group-name /aws/ec2/familytree \
  --log-stream-name "stream-name"
```

## 🚀 部署相關

```bash
# 檢查最新部署包
aws s3 ls s3://familytree-deploy-1564/ --recursive --human-readable

# 下載並解壓部署包
aws s3 cp s3://familytree-deploy-1564/familytree-deploy.tar.gz ./
tar -xzf familytree-deploy.tar.gz

# SSH 連接到實例
ssh -i TreeTest.pem ec2-user@16.176.220.138
```

## ⚡ 快速檢查腳本

```bash
#!/bin/bash
# quick-check.sh - 快速檢查AWS資源狀態

echo "=== AWS 快速狀態檢查 ==="
echo "時間: $(date)"
echo

echo "🖥️  EC2 實例狀態:"
aws ec2 describe-instances \
  --instance-ids i-07e8d754a2a75b2b1 \
  --query 'Reservations[0].Instances[0].[State.Name,PublicIpAddress]' \
  --output text

echo
echo "🗄️  S3 存儲桶內容:"
aws s3 ls s3://familytree-deploy-1564/ --human-readable

echo
echo "🔧 服務連通性測試:"
curl -s -o /dev/null -w "%{http_code}" http://16.176.220.138 || echo "無法連接"
```

## 🆘 故障排除

```bash
# 檢查實例系統日誌
aws ec2 get-console-output --instance-id i-07e8d754a2a75b2b1

# 檢查實例截圖
aws ec2 get-console-screenshot --instance-id i-07e8d754a2a75b2b1

# 檢查網絡連通性
aws ec2 describe-vpc-endpoints
aws ec2 describe-route-tables
``` 