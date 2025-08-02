// 全文檢索服務：提供搜索、收藏、歷史記錄等功能的API交互
// 主要功能：全文搜索、收藏管理、搜索歷史、熱門關鍵字

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { AppConstants } from '../constants/app.constants';
import { ProjectService } from './project.service';

// 搜索請求接口
export interface SearchRequest {
  keyword: string;
  type: 'exact' | 'fuzzy';
  projectId?: string | null;
  page?: number;
  pageSize?: number;
}

// 搜索結果接口
export interface SearchResult {
  success: boolean;
  message: string;
  data?: SearchData;
}

// 搜索數據接口
export interface SearchData {
  keyword: string;
  searchType: string;
  totalCount: number;
  page: number;
  pageSize: number;
  results: PersonSearchResult[];
  popularKeywords: string[];
  searchHistory: string[];
}

// 人員搜索結果接口
export interface PersonSearchResult {
  id: number;
  name: string;
  gender: string;
  birthday: string;
  nationality: string;
  mobile: string;
  phone?: string;
  idNumber?: string;
  passportNumber?: string;
  familyRelationships?: string;
  friends?: string;
  activities?: string;
  experience?: string;
  education?: string;
  publications?: string;
  email?: string;
  currentEmployer?: string;
  address?: string;
  mailingAddress?: string;
  birthplace?: string;
  ethnicity?: string;
  ancestralOrigin?: string;
  politicalParty?: string;
  onlineAccounts?: string;
  frequentLocations?: string;
  travelHistory?: string;
  discoveryProcess?: string;
  remarks?: string;
  fileMd5?: string;
  profileData?: string;
  createdAt: string;
  updatedAt: string;
  isFavorited: boolean;
  source: string;
  matchedFields: string;
}

// 收藏請求接口
export interface FavoriteRequest {
  personId: number;
  personName: string;
}

// 收藏結果接口
export interface FavoriteResult {
  success: boolean;
  message: string;
  data?: FavoriteData;
}

// 收藏數據接口
export interface FavoriteData {
  id: number;
  personId: number;
  personName: string;
  favoritedAt: string;
}

// 收藏列表結果接口
export interface FavoriteListResult {
  success: boolean;
  message: string;
  data: FavoriteItem[];
}

// 收藏項目接口
export interface FavoriteItem {
  id: number;
  personId: number;
  personName: string;
  lastViewedTime?: string;
  favoritedAt: string;
  displayTime: string;
  canDelete: boolean;
}

// API回應接口
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  timestamp: string;
}

// 搜索統計接口
export interface SearchStatistics {
  totalSearches: number;
  uniqueKeywords: number;
  totalFavorites: number;
  searchTypeStats: { [key: string]: number };
}

@Injectable({
  providedIn: 'root'
})
export class FullTextSearchService {
  private baseUrl = AppConstants.API_BASE_URL;
  private searchUrl = `${this.baseUrl}/FullTextSearch`;
  private favoritesUrl = `${this.baseUrl}/Favorites`;

  // 狀態管理
  private favoritesSubject = new BehaviorSubject<FavoriteItem[]>([]);
  public favorites$ = this.favoritesSubject.asObservable();


  private searchHistorySubject = new BehaviorSubject<string[]>([]);
  public searchHistory$ = this.searchHistorySubject.asObservable();

  constructor(
    private http: HttpClient,
    private projectService: ProjectService
  ) {
    // FullTextSearchService 初始化
    // 初始化時載入收藏列表和搜索歷史
    this.loadInitialData();
  }

  /**
   * 獲取當前專案 ID 並創建 HTTP 參數
   */
  private getProjectParams(): HttpParams {
    const currentProject = this.projectService.getCurrentProject();
    let params = new HttpParams();
    
    if (currentProject) {
      params = params.set('project_id', currentProject.id);
      // 添加專案 ID 到請求
    } else {
      // 沒有當前專案，不進行 API 請求
      throw new Error('請先選擇專案');
    }
    
    return params;
  }

  /**
   * 初始化載入資料
   */
  private loadInitialData(): void {
    // 載入初始資料
    try {
      this.loadFavorites();
      this.loadSearchHistory();
    } catch (error) {
      // 初始化時沒有當前專案，跳過資料載入
    }
  }

