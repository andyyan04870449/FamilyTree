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
    { key: 'Photo', name: '照片' },
    { key: 'name', name: '姓名' },
    { key: 'DiscoveryProcess', name: '發掘來源' },
    { key: 'gender', name: '性別' },
    { key: 'birthday', name: '生日' },
    { key: 'Birthplace', name: '出生地' },
    { key: 'nationality', name: '國籍' },
    { key: 'Ethnicity', name: '民族' },
    { key: 'AncestralHome', name: '籍貫' },
    { key: 'PoliticalParty', name: '政黨' },
    { key: 'IdNumber', name: '身分證號碼' },
    { key: 'PassportNumber', name: '護照號碼' },
    { key: 'phone', name: '電話' },
    { key: 'mobile', name: '行動電話' },
    { key: 'Email', name: '電子信箱' },
    { key: 'CurrentWorkplace', name: '現職單位' },
    { key: 'CurrentAddress', name: '地址' },
    { key: 'MailingAddress', name: '通訊地址' },
    { key: 'FamilyRelationships', name: '親屬關係' },
    { key: 'Experience', name: '經歷' },
    { key: 'Education', name: '學歷' },
    { key: 'OnlineAccounts', name: '網路帳號' },
    { key: 'Publications', name: '著作' },
    { key: 'Activities', name: '活動' },
    { key: 'ImportantFriends', name: '重要友人' },
    { key: 'FrequentPlaces', name: '經常出入場所' },
    { key: 'TravelRecords', name: '出國紀錄' },
    { key: 'Notes', name: '備註' },
    { key: 'CreatedAt', name: '建檔時間' },
    { key: 'CreatedBy', name: '建檔人' },
    { key: 'UpdatedAt', name: '更新時間' },
    { key: 'UpdatedBy', name: '更新人' }
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
    this.logService.info('PersonComparisonComponent', 'ngOnChanges 觸發', {
      personAId: this.personAId,
      personBId: this.personBId,
      isVisible: this.isVisible,
      shouldLoadData: !!(this.personAId && this.personBId && this.isVisible)
    });

    if (this.personAId && this.personBId && this.isVisible) {
      this.logService.info('PersonComparisonComponent', '開始載入人員資料');
      this.loadPersonData();
    } else {
      this.logService.warn('PersonComparisonComponent', '不滿足載入資料條件', {
        personAId: this.personAId,
        personBId: this.personBId,
        isVisible: this.isVisible
      });
    }
  }

  /**
   * 載入人員資料
   */
  private async loadPersonData(): Promise<void> {
    if (!this.personAId || !this.personBId) {
      this.logService.error('PersonComparisonComponent', '人員ID不完整', {
        personAId: this.personAId,
        personBId: this.personBId
      });
      return;
    }

    try {
      this.logService.info('PersonComparisonComponent', '開始載入人員資料', {
        personAId: this.personAId,
        personBId: this.personBId
      });

      // 同時載入兩個人員的資料
      const [personAResponse, personBResponse] = await Promise.all([
        this.personService.getPerson(this.personAId).toPromise(),
        this.personService.getPerson(this.personBId).toPromise()
      ]);

      this.personA = personAResponse;
      this.personB = personBResponse;

      this.logService.info('PersonComparisonComponent', '人員A資料載入完成', {
        personA: this.personA,
        keys: this.personA ? Object.keys(this.personA) : [],
        photoField: this.personA?.Photo,
        projectIdField: this.personA?.project_id
      });

      this.logService.info('PersonComparisonComponent', '人員B資料載入完成', {
        personB: this.personB,
        keys: this.personB ? Object.keys(this.personB) : [],
        photoField: this.personB?.Photo,
        projectIdField: this.personB?.project_id
      });

      // 初始化比較選擇（預設都選A）
      this.buildComparisonData();
      
      this.logService.info('PersonComparisonComponent', '人員資料載入完成，開始初始化比較表格');
      
    } catch (error) {
      this.logService.error('PersonComparisonComponent', '載入人員資料失敗', error);
    }
  }

  /**
   * 建立比較資料結構
   */
  private buildComparisonData(): void {
    this.logService.info('PersonComparisonComponent', '開始建立比較資料結構', {
      fieldDefinitionsLength: this.fieldDefinitions.length,
      personA: this.personA,
      personB: this.personB
    });

    this.comparisonFields = this.fieldDefinitions.map((field, index) => {
      this.logService.info('PersonComparisonComponent', `處理欄位 ${index + 1}/${this.fieldDefinitions.length}`, {
        fieldName: field.name,
        fieldKey: field.key
      });

      const valueA = this.getFieldValue(this.personA, field.key);
      const valueB = this.getFieldValue(this.personB, field.key);
      
      if (field.key === 'Photo') {
        this.logService.info('PersonComparisonComponent', '照片欄位處理結果', {
          fieldKey: field.key,
          valueA,
          valueB,
          personAPhoto_Photo: this.personA?.Photo,
          personAPhoto_photo: this.personA?.photo,
          personBPhoto_Photo: this.personB?.Photo,
          personBPhoto_photo: this.personB?.photo
        });
      }
      
      // 預設選擇邏輯：如果A有值B沒有選A，如果B有值A沒有選B，都有值選A
      let defaultSelection: 'A' | 'B' = 'A';
      if (valueA === '-' && valueB !== '-') {
        defaultSelection = 'B';
      } else {
        defaultSelection = 'A';
      }

      const result = {
        fieldName: field.name,
        fieldKey: field.key,
        personA: valueA,
        personB: valueB,
        selectedValue: defaultSelection
      };

      if (field.key === 'Photo') {
        this.logService.info('PersonComparisonComponent', '照片欄位最終結果', {
          result,
          defaultSelection
        });
      }

      return result;
    });

    this.logService.info('PersonComparisonComponent', '比較資料結構建立完成', {
      comparisonFieldsLength: this.comparisonFields.length,
      photoField: this.comparisonFields.find(f => f.fieldKey === 'Photo')
    });
  }

  /**
   * 獲取欄位值並格式化顯示
   */
  private getFieldValue(person: any, fieldKey: string): string {
    if (!person) {
      this.logService.warn('PersonComparisonComponent', 'getFieldValue: 人員資料為空', { fieldKey });
      return '-';
    }
    
    const value = person[fieldKey];
    
    if (fieldKey === 'Photo') {
      this.logService.info('PersonComparisonComponent', 'getFieldValue: 處理照片欄位', {
        fieldKey,
        person,
        value,
        photoIndex_Photo: person.Photo,
        photoIndex_photo: person.photo,
        projectId: person.project_id
      });
    }
    
    switch (fieldKey) {
      case 'Photo':
        const photoUrl = this.getPhotoUrl(person);
        this.logService.info('PersonComparisonComponent', 'getFieldValue: 照片URL處理完成', {
          fieldKey,
          photoUrl
        });
        return photoUrl;
      case 'CreatedAt':
      case 'UpdatedAt':
        return value ? new Date(value).toLocaleString('zh-TW') : '-';
      default:
        return value || '-';
    }
  }

  /**
   * 獲取照片URL，如果沒有照片則返回預設值
   */
  private getPhotoUrl(person: any): string {
    this.logService.info('PersonComparisonComponent', '開始獲取照片URL', {
      person: person,
      personKeys: person ? Object.keys(person) : [],
      photoField_Photo: person?.Photo,      // PersonDataController 格式
      photoField_photo: person?.photo,      // RelationshipGraphController 格式
      nameField: person?.name,
      projectIdField: person?.project_id
    });

    if (!person) {
      this.logService.warn('PersonComparisonComponent', '人員資料為空，返回無照片');
      return '無照片';
    }

    // 支援兩種照片欄位格式：
    // 1. Photo（來自 PersonDataController）
    // 2. photo（來自 RelationshipGraphController）
    const photoIndex = person.Photo || person.photo;

    if (!photoIndex) {
      this.logService.warn('PersonComparisonComponent', '照片索引為空，返回無照片', {
        photoField_Photo: person.Photo,
        photoField_photo: person.photo,
        allFields: person
      });
      return '無照片';
    }
    
    try {
      this.logService.info('PersonComparisonComponent', '調用 photoUtils.getPersonPhotoUrl', {
        photoIndex: photoIndex,
        personName: person.name,
        projectId: person.project_id,
        dataSource: person.Photo ? 'PersonDataController' : 'RelationshipGraphController'
      });

      const url = this.photoUtils.getPersonPhotoUrl(photoIndex, person.name, person.project_id);
      
      this.logService.info('PersonComparisonComponent', '照片URL生成結果', {
        generatedUrl: url,
        isNull: url === null,
        isUndefined: url === undefined,
        photoIndex: photoIndex
      });

      return url || '無照片';
    } catch (error) {
      this.logService.error('PersonComparisonComponent', '獲取照片URL失敗', {
        error: error,
        person: person,
        photoIndex: photoIndex,
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