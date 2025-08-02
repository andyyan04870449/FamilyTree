// 全文檢索頁面 - 提供全文搜索、收藏管理、搜索歷史等功能
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { FullTextSearchService, SearchRequest, SearchResult as ApiSearchResult, PersonSearchResult } from '../../services/fulltext-search.service';
import { FavoritesService } from '../../services/favorites.service';
import { ProjectService } from '../../services/project.service';
import { PersonDetailDialogComponent } from '../../components/person-detail-dialog/person-detail-dialog.component';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { LoadingComponent } from '../../components/ui/loading/loading.component';
import { ActionButton } from '../../shared/components/action-button-group/action-button-group.component';
import { cn } from '../../utils/cn';

// 搜索結果接口
interface SearchResult {
  id: number;
  name: string;
  gender: string;
  source: string;
  createdAt: string;
  isFavorited: boolean;
  selected?: boolean;
  matchCount?: number;
}

// 搜索歷史接口
interface SearchHistory {
  keyword: string;
  count: number;
  lastUsed: string;
}

// 熱門關鍵字接口 - 已移除功能

// 收藏接口
interface Favorite {
  id: number;
  name: string;
  addedAt: string;
}



@Component({
  selector: 'app-full-text-search',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    LoadingComponent,
    PersonDetailDialogComponent,
  ],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-7xl mx-auto space-y-6">
        <!-- 頁面標題 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div>
              <h1 class="text-2xl font-bold text-gray-900 flex items-center gap-3">
                <div class="w-10 h-10 bg-orange-100 rounded-lg flex items-center justify-center">
                  <svg class="w-6 h-6 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
                  </svg>
                </div>
                關鍵字檢索
              </h1>
              <p class="text-gray-600 mt-1">搜尋人員資料和管理收藏</p>
            </div>
            
            <app-button
              *ngIf="searchResults.length > 0"
              variant="secondary"
              size="md"
              label="匯出結果"
              icon="📄"
              (clicked)="exportResults()"
            ></app-button>
          </div>
        </div>

        <!-- 搜尋區域 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <form (ngSubmit)="performSearch()" class="space-y-4">
            <div class="grid grid-cols-1 lg:grid-cols-12 gap-4 items-end">
              <!-- 關鍵字輸入 -->
              <div class="lg:col-span-4">
                <label for="search-keyword" class="block text-sm font-medium text-gray-700 mb-2">
                  關鍵字 <span class="text-red-500">*</span>
                </label>
                <input 
                  id="search-keyword"
                  type="text" 
                  placeholder="請輸入關鍵字"
                  [(ngModel)]="searchKeyword"
                  (keyup.enter)="performSearch()"
                  required
                  class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                />
              </div>
              
              <!-- 查詢方式 -->
              <div class="lg:col-span-3">
                <fieldset>
                  <legend class="block text-sm font-medium text-gray-700 mb-2">查詢方式</legend>
                  <div class="flex gap-4">
                    <label class="flex items-center">
                      <input 
                        type="radio" 
                        name="searchType" 
                        value="exact" 
                        [(ngModel)]="searchType"
                        class="w-4 h-4 text-orange-600 bg-gray-100 border-gray-300 focus:ring-orange-500 focus:ring-2"
                      >
                      <span class="ml-2 text-sm text-gray-700">精準</span>
                    </label>
                    <label class="flex items-center">
                      <input 
                        type="radio" 
                        name="searchType" 
                        value="fuzzy" 
                        [(ngModel)]="searchType"
                        class="w-4 h-4 text-orange-600 bg-gray-100 border-gray-300 focus:ring-orange-500 focus:ring-2"
                      >
                      <span class="ml-2 text-sm text-gray-700">模糊</span>
                    </label>
                  </div>
                </fieldset>
              </div>
              
              <!-- 操作按鈕 -->
              <div class="lg:col-span-3 flex gap-3">
                <app-button
                  type="submit"
                  variant="primary"
                  size="md"
                  label="搜尋"
                  icon="🔍"
                  [disabled]="loading || !searchKeyword.trim()"
                ></app-button>
                <app-button
                  type="button"
                  variant="secondary"
                  size="md"
                  label="重置"
                  icon="🔄"
                  [disabled]="loading"
                  (clicked)="resetSearch()"
                ></app-button>
              </div>
            </div>
            
            <!-- 搜尋歷史 -->
            <div *ngIf="searchHistory.length > 0" class="pt-4 border-t border-gray-200">
              <div class="flex items-center gap-2 mb-2">
                <span class="text-sm font-medium text-gray-700">歷史紀錄：</span>
              </div>
              <div class="flex flex-wrap gap-2">
                <button 
                  *ngFor="let history of searchHistory" 
                  type="button"
                  class="px-3 py-1 text-xs bg-gray-100 text-gray-700 rounded-full hover:bg-gray-200 transition-colors"
                  (click)="useHistoryKeyword(history.keyword)"
                >
                  {{ history.keyword }}
                </button>
              </div>
            </div>
            <div *ngIf="searchHistory.length === 0" class="pt-4 border-t border-gray-200">
              <span class="text-sm text-gray-500">暫無搜索歷史</span>
            </div>
          </form>
        </div>

        <!-- 主要內容區域 -->
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-6">
          <!-- 左側搜尋結果 -->
          <div class="lg:col-span-3">
            <div class="bg-white rounded-lg shadow-sm border border-gray-200">
              <div class="p-6 border-b border-gray-200">
                <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
                  🔍 結果列表
                  <span class="bg-gray-100 text-gray-600 text-sm px-2 py-1 rounded-full">{{ totalResults }}</span>
                </h2>
              </div>
              
              <!-- 載入中 -->
              <div *ngIf="loading" class="p-12">
                <app-loading 
                  variant="spinner" 
                  size="lg"
                  message="搜尋中..."
                ></app-loading>
              </div>

              <!-- 錯誤訊息 -->
              <div *ngIf="error && !loading" class="p-6">
                <div class="bg-red-50 border border-red-200 rounded-lg p-4">
                  <div class="flex items-center">
                    <div class="text-red-600 text-xl mr-3">⚠️</div>
                    <div>
                      <h3 class="text-sm font-medium text-red-800">搜尋失敗</h3>
                      <p class="text-sm text-red-700">{{ error }}</p>
                    </div>
                  </div>
                </div>
              </div>

              <!-- 搜尋結果表格 -->
              <div *ngIf="!loading && !error && searchResults.length > 0" class="overflow-x-auto">
                <table class="w-full">
                  <thead class="bg-gray-50">
                    <tr>
                      <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-12">
                        <input 
                          type="checkbox" 
                          (change)="toggleSelectAll($event)"
                          class="w-4 h-4 text-orange-600 bg-gray-100 border-gray-300 rounded focus:ring-orange-500 focus:ring-2"
                        >
                      </th>
                      <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-16">項次</th>
                      <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">姓名</th>
                      <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">符合欄位</th>
                      <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">資料來源</th>
                      <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">建檔日期</th>
                      <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-32">功能</th>
                    </tr>
                  </thead>
                  <tbody class="bg-white divide-y divide-gray-200">
                    <tr *ngFor="let result of searchResults; let i = index" 
                        class="hover:bg-gray-50"
                        [class]="cn(result.selected ? 'bg-orange-50' : '')">
                      <td class="px-6 py-4 whitespace-nowrap">
                        <input 
                          type="checkbox" 
                          [(ngModel)]="result.selected"
                          class="w-4 h-4 text-orange-600 bg-gray-100 border-gray-300 rounded focus:ring-orange-500 focus:ring-2"
                        >
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{{ i + 1 }}</td>
                      <td class="px-6 py-4 whitespace-nowrap">
                        <div class="text-sm font-medium text-gray-900">{{ result.name }}</div>
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                          {{ result.matchCount }} 筆
                        </span>
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                          {{ result.source }}
                        </span>
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {{ formatDate(result.createdAt) }}
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap text-sm">
                        <div class="flex gap-2">
                          <app-button
                            variant="view"
                            size="sm"
                            label="檢視"
                            (clicked)="viewDetails(result)"
                          ></app-button>
                          <app-button
                            [variant]="result.isFavorited ? 'favorite' : 'secondary'"
                            size="sm"
                            [label]="result.isFavorited ? '已收藏' : '收藏'"
                            (clicked)="toggleFavorite(result)"
                          ></app-button>
                        </div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- 分頁控制 -->
              <div *ngIf="totalPages > 1" class="p-6 border-t border-gray-200">
                <div class="flex flex-col sm:flex-row justify-between items-center gap-4">
                  <div class="text-sm text-gray-600">
                    顯示 {{ (currentPage - 1) * pageSize + 1 }} 至 {{ Math.min(currentPage * pageSize, totalResults) }} 筆，共 {{ totalResults }} 筆資料
                  </div>
                  
                  <div class="flex items-center gap-2">
                    <button 
                      class="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      [disabled]="currentPage === 1"
                      (click)="goToPage(1)"
                    >
                      ≪
                    </button>
                    <button 
                      class="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      [disabled]="currentPage === 1"
                      (click)="goToPage(currentPage - 1)"
                    >
                      ‹
                    </button>
                    
                    <div class="flex gap-1">
                      <button 
                        *ngFor="let page of getPageNumbers()" 
                        class="px-3 py-2 text-sm font-medium rounded-md transition-colors"
                        [class]="cn(
                          page === currentPage 
                            ? 'bg-orange-600 text-white' 
                            : 'text-gray-700 bg-white border border-gray-300 hover:bg-gray-50'
                        )"
                        (click)="goToPage(page)"
                      >
                        {{ page }}
                      </button>
                    </div>
                    
                    <button 
                      class="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      [disabled]="currentPage === totalPages"
                      (click)="goToPage(currentPage + 1)"
                    >
                      ›
                    </button>
                    <button 
                      class="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                      [disabled]="currentPage === totalPages"
                      (click)="goToPage(totalPages)"
                    >
                      ≫
                    </button>
                    
                    <select 
                      [(ngModel)]="pageSize" 
                      (change)="onPageSizeChange()"
                      class="ml-2 px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-orange-500 focus:border-transparent"
                    >
                      <option value="10">10</option>
                      <option value="20">20</option>
                      <option value="50">50</option>
                    </select>
                  </div>
                </div>
              </div>

              <!-- 空結果 -->
              <div *ngIf="!loading && !error && hasSearched && searchResults.length === 0" class="text-center py-12">
                <div class="text-gray-400 text-6xl mb-4">🔍</div>
                <h3 class="text-lg font-medium text-gray-900 mb-2">沒有找到相關結果</h3>
                <p class="text-gray-600">請嘗試使用不同的關鍵字或查詢方式</p>
              </div>
            </div>
          </div>

          <!-- 右側收藏夾 -->
          <div class="lg:col-span-1">
            <div class="bg-white rounded-lg shadow-sm border border-gray-200 sticky top-6">
              <div class="p-6 border-b border-gray-200">
                <h3 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
                  ⭐ 收藏夾
                  <span class="bg-gray-100 text-gray-600 text-sm px-2 py-1 rounded-full">{{ favorites.length }}</span>
                </h3>
              </div>
              
              <!-- 收藏列表 -->
              <div class="max-h-96 overflow-y-auto">
                <div *ngIf="favorites.length > 0" class="divide-y divide-gray-200">
                  <div *ngFor="let favorite of favorites" class="p-4 hover:bg-gray-50">
                    <div class="flex items-center justify-between">
                      <button 
                        class="text-sm font-medium text-gray-900 hover:text-orange-600 transition-colors text-left flex-1 truncate"
                        (click)="viewFavoriteDetailsByName(favorite.name)"
                      >
                        {{ favorite.name }}
                      </button>
                      <button 
                        class="ml-2 text-red-500 hover:text-red-700 transition-colors"
                        (click)="removeFavoriteByName(favorite.name)"
                        [attr.aria-label]="'移除收藏：' + favorite.name"
                      >
                        🗑️
                      </button>
                    </div>
                    <div class="text-xs text-gray-500 mt-1">
                      {{ formatDate(favorite.addedAt) }}
                    </div>
                  </div>
                </div>
                
                <!-- 空狀態 -->
                <div *ngIf="favorites.length === 0" class="p-6 text-center">
                  <div class="text-gray-400 text-4xl mb-2">⭐</div>
                  <p class="text-sm text-gray-500">尚無收藏項目</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 人員詳細資料對話框 -->
    <app-person-detail-dialog
      [personId]="selectedPersonId"
      [isVisible]="showDetailDialog"
      (close)="closeDetailDialog()"
    ></app-person-detail-dialog>
  `
})
export class FullTextSearchPage implements OnInit {
  // Utility function for class names
  cn = cn;
  // 搜索相關
  searchKeyword: string = '北大';
  searchType: 'fuzzy' | 'exact' = 'exact';
  loading: boolean = false;
  error: string = '';
  hasSearched: boolean = false;

  // 分頁相關
  searchResults: SearchResult[] = [];
  totalResults: number = 0;
  currentPage: number = 1;
  totalPages: number = 1;
  pageSize: number = 10;

  // 搜索歷史
  searchHistory: SearchHistory[] = [];

  // 收藏相關
  favorites: Favorite[] = [];

  // 數學函數
  Math = Math;

  // 對話框控制
  showDetailDialog: boolean = false;
  selectedPersonId: number | null = null;

  constructor(
    private fullTextSearchService: FullTextSearchService,
    private favoritesService: FavoritesService,
    private projectService: ProjectService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadInitialData();
  }

  // 全選/取消全選
  toggleSelectAll(event: any): void {
    const checked = event.target.checked;
    this.searchResults.forEach(result => result.selected = checked);
  }

  // 切換單個項目選中狀態
  toggleItemSelection(result: SearchResult): void {
    result.selected = !result.selected;
  }

  // 頁面大小變更
  onPageSizeChange(): void {
    this.currentPage = 1;
    this.totalPages = Math.ceil(this.totalResults / this.pageSize);
    // 這裡可以重新載入數據
  }

  // 檢視收藏詳情 (依名稱)
  viewFavoriteDetailsByName(name: string): void {
    console.log('檢視收藏詳情:', name);
    // 從收藏列表中找到對應的人員ID
    const favorite = this.favorites.find(f => f.name === name);
    if (favorite) {
      this.selectedPersonId = favorite.id;
      this.showDetailDialog = true;
    } else {
      console.warn('未找到收藏項目:', name);
    }
  }

  // 移除收藏 (依名稱)
  async removeFavoriteByName(name: string): Promise<void> {
    console.log('移除收藏:', name);
    try {
      // 從收藏列表中找到對應項目
      const favorite = this.favorites.find(f => f.name === name);
      if (favorite) {
        await this.favoritesService.removeFavorite(favorite.id);
        this.favorites = this.favorites.filter(f => f.id !== favorite.id);
        
        // 同步更新搜索結果的收藏狀態
        const searchResult = this.searchResults.find(r => r.id === favorite.id);
        if (searchResult) {
          searchResult.isFavorited = false;
        }
        console.log('✅ 收藏移除成功:', name);
      } else {
        console.warn('未找到收藏項目:', name);
      }
    } catch (error) {
      console.error('❌ 移除收藏失敗:', error);
    }
  }

  // 載入初始數據
  private async loadInitialData(): Promise<void> {
    await Promise.all([
      this.loadSearchHistory(),
      this.loadFavorites()
    ]);
  }

  // 載入搜索歷史
  private async loadSearchHistory(): Promise<void> {
    try {
      this.fullTextSearchService.getSearchHistory().subscribe({
        next: (response) => {
          if (response?.success && response.data) {
            this.searchHistory = response.data.map((keyword: string) => ({
              keyword,
              count: 1,
              lastUsed: new Date().toISOString()
            }));
          }
        },
        error: (error) => {
          console.error('載入搜索歷史失敗:', error);
        }
      });
    } catch (error) {
      console.error('載入搜索歷史失敗:', error);
    }
  }


  // 載入收藏列表
  private async loadFavorites(): Promise<void> {
    try {
      console.log('🔄 開始載入收藏列表');
      const response = await this.favoritesService.getFavorites();
      console.log('📋 收藏服務回應:', response);
      this.favorites = response; // 服務已經處理了數據映射
      console.log('✅ 收藏列表載入完成:', this.favorites);
    } catch (error) {
      console.error('❌ 載入收藏列表失敗:', error);
    }
  }

  // 執行搜索
  async performSearch(): Promise<void> {
    if (!this.searchKeyword.trim()) {
      this.error = '請輸入搜索關鍵字';
      return;
    }

    this.loading = true;
    this.error = '';

    try {
      // 獲取當前專案ID
      const currentProject = this.projectService.getCurrentProject();
      const projectId = currentProject?.id || null;

      const searchRequest: SearchRequest = {
        keyword: this.searchKeyword.trim(),
        type: this.searchType,
        projectId: projectId,
        page: this.currentPage,
        pageSize: this.pageSize
      };

      this.fullTextSearchService.search(searchRequest).subscribe({
        next: (response) => {
          if (response?.success) {
            // 无论是否有搜索结果，都更新搜索历史
            if (response.data?.searchHistory) {
              this.searchHistory = response.data.searchHistory.map((keyword: string) => ({
                keyword,
                count: 1,
                lastUsed: new Date().toISOString()
              }));
            }
            
            if (response.data) {
              this.searchResults = response.data.results.map((item: PersonSearchResult) => ({
                id: item.id,
                name: item.name,
                gender: item.gender,
                source: item.source,
                createdAt: item.createdAt,
                isFavorited: this.favorites.some(f => f.id === item.id)
              }));
              this.totalResults = response.data.totalCount;
              this.totalPages = Math.ceil(this.totalResults / this.pageSize);
            } else {
              // 没有data但success=true的情况，清空结果
              this.searchResults = [];
              this.totalResults = 0;
              this.totalPages = 1;
            }
            this.hasSearched = true;
          } else {
            this.error = response?.message || '搜索失敗';
          }
          this.loading = false;
        },
        error: (error) => {
          console.error('搜索失敗:', error);
          this.error = '搜索失敗，請稍後再試';
          this.loading = false;
          this.hasSearched = true;
        }
      });
    } catch (error) {
      console.error('搜索失敗:', error);
      this.error = '搜索失敗，請稍後再試';
      this.loading = false;
      this.hasSearched = true;
    }
  }



  // 使用歷史關鍵字
  useHistoryKeyword(keyword: string): void {
    this.searchKeyword = keyword;
    this.performSearch();
  }


  // 重置搜索
  resetSearch(): void {
    this.searchKeyword = '';
    this.searchType = 'fuzzy';
    this.searchResults = [];
    this.totalResults = 0;
    this.currentPage = 1;
    this.totalPages = 1;
    this.hasSearched = false;
    this.error = '';
  }



  // 切換收藏狀態
  async toggleFavorite(result: SearchResult): Promise<void> {
    try {
      if (result.isFavorited) {
        // 移除收藏 - 需要先找到對應的收藏ID
        const favorite = this.favorites.find(f => f.id === result.id);
        if (favorite) {
          await this.favoritesService.removeFavorite(favorite.id);
          result.isFavorited = false;
          this.favorites = this.favorites.filter(f => f.id !== result.id);
        }
      } else {
        // 添加收藏
        await this.favoritesService.addFavorite({
          personId: result.id,
          personName: result.name
        });
        result.isFavorited = true;
        this.favorites.push({
          id: result.id,
          name: result.name,
          addedAt: new Date().toISOString()
        });
      }
    } catch (error) {
      console.error('切換收藏狀態失敗:', error);
    }
  }

  // 移除收藏
  async removeFavorite(favorite: Favorite): Promise<void> {
    try {
      await this.favoritesService.removeFavorite(favorite.id);
      this.favorites = this.favorites.filter(f => f.id !== favorite.id);
      
      // 同步更新搜索結果的收藏狀態
      const searchResult = this.searchResults.find(r => r.id === favorite.id);
      if (searchResult) {
        searchResult.isFavorited = false;
      }
    } catch (error) {
      console.error('移除收藏失敗:', error);
    }
  }

  // 清空所有收藏
  async clearAllFavorites(): Promise<void> {
    try {
      await this.favoritesService.clearAllFavorites();
      this.favorites = [];
      
      // 同步更新搜索結果的收藏狀態
      this.searchResults.forEach(result => {
        result.isFavorited = false;
      });
    } catch (error) {
      console.error('清空收藏失敗:', error);
    }
  }

  // 查看詳情
  viewDetails(result: SearchResult): void {
    this.selectedPersonId = result.id;
    this.showDetailDialog = true;
  }

  // 查看收藏詳情
  viewFavoriteDetails(favorite: Favorite): void {
    this.selectedPersonId = favorite.id;
    this.showDetailDialog = true;
  }



  // 關閉詳情對話框
  closeDetailDialog(): void {
    this.showDetailDialog = false;
    this.selectedPersonId = null;
  }

  // 導出結果
  async exportResults(): Promise<void> {
    try {
      if (this.searchResults.length === 0) {
        this.error = '沒有可導出的結果';
        return;
      }
      
      await this.fullTextSearchService.exportResults(this.searchResults);
    } catch (error) {
      console.error('導出失敗:', error);
      this.error = '導出失敗，請稍後再試';
    }
  }

  // 跳轉到指定頁面
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.currentPage = page;
      this.performSearch();
    }
  }

  // 獲取頁碼數組
  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;
    
    if (this.totalPages <= maxVisiblePages) {
      for (let i = 1; i <= this.totalPages; i++) {
        pages.push(i);
      }
    } else {
      const start = Math.max(1, this.currentPage - Math.floor(maxVisiblePages / 2));
      const end = Math.min(this.totalPages, start + maxVisiblePages - 1);
      
      for (let i = start; i <= end; i++) {
        pages.push(i);
      }
    }
    
    return pages;
  }

  // 格式化日期
  formatDate(dateString: string): string {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateString;
    }
  }

  // 獲取操作按鈕配置
  getActionButtons(result: SearchResult): ActionButton[] {
    return [
      {
        id: 'view',
        label: '檢視',
        buttonClass: 'btn-action btn-view',
        title: '檢視詳細資料'
      },
      {
        id: 'favorite',
        label: '收藏',
        buttonClass: 'btn-action btn-favorite',
        isActive: result.isFavorited,
        title: result.isFavorited ? '取消收藏' : '加入收藏'
      }
    ];
  }

  // 處理操作按鈕點擊
  handleActionClick(event: { actionId: string; event: Event }, result: SearchResult): void {
    switch (event.actionId) {
      case 'view':
        this.viewDetails(result);
        break;
      case 'favorite':
        this.toggleFavorite(result);
        break;
    }
  }

  // 獲取收藏夾操作按鈕
  getFavoriteActions(favorite: Favorite): ActionButton[] {
    return [
      {
        id: 'delete',
        icon: '🗑️',
        buttonClass: 'btn-action btn-delete',
        title: '刪除收藏',
        ariaLabel: `刪除收藏：${favorite.name}`
      },
      {
        id: 'view',
        icon: '›',
        buttonClass: 'btn-action btn-view',
        title: '檢視詳情',
        ariaLabel: `檢視 ${favorite.name} 的詳情`
      }
    ];
  }

  // 處理收藏夾操作
  handleFavoriteAction(event: { actionId: string; event: Event }, favorite: Favorite): void {
    switch (event.actionId) {
      case 'delete':
        this.removeFavoriteByName(favorite.name);
        break;
      case 'view':
        this.viewFavoriteDetailsByName(favorite.name);
        break;
    }
  }
} 