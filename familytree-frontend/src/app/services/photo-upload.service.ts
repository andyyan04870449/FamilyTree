// 照片上傳服務：處理圖片和壓縮檔案的上傳功能
// 主要功能：支援 JPG、PNG、ZIP、7Z 格式，提供上傳進度追蹤

import { Injectable } from '@angular/core';
import { HttpClient, HttpRequest, HttpEvent, HttpEventType, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, catchError, of } from 'rxjs';
import { AppConstants } from '../constants/app.constants';
import { ProjectService } from './project.service';

// 照片上傳回應介面
export interface PhotoUploadResponse {
  success: boolean;
  message?: string;
  data?: {
    uploadedFiles?: Array<{
      originalFileName: string;
      savedFileName: string;
      fileSize: number;
      uploadTime: string;
    }>;
    failedFiles?: string[];
    totalUploaded: number;
    totalFailed: number;
  };
}

// 上傳進度介面
export interface PhotoUploadProgress {
  progress: number;
  status: 'uploading' | 'completed' | 'error';
}

// 照片檔案資訊介面
export interface PhotoFileInfo {
  id: number;
  originalFileName: string;
  savedFileName: string;
  fileSize: number;
  uploadTime: string;
  projectId: string;
}

@Injectable({
  providedIn: 'root'
})
export class PhotoUploadService {
  private readonly apiUrl = `${AppConstants.API_BASE_URL}/PhotoUpload`;
  
  // 照片列表狀態管理
  private photosSubject = new BehaviorSubject<PhotoFileInfo[]>([]);
  public photos$ = this.photosSubject.asObservable();
  
  // 支援的圖片格式
  private readonly supportedImageTypes = ['image/jpeg', 'image/png'];
  private readonly supportedArchiveTypes = ['application/zip', 'application/x-7z-compressed'];
  private readonly supportedExtensions = ['.jpg', '.jpeg', '.png', '.zip', '.7z'];

  constructor(
    private http: HttpClient,
    private projectService: ProjectService
  ) {}

  /**
   * 獲取照片列表
   */
  getPhotoList(): Observable<any> {
    const currentProject = this.projectService.getCurrentProject();
    if (!currentProject) {
      throw new Error('請先選擇專案');
    }

    const params = new HttpParams().set('project_id', currentProject.id);
    return this.http.get<any>(`${this.apiUrl}/list`, { params }).pipe(
      map(response => {
        if (response.success) {
          const photos = response.data || [];
          this.photosSubject.next(photos);
        }
        return response;
      })
    );
  }

  /**
   * 刷新照片列表
   */
  refreshPhotoList(): void {
    const currentProject = this.projectService.getCurrentProject();
    if (currentProject) {
      console.log('🔄 [PhotoUpload] 刷新照片列表:', currentProject.id);
      this.getPhotoList().subscribe({
        next: () => console.log('✅ [PhotoUpload] 照片列表刷新完成'),
        error: (error) => console.error('❌ [PhotoUpload] 照片列表刷新失敗:', error)
      });
    } else {
      console.warn('⚠️ [PhotoUpload] 沒有當前專案，無法刷新照片列表');
    }
  }

  /**
   * 上傳照片檔案（支援進度追蹤）
   */
  uploadPhotoWithProgress(file: File, projectId?: string): Observable<PhotoUploadProgress | PhotoUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    if (projectId) {
      formData.append('project_id', projectId);
    }

    const req = new HttpRequest('POST', `${this.apiUrl}/upload`, formData, {
      reportProgress: true
    });

