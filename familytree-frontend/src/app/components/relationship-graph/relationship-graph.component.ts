// 通用關聯圖譜組件：接收外部數據並呈現互動式圖譜
// 主要功能：D3.js 圖譜渲染、互動控制、數據可視化

import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RelationshipGraphService, GraphData, GraphNode, GraphLink, CreateRelationshipResponse } from '../../services/relationship-graph.service';
import { LogService } from '../../services/log.service';
import { PersonDetailDialogComponent } from '../person-detail-dialog/person-detail-dialog.component';
import * as d3 from 'd3';

@Component({
  selector: 'app-relationship-graph',
  standalone: true,
  imports: [CommonModule, FormsModule, PersonDetailDialogComponent],
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

      <!-- 建立關係模式提示 -->
      <div class="relationship-mode-indicator" *ngIf="isCreatingRelationship">
        <div class="indicator-content">
          <span class="indicator-icon">🔗</span>
          <span class="indicator-text">建立關係模式：請選擇第二個節點</span>
          <button class="cancel-btn" (click)="cancelRelationshipCreation()">取消</button>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./relationship-graph.component.scss']
})
export class RelationshipGraphComponent implements OnInit, OnChanges, AfterViewInit, OnDestroy {
  @ViewChild('graphContainer', { static: false }) graphContainer!: ElementRef;
  @ViewChild('graphViewport', { static: false }) graphViewport!: ElementRef;
  @ViewChild('relationshipInput', { static: false }) relationshipInput!: ElementRef;

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

  constructor(
    private relationshipGraphService: RelationshipGraphService,
    private cdr: ChangeDetectorRef,
    private logService: LogService
  ) {
    this.logService.info('RelationshipGraphComponent', '組件已初始化');
  }

