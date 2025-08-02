// 應用程式常數定義
export class AppConstants {
  // API 相關 - 根據環境自動選擇正確的API URL
  static readonly API_BASE_URL = AppConstants.getApiBaseUrl();
  static readonly PERSON_API_URL = `${AppConstants.API_BASE_URL}/PersonData`;

  // 動態取得API基礎URL
  private static getApiBaseUrl(): string {
    const hostname = window.location.hostname;
    const port = window.location.port;
    
    console.log('🌐 當前主機資訊:', { hostname, port, protocol: window.location.protocol });
    
    // 如果是通過ngrok訪問（包含.ngrok.io）
    if (hostname.includes('.ngrok.io')) {
      console.log('🔗 使用 ngrok URL');
      return 'https://KUNYOU-POC-backend.ngrok.io/api';
    }
    
    // 如果是本地開發環境
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      console.log('🏠 本地開發環境，使用代理');
      return '/api'; // 使用代理
    }
    
    // 如果代理失敗，嘗試直接連接（備用方案）
    if (localStorage.getItem('use-direct-api') === 'true') {
      console.log('🔧 使用直接 API 連接');
      return 'http://localhost:5088/api';
    }
    
    // 其他環境
    console.log('🌍 其他環境，使用代理');
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
    OPERATION_FAILED: '操作失敗',
    CREATE_FAILED: '創建失敗',
    UPDATE_FAILED: '更新失敗',
    DELETE_FAILED: '刪除失敗',
    STATISTICS_FAILED: '獲取統計資訊失敗'
  } as const;

  // 狀態欄相關
  static readonly SESSION_TIMEOUT_SECONDS = 600; // 10分鐘
  static readonly DEFAULT_USER_NAME = '王小明';

  // 分析工作相關
  static readonly REFRESH_INTERVAL_MS = 3000; // 3秒
  static readonly MAX_RETRY_COUNT = 3;

  // 圖譜相關
  static readonly GRAPH_ANIMATION_DURATION = 300;
  static readonly ZOOM_SCALE_FACTOR = 1.2;

  // 狀態文字
  static readonly STATUS_TEXTS = {
    processing: '處理中',
    completed: '已完成',
    failed: '失敗',
    unknown: '未知'
  } as const;

  // 通知類型
  static readonly NOTIFICATION_TYPES = {
    success: 'success',
    error: 'error',
    info: 'info'
  } as const;

  // 性別選項
  static readonly GENDER_OPTIONS = {
    male: '男',
    female: '女'
  } as const;

  // 預設搜索條件
  static readonly DEFAULT_SEARCH_CRITERIA = {
    idNumber: '',
    passportNumber: '',
    birthday: '',
    name: '黃心田',
    mobile: '',
    gender: '女'
  } as const;

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

    // 檔案狀態
    STATUS: {
      UPLOADED: 'uploaded',
      PROCESSING: 'processing',
      PROCESSED: 'processed',
      FAILED: 'failed',
      DELETED: 'deleted'
    } as const,

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
    VALIDATION_ERROR: '資料驗證失敗',
    TIMEOUT_ERROR: '操作超時',
    UNKNOWN_ERROR: '未知錯誤'
  } as const;

  // 成功訊息常數
  static readonly SUCCESS_MESSAGES = {
    DATA_LOADED: '資料載入成功',
    DATA_SAVED: '資料儲存成功',
    DATA_UPDATED: '資料更新成功',
    DATA_DELETED: '資料刪除成功',
    OPERATION_SUCCESS: '操作成功'
  } as const;

  // 示例數據（僅用於開發測試）
  static readonly SAMPLE_PERSONS = [
    { 
      id: 1, 
      name: '范立', 
      gender: '男', 
      birthday: '1988-06-07', 
      nationality: '中國', 
      mobile: '13357913171', 
      createdAt: new Date().toISOString(), 
      updatedAt: new Date().toISOString() 
    },
    { 
      id: 2, 
      name: '趙威', 
      gender: '男', 
      birthday: '1975-04-15', 
      nationality: '中國', 
      mobile: '13884937455', 
      createdAt: new Date().toISOString(), 
      updatedAt: new Date().toISOString() 
    },
    { 
      id: 3, 
      name: '項依潔', 
      gender: '女', 
      birthday: '1975-10-29', 
      nationality: '中國', 
      mobile: '13573590064', 
      createdAt: new Date().toISOString(), 
      updatedAt: new Date().toISOString() 
    }
  ];

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