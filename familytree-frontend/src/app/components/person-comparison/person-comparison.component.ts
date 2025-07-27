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

  // 比較欄位定義（基於person_profile資料表，使用後端實際返回的camelCase欄位名稱）
  private fieldDefinitions = [
    { key: 'photo', name: '照片' },
    { key: 'name', name: '姓名' },
    { key: 'discoveryProcess', name: '發掘來源' },
    { key: 'gender', name: '性別' },
    { key: 'birthday', name: '生日' },
    { key: 'birthplace', name: '出生地' },
    { key: 'nationality', name: '國籍' },
    { key: 'ethnicity', name: '民族' },
    { key: 'ancestralHome', name: '籍貫' },
    { key: 'politicalParty', name: '政黨' },
    { key: 'idNumber', name: '身分證號碼' },
    { key: 'passportNumber', name: '護照號碼' },
    { key: 'phone', name: '電話' },
    { key: 'mobile', name: '行動電話' },
    { key: 'email', name: '電子信箱' },
    { key: 'currentWorkplace', name: '現職單位' },
    { key: 'currentAddress', name: '地址' },
    { key: 'mailingAddress', name: '通訊地址' },
    { key: 'familyRelationships', name: '親屬關係' },
    { key: 'experience', name: '經歷' },
    { key: 'education', name: '學歷' },
    { key: 'onlineAccounts', name: '網路帳號' },
    { key: 'publications', name: '著作' },
    { key: 'activities', name: '活動' },
    { key: 'importantFriends', name: '重要友人' },
    { key: 'frequentPlaces', name: '經常出入場所' },
    { key: 'travelRecords', name: '出國紀錄' },
    { key: 'notes', name: '備註' },
    { key: 'createdAt', name: '建檔時間' },
    { key: 'createdBy', name: '建檔人' },
    { key: 'updatedAt', name: '更新時間' },
    { key: 'updatedBy', name: '更新人' }
  ];

  constructor(
    private logService: LogService,
    private personService: PersonService,
    private photoUtils: PhotoUtilsService
  ) {}

  ngOnInit() {
    // 組件初始化
  }

  ngOnChanges() {
    if (this.personAId && this.personBId && this.isVisible) {
      this.logService.info('PersonComparisonComponent', '開始載入比較資料', {
        personAId: this.personAId,
        personBId: this.personBId
      });
      this.loadPersonData();
    }
  }

  /**
   * 載入人員資料
   */
  private async loadPersonData(): Promise<void> {
    if (!this.personAId || !this.personBId) return;

    try {
      this.loading = true;
      this.error = '';

      // 同時載入兩個人員的資料
      const [personAResponse, personBResponse] = await Promise.all([
        this.personService.getPerson(this.personAId).toPromise(),
        this.personService.getPerson(this.personBId).toPromise()
      ]);

      this.personA = personAResponse;
      this.personB = personBResponse;

      this.logService.info('PersonComparisonComponent', '✅ 人員資料載入完成', {
        personA: {
          id: this.personA?.id,
          name: this.personA?.name,
          fieldsCount: this.personA ? Object.keys(this.personA).length : 0,
          hasPhoto: !!this.personA?.Photo,
          projectId: this.personA?.project_id
        },
        personB: {
          id: this.personB?.id,
          name: this.personB?.name,
          fieldsCount: this.personB ? Object.keys(this.personB).length : 0,
          hasPhoto: !!this.personB?.Photo,
          projectId: this.personB?.project_id
        }
      });

      // 初始化比較選擇
      this.buildComparisonData();
      
    } catch (error) {
      this.error = '載入人員資料失敗';
      this.logService.error('PersonComparisonComponent', '❌ 載入人員資料失敗', error);
    } finally {
      this.loading = false;
    }
  }

  /**
   * 建立比較資料結構
   */
  private buildComparisonData(): void {
    this.comparisonFields = this.fieldDefinitions.map(field => {
      const valueA = this.getFieldValue(this.personA, field.key);
      const valueB = this.getFieldValue(this.personB, field.key);
      
      // 預設選擇邏輯：如果A有值B沒有選A，如果B有值A沒有選B，都有值選A
      let defaultSelection: 'A' | 'B' = 'A';
      if (valueA === '-' && valueB !== '-') {
        defaultSelection = 'B';
      }

      return {
        fieldName: field.name,
        fieldKey: field.key,
        personA: valueA,
        personB: valueB,
        selectedValue: defaultSelection
      };
    });

    // 記錄關鍵欄位的資料狀況
    const emptyFields = this.comparisonFields.filter(f => f.personA === '-' && f.personB === '-');
    const photoField = this.comparisonFields.find(f => f.fieldKey === 'Photo');
    
          this.logService.info('PersonComparisonComponent', '🔍 比較資料結構建立完成', {
        totalFields: this.comparisonFields.length,
        emptyFieldsCount: emptyFields.length,
        emptyFields: emptyFields.map(f => f.fieldName),
        photoStatus: {
          personA: photoField?.personA || '無',
          personB: photoField?.personB || '無'
        },
        sampleData: {
          name: { A: this.personA?.name, B: this.personB?.name },
          phone: { A: this.personA?.phone, B: this.personB?.phone },
          email: { A: this.personA?.Email, B: this.personB?.Email }
        }
      });


  }

  /**
   * 獲取欄位值並格式化顯示
   */
  private getFieldValue(person: any, fieldKey: string): string {
    if (!person) return '-';
    
    const value = person[fieldKey];
    
    switch (fieldKey) {
      case 'photo':
        return this.getPhotoUrl(person);
      case 'createdAt':
      case 'updatedAt':
        return value ? new Date(value).toLocaleString('zh-TW') : '-';
      default:
        return value || '-';
    }
  }

  /**
   * 獲取照片URL，如果沒有照片則返回預設值
   */
  private getPhotoUrl(person: any): string {
    if (!person) return '無照片';

    // 支援兩種照片欄位格式：photo（主要）或 photoIndex（備用）
    const photoIndex = person.photo || person.photoIndex;

    if (!photoIndex) {
      this.logService.debug('PersonComparisonComponent', `📷 ${person.name || 'Unknown'}: 無照片索引`, {
        photo: person.photo,
        photoIndex: person.photoIndex
      });
      return '無照片';
    }
    
    try {
      const url = this.photoUtils.getPersonPhotoUrl(photoIndex, person.name, person.project_id);
      
      if (!url) {
        this.logService.warn('PersonComparisonComponent', `📷 ${person.name || 'Unknown'}: 照片URL生成失敗`, {
          photoIndex,
          projectId: person.project_id
        });
        return '無照片';
      }

      return url;
    } catch (error) {
      this.logService.error('PersonComparisonComponent', `📷 ${person.name || 'Unknown'}: 照片載入錯誤`, {
        error,
        photoIndex,
        projectId: person.project_id
      });
      return '照片載入失敗';
    }
  }

  /**
   * 選擇欄位值
   */
  selectFieldValue(field: ComparisonField, selection: 'A' | 'B'): void {
    field.selectedValue = selection;
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

    this.logService.info('PersonComparisonComponent', '🔄 執行合併操作', {
      personA: this.personA.name,
      personB: this.personB.name,
      selectedFields: this.comparisonFields.filter(f => f.selectedValue === 'A').length + ' from A, ' +
                     this.comparisonFields.filter(f => f.selectedValue === 'B').length + ' from B'
    });

    this.confirmMerge.emit(mergeSelection);
  }

  /**
   * 快速選擇全部A或全部B
   */
  selectAll(selection: 'A' | 'B'): void {
    let changedCount = 0;
    this.comparisonFields.forEach(field => {
      if (selection === 'A' && field.personA !== '-') {
        field.selectedValue = 'A';
        changedCount++;
      } else if (selection === 'B' && field.personB !== '-') {
        field.selectedValue = 'B';
        changedCount++;
      }
    });
    
    this.logService.info('PersonComparisonComponent', `📋 快速選擇全部${selection}`, {
      changedFields: changedCount
    });
  }
} 