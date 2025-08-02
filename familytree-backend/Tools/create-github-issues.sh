#!/bin/bash

# GitHub Issues 創建腳本
# 使用方法: ./create-github-issues.sh

echo "🔧 正在創建 GitHub Issues..."

# 檢查 gh CLI 是否已認證
if ! gh auth status > /dev/null 2>&1; then
    echo "❌ GitHub CLI 未認證"
    echo "請執行: gh auth login"
    echo "或提供 GitHub token: export GITHUB_TOKEN=your_token"
    exit 1
fi

# Issue 1: person_profile 表結構重構
echo "📋 創建 Issue 1: person_profile 表結構重構"
gh issue create \
    --title "資料庫優化：person_profile表結構重構" \
    --label "database,performance,optimization,high-priority" \
    --body-file "Documentation/github-issues/issue-01-person-profile-optimization.md"

if [ $? -eq 0 ]; then
    echo "✅ Issue 1 創建成功"
else
    echo "❌ Issue 1 創建失敗"
fi

# Issue 2: 資料隔離策略標準化  
echo "📋 創建 Issue 2: 資料隔離策略標準化"
gh issue create \
    --title "資料隔離策略不一致：統一採用user_id隔離機制" \
    --label "database,security,architecture,high-priority" \
    --body-file "Documentation/github-issues/issue-02-data-isolation-standardization.md"

if [ $? -eq 0 ]; then
    echo "✅ Issue 2 創建成功"
else
    echo "❌ Issue 2 創建失敗"
fi

echo "🎉 GitHub Issues 創建完成！"
echo "請到 GitHub repository 查看: https://github.com/andyyan04870449/FamilyTree/issues"