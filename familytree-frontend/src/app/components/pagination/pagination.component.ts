import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../ui/button/button.component';
import { cn } from '../../utils/cn';
import { USER_MANAGEMENT_CONSTANTS } from '../../constants/user-management.constants';

export interface PaginationConfig {
  currentPage: number;
  totalPages: number;
  totalItems: number;
  pageSize: number;
  maxVisiblePages?: number;
  pageSizeOptions?: readonly number[];
}

export interface PaginationEvent {
  type: 'pageChange' | 'pageSizeChange';
  page?: number;
  pageSize?: number;
}

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent],
  template: `
    <div *ngIf="config.totalPages > 1" class="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <div class="flex flex-col sm:flex-row items-center justify-between gap-4">
        <!-- 資料統計 -->
        <div class="text-sm text-gray-700">
          顯示 {{ getStartItem() }} 至 {{ getEndItem() }} 筆，共 {{ config.totalItems }} 筆資料
        </div>
        
        <!-- 分頁控制 -->
        <div class="flex items-center gap-2">
          <!-- 第一頁 -->
          <app-button
            variant="secondary"
            size="sm"
            label="第一頁"
            [disabled]="config.currentPage === 1"
            (clicked)="goToPage(1)"
          ></app-button>
          
          <!-- 上一頁 -->
          <app-button
            variant="secondary"
            size="sm"
            label="上一頁"
            [disabled]="config.currentPage === 1"
            (clicked)="goToPage(config.currentPage - 1)"
          ></app-button>
          
          <!-- 頁碼按鈕 -->
          <div class="flex gap-1">
            <button
              *ngFor="let page of visiblePages"
              (click)="goToPage(page)"
              [class]="getPageButtonClass(page)"
            >
              {{ page }}
            </button>
          </div>
          
          <!-- 下一頁 -->
          <app-button
            variant="secondary"
            size="sm"
            label="下一頁"
            [disabled]="config.currentPage === config.totalPages"
            (clicked)="goToPage(config.currentPage + 1)"
          ></app-button>
          
          <!-- 最後頁 -->
          <app-button
            variant="secondary"
            size="sm"
            label="最後頁"
            [disabled]="config.currentPage === config.totalPages"
            (clicked)="goToPage(config.totalPages)"
          ></app-button>
          
          <!-- 每頁筆數選擇 -->
          <select
            [value]="config.pageSize"
            (change)="onPageSizeChange($event)"
            class="ml-2 px-3 py-1 border border-gray-300 rounded text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option 
              *ngFor="let size of pageSizeOptions" 
              [value]="size"
            >
              {{ size }}
            </option>
          </select>
        </div>
      </div>
    </div>
  `
})
export class PaginationComponent implements OnChanges {
  readonly cn = cn;

  @Input() config: PaginationConfig = {
    currentPage: 1,
    totalPages: 1,
    totalItems: 0,
    pageSize: 10,
    maxVisiblePages: 5,
    pageSizeOptions: USER_MANAGEMENT_CONSTANTS.PAGE_SIZES
  };

  @Output() paginationChange = new EventEmitter<PaginationEvent>();

  visiblePages: number[] = [];
  pageSizeOptions: readonly number[] = USER_MANAGEMENT_CONSTANTS.PAGE_SIZES;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['config']) {
      this.updateVisiblePages();
      this.pageSizeOptions = this.config.pageSizeOptions || USER_MANAGEMENT_CONSTANTS.PAGE_SIZES;
    }
  }

  private updateVisiblePages(): void {
    const maxVisible = this.config.maxVisiblePages || 5;
    const current = this.config.currentPage;
    const total = this.config.totalPages;

    let start = Math.max(1, current - Math.floor(maxVisible / 2));
    let end = Math.min(total, start + maxVisible - 1);

    // 調整起始位置以確保顯示足夠的頁碼
    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }

    this.visiblePages = [];
    for (let i = start; i <= end; i++) {
      this.visiblePages.push(i);
    }
  }

  getStartItem(): number {
    return (this.config.currentPage - 1) * this.config.pageSize + 1;
  }

  getEndItem(): number {
    return Math.min(this.config.currentPage * this.config.pageSize, this.config.totalItems);
  }

  getPageButtonClass(page: number): string {
    return this.cn(
      'px-3 py-1 text-sm rounded transition-colors',
      page === this.config.currentPage
        ? 'bg-blue-600 text-white'
        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
    );
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.config.totalPages && page !== this.config.currentPage) {
      this.paginationChange.emit({
        type: 'pageChange',
        page
      });
    }
  }

  onPageSizeChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const pageSize = parseInt(target.value, 10);
    
    if (pageSize !== this.config.pageSize) {
      this.paginationChange.emit({
        type: 'pageSizeChange',
        pageSize
      });
    }
  }
}