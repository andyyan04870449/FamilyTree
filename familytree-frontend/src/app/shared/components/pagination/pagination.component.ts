import { Component, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <nav class="pagination" aria-label="分頁導航" *ngIf="totalPages > 1">
      <div class="pagination__info">
        顯示 {{ startItem }}-{{ endItem }} 筆，共 {{ totalItems }} 筆
      </div>
      
      <div class="pagination__controls">
        <button 
          class="pagination__btn" 
          [disabled]="currentPage === 1"
          (click)="goToPage(currentPage - 1)"
          aria-label="前往上一頁"
        >
          ← 上一頁
        </button>
        
        <div class="pagination__pages">
          <button 
            *ngFor="let page of getPageNumbers()" 
            class="pagination__btn pagination__btn--page" 
            [class.is-active]="page === currentPage"
            (click)="goToPage(page)"
            [attr.aria-label]="'前往第 ' + page + ' 頁'"
            [attr.aria-current]="page === currentPage ? 'page' : null"
          >
            {{ page }}
          </button>
        </div>
        
        <button 
          class="pagination__btn" 
          [disabled]="currentPage === totalPages"
          (click)="goToPage(currentPage + 1)"
          aria-label="前往下一頁"
        >
          下一頁 →
        </button>
        
        <select 
          class="pagination__size-selector" 
          [(ngModel)]="pageSize" 
          (change)="onPageSizeChange()"
          aria-label="選擇每頁顯示筆數"
        >
          <option *ngFor="let size of pageSizeOptions" [value]="size">
            {{ size }} 筆/頁
          </option>
        </select>
      </div>
    </nav>
  `,
  styles: [`
    .pagination {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem;
      border-top: 1px solid #eee;
      flex-wrap: wrap;
      gap: 1rem;
    }

    .pagination__info {
      color: #666;
      font-size: 0.875rem;
    }

    .pagination__controls {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .pagination__pages {
      display: flex;
      gap: 0.25rem;
    }

    .pagination__btn {
      padding: 0.5rem 1rem;
      border: 1px solid #ddd;
      background: white;
      color: #333;
      cursor: pointer;
      border-radius: 4px;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .pagination__btn:hover:not(:disabled) {
      background: #f5f5f5;
      border-color: #999;
    }

    .pagination__btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .pagination__btn--page {
      padding: 0.5rem 0.75rem;
      min-width: 2.5rem;
    }

    .pagination__btn--page.is-active {
      background: #007bff;
      color: white;
      border-color: #007bff;
    }

    .pagination__size-selector {
      padding: 0.5rem;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 0.875rem;
      background: white;
    }
  `]
})
export class PaginationComponent implements OnChanges {
  @Input() currentPage: number = 1;
  @Input() totalItems: number = 0;
  @Input() pageSize: number = 10;
  @Input() pageSizeOptions: number[] = [10, 20, 50];
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  totalPages: number = 0;
  startItem: number = 0;
  endItem: number = 0;

  ngOnChanges() {
    this.calculatePages();
  }

  calculatePages() {
    this.totalPages = Math.ceil(this.totalItems / this.pageSize);
    this.startItem = (this.currentPage - 1) * this.pageSize + 1;
    this.endItem = Math.min(this.currentPage * this.pageSize, this.totalItems);
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    const halfRange = Math.floor(maxPagesToShow / 2);
    
    let start = Math.max(1, this.currentPage - halfRange);
    let end = Math.min(this.totalPages, start + maxPagesToShow - 1);
    
    if (end - start < maxPagesToShow - 1) {
      start = Math.max(1, end - maxPagesToShow + 1);
    }
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    
    return pages;
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.pageChange.emit(page);
    }
  }

  onPageSizeChange() {
    this.pageSizeChange.emit(this.pageSize);
  }
}