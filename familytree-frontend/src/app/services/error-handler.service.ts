import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { ToastController } from '@ionic/angular';

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  constructor(private toastController: ToastController) {}

  handleError(operation: string, context?: string) {
    return (error: HttpErrorResponse): Observable<never> => {
      const errorMessage = this.getErrorMessage(error, operation);
      this.logError(errorMessage, context);
      this.showUserError(errorMessage);
      return throwError(() => new Error(errorMessage));
    };
  }

  private getErrorMessage(error: HttpErrorResponse, operation: string): string {
    if (error.status === 0) {
      return '網路連線錯誤，請檢查網路設定';
    }
    
    if (error.status === 401) {
      return '登入已過期，請重新登入';
    }
    
    if (error.status === 403) {
      return '您沒有權限執行此操作';
    }
    
    if (error.status === 404) {
      return '找不到請求的資源';
    }
    
    if (error.status >= 500) {
      return '伺服器錯誤，請稍後再試';
    }
    
    return error.error?.message || `${operation}失敗`;
  }

  private logError(message: string, context?: string): void {
    const timestamp = new Date().toISOString();
    console.error(`[${timestamp}] [${context || 'Unknown'}] ${message}`);
  }

  private async showUserError(message: string): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 3000,
      color: 'danger',
      position: 'top'
    });
    await toast.present();
  }

  async showSuccess(message: string): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 2000,
      color: 'success',
      position: 'top'
    });
    await toast.present();
  }

  async showInfo(message: string): Promise<void> {
    const toast = await this.toastController.create({
      message,
      duration: 2000,
      color: 'primary',
      position: 'top'
    });
    await toast.present();
  }
}