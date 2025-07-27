// 照片工具服務 - 提供全專案通用的照片處理功能
// 主要功能：照片URL生成、錯誤處理、預設頭像、照片驗證

import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { PhotoUploadService } from './photo-upload.service';
import { ProjectService } from './project.service';
import { LogService } from './log.service';

export interface PersonPhotoConfig {
  photoIndex?: string | null | undefined;
  personName?: string;
  projectId?: string;
  size?: 'small' | 'medium' | 'large';
  fallbackToInitial?: boolean;
}

export interface PhotoUrlResult {
  url: string | null;
  isValid: boolean;
  fallbackText: string | null;
  errorMessage?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PhotoUtilsService {

  constructor(
    private photoUploadService: PhotoUploadService,
    private projectService: ProjectService,
    private logService: LogService
  ) {}

  /**
   * 取得人員照片URL結果
   */
    getPersonPhotoResult(config: PersonPhotoConfig): PhotoUrlResult {
    this.logService.debug('PhotoUtils', '開始處理照片請求', config);
    
    try {
      // 檢查照片索引是否存在
      if (!config.photoIndex || config.photoIndex.trim() === '') {
        this.logService.info('PhotoUtils', '無照片索引，使用預設頭像', config);
        return {
          url: null,
          isValid: false,
          fallbackText: this.generateFallbackText(config.personName),
          errorMessage: '無照片索引'
        };
      }

      // 驗證照片索引格式
      if (!this.isValidPhotoIndex(config.photoIndex)) {
        this.logService.warn('PhotoUtils', '照片索引格式無效', { 
          photoIndex: config.photoIndex, 
          personName: config.personName 
        });
        return {
          url: null,
          isValid: false,
          fallbackText: this.generateFallbackText(config.personName),
          errorMessage: '照片索引格式無效'
        };
      }

       // 檢查專案是否存在
       const currentProject = this.projectService.getCurrentProject();
       const projectId = config.projectId || currentProject?.id;
       
       this.logService.debug('PhotoUtils', '專案資訊檢查', { 
         configProjectId: config.projectId,
         currentProject: currentProject,
         finalProjectId: projectId
       });
       
       if (!projectId) {
         this.logService.warn('PhotoUtils', '無專案ID，無法生成照片URL', { 
           currentProject, 
           config 
         });
         return {
           url: null,
           isValid: false,
           fallbackText: this.generateFallbackText(config.personName),
           errorMessage: '請先選擇專案'
         };
       }

       // 生成照片URL
       const photoUrl = this.photoUploadService.getPhotoFileUrlByIndex(config.photoIndex, projectId);
       
       this.logService.debug('PhotoUtils', '成功生成照片URL', {
         photoIndex: config.photoIndex,
         projectId,
         url: photoUrl
       });

       // 驗證URL格式
       if (!photoUrl || photoUrl.indexOf('project_id=') === -1) {
         this.logService.warn('PhotoUtils', '生成的照片URL格式異常', { photoUrl, projectId });
         return {
           url: null,
           isValid: false,
           fallbackText: this.generateFallbackText(config.personName),
           errorMessage: '照片URL格式異常'
         };
       }

       return {
         url: photoUrl,
         isValid: true,
         fallbackText: this.generateFallbackText(config.personName),
         errorMessage: undefined
       };

     } catch (error: any) {
       this.logService.error('PhotoUtils', '生成照片URL失敗', {
         error: error.message,
         config
       });

      return {
        url: null,
        isValid: false,
        fallbackText: this.generateFallbackText(config.personName),
        errorMessage: error.message || '照片URL生成失敗'
      };
    }
  }

  /**
   * 僅取得照片URL（向下兼容的簡化方法）
   */
  getPersonPhotoUrl(photoIndex: string | null | undefined, personName?: string, projectId?: string): string | null {
    const result = this.getPersonPhotoResult({
      photoIndex,
      personName,
      projectId
    });
    return result.url;
  }

  /**
   * 生成預設頭像文字
   */
  generateFallbackText(personName?: string): string {
    if (!personName || personName.trim() === '') {
      return '?';
    }
    
    // 取姓名第一個字元作為頭像
    const firstChar = personName.trim().charAt(0);
    return firstChar || '?';
  }

  /**
   * 檢查照片索引格式是否有效
   */
  isValidPhotoIndex(photoIndex: string | null | undefined): boolean {
    if (!photoIndex || photoIndex.trim() === '') {
      return false;
    }

    // 檢查是否為數字格式
    const trimmed = photoIndex.trim();
    const isNumeric = /^\d+$/.test(trimmed);
    
         if (!isNumeric) {
       this.logService.warn('PhotoUtils', '照片索引格式無效', { photoIndex });
       return false;
     }

    return true;
  }

  /**
   * 標準化照片索引格式（補零至6位）
   */
  normalizePhotoIndex(photoIndex: string | null | undefined): string | null {
    if (!this.isValidPhotoIndex(photoIndex)) {
      return null;
    }

    return photoIndex!.trim().padStart(6, '0');
  }

  /**
   * 取得照片檔案名稱
   */
  getPhotoFileName(photoIndex: string): string {
    const normalized = this.normalizePhotoIndex(photoIndex);
    if (!normalized) {
      throw new Error(`無效的照片索引: ${photoIndex}`);
    }
    return `${normalized}.PNG`;
  }

  /**
   * 檢查照片是否存在（簡化版本）
   */
  checkPhotoExists(photoIndex: string, projectId?: string): Observable<boolean> {
    // 簡化邏輯：直接返回true，讓瀏覽器的img標籤處理錯誤
    // 如果照片不存在，img的onerror事件會被觸發
    return of(true);
  }

  /**
   * 取得照片CSS類別
   */
  getPhotoSizeClass(size: 'small' | 'medium' | 'large' = 'medium'): string {
    const sizeClasses = {
      small: 'photo-size-small',
      medium: 'photo-size-medium', 
      large: 'photo-size-large'
    };
    return sizeClasses[size];
  }

  /**
   * 處理照片載入錯誤的統一方法
   */
  handlePhotoError(event: Event, fallbackElement?: HTMLElement): void {
    const imgElement = event.target as HTMLImageElement;
    if (imgElement) {
      // 隱藏失敗的圖片
      imgElement.style.display = 'none';
      
      // 顯示備用元素
      if (fallbackElement) {
        fallbackElement.style.display = 'flex';
      } else {
        // 尋找下一個兄弟元素作為備用顯示
        const nextElement = imgElement.nextElementSibling as HTMLElement;
        if (nextElement) {
          nextElement.style.display = 'flex';
        }
      }
    }
    
         this.logService.debug('PhotoUtils', '照片載入失敗，已切換至預設顯示', {
       src: imgElement?.src
     });
  }
} 