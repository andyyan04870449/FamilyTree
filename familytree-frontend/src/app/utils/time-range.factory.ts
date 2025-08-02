import { Injectable } from '@angular/core';

export interface TimeRange {
  start: Date;
  end: Date;
  label: string;
  key: string;
}

export interface QuickTimeRangeOption {
  label: string;
  key: string;
  getValue: () => { start: Date; end: Date };
  description?: string;
}

export interface TimeFormatOptions {
  includeTime?: boolean;
  includeSeconds?: boolean;
  includeMilliseconds?: boolean;
  format?: 'date' | 'datetime-local' | 'iso' | 'timestamp';
  timezone?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TimeRangeFactory {

  /**
   * 預定義的快速時間選擇選項
   */
  private readonly predefinedRanges: QuickTimeRangeOption[] = [
    {
      label: '今天',
      key: 'today',
      description: '今日 00:00 到 23:59',
      getValue: () => this.createTodayRange()
    },
    {
      label: '昨天',
      key: 'yesterday',
      description: '昨日 00:00 到 23:59',
      getValue: () => this.createYesterdayRange()
    },
    {
      label: '最近7天',
      key: 'last7days',
      description: '過去7天的資料',
      getValue: () => this.createLastDaysRange(7)
    },
    {
      label: '最近30天',
      key: 'last30days',
      description: '過去30天的資料',
      getValue: () => this.createLastDaysRange(30)
    },
    {
      label: '最近90天',
      key: 'last90days',
      description: '過去90天的資料',
      getValue: () => this.createLastDaysRange(90)
    },
    {
      label: '本週',
      key: 'thisWeek',
      description: '本週一到今天',
      getValue: () => this.createThisWeekRange()
    },
    {
      label: '上週',
      key: 'lastWeek',
      description: '上週一到上週日',
      getValue: () => this.createLastWeekRange()
    },
    {
      label: '本月',
      key: 'thisMonth',
      description: '本月1號到今天',
      getValue: () => this.createThisMonthRange()
    },
    {
      label: '上月',
      key: 'lastMonth',
      description: '上月1號到上月最後一天',
      getValue: () => this.createLastMonthRange()
    }
  ];

  /**
   * 獲取所有預定義的快速時間選擇選項
   */
  getQuickTimeRanges(): QuickTimeRangeOption[] {
    return [...this.predefinedRanges];
  }

  /**
   * 獲取指定類型的快速時間選擇選項
   */
  getQuickTimeRangesByType(types: string[]): QuickTimeRangeOption[] {
    return this.predefinedRanges.filter(range => types.includes(range.key));
  }

  /**
   * 根據 key 獲取特定的時間範圍
   */
  getTimeRangeByKey(key: string): TimeRange | null {
    const option = this.predefinedRanges.find(range => range.key === key);
    if (!option) return null;

    const { start, end } = option.getValue();
    return {
      start,
      end,
      label: option.label,
      key: option.key
    };
  }

  /**
   * 創建自定義時間範圍
   */
  createCustomRange(start: Date, end: Date, label?: string): TimeRange {
    return {
      start: new Date(start),
      end: new Date(end),
      label: label || `${this.formatDate(start)} - ${this.formatDate(end)}`,
      key: 'custom'
    };
  }

  /**
   * 創建最近 N 天的時間範圍
   */
  createLastDaysRange(days: number): { start: Date; end: Date } {
    const end = new Date();
    const start = new Date(Date.now() - days * 24 * 60 * 60 * 1000);
    return { start, end };
  }

  /**
   * 創建最近 N 小時的時間範圍
   */
  createLastHoursRange(hours: number): { start: Date; end: Date } {
    const end = new Date();
    const start = new Date(Date.now() - hours * 60 * 60 * 1000);
    return { start, end };
  }

  /**
   * 格式化日期為表單輸入格式
   */
  formatDateForInput(date: Date, options: TimeFormatOptions = {}): string {
    const {
      format = 'datetime-local',
      includeTime = true,
      includeSeconds = false,
      includeMilliseconds = false
    } = options;

    switch (format) {
      case 'date':
        return this.formatDateOnly(date);
      case 'datetime-local':
        return this.formatDateTimeLocal(date, includeSeconds);
      case 'iso':
        return date.toISOString();
      case 'timestamp':
        return date.getTime().toString();
      default:
        return includeTime ? 
          this.formatDateTimeLocal(date, includeSeconds) : 
          this.formatDateOnly(date);
    }
  }

  /**
   * 解析字符串為日期
   */
  parseDate(dateString: string): Date | null {
    if (!dateString) return null;
    
    const date = new Date(dateString);
    return isNaN(date.getTime()) ? null : date;
  }

  /**
   * 檢查兩個時間範圍是否重疊
   */
  isRangeOverlapping(range1: TimeRange, range2: TimeRange): boolean {
    return range1.start <= range2.end && range2.start <= range1.end;
  }

  /**
   * 合併多個時間範圍
   */
  mergeTimeRanges(ranges: TimeRange[]): TimeRange[] {
    if (ranges.length <= 1) return ranges;

    // 按開始時間排序
    const sortedRanges = ranges.sort((a, b) => a.start.getTime() - b.start.getTime());
    const merged: TimeRange[] = [sortedRanges[0]];

    for (let i = 1; i < sortedRanges.length; i++) {
      const current = sortedRanges[i];
      const last = merged[merged.length - 1];

      if (this.isRangeOverlapping(last, current)) {
        // 合併重疊的範圍
        last.end = new Date(Math.max(last.end.getTime(), current.end.getTime()));
        last.label = `${this.formatDate(last.start)} - ${this.formatDate(last.end)}`;
      } else {
        merged.push(current);
      }
    }

    return merged;
  }

  /**
   * 獲取時間範圍的持續時間（毫秒）
   */
  getRangeDuration(range: TimeRange): number {
    return range.end.getTime() - range.start.getTime();
  }

  /**
   * 獲取時間範圍的持續時間描述
   */
  getRangeDurationDescription(range: TimeRange): string {
    const duration = this.getRangeDuration(range);
    const days = Math.floor(duration / (24 * 60 * 60 * 1000));
    const hours = Math.floor((duration % (24 * 60 * 60 * 1000)) / (60 * 60 * 1000));
    const minutes = Math.floor((duration % (60 * 60 * 1000)) / (60 * 1000));

    if (days > 0) {
      return `${days} 天${hours > 0 ? ` ${hours} 小時` : ''}`;
    } else if (hours > 0) {
      return `${hours} 小時${minutes > 0 ? ` ${minutes} 分鐘` : ''}`;
    } else {
      return `${minutes} 分鐘`;
    }
  }

  /**
   * 驗證時間範圍是否有效
   */
  validateTimeRange(range: Partial<TimeRange>): { valid: boolean; errors: string[] } {
    const errors: string[] = [];

    if (!range.start) {
      errors.push('開始時間不能為空');
    }

    if (!range.end) {
      errors.push('結束時間不能為空');
    }

    if (range.start && range.end && range.start >= range.end) {
      errors.push('開始時間必須早於結束時間');
    }

    if (range.start && range.start > new Date()) {
      errors.push('開始時間不能是未來時間');
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  // 私有輔助方法

  private createTodayRange(): { start: Date; end: Date } {
    const today = new Date();
    const start = new Date(today.setHours(0, 0, 0, 0));
    const end = new Date(today.setHours(23, 59, 59, 999));
    return { start, end };
  }

  private createYesterdayRange(): { start: Date; end: Date } {
    const yesterday = new Date(Date.now() - 24 * 60 * 60 * 1000);
    const start = new Date(yesterday.setHours(0, 0, 0, 0));
    const end = new Date(yesterday.setHours(23, 59, 59, 999));
    return { start, end };
  }

  private createThisWeekRange(): { start: Date; end: Date } {
    const today = new Date();
    const dayOfWeek = today.getDay();
    const monday = new Date(today.getTime() - (dayOfWeek - 1) * 24 * 60 * 60 * 1000);
    const start = new Date(monday.setHours(0, 0, 0, 0));
    const end = new Date();
    return { start, end };
  }

  private createLastWeekRange(): { start: Date; end: Date } {
    const today = new Date();
    const dayOfWeek = today.getDay();
    const lastMonday = new Date(today.getTime() - (dayOfWeek + 6) * 24 * 60 * 60 * 1000);
    const lastSunday = new Date(lastMonday.getTime() + 6 * 24 * 60 * 60 * 1000);
    
    const start = new Date(lastMonday.setHours(0, 0, 0, 0));
    const end = new Date(lastSunday.setHours(23, 59, 59, 999));
    return { start, end };
  }

  private createThisMonthRange(): { start: Date; end: Date } {
    const today = new Date();
    const start = new Date(today.getFullYear(), today.getMonth(), 1, 0, 0, 0, 0);
    const end = new Date();
    return { start, end };
  }

  private createLastMonthRange(): { start: Date; end: Date } {
    const today = new Date();
    const start = new Date(today.getFullYear(), today.getMonth() - 1, 1, 0, 0, 0, 0);
    const end = new Date(today.getFullYear(), today.getMonth(), 0, 23, 59, 59, 999);
    return { start, end };
  }

  private formatDateOnly(date: Date): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private formatDateTimeLocal(date: Date, includeSeconds: boolean = false): string {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    
    let result = `${year}-${month}-${day}T${hours}:${minutes}`;
    
    if (includeSeconds) {
      const seconds = date.getSeconds().toString().padStart(2, '0');
      result += `:${seconds}`;
    }
    
    return result;
  }

  private formatDate(date: Date): string {
    return date.toLocaleDateString('zh-TW');
  }
}