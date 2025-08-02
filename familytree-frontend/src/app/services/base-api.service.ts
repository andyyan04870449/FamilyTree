import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, tap, finalize } from 'rxjs/operators';
import { ToastService } from './toast.service';
import { ApiConfigService } from './api-config.service';

/**
 * API回應的標準格式
 */
export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data?: T;
  error?: string;
  timestamp?: string;
}

/**
 * 分頁查詢回應格式
 */
export interface PagedApiResponse<T = any> extends ApiResponse<T[]> {
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * API呼叫選項
 */
export interface ApiCallOptions {
  /** 成功時顯示的訊息 */
  successMessage?: string;
  /** 錯誤時顯示的訊息 */
  errorMessage?: string;
  /** 是否顯示載入中指示器 */
  showLoading?: boolean;
  /** 成功回調函數 */
  onSuccess?: (data: any) => void;
  /** 錯誤回調函數 */
  onError?: (error: any) => void;
  /** 是否自動處理錯誤 */
  autoHandleError?: boolean;
}

/**
 * 查詢參數介面
 */
export interface QueryParams {
  [key: string]: string | number | boolean | string[] | number[] | undefined;
}

/**
 * 基礎API服務類別
 * 
 * 提供統一的API呼叫模式，包含：
 * - 標準化的錯誤處理
 * - 統一的成功/錯誤訊息顯示
 * - 載入狀態管理
 * - 重複程式碼消除
 */
@Injectable({
  providedIn: 'root'
})
export abstract class BaseApiService {
  /**
   * 子類別必須提供的API基礎URL
   */
  protected abstract readonly apiUrl: string;

  /**
   * 當前載入狀態計數器
   */
  private loadingCount = 0;

  protected apiConfig = inject(ApiConfigService);

  constructor(
    protected http: HttpClient,
    protected toastService: ToastService
  ) {}

  /**
   * 執行API呼叫的通用方法
   * 
   * @param operation API操作的Observable
   * @param options 呼叫選項
   * @returns 處理後的Observable
   */
  protected handleApiCall<T>(
    operation: Observable<T>,
    options: ApiCallOptions = {}
  ): Observable<T> {
    const {
      successMessage,
      errorMessage,
      showLoading = false,
      onSuccess,
      onError,
      autoHandleError = true
    } = options;

    if (showLoading) {
      this.setLoading(true);
    }

    return operation.pipe(
      tap((response: any) => {
        // 處理成功回應
        if (successMessage) {
          this.toastService.success(successMessage);
        }
        
        if (onSuccess) {
          onSuccess(response);
        }
      }),
      catchError((error: HttpErrorResponse) => {
        // 處理錯誤回應
        const errorMsg = this.extractErrorMessage(error, errorMessage);
        
        if (autoHandleError) {
          this.toastService.error(errorMsg);
        }
        
        if (onError) {
          onError(error);
        }
        
        return throwError(() => error);
      }),
      finalize(() => {
        if (showLoading) {
          this.setLoading(false);
        }
      })
    );
  }

  /**
   * GET 請求
   */
  protected get<T>(
    endpoint: string,
    params?: QueryParams,
    options: ApiCallOptions = {}
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    const httpParams = this.buildHttpParams(params);
    
    const operation = this.http.get<T>(url, { params: httpParams });
    return this.handleApiCall(operation, options);
  }

  /**
   * POST 請求
   */
  protected post<T>(
    endpoint: string,
    body: any = {},
    options: ApiCallOptions = {}
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    
    const operation = this.http.post<T>(url, body);
    return this.handleApiCall(operation, options);
  }

  /**
   * PUT 請求
   */
  protected put<T>(
    endpoint: string,
    body: any = {},
    options: ApiCallOptions = {}
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    
    const operation = this.http.put<T>(url, body);
    return this.handleApiCall(operation, options);
  }

  /**
   * DELETE 請求
   */
  protected delete<T>(
    endpoint: string,
    options: ApiCallOptions = {}
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    
    const operation = this.http.delete<T>(url);
    return this.handleApiCall(operation, options);
  }

  /**
   * PATCH 請求
   */
  protected patch<T>(
    endpoint: string,
    body: any = {},
    options: ApiCallOptions = {}
  ): Observable<T> {
    const url = this.buildUrl(endpoint);
    
    const operation = this.http.patch<T>(url, body);
    return this.handleApiCall(operation, options);
  }

