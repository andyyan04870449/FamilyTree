import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, Toast } from '../../services/toast.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="toast-container">
      <div 
        *ngFor="let toast of toasts" 
        class="toast"
        [class.toast-success]="toast.type === 'success'"
        [class.toast-error]="toast.type === 'error'"
        [class.toast-warning]="toast.type === 'warning'"
        [class.toast-info]="toast.type === 'info'"
      >
        <div class="toast-icon">
          <span *ngIf="toast.type === 'success'">✓</span>
          <span *ngIf="toast.type === 'error'">✕</span>
          <span *ngIf="toast.type === 'warning'">!</span>
          <span *ngIf="toast.type === 'info'">i</span>
        </div>
        <div class="toast-message">{{ toast.message }}</div>
        <button class="toast-close" (click)="removeToast(toast.id!)">✕</button>
      </div>
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 20px;
      right: 20px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 10px;
      max-width: 400px;
    }

    .toast {
      display: flex;
      align-items: center;
      padding: 16px;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      background: white;
      animation: slideIn 0.3s ease-out;
      position: relative;
    }

    @keyframes slideIn {
      from {
        transform: translateX(100%);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    .toast-icon {
      width: 24px;
      height: 24px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-right: 12px;
      flex-shrink: 0;
      font-weight: bold;
      color: white;
    }

    .toast-message {
      flex: 1;
      color: #333;
      font-size: 14px;
      line-height: 1.4;
    }

    .toast-close {
      background: none;
      border: none;
      font-size: 18px;
      color: #666;
      cursor: pointer;
      padding: 0;
      margin-left: 12px;
      opacity: 0.6;
      transition: opacity 0.2s;
    }

    .toast-close:hover {
      opacity: 1;
    }

    .toast-success .toast-icon {
      background: #4caf50;
    }

    .toast-error .toast-icon {
      background: #f44336;
    }

    .toast-warning .toast-icon {
      background: #ff9800;
    }

    .toast-info .toast-icon {
      background: #2196f3;
    }

    .toast-success {
      border-left: 4px solid #4caf50;
    }

    .toast-error {
      border-left: 4px solid #f44336;
    }

    .toast-warning {
      border-left: 4px solid #ff9800;
    }

    .toast-info {
      border-left: 4px solid #2196f3;
    }
  `]
})
export class ToastComponent implements OnInit, OnDestroy {
  toasts: Toast[] = [];
  private destroy$ = new Subject<void>();
  private toastTimers = new Map<string, any>();

  constructor(private toastService: ToastService) {}

  ngOnInit(): void {
    this.toastService.toast$
      .pipe(takeUntil(this.destroy$))
      .subscribe(toast => {
        this.addToast(toast);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.toastTimers.forEach(timer => clearTimeout(timer));
  }

  private addToast(toast: Toast): void {
    this.toasts.push(toast);
    
    if (toast.duration && toast.id) {
      const timer = setTimeout(() => {
        this.removeToast(toast.id!);
      }, toast.duration);
      
      this.toastTimers.set(toast.id, timer);
    }
  }

  removeToast(id: string): void {
    const index = this.toasts.findIndex(t => t.id === id);
    if (index > -1) {
      this.toasts.splice(index, 1);
      
      if (this.toastTimers.has(id)) {
        clearTimeout(this.toastTimers.get(id));
        this.toastTimers.delete(id);
      }
    }
  }
}