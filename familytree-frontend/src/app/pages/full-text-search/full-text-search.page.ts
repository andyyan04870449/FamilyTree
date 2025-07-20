// 全文檢索頁面 - 提供關鍵字搜索人員資料功能
// 主要功能：精準/模糊查詢、歷史記錄、熱門關鍵字、收藏管理、結果匯出

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PersonDetailDialogComponent } from '../../components/person-detail-dialog/person-detail-dialog.component';
import { FullTextSearchService } from '../../services/fulltext-search.service';
import { FavoritesService } from '../../services/favorites.service';

interface SearchResult {
  id: number;
  name: string;
  gender: string;
  source: string;
  createdAt: string;
  selected: boolean;
  isFavorited: boolean;
}

interface SearchHistory {
  keyword: string;
  count: number;
  lastUsed: string;
}

interface PopularKeyword {
  keyword: string;
  count: number;
}

interface Favorite {
  id: number;
  name: string;
  addedAt: string;
}

@Component({
  selector: 'app-full-text-search',
  standalone: true,
  imports: [CommonModule, FormsModule, PersonDetailDialogComponent],
  templateUrl: './full-text-search.page.html',
  styleUrls: ['./full-text-search.page.scss']
})
export class FullTextSearchPage implements OnInit {
  // 搜索相關
  searchKeyword: string = '';
  searchType: 'fuzzy' | 'exact' = 'fuzzy';
  loading: boolean = false;
  error: string = '';
  hasSearched: boolean = false;

  // 搜索結果
  searchResults: SearchResult[] = [];
  totalResults: number = 0;
  currentPage: number = 1;
  totalPages: number = 1;
  pageSize: number = 10;
  selectAll: boolean = false;

  // 歷史記錄和熱門關鍵字
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

  // 載入初始資料
  private async loadInitialData(): Promise<void> {
    try {
      await Promise.all([
        this.loadSearchHistory(),
        this.loadPopularKeywords(),
        this.loadFavorites()
      ]);
    } catch (error) {
      console.error('載入初始資料失敗:', error);
      this.error = '載入資料失敗，請重新整理頁面';
    }
  }

  // 載入搜索歷史
  private async loadSearchHistory(): Promise<void> {
    try {
      const response = await this.fullTextSearchService.getSearchHistory().toPromise();
      if (response?.success && response.data) {
        this.searchHistory = response.data.map((keyword, index) => ({
          keyword,
          count: 1,
          lastUsed: new Date().toISOString()
        }));
      }
    } catch (error) {
      console.error('載入搜索歷史失敗:', error);
    }
  }

  // 載入熱門關鍵字
  private async loadPopularKeywords(): Promise<void> {
    try {
      const response = await this.fullTextSearchService.getPopularKeywords().toPromise();
      if (response?.success && response.data) {
        this.popularKeywords = response.data.map((keyword, index) => ({
          keyword,
          count: 10 - index
        }));
      }
    } catch (error) {
      console.error('載入熱門關鍵字失敗:', error);
    }
  }

  // 載入收藏
  private async loadFavorites(): Promise<void> {
    try {
      this.favorites = await this.favoritesService.getFavorites();
    } catch (error) {
      console.error('載入收藏失敗:', error);
    }
  }

