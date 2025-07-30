// 模式指示器組件
// 顯示當前圖表操作模式（關係建立模式、合併模式等）

import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GraphMode } from '../services/graph-state.service';

@Component({
  selector: 'app-mode-indicator',
  standalone: true,
  imports: [CommonModule],
  styleUrls: ['../relationship-graph.component.scss'],
  template: `
    <!-- 關係建立模式指示器 -->
    <div 
      class="relationship-mode-indicator animate-slide-in-top"
      *ngIf="mode === 'create-relationship'">
      <div class="indicator-content">
        <span class="indicator-icon">🔗</span>
        <span class="indicator-text">{{ getModeText() }}</span>
        <button class="cancel-btn" (click)="onCancel()">
          取消
        </button>
      </div>
    </div>

    <!-- 合併模式指示器 -->
    <div 
      class="comparison-mode-indicator animate-slide-in-top"
      *ngIf="mode === 'merge'">
      <div class="indicator-content">
        <span class="indicator-icon">🔄</span>
        <span class="indicator-text">{{ getModeText() }}</span>
        <button class="cancel-btn" (click)="onCancel()">
          取消
        </button>
      </div>
    </div>
  `,
  styles: []
})
export class ModeIndicatorComponent implements OnInit {
  @Input() mode: GraphMode | null = null;
  @Input() selectedCount = 0;
  @Input() maxSelections = 2;

  @Output() cancelled = new EventEmitter<void>();

  constructor() {}

  ngOnInit(): void {}

  onCancel(): void {
    this.cancelled.emit();
  }

  getModeText(): string {
    switch (this.mode) {
      case 'create-relationship':
        return this.getRelationshipModeText();
      case 'merge':
        return this.getMergeModeText();
      default:
        return '';
    }
  }

  private getRelationshipModeText(): string {
    if (this.selectedCount === 0) {
      return '請選擇第一個人員來建立關係';
    } else if (this.selectedCount === 1) {
      return '請選擇第二個人員來建立關係';
    } else {
      return '已選擇兩個人員，可以建立關係';
    }
  }

  private getMergeModeText(): string {
    if (this.selectedCount === 0) {
      return '請選擇第一個要合併的人員';
    } else if (this.selectedCount === 1) {
      return '請選擇第二個要合併的人員';
    } else {
      return '已選擇兩個人員，可以進行合併';
    }
  }

  getProgressPercentage(): number {
    return Math.min((this.selectedCount / this.maxSelections) * 100, 100);
  }

  isCompleted(): boolean {
    return this.selectedCount >= this.maxSelections;
  }
}