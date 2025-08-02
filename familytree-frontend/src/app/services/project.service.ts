// 專案管理服務：處理專案 CRUD 操作的 HTTP 通信
// 主要功能：專案列表、新增、編輯、刪除、搜尋、統計

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { map, catchError, tap, retry, delay } from 'rxjs/operators';
import { AppConstants } from '../constants/app.constants';

// 專案相關介面
export interface Project {
  id: string;
  userId: string;
  projectName: string;
  projectDescription?: string;
  status: 'active' | 'completed' | 'archived' | 'draft';
  createdAt: string;
  completedAt?: string;
  updatedAt: string;
  memberCount: number;
  relationshipCount: number;
  progress: number;
  tags: string[];
}

export interface CreateProjectRequest {
  projectName: string;
  projectDescription?: string;
  userId: string;
  status?: string;
}

export interface UpdateProjectRequest {
  projectName: string;
  projectDescription?: string;
  status?: string;
}

export interface ProjectListResponse {
  success: boolean;
  message: string;
  projects: Project[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ProjectStatistics {
  totalProjects: number;
  activeProjects: number;
  completedProjects: number;
  archivedProjects: number;
  totalMembers: number;
  totalRelationships: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  error?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private readonly baseUrl = `${AppConstants.API_BASE_URL}/project`;
  
  // 當前專案狀態管理
  private currentProjectSubject = new BehaviorSubject<Project | null>(null);
  public currentProject$ = this.currentProjectSubject.asObservable();
  
  // 專案列表狀態管理
  private projectsSubject = new BehaviorSubject<Project[]>([]);
  public projects$ = this.projectsSubject.asObservable();
  
  // 載入狀態管理
  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();

  constructor(private http: HttpClient) {
    // ProjectService 初始化
    this.loadStoredCurrentProject();
  }

  /**
   * 獲取所有專案列表
   */
  getProjects(status?: string, search?: string): Observable<ProjectListResponse> {
    // 獲取專案列表
    this.setLoading(true);
    
    let params = new HttpParams();
    if (status && status !== 'all') {
      params = params.set('status', status);
    }
    if (search && search.trim()) {
      params = params.set('search', search.trim());
    }

    const fullUrl = `${this.baseUrl}`;
    // 完整請求URL

    return this.http.get<ProjectListResponse>(fullUrl, { params }).pipe(
      retry(2),
      tap(response => {
        console.log('📡 API 回應:', response);
        if (response && response.success) {
          const projects = response.projects || [];
          console.log('✅ 載入專案列表:', projects.length, '個專案');
          this.projectsSubject.next(projects);
        } else {
          console.error('❌ API 回應表示失敗:', response);
          this.projectsSubject.next([]);
        }
      }),
      catchError(error => {
        // 獲取專案列表失敗
        
        // 設定載入狀態為 false
        this.setLoading(false);
        
        // 如果是網路連接問題，建議使用直接連接
        if (error.status === 0 || error.status === 504) {
          // 網路連接問題，建議啟用直接 API 連接
          // 在瀏覽器控制台執行: localStorage.setItem("use-direct-api", "true") 然後重新整理頁面
        }
        
        // 回傳更詳細的錯誤信息
        const detailedError = {
          originalError: error,
          url: fullUrl,
          timestamp: new Date().toISOString(),
          userAgent: navigator.userAgent,
          suggestion: error.status === 0 ? '網路連接問題，請檢查後端服務是否正在運行' : '請檢查 API 服務狀態'
        };
        
        return throwError(() => detailedError);
      }),
      tap(() => this.setLoading(false))
    );
  }

  /**
   * 根據ID獲取單一專案
   */
  getProject(id: string): Observable<Project> {
    // 獲取專案詳情
    
    return this.http.get<{ success: boolean; project: Project }>(`${this.baseUrl}/${id}`).pipe(
      map(response => {
        if (response.success) {
          // 專案詳情載入成功
          return response.project;
        }
        throw new Error('專案不存在');
      }),
      catchError(error => {
        // 獲取專案詳情失敗
        return throwError(() => error);
      })
    );
  }

  /**
   * 創建新專案
   */
  createProject(request: CreateProjectRequest): Observable<{ projectId: string }> {
    // 創建新專案
    console.log('📮 發送創建專案請求:', request);
    console.log('📮 請求 URL:', this.baseUrl);
    this.setLoading(true);

    return this.http.post<{ success: boolean; projectId?: string; project?: any; message: string }>(`${this.baseUrl}`, request).pipe(
      map(response => {
        if (response.success) {
          // 處理兩種可能的回應格式
          const projectId = response.projectId || (response.project && response.project.id);
          console.log('✅ 專案創建成功:', projectId);
          // 重新載入專案列表
          this.refreshProjects();
          return { projectId: projectId || '' };
        }
        throw new Error(response.message || '創建專案失敗');
      }),
      catchError(error => {
        // 創建專案失敗
        console.error('❌ 創建專案失敗 - 詳細錯誤:', error);
        if (error.error) {
          console.error('錯誤內容:', error.error);
        }
        return throwError(() => error);
      }),
      tap(() => this.setLoading(false))
    );
  }

  /**
   * 更新專案
   */
  updateProject(id: string, request: UpdateProjectRequest): Observable<void> {
    console.log('🔄 更新專案:', id, request);
    this.setLoading(true);

    return this.http.put<{ success: boolean; message: string }>(`${this.baseUrl}/${id}`, request).pipe(
      map(response => {
        if (response.success) {
          console.log('✅ 專案更新成功，準備重新載入專案列表');
          // 重新載入專案列表以獲取完整資料
          this.refreshProjects();
          // 如果是當前專案，重新載入詳情
          if (this.currentProjectSubject.value?.id === id) {
            this.getProject(id).subscribe({
              next: (project) => {
                this.setCurrentProject(project);
              },
              error: (error) => {
                console.error('❌ 無法重新載入專案詳情:', error);
                // 如果無法載入詳情，清除當前專案
                this.setCurrentProject(null);
              }
            });
          }
          return;
        }
        throw new Error(response.message || '更新專案失敗');
      }),
      catchError(error => {
        console.error('❌ 更新專案失敗:', error);
        this.setLoading(false);
        return throwError(() => error);
      }),
      tap(() => {
        // 延遲一點設置 loading 為 false，確保資料重新載入完成
        setTimeout(() => this.setLoading(false), 500);
      })
    );
  }

  /**
   * 軟刪除專案
   */
  deleteProject(id: string): Observable<void> {
    // 刪除專案
    this.setLoading(true);

    return this.http.delete<{ success: boolean; message: string }>(`${this.baseUrl}/${id}`).pipe(
      map(response => {
        if (response.success) {
          // 專案刪除成功
          // 重新載入專案列表
          this.refreshProjects();
          // 如果刪除的是當前專案，清除當前專案
          if (this.currentProjectSubject.value?.id === id) {
            this.setCurrentProject(null);
          }
          return;
        }
        throw new Error(response.message || '刪除專案失敗');
      }),
      catchError(error => {
        // 刪除專案失敗
        return throwError(() => error);
      }),
      tap(() => this.setLoading(false))
    );
  }

  /**
   * 獲取專案統計資訊
   */
  getProjectStatistics(): Observable<ProjectStatistics> {
    // 獲取專案統計
    const fullUrl = `${this.baseUrl}/statistics`;
    // 統計 API URL
    
    return this.http.get<{ success: boolean; statistics: ProjectStatistics }>(fullUrl).pipe(
      map(response => {
        // 統計 API 回應
        if (response && response.success) {
          // 統計資訊載入成功
          return response.statistics;
        }
        throw new Error('統計 API 回應表示失敗');
      }),
      catchError(error => {
        // 獲取統計資訊失敗
        return throwError(() => error);
      })
    );
  }

  /**
   * 設定當前專案
   */
  setCurrentProject(project: Project | null): void {
    console.log('🎯 設定當前專案:', project?.projectName || 'null');
    this.currentProjectSubject.next(project);
    
    // 儲存到本地儲存
    if (project) {
      localStorage.setItem('currentProject', JSON.stringify(project));
    } else {
      localStorage.removeItem('currentProject');
    }
  }

  /**
   * 獲取當前專案
   */
  getCurrentProject(): Project | null {
    return this.currentProjectSubject.value;
  }

  /**
   * 刷新專案列表
   */
  refreshProjects(): void {
    console.log('🔄 刷新專案列表');
    this.getProjects().subscribe({
      next: () => console.log('✅ 專案列表刷新完成'),
      error: (error) => console.error('❌ 專案列表刷新失敗:', error)
    });
  }

  /**
   * 搜尋專案
   */
  searchProjects(searchTerm: string, status?: string): Observable<Project[]> {
    console.log('🔍 搜尋專案:', searchTerm);
    
    return this.getProjects(status, searchTerm).pipe(
      map(response => response.projects)
    );
  }

  // 已移除 generateDefaultUserId() 方法，現在使用真實的用戶 ID

  /**
   * 設定載入狀態
   */
  private setLoading(loading: boolean): void {
    this.loadingSubject.next(loading);
  }

  /**
   * 從本地儲存載入當前專案
   */
  private loadStoredCurrentProject(): void {
    try {
      const stored = localStorage.getItem('currentProject');
      if (stored) {
        const project = JSON.parse(stored) as Project;
        this.currentProjectSubject.next(project);
        console.log('📂 從本地儲存載入當前專案:', project.projectName);
      }
    } catch (error) {
      console.error('❌ 載入儲存的當前專案失敗:', error);
      localStorage.removeItem('currentProject');
    }
  }

  /**
   * 清除所有快取
   */
  clearCache(): void {
    console.log('🧹 清除專案服務快取');
    this.projectsSubject.next([]);
    this.setCurrentProject(null);
    this.setLoading(false);
  }

  /**
   * 檢查專案名稱是否已存在
   */
  checkProjectNameExists(name: string, excludeId?: string): Observable<boolean> {
    return this.projects$.pipe(
      map(projects => {
        const exists = projects.some(p => 
          p.projectName.toLowerCase() === name.toLowerCase() && 
          p.id !== excludeId
        );
        return exists;
      })
    );
  }

  /**
   * 格式化專案狀態為中文
   */
  formatProjectStatus(status: string): string {
    const statusMap: { [key: string]: string } = {
      'active': '進行中',
      'completed': '已完成',
      'archived': '已封存',
      'draft': '草稿'
    };
    return statusMap[status] || status;
  }

  /**
   * 計算專案進度文字描述
   */
  getProgressDescription(progress: number): string {
    if (progress === 0) return '尚未開始';
    if (progress < 25) return '剛開始';
    if (progress < 50) return '進行中';
    if (progress < 75) return '大部分完成';
    if (progress < 100) return '接近完成';
    return '已完成';
  }
} 