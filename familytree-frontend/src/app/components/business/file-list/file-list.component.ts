import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../../ui/button/button.component';
import { LoadingComponent } from '../../ui/loading/loading.component';
import { ConfirmDialogComponent } from '../../ui/modal/confirm-dialog.component';
import { cn } from '../../../utils/cn';

import {
  FileListProps,
  FileUploadModel,
  FileStatus,
  FileListFilter,
  DeleteImpactResponse
} from './file-list.interface';

@Component({
  selector: 'app-file-list',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    ButtonComponent, 
    LoadingComponent,
    ConfirmDialogComponent
  ],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200">
      <!-- Header -->
      <div class="p-6 border-b border-gray-200">
        <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
          <div>
            <h3 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
              📁 已上傳檔案
            </h3>
            <p class="text-sm text-gray-600 mt-1">
              共 {{ filteredFiles.length }} 個檔案
              <span *ngIf="files.length !== filteredFiles.length">
                (篩選前 {{ files.length }} 個)
              </span>
            </p>
          </div>
          
          <div class="flex flex-wrap gap-2">
            <!-- Search -->
            <div class="relative">
              <input
                type="text"
                [(ngModel)]="searchTerm"
                (ngModelChange)="onFilterChange()"
                placeholder="搜尋檔案名稱..."
                class="pl-8 pr-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              >
              <svg class="absolute left-2.5 top-2.5 h-4 w-4 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd" />
              </svg>
            </div>
            
            <!-- Status Filter -->
            <select 
              [(ngModel)]="selectedStatus"
              (change)="onFilterChange()"
              class="px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value="">所有狀態</option>
              <option value="PENDING">等待處理</option>
              <option value="PROCESSING">處理中</option>
              <option value="COMPLETED">已完成</option>
              <option value="FAILED">失敗</option>
              <option value="CANCELLED">已取消</option>
            </select>
            
            <!-- Refresh Button -->
            <app-button
              variant="secondary"
              size="sm"
              icon="🔄"
              label="重新整理"
              [disabled]="loading"
              [loading]="loading"
              (clicked)="refreshFiles()"
            ></app-button>
          </div>
        </div>
      </div>

      <!-- Content -->
      <div class="p-6">
        <!-- Loading State -->
        <div *ngIf="loading" class="flex justify-center py-8">
          <app-loading 
            variant="spinner" 
            size="md"
            message="載入檔案列表中..."
          ></app-loading>
        </div>

        <!-- Error State -->
        <div *ngIf="error && !loading" class="text-center py-8">
          <div class="text-red-400 text-6xl mb-4">❌</div>
          <h3 class="text-lg font-medium text-gray-900 mb-2">載入失敗</h3>
          <p class="text-gray-600 mb-4">{{ error }}</p>
          <app-button
            variant="primary"
            label="重試"
            (clicked)="refreshFiles()"
          ></app-button>
        </div>

        <!-- Empty State -->
        <div *ngIf="!loading && !error && filteredFiles.length === 0" class="text-center py-8">
          <div class="text-gray-400 text-6xl mb-4">📄</div>
          <h3 class="text-lg font-medium text-gray-900 mb-2">
            {{ searchTerm || selectedStatus ? '沒有符合條件的檔案' : '尚未上傳任何檔案' }}
          </h3>
          <p class="text-gray-600">
            {{ searchTerm || selectedStatus ? '請嘗試調整搜尋條件' : '上傳的檔案將顯示在這裡' }}
          </p>
        </div>

        <!-- File List -->
        <div *ngIf="!loading && !error && filteredFiles.length > 0" class="space-y-4">
          <div 
            *ngFor="let file of filteredFiles; trackBy: trackByFileId"
            class="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
          >
            <div class="flex items-start justify-between">
              <!-- File Info -->
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-3 mb-2">
                  <div class="text-2xl">📊</div>
                  <div class="flex-1 min-w-0">
                    <h4 class="text-sm font-medium text-gray-900 truncate">
                      {{ file.originalFilename }}
                    </h4>
                    <div class="flex items-center gap-4 text-xs text-gray-500 mt-1">
                      <span>{{ formatFileSize(file.fileSize) }}</span>
                      <span>{{ formatDate(file.uploadTime) }}</span>
                      <span *ngIf="file.userId">上傳者: {{ file.userId }}</span>
                    </div>
                  </div>
                </div>
                
                <!-- Status and Progress -->
                <div class="flex items-center gap-3 mb-2">
                  <span [class]="getStatusClasses(file.status)">
                    {{ getStatusText(file.status) }}
                  </span>
                  
                  <span 
                    *ngIf="file.isMerged" 
                    class="px-2 py-1 text-xs font-medium bg-green-100 text-green-800 rounded-full"
                  >
                    已合併
                  </span>
                </div>
                
                <!-- Progress Info -->
                <div *ngIf="file.status === 'PROCESSING' && file.totalRecords" class="mb-2">
                  <div class="flex items-center justify-between text-xs text-gray-600 mb-1">
                    <span>處理進度</span>
                    <span>{{ file.processedRecords || 0 }} / {{ file.totalRecords }}</span>
                  </div>
                  <div class="w-full bg-gray-200 rounded-full h-2">
                    <div 
                      class="bg-blue-600 h-2 rounded-full transition-all duration-300"
                      [style.width.%]="getProgressPercentage(file)"
                    ></div>
                  </div>
                </div>
                
                <!-- Error Message -->
                <div *ngIf="file.status === 'FAILED' && file.errorMessage" 
                     class="text-sm text-red-600 bg-red-50 p-2 rounded mt-2">
                  <strong>錯誤：</strong>{{ file.errorMessage }}
                </div>
              </div>
              
              <!-- Actions -->
              <div *ngIf="showActions" class="flex items-center gap-2 ml-4">
                <app-button
                  *ngIf="allowDownload"
                  variant="secondary"
                  size="sm"
                  icon="📥"
                  title="下載檔案"
                  (clicked)="downloadFile(file)"
                ></app-button>
                
                <app-button
                  *ngIf="allowDelete"
                  variant="danger"
                  size="sm"
                  icon="🗑️"
                  title="刪除檔案"
                  [disabled]="file.status === 'PROCESSING'"
                  (clicked)="confirmDeleteFile(file)"
                ></app-button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Dialog -->
    <app-confirm-dialog
      [isOpen]="showDeleteDialog"
      [title]="deleteDialogTitle"
      [message]="deleteDialogMessage"
      [isDanger]="true"
      confirmText="確認刪除"
      cancelText="取消"
      (confirm)="executeDelete()"
      (cancel)="cancelDelete()"
    ></app-confirm-dialog>
  `
})
export class FileListComponent implements OnInit, OnChanges, FileListProps {
  @Input() files: FileUploadModel[] = [];
  @Input() loading: boolean = false;
  @Input() error?: string;
  @Input() showActions: boolean = true;
  @Input() allowDelete: boolean = true;
  @Input() allowDownload: boolean = true;

  @Output() fileDelete = new EventEmitter<FileUploadModel>();
  @Output() fileDownload = new EventEmitter<FileUploadModel>();
  @Output() refresh = new EventEmitter<void>();

  // Filter state
  filteredFiles: FileUploadModel[] = [];
  searchTerm: string = '';
  selectedStatus: string = '';

  // Dialog state
  showDeleteDialog: boolean = false;
  deleteDialogTitle: string = '';
  deleteDialogMessage: string = '';
  fileToDelete: FileUploadModel | null = null;

  constructor() {}

  ngOnInit(): void {
    this.applyFilters();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['files']) {
      this.applyFilters();
    }
  }

  // Public methods
  refreshFiles(): void {
    this.refresh.emit();
  }

  downloadFile(file: FileUploadModel): void {
    this.fileDownload.emit(file);
  }

  confirmDeleteFile(file: FileUploadModel): void {
    this.fileToDelete = file;
    this.deleteDialogTitle = '確認刪除檔案';
    this.deleteDialogMessage = `確定要刪除檔案 "${file.originalFilename}" 嗎？\n\n此操作無法復原。`;
    this.showDeleteDialog = true;
  }

  executeDelete(): void {
    if (this.fileToDelete) {
      this.fileDelete.emit(this.fileToDelete);
      this.cancelDelete();
    }
  }

  cancelDelete(): void {
    this.showDeleteDialog = false;
    this.fileToDelete = null;
    this.deleteDialogTitle = '';
    this.deleteDialogMessage = '';
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  trackByFileId(index: number, file: FileUploadModel): string {
    return file.id;
  }

  // Formatting methods
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  formatDate(date: Date | string): string {
    const d = new Date(date);
    return d.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getStatusText(status: FileStatus): string {
    const statusMap: Record<FileStatus, string> = {
      'PENDING': '等待處理',
      'PROCESSING': '處理中',
      'COMPLETED': '已完成',
      'FAILED': '失敗',
      'CANCELLED': '已取消'
    };
    return statusMap[status] || status;
  }

  getStatusClasses(status: FileStatus): string {
    const baseClasses = 'px-2 py-1 text-xs font-medium rounded-full';
    
    const statusClasses: Record<FileStatus, string> = {
      'PENDING': 'bg-yellow-100 text-yellow-800',
      'PROCESSING': 'bg-blue-100 text-blue-800',
      'COMPLETED': 'bg-green-100 text-green-800',
      'FAILED': 'bg-red-100 text-red-800',
      'CANCELLED': 'bg-gray-100 text-gray-800'
    };

    return cn(baseClasses, statusClasses[status] || 'bg-gray-100 text-gray-800');
  }

  getProgressPercentage(file: FileUploadModel): number {
    if (!file.totalRecords || file.totalRecords === 0) return 0;
    return Math.round(((file.processedRecords || 0) / file.totalRecords) * 100);
  }

  // Private methods
  private applyFilters(): void {
    let filtered = [...this.files];

    // Apply search filter
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(file => 
        file.originalFilename.toLowerCase().includes(term) ||
        file.filename.toLowerCase().includes(term)
      );
    }

    // Apply status filter
    if (this.selectedStatus) {
      filtered = filtered.filter(file => file.status === this.selectedStatus);
    }

    // Sort by upload time (newest first)
    filtered.sort((a, b) => new Date(b.uploadTime).getTime() - new Date(a.uploadTime).getTime());

    this.filteredFiles = filtered;
  }
}