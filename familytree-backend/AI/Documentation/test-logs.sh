#!/bin/bash

# 圖譜分析日誌測試腳本

echo "=== 圖譜分析日誌測試 ==="
echo "開始時間: $(date)"
echo

# 檢查日誌目錄是否存在
LOG_DIR="logs"
if [ ! -d "$LOG_DIR" ]; then
    echo "❌ 日誌目錄不存在: $LOG_DIR"
    echo "請先啟動後端服務以創建日誌目錄"
    exit 1
fi

echo "✅ 日誌目錄存在: $LOG_DIR"

# 檢查日誌檔案
LOG_FILES=$(ls -la $LOG_DIR/familytree-analysis-*.log 2>/dev/null | wc -l)
if [ $LOG_FILES -eq 0 ]; then
    echo "ℹ️ 尚未找到日誌檔案"
    echo "請啟動後端服務並執行分析以產生日誌"
else
    echo "✅ 找到 $LOG_FILES 個日誌檔案"
    
    # 顯示最新的日誌檔案
    LATEST_LOG=$(ls -t $LOG_DIR/familytree-analysis-*.log | head -1)
    echo "📄 最新日誌檔案: $(basename $LATEST_LOG)"
    
    # 顯示檔案大小
    FILE_SIZE=$(du -h "$LATEST_LOG" | cut -f1)
    echo "📊 檔案大小: $FILE_SIZE"
    
    # 顯示最後 10 行日誌
    echo
    echo "📋 最後 10 行日誌內容:"
    echo "----------------------------------------"
    tail -n 10 "$LATEST_LOG"
    echo "----------------------------------------"
fi

echo
echo "=== 日誌查看指令 ==="
echo "1. 即時查看日誌: tail -f $LOG_DIR/familytree-analysis-*.log"
echo "2. 查看最近 100 行: tail -n 100 $LOG_DIR/familytree-analysis-*.log"
echo "3. 查看錯誤日誌: grep '❌\|ERROR' $LOG_DIR/familytree-analysis-*.log"
echo "4. 查看特定人員: grep 'PersonId = 1' $LOG_DIR/familytree-analysis-*.log"
echo "5. API 查看日誌: curl 'http://localhost:5000/api/log/tail?lines=50'"
echo "6. 從專案根目錄執行: ./AI/Documentation/test-logs.sh"
echo

echo "測試完成時間: $(date)" 