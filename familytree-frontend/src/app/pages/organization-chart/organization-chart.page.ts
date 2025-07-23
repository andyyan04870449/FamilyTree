// 組織圖頁面：用於視覺化顯示組織架構圖
// 主要功能：階層式人員卡片顯示、關係連線、人員管理操作

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// 組織圖節點接口
interface OrgNode {
  id: string;
  name: string;
  position?: string;
  passport?: string;
  birthday?: string;
  avatar?: string;
  children?: OrgNode[];
  parentId?: string;
}

@Component({
  selector: 'app-organization-chart',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="organization-chart-page">
      <!-- 頁面標題 -->
      <div class="page-header">
        <h1>🏢 組織圖</h1>
        <p>視覺化組織架構與人員階層關係</p>
      </div>

      <!-- 功能操作區 -->
      <div class="toolbar-section">
        <div class="toolbar-left">
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

      <!-- 組織圖統計資訊 -->
      <div class="stats-section">
        <div class="stat-item">
          <span class="stat-number">{{ totalMembers }}</span>
          <span class="stat-label">總人員</span>
        </div>
        <div class="stat-item">
          <span class="stat-number">{{ hierarchyLevels }}</span>
          <span class="stat-label">層級數</span>
        </div>
        <div class="stat-item">
          <span class="stat-number">{{ departments }}</span>
          <span class="stat-label">部門數</span>
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
        <div class="chart-viewport">
          <!-- 組織圖將在這裡渲染 -->
          <div class="org-chart-wrapper">
            
            <!-- 簡化的組織圖示例 -->
            <div class="simple-org-chart">
              
              <!-- 根節點 -->
              <div class="org-node root-node">
                <div class="node-card">
                  <div class="node-avatar">王</div>
                  <div class="node-info">
                    <h3>王大明</h3>
                    <p>執行長</p>
                    <span>A000000000</span>
                  </div>
                </div>
              </div>

              <!-- 連接線 -->
              <div class="connection-lines">
                <div class="vertical-line"></div>
                <div class="horizontal-line"></div>
              </div>

              <!-- 子節點容器 -->
              <div class="child-nodes">
                <div class="org-node child-node">
                  <div class="node-card">
                    <div class="node-avatar">李</div>
                    <div class="node-info">
                      <h3>李小美</h3>
                      <p>技術總監</p>
                      <span>B987654321</span>
                    </div>
                  </div>
                </div>

                <div class="org-node child-node">
                  <div class="node-card">
                    <div class="node-avatar">張</div>
                    <div class="node-info">
                      <h3>張三</h3>
                      <p>營運總監</p>
                      <span>C456789012</span>
                    </div>
                  </div>
                </div>
              </div>

            </div>
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
      </div>

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
export class OrganizationChartComponent implements OnInit {
  
  // 狀態管理
  loading = false;
  error = '';
  
  // 統計資料
  totalMembers = 3;
  hierarchyLevels = 2;
  departments = 2;
  
  // 組織資料
  organizationData: OrgNode[] = [];
  
  constructor() {
    console.log('🏢 OrganizationChartComponent 初始化');
  }

  ngOnInit(): void {
    this.loadOrganizationData();
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
   * 新增人員
   */
  addNewPerson(): void {
    console.log('👤 新增人員');
    // TODO: 開啟新增人員對話框
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
    // TODO: 實現全螢幕功能
  }

  /**
   * 放大圖表
   */
  zoomIn(): void {
    console.log('🔍+ 放大圖表');
    // TODO: 實現圖表縮放
  }

  /**
   * 縮小圖表
   */
  zoomOut(): void {
    console.log('🔍− 縮小圖表');
    // TODO: 實現圖表縮放
  }

  /**
   * 重置縮放
   */
  resetZoom(): void {
    console.log('⌂ 重置縮放');
    // TODO: 實現縮放重置
  }

  /**
   * 置中顯示
   */
  centerChart(): void {
    console.log('⭕ 置中顯示圖表');
    // TODO: 實現圖表置中
  }
} 