// 檔案上傳服務 - 處理檔案上傳 API 呼叫和檔案操作
import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AppConstants } from '../constants/app.constants';
import { ProjectService } from './project.service';

export interface FileModel {
  fileId: string;
  userId: string;
  filename: string;
  originalFilename: string;
  filePath: string;
  fileSize: number;
  md5Hash: string;
  fileType?: string;
  mimeType?: string;
  associatedRecordId?: string;
  associatedRecordType?: string;
  uploadStatus: string;
  isProcessed: boolean;
  processedAt?: string;
  uploadedAt: string;
  createdAt: string;
  updatedAt: string;
  relatedPersonsCount: number;
}

export interface FileUploadResponse {
  success: boolean;
  message: string;
  data?: FileModel;
  isDuplicate: boolean;
  filePath?: string;
}

export interface FileListResponse {
  success: boolean;
  message: string;
  files: FileModel[];
  totalCount: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface UploadProgress {
  progress: number;
  loaded: number;
  total: number;
}

export interface DeleteImpactResponse {
  success: boolean;
  message: string;
  personCount: number;
  personNames: string[];
  fileName: string;
  hasMorePersons: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class FileUploadService {
  private apiUrl = `${AppConstants.API_BASE_URL}/File`;
  private filesSubject = new BehaviorSubject<FileModel[]>([]);
  public files$ = this.filesSubject.asObservable();

  constructor(
    private http: HttpClient,
    private projectService: ProjectService
  ) {}

  /**
   * 獲取當前專案 ID 並創建 HTTP 參數
   */
  private getProjectParams(): HttpParams {
    const currentProject = this.projectService.getCurrentProject();
    let params = new HttpParams();
    
    if (currentProject) {
      params = params.set('project_id', currentProject.id);
      console.log('🎯 [FileUploadService] 添加專案 ID 到請求:', currentProject.id);
    } else {
      console.warn('⚠️ [FileUploadService] 沒有當前專案，返回空參數');
      // 不再拋出錯誤，返回空參數讓調用者處理
    }
    
    return params;
  }

  // 上傳檔案
  uploadFile(file: File): Observable<FileUploadResponse> {
    const currentProject = this.projectService.getCurrentProject();
    if (!currentProject) {
      throw new Error('請先選擇專案');
    }

    const formData = new FormData();
    formData.append('file', file);
    formData.append('associatedRecordId', currentProject.id);
    formData.append('associatedRecordType', 'project');

    return this.http.post<FileUploadResponse>(`${this.apiUrl}/upload`, formData);
  }

  // 上傳檔案並監控進度
  uploadFileWithProgress(file: File): Observable<UploadProgress | FileUploadResponse> {
    const currentProject = this.projectService.getCurrentProject();
    if (!currentProject) {
      throw new Error('請先選擇專案');
    }

    const formData = new FormData();
    formData.append('file', file);
    formData.append('associatedRecordId', currentProject.id);
    formData.append('associatedRecordType', 'project');

    return this.http.post(`${this.apiUrl}/upload`, formData, {
      reportProgress: true,
      observe: 'events'
    }).pipe(
      map((event: HttpEvent<any>) => {
        switch (event.type) {
          case HttpEventType.UploadProgress:
            const progress = event.total ? Math.round(100 * event.loaded / event.total) : 0;
            return {
              progress,
              loaded: event.loaded,
              total: event.total || 0
            } as UploadProgress;
          case HttpEventType.Response:
            return event.body as FileUploadResponse;
          default:
            return { progress: 0, loaded: 0, total: 0 } as UploadProgress;
        }
      })
    );
  }

  // 取得檔案列表
  getFileList(): Observable<any> {
    const currentProject = this.projectService.getCurrentProject();
    
    if (!currentProject) {
      console.warn('⚠️ [FileUploadService] 沒有當前專案，清空檔案列表');
      // 沒有專案時，清空檔案列表並返回空結果
      this.filesSubject.next([]);
      return new Observable(observer => {
        observer.next({ success: true, data: [], message: '請先選擇專案' });
        observer.complete();
      });
    }

    let params = new HttpParams();
    params = params.set('AssociatedRecordId', currentProject.id);
    params = params.set('AssociatedRecordType', 'project');
    console.log('📡 [FileUploadService] 發送檔案列表請求，專案:', currentProject.projectName, 'ID:', currentProject.id);
    
    return this.http.get<any>(`${this.apiUrl}/list`, { params }).pipe(
      map(response => {
        console.log('📋 [FileUploadService] 收到檔案列表回應:', response);
        if (response.success) {
          // 處理新的回應格式：後端現在回應 { success: true, data: [...], message: "..." }
          const files = response.data || response.files || [];
          console.log('📁 [FileUploadService] 解析到檔案數量:', files.length);
          this.filesSubject.next(files);
        } else {
          console.error('❌ [FileUploadService] API 回應失敗:', response.message);
          this.filesSubject.next([]);
        }
        return response;
      }),
      catchError(error => {
        console.error('❌ [FileUploadService] 獲取檔案列表失敗:', error);
        this.filesSubject.next([]);
        // 返回一個表示錯誤的響應而不是拋出錯誤
        return new Observable(observer => {
          observer.next({ success: false, data: [], message: '獲取檔案列表失敗' });
          observer.complete();
        });
      })
    );
  }

  // 取得刪除檔案的影響資訊
  getDeleteImpact(fileId: string): Observable<DeleteImpactResponse> {
    return this.http.get<DeleteImpactResponse>(`${this.apiUrl}/${fileId}/impact`);
  }

  // 刪除檔案
  deleteFile(fileId: string): Observable<FileUploadResponse> {
    return this.http.delete<FileUploadResponse>(`${this.apiUrl}/${fileId}`).pipe(
      map(response => {
        if (response.success) {
          // 從本地列表中移除已刪除的檔案
          const currentFiles = this.filesSubject.value;
          const updatedFiles = currentFiles.filter(file => file.fileId !== fileId);
          this.filesSubject.next(updatedFiles);
        }
        return response;
      })
    );
  }

  // 檢查檔案類型是否有效
  isValidFileType(file: File): boolean {
    const allowedTypes = [
      'application/vnd.ms-excel', // .xls
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' // .xlsx
    ];
    const allowedExtensions = ['.xls', '.xlsx'];
    
    // 檢查 MIME 類型
    if (allowedTypes.includes(file.type)) {
      return true;
    }
    
    // 檢查檔案副檔名
    const fileName = file.name.toLowerCase();
    return allowedExtensions.some(ext => fileName.endsWith(ext));
  }

  // 格式化檔案大小
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  // 格式化日期
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  // 取得檔案狀態顯示文字
  getStatusText(status: string): string {
    const statusMap: { [key: string]: string } = {
      'uploaded': '已上傳',
      'processing': '處理中',
      'processed': '已處理',
      'failed': '失敗',
      'deleted': '已刪除'
    };
    return statusMap[status] || status;
  }

  // 取得檔案狀態顏色
  getStatusColor(status: string): string {
    const colorMap: { [key: string]: string } = {
      'uploaded': 'success',
      'processing': 'warning',
      'processed': 'info',
      'failed': 'danger',
      'deleted': 'secondary'
    };
    return colorMap[status] || 'secondary';
  }

  // 重新整理檔案列表
  refreshFileList(): void {
    this.getFileList().subscribe();
  }

  // 獲取當前檔案列表
  getCurrentFiles(): FileModel[] {
    return this.filesSubject.value;
  }
} 