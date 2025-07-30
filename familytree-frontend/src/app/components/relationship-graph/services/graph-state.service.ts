// 關係圖表狀態管理服務
// 統一管理圖表的所有狀態，包括模式切換、選擇狀態、對話框狀態等

import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { GraphNode } from '../../../services/relationship-graph.service';

// 圖表模式枚舉
export enum GraphMode {
  Normal = 'normal',
  CreateRelationship = 'create-relationship',
  Merge = 'merge'
}

// 對話框狀態介面
export interface DialogState {
  showDetailDialog: boolean;
  showRelationshipDialog: boolean;
  showMergeConfirmDialog: boolean;
  showPersonComparison: boolean;
  showNodeMenu: boolean;
}

// 圖表統計資料介面
export interface GraphStatistics {
  familyLinks: number;
  friendLinks: number;
  averageConnections: number;
}

// 選擇狀態介面
export interface SelectionState {
  selectedPerson: GraphNode | null;
  selectedPersonIdForDetail: number | null;
  selectedNodeForMenu: GraphNode | null;
  firstSelectedNode: GraphNode | null;
  secondSelectedNode: GraphNode | null;
  firstSelectedNodeForMerge: GraphNode | null;
  secondSelectedNodeForMerge: GraphNode | null;
  mergePersonAId: number | null;
  mergePersonBId: number | null;
}

// 互動狀態介面
export interface InteractionState {
  isDragging: boolean;
  isDraggingNode: boolean;
  isDraggingMenu: boolean;
  showSearchPanel: boolean;
  menuDragOffset: { x: number; y: number };
  nodeMenuPosition: { x: number; y: number };
}

@Injectable({
  providedIn: 'root'
})
export class GraphStateService {
  // 圖表模式狀態
  private graphModeSubject = new BehaviorSubject<GraphMode>(GraphMode.Normal);
  public graphMode$ = this.graphModeSubject.asObservable();

  // 對話框狀態
  private dialogStateSubject = new BehaviorSubject<DialogState>({
    showDetailDialog: false,
    showRelationshipDialog: false,
    showMergeConfirmDialog: false,
    showPersonComparison: false,
    showNodeMenu: false
  });
  public dialogState$ = this.dialogStateSubject.asObservable();

  // 選擇狀態
  private selectionStateSubject = new BehaviorSubject<SelectionState>({
    selectedPerson: null,
    selectedPersonIdForDetail: null,
    selectedNodeForMenu: null,
    firstSelectedNode: null,
    secondSelectedNode: null,
    firstSelectedNodeForMerge: null,
    secondSelectedNodeForMerge: null,
    mergePersonAId: null,
    mergePersonBId: null
  });
  public selectionState$ = this.selectionStateSubject.asObservable();

  // 互動狀態
  private interactionStateSubject = new BehaviorSubject<InteractionState>({
    isDragging: true,
    isDraggingNode: false,
    isDraggingMenu: false,
    showSearchPanel: false,
    menuDragOffset: { x: 0, y: 0 },
    nodeMenuPosition: { x: 0, y: 0 }
  });
  public interactionState$ = this.interactionStateSubject.asObservable();

  // 圖表統計
  private statisticsSubject = new BehaviorSubject<GraphStatistics>({
    familyLinks: 0,
    friendLinks: 0,
    averageConnections: 0
  });
  public statistics$ = this.statisticsSubject.asObservable();

  // 關聯輸入相關狀態
  private relationshipTypeSubject = new BehaviorSubject<string>('');
  public relationshipType$ = this.relationshipTypeSubject.asObservable();

  // 載入和錯誤狀態
  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();

  private errorSubject = new BehaviorSubject<string>('');
  public error$ = this.errorSubject.asObservable();

  constructor() {}

  // === 模式管理方法 ===

  setGraphMode(mode: GraphMode): void {
    this.graphModeSubject.next(mode);
    
    // 模式切換時重置相關狀態
    if (mode === GraphMode.Normal) {
      this.resetSelectionState();
      this.closeAllDialogs();
    }
  }

  getGraphMode(): GraphMode {
    return this.graphModeSubject.value;
  }

  isCreatingRelationship(): boolean {
    return this.getGraphMode() === GraphMode.CreateRelationship;
  }

  isMerging(): boolean {
    return this.getGraphMode() === GraphMode.Merge;
  }

  // === 對話框管理方法 ===

  updateDialogState(updates: Partial<DialogState>): void {
    const currentState = this.dialogStateSubject.value;
    this.dialogStateSubject.next({ ...currentState, ...updates });
  }

