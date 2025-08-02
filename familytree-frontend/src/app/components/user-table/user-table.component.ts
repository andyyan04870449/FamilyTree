import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from '../ui/button/button.component';
import { UserModel } from '../../services/user.service';
import { cn } from '../../utils/cn';
import { 
  CSS_CLASSES, 
  USER_STATUS_DISPLAY 
} from '../../constants/user-management.constants';

export interface UserAction {
  type: 'viewPermissions' | 'edit' | 'resetPassword' | 'toggleStatus';
  user: UserModel;
}

export interface UserTableColumn {
  key: string;
  label: string;
  width?: string;
  sortable?: boolean;
}

@Component({
  selector: 'app-user-table',
  standalone: true,
  imports: [CommonModule, ButtonComponent],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200">
      <!-- Table Header -->
      <div class="p-6 border-b border-gray-200">
        <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
          👥 帳號列表
          <span class="bg-gray-100 text-gray-600 text-sm px-2 py-1 rounded-full">{{ totalItems }}</span>
        </h2>
      </div>
      
      <!-- Table Content -->
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th 
                *ngFor="let column of columns" 
                [class]="getHeaderClass(column)"
              >
                {{ column.label }}
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr 
              *ngFor="let user of users; let i = index" 
              class="hover:bg-gray-50"
            >
              <!-- 項次 -->
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {{ getRowNumber(i) }}
              </td>
              
              <!-- 帳號 -->
              <td class="px-6 py-4 whitespace-nowrap">
                <div class="flex items-center">
                  <div class="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center mr-3">
                    <span class="text-sm font-medium text-blue-600">
                      {{ user.username.charAt(0).toUpperCase() }}
                    </span>
                  </div>
                  <span class="text-sm font-medium text-gray-900">{{ user.username }}</span>
                </div>
              </td>
              
              <!-- 姓名 -->
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {{ user.fullName || user.username }}
              </td>
              
              <!-- 電子信箱 -->
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                {{ user.email }}
              </td>
              
              <!-- 狀態 -->
              <td class="px-6 py-4 whitespace-nowrap">
                <span [class]="getStatusClass(user.status)">
                  {{ getStatusDisplay(user.status) }}
                </span>
              </td>
              
              <!-- 功能按鈕 -->
              <td class="px-6 py-4 whitespace-nowrap text-sm">
                <div class="flex flex-wrap gap-1">
                  <app-button
                    variant="view"
                    size="sm"
                    label="檢視權限"
                    (clicked)="onAction('viewPermissions', user)"
                  ></app-button>
                  <app-button
                    variant="secondary"
                    size="sm"
                    label="編輯"
                    (clicked)="onAction('edit', user)"
                  ></app-button>
                  <app-button
                    variant="favorite"
                    size="sm"
                    label="重置密碼"
                    (clicked)="onAction('resetPassword', user)"
                  ></app-button>
                  <app-button
                    [variant]="user.status === 'active' ? 'danger' : 'view'"
                    size="sm"
                    [label]="user.status === 'active' ? '停用' : '啟用'"
                    (clicked)="onAction('toggleStatus', user)"
                  ></app-button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
        
        <!-- Empty State -->
        <div *ngIf="users.length === 0" class="text-center py-12">
          <div class="text-gray-400 text-6xl mb-4">📭</div>
          <h3 class="text-lg font-medium text-gray-900 mb-2">找不到符合條件的使用者</h3>
          <p class="text-gray-600">請調整搜尋條件後再試</p>
        </div>
      </div>
    </div>
  `
})
export class UserTableComponent {
  readonly CSS_CLASSES = CSS_CLASSES;
  readonly statusDisplay = USER_STATUS_DISPLAY;
  readonly cn = cn;

  @Input() users: UserModel[] = [];
  @Input() currentPage = 1;
  @Input() pageSize = 10;
  @Input() totalItems = 0;

  @Output() userAction = new EventEmitter<UserAction>();

  readonly columns: UserTableColumn[] = [
    { key: 'index', label: '項次', width: 'w-16' },
    { key: 'username', label: '帳號' },
    { key: 'fullName', label: '姓名' },
    { key: 'email', label: '電子信箱' },
    { key: 'status', label: '狀態', width: 'w-20' },
    { key: 'actions', label: '功能', width: 'w-64' }
  ];

  getHeaderClass(column: UserTableColumn): string {
    const baseClass = 'px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider';
    return column.width ? `${baseClass} ${column.width}` : baseClass;
  }

  getRowNumber(index: number): number {
    return (this.currentPage - 1) * this.pageSize + index + 1;
  }

  getStatusClass(status: 'active' | 'inactive'): string {
    return this.cn(
      this.CSS_CLASSES.STATUS_BADGE_BASE,
      status === 'active' ? this.CSS_CLASSES.STATUS_ACTIVE : this.CSS_CLASSES.STATUS_INACTIVE
    );
  }

  getStatusDisplay(status: 'active' | 'inactive'): string {
    return this.statusDisplay[status];
  }

  onAction(type: UserAction['type'], user: UserModel): void {
    this.userAction.emit({ type, user });
  }
}