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

interface AnalysisItem {
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

  // 分析清單相關
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
        this.searchHistory = response.data.map((keyword: string, index: number) => ({
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
        this.popularKeywords = response.data.map((keyword: string, index: number) => ({
          keyword,
          count: 10 - index
        }));
      }
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
      const response = await this.fullTextSearchService.search({
        keyword: this.searchKeyword.trim(),
        type: this.searchType,
        page: this.currentPage,
        pageSize: this.pageSize
      }).toPromise();

      if (response?.success && response.data) {
        this.searchResults = response.data.results.map((item: any) => ({
          ...item,
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
    } catch (error) {
      console.error('搜索失敗:', error);
      this.error = '搜索失敗，請稍後再試';
    } finally {
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
    this.selectAll = this.searchResults.length > 0 && 
                    this.searchResults.every(result => result.selected);
    // 當全選狀態改變時，同步更新分析清單
    this.updateAnalysisList();
  }

  // 更新分析清單
  private updateAnalysisList(): void {
    const selectedResults = this.searchResults.filter(result => result.selected);
    
    // 將選中的結果加入分析清單（避免重複）
    selectedResults.forEach(result => {
      const existingItem = this.analysisList.find(item => item.id === result.id);
      if (!existingItem) {
        this.analysisList.push({
          id: result.id,
          name: result.name,
          addedAt: new Date().toISOString()
        });
      }
    });

    // 移除未選中的項目（僅限於當前搜索結果中的人員）
    this.analysisList = this.analysisList.filter(item => {
      const isInCurrentResults = this.searchResults.some(result => result.id === item.id);
      if (isInCurrentResults) {
        // 如果在當前搜索結果中，檢查是否仍然被選中
        return selectedResults.some(result => result.id === item.id);
      }
      // 如果不在當前搜索結果中，保留在分析清單中
      return true;
    });
  }

  // 從分析清單移除項目
  removeFromAnalysisList(item: AnalysisItem): void {
    this.analysisList = this.analysisList.filter(analysisItem => analysisItem.id !== item.id);
    
    // 同時取消對應搜索結果的選中狀態（如果該人員在當前搜索結果中）
    const searchResult = this.searchResults.find(result => result.id === item.id);
    if (searchResult) {
      searchResult.selected = false;
      this.updateSelectAll();
    }
  }

  // 清空分析清單
  clearAnalysisList(): void {
    this.analysisList = [];
    
    // 取消所有當前搜索結果的選中狀態
    this.searchResults.forEach(result => {
      result.selected = false;
    });
    this.selectAll = false;
  }

  // 切換收藏狀態
  async toggleFavorite(result: SearchResult): Promise<void> {
    try {
      if (result.isFavorited) {
        await this.favoritesService.removeFavorite(result.id);
        this.favorites = this.favorites.filter(f => f.id !== result.id);
        result.isFavorited = false;
      } else {
        await this.favoritesService.addFavorite({
          personId: result.id,
          personName: result.name
        });
        this.favorites.push({
          id: result.id,
          name: result.name,
          addedAt: new Date().toISOString()
        });
        result.isFavorited = true;
      }
    } catch (error) {
      console.error('切換收藏狀態失敗:', error);
      this.error = '收藏操作失敗，請稍後再試';
    }
  }

  // 移除收藏
  async removeFavorite(favorite: Favorite): Promise<void> {
    try {
      await this.favoritesService.removeFavorite(favorite.id);
      this.favorites = this.favorites.filter(f => f.id !== favorite.id);
      
      // 更新搜索結果中的收藏狀態
      const searchResult = this.searchResults.find(result => result.id === favorite.id);
      if (searchResult) {
        searchResult.isFavorited = false;
      }
    } catch (error) {
      console.error('移除收藏失敗:', error);
      this.error = '移除收藏失敗，請稍後再試';
    }
  }

  // 清空所有收藏
  async clearAllFavorites(): Promise<void> {
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

  // 從分析清單跳轉到關聯圖譜
  showRelationshipGraph(): void {
    if (this.analysisList.length === 0) {
      return;
    }
    
    // 獲取分析清單中的人員ID並跳轉到關聯圖譜頁面
    const selectedIds = this.analysisList.map(item => item.id);
    const personIdsParam = selectedIds.join(',');
    
    console.log('🔗 跳轉到關聯圖譜頁面，分析清單人員ID:', selectedIds);
    this.router.navigate(['/relationship-graph', personIdsParam]);
  }
} 