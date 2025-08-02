#!/bin/bash

# 使用 GitHub API 創建 Issues
# 使用方法: GITHUB_TOKEN=your_token ./create-issues-api.sh

if [ -z "$GITHUB_TOKEN" ]; then
    echo "❌ 請設定 GITHUB_TOKEN 環境變數"
    echo "例如: export GITHUB_TOKEN=ghp_xxxxxxxxxxxx"
    echo "取得 token: https://github.com/settings/personal-access-tokens/new"
    exit 1
fi

REPO_OWNER="andyyan04870449"
REPO_NAME="FamilyTree"
API_URL="https://api.github.com/repos/$REPO_OWNER/$REPO_NAME/issues"

echo "🔧 使用 GitHub API 創建 Issues..."

# Issue 1: person_profile 表結構重構
echo "📋 創建 Issue 1: person_profile 表結構重構"

ISSUE1_BODY=$(cat Documentation/github-issues/issue-01-person-profile-optimization.md)

curl -X POST \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  "$API_URL" \
  -d @- << EOF
{
  "title": "資料庫優化：person_profile表結構重構",
  "body": $(echo "$ISSUE1_BODY" | jq -Rs .),
  "labels": ["database", "performance", "optimization", "high-priority"]
}
EOF

echo -e "\n"

# Issue 2: 資料隔離策略標準化
echo "📋 創建 Issue 2: 資料隔離策略標準化"

ISSUE2_BODY=$(cat Documentation/github-issues/issue-02-data-isolation-standardization.md)

curl -X POST \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  "$API_URL" \
  -d @- << EOF
{
  "title": "資料隔離策略不一致：統一採用user_id隔離機制", 
  "body": $(echo "$ISSUE2_BODY" | jq -Rs .),
  "labels": ["database", "security", "architecture", "high-priority"]
}
EOF

echo -e "\n🎉 Issues 創建完成！"
echo "查看 Issues: https://github.com/$REPO_OWNER/$REPO_NAME/issues"