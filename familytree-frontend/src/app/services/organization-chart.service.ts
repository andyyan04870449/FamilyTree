import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface OrgNode {
  id: string;
  name: string;
  position: string;
  department: string;
  phone?: string;
  email?: string;
  joinDate: string;
  avatar?: string;
  x: number;
  y: number;
}

export interface OrgConnection {
  from: string;
  to: string;
  type?: 'solid' | 'dashed';
}

export interface OrgFilter {
  id: string;
  label: string;
  count: number;
  checked: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class OrganizationChartService {
  private nodesSubject = new BehaviorSubject<Record<string, OrgNode>>({});
  private connectionsSubject = new BehaviorSubject<OrgConnection[]>([]);
  private filtersSubject = new BehaviorSubject<OrgFilter[]>([
    { id: 'all', label: '全部', count: 6, checked: true },
    { id: 'leader', label: '主管職', count: 3, checked: true },
    { id: 'engineer', label: '工程師', count: 2, checked: true },
    { id: 'operations', label: '營運人員', count: 1, checked: true }
  ]);

  nodes$ = this.nodesSubject.asObservable();
  connections$ = this.connectionsSubject.asObservable();
  filters$ = this.filtersSubject.asObservable();

  constructor() {
    this.initializeSampleData();
  }

  private initializeSampleData(): void {
    const sampleNodes: Record<string, OrgNode> = {
      'node-1': {
        id: 'node-1',
        name: '陳XX',
        position: '總經理',
        department: '管理層',
        phone: '0912-345678',
        joinDate: '2020-03-17',
        avatar: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150&h=150&fit=crop&crop=face',
        x: 400,
        y: 50
      },
      'node-2': {
        id: 'node-2',
        name: '李XX',
        position: '技術總監',
        department: '技術部',
        phone: '0912-345679',
        joinDate: '2019-01-22',
        avatar: 'https://images.unsplash.com/photo-1494790108755-2616b332db04?w=150&h=150&fit=crop&crop=face',
        x: 200,
        y: 250
      },
      'node-3': {
        id: 'node-3',
        name: '王XX',
        position: '營運經理',
        department: '營運部',
        phone: '0912-345680',
        joinDate: '2023-11-19',
        x: 600,
        y: 250
      },
      'node-4': {
        id: 'node-4',
        name: '張XX',
        position: '軟體工程師',
        department: '技術部',
        joinDate: '2018-10-02',
        x: 100,
        y: 450
      },
      'node-5': {
        id: 'node-5',
        name: '劉XX',
        position: '資深軟體工程師',
        department: '技術部',
        joinDate: '2018-10-02',
        avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150&h=150&fit=crop&crop=face',
        x: 300,
        y: 450
      },
      'node-6': {
        id: 'node-6',
        name: '黃XX(女)',
        position: '營運小姊手',
        department: '營運部',
        joinDate: '2018-10-02',
        x: 600,
        y: 450
      }
    };

    const sampleConnections: OrgConnection[] = [
      { from: 'node-1', to: 'node-2', type: 'solid' },
      { from: 'node-1', to: 'node-3', type: 'solid' },
      { from: 'node-2', to: 'node-4', type: 'solid' },
      { from: 'node-2', to: 'node-5', type: 'dashed' },
      { from: 'node-3', to: 'node-6', type: 'solid' }
    ];

    this.nodesSubject.next(sampleNodes);
    this.connectionsSubject.next(sampleConnections);
  }

  getNodes(): Record<string, OrgNode> {
    return this.nodesSubject.value;
  }

  getConnections(): OrgConnection[] {
    return this.connectionsSubject.value;
  }

  updateNodePosition(id: string, x: number, y: number): void {
    const nodes = this.nodesSubject.value;
    if (nodes[id]) {
      nodes[id] = { ...nodes[id], x, y };
      this.nodesSubject.next({ ...nodes });
    }
  }

  updateNode(node: OrgNode): void {
    const nodes = this.nodesSubject.value;
    nodes[node.id] = node;
    this.nodesSubject.next({ ...nodes });
  }

  addNode(node: Omit<OrgNode, 'id'>): string {
    const id = `node-${Date.now()}`;
    const newNode: OrgNode = { ...node, id };
    const nodes = this.nodesSubject.value;
    nodes[id] = newNode;
    this.nodesSubject.next({ ...nodes });
    return id;
  }

  deleteNode(id: string): void {
    const nodes = this.nodesSubject.value;
    delete nodes[id];
    this.nodesSubject.next({ ...nodes });
    
    const connections = this.connectionsSubject.value.filter(
      conn => conn.from !== id && conn.to !== id
    );
    this.connectionsSubject.next(connections);
  }

  addConnection(connection: OrgConnection): void {
    const connections = [...this.connectionsSubject.value, connection];
    this.connectionsSubject.next(connections);
  }

  removeConnection(from: string, to: string): void {
    const connections = this.connectionsSubject.value.filter(
      conn => !(conn.from === from && conn.to === to)
    );
    this.connectionsSubject.next(connections);
  }

  updateFilter(filterId: string, checked: boolean): void {
    const filters = this.filtersSubject.value.map(filter =>
      filter.id === filterId ? { ...filter, checked } : filter
    );
    this.filtersSubject.next(filters);
  }

  searchNodes(searchTerm: string): OrgNode[] {
    if (!searchTerm) return Object.values(this.nodesSubject.value);
    
    const term = searchTerm.toLowerCase();
    return Object.values(this.nodesSubject.value).filter(node =>
      node.name.toLowerCase().includes(term) ||
      node.position.toLowerCase().includes(term) ||
      node.department.toLowerCase().includes(term) ||
      (node.phone && node.phone.includes(term))
    );
  }

  getFilteredNodes(activeFilters: string[]): OrgNode[] {
    if (activeFilters.includes('all')) {
      return Object.values(this.nodesSubject.value);
    }

    return Object.values(this.nodesSubject.value).filter(node => {
      if (activeFilters.includes('leader') && 
          ['總經理', '技術總監', '營運經理'].includes(node.position)) {
        return true;
      }
      if (activeFilters.includes('engineer') && 
          node.position.includes('工程師')) {
        return true;
      }
      if (activeFilters.includes('operations') && 
          node.department === '營運部' && !node.position.includes('經理')) {
        return true;
      }
      return false;
    });
  }
}