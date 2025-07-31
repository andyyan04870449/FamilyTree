import { Component, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-input-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dialog-overlay" *ngIf="isOpen" (click)="onCancel()">
      <div class="dialog" (click)="$event.stopPropagation()">
        <div class="dialog-header">
          <h3>{{ title }}</h3>
          <button class="close-btn" (click)="onCancel()">×</button>
        </div>
        
        <div class="dialog-content">
          <div class="form-group">
            <label>{{ label }}</label>
            <input 
              type="text" 
              [(ngModel)]="inputValue"
              [placeholder]="placeholder"
              (keyup.enter)="onConfirm()"
              #inputField
            />
          </div>
        </div>
        
        <div class="dialog-actions">
          <button class="btn-cancel" (click)="onCancel()">{{ cancelText }}</button>
          <button 
            class="btn-confirm" 
            (click)="onConfirm()"
            [disabled]="!inputValue || !inputValue.trim()"
          >
            {{ confirmText }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      position: relative;
      z-index: 1000;
    }
  `]
})
export class InputDialogComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() title = '請輸入';
  @Input() label = '名稱';
  @Input() placeholder = '請輸入...';
  @Input() initialValue = '';
  @Input() confirmText = '確認';
  @Input() cancelText = '取消';
  
  @Output() confirm = new EventEmitter<string>();
  @Output() cancel = new EventEmitter<void>();
  
  inputValue = '';
  
  ngOnChanges(): void {
    if (this.isOpen) {
      this.inputValue = this.initialValue;
      // 自動聚焦輸入框
      setTimeout(() => {
        const input = document.querySelector('input[type="text"]') as HTMLInputElement;
        if (input) {
          input.focus();
          input.select();
        }
      }, 100);
    }
  }
  
  onConfirm(): void {
    if (this.inputValue && this.inputValue.trim()) {
      this.confirm.emit(this.inputValue.trim());
    }
  }
  
  onCancel(): void {
    this.cancel.emit();
  }
}