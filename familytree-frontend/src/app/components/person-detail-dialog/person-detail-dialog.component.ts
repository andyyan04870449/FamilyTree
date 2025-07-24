// 人員詳細資料對話框組件 - 支援查看和編輯模式，保持原本的UI設計
import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PersonDataService, PersonDataModel, PersonDataRequest } from '../../services/person-data.service';
import { PhotoUploadService } from '../../services/photo-upload.service';

interface RelationshipItem {
  index: number;
  name: string;
  relationship: string;
}

interface FriendItem {
  index: number;
  name: string;
  unit: string;
  event: string;
}

interface ExperienceItem {
  index: number;
  unit: string;
  position: string;
  period: string;
}

interface PublicationItem {
  index: number;
  title: string;
  coAuthors: string;
}

interface ActivityItem {
  index: number;
  name: string;
  participants: string;
}

interface TravelRecordItem {
  index: number;
  period: string;
  destination: string;
  purpose: string;
}

@Component({
  selector: 'app-person-detail-dialog',
  templateUrl: './person-detail-dialog.component.html',
  styleUrls: ['./person-detail-dialog.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class PersonDetailDialogComponent implements OnChanges {
  @Input() personId: number | null = null;
  @Input() isVisible: boolean = false;
  @Output() close = new EventEmitter<void>();

  personData: PersonDataModel | null = null;
  editData: PersonDataRequest = this.getEmptyEditData();
  originalData: PersonDataRequest = this.getEmptyEditData();
  
  loading = false;
  saving = false;
  error = '';
  isEditMode = false;
  activeTab = 'relationships'; // 預設顯示親屬關係

  // 照片URL（只在有照片時設定）
  photoUrl: string | null = null;

  // 解析後的資料
  relationshipItems: RelationshipItem[] = [];
  friendsItems: FriendItem[] = [];
  educationItems: string[] = [];
  experienceItems: ExperienceItem[] = [];
  onlineAccountItems: string[] = [];
  publicationItems: PublicationItem[] = [];
  activityItems: ActivityItem[] = [];
  frequentPlaceItems: string[] = [];
  travelRecordItems: TravelRecordItem[] = [];
  noteItems: string[] = [];

  constructor(
    private personDataService: PersonDataService,
    private photoUploadService: PhotoUploadService
  ) {
    console.log('[PersonDetailDialog] 組件已建立');
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log('[PersonDetailDialog] ngOnChanges - 變更:', changes);
    
    if (changes['personId'] && this.personId && this.isVisible) {
      this.loadPersonData();
    }
    
    if (changes['isVisible']) {
      if (!this.isVisible) {
        this.resetDialog();
      } else if (this.personId) {
        this.loadPersonData();
      }
    }
  }

  private loadPersonData(): void {
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
        
        if (response.success && response.data) {
          console.log('[PersonDetailDialog] 資料載入成功:', response.data);
          this.personData = response.data;
          this.setEditDataFromPersonData();
          this.parseData();
        } else {
          console.error('[PersonDetailDialog] API 回應失敗 - success:', response.success, 'data:', response.data);
          this.error = response.message || '載入人員資料失敗';
        }
      },
      error: (err: any) => {
        console.error('[PersonDetailDialog] API 錯誤:', err);
        this.loading = false;
        this.error = '載入人員資料時發生錯誤';
      }
    });
  }

  private setEditDataFromPersonData(): void {
    if (!this.personData) return;
    
    this.editData = {
      photo: this.personData.photo || '',
      name: this.personData.name || '',
      discoveryProcess: this.personData.discoveryProcess || '',
      gender: this.personData.gender || '',
      birthday: this.formatDateForInput(this.personData.birthday) || '',
      birthplace: this.personData.birthplace || '',
      nationality: this.personData.nationality || '',
      ethnicity: this.personData.ethnicity || '',
      ancestralHome: this.personData.ancestralHome || '',
      politicalParty: this.personData.politicalParty || '',
      idNumber: this.personData.idNumber || '',
      passportNumber: this.personData.passportNumber || '',
      phone: this.personData.phone || '',
      mobile: this.personData.mobile || '',
      email: this.personData.email || '',
      currentWorkplace: this.personData.currentWorkplace || '',
      currentAddress: this.personData.currentAddress || '',
      mailingAddress: this.personData.mailingAddress || '',
      familyRelationships: this.personData.familyRelationships || '',
      experience: this.personData.experience || '',
      education: this.personData.education || '',
      onlineAccounts: this.personData.onlineAccounts || '',
      publications: this.personData.publications || '',
      activities: this.personData.activities || '',
      importantFriends: this.personData.importantFriends || '',
      frequentPlaces: this.personData.frequentPlaces || '',
      travelRecords: this.personData.travelRecords || '',
      notes: this.personData.notes || ''
    };
    
    // 儲存原始資料
    this.originalData = { ...this.editData };
    
    // 載入照片
    this.loadPhoto();
  }

  loadPhoto(): void {
    if (!this.personData?.photo) {
      this.photoUrl = null;
      return;
    }

    try {
      this.photoUrl = this.photoUploadService.getPhotoFileUrlByIndex(this.personData.photo);
    } catch (error) {
      this.photoUrl = null;
    }
  }

  private parseData(): void {
    if (!this.personData) {
      console.warn('[PersonDetailDialog] parseData - personData 為空');
      return;
    }

    console.log('[PersonDetailDialog] 開始解析資料:', this.personData);

    // 解析親屬關係
    console.log('[PersonDetailDialog] 原始親屬關係資料:', this.personData.familyRelationships);
    this.relationshipItems = this.parseRelationships(this.personData.familyRelationships);
    console.log('[PersonDetailDialog] 解析後的親屬關係:', this.relationshipItems);
    
    // 解析朋友資料
    this.friendsItems = this.parseFriends(this.personData.importantFriends);
    console.log('[PersonDetailDialog] 解析後的朋友資料:', this.friendsItems);
    
    // 解析其他資料
    this.educationItems = this.parseList(this.personData.education);
    this.experienceItems = this.parseExperience(this.personData.experience);
    this.onlineAccountItems = this.parseList(this.personData.onlineAccounts);
    this.publicationItems = this.parsePublications(this.personData.publications);
    this.activityItems = this.parseActivities(this.personData.activities);
    this.frequentPlaceItems = this.parseList(this.personData.frequentPlaces);
    this.travelRecordItems = this.parseTravelRecords(this.personData.travelRecords);
    this.noteItems = this.parseList(this.personData.notes);
  }

  private parseRelationships(data: string | undefined | null): RelationshipItem[] {
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
            // 格式：職稱，姓名
            items.push({
              index: index + 1,
              relationship: parts[0] || '--', // 第一個是職稱（關係）
              name: parts[1] || '--'         // 第二個是姓名
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

  private parseFriends(data: string | undefined | null): FriendItem[] {
    if (!data || data.trim() === '') return [];
    
    try {
      const items: FriendItem[] = [];
      const lines = this.splitData(data);
      
      lines.forEach((line, index) => {
        const trimmedLine = line.trim();
        if (trimmedLine) {
          const parts = trimmedLine.split(',').map(p => p.trim());
          items.push({
            index: index + 1,
            name: parts[0] || '--',
            unit: parts[1] || '--',
            event: parts[2] || '--'
          });
        }
      });
      
      return items;
    } catch (error) {
      console.error('[PersonDetailDialog] 解析朋友資料時發生錯誤:', error);
      return [];
    }
  }

  private parseExperience(data: string | undefined | null): ExperienceItem[] {
    if (!data || data.trim() === '') return [];
    
    try {
      const items: ExperienceItem[] = [];
      const lines = this.splitData(data);
      
      lines.forEach((line, index) => {
        const trimmedLine = line.trim();
        if (trimmedLine) {
          const parts = trimmedLine.split(',').map(p => p.trim());
          items.push({
            index: index + 1,
            unit: parts[0] || '--',
            position: parts[1] || '--',
            period: parts[2] || '--'
          });
        }
      });
      
      return items;
    } catch (error) {
      console.error('[PersonDetailDialog] 解析經歷資料時發生錯誤:', error);
      return [];
    }
  }

  private parsePublications(data: string | undefined | null): PublicationItem[] {
    if (!data || data.trim() === '') return [];
    
    try {
      const items: PublicationItem[] = [];
      const lines = this.splitData(data);
      
      lines.forEach((line, index) => {
        const trimmedLine = line.trim();
        if (trimmedLine) {
          const parts = trimmedLine.split(',').map(p => p.trim());
          items.push({
            index: index + 1,
            title: parts[0] || '--',
            coAuthors: parts[1] || '--'
          });
        }
      });
      
      return items;
    } catch (error) {
      console.error('[PersonDetailDialog] 解析著作資料時發生錯誤:', error);
      return [];
    }
  }

  private parseActivities(data: string | undefined | null): ActivityItem[] {
    if (!data || data.trim() === '') return [];
    
    try {
      const items: ActivityItem[] = [];
      const lines = this.splitData(data);
      
      lines.forEach((line, index) => {
        const trimmedLine = line.trim();
        if (trimmedLine) {
          const parts = trimmedLine.split(',').map(p => p.trim());
          items.push({
            index: index + 1,
            name: parts[0] || '--',
            participants: parts[1] || '--'
          });
        }
      });
      
      return items;
    } catch (error) {
      console.error('[PersonDetailDialog] 解析活動資料時發生錯誤:', error);
      return [];
    }
  }

  private parseTravelRecords(data: string | undefined | null): TravelRecordItem[] {
    if (!data || data.trim() === '') return [];
    
    try {
      const items: TravelRecordItem[] = [];
      const lines = this.splitData(data);
      
      lines.forEach((line, index) => {
        const trimmedLine = line.trim();
        if (trimmedLine) {
          const parts = trimmedLine.split(' ').map(p => p.trim());
          if (parts.length >= 2) {
            items.push({
              index: index + 1,
              period: parts[0] || '--',
              destination: parts[1] || '--',
              purpose: parts.slice(2).join(' ') || '--'
            });
          }
        }
      });
      
      return items;
    } catch (error) {
      console.error('[PersonDetailDialog] 解析出國紀錄時發生錯誤:', error);
      return [];
    }
  }

  private splitData(data: string): string[] {
    if (data.includes(';')) {
      return data.split(';');
    } else if (data.includes('\n')) {
      return data.split('\n');
    } else if (data.includes('|')) {
      return data.split('|');
    } else {
      return [data];
    }
  }

  private parseList(data: string | undefined | null): string[] {
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

  private formatDateForInput(dateString: string | undefined): string {
    if (!dateString) return '';
    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime())) return '';
      return date.toISOString().split('T')[0];
    } catch {
      return '';
    }
  }

  private getEmptyEditData(): PersonDataRequest {
    return {
      photo: '',
      name: '',
      discoveryProcess: '',
      gender: '',
      birthday: '',
      birthplace: '',
      nationality: '',
      ethnicity: '',
      ancestralHome: '',
      politicalParty: '',
      idNumber: '',
      passportNumber: '',
      phone: '',
      mobile: '',
      email: '',
      currentWorkplace: '',
      currentAddress: '',
      mailingAddress: '',
      familyRelationships: '',
      experience: '',
      education: '',
      onlineAccounts: '',
      publications: '',
      activities: '',
      importantFriends: '',
      frequentPlaces: '',
      travelRecords: '',
      notes: ''
    };
  }

  enterEditMode(): void {
    this.isEditMode = true;
    // 重新設定編輯資料，確保是最新的
    this.setEditDataFromPersonData();
  }

  cancelEdit(): void {
    this.isEditMode = false;
    // 恢復原始資料
    this.editData = { ...this.originalData };
  }

  saveChanges(): void {
    if (!this.personId || this.saving) return;

    // 基本驗證
    if (!this.editData.name.trim()) {
      alert('姓名為必填欄位');
      return;
    }

    this.saving = true;

    this.personDataService.updatePersonData(this.personId, this.editData).subscribe({
      next: (response: any) => {
        this.saving = false;
        if (response.success) {
          // 更新成功，重新載入資料
          this.isEditMode = false;
          this.loadPersonData();
          alert('資料更新成功！');
        } else {
          alert(`更新失敗: ${response.message}`);
        }
      },
      error: (err: any) => {
        this.saving = false;
        console.error('更新人員資料錯誤:', err);
        alert('更新人員資料時發生錯誤');
      }
    });
  }

  setActiveTab(tab: string): void {
    console.log('[PersonDetailDialog] 切換標籤頁:', tab);
    this.activeTab = tab;
  }

  getTabCount(tab: string): number {
    switch (tab) {
      case 'relationships': return this.relationshipItems.length;
      case 'friends': return this.friendsItems.length;
      case 'education': return this.educationItems.length;
      case 'experience': return this.experienceItems.length;
      case 'online': return this.onlineAccountItems.length;
      case 'publications': return this.publicationItems.length;
      case 'activities': return this.activityItems.length;
      case 'places': return this.frequentPlaceItems.length;
      case 'travel': return this.travelRecordItems.length;
      case 'notes': return this.noteItems.length;
      default: return 0;
    }
  }

  onClose(): void {
    if (this.saving) return;
    
    if (this.isEditMode) {
      if (confirm('您有未儲存的變更，確定要關閉嗎？')) {
        this.resetDialog();
        this.close.emit();
      }
    } else {
      this.resetDialog();
      this.close.emit();
    }
  }

  onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  private resetDialog(): void {
    this.isEditMode = false;
    this.personData = null;
    this.editData = this.getEmptyEditData();
    this.originalData = this.getEmptyEditData();
    this.loading = false;
    this.saving = false;
    this.error = '';
    this.activeTab = 'relationships';
    this.relationshipItems = [];
    this.friendsItems = [];
    this.educationItems = [];
    this.experienceItems = [];
    this.onlineAccountItems = [];
    this.publicationItems = [];
    this.activityItems = [];
    this.frequentPlaceItems = [];
    this.travelRecordItems = [];
    this.noteItems = [];
  }

  // 輔助方法
  formatDate(dateString: string | undefined): string {
    if (!dateString || dateString === '0001-01-01T00:00:00') return '-';
    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime())) return '-';
      return date.toLocaleDateString('zh-TW');
    } catch {
      return '-';
    }
  }

  formatDateTime(dateString: string | undefined): string {
    if (!dateString || dateString === '0001-01-01T00:00:00') return '--';
    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime())) return '--';
      return date.toLocaleString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return '--';
    }
  }

  getGenderText(gender: string | undefined): string {
    if (!gender) return '-';
    return gender === 'M' ? '男' : gender === 'F' ? '女' : gender;
  }
} 