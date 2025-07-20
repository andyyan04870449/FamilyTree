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
  selected: boolean;
  isFavorited: boolean;
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

// 分析項目接口
interface AnalysisItem {
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
  searchKeyword: string = '';
  searchType: 'fuzzy' | 'exact' = 'fuzzy';
  loading: boolean = false;
  error: string = '';
  hasSearched: boolean = false;

  // 分頁相關
  searchResults: SearchResult[] = [];
  totalResults: number = 0;
  currentPage: number = 1;
  totalPages: number = 1;
  pageSize: number = 10;
  selectAll: boolean = false;

  // 歷史和熱門關鍵字
  searchHistory: SearchHistory[] = [];
  popularKeywords: PopularKeyword[] = [];

  // 收藏相關
  favorites: Favorite[] = [];

  // 分析清單
  analysisList: AnalysisItem[] = [];

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
      const response = await this.favoritesService.getFavorites();
      this.favorites = response.map((item: any) => ({
        id: item.personId || item.id,
        name: item.personName || item.name,
        addedAt: item.favoritedAt || item.addedAt
      }));
    } catch (error) {
      console.error('載入收藏列表失敗:', error);
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
              selected: false,
              isFavorited: this.favorites.some(f => f.id === item.id)
            }));
            this.totalResults = response.data.totalCount;
            this.totalPages = Math.ceil(this.totalResults / this.pageSize);
            this.hasSearched = true;
            
            // 更新搜索結果的選中狀態，保持與分析清單的同步
            this.syncSearchResultsWithAnalysisList();
          } else {
            this.error = response?.message || '搜索失敗';
          }
          this.loading = false;
        },
        error: (error) => {
          console.error('搜索失敗:', error);
          this.error = '搜索失敗，請稍後再試';
          this.loading = false;
        }
      });
    } catch (error) {
      console.error('搜索失敗:', error);
      this.error = '搜索失敗，請稍後再試';
      this.loading = false;
    }
  }

  // 同步搜索結果與分析清單的選中狀態
  private syncSearchResultsWithAnalysisList(): void {
    this.searchResults.forEach(result => {
      // 如果該人員在分析清單中，則設為選中狀態
      result.selected = this.analysisList.some(item => item.id === result.id);
    });
    this.updateSelectAll();
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
    this.selectAll = false;
  }

  // 全選/取消全選
  toggleSelectAll(): void {
    this.searchResults.forEach(result => {
      result.selected = this.selectAll;
    });
    this.updateAnalysisList();
  }

  // 更新全選狀態
  updateSelectAll(): void {
    this.selectAll = this.searchResults.length > 0 && this.searchResults.every(result => result.selected);
  }

  // 切換單個結果的選中狀態
  toggleResultSelection(result: SearchResult): void {
    result.selected = !result.selected;
    this.updateAnalysisList();
    this.updateSelectAll();
  }

  // 更新分析清單
  private updateAnalysisList(): void {
    // 移除未選中的項目
    this.analysisList = this.analysisList.filter(item => 
      this.searchResults.some(result => result.id === item.id && result.selected)
    );

    // 添加新選中的項目
    this.searchResults.forEach(result => {
      if (result.selected && !this.analysisList.some(item => item.id === result.id)) {
        this.analysisList.push({
          id: result.id,
          name: result.name,
          addedAt: new Date().toISOString()
        });
      }
    });
  }

  // 從分析清單中移除項目
  removeFromAnalysisList(item: AnalysisItem): void {
    this.analysisList = this.analysisList.filter(i => i.id !== item.id);
    
    // 同步更新搜索結果的選中狀態
    const searchResult = this.searchResults.find(r => r.id === item.id);
    if (searchResult) {
      searchResult.selected = false;
      this.updateSelectAll();
    }
  }

  // 清空分析清單
  clearAnalysisList(): void {
    this.analysisList = [];
    
    // 同步更新搜索結果的選中狀態
    this.searchResults.forEach(result => {
      result.selected = false;
    });
    this.selectAll = false;
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

  // 查看分析項目詳情
  viewAnalysisItemDetails(item: AnalysisItem): void {
    this.selectedPersonId = item.id;
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
      const selectedResults = this.searchResults.filter(result => result.selected);
      if (selectedResults.length === 0) {
        this.error = '請先選擇要導出的結果';
        return;
      }
      
      await this.fullTextSearchService.exportResults(selectedResults);
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

  // 獲取選中數量
  getSelectedCount(): number {
    return this.searchResults.filter(result => result.selected).length;
  }

  // 獲取選中的人員ID列表
  getSelectedPersonIds(): number[] {
    return this.searchResults
      .filter(result => result.selected)
      .map(result => result.id);
  }

  // 顯示關聯圖譜
  showRelationshipGraph(): void {
    const selectedIds = this.getSelectedPersonIds();
    if (selectedIds.length === 0) {
      this.error = '請先選擇要分析的人員';
      return;
    }
    
    // 導航到關聯圖譜頁面
    this.router.navigate(['/relationship-graph'], {
      queryParams: { personIds: selectedIds.join(',') }
    });
  }
} 