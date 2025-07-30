// 關係對話框組件
// 負責處理新建關係的對話框顯示和邏輯

import { Component, Input, Output, EventEmitter, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GraphNode } from '../../../services/relationship-graph.service';

export interface RelationshipDialogData {
  firstNode: GraphNode;
  secondNode: GraphNode;
}

export interface RelationshipCreationData {
  sourcePersonId: number;
  targetPersonId: number;
  relationshipType: string;
  sourceProjectId: string;
  targetProjectId: string;
}

@Component({
  selector: 'app-relationship-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrls: ['../relationship-graph.component.scss'],
  template: `
    <div class="relationship-dialog-overlay" *ngIf="show" (click)="onOverlayClick($event)">
      <div class="relationship-dialog" (click)="$event.stopPropagation()">
        <!-- 對話框標題 -->
        <div class="relationship-dialog-header">
          <h3>建立關係</h3>
          <button class="close-btn" (click)="onClose()" aria-label="關閉">
            ×
          </button>
        </div>

        <!-- 對話框內容 -->
        <div class="relationship-dialog-content" *ngIf="relationshipData">
          <!-- 關係資訊顯示 -->
          <div class="relationship-info">
            <p>
              建立 <strong>{{ relationshipData.firstNode.name || '未知' }}</strong> 
              與 <strong>{{ relationshipData.secondNode.name || '未知' }}</strong> 
              之間的關係
            </p>
          </div>

          <!-- 關係類型輸入表單 -->
          <div class="relationship-form">
            <label for="relationshipType">關係類型：</label>
            <input
              #relationshipInput
              id="relationshipType"
              type="text"
              class="relationship-input"
              [(ngModel)]="relationshipType"
              (keydown)="onKeyDown($event)"
              placeholder="例如：父子、母女、夫妻、朋友等"
              maxlength="50"
              autocomplete="off">
            
            <!-- 建議的關係類型 -->
            <div class="relationship-suggestions" *ngIf="showSuggestions">
              <div 
                class="suggestion-item"
                *ngFor="let suggestion of filteredSuggestions"
                (click)="selectSuggestion(suggestion)"
                [class.active]="suggestion === selectedSuggestion">
                {{ suggestion }}
              </div>
            </div>
          </div>

          <!-- 操作按鈕 -->
          <div class="relationship-actions">
            <button 
              class="btn btn-secondary" 
              (click)="onClose()"
              [disabled]="isProcessing">
              取消
            </button>
            <button 
              class="btn btn-primary" 
              (click)="onConfirm()"
              [disabled]="!isValidRelationship() || isProcessing">
              {{ isProcessing ? '建立中...' : '確認建立' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .relationship-suggestions {
      position: absolute;
      top: 100%;
      left: 0;
      right: 0;
      background: white;
      border: 1px solid #ddd;
      border-top: none;
      border-radius: 0 0 4px 4px;
      max-height: 150px;
      overflow-y: auto;
      z-index: 1000;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .suggestion-item {
      padding: 8px 12px;
      cursor: pointer;
      font-size: 14px;
      border-bottom: 1px solid #f0f0f0;
      transition: background-color 0.2s ease;
    }

    .suggestion-item:hover,
    .suggestion-item.active {
      background-color: #f8f9fa;
    }

    .suggestion-item:last-child {
      border-bottom: none;
    }

    .relationship-form {
      position: relative;
    }
  `]
})
export class RelationshipDialogComponent implements OnInit, AfterViewInit {
  @Input() show = false;
  @Input() relationshipData: RelationshipDialogData | null = null;
  @Input() isProcessing = false;

  @Output() confirmed = new EventEmitter<RelationshipCreationData>();
  @Output() cancelled = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  @ViewChild('relationshipInput') relationshipInput!: ElementRef<HTMLInputElement>;

  relationshipType = '';
  showSuggestions = false;
  selectedSuggestion = '';

