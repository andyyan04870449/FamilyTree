import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AuthService } from './auth.service';

// 稽核日誌相關介面
export interface AuditLogFilter {
  fromDate?: string;
  toDate?: string;
  userId?: string;
  userName?: string;
  userRole?: string;
  eventTypes?: string[];
  actions?: string[];
  eventCategory?: string;
  resourceType?: string;
  resourceId?: string;
  resourceName?: string;
  success?: boolean;
  errorCode?: string;
  securityLevels?: string[];
  onlySuspicious?: boolean;
  minRiskScore?: number;
  maxRiskScore?: number;
  ipAddress?: string;
  sessionId?: string;
  batchId?: string;
  tags?: string[];
  complianceFlags?: string[];
  searchText?: string;
  searchFields?: string[];
  page?: number;
  pageSize?: number;
  sortField?: string;
  sortDirection?: string;
  includeChangeDetails?: boolean;
  maskSensitiveData?: boolean;
}

export interface AuditLogEntry {
  id: number;
  eventId: string;
  batchId?: string;
  sessionId?: string;
  userId?: string;
  userName?: string;
  userRole?: string;
  eventType: string;
  action: string;
  resourceType?: string;
  resourceId?: string;
  resourceName?: string;
  oldValues?: any;
  newValues?: any;
  changesSummary?: string;
  ipAddress?: string;
  userAgent?: string;
  requestMethod?: string;
  requestUrl?: string;
  requestId?: string;
  success: boolean;
  errorMessage?: string;
  errorCode?: string;
  responseTimeMs?: number;
  securityLevel: string;
  riskScore: number;
  isSuspicious: boolean;
  occurredAt: string;
  createdAt: string;
  additionalData?: any;
  tags?: string[];
  complianceFlags?: string[];
}

export interface AuditLogQueryResult {
  logs: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  aggregations?: { [key: string]: any };
  queryExecutedAt: string;
  queryDuration: string;
}

export interface AuditLogSummary {
  logDate: string;
  eventType: string;
  action: string;
  resourceType?: string;
  eventCount: number;
  failedCount: number;
  suspiciousCount: number;
  avgResponseTime?: number;
  uniqueUsers: number;
}

export interface AuditStatistics {
  periodStart: string;
  periodEnd: string;
  totalEvents: number;
  successfulEvents: number;
  failedEvents: number;
  suspiciousEvents: number;
  uniqueUsers: number;
  uniqueResources: number;
  successRate: number;
  eventTypeBreakdown: { [key: string]: number };
  actionBreakdown: { [key: string]: number };
  securityLevelBreakdown: { [key: string]: number };
  hourlyBreakdown: { [key: string]: number };
  topUsers: TopUserActivity[];
  topResources: TopResourceActivity[];
}

export interface TopUserActivity {
  userId: string;
  userName: string;
  eventCount: number;
  failedEventCount: number;
  lastActivity: string;
}

export interface TopResourceActivity {
  resourceType: string;
  resourceId: string;
  resourceName: string;
  eventCount: number;
  lastAccessed: string;
}