  // 執行搜索
  async performSearch(): Promise<void> {
    if (!this.searchKeyword.trim()) {
      return;
    }

    this.loading = true;
    this.error = '';
    this.hasSearched = true;

    try {
      // 執行搜索（搜索時會自動記錄關鍵字）
      // 執行搜索
      const response = await this.fullTextSearchService.search({
        keyword: this.searchKeyword.trim(),
        type: this.searchType,
        page: this.currentPage,
        pageSize: this.pageSize
      }).toPromise();

      if (response?.success && response.data) {
        this.searchResults = response.data.results.map((result: any) => ({
          id: result.id,
          name: result.name,
          gender: result.gender,
          source: result.source,
          createdAt: result.createdAt,
          selected: false,
          isFavorited: this.favorites.some(fav => fav.id === result.id)
        }));

        this.totalResults = response.data.totalCount;
        this.totalPages = Math.ceil(this.totalResults / this.pageSize);
      } else {
        this.searchResults = [];
        this.totalResults = 0;
        this.totalPages = 1;
      }

      // 重新載入歷史記錄和熱門關鍵字
      await Promise.all([
        this.loadSearchHistory(),
        this.loadPopularKeywords()
      ]);

    } catch (error) {
      console.error('搜索失敗:', error);
      this.error = '搜索失敗，請稍後再試';
      this.searchResults = [];
      this.totalResults = 0;
      this.totalPages = 1;
    } finally {
      this.loading = false;
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
    this.selectAll = false;
  }

  // 切換全選
  toggleSelectAll(): void {
    this.searchResults.forEach(result => {
      result.selected = this.selectAll;
    });
  }

  // 更新全選狀態
  updateSelectAll(): void {
    this.selectAll = this.searchResults.length > 0 && 
                    this.searchResults.every(result => result.selected);
  }

  // 切換收藏
  async toggleFavorite(result: SearchResult): Promise<void> {
    try {
      if (result.isFavorited) {
        await this.favoritesService.removeFavorite(result.id);
        result.isFavorited = false;
        this.favorites = this.favorites.filter(fav => fav.id !== result.id);
      } else {
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
      console.error('切換收藏失敗:', error);
      this.error = '收藏操作失敗，請稍後再試';
    }
  }

  // 移除收藏
  async removeFavorite(favorite: Favorite): Promise<void> {
    try {
      await this.favoritesService.removeFavorite(favorite.id);
      this.favorites = this.favorites.filter(fav => fav.id !== favorite.id);
      
      // 更新搜索結果中的收藏狀態
      const result = this.searchResults.find(r => r.id === favorite.id);
      if (result) {
        result.isFavorited = false;
      }
    } catch (error) {
      console.error('移除收藏失敗:', error);
      this.error = '移除收藏失敗，請稍後再試';
    }
  }

  // 清空所有收藏
  async clearAllFavorites(): Promise<void> {
    if (!confirm('確定要清空所有收藏嗎？')) {
      return;
    }

    try {
      await this.favoritesService.clearAllFavorites();
      this.favorites = [];
      
      // 更新搜索結果中的收藏狀態
      this.searchResults.forEach(result => {
        result.isFavorited = false;
      });
    } catch (error) {
      console.error('清空收藏失敗:', error);
      this.error = '清空收藏失敗，請稍後再試';
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

  // 匯出結果
  async exportResults(): Promise<void> {
    const selectedResults = this.searchResults.filter(result => result.selected);
    
    if (selectedResults.length === 0) {
      this.error = '請先選擇要匯出的項目';
      return;
    }

    try {
      await this.fullTextSearchService.exportResults(selectedResults);
    } catch (error) {
      console.error('匯出失敗:', error);
      this.error = '匯出失敗，請稍後再試';
    }
  }

  // 分頁導航
  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }

    this.currentPage = page;
    this.performSearch();
  }

  // 取得分頁數字
  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;
    const start = Math.max(1, this.currentPage - Math.floor(maxVisible / 2));
    const end = Math.min(this.totalPages, start + maxVisible - 1);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }

  // 格式化日期
  formatDate(dateString: string): string {
    if (!dateString) return '--';
    
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('zh-TW');
    } catch {
      return '--';
    }
  }

  // 關聯圖譜相關方法
  getSelectedCount(): number {
    return this.searchResults.filter(result => result.selected).length;
  }

  getSelectedPersonIds(): number[] {
    return this.searchResults
      .filter(result => result.selected)
      .map(result => result.id);
  }

  showRelationshipGraph(): void {
    if (this.getSelectedCount() === 0) {
      return;
    }
    
    // 獲取選中的人員ID並跳轉到關聯圖譜頁面
    const selectedIds = this.getSelectedPersonIds();
    const personIdsParam = selectedIds.join(',');
    
    console.log('🔗 跳轉到關聯圖譜頁面，選中人員ID:', selectedIds);
    this.router.navigate(['/relationship-graph', personIdsParam]);
  }
} 