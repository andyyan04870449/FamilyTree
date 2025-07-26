# FamilyTree 專案 - 資料庫綱要文件

本文檔詳細描述了 FamilyTree 專案資料庫中所有資料表的結構、欄位、資料型別及用途。
文件內容是基於對 PostgreSQL 資料庫結構的直接查詢，並結合後端 C# 程式碼模型分析而成，旨在提供一份清晰、準確的參考。

---

## 資料表總覽

以下是本專案使用的所有資料表清單。點擊連結可直接跳至對應的詳細說明。

### 核心功能資料表
- [`projects`](#projects)
- [`person_profile`](#person_profile)

### 關係圖功能
- [`relationship_layers`](#relationship_layers)
- [`visual_analysis_graphs`](#visual_analysis_graphs)
- [`visual_analysis_nodes`](#visual_analysis_nodes)

### 檔案與照片管理
- [`user_update_file`](#user_update_file)
- [`photos`](#photos)

### 全文搜尋與使用者行為
- [`search_logs`](#search_logs)
- [`search_keywords`](#search_keywords)
- [`popular_keywords`](#popular_keywords)
- [`user_favorites`](#user_favorites)

### 系統與輔助功能
- [`field_mapping`](#field_mapping)
- [`mergedpersons`](#mergedpersons--personmergelog)
- [`personmergelog`](#mergedpersons--personmergelog)
- [`sync_log`](#sync_log-sync_status-sync_error_log)
- [`sync_status`](#sync_log-sync_status-sync_error_log)
- [`sync_error_log`](#sync_log-sync_status-sync_error_log)

---

## 核心功能資料表

### `projects`
**用途**: 管理所有專案的基本資訊。每個專案都是一個獨立的資料集合。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 專案ID | 主鍵，由 `userID-YYYYMMDDHHMMSS` 組成，確保唯一性。 | `varchar` |
| `user_id` | 建立者用戶ID | 記錄是哪個使用者建立了這個專案。 | `varchar` |
| `project_name` | 專案名稱 | 專案的顯示名稱。 | `varchar` |
| `project_description` | 專案描述 | 對專案的詳細文字說明。 | `text` |
| `status` | 專案狀態 | 標示專案目前狀態，如 `active`, `completed`, `archived`。 | `varchar` |
| `created_at` | 建立時間 | 記錄專案建立的時間戳。 | `timestamp` |
| `completed_at` | 完成時間 | 如果專案已完成，記錄完成的時間。 | `timestamp` |
| `updated_at` | 更新時間 | 記錄專案資訊最後一次被修改的時間。 | `timestamp` |

### `person_profile`
**用途**: 儲存從各種來源（如Excel）導入的原始人物基本資料。這是最詳細的人物資訊基礎表。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 人員ID | 主鍵，系統內的唯一人員標識符。 | `int4` |
| `name` | 姓名 | 人物姓名。 | `text` |
| `gender` | 性別 | 人物性別。 | `text` |
| `birthday` | 生日 | 出生日期。 | `text` |
| `birthplace` | 出生地 | 父母戶籍所在地。 | `text` |
| `nationality` | 國籍 | 人物國籍。 | `text` |
| `id_number` | 身分證號碼 | 身分證號。 | `text` |
| `passport_number` | 護照號碼 | 護照號碼。 | `text` |
| `phone` | 電話 | 固定電話號碼。 | `text` |
| `mobile` | 行動電話 | 手機號碼。 | `text` |
| `email` | 電子信箱 | 電子郵件地址。 | `text` |
| `address` | 現居地址 | 目前的居住地址。 | `text` |
| `family_relationships` | 親屬關係 | 以文字描述的家庭成員和關係。 | `text` |
| `experience` | 經歷 | 工作或個人經歷。 | `text` |
| `education` | 學歷 | 教育背景。 | `text` |
| `friends` | 友人 | 朋友或社交關係。 | `text` |
| `remarks` | 備註 | 其他備註資訊。 | `text` |
| `project_id` | 專案ID | 標示此筆資料屬於哪個專案，實現資料隔離。 | `varchar` |
| `file_md5` | 來源檔案MD5 | 記錄資料來源檔案的MD5，用於追溯和去重。 | `varchar` |
| `source_file_id` | 來源檔案ID | 關聯到 `user_update_file` 表的ID。 | `int4` |
| `source_file_name` | 來源檔案名稱 | 原始上傳的檔案名稱。 | `varchar` |
| `created_at` | 建檔時間 | 記錄建立的時間。 | `text` |
| `updated_at` | 最後更新時間 | 記錄最後更新的時間。 | `text` |
| `created_by` | 建檔人 | 建立此記錄的使用者。 | `text` |
| `updated_by` | 最後更新人 | 最後更新此記錄的使用者。 | `text` |

---

## 關係圖功能

### `relationship_layers`
**用途**: 儲存結構化的人物關係，用於建立關係圖。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 關係ID | 主鍵。 | `int4` |
| `source_person_id` | 來源人員ID | 關係的發起方。 | `int4` |
| `target_person_id` | 目標人員ID | 關係的指向方。 | `int4` |
| `relation_type` | 關係類型 | 關係的描述，如 "朋友", "父親"。 | `varchar` |
| `source_field` | 來源欄位 | 該關係是從哪個欄位分析出來的，如 `family_relationships`。 | `varchar` |
| `project_id` | 專案ID | 所屬專案。 | `varchar` |
| `visual_analysis_graph_id` | 視覺分析圖ID | 關聯到特定的視覺化分析圖。 | `int4` |

### `visual_analysis_graphs`
**用途**: 定義一個視覺化關係圖表，它可以包含來自多個專案的節點。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 圖表ID | 主鍵。 | `int4` |
| `name` | 分析圖名稱 | 圖表的自訂名稱。 | `varchar` |
| `project_ids` | 專案ID列表 | 此圖表包含的專案ID，以文字形式儲存。 | `text` |
| `updated_by` | 更新者 | 最後修改此圖表的使用者。 | `varchar` |
| `updated_at` | 更新時間 | 最後修改時間。 | `timestamp` |

### `visual_analysis_nodes`
**用途**: 儲存視覺化圖表中每個節點（人物）的位置、可見性等狀態。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 節點ID | 主鍵。 | `int4` |
| `graph_id` | 圖表ID | 關聯到 `visual_analysis_graphs` 的ID。 | `int4` |
| `person_id` | 人員ID | 此節點代表的人物ID。 | `int4` |
| `is_visible` | 是否可見 | 控制此節點在圖表上是否顯示。 | `bool` |
| `node_x` | X座標 | 節點在圖表畫布上的X座標。 | `float8` |
| `node_y` | Y座標 | 節點在圖表畫布上的Y座標。 | `float8` |
| `project_id` | 專案ID | 該節點人物所屬的原始專案ID。 | `varchar` |

---

## 檔案與照片管理

### `user_update_file`
**用途**: 記錄使用者上傳用於資料導入的檔案（主要是Excel）的資訊。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 檔案ID | 主鍵。 | `int4` |
| `filename` | 儲存檔名 | 檔案在伺服器上儲存的名稱。 | `varchar` |
| `original_filename` | 原始檔名 | 使用者上傳時的檔案名稱。 | `varchar` |
| `file_path` | 檔案路徑 | 檔案在伺服器上的儲存路徑。 | `varchar` |
| `file_size` | 檔案大小 | 檔案大小（Bytes）。 | `int8` |
| `md5_hash` | MD5雜湊值 | 用於檢查檔案重複性，是檔案的唯一標識。 | `varchar` |
| `upload_time` | 上傳時間 | 檔案上傳的時間。 | `timestamp` |
| `status` | 狀態 | 檔案處理狀態 (`uploaded`, `processing`, `merged`, `error`)。 | `varchar` |
| `project_id` | 專案ID | 此次上傳關聯的專案。 | `varchar` |

### `photos`
**用途**: 儲存上傳的照片的基本資訊。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 照片ID | 主鍵。 | `int4` |
| `original_filename` | 原始檔名 | 照片的原始檔案名稱。 | `varchar` |
| `saved_filename` | 儲存檔名 | 在伺服器上儲存的名稱。 | `varchar` |
| `file_path` | 檔案路徑 | 儲存路徑。 | `text` |
| `file_size` | 檔案大小 | 檔案大小（Bytes）。 | `int8` |
| `md5_hash` | MD5雜湊值 | 用於照片去重。 | `varchar` |
| `project_id` | 專案ID | 所屬專案。 | `varchar` |
| `upload_time` | 上傳時間 | 上傳時間。 | `timestamptz` |
| `deleted_at` | 刪除時間 | 軟刪除標記。 | `timestamp` |

---

## 全文搜尋與使用者行為

### `search_logs`
**用途**: 記錄每一次使用者執行的搜尋操作，用於分析和優化。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 日誌ID | 主鍵。 | `int4` |
| `keyword` | 關鍵字 | 使用者搜尋時輸入的關鍵字。 | `varchar` |
| `search_type` | 搜尋類型 | `fuzzy` (模糊) 或 `exact` (精準)。 | `varchar` |
| `result_count` | 結果數量 | 本次搜尋返回的結果筆數。 | `int4` |
| `search_time` | 搜尋時間 | 執行搜尋的時間。 | `timestamp` |
| `ip_address` | IP位址 | 使用者的IP位址。 | `varchar` |
| `project_id` | 專案ID | 在哪個專案下執行的搜尋。 | `varchar` |

### `search_keywords`
**用途**: 累計每個關鍵字的搜尋次數，用於分析熱門關鍵字。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | ID | 主鍵。 | `int4` |
| `keyword` | 關鍵字 | 被搜尋的關鍵字。 | `varchar` |
| `search_count` | 搜尋次數 | 該關鍵字被搜尋的總次數。 | `int4` |
| `last_search_time` | 最後搜尋時間 | 最近一次被搜尋的時間。 | `timestamp` |
| `project_id` | 專案ID | 所屬專案。 | `varchar` |

### `popular_keywords`
**用途**: 專門儲存計算出的熱門關鍵字及其熱度。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `keyword` | 關鍵字 | 熱門關鍵字。 | `varchar` |
| `search_count` | 搜尋次數 | 總搜尋次數。 | `int4` |
| `popularity_level` | 熱門程度 | 如 "熱門", "常用", "一般"。 | `text` |

### `user_favorites`
**用途**: 儲存使用者收藏的人物列表。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 收藏ID | 主鍵。 | `int4` |
| `person_id` | 人員ID | 被收藏的人物ID。 | `int4` |
| `person_name` | 人員姓名 | 被收藏的人物姓名。 | `varchar` |
| `last_viewed_time` | 最後查看時間 | 使用者最後一次點擊查看此人的時間。 | `timestamp` |
| `favorited_at` | 收藏時間 | 加入收藏的時間。 | `timestamp` |
| `project_id` | 專案ID | 所屬專案。 | `varchar` |

---

## 系統與輔助功能

### `field_mapping`
**用途**: 定義 Excel 檔案中的欄位名稱與資料庫 `person_profile` 表中欄位的對應關係。

| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 對應ID | 主鍵。 | `int4` |
| `excel_field_name` | Excel 欄位名稱 | Excel 表頭的欄位名稱。 | `varchar` |
| `db_field_name` | 資料庫欄位名稱 | 對應到 `person_profile` 的欄位名。 | `varchar` |

### `mergedpersons` & `personmergelog`
**用途**: 用於處理與記錄人物資料合併的過程。`mergedpersons` 儲存合併後的最終資料，`personmergelog` 記錄每一次合併操作的細節。

**`mergedpersons`**
| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 合併後人員ID | 主鍵。 | `int4` |
| `name` | 姓名 | 合併後的姓名。 | `varchar` |
| `...` | ... | 其他欄位與 `person_profile` 類似。 | `...` |
| `source_person_a_id` | 來源人員A ID | 記錄第一個合併來源的人物ID。 | `int4` |
| `source_person_b_id` | 來源人員B ID | 記錄第二個合併來源的人物ID。 | `int4` |

**`personmergelog`**
| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 日誌ID | 主鍵。 | `int4` |
| `merged_person_id` | 合併後人員ID | 關聯到 `mergedpersons` 表。 | `int4` |
| `source_person_a_id` | 來源人員A ID | 來源之一。 | `int4` |
| `source_person_b_id` | 來源人員B ID | 來源之二。 | `int4` |
| `merged_by` | 合併操作者 | 執行合併的使用者。 | `varchar` |
| `merged_at` | 合併時間 | 執行合併的時間。 | `timestamptz` |

### `sync_log`, `sync_status`, `sync_error_log`
**用途**: 這一組資料表用於記錄與管理跨系統或跨資料表的資料同步狀態、日誌與錯誤。

**`sync_log`**
| 欄位名稱 | 中文說明/描述 | 用途 | 資料型別 |
| :--- | :--- | :--- | :--- |
| `id` | 日誌ID | 主鍵。 | `int4` |
| `source_id` | 來源ID | 同步的來源資料ID。 | `int4` |
| `source_table` | 來源資料表 | 同步的來源資料表名稱。 | `varchar` |
| `status` | 狀態 | `success` 或 `failed`。 | `varchar` |
| `sync_time` | 同步時間 | 執行同步的時間。 | `timestamp` |

--- 