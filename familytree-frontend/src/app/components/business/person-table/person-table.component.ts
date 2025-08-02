import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../../ui/button/button.component';
import { LoadingComponent } from '../../ui/loading/loading.component';
import { cn } from '../../../utils/cn';

import {
  PersonTableProps,
  Person,
  PersonTableColumn,
  SortConfig,
  FilterConfig
} from './person-table.interface';

@Component({
  selector: 'app-person-table',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, LoadingComponent],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
      <!-- Table Header -->
      <div class="p-4 border-b border-gray-200 bg-gray-50">
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h3 class="text-lg font-medium text-gray-900">人員列表</h3>
            <p class="text-sm text-gray-600 mt-1">
              共 {{ filteredData.length }} 筆記錄
              <span *ngIf="selectedPersons.length > 0">
                ，已選擇 {{ selectedPersons.length }} 筆
              </span>
            </p>
          </div>
          
          <div class="flex items-center gap-2">
            <!-- Global Filter -->
            <div class="relative" *ngIf="filterable">
              <input
                type="text"
                [(ngModel)]="globalFilter"
                (ngModelChange)="onGlobalFilterChange()"
                placeholder="搜尋人員..."
                class="pl-8 pr-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              >
              <svg class="absolute left-2.5 top-2.5 h-4 w-4 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd" />
              </svg>
            </div>
            
            <!-- Clear Selection -->
            <app-button
              *ngIf="selectable && selectedPersons.length > 0"
              variant="secondary"
              size="sm"
              label="清除選擇"
              (clicked)="clearSelection()"
            ></app-button>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="p-8">
        <app-loading 
          variant="spinner" 
          size="md"
          message="載入人員資料中..."
        ></app-loading>
      </div>

      <!-- Empty State -->
      <div *ngIf="!loading && filteredData.length === 0" class="p-8 text-center">
        <div class="text-gray-400 text-6xl mb-4">👥</div>
        <h3 class="text-lg font-medium text-gray-900 mb-2">沒有找到人員資料</h3>
        <p class="text-gray-600">
          {{ globalFilter ? '請嘗試不同的搜尋條件' : '目前沒有任何人員資料' }}
        </p>
      </div>

      <!-- Table -->
      <div *ngIf="!loading && filteredData.length > 0" class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <!-- Table Header -->
          <thead class="bg-gray-50">
            <tr>
              <!-- Selection Checkbox -->
              <th *ngIf="selectable" class="px-6 py-3 text-left">
                <input
                  type="checkbox"
                  [checked]="isAllSelected"
                  [indeterminate]="isIndeterminate"
                  (change)="toggleAllSelection()"
                  class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                >
              </th>
              
              <!-- Column Headers -->
              <th 
                *ngFor="let column of displayColumns; trackBy: trackByColumn"
                [style.width]="column.width"
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                <div class="flex items-center gap-2">
                  <span>{{ column.label }}</span>
                  
                  <!-- Sort Button -->
                  <button
                    *ngIf="column.sortable && sortable"
                    type="button"
                    (click)="toggleSort(column.key)"
                    class="text-gray-400 hover:text-gray-600 transition-colors"
                  >
                    <svg 
                      class="h-4 w-4" 
                      [class]="getSortIconClasses(column.key)"
                      viewBox="0 0 20 20" 
                      fill="currentColor"
                    >
                      <path d="M10 3L5 8h10l-5-5z" *ngIf="!currentSort || currentSort.column !== column.key"/>
                      <path d="M10 3L5 8h10l-5-5z" *ngIf="currentSort?.column === column.key && currentSort.direction === 'asc'"/>
                      <path d="M10 17l5-5H5l5 5z" *ngIf="currentSort?.column === column.key && currentSort.direction === 'desc'"/>
                    </svg>
                  </button>
                </div>
                
                <!-- Column Filter -->
                <div *ngIf="column.filterable && filterable" class="mt-2">
                  <input
                    type="text"
                    [(ngModel)]="columnFilters[column.key]"
                    (ngModelChange)="onColumnFilterChange()"
                    placeholder="篩選..."
                    class="w-full px-2 py-1 text-xs border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
                  >
                </div>
              </th>
            </tr>
          </thead>
          
          <!-- Table Body -->
          <tbody class="bg-white divide-y divide-gray-200">
            <tr 
              *ngFor="let person of paginatedData; trackBy: trackByPerson; let i = index"
              [class]="getRowClasses(person)"
              (click)="onRowClick(person)"
            >
              <!-- Selection Checkbox -->
              <td *ngIf="selectable" class="px-6 py-4">
                <input
                  type="checkbox"
                  [checked]="isPersonSelected(person)"
                  (change)="togglePersonSelection(person, $event)"
                  (click)="$event.stopPropagation()"
                  class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                >
              </td>
              
              <!-- Data Cells -->
              <td 
                *ngFor="let column of displayColumns; trackBy: trackByColumn"
                class="px-6 py-4 whitespace-nowrap"
              >
                <div [class]="getCellClasses(column)">
                  {{ formatCellValue(person[column.key], column) }}
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div 
        *ngIf="!loading && filteredData.length > 0 && pagination"
        class="px-6 py-3 border-t border-gray-200 bg-gray-50"
      >
        <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div class="flex items-center gap-4">
            <span class="text-sm text-gray-700">
              顯示第 {{ startIndex + 1 }} - {{ endIndex }} 筆，共 {{ filteredData.length }} 筆
            </span>
            
            <div class="flex items-center gap-2">
              <label class="text-sm text-gray-700">每頁顯示:</label>
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
          
          <div class="flex items-center gap-2">
            <app-button
              variant="secondary"
              size="sm"
              label="上一頁"
              [disabled]="currentPage <= 1"
              (clicked)="previousPage()"
            ></app-button>
            
            <span class="text-sm text-gray-700">
              第 {{ currentPage }} 頁，共 {{ totalPages }} 頁
            </span>
            
            <app-button
              variant="secondary"
              size="sm"
              label="下一頁"
              [disabled]="currentPage >= totalPages"
              (clicked)="nextPage()"
            ></app-button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class PersonTableComponent implements OnInit, OnChanges, PersonTableProps {
  @Input() data: Person[] = [];
  @Input() columns?: PersonTableColumn[];
  @Input() loading: boolean = false;
  @Input() pagination: boolean = true;
  @Input() pageSize: number = 25;
  @Input() sortable: boolean = true;
  @Input() filterable: boolean = true;
  @Input() selectable: boolean = false;

  @Output() rowClick = new EventEmitter<Person>();
  @Output() selectionChange = new EventEmitter<Person[]>();

  // Table state
  filteredData: Person[] = [];
  paginatedData: Person[] = [];
  selectedPersons: Person[] = [];
  currentSort: SortConfig | null = null;
  columnFilters: FilterConfig = {};
  globalFilter: string = '';
  
  // Pagination state
  currentPage: number = 1;
  currentPageSize: number = 25;

  // Default columns
  defaultColumns: PersonTableColumn[] = [
    { key: 'id', label: 'ID', sortable: true, filterable: true, width: '80px' },
    { key: 'name', label: '姓名', sortable: true, filterable: true, width: '150px' },
    { key: 'gender', label: '性別', sortable: true, filterable: true, width: '80px' },
    { key: 'birthday', label: '生日', sortable: true, filterable: true, width: '120px', type: 'date' },
    { key: 'nationality', label: '國籍', sortable: true, filterable: true, width: '100px' },
    { key: 'mobile', label: '電話', sortable: true, filterable: true, width: '150px' },
    { 
      key: 'createdAt', 
      label: '創建時間', 
      sortable: true, 
      filterable: false, 
      width: '180px',
      type: 'date',
      formatter: (value: Date) => value ? new Date(value).toLocaleString('zh-TW') : ''
    }
  ];

  constructor() {
    this.currentPageSize = this.pageSize;
  }

  ngOnInit(): void {
    this.initializeData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['data'] || changes['columns']) {
      this.initializeData();
    }
    
    if (changes['pageSize']) {
      this.currentPageSize = this.pageSize;
      this.updatePagination();
    }
  }

  // Getters
  get displayColumns(): PersonTableColumn[] {
    return this.columns || this.defaultColumns;
  }

  get totalPages(): number {
    return Math.ceil(this.filteredData.length / this.currentPageSize);
  }

  get startIndex(): number {
    return (this.currentPage - 1) * this.currentPageSize;
  }

  get endIndex(): number {
    return Math.min(this.startIndex + this.currentPageSize, this.filteredData.length);
  }

  get isAllSelected(): boolean {
    return this.paginatedData.length > 0 && 
           this.paginatedData.every(person => this.isPersonSelected(person));
  }

  get isIndeterminate(): boolean {
    const selectedInPage = this.paginatedData.filter(person => this.isPersonSelected(person));
    return selectedInPage.length > 0 && selectedInPage.length < this.paginatedData.length;
  }

  // Public methods
  onRowClick(person: Person): void {
    this.rowClick.emit(person);
  }

  toggleSort(column: keyof Person): void {
    if (!this.sortable) return;

    if (this.currentSort?.column === column) {
      this.currentSort.direction = this.currentSort.direction === 'asc' ? 'desc' : 'asc';
    } else {
      this.currentSort = { column, direction: 'asc' };
    }

    this.applyFiltersAndSort();
  }

  onGlobalFilterChange(): void {
    this.currentPage = 1;
    this.applyFiltersAndSort();
  }

  onColumnFilterChange(): void {
    this.currentPage = 1;
    this.applyFiltersAndSort();
  }

  onPageSizeChange(): void {
    this.currentPage = 1;
    this.updatePagination();
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updatePagination();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updatePagination();
    }
  }

  togglePersonSelection(person: Person, event: Event): void {
    if (!this.selectable) return;

    const checkbox = event.target as HTMLInputElement;
    
    if (checkbox.checked) {
      if (!this.isPersonSelected(person)) {
        this.selectedPersons.push(person);
      }
    } else {
      this.selectedPersons = this.selectedPersons.filter(p => p.id !== person.id);
    }
    
    this.selectionChange.emit([...this.selectedPersons]);
  }

  toggleAllSelection(): void {
    if (!this.selectable) return;

    if (this.isAllSelected) {
      // Remove all current page items from selection
      this.selectedPersons = this.selectedPersons.filter(
        selected => !this.paginatedData.some(page => page.id === selected.id)
      );
    } else {
      // Add all current page items to selection
      this.paginatedData.forEach(person => {
        if (!this.isPersonSelected(person)) {
          this.selectedPersons.push(person);
        }
      });
    }
    
    this.selectionChange.emit([...this.selectedPersons]);
  }

  clearSelection(): void {
    this.selectedPersons = [];
    this.selectionChange.emit([]);
  }

  isPersonSelected(person: Person): boolean {
    return this.selectedPersons.some(selected => selected.id === person.id);
  }

  formatCellValue(value: any, column: PersonTableColumn): string {
    if (value === null || value === undefined) {
      return '';
    }

    if (column.formatter) {
      return column.formatter(value);
    }

    if (column.type === 'date' && value instanceof Date) {
      return value.toLocaleDateString('zh-TW');
    }

    return String(value);
  }

  getSortIconClasses(column: keyof Person): string {
    const baseClasses = 'transition-colors';
    
    if (!this.currentSort || this.currentSort.column !== column) {
      return cn(baseClasses, 'text-gray-400');
    }
    
    return cn(baseClasses, 'text-blue-600');
  }

  getRowClasses(person: Person): string {
    return cn(
      'hover:bg-gray-50 transition-colors cursor-pointer',
      {
        'bg-blue-50 hover:bg-blue-100': this.isPersonSelected(person)
      }
    );
  }

  getCellClasses(column: PersonTableColumn): string {
    return cn(
      'text-sm',
      {
        'text-gray-900 font-medium': column.key === 'name',
        'text-gray-500': column.key !== 'name'
      }
    );
  }

  trackByPerson(index: number, person: Person): string {
    return person.id;
  }

  trackByColumn(index: number, column: PersonTableColumn): string {
    return column.key;
  }

  // Private methods
  private initializeData(): void {
    this.currentPage = 1;
    this.applyFiltersAndSort();
  }

  private applyFiltersAndSort(): void {
    let data = [...this.data];

    // Apply global filter
    if (this.globalFilter) {
      const filter = this.globalFilter.toLowerCase();
      data = data.filter(person => 
        Object.values(person).some(value => 
          String(value).toLowerCase().includes(filter)
        )
      );
    }

    // Apply column filters
    Object.entries(this.columnFilters).forEach(([key, filter]) => {
      if (filter) {
        const filterValue = filter.toLowerCase();
        data = data.filter(person => 
          String(person[key as keyof Person]).toLowerCase().includes(filterValue)
        );
      }
    });

    // Apply sorting
    if (this.currentSort) {
      data.sort((a, b) => {
        const aValue = a[this.currentSort!.column];
        const bValue = b[this.currentSort!.column];
        
        if (aValue === bValue) return 0;
        
        const result = aValue < bValue ? -1 : 1;
        return this.currentSort!.direction === 'asc' ? result : -result;
      });
    }

    this.filteredData = data;
    this.updatePagination();
  }

  private updatePagination(): void {
    if (!this.pagination) {
      this.paginatedData = this.filteredData;
      return;
    }

    const start = this.startIndex;
    const end = this.endIndex;
    this.paginatedData = this.filteredData.slice(start, end);
  }
}