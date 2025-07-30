// 重構後的關聯圖譜組件
// 使用模組化架構，整合新的服務和子組件

import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, Observable } from 'rxjs';

// 服務導入
import { RelationshipGraphService, GraphData, GraphNode, GraphLink, CreateRelationshipResponse } from '../../services/relationship-graph.service';
import { LogService } from '../../services/log.service';
import { PhotoUtilsService } from '../../services/photo-utils.service';
import { ProjectService } from '../../services/project.service';

// 新建立的服務
import { GraphStateService, GraphMode, GraphStatistics, DialogState, SelectionState, InteractionState } from './services/graph-state.service';
import { GraphInteractionService } from './services/graph-interaction.service';

// 子組件導入
import { PersonDetailDialogComponent } from '../person-detail-dialog/person-detail-dialog.component';
import { PersonComparisonComponent } from '../person-comparison/person-comparison.component';
import { MergeDialogComponent, MergeDialogData, MergeSelection } from './components/merge-dialog.component';
import { RelationshipDialogComponent, RelationshipDialogData, RelationshipCreationData } from './components/relationship-dialog.component';
import { NodeMenuComponent, NodeMenuAction, MenuPosition } from './components/node-menu.component';
import { ModeIndicatorComponent } from './components/mode-indicator.component';