  // ============ 搜索功能 ============

  /**
   * 執行全文檢索搜索 - 全專案搜尋
   */
  search(request: SearchRequest): Observable<SearchResult> {
    // 執行全文檢索搜索
    
    const searchRequest = {
      keyword: request.keyword,
      searchType: request.type,
      projectId: request.projectId || null,
      page: request.page || 1,
      pageSize: request.pageSize || 10
    };

    return this.http.post<SearchResult>(`${this.searchUrl}/search`, searchRequest);
  }


  /**
   * 獲取搜索歷史
   */
  getSearchHistory(): Observable<ApiResponse<string[]>> {
    // 獲取搜索歷史
    const params = this.getProjectParams();
    return this.http.get<ApiResponse<string[]>>(`${this.searchUrl}/search-history`, { params });
  }

  /**
   * 清除搜索歷史
   */
  clearSearchHistory(): Observable<ApiResponse<any>> {
    // 清除搜索歷史
    const params = this.getProjectParams();
    return this.http.delete<ApiResponse<any>>(`${this.searchUrl}/search-history`, { params });
  }

  /**
   * 獲取搜索統計
   */
  getSearchStatistics(): Observable<ApiResponse<SearchStatistics>> {
    // 獲取搜索統計
    const params = this.getProjectParams();
    return this.http.get<ApiResponse<SearchStatistics>>(`${this.searchUrl}/statistics`, { params });
  }

  /**
   * 記錄搜索關鍵字
   * 注意：搜索時會自動記錄關鍵字，此方法僅為相容性保留
   */
  recordSearch(keyword: string): Promise<void> {
    // 搜索關鍵字會自動記錄，無需單獨調用
    return Promise.resolve();
  }

  /**
   * 匯出搜索結果
   * 注意：後端暫未實現匯出功能，此方法僅為相容性保留
   */
  exportResults(results: any[]): Promise<void> {
    // 匯出功能暫未實現
    return Promise.reject(new Error('匯出功能暫未實現'));
  }

  // ============ 收藏功能 ============

  /**
   * 獲取收藏列表
   */
  getFavorites(): Observable<FavoriteListResult> {
    // 獲取收藏列表
    const params = this.getProjectParams();
    return this.http.get<FavoriteListResult>(`${this.favoritesUrl}`, { params });
  }

  /**
   * 添加收藏
   */
  addFavorite(request: FavoriteRequest): Observable<FavoriteResult> {
    // 添加收藏
    const params = this.getProjectParams();
    return this.http.post<FavoriteResult>(`${this.favoritesUrl}`, request, { params });
  }

  /**
   * 刪除收藏（通過收藏ID）
   */
  removeFavorite(favoriteId: number): Observable<FavoriteResult> {
    // 刪除收藏
    const params = this.getProjectParams();
    return this.http.delete<FavoriteResult>(`${this.favoritesUrl}/${favoriteId}`, { params });
  }

  /**
   * 刪除收藏（通過人員ID）
   */
  removeFavoriteByPersonId(personId: number): Observable<FavoriteResult> {
    // 通過人員ID刪除收藏
    const params = this.getProjectParams();
    return this.http.delete<FavoriteResult>(`${this.favoritesUrl}/person/${personId}`, { params });
  }

  /**
   * 檢查收藏狀態
   */
  checkFavoriteStatus(personId: number): Observable<ApiResponse<any>> {
    // 檢查收藏狀態
    const params = this.getProjectParams();
    return this.http.get<ApiResponse<any>>(`${this.favoritesUrl}/check/${personId}`, { params });
  }

  /**
   * 更新查看時間
   */
  updateViewTime(personId: number): Observable<ApiResponse<any>> {
    // 更新查看時間
    const params = this.getProjectParams();
    return this.http.put<ApiResponse<any>>(`${this.favoritesUrl}/view/${personId}`, {}, { params });
  }

  /**
   * 獲取收藏統計
   */
  getFavoriteStatistics(): Observable<ApiResponse<any>> {
    // 獲取收藏統計
    const params = this.getProjectParams();
    return this.http.get<ApiResponse<any>>(`${this.favoritesUrl}/statistics`, { params });
  }

  // ============ 狀態管理方法 ============