  /**
   * 處理分頁查詢
   */
  protected getPagedData<T>(
    endpoint: string,
    page: number = 1,
    pageSize: number = 20,
    params?: QueryParams,
    options: ApiCallOptions = {}
  ): Observable<PagedApiResponse<T>> {
    const queryParams = {
      page,
      pageSize,
      ...params
    };
    
    return this.get<PagedApiResponse<T>>(endpoint, queryParams, options);
  }

  /**
   * 建構完整的URL
   */
  private buildUrl(endpoint: string): string {
    // 如果是完整URL，直接返回
    if (endpoint.startsWith('http://') || endpoint.startsWith('https://')) {
      return endpoint;
    }
    
    // 移除開頭的斜線以避免重複
    const cleanEndpoint = endpoint.startsWith('/') ? endpoint.slice(1) : endpoint;
    
    // 確保apiUrl以斜線結尾
    const baseUrl = this.apiUrl.endsWith('/') ? this.apiUrl : `${this.apiUrl}/`;
    
    // 如果apiUrl是相對路徑，使用apiConfig的baseUrl
    const fullBaseUrl = this.apiUrl.startsWith('/') 
      ? `${this.apiConfig.baseUrl}${this.apiUrl}`
      : baseUrl;
    
    return `${fullBaseUrl}${cleanEndpoint}`;
  }

  /**
   * 建構HTTP查詢參數
   */
  private buildHttpParams(params?: QueryParams): HttpParams {
    let httpParams = new HttpParams();
    
    if (params) {
      Object.keys(params).forEach(key => {
        const value = params[key];
        if (value !== undefined && value !== null) {
          if (Array.isArray(value)) {
            value.forEach(item => {
              httpParams = httpParams.append(key, item.toString());
            });
          } else {
            httpParams = httpParams.set(key, value.toString());
          }
        }
      });
    }
    
    return httpParams;
  }

  /**
   * 從錯誤回應中提取錯誤訊息
   */
  private extractErrorMessage(error: HttpErrorResponse, fallbackMessage?: string): string {
    // 優先使用自定義錯誤訊息
    if (fallbackMessage) {
      return fallbackMessage;
    }

    // 嘗試從API回應中提取錯誤訊息
    if (error.error && typeof error.error === 'object') {
      if (error.error.message) {
        return error.error.message;
      }
      if (error.error.error) {
        return error.error.error;
      }
    }

    // 根據HTTP狀態碼返回預設訊息
    switch (error.status) {
      case 400:
        return '請求參數錯誤';
      case 401:
        return '未授權，請重新登入';
      case 403:
        return '權限不足';
      case 404:
        return '資源不存在';
      case 409:
        return '資料衝突';
      case 422:
        return '資料驗證失敗';
      case 429:
        return '請求過於頻繁，請稍後再試';
      case 500:
        return '伺服器內部錯誤';
      case 502:
        return '伺服器暫時無法使用';
      case 503:
        return '服務暫時不可用';
      default:
        return error.message || '發生未知錯誤';
    }
  }

  /**
   * 設定載入狀態
   */
  private setLoading(loading: boolean): void {
    if (loading) {
      this.loadingCount++;
    } else {
      this.loadingCount = Math.max(0, this.loadingCount - 1);
    }
    
    // 這裡可以發出載入狀態變化事件
    // 子類別可以覆寫此方法來實作自己的載入指示器
  }

  /**
   * 取得當前是否正在載入
   */
  protected get isLoading(): boolean {
    return this.loadingCount > 0;
  }

  /**
   * 便利方法：建立成功回應
   */
  protected createSuccessResponse<T>(data: T, message: string = '操作成功'): ApiResponse<T> {
    return {
      success: true,
      message,
      data,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * 便利方法：建立錯誤回應
   */
  protected createErrorResponse(message: string, error?: any): ApiResponse {
    return {
      success: false,
      message,
      error: error?.message || error,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * 便利方法：重試機制
   */
  protected retryApiCall<T>(
    operation: () => Observable<T>,
    maxRetries: number = 3,
    delayMs: number = 1000
  ): Observable<T> {
    return new Observable<T>(subscriber => {
      let retryCount = 0;
      
      const attempt = () => {
        operation().subscribe({
          next: (value) => subscriber.next(value),
          complete: () => subscriber.complete(),
          error: (error) => {
            if (retryCount < maxRetries) {
              retryCount++;
              setTimeout(() => attempt(), delayMs * retryCount);
            } else {
              subscriber.error(error);
            }
          }
        });
      };
      
      attempt();
    });
  }
}