export interface EventType {
  code: string;
  name: string;
  category: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  error?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuditLogService {
  private readonly apiUrl = '/api/auditlog';
  private currentFilter$ = new BehaviorSubject<AuditLogFilter>({});
  private eventTypes: EventType[] = [];

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {
    this.loadEventTypes();
  }

  /**
   * 查詢稽核日誌
   */
  queryLogs(filter: AuditLogFilter): Observable<AuditLogQueryResult> {
    return this.http.post<ApiResponse<AuditLogQueryResult>>(`${this.apiUrl}/query`, filter)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            this.currentFilter$.next(filter);
            return response.data;
          }
          throw new Error(response.message || '查詢稽核日誌失敗');
        }),
        catchError(error => {
          console.error('查詢稽核日誌錯誤:', error);
          throw error;
        })
      );
  }

  /**
   * 獲取稽核日誌摘要
   */
  getSummary(fromDate?: string, toDate?: string): Observable<AuditLogSummary[]> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<ApiResponse<AuditLogSummary[]>>(`${this.apiUrl}/summary`, { params })
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || '獲取稽核摘要失敗');
        })
      );
  }

  /**
   * 獲取稽核統計資料
   */
  getStatistics(fromDate?: string, toDate?: string): Observable<AuditStatistics> {
    let params = new HttpParams();
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<ApiResponse<AuditStatistics>>(`${this.apiUrl}/statistics`, { params })
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || '獲取稽核統計失敗');
        })
      );
  }

  /**
   * 匯出稽核日誌
   */
  exportLogs(filter: AuditLogFilter, format: 'CSV' | 'JSON' = 'CSV'): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/export?format=${format}`, filter, {
      responseType: 'blob'
    });
  }

  /**
   * 獲取事件類型列表
   */
  getEventTypes(): Observable<EventType[]> {
    if (this.eventTypes.length > 0) {
      return new Observable(observer => {
        observer.next(this.eventTypes);
        observer.complete();
      });
    }

    return this.http.get<ApiResponse<EventType[]>>(`${this.apiUrl}/event-types`)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            this.eventTypes = response.data;
            return response.data;
          }
          throw new Error(response.message || '獲取事件類型失敗');
        })
      );
  }

  /**
   * 生成合規性報告
   */
  generateComplianceReport(reportType: string, fromDate: string, toDate: string): Observable<{ reportId: string }> {
    let params = new HttpParams()
      .set('reportType', reportType)
      .set('fromDate', fromDate)
      .set('toDate', toDate);

    return this.http.post<ApiResponse<{ reportId: string }>>(`${this.apiUrl}/compliance-report`, null, { params })
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || '生成合規性報告失敗');
        })
      );
  }

  /**
   * 清理過期日誌
   */
  cleanupExpiredLogs(): Observable<{ deletedCount: number }> {
    return this.http.post<ApiResponse<{ deletedCount: number }>>(`${this.apiUrl}/cleanup`, {})
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || '清理過期日誌失敗');
        })
      );
  }

  /**
   * 獲取系統狀態
   */
  getSystemStatus(): Observable<any> {
    return this.http.get<ApiResponse<any>>(`${this.apiUrl}/system-status`)
      .pipe(
        map(response => {
          if (response.success && response.data) {
            return response.data;
          }
          throw new Error(response.message || '獲取系統狀態失敗');
        })
      );
  }

  /**
   * 獲取當前過濾條件
   */
  getCurrentFilter(): Observable<AuditLogFilter> {
    return this.currentFilter$.asObservable();
  }

  /**
   * 重設過濾條件
   */
  resetFilter(): void {
    this.currentFilter$.next({});
  }

  /**
   * 獲取預設的過濾條件
   */
  getDefaultFilter(): AuditLogFilter {
    const now = new Date();
    const thirtyDaysAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);

    return {
      fromDate: this.formatDate(thirtyDaysAgo),
      toDate: this.formatDate(now),
      page: 1,
      pageSize: 50,
      sortField: 'occurredAt',
      sortDirection: 'DESC',
      maskSensitiveData: true
    };
  }

  /**
   * 格式化日期為 ISO 字串
   */
  private formatDate(date: Date): string {
    return date.toISOString().split('.')[0] + 'Z';
  }

  /**
   * 載入事件類型
   */
  private loadEventTypes(): void {
    this.getEventTypes().subscribe(
      types => this.eventTypes = types,
      error => console.warn('載入事件類型失敗:', error)
    );
  }

  /**
   * 格式化事件類型顯示名稱
   */
  getEventTypeName(code: string): string {
    const eventType = this.eventTypes.find(t => t.code === code);
    return eventType ? eventType.name : code;
  }

  /**
   * 根據分類獲取事件類型
   */
  getEventTypesByCategory(category: string): EventType[] {
    return this.eventTypes.filter(t => t.category === category);
  }

  /**
   * 獲取所有分類
   */
  getCategories(): string[] {
    const categories = [...new Set(this.eventTypes.map(t => t.category))];
    return categories.sort();
  }

  /**
   * 檢查使用者是否有稽核日誌查看權限
   */
  canViewAuditLogs(): boolean {
    const user = this.authService.currentUserValue;
    if (!user || !user.role) return false;
    
    const role = user.role.toLowerCase();
    return role === 'admin' || role === 'auditreader';
  }

  /**
   * 檢查使用者是否有管理權限
   */
  canManageAuditLogs(): boolean {
    const user = this.authService.currentUserValue;
    if (!user || !user.role) return false;
    
    const role = user.role.toLowerCase();
    return role === 'admin';
  }
}