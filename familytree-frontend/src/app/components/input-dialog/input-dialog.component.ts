import { Component, Input, Output, EventEmitter, OnChanges, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-input-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './input-dialog.component.html',
  styleUrls: ['./input-dialog.component.scss']
})
export class InputDialogComponent implements OnChanges, AfterViewInit {
  @Input() isOpen = false;
  @Input() title = '請輸入';
  @Input() label = '名稱';
  @Input() placeholder = '請輸入...';
  @Input() initialValue = '';
  @Input() confirmText = '確認';
  @Input() cancelText = '取消';
  
  @Output() confirm = new EventEmitter<string>();
  @Output() cancel = new EventEmitter<void>();
  
  @ViewChild('inputField', { static: false }) inputField!: ElementRef<HTMLInputElement>;
  
  inputValue = '';
  private isInitialized = false;
  
  ngOnChanges(): void {
    if (this.isOpen) {
      this.inputValue = this.initialValue;
      this.focusInput();
    }
  }

  ngAfterViewInit(): void {
    this.isInitialized = true;
    if (this.isOpen) {
      this.focusInput();
    }
  }

  private focusInput(): void {
    if (!this.isInitialized || !this.inputField) {
      // 如果組件還未初始化或輸入框不存在，稍後重試
      setTimeout(() => this.focusInput(), 50);
      return;
    }

    setTimeout(() => {
      if (this.inputField?.nativeElement) {
        this.inputField.nativeElement.focus();
        this.inputField.nativeElement.select();
      }
    }, 100);
  }
  
  onConfirm(): void {
    if (this.inputValue && this.inputValue.trim()) {
      this.confirm.emit(this.inputValue.trim());
    }
  }
  
  onCancel(): void {
    this.cancel.emit();
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.onCancel();
    } else if (event.key === 'Enter' && this.inputValue && this.inputValue.trim()) {
      event.preventDefault();
      this.onConfirm();
    }
  }

  onOverlayClick(event: MouseEvent): void {
    // 只有當點擊的是覆蓋層本身時才關閉對話框
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }
}