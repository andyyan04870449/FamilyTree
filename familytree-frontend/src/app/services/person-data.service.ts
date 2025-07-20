// 人員資料服務 - 處理人員資料的API呼叫
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConstants } from '../constants/app.constants';

export interface PersonDataModel {
  id: number;
  fileMd5: string;
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
  createdAt: string;
  createdBy?: string;
  updatedAt: string;
  updatedBy?: string;
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
  private apiUrl = `${AppConstants.API_BASE_URL}/persondata`;

  constructor(private http: HttpClient) {}

  // 取得人員資料列表（分頁）
  getPersonDataList(page: number = 1, pageSize: number = 20): Observable<PersonDataListResponse> {
    const params = { page: page.toString(), pageSize: pageSize.toString() };
    return this.http.get<PersonDataListResponse>(this.apiUrl, { params });
  }

  // 取得單一人員資料
  getPersonData(id: number): Observable<PersonDataResponse> {
    return this.http.get<PersonDataResponse>(`${this.apiUrl}/${id}`);
  }

  // 新增人員資料
  createPersonData(personData: PersonDataRequest): Observable<PersonDataResponse> {
    return this.http.post<PersonDataResponse>(this.apiUrl, personData);
  }

  // 更新人員資料
  updatePersonData(id: number, personData: PersonDataRequest): Observable<PersonDataResponse> {
    return this.http.put<PersonDataResponse>(`${this.apiUrl}/${id}`, personData);
  }

  // 刪除人員資料
  deletePersonData(id: number): Observable<PersonDataResponse> {
    return this.http.delete<PersonDataResponse>(`${this.apiUrl}/${id}`);
  }

  // 搜尋人員資料
  searchPersonData(name?: string, page: number = 1, pageSize: number = 20): Observable<PersonDataListResponse> {
    const params: any = { page: page.toString(), pageSize: pageSize.toString() };
    if (name) {
      params.name = name;
    }
    return this.http.get<PersonDataListResponse>(`${this.apiUrl}/search`, { params });
  }
} 