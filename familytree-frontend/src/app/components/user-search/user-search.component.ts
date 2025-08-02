import { Component, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../ui/button/button.component';
import { 
  CSS_CLASSES, 
  USER_STATUS_OPTIONS 
} from '../../constants/user-management.constants';

export interface UserSearchFilters {
  searchAccount: string;
  searchName: string;
  selectedStatus: string;
}

@Component({
  selector: 'app-user-search',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <h2 class="text-lg font-semibold text-gray-900 mb-4">搜尋篩選</h2>
      
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">帳號</label>
          <input
            type="text"
            [(ngModel)]="searchAccount"
            (ngModelChange)="onFilterChange()"
            placeholder="請輸入帳號"
            [class]="CSS_CLASSES.INPUT"
          />
        </div>
        
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">姓名</label>
          <input
            type="text"
            [(ngModel)]="searchName"
            (ngModelChange)="onFilterChange()"
            placeholder="請輸入姓名"
            [class]="CSS_CLASSES.INPUT"
          />
        </div>
        
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">狀態</label>
          <select
            [(ngModel)]="selectedStatus"
            (ngModelChange)="onFilterChange()"
            [class]="CSS_CLASSES.SELECT"
          >
            <option *ngFor="let option of statusOptions" [value]="option.value">
              {{ option.label }}
            </option>
          </select>
        </div>
        
        <div class="flex items-end gap-2">
          <app-button
            variant="primary"
            size="md"
            label="搜尋"
            icon="🔍"
            (clicked)="onSearch()"
            class="flex-1"
          ></app-button>
          <app-button
            variant="secondary"
            size="md"
            label="重置"
            (clicked)="onReset()"
          ></app-button>
        </div>
      </div>
    </div>
  `
})
export class UserSearchComponent {
  readonly CSS_CLASSES = CSS_CLASSES;
  readonly statusOptions = USER_STATUS_OPTIONS;

  @Input() searchAccount = '';
  @Input() searchName = '';
  @Input() selectedStatus = '';

  @Output() filtersChange = new EventEmitter<UserSearchFilters>();
  @Output() search = new EventEmitter<UserSearchFilters>();
  @Output() reset = new EventEmitter<void>();

  onFilterChange(): void {
    this.filtersChange.emit({
      searchAccount: this.searchAccount,
      searchName: this.searchName,
      selectedStatus: this.selectedStatus
    });
  }

  onSearch(): void {
    console.log('🔍 執行搜尋');
    this.search.emit({
      searchAccount: this.searchAccount,
      searchName: this.searchName,
      selectedStatus: this.selectedStatus
    });
  }

  onReset(): void {
    console.log('🔄 重置搜尋條件');
    this.searchAccount = '';
    this.searchName = '';
    this.selectedStatus = '';
    this.reset.emit();
  }
}