// 使用 G6 實現的組織圖頁面
// 提供更美觀的視覺效果和更好的互動體驗

import { Component, OnInit, OnDestroy, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import * as G6 from '@antv/g6';

@Component({
  selector: 'app-organization-chart-g6',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="organization-chart-g6-page">
      <!-- 頁面標題 -->
      <div class="page-header">
        <div class="header-content">
          <h1>組織架構圖</h1>
          <p>使用 G6 引擎呈現的互動式組織圖</p>
        </div>
        <div class="header-actions">
          <button class="btn btn-primary" (click)="fitView()">
            <span class="icon">🔍</span>
            適應畫面
          </button>
          <button class="btn btn-secondary" (click)="downloadImage()">
            <span class="icon">💾</span>
            下載圖片
          </button>
        </div>
      </div>

      <!-- 工具列 -->
      <div class="toolbar">
        <div class="toolbar-group">
          <label>佈局方式：</label>
          <select [(ngModel)]="layoutType" (change)="changeLayout()">
            <option value="compactBox">緊湊樹狀</option>
            <option value="dendrogram">樹狀圖</option>
            <option value="mindmap">思維導圖</option>
            <option value="indented">縮進樹</option>
          </select>
        </div>
        
        <div class="toolbar-group">
          <label>主題風格：</label>
          <select [(ngModel)]="theme" (change)="changeTheme()">
            <option value="modern">現代風格</option>
            <option value="classic">經典風格</option>
            <option value="dark">深色主題</option>
            <option value="colorful">彩色主題</option>
          </select>
        </div>

        <div class="toolbar-group">
          <button class="btn btn-sm" (click)="expandAll()">全部展開</button>
          <button class="btn btn-sm" (click)="collapseAll()">全部收合</button>
        </div>
      </div>

      <!-- 圖表容器 -->
      <div id="g6-container" class="chart-container"></div>

      <!-- 右鍵選單 -->
      <div class="context-menu" *ngIf="showContextMenu" 
           [style.left.px]="contextMenuX" 
           [style.top.px]="contextMenuY">
        <div class="menu-item" (click)="addChild()">
          <span class="icon">➕</span> 新增下屬
        </div>
        <div class="menu-item" (click)="editNode()">
          <span class="icon">✏️</span> 編輯資訊
        </div>
        <div class="menu-item" (click)="deleteNode()">
          <span class="icon">🗑️</span> 刪除節點
        </div>
        <div class="menu-divider"></div>
        <div class="menu-item" (click)="expandNode()">
          <span class="icon">📂</span> 展開/收合
        </div>
      </div>

      <!-- 編輯對話框 -->
      <div class="edit-dialog" *ngIf="showEditDialog">
        <div class="dialog-content">
          <h3>編輯人員資訊</h3>
          <div class="form-group">
            <label>姓名：</label>
            <input type="text" [(ngModel)]="editingNode.name" />
          </div>
          <div class="form-group">
            <label>職位：</label>
            <input type="text" [(ngModel)]="editingNode.position" />
          </div>
          <div class="form-group">
            <label>部門：</label>
            <input type="text" [(ngModel)]="editingNode.department" />
          </div>
          <div class="dialog-actions">
            <button class="btn btn-primary" (click)="saveEdit()">儲存</button>
            <button class="btn btn-secondary" (click)="cancelEdit()">取消</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./organization-chart-g6.page.scss']
})
export class OrganizationChartG6Page implements OnInit, AfterViewInit, OnDestroy {
  private graph!: any;
  layoutType = 'compactBox';
  theme = 'modern';
  showContextMenu = false;
  contextMenuX = 0;
  contextMenuY = 0;
  selectedNode: any = null;
  showEditDialog = false;
  editingNode: any = { name: '', position: '', department: '' };

  // 示例數據
  private mockData = {
    id: 'root',
    name: '張三',
    position: '總經理',
    department: '管理部',
    children: [
      {
        id: '2',
        name: '李四',
        position: '技術總監',
        department: '技術部',
        children: [
          {
            id: '4',
            name: '王五',
            position: '前端主管',
            department: '前端組'
          },
          {
            id: '5',
            name: '趙六',
            position: '後端主管',
            department: '後端組'
          }
        ]
      },
      {
        id: '3',
        name: '陳七',
        position: '銷售總監',
        department: '銷售部',
        children: [
          {
            id: '6',
            name: '劉八',
            position: '銷售經理',
            department: '華北區'
          }
        ]
      }
    ]
  };

  ngOnInit(): void {
    // 初始化邏輯
  }

  ngAfterViewInit(): void {
    this.initGraph();
  }

  ngOnDestroy(): void {
    if (this.graph) {
      this.graph.destroy();
    }
  }

  private initGraph(): void {
    const container = document.getElementById('g6-container');
    if (!container) return;

    const width = container.scrollWidth;
    const height = container.scrollHeight || 600;

    // 註冊自定義節點
    this.registerCustomNode();

    this.graph = new (G6 as any).TreeGraph({
      container: 'g6-container',
      width,
      height,
      modes: {
        default: [
          'drag-canvas',
          'zoom-canvas',
          'click-select',
          {
            type: 'collapse-expand',
            onChange: (item: any, collapsed: any) => {
              const data = item?.get('model');
              data.collapsed = collapsed;
              return true;
            },
          },
        ],
      },
      defaultNode: {
        type: 'org-card',
        size: [200, 80],
      },
      defaultEdge: {
        type: 'smooth',
        style: {
          stroke: '#CED4D9',
          lineWidth: 2,
        },
      },
      layout: {
        type: 'compactBox',
        direction: 'TB',
        getId: (d: any) => d.id,
        getHeight: () => 80,
        getWidth: () => 200,
        getVGap: () => 20,
        getHGap: () => 40,
      },
    });

    // 載入數據
    this.graph.data(this.mockData);
    this.graph.render();
    this.graph.fitView();

    // 綁定事件
    this.bindEvents();
  }

  private registerCustomNode(): void {
    // 註冊自定義的組織圖卡片節點
    (G6 as any).registerNode(
      'org-card',
      {
        draw(cfg: any, group: any) {
          const { name, position, department } = cfg;
          const rect = group.addShape('rect', {
            attrs: {
              x: -100,
              y: -40,
              width: 200,
              height: 80,
              radius: 8,
              stroke: '#5B8FF9',
              fill: '#fff',
              lineWidth: 1,
              shadowBlur: 10,
              shadowColor: 'rgba(0, 0, 0, 0.08)',
            },
            name: 'main-box',
            draggable: true,
          });

          // 姓名
          group.addShape('text', {
            attrs: {
              textBaseline: 'top',
              x: 0,
              y: -20,
              text: name,
              fontSize: 16,
              fontWeight: 'bold',
              textAlign: 'center',
              fill: '#262626',
            },
            name: 'name',
          });

          // 職位
          group.addShape('text', {
            attrs: {
              textBaseline: 'top',
              x: 0,
              y: 0,
              text: position,
              fontSize: 14,
              textAlign: 'center',
              fill: '#595959',
            },
            name: 'position',
          });

          // 部門
          group.addShape('text', {
            attrs: {
              textBaseline: 'top',
              x: 0,
              y: 20,
              text: department,
              fontSize: 12,
              textAlign: 'center',
              fill: '#8C8C8C',
            },
            name: 'department',
          });

          return rect;
        },
        setState(name: string, value: any, item: any) {
          const group = item.getContainer();
          const shape = group.get('children')[0];
          if (name === 'selected') {
            if (value) {
              shape.attr('stroke', '#FF6B6B');
              shape.attr('lineWidth', 2);
            } else {
              shape.attr('stroke', '#5B8FF9');
              shape.attr('lineWidth', 1);
            }
          }
        },
      },
      'single-node'
    );
  }

  private bindEvents(): void {
    // 節點點擊事件
    this.graph.on('node:click', (evt: any) => {
      const node = evt.item;
      this.graph.setItemState(this.selectedNode, 'selected', false);
      this.graph.setItemState(node, 'selected', true);
      this.selectedNode = node;
    });

    // 右鍵選單
    this.graph.on('node:contextmenu', (evt: any) => {
      evt.preventDefault();
      evt.stopPropagation();
      
      this.selectedNode = evt.item;
      this.contextMenuX = evt.canvasX;
      this.contextMenuY = evt.canvasY;
      this.showContextMenu = true;
    });

    // 畫布點擊關閉選單
    this.graph.on('canvas:click', () => {
      this.showContextMenu = false;
    });

    // 視窗大小改變時調整圖表
    window.addEventListener('resize', () => {
      const container = document.getElementById('g6-container');
      if (container && this.graph) {
        const width = container.scrollWidth;
        const height = container.scrollHeight;
        this.graph.changeSize(width, height);
      }
    });
  }

  fitView(): void {
    this.graph?.fitView();
  }

  downloadImage(): void {
    this.graph?.downloadFullImage('組織架構圖', 'image/png');
  }

  changeLayout(): void {
    this.graph?.updateLayout({
      type: this.layoutType,
      direction: this.layoutType === 'mindmap' ? 'H' : 'TB',
    });
  }

  changeTheme(): void {
    // 根據主題更換節點和邊的樣式
    const themes: any = {
      modern: {
        node: { stroke: '#5B8FF9', fill: '#fff' },
        edge: { stroke: '#CED4D9' }
      },
      classic: {
        node: { stroke: '#87e8de', fill: '#f6ffed' },
        edge: { stroke: '#b5b5b5' }
      },
      dark: {
        node: { stroke: '#434343', fill: '#262626' },
        edge: { stroke: '#434343' }
      },
      colorful: {
        node: { stroke: '#ff6b6b', fill: '#ffe0e0' },
        edge: { stroke: '#ffa940' }
      }
    };

    const theme = themes[this.theme];
    const nodes = this.graph?.getNodes() || [];
    const edges = this.graph?.getEdges() || [];

    nodes.forEach((node: any) => {
      this.graph?.updateItem(node, {
        style: theme.node
      });
    });

    edges.forEach((edge: any) => {
      this.graph?.updateItem(edge, {
        style: theme.edge
      });
    });
  }

  expandAll(): void {
    const nodes = this.graph?.getNodes() || [];
    nodes.forEach((node: any) => {
      node.getModel().collapsed = false;
    });
    this.graph?.layout();
  }

  collapseAll(): void {
    const nodes = this.graph?.getNodes() || [];
    nodes.forEach((node: any) => {
      if (node.get('id') !== 'root') {
        node.getModel().collapsed = true;
      }
    });
    this.graph?.layout();
  }

  addChild(): void {
    if (!this.selectedNode) return;
    
    const model = this.selectedNode.getModel();
    const newNode = {
      id: `node-${Date.now()}`,
      name: '新員工',
      position: '職位',
      department: '部門'
    };

    if (!model.children) {
      model.children = [];
    }
    model.children.push(newNode);
    
    this.graph?.updateChild(model, model.id);
    this.showContextMenu = false;
  }

  editNode(): void {
    if (!this.selectedNode) return;
    
    const model = this.selectedNode.getModel();
    this.editingNode = { ...model };
    this.showEditDialog = true;
    this.showContextMenu = false;
  }

  deleteNode(): void {
    if (!this.selectedNode) return;
    
    if (confirm('確定要刪除這個節點嗎？')) {
      this.graph?.removeChild(this.selectedNode.get('id'));
    }
    this.showContextMenu = false;
  }

  expandNode(): void {
    if (!this.selectedNode) return;
    
    const model = this.selectedNode.getModel();
    model.collapsed = !model.collapsed;
    this.graph?.updateChild(model, model.id);
    this.showContextMenu = false;
  }

  saveEdit(): void {
    if (!this.selectedNode) return;
    
    this.graph?.updateItem(this.selectedNode, this.editingNode);
    this.showEditDialog = false;
  }

  cancelEdit(): void {
    this.showEditDialog = false;
  }
}