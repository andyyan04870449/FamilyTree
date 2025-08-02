import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingComponent } from '../ui/loading/loading.component';
import { ButtonComponent } from '../ui/button/button.component';

export type DisplayState = 'loading' | 'error' | 'empty' | 'success';

export interface StateConfig {
  loading?: {
    message?: string;
    variant?: 'spinner' | 'dots' | 'pulse';
    size?: 'sm' | 'md' | 'lg';
  };
  error?: {
    title?: string;
    message?: string;
    icon?: string;
    showRetry?: boolean;
    retryLabel?: string;
  };
  empty?: {
    title?: string;
    message?: string;
    icon?: string;
    showAction?: boolean;
    actionLabel?: string;
  };
}

@Component({
  selector: 'app-state-display',
  standalone: true,
  imports: [CommonModule, LoadingComponent, ButtonComponent],
  template: `
    <!-- Loading State -->
    <div 
      *ngIf="state === 'loading'" 
      class="bg-white rounded-lg shadow-sm border border-gray-200 p-12"
    >
      <app-loading 
        [variant]="config.loading?.variant || 'spinner'"
        [size]="config.loading?.size || 'lg'"
        [message]="config.loading?.message || '載入中...'"
      ></app-loading>
    </div>

    <!-- Error State -->
    <div 
      *ngIf="state === 'error'" 
      class="bg-white rounded-lg shadow-sm border border-gray-200 p-8"
    >
      <div class="text-center">
        <div class="text-red-400 text-6xl mb-4">
          {{ config.error?.icon || '❌' }}
        </div>
        <h3 class="text-lg font-medium text-gray-900 mb-2">
          {{ config.error?.title || '載入失敗' }}
        </h3>
        <p class="text-gray-600 mb-4">
          {{ config.error?.message || '發生未知錯誤，請稍後再試' }}
        </p>
        <app-button
          *ngIf="config.error?.showRetry !== false"
          variant="primary"
          [label]="config.error?.retryLabel || '重試'"
          (clicked)="onAction('retry')"
        ></app-button>
      </div>
    </div>

    <!-- Empty State -->
    <div 
      *ngIf="state === 'empty'" 
      class="bg-white rounded-lg shadow-sm border border-gray-200 p-8"
    >
      <div class="text-center">
        <div class="text-gray-400 text-6xl mb-4">
          {{ config.empty?.icon || '📭' }}
        </div>
        <h3 class="text-lg font-medium text-gray-900 mb-2">
          {{ config.empty?.title || '沒有資料' }}
        </h3>
        <p class="text-gray-600 mb-4">
          {{ config.empty?.message || '目前沒有符合條件的資料' }}
        </p>
        <app-button
          *ngIf="config.empty?.showAction"
          variant="primary"
          [label]="config.empty?.actionLabel || '新增'"
          (clicked)="onAction('add')"
        ></app-button>
      </div>
    </div>
  `
})
export class StateDisplayComponent {
  @Input() state: DisplayState = 'success';
  @Input() config: StateConfig = {};
  
  @Output() action = new EventEmitter<'retry' | 'add'>();

  onAction(actionType: 'retry' | 'add'): void {
    this.action.emit(actionType);
  }
}