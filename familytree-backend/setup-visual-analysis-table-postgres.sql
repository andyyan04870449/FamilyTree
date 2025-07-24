-- 建立視覺化分析圖資料表 (PostgreSQL版本)
-- 功能：管理視覺化關聯分析圖的資料

-- 建立視覺化分析圖資料表
CREATE TABLE IF NOT EXISTS visual_analysis_graphs (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,                    -- 分析圖名稱
    project_ids TEXT,                              -- 專案ID（逗號分隔）
    updated_by VARCHAR(100) DEFAULT 'user',       -- 更新人
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- 最後更新時間
);

-- 建立索引
CREATE INDEX IF NOT EXISTS idx_visual_analysis_updated_at ON visual_analysis_graphs(updated_at);
CREATE INDEX IF NOT EXISTS idx_visual_analysis_name ON visual_analysis_graphs(name);

-- 插入測試資料
INSERT INTO visual_analysis_graphs (name, project_ids, updated_by, updated_at) VALUES 
('關聯分析圖_20250723001', '782093,888888', 'user', '2024-12-15 10:30:00'),
('關聯分析圖_20250723002', '893148', 'user', '2024-12-15 11:00:00'),
('關聯分析圖_20250723003', '782093', 'user', '2024-12-15 12:15:00'),
('關聯分析圖_20250723004', '888888,893148', 'user', '2024-12-15 14:20:00'),
('關聯分析圖_20250723005', '782093,888888,893148', 'user', '2024-12-15 16:45:00')
ON CONFLICT (name) DO NOTHING;

-- 顯示建立結果
SELECT 'visual_analysis_graphs 資料表建立完成，包含' || COUNT(*) || '筆測試資料' as result FROM visual_analysis_graphs; 