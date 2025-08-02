import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { Subject, combineLatest, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { ButtonComponent } from '../../ui/button/button.component';
import { SelectComponent } from '../../ui/select/select.component';
import { LoadingComponent } from '../../ui/loading/loading.component';
import { cn } from '../../../utils/cn';

import { 
  AuditViewerProps,
  AuditLogEntry,
  AuditLogFilter,
  AuditLogQueryResult,
  AuditStatistics
} from './audit-viewer.interface';

import { SelectOption } from '../../../../shared/interfaces/select-option.interface';

@Component({
  selector: 'app-audit-viewer',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    ReactiveFormsModule, 
    ButtonComponent,
    SelectComponent,
    LoadingComponent
  ],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200">
      <!-- Header -->
      <div class="p-6 border-b border-gray-200">
        <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
          <div>
            <h2 class="text-xl font-semibold text-gray-900">稽核日誌查詢</h2>
            <p class="text-sm text-gray-600 mt-1">查看和分析系統稽核記錄</p>
          </div>
          
          <div class="flex flex-wrap gap-2">
            <app-button
              variant="secondary"
              size="sm"
              icon="📊"
              [label]="showStatistics ? '隱藏統計' : '顯示統計'"
              (clicked)="toggleStatistics()"
            ></app-button>
            
            <app-button
              variant="secondary"
              size="sm"
              icon="🔄"
              label="重新整理"
              [disabled]="loading"
              [loading]="loading"
              (clicked)="refreshData()"
            ></app-button>
            
            <app-button
              variant="primary"
              size="sm"
              icon="📥"
              label="匯出日誌"
              [disabled]="loading || !queryResult"
              (clicked)="exportLogs()"
            ></app-button>
            
            <app-button
              *ngIf="canManage"
              variant="danger"
              size="sm"
              icon="🗑️"
              label="清理過期"
              [disabled]="loading"
              (clicked)="cleanupLogs()"
            ></app-button>
          </div>
        </div>
      </div>

      <!-- Statistics Panel -->
      <div *ngIf="showStatistics && statistics" class="p-6 bg-gray-50 border-b border-gray-200">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div class="bg-white p-4 rounded-lg border border-gray-200">
            <div class="text-sm font-medium text-gray-500">總事件數</div>
            <div class="text-2xl font-bold text-gray-900 mt-1">{{ statistics.totalEvents | number }}</div>
          </div>
          
          <div class="bg-white p-4 rounded-lg border border-gray-200">
            <div class="text-sm font-medium text-gray-500">成功率</div>
            <div class="text-2xl font-bold text-green-600 mt-1">{{ statistics.successRate | number:'1.1-1' }}%</div>
          </div>
          
          <div class="bg-white p-4 rounded-lg border border-gray-200">
            <div class="text-sm font-medium text-gray-500">可疑事件</div>
            <div class="text-2xl font-bold text-orange-600 mt-1">{{ statistics.suspiciousEvents | number }}</div>
          </div>
          
          <div class="bg-white p-4 rounded-lg border border-gray-200">
            <div class="text-sm font-medium text-gray-500">唯一使用者</div>
            <div class="text-2xl font-bold text-blue-600 mt-1">{{ statistics.uniqueUsers | number }}</div>
          </div>
        </div>
      </div>

      <!-- Filter Panel -->
      <div class="border-b border-gray-200">
        <div class="p-6">
          <div class="flex items-center justify-between mb-4">
            <div class="flex items-center gap-2">
              <h3 class="text-lg font-medium text-gray-900">過濾條件</h3>
              <span 
                *ngIf="activeFilterCount > 0" 
                class="px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded-full"
              >
                {{ activeFilterCount }} 個條件
              </span>
            </div>
            
            <div class="flex items-center gap-2">
              <app-button
                *ngIf="hasActiveFilters"
                variant="secondary"
                size="sm"
                label="清除篩選"
                (clicked)="clearAllFilters()"
              ></app-button>
              
              <app-button
                variant="secondary"
                size="sm"
                [label]="showFilters ? '收合' : '展開'"
                [icon]="showFilters ? '⬆️' : '⬇️'"
                (clicked)="showFilters = !showFilters"
              ></app-button>
            </div>
          </div>
          
          <form [formGroup]="filterForm" *ngIf="showFilters" class="space-y-4">
            <!-- Time Range -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">開始時間</label>
                <input
                  type="datetime-local"
                  formControlName="startDate"
                  class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                >
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">結束時間</label>
                <input
                  type="datetime-local"
                  formControlName="endDate"
                  class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                >
              </div>
            </div>
            
            <!-- User Filters -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">使用者ID</label>
                <input
                  type="text"
                  formControlName="userId"
                  placeholder="輸入使用者ID"
                  class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                >
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">使用者名稱</label>
                <input
                  type="text"
                  formControlName="userName"
                  placeholder="輸入使用者名稱"
                  class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                >
              </div>
            </div>
            
            <!-- Event and Resource Type -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">事件類型</label>
                <app-select
                  [options]="eventTypeOptions"
                  placeholder="選擇事件類型"
                  formControlName="eventTypes"
                  [searchable]="true"
                ></app-select>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">資源類型</label>
                <app-select
                  [options]="resourceTypeOptions"
                  placeholder="選擇資源類型"
                  formControlName="resourceType"
                ></app-select>
              </div>
            </div>
            
            <!-- Security Level and Success -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">安全等級</label>
                <app-select
                  [options]="securityLevelOptions"
                  placeholder="選擇安全等級"
                  formControlName="securityLevels"
                ></app-select>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">操作結果</label>
                <app-select
                  [options]="successOptions"
                  placeholder="選擇操作結果"
                  formControlName="success"
                ></app-select>
              </div>
            </div>
          </form>
        </div>
      </div>

      <!-- Results -->
      <div class="p-6">
        <!-- Loading State -->
        <div *ngIf="loading" class="flex justify-center py-8">
          <app-loading 
            variant="spinner" 
            size="lg"
            message="載入稽核日誌中..."
          ></app-loading>
        </div>

        <!-- No Results -->
        <div *ngIf="!loading && queryResult && queryResult.logs.length === 0" 
             class="text-center py-8">
          <div class="text-gray-400 text-6xl mb-4">📝</div>
          <h3 class="text-lg font-medium text-gray-900 mb-2">沒有找到稽核日誌</h3>
          <p class="text-gray-600">請嘗試調整搜尋條件</p>
        </div>

        <!-- Results Table -->
        <div *ngIf="!loading && queryResult && queryResult.logs.length > 0" class="space-y-4">
          <!-- Results Info -->
          <div class="flex items-center justify-between text-sm text-gray-600">
            <span>
              顯示第 {{ ((queryResult.page - 1) * queryResult.pageSize) + 1 }} - 
              {{ Math.min(queryResult.page * queryResult.pageSize, queryResult.totalCount) }} 筆，
              共 {{ queryResult.totalCount }} 筆記錄
            </span>
            <div class="flex items-center gap-2">
              <label class="text-sm">每頁顯示:</label>
              <select 
                [(ngModel)]="currentPageSize"
                (change)="onPageSizeChange()"
                class="px-2 py-1 border border-gray-300 rounded text-sm"
              >
                <option [value]="10">10</option>
                <option [value]="25">25</option>
                <option [value]="50">50</option>
                <option [value]="100">100</option>
              </select>
            </div>
          </div>

          <!-- Table -->
          <div class="overflow-x-auto border border-gray-200 rounded-lg">
            <table class="min-w-full divide-y divide-gray-200">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    時間
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    事件類型
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    使用者
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    動作
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    結果
                  </th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    IP位址
                  </th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr *ngFor="let log of queryResult.logs; trackBy: trackByLogId" 
                    class="hover:bg-gray-50 transition-colors">
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {{ log.occurredAt | date:'yyyy/MM/dd HH:mm:ss' }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span [class]="getEventTypeClasses(log.eventType)">
                      {{ log.eventType }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm text-gray-900">{{ log.userName || log.userId }}</div>
                    <div class="text-sm text-gray-500">{{ log.userRole }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {{ log.action }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span [class]="getStatusClasses(log.success)">
                      {{ log.success ? '成功' : '失敗' }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {{ log.ipAddress }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Pagination -->
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2">
              <app-button
                variant="secondary"
                size="sm"
                label="上一頁"
                [disabled]="queryResult.page <= 1"
                (clicked)="previousPage()"
              ></app-button>
              
              <span class="text-sm text-gray-600">
                第 {{ queryResult.page }} 頁，共 {{ queryResult.totalPages }} 頁
              </span>
              
              <app-button
                variant="secondary"
                size="sm"
                label="下一頁"
                [disabled]="queryResult.page >= queryResult.totalPages"
                (clicked)="nextPage()"
              ></app-button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AuditViewerComponent implements OnInit, OnDestroy, AuditViewerProps {
  @Input() canManage: boolean = false;
  @Input() showStatistics: boolean = true;
  @Input() autoRefresh: boolean = false;
  @Input() refreshInterval: number = 30000;
  @Input() pageSize: number = 25;

  // Component state
  loading = false;
  showFilters = true;
  queryResult: AuditLogQueryResult | null = null;
  statistics: AuditStatistics | null = null;
  currentPageSize = 25;

  // Form
  filterForm: FormGroup;
  
  // Options for dropdowns
  eventTypeOptions: SelectOption[] = [
    { value: 'LOGIN', label: '登入' },
    { value: 'LOGOUT', label: '登出' },
    { value: 'CREATE', label: '建立' },
    { value: 'UPDATE', label: '更新' },
    { value: 'DELETE', label: '刪除' },
    { value: 'VIEW', label: '檢視' },
    { value: 'EXPORT', label: '匯出' },
    { value: 'IMPORT', label: '匯入' }
  ];

  resourceTypeOptions: SelectOption[] = [
    { value: 'USER', label: '使用者' },
    { value: 'ROLE', label: '角色' },
    { value: 'PERMISSION', label: '權限' },
    { value: 'FILE', label: '檔案' },
    { value: 'PROJECT', label: '專案' },
    { value: 'PERSON', label: '人員' }
  ];

  securityLevelOptions: SelectOption[] = [
    { value: 'LOW', label: '低' },
    { value: 'MEDIUM', label: '中' },
    { value: 'HIGH', label: '高' },
    { value: 'CRITICAL', label: '極高' }
  ];

  successOptions: SelectOption[] = [
    { value: true, label: '成功' },
    { value: false, label: '失敗' }
  ];

  private destroy$ = new Subject<void>();

  constructor(private fb: FormBuilder) {
    this.filterForm = this.createFilterForm();
    this.currentPageSize = this.pageSize;
  }

  ngOnInit(): void {
    this.initializeComponent();
    this.setupFormSubscriptions();
    this.loadInitialData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Getters
  get activeFilterCount(): number {
    const values = this.filterForm.value;
    let count = 0;
    
    if (values.startDate) count++;
    if (values.endDate) count++;
    if (values.userId) count++;
    if (values.userName) count++;
    if (values.eventTypes) count++;
    if (values.resourceType) count++;
    if (values.securityLevels) count++;
    if (values.success !== null && values.success !== undefined) count++;
    
    return count;
  }

  get hasActiveFilters(): boolean {
    return this.activeFilterCount > 0;
  }

  // Public methods
  toggleStatistics(): void {
    this.showStatistics = !this.showStatistics;
    if (this.showStatistics && !this.statistics) {
      this.loadStatistics();
    }
  }

  refreshData(): void {
    this.loadAuditLogs();
    if (this.showStatistics) {
      this.loadStatistics();
    }
  }

  exportLogs(): void {
    if (!this.queryResult) return;
    
    // Implementation for export functionality
    console.log('Exporting logs...');
  }

  cleanupLogs(): void {
    if (!this.canManage) return;
    
    // Implementation for cleanup functionality
    console.log('Cleaning up logs...');
  }

  clearAllFilters(): void {
    this.filterForm.reset();
    this.loadAuditLogs();
  }

  onPageSizeChange(): void {
    this.pageSize = this.currentPageSize;
    this.loadAuditLogs();
  }

  previousPage(): void {
    if (this.queryResult && this.queryResult.page > 1) {
      this.loadAuditLogs(this.queryResult.page - 1);
    }
  }

  nextPage(): void {
    if (this.queryResult && this.queryResult.page < this.queryResult.totalPages) {
      this.loadAuditLogs(this.queryResult.page + 1);
    }
  }

  trackByLogId(index: number, log: AuditLogEntry): string {
    return log.id;
  }

  getEventTypeClasses(eventType: string): string {
    const baseClasses = 'px-2 py-1 text-xs font-medium rounded-full';
    
    switch (eventType) {
      case 'LOGIN':
      case 'LOGOUT':
        return cn(baseClasses, 'bg-blue-100 text-blue-800');
      case 'CREATE':
        return cn(baseClasses, 'bg-green-100 text-green-800');
      case 'UPDATE':
        return cn(baseClasses, 'bg-yellow-100 text-yellow-800');
      case 'DELETE':
        return cn(baseClasses, 'bg-red-100 text-red-800');
      case 'VIEW':
        return cn(baseClasses, 'bg-gray-100 text-gray-800');
      default:
        return cn(baseClasses, 'bg-purple-100 text-purple-800');
    }
  }

  getStatusClasses(success: boolean): string {
    const baseClasses = 'px-2 py-1 text-xs font-medium rounded-full';
    
    return success
      ? cn(baseClasses, 'bg-green-100 text-green-800')
      : cn(baseClasses, 'bg-red-100 text-red-800');
  }

  // Private methods
  private createFilterForm(): FormGroup {
    return this.fb.group({
      startDate: [null],
      endDate: [null],
      userId: [''],
      userName: [''],
      eventTypes: [null],
      resourceType: [null],
      securityLevels: [null],
      success: [null]
    });
  }

  private initializeComponent(): void {
    // Initialize component state
  }

  private setupFormSubscriptions(): void {
    // Debounce form changes and trigger search
    this.filterForm.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.loadAuditLogs();
      });
  }

  private loadInitialData(): void {
    this.loadAuditLogs();
    if (this.showStatistics) {
      this.loadStatistics();
    }
  }

  private loadAuditLogs(page: number = 1): void {
    this.loading = true;
    
    const filter = this.buildFilter(page);
    
    // Mock data for demonstration
    setTimeout(() => {
      this.queryResult = this.getMockQueryResult(filter);
      this.loading = false;
    }, 1000);
  }

  private loadStatistics(): void {
    // Mock statistics for demonstration
    this.statistics = {
      totalEvents: 15420,
      successRate: 98.7,
      suspiciousEvents: 23,
      uniqueUsers: 156,
      eventsByType: {},
      eventsByHour: {}
    };
  }

  private buildFilter(page: number): AuditLogFilter {
    const formValue = this.filterForm.value;
    
    return {
      timeRange: {
        startDate: formValue.startDate ? new Date(formValue.startDate) : undefined,
        endDate: formValue.endDate ? new Date(formValue.endDate) : undefined
      },
      userId: formValue.userId || undefined,
      userName: formValue.userName || undefined,
      eventTypes: formValue.eventTypes ? [formValue.eventTypes] : undefined,
      resourceType: formValue.resourceType || undefined,
      securityLevels: formValue.securityLevels ? [formValue.securityLevels] : undefined,
      success: formValue.success,
      page,
      pageSize: this.pageSize,
      sortField: 'occurredAt',
      sortDirection: 'DESC'
    };
  }

  private getMockQueryResult(filter: AuditLogFilter): AuditLogQueryResult {
    // Mock data - replace with actual service call
    const mockLogs: AuditLogEntry[] = Array.from({ length: this.pageSize }, (_, i) => ({
      id: `log-${i + 1}`,
      eventType: this.eventTypeOptions[Math.floor(Math.random() * this.eventTypeOptions.length)].value as string,
      action: '檢視使用者列表',
      userId: `user-${i + 1}`,
      userName: `使用者 ${i + 1}`,
      userRole: 'ADMIN',
      resourceType: 'USER',
      resourceId: `resource-${i + 1}`,
      ipAddress: `192.168.1.${100 + i}`,
      userAgent: 'Mozilla/5.0...',
      success: Math.random() > 0.1,
      errorMessage: Math.random() > 0.9 ? '權限不足' : undefined,
      additionalData: {},
      occurredAt: new Date(Date.now() - Math.random() * 86400000 * 7),
      securityLevel: this.securityLevelOptions[Math.floor(Math.random() * this.securityLevelOptions.length)].value as any
    }));

    return {
      logs: mockLogs,
      totalCount: 1000,
      page: filter.page || 1,
      pageSize: filter.pageSize || 25,
      totalPages: Math.ceil(1000 / (filter.pageSize || 25))
    };
  }
}