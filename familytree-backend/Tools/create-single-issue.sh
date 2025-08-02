#!/bin/bash

# 創建單個 GitHub Issue 的簡化腳本
# 使用方法: ./create-single-issue.sh "Issue標題" "Issue內容檔案路徑" "label1,label2"

if [ -z "$GITHUB_TOKEN" ]; then
    echo "❌ 請設定 GITHUB_TOKEN 環境變數"
    echo "1. 到 GitHub → Settings → Developer settings → Personal access tokens"
    echo "2. 創建新的 Classic token，勾選 'repo' 權限"
    echo "3. export GITHUB_TOKEN=your_token_here"
    exit 1
fi

if [ $# -ne 3 ]; then
    echo "使用方法: $0 \"標題\" \"內容檔案路徑\" \"label1,label2\""
    echo "範例: $0 \"資料庫優化\" \"issue-01.md\" \"database,performance\""
    exit 1
fi

TITLE="$1"
BODY_FILE="$2"
LABELS="$3"

if [ ! -f "$BODY_FILE" ]; then
    echo "❌ 找不到檔案: $BODY_FILE"
    exit 1
fi

# 讀取內容並轉換為JSON格式
BODY_CONTENT=$(cat "$BODY_FILE" | jq -Rs .)

# 轉換labels為JSON陣列
IFS=',' read -ra LABEL_ARRAY <<< "$LABELS"
LABELS_JSON="["
for i in "${!LABEL_ARRAY[@]}"; do
    if [ $i -gt 0 ]; then
        LABELS_JSON="$LABELS_JSON,"
    fi
    LABELS_JSON="$LABELS_JSON\"${LABEL_ARRAY[i]}\""
done
LABELS_JSON="$LABELS_JSON]"

echo "📋 正在創建 Issue: $TITLE"

# 創建Issue
RESPONSE=$(curl -s -w "%{http_code}" -o /tmp/gh_response.json -X POST \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  "https://api.github.com/repos/andyyan04870449/FamilyTree/issues" \
  -d "{
    \"title\": \"$TITLE\",
    \"body\": $BODY_CONTENT,
    \"labels\": $LABELS_JSON
  }")

HTTP_CODE="${RESPONSE: -3}"

if [ "$HTTP_CODE" = "201" ]; then
    ISSUE_URL=$(cat /tmp/gh_response.json | jq -r '.html_url')
    ISSUE_NUMBER=$(cat /tmp/gh_response.json | jq -r '.number')
    echo "✅ Issue #$ISSUE_NUMBER 創建成功"
    echo "🔗 URL: $ISSUE_URL"
else
    echo "❌ Issue 創建失敗 (HTTP $HTTP_CODE)"
    echo "回應內容:"
    cat /tmp/gh_response.json
fi

rm -f /tmp/gh_response.json