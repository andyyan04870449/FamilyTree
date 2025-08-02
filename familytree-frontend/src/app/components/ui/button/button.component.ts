import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { cva } from 'class-variance-authority';
import { cn } from '../../../utils/cn';
import { ButtonProps } from './button.interface';

const buttonVariants = cva(
  'inline-flex items-center justify-center gap-1 px-3 py-1.5 border border-transparent rounded text-sm font-medium cursor-pointer transition-all duration-200 outline-none focus-visible:ring-2 focus-visible:ring-blue-500 focus-visible:ring-opacity-50 disabled:opacity-50 disabled:cursor-not-allowed',
  {
    variants: {
      variant: {
        default: 'bg-gray-100 text-gray-700 border-gray-300 hover:bg-gray-200 hover:border-gray-400 active:bg-gray-300',
        primary: 'bg-blue-600 text-white hover:bg-blue-700 active:bg-blue-800',
        secondary: 'bg-gray-600 text-white hover:bg-gray-700 active:bg-gray-800',
        view: 'bg-blue-500 text-white hover:bg-blue-600 active:bg-blue-700',
        favorite: 'bg-orange-500 text-white hover:bg-orange-600 active:bg-orange-700',
        danger: 'bg-red-500 text-white hover:bg-red-600 active:bg-red-700'
      },
      size: {
        sm: 'px-2 py-1 text-xs',
        md: 'px-3 py-1.5 text-sm',
        lg: 'px-4 py-2 text-base'
      },
      isActive: {
        true: '',
        false: ''
      }
    },
    compoundVariants: [
      {
        variant: 'favorite',
        isActive: true,
        class: 'bg-red-500 hover:bg-red-600'
      }
    ],
    defaultVariants: {
      variant: 'default',
      size: 'md',
      isActive: false
    }
  }
);

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [class]="buttonClasses"
      [disabled]="disabled || loading"
      [title]="title || label"
      [attr.aria-label]="ariaLabel || title || label"
      (click)="handleClick($event)"
    >
      <span *ngIf="loading" class="mr-1">
        <svg class="animate-spin h-4 w-4" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none"></circle>
          <path class="opacity-75" fill="currentColor" d="m4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      </span>
      <span *ngIf="icon && !loading" class="text-base leading-none" [attr.aria-hidden]="!!label">{{ icon }}</span>
      <span *ngIf="label" class="whitespace-nowrap">{{ label }}</span>
      <ng-content *ngIf="!label && !icon"></ng-content>
    </button>
  `
})
export class ButtonComponent implements ButtonProps {
  @Input() variant: ButtonProps['variant'] = 'default';
  @Input() size: ButtonProps['size'] = 'md';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() icon?: string;
  @Input() label?: string;
  @Input() title?: string;
  @Input() ariaLabel?: string;
  @Input() isActive = false;

  @Output() clicked = new EventEmitter<Event>();

  get buttonClasses() {
    return cn(
      buttonVariants({
        variant: this.variant,
        size: this.size,
        isActive: this.isActive
      })
    );
  }

  handleClick(event: Event): void {
    if (!this.disabled && !this.loading) {
      this.clicked.emit(event);
    }
  }
}