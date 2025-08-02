/**
 * 開發環境設定
 */
export const environment = {
  production: false,
  
  /**
   * API 基礎設定
   */
  api: {
    baseUrl: 'http://localhost:5088',
    timeout: 30000, // 30秒
    endpoints: {
      auth: '/api/auth',
      user: '/api/user',
      person: '/api/person',
      project: '/api/project',
      file: '/api/file',
      auditLog: '/api/auditlog',
      fullTextSearch: '/api/fulltextsearch',
      favorites: '/api/favorites',
      photoUpload: '/api/photoupload',
      visualAnalysis: '/api/visualanalysis',
      organizationChart: '/api/organizationchart'
    }
  },

  /**
   * 功能開關
   */
  features: {
    enableAuditLog: true,
    enableFullTextSearch: true,
    enableVisualAnalysis: true,
    enableDebugMode: true,
    enableConsoleLogging: true
  },

  /**
   * 分頁設定
   */
  pagination: {
    defaultPageSize: 20,
    maxPageSize: 100,
    showSizeOptions: [10, 20, 50, 100]
  },

  /**
   * 檔案上傳設定
   */
  fileUpload: {
    maxSizeInMB: 10,
    allowedTypes: ['image/jpeg', 'image/png', 'image/gif', 'application/pdf'],
    maxFiles: 5
  },

  /**
   * UI 設定
   */
  ui: {
    theme: 'default',
    language: 'zh-TW',
    dateFormat: 'yyyy-MM-dd',
    timeFormat: 'HH:mm:ss'
  },

  /**
   * 快取設定
   */
  cache: {
    enabled: true,
    duration: 300000, // 5分鐘
    keys: {
      userProfile: 'user_profile',
      permissions: 'user_permissions',
      roles: 'user_roles'
    }
  }
};