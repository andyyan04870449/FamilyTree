// 組織圖頁面：用於視覺化顯示組織架構圖
// 主要功能：階層式人員卡片顯示、關係連線、人員管理操作

import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { OrganizationChartService, OrgNode, OrgConnection, OrgFilter } from '../../services/organization-chart.service';

// 匯入組件
import { OrgNodeComponent } from '../../components/org-node/org-node.component';
import { OrgConnectionsComponent } from '../../components/org-connections/org-connections.component';
import { OrgSidebarComponent } from '../../components/org-sidebar/org-sidebar.component';
import { NodeEditDialogComponent } from '../../components/node-edit-dialog/node-edit-dialog.component';

@Component({
  selector: 'app-organization-chart',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule,
    OrgNodeComponent,
    OrgConnectionsComponent,
    OrgSidebarComponent,
    NodeEditDialogComponent
  ],
  template: `
    <div class="organization-chart-page">
      <!-- 功能操作區 -->
      <div class="toolbar-section">
        <div class="toolbar-left">
          <button *ngIf="isFromRelationshipGraph" class="btn btn-secondary" (click)="backToRelationshipGraph()">
            <span class="btn-icon">←</span>
            <span class="btn-text">返回關係圖</span>
          </button>
          <button class="btn btn-secondary" (click)="isSidebarOpen = !isSidebarOpen">
            <span class="btn-icon">🔍</span>
            <span class="btn-text">搜尋篩選</span>
          </button>
          <button class="btn btn-primary" (click)="addNewPerson()">
            <span class="btn-icon">👤</span>
            <span class="btn-text">新增人員</span>
          </button>
          <button class="btn btn-secondary" (click)="refreshChart()">
            <span class="btn-icon">🔄</span>
            <span class="btn-text">重新整理</span>
          </button>
        </div>
        <div class="toolbar-right">
          <button class="btn btn-info" (click)="exportChart()">
            <span class="btn-icon">📥</span>
            <span class="btn-text">匯出圖表</span>
          </button>
          <button class="btn btn-warning" (click)="toggleFullscreen()">
            <span class="btn-icon">⛶</span>
            <span class="btn-text">全螢幕</span>
          </button>
        </div>
      </div>

      <!-- 載入狀態 -->
      <div *ngIf="loading" class="loading-container">
        <div class="loading-spinner"></div>
        <p>載入組織架構中...</p>
      </div>

      <!-- 錯誤狀態 -->
      <div *ngIf="error && !loading" class="error-container">
        <div class="error-icon">⚠️</div>
        <p>{{ error }}</p>
        <button class="btn btn-primary" (click)="loadOrganizationData()">重新載入</button>
      </div>

      <!-- 組織圖容器 -->
      <div *ngIf="!loading && !error" class="chart-container" #chartContainer>
          <!-- 側邊欄 -->
          <app-org-sidebar
            [isOpen]="isSidebarOpen"
            [searchTerm]="searchTerm"
            [filters]="filters"
            (close)="isSidebarOpen = false"
            (onSearchChange)="handleSearch($event)"
            (onFilterChange)="handleFilterChange($event.filterId, $event.checked)"
          ></app-org-sidebar>
          
          <div 
            class="chart-viewport"
            (mousemove)="onMouseMove($event)"
            (mouseup)="onMouseUp()"
          >
            <div 
              class="org-chart-wrapper"
              [style.transform]="'scale(' + zoom + ')'"
              [style.transformOrigin]="'center center'"
            >
              <!-- 節點組件 (放在上層) -->
              <app-org-node
                *ngFor="let node of nodes | keyvalue"
                [node]="node.value"
                [isSelected]="selectedNode === node.value.id"
                [isDragging]="isDragging && currentDragNode === node.value.id"
                (select)="selectedNode = $event"
                (edit)="handleNodeEdit($event)"
                (delete)="handleNodeDelete($event)"
                (dragStart)="onNodeMouseDown($event.event, $event.nodeId)"
              ></app-org-node>
              
              <!-- 連線組件 (放在下層) -->
              <app-org-connections
                [connections]="connections"
                [nodes]="nodes"
              ></app-org-connections>
            </div>
          </div>

          <!-- 圖表控制按鈕 -->
          <div class="chart-controls">
            <button class="control-btn" (click)="zoomIn()" title="放大">
              <span>🔍+</span>
            </button>
            <button class="control-btn" (click)="zoomOut()" title="縮小">
              <span>🔍−</span>
            </button>
            <button class="control-btn" (click)="resetZoom()" title="重置縮放">
              <span>⌂</span>
            </button>
            <button class="control-btn" (click)="centerChart()" title="置中顯示">
              <span>⭕</span>
            </button>
          </div>
          
          <!-- 底部操作按鈕 -->
          <div class="bottom-actions">
            <button class="btn btn-secondary" (click)="saveToTemp()">
              暫存系統
            </button>
            <button class="btn btn-primary" (click)="confirmSave()">
              確認
            </button>
          </div>
      </div>
      
      <!-- 編輯對話框 -->
      <app-node-edit-dialog
        [isOpen]="isEditDialogOpen"
        [node]="editingNode"
        (close)="isEditDialogOpen = false; editingNode = null"
        (save)="handleNodeSave($event); isEditDialogOpen = false; editingNode = null"
      ></app-node-edit-dialog>

      <!-- 空狀態 -->
      <div *ngIf="!loading && !error && totalMembers === 0" class="empty-state">
        <div class="empty-icon">📊</div>
        <h3>尚無組織架構資料</h3>
        <p>請先新增人員資料，建立組織架構</p>
        <button class="btn btn-primary" (click)="addNewPerson()">
          <span class="btn-icon">👤</span>
          <span class="btn-text">新增第一位人員</span>
        </button>
      </div>
    </div>
  `,
  styleUrls: ['./organization-chart.page.scss']
})
export class OrganizationChartComponent implements OnInit, OnDestroy {
  @ViewChild('chartContainer') chartContainer!: ElementRef<HTMLDivElement>;
  
