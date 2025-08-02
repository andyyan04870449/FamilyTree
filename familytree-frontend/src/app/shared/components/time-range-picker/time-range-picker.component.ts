import { Component, Input, Output, EventEmitter, OnInit, ChangeDetectionStrategy, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TimeRangeFactory, TimeRange, QuickTimeRangeOption, TimeFormatOptions } from '../../../utils/time-range.factory';

export interface TimeRangeValue {
  startDate: string;
  endDate: string;
  selectedRange?: string;
}

@Component({
  selector: 'app-time-range-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TimeRangePickerComponent),
      multi: true
    }
  ],
  template: `
    <div class="time-range-picker">
      <!-- 快速時間選擇按鈕 -->
      <div class="time-range-picker__quick-buttons" *ngIf="showQuickButtons">
        <label class="time-range-picker__label">快速選擇</label>
        <div class="quick-buttons">
          <button 
            *ngFor="let range of quickRanges"
            type="button"
            class="btn btn--sm btn--outline"
            [class.btn--primary]="selectedQuickRange === range.key"
            (click)="selectQuickRange(range)"
            [title]="range.description">
            {{ range.label }}
          </button>
          <button 
            *ngIf="selectedQuickRange && allowClear"
            type="button"
            class="btn btn--sm btn--secondary"
            (click)="clearSelection()"
            title="清除選擇">
            <i class="fas fa-times" aria-hidden="true"></i>
            清除
          </button>
        </div>
      </div>

      <!-- 自定義時間範圍輸入 -->
      <div class="time-range-picker__custom" [class.time-range-picker__custom--expanded]="showCustomInputs">
        <div class="time-range-picker__toggle" *ngIf="showQuickButtons">
          <button 
            type="button"
            class="btn btn--link"
            (click)="showCustomInputs = !showCustomInputs"
            [attr.aria-expanded]="showCustomInputs">
            <i class="fas" [ngClass]="showCustomInputs ? 'fa-chevron-up' : 'fa-chevron-down'" aria-hidden="true"></i>
            {{ showCustomInputs ? '收合' : '自定義時間範圍' }}
          </button>
        </div>

        <div class="time-range-inputs" *ngIf="showCustomInputs || !showQuickButtons">
          <div class="form-field" role="group" aria-labelledby="time-range-label">
            <legend id="time-range-label" class="form-field__label">{{ label || '時間範圍' }}</legend>
            <div class="form-field__group">
              <div class="form-field__item">
                <label for="startDate" class="form-field__label">開始時間</label>
                <input 
                  type="datetime-local" 
                  id="startDate"
                  [(ngModel)]="startDate"
                  (ngModelChange)="onDateChange()"
                  class="form-field__input"
                  [max]="endDate || maxDate"
                  [min]="minDate"
                  [disabled]="disabled"
                  aria-label="開始時間">
              </div>
              <span class="form-field__separator">至</span>
              <div class="form-field__item">
                <label for="endDate" class="form-field__label">結束時間</label>
                <input 
                  type="datetime-local" 
                  id="endDate"
                  [(ngModel)]="endDate"
                  (ngModelChange)="onDateChange()"
                  class="form-field__input"
                  [min]="startDate || minDate"
                  [max]="maxDate"
                  [disabled]="disabled"
                  aria-label="結束時間">
              </div>
            </div>
          </div>

          <!-- 時間範圍資訊 -->
          <div class="time-range-info" *ngIf="showRangeInfo && startDate && endDate">
            <small class="time-range-info__duration">
              <i class="fas fa-clock" aria-hidden="true"></i>
              時間跨度：{{ getRangeDuration() }}
            </small>
          </div>

          <!-- 驗證錯誤訊息 -->
          <div class="time-range-errors" *ngIf="validationErrors.length > 0">
            <div 
              *ngFor="let error of validationErrors"
              class="error-message">
              <i class="fas fa-exclamation-triangle" aria-hidden="true"></i>
              {{ error }}
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./time-range-picker.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TimeRangePickerComponent implements OnInit, ControlValueAccessor {
  @Input() label: string = '';
  @Input() showQuickButtons: boolean = true;
  @Input() showCustomInputs: boolean = false;
  @Input() showRangeInfo: boolean = true;
  @Input() allowClear: boolean = true;
  @Input() disabled: boolean = false;
  @Input() minDate: string = '';
  @Input() maxDate: string = '';
  @Input() quickRangeTypes: string[] = ['today', 'last7days', 'last30days', 'last90days'];
  @Input() formatOptions: TimeFormatOptions = { format: 'datetime-local' };

  @Output() rangeChange = new EventEmitter<TimeRangeValue>();
  @Output() quickRangeSelected = new EventEmitter<string>();

  public startDate: string = '';
  public endDate: string = '';
  public selectedQuickRange: string = '';
  public quickRanges: QuickTimeRangeOption[] = [];
  public validationErrors: string[] = [];

  private onChange = (value: TimeRangeValue) => {};
  private onTouched = () => {};

  constructor(private timeRangeFactory: TimeRangeFactory) {}

  ngOnInit(): void {
    this.initializeQuickRanges();
    this.setDefaultMaxDate();
  }

  // ControlValueAccessor implementation
  writeValue(value: TimeRangeValue): void {
    if (value) {
      this.startDate = value.startDate || '';
      this.endDate = value.endDate || '';
      this.selectedQuickRange = value.selectedRange || '';
    } else {
      this.clearSelection();
    }
  }

  registerOnChange(fn: (value: TimeRangeValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  /**
   * 選擇快速時間範圍
   */
  selectQuickRange(range: QuickTimeRangeOption): void {
    const { start, end } = range.getValue();
    
    this.startDate = this.timeRangeFactory.formatDateForInput(start, this.formatOptions);
    this.endDate = this.timeRangeFactory.formatDateForInput(end, this.formatOptions);
    this.selectedQuickRange = range.key;
    
    this.validateAndEmit();
    this.quickRangeSelected.emit(range.key);
  }

  /**
   * 清除所有選擇
   */
  clearSelection(): void {
    this.startDate = '';
    this.endDate = '';
    this.selectedQuickRange = '';
    this.validationErrors = [];
    
    this.emitChange();
  }

  /**
   * 日期變更處理
   */
  onDateChange(): void {
    // 如果手動修改日期，清除快速選擇狀態
    if (this.selectedQuickRange) {
      this.selectedQuickRange = '';
    }
    
    this.validateAndEmit();
  }

  /**
   * 獲取時間範圍持續時間描述
   */
  getRangeDuration(): string {
    if (!this.startDate || !this.endDate) return '';
    
    const start = this.timeRangeFactory.parseDate(this.startDate);
    const end = this.timeRangeFactory.parseDate(this.endDate);
    
    if (!start || !end) return '';
    
    const range = this.timeRangeFactory.createCustomRange(start, end);
    return this.timeRangeFactory.getRangeDurationDescription(range);
  }

  /**
   * 設定預設的快速時間範圍
   */
  setDefaultQuickRange(rangeKey: string): void {
    const range = this.quickRanges.find(r => r.key === rangeKey);
    if (range) {
      this.selectQuickRange(range);
    }
  }

  /**
   * 獲取當前的時間範圍值
   */
  getCurrentValue(): TimeRangeValue {
    return {
      startDate: this.startDate,
      endDate: this.endDate,
      selectedRange: this.selectedQuickRange
    };
  }

  private initializeQuickRanges(): void {
    this.quickRanges = this.timeRangeFactory.getQuickTimeRangesByType(this.quickRangeTypes);
  }

  private setDefaultMaxDate(): void {
    if (!this.maxDate) {
      const now = new Date();
      this.maxDate = this.timeRangeFactory.formatDateForInput(now, this.formatOptions);
    }
  }

  private validateAndEmit(): void {
    this.validateTimeRange();
    this.emitChange();
  }

  private validateTimeRange(): void {
    this.validationErrors = [];
    
    if (!this.startDate && !this.endDate) {
      return; // 空值是允許的
    }
    
    const start = this.timeRangeFactory.parseDate(this.startDate);
    const end = this.timeRangeFactory.parseDate(this.endDate);
    
    if (this.startDate && !start) {
      this.validationErrors.push('開始時間格式不正確');
    }
    
    if (this.endDate && !end) {
      this.validationErrors.push('結束時間格式不正確');
    }
    
    if (start && end) {
      const range = this.timeRangeFactory.createCustomRange(start, end);
      const validation = this.timeRangeFactory.validateTimeRange(range);
      
      if (!validation.valid) {
        this.validationErrors.push(...validation.errors);
      }
    }
  }

  private emitChange(): void {
    const value: TimeRangeValue = {
      startDate: this.startDate,
      endDate: this.endDate,
      selectedRange: this.selectedQuickRange
    };
    
    this.onChange(value);
    this.rangeChange.emit(value);
    this.onTouched();
  }
}