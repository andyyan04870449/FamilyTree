/**
 * 視覺化分析服務
 * 提供視覺化分析圖的CRUD操作API呼叫
 */

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConstants } from '../constants/app.constants';

// 介面定義
export interface VisualAnalysisGraph {
  id: number;
  name: string;
  projectIds: string;
  updatedBy: string;
  updatedAt: string;
  relationCount: number;
  cases: string[];
}

export interface CreateVisualAnalysisRequest {
  name: string;
  projectIds: string[];
  updatedBy?: string;
}

export interface UpdateVisualAnalysisRequest {
  name: string;
  projectIds: string[];
  updatedBy?: string;
}

export interface VisualAnalysisListResponse {
  success: boolean;
  message: string;
  graphs: VisualAnalysisGraph[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface VisualAnalysisApiResponse {
  success: boolean;
  message: string;
  data?: any;
}

@Injectable({
  providedIn: 'root'
})
export class VisualAnalysisService {
  private readonly baseUrl = `${AppConstants.API_BASE_URL}/VisualAnalysis`;

  constructor(private http: HttpClient) { }

  /**
   * 獲取視覺化分析圖列表
   */
  getVisualAnalysisGraphs(pageNumber: number = 1, pageSize: number = 10): Observable<VisualAnalysisListResponse> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<VisualAnalysisListResponse>(this.baseUrl, { params });
  }

  /**
   * 創建新的視覺化分析圖
   */
  createVisualAnalysisGraph(request: CreateVisualAnalysisRequest): Observable<VisualAnalysisApiResponse> {
    const payload = {
      name: request.name,
      projectIds: request.projectIds,
      updatedBy: request.updatedBy || 'user'
    };

    return this.http.post<VisualAnalysisApiResponse>(this.baseUrl, payload);
  }

  /**
   * 更新視覺化分析圖
   */
  updateVisualAnalysisGraph(id: number, request: UpdateVisualAnalysisRequest): Observable<VisualAnalysisApiResponse> {
    const payload = {
      name: request.name,
      projectIds: request.projectIds,
      updatedBy: request.updatedBy || 'user'
    };

    return this.http.put<VisualAnalysisApiResponse>(`${this.baseUrl}/${id}`, payload);
  }

  /**
   * 刪除視覺化分析圖
   */
  deleteVisualAnalysisGraph(id: number): Observable<VisualAnalysisApiResponse> {
    return this.http.delete<VisualAnalysisApiResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * 獲取編輯器資料
   */
  getEditorData(id: number): Observable<VisualAnalysisEditorData> {
    return this.http.get<VisualAnalysisEditorData>(`${this.baseUrl}/${id}/editor`);
  }

  /**
   * 更新節點可見性
   */
  updateNodeVisibility(id: number, request: UpdateNodeVisibilityRequest): Observable<VisualAnalysisApiResponse> {
    return this.http.put<VisualAnalysisApiResponse>(`${this.baseUrl}/${id}/nodes/visibility`, request);
  }

  // 建立關係 (使用關係圖譜的 API)
  createRelationship(request: CreateVisualAnalysisRelationshipRequest): Observable<VisualAnalysisApiResponse> {
    console.log('🔗 視覺化分析 - 建立人員關係:', request);
    return this.http.post<VisualAnalysisApiResponse>(`${AppConstants.API_BASE_URL}/RelationshipGraph/create-relationship`, request);
  }
}

// 編輯器相關介面
export interface VisualAnalysisNode {
  id: number;
  graphId: number;
  projectId: string;
  personId: number;
  isVisible: boolean;
  nodeX: number;
  nodeY: number;
  createdAt: string;
  updatedAt: string;
  personName: string;
  personGender: string;
  projectName: string;
}

export interface PersonNode {
  personId: number;
  name: string;
  isVisible: boolean;
}

export interface ProjectNodeGroup {
  projectId: string;
  projectName: string;
  persons: PersonNode[];
}

export interface VisualAnalysisEditorData {
  success: boolean;
  message: string;
  graph: VisualAnalysisGraph;
  nodes: VisualAnalysisNode[];
  relationships: VisualAnalysisRelationship[];
  projectGroups: ProjectNodeGroup[];
}

export interface VisualAnalysisRelationship {
  id: number;
  sourcePersonId: number;
  targetPersonId: number;
  relationType: string;
  visualAnalysisGraphId?: number;
  sourcePersonName: string;
  targetPersonName: string;
}

export interface NodeVisibilityUpdate {
  projectId: string;
  personId: number;
  isVisible: boolean;
}

export interface UpdateNodeVisibilityRequest {
  updates: NodeVisibilityUpdate[];
}

// 建立關係請求介面
export interface CreateVisualAnalysisRelationshipRequest {
  sourcePersonId: number;
  targetPersonId: number;
  relationshipType: string;
  visualAnalysisGraphId?: number; // 視覺化分析圖表ID，可為空
} 