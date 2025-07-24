#!/bin/bash

# 設置視覺化分析圖資料表
# 功能：建立 visual_analysis_graphs 資料表

echo "開始建立視覺化分析圖資料表..."

# 使用 SQLite 建立資料表
sqlite3 familytree.db << 'EOF'

-- 建立視覺化分析圖資料表
CREATE TABLE IF NOT EXISTS visual_analysis_graphs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,                    -- 分析圖名稱
    project_ids TEXT,                      -- 專案ID（逗號分隔）
    updated_by TEXT DEFAULT 'user',       -- 更新人
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP -- 最後更新時間
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_visual_analysis_updated_at ON visual_analysis_graphs(updated_at);
CREATE INDEX IF NOT EXISTS idx_visual_analysis_name ON visual_analysis_graphs(name);

-- 插入測試資料
INSERT OR IGNORE INTO visual_analysis_graphs (id, name, project_ids, updated_by, updated_at) VALUES 
(1, '關聯分析圖_20250723001', '782093,888888', 'user', '2024-12-15 10:30:00'),
(2, '關聯分析圖_20250723002', '893148', 'user', '2024-12-15 11:00:00'),
(3, '關聯分析圖_20250723003', '782093', 'user', '2024-12-15 12:15:00'),
(4, '關聯分析圖_20250723004', '888888,893148', 'user', '2024-12-15 14:20:00'),
(5, '關聯分析圖_20250723005', '782093,888888,893148', 'user', '2024-12-15 16:45:00');

-- 顯示建立結果
SELECT 'visual_analysis_graphs 資料表建立完成，包含' || COUNT(*) || '筆測試資料' as result FROM visual_analysis_graphs;

EOF

echo "視覺化分析圖資料表建立完成！" 