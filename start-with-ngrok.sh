#!/bin/bash

# 此檔案的目的：整合啟動家族樹應用和 ngrok 隧道，提供完整的遠程訪問解決方案
# 主要功能：同時啟動前後端服務和 ngrok 隧道，並顯示公開網址

# 載入 ngrok 配置
if [ -f "ngrok-config.sh" ]; then
    source ngrok-config.sh
else
    echo "⚠️  找不到 ngrok-config.sh，使用預設配置"
    export NGROK_AUTHTOKEN="2wcEhFbbW0lJlInEZPqKNzHhfFv_6EoK7JfA5h2BjoNca35yu"
    export FRONTEND_SUBDOMAIN="familytree-frontend"
    export BACKEND_SUBDOMAIN="familytree-backend"
    export FRONTEND_PORT="4200"
    export BACKEND_PORT="5087"
    export NGROK_UI_PORT="4040"
    export TUNNEL_START_DELAY="3"
    export TUNNEL_INIT_DELAY="5"
    export MAX_RETRIES="5"
    export RETRY_DELAY="3"
fi

echo "🚀 啟動家族樹應用 + ngrok 固定網址隧道..."

# 檢查是否在正確的目錄
if [ ! -d "familytree-backend" ] || [ ! -d "familytree-frontend" ]; then
    echo "❌ 錯誤：找不到後端或前端目錄"
    echo "請確保你在 FamilyTree 根目錄下運行此腳本"
    exit 1
fi

# 檢查必要工具
if ! command -v dotnet &> /dev/null; then
    echo "❌ 錯誤：找不到 dotnet 命令"
    echo "請確保已安裝 .NET Core 8 SDK"
    exit 1
fi

if ! command -v node &> /dev/null; then
    echo "❌ 錯誤：找不到 node 命令"
    echo "請確保已安裝 Node.js"
    exit 1
fi

if ! command -v ngrok &> /dev/null; then
    echo "❌ 錯誤：找不到 ngrok 命令"
    echo "請先安裝 ngrok: brew install ngrok/ngrok/ngrok"
    exit 1
fi

echo "✅ 環境檢查通過"

# 清理可能佔用的端口
echo "🧹 清理可能佔用的端口..."
lsof -ti :5087 | xargs kill -9 2>/dev/null
lsof -ti :4200 | xargs kill -9 2>/dev/null
pkill ngrok 2>/dev/null
sleep 2

# 函數：啟動後端
start_backend() {
    echo "🔧 啟動後端服務器..."
    cd familytree-backend
    dotnet run --urls "http://localhost:5087" &
    BACKEND_PID=$!
    cd ..
    echo "✅ 後端已啟動 (PID: $BACKEND_PID)"
}

# 函數：啟動前端
start_frontend() {
    echo "🎨 啟動前端服務器..."
    cd familytree-frontend
    npm start &
    FRONTEND_PID=$!
    cd ..
    echo "✅ 前端已啟動 (PID: $FRONTEND_PID)"
}

# 函數：啟動 ngrok 隧道（單一會話，多個隧道）
start_ngrok() {
    echo "🌐 啟動 ngrok 固定網址隧道..."
    
    # 確保 ngrok 進程已停止
    pkill -f ngrok 2>/dev/null
    sleep 2
    
    # 檢查是否為付費版（有固定網址）
    echo "🔍 使用付費版固定網址功能..."
    echo "📱 前端固定網址: https://${FRONTEND_SUBDOMAIN}.ngrok.io"
    echo "🔧 後端固定網址: https://${BACKEND_SUBDOMAIN}.ngrok.io"
    
    # 創建 ngrok 配置文件
    create_ngrok_config
    
    # 啟動單一 ngrok 會話，包含多個隧道
    echo "🚀 啟動 ngrok 會話（包含前端和後端隧道）..."
    ngrok start --all --config=./ngrok-temp.yml > /dev/null 2>&1 &
    NGROK_PID=$!
    
    echo "✅ ngrok 固定網址隧道已啟動"
    echo "⏳ 等待隧道初始化..."
    sleep ${TUNNEL_INIT_DELAY}
}

# 函數：創建臨時 ngrok 配置
create_ngrok_config() {
    echo "📝 創建 ngrok 配置檔案..."
    
    cat > ./ngrok-temp.yml << EOF
version: "2"
authtoken: ${NGROK_AUTHTOKEN}
tunnels:
  frontend:
    addr: ${FRONTEND_PORT}
    proto: http
    host_header: localhost:${FRONTEND_PORT}
    url: https://${FRONTEND_SUBDOMAIN}.ngrok.io
  backend:
    addr: ${BACKEND_PORT}
    proto: http
    host_header: localhost:${BACKEND_PORT}
    url: https://${BACKEND_SUBDOMAIN}.ngrok.io
EOF
    
    echo "✅ 配置檔案已創建: ./ngrok-temp.yml"
}

