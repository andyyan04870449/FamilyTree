// 節點選單組件
// 負責顯示右鍵點擊節點時的操作選單

import { Component, Input, Output, EventEmitter, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GraphNode } from '../../../services/relationship-graph.service';

export interface NodeMenuAction {
  type: 'view-data' | 'create-relationship' | 'org-chart' | 'merge';
  data?: any;
}

export interface MenuPosition {
  x: number;
  y: number;
}

@Component({
  selector: 'app-node-menu',
  standalone: true,
  imports: [CommonModule],
  styleUrls: ['../relationship-graph.component.scss'],
  template: `
    <div class="node-menu-overlay" *ngIf="show" (click)="onClose()">
      <div 
        class="node-menu"
        [class.dragging]="isDragging"
        [style.left.px]="position.x"
        [style.top.px]="position.y"
        (click)="$event.stopPropagation()"
        (mousedown)="startMenuDrag($event)">
        
        <!-- 選單標題 -->
        <div class="node-menu-header">
          <h4>{{ selectedNode?.name || '操作選單' }}</h4>
          <button class="close-btn" (click)="onClose()" aria-label="關閉">
            ×
          </button>
        </div>

        <!-- 選單內容 -->
        <div class="node-menu-content" *ngIf="selectedNode">
          <div class="node-menu-actions">
            <!-- 查看詳細資料 -->
            <button 
              class="node-menu-btn"
              (click)="onAction('view-data')"
              style="background: #e3f2fd; color: #1976d2;">
              <span class="btn-icon">👁️</span>
              <span class="btn-text">查看詳細資料</span>
            </button>

            <!-- 建立關係 -->
            <button 
              class="node-menu-btn"
              (click)="onAction('create-relationship')"
              style="background: #f3e5f5; color: #7b1fa2;">
              <span class="btn-icon">🔗</span>
              <span class="btn-text">建立關係</span>
            </button>

            <!-- 組織圖編輯 -->
            <button 
              class="node-menu-btn"
              (click)="onAction('org-chart')"
              style="background: #e8f5e8; color: #388e3c;">
              <span class="btn-icon">📊</span>
              <span class="btn-text">組織圖編輯</span>
            </button>

            <!-- 合併處理 -->
            <button 
              class="node-menu-btn"
              (click)="onAction('merge')"
              style="background: #fff3e0; color: #f57c00;">
              <span class="btn-icon">🔄</span>
              <span class="btn-text">合併處理</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: []
})
export class NodeMenuComponent implements OnInit {
  @Input() show = false;
  @Input() selectedNode: GraphNode | null = null;
  @Input() position: MenuPosition = { x: 0, y: 0 };

  @Output() actionSelected = new EventEmitter<NodeMenuAction>();
  @Output() closed = new EventEmitter<void>();

  isDragging = false;
  dragOffset = { x: 0, y: 0 };

  constructor() {}

  ngOnInit(): void {}

  onClose(): void {
    this.closed.emit();
  }

  onAction(actionType: NodeMenuAction['type']): void {
    if (!this.selectedNode) return;

    const action: NodeMenuAction = {
      type: actionType,
      data: {
        node: this.selectedNode
      }
    };

    this.actionSelected.emit(action);
    this.onClose();
  }

  startMenuDrag(event: MouseEvent): void {
    // 只有在標題區域才能拖曳
    const target = event.target as HTMLElement;
    if (!target.closest('.node-menu-header')) return;

    event.preventDefault();
    event.stopPropagation();

    this.isDragging = true;
    this.dragOffset.x = event.clientX - this.position.x;
    this.dragOffset.y = event.clientY - this.position.y;

    // 添加全域滑鼠事件監聽器
    document.addEventListener('mousemove', this.onMenuDrag);
    document.addEventListener('mouseup', this.stopMenuDrag);
  }

  @HostListener('document:mousemove', ['$event'])
  onMenuDrag = (event: MouseEvent): void => {
    if (!this.isDragging) return;

    event.preventDefault();

    // 計算新位置
    const newX = event.clientX - this.dragOffset.x;
    const newY = event.clientY - this.dragOffset.y;

    // 確保選單不會移出視窗邊界
    const menuWidth = 180; // 大約的選單寬度
    const menuHeight = 200; // 大約的選單高度
    const maxX = window.innerWidth - menuWidth;
    const maxY = window.innerHeight - menuHeight;

    this.position = {
      x: Math.max(0, Math.min(newX, maxX)),
      y: Math.max(0, Math.min(newY, maxY))
    };
  };

  @HostListener('document:mouseup', ['$event'])
  stopMenuDrag = (event: MouseEvent): void => {
    if (!this.isDragging) return;

    this.isDragging = false;
    
    // 移除全域滑鼠事件監聽器
    document.removeEventListener('mousemove', this.onMenuDrag);
    document.removeEventListener('mouseup', this.stopMenuDrag);
  };

  // 檢查選單是否超出視窗邊界並調整位置
  adjustPosition(): void {
    const menuWidth = 180;
    const menuHeight = 200;
    const padding = 10;

    let adjustedX = this.position.x;
    let adjustedY = this.position.y;

    // 檢查右邊界
    if (adjustedX + menuWidth > window.innerWidth - padding) {
      adjustedX = window.innerWidth - menuWidth - padding;
    }

    // 檢查左邊界
    if (adjustedX < padding) {
      adjustedX = padding;
    }

    // 檢查下邊界
    if (adjustedY + menuHeight > window.innerHeight - padding) {
      adjustedY = window.innerHeight - menuHeight - padding;
    }

    // 檢查上邊界
    if (adjustedY < padding) {
      adjustedY = padding;
    }

    this.position = { x: adjustedX, y: adjustedY };
  }

  // 根據滑鼠位置計算最佳選單位置
  calculateOptimalPosition(mouseX: number, mouseY: number): MenuPosition {
    const menuWidth = 180;
    const menuHeight = 200;
    const offset = 10;

    let x = mouseX + offset;
    let y = mouseY;

    // 如果選單會超出右邊界，則顯示在滑鼠左側
    if (x + menuWidth > window.innerWidth - offset) {
      x = mouseX - menuWidth - offset;
    }

    // 如果選單會超出下邊界，則向上調整
    if (y + menuHeight > window.innerHeight - offset) {
      y = window.innerHeight - menuHeight - offset;
    }

    // 確保不會超出左邊界和上邊界
    x = Math.max(offset, x);
    y = Math.max(offset, y);

    return { x, y };
  }
}