// API 響應模型 - 統一管理所有 API 響應格式
// 設計理念：提供一致的 API 響應結構，便於前端處理

/**
 * 標準 API 響應介面
 */
export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data?: T;
  errors?: ApiError[];
  timestamp?: string;
  path?: string;
  requestId?: string;
}

/**
 * API 錯誤詳情
 */
export interface ApiError {
  code: string;
  message: string;
  field?: string;
  details?: any;
}

/**
 * 分頁 API 響應介面
 */
export interface PagedApiResponse<T> extends ApiResponse<T[]> {
  pagination: PaginationInfo;
}

/**
 * 分頁資訊
 */
export interface PaginationInfo {
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

/**
 * 列表查詢參數
 */
export interface ListQueryParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  search?: string;
  filters?: { [key: string]: any };
}

/**
 * 檔案上傳響應
 */
export interface FileUploadResponse extends ApiResponse<FileInfo> {
  isDuplicate?: boolean;
  filePath?: string;
}

/**
 * 檔案資訊
 */
export interface FileInfo {
  fileId: string;
  fileName: string;
  fileSize: number;
  fileType: string;
  uploadTime: string;
  status: string;
}

/**
 * 批次操作響應
 */
export interface BatchOperationResponse<T = any> extends ApiResponse<T> {
  successCount: number;
  failureCount: number;
  failedItems?: BatchFailedItem[];
}

/**
 * 批次操作失敗項目
 */
export interface BatchFailedItem {
  id: string | number;
  reason: string;
  data?: any;
}

/**
 * 操作結果
 */
export interface OperationResult {
  success: boolean;
  message: string;
  affectedCount?: number;
  warnings?: string[];
}

/**
 * 驗證結果
 */
export interface ValidationResult {
  isValid: boolean;
  errors: ValidationError[];
}

/**
 * 驗證錯誤
 */
export interface ValidationError {
  field: string;
  message: string;
  code?: string;
  value?: any;
}

/**
 * API 響應建構器
 */
export class ApiResponseBuilder {
  
  /**
   * 建立成功響應
   */
  static success<T>(data?: T, message: string = '操作成功'): ApiResponse<T> {
    return {
      success: true,
      message,
      data,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * 建立錯誤響應
   */
  static error(message: string, errors?: ApiError[]): ApiResponse {
    return {
      success: false,
      message,
      errors,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * 建立分頁響應
   */
  static paged<T>(
    data: T[],
    page: number,
    pageSize: number,
    totalItems: number
  ): PagedApiResponse<T> {
    const totalPages = Math.ceil(totalItems / pageSize);
    
    return {
      success: true,
      message: '查詢成功',
      data,
      pagination: {
        currentPage: page,
        pageSize,
        totalItems,
        totalPages,
        hasNext: page < totalPages,
        hasPrevious: page > 1
      },
      timestamp: new Date().toISOString()
    };
  }

  /**
   * 建立批次操作響應
   */
  static batch<T>(
    successCount: number,
    failureCount: number,
    failedItems?: BatchFailedItem[],
    data?: T
  ): BatchOperationResponse<T> {
    const message = failureCount === 0
      ? `成功處理 ${successCount} 項`
      : `成功: ${successCount} 項, 失敗: ${failureCount} 項`;

    return {
      success: failureCount === 0,
      message,
      data,
      successCount,
      failureCount,
      failedItems,
      timestamp: new Date().toISOString()
    };
  }

  /**
   * 建立驗證錯誤響應
   */
  static validationError(errors: ValidationError[]): ApiResponse {
    return {
      success: false,
      message: '資料驗證失敗',
      errors: errors.map(e => ({
        code: e.code || 'VALIDATION_ERROR',
        message: e.message,
        field: e.field
      })),
      timestamp: new Date().toISOString()
    };
  }
}

/**
 * API 響應處理工具
 */
export class ApiResponseUtils {
  
  /**
   * 檢查響應是否成功
   */
  static isSuccess(response: ApiResponse): boolean {
    return response && response.success === true;
  }

  /**
   * 提取響應數據
   */
  static getData<T>(response: ApiResponse<T>): T | undefined {
    return response.data;
  }

  /**
   * 提取錯誤訊息
   */
  static getErrorMessage(response: ApiResponse): string {
    if (response.errors && response.errors.length > 0) {
      return response.errors.map(e => e.message).join(', ');
    }
    return response.message || '未知錯誤';
  }

  /**
   * 檢查是否有驗證錯誤
   */
  static hasValidationErrors(response: ApiResponse): boolean {
    return response.errors?.some(e => e.code === 'VALIDATION_ERROR') || false;
  }

  /**
   * 提取特定欄位的錯誤
   */
  static getFieldError(response: ApiResponse, field: string): string | undefined {
    const error = response.errors?.find(e => e.field === field);
    return error?.message;
  }

  /**
   * 合併多個響應的錯誤
   */
  static mergeErrors(responses: ApiResponse[]): ApiError[] {
    const errors: ApiError[] = [];
    
    responses.forEach(response => {
      if (response.errors) {
        errors.push(...response.errors);
      } else if (!response.success) {
        errors.push({
          code: 'GENERAL_ERROR',
          message: response.message
        });
      }
    });
    
    return errors;
  }
}