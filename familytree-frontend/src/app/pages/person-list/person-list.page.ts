// 人員列表頁面 - 顯示所有資料庫中的人員資料，提供分頁和搜尋功能
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { PersonDataService, PersonDataModel } from '../../services/person-data.service';
import { PersonDetailDialogComponent } from '../../components/person-detail-dialog/person-detail-dialog.component';
import { PersonPhotoComponent } from '../../components/person-photo/person-photo.component';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { LoadingComponent } from '../../components/ui/loading/loading.component';
import { cn } from '../../utils/cn';

@Component({
  selector: 'app-person-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    LoadingComponent,
    PersonDetailDialogComponent,
    PersonPhotoComponent
  ],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-7xl mx-auto space-y-6">
        <!-- 頁面標題 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div>
              <h1 class="text-2xl font-bold text-gray-900 flex items-center gap-3">
                <div class="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
                  <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z"/>
                  </svg>
                </div>
                人員列表
              </h1>
              <p class="text-gray-600 mt-1">管理所有資料庫中的人員資料</p>
            </div>
          </div>
        </div>

        <!-- 搜尋區域 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col md:flex-row gap-4">
            <div class="flex-1">
              <label for="search-name" class="block text-sm font-medium text-gray-700 mb-2">搜尋姓名</label>
              <input 
                id="search-name"
                type="text" 
                [(ngModel)]="searchName" 
                placeholder="輸入姓名進行搜尋..." 
                (keyup.enter)="searchPersonData()"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                aria-label="輸入姓名進行搜尋"
              />
            </div>
            <div class="flex gap-3 md:items-end">
              <app-button
                variant="primary"
                size="md"
                label="搜尋"
                icon="🔍"
                [disabled]="loading"
                (clicked)="searchPersonData()"
              ></app-button>
              <app-button
                variant="secondary"
                size="md"
                label="清除"
                icon="🗑️"
                [disabled]="loading"
                (clicked)="clearSearch()"
              ></app-button>
            </div>
          </div>
        </div>

        <!-- 統計資訊 -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <div class="flex items-center">
              <div class="w-8 h-8 bg-blue-100 rounded-lg flex items-center justify-center mr-3">
                <span class="text-blue-600 text-lg">👥</span>
              </div>
              <div>
                <p class="text-2xl font-semibold text-gray-900">{{ totalCount }}</p>
                <p class="text-sm text-gray-600">總人數</p>
              </div>
            </div>
          </div>
          <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <div class="flex items-center">
              <div class="w-8 h-8 bg-green-100 rounded-lg flex items-center justify-center mr-3">
                <span class="text-green-600 text-lg">📄</span>
              </div>
              <div>
                <p class="text-2xl font-semibold text-gray-900">{{ currentPage }}</p>
                <p class="text-sm text-gray-600">當前頁面</p>
              </div>
            </div>
          </div>
          <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <div class="flex items-center">
              <div class="w-8 h-8 bg-purple-100 rounded-lg flex items-center justify-center mr-3">
                <span class="text-purple-600 text-lg">📚</span>
              </div>
              <div>
                <p class="text-2xl font-semibold text-gray-900">{{ totalPages }}</p>
                <p class="text-sm text-gray-600">總頁數</p>
              </div>
            </div>
          </div>
        </div>

        <!-- 載入中 -->
        <div *ngIf="loading" class="bg-white rounded-lg shadow-sm border border-gray-200 p-12">
          <app-loading 
            variant="spinner" 
            size="lg"
            message="載入人員資料中..."
          ></app-loading>
        </div>

        <!-- 錯誤訊息 -->
        <div *ngIf="error && !loading" class="bg-red-50 border border-red-200 rounded-lg p-6">
          <div class="flex items-center">
            <div class="text-red-600 text-2xl mr-3">⚠️</div>
            <div>
              <h3 class="text-lg font-medium text-red-800">載入失敗</h3>
              <p class="text-red-700">{{ error }}</p>
            </div>
          </div>
        </div>

        <!-- 人員列表 -->
        <div *ngIf="!loading && !error" class="bg-white rounded-lg shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-200">
            <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
              👥 人員資料
              <span class="bg-gray-100 text-gray-600 text-sm px-2 py-1 rounded-full">{{ totalCount }}</span>
            </h2>
          </div>
          
          <!-- 空狀態 -->
          <div *ngIf="!personDataList || personDataList.length === 0" class="text-center py-12">
            <div class="text-gray-400 text-6xl mb-4">👥</div>
            <h3 class="text-lg font-medium text-gray-900 mb-2">目前沒有人員資料</h3>
            <p class="text-gray-600">上傳Excel檔案或手動新增人員資料</p>
          </div>
          
          <!-- 人員資料表格 -->
          <div *ngIf="personDataList && personDataList.length > 0" class="overflow-x-auto">
            <table class="w-full">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">序號</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">照片</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">姓名</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">性別</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">來源</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">建立日期</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-32">功能</th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr *ngFor="let person of personDataList; let i = index" class="hover:bg-gray-50">
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {{ (currentPage - 1) * pageSize + i + 1 }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <app-person-photo
                      [photoIndex]="person.photo"
                      [personName]="person.name"
                      size="medium"
                      shape="rounded"
                    ></app-person-photo>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="text-sm font-medium text-gray-900">{{ person.name }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                          [class]="cn(
                            person.gender === 'M' ? 'bg-blue-100 text-blue-800' :
                            person.gender === 'F' ? 'bg-pink-100 text-pink-800' :
                            'bg-gray-100 text-gray-800'
                          )">
                      {{ getGenderText(person.gender) }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                          [class]="cn(
                            person.fileMd5 ? 'bg-green-100 text-green-800' : 'bg-blue-100 text-blue-800'
                          )">
                      {{ getSourceText(person) }}
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {{ formatDate(person.createdAt) }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm">
                    <div class="flex gap-2">
                      <app-button
                        variant="view"
                        size="sm"
                        label="檢視"
                        (clicked)="editPersonData(person)"
                        [attr.aria-label]="'檢視 ' + person.name + ' 的資料'"
                      ></app-button>
                      <app-button
                        variant="danger"
                        size="sm"
                        label="刪除"
                        (clicked)="deletePersonData(person)"
                        [attr.aria-label]="'刪除 ' + person.name + ' 的資料'"
                      ></app-button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- 分頁控制 -->
        <div *ngIf="!loading && !error && totalPages > 1" class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col sm:flex-row justify-between items-center gap-4">
            <div class="text-sm text-gray-600">
              顯示 {{ (currentPage - 1) * pageSize + 1 }}-{{ (currentPage * pageSize > totalCount ? totalCount : currentPage * pageSize) }} 筆，共 {{ totalCount }} 筆
            </div>
            
            <div class="flex items-center gap-2">
              <app-button
                variant="secondary"
                size="sm"
                label="← 上一頁"
                [disabled]="currentPage === 1"
                (clicked)="goToPage(currentPage - 1)"
              ></app-button>
              
              <div class="flex gap-1">
                <button 
                  *ngFor="let page of getPageNumbers()" 
                  class="px-3 py-2 text-sm font-medium rounded-md transition-colors"
                  [class]="cn(
                    page === currentPage 
                      ? 'bg-blue-600 text-white' 
                      : 'text-gray-700 hover:bg-gray-100'
                  )"
                  (click)="goToPage(page)"
                  [attr.aria-label]="'前往第 ' + page + ' 頁'"
                  [attr.aria-current]="page === currentPage ? 'page' : null"
                >
                  {{ page }}
                </button>
              </div>
              
              <app-button
                variant="secondary"
                size="sm"
                label="下一頁 →"
                [disabled]="currentPage === totalPages"
                (clicked)="goToPage(currentPage + 1)"
              ></app-button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 個人詳細資料對話框 -->
    <app-person-detail-dialog
      [personId]="selectedPersonId"
      [isVisible]="showDetailDialog"
      (close)="closeDetailDialog()"
    ></app-person-detail-dialog>
  `
})
export class PersonListComponent implements OnInit, OnDestroy {
  // Utility function for class names
  cn = cn;
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

  constructor(
    private personDataService: PersonDataService,
    private router: Router
  ) {
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
            // 處理新的回應格式：{ success: true, data: [...], pagination: {...} }
            this.personDataList = response.data || response.personDataList || [];
            this.totalCount = response.pagination?.totalCount || response.totalCount || 0;
            this.totalPages = response.pagination?.totalPages || response.totalPages || 0;
            console.log('[PersonListPage] 人員資料列表:', this.personDataList);
          } else {
            this.error = response.message || '載入資料失敗';
            this.personDataList = [];
            console.error('[PersonListPage] 載入人員資料失敗:', response.message);
          }
        },
        error: (err: any) => {
          console.error('[PersonListPage] 載入人員資料API錯誤:', err);
          this.loading = false;
          this.personDataList = [];
          
          if (err.message === '請先選擇專案') {
            this.error = '請先選擇專案後再檢視人員資料';
            // 3秒後自動跳轉到專案管理頁面
            setTimeout(() => {
              this.router.navigate(['/project-management']);
            }, 3000);
          } else {
            this.error = '載入人員資料失敗';
          }
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
            // 處理新的回應格式
            this.personDataList = response.data || response.personDataList || [];
            this.totalCount = response.pagination?.totalCount || response.totalCount || 0;
            this.totalPages = response.pagination?.totalPages || response.totalPages || 0;
          } else {
            this.error = response.message || '搜尋資料失敗';
            this.personDataList = [];
            console.error('[PersonListPage] 搜尋人員資料失敗:', response.message);
          }
        },
        error: (err: any) => {
          console.error('[PersonListPage] 搜尋人員資料API錯誤:', err);
          this.loading = false;
          this.error = '搜尋人員資料失敗';
          this.personDataList = [];
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
            // 重新載入資料
            if (this.searchName.trim()) {
              this.searchPersonData();
            } else {
              this.loadPersonData();
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
    if (!dateString || dateString === '0001-01-01T00:00:00') return '-';
    const date = new Date(dateString);
    return date.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getGenderText(gender: string | null | undefined): string {
    if (!gender) return '-';
    return gender === 'M' ? '男' : gender === 'F' ? '女' : gender;
  }

  getBirthdayText(birthday: string | null): string {
    if (!birthday) return '-';
    const date = new Date(birthday);
    return date.toLocaleDateString('zh-TW');
  }

  getSourceText(person: PersonDataModel): string {
    // 根據資料特徵判斷來源
    if (person.fileMd5) {
      return '檔案上傳';
    }
    if (person.createdBy) {
      return '手動建立';
    }
    return '檔案上傳';
  }


} 