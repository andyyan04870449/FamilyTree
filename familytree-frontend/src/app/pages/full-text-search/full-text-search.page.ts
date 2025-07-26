// 全文檢索頁面 - 提供全文搜索、收藏管理、搜索歷史等功能
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { FullTextSearchService, SearchRequest, SearchResult as ApiSearchResult, PersonSearchResult } from '../../services/fulltext-search.service';
import { FavoritesService } from '../../services/favorites.service';
import { PersonDetailDialogComponent } from '../../components/person-detail-dialog/person-detail-dialog.component';

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

// 熱門關鍵字接口
interface PopularKeyword {
  keyword: string;
  count: number;
}

// 收藏接口
interface Favorite {
  id: number;
  name: string;
  addedAt: string;
}



@Component({
  selector: 'app-full-text-search',
  templateUrl: './full-text-search.page.html',
  styleUrls: ['./full-text-search.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, PersonDetailDialogComponent]
})
export class FullTextSearchPage implements OnInit {
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

  // 歷史和熱門關鍵字
  searchHistory: SearchHistory[] = [];
  popularKeywords: PopularKeyword[] = [];

  // 收藏相關
  favorites: Favorite[] = [];



  // 對話框控制
  showDetailDialog: boolean = false;
  selectedPersonId: number | null = null;

  constructor(
    private fullTextSearchService: FullTextSearchService,
    private favoritesService: FavoritesService,
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
      this.loadPopularKeywords(),
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

  // 載入熱門關鍵字
  private async loadPopularKeywords(): Promise<void> {
    try {
      this.fullTextSearchService.getPopularKeywords().subscribe({
        next: (response) => {
          if (response?.success && response.data) {
            this.popularKeywords = response.data.map((keyword: string) => ({
              keyword,
              count: 1
            }));
          }
        },
        error: (error) => {
          console.error('載入熱門關鍵字失敗:', error);
        }
      });
    } catch (error) {
      console.error('載入熱門關鍵字失敗:', error);
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
      const searchRequest: SearchRequest = {
        keyword: this.searchKeyword.trim(),
        type: this.searchType,
        page: this.currentPage,
        pageSize: this.pageSize
      };

      this.fullTextSearchService.search(searchRequest).subscribe({
        next: (response) => {
          if (response?.success && response.data) {
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

  // 使用熱門關鍵字
  usePopularKeyword(keyword: string): void {
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




} 