import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AppConstants } from '../constants/app.constants';
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
export class PersonService {
  private apiUrl = AppConstants.PERSON_API_URL;

  constructor(
    private http: HttpClient,
    private projectService: ProjectService
  ) { }

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

  getPersons(): Observable<Person[]> {
    const params = this.getProjectParams();
    return this.http.get<any>(this.apiUrl, { params }).pipe(
      map(response => {
        // 適配新的後端回應格式 { success: true, personDataList: [...], pagination: {...} }
        if (response && response.personDataList) {
          return response.personDataList;
        }
        // 如果是舊格式直接返回陣列
        return Array.isArray(response) ? response : [];
      })
    );
  }

  getPerson(id: number): Observable<Person> {
    const params = this.getProjectParams();
    return this.http.get<any>(`${this.apiUrl}/${id}`, { params }).pipe(
      map(response => {
        // 適配新的後端回應格式 { success: true, personData: {...} }
        if (response && response.personData) {
          return response.personData;
        }
        // 如果是舊格式直接返回物件
        return response;
      })
    );
  }

  createPerson(person: Omit<Person, 'id' | 'createdAt' | 'updatedAt'>): Observable<Person> {
    const params = this.getProjectParams();
    return this.http.post<Person>(this.apiUrl, person, { params });
  }

  updatePerson(id: number, person: Partial<Person>): Observable<Person> {
    const params = this.getProjectParams();
    return this.http.put<Person>(`${this.apiUrl}/${id}`, person, { params });
  }

  deletePerson(id: number): Observable<void> {
    const params = this.getProjectParams();
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { params });
  }

  searchPersons(name?: string, birthDate?: string): Observable<Person[]> {
    let params = this.getProjectParams();
    
    if (name) {
      params = params.set('name', name);
    }
    if (birthDate) {
      params = params.set('birthDate', birthDate);
    }
    
    return this.http.get<Person[]>(`${this.apiUrl}/search`, { params });
  }
} 