import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { cn } from '../../utils/cn';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div 
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[1000] transition-opacity duration-300"
      *ngIf="isOpen" 
      (click)="onCancel()"
    >
      <div 
        class="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 transform transition-all duration-300 scale-100"
        (click)="$event.stopPropagation()"
      >
        <!-- 標題 -->
        <div class="flex items-center justify-between p-6 border-b border-gray-200">
          <h3 class="text-lg font-semibold text-gray-900">{{ title }}</h3>
          <button 
            class="text-gray-400 hover:text-gray-600 text-2xl leading-none focus:outline-none transition-colors duration-200"
            (click)="onCancel()" 
            type="button" 
            aria-label="關閉對話框"
          >
            ×
          </button>
        </div>
        
        <!-- 內容 -->
        <div class="p-6">
          <p class="text-gray-700 leading-relaxed">{{ message }}</p>
        </div>
        
        <!-- 操作按鈕 -->
        <div class="flex items-center justify-end gap-3 p-6 border-t border-gray-200">
          <button 
            class="px-4 py-2 text-gray-700 bg-gray-100 border border-gray-300 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500 focus:ring-offset-2 transition-colors duration-200"
            (click)="onCancel()"
            type="button"
          >
            {{ cancelText }}
          </button>
          <button 
            class="px-4 py-2 text-white border border-transparent rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 transition-colors duration-200"
            [class]="cn(
              isDanger 
                ? 'bg-red-600 hover:bg-red-700 focus:ring-red-500' 
                : 'bg-blue-600 hover:bg-blue-700 focus:ring-blue-500'
            )"
            (click)="onConfirm()"
            type="button"
          >
            {{ confirmText }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class ConfirmDialogComponent implements OnChanges, OnDestroy {
  // Utility function for class names
  cn = cn;
  
  @Input() isOpen = false;
  @Input() title = '確認';
  @Input() message = '確定要執行此操作嗎？';
  @Input() confirmText = '確認';
  @Input() cancelText = '取消';
  @Input() isDanger = false;
  
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      if (this.isOpen) {
        // 防止背景滾動
        document.body.style.overflow = 'hidden';
      } else {
        // 恢復背景滾動
        document.body.style.overflow = '';
      }
    }
  }
  
  ngOnDestroy(): void {
    // 確保在組件銷毀時恢復背景滾動
    document.body.style.overflow = '';
  }
  
  onConfirm(): void {
    this.confirm.emit();
  }
  
  onCancel(): void {
    this.cancel.emit();
  }
}