  // 建議的關係類型
  relationshipSuggestions = [
    // 家庭關係
    '父子', '父女', '母子', '母女',
    '夫妻', '兄弟', '姐妹', '兄妹', '姐弟',
    '祖父母', '外祖父母', '孫子', '孫女',
    '叔伯', '阿姨', '舅舅', '姑姑',
    '堂兄弟', '堂姐妹', '表兄弟', '表姐妹',
    
    // 社會關係
    '朋友', '同事', '同學', '鄰居',
    '師生', '上下級', '合作夥伴',
    '醫病關係', '客戶關係'
  ];

  filteredSuggestions: string[] = [];

  constructor() {}

  ngOnInit(): void {
    this.resetForm();
  }

  ngAfterViewInit(): void {
    // 對話框顯示時自動焦點到輸入框
    if (this.show && this.relationshipInput) {
      setTimeout(() => {
        this.relationshipInput.nativeElement.focus();
      }, 100);
    }
  }

  onClose(): void {
    this.resetForm();
    this.closed.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    this.onClose();
  }

  onConfirm(): void {
    if (!this.isValidRelationship() || !this.relationshipData || this.isProcessing) return;

    const relationshipCreationData: RelationshipCreationData = {
      sourcePersonId: Number(this.relationshipData.firstNode.id),
      targetPersonId: Number(this.relationshipData.secondNode.id),
      relationshipType: this.relationshipType.trim(),
      sourceProjectId: this.relationshipData.firstNode.projectId || '',
      targetProjectId: this.relationshipData.secondNode.projectId || ''
    };

    this.confirmed.emit(relationshipCreationData);
  }

  onKeyDown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'Enter':
        if (this.showSuggestions && this.selectedSuggestion) {
          this.selectSuggestion(this.selectedSuggestion);
        } else {
          this.onConfirm();
        }
        break;
      
      case 'Escape':
        if (this.showSuggestions) {
          this.hideSuggestions();
        } else {
          this.onClose();
        }
        break;
      
      case 'ArrowDown':
        if (this.showSuggestions) {
          event.preventDefault();
          this.navigateSuggestions(1);
        }
        break;
      
      case 'ArrowUp':
        if (this.showSuggestions) {
          event.preventDefault();
          this.navigateSuggestions(-1);
        }
        break;
      
      default:
        // 輸入時更新建議列表
        setTimeout(() => {
          this.updateSuggestions();
        }, 10);
    }
  }

  selectSuggestion(suggestion: string): void {
    this.relationshipType = suggestion;
    this.hideSuggestions();
    this.relationshipInput.nativeElement.focus();
  }

  private updateSuggestions(): void {
    const input = this.relationshipType.trim().toLowerCase();
    
    if (input.length === 0) {
      this.hideSuggestions();
      return;
    }

    this.filteredSuggestions = this.relationshipSuggestions.filter(suggestion =>
      suggestion.toLowerCase().includes(input)
    );

    if (this.filteredSuggestions.length > 0) {
      this.showSuggestions = true;
      this.selectedSuggestion = this.filteredSuggestions[0];
    } else {
      this.hideSuggestions();
    }
  }

  private navigateSuggestions(direction: number): void {
    if (!this.showSuggestions || this.filteredSuggestions.length === 0) return;

    const currentIndex = this.filteredSuggestions.indexOf(this.selectedSuggestion);
    let newIndex = currentIndex + direction;

    if (newIndex < 0) {
      newIndex = this.filteredSuggestions.length - 1;
    } else if (newIndex >= this.filteredSuggestions.length) {
      newIndex = 0;
    }

    this.selectedSuggestion = this.filteredSuggestions[newIndex];
  }

  private hideSuggestions(): void {
    this.showSuggestions = false;
    this.selectedSuggestion = '';
  }

  isValidRelationship(): boolean {
    return this.relationshipType.trim().length > 0;
  }

  private resetForm(): void {
    this.relationshipType = '';
    this.hideSuggestions();
  }
}