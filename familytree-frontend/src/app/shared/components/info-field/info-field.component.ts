import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-info-field',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="info-field" [class.info-field--full-width]="fullWidth">
      <label class="info-field__label" [for]="fieldId">
        {{ label }}
        <span *ngIf="required && isEditMode" class="info-field__required" aria-label="必填欄位">*</span>
      </label>
      
      <!-- 編輯模式 -->
      <ng-container *ngIf="isEditMode">
        <!-- 文字輸入 -->
        <input 
          *ngIf="type === 'text' || type === 'tel' || type === 'email' || type === 'date'"
          [id]="fieldId"
          [type]="type"
          [ngModel]="value"
          (ngModelChange)="onValueChange($event)"
          [required]="required"
          [attr.aria-required]="required"
          class="info-field__input"
          [attr.aria-label]="label"
        >
        
        <!-- 選擇框 -->
        <select 
          *ngIf="type === 'select'"
          [id]="fieldId"
          [ngModel]="value"
          (ngModelChange)="onValueChange($event)"
          [required]="required"
          [attr.aria-required]="required"
          class="info-field__select"
          [attr.aria-label]="label"
        >
          <option value="">請選擇</option>
          <option *ngFor="let option of options" [value]="option.value">
            {{ option.label }}
          </option>
        </select>
        
        <!-- 文字區域 -->
        <textarea 
          *ngIf="type === 'textarea'"
          [id]="fieldId"
          [ngModel]="value"
          (ngModelChange)="onValueChange($event)"
          [rows]="rows || 2"
          [required]="required"
          [attr.aria-required]="required"
          class="info-field__textarea"
          [attr.aria-label]="label"
        ></textarea>
      </ng-container>
      
      <!-- 顯示模式 -->
      <span *ngIf="!isEditMode" class="info-field__value">
        {{ displayValue || value || '--' }}
      </span>
    </div>
  `,
  styleUrls: ['./info-field.component.scss']
})
export class InfoFieldComponent {
  @Input() label!: string;
  @Input() value: any;
  @Output() valueChange = new EventEmitter<any>();
  @Input() displayValue?: string;
  @Input() type: 'text' | 'tel' | 'email' | 'date' | 'select' | 'textarea' = 'text';
  @Input() isEditMode = false;
  @Input() required = false;
  @Input() fullWidth = false;
  @Input() options?: Array<{ value: string; label: string }>;
  @Input() rows?: number;
  @Input() fieldId = `field-${Math.random().toString(36).substr(2, 9)}`;

  onValueChange(newValue: any): void {
    this.value = newValue;
    this.valueChange.emit(newValue);
  }
}