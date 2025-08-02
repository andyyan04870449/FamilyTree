// 錯誤處理工具類 - 統一管理錯誤處理邏輯
// 設計理念：提供一致的錯誤處理體驗，減少重複代碼

import { HttpErrorResponse } from '@angular/common/http';
import { throwError, Observable } from 'rxjs';

export interface ErrorDetail {
  code: string;
  message: string;
  field?: string;
  details?: any;
}

export interface StandardError {
  message: string;
  code?: string;
  statusCode?: number;
  errors?: ErrorDetail[];
  timestamp?: Date;
  path?: string;
}

export class ErrorHandlerUtils {
  
  // 標準錯誤代碼
  static readonly ERROR_CODES = {
    // 網路錯誤
    NETWORK_ERROR: 'NETWORK_ERROR',
    TIMEOUT: 'TIMEOUT',
    
    // 客戶端錯誤
    BAD_REQUEST: 'BAD_REQUEST',
    UNAUTHORIZED: 'UNAUTHORIZED',
    FORBIDDEN: 'FORBIDDEN',
    NOT_FOUND: 'NOT_FOUND',
    CONFLICT: 'CONFLICT',
    VALIDATION_ERROR: 'VALIDATION_ERROR',
    
    // 伺服器錯誤
    INTERNAL_SERVER_ERROR: 'INTERNAL_SERVER_ERROR',
    SERVICE_UNAVAILABLE: 'SERVICE_UNAVAILABLE',
    
    // 業務錯誤
    BUSINESS_ERROR: 'BUSINESS_ERROR',
    DATA_NOT_FOUND: 'DATA_NOT_FOUND',
    DUPLICATE_DATA: 'DUPLICATE_DATA',
    OPERATION_FAILED: 'OPERATION_FAILED'
  } as const;

  // 標準錯誤訊息
  static readonly ERROR_MESSAGES: { [key: string]: string } = {
    // 網路錯誤
    [ErrorHandlerUtils.ERROR_CODES.NETWORK_ERROR]: '網路連線錯誤，請檢查網路連線',
    [ErrorHandlerUtils.ERROR_CODES.TIMEOUT]: '請求超時，請稍後再試',
    
    // 客戶端錯誤
    [ErrorHandlerUtils.ERROR_CODES.BAD_REQUEST]: '請求參數錯誤',
    [ErrorHandlerUtils.ERROR_CODES.UNAUTHORIZED]: '請先登入',
    [ErrorHandlerUtils.ERROR_CODES.FORBIDDEN]: '您沒有權限執行此操作',
    [ErrorHandlerUtils.ERROR_CODES.NOT_FOUND]: '找不到請求的資源',
    [ErrorHandlerUtils.ERROR_CODES.CONFLICT]: '資料衝突',
    [ErrorHandlerUtils.ERROR_CODES.VALIDATION_ERROR]: '資料驗證失敗',
    
    // 伺服器錯誤
    [ErrorHandlerUtils.ERROR_CODES.INTERNAL_SERVER_ERROR]: '伺服器內部錯誤',
    [ErrorHandlerUtils.ERROR_CODES.SERVICE_UNAVAILABLE]: '服務暫時無法使用',
    
    // 業務錯誤
    [ErrorHandlerUtils.ERROR_CODES.BUSINESS_ERROR]: '業務處理錯誤',
    [ErrorHandlerUtils.ERROR_CODES.DATA_NOT_FOUND]: '找不到相關資料',
    [ErrorHandlerUtils.ERROR_CODES.DUPLICATE_DATA]: '資料已存在',
    [ErrorHandlerUtils.ERROR_CODES.OPERATION_FAILED]: '操作失敗',
    
    // 預設錯誤
    DEFAULT: '發生未知錯誤，請稍後再試'
  };

  /**
   * 處理 HTTP 錯誤
   * @param error HTTP 錯誤
   * @param customMessage 自定義錯誤訊息
   * @returns Observable 錯誤
   */
  static handleHttpError(error: HttpErrorResponse, customMessage?: string): Observable<never> {
    const standardError = this.parseHttpError(error, customMessage);
    console.error('HTTP Error:', standardError);
    return throwError(() => standardError);
  }

  /**
   * 解析 HTTP 錯誤
   * @param error HTTP 錯誤
   * @param customMessage 自定義錯誤訊息
   * @returns 標準錯誤物件
   */
  static parseHttpError(error: HttpErrorResponse, customMessage?: string): StandardError {
    let message = customMessage || this.ERROR_MESSAGES['DEFAULT'];
    let code: string = this.ERROR_CODES.INTERNAL_SERVER_ERROR;
    let errors: ErrorDetail[] = [];

    // 網路錯誤
    if (error.error instanceof ErrorEvent) {
      code = this.ERROR_CODES.NETWORK_ERROR;
      message = customMessage || this.ERROR_MESSAGES[code];
    } 
    // HTTP 狀態碼錯誤
    else {
      switch (error.status) {
        case 0:
          code = this.ERROR_CODES.NETWORK_ERROR;
          message = customMessage || this.ERROR_MESSAGES[code];
          break;
        case 400:
          code = this.ERROR_CODES.BAD_REQUEST;
          message = this.extractErrorMessage(error) || customMessage || this.ERROR_MESSAGES[code];
          errors = this.extractValidationErrors(error);
          break;
        case 401:
          code = this.ERROR_CODES.UNAUTHORIZED;
          message = customMessage || this.ERROR_MESSAGES[code];
          break;
        case 403:
          code = this.ERROR_CODES.FORBIDDEN;
          message = customMessage || this.ERROR_MESSAGES[code];
          break;
        case 404:
          code = this.ERROR_CODES.NOT_FOUND;
          message = customMessage || this.ERROR_MESSAGES[code];
          break;
        case 409:
          code = this.ERROR_CODES.CONFLICT;
          message = this.extractErrorMessage(error) || customMessage || this.ERROR_MESSAGES[code];
          break;
        case 500:
          code = this.ERROR_CODES.INTERNAL_SERVER_ERROR;
          message = customMessage || this.ERROR_MESSAGES[code];
          break;
        case 503:
          code = this.ERROR_CODES.SERVICE_UNAVAILABLE;
          message = customMessage || this.ERROR_MESSAGES[code];
          break;
        default:
          message = this.extractErrorMessage(error) || customMessage || this.ERROR_MESSAGES['DEFAULT'];
      }
    }

    return {
      message,
      code,
      statusCode: error.status,
      errors,
      timestamp: new Date(),
      path: error.url || undefined
    };
  }

