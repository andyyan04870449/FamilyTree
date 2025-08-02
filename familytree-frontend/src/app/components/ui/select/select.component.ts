import {
  Component,
  Input,
  Output,
  EventEmitter,
  forwardRef,
  ElementRef,
  ViewChild,
  HostListener,
  OnInit,
  OnDestroy,
  ChangeDetectionStrategy,
  ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Subject } from 'rxjs';
import { cva } from 'class-variance-authority';
import { cn } from '../../../utils/cn';
import { SelectProps, SelectOption } from './select.interface';

const selectVariants = cva(
  'relative w-full cursor-pointer transition-all duration-200',
  {
    variants: {
      variant: {
        default: 'bg-white border border-gray-300',
        outlined: 'bg-transparent border-2 border-gray-300',
        filled: 'bg-gray-50 border border-transparent'
      },
      size: {
        sm: 'text-sm',
        md: 'text-base',
        lg: 'text-lg'
      },
      error: {
        true: 'border-red-500 focus-within:border-red-600',
        false: 'hover:border-gray-400 focus-within:border-blue-500 focus-within:ring-2 focus-within:ring-blue-200'
      },
      disabled: {
        true: 'opacity-50 cursor-not-allowed bg-gray-100',
        false: ''
      }
    },
    defaultVariants: {
      variant: 'default',
      size: 'md',
      error: false,
      disabled: false
    }
  }
);

const triggerVariants = cva(
  'flex items-center justify-between w-full px-3 py-2 rounded-md focus:outline-none',
  {
    variants: {
      size: {
        sm: 'px-2 py-1.5 text-sm',
        md: 'px-3 py-2 text-base',
        lg: 'px-4 py-3 text-lg'
      }
    },
    defaultVariants: {
      size: 'md'
    }
  }
);

@Component({
  selector: 'app-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div 
      [class]="selectClasses"
      #dropdownContainer
    >
      <!-- Select Trigger -->
      <div 
        [class]="triggerClasses"
        [attr.tabindex]="disabled ? -1 : 0"
        [attr.aria-haspopup]="true"
        [attr.aria-expanded]="isOpen"
        [attr.aria-disabled]="disabled"
        [attr.role]="'combobox'"
        (click)="toggleDropdown()"
        (keydown)="onKeyDown($event)"
      >
        <span 
          class="flex-1 text-left"
          [class]="displayValueClasses"
        >
          {{ displayValue }}
        </span>
        
        <!-- Loading Spinner -->
        <svg 
          *ngIf="loading" 
          class="animate-spin h-4 w-4 text-gray-400 mr-2" 
          viewBox="0 0 24 24"
        >
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" fill="none"></circle>
          <path class="opacity-75" fill="currentColor" d="m4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        
        <!-- Clear Button -->
        <button
          *ngIf="clearable && selectedOption && !disabled"
          type="button"
          class="mr-2 p-1 hover:bg-gray-100 rounded transition-colors"
          (click)="clearSelection($event)"
          aria-label="清除選擇"
        >
          <svg class="h-3 w-3 text-gray-400" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
          </svg>
        </button>
        
        <!-- Arrow Icon -->
        <svg 
          class="h-4 w-4 text-gray-400 transition-transform duration-200"
          [class.rotate-180]="isOpen"
          viewBox="0 0 20 20" 
          fill="currentColor"
        >
          <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
        </svg>
      </div>

      <!-- Dropdown -->
      <div 
        class="absolute z-50 w-full mt-1 bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-auto"
        [class.hidden]="!isOpen"
        [attr.role]="'listbox'"
        [attr.aria-label]="placeholder"
        *ngIf="isOpen"
      >
        <!-- Search Input -->
        <div 
          class="p-2 border-b border-gray-200"
          *ngIf="searchable"
        >
          <input
            #searchInput
            type="text"
            class="w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="搜尋選項..."
            [value]="searchTerm"
            (input)="onSearchInput($event)"
            (keydown)="onKeyDown($event)"
            aria-label="搜尋選項"
          >
        </div>

        <!-- Options List -->
        <ul class="py-1" role="listbox">
          <li
            *ngFor="let option of filteredOptions; let i = index; trackBy: trackByValue"
            class="px-3 py-2 cursor-pointer transition-colors duration-150"
            [class]="getOptionClasses(option, i)"
            [attr.role]="'option'"
            [attr.aria-selected]="option.value === value"
            [attr.aria-disabled]="option.disabled"
            (click)="selectOption(option)"
            (mouseenter)="highlightedIndex = i"
          >
            <div class="flex items-center">
              <span *ngIf="option.icon" class="mr-2">{{ option.icon }}</span>
              <div class="flex-1">
                <div class="font-medium">{{ option.label }}</div>
                <div *ngIf="option.description" class="text-sm text-gray-500">{{ option.description }}</div>
              </div>
              <!-- Selected Check -->
              <svg 
                *ngIf="option.value === value"
                class="h-4 w-4 text-blue-600" 
                viewBox="0 0 20 20" 
                fill="currentColor"
              >
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
            </div>
          </li>
          
          <!-- No Options Message -->
          <li 
            class="px-3 py-2 text-gray-500 text-center"
            *ngIf="filteredOptions.length === 0"
            role="option"
            aria-disabled="true"
          >
            沒有找到符合的選項
          </li>
        </ul>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectComponent),
      multi: true
    }
  ]
})
export class SelectComponent implements OnInit, OnDestroy, ControlValueAccessor, SelectProps {
  @Input() options: SelectOption[] = [];
  @Input() placeholder: string = '請選擇...';
  @Input() disabled: boolean = false;
  @Input() searchable: boolean = false;
  @Input() clearable: boolean = false;
  @Input() size: SelectProps['size'] = 'md';
  @Input() variant: SelectProps['variant'] = 'default';
  @Input() error: boolean = false;
  @Input() loading: boolean = false;
  
