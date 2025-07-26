# FamilyTree UI 設計系統

## 設計理念
- **現代簡潔風格**：卡片式設計，圓角邊框，清爽的視覺效果
- **深色主題優先**：專業感，減少視覺疲勞
- **響應式設計**：支援桌面和行動裝置，自適應佈局
- **使用者體驗導向**：直觀的操作流程，清晰的視覺層次

## 色彩系統

### 主要色彩
```scss
// 主色調 - 藍色系
$primary-blue: #3b82f6;        // 主要藍色
$primary-blue-hover: #2563eb;  // 懸停藍色
$primary-blue-light: #4a90e2;  // 淺藍色
$primary-blue-dark: #357abd;   // 深藍色

// 深色主題背景
$bg-dark: #1a1a1a;             // 主要背景
$bg-dark-secondary: #252525;   // 次要背景
$bg-dark-tertiary: #2a2a2a;    // 第三級背景
$bg-dark-quaternary: #1f1f1f;  // 第四級背景

// 邊框色彩
$border-dark: #333;             // 主要邊框
$border-dark-light: #444;       // 淺色邊框
$border-dark-lighter: #555;     // 更淺邊框
```

### 功能色彩
```scss
// 成功/儲存
$success-green: #28a745;
$success-green-hover: #1e7e34;

// 警告/取消
$warning-gray: #6c757d;
$warning-gray-hover: #545b62;

// 錯誤/刪除
$error-red: #dc2626;
$error-red-light: #ff6b6b;

// 載入/處理中
$loading-blue: #4a90e2;
```

### 文字色彩
```scss
// 主要文字
$text-primary: #e0e0e0;        // 主要文字
$text-secondary: #ccc;          // 次要文字
$text-tertiary: #999;           // 第三級文字
$text-quaternary: #777;         // 第四級文字

// 標籤文字
$label-text: #999;              // 標籤文字
$required-text: #ff6b6b;        // 必填標記
```

## 字體系統

### 字體大小
```scss
$font-size-xs: 11px;           // 超小字體
$font-size-sm: 12px;           // 小字體
$font-size-base: 14px;         // 基礎字體
$font-size-md: 16px;           // 中等字體
$font-size-lg: 18px;           // 大字體
$font-size-xl: 24px;           // 超大字體
$font-size-2xl: 2.5rem;        // 標題字體
```

### 字體粗細
```scss
$font-weight-normal: 400;
$font-weight-medium: 500;
$font-weight-semibold: 600;
$font-weight-bold: 700;
```

## 間距系統

### 基礎間距
```scss
$spacing-xs: 5px;
$spacing-sm: 8px;
$spacing-md: 12px;
$spacing-lg: 15px;
$spacing-xl: 20px;
$spacing-2xl: 25px;
$spacing-3xl: 30px;
$spacing-4xl: 40px;
$spacing-5xl: 60px;
```

### 容器間距
```scss
$container-padding: 20px;
$section-padding: 25px;
$card-padding: 15px;
$dialog-padding: 20px;
```

## 圓角系統
```scss
$border-radius-sm: 4px;        // 小圓角
$border-radius-md: 6px;        // 中等圓角
$border-radius-lg: 8px;        // 大圓角
$border-radius-xl: 12px;       // 超大圓角
$border-radius-full: 50%;      // 圓形
```

## 陰影系統
```scss
$shadow-sm: 0 2px 4px rgba(0, 0, 0, 0.1);
$shadow-md: 0 4px 12px rgba(0, 0, 0, 0.1);
$shadow-lg: 0 20px 40px rgba(0, 0, 0, 0.3);
$shadow-focus: 0 0 0 3px rgba(74, 144, 226, 0.1);
```

## 元件設計規範

