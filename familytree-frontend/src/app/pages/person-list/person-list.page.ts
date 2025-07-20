// 人員列表頁面 - 顯示所有資料庫中的人員資料，提供分頁和搜尋功能
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { PersonDataService, PersonDataModel } from '../../services/person-data.service';
import { PersonDetailDialogComponent } from '../../components/person-detail-dialog/person-detail-dialog.component';

@Component({
  selector: 'app-person-list',
  templateUrl: './person-list.page.html',
  styleUrls: ['./person-list.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, PersonDetailDialogComponent]
})
export class PersonListComponent implements OnInit, OnDestroy {
  personDataList: PersonDataModel[] = [];
  loading = false;
  error = '';
  searchName = '';
  
  // 分頁相關
  currentPage = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;
  
  // 對話框相關
  showDetailDialog = false;
  selectedPersonId: number | null = null;
  
  private subscription = new Subscription();

  constructor(private personDataService: PersonDataService) {
    console.log('[PersonListPage] 組件已建立');
  }

  ngOnInit(): void {
    console.log('[PersonListPage] ngOnInit - 開始載入人員資料');
    this.loadPersonData();
  }

  ngOnDestroy(): void {
    console.log('[PersonListPage] 組件銷毀');
    this.subscription.unsubscribe();
  }

  loadPersonData(): void {
    console.log('[PersonListPage] loadPersonData - 頁面:', this.currentPage, '每頁數量:', this.pageSize);
    this.loading = true;
    this.error = '';

    this.subscription.add(
      this.personDataService.getPersonDataList(this.currentPage, this.pageSize).subscribe({
        next: (response: any) => {
          console.log('[PersonListPage] 載入人員資料成功:', response);
          this.loading = false;
          if (response.success) {
            this.personDataList = response.personDataList;
            this.totalCount = response.totalCount;
            this.totalPages = response.totalPages;
            this.currentPage = response.pageNumber;
            console.log('[PersonListPage] 人員資料列表:', this.personDataList);
          } else {
            this.error = response.message;
            console.error('[PersonListPage] 載入人員資料失敗:', response.message);
          }
        },
        error: (err: any) => {
          console.error('[PersonListPage] 載入人員資料API錯誤:', err);
          this.loading = false;
          this.error = '載入人員資料失敗';
        }
      })
    );
  }

  searchPersonData(): void {
    console.log('[PersonListPage] searchPersonData - 搜尋條件:', this.searchName);
    this.currentPage = 1; // 重置到第一頁
    this.loading = true;
    this.error = '';

    this.subscription.add(
      this.personDataService.searchPersonData(this.searchName, this.currentPage, this.pageSize).subscribe({
        next: (response: any) => {
          console.log('[PersonListPage] 搜尋人員資料成功:', response);
          this.loading = false;
          if (response.success) {
            this.personDataList = response.personDataList;
            this.totalCount = response.totalCount;
            this.totalPages = response.totalPages;
            this.currentPage = response.pageNumber;
          } else {
            this.error = response.message;
            console.error('[PersonListPage] 搜尋人員資料失敗:', response.message);
          }
        },
        error: (err: any) => {
          console.error('[PersonListPage] 搜尋人員資料API錯誤:', err);
          this.loading = false;
          this.error = '搜尋人員資料失敗';
        }
      })
    );
  }

  clearSearch(): void {
    console.log('[PersonListPage] clearSearch - 清除搜尋條件');
    this.searchName = '';
    this.currentPage = 1;
    this.loadPersonData();
  }

  goToPage(page: number): void {
    console.log('[PersonListPage] goToPage - 目標頁面:', page);
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.currentPage = page;
      if (this.searchName.trim()) {
        this.searchPersonData();
      } else {
        this.loadPersonData();
      }
    }
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;
    
    if (this.totalPages <= maxVisiblePages) {
      // 如果總頁數不多，顯示所有頁數
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      // 如果總頁數很多，顯示當前頁附近的頁數
      let start = Math.max(1, this.currentPage - Math.floor(maxVisiblePages / 2));
      let end = Math.min(this.totalPages, start + maxVisiblePages - 1);
      
      if (end - start + 1 < maxVisiblePages) {
        start = Math.max(1, end - maxVisiblePages + 1);
      }
      
      for (let i = start; i <= end; i++) {
        pages.push(i);
      }
    }
    
    return pages;
  }

  editPersonData(person: PersonDataModel): void {
    console.log('[PersonListPage] editPersonData - 選中的人員:', person);
    console.log('[PersonListPage] 人員ID:', person.id, '姓名:', person.name);
    
    // 立即測試API調用
    console.log('[PersonListPage] 立即測試API調用...');
    this.personDataService.getPersonData(person.id).subscribe({
      next: (response: any) => {
        console.log('[PersonListPage] 測試API調用成功:', response);
      },
      error: (err: any) => {
        console.error('[PersonListPage] 測試API調用失敗:', err);
      }
    });
    
    this.selectedPersonId = person.id;
    this.showDetailDialog = true;
    
    console.log('[PersonListPage] 對話框狀態設定完成 - selectedPersonId:', this.selectedPersonId, 'showDetailDialog:', this.showDetailDialog);
    
    // 延遲一下再次確認狀態
    setTimeout(() => {
      console.log('[PersonListPage] 延遲檢查 - selectedPersonId:', this.selectedPersonId, 'showDetailDialog:', this.showDetailDialog);
    }, 100);
  }

  closeDetailDialog(): void {
    console.log('[PersonListPage] closeDetailDialog - 關閉詳細資料對話框');
    this.showDetailDialog = false;
    this.selectedPersonId = null;
    console.log('[PersonListPage] 對話框已關閉');
  }

  deletePersonData(person: PersonDataModel): void {
    console.log('[PersonListPage] deletePersonData - 刪除人員:', person.name);
    if (confirm(`確定要刪除人員 "${person.name}" 嗎？`)) {
      this.subscription.add(
        this.personDataService.deletePersonData(person.id).subscribe({
          next: (response: any) => {
            console.log('[PersonListPage] 刪除人員回應:', response);
            if (response.success) {
              console.log('[PersonListPage] 人員資料刪除成功');
              // 重新載入資料
              if (this.searchName.trim()) {
                this.searchPersonData();
              } else {
                this.loadPersonData();
              }
            } else {
              alert(`刪除失敗: ${response.message}`);
            }
          },
          error: (err: any) => {
            console.error('[PersonListPage] 刪除人員API錯誤:', err);
            alert('刪除人員資料時發生錯誤');
          }
        })
      );
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getGenderText(gender: string | null): string {
    if (!gender) return '-';
    return gender === 'M' ? '男' : gender === 'F' ? '女' : gender;
  }

  getBirthdayText(birthday: string | null): string {
    if (!birthday) return '-';
    const date = new Date(birthday);
    return date.toLocaleDateString('zh-TW');
  }
} 