  @Output() valueChange = new EventEmitter<any>();
  @Output() cleared = new EventEmitter<void>();

  @ViewChild('dropdownContainer', { static: false }) dropdownContainer?: ElementRef;
  @ViewChild('searchInput', { static: false }) searchInput?: ElementRef;

  // Component state
  public isOpen: boolean = false;
  public selectedOption: SelectOption | null = null;
  public filteredOptions: SelectOption[] = [];
  public searchTerm: string = '';
  public highlightedIndex: number = -1;

  // Control value accessor
  public value: any = null;
  private onChange = (value: any) => {};
  private onTouched = () => {};

  // Cleanup
  private destroy$ = new Subject<void>();

  constructor(private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.initializeComponent();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ControlValueAccessor implementation
  writeValue(value: any): void {
    this.value = value;
    this.updateSelectedOption();
    this.cdr.markForCheck();
  }

  registerOnChange(fn: (value: any) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.cdr.markForCheck();
  }

  // Computed classes
  get selectClasses(): string {
    return cn(
      selectVariants({
        variant: this.variant,
        size: this.size,
        error: this.error,
        disabled: this.disabled
      }),
      'rounded-md'
    );
  }

  get triggerClasses(): string {
    return cn(
      triggerVariants({ size: this.size })
    );
  }

  get displayValueClasses(): string {
    return cn(
      this.selectedOption ? 'text-gray-900' : 'text-gray-500'
    );
  }

  get displayValue(): string {
    return this.selectedOption ? this.selectedOption.label : this.placeholder;
  }

  getOptionClasses(option: SelectOption, index: number): string {
    return cn(
      'hover:bg-gray-50',
      {
        'bg-blue-50 text-blue-700': option.value === this.value,
        'bg-gray-100': index === this.highlightedIndex && option.value !== this.value,
        'opacity-50 cursor-not-allowed': option.disabled,
        'cursor-pointer': !option.disabled
      }
    );
  }

  // Public methods
  public toggleDropdown(): void {
    if (this.disabled || this.loading) return;

    this.isOpen = !this.isOpen;
    
    if (this.isOpen) {
      this.resetSearch();
      this.highlightedIndex = this.getSelectedOptionIndex();
      
      setTimeout(() => {
        if (this.searchable && this.searchInput?.nativeElement) {
          this.searchInput.nativeElement.focus();
        }
      }, 0);
    }
    
    this.cdr.markForCheck();
  }

  public selectOption(option: SelectOption): void {
    if (option.disabled) return;

    this.selectedOption = option;
    this.value = option.value;
    this.isOpen = false;
    this.resetSearch();

    this.onChange(this.value);
    this.valueChange.emit(this.value);
    this.onTouched();
    
    this.cdr.markForCheck();
  }

  public clearSelection(event: Event): void {
    event.stopPropagation();
    
    this.selectedOption = null;
    this.value = null;
    
    this.onChange(this.value);
    this.valueChange.emit(this.value);
    this.cleared.emit();
    this.onTouched();
    
    this.cdr.markForCheck();
  }

  public onSearchInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm = target.value.toLowerCase();
    this.filterOptions();
    this.highlightedIndex = 0;
    this.cdr.markForCheck();
  }