  /**
   * 從錯誤回應中提取錯誤訊息
   * @param error HTTP 錯誤
   * @returns 錯誤訊息
   */
  private static extractErrorMessage(error: HttpErrorResponse): string {
    if (error.error) {
      // 嘗試從不同的錯誤格式中提取訊息
      if (typeof error.error === 'string') {
        return error.error;
      }
      if (error.error.message) {
        return error.error.message;
      }
      if (error.error.Message) {
        return error.error.Message;
      }
      if (error.error.error) {
        return error.error.error;
      }
      if (error.error.errors && Array.isArray(error.error.errors) && error.error.errors.length > 0) {
        return error.error.errors.map((e: any) => e.message || e.Message || e).join(', ');
      }
    }
    return '';
  }

  /**
   * 從錯誤回應中提取驗證錯誤
   * @param error HTTP 錯誤
   * @returns 驗證錯誤陣列
   */
  private static extractValidationErrors(error: HttpErrorResponse): ErrorDetail[] {
    const errors: ErrorDetail[] = [];
    
    if (error.error && error.error.errors) {
      if (Array.isArray(error.error.errors)) {
        // 陣列格式的錯誤
        error.error.errors.forEach((e: any) => {
          errors.push({
            code: e.code || 'VALIDATION_ERROR',
            message: e.message || e.Message || String(e),
            field: e.field || e.Field || undefined
          });
        });
      } else if (typeof error.error.errors === 'object') {
        // 物件格式的錯誤（如 ASP.NET Core ModelState）
        Object.keys(error.error.errors).forEach(field => {
          const fieldErrors = error.error.errors[field];
          if (Array.isArray(fieldErrors)) {
            fieldErrors.forEach(msg => {
              errors.push({
                code: 'VALIDATION_ERROR',
                message: msg,
                field
              });
            });
          }
        });
      }
    }
    
    return errors;
  }

  /**
   * 處理一般錯誤
   * @param error 錯誤物件
   * @param customMessage 自定義錯誤訊息
   * @returns Observable 錯誤
   */
  static handleError(error: any, customMessage?: string): Observable<never> {
    const standardError: StandardError = {
      message: customMessage || error.message || this.ERROR_MESSAGES['DEFAULT'],
      code: error.code || this.ERROR_CODES.BUSINESS_ERROR,
      timestamp: new Date()
    };
    
    console.error('General Error:', standardError);
    return throwError(() => standardError);
  }

  /**
   * 檢查是否為特定錯誤代碼
   * @param error 錯誤物件
   * @param code 錯誤代碼
   * @returns 是否匹配
   */
  static isErrorCode(error: any, code: string): boolean {
    return error && error.code === code;
  }

  /**
   * 檢查是否為網路錯誤
   * @param error 錯誤物件
   * @returns 是否為網路錯誤
   */
  static isNetworkError(error: any): boolean {
    return this.isErrorCode(error, this.ERROR_CODES.NETWORK_ERROR) ||
           (error instanceof HttpErrorResponse && error.status === 0);
  }

  /**
   * 檢查是否為認證錯誤
   * @param error 錯誤物件
   * @returns 是否為認證錯誤
   */
  static isAuthError(error: any): boolean {
    return this.isErrorCode(error, this.ERROR_CODES.UNAUTHORIZED) ||
           (error instanceof HttpErrorResponse && error.status === 401);
  }

  /**
   * 檢查是否為權限錯誤
   * @param error 錯誤物件
   * @returns 是否為權限錯誤
   */
  static isForbiddenError(error: any): boolean {
    return this.isErrorCode(error, this.ERROR_CODES.FORBIDDEN) ||
           (error instanceof HttpErrorResponse && error.status === 403);
  }

  /**
   * 檢查是否為驗證錯誤
   * @param error 錯誤物件
   * @returns 是否為驗證錯誤
   */
  static isValidationError(error: any): boolean {
    return this.isErrorCode(error, this.ERROR_CODES.VALIDATION_ERROR) ||
           (error instanceof HttpErrorResponse && error.status === 400 && error.error?.errors);
  }

  /**
   * 取得使用者友善的錯誤訊息
   * @param error 錯誤物件
   * @returns 錯誤訊息
   */
  static getUserFriendlyMessage(error: any): string {
    if (error?.message) {
      return error.message;
    }
    
    if (error instanceof HttpErrorResponse) {
      const parsed = this.parseHttpError(error);
      return parsed.message;
    }
    
    return this.ERROR_MESSAGES['DEFAULT'];
  }

  /**
   * 格式化驗證錯誤為字串
   * @param errors 驗證錯誤陣列
   * @returns 格式化的錯誤訊息
   */
  static formatValidationErrors(errors: ErrorDetail[]): string {
    if (!errors || errors.length === 0) return '';
    
    return errors
      .map(e => e.field ? `${e.field}: ${e.message}` : e.message)
      .join('\n');
  }
}