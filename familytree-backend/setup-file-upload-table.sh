#!/bin/bash

echo "🔧 建立檔案上傳資料庫表格..."

# 檢查是否在正確的目錄
if [ ! -f "familytree-backend.csproj" ]; then
    echo "❌ 錯誤：找不到 familytree-backend.csproj 檔案"
    echo "請確保你在 familytree-backend 目錄下運行此腳本"
    exit 1
fi

echo "📁 進入後端目錄: $(pwd)"

# 檢查 SQL 檔案是否存在
if [ ! -f "create_user_update_file_table.sql" ]; then
    echo "❌ 錯誤：找不到 create_user_update_file_table.sql 檔案"
    exit 1
fi

echo "✅ 找到 SQL 檔案"

# 檢查環境變數
if [ -z "$DATABASE_URL" ]; then
    echo "⚠️  警告：DATABASE_URL 環境變數未設定"
    echo "請確保資料庫連線字串已正確設定"
    echo "你可以使用以下格式設定："
    echo "export DATABASE_URL='Host=localhost;Database=familytree;Username=your_username;Password=your_password'"
    echo ""
    read -p "是否繼續執行？(y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "❌ 操作已取消"
        exit 1
    fi
fi

# 執行 SQL 腳本
echo "🗄️  執行資料庫表格建立腳本..."

# 使用 psql 執行 SQL 腳本
if command -v psql &> /dev/null; then
    echo "使用 psql 執行 SQL 腳本..."
    
    # 從 DATABASE_URL 解析連線參數
    if [ ! -z "$DATABASE_URL" ]; then
        # 簡單的 URL 解析（實際使用中可能需要更複雜的解析）
        DB_HOST=$(echo $DATABASE_URL | grep -o 'Host=[^;]*' | cut -d'=' -f2)
        DB_NAME=$(echo $DATABASE_URL | grep -o 'Database=[^;]*' | cut -d'=' -f2)
        DB_USER=$(echo $DATABASE_URL | grep -o 'Username=[^;]*' | cut -d'=' -f2)
        DB_PASS=$(echo $DATABASE_URL | grep -o 'Password=[^;]*' | cut -d'=' -f2)
        
        if [ ! -z "$DB_HOST" ] && [ ! -z "$DB_NAME" ] && [ ! -z "$DB_USER" ]; then
            PGPASSWORD=$DB_PASS psql -h $DB_HOST -U $DB_USER -d $DB_NAME -f create_user_update_file_table.sql
            if [ $? -eq 0 ]; then
                echo "✅ 資料庫表格建立成功"
            else
                echo "❌ 資料庫表格建立失敗"
                exit 1
            fi
        else
            echo "❌ 無法解析資料庫連線參數"
            exit 1
        fi
    else
        echo "❌ DATABASE_URL 未設定"
        exit 1
    fi
else
    echo "⚠️  psql 命令未找到，請手動執行 SQL 腳本"
    echo "SQL 檔案位置: $(pwd)/create_user_update_file_table.sql"
    echo ""
    echo "你可以使用以下命令手動執行："
    echo "psql -h your_host -U your_username -d your_database -f create_user_update_file_table.sql"
fi

echo ""
echo "🎉 檔案上傳表格設定完成！"
echo "📁 上傳目錄將在應用程式啟動時自動建立"
echo ""
echo "下一步："
echo "1. 啟動後端服務: ./start-backend.sh"
echo "2. 測試檔案上傳 API: POST /api/fileupload/upload"
echo "3. 查看檔案列表 API: GET /api/fileupload/list" 