  // 狀態管理
  loading = false;
  error = '';
  
  // 統計資料
  totalMembers = 0;
  hierarchyLevels = 2;
  departments = 2;
  
  // 組織資料
  nodes: Record<string, OrgNode> = {};
  connections: OrgConnection[] = [];
  filters: OrgFilter[] = [];
  
  // UI 狀態
  selectedNode: string | null = null;
  isSidebarOpen = false;
  editingNode: OrgNode | null = null;
  isEditDialogOpen = false;
  searchTerm = '';
  zoom = 1;
  isDragging = false;
  dragStart = { x: 0, y: 0 };
  currentDragNode: string | null = null;
  
  // 導航相關
  isFromRelationshipGraph = false;
  sourceNodeName = '';
  
  private destroy$ = new Subject<void>();
  
  constructor(
    private orgChartService: OrganizationChartService,
    private route: ActivatedRoute,
    private router: Router
  ) {
    console.log('🏢 OrganizationChartComponent 初始化');
  }

  ngOnInit(): void {
    // 處理來自關係圖的查詢參數
    this.route.queryParams.subscribe(params => {
      if (params['nodeId'] && params['from'] === 'relationship-graph') {
        this.isFromRelationshipGraph = true;
        this.sourceNodeName = params['nodeName'] || '';
        
        console.log('從關係圖導航而來', {
          nodeId: params['nodeId'],
          nodeName: params['nodeName'],
          from: params['from']
        });
        
        // TODO: 根據節點ID載入相關的組織資料
        // 例如：高亮顯示特定節點，或載入該節點相關的組織結構
      }
    });
    
    this.loadOrganizationData();
    this.subscribeToData();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  private subscribeToData(): void {
    this.orgChartService.nodes$
      .pipe(takeUntil(this.destroy$))
      .subscribe(nodes => {
        this.nodes = nodes;
        this.totalMembers = Object.keys(nodes).length;
      });
      
    this.orgChartService.connections$
      .pipe(takeUntil(this.destroy$))
      .subscribe(connections => {
        this.connections = connections;
      });
      
    this.orgChartService.filters$
      .pipe(takeUntil(this.destroy$))
      .subscribe(filters => {
        this.filters = filters;
      });
  }

  /**
   * 載入組織架構資料
   */
  loadOrganizationData(): void {
    this.loading = true;
    this.error = '';
    
    // TODO: 實際從後端API載入資料
    setTimeout(() => {
      this.loading = false;
      console.log('📊 組織架構資料載入完成');
    }, 1000);
  }

  /**
   * 處理節點拖拽
   */
  handleNodeDrag(id: string, x: number, y: number): void {
    this.orgChartService.updateNodePosition(id, x, y);
  }

  /**
   * 開始拖拽節點
   */
  onNodeMouseDown(event: MouseEvent, nodeId: string): void {
    event.preventDefault();
    this.isDragging = true;
    this.currentDragNode = nodeId;
    this.selectedNode = nodeId;
    
    const node = this.nodes[nodeId];
    this.dragStart = {
      x: event.clientX - node.x,
      y: event.clientY - node.y
    };
  }

  /**
   * 處理鼠標移動
   */
  onMouseMove(event: MouseEvent): void {
    if (!this.isDragging || !this.currentDragNode) return;
    
    const newX = event.clientX - this.dragStart.x;
    const newY = event.clientY - this.dragStart.y;
    this.handleNodeDrag(this.currentDragNode, newX, newY);
  }

  /**
   * 結束拖拽
   */
  onMouseUp(): void {
    this.isDragging = false;
    this.currentDragNode = null;
  }

  /**
   * 處理節點編輯
   */
  handleNodeEdit(id: string): void {
    this.editingNode = this.nodes[id];
    this.isEditDialogOpen = true;
  }

  /**
   * 處理節點刪除
   */
  handleNodeDelete(id: string): void {
    if (confirm('確定要刪除此人員嗎？')) {
      this.orgChartService.deleteNode(id);
      if (this.selectedNode === id) {
        this.selectedNode = null;
      }
    }
  }

  /**
   * 處理節點保存
   */
  handleNodeSave(nodeData: OrgNode): void {
    if (this.editingNode) {
      this.orgChartService.updateNode(nodeData);
    } else {
      const { id, ...nodeWithoutId } = nodeData;
      this.orgChartService.addNode(nodeWithoutId);
    }
    this.isEditDialogOpen = false;
    this.editingNode = null;
  }

  /**
   * 新增人員
   */
  addNewPerson(): void {
    console.log('👤 新增人員');
    this.editingNode = null;
    this.isEditDialogOpen = true;
  }

  /**
   * 重新整理圖表
   */
  refreshChart(): void {
    console.log('🔄 重新整理組織圖');
    this.loadOrganizationData();
  }

  /**
   * 匯出圖表
   */
  exportChart(): void {
    console.log('📥 匯出組織圖');
    // TODO: 實現圖表匯出功能
  }

  /**
   * 切換全螢幕
   */
  toggleFullscreen(): void {
    console.log('⛶ 切換全螢幕模式');
    if (!document.fullscreenElement) {
      this.chartContainer?.nativeElement.requestFullscreen();
    } else {
      document.exitFullscreen();
    }
  }

  /**
   * 放大圖表
   */
  zoomIn(): void {
    this.zoom = Math.min(this.zoom + 0.1, 2);
    console.log('🔍+ 放大圖表', this.zoom);
  }

  /**
   * 縮小圖表
   */
  zoomOut(): void {
    this.zoom = Math.max(this.zoom - 0.1, 0.5);
    console.log('🔍− 縮小圖表', this.zoom);
  }

  /**
   * 重置縮放
   */
  resetZoom(): void {
    this.zoom = 1;
    console.log('⌂ 重置縮放');
  }

  /**
   * 置中顯示
   */
  centerChart(): void {
    console.log('⭕ 置中顯示圖表');
    // TODO: 實現圖表置中
  }

  /**
   * 處理搜尋
   */
  handleSearch(term: string): void {
    this.searchTerm = term;
    const results = this.orgChartService.searchNodes(term);
    console.log('搜尋結果:', results);
  }

  /**
   * 處理篩選器變更
   */
  handleFilterChange(filterId: string, checked: boolean): void {
    this.orgChartService.updateFilter(filterId, checked);
  }

  /**
   * 暫存系統
   */
  saveToTemp(): void {
    console.log('暫存系統');
    // TODO: 實現暫存功能
  }

  /**
   * 確認儲存
   */
  confirmSave(): void {
    console.log('確認儲存');
    // TODO: 實現儲存功能
  }

  /**
   * 返回關係圖頁面
   */
  backToRelationshipGraph(): void {
    this.router.navigate(['/relationship-graph']);
  }
} 