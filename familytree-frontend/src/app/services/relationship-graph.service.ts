// 關聯圖譜服務：提供通用的圖譜數據處理和分析功能
// 主要功能：數據轉換、關係分析、圖譜生成

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppConstants } from '../constants/app.constants';

export interface GraphNode {
  id: string;
  name: string;
  gender: 'male' | 'female';
  photo?: string;
  isExpanded?: boolean;
  x?: number;
  y?: number;
  fx?: number | null;
  fy?: number | null;
  data?: any; // 原始數據
}

export interface GraphLink {
  source: string;
  target: string;
  type: string;
  isFamily: boolean;
  strength?: number;
}

export interface GraphData {
  nodes: GraphNode[];
  links: GraphLink[];
  metadata?: {
    totalNodes: number;
    totalLinks: number;
    familyLinks: number;
    friendLinks: number;
    analysisDate: string;
  };
}

export interface AnalysisRequest {
  personIds: number[];
  analysisType: 'all' | 'selected';
  maxDepth?: number;
}

export interface AnalysisResponse {
  success: boolean;
  message: string;
  data?: GraphData;
}

@Injectable({
  providedIn: 'root'
})
export class RelationshipGraphService {
  private baseUrl = AppConstants.API_BASE_URL;

  constructor(private http: HttpClient) {
    console.log('🔗 RelationshipGraphService 初始化');
  }

  /**
   * 分析所有人員的關聯關係
   */
  analyzeAllPersons(): Observable<AnalysisResponse> {
    console.log('📊 分析所有人員關聯關係');
    return this.http.post<AnalysisResponse>(`${this.baseUrl}/RelationshipGraph/analyze-all`, {});
  }

  /**
   * 分析選定人員的關聯關係
   */
  analyzeSelectedPersons(personIds: number[]): Observable<AnalysisResponse> {
    console.log('📊 分析選定人員關聯關係:', personIds);
    return this.http.post<AnalysisResponse>(`${this.baseUrl}/RelationshipGraph/analyze-selected`, {
      personIds,
      maxDepth: 3
    });
  }

  /**
   * 將人員數據轉換為圖譜節點
   */
  convertPersonsToNodes(persons: any[]): GraphNode[] {
    return persons.map(person => ({
      id: person.id.toString(),
      name: person.name,
      gender: person.gender === '男' ? 'male' : 'female',
      isExpanded: true,
      data: person
    }));
  }

  /**
   * 解析家族關係並轉換為圖譜連線
   */
  parseFamilyRelationships(persons: any[]): GraphLink[] {
    const links: GraphLink[] = [];
    
    for (const person of persons) {
      if (person.familyRelationships) {
        const lines = person.familyRelationships.split('\n').filter((line: string) => line.trim());
        
        for (const line of lines) {
          const parts = line.split('：');
          if (parts.length === 2) {
            const relationType = parts[0].trim();
            const targetNames = parts[1].split(/[,，、]/).map((name: string) => name.trim());
            
            for (const targetName of targetNames) {
              if (targetName) {
                const targetPerson = persons.find(p => p.name === targetName);
                if (targetPerson) {
                  links.push({
                    source: person.id.toString(),
                    target: targetPerson.id.toString(),
                    type: relationType,
                    isFamily: true,
                    strength: 1.0
                  });
                }
              }
            }
          }
        }
      }
    }
    
    return links;
  }

  /**
   * 解析朋友關係並轉換為圖譜連線
   */
  parseFriendRelationships(persons: any[]): GraphLink[] {
    const links: GraphLink[] = [];
    
    for (const person of persons) {
      if (person.friends) {
        const lines = person.friends.split('\n').filter((line: string) => line.trim());
        
        for (const line of lines) {
          const parts = line.split(/[,，、]/);
          for (const part of parts) {
            const trimmedPart = part.trim();
            if (trimmedPart) {
              const nameMatch = trimmedPart.match(/^([^\s，、]+)/);
              if (nameMatch) {
                const name = nameMatch[1].trim();
                if (name) {
                  const friendPerson = persons.find(p => p.name === name);
                  if (friendPerson) {
                    links.push({
                      source: person.id.toString(),
                      target: friendPerson.id.toString(),
                      type: '朋友',
                      isFamily: false,
                      strength: 0.5
                    });
                  }
                }
              }
            }
          }
        }
      }
    }
    
    return links;
  }

  /**
   * 生成完整的圖譜數據
   */
  generateGraphData(persons: any[]): GraphData {
    console.log('🔗 生成圖譜數據，人員數量:', persons.length);
    
    const nodes = this.convertPersonsToNodes(persons);
    const familyLinks = this.parseFamilyRelationships(persons);
    const friendLinks = this.parseFriendRelationships(persons);
    
    // 合併所有連線並移除重複
    const allLinks = [...familyLinks, ...friendLinks];
    const uniqueLinks = this.removeDuplicateLinks(allLinks);
    
    const metadata = {
      totalNodes: nodes.length,
      totalLinks: uniqueLinks.length,
      familyLinks: familyLinks.length,
      friendLinks: friendLinks.length,
      analysisDate: new Date().toISOString()
    };
    
    return {
      nodes,
      links: uniqueLinks,
      metadata
    };
  }

  /**
   * 移除重複的連線
   */
  private removeDuplicateLinks(links: GraphLink[]): GraphLink[] {
    return links.filter((link, index, self) => 
      index === self.findIndex(l => 
        (l.source === link.source && l.target === link.target && l.type === link.type) ||
        (l.source === link.target && l.target === link.source && l.type === link.type)
      )
    );
  }

  /**
   * 獲取圖譜統計信息
   */
  getGraphStatistics(graphData: GraphData): any {
    const familyLinks = graphData.links.filter(link => link.isFamily);
    const friendLinks = graphData.links.filter(link => !link.isFamily);
    
    const maleNodes = graphData.nodes.filter(node => node.gender === 'male');
    const femaleNodes = graphData.nodes.filter(node => node.gender === 'female');
    
    return {
      totalNodes: graphData.nodes.length,
      totalLinks: graphData.links.length,
      familyLinks: familyLinks.length,
      friendLinks: friendLinks.length,
      maleNodes: maleNodes.length,
      femaleNodes: femaleNodes.length,
      averageConnections: graphData.nodes.length > 0 ? 
        (graphData.links.length / graphData.nodes.length).toFixed(2) : '0'
    };
  }
} 