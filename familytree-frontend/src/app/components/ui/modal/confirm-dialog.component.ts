import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from './modal.component';
import { ButtonComponent } from '../button/button.component';
import { ConfirmDialogProps } from './modal.interface';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, ModalComponent, ButtonComponent],
  template: `
    <app-modal
      [isOpen]="isOpen"
      [title]="title"
      [variant]="isDanger ? 'danger' : 'default'"
      [size]="size"
      [showCloseButton]="true"
      [closeOnBackdropClick]="true"
      [closeOnEscape]="true"
      (closed)="onCancel()"
    >
      <!-- Dialog Content -->
      <div class="text-sm text-gray-700 leading-relaxed">
        <p *ngIf="message">{{ message }}</p>
        <ng-content></ng-content>
      </div>

      <!-- Dialog Actions -->
      <div slot="footer" class="flex gap-3 justify-end">
        <app-button
          variant="secondary"
          [label]="cancelText"
          (clicked)="onCancel()"
        ></app-button>
        
        <app-button
          [variant]="isDanger ? 'danger' : 'primary'"
          [label]="confirmText"
          (clicked)="onConfirm()"
        ></app-button>
      </div>
    </app-modal>
  `
})
export class ConfirmDialogComponent implements ConfirmDialogProps {
  @Input() isOpen: boolean = false;
  @Input() size: ConfirmDialogProps['size'] = 'sm';
  @Input() title: string = '確認';
  @Input() message!: string;
  @Input() confirmText: string = '確認';
  @Input() cancelText: string = '取消';
  @Input() isDanger: boolean = false;
  @Input() showCloseButton: boolean = true;
  @Input() closeOnBackdropClick: boolean = true;
  @Input() closeOnEscape: boolean = true;
  @Input() preventBodyScroll: boolean = true;
  @Input() centered: boolean = true;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onConfirm(): void {
    this.confirm.emit();
  }

  onCancel(): void {
    this.cancel.emit();
  }
}