@Component({
  selector: 'app-relationship-graph',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    PersonDetailDialogComponent, 
    PersonComparisonComponent,
    MergeDialogComponent,
    RelationshipDialogComponent,
    NodeMenuComponent,
    ModeIndicatorComponent
  ],
  template: `
    <div class="relationship-graph-container">
      <!-- 圖譜標題和統計 -->
      <div class="graph-header">
        <div class="header-left">
          <h2>🔗 關聯圖譜</h2>
          <p class="graph-subtitle">{{ graphData?.metadata?.totalNodes || 0 }} 個節點，{{ graphData?.metadata?.totalLinks || 0 }} 個關係</p>
        </div>

        <div class="header-right">
          <div class="statistics">
            <div class="stat-item">
              <span class="stat-label">家族關係</span>
              <span class="stat-value">{{ (statistics$ | async)?.familyLinks || 0 }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">朋友關係</span>
              <span class="stat-value">{{ (statistics$ | async)?.friendLinks || 0 }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">平均連接</span>
              <span class="stat-value">{{ (statistics$ | async)?.averageConnections || 0 }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 載入狀態 -->
      <div class="loading-container" *ngIf="loading$ | async">
        <div class="loading-spinner"></div>
        <p>正在分析關聯關係...</p>
      </div>

      <!-- 錯誤狀態 -->
      <div class="error-container" *ngIf="error$ | async as error">
        <div class="error-icon">⚠️</div>
        <p>{{ error }}</p>
        <button class="btn btn-primary" (click)="retryAnalysis()">重試</button>
      </div>

      <!-- 圖譜容器 -->
      <div class="graph-viewport" *ngIf="!(loading$ | async) && !(error$ | async)" #graphViewport>
        <div class="graph-container" #graphContainer></div>
        
        <!-- 圖例 -->
        <div class="legend">
          <div class="legend-item">
            <div class="legend-color male"></div>
            <span>男性</span>
          </div>
          <div class="legend-item">
            <div class="legend-color female"></div>
            <span>女性</span>
          </div>
          <div class="legend-item">
            <div class="legend-line family"></div>
            <span>家族關係</span>
          </div>
          <div class="legend-item">
            <div class="legend-line friend"></div>
            <span>朋友關係</span>
          </div>
          
          <!-- 搜尋功能 -->
          <div class="legend-divider"></div>
          <div class="legend-search">
            <button 
              class="search-toggle-btn" 
              [class.active]="(interactionState$ | async)?.showSearchPanel"
              (click)="toggleSearchPanel()"
              title="搜尋">
              🔍 搜尋
            </button>
          </div>
        </div>
        
        <!-- 控制面板 -->
        <div class="graph-controls">
          <button class="control-btn" (click)="zoomIn()" title="放大">+</button>
          <button class="control-btn" (click)="zoomOut()" title="縮小">−</button>
          <button class="control-btn" (click)="resetView()" title="重置視圖">⌂</button>
          <button class="control-btn" (click)="toggleDrag()" title="切換拖曳">
            {{ (interactionState$ | async)?.isDragging ? '🔒' : '🔓' }}
          </button>
          <button class="control-btn" (click)="toggleFullscreen()" title="全螢幕">⛶</button>
        </div>
      </div>

      <!-- 模式指示器 -->
      <app-mode-indicator
        [mode]="graphMode$ | async"
        [selectedCount]="getSelectedCount()"
        (cancelled)="cancelCurrentMode()">
      </app-mode-indicator>

      <!-- 節點選單 -->
      <app-node-menu
        [show]="(dialogState$ | async)?.showNodeMenu || false"
        [selectedNode]="(selectionState$ | async)?.selectedNodeForMenu || null"
        [position]="nodeMenuPosition"
        (actionSelected)="onNodeMenuAction($event)"
        (closed)="closeNodeMenu()">
      </app-node-menu>

      <!-- 關係建立對話框 -->
      <app-relationship-dialog
        [show]="(dialogState$ | async)?.showRelationshipDialog || false"
        [relationshipData]="getRelationshipDialogData()"
        [isProcessing]="isProcessingRelationship"
        (confirmed)="confirmRelationshipCreation($event)"
        (closed)="closeRelationshipDialog()">
      </app-relationship-dialog>

      <!-- 合併確認對話框 -->
      <app-merge-dialog
        [show]="(dialogState$ | async)?.showMergeConfirmDialog || false"
        [mergeData]="getMergeDialogData()"
        [isProcessing]="isProcessingMerge"
        (confirmed)="confirmMerge($event)"
        (closed)="closeMergeDialog()">
      </app-merge-dialog>

      <!-- 人員詳情對話框 -->
      <app-person-detail-dialog
        *ngIf="(dialogState$ | async)?.showDetailDialog"
        [personId]="(selectionState$ | async)?.selectedPersonIdForDetail || 0"
        (closed)="closePersonDetailDialog()">
      </app-person-detail-dialog>

      <!-- 人員比較對話框 -->
      <app-person-comparison
        *ngIf="(dialogState$ | async)?.showPersonComparison"
        [personAId]="(selectionState$ | async)?.mergePersonAId || 0"
        [personBId]="(selectionState$ | async)?.mergePersonBId || 0"
        (closed)="closePersonComparison()"
        (mergeSelected)="handleMergeComparison($event)">
      </app-person-comparison>
    </div>
  `,
  styleUrls: ['./relationship-graph.component.scss']
})
export class RelationshipGraphComponent implements OnInit, OnChanges, AfterViewInit, OnDestroy {
  @ViewChild('graphContainer', { static: false }) graphContainer!: ElementRef;
  @ViewChild('graphViewport', { static: false }) graphViewport!: ElementRef;

  @Input() graphData?: GraphData;
  @Input() autoAnalyze: boolean = false;
  @Input() selectedPersonIds: number[] = [];
  @Input() visualAnalysisGraphId?: number;

  @Output() onShowAllNodes = new EventEmitter<void>();
  @Output() relationshipCreated = new EventEmitter<void>();

  // 可觀察狀態 - 在 ngOnInit 中初始化
  graphMode$: Observable<GraphMode> | undefined;
  dialogState$: Observable<DialogState> | undefined;
  selectionState$: Observable<SelectionState> | undefined;
  interactionState$: Observable<InteractionState> | undefined;
  statistics$: Observable<GraphStatistics> | undefined;
  loading$: Observable<boolean> | undefined;
  error$: Observable<string> | undefined;

  // 處理狀態
  isProcessingRelationship = false;
  isProcessingMerge = false;
  nodeMenuPosition: MenuPosition = { x: 0, y: 0 };

  private subscriptions: Subscription[] = [];

