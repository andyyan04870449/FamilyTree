// 個人詳細資料對話框組件 - 顯示完整的個人資料信息，包含基本資料和各種關聯資料的標籤頁
import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PersonDataService, PersonDataModel } from '../../services/person-data.service';

interface RelationshipItem {
  index: number;
  name: string;
  birthday: string;
  gender: string;
  relationship: string;
}

@Component({
  selector: 'app-person-detail-dialog',
  templateUrl: './person-detail-dialog.component.html',
  styleUrls: ['./person-detail-dialog.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class PersonDetailDialogComponent implements OnInit, OnChanges {
  @Input() personId: number | null = null;
  @Input() isVisible = false;
  @Output() close = new EventEmitter<void>();

  personData: PersonDataModel | null = null;
  loading = false;
  error = '';
  activeTab = 'relationships'; // 預設顯示親屬關係

  // 解析後的資料
  relationshipItems: RelationshipItem[] = [];
  importantFriends: string[] = [];
  experiences: string[] = [];
  educations: string[] = [];
  onlineAccounts: string[] = [];
  travelRecords: string[] = [];
  activities: string[] = [];
  frequentPlaces: string[] = [];
  publications: string[] = [];

  constructor(private personDataService: PersonDataService) {
    console.log('[PersonDetailDialog] 組件已建立');
  }

  ngOnInit(): void {
    console.log('[PersonDetailDialog] ngOnInit - personId:', this.personId, 'isVisible:', this.isVisible);
    if (this.personId && this.isVisible) {
      this.loadPersonData();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log('[PersonDetailDialog] ngOnChanges - 變更:', changes);
    
    if (changes['personId'] || changes['isVisible']) {
      console.log('[PersonDetailDialog] 偵測到重要屬性變更 - personId:', this.personId, 'isVisible:', this.isVisible);
      
      if (this.personId && this.isVisible) {
        console.log('[PersonDetailDialog] 條件符合，開始載入資料');
        this.loadPersonData();
      } else {
        console.log('[PersonDetailDialog] 條件不符合，清空資料');
        this.personData = null;
        this.error = '';
      }
    }
  }

  loadPersonData(): void {
    if (!this.personId) {
      console.error('[PersonDetailDialog] loadPersonData - personId 為空');
      return;
    }

    console.log('[PersonDetailDialog] 開始載入人員資料 - ID:', this.personId);
    this.loading = true;
    this.error = '';
    this.personData = null;

    this.personDataService.getPersonData(this.personId).subscribe({
      next: (response: any) => {
        console.log('[PersonDetailDialog] API 回應:', response);
        this.loading = false;
        
        if (response.success && response.personData) {
          console.log('[PersonDetailDialog] 資料載入成功:', response.personData);
          this.personData = response.personData;
          this.parseData();
        } else {
          console.error('[PersonDetailDialog] API 回應失敗:', response.message);
          this.error = response.message || '載入失敗';
        }
      },
      error: (err: any) => {
        console.error('[PersonDetailDialog] API 錯誤:', err);
        this.loading = false;
        this.error = '載入人員詳細資料失敗';
      }
    });
  }

  parseData(): void {
    if (!this.personData) {
      console.warn('[PersonDetailDialog] parseData - personData 為空');
      return;
    }

    console.log('[PersonDetailDialog] 開始解析資料:', this.personData);

    // 解析親屬關係
    console.log('[PersonDetailDialog] 原始親屬關係資料:', this.personData.familyRelationships);
    this.relationshipItems = this.parseRelationships(this.personData.familyRelationships);
    console.log('[PersonDetailDialog] 解析後的親屬關係:', this.relationshipItems);
    
    // 解析其他資料
    this.importantFriends = this.parseList(this.personData.importantFriends);
    this.experiences = this.parseList(this.personData.experience);
    this.educations = this.parseList(this.personData.education);
    this.onlineAccounts = this.parseList(this.personData.onlineAccounts);
    this.travelRecords = this.parseList(this.personData.travelRecords);
    this.activities = this.parseList(this.personData.activities);
    this.frequentPlaces = this.parseList(this.personData.frequentPlaces);
    this.publications = this.parseList(this.personData.publications);

    console.log('[PersonDetailDialog] 所有解析完成 - 重要友人:', this.importantFriends.length, 
                '工作經歷:', this.experiences.length, '學歷:', this.educations.length);
  }

  parseRelationships(data: string | undefined | null): RelationshipItem[] {
    console.log('[PersonDetailDialog] parseRelationships - 輸入資料:', data);
    
    if (!data || data.trim() === '') {
      console.log('[PersonDetailDialog] 親屬關係資料為空');
      return [];
    }
    
    try {
      const items: RelationshipItem[] = [];
      
      // 嘗試多種分隔符號
      let lines: string[] = [];
      if (data.includes(';')) {
        lines = data.split(';');
      } else if (data.includes('\n')) {
        lines = data.split('\n');
      } else if (data.includes('|')) {
        lines = data.split('|');
      } else {
        lines = [data]; // 如果沒有分隔符號，就當作一筆資料
      }
      
      console.log('[PersonDetailDialog] 分割後的行數:', lines.length, '內容:', lines);
      
      lines.forEach((line, index) => {
        const trimmedLine = line.trim();
        if (trimmedLine) {
          const parts = trimmedLine.split(',').map(p => p.trim());
          console.log('[PersonDetailDialog] 處理第', index + 1, '行:', parts);
          
          if (parts.length >= 1) {
            items.push({
              index: index + 1,
              name: parts[0] || '',
              birthday: parts[1] || '--',
              gender: this.parseGender(parts[2]) || '--',
              relationship: parts[3] || '--'
            });
          }
        }
      });
      
      console.log('[PersonDetailDialog] 解析完成的親屬關係項目:', items);
      return items;
    } catch (error) {
      console.error('[PersonDetailDialog] 解析親屬關係時發生錯誤:', error);
      return [];
    }
  }

  parseList(data: string | undefined | null): string[] {
    console.log('[PersonDetailDialog] parseList - 輸入資料:', data);
    
    if (!data || data.trim() === '') {
      return [];
    }
    
    let result: string[] = [];
    
    // 嘗試多種分隔符號
    if (data.includes(';')) {
      result = data.split(';');
    } else if (data.includes('\n')) {
      result = data.split('\n');
    } else if (data.includes('|')) {
      result = data.split('|');
    } else {
      result = [data];
    }
    
    result = result.map(item => item.trim()).filter(item => item);
    console.log('[PersonDetailDialog] parseList 結果:', result);
    return result;
  }

  parseGender(gender: string | undefined | null): string {
    if (!gender) return '';
    const g = gender.toLowerCase().trim();
    if (g === 'm' || g === '男' || g === 'male') return '男';
    if (g === 'f' || g === '女' || g === 'female') return '女';
    return gender;
  }

  setActiveTab(tab: string): void {
    console.log('[PersonDetailDialog] 切換標籤頁:', tab);
    this.activeTab = tab;
  }

  getTabCount(tab: string): number {
    switch (tab) {
      case 'relationships': return this.relationshipItems.length;
      case 'friends': return this.importantFriends.length;
      case 'experience': return this.experiences.length;
      case 'education': return this.educations.length;
      case 'accounts': return this.onlineAccounts.length;
      case 'travel': return this.travelRecords.length;
      case 'activities': return this.activities.length;
      case 'places': return this.frequentPlaces.length;
      case 'publications': return this.publications.length;
      default: return 0;
    }
  }

  formatDate(dateString: string | undefined | null): string {
    if (!dateString) return '--';
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('zh-TW');
    } catch {
      return dateString;
    }
  }

  getGenderText(gender: string | undefined | null): string {
    if (!gender) return '未知';
    return gender === 'M' ? '男' : gender === 'F' ? '女' : gender;
  }

  onClose(): void {
    console.log('[PersonDetailDialog] 關閉對話框');
    this.close.emit();
  }

  onOverlayClick(event: Event): void {
    if (event.target === event.currentTarget) {
      console.log('[PersonDetailDialog] 點擊背景關閉');
      this.onClose();
    }
  }
} 