import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { Subject, combineLatest, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { 
  AuditLogService, 
  AuditLogFilter, 
  AuditLogEntry, 
  AuditLogQueryResult,
  AuditLogSummary,
  AuditStatistics,
  EventType 
} from '../../services/audit-log.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-audit-log-viewer',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="audit-log-viewer">
      <!-- 標題和操作按鈕 -->
      <div class="header">
        <h2>稽核日誌查詢</h2>
        <div class="header-actions">
          <button 
            type="button" 
            class="btn btn-secondary"
            (click)="showStatistics = !showStatistics">
            {{ showStatistics ? '隱藏' : '顯示' }}統計
          </button>
          <button 
            type="button" 
            class="btn btn-secondary"
            (click)="refreshData()"
            [disabled]="loading">
            重新整理
          </button>
          <button 
            type="button" 
            class="btn btn-primary"
            (click)="exportLogs()"
            [disabled]="loading || !queryResult">
            匯出日誌
          </button>
          <button 
            *ngIf="canManage"
            type="button" 
            class="btn btn-warning"
            (click)="cleanupLogs()"
            [disabled]="loading">
            清理過期日誌
          </button>
        </div>
      </div>

      <!-- 統計資訊面板 -->
      <div *ngIf="showStatistics && statistics" class="statistics-panel">
        <div class="stats-grid">
          <div class="stat-card">
            <div class="stat-label">總事件數</div>
            <div class="stat-value">{{ statistics.totalEvents | number }}</div>
          </div>
          <div class="stat-card">
            <div class="stat-label">成功率</div>
            <div class="stat-value">{{ statistics.successRate | number:'1.1-1' }}%</div>
          </div>
          <div class="stat-card">
            <div class="stat-label">可疑事件</div>
            <div class="stat-value warning">{{ statistics.suspiciousEvents | number }}</div>
          </div>
          <div class="stat-card">
            <div class="stat-label">唯一使用者</div>
            <div class="stat-value">{{ statistics.uniqueUsers | number }}</div>
          </div>
        </div>
      </div>

      <!-- 查詢過濾器 -->
      <div class="filter-panel" [class.expanded]="showFilters">
        <div class="filter-header">
          <h3>過濾條件</h3>
          <button 
            type="button" 
            class="btn btn-link"
            (click)="showFilters = !showFilters">
            {{ showFilters ? '收合' : '展開' }}
          </button>
        </div>
        
        <form [formGroup]="filterForm" *ngIf="showFilters">
          <div class="form-row">
            <!-- 時間範圍 -->
            <div class="form-group">
              <label for="fromDate">開始時間</label>
              <input 
                type="datetime-local" 
                id="fromDate"
                formControlName="fromDate"
                class="form-control">
            </div>
            <div class="form-group">
              <label for="toDate">結束時間</label>
              <input 
                type="datetime-local" 
                id="toDate"
                formControlName="toDate"
                class="form-control">
            </div>
          </div>

          <div class="form-row">
            <!-- 使用者過濾 -->
            <div class="form-group">
              <label for="userId">使用者ID</label>
              <input 
                type="text" 
                id="userId"
                formControlName="userId"
                class="form-control"
                placeholder="輸入使用者ID">
            </div>
            <div class="form-group">
              <label for="userName">使用者名稱</label>
              <input 
                type="text" 
                id="userName"
                formControlName="userName"
                class="form-control"
                placeholder="輸入使用者名稱">
            </div>
          </div>

          <div class="form-row">
            <!-- 事件類型 -->
            <div class="form-group">
              <label for="eventTypes">事件類型</label>
              <select 
                id="eventTypes"
                formControlName="eventTypes"
                class="form-control"
                multiple>
                <optgroup *ngFor="let category of categories" [label]="category">
                  <option 
                    *ngFor="let eventType of getEventTypesByCategory(category)"
                    [value]="eventType.code">
                    {{ eventType.name }}
                  </option>
                </optgroup>
              </select>
            </div>
            <!-- 資源類型 -->
            <div class="form-group">
              <label for="resourceType">資源類型</label>
              <select 
                id="resourceType"
                formControlName="resourceType"
                class="form-control">
                <option value="">全部</option>
                <option value="Person">人員資料</option>
                <option value="File">檔案</option>
                <option value="Project">專案</option>
                <option value="System">系統</option>
              </select>
            </div>
          </div>

          <div class="form-row">
            <!-- 安全等級 -->
            <div class="form-group">
              <label for="securityLevels">安全等級</label>
              <select 
                id="securityLevels"
                formControlName="securityLevels"
                class="form-control"
                multiple>
                <option value="LOW">低</option>
                <option value="NORMAL">一般</option>
                <option value="HIGH">高</option>
                <option value="CRITICAL">嚴重</option>
              </select>
            </div>
            <!-- 操作結果 -->
            <div class="form-group">
              <label for="success">操作結果</label>
              <select 
                id="success"
                formControlName="success"
                class="form-control">
                <option value="">全部</option>
                <option value="true">成功</option>
                <option value="false">失敗</option>
              </select>
            </div>
          </div>

          <div class="form-row">
            <!-- 搜尋文字 -->
            <div class="form-group">
              <label for="searchText">搜尋</label>
              <input 
                type="text" 
                id="searchText"
                formControlName="searchText"
                class="form-control"
                placeholder="搜尋日誌內容...">
            </div>
            <!-- 特殊選項 -->
            <div class="form-group">
              <div class="form-check">
                <input 
                  type="checkbox" 
                  id="onlySuspicious"
                  formControlName="onlySuspicious"
                  class="form-check-input">
                <label for="onlySuspicious" class="form-check-label">
                  只顯示可疑事件
                </label>
              </div>
            </div>
          </div>

          <div class="filter-actions">
            <button 
              type="button" 
              class="btn btn-primary"
              (click)="applyFilter()"
              [disabled]="loading">
              套用過濾
            </button>
            <button 
              type="button" 
              class="btn btn-secondary"
              (click)="resetFilter()">
              重設
            </button>
          </div>
        </form>
      </div>

      <!-- 載入狀態 -->
      <div *ngIf="loading" class="loading-indicator">
        <div class="spinner"></div>
        <span>載入中...</span>
      </div>

      <!-- 查詢結果 -->
      <div *ngIf="!loading && queryResult" class="results-panel">
        <!-- 結果摘要 -->
        <div class="results-summary">
          <span>
            共找到 {{ queryResult.totalCount | number }} 筆記錄，
            顯示第 {{ (queryResult.page - 1) * queryResult.pageSize + 1 }} - 
            {{ Math.min(queryResult.page * queryResult.pageSize, queryResult.totalCount) }} 筆
          </span>
          <span class="query-time">
            查詢耗時: {{ queryResult.queryDuration }}
          </span>
        </div>

        <!-- 日誌表格 -->
        <div class="table-container">
          <table class="audit-log-table">
            <thead>
              <tr>
                <th (click)="sort('occurredAt')" class="sortable">
                  時間 
                  <span class="sort-indicator" [class]="getSortClass('occurredAt')"></span>
                </th>
                <th (click)="sort('eventType')" class="sortable">
                  事件類型
                  <span class="sort-indicator" [class]="getSortClass('eventType')"></span>
                </th>
                <th (click)="sort('action')" class="sortable">
                  操作
                  <span class="sort-indicator" [class]="getSortClass('action')"></span>
                </th>
                <th (click)="sort('userName')" class="sortable">
                  使用者
                  <span class="sort-indicator" [class]="getSortClass('userName')"></span>
                </th>
                <th>資源</th>
                <th (click)="sort('success')" class="sortable">
                  結果
                  <span class="sort-indicator" [class]="getSortClass('success')"></span>
                </th>
                <th (click)="sort('securityLevel')" class="sortable">
                  安全等級
                  <span class="sort-indicator" [class]="getSortClass('securityLevel')"></span>
                </th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              <tr 
                *ngFor="let log of queryResult.logs; trackBy: trackByLogId"
                [class.suspicious]="log.isSuspicious"
                [class.failed]="!log.success">
                <td class="timestamp">
                  {{ log.occurredAt | date:'yyyy-MM-dd HH:mm:ss' }}
                </td>
                <td class="event-type">
                  <span class="event-badge" [class]="getEventTypeClass(log.eventType)">
                    {{ getEventTypeName(log.eventType) }}
                  </span>
                </td>
                <td class="action">{{ log.action }}</td>
                <td class="user">
                  <div class="user-info">
                    <span class="user-name">{{ log.userName || log.userId || 'N/A' }}</span>
                    <small class="user-role" *ngIf="log.userRole">{{ log.userRole }}</small>
                  </div>
                </td>
                <td class="resource">
                  <div class="resource-info" *ngIf="log.resourceType">
                    <span class="resource-type">{{ log.resourceType }}</span>
                    <small class="resource-name" *ngIf="log.resourceName">{{ log.resourceName }}</small>
                  </div>
                </td>
                <td class="result">
                  <span 
                    class="result-badge"
                    [class.success]="log.success"
                    [class.failed]="!log.success">
                    {{ log.success ? '成功' : '失敗' }}
                  </span>
                  <small class="error-code" *ngIf="log.errorCode">{{ log.errorCode }}</small>
                </td>
                <td class="security-level">
                  <span 
                    class="security-badge"
                    [class]="getSecurityLevelClass(log.securityLevel)">
                    {{ getSecurityLevelName(log.securityLevel) }}
                  </span>
                  <span 
                    class="risk-score"
                    *ngIf="log.riskScore > 0"
                    [class.high-risk]="log.riskScore > 70">
                    ({{ log.riskScore }})
                  </span>
                </td>
                <td class="actions">
                  <button 
                    type="button"
                    class="btn btn-sm btn-link"
                    (click)="viewLogDetails(log)">
                    詳情
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- 分頁 -->
        <div class="pagination-container" *ngIf="queryResult.totalPages > 1">
          <nav aria-label="稽核日誌分頁">
            <ul class="pagination">
              <li class="page-item" [class.disabled]="!queryResult.hasPreviousPage">
                <button 
                  class="page-link"
                  (click)="changePage(queryResult.page - 1)"
                  [disabled]="!queryResult.hasPreviousPage">
                  上一頁
                </button>
              </li>
              
              <li 
                *ngFor="let page of getPageNumbers()"
                class="page-item"
                [class.active]="page === queryResult.page">
                <button 
                  class="page-link"
                  (click)="changePage(page)">
                  {{ page }}
                </button>
              </li>
              
              <li class="page-item" [class.disabled]="!queryResult.hasNextPage">
                <button 
                  class="page-link"
                  (click)="changePage(queryResult.page + 1)"
                  [disabled]="!queryResult.hasNextPage">
                  下一頁
                </button>
              </li>
            </ul>
          </nav>
        </div>
      </div>

      <!-- 空狀態 -->
      <div *ngIf="!loading && queryResult && queryResult.logs.length === 0" class="empty-state">
        <div class="empty-icon">📊</div>
        <h3>無稽核日誌</h3>
        <p>在指定的條件下未找到任何稽核日誌記錄</p>
        <button 
          type="button"
          class="btn btn-primary"
          (click)="resetFilter()">
          重設過濾條件
        </button>
      </div>
    </div>

    <!-- 日誌詳情模態框 -->
    <div 
      *ngIf="selectedLog"
      class="modal-overlay"
      (click)="closeLogDetails()">
      <div class="modal-content" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h4>稽核日誌詳情</h4>
          <button 
            type="button"
            class="btn-close"
            (click)="closeLogDetails()">
            ×
          </button>
        </div>
        <div class="modal-body">
          <div class="log-details">
            <div class="detail-group">
              <h5>基本資訊</h5>
              <div class="detail-grid">
                <div class="detail-item">
                  <label>事件ID:</label>
                  <span>{{ selectedLog.eventId }}</span>
                </div>
                <div class="detail-item">
                  <label>發生時間:</label>
                  <span>{{ selectedLog.occurredAt | date:'yyyy-MM-dd HH:mm:ss.SSS' }}</span>
                </div>
                <div class="detail-item">
                  <label>事件類型:</label>
                  <span>{{ getEventTypeName(selectedLog.eventType) }}</span>
                </div>
                <div class="detail-item">
                  <label>操作:</label>
                  <span>{{ selectedLog.action }}</span>
                </div>
              </div>
            </div>

            <div class="detail-group" *ngIf="selectedLog.userId">
              <h5>使用者資訊</h5>
              <div class="detail-grid">
                <div class="detail-item">
                  <label>使用者ID:</label>
                  <span>{{ selectedLog.userId }}</span>
                </div>
                <div class="detail-item">
                  <label>使用者名稱:</label>
                  <span>{{ selectedLog.userName || 'N/A' }}</span>
                </div>
                <div class="detail-item">
                  <label>角色:</label>
                  <span>{{ selectedLog.userRole || 'N/A' }}</span>
                </div>
                <div class="detail-item">
                  <label>IP位址:</label>
                  <span>{{ selectedLog.ipAddress || 'N/A' }}</span>
                </div>
              </div>
            </div>

            <div class="detail-group" *ngIf="selectedLog.resourceType">
              <h5>資源資訊</h5>
              <div class="detail-grid">
                <div class="detail-item">
                  <label>資源類型:</label>
                  <span>{{ selectedLog.resourceType }}</span>
                </div>
                <div class="detail-item">
                  <label>資源ID:</label>
                  <span>{{ selectedLog.resourceId || 'N/A' }}</span>
                </div>
                <div class="detail-item">
                  <label>資源名稱:</label>
                  <span>{{ selectedLog.resourceName || 'N/A' }}</span>
                </div>
              </div>
            </div>

            <div class="detail-group">
              <h5>結果資訊</h5>
              <div class="detail-grid">
                <div class="detail-item">
                  <label>操作結果:</label>
                  <span [class.text-success]="selectedLog.success" [class.text-danger]="!selectedLog.success">
                    {{ selectedLog.success ? '成功' : '失敗' }}
                  </span>
                </div>
                <div class="detail-item" *ngIf="selectedLog.errorMessage">
                  <label>錯誤訊息:</label>
                  <span class="text-danger">{{ selectedLog.errorMessage }}</span>
                </div>
                <div class="detail-item" *ngIf="selectedLog.responseTimeMs">
                  <label>回應時間:</label>
                  <span>{{ selectedLog.responseTimeMs }}ms</span>
                </div>
              </div>
            </div>

            <div class="detail-group">
              <h5>安全資訊</h5>
              <div class="detail-grid">
                <div class="detail-item">
                  <label>安全等級:</label>
                  <span [class]="getSecurityLevelClass(selectedLog.securityLevel)">
                    {{ getSecurityLevelName(selectedLog.securityLevel) }}
                  </span>
                </div>
                <div class="detail-item">
                  <label>風險評分:</label>
                  <span [class.text-warning]="selectedLog.riskScore > 50" [class.text-danger]="selectedLog.riskScore > 70">
                    {{ selectedLog.riskScore }}
                  </span>
                </div>
                <div class="detail-item">
                  <label>可疑事件:</label>
                  <span [class.text-danger]="selectedLog.isSuspicious">
                    {{ selectedLog.isSuspicious ? '是' : '否' }}
                  </span>
                </div>
              </div>
            </div>

            <div class="detail-group" *ngIf="selectedLog.changesSummary">
              <h5>變更摘要</h5>
              <div class="changes-summary">
                {{ selectedLog.changesSummary }}
              </div>
            </div>

            <div class="detail-group" *ngIf="selectedLog.oldValues || selectedLog.newValues">
              <h5>變更詳情</h5>
              <div class="changes-detail">
                <div class="change-section" *ngIf="selectedLog.oldValues">
                  <h6>變更前:</h6>
                  <pre>{{ selectedLog.oldValues | json }}</pre>
                </div>
                <div class="change-section" *ngIf="selectedLog.newValues">
                  <h6>變更後:</h6>
                  <pre>{{ selectedLog.newValues | json }}</pre>
                </div>
              </div>
            </div>

            <div class="detail-group" *ngIf="selectedLog.additionalData">
              <h5>額外資訊</h5>
              <pre>{{ selectedLog.additionalData | json }}</pre>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./audit-log-viewer.component.scss']
})
export class AuditLogViewerComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // 資料狀態
  loading = false;
  showFilters = true;
  showStatistics = false;
  queryResult: AuditLogQueryResult | null = null;
  statistics: AuditStatistics | null = null;
  selectedLog: AuditLogEntry | null = null;
  eventTypes: EventType[] = [];
  categories: string[] = [];

  // 表單
  filterForm: FormGroup;
  currentFilter: AuditLogFilter = {};

  // 權限
  canManage = false;
  // 提供 Math 物件給模板使用
  Math = Math;

  constructor(
    private auditLogService: AuditLogService,
    private toastService: ToastService,
    private fb: FormBuilder
  ) {
    this.filterForm = this.createFilterForm();
    this.canManage = this.auditLogService.canManageAuditLogs();
  }

  ngOnInit(): void {
    this.initializeComponent();
    this.setupFormSubscriptions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private async initializeComponent(): Promise<void> {
    try {
      // 載入事件類型
      this.eventTypes = await this.auditLogService.getEventTypes().toPromise() || [];
      this.categories = this.auditLogService.getCategories();

      // 設定預設過濾條件
      const defaultFilter = this.auditLogService.getDefaultFilter();
      this.filterForm.patchValue(this.convertFilterToFormValue(defaultFilter));
      
      // 執行初始查詢
      await this.applyFilter();

      // 載入統計資料
      if (this.showStatistics) {
        await this.loadStatistics();
      }
    } catch (error) {
      console.error('初始化稽核日誌檢視器失敗:', error);
      this.toastService.error('載入稽核日誌失敗');
    }
  }

  private createFilterForm(): FormGroup {
    return this.fb.group({
      fromDate: [''],
      toDate: [''],
      userId: [''],
      userName: [''],
      eventTypes: [[]],
      resourceType: [''],
      securityLevels: [[]],
      success: [''],
      searchText: [''],
      onlySuspicious: [false]
    });
  }

  private setupFormSubscriptions(): void {
    // 搜尋文字即時搜尋
    this.filterForm.get('searchText')?.valueChanges
      .pipe(
        debounceTime(500),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        if (this.filterForm.get('searchText')?.value?.length >= 3 || 
            this.filterForm.get('searchText')?.value?.length === 0) {
          this.applyFilter();
        }
      });
  }

  async applyFilter(): Promise<void> {
    try {
      this.loading = true;
      const formValue = this.filterForm.value;
      const filter = this.convertFormValueToFilter(formValue);
      
      this.currentFilter = filter;
      this.queryResult = await this.auditLogService.queryLogs(filter).toPromise() || null;
    } catch (error) {
      console.error('查詢稽核日誌失敗:', error);
      this.toastService.error('查詢稽核日誌失敗');
    } finally {
      this.loading = false;
    }
  }

  async refreshData(): Promise<void> {
    await this.applyFilter();
    if (this.showStatistics) {
      await this.loadStatistics();
    }
    this.toastService.success('資料已更新');
  }

  resetFilter(): void {
    const defaultFilter = this.auditLogService.getDefaultFilter();
    this.filterForm.reset(this.convertFilterToFormValue(defaultFilter));
    this.applyFilter();
  }

  async loadStatistics(): Promise<void> {
    try {
      const filter = this.currentFilter;
      this.statistics = await this.auditLogService.getStatistics(
        filter.fromDate, 
        filter.toDate
      ).toPromise() || null;
    } catch (error) {
      console.error('載入統計資料失敗:', error);
    }
  }

  async exportLogs(): Promise<void> {
    try {
      this.loading = true;
      const blob = await this.auditLogService.exportLogs(this.currentFilter, 'CSV').toPromise();
      
      if (blob) {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `audit_logs_${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
        
        this.toastService.success('稽核日誌已匯出');
      }
    } catch (error) {
      console.error('匯出稽核日誌失敗:', error);
      this.toastService.error('匯出稽核日誌失敗');
    } finally {
      this.loading = false;
    }
  }

  async cleanupLogs(): Promise<void> {
    if (!confirm('確定要清理過期的稽核日誌嗎？此操作無法復原。')) {
      return;
    }

    try {
      this.loading = true;
      const result = await this.auditLogService.cleanupExpiredLogs().toPromise();
      
      if (result) {
        this.toastService.success(`已清理 ${result.deletedCount} 個過期日誌`);
        await this.refreshData();
      }
    } catch (error) {
      console.error('清理過期日誌失敗:', error);
      this.toastService.error('清理過期日誌失敗');
    } finally {
      this.loading = false;
    }
  }

  changePage(page: number): void {
    if (page >= 1 && page <= (this.queryResult?.totalPages || 1)) {
      this.currentFilter.page = page;
      this.applyFilter();
    }
  }

  sort(field: string): void {
    if (this.currentFilter.sortField === field) {
      this.currentFilter.sortDirection = this.currentFilter.sortDirection === 'ASC' ? 'DESC' : 'ASC';
    } else {
      this.currentFilter.sortField = field;
      this.currentFilter.sortDirection = 'DESC';
    }
    this.applyFilter();
  }

  viewLogDetails(log: AuditLogEntry): void {
    this.selectedLog = log;
  }

  closeLogDetails(): void {
    this.selectedLog = null;
  }

  // 輔助方法
  trackByLogId(index: number, log: AuditLogEntry): number {
    return log.id;
  }

  getEventTypeName(code: string): string {
    return this.auditLogService.getEventTypeName(code);
  }

  getEventTypesByCategory(category: string): EventType[] {
    return this.auditLogService.getEventTypesByCategory(category);
  }

  getEventTypeClass(eventType: string): string {
    if (eventType.includes('LOGIN') || eventType.includes('AUTH')) return 'security';
    if (eventType.includes('DATA')) return 'business';
    if (eventType.includes('FILE')) return 'file';
    if (eventType.includes('SYSTEM')) return 'system';
    if (eventType.includes('SECURITY')) return 'critical';
    return 'default';
  }

  getSecurityLevelClass(level: string): string {
    switch (level) {
      case 'LOW': return 'security-low';
      case 'NORMAL': return 'security-normal';
      case 'HIGH': return 'security-high';
      case 'CRITICAL': return 'security-critical';
      default: return 'security-normal';
    }
  }

  getSecurityLevelName(level: string): string {
    switch (level) {
      case 'LOW': return '低';
      case 'NORMAL': return '一般';
      case 'HIGH': return '高';
      case 'CRITICAL': return '嚴重';
      default: return level;
    }
  }

  getSortClass(field: string): string {
    if (this.currentFilter.sortField !== field) return '';
    return this.currentFilter.sortDirection === 'ASC' ? 'asc' : 'desc';
  }

  getPageNumbers(): number[] {
    if (!this.queryResult) return [];
    
    const total = this.queryResult.totalPages;
    const current = this.queryResult.page;
    const pages: number[] = [];
    
    // 顯示當前頁面前後各2頁
    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    
    return pages;
  }

  private convertFilterToFormValue(filter: AuditLogFilter): any {
    return {
      fromDate: filter.fromDate ? filter.fromDate.slice(0, 16) : '',
      toDate: filter.toDate ? filter.toDate.slice(0, 16) : '',
      userId: filter.userId || '',
      userName: filter.userName || '',
      eventTypes: filter.eventTypes || [],
      resourceType: filter.resourceType || '',
      securityLevels: filter.securityLevels || [],
      success: filter.success !== undefined ? filter.success.toString() : '',
      searchText: filter.searchText || '',
      onlySuspicious: filter.onlySuspicious || false
    };
  }

  private convertFormValueToFilter(formValue: any): AuditLogFilter {
    const filter: AuditLogFilter = {
      ...this.currentFilter,
      fromDate: formValue.fromDate ? new Date(formValue.fromDate).toISOString() : undefined,
      toDate: formValue.toDate ? new Date(formValue.toDate).toISOString() : undefined,
      userId: formValue.userId || undefined,
      userName: formValue.userName || undefined,
      eventTypes: formValue.eventTypes?.length ? formValue.eventTypes : undefined,
      resourceType: formValue.resourceType || undefined,
      securityLevels: formValue.securityLevels?.length ? formValue.securityLevels : undefined,
      success: formValue.success !== '' ? formValue.success === 'true' : undefined,
      searchText: formValue.searchText || undefined,
      onlySuspicious: formValue.onlySuspicious || undefined,
      page: 1, // 重設為第一頁
      pageSize: 50,
      sortField: this.currentFilter.sortField || 'occurredAt',
      sortDirection: this.currentFilter.sortDirection || 'DESC'
    };

    return filter;
  }
}