/**
 * 生產環境設定
 */
export const environment = {
  production: true,
  
  /**
   * API 基礎設定
   */
  api: {
    baseUrl: 'https://api.familytree.com', // 生產環境API地址
    timeout: 15000, // 15秒
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
    enableDebugMode: false,
    enableConsoleLogging: false
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
    duration: 600000, // 10分鐘 (生產環境較長快取時間)
    keys: {
      userProfile: 'user_profile',
      permissions: 'user_permissions',
      roles: 'user_roles'
    }
  }
};