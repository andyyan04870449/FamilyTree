import { Injectable } from '@angular/core';
import { TimeRangeFactory, TimeRange } from '../utils/time-range.factory';

export interface DateFormatPresets {
  [key: string]: Intl.DateTimeFormatOptions;
}

export interface TimeZoneConfig {
  timeZone: string;
  label: string;
  offset: string;
}

@Injectable({
  providedIn: 'root'
})
export class TimeUtilsService {

  /**
   * 預定義的日期格式
   */
  private readonly dateFormatPresets: DateFormatPresets = {
    short: {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    },
    medium: {
      year: 'numeric',
      month: 'short',
      day: '2-digit'
    },
    long: {
      year: 'numeric',
      month: 'long',
      day: '2-digit',
      weekday: 'long'
    },
    time: {
      hour: '2-digit',
      minute: '2-digit'
    },
    datetime: {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    },
    full: {
      year: 'numeric',
      month: 'long',
      day: '2-digit',
      weekday: 'long',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    }
  };

  /**
   * 支援的時區配置
   */
  private readonly timeZones: TimeZoneConfig[] = [
    { timeZone: 'Asia/Taipei', label: '台北時間', offset: 'UTC+8' },
    { timeZone: 'UTC', label: '協調世界時', offset: 'UTC+0' },
    { timeZone: 'America/New_York', label: '紐約時間', offset: 'UTC-5/-4' },
    { timeZone: 'Europe/London', label: '倫敦時間', offset: 'UTC+0/+1' },
    { timeZone: 'Asia/Tokyo', label: '東京時間', offset: 'UTC+9' }
  ];

  constructor(private timeRangeFactory: TimeRangeFactory) {}

  /**
   * 格式化日期時間
   */
  formatDateTime(
    date: Date | string | number,
    format: keyof DateFormatPresets | Intl.DateTimeFormatOptions = 'datetime',
    locale: string = 'zh-TW',
    timeZone?: string
  ): string {
    const dateObj = this.parseDate(date);
    if (!dateObj) return '';

    const options = typeof format === 'string' 
      ? this.dateFormatPresets[format] || this.dateFormatPresets.datetime
      : format;

    if (timeZone) {
      options.timeZone = timeZone;
    }

    try {
      return new Intl.DateTimeFormat(locale, options).format(dateObj);
    } catch (error) {
      console.warn('Date formatting failed, falling back to default:', error);
      return dateObj.toLocaleString(locale);
    }
  }

  /**
   * 格式化相對時間（例如：2小時前）
   */
  formatRelativeTime(
    date: Date | string | number,
    locale: string = 'zh-TW'
  ): string {
    const dateObj = this.parseDate(date);
    if (!dateObj) return '';

    const now = new Date();
    const diffMs = now.getTime() - dateObj.getTime();
    
    // 如果是未來時間
    if (diffMs < 0) {
      const future = Math.abs(diffMs);
      return this.formatFutureTime(future, locale);
    }

    return this.formatPastTime(diffMs, locale);
  }

  /**
   * 計算兩個日期之間的差異
   */
  getDateDifference(
    startDate: Date | string | number,
    endDate: Date | string | number
  ): {
    milliseconds: number;
    seconds: number;
    minutes: number;
    hours: number;
    days: number;
    weeks: number;
    months: number;
    years: number;
  } {
    const start = this.parseDate(startDate);
    const end = this.parseDate(endDate);
    
    if (!start || !end) {
      throw new Error('Invalid date provided');
    }

    const diffMs = Math.abs(end.getTime() - start.getTime());
    
    return {
      milliseconds: diffMs,
      seconds: Math.floor(diffMs / 1000),
      minutes: Math.floor(diffMs / (1000 * 60)),
      hours: Math.floor(diffMs / (1000 * 60 * 60)),
      days: Math.floor(diffMs / (1000 * 60 * 60 * 24)),
      weeks: Math.floor(diffMs / (1000 * 60 * 60 * 24 * 7)),
      months: Math.floor(diffMs / (1000 * 60 * 60 * 24 * 30.44)), // 平均月份
      years: Math.floor(diffMs / (1000 * 60 * 60 * 24 * 365.25)) // 平均年份
    };
  }

  /**
   * 驗證日期是否在指定範圍內
   */
  isDateInRange(
    date: Date | string | number,
    startDate: Date | string | number,
    endDate: Date | string | number
  ): boolean {
    const targetDate = this.parseDate(date);
    const start = this.parseDate(startDate);
    const end = this.parseDate(endDate);

    if (!targetDate || !start || !end) {
      return false;
    }

    return targetDate >= start && targetDate <= end;
  }

  /**
   * 獲取日期的開始和結束時間（當天的00:00:00和23:59:59）
   */
  getDayBounds(date: Date | string | number): { start: Date; end: Date } {
    const dateObj = this.parseDate(date);
    if (!dateObj) {
      throw new Error('Invalid date provided');
    }

    const start = new Date(dateObj);
    start.setHours(0, 0, 0, 0);

    const end = new Date(dateObj);
    end.setHours(23, 59, 59, 999);

    return { start, end };
  }

