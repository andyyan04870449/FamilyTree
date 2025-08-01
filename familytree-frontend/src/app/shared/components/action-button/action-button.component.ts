import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-action-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [class]="buttonClass"
      [class.is-active]="isActive"
      [disabled]="disabled"
      [title]="title || label"
      [attr.aria-label]="ariaLabel || title || label"
      (click)="handleClick($event)"
    >
      <span *ngIf="icon" class="action-button__icon" [attr.aria-hidden]="!!label">{{ icon }}</span>
      <span *ngIf="label" class="action-button__label">{{ label }}</span>
    </button>
  `,
  styleUrls: ['./action-button.component.scss']
})
export class ActionButtonComponent {
  @Input() label?: string;
  @Input() icon?: string;
  @Input() title?: string;
  @Input() ariaLabel?: string;
  @Input() buttonClass = 'btn-action';
  @Input() isActive = false;
  @Input() disabled = false;
  @Output() clicked = new EventEmitter<Event>();

  handleClick(event: Event): void {
    if (!this.disabled) {
      this.clicked.emit(event);
    }
  }
}