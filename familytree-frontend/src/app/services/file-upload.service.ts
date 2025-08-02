// 檔案上傳服務 - 處理檔案上傳 API 呼叫和檔案操作
import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AppConstants } from '../constants/app.constants';
import { ProjectService } from './project.service';
import { BaseHttpService } from './base-http.service';
import { CommonUtils } from '../utils/common.utils';

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
export class FileUploadService extends BaseHttpService {
  private filesSubject = new BehaviorSubject<FileModel[]>([]);
  public files$ = this.filesSubject.asObservable();

  constructor(
    http: HttpClient,
    private projectService: ProjectService
  ) {
    super(http);
  }

  protected getBaseUrl(): string {
    return `${AppConstants.API_BASE_URL}/File`;
  }

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
  uploadSingleFile(file: File): Observable<FileUploadResponse> {
    const currentProject = this.projectService.getCurrentProject();
    if (!currentProject) {
      throw new Error(AppConstants.MESSAGES.PROJECT_NOT_SELECTED);
    }

    const formData = new FormData();
    formData.append('file', file);
    formData.append('associatedRecordId', currentProject.id);
    formData.append('associatedRecordType', 'project');

    return this.post<FileUploadResponse>('upload', formData, {
      headers: {} // 讓瀏覽器自動設置 multipart/form-data
    });
  }

  // 上傳檔案並監控進度
  uploadFileWithProgress(file: File): Observable<UploadProgress | FileUploadResponse> {
    const currentProject = this.projectService.getCurrentProject();
    if (!currentProject) {
      throw new Error(AppConstants.MESSAGES.PROJECT_NOT_SELECTED);
    }

    const additionalData = {
      associatedRecordId: currentProject.id,
      associatedRecordType: 'project'
    };

    return (super.uploadFile('upload', file, additionalData) as Observable<HttpEvent<any>>).pipe(
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
      return of({ success: true, data: [], message: AppConstants.MESSAGES.PROJECT_NOT_SELECTED });
    }

    const params = this.buildParams({
      AssociatedRecordId: currentProject.id,
      AssociatedRecordType: 'project'
    });
    
    console.log('📡 [FileUploadService] 發送檔案列表請求，專案:', currentProject.projectName, 'ID:', currentProject.id);
    
    return this.get<any>('list', { params }).pipe(
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
        return of({ success: false, data: [], message: AppConstants.MESSAGES.FILE_LIST_FAILED });
      })
    );
  }

  // 取得刪除檔案的影響資訊
  getDeleteImpact(fileId: string): Observable<DeleteImpactResponse> {
    return this.get<DeleteImpactResponse>(`${fileId}/impact`);
  }

  // 刪除檔案
  deleteFile(fileId: string): Observable<FileUploadResponse> {
    return this.delete<FileUploadResponse>(fileId).pipe(
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
    return AppConstants.isValidFileType(file.name);
  }

  // 格式化檔案大小
  formatFileSize(bytes: number): string {
    return CommonUtils.formatFileSize(bytes);
  }

  // 格式化日期
  formatDate(dateString: string): string {
    return CommonUtils.formatDateTime(dateString);
  }

  // 取得檔案狀態顯示文字
  getStatusText(status: string): string {
    const statusMap: { [key: string]: string } = {
      [AppConstants.FILE_SYSTEM.STATUS.UPLOADED]: '已上傳',
      [AppConstants.FILE_SYSTEM.STATUS.PROCESSING]: '處理中',
      [AppConstants.FILE_SYSTEM.STATUS.PROCESSED]: '已處理',
      [AppConstants.FILE_SYSTEM.STATUS.FAILED]: '失敗',
      [AppConstants.FILE_SYSTEM.STATUS.DELETED]: '已刪除'
    };
    return statusMap[status] || status;
  }

  // 取得檔案狀態顏色
  getStatusColor(status: string): string {
    const colorMap: { [key: string]: string } = {
      [AppConstants.FILE_SYSTEM.STATUS.UPLOADED]: 'success',
      [AppConstants.FILE_SYSTEM.STATUS.PROCESSING]: 'warning',
      [AppConstants.FILE_SYSTEM.STATUS.PROCESSED]: 'info',
      [AppConstants.FILE_SYSTEM.STATUS.FAILED]: 'danger',
      [AppConstants.FILE_SYSTEM.STATUS.DELETED]: 'secondary'
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