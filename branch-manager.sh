#!/bin/bash

# 分支管理腳本
# 此檔案的目的：管理不同開發環境的分支
# 主要功能：切換分支、創建新分支、查看分支狀態

echo "🌿 分支管理工具"
echo "================"

# 顯示當前分支
CURRENT_BRANCH=$(git branch --show-current)
echo "📍 當前分支: $CURRENT_BRANCH"

echo ""
echo "📋 可用分支:"
git branch -a

echo ""
echo "🔧 分支操作選項:"
echo "1. 切換到 main 分支"
echo "2. 切換到 development 分支"
echo "3. 創建新的功能分支"
echo "4. 查看分支差異"
echo "5. 合併分支"
echo "6. 刪除分支"
echo "7. 退出"

read -p "請選擇操作 (1-7): " choice

case $choice in
    1)
        echo "🔄 切換到 main 分支..."
        git checkout main
        echo "✅ 已切換到 main 分支"
        ;;
    2)
        echo "🔄 切換到 development 分支..."
        git checkout development
        echo "✅ 已切換到 development 分支"
        ;;
    3)
        read -p "請輸入新分支名稱: " new_branch
        echo "🌿 創建新分支: $new_branch"
        git checkout -b $new_branch
        echo "✅ 新分支已創建並切換"
        ;;
    4)
        echo "📊 查看分支差異..."
        if [ "$CURRENT_BRANCH" = "main" ]; then
            git diff development
        else
            git diff main
        fi
        ;;
    5)
        echo "🔀 合併分支..."
        if [ "$CURRENT_BRANCH" = "main" ]; then
            echo "合併 development 到 main"
            git merge development
        else
            echo "合併 main 到 $CURRENT_BRANCH"
            git merge main
        fi
        ;;
    6)
        read -p "請輸入要刪除的分支名稱: " delete_branch
        if [ "$delete_branch" = "main" ] || [ "$delete_branch" = "development" ]; then
            echo "⚠️  不能刪除主要分支"
        else
            echo "🗑️  刪除分支: $delete_branch"
            git branch -d $delete_branch
        fi
        ;;
    7)
        echo "👋 再見！"
        exit 0
        ;;
    *)
        echo "❌ 無效選擇"
        ;;
esac

echo ""
echo "📍 當前分支: $(git branch --show-current)" 