  closeAllDialogs(): void {
    this.updateDialogState({
      showDetailDialog: false,
      showRelationshipDialog: false,
      showMergeConfirmDialog: false,
      showPersonComparison: false,
      showNodeMenu: false
    });
  }

  showDialog(dialogType: keyof DialogState): void {
    this.updateDialogState({ [dialogType]: true });
  }

  closeDialog(dialogType: keyof DialogState): void {
    this.updateDialogState({ [dialogType]: false });
  }

  // === 選擇狀態管理方法 ===

  updateSelectionState(updates: Partial<SelectionState>): void {
    const currentState = this.selectionStateSubject.value;
    this.selectionStateSubject.next({ ...currentState, ...updates });
  }

  resetSelectionState(): void {
    this.updateSelectionState({
      selectedPerson: null,
      selectedPersonIdForDetail: null,
      selectedNodeForMenu: null,
      firstSelectedNode: null,
      secondSelectedNode: null,
      firstSelectedNodeForMerge: null,
      secondSelectedNodeForMerge: null,
      mergePersonAId: null,
      mergePersonBId: null
    });
  }

  // 設置第一個選擇的節點（用於關係建立）
  setFirstSelectedNode(node: GraphNode | null): void {
    this.updateSelectionState({ firstSelectedNode: node });
  }

  // 設置第二個選擇的節點（用於關係建立）
  setSecondSelectedNode(node: GraphNode | null): void {
    this.updateSelectionState({ secondSelectedNode: node });
  }

  // 設置合併的第一個節點
  setFirstSelectedNodeForMerge(node: GraphNode | null): void {
    this.updateSelectionState({ firstSelectedNodeForMerge: node });
  }

  // 設置合併的第二個節點
  setSecondSelectedNodeForMerge(node: GraphNode | null): void {
    this.updateSelectionState({ secondSelectedNodeForMerge: node });
  }

  // === 互動狀態管理方法 ===

  updateInteractionState(updates: Partial<InteractionState>): void {
    const currentState = this.interactionStateSubject.value;
    this.interactionStateSubject.next({ ...currentState, ...updates });
  }

  toggleDragging(): void {
    const current = this.interactionStateSubject.value;
    this.updateInteractionState({ isDragging: !current.isDragging });
  }

  toggleSearchPanel(): void {
    const current = this.interactionStateSubject.value;
    this.updateInteractionState({ showSearchPanel: !current.showSearchPanel });
  }

  setNodeMenuPosition(x: number, y: number): void {
    this.updateInteractionState({ nodeMenuPosition: { x, y } });
  }

  // === 統計資料管理方法 ===

  updateStatistics(stats: Partial<GraphStatistics>): void {
    const currentStats = this.statisticsSubject.value;
    this.statisticsSubject.next({ ...currentStats, ...stats });
  }

  // === 關聯類型管理方法 ===

  setRelationshipType(type: string): void {
    this.relationshipTypeSubject.next(type);
  }

  getRelationshipType(): string {
    return this.relationshipTypeSubject.value;
  }

  clearRelationshipType(): void {
    this.relationshipTypeSubject.next('');
  }

  // === 載入狀態管理方法 ===

  setLoading(loading: boolean): void {
    this.loadingSubject.next(loading);
  }

  setError(error: string): void {
    this.errorSubject.next(error);
  }

  clearError(): void {
    this.errorSubject.next('');
  }

  // === 便利方法 ===

  // 獲取當前狀態快照
  getCurrentState() {
    return {
      graphMode: this.graphModeSubject.value,
      dialogState: this.dialogStateSubject.value,
      selectionState: this.selectionStateSubject.value,
      interactionState: this.interactionStateSubject.value,
      statistics: this.statisticsSubject.value,
      relationshipType: this.relationshipTypeSubject.value,
      loading: this.loadingSubject.value,
      error: this.errorSubject.value
    };
  }

  // 重置所有狀態
  resetAllStates(): void {
    this.setGraphMode(GraphMode.Normal);
    this.closeAllDialogs();
    this.resetSelectionState();
    this.updateInteractionState({
      isDragging: true,
      isDraggingNode: false,
      isDraggingMenu: false,
      showSearchPanel: false,
      menuDragOffset: { x: 0, y: 0 },
      nodeMenuPosition: { x: 0, y: 0 }
    });
    this.clearRelationshipType();
    this.setLoading(false);
    this.clearError();
  }
}