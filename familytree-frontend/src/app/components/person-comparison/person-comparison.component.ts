/*
 * 人員資料比較表組件 - 用於合併流程
 * 功能：在合併前提供兩人資料的詳細比較界面，讓用戶選擇保留的資料
 * 用途：作為合併流程的第二步，比較完成後執行實際合併操作
 */

import { Component, Input, Output, EventEmitter, OnInit, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { LogService } from '../../services/log.service';
import { PersonService } from '../../services/person.service';
import { PhotoUtilsService } from '../../services/photo-utils.service';

export interface ComparisonField {
  fieldName: string;
  fieldKey: string;
  personA: string;
  personB: string;
  selectedValue: 'A' | 'B'; // 用戶選擇保留A的值或B的值
}

export interface MergeSelection {
  personAId: number;
  personBId: number;
  mergedData: { [key: string]: any };
}

@Component({
  selector: 'app-person-comparison',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './person-comparison.component.html',
  styleUrls: ['./person-comparison.component.scss']
})
export class PersonComparisonComponent implements OnInit, OnChanges {
  @Input() personAId: number | null = null;
  @Input() personBId: number | null = null;
  @Input() isVisible: boolean = false;
  @Output() closeComparison = new EventEmitter<void>();
  @Output() confirmMerge = new EventEmitter<MergeSelection>();

  personA: any = null;
  personB: any = null;
  comparisonFields: ComparisonField[] = [];
  loading: boolean = false;
  error: string = '';

  // 比較欄位定義（基於person_profile資料表）
  private fieldDefinitions = [
    { key: 'photo_index', name: '照片' },
    { key: 'name', name: '姓名' },
    { key: 'discovery_source', name: '發掘來源' },
    { key: 'discovery_process', name: '發掘經過' },
    { key: 'gender', name: '性別' },
    { key: 'birthday', name: '生日' },
    { key: 'birthplace', name: '出生地' },
    { key: 'nationality', name: '國籍' },
    { key: 'ethnicity', name: '民族' },
    { key: 'ancestral_origin', name: '籍貫' },
    { key: 'political_party', name: '政黨' },
    { key: 'id_number', name: '身分證號碼' },
    { key: 'passport_number', name: '護照號碼' },
    { key: 'phone', name: '電話' },
    { key: 'mobile', name: '行動電話' },
    { key: 'email', name: '電子信箱' },
    { key: 'current_employer', name: '現職單位' },
    { key: 'address', name: '地址' },
    { key: 'mailing_address', name: '通訊地址' },
    { key: 'family_relationships', name: '親屬關係' },
    { key: 'experience', name: '經歷' },
    { key: 'education', name: '學歷' },
    { key: 'online_accounts', name: '網路帳號' },
    { key: 'publications', name: '著作' },
    { key: 'activities', name: '活動' },
    { key: 'friends', name: '朋友' },
    { key: 'important_friends', name: '重要友人' },
    { key: 'frequent_locations', name: '經常出入場所' },
    { key: 'travel_history', name: '出國紀錄' },
    { key: 'remarks', name: '備註' },
    { key: 'created_at', name: '建檔時間' },
    { key: 'created_by', name: '建檔人' },
    { key: 'updated_at', name: '更新時間' },
    { key: 'updated_by', name: '更新人' }
  ];

  constructor(
    private logService: LogService,
    private personService: PersonService,
    private photoUtils: PhotoUtilsService
  ) {}

  ngOnInit() {
    this.logService.info('PersonComparisonComponent', '合併比較組件初始化完成');
  }

  ngOnChanges() {
    if (this.personAId && this.personBId && this.isVisible) {
      this.loadPersonData();
    }
  }

  /**
   * 從資料庫載入兩個人員的詳細資料
   */
  private async loadPersonData(): Promise<void> {
    if (!this.personAId || !this.personBId) return;

    this.loading = true;
    this.error = '';

    try {
      this.logService.info('PersonComparisonComponent', '開始載入人員資料', {
        personAId: this.personAId,
        personBId: this.personBId
      });

      // 並行載入兩個人員的資料
      const [personAResult, personBResult] = await Promise.all([
        firstValueFrom(this.personService.getPerson(this.personAId)),
        firstValueFrom(this.personService.getPerson(this.personBId))
      ]);

      this.personA = personAResult;
      this.personB = personBResult;

      this.logService.info('PersonComparisonComponent', '人員資料載入完成', {
        personA: this.personA,
        personB: this.personB
      });

      this.buildComparisonData();

    } catch (error) {
      this.error = '載入人員資料時發生錯誤';
      this.logService.error('PersonComparisonComponent', '載入人員資料失敗', error);
    } finally {
      this.loading = false;
    }
  }

  /**
   * 建立比較資料結構
   */
  private buildComparisonData(): void {
    this.comparisonFields = this.fieldDefinitions.map((field, index) => {
      const valueA = this.getFieldValue(this.personA, field.key);
      const valueB = this.getFieldValue(this.personB, field.key);
      
      // 預設選擇邏輯：如果A有值B沒有選A，如果B有值A沒有選B，都有值選A
      let defaultSelection: 'A' | 'B' = 'A';
      if (valueA === '-' && valueB !== '-') {
        defaultSelection = 'B';
      } else {
        defaultSelection = 'A';
      }

      return {
        fieldName: field.name,
        fieldKey: field.key,
        personA: valueA,
        personB: valueB,
        selectedValue: defaultSelection
      };
    });
  }

  /**
   * 獲取欄位值並格式化顯示
   */
  private getFieldValue(person: any, fieldKey: string): string {
    if (!person) return '-';
    
    const value = person[fieldKey];
    
    switch (fieldKey) {
      case 'photo_index':
        return this.getPhotoUrl(person);
      case 'created_at':
      case 'updated_at':
        return value ? new Date(value).toLocaleString('zh-TW') : '-';
      default:
        return value || '-';
    }
  }

  /**
   * 獲取照片URL，如果沒有照片則返回預設值
   */
  private getPhotoUrl(person: any): string {
    if (!person || !person.photo_index) {
      return '無照片';
    }
    
    try {
      const url = this.photoUtils.getPersonPhotoUrl(person.photo_index, person.name);
      return url || '無照片';
    } catch (error) {
      this.logService.error('PersonComparisonComponent', '獲取照片URL失敗', error);
      return '照片載入失敗';
    }
  }

  /**
   * 選擇欄位值
   */
  selectFieldValue(field: ComparisonField, selection: 'A' | 'B'): void {
    field.selectedValue = selection;
    this.logService.debug('PersonComparisonComponent', '選擇欄位值', {
      field: field.fieldName,
      selection: selection
    });
  }

  /**
   * 檢查是否有差異的欄位
   */
  isDifferent(field: ComparisonField): boolean {
    return field.personA !== field.personB && field.personA !== '-' && field.personB !== '-';
  }

  /**
   * 取消比較
   */
  cancel(): void {
    this.logService.info('PersonComparisonComponent', '取消資料比較');
    this.closeComparison.emit();
  }

  /**
   * 確認合併
   */
  confirmMergeAction(): void {
    if (!this.personA || !this.personB) return;

    // 建立合併後的資料
    const mergedData: { [key: string]: any } = {};
    
    this.comparisonFields.forEach(field => {
      switch (field.selectedValue) {
        case 'A':
          mergedData[field.fieldKey] = this.personA[field.fieldKey];
          break;
        case 'B':
          mergedData[field.fieldKey] = this.personB[field.fieldKey];
          break;
      }
    });

    const mergeSelection: MergeSelection = {
      personAId: this.personAId!,
      personBId: this.personBId!,
      mergedData: mergedData
    };

    this.logService.info('PersonComparisonComponent', '確認合併操作', {
      mergeSelection: mergeSelection
    });

    this.confirmMerge.emit(mergeSelection);
  }

  /**
   * 快速選擇全部A或全部B
   */
  selectAll(selection: 'A' | 'B'): void {
    this.comparisonFields.forEach(field => {
      if (selection === 'A' && field.personA !== '-') {
        field.selectedValue = 'A';
      } else if (selection === 'B' && field.personB !== '-') {
        field.selectedValue = 'B';
      }
    });
    
    this.logService.info('PersonComparisonComponent', `快速選擇全部${selection}`);
  }
} 