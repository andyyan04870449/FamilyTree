# FamilyTree 設計系統使用指南

## 概述

本設計系統基於案件管理頁面的優秀設計，提供統一的視覺元素和佈局標準，確保整個應用程式的一致性和可維護性。

## 核心設計原則

### 1. 深色主題優先
- 主背景：深灰色 (`$bg-primary`)
- 表面背景：稍淺的深灰色 (`$bg-surface`)
- 文字：白色和淺灰色系

### 2. 8px 網格系統
- 所有間距都基於 8px 的倍數
- 確保視覺節奏的一致性

### 3. 語義化命名
- 使用功能描述而非視覺描述
- 例如：`$text-primary` 而非 `#FFFFFF`

## 頁面佈局標準

### 基本頁面結構

```scss
.page-container {
  @include page-container;
}
```

### 整合頁面標題和搜尋區域

```html
<div class="page-header">
  <div class="header-top">
    <div class="header-left">
      <h1>頁面標題</h1>
      <div class="page-subtitle">頁面描述</div>
    </div>
    <div class="header-right">
      <button class="btn-primary-action">主要操作</button>
    </div>
  </div>
  
  <div class="search-filter-section">
    <div class="search-row">
      <div class="search-group">
        <label>搜尋標籤</label>
        <input class="search-input" placeholder="請輸入...">
      </div>
      <!-- 更多搜尋欄位 -->
      <div class="search-actions">
        <button class="btn-search">搜尋</button>
      </div>
    </div>
  </div>
</div>
```

### 表格區域

```html
<div class="table-section">
  <div class="table-header">
    <div class="table-title">表格標題</div>
    <div class="table-actions">
      <!-- 表格操作按鈕 -->
    </div>
  </div>
  
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
</div>
```

## 按鈕系統

### 主要操作按鈕
```html
<button class="btn-primary-action">新增</button>
```

### 表格操作按鈕
```html
<button class="btn-action btn-edit">編輯</button>
<button class="btn-action btn-upload">上傳</button>
<button class="btn-action btn-analysis">分析</button>
<button class="btn-action btn-delete">刪除</button>
<button class="btn-action btn-name">命名</button>
```

### 按鈕顏色語義
- `btn-edit`: 黃色 (`$warning`) - 編輯操作
- `btn-upload`: 綠色 (`$success`) - 上傳/新增操作
- `btn-analysis`: 藍色 (`$info`) - 分析操作
- `btn-delete`: 紅色 (`$error`) - 刪除操作
- `btn-name`: 綠色 (`$success`) - 命名操作

## 狀態組件

### 載入狀態
```html
<div class="loading-section">
  <div class="loading-spinner"></div>
  <p>載入中...</p>
</div>
```

### 空狀態
```html
<div class="empty-state">
  <div class="empty-icon">📁</div>
  <h3>沒有資料</h3>
  <p>目前沒有任何資料，請新增一些內容。</p>
</div>
```

## 分頁控制

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

## 響應式設計

### 移動端適配
- 頁面標題在小螢幕上垂直排列
- 搜尋欄位在小螢幕上單欄顯示
- 表格操作按鈕在小螢幕上垂直排列

### 斷點
- 移動端：`max-width: 768px`
- 平板：`768px - 1024px`
- 桌面：`min-width: 1024px`

## 使用建議

### 1. 新頁面開發
1. 使用 `page-container` 作為頁面根容器
2. 使用 `page-header` 包含標題和搜尋功能
3. 使用 `table-section` 和 `data-table` 顯示資料
4. 使用標準化的按鈕類別

### 2. 現有頁面改造
1. 逐步替換現有的樣式類別
2. 使用設計系統的 mixins 和 tokens
3. 保持功能不變，只更新視覺樣式

### 3. 自定義需求
1. 優先使用設計系統提供的變數
2. 如需自定義，請遵循現有的命名規範
3. 考慮將通用的自定義樣式貢獻回設計系統

## 檔案結構

```
src/styles/
├── index.scss              # 主入口文件
├── foundation/
│   ├── _tokens.scss        # 設計 tokens
│   └── _mixins.scss        # 可重用 mixins
├── components/
│   ├── _buttons.scss       # 按鈕組件
│   ├── _forms.scss         # 表單組件
│   ├── _tables.scss        # 表格組件
│   ├── _cards.scss         # 卡片組件
│   └── _page-layout.scss   # 頁面佈局組件
└── DESIGN_SYSTEM_GUIDE.md  # 本指南
```

## 最佳實踐

1. **一致性優先**：優先使用設計系統提供的標準組件
2. **語義化命名**：使用描述功能的類別名稱
3. **響應式設計**：確保在所有裝置上都有良好的體驗
4. **無障礙設計**：考慮鍵盤導航和螢幕閱讀器支援
5. **效能優化**：避免過度自定義，保持 CSS 的簡潔性

## 更新日誌

### v2.0.0 (當前版本)
- 基於案件管理頁面設計標準化
- 新增頁面佈局組件
- 統一按鈕和表格樣式
- 改善響應式設計支援
- 新增分頁控制組件 