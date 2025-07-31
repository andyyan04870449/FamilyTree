import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrgFilter } from '../../services/organization-chart.service';

@Component({
  selector: 'app-org-sidebar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="sidebar-overlay" *ngIf="isOpen" (click)="onClose()"></div>
    <div class="sidebar" [class.open]="isOpen">
      <div class="sidebar-header">
        <h3>搜尋與篩選</h3>
        <button class="close-btn" (click)="onClose()">✕</button>
      </div>
      
      <div class="sidebar-content">
        <div class="search-section">
          <label>搜尋人員</label>
          <input
            type="text"
            class="search-input"
            placeholder="輸入姓名、職位或部門..."
            [(ngModel)]="searchTerm"
            (ngModelChange)="onSearchChange.emit($event)"
          />
        </div>
        
        <div class="filter-section">
          <label>篩選條件</label>
          <div class="filter-list">
            <div class="filter-item" *ngFor="let filter of filters">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  [checked]="filter.checked"
                  (change)="onFilterChange.emit({ filterId: filter.id, checked: $any($event.target).checked })"
                />
                <span>{{ filter.label }}</span>
                <span class="filter-count">({{ filter.count }})</span>
              </label>
            </div>
          </div>
        </div>
        
        <div class="stats-section">
          <h4>統計資訊</h4>
          <div class="stat-item">
            <span class="stat-label">總人數：</span>
            <span class="stat-value">{{ getTotalCount() }}</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">部門數：</span>
            <span class="stat-value">3</span>
          </div>
          <div class="stat-item">
            <span class="stat-label">層級數：</span>
            <span class="stat-value">3</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .sidebar-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      z-index: 99;
    }
    
    .sidebar {
      position: fixed;
      top: 0;
      left: -320px;
      width: 320px;
      height: 100%;
      background: var(--bg-surface);
      border-right: 1px solid var(--border-primary);
      box-shadow: 2px 0 8px rgba(0, 0, 0, 0.1);
      transition: left 0.3s ease;
      z-index: 100;
      
      &.open {
        left: 0;
      }
    }
    
    .sidebar-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px;
      border-bottom: 1px solid var(--border-primary);
      
      h3 {
        margin: 0;
        font-size: 18px;
        font-weight: 600;
        color: var(--text-primary);
      }
      
      .close-btn {
        background: none;
        border: none;
        font-size: 20px;
        color: var(--text-tertiary);
        cursor: pointer;
        padding: 4px 8px;
        border-radius: 4px;
        transition: all 0.2s;
        
        &:hover {
          background: var(--bg-secondary);
          color: var(--text-primary);
        }
      }
    }
    
    .sidebar-content {
      padding: 20px;
      height: calc(100% - 80px);
      overflow-y: auto;
    }
    
    .search-section,
    .filter-section,
    .stats-section {
      margin-bottom: 24px;
      
      label {
        display: block;
        margin-bottom: 8px;
        font-size: 14px;
        font-weight: 500;
        color: var(--text-secondary);
      }
    }
    
    .search-input {
      width: 100%;
      padding: 10px 12px;
      border: 1px solid var(--border-secondary);
      border-radius: 6px;
      background: var(--bg-primary);
      color: var(--text-primary);
      font-size: 14px;
      
      &:focus {
        outline: none;
        border-color: var(--primary);
        box-shadow: 0 0 0 2px rgba(var(--primary-rgb), 0.1);
      }
      
      &::placeholder {
        color: var(--text-tertiary);
      }
    }
    
    .filter-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    
    .filter-item {
      .checkbox-label {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 8px 12px;
        border-radius: 6px;
        cursor: pointer;
        transition: background 0.2s;
        
        &:hover {
          background: var(--bg-secondary);
        }
        
        input[type="checkbox"] {
          width: 16px;
          height: 16px;
          cursor: pointer;
        }
        
        span {
          font-size: 14px;
          color: var(--text-primary);
        }
        
        .filter-count {
          margin-left: auto;
          font-size: 12px;
          color: var(--text-tertiary);
        }
      }
    }
    
    .stats-section {
      h4 {
        margin: 0 0 12px 0;
        font-size: 16px;
        font-weight: 600;
        color: var(--text-primary);
      }
      
      .stat-item {
        display: flex;
        justify-content: space-between;
        padding: 8px 0;
        font-size: 14px;
        
        .stat-label {
          color: var(--text-secondary);
        }
        
        .stat-value {
          font-weight: 600;
          color: var(--primary);
        }
      }
    }
  `]
})
export class OrgSidebarComponent {
  @Input() isOpen = false;
  @Input() searchTerm = '';
  @Input() filters: OrgFilter[] = [];
  
  @Output() close = new EventEmitter<void>();
  @Output() onSearchChange = new EventEmitter<string>();
  @Output() onFilterChange = new EventEmitter<{ filterId: string; checked: boolean }>();
  
  onClose(): void {
    this.close.emit();
  }
  
  getTotalCount(): number {
    return this.filters.find(f => f.id === 'all')?.count || 0;
  }
}