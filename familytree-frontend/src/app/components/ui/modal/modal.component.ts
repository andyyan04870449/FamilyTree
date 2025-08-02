import { 
  Component, 
  Input, 
  Output, 
  EventEmitter, 
  OnChanges, 
  SimpleChanges, 
  OnDestroy, 
  HostListener,
  ElementRef,
  ViewChild,
  AfterViewInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { cva } from 'class-variance-authority';
import { cn } from '../../../utils/cn';
import { ModalProps } from './modal.interface';

const overlayVariants = cva(
  'fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 backdrop-blur-sm transition-opacity duration-200',
  {
    variants: {
      centered: {
        true: 'items-center',
        false: 'items-start pt-16'
      }
    },
    defaultVariants: {
      centered: true
    }
  }
);

const modalVariants = cva(
  'relative bg-white rounded-lg shadow-xl transform transition-all duration-200 max-h-[90vh] overflow-hidden',
  {
    variants: {
      size: {
        sm: 'w-full max-w-sm mx-4',
        md: 'w-full max-w-md mx-4',
        lg: 'w-full max-w-lg mx-4',
        xl: 'w-full max-w-4xl mx-4',
        full: 'w-full h-full max-w-none max-h-none m-0 rounded-none'
      },
      variant: {
        default: 'border border-gray-200',
        danger: 'border border-red-200',
        success: 'border border-green-200',
        warning: 'border border-yellow-200',
        info: 'border border-blue-200'
      }
    },
    defaultVariants: {
      size: 'md',
      variant: 'default'
    }
  }
);

const headerVariants = cva(
  'flex items-center justify-between p-6 border-b',
  {
    variants: {
      variant: {
        default: 'border-gray-200',
        danger: 'border-red-200 bg-red-50',
        success: 'border-green-200 bg-green-50',
        warning: 'border-yellow-200 bg-yellow-50',
        info: 'border-blue-200 bg-blue-50'
      }
    },
    defaultVariants: {
      variant: 'default'
    }
  }
);

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Modal Overlay -->
    <div 
      *ngIf="isOpen"
      [class]="overlayClasses"
      (click)="onBackdropClick($event)"
      [@fadeIn]
      role="dialog"
      aria-modal="true"
      [attr.aria-labelledby]="titleId"
    >
      <!-- Modal Container -->
      <div 
        #modalContent
        [class]="modalClasses"
        (click)="$event.stopPropagation()"
        [@slideIn]
        tabindex="-1"
      >
        <!-- Modal Header -->
        <div 
          *ngIf="title || showCloseButton"
          [class]="headerClasses"
        >
          <h3 
            *ngIf="title"
            [id]="titleId"
            class="text-lg font-semibold text-gray-900 m-0"
          >
            {{ title }}
          </h3>
          
          <button 
            *ngIf="showCloseButton"
            type="button"
            class="p-1 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
            (click)="onClose()"
            aria-label="關閉對話框"
          >
            <svg class="w-5 h-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>

        <!-- Modal Body -->
        <div class="p-6 overflow-y-auto flex-1">
          <ng-content></ng-content>
        </div>

        <!-- Modal Footer -->
        <div 
          *ngIf="hasFooterContent"
          class="px-6 py-4 bg-gray-50 border-t border-gray-200 flex justify-end gap-3"
        >
          <ng-content select="[slot=footer]"></ng-content>
        </div>
      </div>
    </div>
  `,
  animations: [
    // You can add animations here if needed
  ]
})
export class ModalComponent implements OnChanges, OnDestroy, AfterViewInit, ModalProps {
  @Input() isOpen: boolean = false;
  @Input() size: ModalProps['size'] = 'md';
  @Input() variant: ModalProps['variant'] = 'default';
  @Input() title?: string;
  @Input() showCloseButton: boolean = true;
  @Input() closeOnBackdropClick: boolean = true;
  @Input() closeOnEscape: boolean = true;
  @Input() preventBodyScroll: boolean = true;
  @Input() centered: boolean = true;

  @Output() closed = new EventEmitter<void>();
  @Output() opened = new EventEmitter<void>();

  @ViewChild('modalContent') modalContent?: ElementRef;

  // Unique ID for accessibility
  titleId = `modal-title-${Math.random().toString(36).substr(2, 9)}`;
  hasFooterContent = false;

  constructor(private elementRef: ElementRef) {}

  ngAfterViewInit(): void {
    // Check if footer content exists
    const footerSlot = this.elementRef.nativeElement.querySelector('[slot=footer]');
    this.hasFooterContent = !!footerSlot;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      if (this.isOpen) {
        this.onModalOpen();
      } else {
        this.onModalClose();
      }
    }
  }

  ngOnDestroy(): void {
    this.restoreBodyScroll();
  }

  @HostListener('document:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (this.isOpen && this.closeOnEscape && event.key === 'Escape') {
      this.onClose();
    }
  }

  get overlayClasses(): string {
    return cn(
      overlayVariants({ centered: this.centered })
    );
  }

  get modalClasses(): string {
    return cn(
      modalVariants({ 
        size: this.size, 
        variant: this.variant 
      })
    );
  }

  get headerClasses(): string {
    return cn(
      headerVariants({ variant: this.variant })
    );
  }

  onBackdropClick(event: Event): void {
    if (this.closeOnBackdropClick && event.target === event.currentTarget) {
      this.onClose();
    }
  }

  onClose(): void {
    this.closed.emit();
  }

  private onModalOpen(): void {
    if (this.preventBodyScroll) {
      document.body.style.overflow = 'hidden';
    }
    
    // Focus the modal content for accessibility
    setTimeout(() => {
      this.modalContent?.nativeElement?.focus();
    }, 0);
    
    this.opened.emit();
  }

  private onModalClose(): void {
    this.restoreBodyScroll();
  }

  private restoreBodyScroll(): void {
    if (this.preventBodyScroll) {
      document.body.style.overflow = '';
    }
  }
}