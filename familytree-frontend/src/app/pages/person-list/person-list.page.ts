import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PersonService, AnalysisProgress } from '../../services/person.service';
import { Router } from '@angular/router';
import { AppConstants } from '../../constants/app.constants';

@Component({
  selector: 'app-person-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="analysis-jobs-page">
      <div class="page-header">
        <h1>🌳 關聯圖譜分析工作</h1>
        <p>監控和管理正在執行的圖譜分析任務</p>
      </div>
      
      <div class="jobs-container">
        <div class="jobs-header">
          <div class="stats">
            <div class="stat-item">
              <span class="stat-number">{{ activeJobsCount }}</span>
              <span class="stat-label">進行中</span>
            </div>
            <div class="stat-item">
              <span class="stat-number">{{ completedJobsCount }}</span>
              <span class="stat-label">已完成</span>
            </div>
            <div class="stat-item">
              <span class="stat-number">{{ failedJobsCount }}</span>
              <span class="stat-label">失敗</span>
            </div>
          </div>
          <div class="auto-refresh-info">
            <span class="refresh-indicator">🔄 即時更新中</span>
          </div>
        </div>
        
        <div class="jobs-list">
          <div *ngIf="analysisJobs.length === 0" class="empty-placeholder">
            <div class="placeholder-icon">🌳</div>
            <h3>目前沒有分析工作</h3>
            <p>前往人物清單開始新的圖譜分析</p>
          </div>
          
          <div *ngFor="let job of analysisJobs" class="job-item" [class.completed]="job.status === 'completed'" [class.failed]="job.status === 'failed'" [class.processing]="job.status === 'processing'">
            <div class="job-header">
              <div class="job-info">
                <h4>{{ job.personName || '未知人員' }}</h4>
                <span class="job-status" [class]="'status-' + job.status">
                  {{ getStatusText(job.status) }}
                </span>
              </div>
              <div class="job-actions">
                <button *ngIf="job.status === 'processing'" class="btn btn-sm btn-warning" (click)="stopJob(job.personId)">
                  ⏹️ 終止
                </button>
                <button *ngIf="job.status === 'completed'" class="btn btn-sm btn-success" (click)="viewResults(job.personId)">
                  👁️ 查看結果
                </button>
              </div>
            </div>
            
            <div class="job-progress" *ngIf="job.status === 'processing'">
              <div class="progress-bar">
                <div class="progress-fill" [style.width.%]="job.progressPercentage"></div>
              </div>
              <span class="progress-text">{{ job.progressPercentage }}%</span>
            </div>
            
            <div class="job-details">
              <div class="detail-item" *ngIf="job.status === 'completed'">
                <span class="detail-label">完成時間:</span>
                <span class="detail-value">{{ job.completedTime | date:'yyyy-MM-dd HH:mm:ss' }}</span>
              </div>
              <div class="detail-item" *ngIf="job.status === 'failed'">
                <span class="detail-label">錯誤訊息:</span>
                <span class="detail-value error">{{ job.errorMessage || '未知錯誤' }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./person-list.page.scss']
})
export class PersonListComponent implements OnInit, OnDestroy {
  analysisJobs: AnalysisJob[] = [];
  private refreshInterval: any;

  get activeJobsCount(): number {
    return this.analysisJobs.filter(job => job.status === 'processing').length;
  }

  get completedJobsCount(): number {
    return this.analysisJobs.filter(job => job.status === 'completed').length;
  }

  get failedJobsCount(): number {
    return this.analysisJobs.filter(job => job.status === 'failed').length;
  }

  constructor(private personService: PersonService, private router: Router) {}

  ngOnInit() {
    this.loadAnalysisJobs();
    // 自動重新整理，實現即時更新
    this.refreshInterval = setInterval(() => {
      this.loadAnalysisJobs();
    }, AppConstants.REFRESH_INTERVAL_MS);
  }

  ngOnDestroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  loadAnalysisJobs() {
    this.personService.getAllAnalysisJobs().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.analysisJobs = response.data.map(job => ({
            personId: job.personId,
            personName: job.personName,
            status: job.status,
            progressPercentage: job.progressPercentage,
            completedTime: job.completedTime ? new Date(job.completedTime) : null,
            errorMessage: job.errorMessage || null
          }));
        } else {
          console.error('載入分析工作失敗:', response.message);
          this.analysisJobs = [];
        }
      },
      error: (error) => {
        console.error('載入分析工作錯誤:', error);
        this.analysisJobs = [];
      }
    });
  }

  getStatusText(status: string): string {
    return AppConstants.STATUS_TEXTS[status as keyof typeof AppConstants.STATUS_TEXTS] || AppConstants.STATUS_TEXTS.unknown;
  }

  viewResults(personId: number) {
    console.log('查看結果:', personId);
    // 導航到家系圖頁面並傳遞人員ID
    this.router.navigate(['/family-tree'], { 
      queryParams: { 
        viewResults: 'true', 
        personId: personId.toString() 
      } 
    });
  }

  stopJob(personId: number) {
    const job = this.analysisJobs.find(j => j.personId === personId);
    const personName = job?.personName || `人物ID ${personId}`;
    
    if (confirm(`確定要終止 ${personName} 的分析工作嗎？此操作無法撤銷。`)) {
      console.log('開始終止工作:', personId);
      this.retryStopAnalysis(personId, AppConstants.MAX_RETRY_COUNT);
    }
  }

  private retryStopAnalysis(personId: number, retryCount: number) {
    this.personService.stopAnalysis(personId).subscribe({
      next: (response) => {
        console.log('終止工作回應:', response);
        if (response.success) {
          this.showNotification('分析工作已終止', AppConstants.NOTIFICATION_TYPES.success);
          this.loadAnalysisJobs();
        } else {
          this.removeJobFromList(personId);
        }
      },
      error: (error) => {
        console.error('終止工作錯誤:', error);
        if (retryCount > 0) {
          console.log(`重試終止工作，剩餘重試次數: ${retryCount - 1}`);
          setTimeout(() => {
            this.retryStopAnalysis(personId, retryCount - 1);
          }, 1000);
        } else {
          this.showNotification('終止工作失敗', AppConstants.NOTIFICATION_TYPES.error);
          this.removeJobFromList(personId);
        }
      }
    });
  }

  private removeJobFromList(personId: number) {
    this.analysisJobs = this.analysisJobs.filter(job => job.personId !== personId);
  }

  private showNotification(message: string, type: 'success' | 'error' | 'info') {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
      <div class="notification-content">
        <span class="notification-icon">${this.getNotificationIcon(type)}</span>
        <span class="notification-message">${message}</span>
      </div>
    `;

    // 添加樣式
    notification.style.cssText = `
      position: fixed;
      top: 20px;
      right: 20px;
      padding: 12px 20px;
      border-radius: 8px;
      color: white;
      font-weight: 500;
      z-index: 10000;
      background: ${this.getNotificationColor(type)};
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
      animation: slideIn 0.3s ease-out;
    `;

    document.body.appendChild(notification);

    // 3秒後自動移除
    setTimeout(() => {
      if (notification.parentNode) {
        notification.parentNode.removeChild(notification);
      }
    }, 3000);
  }

  private getNotificationIcon(type: 'success' | 'error' | 'info'): string {
    switch (type) {
      case 'success': return '✅';
      case 'error': return '❌';
      case 'info': return 'ℹ️';
      default: return 'ℹ️';
    }
  }

  private getNotificationColor(type: 'success' | 'error' | 'info'): string {
    switch (type) {
      case 'success': return '#10b981';
      case 'error': return '#ef4444';
      case 'info': return '#3b82f6';
      default: return '#3b82f6';
    }
  }
}

interface AnalysisJob {
  personId: number;
  personName: string;
  status: 'processing' | 'completed' | 'failed';
  progressPercentage: number;
  completedTime: Date | null;
  errorMessage: string | null;
} 