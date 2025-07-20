// 通用關聯圖譜組件：接收外部數據並呈現互動式圖譜
// 主要功能：D3.js 圖譜渲染、互動控制、數據可視化

import { Component, Input, OnInit, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RelationshipGraphService, GraphData, GraphNode, GraphLink } from '../../services/relationship-graph.service';
import { LogService } from '../../services/log.service';
import * as d3 from 'd3';

@Component({
  selector: 'app-relationship-graph',
  standalone: true,
  imports: [CommonModule],
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
    </div>
  `,
  styleUrls: ['./relationship-graph.component.scss']
})
export class RelationshipGraphComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('graphContainer', { static: false }) graphContainer!: ElementRef;
  @ViewChild('graphViewport', { static: false }) graphViewport!: ElementRef;

  @Input() graphData?: GraphData;
  @Input() autoAnalyze: boolean = false;
  @Input() selectedPersonIds: number[] = [];

  private svg: any;
  private simulation: any;
  isDragging = false;
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

  constructor(
    private relationshipGraphService: RelationshipGraphService,
    private cdr: ChangeDetectorRef,
    private logService: LogService
  ) {
    this.logService.info('RelationshipGraphComponent', '組件已初始化');
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
      const radius = Math.min(width, height) * 0.3;
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

    // 創建力導向模擬
    this.simulation = d3.forceSimulation()
      .force('link', d3.forceLink().id((d: any) => d.id).distance(100))
      .force('charge', d3.forceManyBody().strength(-300))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide().radius(30));

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

    // 更新節點
    const node = graphGroup.selectAll('.node')
      .data(visibleNodes)
      .join('g')
      .attr('class', 'node')
      .call(this.dragBehavior());

    // 節點圓圈
    node.selectAll('.node-circle')
      .data((d: any) => [d])
      .join('circle')
      .attr('class', 'node-circle')
      .attr('r', 25)
      .style('fill', (d: any) => {
        if (d.gender === 'male') {
          return '#42A5F5';
        } else {
          return '#F48FB1';
        }
      })
      .style('stroke', '#fff')
      .style('stroke-width', 3)
      .style('cursor', 'pointer')
      .on('click', (event: any, d: any) => {
        this.viewPersonDetails(d);
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
      
      // 使用網格佈局
      const cols = Math.ceil(Math.sqrt(visibleNodes.length));
      const rows = Math.ceil(visibleNodes.length / cols);
      const nodeSize = 80;
      const spacing = 120;
      
      visibleNodes.forEach((node, index) => {
        const col = index % cols;
        const row = Math.floor(index / cols);
        node.x = (col + 1) * spacing;
        node.y = (row + 1) * spacing;
        
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
        if (!this.isDragging) return;
        if (!event.active) this.simulation.alphaTarget(0.3).restart();
        d.fx = d.x;
        d.fy = d.y;
      })
      .on('drag', (event: any, d: any) => {
        if (!this.isDragging) return;
        d.fx = event.x;
        d.fy = event.y;
      })
      .on('end', (event: any, d: any) => {
        if (!this.isDragging) return;
        if (!event.active) this.simulation.alphaTarget(0);
        d.fx = null;
        d.fy = null;
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
} 