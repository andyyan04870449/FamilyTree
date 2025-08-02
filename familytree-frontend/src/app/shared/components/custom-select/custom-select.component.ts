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
import { SelectOption } from '../../interfaces/select-option.interface';

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './custom-select.component.html',
  styleUrls: ['./custom-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomSelectComponent),
      multi: true
    }
  ]
})
export class CustomSelectComponent implements OnInit, OnDestroy, ControlValueAccessor {
  @Input() options: SelectOption[] = [];
  @Input() placeholder: string = '請選擇...';
  @Input() disabled: boolean = false;
  @Input() searchable: boolean = false;
  @Output() valueChange = new EventEmitter<any>();

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

  // Public methods
  public toggleDropdown(): void {
    if (this.disabled) return;

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

  // Getters for template
  public get displayValue(): string {
    return this.selectedOption ? this.selectedOption.label : this.placeholder;
  }

  public get isOptionSelected(): boolean {
    return this.selectedOption !== null;
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
        option.label.toLowerCase().includes(this.searchTerm)
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