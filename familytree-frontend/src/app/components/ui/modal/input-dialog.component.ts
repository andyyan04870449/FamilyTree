import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from './modal.component';
import { ButtonComponent } from '../button/button.component';
import { InputDialogProps } from './modal.interface';

@Component({
  selector: 'app-input-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent, ButtonComponent],
  template: `
    <app-modal
      [isOpen]="isOpen"
      [title]="title"
      [size]="size"
      [showCloseButton]="true"
      [closeOnBackdropClick]="true"
      [closeOnEscape]="true"
      (closed)="onCancel()"
    >
      <!-- Dialog Content -->
      <div class="space-y-4">
        <ng-content></ng-content>
        
        <div *ngIf="inputLabel || inputPlaceholder">
          <label 
            *ngIf="inputLabel"
            [for]="inputId"
            class="block text-sm font-medium text-gray-700 mb-2"
          >
            {{ inputLabel }}
            <span *ngIf="required" class="text-red-500 ml-1">*</span>
          </label>
          
          <input
            [id]="inputId"
            [type]="inputType"
            [(ngModel)]="currentValue"
            [placeholder]="inputPlaceholder"
            [required]="required"
            class="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            (keydown.enter)="onSubmit()"
            (keydown.escape)="onCancel()"
            #inputElement
          >
          
          <div 
            *ngIf="required && showValidation && !currentValue?.trim()"
            class="mt-1 text-sm text-red-600"
          >
            此欄位為必填
          </div>
        </div>
      </div>

      <!-- Dialog Actions -->
      <div slot="footer" class="flex gap-3 justify-end">
        <app-button
          variant="secondary"
          [label]="cancelText"
          (clicked)="onCancel()"
        ></app-button>
        
        <app-button
          variant="primary"
          [label]="submitText"
          [disabled]="required && !currentValue?.trim()"
          (clicked)="onSubmit()"
        ></app-button>
      </div>
    </app-modal>
  `
})
export class InputDialogComponent implements OnInit, InputDialogProps {
  @Input() isOpen: boolean = false;
  @Input() size: InputDialogProps['size'] = 'md';
  @Input() title: string = '輸入';
  @Input() inputLabel?: string;
  @Input() inputPlaceholder?: string;
  @Input() inputType: InputDialogProps['inputType'] = 'text';
  @Input() inputValue?: string;
  @Input() submitText: string = '確認';
  @Input() cancelText: string = '取消';
  @Input() required: boolean = false;
  @Input() showCloseButton: boolean = true;
  @Input() closeOnBackdropClick: boolean = true;
  @Input() closeOnEscape: boolean = true;
  @Input() preventBodyScroll: boolean = true;
  @Input() centered: boolean = true;

  @Output() submit = new EventEmitter<string>();
  @Output() cancel = new EventEmitter<void>();

  currentValue: string = '';
  showValidation: boolean = false;
  
  // Unique ID for the input
  inputId = `input-${Math.random().toString(36).substr(2, 9)}`;

  ngOnInit(): void {
    this.currentValue = this.inputValue || '';
  }

  onSubmit(): void {
    this.showValidation = true;
    
    if (this.required && !this.currentValue?.trim()) {
      return;
    }
    
    this.submit.emit(this.currentValue);
    this.showValidation = false;
  }

  onCancel(): void {
    this.cancel.emit();
    this.showValidation = false;
  }
}