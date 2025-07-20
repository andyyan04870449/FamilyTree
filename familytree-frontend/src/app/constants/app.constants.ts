// 應用程式常數定義
export class AppConstants {
  // API 相關 - 根據環境自動選擇正確的API URL
  static readonly API_BASE_URL = AppConstants.getApiBaseUrl();
  static readonly PERSON_API_URL = `${AppConstants.API_BASE_URL}/person`;
  static readonly ANALYSIS_API_URL = `${AppConstants.API_BASE_URL}/analysis`;

  // 動態取得API基礎URL
  private static getApiBaseUrl(): string {
    const hostname = window.location.hostname;
    const port = window.location.port;
    
    // 如果是通過ngrok訪問（包含.ngrok.io）
    if (hostname.includes('.ngrok.io')) {
      // 使用後端的ngrok URL
      return 'https://familytree-backend-dev.ngrok.io/api';
    }
    
    // 如果是本地開發環境
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      return '/api'; // 使用代理
    }
    
    // 其他環境
    return '/api';
  }

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
} 