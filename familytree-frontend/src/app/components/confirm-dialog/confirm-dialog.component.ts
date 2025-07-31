import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dialog-overlay" *ngIf="isOpen" (click)="onCancel()">
      <div class="dialog" (click)="$event.stopPropagation()">
        <div class="dialog-header">
          <h3>{{ title }}</h3>
          <button class="close-btn" (click)="onCancel()">×</button>
        </div>
        
        <div class="dialog-content">
          <p>{{ message }}</p>
        </div>
        
        <div class="dialog-actions">
          <button class="btn-cancel" (click)="onCancel()">{{ cancelText }}</button>
          <button 
            class="btn-confirm" 
            [class.btn-danger]="isDanger"
            (click)="onConfirm()"
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
export class ConfirmDialogComponent {
  @Input() isOpen = false;
  @Input() title = '確認';
  @Input() message = '確定要執行此操作嗎？';
  @Input() confirmText = '確認';
  @Input() cancelText = '取消';
  @Input() isDanger = false;
  
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  
  onConfirm(): void {
    this.confirm.emit();
  }
  
  onCancel(): void {
    this.cancel.emit();
  }
}