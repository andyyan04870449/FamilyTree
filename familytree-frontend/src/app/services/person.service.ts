import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BaseApiService, ApiResponse, PagedApiResponse, ApiCallOptions } from './base-api.service';
import { ToastService } from './toast.service';
import { ProjectService } from './project.service';

export interface Person {
  id: number;
  name: string;
  gender: string;
  birthday: string;
  nationality: string;
  mobile: string;
  phone?: string;
  idNumber?: string;
  passportNumber?: string;
  profileData?: string;
  familyRelationships?: string;
  friends?: string;
  createdAt: string;
  updatedAt: string;
}



@Injectable({
  providedIn: 'root'
})
export class PersonService extends BaseApiService {
  protected readonly apiUrl = this.apiConfig.person.base();

  constructor(
    protected override http: HttpClient,
    protected override toastService: ToastService,
    private projectService: ProjectService
  ) {
    super(http, toastService);
  }

  /**
   * 獲取當前專案 ID 並創建 HTTP 參數
   */
  private getProjectParams(): HttpParams {
    const currentProject = this.projectService.getCurrentProject();
    let params = new HttpParams();
    
    if (currentProject) {
      params = params.set('project_id', currentProject.id);
    } else {
      throw new Error('請先選擇專案');
    }
    
    return params;
  }

  /**
   * 獲取當前專案 ID 並創建查詢參數物件
   */
  private getProjectParamsObject(): { [key: string]: any } {
    const currentProject = this.projectService.getCurrentProject();
    
    if (currentProject) {
      return { project_id: currentProject.id };
    } else {
      throw new Error('請先選擇專案');
    }
  }

  getPersons(): Observable<Person[]> {
    const projectParams = this.getProjectParamsObject();
    return this.get<ApiResponse<Person[]>>('', projectParams, {
      errorMessage: '取得人員列表失敗'
    }).pipe(
      map(response => {
        // 適配新的後端回應格式
        if (response.data && Array.isArray(response.data)) {
          return response.data;
        }
        // 兼容舊格式
        if ((response as any).personDataList) {
          return (response as any).personDataList;
        }
        return [];
      })
    );
  }

  getPerson(id: number): Observable<Person> {
    const projectParams = this.getProjectParamsObject();
    return this.get<ApiResponse<Person>>(id.toString(), projectParams, {
      errorMessage: '取得人員資料失敗'
    }).pipe(
      map(response => {
        // 適配新的後端回應格式
        if (response.data) {
          return response.data;
        }
        // 兼容舊格式
        if ((response as any).personData) {
          return (response as any).personData;
        }
        // 如果response本身就是Person對象，直接返回
        if (response && typeof response === 'object' && 'id' in response) {
          return response as unknown as Person;
        }
        throw new Error('無效的回應格式');
      })
    );
  }

  createPerson(person: Omit<Person, 'id' | 'createdAt' | 'updatedAt'>): Observable<Person> {
    const projectParams = this.getProjectParamsObject();
    const body = { ...person, ...projectParams };
    return this.post<ApiResponse<Person>>('', body, {
      successMessage: '人員資料建立成功',
      errorMessage: '建立人員資料失敗'
    }).pipe(
      map(response => response.data!)
    );
  }

  updatePerson(id: number, person: Partial<Person>): Observable<Person> {
    const projectParams = this.getProjectParamsObject();
    const body = { ...person, ...projectParams };
    return this.put<ApiResponse<Person>>(id.toString(), body, {
      successMessage: '人員資料更新成功',
      errorMessage: '更新人員資料失敗'
    }).pipe(
      map(response => response.data!)
    );
  }

  deletePerson(id: number): Observable<void> {
    const projectParams = this.getProjectParamsObject();
    // 將 project_id 加入查詢參數
    return this.delete<ApiResponse<void>>(id.toString(), {
      successMessage: '人員資料刪除成功',
      errorMessage: '刪除人員資料失敗'
    }).pipe(
      map(() => void 0)
    );
  }

  searchPersons(name?: string, birthDate?: string): Observable<Person[]> {
    const projectParams = this.getProjectParamsObject();
    const searchParams = {
      ...projectParams,
      ...(name && { name }),
      ...(birthDate && { birthDate })
    };
    
    return this.get<ApiResponse<Person[]>>('search', searchParams, {
      errorMessage: '搜尋人員失敗'
    }).pipe(
      map(response => response.data || [])
    );
  }
} 