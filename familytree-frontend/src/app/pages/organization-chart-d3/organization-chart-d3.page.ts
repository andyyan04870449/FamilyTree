// 使用 D3.js 實現的組織圖頁面
// 提供流暢的動畫效果和優雅的視覺呈現

import { Component, OnInit, AfterViewInit, OnDestroy, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import * as d3 from 'd3';

interface TreeNode {
  name: string;
  position: string;
  department: string;
  email?: string;
  phone?: string;
  avatar?: string;
  children?: TreeNode[];
  _children?: TreeNode[]; // 用於收合/展開
  x?: number;
  y?: number;
  parent?: any;
}

@Component({
  selector: 'app-organization-chart-d3',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="organization-chart-d3-page">
      <!-- 頁面標題 -->
      <div class="page-header">
        <div class="header-content">
          <h1>組織架構圖</h1>
          <p>使用 D3.js 實現的互動式組織圖</p>
        </div>
        <div class="header-actions">
          <button class="btn btn-primary" (click)="centerView()">
            <span class="icon">🎯</span>
            置中顯示
          </button>
          <button class="btn btn-secondary" (click)="exportSVG()">
            <span class="icon">📸</span>
            匯出 SVG
          </button>
        </div>
      </div>

      <!-- 控制面板 -->
      <div class="control-panel">
        <div class="control-group">
          <label>縮放：</label>
          <button class="zoom-btn" (click)="zoomIn()">➕</button>
          <span class="zoom-level">{{ Math.round(currentZoom * 100) }}%</span>
          <button class="zoom-btn" (click)="zoomOut()">➖</button>
          <button class="zoom-btn" (click)="resetZoom()">🔄</button>
        </div>

        <div class="control-group">
          <label>動畫速度：</label>
          <select [(ngModel)]="animationDuration" (change)="updateAnimationSpeed()">
            <option [value]="200">快速</option>
            <option [value]="500">正常</option>
            <option [value]="1000">慢速</option>
          </select>
        </div>

        <div class="control-group">
          <label>節點樣式：</label>
          <select [(ngModel)]="nodeStyle" (change)="updateNodeStyle()">
            <option value="card">卡片式</option>
            <option value="circle">圓形</option>
            <option value="rect">矩形</option>
          </select>
        </div>

        <div class="control-group">
          <button class="btn btn-sm" (click)="expandAll()">
            <span class="icon">📂</span> 全部展開
          </button>
          <button class="btn btn-sm" (click)="collapseAll()">
            <span class="icon">📁</span> 全部收合
          </button>
        </div>
      </div>

      <!-- 圖表容器 -->
      <div class="chart-container" #chartContainer>
        <svg #svgElement></svg>
        
        <!-- 提示訊息 -->
        <div class="tooltip" #tooltip></div>
      </div>

      <!-- 浮動操作按鈕 -->
      <div class="fab-container">
        <button class="fab" (click)="addNewMember()">
          <span class="icon">➕</span>
        </button>
      </div>
    </div>
  `,
  styleUrls: ['./organization-chart-d3.page.scss']
})
export class OrganizationChartD3Page implements OnInit, AfterViewInit, OnDestroy {
  Math = Math;
  currentZoom = 1;
  animationDuration = 500;
  nodeStyle = 'card';
  
  private svg: any;
  private g: any;
  private tree: any;
  private root: any;
  private width = 0;
  private height = 0;
  private zoom: any;

  // 示例數據
  private treeData: TreeNode = {
    name: '王大明',
    position: '總經理',
    department: '管理層',
    email: 'ceo@company.com',
    phone: '0912-345-678',
    children: [
      {
        name: '李小華',
        position: '技術總監',
        department: '技術部',
        email: 'cto@company.com',
        children: [
          {
            name: '張三',
            position: '前端組長',
            department: '前端開發組',
            children: [
              {
                name: '王小明',
                position: '前端工程師',
                department: '前端開發組'
              },
              {
                name: '陳小美',
                position: '前端工程師',
                department: '前端開發組'
              }
            ]
          },
          {
            name: '李四',
            position: '後端組長',
            department: '後端開發組',
            children: [
              {
                name: '劉大',
                position: 'Java工程師',
                department: '後端開發組'
              }
            ]
          }
        ]
      },
      {
        name: '陳美玲',
        position: '人資總監',
        department: '人力資源部',
        email: 'hr@company.com',
        children: [
          {
            name: '林小雨',
            position: '招聘經理',
            department: '人力資源部'
          },
          {
            name: '黃大同',
            position: '培訓經理',
            department: '人力資源部'
          }
        ]
      },
      {
        name: '趙錢孫',
        position: '財務總監',
        department: '財務部',
        email: 'cfo@company.com'
      }
    ]
  };

  constructor(private elementRef: ElementRef) {}

  ngOnInit(): void {
    // 初始化
  }

  ngAfterViewInit(): void {
    this.initChart();
    this.centerView();
  }

  ngOnDestroy(): void {
    // 清理資源
    if (this.svg) {
      this.svg.remove();
    }
  }

  private initChart(): void {
    const container = this.elementRef.nativeElement.querySelector('.chart-container');
    this.width = container.clientWidth;
    this.height = container.clientHeight;

    // 創建 SVG
    this.svg = d3.select(container.querySelector('svg'))
      .attr('width', this.width)
      .attr('height', this.height);

    // 添加背景
    this.svg.append('rect')
      .attr('width', this.width)
      .attr('height', this.height)
      .attr('fill', '#f5f5f5');

    // 創建容器組
    this.g = this.svg.append('g')
      .attr('transform', `translate(${this.width / 2}, 50)`);

    // 創建樹狀佈局
    this.tree = d3.tree()
      .nodeSize([220, 180])
      .separation((a: any, b: any) => a.parent === b.parent ? 1 : 1.2);

    // 設置縮放行為
    this.zoom = d3.zoom()
      .scaleExtent([0.1, 4])
      .on('zoom', (event: any) => {
        this.g.attr('transform', event.transform);
        this.currentZoom = event.transform.k;
      });

    this.svg.call(this.zoom);

    // 初始化根節點
    this.root = d3.hierarchy(this.treeData);
    this.root.x0 = 0;
    this.root.y0 = 0;

    // 展開第一層
    if (this.root.children) {
      this.root.children.forEach((d: any) => {
        if (d.children) {
          d._children = d.children;
          d.children = null;
        }
      });
    }

    // 渲染
    this.update(this.root);
  }

  private update(source: any): void {
    // 計算新的樹狀佈局
    const treeData = this.tree(this.root);
    const nodes = treeData.descendants();
    const links = treeData.links();

    // 節點處理
    const node = this.g.selectAll('g.node')
      .data(nodes, (d: any) => d.id || (d.id = Math.random()));

    // 新增節點
    const nodeEnter = node.enter().append('g')
      .attr('class', 'node')
      .attr('transform', (d: any) => `translate(${source.x0}, ${source.y0})`)
      .on('click', (event: any, d: any) => this.toggle(d));

    // 根據不同樣式添加節點
    this.addNodeByStyle(nodeEnter);

    // 節點過渡動畫
    const nodeUpdate = nodeEnter.merge(node as any);
    nodeUpdate.transition()
      .duration(this.animationDuration)
      .attr('transform', (d: any) => `translate(${d.x}, ${d.y})`);

    // 移除節點
    const nodeExit = node.exit().transition()
      .duration(this.animationDuration)
      .attr('transform', (d: any) => `translate(${source.x}, ${source.y})`)
      .remove();

    nodeExit.select('rect').attr('width', 0).attr('height', 0);
    nodeExit.select('circle').attr('r', 0);
    nodeExit.select('text').style('fill-opacity', 0);

    // 連線處理
    const link = this.g.selectAll('path.link')
      .data(links, (d: any) => d.target.id);

    // 新增連線
    const linkEnter = link.enter().insert('path', 'g')
      .attr('class', 'link')
      .attr('d', (d: any) => {
        const o = { x: source.x0, y: source.y0 };
        return this.diagonal(o, o);
      });

    // 連線過渡動畫
    const linkUpdate = linkEnter.merge(link as any);
    linkUpdate.transition()
      .duration(this.animationDuration)
      .attr('d', (d: any) => this.diagonal(d.source, d.target));

    // 移除連線
    link.exit().transition()
      .duration(this.animationDuration)
      .attr('d', (d: any) => {
        const o = { x: source.x, y: source.y };
        return this.diagonal(o, o);
      })
      .remove();

    // 保存舊位置
    nodes.forEach((d: any) => {
      d.x0 = d.x;
      d.y0 = d.y;
    });
  }

  private addNodeByStyle(nodeEnter: any): void {
    switch (this.nodeStyle) {
      case 'card':
        this.addCardNode(nodeEnter);
        break;
      case 'circle':
        this.addCircleNode(nodeEnter);
        break;
      case 'rect':
        this.addRectNode(nodeEnter);
        break;
    }
  }

  private addCardNode(nodeEnter: any): void {
    // 卡片背景
    nodeEnter.append('rect')
      .attr('class', 'node-rect')
      .attr('x', -100)
      .attr('y', -50)
      .attr('width', 200)
      .attr('height', 100)
      .attr('rx', 8)
      .attr('ry', 8);

    // 頭像或圖標
    nodeEnter.append('circle')
      .attr('class', 'node-avatar')
      .attr('cx', -60)
      .attr('cy', 0)
      .attr('r', 25);

    // 姓名
    nodeEnter.append('text')
      .attr('class', 'node-name')
      .attr('x', -20)
      .attr('y', -15)
      .text((d: any) => d.data.name);

    // 職位
    nodeEnter.append('text')
      .attr('class', 'node-position')
      .attr('x', -20)
      .attr('y', 5)
      .text((d: any) => d.data.position);

    // 部門
    nodeEnter.append('text')
      .attr('class', 'node-department')
      .attr('x', -20)
      .attr('y', 25)
      .text((d: any) => d.data.department);

    // 展開/收合指示器
    nodeEnter.append('circle')
      .attr('class', 'node-indicator')
      .attr('cx', 0)
      .attr('cy', 55)
      .attr('r', 8)
      .style('fill', (d: any) => d._children ? '#52c41a' : '#fff')
      .style('display', (d: any) => d.children || d._children ? 'block' : 'none');
  }

  private addCircleNode(nodeEnter: any): void {
    // 圓形背景
    nodeEnter.append('circle')
      .attr('class', 'node-circle')
      .attr('r', 40);

    // 姓名
    nodeEnter.append('text')
      .attr('class', 'node-name')
      .attr('y', -5)
      .attr('text-anchor', 'middle')
      .text((d: any) => d.data.name);

    // 職位
    nodeEnter.append('text')
      .attr('class', 'node-position')
      .attr('y', 10)
      .attr('text-anchor', 'middle')
      .text((d: any) => d.data.position);
  }

  private addRectNode(nodeEnter: any): void {
    // 矩形背景
    nodeEnter.append('rect')
      .attr('class', 'node-rect')
      .attr('x', -80)
      .attr('y', -30)
      .attr('width', 160)
      .attr('height', 60);

    // 姓名
    nodeEnter.append('text')
      .attr('class', 'node-name')
      .attr('y', -5)
      .attr('text-anchor', 'middle')
      .text((d: any) => d.data.name);

    // 職位
    nodeEnter.append('text')
      .attr('class', 'node-position')
      .attr('y', 15)
      .attr('text-anchor', 'middle')
      .text((d: any) => d.data.position);
  }

  private diagonal(s: any, d: any): string {
    return `M ${s.x} ${s.y}
            C ${s.x} ${(s.y + d.y) / 2},
              ${d.x} ${(s.y + d.y) / 2},
              ${d.x} ${d.y}`;
  }

  private toggle(d: any): void {
    if (d.children) {
      d._children = d.children;
      d.children = null;
    } else {
      d.children = d._children;
      d._children = null;
    }
    this.update(d);
  }

  centerView(): void {
    const bounds = this.g.node().getBBox();
    const fullWidth = this.width;
    const fullHeight = this.height;
    const width = bounds.width;
    const height = bounds.height;
    const midX = bounds.x + width / 2;
    const midY = bounds.y + height / 2;
    
    const scale = 0.8 / Math.max(width / fullWidth, height / fullHeight);
    const translate = [fullWidth / 2 - scale * midX, fullHeight / 2 - scale * midY];

    this.svg.transition()
      .duration(750)
      .call(this.zoom.transform, d3.zoomIdentity
        .translate(translate[0], translate[1])
        .scale(scale));
  }

  zoomIn(): void {
    this.svg.transition().call(this.zoom.scaleBy, 1.3);
  }

  zoomOut(): void {
    this.svg.transition().call(this.zoom.scaleBy, 0.7);
  }

  resetZoom(): void {
    this.svg.transition().call(this.zoom.transform, d3.zoomIdentity);
    this.currentZoom = 1;
  }

  expandAll(): void {
    this.expand(this.root);
    this.update(this.root);
  }

  collapseAll(): void {
    this.collapse(this.root);
    this.update(this.root);
  }

  private expand(d: any): void {
    if (d._children) {
      d.children = d._children;
      d._children = null;
    }
    if (d.children) {
      d.children.forEach((child: any) => this.expand(child));
    }
  }

  private collapse(d: any): void {
    if (d.children) {
      d._children = d.children;
      d._children.forEach((child: any) => this.collapse(child));
      d.children = null;
    }
  }

  updateAnimationSpeed(): void {
    // 動畫速度已透過 ngModel 自動更新
  }

  updateNodeStyle(): void {
    // 清除現有節點並重新渲染
    this.g.selectAll('g.node').remove();
    this.g.selectAll('path.link').remove();
    this.update(this.root);
  }

  exportSVG(): void {
    const svgData = this.svg.node().outerHTML;
    const blob = new Blob([svgData], { type: 'image/svg+xml' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = '組織架構圖.svg';
    link.click();
    URL.revokeObjectURL(url);
  }

  addNewMember(): void {
    // 實現新增成員功能
    console.log('新增成員功能待實現');
  }
}