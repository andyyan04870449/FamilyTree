import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '../ui/button/button.component';

export interface HeaderAction {
  id: string;
  label: string;
  icon?: string;
  variant?: 'primary' | 'secondary' | 'danger' | 'view' | 'favorite';
  size?: 'sm' | 'md' | 'lg';
  route?: string;
  action?: string;
  disabled?: boolean;
}

export interface PageHeaderConfig {
  title: string;
  subtitle?: string;
  icon?: string;
  actions?: HeaderAction[];
}

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonComponent],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
        <!-- 標題區域 -->
        <div>
          <h1 class="text-2xl font-bold text-gray-900 flex items-center gap-3">
            <div 
              *ngIf="config.icon" 
              class="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center"
            >
              <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path 
                  stroke-linecap="round" 
                  stroke-linejoin="round" 
                  stroke-width="2" 
                  [attr.d]="config.icon"
                />
              </svg>
            </div>
            {{ config.title }}
          </h1>
          <p 
            *ngIf="config.subtitle" 
            class="text-gray-600 mt-1"
          >
            {{ config.subtitle }}
          </p>
        </div>
        
        <!-- 操作按鈕區域 -->
        <div class="flex flex-wrap gap-2" *ngIf="config.actions && config.actions.length > 0">
          <ng-container *ngFor="let action of config.actions; trackBy: trackByActionId">
            <!-- 路由按鈕 -->
            <app-button
              *ngIf="action.route"
              [variant]="action.variant || 'secondary'"
              [size]="action.size || 'sm'"
              [label]="action.label"
              [icon]="action.icon"
              [disabled]="!!action.disabled"
              [routerLink]="action.route"
            ></app-button>
            
            <!-- 動作按鈕 -->
            <app-button
              *ngIf="action.action"
              [variant]="action.variant || 'secondary'"
              [size]="action.size || 'sm'"
              [label]="action.label"
              [icon]="action.icon"
              [disabled]="!!action.disabled"
              (clicked)="onAction(action)"
            ></app-button>
          </ng-container>
        </div>
      </div>
    </div>
  `
})
export class PageHeaderComponent {
  @Input() config: PageHeaderConfig = {
    title: '',
    subtitle: '',
    actions: []
  };

  @Output() actionClick = new EventEmitter<HeaderAction>();

  onAction(action: HeaderAction): void {
    this.actionClick.emit(action);
  }

  trackByActionId(index: number, action: HeaderAction): string {
    return action.id;
  }
}