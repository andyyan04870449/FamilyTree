#!/bin/bash

# FamilyTree CI/CD 快速設置腳本
set -e

echo "🚀 FamilyTree CI/CD 快速設置"
echo "================================"

# 檢查必要檔案
echo "📋 檢查必要檔案..."

if [ ! -f "TreeTest.pem" ]; then
    echo "❌ 找不到 SSH 金鑰檔案: TreeTest.pem"
    echo "請確保 SSH 金鑰檔案存在於當前目錄"
    exit 1
fi

if [ ! -f ".github/workflows/deploy.yml" ]; then
    echo "❌ 找不到 GitHub Actions 工作流程檔案"
    exit 1
fi

if [ ! -f "scripts/deploy-on-server.sh" ]; then
    echo "❌ 找不到部署腳本"
    exit 1
fi

echo "✅ 所有必要檔案都存在"

# 設置 Git 倉庫
echo ""
echo "📦 設置 Git 倉庫..."

if [ ! -d ".git" ]; then
    echo "初始化 Git 倉庫..."
    git init
else
    echo "Git 倉庫已存在"
fi

# 添加檔案
echo "添加檔案到 Git..."
git add .

# 檢查是否有變更
if git diff --cached --quiet; then
    echo "沒有新的變更需要提交"
else
    echo "提交變更..."
    git commit -m "Setup CI/CD automation"
fi

# 顯示 SSH 金鑰內容
echo ""
echo "🔑 請將以下 SSH 私鑰內容添加到 GitHub Secrets:"
echo "================================"
cat TreeTest.pem
echo "================================"
echo ""
echo "📋 設置步驟："
echo "1. 複製上面的 SSH 私鑰內容"
echo "2. 進入您的 GitHub 倉庫"
echo "3. 點擊 Settings → Secrets and variables → Actions"
echo "4. 點擊 New repository secret"
echo "5. Name: EC2_SSH_KEY"
echo "6. Value: 貼上剛才複製的 SSH 私鑰內容"
echo "7. 點擊 Add secret"
echo ""
echo "🌐 推送代碼到 GitHub："
echo "git remote add origin https://github.com/yourusername/FamilyTree.git"
echo "git push -u origin main"
echo ""
echo "✅ 設置完成！推送代碼後將自動觸發部署。" 