### 按鈕元件
```scss
// 基礎按鈕樣式
.btn {
  padding: 8px 12px;
  border: none;
  border-radius: $border-radius-sm;
  font-size: $font-size-base;
  font-weight: $font-weight-medium;
  cursor: pointer;
  transition: all 0.2s ease;
  
  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}

// 主要按鈕
.btn-primary {
  @extend .btn;
  background-color: $primary-blue;
  color: white;
  
  &:hover:not(:disabled) {
    background-color: $primary-blue-hover;
  }
}

// 成功按鈕
.btn-success {
  @extend .btn;
  background-color: $success-green;
  color: white;
  
  &:hover:not(:disabled) {
    background-color: $success-green-hover;
  }
}

// 警告按鈕
.btn-warning {
  @extend .btn;
  background-color: $warning-gray;
  color: white;
  
  &:hover:not(:disabled) {
    background-color: $warning-gray-hover;
  }
}

// 圖示按鈕
.btn-icon {
  width: 40px;
  height: 40px;
  border-radius: $border-radius-sm;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
}
```

### 輸入框元件
```scss
// 基礎輸入框
.input-base {
  padding: 8px 12px;
  background: $bg-dark-tertiary;
  border: 1px solid $border-dark-light;
  border-radius: $border-radius-sm;
  color: $text-primary;
  font-size: $font-size-md;
  transition: all 0.2s ease;
  
  &:focus {
    outline: none;
    border-color: $primary-blue-light;
    box-shadow: $shadow-focus;
  }
  
  &::placeholder {
    color: $text-quaternary;
  }
}

// 文字輸入框
.input-text {
  @extend .input-base;
}

// 下拉選單
.input-select {
  @extend .input-base;
  cursor: pointer;
}

// 文字區域
.input-textarea {
  @extend .input-base;
  resize: vertical;
  min-height: 60px;
  font-family: inherit;
}
```

### 卡片元件
```scss
// 基礎卡片
.card {
  background-color: $bg-dark-secondary;
  border: 1px solid $border-dark;
  border-radius: $border-radius-lg;
  padding: $card-padding;
  transition: all 0.2s ease;
  
  &:hover {
    box-shadow: $shadow-md;
    transform: translateY(-2px);
  }
}

// 人員卡片
.person-card {
  @extend .card;
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: $spacing-lg;
}
```

### 對話框元件
```scss
// 對話框覆蓋層
.dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  backdrop-filter: blur(2px);
}

// 對話框容器
.dialog-container {
  background: $bg-dark;
  border-radius: $border-radius-xl;
  width: 90%;
  max-width: 1200px;
  max-height: 90vh;
  overflow: hidden;
  position: relative;
  box-shadow: $shadow-lg;
  border: 1px solid $border-dark;
}
```

### 標籤頁元件
```scss
// 標籤頁導航
.tabs-nav {
  display: flex;
  flex-wrap: wrap;
  border-bottom: 1px solid $border-dark;
  padding: 0 $container-padding;
  background: $bg-dark-quaternary;
  border-radius: $border-radius-lg $border-radius-lg 0 0;
  overflow-x: auto;
}

// 標籤按鈕
.tab-btn {
  background: transparent;
  border: none;
  color: $text-tertiary;
  padding: $spacing-md $spacing-lg;
  cursor: pointer;
  font-size: $font-size-sm;
  border-bottom: 3px solid transparent;
  transition: all 0.2s ease;
  white-space: nowrap;
  
  &:hover {
    color: $text-secondary;
    background: $bg-dark-tertiary;
  }
  
  &.active {
    color: $primary-blue-light;
    border-bottom-color: $primary-blue-light;
    background: $bg-dark-tertiary;
  }
}
```

### 表格元件
```scss
// 基礎表格
.table {
  width: 100%;
  border-collapse: collapse;
  background: $bg-dark-tertiary;
  border-radius: $border-radius-md;
  overflow: hidden;
  
  th, td {
    padding: $spacing-md $spacing-lg;
    text-align: left;
    border-bottom: 1px solid $border-dark;
  }
  
  th {
    background: $bg-dark-tertiary;
    color: $text-secondary;
    font-weight: $font-weight-semibold;
    font-size: $font-size-sm;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }
  
  td {
    color: $text-primary;
    font-size: $font-size-sm;
  }
  
  tr:hover {
    background: $bg-dark-tertiary;
  }
  
  tr:last-child td {
    border-bottom: none;
  }
}
```

