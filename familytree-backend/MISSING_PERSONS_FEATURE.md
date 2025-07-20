# Missing Persons 功能說明

## 📋 功能概述

這個功能用於記錄 AI 分析出但資料庫中不存在的人員。當 AI 分析人員關係時，如果發現某個關係中的人員在資料庫中找不到，系統會自動將這個人員記錄到 `missing_persons` 表格中。

## 🗄️ 資料庫結構

### missing_persons 表格

| 欄位 | 類型 | 說明 |
|------|------|------|
| id | SERIAL PRIMARY KEY | 主鍵 |
| name | VARCHAR(255) | 人員姓名 |
| relation_type | VARCHAR(100) | 與來源人員的關係類型 |
| source_person_id | INTEGER | 來源人員的 ID |
| source_field | VARCHAR(50) | 來源欄位（family_relationships, friends, activities） |
| analysis_session_id | VARCHAR(255) | 分析會話 ID |
| layer_depth | INTEGER | 分析層級深度 |
| discovered_at | TIMESTAMP | 發現時間 |
| status | VARCHAR(50) | 狀態（pending: 待處理, resolved: 已解決, ignored: 忽略） |
| resolved_person_id | INTEGER | 解決後對應的人員 ID |
| notes | TEXT | 備註信息 |

## 🚀 設置步驟

### 1. 創建資料庫表格

```bash
cd familytree-backend
./setup-missing-persons-table.sh
```

### 2. 重新啟動應用程式

```bash
cd ..
./start-all.sh
```

## 📊 API 端點

### 獲取所有找不到的人員記錄

```
GET /api/missingperson
```

查詢參數：
- `status`: 過濾狀態（pending, resolved, ignored）
- `sourcePersonId`: 過濾來源人員 ID

### 獲取特定記錄

```
GET /api/missingperson/{id}
```

### 更新記錄狀態

```
PUT /api/missingperson/{id}
```

請求體：
```json
{
  "status": "resolved",
  "resolvedPersonId": 123,
  "notes": "已找到對應人員"
}
```

### 刪除記錄

```
DELETE /api/missingperson/{id}
```

### 獲取統計信息

```
GET /api/missingperson/stats
```

## 🔄 工作流程

### 1. AI 分析階段
- AI 分析人員關係資料
- 提取出人名和關係類型

### 2. 查找人員階段
- 系統在 `person_profile` 表格中查找對應人員
- 如果找到：保存關係到 `relationship_layers` 表格
- 如果找不到：記錄到 `missing_persons` 表格

### 3. 管理階段
- 管理員可以查看所有找不到的人員記錄
- 可以更新記錄狀態（pending → resolved/ignored）
- 可以關聯到新創建的人員記錄

## 💡 使用場景

### 場景 1：發現新的人員關係
1. AI 分析出 "范統" 是 "父" 的關係
2. 系統找不到 "范統" 這個人員
3. 記錄到 `missing_persons` 表格
4. 管理員可以手動創建 "范統" 的人員記錄
5. 更新 `missing_persons` 記錄狀態為 "resolved"

### 場景 2：忽略無關人員
1. AI 分析出 "路人甲" 是 "同事" 的關係
2. 系統找不到 "路人甲" 這個人員
3. 記錄到 `missing_persons` 表格
4. 管理員可以將狀態設為 "ignored"

## 📈 統計信息

系統提供統計信息來了解：
- 總共有多少找不到的人員記錄
- 各狀態的記錄數量（pending, resolved, ignored）
- 按來源欄位分類的統計

## 🔧 維護建議

1. **定期檢查**：定期查看 pending 狀態的記錄
2. **批量處理**：可以批量更新相似記錄的狀態
3. **數據清理**：定期清理 ignored 狀態的舊記錄
4. **關聯檢查**：當創建新人員時，檢查是否有相關的 missing_persons 記錄

## 🎯 優勢

1. **不丟失信息**：AI 分析出的關係不會丟失
2. **可追蹤**：可以追蹤每個找不到的人員的來源
3. **可管理**：提供完整的管理介面
4. **可統計**：提供統計信息幫助決策
5. **可關聯**：支持後續關聯到新創建的人員記錄 