// 應用程式常數定義 - 精簡版
export class AppConstants {
  // API 相關
  static readonly API_BASE_URL = AppConstants.getApiBaseUrl();
  static readonly PERSON_API_URL = `${AppConstants.API_BASE_URL}/PersonData`;

  // 動態取得API基礎URL
  private static getApiBaseUrl(): string {
    const hostname = window.location.hostname;
    const port = window.location.port;
    
    // 如果是通過ngrok訪問
    if (hostname.includes('.ngrok.io')) {
      return 'https://KUNYOU-POC-backend.ngrok.io/api';
    }
    
    // 本地開發環境
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      return '/api'; // 使用代理
    }
    
    // 備用方案
    if (localStorage.getItem('use-direct-api') === 'true') {
      return 'http://localhost:5088/api';
    }
    
    return '/api';
  }

  // 系統訊息常數
  static readonly MESSAGES = {
    PROJECT_NOT_SELECTED: '請先選擇專案',
    PROJECT_NOT_FOUND: '專案不存在',
    FILE_LIST_FAILED: '獲取檔案列表失敗',
    UPLOAD_SUCCESS: '檔案上傳成功',
    DELETE_SUCCESS: '檔案刪除成功',
    OPERATION_SUCCESS: '操作成功',
    OPERATION_FAILED: '操作失敗'
  } as const;

  // 狀態欄相關（實際使用中）
  static readonly SESSION_TIMEOUT_SECONDS = 600; // 10分鐘
  static readonly DEFAULT_USER_NAME = '王小明';

  // 檔案系統相關常數
  static readonly FILE_SYSTEM = {
    // 支援的檔案類型
    SUPPORTED_TYPES: {
      EXCEL: 'excel',
      PHOTO: 'photo',
      IMAGE: 'image',
      ARCHIVE: 'archive',
      UNKNOWN: 'unknown'
    } as const,

    // 支援的副檔名
    ALLOWED_EXTENSIONS: ['.xls', '.xlsx', '.jpg', '.jpeg', '.png', '.zip', '.7z'],

    // 檔案大小限制 (50MB)
    MAX_FILE_SIZE_MB: 50,
    MAX_FILE_SIZE_BYTES: 50 * 1024 * 1024,

    // 檔案類型對應
    TYPE_MAPPINGS: {
      '.xls': 'excel',
      '.xlsx': 'excel',
      '.jpg': 'photo',
      '.jpeg': 'photo',
      '.png': 'image',
      '.zip': 'archive',
      '.7z': 'archive'
    } as const,

    // 檔案類型圖示
    TYPE_ICONS: {
      excel: '📊',
      photo: '📸',
      image: '🖼️',
      archive: '📦',
      unknown: '📁'
    } as const,

    // 檔案類型名稱
    TYPE_NAMES: {
      excel: 'Excel 資料',
      photo: '照片檔案',
      image: '圖片檔案',
      archive: '壓縮檔案',
      unknown: '檔案'
    } as const,

    // 檔案上傳訊息
    MESSAGES: {
      UPLOAD_SUCCESS: '檔案上傳成功',
      UPLOAD_FAILED: '檔案上傳失敗',
      FILE_TOO_LARGE: '檔案大小超過限制',
      INVALID_FILE_TYPE: '不支援的檔案格式',
      SELECT_FILE_FIRST: '請先選擇檔案',
      DELETE_CONFIRM: '確定要刪除此檔案嗎？',
      DELETE_SUCCESS: '檔案刪除成功',
      DELETE_FAILED: '檔案刪除失敗'
    } as const
  } as const;

  // 錯誤訊息常數
  static readonly ERROR_MESSAGES = {
    NETWORK_ERROR: '網路連線錯誤',
    SERVER_ERROR: '伺服器錯誤',
    UNAUTHORIZED: '權限不足',
    NOT_FOUND: '找不到資源',
    VALIDATION_ERROR: '資料驗證失敗'
  } as const;

  // 成功訊息常數
  static readonly SUCCESS_MESSAGES = {
    DATA_LOADED: '資料載入成功',
    DATA_SAVED: '資料儲存成功',
    DATA_UPDATED: '資料更新成功',
    DATA_DELETED: '資料刪除成功'
  } as const;

  // 檔案工具方法
  static getFileTypeByExtension(fileName: string): string {
    if (!fileName) return AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN;
    
    const extension = fileName.toLowerCase().match(/\.[^.]+$/)?.[0] || '';
    return AppConstants.FILE_SYSTEM.TYPE_MAPPINGS[extension as keyof typeof AppConstants.FILE_SYSTEM.TYPE_MAPPINGS] || 
           AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN;
  }

  static getFileTypeIcon(fileType: string): string {
    return AppConstants.FILE_SYSTEM.TYPE_ICONS[fileType as keyof typeof AppConstants.FILE_SYSTEM.TYPE_ICONS] || 
           AppConstants.FILE_SYSTEM.TYPE_ICONS.unknown;
  }

  static getFileTypeName(fileType: string): string {
    return AppConstants.FILE_SYSTEM.TYPE_NAMES[fileType as keyof typeof AppConstants.FILE_SYSTEM.TYPE_NAMES] || 
           AppConstants.FILE_SYSTEM.TYPE_NAMES.unknown;
  }

  static isValidFileType(fileName: string): boolean {
    const extension = fileName.toLowerCase().match(/\.[^.]+$/)?.[0] || '';
    return AppConstants.FILE_SYSTEM.ALLOWED_EXTENSIONS.includes(extension as any);
  }

  static formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}