#!/bin/bash

# 照片資料表設置腳本
# 此檔案的目的：建立照片儲存相關的資料表結構
# 主要功能：支援多專案照片分離、重複檔案檢測、檔案元數據管理

echo "📸 設置照片資料表..."

# 檢查是否在正確的目錄
if [ ! -f "familytree-backend.csproj" ]; then
    echo "❌ 錯誤：請在 familytree-backend 目錄下運行此腳本"
    exit 1
fi

# 檢查資料庫連接
echo "📡 檢查資料庫連接..."

# 建立照片資料表的 SQL
SQL_SCRIPT="
-- 照片資料表
-- 用途：儲存專案照片的元數據資訊，支援專案分離和重複檢測
CREATE TABLE IF NOT EXISTS photos (
    id SERIAL PRIMARY KEY,
    original_filename VARCHAR(255) NOT NULL,           -- 原始檔名
    saved_filename VARCHAR(255) NOT NULL,              -- 儲存的檔名（處理重複後）
    file_path TEXT NOT NULL,                           -- 檔案完整路徑
    file_size BIGINT NOT NULL,                         -- 檔案大小（bytes）
    md5_hash VARCHAR(32) NOT NULL,                     -- MD5雜湊值（用於重複檢測）
    project_id VARCHAR(50) NOT NULL,                   -- 專案ID（實現專案分離）
    upload_time TIMESTAMP WITH TIME ZONE DEFAULT NOW(), -- 上傳時間
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(), -- 建立時間
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()  -- 更新時間
);

-- 建立索引以提升查詢效能
CREATE INDEX IF NOT EXISTS idx_photos_project_id ON photos(project_id);
CREATE INDEX IF NOT EXISTS idx_photos_md5_hash ON photos(md5_hash);
CREATE INDEX IF NOT EXISTS idx_photos_project_md5 ON photos(project_id, md5_hash);
CREATE INDEX IF NOT EXISTS idx_photos_upload_time ON photos(upload_time);

-- 建立唯一約束（在同一專案內，MD5不能重複）
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uk_photos_project_md5') THEN
        ALTER TABLE photos ADD CONSTRAINT uk_photos_project_md5 UNIQUE(project_id, md5_hash);
    END IF;
END
\$\$;

-- 新增觸發器以自動更新 updated_at 欄位
CREATE OR REPLACE FUNCTION update_photos_updated_at()
RETURNS TRIGGER AS \$\$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
\$\$ language 'plpgsql';

DROP TRIGGER IF EXISTS trigger_photos_updated_at ON photos;
CREATE TRIGGER trigger_photos_updated_at
    BEFORE UPDATE ON photos
    FOR EACH ROW
    EXECUTE FUNCTION update_photos_updated_at();

-- 照片標籤關聯表（用於未來擴展，如人員照片關聯）
CREATE TABLE IF NOT EXISTS photo_tags (
    id SERIAL PRIMARY KEY,
    photo_id INTEGER NOT NULL REFERENCES photos(id) ON DELETE CASCADE,
    tag_type VARCHAR(50) NOT NULL,                     -- 標籤類型（如 'person_id', 'location' 等）
    tag_value VARCHAR(100) NOT NULL,                   -- 標籤值（如人員ID、地點名稱等）
    project_id VARCHAR(50) NOT NULL,                   -- 專案ID
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- 照片標籤索引
CREATE INDEX IF NOT EXISTS idx_photo_tags_photo_id ON photo_tags(photo_id);
CREATE INDEX IF NOT EXISTS idx_photo_tags_project_id ON photo_tags(project_id);
CREATE INDEX IF NOT EXISTS idx_photo_tags_type_value ON photo_tags(tag_type, tag_value);

-- 建立複合唯一約束（避免重複標籤）
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uk_photo_tags_unique') THEN
        ALTER TABLE photo_tags ADD CONSTRAINT uk_photo_tags_unique UNIQUE(photo_id, tag_type, tag_value);
    END IF;
END
\$\$;

-- 檢查資料表是否建立成功
DO \$\$
DECLARE
    photos_count INTEGER;
    photo_tags_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO photos_count FROM information_schema.tables 
    WHERE table_name = 'photos' AND table_schema = 'public';
    
    SELECT COUNT(*) INTO photo_tags_count FROM information_schema.tables 
    WHERE table_name = 'photo_tags' AND table_schema = 'public';
    
    IF photos_count > 0 AND photo_tags_count > 0 THEN
        RAISE NOTICE '✅ 照片資料表建立成功';
    ELSE
        RAISE EXCEPTION '❌ 照片資料表建立失敗';
    END IF;
END
\$\$;
"

echo "🚀 執行照片資料表建立 SQL..."

# 執行 SQL 腳本
if PGPASSWORD="" psql -h localhost -p 5432 -U user -d familytree -c "$SQL_SCRIPT"; then
    echo ""
    echo "✅ 照片資料表設置完成！"
    echo ""
    echo "📊 建立的資料表："
    echo "- photos: 照片元數據儲存"
    echo "- photo_tags: 照片標籤關聯（用於人員照片等）"
    echo ""
    echo "🔑 主要功能："
    echo "- 專案分離：透過 project_id 實現多專案照片隔離"
    echo "- 重複檢測：透過 MD5 雜湊值避免重複上傳"
    echo "- 檔名處理：自動處理重複檔名（檔名-1, 檔名-2...）"
    echo "- 標籤系統：支援照片與人員等實體的關聯"
    echo ""
    echo "📁 照片儲存結構："
    echo "  photos/"
    echo "  ├── project1/"
    echo "  │   ├── photo1.jpg"
    echo "  │   └── photo2.png"
    echo "  └── project2/"
    echo "      ├── photo1.jpg"
    echo "      └── photo2.png"
    echo ""
    echo "🎯 現在可以開始上傳照片了！"
else
    echo "❌ 照片資料表設置失敗"
    exit 1
fi 