import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

/**
 * API設定服務
 * 
 * 提供統一的API端點管理，避免硬代碼URL散布在各個服務中
 * 支援不同環境的配置切換
 */
@Injectable({
  providedIn: 'root'
})
export class ApiConfigService {
  private readonly config = environment.api;

  /**
   * 取得API基礎URL
   */
  get baseUrl(): string {
    return this.config.baseUrl;
  }

  /**
   * 取得請求超時時間
   */
  get timeout(): number {
    return this.config.timeout;
  }

  /**
   * 取得完整的API端點URL
   * 
   * @param endpoint 端點名稱
   * @returns 完整的API URL
   */
  getEndpointUrl(endpoint: keyof typeof this.config.endpoints): string {
    const endpointPath = this.config.endpoints[endpoint];
    return `${this.baseUrl}${endpointPath}`;
  }

  /**
   * 建構自定義端點URL
   * 
   * @param basePath 基礎路徑
   * @param subPath 子路徑（可選）
   * @returns 完整的API URL
   */
  buildUrl(basePath: string, subPath?: string): string {
    const base = basePath.startsWith('/') ? basePath : `/${basePath}`;
    const full = subPath ? `${base}/${subPath}` : base;
    return `${this.baseUrl}${full}`;
  }

  /**
   * 取得所有可用的端點
   */
  get availableEndpoints(): string[] {
    return Object.keys(this.config.endpoints);
  }

  /**
   * 檢查端點是否存在
   * 
   * @param endpoint 端點名稱
   * @returns 是否存在該端點
   */
  hasEndpoint(endpoint: string): boolean {
    return endpoint in this.config.endpoints;
  }

  /**
   * 取得環境特定的設定
   */
  get isProduction(): boolean {
    return environment.production;
  }

  /**
   * 取得功能開關狀態
   */
  isFeatureEnabled(feature: keyof typeof environment.features): boolean {
    return environment.features[feature];
  }

  /**
   * 取得分頁設定
   */
  get paginationConfig() {
    return environment.pagination;
  }

  /**
   * 取得檔案上傳設定
   */
  get fileUploadConfig() {
    return environment.fileUpload;
  }

  /**
   * 取得UI設定
   */
  get uiConfig() {
    return environment.ui;
  }

  /**
   * 取得快取設定
   */
  get cacheConfig() {
    return environment.cache;
  }

  /**
   * 便利方法：取得特定服務的端點
   */
  auth = {
    base: () => this.getEndpointUrl('auth'),
    login: () => `${this.getEndpointUrl('auth')}/login`,
    logout: () => `${this.getEndpointUrl('auth')}/logout`,
    register: () => `${this.getEndpointUrl('auth')}/register`,
    refresh: () => `${this.getEndpointUrl('auth')}/refresh`,
    profile: () => `${this.getEndpointUrl('auth')}/profile`
  };

  user = {
    base: () => this.getEndpointUrl('user'),
    list: () => this.getEndpointUrl('user'),
    detail: (id: string) => `${this.getEndpointUrl('user')}/${id}`,
    update: (id: string) => `${this.getEndpointUrl('user')}/${id}`,
    changePassword: (id: string) => `${this.getEndpointUrl('user')}/${id}/change-password`,
    resetPassword: (id: string) => `${this.getEndpointUrl('user')}/${id}/reset-password`,
    checkUsername: () => `${this.getEndpointUrl('user')}/check-username`,
    checkEmail: () => `${this.getEndpointUrl('user')}/check-email`
  };

  person = {
    base: () => this.getEndpointUrl('person'),
    list: () => this.getEndpointUrl('person'),
    detail: (id: string) => `${this.getEndpointUrl('person')}/${id}`,
    create: () => this.getEndpointUrl('person'),
    update: (id: string) => `${this.getEndpointUrl('person')}/${id}`,
    delete: (id: string) => `${this.getEndpointUrl('person')}/${id}`,
    search: () => `${this.getEndpointUrl('person')}/search`,
    relationships: (id: string) => `${this.getEndpointUrl('person')}/${id}/relationships`
  };

  project = {
    base: () => this.getEndpointUrl('project'),
    list: () => this.getEndpointUrl('project'),
    detail: (id: string) => `${this.getEndpointUrl('project')}/${id}`,
    create: () => this.getEndpointUrl('project'),
    update: (id: string) => `${this.getEndpointUrl('project')}/${id}`,
    delete: (id: string) => `${this.getEndpointUrl('project')}/${id}`
  };

  file = {
    base: () => this.getEndpointUrl('file'),
    upload: () => `${this.getEndpointUrl('file')}/upload`,
    download: (id: string) => `${this.getEndpointUrl('file')}/${id}/download`,
    delete: (id: string) => `${this.getEndpointUrl('file')}/${id}`
  };

  auditLog = {
    base: () => this.getEndpointUrl('auditLog'),
    query: () => `${this.getEndpointUrl('auditLog')}/query`,
    summary: () => `${this.getEndpointUrl('auditLog')}/summary`,
    statistics: () => `${this.getEndpointUrl('auditLog')}/statistics`,
    export: () => `${this.getEndpointUrl('auditLog')}/export`,
    eventTypes: () => `${this.getEndpointUrl('auditLog')}/event-types`
  };

  fullTextSearch = {
    base: () => this.getEndpointUrl('fullTextSearch'),
    search: () => `${this.getEndpointUrl('fullTextSearch')}/search`,
    index: () => `${this.getEndpointUrl('fullTextSearch')}/index`,
    rebuild: () => `${this.getEndpointUrl('fullTextSearch')}/rebuild`
  };

  favorites = {
    base: () => this.getEndpointUrl('favorites'),
    list: () => this.getEndpointUrl('favorites'),
    add: () => this.getEndpointUrl('favorites'),
    remove: (id: string) => `${this.getEndpointUrl('favorites')}/${id}`
  };

  photoUpload = {
    base: () => this.getEndpointUrl('photoUpload'),
    upload: () => `${this.getEndpointUrl('photoUpload')}/upload`,
    crop: () => `${this.getEndpointUrl('photoUpload')}/crop`
  };

  visualAnalysis = {
    base: () => this.getEndpointUrl('visualAnalysis'),
    analyze: () => `${this.getEndpointUrl('visualAnalysis')}/analyze`,
    export: () => `${this.getEndpointUrl('visualAnalysis')}/export`
  };

  organizationChart = {
    base: () => this.getEndpointUrl('organizationChart'),
    data: () => `${this.getEndpointUrl('organizationChart')}/data`,
    export: () => `${this.getEndpointUrl('organizationChart')}/export`
  };
}