-- 創建視覺化分析圖表節點資料表
CREATE TABLE IF NOT EXISTS visual_analysis_nodes (
    id SERIAL PRIMARY KEY,
    graph_id INTEGER NOT NULL,
    project_id VARCHAR(50) NOT NULL,
    person_id INTEGER NOT NULL,
    is_visible BOOLEAN DEFAULT true,
    node_x FLOAT DEFAULT 0,
    node_y FLOAT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT fk_visual_analysis_nodes_graph_id 
        FOREIGN KEY (graph_id) REFERENCES visual_analysis_graphs(id) ON DELETE CASCADE
);

-- 建立索引以提高查詢效能
CREATE INDEX IF NOT EXISTS idx_visual_analysis_nodes_graph_id ON visual_analysis_nodes(graph_id);
CREATE INDEX IF NOT EXISTS idx_visual_analysis_nodes_project_id ON visual_analysis_nodes(project_id);
CREATE INDEX IF NOT EXISTS idx_visual_analysis_nodes_person_id ON visual_analysis_nodes(person_id);

-- 建立唯一約束防止重複資料
CREATE UNIQUE INDEX IF NOT EXISTS idx_visual_analysis_nodes_unique 
    ON visual_analysis_nodes(graph_id, project_id, person_id);

-- 插入測試資料（可選）
-- 假設圖表ID=1包含專案 "888888-20250723205343" 和 "782093-20250723205634"
-- 這裡先不插入測試資料，由API動態生成

COMMENT ON TABLE visual_analysis_nodes IS '視覺化分析圖表節點資料表';
COMMENT ON COLUMN visual_analysis_nodes.graph_id IS '關聯的視覺化分析圖表ID';
COMMENT ON COLUMN visual_analysis_nodes.project_id IS '專案ID';
COMMENT ON COLUMN visual_analysis_nodes.person_id IS '人員ID';
COMMENT ON COLUMN visual_analysis_nodes.is_visible IS '是否在圖表中顯示該節點';
COMMENT ON COLUMN visual_analysis_nodes.node_x IS '節點在畫布上的X座標';
COMMENT ON COLUMN visual_analysis_nodes.node_y IS '節點在畫布上的Y座標'; 