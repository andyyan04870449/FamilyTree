import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrgNode } from '../../services/organization-chart.service';

@Component({
  selector: 'app-org-node',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div 
      class="org-node-card"
      [class.selected]="isSelected"
      [style.left.px]="node.x"
      [style.top.px]="node.y"
      [style.transform]="isDragging ? 'scale(1.05)' : 'scale(1)'"
      (mousedown)="onMouseDown($event)"
    >
      <div class="node-content">
        <div class="node-avatar">
          <img *ngIf="node.avatar" [src]="node.avatar" [alt]="node.name" />
          <div *ngIf="!node.avatar" class="avatar-placeholder">
            {{ getInitials(node.name) }}
          </div>
        </div>
        <div class="node-info">
          <div class="node-header">
            <h3>{{ node.name }}</h3>
            <div class="node-actions">
              <button 
                class="action-btn edit-btn"
                (click)="onEdit($event)"
                title="編輯"
              >
                ✏️
              </button>
              <button 
                class="action-btn delete-btn"
                (click)="onDelete($event)"
                title="刪除"
              >
                ❌
              </button>
            </div>
          </div>
          <p class="node-position">{{ node.position }}</p>
          <p class="node-department">{{ node.department }}</p>
          <p class="node-phone" *ngIf="node.phone">📞 {{ node.phone }}</p>
          <p class="node-date">到職：{{ node.joinDate }}</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .org-node-card {
      position: absolute;
      min-width: 260px;
      background: var(--bg-surface);
      border: 1px solid var(--border-primary);
      border-radius: 8px;
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
      cursor: move;
      transition: all 0.2s ease;
      z-index: 2; /* 確保在連接線上方 */
      
      &:hover {
        box-shadow: 0 6px 12px rgba(0, 0, 0, 0.15);
      }
      
      &.selected {
        border-color: var(--primary);
        box-shadow: 0 0 0 2px rgba(var(--primary-rgb), 0.2);
      }
    }
    
    .node-content {
      display: flex;
      gap: 12px;
      padding: 16px;
    }
    
    .node-avatar {
      width: 50px;
      height: 50px;
      border-radius: 50%;
      overflow: hidden;
      flex-shrink: 0;
      background: var(--bg-secondary);
      border: 2px solid var(--border-secondary);
      
      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
      }
      
      .avatar-placeholder {
        width: 100%;
        height: 100%;
        display: flex;
        align-items: center;
        justify-content: center;
        font-weight: 600;
        font-size: 18px;
        color: var(--text-primary);
        background: var(--primary);
        color: white;
      }
    }
    
    .node-info {
      flex: 1;
      min-width: 0;
    }
    
    .node-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 4px;
      
      h3 {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
        color: var(--text-primary);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      
      .node-actions {
        display: flex;
        gap: 4px;
        opacity: 0;
        transition: opacity 0.2s;
      }
    }
    
    .org-node-card:hover .node-actions {
      opacity: 1;
    }
    
    .action-btn {
      background: none;
      border: none;
      cursor: pointer;
      padding: 4px;
      border-radius: 4px;
      transition: all 0.2s;
      font-size: 12px;
      
      &:hover {
        background: var(--bg-secondary);
      }
      
      &.edit-btn:hover {
        background: var(--warning);
      }
      
      &.delete-btn:hover {
        background: var(--error);
      }
    }
    
    .node-position {
      margin: 0 0 2px 0;
      font-size: 14px;
      font-weight: 500;
      color: var(--primary);
    }
    
    .node-department {
      margin: 0 0 4px 0;
      font-size: 13px;
      color: var(--text-secondary);
    }
    
    .node-phone,
    .node-date {
      margin: 0;
      font-size: 12px;
      color: var(--text-tertiary);
    }
  `]
})
export class OrgNodeComponent {
  @Input() node!: OrgNode;
  @Input() isSelected = false;
  @Input() isDragging = false;
  
  @Output() select = new EventEmitter<string>();
  @Output() edit = new EventEmitter<string>();
  @Output() delete = new EventEmitter<string>();
  @Output() dragStart = new EventEmitter<{ event: MouseEvent; nodeId: string }>();
  
  onMouseDown(event: MouseEvent): void {
    event.preventDefault();
    this.select.emit(this.node.id);
    this.dragStart.emit({ event, nodeId: this.node.id });
  }
  
  onEdit(event: MouseEvent): void {
    event.stopPropagation();
    this.edit.emit(this.node.id);
  }
  
  onDelete(event: MouseEvent): void {
    event.stopPropagation();
    this.delete.emit(this.node.id);
  }
  
  getInitials(name: string): string {
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return parts[0].charAt(0) + parts[1].charAt(0);
    }
    return name.substring(0, 2).toUpperCase();
  }
}