    return this.http.request<PhotoUploadResponse>(req).pipe(
      map((event: HttpEvent<PhotoUploadResponse>) => {
        console.log('📡 [PhotoUpload] HTTP事件:', {
          type: event.type,
          typeName: this.getEventTypeName(event.type),
          event: event
        });

        switch (event.type) {
          case HttpEventType.UploadProgress:
            const progress = event.total ? Math.round(100 * event.loaded / event.total) : 0;
            console.log('📊 [PhotoUpload] 上傳進度:', progress + '%', {
              loaded: event.loaded,
              total: event.total
            });
            return { progress, status: 'uploading' as const };
            
          case HttpEventType.Response:
            console.log('📥 [PhotoUpload] 收到Response事件:', {
              status: event.status,
              body: event.body,
              success: event.body?.success,
              fullBody: JSON.stringify(event.body, null, 2)
            });
            
            if (event.body?.success) {
              console.log('✅ [PhotoUpload] 上傳成功，準備返回完成狀態');
              console.log('📊 [PhotoUpload] 回應資料詳情:', event.body.data);
              
              // 後端回應格式：{success: true, data: {uploadedFiles, failedFiles}, message}
              // 返回完成狀態，不包含progress屬性以避免混淆
              const responseData = event.body.data as any || {};
              const result: PhotoUploadResponse = { 
                success: true,
                message: event.body.message,
                data: {
                  uploadedFiles: responseData.uploadedFiles,
                  failedFiles: responseData.failedFiles,
                  totalUploaded: responseData.totalUploaded || (responseData.uploadedFiles?.length || 0),
                  totalFailed: responseData.totalFailed || (responseData.failedFiles?.length || 0)
                }
              };
              
              console.log('🔄 [PhotoUpload] 準備返回完成結果:', result);
              return result;
            } else {
              console.error('❌ [PhotoUpload] 上傳失敗');
              return {
                success: false,
                message: event.body?.message || '上傳失敗',
                data: {
                  uploadedFiles: [],
                  failedFiles: [],
                  totalUploaded: 0,
                  totalFailed: 1
                }
              } as PhotoUploadResponse;
            }
            
          case HttpEventType.Sent:
            console.log('📤 [PhotoUpload] 請求已發送');
            return { progress: 0, status: 'uploading' as const };
            
          case HttpEventType.ResponseHeader:
            console.log('📋 [PhotoUpload] 收到回應標頭:', {
              status: event.status,
              headers: event.headers
            });
            return { progress: 0, status: 'uploading' as const };
            
          default:
            console.log('📋 [PhotoUpload] 其他HTTP事件:', this.getEventTypeName(event.type));
            return { progress: 0, status: 'uploading' as const };
        }
      }),
      catchError(error => {
        console.error('❌ [PhotoUpload] 上傳過程發生錯誤:', error);
        return of({
          success: false,
          message: error.message || '上傳過程發生錯誤',
          data: {
            uploadedFiles: [],
            failedFiles: [],
            totalUploaded: 0,
            totalFailed: 1
          }
        } as PhotoUploadResponse);
      })
    );
  }



  /**
   * 刪除照片
   */
  deletePhoto(photoId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${photoId}`).pipe(
      map(response => {
        // 無論後端回應如何，都嘗試從本地列表中移除
        const currentPhotos = this.photosSubject.value;
        const updatedPhotos = currentPhotos.filter(photo => photo.id !== photoId);
        this.photosSubject.next(updatedPhotos);
        
        console.log('🗑️ [PhotoUpload] 照片刪除後更新本地列表:', updatedPhotos.length);
        return response;
      })
    );
  }



  /**
   * 根據照片編號獲取照片檔案 URL
   */
  getPhotoFileUrlByIndex(photoIndex: string, projectId?: string): string {
    const currentProject = projectId || this.projectService.getCurrentProject()?.id;
    if (!currentProject) {
      throw new Error('請先選擇專案');
    }
    
    // 將照片編號轉換為6位數字格式 (不加副檔名，讓後端處理)
    const formattedIndex = photoIndex.padStart(6, '0');
    
    // 使用 photo-by-index 端點，更適合根據索引查找
    return `${this.apiUrl}/photo-by-index/${formattedIndex}?project_id=${currentProject}`;
  }

  /**
   * 檢查照片是否存在
   */
  checkPhotoExists(photoIndex: string, projectId?: string): Observable<boolean> {
    // 簡化邏輯：直接返回true，讓瀏覽器的img標籤處理錯誤
    // 如果照片不存在，img的onerror事件會被觸發
    return of(true);
  }

  /**
   * 檢查檔案類型是否支援
   */
  isValidPhotoType(file: File): boolean {
    const extension = this.getFileExtension(file.name);
    const mimeType = file.type;
    
    // 檢查副檔名
    const extensionValid = this.supportedExtensions.includes(extension);
    
    if (!extensionValid) {
      return false;
    }
    
    // 對於拖曳檔案，MIME 類型可能不正確，主要依靠副檔名判斷
    if (!mimeType || mimeType === 'application/octet-stream' || mimeType === '') {
      return true; // 副檔名已經驗證通過
    }
    
    // 檢查 MIME 類型（對壓縮檔較寬鬆）
    const mimeTypeValid = this.supportedImageTypes.includes(mimeType) || 
                         this.supportedArchiveTypes.includes(mimeType) ||
                         mimeType === 'application/x-zip-compressed' ||
                         mimeType === 'application/zip' ||
                         mimeType === 'application/x-7z-compressed';

    return extensionValid && (mimeTypeValid || this.isArchiveFile(extension));
  }

  /**
   * 檢查是否為壓縮檔案
   */
  isArchiveFile(fileName: string): boolean {
    const extension = this.getFileExtension(fileName);
    return ['.zip', '.7z'].includes(extension);
  }

  /**
   * 檢查是否為圖片檔案
   */
  isImageFile(fileName: string): boolean {
    const extension = this.getFileExtension(fileName);
    return ['.jpg', '.jpeg', '.png'].includes(extension);
  }

  /**
   * 取得檔案副檔名
   */
  private getFileExtension(fileName: string): string {
    const lastDotIndex = fileName.lastIndexOf('.');
    if (lastDotIndex === -1) return '';
    return fileName.substring(lastDotIndex).toLowerCase();
  }

  /**
   * 格式化檔案大小
   */
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  /**
   * 服務健康檢查
   */
  healthCheck(): Observable<any> {
    return this.http.get(`${this.apiUrl}/health`);
  }

  /**
   * 取得HTTP事件類型名稱（用於調試）
   */
  private getEventTypeName(eventType: number): string {
    switch (eventType) {
      case HttpEventType.Sent: return 'Sent';
      case HttpEventType.UploadProgress: return 'UploadProgress';
      case HttpEventType.ResponseHeader: return 'ResponseHeader';
      case HttpEventType.DownloadProgress: return 'DownloadProgress';
      case HttpEventType.Response: return 'Response';
      case HttpEventType.User: return 'User';
      default: return `Unknown(${eventType})`;
    }
  }

  /**
   * 獲取當前照片列表
   */
  getCurrentPhotos(): PhotoFileInfo[] {
    return this.photosSubject.value;
  }
} 