/**
 * 使用者帳號管理頁面常數定義
 */

// 分頁設定常數
export const USER_MANAGEMENT_CONSTANTS = {
  // 每頁顯示數量選項
  PAGE_SIZES: [10, 20, 50, 100] as const,
  
  // 預設每頁顯示數量
  DEFAULT_PAGE_SIZE: 10,
  
  // 分頁控制器最大顯示頁數
  MAX_VISIBLE_PAGES: 5,
  
  // 搜尋防抖時間（毫秒）
  SEARCH_DEBOUNCE_TIME: 300,
} as const;

// 共用的 CSS 類名常數
export const CSS_CLASSES = {
  // 輸入框樣式
  INPUT: 'w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent',
  
  // 選擇框樣式
  SELECT: 'w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white',
  
  // 表格儲存格樣式
  TABLE_CELL: 'px-6 py-4 whitespace-nowrap text-sm',
  
  // 表格標題樣式
  TABLE_HEADER: 'px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider',
  
  // 狀態徽章基礎樣式
  STATUS_BADGE_BASE: 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
  
  // 啟用狀態樣式
  STATUS_ACTIVE: 'bg-green-100 text-green-800',
  
  // 停用狀態樣式
  STATUS_INACTIVE: 'bg-red-100 text-red-800',
} as const;

// 使用者狀態選項
export const USER_STATUS_OPTIONS = [
  { value: '', label: '全部狀態' },
  { value: 'active', label: '啟用' },
  { value: 'inactive', label: '停用' }
] as const;

// 使用者狀態顯示映射
export const USER_STATUS_DISPLAY = {
  active: '啟用',
  inactive: '停用'
} as const;

// 表單驗證規則
export const VALIDATION_RULES = {
  // 帳號名稱
  USERNAME: {
    MIN_LENGTH: 3,
    MAX_LENGTH: 20,
    PATTERN: /^[a-zA-Z0-9_]+$/
  },
  
  // 全名
  FULL_NAME: {
    MIN_LENGTH: 2,
    MAX_LENGTH: 50
  },
  
  // 電子郵件
  EMAIL: {
    PATTERN: /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  },
  
  // 密碼
  PASSWORD: {
    MIN_LENGTH: 8,
    MAX_LENGTH: 128,
    PATTERN: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/
  }
} as const;

// 錯誤訊息常數
export const ERROR_MESSAGES = {
  REQUIRED_FIELD: '此欄位為必填',
  INVALID_EMAIL: '請輸入有效的電子郵件地址',
  INVALID_USERNAME: '帳號只能包含字母、數字和底線',
  PASSWORD_TOO_SHORT: `密碼長度至少需要 ${VALIDATION_RULES.PASSWORD.MIN_LENGTH} 個字符`,
  PASSWORD_WEAK: '密碼需包含大小寫字母和數字',
  USERNAME_TOO_SHORT: `帳號長度至少需要 ${VALIDATION_RULES.USERNAME.MIN_LENGTH} 個字符`,
  FULL_NAME_TOO_SHORT: `姓名長度至少需要 ${VALIDATION_RULES.FULL_NAME.MIN_LENGTH} 個字符`,
  LOAD_USERS_FAILED: '載入使用者列表失敗',
  SAVE_USER_FAILED: '儲存使用者資料失敗',
  DELETE_USER_FAILED: '刪除使用者失敗'
} as const;