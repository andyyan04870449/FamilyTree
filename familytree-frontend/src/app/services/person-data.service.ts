// 人員資料服務 - 處理人員資料的API呼叫
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConstants } from '../constants/app.constants';
import { ProjectService } from './project.service';

export interface PersonDataModel {
  id: number;
  name: string;
  gender?: string;
  birthday?: string;
  nationality?: string;
  mobile?: string;
  phone?: string;
  idNumber?: string;
  passportNumber?: string;
  familyRelationships?: string;
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
  updatedBy?: string;
  photo?: string;
  discoveryProcess?: string;
  birthplace?: string;
  ethnicity?: string;
  ancestralHome?: string;
  politicalParty?: string;
  email?: string;
  currentWorkplace?: string;
  currentAddress?: string;
  mailingAddress?: string;
  experience?: string;
  education?: string;
  onlineAccounts?: string;
  publications?: string;
  activities?: string;
  frequentPlaces?: string;
  travelRecords?: string;
  notes?: string;
  fileMd5?: string;
  importantFriends?: string;
  projectId?: string;
}

export interface PersonDataResponse {
  success: boolean;
  message: string;
  personData?: PersonDataModel;
}

export interface PersonDataListResponse {
  success: boolean;
  message: string;
  personDataList: PersonDataModel[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PersonDataRequest {
  photo?: string;
  name: string;
  discoveryProcess?: string;
  gender?: string;
  birthday?: string;
  birthplace?: string;
  nationality?: string;
  ethnicity?: string;
  ancestralHome?: string;
  politicalParty?: string;
  idNumber?: string;
  passportNumber?: string;
  phone?: string;
  mobile?: string;
  email?: string;
  currentWorkplace?: string;
  currentAddress?: string;
  mailingAddress?: string;
  familyRelationships?: string;
  experience?: string;
  education?: string;
  onlineAccounts?: string;
  publications?: string;
  activities?: string;
  importantFriends?: string;
  frequentPlaces?: string;
  travelRecords?: string;
  notes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PersonDataService {
  private apiUrl = `${AppConstants.API_BASE_URL}/PersonData`;

  constructor(
    private http: HttpClient,
    private projectService: ProjectService
  ) {
    console.log('[PersonDataService] 初始化，API URL:', this.apiUrl);
  }

  /**
   * 獲取當前專案 ID 並創建 HTTP 參數
   */
  private getProjectParams(): HttpParams {
    const currentProject = this.projectService.getCurrentProject();
    let params = new HttpParams();
    
    if (currentProject) {
      params = params.set('project_id', currentProject.id);
      console.log('🎯 [PersonDataService] 添加專案 ID 到請求:', currentProject.id);
    } else {
      console.warn('⚠️ [PersonDataService] 沒有當前專案，不進行 API 請求');
      // 拋出錯誤或返回預設值
      throw new Error('請先選擇專案');
    }
    
    return params;
  }

  // 取得人員資料列表
  getPersonDataList(page: number = 1, pageSize: number = 20): Observable<any> {
    console.log('[PersonDataService] 獲取人員列表 - 頁面:', page, '每頁數量:', pageSize);
    let params = this.getProjectParams();
    params = params.set('page', page.toString());
    params = params.set('pageSize', pageSize.toString());
    
    return this.http.get<any>(this.apiUrl, { params });
  }

  // 取得單一人員資料
  getPersonData(id: number): Observable<any> {
    console.log('[PersonDataService] 獲取人員資料 - ID:', id);
    const params = this.getProjectParams();
    return this.http.get(`${this.apiUrl}/${id}`, { params });
  }

  // 新增人員資料
  createPersonData(personData: PersonDataRequest): Observable<any> {
    console.log('[PersonDataService] 創建人員資料:', personData);
    const params = this.getProjectParams();
    return this.http.post(this.apiUrl, personData, { params });
  }

  // 更新人員資料
  updatePersonData(id: number, personData: PersonDataRequest): Observable<any> {
    console.log('[PersonDataService] 更新人員資料 - ID:', id, '資料:', personData);
    const params = this.getProjectParams();
    return this.http.put(`${this.apiUrl}/${id}`, personData, { params });
  }

  // 刪除人員資料
  deletePersonData(id: number): Observable<any> {
    console.log('[PersonDataService] 刪除人員資料 - ID:', id);
    const params = this.getProjectParams();
    return this.http.delete(`${this.apiUrl}/${id}`, { params });
  }

  // 搜尋人員資料
  searchPersonData(name?: string, page: number = 1, pageSize: number = 20): Observable<any> {
    console.log('[PersonDataService] 搜尋人員資料 - 名稱:', name, '頁面:', page, '每頁數量:', pageSize);
    let params = this.getProjectParams();
    params = params.set('page', page.toString());
    params = params.set('pageSize', pageSize.toString());
    if (name) {
      params = params.set('query', name); // 後端搜尋 API 使用 query 參數
    }
    return this.http.get(`${this.apiUrl}/search`, { params });
  }
} 