-- 修復視覺化分析關係遷移腳本
-- 步驟1: 檢查並插入預設圖表記錄
-- 步驟2: 添加 visual_analysis_graph_id 欄位
-- 步驟3: 更新現有關係記錄
-- 步驟4: 添加外鍵約束和索引

BEGIN;

-- 檢查是否已存在預設圖表，如果不存在則插入
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM visual_analysis_graphs WHERE name = '視覺化圖表分析') THEN
        INSERT INTO visual_analysis_graphs (name, project_ids, updated_by, updated_at) 
        VALUES ('視覺化圖表分析', '', 'system', CURRENT_TIMESTAMP);
        RAISE NOTICE '✅ 已插入預設視覺化分析圖表';
    ELSE
        RAISE NOTICE '⚠️ 預設視覺化分析圖表已存在，跳過插入';
    END IF;
END $$;

-- 檢查並添加 visual_analysis_graph_id 欄位
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'relationship_layers' 
        AND column_name = 'visual_analysis_graph_id'
    ) THEN
        ALTER TABLE relationship_layers 
        ADD COLUMN visual_analysis_graph_id INTEGER;
        
        COMMENT ON COLUMN relationship_layers.visual_analysis_graph_id 
        IS '視覺化分析圖表ID，關聯到 visual_analysis_graphs.id';
        
        RAISE NOTICE '✅ 已添加 visual_analysis_graph_id 欄位';
    ELSE
        RAISE NOTICE '⚠️ visual_analysis_graph_id 欄位已存在，跳過添加';
    END IF;
END $$;

-- 更新現有關係記錄，設定為預設圖表ID
DO $$
DECLARE
    default_graph_id INTEGER;
    updated_count INTEGER;
BEGIN
    -- 獲取預設圖表ID
    SELECT id INTO default_graph_id 
    FROM visual_analysis_graphs 
    WHERE name = '視覺化圖表分析' 
    LIMIT 1;
    
    IF default_graph_id IS NOT NULL THEN
        -- 更新所有 visual_analysis_graph_id 為 NULL 的記錄
        UPDATE relationship_layers 
        SET visual_analysis_graph_id = default_graph_id
        WHERE visual_analysis_graph_id IS NULL;
        
        GET DIAGNOSTICS updated_count = ROW_COUNT;
        RAISE NOTICE '✅ 已更新 % 筆關係記錄，關聯到預設圖表 (ID: %)', updated_count, default_graph_id;
    ELSE
        RAISE EXCEPTION '❌ 找不到預設視覺化分析圖表';
    END IF;
END $$;

-- 檢查並添加外鍵約束
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'fk_relationship_layers_visual_analysis_graph'
        AND table_name = 'relationship_layers'
    ) THEN
        ALTER TABLE relationship_layers 
        ADD CONSTRAINT fk_relationship_layers_visual_analysis_graph
        FOREIGN KEY (visual_analysis_graph_id) 
        REFERENCES visual_analysis_graphs(id) 
        ON DELETE SET NULL;
        
        RAISE NOTICE '✅ 已添加外鍵約束';
    ELSE
        RAISE NOTICE '⚠️ 外鍵約束已存在，跳過添加';
    END IF;
END $$;

-- 檢查並添加索引
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'idx_relationship_layers_visual_analysis_graph_id'
    ) THEN
        CREATE INDEX idx_relationship_layers_visual_analysis_graph_id 
        ON relationship_layers(visual_analysis_graph_id);
        
        RAISE NOTICE '✅ 已添加索引';
    ELSE
        RAISE NOTICE '⚠️ 索引已存在，跳過添加';
    END IF;
END $$;

COMMIT;

-- 驗證遷移結果
SELECT 
    'visual_analysis_graphs' as table_name,
    COUNT(*) as record_count
FROM visual_analysis_graphs
UNION ALL
SELECT 
    'relationship_layers_with_graph_id',
    COUNT(*)
FROM relationship_layers 
WHERE visual_analysis_graph_id IS NOT NULL;

\echo '🎉 視覺化分析關係遷移完成' 