  ngOnChanges(changes: SimpleChanges): void {
    this.logService.info('RelationshipGraphComponent', 'ngOnChanges 被調用', {
      changes: Object.keys(changes),
      selectedPersonIds: this.selectedPersonIds
    });

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
          dataLinks: response.data?.links?.length || 0
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
      linksCount: this.graphData.links.length
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
      
      this.logService.debug('RelationshipGraphComponent', `節點 ${node.name} 位置設置`, {
        nodeId: node.id,
        nodeName: node.name,
        position: { x: node.x, y: node.y },
        angle: angle,
        radius: radius
      });
    });

    // 創建力導向模擬 - 適中的力道讓佈局平衡
    this.simulation = d3.forceSimulation()
      .force('link', d3.forceLink().id((d: any) => d.id).distance(100)) // 保持連線距離 100
      .force('charge', d3.forceManyBody().strength(-20)) // 適中的排斥力 -20
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide().radius(22)); // 適中的碰撞半徑 22

    // 如果沒有連線，使用靜態佈局
    if (this.graphData.links.length === 0) {
      this.logService.info('RelationshipGraphComponent', '沒有連線，使用靜態佈局');
      this.simulation.stop(); // 停止力導向模擬
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
      const sourceVisible = !this.hiddenNodes.has(link.source);
      const targetVisible = !this.hiddenNodes.has(link.target);
      return sourceVisible && targetVisible;
    });

    this.logService.info('RelationshipGraphComponent', '更新圖譜', {
      visibleNodesCount: visibleNodes.length,
      visibleLinksCount: visibleLinks.length,
      totalNodes: this.graphData.nodes.length,
      totalLinks: this.graphData.links.length
    });

    // 更新連線
    const link = graphGroup.selectAll('.link')
      .data(visibleLinks)
      .join('line')
      .attr('class', 'link')
      .style('stroke', (d: any) => d.isFamily ? '#ff6b35' : '#666')
      .style('stroke-width', 2)
      .style('opacity', 0.6);

    // 更新連線標籤
    const linkLabel = graphGroup.selectAll('.link-label')
      .data(visibleLinks)
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

    // 節點頭像
    node.selectAll('.node-icon')
      .data((d: any) => [d])
      .join('text')
      .attr('class', 'node-icon')
      .attr('dy', '0.35em')
      .style('text-anchor', 'middle')
      .style('font-size', '16px')
      .style('fill', '#fff')
      .style('pointer-events', 'none')
      .text('👤');

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

    // 更新模擬
    if (visibleLinks.length > 0) {
      // 有連線時使用力導向模擬
      this.simulation
        .nodes(visibleNodes)
        .on('tick', () => {
          link
            .attr('x1', (d: any) => d.source.x)
            .attr('y1', (d: any) => d.source.y)
            .attr('x2', (d: any) => d.target.x)
            .attr('y2', (d: any) => d.target.y);

          // 更新連線標籤位置
          linkLabel
            .attr('x', (d: any) => (d.source.x + d.target.x) / 2)
            .attr('y', (d: any) => (d.source.y + d.target.y) / 2);

          node
            .attr('transform', (d: any) => `translate(${d.x},${d.y})`);
        });

      this.simulation.force('link')
        .links(visibleLinks);
    } else {
      // 沒有連線時直接設置節點位置
      this.logService.info('RelationshipGraphComponent', '沒有連線，直接設置節點位置', {
        nodesCount: visibleNodes.length
      });
      
      // 使用網格佈局 - 大幅減少節點間距
      const cols = Math.ceil(Math.sqrt(visibleNodes.length));
      const rows = Math.ceil(visibleNodes.length / cols);
      const nodeSize = 80; // 恢復原本的節點大小
      const spacing = 120; // 恢復原本的間距
      
      // 計算網格佈局的起始位置，讓節點群組居中
      const totalWidth = cols * spacing;
      const totalHeight = rows * spacing;
      const startX = (width - totalWidth) / 2;
      const startY = (height - totalHeight) / 2;
      
      visibleNodes.forEach((node, index) => {
        const col = index % cols;
        const row = Math.floor(index / cols);
        node.x = startX + col * spacing + spacing / 2;
        node.y = startY + row * spacing + spacing / 2;
        
        this.logService.debug('RelationshipGraphComponent', `節點 ${node.name} 網格位置`, {
          nodeId: node.id,
          nodeName: node.name,
          position: { x: node.x, y: node.y },
          gridPosition: { col, row }
        });
      });
      
      // 直接更新節點位置
      node.attr('transform', (d: any) => `translate(${d.x},${d.y})`);
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

  /**
   * 處理節點點擊
   */
  handleNodeClick(event: MouseEvent, node: GraphNode): void {
    event.stopPropagation();
    this.logService.info('RelationshipGraphComponent', '節點被點擊', { nodeName: node.name });
    
    // 如果在建立關係模式下，處理關係建立邏輯
    if (this.isCreatingRelationship) {
      this.handleRelationshipNodeClick(node);
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
   * 處理合併功能
   */
  handleMerge(): void {
    this.logService.info('RelationshipGraphComponent', '合併功能開發中', {
      nodeName: this.selectedNodeForMenu?.name
    });
    this.closeNodeMenu();
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
   * 更新節點狀態（建立關係模式）
   */
  private updateNodeStates(): void {
    if (!this.svg) return;
    
    this.logService.debug('RelationshipGraphComponent', '更新節點狀態', {
      isCreatingRelationship: this.isCreatingRelationship,
      firstSelectedNode: this.firstSelectedNode?.name
    });
    
    // 更新所有節點的樣式
    this.svg.selectAll('.node-circle')
      .style('opacity', (d: any) => {
        if (this.isCreatingRelationship && this.firstSelectedNode && this.firstSelectedNode.id === d.id) {
          return 0.5; // 第一個選中的節點變半透明
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
        this.logService.info('RelationshipGraphComponent', '關係保存成功', {
          success: response.success,
          message: response.message
        });
        
        if (response.success) {
          this.logService.info('RelationshipGraphComponent', '關係保存成功，添加新連線到圖譜');
          this.addNewRelationshipToGraph();
          this.resetRelationshipCreation();
          
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

    this.logService.info('RelationshipGraphComponent', '添加新連線到圖譜', {
      newLink,
      currentLinksCount: this.graphData.links.length
    });

    // 添加新連線到圖譜數據
    this.graphData.links.push(newLink);

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
    
    // 更新連線數據 - 只包含節點都存在的連線
    const visibleLinks = this.graphData.links.filter(link => {
      const sourceVisible = !this.hiddenNodes.has(link.source);
      const targetVisible = !this.hiddenNodes.has(link.target);
      const sourceExists = this.graphData!.nodes.some(node => node.id === link.source);
      const targetExists = this.graphData!.nodes.some(node => node.id === link.target);
      return sourceVisible && targetVisible && sourceExists && targetExists;
    });

    // 重新綁定連線數據並添加新連線，確保圖層順序
    const link = graphGroup.selectAll('.link')
      .data(visibleLinks)
      .join('line')
      .attr('class', 'link')
      .style('stroke', (d: any) => d.isFamily ? '#ff6b35' : '#666')
      .style('stroke-width', 2)
      .style('opacity', 0.6);

    // 重新綁定連線標籤
    const linkLabel = graphGroup.selectAll('.link-label')
      .data(visibleLinks)
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

    // 立即設置新連線的位置
    link
      .attr('x1', (d: any) => {
        const sourceNode = this.graphData?.nodes.find(n => n.id === d.source || n.id === String(d.source));
        this.logService.debug('RelationshipGraphComponent', '設置連線源點位置', {
          linkSource: d.source,
          sourceNode: sourceNode,
          position: sourceNode ? { x: sourceNode.x, y: sourceNode.y } : null
        });
        return sourceNode?.x || 0;
      })
      .attr('y1', (d: any) => {
        const sourceNode = this.graphData?.nodes.find(n => n.id === d.source || n.id === String(d.source));
        return sourceNode?.y || 0;
      })
      .attr('x2', (d: any) => {
        const targetNode = this.graphData?.nodes.find(n => n.id === d.target || n.id === String(d.target));
        this.logService.debug('RelationshipGraphComponent', '設置連線目標位置', {
          linkTarget: d.target,
          targetNode: targetNode,
          position: targetNode ? { x: targetNode.x, y: targetNode.y } : null
        });
        return targetNode?.x || 0;
      })
      .attr('y2', (d: any) => {
        const targetNode = this.graphData?.nodes.find(n => n.id === d.target || n.id === String(d.target));
        return targetNode?.y || 0;
      });

    // 立即設置連線標籤位置
    linkLabel
      .attr('x', (d: any) => {
        const sourceNode = this.graphData?.nodes.find(n => n.id === d.source || n.id === String(d.source));
        const targetNode = this.graphData?.nodes.find(n => n.id === d.target || n.id === String(d.target));
        return ((sourceNode?.x || 0) + (targetNode?.x || 0)) / 2;
      })
      .attr('y', (d: any) => {
        const sourceNode = this.graphData?.nodes.find(n => n.id === d.source || n.id === String(d.source));
        const targetNode = this.graphData?.nodes.find(n => n.id === d.target || n.id === String(d.target));
        return ((sourceNode?.y || 0) + (targetNode?.y || 0)) / 2;
      });

    // 為了確保節點位置不變，我們不重啟力導向模擬
    // 只是靜態地更新力導向的連線數據，不啟動模擬
    if (this.simulation && visibleLinks.length > 0) {
      try {
        // 只更新連線數據，但不重啟模擬
        this.simulation.force('link').links(visibleLinks);
        this.logService.info('RelationshipGraphComponent', '已更新力導向連線數據，但保持節點位置不變');
      } catch (error) {
        this.logService.error('RelationshipGraphComponent', '更新力導向連線數據失敗', error);
      }
    }

    this.logService.info('RelationshipGraphComponent', '圖譜顯示更新完成，節點位置保持不變');
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
} 