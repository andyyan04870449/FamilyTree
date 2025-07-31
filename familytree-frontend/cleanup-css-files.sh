#!/bin/bash

# CSS 優化後的檔案清理腳本

echo "開始清理 CSS 優化過程中的範例和測試檔案..."

# 1. 刪除示範/測試檔案
echo "刪除示範檔案..."
rm -f src/styles/components/_card-mobile-first.scss
rm -f src/styles/components/_table-mobile-first.scss
rm -f src/app/pages/login/login.mobile-first.scss
rm -f src/app/pages/login/login.page.bem.scss
rm -f src/app/pages/person-list/person-list.optimized.scss
rm -f src/app/components/sidebar-nav/sidebar-nav.optimized.scss

# 2. 備份舊版組件檔案（以防萬一）
echo "備份舊版組件檔案..."
mkdir -p backup/old-components
cp src/styles/components/_buttons.scss backup/old-components/ 2>/dev/null || true
cp src/styles/components/_cards.scss backup/old-components/ 2>/dev/null || true
cp src/styles/components/_forms.scss backup/old-components/ 2>/dev/null || true
cp src/styles/components/_tables.scss backup/old-components/ 2>/dev/null || true

# 3. 詢問是否刪除舊版檔案
echo ""
echo "以下舊版組件檔案已經整合到 BEM 版本中："
echo "- _buttons.scss → _buttons-bem.scss"
echo "- _cards.scss → _cards-bem.scss"
echo "- _forms.scss → _forms-bem.scss"
echo "- _tables.scss → _tables-bem.scss"
echo ""
read -p "是否刪除這些舊版檔案？(y/n) " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]
then
    echo "刪除舊版組件檔案..."
    rm -f src/styles/components/_buttons.scss
    rm -f src/styles/components/_cards.scss
    rm -f src/styles/components/_forms.scss
    rm -f src/styles/components/_tables.scss
fi

# 4. 檢查需要手動評估的檔案
echo ""
echo "以下檔案需要手動評估："
echo "- src/styles/components/_unified-design.scss - 檢查是否還在使用"
echo "- src/styles/components/_select.scss - 考慮整合到 _forms-bem.scss"

echo ""
echo "清理完成！"
echo ""
echo "注意事項："
echo "1. 請確保所有組件都已更新為使用 BEM 版本"
echo "2. 備份檔案保存在 backup/old-components/ 目錄"
echo "3. 建議在刪除檔案後重新編譯專案以確保沒有破壞任何引用"