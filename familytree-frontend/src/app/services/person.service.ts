import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
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

export interface AnalysisRequest {
  personId: number;
}

export interface AnalysisProgress {
  personId: number;
  personName: string;
  progressPercentage: number;
  status: string;
  analysisResult?: any;
}

export interface AnalysisResponse {
  success: boolean;
  message: string;
  personId?: number;
  data?: AnalysisProgress;
}

export interface AnalysisJobsResponse {
  success: boolean;
  message: string;
  data?: AnalysisJob[];
}

export interface AnalysisJob {
  personId: number;
  personName: string;
  status: 'processing' | 'completed' | 'failed';
  progressPercentage: number;
  startTime: string;
  completedTime?: string;
  errorMessage?: string;
  currentStep?: string;
  statusMessage?: string;
}

export interface AnalysisResultResponse {
  success: boolean;
  message: string;
  data?: {
    personId: number;
    personName: string;
    analysisResult: any;
    analysisDate: string;
    status: string;
  };
}

export interface MissingPerson {
  id: number;
  name: string;
  relationType: string;
  sourcePersonId: number;
  sourceField: string;
  analysisSessionId: string;
  layerDepth: number;
  discoveredAt: string;
  status: string;
  resolvedPersonId?: number;
  notes?: string;
  sourcePersonName?: string;
  resolvedPersonName?: string;
}

export interface MissingPersonStats {
  stats: Array<{ status: string; count: number }>;
  total: number;
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
      console.log('🎯 添加專案 ID 到請求:', currentProject.id);
    } else {
      console.warn('⚠️ 沒有當前專案，不進行 API 請求');
      throw new Error('請先選擇專案');
    }
    
    return params;
  }

  getPersons(): Observable<Person[]> {
    const params = this.getProjectParams();
    return this.http.get<Person[]>(this.apiUrl, { params });
  }

  getPerson(id: number): Observable<Person> {
    const params = this.getProjectParams();
    return this.http.get<Person>(`${this.apiUrl}/${id}`, { params });
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

  // 視覺分析相關方法
  startAnalysis(personId: number): Observable<AnalysisResponse> {
    const params = this.getProjectParams();
    const body = { personId };
    return this.http.post<AnalysisResponse>(`${AppConstants.ANALYSIS_API_URL}/start`, body, { params });
  }

  getAnalysisProgress(personId: number): Observable<AnalysisResponse> {
    const params = this.getProjectParams();
    return this.http.get<AnalysisResponse>(`${AppConstants.ANALYSIS_API_URL}/progress/${personId}`, { params });
  }

  getAllAnalysisJobs(): Observable<AnalysisJobsResponse> {
    const params = this.getProjectParams();
    return this.http.get<AnalysisJobsResponse>(`${AppConstants.ANALYSIS_API_URL}/jobs`, { params });
  }

  stopAnalysis(personId: number): Observable<AnalysisResponse> {
    const params = this.getProjectParams();
    const url = `${AppConstants.ANALYSIS_API_URL}/stop/${personId}`;
    console.log('調用終止API:', url);
    return this.http.delete<AnalysisResponse>(url, { params });
  }

  resetAnalysis(personId: number): Observable<AnalysisResponse> {
    const params = this.getProjectParams();
    const url = `${AppConstants.ANALYSIS_API_URL}/reset/${personId}`;
    console.log('調用重置API:', url);
    return this.http.post<AnalysisResponse>(url, {}, { params });
  }

  getAnalysisResult(personId: number): Observable<AnalysisResultResponse> {
    const params = this.getProjectParams();
    const url = `${AppConstants.ANALYSIS_API_URL}/result/${personId}`;
    console.log('調用獲取分析結果API:', url);
    return this.http.get<AnalysisResultResponse>(url, { params });
  }
} 