  /**
   * 轉換時區
   */
  convertTimeZone(
    date: Date | string | number,
    fromTimeZone: string,
    toTimeZone: string
  ): Date {
    const dateObj = this.parseDate(date);
    if (!dateObj) {
      throw new Error('Invalid date provided');
    }

    // 使用 Intl.DateTimeFormat 來處理時區轉換
    const utcTime = dateObj.getTime() + (dateObj.getTimezoneOffset() * 60000);
    return new Date(utcTime);
  }

  /**
   * 檢查是否為工作日
   */
  isWorkingDay(date: Date | string | number): boolean {
    const dateObj = this.parseDate(date);
    if (!dateObj) return false;

    const dayOfWeek = dateObj.getDay();
    return dayOfWeek >= 1 && dayOfWeek <= 5; // 週一到週五
  }

  /**
   * 獲取下一個工作日
   */
  getNextWorkingDay(date: Date | string | number): Date {
    const dateObj = this.parseDate(date);
    if (!dateObj) {
      throw new Error('Invalid date provided');
    }

    let nextDay = new Date(dateObj);
    do {
      nextDay.setDate(nextDay.getDate() + 1);
    } while (!this.isWorkingDay(nextDay));

    return nextDay;
  }

  /**
   * 獲取可用的時區列表
   */
  getAvailableTimeZones(): TimeZoneConfig[] {
    return [...this.timeZones];
  }

  /**
   * 獲取當前瀏覽器時區
   */
  getBrowserTimeZone(): string {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
  }

  /**
   * 創建時間範圍的人性化描述
   */
  describeDateRange(
    startDate: Date | string | number,
    endDate: Date | string | number,
    locale: string = 'zh-TW'
  ): string {
    const start = this.parseDate(startDate);
    const end = this.parseDate(endDate);

    if (!start || !end) {
      return '無效的日期範圍';
    }

    const diff = this.getDateDifference(start, end);
    
    if (diff.days === 0) {
      return `同一天 (${this.formatDateTime(start, 'short', locale)})`;
    } else if (diff.days === 1) {
      return `連續兩天 (${this.formatDateTime(start, 'short', locale)} - ${this.formatDateTime(end, 'short', locale)})`;
    } else if (diff.days <= 7) {
      return `${diff.days} 天 (${this.formatDateTime(start, 'short', locale)} - ${this.formatDateTime(end, 'short', locale)})`;
    } else if (diff.weeks <= 4) {
      return `${diff.weeks} 週 (${this.formatDateTime(start, 'short', locale)} - ${this.formatDateTime(end, 'short', locale)})`;
    } else if (diff.months <= 12) {
      return `${diff.months} 個月 (${this.formatDateTime(start, 'medium', locale)} - ${this.formatDateTime(end, 'medium', locale)})`;
    } else {
      return `${diff.years} 年 (${this.formatDateTime(start, 'medium', locale)} - ${this.formatDateTime(end, 'medium', locale)})`;
    }
  }

  /**
   * 解析各種格式的日期
   */
  parseDate(date: Date | string | number): Date | null {
    if (!date) return null;

    let dateObj: Date;

    if (date instanceof Date) {
      dateObj = new Date(date);
    } else if (typeof date === 'number') {
      dateObj = new Date(date);
    } else if (typeof date === 'string') {
      // 嘗試解析 ISO 字符串或其他常見格式
      dateObj = new Date(date);
      
      // 如果解析失敗，嘗試其他格式
      if (isNaN(dateObj.getTime())) {
        // 嘗試解析 yyyy-MM-dd 格式
        const isoMatch = date.match(/^(\d{4})-(\d{2})-(\d{2})$/);
        if (isoMatch) {
          dateObj = new Date(
            parseInt(isoMatch[1]),
            parseInt(isoMatch[2]) - 1,
            parseInt(isoMatch[3])
          );
        }
      }
    } else {
      return null;
    }

    return isNaN(dateObj.getTime()) ? null : dateObj;
  }

  // 私有輔助方法

  private formatPastTime(diffMs: number, locale: string): string {
    const seconds = Math.floor(diffMs / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    const weeks = Math.floor(days / 7);
    const months = Math.floor(days / 30.44);
    const years = Math.floor(days / 365.25);

    if (years > 0) return `${years} 年前`;
    if (months > 0) return `${months} 個月前`;
    if (weeks > 0) return `${weeks} 週前`;
    if (days > 0) return `${days} 天前`;
    if (hours > 0) return `${hours} 小時前`;
    if (minutes > 0) return `${minutes} 分鐘前`;
    return '剛剛';
  }

  private formatFutureTime(diffMs: number, locale: string): string {
    const seconds = Math.floor(diffMs / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);
    const weeks = Math.floor(days / 7);
    const months = Math.floor(days / 30.44);
    const years = Math.floor(days / 365.25);

    if (years > 0) return `${years} 年後`;
    if (months > 0) return `${months} 個月後`;
    if (weeks > 0) return `${weeks} 週後`;
    if (days > 0) return `${days} 天後`;
    if (hours > 0) return `${hours} 小時後`;
    if (minutes > 0) return `${minutes} 分鐘後`;
    return '即將';
  }
}