// 通用工具類 - 統一管理常用的格式化和轉換方法
// 設計理念：消除重複代碼，提供一致的格式化體驗

export class CommonUtils {
  
  /**
   * 格式化檔案大小
   * @param bytes 檔案大小（位元組）
   * @returns 格式化後的檔案大小字串
   */
  static formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  /**
   * 格式化日期（年-月-日）
   * @param dateString 日期字串
   * @returns 格式化後的日期
   */
  static formatDate(dateString: string | Date | undefined): string {
    if (!dateString) return '';
    
    try {
      const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
      if (isNaN(date.getTime())) return '';
      
      return date.toLocaleDateString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
      });
    } catch {
      return '';
    }
  }

  /**
   * 格式化日期時間（年-月-日 時:分）
   * @param dateString 日期時間字串
   * @returns 格式化後的日期時間
   */
  static formatDateTime(dateString: string | Date | undefined): string {
    if (!dateString) return '';
    
    try {
      const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
      if (isNaN(date.getTime())) return '';
      
      return date.toLocaleString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return '';
    }
  }

  /**
   * 格式化日期時間（含秒）
   * @param dateString 日期時間字串
   * @returns 格式化後的日期時間
   */
  static formatDateTimeFull(dateString: string | Date | undefined): string {
    if (!dateString) return '';
    
    try {
      const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
      if (isNaN(date.getTime())) return '';
      
      return date.toLocaleString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
      });
    } catch {
      return '';
    }
  }

  /**
   * 格式化相對時間（例如：3 分鐘前）
   * @param dateString 日期時間字串
   * @returns 相對時間描述
   */
  static formatRelativeTime(dateString: string | Date | undefined): string {
    if (!dateString) return '';
    
    try {
      const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
      const now = new Date();
      const diffMs = now.getTime() - date.getTime();
      const diffSec = Math.floor(diffMs / 1000);
      const diffMin = Math.floor(diffSec / 60);
      const diffHour = Math.floor(diffMin / 60);
      const diffDay = Math.floor(diffHour / 24);

      if (diffSec < 60) return '剛剛';
      if (diffMin < 60) return `${diffMin} 分鐘前`;
      if (diffHour < 24) return `${diffHour} 小時前`;
      if (diffDay < 7) return `${diffDay} 天前`;
      
      return CommonUtils.formatDate(date);
    } catch {
      return '';
    }
  }

  /**
   * 格式化日期為 ISO 格式（用於 API 請求）
   * @param date 日期
   * @returns ISO 格式日期字串
   */
  static formatDateForAPI(date: Date | string): string {
    try {
      const d = typeof date === 'string' ? new Date(date) : date;
      return d.toISOString();
    } catch {
      return '';
    }
  }

  /**
   * 格式化日期為 HTML input 元素格式
   * @param dateString 日期字串
   * @returns YYYY-MM-DD 格式
   */
  static formatDateForInput(dateString: string | undefined): string {
    if (!dateString) return '';
    
    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime())) return '';
      
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      
      return `${year}-${month}-${day}`;
    } catch {
      return '';
    }
  }

  /**
   * 格式化日期時間為 HTML datetime-local 元素格式
   * @param dateString 日期時間字串
   * @returns YYYY-MM-DDTHH:mm 格式
   */
  static formatDateTimeForInput(dateString: string | Date | undefined): string {
    if (!dateString) return '';
    
    try {
      const date = typeof dateString === 'string' ? new Date(dateString) : dateString;
      if (isNaN(date.getTime())) return '';
      
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      const hours = String(date.getHours()).padStart(2, '0');
      const minutes = String(date.getMinutes()).padStart(2, '0');
      
      return `${year}-${month}-${day}T${hours}:${minutes}`;
    } catch {
      return '';
    }
  }

  /**
   * 截斷文字並加省略號
   * @param text 原始文字
   * @param maxLength 最大長度
   * @returns 截斷後的文字
   */
  static truncateText(text: string, maxLength: number): string {
    if (!text || text.length <= maxLength) return text || '';
    return text.substring(0, maxLength) + '...';
  }

  /**
   * 深度複製物件
   * @param obj 原始物件
   * @returns 複製後的物件
   */
  static deepClone<T>(obj: T): T {
    return JSON.parse(JSON.stringify(obj));
  }

  /**
   * 延遲執行
   * @param ms 延遲毫秒數
   * @returns Promise
   */
  static delay(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * 防抖函數
   * @param func 要執行的函數
   * @param wait 等待時間（毫秒）
   * @returns 防抖後的函數
   */
  static debounce<T extends (...args: any[]) => any>(
    func: T,
    wait: number
  ): (...args: Parameters<T>) => void {
    let timeout: ReturnType<typeof setTimeout> | null = null;
    
    return function(...args: Parameters<T>) {
      if (timeout) clearTimeout(timeout);
      timeout = setTimeout(() => func(...args), wait);
    };
  }

  /**
   * 節流函數
   * @param func 要執行的函數
   * @param limit 時間限制（毫秒）
   * @returns 節流後的函數
   */
  static throttle<T extends (...args: any[]) => any>(
    func: T,
    limit: number
  ): (...args: Parameters<T>) => void {
    let inThrottle = false;
    
    return function(...args: Parameters<T>) {
      if (!inThrottle) {
        func(...args);
        inThrottle = true;
        setTimeout(() => inThrottle = false, limit);
      }
    };
  }

  /**
   * 生成 UUID
   * @returns UUID 字串
   */
  static generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  /**
   * 檢查是否為空值
   * @param value 要檢查的值
   * @returns 是否為空
   */
  static isEmpty(value: any): boolean {
    return value === null || 
           value === undefined || 
           value === '' || 
           (Array.isArray(value) && value.length === 0) ||
           (typeof value === 'object' && Object.keys(value).length === 0);
  }

  /**
   * 安全地取得巢狀物件屬性
   * @param obj 物件
   * @param path 屬性路徑（例如：'user.profile.name'）
   * @param defaultValue 預設值
   * @returns 屬性值或預設值
   */
  static getNestedProperty(obj: any, path: string, defaultValue: any = null): any {
    const keys = path.split('.');
    let result = obj;
    
    for (const key of keys) {
      if (result && typeof result === 'object' && key in result) {
        result = result[key];
      } else {
        return defaultValue;
      }
    }
    
    return result;
  }

  /**
   * 比較兩個版本號
   * @param v1 版本1
   * @param v2 版本2
   * @returns 1: v1 > v2, -1: v1 < v2, 0: v1 = v2
   */
  static compareVersions(v1: string, v2: string): number {
    const parts1 = v1.split('.').map(Number);
    const parts2 = v2.split('.').map(Number);
    
    for (let i = 0; i < Math.max(parts1.length, parts2.length); i++) {
      const part1 = parts1[i] || 0;
      const part2 = parts2[i] || 0;
      
      if (part1 > part2) return 1;
      if (part1 < part2) return -1;
    }
    
    return 0;
  }
}