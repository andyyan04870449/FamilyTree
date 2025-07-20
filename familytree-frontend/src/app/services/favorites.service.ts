// 收藏服務：提供人員收藏管理功能
// 主要功能：添加收藏、移除收藏、清空收藏、獲取收藏列表

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConstants } from '../constants/app.constants';

export interface Favorite {
  id: number;
  name: string;
  addedAt: string;
}

export interface FavoriteRequest {
  personId: number;
  personName: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
}

@Injectable({
  providedIn: 'root'
})
export class FavoritesService {
  private baseUrl = AppConstants.API_BASE_URL;
  private favoritesUrl = `${this.baseUrl}/Favorites`;

  constructor(private http: HttpClient) {
    console.log('💖 FavoritesService 初始化');
  }

  /**
   * 獲取收藏列表
   */
  getFavorites(): Promise<Favorite[]> {
    console.log('📋 獲取收藏列表');
    return new Promise((resolve, reject) => {
      this.http.get<ApiResponse<Favorite[]>>(this.favoritesUrl).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            console.log('✅ 收藏列表獲取成功:', response.data);
            resolve(response.data);
          } else {
            console.warn('⚠️ 收藏列表獲取失敗:', response.message);
            resolve([]);
          }
        },
        error: (error) => {
          console.error('❌ 收藏列表獲取錯誤:', error);
          reject(error);
        }
      });
    });
  }

  /**
   * 添加收藏
   */
  addFavorite(favorite: FavoriteRequest): Promise<void> {
    console.log('➕ 添加收藏:', favorite);
    return new Promise((resolve, reject) => {
      this.http.post<ApiResponse<any>>(this.favoritesUrl, favorite).subscribe({
        next: (response) => {
          if (response.success) {
            console.log('✅ 收藏添加成功');
            resolve();
          } else {
            console.warn('⚠️ 收藏添加失敗:', response.message);
            reject(new Error(response.message));
          }
        },
        error: (error) => {
          console.error('❌ 收藏添加錯誤:', error);
          reject(error);
        }
      });
    });
  }

  /**
   * 移除收藏
   */
  removeFavorite(id: number): Promise<void> {
    console.log('➖ 移除收藏:', id);
    return new Promise((resolve, reject) => {
      this.http.delete<ApiResponse<any>>(`${this.favoritesUrl}/${id}`).subscribe({
        next: (response) => {
          if (response.success) {
            console.log('✅ 收藏移除成功');
            resolve();
          } else {
            console.warn('⚠️ 收藏移除失敗:', response.message);
            reject(new Error(response.message));
          }
        },
        error: (error) => {
          console.error('❌ 收藏移除錯誤:', error);
          reject(error);
        }
      });
    });
  }

  /**
   * 清空所有收藏
   */
  clearAllFavorites(): Promise<void> {
    console.log('🗑️ 清空所有收藏');
    return new Promise((resolve, reject) => {
      this.http.delete<ApiResponse<any>>(`${this.favoritesUrl}/clear`).subscribe({
        next: (response) => {
          if (response.success) {
            console.log('✅ 收藏清空成功');
            resolve();
          } else {
            console.warn('⚠️ 收藏清空失敗:', response.message);
            reject(new Error(response.message));
          }
        },
        error: (error) => {
          console.error('❌ 收藏清空錯誤:', error);
          reject(error);
        }
      });
    });
  }

  /**
   * 檢查收藏狀態
   */
  checkFavoriteStatus(id: number): Promise<boolean> {
    console.log('🔍 檢查收藏狀態:', id);
    return new Promise((resolve, reject) => {
      this.http.get<ApiResponse<{ isFavorited: boolean }>>(`${this.favoritesUrl}/status/${id}`).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            console.log('✅ 收藏狀態檢查成功:', response.data.isFavorited);
            resolve(response.data.isFavorited);
          } else {
            console.warn('⚠️ 收藏狀態檢查失敗:', response.message);
            resolve(false);
          }
        },
        error: (error) => {
          console.error('❌ 收藏狀態檢查錯誤:', error);
          reject(error);
        }
      });
    });
  }
} 