import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { cva } from 'class-variance-authority';
import { cn } from '../../../utils/cn';
import { LoadingProps } from './loading.interface';

const loadingContainerVariants = cva(
  'flex flex-col items-center justify-center gap-3',
  {
    variants: {
      fullScreen: {
        true: 'fixed inset-0 z-50',
        false: 'p-8'
      },
      overlay: {
        true: 'bg-white bg-opacity-80 backdrop-blur-sm',
        false: ''
      }
    },
    defaultVariants: {
      fullScreen: false,
      overlay: false
    }
  }
);

const spinnerVariants = cva(
  'rounded-full border-solid animate-spin',
  {
    variants: {
      size: {
        sm: 'w-4 h-4 border-2',
        md: 'w-8 h-8 border-3',
        lg: 'w-12 h-12 border-4',
        xl: 'w-16 h-16 border-4'
      }
    },
    defaultVariants: {
      size: 'md'
    }
  }
);

const textVariants = cva(
  'text-gray-600 font-medium',
  {
    variants: {
      size: {
        sm: 'text-sm',
        md: 'text-base',
        lg: 'text-lg',
        xl: 'text-xl'
      }
    },
    defaultVariants: {
      size: 'md'
    }
  }
);

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div 
      [class]="containerClasses" 
      role="status" 
      aria-live="polite"
      [attr.aria-label]="message || 'Loading'"
    >
      <!-- Spinner Variant -->
      <div 
        *ngIf="variant === 'spinner'"
        [class]="spinnerClasses"
        [style.border-color]="borderColor"
        [style.border-top-color]="color"
        aria-hidden="true"
      ></div>

      <!-- Dots Variant -->
      <div 
        *ngIf="variant === 'dots'"
        class="flex gap-1"
        aria-hidden="true"
      >
        <div 
          *ngFor="let dot of dotsArray; let i = index"
          [class]="getDotClasses(i)"
          [style.background-color]="color"
        ></div>
      </div>

      <!-- Pulse Variant -->
      <div 
        *ngIf="variant === 'pulse'"
        [class]="pulseClasses"
        [style.background-color]="color"
        aria-hidden="true"
      ></div>

      <!-- Bars Variant -->
      <div 
        *ngIf="variant === 'bars'"
        class="flex gap-1 items-end"
        aria-hidden="true"
      >
        <div 
          *ngFor="let bar of barsArray; let i = index"
          [class]="getBarClasses(i)"
          [style.background-color]="color"
        ></div>
      </div>

      <!-- Loading Message -->
      <p 
        *ngIf="message" 
        [class]="messageClasses"
        [id]="messageId"
      >
        {{ message }}
      </p>
    </div>
  `
})
export class LoadingComponent implements LoadingProps {
  @Input() size: LoadingProps['size'] = 'md';
  @Input() variant: LoadingProps['variant'] = 'spinner';
  @Input() message?: string;
  @Input() fullScreen: boolean = false;
  @Input() overlay: boolean = false;
  @Input() color: string = '#3b82f6'; // blue-500

  // Arrays for rendering multiple elements
  dotsArray = Array(3).fill(0);
  barsArray = Array(4).fill(0);
  
  // Unique ID for accessibility
  messageId = `loading-message-${Math.random().toString(36).substr(2, 9)}`;

  get containerClasses(): string {
    return cn(
      loadingContainerVariants({
        fullScreen: this.fullScreen,
        overlay: this.overlay
      })
    );
  }

  get spinnerClasses(): string {
    return cn(
      spinnerVariants({ size: this.size })
    );
  }

  get messageClasses(): string {
    return cn(
      textVariants({ size: this.size }),
      'm-0'
    );
  }

  get borderColor(): string {
    // Lighter version of the color for the border
    return this.color + '20'; // Add opacity
  }

  get pulseClasses(): string {
    const sizeMap = {
      sm: 'w-8 h-8',
      md: 'w-12 h-12',
      lg: 'w-16 h-16',
      xl: 'w-20 h-20'
    };
    
    return cn(
      'rounded-full animate-pulse',
      sizeMap[this.size || 'md']
    );
  }

  getDotClasses(index: number): string {
    const sizeMap = {
      sm: 'w-2 h-2',
      md: 'w-3 h-3',
      lg: 'w-4 h-4',
      xl: 'w-5 h-5'
    };
    
    return cn(
      'rounded-full animate-bounce',
      sizeMap[this.size || 'md'],
      {
        'animation-delay-100': index === 1,
        'animation-delay-200': index === 2
      }
    );
  }

  getBarClasses(index: number): string {
    const sizeMap = {
      sm: 'w-1',
      md: 'w-1.5',
      lg: 'w-2',
      xl: 'w-3'
    };
    
    const heightMap = {
      sm: ['h-3', 'h-4', 'h-2', 'h-3'],
      md: ['h-4', 'h-6', 'h-3', 'h-4'],
      lg: ['h-6', 'h-8', 'h-4', 'h-6'],
      xl: ['h-8', 'h-12', 'h-6', 'h-8']
    };
    
    return cn(
      'animate-pulse rounded-sm',
      sizeMap[this.size || 'md'],
      heightMap[this.size || 'md'][index],
      {
        'animation-delay-75': index === 1,
        'animation-delay-150': index === 2,
        'animation-delay-225': index === 3
      }
    );
  }
}