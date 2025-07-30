// 通用關聯圖譜組件：接收外部數據並呈現互動式圖譜
// 主要功能：D3.js 圖譜渲染、互動控制、數據可視化

import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RelationshipGraphService, GraphData, GraphNode, GraphLink, CreateRelationshipResponse } from '../../services/relationship-graph.service';
import { LogService } from '../../services/log.service';
import { PhotoUtilsService } from '../../services/photo-utils.service';
import { ProjectService } from '../../services/project.service';
import { PersonDetailDialogComponent } from '../person-detail-dialog/person-detail-dialog.component';
import { PersonComparisonComponent } from '../person-comparison/person-comparison.component';
import * as d3 from 'd3';

@Component({
  selector: 'app-relationship-graph',
  standalone: true,
  imports: [CommonModule, FormsModule, PersonDetailDialogComponent, PersonComparisonComponent],
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
              <span class="stat-value">{{ statistics.familyLinks }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">朋友關係</span>
              <span class="stat-value">{{ statistics.friendLinks }}</span>
            </div>
            <div class="stat-item">
              <span class="stat-label">平均連接</span>
              <span class="stat-value">{{ statistics.averageConnections }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- 載入狀態 -->
      <div class="loading-container" *ngIf="loading">
        <div class="loading-spinner"></div>
        <p>正在分析關聯關係...</p>
      </div>

      <!-- 錯誤狀態 -->
      <div class="error-container" *ngIf="error">
        <div class="error-icon">⚠️</div>
        <p>{{ error }}</p>
        <button class="btn btn-primary" (click)="retryAnalysis()">重試</button>
      </div>

      <!-- 圖譜容器 -->
      <div class="graph-viewport" *ngIf="!loading && !error" #graphViewport>
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
              [class.active]="showSearchPanel"
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
            {{ isDragging ? '🔒' : '🔓' }}
          </button>
          <button class="control-btn" (click)="toggleFullscreen()" title="全螢幕">⛶</button>
        </div>
      </div>

      <!-- 人員詳情對話框 -->
      <div class="dialog-overlay" *ngIf="showDetailDialog" (click)="closeDetailDialog()">
        <div class="dialog-container" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h3>人員詳情</h3>
            <button class="close-btn" (click)="closeDetailDialog()">×</button>
          </div>
          <div class="dialog-content" *ngIf="selectedPerson">
            <div class="person-info">
              <div class="person-avatar">
                <div class="avatar-text">{{ selectedPerson.name.charAt(0) }}</div>
              </div>
              <div class="person-details">
                <h4>{{ selectedPerson.name }}</h4>
                <p>性別：{{ selectedPerson.gender === 'male' ? '男' : '女' }}</p>
                <p *ngIf="selectedPerson.data?.birthday">生日：{{ selectedPerson.data.birthday }}</p>
                <p *ngIf="selectedPerson.data?.mobile">電話：{{ selectedPerson.data.mobile }}</p>
              </div>
            </div>
            <div class="person-relationships" *ngIf="selectedPerson.data">
              <h5>關係資訊</h5>
              <div *ngIf="selectedPerson.data.familyRelationships">
                <h6>家族關係</h6>
                <p>{{ selectedPerson.data.familyRelationships }}</p>
              </div>
              <div *ngIf="selectedPerson.data.friends">
                <h6>朋友關係</h6>
                <p>{{ selectedPerson.data.friends }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 節點操作選單 -->
      <div class="node-menu-overlay" *ngIf="showNodeMenu" (click)="closeNodeMenu()">
        <div class="node-menu" 
             [style.left.px]="nodeMenuPosition.x" 
             [style.top.px]="nodeMenuPosition.y"
             [class.dragging]="isDraggingMenu"
             (click)="$event.stopPropagation()"
             (mousedown)="$event.stopPropagation()">
          <div class="node-menu-header" 
               (mousedown)="startMenuDrag($event)"
               (mousemove)="onMenuDrag($event)"
               (mouseup)="stopMenuDrag($event)"
               (mouseleave)="stopMenuDrag($event)">
            <h4>{{ selectedNodeForMenu?.name }}</h4>
            <button class="close-btn" (click)="closeNodeMenu()">×</button>
          </div>
          <div class="node-menu-content">
            <div class="node-menu-actions">
              <button class="btn btn-primary node-menu-btn" (click)="handleViewData()">
                <span class="btn-icon">👁️</span>
                <span class="btn-text">檢視資料</span>
              </button>
              <button class="btn btn-success node-menu-btn" (click)="handleCreateRelationship()">
                <span class="btn-icon">➕</span>
                <span class="btn-text">建立關係</span>
              </button>
              <button class="btn btn-info node-menu-btn" (click)="handleOrgChart()">
                <span class="btn-icon">🏢</span>
                <span class="btn-text">組織圖</span>
              </button>
              <button class="btn btn-warning node-menu-btn" (click)="handleMerge()">
                <span class="btn-icon">🔗</span>
                <span class="btn-text">合併</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- 人員詳細資料對話框 -->
      <app-person-detail-dialog
        [personId]="selectedPersonIdForDetail"
        [isVisible]="showPersonDetailDialog"
        (close)="closePersonDetailDialog()">
      </app-person-detail-dialog>

      <!-- 建立關係對話框 -->
      <div class="relationship-dialog-overlay" *ngIf="showRelationshipDialog" (click)="cancelRelationshipCreation()">
        <div class="relationship-dialog" (click)="$event.stopPropagation()">
          <div class="relationship-dialog-header">
            <h3>建立關係</h3>
            <button class="close-btn" (click)="cancelRelationshipCreation()">×</button>
          </div>
          <div class="relationship-dialog-content">
            <div class="relationship-info">
              <p><strong>{{ firstSelectedNode?.name }}</strong> 與 <strong>{{ secondSelectedNode?.name }}</strong></p>
            </div>
            <div class="relationship-form">
              <label for="relationshipType">關係類型：</label>
              <input 
                type="text" 
                id="relationshipType"
                [(ngModel)]="relationshipType" 
                placeholder="例如：父子、夫妻、朋友..."
                class="relationship-input"
                (keydown)="handleRelationshipInputKeydown($event)"
                #relationshipInput
              >
            </div>
            <div class="relationship-actions">
              <button class="btn btn-primary" (click)="confirmRelationshipCreation()" [disabled]="!relationshipType.trim()">
                確定建立
              </button>
              <button class="btn btn-secondary" (click)="cancelRelationshipCreation()">
                取消
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- 合併確認對話框 -->
      <div class="dialog-overlay" *ngIf="showMergeConfirmDialog" (click)="cancelMergeConfirm()">
        <div class="merge-confirm-dialog" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h3>🔗 確認合併操作</h3>
            <button class="close-btn" (click)="cancelMergeConfirm()">×</button>
          </div>
          
          <div class="merge-content">
            <div class="merge-preview">
              <div class="person-card person-a">
                <div class="person-photo-container">
                                          <img *ngIf="firstSelectedNodeForMerge?.data?.photo"
                             [src]="getPersonPhotoUrl(firstSelectedNodeForMerge?.data?.photo, firstSelectedNodeForMerge?.name, firstSelectedNodeForMerge?.data?.projectId)"
                       class="person-photo"
                       (error)="handlePhotoError($event, personAFallback)"
                       alt="人員A照片">
                                          <div #personAFallback class="person-fallback" [style.display]="!firstSelectedNodeForMerge?.data?.photo ? 'flex' : 'none'">
                    {{ generateFallbackText(firstSelectedNodeForMerge?.name) }}
                  </div>
                </div>
                <div class="person-details">
                  <h4>{{ firstSelectedNodeForMerge?.name || '人員A' }}</h4>
                  <p class="person-id">ID: {{ mergePersonAId }}</p>
                  <p class="person-info">{{ firstSelectedNodeForMerge?.gender || '性別未設定' }} | {{ firstSelectedNodeForMerge?.data?.birthday || '生日未設定' }}</p>
                </div>
              </div>
              
              <div class="merge-direction">
                <div class="merge-arrow">
                  <svg width="40" height="40" viewBox="0 0 40 40" fill="none">
                    <circle cx="20" cy="20" r="18" stroke="#ffc107" stroke-width="2" fill="rgba(255, 193, 7, 0.1)"/>
                    <path d="M12 20 L28 20 M20 12 L28 20 L20 28" stroke="#ffc107" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </div>
                <span class="merge-label">合併為</span>
              </div>
              
              <div class="person-card person-b">
                <div class="person-photo-container">
                                          <img *ngIf="secondSelectedNodeForMerge?.data?.photo"
                             [src]="getPersonPhotoUrl(secondSelectedNodeForMerge?.data?.photo, secondSelectedNodeForMerge?.name, secondSelectedNodeForMerge?.data?.projectId)"
                       class="person-photo"
                       (error)="handlePhotoError($event, personBFallback)"
                       alt="人員B照片">
                                          <div #personBFallback class="person-fallback" [style.display]="!secondSelectedNodeForMerge?.data?.photo ? 'flex' : 'none'">
                    {{ generateFallbackText(secondSelectedNodeForMerge?.name) }}
                  </div>
                </div>
                <div class="person-details">
                  <h4>{{ secondSelectedNodeForMerge?.name || '人員B' }}</h4>
                  <p class="person-id">ID: {{ mergePersonBId }}</p>
                  <p class="person-info">{{ secondSelectedNodeForMerge?.gender || '性別未設定' }} | {{ secondSelectedNodeForMerge?.data?.birthday || '生日未設定' }}</p>
                </div>
              </div>
            </div>
            
            <div class="merge-warning">
              <div class="warning-icon">⚠️</div>
              <div class="warning-content">
                <h5>重要提醒</h5>
                <p>合併操作將不可逆地將兩個人員資料合併為一筆記錄。原始資料將被保留在合併記錄中供查詢。</p>
              </div>
            </div>
          </div>
          
          <div class="dialog-actions">
            <button class="btn btn-secondary" (click)="cancelMergeConfirm()">
              <span class="btn-icon">✕</span>
              取消合併
            </button>
            <button class="btn btn-primary" (click)="confirmMergeAndProceed()">
              <span class="btn-icon">✓</span>
              確認合併
            </button>
          </div>
        </div>
      </div>

      <!-- 建立關係模式提示 -->
      <div class="relationship-mode-indicator" *ngIf="isCreatingRelationship">
        <div class="indicator-content">
          <span class="indicator-icon">🔗</span>
          <span class="indicator-text">建立關係模式：請選擇第二個節點</span>
          <button class="cancel-btn" (click)="cancelRelationshipCreation()">取消</button>
        </div>
      </div>

      <!-- 人員比較對話框 -->
      <app-person-comparison
        [personAId]="mergePersonAId"
        [personBId]="mergePersonBId"
        [isVisible]="showPersonComparison"
        (closeComparison)="closePersonComparison()"
        (confirmMerge)="handleMergeComparison($event)">
      </app-person-comparison>
    </div>
  `,
  styleUrls: ['./relationship-graph.component.scss']
})
export class RelationshipGraphComponent implements OnInit, OnChanges, AfterViewInit, OnDestroy {
  @ViewChild('graphContainer', { static: false }) graphContainer!: ElementRef;
  @ViewChild('graphViewport', { static: false }) graphViewport!: ElementRef;
  @ViewChild('relationshipInput', { static: false }) relationshipInput!: ElementRef;

  // 新增事件發射器
  @Output() onShowAllNodes = new EventEmitter<void>();

  // 搜尋面板狀態
  showSearchPanel = false;

  @Input() graphData?: GraphData;
  @Input() autoAnalyze: boolean = false;
  @Input() selectedPersonIds: number[] = [];
  @Input() visualAnalysisGraphId?: number; // 視覺化分析圖表ID，用於建立關係時傳遞

  @Output() relationshipCreated = new EventEmitter<void>(); // 關係建立成功事件

  private svg: any;
  private simulation: any;
  isDragging = true; // 預設啟用拖曳功能
  private hiddenNodes: Set<string> = new Set();

  loading = false;
  error = '';
  showDetailDialog = false;
  selectedPerson: GraphNode | null = null;
  statistics: any = {
    familyLinks: 0,
    friendLinks: 0,
    averageConnections: '0'
  };

  // 節點互動相關
  isDraggingNode = false;
  draggedNode: GraphNode | null = null;
  showNodeMenu = false;
  nodeMenuPosition = { x: 0, y: 0 };
  selectedNodeForMenu: GraphNode | null = null;

  // 選單拖動相關
  isDraggingMenu = false;
  menuDragOffset = { x: 0, y: 0 };

  // 人員詳細資料對話框相關
  showPersonDetailDialog = false;
  selectedPersonIdForDetail: number | null = null;

  // 建立關係相關
  isCreatingRelationship = false;
  firstSelectedNode: GraphNode | null = null;
  showRelationshipDialog = false;
  relationshipType = '';
  secondSelectedNode: GraphNode | null = null;

  // 合併功能相關狀態
  isMerging = false;
  firstSelectedNodeForMerge: GraphNode | null = null;
  secondSelectedNodeForMerge: GraphNode | null = null;
  showMergeConfirmDialog = false; // 合併確認對話框
  showPersonComparison = false; // 人員比較對話框
  mergePersonAId: number | null = null;
  mergePersonBId: number | null = null;

  constructor(
    private relationshipGraphService: RelationshipGraphService,
    private cdr: ChangeDetectorRef,
    private logService: LogService,
    private photoUtils: PhotoUtilsService,
    private projectService: ProjectService
  ) {
    this.logService.info('RelationshipGraphComponent', '組件已初始化');
  }

  ngOnChanges(changes: SimpleChanges): void {
    this.logService.info('RelationshipGraphComponent', 'ngOnChanges 被調用', {
      changes: Object.keys(changes),
      selectedPersonIds: this.selectedPersonIds,
      currentGraphDataLinksCount: this.graphData?.metadata?.totalLinks || 0
    });

    // 檢查 graphData 是否被外部重置，這可能導致新建立的關聯線丟失
    if (changes['graphData'] && !changes['graphData'].firstChange) {
      const oldData = changes['graphData'].previousValue;
      const newData = changes['graphData'].currentValue;
      
      this.logService.warn('RelationshipGraphComponent', 'graphData 被外部更改，可能導致新建立的關聯線丟失', {
        oldLinksCount: oldData?.metadata?.totalLinks || 0,
        newLinksCount: newData?.metadata?.totalLinks || 0,
        oldLinks: oldData?.links?.map((l: any) => ({ source: l.source, target: l.target, type: l.type })) || [],
        newLinks: newData?.links?.map((l: any) => ({ source: l.source, target: l.target, type: l.type })) || []
      });
      
      // 當 graphData 被外部更改時，更新統計並重新初始化圖譜
      if (newData) {
        this.updateStatistics();
        
        // 如果容器已經準備好，重新初始化圖譜
        setTimeout(() => {
          if (this.graphContainer && this.graphData) {
            this.logService.info('RelationshipGraphComponent', '因 graphData 外部更改而重新初始化圖譜');
            this.initGraph();
          }
        }, 100);
      }
    }

    // 當 selectedPersonIds 變更且組件已經初始化時，重新執行分析
    if (changes['selectedPersonIds'] && !changes['selectedPersonIds'].firstChange) {
      this.logService.info('RelationshipGraphComponent', 'selectedPersonIds 已變更，重新執行分析');
      if (this.autoAnalyze) {
        this.performAnalysis();
      }
    }
  }

  ngOnInit(): void {
    this.logService.info('RelationshipGraphComponent', 'ngOnInit 被調用', {
      autoAnalyze: this.autoAnalyze,
      hasGraphData: !!this.graphData,
      selectedPersonIds: this.selectedPersonIds
    });

    if (this.autoAnalyze) {
      this.logService.info('RelationshipGraphComponent', '開始自動分析');
      this.performAnalysis();
    } else if (this.graphData) {
      this.logService.info('RelationshipGraphComponent', '使用現有圖譜數據');
      this.updateStatistics();
    } else {
      this.logService.warn('RelationshipGraphComponent', '沒有圖譜數據且未啟用自動分析');
    }
  }

  ngAfterViewInit(): void {
    this.logService.info('RelationshipGraphComponent', 'ngAfterViewInit 被調用');
    
    // 檢查容器是否可用
    if (!this.graphContainer) {
      this.logService.warn('RelationshipGraphComponent', '圖譜容器尚未準備好，將在數據載入後重試');
      return;
    }
    
    // 如果已經有數據，立即初始化
    if (this.graphData) {
      this.logService.info('RelationshipGraphComponent', '有圖譜數據，開始初始化圖譜');
      this.initGraph();
    } else if (this.autoAnalyze) {
      this.logService.info('RelationshipGraphComponent', '啟用自動分析，等待分析完成後初始化');
      // 自動分析會在 performAnalysis 中處理
    } else {
      this.logService.warn('RelationshipGraphComponent', '沒有圖譜數據且未啟用自動分析');
    }
  }

  ngOnDestroy(): void {
    if (this.simulation) {
      this.simulation.stop();
    }
  }

  /**
   * 執行關聯分析
   */
  performAnalysis(): void {
    this.logService.info('RelationshipGraphComponent', '開始執行關聯分析', {
      selectedPersonIds: this.selectedPersonIds,
      analysisType: this.selectedPersonIds.length > 0 ? 'selected' : 'all'
    });

    this.loading = true;
    this.error = '';

    const request = this.selectedPersonIds.length > 0 ? 
      this.relationshipGraphService.analyzeSelectedPersons(this.selectedPersonIds) :
      this.relationshipGraphService.analyzeAllPersons();

    request.subscribe({
      next: (response) => {
        this.logService.info('RelationshipGraphComponent', '關聯分析響應', {
          success: response.success,
          message: response.message,
          hasData: !!response.data,
          dataNodes: response.data?.nodes?.length || 0,
          dataLinks: response.data?.metadata?.totalLinks || 0
        });

        if (response.success && response.data) {
          this.graphData = response.data;
          this.updateStatistics();
          
          // 確保容器已經準備好後再初始化圖譜
          setTimeout(() => {
            if (this.graphContainer) {
              this.logService.info('RelationshipGraphComponent', '容器已準備好，開始初始化圖譜');
              this.initGraph();
            } else {
              this.logService.error('RelationshipGraphComponent', '容器仍然不可用，無法初始化圖譜');
              // 強制觸發變更檢測，然後重試
              this.cdr.detectChanges();
              setTimeout(() => {
                if (this.graphContainer) {
                  this.logService.info('RelationshipGraphComponent', '重試後容器已準備好，開始初始化圖譜');
                  this.initGraph();
                } else {
                  this.logService.error('RelationshipGraphComponent', '重試後容器仍然不可用');
                }
              }, 200);
            }
          }, 100); // 等待 100ms 確保 DOM 已渲染
        } else {
          this.error = response.message || '分析失敗';
          this.logService.error('RelationshipGraphComponent', '分析失敗', {
            message: this.error
          });
        }
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.logService.error('RelationshipGraphComponent', '關聯分析請求失敗', error);
        this.error = '分析失敗，請稍後再試';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * 重試分析
   */
  retryAnalysis(): void {
    this.performAnalysis();
  }

  /**
   * 更新統計信息
   */
  updateStatistics(): void {
    if (this.graphData) {
      this.statistics = this.relationshipGraphService.getGraphStatistics(this.graphData);
    }
  }

  /**
   * 初始化圖譜
   */
  private initGraph(): void {
    this.logService.time('initGraph');
    
    if (!this.graphContainer || !this.graphData) {
      this.logService.error('RelationshipGraphComponent', '無法初始化圖譜：缺少容器或數據', {
        hasContainer: !!this.graphContainer,
        hasGraphData: !!this.graphData
      });
      return;
    }

    const container = this.graphContainer.nativeElement;
    const width = container.clientWidth || 800;
    const height = container.clientHeight || 600;

    this.logService.info('RelationshipGraphComponent', '初始化圖譜', {
      containerSize: { width, height },
      graphData: this.graphData,
      nodesCount: this.graphData.nodes.length,
      linksCount: this.graphData.metadata?.totalLinks || 0
    });

    // 清除現有內容
    d3.select(container).selectAll('*').remove();

    // 創建 SVG
    this.svg = d3.select(container)
      .append('svg')
      .attr('width', width)
      .attr('height', height)
      .style('background', '#1a1a1a');

    // 創建縮放行為
    const zoom = d3.zoom()
      .scaleExtent([0.1, 4])
      .on('zoom', (event) => {
        this.svg.select('.graph-group')
          .attr('transform', event.transform);
      });

    this.svg.call(zoom);

    // 創建圖形群組
    const graphGroup = this.svg.append('g')
      .attr('class', 'graph-group');

    // 為節點設置初始位置
    const nodes = this.graphData.nodes;
    this.logService.info('RelationshipGraphComponent', '設置節點初始位置', {
      nodesCount: nodes.length,
      containerSize: { width, height }
    });
    
    nodes.forEach((node, index) => {
      const angle = (index / nodes.length) * 2 * Math.PI;
      const radius = Math.min(width, height) * 0.3; // 恢復原本的初始佈局半徑 0.3
      node.x = width / 2 + Math.cos(angle) * radius;
      node.y = height / 2 + Math.sin(angle) * radius;
      
      // 移除節點位置設置的debug日誌以提高性能
      // this.logService.debug('RelationshipGraphComponent', `節點 ${node.name} 位置設置`, { ... });
    });

    // 創建力導向模擬 - 適中的力道讓佈局平衡
    this.simulation = d3.forceSimulation()
      .force('link', d3.forceLink().id((d: any) => d.id).distance(100)) // 保持連線距離 100
      .force('charge', d3.forceManyBody().strength(-20)) // 適中的排斥力 -20
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide().radius(22)); // 適中的碰撞半徑 22

    // 即使沒有連線也保持力導向模擬運行，提供節點互動和防重疊效果
    if (this.graphData.metadata?.totalLinks === 0) {
      this.logService.info('RelationshipGraphComponent', '沒有連線，但保持力導向模擬以提供節點互動');
      // 不停止模擬，讓節點排斥力、碰撞檢測和拖拽效果正常工作
    }

    this.updateGraph();
    this.logService.timeEnd('initGraph');
  }

  /**
   * 更新圖譜
   */
  private updateGraph(): void {
    this.logService.time('updateGraph');
    
    if (!this.svg || !this.graphData) {
      this.logService.error('RelationshipGraphComponent', 'updateGraph: 缺少 SVG 或圖譜數據', {
        hasSvg: !!this.svg,
        hasGraphData: !!this.graphData
      });
      return;
    }

    const graphGroup = this.svg.select('.graph-group');
    const width = this.svg.node().getBoundingClientRect().width;
    const height = this.svg.node().getBoundingClientRect().height;
    const visibleNodes = this.graphData.nodes.filter(node => !this.hiddenNodes.has(node.id));
    const visibleLinks = this.graphData.links.filter(link => {
      // 處理D3可能已將source/target轉換為節點對象的情況
      let sourceId: string;
      let targetId: string;
      
      if (typeof link.source === 'object' && link.source !== null && 'id' in link.source) {
        sourceId = String((link.source as any).id);
      } else {
        sourceId = String(link.source);
      }
      
      if (typeof link.target === 'object' && link.target !== null && 'id' in link.target) {
        targetId = String((link.target as any).id);
      } else {
        targetId = String(link.target);
      }
      
      const sourceVisible = !this.hiddenNodes.has(sourceId);
      const targetVisible = !this.hiddenNodes.has(targetId);
      
      // 檢查節點存在性
      const sourceExists = this.graphData!.nodes.some(node => 
        String(node.id) === sourceId
      );
      const targetExists = this.graphData!.nodes.some(node => 
        String(node.id) === targetId
      );
      
      const isVisible = sourceVisible && targetVisible && sourceExists && targetExists;
      
      // 移除重複的連線可見性檢查日誌以提高性能
      // this.logService.info('RelationshipGraphComponent', 'updateGraph連線可見性檢查', { ... });
      
      return isVisible;
    });

    this.logService.info('RelationshipGraphComponent', '更新圖譜', {
      visibleNodesCount: visibleNodes.length,
      visibleLinksCount: visibleLinks.length,
      totalNodes: this.graphData.nodes.length,
      totalLinks: this.graphData.metadata?.totalLinks || 0
    });

    // 更新連線，使用唯一鍵值函數確保D3正確識別現有連線
    const link = graphGroup.selectAll('.link')
      .data(visibleLinks, (d: any) => {
        const sourceId = typeof d.source === 'object' ? d.source.id : d.source;
        const targetId = typeof d.target === 'object' ? d.target.id : d.target;
        return `${sourceId}-${targetId}-${d.type}`;
      })
      .join('line')
      .attr('class', 'link')
      .style('stroke', (d: any) => d.isFamily ? '#ff6b35' : '#666')
      .style('stroke-width', 2)
      .style('opacity', 0.6);

    // 更新連線標籤，使用相同的唯一鍵值函數
    const linkLabel = graphGroup.selectAll('.link-label')
      .data(visibleLinks, (d: any) => {
        const sourceId = typeof d.source === 'object' ? d.source.id : d.source;
        const targetId = typeof d.target === 'object' ? d.target.id : d.target;
        return `${sourceId}-${targetId}-${d.type}`;
      })
      .join('text')
      .attr('class', 'link-label')
      .style('text-anchor', 'middle')
      .style('font-size', '10px')
      .style('fill', '#e0e0e0')
      .style('pointer-events', 'none')
      .style('font-weight', 'bold')
      .text((d: any) => d.type || '關係');

    // 更新節點
    const node = graphGroup.selectAll('.node')
      .data(visibleNodes)
      .join('g')
      .attr('class', 'node graph-node')
      .attr('data-node-id', (d: any) => d.id)
      .call(this.dragBehavior());

    // 節點圓圈
    node.selectAll('.node-circle')
      .data((d: any) => [d])
      .join('circle')
      .attr('class', 'node-circle')
      .attr('r', 25) // 恢復原本的節點圓圈半徑 25
      .style('fill', (d: any) => {
        if (d.gender === 'male') {
          return '#42A5F5';
        } else {
          return '#F48FB1';
        }
      })
      .style('stroke', (d: any) => {
        // 如果節點被選中，使用高亮邊框
        if (this.selectedNodeForMenu && this.selectedNodeForMenu.id === d.id) {
          return '#FFD700'; // 金色高亮
        }
        return '#fff';
      })
      .style('stroke-width', (d: any) => {
        // 如果節點被選中，使用更粗的邊框
        if (this.selectedNodeForMenu && this.selectedNodeForMenu.id === d.id) {
          return 5;
        }
        return 3;
      })
      .style('cursor', 'pointer')
      .on('click', (event: any, d: any) => {
        this.handleNodeClick(event, d);
      });

    // 節點照片或頭像（新增功能）
    const self = this;
    node.selectAll('.node-image')
      .data((d: any) => [d])
      .join((enter: any) => {
        const imageGroup = enter.append('g').attr('class', 'node-image');
        
        // 首先添加預設圖標
        imageGroup.append('text')
          .attr('class', 'node-icon-default')
          .attr('dy', '0.35em')
          .style('text-anchor', 'middle')
          .style('font-size', '16px')
          .style('fill', '#fff')
          .style('pointer-events', 'none')
          .text('👤');
        
        return imageGroup;
      })
      .each((d: any, i: number, nodes: any[]) => {
        const imageElement = d3.select(nodes[i]);
        
        // 統一使用PhotoUtilsService處理照片
        this.loadNodePhoto(d, imageElement, self);
      });

    // 節點名稱
    node.selectAll('.node-label')
      .data((d: any) => [d])
      .join('text')
      .attr('class', 'node-label')
      .attr('dy', 35)
      .style('text-anchor', 'middle')
      .style('font-size', '12px')
      .style('fill', '#e0e0e0')
      .style('pointer-events', 'none')
      .text((d: any) => d.name);

    // 更新力導向模擬 - 無論是否有連線都使用模擬以提供節點互動
    this.simulation
      .nodes(visibleNodes)
      .on('tick', () => {
        // 動態獲取當前的連線，而不是使用方法開始時的快照
        const currentLinks = this.svg?.select('.graph-group').selectAll('.link');
        const currentLinkLabels = this.svg?.select('.graph-group').selectAll('.link-label');
        
        if (currentLinks && !currentLinks.empty()) {
          // 臨時調試：檢查tick事件中的連線處理
          const tickDebugCount = (this as any)._tickDebugCount || 0;
          if (tickDebugCount < 3) { // 只記錄前3次tick
            this.logService.info('RelationshipGraphComponent', `tick事件 #${tickDebugCount}`, {
              currentLinksCount: currentLinks.size(),
              visibleLinksLength: visibleLinks.length
            });
            (this as any)._tickDebugCount = tickDebugCount + 1;
          }
          
          currentLinks
            .attr('x1', (d: any) => {
              // 檢查source是否為節點對象，如果不是則查找對應節點
              if (typeof d.source === 'object' && d.source.x !== undefined) {
                return d.source.x;
              } else {
                const sourceNode = this.graphData?.nodes.find(n => String(n.id) === String(d.source));
                if (!sourceNode) {
                  this.logService.warn('RelationshipGraphComponent', 'tick事件中找不到source節點', {
                    linkSource: d.source,
                    linkType: d.type,
                    availableNodeIds: this.graphData?.nodes.map(n => n.id)
                  });
                }
                return sourceNode ? sourceNode.x : 0;
              }
            })
            .attr('y1', (d: any) => {
              if (typeof d.source === 'object' && d.source.y !== undefined) {
                return d.source.y;
              } else {
                const sourceNode = this.graphData?.nodes.find(n => String(n.id) === String(d.source));
                return sourceNode ? sourceNode.y : 0;
              }
            })
            .attr('x2', (d: any) => {
              if (typeof d.target === 'object' && d.target.x !== undefined) {
                return d.target.x;
              } else {
                const targetNode = this.graphData?.nodes.find(n => String(n.id) === String(d.target));
                return targetNode ? targetNode.x : 0;
              }
            })
            .attr('y2', (d: any) => {
              if (typeof d.target === 'object' && d.target.y !== undefined) {
                return d.target.y;
              } else {
                const targetNode = this.graphData?.nodes.find(n => String(n.id) === String(d.target));
                return targetNode ? targetNode.y : 0;
              }
            });

          // 更新連線標籤位置
          if (currentLinkLabels && !currentLinkLabels.empty()) {
            currentLinkLabels
              .attr('x', (d: any) => {
                const sourceX = typeof d.source === 'object' && d.source.x !== undefined ? 
                  d.source.x : (this.graphData?.nodes.find(n => String(n.id) === String(d.source))?.x || 0);
                const targetX = typeof d.target === 'object' && d.target.x !== undefined ? 
                  d.target.x : (this.graphData?.nodes.find(n => String(n.id) === String(d.target))?.x || 0);
                return (sourceX + targetX) / 2;
              })
              .attr('y', (d: any) => {
                const sourceY = typeof d.source === 'object' && d.source.y !== undefined ? 
                  d.source.y : (this.graphData?.nodes.find(n => String(n.id) === String(d.source))?.y || 0);
                const targetY = typeof d.target === 'object' && d.target.y !== undefined ? 
                  d.target.y : (this.graphData?.nodes.find(n => String(n.id) === String(d.target))?.y || 0);
                return (sourceY + targetY) / 2;
              });
          }
        }

        // 更新節點位置
        const currentNodes = this.svg?.select('.graph-group').selectAll('.node');
        if (currentNodes && !currentNodes.empty()) {
          currentNodes.attr('transform', (d: any) => `translate(${d.x},${d.y})`);
        }
      });

    // 設置連線力
    if (visibleLinks.length > 0) {
      // 有連線時設置連線力
      this.simulation.force('link').links(visibleLinks);
      this.logService.info('RelationshipGraphComponent', '啟用力導向模擬（含連線力）', {
        nodesCount: visibleNodes.length,
        linksCount: visibleLinks.length
      });
    } else {
      // 沒有連線時移除連線力，但保持其他力（排斥、碰撞、中心力）
      this.simulation.force('link').links([]);
      this.logService.info('RelationshipGraphComponent', '啟用力導向模擬（無連線力，僅節點互動力）', {
        nodesCount: visibleNodes.length
      });
    }

    this.logService.info('RelationshipGraphComponent', '圖譜更新完成', {
      nodesCreated: visibleNodes.length,
      linksCreated: visibleLinks.length,
      simulationActive: !!this.simulation
    });
    
    this.logService.timeEnd('updateGraph');
  }

  /**
   * 拖曳行為
   */
  private dragBehavior(): any {
    return d3.drag()
      .on('start', (event: any, d: any) => {
        // 如果沒有啟用拖曳模式，則不允許拖曳
        if (!this.isDragging) return;
        
        // 阻止事件冒泡，避免觸發點擊事件
        event.sourceEvent.stopPropagation();
        
        this.logService.debug('RelationshipGraphComponent', '開始拖曳節點', { nodeName: d.name });
        
        // 設置節點固定位置
        d.fx = d.x;
        d.fy = d.y;
        
        // 重新啟動模擬
        if (this.simulation) {
          this.simulation.alphaTarget(0.3).restart();
        }
      })
      .on('drag', (event: any, d: any) => {
        // 如果沒有啟用拖曳模式，則不允許拖曳
        if (!this.isDragging) return;
        
        // 阻止事件冒泡
        event.sourceEvent.stopPropagation();
        
        // 更新節點位置到滑鼠位置
        d.fx = event.x;
        d.fy = event.y;
      })
      .on('end', (event: any, d: any) => {
        // 如果沒有啟用拖曳模式，則不允許拖曳
        if (!this.isDragging) return;
        
        // 阻止事件冒泡
        event.sourceEvent.stopPropagation();
        
        this.logService.debug('RelationshipGraphComponent', '結束拖曳節點', { 
          nodeName: d.name, 
          finalPosition: { x: d.fx, y: d.fy } 
        });
        
        // 釋放節點固定位置
        d.fx = null;
        d.fy = null;
        
        // 停止模擬
        if (this.simulation) {
          this.simulation.alphaTarget(0);
        }
      });
  }

  /**
   * 查看人員詳情
   */
  viewPersonDetails(node: GraphNode): void {
    this.selectedPerson = node;
    this.showDetailDialog = true;
  }

  /**
   * 關閉詳情對話框
   */
  closeDetailDialog(): void {
    this.showDetailDialog = false;
    this.selectedPerson = null;
  }

  /**
   * 控制功能
   */
  zoomIn(): void {
    this.svg.transition().duration(300).call(
      d3.zoom().scaleBy,
      1.3
    );
  }

  zoomOut(): void {
    this.svg.transition().duration(300).call(
      d3.zoom().scaleBy,
      1 / 1.3
    );
  }

  resetView(): void {
    this.svg.transition().duration(750).call(
      d3.zoom().transform,
      d3.zoomIdentity
    );
  }

  toggleDrag(): void {
    this.isDragging = !this.isDragging;
  }

  toggleFullscreen(): void {
    const element = this.graphViewport.nativeElement;
    if (document.fullscreenElement) {
      document.exitFullscreen();
    } else {
      element.requestFullscreen();
    }
  }

  toggleSearchPanel(): void {
    this.showSearchPanel = !this.showSearchPanel;
  }

  /**
   * 處理節點點擊
   */
  handleNodeClick(event: MouseEvent, node: GraphNode): void {
    event.stopPropagation();
    this.logService.info('RelationshipGraphComponent', '節點被點擊', { 
      nodeName: node.name,
      isCreatingRelationship: this.isCreatingRelationship,
      isMerging: this.isMerging
    });
    
    // 如果在建立關係模式下，處理關係建立邏輯
    if (this.isCreatingRelationship) {
      this.handleRelationshipNodeClick(node);
      return;
    }

    // 如果在合併模式下，處理合併邏輯
    if (this.isMerging) {
      this.handleMergeNodeClick(node);
      return;
    }
    
    // 正常模式：顯示節點選單
    this.selectedNodeForMenu = node;
    this.showNodeMenu = true;
    this.nodeMenuPosition = { x: event.clientX, y: event.clientY };
    
    // 更新圖譜以顯示高亮效果
    this.updateNodeHighlight();
    
    // 添加全域點擊監聽器來關閉選單
    setTimeout(() => {
      document.addEventListener('click', this.handleGlobalClick.bind(this), { once: true });
    }, 0);
  }

  /**
   * 處理全域點擊事件
   */
  private handleGlobalClick(event: MouseEvent): void {
    if (this.showNodeMenu) {
      this.closeNodeMenu();
    }
  }

  /**
   * 關閉節點選單
   */
  closeNodeMenu(): void {
    this.showNodeMenu = false;
    this.selectedNodeForMenu = null;
    
    // 更新圖譜以移除高亮效果
    this.updateNodeHighlight();
    
    // 移除全域點擊監聽器
    document.removeEventListener('click', this.handleGlobalClick.bind(this));
  }

  /**
   * 更新節點高亮效果
   */
  private updateNodeHighlight(): void {
    if (!this.svg) return;
    
    this.logService.debug('RelationshipGraphComponent', '更新節點高亮效果', {
      selectedNode: this.selectedNodeForMenu?.name
    });
    
    // 更新所有節點的邊框樣式
    this.svg.selectAll('.node-circle')
      .style('stroke', (d: any) => {
        if (this.selectedNodeForMenu && this.selectedNodeForMenu.id === d.id) {
          return '#FFD700'; // 金色高亮
        }
        return '#fff';
      })
      .style('stroke-width', (d: any) => {
        if (this.selectedNodeForMenu && this.selectedNodeForMenu.id === d.id) {
          return 5;
        }
        return 3;
      });
  }

  /**
   * 處理檢視資料功能
   */
  handleViewData(): void {
    if (this.selectedNodeForMenu) {
      this.logService.info('RelationshipGraphComponent', '開啟人員詳細資料', {
        nodeName: this.selectedNodeForMenu.name,
        nodeId: this.selectedNodeForMenu.id
      });
      
      // 設置要顯示的人員ID
      this.selectedPersonIdForDetail = parseInt(this.selectedNodeForMenu.id);
      this.showPersonDetailDialog = true;
    }
    this.closeNodeMenu();
  }

  /**
   * 處理建立關係功能
   */
  handleCreateRelationship(): void {
    if (this.selectedNodeForMenu) {
      this.logService.info('RelationshipGraphComponent', '開始建立關係模式', {
        firstNodeName: this.selectedNodeForMenu.name,
        firstNodeId: this.selectedNodeForMenu.id
      });
      
      // 設置第一個選中的節點
      this.firstSelectedNode = this.selectedNodeForMenu;
      this.isCreatingRelationship = true;
      
      // 關閉選單
      this.closeNodeMenu();
      
      // 更新圖譜以顯示第一個節點為半透明
      this.updateNodeStates();
    }
  }

  /**
   * 處理組織圖功能
   */
  handleOrgChart(): void {
    if (!this.selectedNodeForMenu) return;

    this.logService.info('RelationshipGraphComponent', '開啟組織圖', {
      person: this.selectedNodeForMenu.name,
      personId: this.selectedNodeForMenu.id
    });

    // TODO: 實現組織圖功能
    alert('組織圖功能開發中...');
    this.closeNodeMenu();
  }

  /**
   * 處理合併功能
   */
  handleMerge(): void {
    if (!this.selectedNodeForMenu) return;

    this.logService.info('RelationshipGraphComponent', '🔀 開始合併流程', {
      person: this.selectedNodeForMenu.name
    });

    // 進入合併模式
    this.isMerging = true;
    this.firstSelectedNodeForMerge = this.selectedNodeForMenu;
    this.closeNodeMenu();

    // 更新節點狀態
    this.updateNodeStates();
    this.cdr.detectChanges();
  }

  /**
   * 關閉人員詳細資料對話框
   */
  closePersonDetailDialog(): void {
    this.showPersonDetailDialog = false;
    this.selectedPersonIdForDetail = null;
    this.logService.info('RelationshipGraphComponent', '關閉人員詳細資料對話框');
  }

  /**
   * 取消合併確認對話框
   */
  cancelMergeConfirm(): void {
    this.showMergeConfirmDialog = false;
    this.resetMergeState();
  }

  /**
   * 確認合併並進入比較對話框
   */
  confirmMergeAndProceed(): void {
    this.logService.info('RelationshipGraphComponent', '🔄 開始比較資料', {
      personA: this.firstSelectedNodeForMerge?.name,
      personB: this.secondSelectedNodeForMerge?.name
    });
    
    // 關閉合併確認對話框，顯示人員比較對話框
    this.showMergeConfirmDialog = false;
    this.showPersonComparison = true;
    this.cdr.detectChanges();
  }

  /**
   * 關閉人員比較對話框
   */
  closePersonComparison(): void {
    this.showPersonComparison = false;
    this.resetMergeState();
  }

  /**
   * 處理人員比較結果，執行實際合併
   */
  handleMergeComparison(mergeSelection: any): void {
    this.logService.info('RelationshipGraphComponent', '✅ 合併資料完成', {
      personA: mergeSelection.personAId,
      personB: mergeSelection.personBId,
      mergedFieldsCount: Object.keys(mergeSelection.mergedData || {}).length
    });
    
    // 關閉人員比較對話框
    this.showPersonComparison = false;
    
    // TODO: 這裡需要調用後端API執行實際的合併操作
    this.logService.warn('RelationshipGraphComponent', '⚠️ 後端合併API尚未實現');
    
    // 暫時顯示成功訊息
    alert(`合併操作已準備完成，但後端API尚未實現。\n\n合併資料預覽：\n${JSON.stringify(mergeSelection.mergedData, null, 2)}`);
    
    this.resetMergeState();
  }

  /**
   * 重置合併狀態
   */
  private resetMergeState(): void {
    this.isMerging = false;
    this.firstSelectedNodeForMerge = null;
    this.secondSelectedNodeForMerge = null;
    this.showMergeConfirmDialog = false;
    this.showPersonComparison = false;
    this.mergePersonAId = null;
    this.mergePersonBId = null;
    
    // 更新節點狀態
    this.updateNodeStates();
    this.cdr.detectChanges();
  }

  /**
   * 取得人員照片URL
   */
  getPersonPhotoUrl(photoIndex: string | null | undefined, personName?: string, projectId?: string): string | null {
    const originalProjectId = projectId;
    // 如果沒有提供專案ID，嘗試從當前專案或節點資料中取得
    if (!projectId) {
      const currentProject = this.projectService?.getCurrentProject();
      projectId = currentProject?.id;
    }

    this.logService.debug('RelationshipGraphComponent', '取得人員照片URL', {
      photoIndex: photoIndex,
      personName: personName,
      projectId: projectId,
      hasPhotoUtils: !!this.photoUtils,
      hasProjectService: !!this.projectService
    });
    
    return this.photoUtils.getPersonPhotoUrl(photoIndex, personName, projectId);
  }

  /**
   * 生成預設頭像文字
   */
  generateFallbackText(personName?: string): string {
    return this.photoUtils.generateFallbackText(personName);
  }

  /**
   * 處理照片載入錯誤
   */
  handlePhotoError(event: Event, fallbackElement?: HTMLElement): void {
    this.photoUtils.handlePhotoError(event, fallbackElement);
  }

  /**
   * 開始拖動選單
   */
  startMenuDrag(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    
    this.isDraggingMenu = true;
    this.menuDragOffset = {
      x: event.clientX - this.nodeMenuPosition.x,
      y: event.clientY - this.nodeMenuPosition.y
    };
    
    this.logService.debug('RelationshipGraphComponent', '開始拖動選單', {
      mousePosition: { x: event.clientX, y: event.clientY },
      menuPosition: this.nodeMenuPosition,
      dragOffset: this.menuDragOffset
    });
  }

  /**
   * 拖動選單中
   */
  onMenuDrag(event: MouseEvent): void {
    if (!this.isDraggingMenu) return;
    
    event.preventDefault();
    event.stopPropagation();
    
    this.nodeMenuPosition = {
      x: event.clientX - this.menuDragOffset.x,
      y: event.clientY - this.menuDragOffset.y
    };
    
    this.logService.debug('RelationshipGraphComponent', '拖動選單中', {
      mousePosition: { x: event.clientX, y: event.clientY },
      menuPosition: this.nodeMenuPosition
    });
  }

  /**
   * 停止拖動選單
   */
  stopMenuDrag(event: MouseEvent): void {
    if (!this.isDraggingMenu) return;
    
    this.isDraggingMenu = false;
    
    this.logService.debug('RelationshipGraphComponent', '停止拖動選單', {
      finalPosition: this.nodeMenuPosition
    });
  }

  /**
   * 處理合併模式下的節點點擊
   */
  private handleMergeNodeClick(node: GraphNode): void {
    if (!this.isMerging || !this.firstSelectedNodeForMerge) return;
    
    // 不能選擇同一個節點
    if (node.id === this.firstSelectedNodeForMerge.id) {
      this.logService.warn('RelationshipGraphComponent', '不能選擇同一個節點進行合併');
      return;
    }
    
    this.logService.info('RelationshipGraphComponent', '選擇第二個節點進行合併', {
      firstNodeName: this.firstSelectedNodeForMerge.name,
      firstNodeId: this.firstSelectedNodeForMerge.id,
      firstNodePhotoIndex: this.firstSelectedNodeForMerge.data?.photo_index,
      firstNodeProjectId: this.firstSelectedNodeForMerge.data?.project_id,
      firstNodeData: this.firstSelectedNodeForMerge.data,
      secondNodeName: node.name,
      secondNodeId: node.id,
      secondNodePhotoIndex: node.data?.photo_index,
      secondNodeProjectId: node.data?.project_id,
      secondNodeData: node.data
    });
    
    // ===============================================================
    // MERGE DIALOG DEBUG LOG
    // ===============================================================
    this.logService.warn('RelationshipGraphComponent - MERGE DEBUG', JSON.stringify({
      timestamp: new Date().toISOString(),
      step: 'Preparing Merge Confirmation Dialog',
      personA: {
        name: this.firstSelectedNodeForMerge.name,
        id: this.firstSelectedNodeForMerge.id,
        photo_index_from_data: this.firstSelectedNodeForMerge.data?.photo_index,
        project_id_from_data: this.firstSelectedNodeForMerge.data?.project_id,
        person_photo_from_data: this.firstSelectedNodeForMerge.data?.PersonPhoto, // For VisualAnalysisController
        raw_data: this.firstSelectedNodeForMerge.data,
        raw_node: this.firstSelectedNodeForMerge
      },
      personB: {
        name: node.name,
        id: node.id,
        photo_index_from_data: node.data?.photo_index,
        project_id_from_data: node.data?.project_id,
        person_photo_from_data: node.data?.PersonPhoto, // For VisualAnalysisController
        raw_data: node.data,
        raw_node: node
      }
    }));
    // ===============================================================
    
    // 設置合併確認對話框的參數
    this.secondSelectedNodeForMerge = node;
    this.mergePersonAId = parseInt(this.firstSelectedNodeForMerge.id);
    this.mergePersonBId = parseInt(node.id);
    this.showMergeConfirmDialog = true; // 先顯示確認對話框
    this.cdr.detectChanges();
  }

  /**
   * 更新節點狀態（建立關係模式和合併模式）
   */
  private updateNodeStates(): void {
    if (!this.svg) return;
    
    this.logService.debug('RelationshipGraphComponent', '更新節點狀態', {
      isCreatingRelationship: this.isCreatingRelationship,
      isMerging: this.isMerging,
      firstSelectedNode: this.firstSelectedNode?.name,
      firstSelectedNodeForMerge: this.firstSelectedNodeForMerge?.name
    });
    
    // 更新所有節點的樣式
    this.svg.selectAll('.node-circle')
      .style('opacity', (d: any) => {
        if (this.isCreatingRelationship && this.firstSelectedNode && this.firstSelectedNode.id === d.id) {
          return 0.5; // 第一個選中的節點變半透明
        }
        if (this.isMerging && this.firstSelectedNodeForMerge && this.firstSelectedNodeForMerge.id === d.id) {
          return 0.5; // 合併模式第一個選中的節點變半透明
        }
        return 1.0;
      })
      .style('cursor', (d: any) => {
        if (this.isCreatingRelationship) {
          if (this.firstSelectedNode && this.firstSelectedNode.id === d.id) {
            return 'not-allowed'; // 第一個節點不可選
          }
          return 'pointer'; // 其他節點可以選
        }
        if (this.isMerging) {
          if (this.firstSelectedNodeForMerge && this.firstSelectedNodeForMerge.id === d.id) {
            return 'not-allowed'; // 合併模式第一個節點不可選
          }
          return 'pointer'; // 其他節點可以選
        }
        return 'pointer';
      });
  }

  /**
   * 處理建立關係模式下的節點點擊
   */
  private handleRelationshipNodeClick(node: GraphNode): void {
    if (!this.isCreatingRelationship || !this.firstSelectedNode) return;
    
    // 不能選擇同一個節點
    if (node.id === this.firstSelectedNode.id) {
      this.logService.warn('RelationshipGraphComponent', '不能選擇同一個節點建立關係');
      return;
    }
    
    this.logService.info('RelationshipGraphComponent', '選擇第二個節點', {
      secondNodeName: node.name,
      secondNodeId: node.id
    });
    
    this.secondSelectedNode = node;
    this.showRelationshipDialog = true;
    
    // 等待 DOM 更新後自動聚焦到輸入框
    setTimeout(() => {
      if (this.relationshipInput) {
        this.relationshipInput.nativeElement.focus();
        this.logService.debug('RelationshipGraphComponent', '自動聚焦到關係輸入框');
      }
    }, 100);
  }

  /**
   * 確認建立關係
   */
  confirmRelationshipCreation(): void {
    if (!this.firstSelectedNode || !this.secondSelectedNode || !this.relationshipType.trim()) {
      this.logService.error('RelationshipGraphComponent', '缺少建立關係的必要資料');
      return;
    }
    
    this.logService.info('RelationshipGraphComponent', '確認建立關係', {
      firstNode: this.firstSelectedNode.name,
      secondNode: this.secondSelectedNode.name,
      relationshipType: this.relationshipType
    });
    
    // TODO: 調用後端 API 保存關係
    // 這裡先模擬成功
    this.saveRelationshipToDatabase();
  }

  /**
   * 保存關係到資料庫
   */
  private saveRelationshipToDatabase(): void {
    if (!this.firstSelectedNode || !this.secondSelectedNode || !this.relationshipType.trim()) {
      this.logService.error('RelationshipGraphComponent', '缺少建立關係的必要資料');
      return;
    }

    const request = {
      sourcePersonId: parseInt(this.firstSelectedNode.id),
      targetPersonId: parseInt(this.secondSelectedNode.id),
      relationshipType: this.relationshipType.trim(),
      visualAnalysisGraphId: this.visualAnalysisGraphId // 傳遞視覺化分析圖表ID
    };

    this.logService.info('RelationshipGraphComponent', '開始保存關係到資料庫', request);

    this.relationshipGraphService.createRelationship(request).subscribe({
      next: (response: CreateRelationshipResponse) => {
        this.logService.info('RelationshipGraphComponent', '關係保存響應接收', {
          success: response.success,
          message: response.message,
          currentGraphDataLinksCount: this.graphData?.metadata?.totalLinks || 0
        });
        
        if (response.success) {
          this.logService.info('RelationshipGraphComponent', '關係保存成功，準備添加新連線到圖譜', {
            firstNode: this.firstSelectedNode?.name,
            secondNode: this.secondSelectedNode?.name,
            relationshipType: this.relationshipType,
            currentLinksCount: this.graphData?.metadata?.totalLinks || 0
          });
          
          this.addNewRelationshipToGraph();
          this.resetRelationshipCreation();
          
          this.logService.info('RelationshipGraphComponent', '新連線添加完成，發出事件給父組件', {
            finalLinksCount: this.graphData?.metadata?.totalLinks || 0
          });
          
          // 發出關係建立成功事件，通知父組件
          this.relationshipCreated.emit();
        } else {
          this.logService.error('RelationshipGraphComponent', '關係保存失敗', {
            message: response.message
          });
          // 可以顯示錯誤訊息給用戶
        }
      },
      error: (error: any) => {
        this.logService.error('RelationshipGraphComponent', '關係保存請求失敗', error);
        // 可以顯示錯誤訊息給用戶
      }
    });
  }

  /**
   * 取消建立關係
   */
  cancelRelationshipCreation(): void {
    this.logService.info('RelationshipGraphComponent', '取消建立關係');
    this.resetRelationshipCreation();
  }

  /**
   * 處理關係輸入框的鍵盤事件
   */
  handleRelationshipInputKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      if (this.relationshipType.trim()) {
        this.confirmRelationshipCreation();
      }
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.cancelRelationshipCreation();
    }
  }

  /**
   * 添加新關係到現有圖譜中，保持節點位置不變
   */
  private addNewRelationshipToGraph(): void {
    if (!this.firstSelectedNode || !this.secondSelectedNode || !this.relationshipType.trim()) {
      this.logService.error('RelationshipGraphComponent', '缺少新關係的必要資料');
      return;
    }

    if (!this.graphData) {
      this.logService.error('RelationshipGraphComponent', '圖譜數據不存在，無法添加新關係');
      return;
    }

    // 驗證節點是否存在於當前圖譜中
    const sourceExists = this.graphData.nodes.some(node => node.id === this.firstSelectedNode!.id);
    const targetExists = this.graphData.nodes.some(node => node.id === this.secondSelectedNode!.id);

    if (!sourceExists || !targetExists) {
      this.logService.error('RelationshipGraphComponent', '選中的節點不存在於當前圖譜中，無法建立關係', {
        firstNodeId: this.firstSelectedNode.id,
        secondNodeId: this.secondSelectedNode.id,
        sourceExists,
        targetExists,
        availableNodes: this.graphData.nodes.map(n => n.id)
      });
      return;
    }

    // 創建新的連線對象
    const newLink: GraphLink = {
      source: this.firstSelectedNode.id,
      target: this.secondSelectedNode.id,
      type: this.relationshipType.trim(),
      isFamily: this.relationshipType.includes('父') || 
                this.relationshipType.includes('母') || 
                this.relationshipType.includes('子') || 
                this.relationshipType.includes('女') ||
                this.relationshipType.includes('夫') ||
                this.relationshipType.includes('妻') ||
                this.relationshipType.includes('兄') ||
                this.relationshipType.includes('弟') ||
                this.relationshipType.includes('姐') ||
                this.relationshipType.includes('妹')
    };

    this.logService.info('RelationshipGraphComponent', '添加新連線到圖譜 - 修復前狀態', {
      newLink,
      currentLinksCount: this.graphData.metadata?.totalLinks || 0,
      currentLinks: this.graphData.links.map(l => ({
        source: l.source,
        target: l.target,
        type: l.type
      }))
    });

    // 添加新連線到圖譜數據
    this.graphData.links.push(newLink);

    this.logService.info('RelationshipGraphComponent', '添加新連線到圖譜 - 修復後狀態', {
      newLinksCount: this.graphData.metadata?.totalLinks || 0,
      allLinks: this.graphData.links.map(l => ({
        source: l.source,
        target: l.target,
        type: l.type
      }))
    });

    // 更新統計資訊
    this.updateStatistics();

    // 只更新圖譜顯示，不重置節點位置
    if (this.svg && this.simulation) {
      this.updateGraphWithNewLink(newLink);
    }

    this.logService.info('RelationshipGraphComponent', '新連線添加完成');
  }

  /**
   * 更新圖譜顯示，添加新連線但保持節點位置
   */
  private updateGraphWithNewLink(newLink: GraphLink): void {
    if (!this.svg || !this.graphData) {
      return;
    }

    const graphGroup = this.svg.select('.graph-group');
    
    // 驗證新連線的節點是否存在於當前圖譜中
    const sourceExists = this.graphData.nodes.some(node => node.id === newLink.source);
    const targetExists = this.graphData.nodes.some(node => node.id === newLink.target);
    
    if (!sourceExists) {
      this.logService.error('RelationshipGraphComponent', '新連線的源節點不存在於當前圖譜中', {
        sourceId: newLink.source,
        availableNodes: this.graphData.nodes.map(n => n.id)
      });
      return;
    }
    
    if (!targetExists) {
      this.logService.error('RelationshipGraphComponent', '新連線的目標節點不存在於當前圖譜中', {
        targetId: newLink.target,
        availableNodes: this.graphData.nodes.map(n => n.id)
      });
      return;
    }
    
    this.logService.info('RelationshipGraphComponent', '開始計算可見連線', {
      totalLinksInGraphData: this.graphData.metadata?.totalLinks || 0,
      hiddenNodesSize: this.hiddenNodes.size,
      hiddenNodes: Array.from(this.hiddenNodes),
      allLinksInGraphData: this.graphData.links.map(l => ({
        source: l.source,
        target: l.target,
        type: l.type
      }))
    });

    // 更新連線數據 - 只包含節點都存在的連線
    const visibleLinks = this.graphData.links.filter(link => {
      // 處理D3可能已將source/target轉換為節點對象的情況
      let sourceId: string;
      let targetId: string;
      
      if (typeof link.source === 'object' && link.source !== null && 'id' in link.source) {
        sourceId = String((link.source as any).id);
      } else {
        sourceId = String(link.source);
      }
      
      if (typeof link.target === 'object' && link.target !== null && 'id' in link.target) {
        targetId = String((link.target as any).id);
      } else {
        targetId = String(link.target);
      }
      
      const sourceVisible = !this.hiddenNodes.has(sourceId);
      const targetVisible = !this.hiddenNodes.has(targetId);
      
      // 檢查節點存在性
      const sourceExists = this.graphData!.nodes.some(node => 
        String(node.id) === sourceId
      );
      const targetExists = this.graphData!.nodes.some(node => 
        String(node.id) === targetId
      );
      
      // 只對前2條和最後1條連線記錄詳細日誌，避免日誌過多
      const linkIndex = this.graphData!.links.indexOf(link);
      const shouldLogDetail = linkIndex < 2 || linkIndex === this.graphData!.links.length - 1;
      
      if (shouldLogDetail) {
        this.logService.info('RelationshipGraphComponent', `updateGraphWithNewLink連線可見性檢查 [${linkIndex}]`, {
          link: { source: link.source, target: link.target, type: link.type },
          sourceId,
          targetId,
          sourceVisible,
          targetVisible,
          sourceExists,
          targetExists,
          isVisible: sourceVisible && targetVisible && sourceExists && targetExists,
          availableNodeIds: this.graphData!.nodes.map(n => n.id),
          linkSourceType: typeof link.source,
          linkTargetType: typeof link.target
        });
      }
      
      return sourceVisible && targetVisible && sourceExists && targetExists;
    });

    this.logService.info('RelationshipGraphComponent', '更新圖譜顯示，保留所有現有連線', {
      totalLinks: this.graphData.metadata?.totalLinks || 0,
      visibleLinksCount: visibleLinks.length,
      visibleLinks: visibleLinks.map(l => ({
        source: l.source,
        target: l.target,
        type: l.type
      })),
      newLinkAdded: newLink
    });

    // 記錄D3數據綁定前的狀態
    const existingLinkElements = graphGroup.selectAll('.link').nodes();
    this.logService.info('RelationshipGraphComponent', 'D3數據綁定前狀態', {
      existingLinkElementsCount: existingLinkElements.length,
      visibleLinksForBinding: visibleLinks.length
    });

    // 重新綁定連線數據並添加新連線，使用唯一鍵值函數確保D3正確識別現有連線
    const link = graphGroup.selectAll('.link')
      .data(visibleLinks, (d: any) => {
        const sourceId = typeof d.source === 'object' ? d.source.id : d.source;
        const targetId = typeof d.target === 'object' ? d.target.id : d.target;
        const key = `${sourceId}-${targetId}-${d.type}`;
        this.logService.debug('RelationshipGraphComponent', 'D3鍵值生成', {
          link: { source: d.source, target: d.target, type: d.type },
          sourceId,
          targetId,
          generatedKey: key
        });
        return key;
      })
      .join('line')
      .attr('class', 'link')
      .style('stroke', (d: any) => d.isFamily ? '#ff6b35' : '#666')
      .style('stroke-width', 2)
      .style('opacity', 0.6);

    // 記錄D3數據綁定後的狀態
    const finalLinkElements = graphGroup.selectAll('.link').nodes();
    this.logService.info('RelationshipGraphComponent', 'D3數據綁定後狀態', {
      finalLinkElementsCount: finalLinkElements.length,
      expectedCount: visibleLinks.length
    });

    // 重新綁定連線標籤，使用相同的唯一鍵值函數
    const linkLabel = graphGroup.selectAll('.link-label')
      .data(visibleLinks, (d: any) => {
        const sourceId = typeof d.source === 'object' ? d.source.id : d.source;
        const targetId = typeof d.target === 'object' ? d.target.id : d.target;
        return `${sourceId}-${targetId}-${d.type}`;
      })
      .join('text')
      .attr('class', 'link-label')
      .style('text-anchor', 'middle')
      .style('font-size', '10px')
      .style('fill', '#e0e0e0')
      .style('pointer-events', 'none')
      .style('font-weight', 'bold')
      .text((d: any) => d.type || '關係');

    // 確保節點在最上層：將所有節點元素移到DOM最後
    graphGroup.selectAll('.node').each(function(this: SVGGElement) {
      this.parentNode?.appendChild(this);
    });

    // 立即設置新連線的位置，處理source/target可能是字符串ID或節點對象的情況
    link
      .attr('x1', (d: any) => {
        // 檢查source是否為節點對象，如果不是則查找對應節點
        if (typeof d.source === 'object' && d.source.x !== undefined) {
          return d.source.x;
        } else {
          const sourceNode = this.graphData?.nodes.find(n => String(n.id) === String(d.source));
          this.logService.info('RelationshipGraphComponent', '立即設置連線源點位置', {
            linkSource: d.source,
            linkType: d.type,
            sourceNode: sourceNode ? { id: sourceNode.id, x: sourceNode.x, y: sourceNode.y } : null,
            foundNode: !!sourceNode,
            allNodeIds: this.graphData?.nodes.map(n => n.id)
          });
          return sourceNode?.x || 0;
        }
      })
      .attr('y1', (d: any) => {
        if (typeof d.source === 'object' && d.source.y !== undefined) {
          return d.source.y;
        } else {
          const sourceNode = this.graphData?.nodes.find(n => n.id === d.source || n.id === String(d.source));
          return sourceNode?.y || 0;
        }
      })
      .attr('x2', (d: any) => {
        if (typeof d.target === 'object' && d.target.x !== undefined) {
          return d.target.x;
        } else {
          const targetNode = this.graphData?.nodes.find(n => String(n.id) === String(d.target));
          this.logService.info('RelationshipGraphComponent', '立即設置連線目標位置', {
            linkTarget: d.target,
            linkType: d.type,
            targetNode: targetNode ? { id: targetNode.id, x: targetNode.x, y: targetNode.y } : null,
            foundNode: !!targetNode
          });
          return targetNode?.x || 0;
        }
      })
      .attr('y2', (d: any) => {
        if (typeof d.target === 'object' && d.target.y !== undefined) {
          return d.target.y;
        } else {
          const targetNode = this.graphData?.nodes.find(n => n.id === d.target || n.id === String(d.target));
          return targetNode?.y || 0;
        }
      });

    // 立即設置連線標籤位置
    linkLabel
      .attr('x', (d: any) => {
        const sourceX = typeof d.source === 'object' && d.source.x !== undefined ? 
          d.source.x : (this.graphData?.nodes.find(n => n.id === d.source || n.id === String(d.source))?.x || 0);
        const targetX = typeof d.target === 'object' && d.target.x !== undefined ? 
          d.target.x : (this.graphData?.nodes.find(n => n.id === d.target || n.id === String(d.target))?.x || 0);
        return (sourceX + targetX) / 2;
      })
      .attr('y', (d: any) => {
        const sourceY = typeof d.source === 'object' && d.source.y !== undefined ? 
          d.source.y : (this.graphData?.nodes.find(n => n.id === d.source || n.id === String(d.source))?.y || 0);
        const targetY = typeof d.target === 'object' && d.target.y !== undefined ? 
          d.target.y : (this.graphData?.nodes.find(n => n.id === d.target || n.id === String(d.target))?.y || 0);
        return (sourceY + targetY) / 2;
      });

    // 更新力導向模擬的連線數據
    if (this.simulation) {
      try {
        const previousLinkForce = this.simulation.force('link');
        const previousLinks = previousLinkForce ? previousLinkForce.links() : [];
        
        this.logService.info('RelationshipGraphComponent', '更新力導向模擬前狀態', {
          previousLinksCount: previousLinks.length,
          newVisibleLinksCount: visibleLinks.length,
          previousLinks: previousLinks.map((l: any) => ({
            source: typeof l.source === 'object' ? l.source.id : l.source,
            target: typeof l.target === 'object' ? l.target.id : l.target,
            type: l.type || 'unknown'
          })),
          newVisibleLinks: visibleLinks.map(l => ({
            source: l.source,
            target: l.target,
            type: l.type
          }))
        });
        
        // 無論是否為第一條連線，都只更新力導向的連線數據，不重新初始化整個圖譜
        if (visibleLinks.length > 0) {
          // 設置連線力，並重新啟動模擬以建立新連線的節點綁定
          this.simulation.force('link').links(visibleLinks);
          
          // 重新啟動模擬讓D3重新建立連線與節點的綁定關係
          this.simulation.alpha(0.3).restart();
          
          const updatedLinkForce = this.simulation.force('link');
          const updatedLinks = updatedLinkForce ? updatedLinkForce.links() : [];
          
          this.logService.info('RelationshipGraphComponent', '已更新力導向連線數據並重啟模擬，重新建立連線節點綁定', {
            linksCount: visibleLinks.length,
            actualUpdatedLinksCount: updatedLinks.length,
            updatedLinks: updatedLinks.map((l: any) => ({
              source: typeof l.source === 'object' ? l.source.id : l.source,
              target: typeof l.target === 'object' ? l.target.id : l.target,
              type: l.type || 'unknown'
            }))
          });
          
          // 短暫延遲後降低模擬強度，避免節點位置大幅變動
          setTimeout(() => {
            if (this.simulation) {
              this.simulation.alpha(0.1);
            }
          }, 500);
        } else {
          // 沒有連線時：移除連線力，但保持其他力（節點排斥、碰撞、中心力）運行
          this.simulation.force('link').links([]);
          this.logService.info('RelationshipGraphComponent', '已移除連線力，但保持節點互動力運行');
        }
      } catch (error) {
        this.logService.error('RelationshipGraphComponent', '更新力導向模擬失敗', error);
      }
    }

    this.logService.info('RelationshipGraphComponent', '圖譜顯示更新完成，所有節點和連線位置保持不變');
  }



  /**
   * 重置建立關係狀態
   */
  private resetRelationshipCreation(): void {
    this.isCreatingRelationship = false;
    this.firstSelectedNode = null;
    this.secondSelectedNode = null;
    this.showRelationshipDialog = false;
    this.relationshipType = '';
    
    // 恢復節點狀態
    this.updateNodeStates();
  }

  /**
   * 統一處理節點照片載入
   */
  private loadNodePhoto(nodeData: any, imageElement: any, self: RelationshipGraphComponent): void {
    // 確定照片索引來源
    let photoIndex = nodeData.data?.photo;
    
    // 如果沒有直接的照片索引，嘗試從節點資料中獲取
    if (!photoIndex || photoIndex === '' || photoIndex === '0') {
      photoIndex = nodeData.photo;
    }
    
    // 確定專案ID
    const projectId = nodeData.data?.projectId || nodeData.projectId;
    
    // 確定人員姓名
    const personName = nodeData.name || nodeData.data?.name;
    
    // 檢查是否有有效的照片資料
    if (!photoIndex || photoIndex === '' || photoIndex === '0' || !projectId) {
      // 沒有照片資料，保持預設圖標
      self.logService?.debug('RelationshipGraphComponent', '無照片資料，使用預設圖標', {
        nodeName: personName,
        photoIndex: photoIndex,
        projectId: projectId
      });
      return;
    }
    
    // 使用PhotoUtilsService統一處理照片URL生成
    const photoUrl = this.photoUtils.getPersonPhotoUrl(photoIndex, personName, projectId);
    
    // 檢查URL是否有效
    if (!photoUrl) {
      self.logService?.debug('RelationshipGraphComponent', '照片URL生成失敗，使用預設圖標', {
        nodeName: personName,
        photoIndex: photoIndex,
        projectId: projectId
      });
      return;
    }
    
    // 預載照片檢查是否存在
    const testImage = new Image();
    testImage.onload = () => {
      // 照片載入成功，替換預設圖標
      imageElement.select('.node-icon-default').remove();
      imageElement.select('.node-image-photo').remove();
      
      // 添加圓形遮罩
      const defs = self.svg?.select('defs').empty() ? 
        self.svg?.append('defs') : self.svg?.select('defs');
      
      const clipId = `clip-circle-${nodeData.id}`;
      defs?.selectAll(`#${clipId}`).remove();
      defs?.append('clipPath')
        .attr('id', clipId)
        .append('circle')
        .attr('r', 22) // 比節點圓圈稍小
        .attr('cx', 0)
        .attr('cy', 0);
      
      // 添加照片
      imageElement.append('image')
        .attr('class', 'node-image-photo')
        .attr('href', photoUrl)
        .attr('x', -22)
        .attr('y', -22)
        .attr('width', 44)
        .attr('height', 44)
        .attr('clip-path', `url(#${clipId})`)
        .style('pointer-events', 'none');
        
      self.logService?.debug('RelationshipGraphComponent', '✅ 成功載入節點照片', {
        nodeName: personName,
        photoIndex: photoIndex,
        photoUrl: photoUrl
      });
    };
    
    testImage.onerror = () => {
      // 照片載入失敗，保持預設圖標
      self.logService?.debug('RelationshipGraphComponent', '❌ 節點照片載入失敗，使用預設圖標', {
        nodeName: personName,
        photoIndex: photoIndex,
        photoUrl: photoUrl
      });
    };
    
    testImage.src = photoUrl;
  }
} 