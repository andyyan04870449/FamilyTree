import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';

export interface SelectOption {
  value: string;
  label: string;
}

export interface AuditConfigResponse {
  success: boolean;
  data: SelectOption[];
  message?: string;
}

export interface AllConfigsResponse {
  success: boolean;
  data: {
    eventTypes: SelectOption[];
    securityLevels: SelectOption[];
    operationResults: SelectOption[];
    resourceTypes: SelectOption[];
  };
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuditConfigService {
  private readonly apiUrl = '/api/audit/config';
  private configCache = new Map<string, SelectOption[]>();
  private allConfigsSubject = new BehaviorSubject<any>(null);

  constructor(private http: HttpClient) {
    this.loadAllConfigs();
  }

  /**
   * 獲取事件類型選項
   */
  getEventTypes(): Observable<SelectOption[]> {
    return this.getCachedConfig('event-types', `${this.apiUrl}/event-types`);
  }

  /**
   * 獲取安全等級選項
   */
  getSecurityLevels(): Observable<SelectOption[]> {
    return this.getCachedConfig('security-levels', `${this.apiUrl}/security-levels`);
  }

  /**
   * 獲取操作結果選項
   */
  getOperationResults(): Observable<SelectOption[]> {
    return this.getCachedConfig('operation-results', `${this.apiUrl}/operation-results`);
  }

  /**
   * 獲取資源類型選項
   */
  getResourceTypes(): Observable<SelectOption[]> {
    return this.getCachedConfig('resource-types', `${this.apiUrl}/resource-types`);
  }

  /**
   * 獲取所有配置（一次性載入，提升效能）
   */
  getAllConfigs(): Observable<any> {
    return this.allConfigsSubject.asObservable();
  }

  /**
   * 重新載入所有配置
   */
  reloadConfigs(): Observable<any> {
    this.configCache.clear();
    return this.loadAllConfigs();
  }

  /**
   * 從快取或API獲取配置
   */
  private getCachedConfig(key: string, url: string): Observable<SelectOption[]> {
    if (this.configCache.has(key)) {
      return of(this.configCache.get(key)!);
    }

    return this.http.get<AuditConfigResponse>(url).pipe(
      map(response => {
        if (response.success && response.data) {
          this.configCache.set(key, response.data);
          return response.data;
        }
        return [];
      }),
      catchError(error => {
        console.error(`載入 ${key} 配置失敗:`, error);
        return of([]);
      })
    );
  }

  /**
   * 載入所有配置
   */
  private loadAllConfigs(): Observable<any> {
    return this.http.get<AllConfigsResponse>(`${this.apiUrl}/all`).pipe(
      map(response => {
        if (response.success && response.data) {
          // 更新快取
          this.configCache.set('event-types', response.data.eventTypes);
          this.configCache.set('security-levels', response.data.securityLevels);
          this.configCache.set('operation-results', response.data.operationResults);
          this.configCache.set('resource-types', response.data.resourceTypes);
          
          this.allConfigsSubject.next(response.data);
          return response.data;
        }
        return null;
      }),
      catchError(error => {
        console.error('載入配置失敗:', error);
        // 如果載入失敗，使用預設配置
        const defaultConfigs = this.getDefaultConfigs();
        this.allConfigsSubject.next(defaultConfigs);
        return of(defaultConfigs);
      })
    );
  }

  /**
   * 獲取預設配置（備援機制）
   */
  private getDefaultConfigs() {
    return {
      eventTypes: [
        { value: '', label: '全部事件類型' },
        { value: 'LOGIN', label: '登入' },
        { value: 'LOGOUT', label: '登出' },
        { value: 'DATA_ACCESS', label: '資料存取' }
      ],
      securityLevels: [
        { value: '', label: '全部安全等級' },
        { value: 'LOW', label: '低' },
        { value: 'NORMAL', label: '一般' },
        { value: 'HIGH', label: '高' }
      ],
      operationResults: [
        { value: '', label: '全部結果' },
        { value: 'true', label: '成功' },
        { value: 'false', label: '失敗' }
      ],
      resourceTypes: [
        { value: '', label: '全部資源類型' },
        { value: 'USER', label: '使用者' },
        { value: 'PROJECT', label: '專案' }
      ]
    };
  }

  /**
   * 清除快取
   */
  clearCache(): void {
    this.configCache.clear();
  }
}