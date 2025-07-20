// 前端日誌服務：提供統一的日誌記錄和調試功能
// 主要功能：日誌分級、時間戳、組件標識、錯誤追蹤

import { Injectable } from '@angular/core';

export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3
}

@Injectable({
  providedIn: 'root'
})
export class LogService {
  private readonly LOG_PREFIX = '🔍 [FamilyTree]';
  private readonly MAX_LOG_LENGTH = 1000; // 最大日誌長度
  private logHistory: string[] = [];

  constructor() {
    console.log(`${this.LOG_PREFIX} 日誌服務已初始化`);
  }

  /**
   * 調試日誌
   */
  debug(component: string, message: string, data?: any): void {
    this.log(LogLevel.DEBUG, component, message, data);
  }

  /**
   * 信息日誌
   */
  info(component: string, message: string, data?: any): void {
    this.log(LogLevel.INFO, component, message, data);
  }

  /**
   * 警告日誌
   */
  warn(component: string, message: string, data?: any): void {
    this.log(LogLevel.WARN, component, message, data);
  }

  /**
   * 錯誤日誌
   */
  error(component: string, message: string, error?: any): void {
    this.log(LogLevel.ERROR, component, message, error);
  }

  /**
   * 核心日誌方法
   */
  private log(level: LogLevel, component: string, message: string, data?: any): void {
    const timestamp = new Date().toISOString();
    const levelEmoji = this.getLevelEmoji(level);
    const levelText = LogLevel[level];
    
    const logMessage = `${this.LOG_PREFIX} ${levelEmoji} [${levelText}] [${component}] ${message}`;
    
    // 添加到歷史記錄
    this.addToHistory(logMessage);
    
    // 根據級別輸出到控制台
    switch (level) {
      case LogLevel.DEBUG:
        console.debug(logMessage, data);
        break;
      case LogLevel.INFO:
        console.info(logMessage, data);
        break;
      case LogLevel.WARN:
        console.warn(logMessage, data);
        break;
      case LogLevel.ERROR:
        console.error(logMessage, data);
        break;
    }
  }

  /**
   * 獲取級別對應的 emoji
   */
  private getLevelEmoji(level: LogLevel): string {
    switch (level) {
      case LogLevel.DEBUG: return '🔍';
      case LogLevel.INFO: return 'ℹ️';
      case LogLevel.WARN: return '⚠️';
      case LogLevel.ERROR: return '❌';
      default: return '📝';
    }
  }

  /**
   * 添加到歷史記錄
   */
  private addToHistory(message: string): void {
    this.logHistory.push(message);
    if (this.logHistory.length > this.MAX_LOG_LENGTH) {
      this.logHistory = this.logHistory.slice(-this.MAX_LOG_LENGTH);
    }
  }

  /**
   * 獲取日誌歷史
   */
  getLogHistory(): string[] {
    return [...this.logHistory];
  }

  /**
   * 清空日誌歷史
   */
  clearHistory(): void {
    this.logHistory = [];
  }

  /**
   * 導出日誌
   */
  exportLogs(): string {
    return this.logHistory.join('\n');
  }

  /**
   * 性能監控
   */
  time(label: string): void {
    console.time(`${this.LOG_PREFIX} ⏱️ ${label}`);
  }

  timeEnd(label: string): void {
    console.timeEnd(`${this.LOG_PREFIX} ⏱️ ${label}`);
  }
} 