  /**
   * 載入收藏列表並更新狀態
   */
     loadFavorites(): void {
     this.getFavorites().subscribe({
       next: (result) => {
         if (result.success && result.data) {
           // 收藏列表載入成功
           this.favoritesSubject.next(result.data);
         } else {
           // 收藏列表載入失敗
           this.favoritesSubject.next([]);
         }
       },
       error: (error) => {
         // 收藏列表載入錯誤
         this.favoritesSubject.next([]);
       }
     });
   }


  /**
   * 載入搜索歷史並更新狀態
   */
  loadSearchHistory(): void {
    this.getSearchHistory().subscribe({
      next: (result) => {
        if (result.success && result.data) {
          // 搜索歷史載入成功
          this.searchHistorySubject.next(result.data);
        } else {
          // 搜索歷史載入失敗
          this.searchHistorySubject.next([]);
        }
      },
              error: (error) => {
          // 搜索歷史載入錯誤
        this.searchHistorySubject.next([]);
      }
    });
  }

  /**
   * 添加收藏並更新狀態
   */
  addFavoriteAndUpdate(request: FavoriteRequest): Observable<FavoriteResult> {
    return new Observable(observer => {
      this.addFavorite(request).subscribe({
        next: (result) => {
          if (result.success) {
            // 重新載入收藏列表
            this.loadFavorites();
          }
          observer.next(result);
          observer.complete();
        },
        error: (error) => {
          observer.error(error);
        }
      });
    });
  }

  /**
   * 刪除收藏並更新狀態
   */
  removeFavoriteAndUpdate(favoriteId: number): Observable<FavoriteResult> {
    return new Observable(observer => {
      this.removeFavorite(favoriteId).subscribe({
        next: (result) => {
          if (result.success) {
            // 重新載入收藏列表
            this.loadFavorites();
          }
          observer.next(result);
          observer.complete();
        },
        error: (error) => {
          observer.error(error);
        }
      });
    });
  }

  /**
   * 通過人員ID刪除收藏並更新狀態
   */
  removeFavoriteByPersonIdAndUpdate(personId: number): Observable<FavoriteResult> {
    return new Observable(observer => {
      this.removeFavoriteByPersonId(personId).subscribe({
        next: (result) => {
          if (result.success) {
            // 重新載入收藏列表
            this.loadFavorites();
          }
          observer.next(result);
          observer.complete();
        },
        error: (error) => {
          observer.error(error);
        }
      });
    });
  }

  /**
   * 清除搜索歷史並更新狀態
   */
  clearSearchHistoryAndUpdate(): Observable<ApiResponse<any>> {
    return new Observable(observer => {
      this.clearSearchHistory().subscribe({
        next: (result) => {
          if (result.success) {
            // 重新載入搜索歷史
            this.loadSearchHistory();
          }
          observer.next(result);
          observer.complete();
        },
        error: (error) => {
          observer.error(error);
        }
      });
    });
  }

  // ============ 工具方法 ============

  /**
   * 格式化顯示時間
   */
  formatDisplayTime(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const minutes = Math.floor(diff / (1000 * 60));
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));

    if (minutes < 1) return '剛剛';
    if (hours < 1) return `${minutes} 分鐘前`;
    if (days < 1) return `${hours} 小時前`;
    if (days < 7) return `${days} 天前`;
    
    return date.toLocaleDateString('zh-TW');
  }

  /**
   * 檢查是否為有效的搜索關鍵字
   */
  isValidSearchKeyword(keyword: string): boolean {
    return !!(keyword && keyword.trim().length > 0);
  }

  /**
   * 清理搜索關鍵字
   */
  cleanSearchKeyword(keyword: string): string {
    return keyword.trim();
  }

  /**
   * 獲取當前收藏列表
   */
  getCurrentFavorites(): FavoriteItem[] {
    return this.favoritesSubject.value;
  }


  /**
   * 獲取當前搜索歷史
   */
  getCurrentSearchHistory(): string[] {
    return this.searchHistorySubject.value;
  }

  /**
   * 檢查人員是否已收藏
   */
  isPersonFavorited(personId: number): boolean {
    const favorites = this.getCurrentFavorites();
    return favorites.some(fav => fav.personId === personId);
  }
} 