  public onKeyDown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.navigateDown();
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.navigateUp();
        break;
      case 'Enter':
        event.preventDefault();
        this.selectHighlightedOption();
        break;
      case 'Escape':
        event.preventDefault();
        this.closeDropdown();
        break;
      case 'Tab':
        this.closeDropdown();
        break;
    }
  }

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!this.dropdownContainer?.nativeElement?.contains(target)) {
      this.closeDropdown();
    }
  }

  // TrackBy function for template
  public trackByValue = (index: number, option: SelectOption): any => {
    return option.value;
  };

  // Private methods
  private initializeComponent(): void {
    this.filteredOptions = [...this.options];
    this.updateSelectedOption();
  }

  private updateSelectedOption(): void {
    this.selectedOption = this.options.find(option => option.value === this.value) || null;
  }

  private resetSearch(): void {
    this.searchTerm = '';
    this.filterOptions();
  }

  private filterOptions(): void {
    if (!this.searchTerm) {
      this.filteredOptions = [...this.options];
    } else {
      this.filteredOptions = this.options.filter(option =>
        option.label.toLowerCase().includes(this.searchTerm) ||
        option.description?.toLowerCase().includes(this.searchTerm)
      );
    }
  }

  private navigateDown(): void {
    if (!this.isOpen) {
      this.toggleDropdown();
      return;
    }

    const availableOptions = this.filteredOptions.filter(option => !option.disabled);
    if (availableOptions.length === 0) return;

    do {
      this.highlightedIndex = (this.highlightedIndex + 1) % this.filteredOptions.length;
    } while (this.filteredOptions[this.highlightedIndex]?.disabled);

    this.cdr.markForCheck();
  }

  private navigateUp(): void {
    if (!this.isOpen) return;

    const availableOptions = this.filteredOptions.filter(option => !option.disabled);
    if (availableOptions.length === 0) return;

    do {
      this.highlightedIndex = this.highlightedIndex <= 0 
        ? this.filteredOptions.length - 1 
        : this.highlightedIndex - 1;
    } while (this.filteredOptions[this.highlightedIndex]?.disabled);

    this.cdr.markForCheck();
  }

  private selectHighlightedOption(): void {
    if (this.highlightedIndex >= 0 && this.highlightedIndex < this.filteredOptions.length) {
      const option = this.filteredOptions[this.highlightedIndex];
      this.selectOption(option);
    }
  }

  private closeDropdown(): void {
    this.isOpen = false;
    this.resetSearch();
    this.highlightedIndex = -1;
    this.cdr.markForCheck();
  }

  private getSelectedOptionIndex(): number {
    if (!this.selectedOption) return -1;
    return this.filteredOptions.findIndex(option => option.value === this.selectedOption!.value);
  }
}