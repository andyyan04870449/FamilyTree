# 🔍 EC2 連接問題診斷指南

## 問題描述
無法連接到 EC2 實例 `i-07e8d754a2a75b2b1`

## 常見問題和解決方案

### 1. 實例未運行
**症狀**: 無法獲取實例 IP 或實例狀態不是 "running"

**解決方案**:
```bash
# 啟動實例
aws ec2 start-instances --instance-ids i-07e8d754a2a75b2b1 --region ap-southeast-2

# 等待實例啟動
aws ec2 wait instance-running --instance-ids i-07e8d754a2a75b2b1 --region ap-southeast-2
```

### 2. 安全群組問題
**症狀**: 實例運行但無法 SSH 連接

**解決方案**:
1. 在 AWS 控制台中檢查安全群組
2. 確保開放端口 22 (SSH)
3. 添加規則：
   - 類型: SSH
   - 協議: TCP
   - 端口範圍: 22
   - 來源: 0.0.0.0/0 (或您的 IP)

### 3. SSH 金鑰問題
**症狀**: SSH 連接被拒絕

**解決方案**:
```bash
# 檢查金鑰權限
chmod 400 your-key.pem

# 嘗試連接
ssh -i your-key.pem ubuntu@16.176.220.138
```

### 4. 網路配置問題
**症狀**: 實例沒有公共 IP

**解決方案**:
1. 檢查實例是否在公共子網中
2. 確保有彈性 IP 或自動分配公共 IP
3. 檢查路由表設置

## 診斷步驟

### 步驟 1: 檢查實例狀態
```bash
aws ec2 describe-instances \
  --instance-ids i-07e8d754a2a75b2b1 \
  --region ap-southeast-2 \
  --query 'Reservations[0].Instances[0].[State.Name,PublicIpAddress,KeyName]' \
  --output table
```

### 步驟 2: 檢查安全群組
```bash
# 獲取安全群組 ID
SECURITY_GROUP=$(aws ec2 describe-instances \
  --instance-ids i-07e8d754a2a75b2b1 \
  --region ap-southeast-2 \
  --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' \
  --output text)

# 檢查安全群組規則
aws ec2 describe-security-groups \
  --group-ids $SECURITY_GROUP \
  --region ap-southeast-2
```

### 步驟 3: 測試網路連接
```bash
# 測試 SSH 端口
nc -z -w5 16.176.220.138 22

# 測試 HTTP 端口
nc -z -w5 16.176.220.138 80
```

## 快速修復腳本

### 修復安全群組
```bash
# 獲取安全群組 ID
SECURITY_GROUP=$(aws ec2 describe-instances \
  --instance-ids i-07e8d754a2a75b2b1 \
  --region ap-southeast-2 \
  --query 'Reservations[0].Instances[0].SecurityGroups[0].GroupId' \
  --output text)

# 添加 SSH 規則
aws ec2 authorize-security-group-ingress \
  --group-id $SECURITY_GROUP \
  --protocol tcp \
  --port 22 \
  --cidr 0.0.0.0/0 \
  --region ap-southeast-2

# 添加 HTTP 規則
aws ec2 authorize-security-group-ingress \
  --group-id $SECURITY_GROUP \
  --protocol tcp \
  --port 80 \
  --cidr 0.0.0.0/0 \
  --region ap-southeast-2

# 添加 HTTPS 規則
aws ec2 authorize-security-group-ingress \
  --group-id $SECURITY_GROUP \
  --protocol tcp \
  --port 443 \
  --cidr 0.0.0.0/0 \
  --region ap-southeast-2

# 添加應用程式端口
aws ec2 authorize-security-group-ingress \
  --group-id $SECURITY_GROUP \
  --protocol tcp \
  --port 4200 \
  --cidr 0.0.0.0/0 \
  --region ap-southeast-2

aws ec2 authorize-security-group-ingress \
  --group-id $SECURITY_GROUP \
  --protocol tcp \
  --port 5088 \
  --cidr 0.0.0.0/0 \
  --region ap-southeast-2
```

## 替代連接方法

### 使用 AWS Systems Manager (SSM)
如果 SSH 無法連接，可以嘗試使用 SSM：

```bash
# 檢查 SSM 連接
aws ssm describe-instance-information \
  --filters "Key=InstanceIds,Values=i-07e8d754a2a75b2b1" \
  --region ap-southeast-2

# 通過 SSM 連接
aws ssm start-session \
  --target i-07e8d754a2a75b2b1 \
  --region ap-southeast-2
```

### 使用 AWS 控制台
1. 登入 AWS 控制台
2. 前往 EC2 服務
3. 選擇實例 `i-07e8d754a2a75b2b1`
4. 點擊 "連接"
5. 選擇 "EC2 Instance Connect" 或 "Session Manager"

## 檢查清單

- [ ] 實例狀態是 "running"
- [ ] 實例有公共 IP 地址
- [ ] 安全群組開放端口 22
- [ ] SSH 金鑰檔案存在且權限正確
- [ ] 網路連接正常
- [ ] 防火牆沒有阻擋連接

## 需要幫助？

如果以上步驟都無法解決問題，請：
1. 檢查 AWS 控制台中的實例狀態
2. 查看 CloudWatch 日誌
3. 聯繫 AWS 支援 