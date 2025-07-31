# 統一設計系統使用指南

## 概述

本設計系統基於案件管理頁面的優秀設計，提供統一的視覺元素和佈局標準，確保整個應用程式的一致性和可維護性。

## 設計原則

### 1. 深色主題優先
- 主背景：深灰色 (`$bg-primary`)
- 表面背景：稍淺的深灰色 (`$bg-surface`)
- 文字：白色和淺灰色系

### 2. 8px 網格系統
- 所有間距都基於 8px 的倍數
- 確保視覺節奏的一致性

### 3. 圓角設計
- 主要圓角：8px
- 次要圓角：4px
- 按鈕圓角：6px

### 4. 陰影效果
- 輕微陰影：`0 2px 4px rgba(0, 0, 0, 0.1)`
- 中等陰影：`0 4px 6px rgba(0, 0, 0, 0.1)`
- 重陰影：`0 20px 25px -5px rgba(0, 0, 0, 0.3)`

## 核心組件

### 1. 頁面容器
```html
<div class="page-container">
  <!-- 頁面內容 -->
</div>
```

### 2. 頁面標題區域
```html
<div class="page-header">
  <div class="header-top">
    <div class="header-left">
      <h1>頁面標題</h1>
      <div class="page-subtitle">頁面描述</div>
    </div>
    <div class="header-right">
      <button class="btn-main">主要操作</button>
    </div>
  </div>
</div>
```

### 3. 搜尋區域
```html
<div class="search-section">
  <div class="search-row">
    <div class="search-group">
      <label>搜尋標籤</label>
      <input class="search-input" placeholder="請輸入...">
    </div>
    <div class="search-actions">
      <button class="btn-search">搜尋</button>
    </div>
  </div>
</div>
```

### 4. 內容區塊
```html
<div class="content-section">
  <div class="section-header">
    <div class="section-title">區塊標題</div>
    <div class="section-actions">
      <!-- 操作按鈕 -->
    </div>
  </div>
  <!-- 內容 -->
</div>
```

### 5. 表格
```html
<table class="data-table">
  <thead>
    <tr>
      <th>欄位1</th>
      <th>欄位2</th>
      <th class="col-actions">功能</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>資料1</td>
      <td>資料2</td>
      <td class="col-actions">
        <div class="action-buttons">
          <button class="btn-action btn-edit">編輯</button>
          <button class="btn-action btn-upload">上傳</button>
          <button class="btn-action btn-analysis">分析</button>
          <button class="btn-action btn-delete">刪除</button>
        </div>
      </td>
    </tr>
  </tbody>
</table>
```

### 6. 操作按鈕
```html
<button class="btn-action btn-edit">編輯</button>
<button class="btn-action btn-upload">上傳</button>
<button class="btn-action btn-analysis">分析</button>
<button class="btn-action btn-delete">刪除</button>
<button class="btn-action btn-name">命名</button>
```

### 7. 狀態組件
```html
<!-- 載入狀態 -->
<div class="loading-section">
  <div class="loading-spinner"></div>
  <p>載入中...</p>
</div>

<!-- 空狀態 -->
<div class="empty-state">
  <div class="empty-icon">📁</div>
  <h3>沒有資料</h3>
  <p>目前沒有任何資料，請新增一些內容。</p>
</div>
```

### 8. 分頁控制
```html
<div class="pagination-section">
  <div class="pagination-info">
    顯示 1-10 筆，共 50 筆
  </div>
  <div class="pagination-controls">
    <button class="pagination-btn">上一頁</button>
    <div class="page-numbers">
      <button class="pagination-btn page-number active">1</button>
      <button class="pagination-btn page-number">2</button>
      <button class="pagination-btn page-number">3</button>
    </div>
    <button class="pagination-btn">下一頁</button>
    <select class="page-selector">
      <option>10</option>
      <option>20</option>
      <option>50</option>
    </select>
  </div>
</div>
```

### 9. 對話框
```html
<div class="dialog-overlay">
  <div class="dialog">
    <div class="dialog-header">
      <h3>對話框標題</h3>
      <button class="close-btn">&times;</button>
    </div>
    <div class="dialog-content">
      <div class="form-group">
        <label>標籤</label>
        <input type="text" placeholder="請輸入...">
      </div>
    </div>
    <div class="dialog-actions">
      <button class="btn-cancel">取消</button>
      <button class="btn-confirm">確認</button>
      <button class="btn-danger">刪除</button>
    </div>
  </div>
</div>
```

## 按鈕顏色語義

- `btn-edit`: 黃色 (`$warning`) - 編輯操作
- `btn-upload`: 綠色 (`$success`) - 上傳/新增操作
- `btn-analysis`: 藍色 (`$info`) - 分析操作
- `btn-delete`: 紅色 (`$error`) - 刪除操作
- `btn-name`: 綠色 (`$success`) - 命名操作

## 響應式設計

### 斷點
- 移動端：`max-width: 768px`
- 平板：`768px - 1024px`
- 桌面：`min-width: 1024px`

### 移動端適配
- 頁面標題在小螢幕上垂直排列
- 搜尋欄位在小螢幕上單欄顯示
- 表格操作按鈕在小螢幕上垂直排列
- 對話框按鈕在小螢幕上垂直排列

## 使用建議

### 1. 新頁面開發
1. 使用 `page-container` 作為頁面根容器
2. 使用 `page-header` 包含標題和主要操作
3. 使用 `search-section` 提供搜尋功能
4. 使用 `content-section` 和 `data-table` 顯示資料
5. 使用標準化的按鈕類別

### 2. 現有頁面改造
1. 逐步替換現有的樣式類別
2. 使用設計系統提供的統一組件
3. 保持功能不變，只更新視覺樣式

### 3. 自定義需求
1. 優先使用設計系統提供的變數
2. 如需自定義，請遵循現有的命名規範
3. 考慮將通用的自定義樣式貢獻回設計系統

## 檔案結構

```
src/styles/
├── index.scss                    # 主入口文件
├── foundation/
│   ├── _tokens.scss             # 設計 tokens
│   └── _mixins.scss             # 可重用 mixins
├── components/
│   ├── _buttons.scss            # 按鈕組件
│   ├── _forms.scss              # 表單組件
│   ├── _tables.scss             # 表格組件
│   ├── _cards.scss              # 卡片組件
│   └── _unified-design.scss     # 統一設計系統
└── UNIFIED_DESIGN_GUIDE.md      # 本指南
```

## 最佳實踐

1. **一致性優先**：優先使用設計系統提供的標準組件
2. **語義化命名**：使用描述功能的類別名稱
3. **響應式設計**：確保在所有裝置上都有良好的體驗
4. **無障礙設計**：考慮鍵盤導航和螢幕閱讀器支援
5. **效能優化**：避免過度自定義，保持 CSS 的簡潔性

## 更新日誌

### v1.0.0 (當前版本)
- 基於案件管理頁面設計標準化
- 統一頁面標題、搜尋區域、表格、按鈕等元素
- 標準化載入狀態、空狀態、分頁控制組件
- 完整的響應式設計支援
- 統一的對話框樣式 