# 函數：顯示公開網址
show_public_urls() {
    echo ""
    echo "⏳ 等待隧道建立..."
    
    # 增加等待時間，確保隧道完全建立
    sleep 10
    
    echo ""
    echo "🌐 固定網址資訊："
    echo "=================="
    
    # 從 API 獲取實際的固定網址
    echo "🔍 獲取實際的固定網址..."
    
    # 重試機制：最多嘗試 5 次，每次間隔 3 秒
    RETRY_COUNT=0
    
    while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
        echo "🔄 獲取固定網址 (第 $((RETRY_COUNT + 1)) 次)..."
        
        # 獲取 ngrok API 響應
        TUNNELS_RESPONSE=$(curl -s http://localhost:${NGROK_UI_PORT}/api/tunnels 2>/dev/null)
        
        if [ ! -z "$TUNNELS_RESPONSE" ]; then
                    # 使用 jq 解析 JSON（如果可用）
        if command -v jq &> /dev/null; then
            FRONTEND_URL=$(echo "$TUNNELS_RESPONSE" | jq -r '.tunnels[] | select(.config.addr | contains("4200")) | .public_url' 2>/dev/null | head -1)
            BACKEND_URL=$(echo "$TUNNELS_RESPONSE" | jq -r '.tunnels[] | select(.config.addr | contains("5087")) | .public_url' 2>/dev/null | head -1)
        else
            # 備用方案：使用 grep 和 sed
            FRONTEND_URL=$(echo "$TUNNELS_RESPONSE" | grep -o '"public_url":"[^"]*"' | grep "4200" | sed 's/"public_url":"//g' | sed 's/"//g' | head -1)
            BACKEND_URL=$(echo "$TUNNELS_RESPONSE" | grep -o '"public_url":"[^"]*"' | grep "5087" | sed 's/"public_url":"//g' | sed 's/"//g' | head -1)
        fi
        
        # 如果無法從 API 獲取，使用預設的固定網址
        if [ -z "$FRONTEND_URL" ]; then
            FRONTEND_URL="https://${FRONTEND_SUBDOMAIN}.ngrok.io"
        fi
        if [ -z "$BACKEND_URL" ]; then
            BACKEND_URL="https://${BACKEND_SUBDOMAIN}.ngrok.io"
        fi
            
            if [ ! -z "$FRONTEND_URL" ] && [ ! -z "$BACKEND_URL" ]; then
                echo "✅ 成功獲取固定網址！"
                break
            fi
        fi
        
        echo "⚠️  網址尚未就緒，等待重試..."
        sleep ${RETRY_DELAY}
        RETRY_COUNT=$((RETRY_COUNT + 1))
    done
    
    # 顯示結果
    if [ ! -z "$FRONTEND_URL" ]; then
        echo "📱 前端固定網址: $FRONTEND_URL"
    else
        echo "❌ 無法獲取前端固定網址"
        echo "💡 請檢查 ngrok 管理界面: http://localhost:${NGROK_UI_PORT}"
    fi
    
    if [ ! -z "$BACKEND_URL" ]; then
        echo "🔧 後端固定網址: $BACKEND_URL"
    else
        echo "❌ 無法獲取後端固定網址"
        echo "💡 請檢查 ngrok 管理界面: http://localhost:${NGROK_UI_PORT}"
    fi
    
    # 驗證隧道是否正常運行
    echo ""
    echo "🔍 驗證隧道狀態..."
    
    # 重試機制：最多嘗試 5 次，每次間隔 3 秒
    RETRY_COUNT=0
    
    while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
        echo "🔄 驗證隧道連接 (第 $((RETRY_COUNT + 1)) 次)..."
        
        # 檢查 ngrok API 是否響應
        TUNNELS_RESPONSE=$(curl -s http://localhost:${NGROK_UI_PORT}/api/tunnels 2>/dev/null)
        
        if [ ! -z "$TUNNELS_RESPONSE" ]; then
            echo "✅ 隧道已成功建立！"
            break
        fi
        
        echo "⚠️  隧道尚未就緒，等待重試..."
        sleep ${RETRY_DELAY}
        RETRY_COUNT=$((RETRY_COUNT + 1))
    done
    
    echo ""
    echo "📊 ngrok 管理界面: http://localhost:${NGROK_UI_PORT}"
    echo "🏠 本地前端: http://localhost:${FRONTEND_PORT}"
    echo "🔧 本地後端: http://localhost:${BACKEND_PORT}"
    echo ""
    echo "💡 將前端固定網址分享給客戶即可！"
    echo "🔒 使用固定網址，每次重啟都不會變動！"
    echo "🎯 前端固定網址: $FRONTEND_URL"
}

# 函數：清理進程
cleanup() {
    echo ""
    echo "🛑 正在停止所有服務..."
    
    # 停止後端
    if [ ! -z "$BACKEND_PID" ]; then
        kill $BACKEND_PID 2>/dev/null
        echo "✅ 後端已停止"
    fi
    
    # 停止前端
    if [ ! -z "$FRONTEND_PID" ]; then
        kill $FRONTEND_PID 2>/dev/null
        echo "✅ 前端已停止"
    fi
    
    # 停止 ngrok 進程
    if [ ! -z "$NGROK_PID" ]; then
        kill $NGROK_PID 2>/dev/null
    fi
    pkill -f ngrok 2>/dev/null
    echo "✅ ngrok 已停止"
    
    # 清理端口
    lsof -ti :5087 | xargs kill -9 2>/dev/null
    lsof -ti :4200 | xargs kill -9 2>/dev/null
    
    # 清理臨時檔案
    rm -f ./ngrok-temp.yml 2>/dev/null
    
    echo "🧹 清理完成"
    exit 0
}

# 設置信號處理
trap cleanup SIGINT SIGTERM

# 啟動所有服務
start_backend
sleep 3
start_frontend
sleep 5
start_ngrok

# 顯示公開網址
show_public_urls

echo ""
echo "按 Ctrl+C 停止所有服務"

# 等待用戶中斷
wait 