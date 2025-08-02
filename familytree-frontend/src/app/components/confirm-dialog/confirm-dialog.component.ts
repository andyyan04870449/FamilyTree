import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrls: ['./confirm-dialog.component.scss']
})
export class ConfirmDialogComponent implements OnChanges, OnDestroy {
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