  constructor(
    private relationshipGraphService: RelationshipGraphService,
    private stateService: GraphStateService,
    private interactionService: GraphInteractionService,
    private cdr: ChangeDetectorRef,
    private logService: LogService,
    private photoUtils: PhotoUtilsService,
    private projectService: ProjectService
  ) {}

  ngOnInit(): void {
    // 初始化可觀察狀態
    this.graphMode$ = this.stateService.graphMode$;
    this.dialogState$ = this.stateService.dialogState$;
    this.selectionState$ = this.stateService.selectionState$;
    this.interactionState$ = this.stateService.interactionState$;
    this.statistics$ = this.stateService.statistics$;
    this.loading$ = this.stateService.loading$;
    this.error$ = this.stateService.error$;
    
    this.logService.info('RelationshipGraphComponent initialized', '');
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['graphData'] && this.graphData) {
      this.updateGraph();
    }
    if (changes['autoAnalyze'] && this.autoAnalyze) {
      this.performAnalysis();
    }
  }

  ngAfterViewInit(): void {
    if (this.graphData && this.graphContainer) {
      setTimeout(() => {
        this.initializeGraph();
        // 在圖表初始化完成後設置狀態訂閱
        this.setupStateSubscriptions();
      }, 100);
    } else {
      // 即使沒有圖表數據，也要設置狀態訂閱（但 updateNodeStates 會安全地退出）
      setTimeout(() => {
        this.setupStateSubscriptions();
      }, 100);
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.interactionService.destroy();
    this.stateService.resetAllStates();
  }

  // === 設置訂閱 ===
  private setupStateSubscriptions(): void {
    // 訂閱圖表模式變化
    if (this.graphMode$) {
      this.subscriptions.push(
        this.graphMode$.subscribe((mode: any) => {
          this.interactionService.updateNodeStates();
        })
      );
    }

    // 訂閱選擇狀態變化
    if (this.selectionState$) {
      this.subscriptions.push(
        this.selectionState$.subscribe((state: any) => {
          this.interactionService.updateNodeStates();
        })
      );
    }
  }

  // === 圖表初始化和更新 ===
  private initializeGraph(): void {
    if (!this.graphData || !this.graphContainer) return;

    this.interactionService.initializeGraph(
      this.graphContainer,
      this.graphData,
      (event, node) => this.handleNodeClick(event, node),
      (event, node) => this.handleNodeContextMenu(event, node)
    );

    this.updateStatistics();
  }

  private updateGraph(): void {
    if (this.graphData) {
      this.initializeGraph();
    }
  }

  // === 統計更新 ===
  private updateStatistics(): void {
    if (!this.graphData) return;

    const stats: GraphStatistics = {
      familyLinks: this.graphData.links.filter(link => link.type === 'family').length,
      friendLinks: this.graphData.links.filter(link => link.type === 'friend').length,
      averageConnections: this.calculateAverageConnections()
    };

    this.stateService.updateStatistics(stats);
  }

  private calculateAverageConnections(): number {
    if (!this.graphData?.nodes.length) return 0;
    
    const totalConnections = this.graphData.links.length * 2; // 每個連線影響兩個節點
    return Math.round((totalConnections / this.graphData.nodes.length) * 10) / 10;
  }

  // === 節點互動處理 ===
  handleNodeClick(event: MouseEvent, node: GraphNode): void {
    const currentMode = this.stateService.getGraphMode();
    
    switch (currentMode) {
      case GraphMode.CreateRelationship:
        this.handleRelationshipNodeClick(node);
        break;
      case GraphMode.Merge:
        this.handleMergeNodeClick(node);
        break;
      default:
        // 正常模式下的單擊行為
        this.stateService.updateSelectionState({ selectedPerson: node });
        break;
    }
  }

  handleNodeContextMenu(event: MouseEvent, node: GraphNode): void {
    event.preventDefault();
    
    this.logService.info('RelationshipGraphComponent', `節點右鍵選單觸發，節點：${node.name}`);
    
    // 計算選單位置
    this.nodeMenuPosition = this.calculateMenuPosition(event.clientX, event.clientY);
    
    // 設置選擇的節點和顯示選單
    this.stateService.updateSelectionState({ selectedNodeForMenu: node });
    this.stateService.showDialog('showNodeMenu');
    
    this.logService.info('RelationshipGraphComponent', `選單位置：x=${this.nodeMenuPosition.x}, y=${this.nodeMenuPosition.y}`);
  }

  private calculateMenuPosition(mouseX: number, mouseY: number): MenuPosition {
    const menuWidth = 180;
    const menuHeight = 200;
    const offset = 10;

    let x = mouseX + offset;
    let y = mouseY;

    // 避免超出視窗邊界
    if (x + menuWidth > window.innerWidth - offset) {
      x = mouseX - menuWidth - offset;
    }
    if (y + menuHeight > window.innerHeight - offset) {
      y = window.innerHeight - menuHeight - offset;
    }

    x = Math.max(offset, x);
    y = Math.max(offset, y);

    return { x, y };
  }

  // === 關係建立邏輯 ===
  private handleRelationshipNodeClick(node: GraphNode): void {
    const currentState = this.stateService.getCurrentState();
    
    if (!currentState.selectionState.firstSelectedNode) {
      this.stateService.setFirstSelectedNode(node);
    } else if (currentState.selectionState.firstSelectedNode.id !== node.id) {
      this.stateService.setSecondSelectedNode(node);
      this.stateService.showDialog('showRelationshipDialog');
    }
  }

  // === 合併邏輯 ===
  private handleMergeNodeClick(node: GraphNode): void {
    const currentState = this.stateService.getCurrentState();
    
    if (!currentState.selectionState.firstSelectedNodeForMerge) {
      this.stateService.setFirstSelectedNodeForMerge(node);
    } else if (currentState.selectionState.firstSelectedNodeForMerge.id !== node.id) {
      this.stateService.setSecondSelectedNodeForMerge(node);
      this.stateService.showDialog('showMergeConfirmDialog');
    }
  }

  // === 控制方法 ===
  zoomIn(): void {
    this.interactionService.zoomIn();
  }

  zoomOut(): void {
    this.interactionService.zoomOut();
  }

  resetView(): void {
    this.interactionService.resetView();
  }

  toggleDrag(): void {
    this.stateService.toggleDragging();
  }

  toggleFullscreen(): void {
    if (document.fullscreenElement) {
      document.exitFullscreen();
    } else {
      this.graphViewport?.nativeElement.requestFullscreen();
    }
  }

  toggleSearchPanel(): void {
    this.stateService.toggleSearchPanel();
  }

  // === 分析方法 ===
  performAnalysis(): void {
    if (!this.selectedPersonIds?.length) {
      this.stateService.setError('請選擇至少一個人員進行分析');
      return;
    }

    this.stateService.setLoading(true);
    this.stateService.clearError();

    this.relationshipGraphService.analyzeSelectedPersons(this.selectedPersonIds).subscribe({
      next: (data: any) => {
        this.graphData = data;
        this.updateGraph();
        this.stateService.setLoading(false);
        this.logService.info('關係圖譜分析完成', `節點數: ${data.nodes.length}`);
      },
      error: (error: any) => {
        this.stateService.setError('分析失敗：' + error.message);
        this.stateService.setLoading(false);
        this.logService.error('關係圖譜分析失敗', error.message || '未知錯誤');
      }
    });
  }

  retryAnalysis(): void {
    this.performAnalysis();
  }

  // === 對話框相關方法 ===
  getRelationshipDialogData(): RelationshipDialogData | null {
    const state = this.stateService.getCurrentState();
    if (state.selectionState.firstSelectedNode && state.selectionState.secondSelectedNode) {
      return {
        firstNode: state.selectionState.firstSelectedNode,
        secondNode: state.selectionState.secondSelectedNode
      };
    }
    return null;
  }

  getMergeDialogData(): MergeDialogData | null {
    const state = this.stateService.getCurrentState();
    if (state.selectionState.firstSelectedNodeForMerge && state.selectionState.secondSelectedNodeForMerge) {
      return {
        personA: state.selectionState.firstSelectedNodeForMerge,
        personB: state.selectionState.secondSelectedNodeForMerge
      };
    }
    return null;
  }

  // === 事件處理方法 ===
  onNodeMenuAction(action: NodeMenuAction): void {
    switch (action.type) {
      case 'view-data':
        this.viewPersonDetails(action.data.node);
        break;
      case 'create-relationship':
        this.startCreateRelationship();
        break;
      case 'org-chart':
        this.handleOrgChart(action.data.node);
        break;
      case 'merge':
        this.startMergeMode();
        break;
    }
  }

  confirmRelationshipCreation(data: RelationshipCreationData): void {
    this.isProcessingRelationship = true;
    
    // 呼叫 API 建立關係
    this.relationshipGraphService.createRelationship({
      sourcePersonId: data.sourcePersonId,
      targetPersonId: data.targetPersonId,
      relationshipType: data.relationshipType,
      visualAnalysisGraphId: this.visualAnalysisGraphId
    }).subscribe({
      next: (response) => {
        this.interactionService.addNewRelationship(
          data.sourcePersonId.toString(),
          data.targetPersonId.toString(),
          data.relationshipType
        );
        
        this.isProcessingRelationship = false;
        this.closeRelationshipDialog();
        this.cancelCurrentMode();
        this.relationshipCreated.emit();
        
        this.logService.info('關係建立成功', response.message);
      },
      error: (error) => {
        this.isProcessingRelationship = false;
        this.stateService.setError('建立關係失敗：' + error.message);
        this.logService.error('建立關係失敗', error.message || '未知錯誤');
      }
    });
  }

  confirmMerge(selection: MergeSelection): void {
    this.isProcessingMerge = true;
    
    // 準備合併資料並顯示比較對話框
    this.stateService.updateSelectionState({
      mergePersonAId: selection.primaryPersonId,
      mergePersonBId: selection.secondaryPersonId
    });
    
    this.closeMergeDialog();
    this.stateService.showDialog('showPersonComparison');
    this.isProcessingMerge = false;
  }

  handleMergeComparison(mergeSelection: any): void {
    // 處理合併確認後的邏輯
    this.closePersonComparison();
    this.cancelCurrentMode();
    
    // 這裡可以加入實際的合併邏輯
    this.logService.info('人員合併完成', mergeSelection);
  }

  // === 模式控制 ===
  startCreateRelationship(): void {
    this.stateService.setGraphMode(GraphMode.CreateRelationship);
  }

  startMergeMode(): void {
    this.stateService.setGraphMode(GraphMode.Merge);
  }

  cancelCurrentMode(): void {
    this.stateService.setGraphMode(GraphMode.Normal);
  }

  getSelectedCount(): number {
    const state = this.stateService.getCurrentState();
    const mode = state.graphMode;
    
    switch (mode) {
      case 'create-relationship':
        return (state.selectionState.firstSelectedNode ? 1 : 0) + 
               (state.selectionState.secondSelectedNode ? 1 : 0);
      case 'merge':
        return (state.selectionState.firstSelectedNodeForMerge ? 1 : 0) + 
               (state.selectionState.secondSelectedNodeForMerge ? 1 : 0);
      default:
        return 0;
    }
  }

  // === 對話框關閉方法 ===
  closeNodeMenu(): void {
    this.stateService.closeDialog('showNodeMenu');
  }

  closeRelationshipDialog(): void {
    this.stateService.closeDialog('showRelationshipDialog');
  }

  closeMergeDialog(): void {
    this.stateService.closeDialog('showMergeConfirmDialog');
  }

  closePersonDetailDialog(): void {
    this.stateService.closeDialog('showDetailDialog');
  }

  closePersonComparison(): void {
    this.stateService.closeDialog('showPersonComparison');
  }

  viewPersonDetails(node: GraphNode): void {
    this.stateService.updateSelectionState({ selectedPersonIdForDetail: Number(node.id) });
    this.stateService.showDialog('showDetailDialog');
  }

  handleOrgChart(node: GraphNode): void {
    // 組織圖編輯邏輯
    this.logService.info('開啟組織圖編輯', `節點ID: ${node.id}`);
  }
}