### 載入元件
```scss
// 載入容器
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: $spacing-5xl $spacing-xl;
  color: $text-secondary;
}

// 載入動畫
.loading-spinner {
  width: 40px;
  height: 40px;
  border: 3px solid $border-dark;
  border-top: 3px solid $loading-blue;
  border-radius: $border-radius-full;
  animation: spin 1s linear infinite;
  margin-bottom: $spacing-lg;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}
```

### 頭像元件
```scss
// 頭像容器
.avatar {
  width: 60px;
  height: 60px;
  background-color: $primary-blue;
  border-radius: $border-radius-full;
  display: flex;
  align-items: center;
  justify-content: center;
  
  .avatar-text {
    color: white;
    font-size: 1.5rem;
    font-weight: $font-weight-bold;
  }
}

// 大頭像
.avatar-large {
  width: 200px;
  height: 250px;
  border-radius: $border-radius-lg;
  background: linear-gradient(135deg, $primary-blue-light, #5ba0f2);
  
  .avatar-text {
    font-size: 72px;
  }
}
```

## 響應式斷點
```scss
$breakpoint-sm: 480px;
$breakpoint-md: 768px;
$breakpoint-lg: 1024px;
$breakpoint-xl: 1200px;

// 響應式混入
@mixin mobile {
  @media (max-width: $breakpoint-md) {
    @content;
  }
}

@mixin tablet {
  @media (min-width: $breakpoint-md) and (max-width: $breakpoint-lg) {
    @content;
  }
}

@mixin desktop {
  @media (min-width: $breakpoint-lg) {
    @content;
  }
}
```

## 工具類別
```scss
// 文字對齊
.text-center { text-align: center; }
.text-left { text-align: left; }
.text-right { text-align: right; }

// 顯示
.d-none { display: none; }
.d-block { display: block; }
.d-flex { display: flex; }
.d-inline { display: inline; }
.d-inline-block { display: inline-block; }

// Flexbox
.flex-column { flex-direction: column; }
.flex-row { flex-direction: row; }
.justify-center { justify-content: center; }
.justify-between { justify-content: space-between; }
.align-center { align-items: center; }
.align-start { align-items: flex-start; }
.align-end { align-items: flex-end; }

// 間距
.m-0 { margin: 0; }
.p-0 { padding: 0; }
.mb-1 { margin-bottom: $spacing-sm; }
.mb-2 { margin-bottom: $spacing-md; }
.mb-3 { margin-bottom: $spacing-lg; }
.p-1 { padding: $spacing-sm; }
.p-2 { padding: $spacing-md; }
.p-3 { padding: $spacing-lg; }

// 寬度
.w-full { width: 100%; }
.w-auto { width: auto; }
.h-full { height: 100%; }
.h-auto { height: auto; }
```

## 使用指南

### 1. 引入設計系統
```scss
// 在主要樣式檔案中引入
@import './styles/variables';
@import './styles/mixins';
@import './styles/components';
```

### 2. 建立新元件
```scss
// 使用設計系統建立新元件
.new-component {
  @extend .card;
  background-color: $bg-dark-secondary;
  
  .component-header {
    @extend .text-center;
    color: $text-primary;
    font-size: $font-size-xl;
    margin-bottom: $spacing-lg;
  }
  
  .component-button {
    @extend .btn-primary;
  }
}
```

### 3. 響應式設計
```scss
.component {
  display: flex;
  gap: $spacing-lg;
  
  @include mobile {
    flex-direction: column;
    gap: $spacing-md;
  }
}
```

## 注意事項

1. **一致性**：所有新元件都應遵循此設計系統
2. **可訪問性**：確保色彩對比度符合WCAG標準
3. **效能**：避免過度使用陰影和動畫效果
4. **維護性**：使用變數和混入，避免硬編碼值
5. **測試**：在不同裝置和瀏覽器中測試響應式效果

## 更新記錄

- **2024-01-XX**：建立初始設計系統
- 基於現有的FamilyTree專案UI風格
- 涵蓋所有主要元件和互動元素
- 包含完整的響應式設計規範 