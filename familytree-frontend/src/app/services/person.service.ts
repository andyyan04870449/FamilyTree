import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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

@Injectable({
  providedIn: 'root'
})
export class PersonService {
  private apiUrl = 'http://localhost:5088/api/person';

  constructor(private http: HttpClient) { }

  getPersons(): Observable<Person[]> {
    return this.http.get<Person[]>(this.apiUrl);
  }

  getPerson(id: number): Observable<Person> {
    return this.http.get<Person>(`${this.apiUrl}/${id}`);
  }

  createPerson(person: Omit<Person, 'id' | 'createdAt' | 'updatedAt'>): Observable<Person> {
    return this.http.post<Person>(this.apiUrl, person);
  }

  updatePerson(id: number, person: Partial<Person>): Observable<Person> {
    return this.http.put<Person>(`${this.apiUrl}/${id}`, person);
  }

  deletePerson(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  searchPersons(name?: string, birthDate?: string): Observable<Person[]> {
    let url = `${this.apiUrl}/search`;
    const params: string[] = [];
    
    if (name) {
      params.push(`name=${encodeURIComponent(name)}`);
    }
    if (birthDate) {
      params.push(`birthDate=${encodeURIComponent(birthDate)}`);
    }
    
    if (params.length > 0) {
      url += '?' + params.join('&');
    }
    
    return this.http.get<Person[]>(url);
  }

  // 視覺分析相關方法
  startAnalysis(personId: number): Observable<AnalysisResponse> {
    return this.http.post<AnalysisResponse>('http://localhost:5088/api/analysis/start', { personId });
  }

                    getAnalysisProgress(personId: number): Observable<AnalysisResponse> {
                    return this.http.get<AnalysisResponse>(`http://localhost:5088/api/analysis/progress/${personId}`);
                  }

                  getAllAnalysisJobs(): Observable<AnalysisJobsResponse> {
                    return this.http.get<AnalysisJobsResponse>('http://localhost:5088/api/analysis/jobs');
                  }

                  stopAnalysis(personId: number): Observable<AnalysisResponse> {
                    const url = `http://localhost:5088/api/analysis/stop/${personId}`;
                    console.log('調用終止API:', url);
                    return this.http.delete<AnalysisResponse>(url);
                  }

  resetAnalysis(personId: number): Observable<AnalysisResponse> {
    const url = `http://localhost:5088/api/analysis/reset/${personId}`;
    console.log('調用重置API:', url);
    return this.http.post<AnalysisResponse>(url, {});
  }

  getAnalysisResult(personId: number): Observable<AnalysisResultResponse> {
    const url = `http://localhost:5088/api/analysis/result/${personId}`;
    console.log('調用獲取分析結果API:', url);
    return this.http.get<AnalysisResultResponse>(url);
  }
} 