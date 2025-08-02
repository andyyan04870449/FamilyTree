// 基礎 HTTP 服務 - 統一管理 API 請求和響應處理
// 設計理念：提供一致的 API 調用體驗，減少重複的錯誤處理代碼

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError, of } from 'rxjs';
import { catchError, map, tap, finalize } from 'rxjs/operators';
import { ErrorHandlerUtils, StandardError } from '../utils/error-handler.utils';
import { ApiResponse } from '../models/api-response.model';


export interface RequestOptions {
  headers?: HttpHeaders | { [header: string]: string | string[] };
  params?: HttpParams | { [param: string]: string | string[] };
  reportProgress?: boolean;
  responseType?: 'json';
  withCredentials?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export abstract class BaseHttpService {
  
  // 請求計數器（用於顯示 loading 狀態）
  private requestCount = 0;
  
  constructor(protected http: HttpClient) {}

  /**
   * 取得 API 基礎 URL
   */
  protected abstract getBaseUrl(): string;

  /**
   * 取得預設的請求標頭
   */
  protected getDefaultHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json'
    });
  }

  /**
   * GET 請求
   */
  protected get<T>(url: string, options?: RequestOptions): Observable<T> {
    return this.request<T>('GET', url, null, options);
  }

  /**
   * POST 請求
   */
  protected post<T>(url: string, body?: any, options?: RequestOptions): Observable<T> {
    return this.request<T>('POST', url, body, options);
  }

  /**
   * PUT 請求
   */
  protected put<T>(url: string, body?: any, options?: RequestOptions): Observable<T> {
    return this.request<T>('PUT', url, body, options);
  }

  /**
   * DELETE 請求
   */
  protected delete<T>(url: string, options?: RequestOptions): Observable<T> {
    return this.request<T>('DELETE', url, null, options);
  }

  /**
   * PATCH 請求
   */
  protected patch<T>(url: string, body?: any, options?: RequestOptions): Observable<T> {
    return this.request<T>('PATCH', url, body, options);
  }

  /**
   * 統一的請求處理
   */
  private request<T>(
    method: string,
    url: string,
    body?: any,
    options?: RequestOptions
  ): Observable<T> {
    // 組合完整 URL
    const fullUrl = this.buildUrl(url);
    
    // 合併請求選項
    const requestOptions = this.mergeOptions(options);
    
    // 記錄請求開始
    this.onRequestStart();
    
    // 根據方法執行請求
    let request$: Observable<any>;
    
    switch (method) {
      case 'GET':
        request$ = this.http.get(fullUrl, requestOptions);
        break;
      case 'POST':
        request$ = this.http.post(fullUrl, body, requestOptions);
        break;
      case 'PUT':
        request$ = this.http.put(fullUrl, body, requestOptions);
        break;
      case 'DELETE':
        request$ = this.http.delete(fullUrl, requestOptions);
        break;
      case 'PATCH':
        request$ = this.http.patch(fullUrl, body, requestOptions);
        break;
      default:
        request$ = throwError(() => new Error(`不支援的 HTTP 方法: ${method}`));
    }
    
    return request$.pipe(
      tap(response => this.logResponse(method, fullUrl, response)),
      map(response => this.extractData<T>(response)),
      catchError(error => this.handleError(error, method, fullUrl)),
      finalize(() => this.onRequestEnd())
    );
  }

  /**
   * 組合完整 URL
   */
  private buildUrl(url: string): string {
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    
    const baseUrl = this.getBaseUrl();
    const separator = baseUrl.endsWith('/') || url.startsWith('/') ? '' : '/';
    return `${baseUrl}${separator}${url}`;
  }

  /**
   * 合併請求選項
   */
  private mergeOptions(options?: RequestOptions): RequestOptions {
    const defaultHeaders = this.getDefaultHeaders();
    
    if (!options) {
      return { headers: defaultHeaders };
    }
    
    // 合併 headers
    let headers = defaultHeaders;
    if (options.headers) {
      if (options.headers instanceof HttpHeaders) {
        // 合併兩個 HttpHeaders
        const httpHeaders = options.headers;
        httpHeaders.keys().forEach(key => {
          const values = httpHeaders.getAll(key);
          if (values) {
            headers = headers.delete(key);
            values.forEach((value: string) => {
              headers = headers.append(key, value);
            });
          }
        });
      } else {
        // 從物件創建並合併
        const headerObj = options.headers as { [header: string]: string | string[] };
        Object.keys(headerObj).forEach(key => {
          const value = headerObj[key];
        if (Array.isArray(value)) {
          value.forEach(v => {
            headers = headers.append(key, v);
          });
        } else {
          headers = headers.set(key, value as string);
        }
        });
      }
    }
    
    return { ...options, headers };
  }

  /**
   * 提取響應數據
   */
  private extractData<T>(response: any): T {
    // 如果響應已經是需要的格式，直接返回
    if (response === null || response === undefined) {
      return response;
    }
    
    // 處理標準 API 響應格式
    if (this.isApiResponse(response)) {
      if (!response.success) {
        throw new Error(response.message || '請求失敗');
      }
      return response.data !== undefined ? response.data : response as T;
    }
    
    // 返回原始響應
    return response;
  }

  /**
   * 檢查是否為標準 API 響應格式
   */
  private isApiResponse(response: any): response is ApiResponse {
    return response && 
           typeof response === 'object' && 
           'success' in response;
  }

  /**
   * 處理錯誤
   */
  private handleError(error: any, method: string, url: string): Observable<never> {
    // 記錄錯誤
    console.error(`[${method}] ${url} 錯誤:`, error);
    
    // 如果是 HTTP 錯誤，使用統一的錯誤處理
    if (error instanceof HttpErrorResponse) {
      return ErrorHandlerUtils.handleHttpError(error);
    }
    
    // 其他錯誤
    return ErrorHandlerUtils.handleError(error);
  }

  /**
   * 記錄響應（用於調試）
   */
  private logResponse(method: string, url: string, response: any): void {
    if (this.isDebugMode()) {
      console.log(`[${method}] ${url} 響應:`, response);
    }
  }

  /**
   * 請求開始時的處理
   */
  private onRequestStart(): void {
    this.requestCount++;
    this.updateLoadingState();
  }

  /**
   * 請求結束時的處理
   */
  private onRequestEnd(): void {
    this.requestCount--;
    this.updateLoadingState();
  }

  /**
   * 更新 loading 狀態
   */
  private updateLoadingState(): void {
    // 子類可以覆寫此方法來實現 loading 狀態管理
    // 例如：this.loadingService.setLoading(this.requestCount > 0);
  }

  /**
   * 是否為調試模式
   */
  private isDebugMode(): boolean {
    // 可以根據環境變數或配置來決定
    return false;
  }

  /**
   * 處理分頁響應
   */
  protected handlePagedResponse<T>(response: any): PagedResponse<T> {
    return {
      data: response.data || [],
      total: response.total || response.totalCount || 0,
      page: response.page || response.currentPage || 1,
      pageSize: response.pageSize || response.size || 20,
      hasNext: response.hasNext || response.hasMore || false,
      hasPrevious: response.hasPrevious || false
    };
  }

  /**
   * 建立查詢參數
   */
  protected buildParams(params: { [key: string]: any }): HttpParams {
    let httpParams = new HttpParams();
    
    Object.keys(params).forEach(key => {
      const value = params[key];
      if (value !== null && value !== undefined && value !== '') {
        if (Array.isArray(value)) {
          value.forEach(item => {
            httpParams = httpParams.append(key, String(item));
          });
        } else {
          httpParams = httpParams.set(key, String(value));
        }
      }
    });
    
    return httpParams;
  }

  /**
   * 處理檔案上傳
   */
  protected uploadFile(
    url: string,
    file: File,
    additionalData?: { [key: string]: any }
  ): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    
    if (additionalData) {
      Object.keys(additionalData).forEach(key => {
        formData.append(key, additionalData[key]);
      });
    }
    
    // 檔案上傳不設置 Content-Type，讓瀏覽器自動設置
    const headers = new HttpHeaders();
    
    return this.http.post(this.buildUrl(url), formData, {
      headers,
      reportProgress: true,
      observe: 'events'
    }).pipe(
      catchError(error => this.handleError(error, 'POST', url))
    );
  }
}

/**
 * 分頁響應介面
 */
export interface PagedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  hasNext: boolean;
  hasPrevious: boolean;
}