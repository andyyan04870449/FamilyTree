// 合併對話框組件
// 負責處理人員合併確認對話框的顯示和邏輯

import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { GraphNode } from '../../../services/relationship-graph.service';
import { PhotoUtilsService } from '../../../services/photo-utils.service';

export interface MergeDialogData {
  personA: GraphNode;
  personB: GraphNode;
}

export interface MergeSelection {
  primaryPersonId: number;
  secondaryPersonId: number;
  primaryProjectId: string;
  secondaryProjectId: string;
}

@Component({
  selector: 'app-merge-dialog',
  standalone: true,
  imports: [CommonModule],
  styleUrls: ['../relationship-graph.component.scss'],
  template: `
    <div class="dialog-overlay" *ngIf="show" (click)="onOverlayClick($event)">
      <div class="merge-confirm-dialog" (click)="$event.stopPropagation()">
        <!-- 對話框標題 -->
        <div class="dialog-header">
          <h3>確認合併人員</h3>
          <button class="close-btn" (click)="onClose()" aria-label="關閉">
            ×
          </button>
        </div>

        <!-- 合併內容 -->
        <div class="merge-content" *ngIf="mergeData">
          <!-- 合併預覽 -->
          <div class="merge-preview">
            <!-- 人員 A -->
            <div class="person-card person-a">
              <div class="person-photo-container">
                <img 
                  *ngIf="getPersonPhotoUrl(mergeData.personA.photoIndex, mergeData.personA.name, mergeData.personA.projectId)"
                  [src]="getPersonPhotoUrl(mergeData.personA.photoIndex, mergeData.personA.name, mergeData.personA.projectId)"
                  class="person-photo"
                  (error)="handlePhotoError($event)"
                  [alt]="mergeData.personA.name || '照片'">
                <div *ngIf="!getPersonPhotoUrl(mergeData.personA.photoIndex, mergeData.personA.name, mergeData.personA.projectId)" 
                     class="person-fallback">
                  {{ generateFallbackText(mergeData.personA.name) }}
                </div>
              </div>
              <div class="person-details">
                <h4>{{ mergeData.personA.name || '未知姓名' }}</h4>
                <div class="person-id">ID: {{ mergeData.personA.id }}</div>
                <div class="person-info">
                  <div *ngIf="mergeData.personA.gender">性別: {{ getGenderText(mergeData.personA.gender) }}</div>
                  <div *ngIf="mergeData.personA.birthday">生日: {{ mergeData.personA.birthday }}</div>
                  <div *ngIf="mergeData.personA.projectId">專案: {{ mergeData.personA.projectId }}</div>
                </div>
              </div>
            </div>

            <!-- 合併方向指示 -->
            <div class="merge-direction">
              <div class="merge-arrow">
                <svg width="24" height="24" viewBox="0 0 24 24" fill="white">
                  <path d="M8.59 16.59L13.17 12L8.59 7.41L10 6l6 6-6 6-1.41-1.41z"/>
                </svg>
              </div>
              <div class="merge-label">合併</div>
            </div>

            <!-- 人員 B -->
            <div class="person-card person-b">
              <div class="person-photo-container">
                <img 
                  *ngIf="getPersonPhotoUrl(mergeData.personB.photoIndex, mergeData.personB.name, mergeData.personB.projectId)"
                  [src]="getPersonPhotoUrl(mergeData.personB.photoIndex, mergeData.personB.name, mergeData.personB.projectId)"
                  class="person-photo"
                  (error)="handlePhotoError($event)"
                  [alt]="mergeData.personB.name || '照片'">
                <div *ngIf="!getPersonPhotoUrl(mergeData.personB.photoIndex, mergeData.personB.name, mergeData.personB.projectId)" 
                     class="person-fallback">
                  {{ generateFallbackText(mergeData.personB.name) }}
                </div>
              </div>
              <div class="person-details">
                <h4>{{ mergeData.personB.name || '未知姓名' }}</h4>
                <div class="person-id">ID: {{ mergeData.personB.id }}</div>
                <div class="person-info">
                  <div *ngIf="mergeData.personB.gender">性別: {{ getGenderText(mergeData.personB.gender) }}</div>
                  <div *ngIf="mergeData.personB.birthday">生日: {{ mergeData.personB.birthday }}</div>
                  <div *ngIf="mergeData.personB.projectId">專案: {{ mergeData.personB.projectId }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- 警告資訊 -->
          <div class="merge-warning">
            <div class="warning-icon">⚠️</div>
            <div class="warning-content">
              <h5>重要提醒</h5>
              <p>合併操作將無法復原。請仔細確認這兩個人員記錄確實為同一人。</p>
            </div>
          </div>
        </div>

        <!-- 操作按鈕 -->
        <div class="dialog-actions">
          <button class="btn btn-secondary" (click)="onClose()">
            <span class="btn-icon">✕</span>
            取消
          </button>
          <button class="btn btn-primary" (click)="onConfirmMerge()" [disabled]="isProcessing">
            <span class="btn-icon">🔗</span>
            {{ isProcessing ? '處理中...' : '確認合併' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: []
})
export class MergeDialogComponent implements OnInit {
  @Input() show = false;
  @Input() mergeData: MergeDialogData | null = null;
  @Input() isProcessing = false;

  @Output() confirmed = new EventEmitter<MergeSelection>();
  @Output() cancelled = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  constructor(
    private photoUtils: PhotoUtilsService
  ) {}

  ngOnInit(): void {}

  onClose(): void {
    this.closed.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    // 點擊遮罩層關閉對話框
    this.onClose();
  }

  onConfirmMerge(): void {
    if (!this.mergeData || this.isProcessing) return;

    const mergeSelection: MergeSelection = {
      primaryPersonId: Number(this.mergeData.personA.id),
      secondaryPersonId: Number(this.mergeData.personB.id),
      primaryProjectId: this.mergeData.personA.projectId || '',
      secondaryProjectId: this.mergeData.personB.projectId || ''
    };

    this.confirmed.emit(mergeSelection);
  }

  getPersonPhotoUrl(photoIndex: string | null | undefined, personName?: string, projectId?: string): string | null {
    return this.photoUtils.getPersonPhotoUrl(photoIndex, personName, projectId);
  }

  generateFallbackText(personName?: string): string {
    if (!personName) return '?';
    
    // 取姓名的第一個字
    return personName.charAt(0).toUpperCase();
  }

  handlePhotoError(event: Event): void {
    const imgElement = event.target as HTMLImageElement;
    if (imgElement) {
      imgElement.style.display = 'none';
      
      // 顯示 fallback 元素
      const container = imgElement.parentElement;
      if (container) {
        const fallback = container.querySelector('.person-fallback') as HTMLElement;
        if (fallback) {
          fallback.style.display = 'flex';
        }
      }
    }
  }

  getGenderText(gender: string): string {
    switch (gender?.toLowerCase()) {
      case 'm':
      case 'male':
        return '男';
      case 'f':
      case 'female':
        return '女';
      default:
        return '未知';
    }
  }
}