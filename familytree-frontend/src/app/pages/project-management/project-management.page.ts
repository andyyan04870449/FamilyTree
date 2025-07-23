// 專案管理頁面：用於管理所有家族樹專案
// 主要功能：專案展示、新增專案、編輯專案、刪除專案

import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProjectService, Project, CreateProjectRequest, ProjectStatistics } from '../../services/project.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-project-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="project-management-page">
      <!-- 頁面標題 -->
      <div class="page-header">
        <h1>專案管理</h1>
      </div>

      <!-- 控制欄 -->
      <div class="controls-section">
        <div class="search-controls">
          <input 
            type="text" 
            [(ngModel)]="searchTerm" 
            (input)="onSearch()"
            placeholder="搜尋專案名稱..." 
            class="search-input">
          
          <select 
            [(ngModel)]="selectedStatus" 
            (change)="onFilterChange()" 
            class="filter-select">
            <option value="all">全部狀態</option>
            <option value="active">進行中</option>
            <option value="completed">已完成</option>
            <option value="archived">已封存</option>
          </select>

          <button 
            (click)="onRefresh()" 
            class="icon-button"
            title="重新整理">
            🔍
          </button>

          <button 
            (click)="toggleViewMode()" 
            class="icon-button"
            [title]="viewMode === 'grid' ? '切換到列表檢視' : '切換到網格檢視'">
            {{ viewMode === 'grid' ? '☰' : '▦' }}
          </button>
        </div>

        <button 
          (click)="showCreateDialog = true" 
          class="create-button">
          + 新增專案
        </button>
      </div>

      <!-- 統計資訊 -->
      <div class="statistics-section" *ngIf="statistics">
        <div class="stat-card">
          <div class="stat-number">{{ statistics.totalProjects }}</div>
          <div class="stat-label">總專案數</div>
        </div>
        <div class="stat-card">
          <div class="stat-number">{{ statistics.activeProjects }}</div>
          <div class="stat-label">進行中</div>
        </div>
        <div class="stat-card">
          <div class="stat-number">{{ statistics.completedProjects }}</div>
          <div class="stat-label">已完成</div>
        </div>
        <div class="stat-card">
          <div class="stat-number">{{ statistics.totalMembers }}</div>
          <div class="stat-label">總成員數</div>
        </div>
      </div>

      <!-- 載入中指示器 -->
      <div *ngIf="loading" class="loading-indicator">
        <div class="spinner"></div>
        <p>載入中...</p>
      </div>

      <!-- 專案列表 -->
      <div *ngIf="!loading" class="projects-section">
        <div [class]="'projects-' + viewMode">
          <div 
            *ngFor="let project of filteredProjects" 
            class="project-card"
            (click)="openProjectDetail(project)">
            
            <div class="project-header">
              <h3>{{ project.projectName }}</h3>
              <div class="project-actions">
                <button 
                  (click)="editProject(project); $event.stopPropagation()" 
                  class="action-button edit"
                  title="編輯專案">
                  ✏️
                </button>
                <button 
                  (click)="deleteProject(project); $event.stopPropagation()" 
                  class="action-button delete"
                  title="刪除專案">
                  🗑️
                </button>
              </div>
            </div>

            <div class="project-content">
              <p class="project-description">{{ project.projectDescription || '暫無描述' }}</p>
              
              <div class="project-stats">
                <span class="stat-item">
                  👥 {{ project.memberCount }} 成員
                </span>
                <span class="stat-item">
                  🔗 {{ project.relationshipCount }} 關係
                </span>
              </div>

              <div class="project-meta">
                <span class="status-badge" [ngClass]="'status-' + project.status">
                  {{ formatStatus(project.status) }}
                </span>
                <span class="created-date">
                  {{ formatDate(project.createdAt) }}
                </span>
              </div>

              <div class="project-tags">
                <span 
                  *ngFor="let tag of project.tags" 
                  class="tag">
                  {{ tag }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- 空狀態 -->
        <div *ngIf="filteredProjects.length === 0" class="empty-state">
          <div class="empty-icon">📋</div>
          <h3>{{ searchTerm ? '找不到相符的專案' : '還沒有任何專案' }}</h3>
          <p>{{ searchTerm ? '請試試其他搜尋條件' : '點擊上方的「新增專案」按鈕來建立第一個專案' }}</p>
          <button 
            *ngIf="!searchTerm" 
            (click)="showCreateDialog = true" 
            class="create-button">
            + 建立第一個專案
          </button>
        </div>
      </div>

      <!-- 新增專案對話框 -->
      <div *ngIf="showCreateDialog" class="dialog-overlay" (click)="closeCreateDialog()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h2>新增專案</h2>
            <button (click)="closeCreateDialog()" class="close-button">×</button>
          </div>
          
          <div class="dialog-content">
            <div class="form-group">
              <label for="projectName">專案名稱 *</label>
              <input 
                id="projectName"
                type="text" 
                [(ngModel)]="projectForm_name" 
                placeholder="輸入專案名稱..."
                class="form-input"
                maxlength="100">
            </div>
            
            <div class="form-group">
              <label for="projectDescription">專案描述</label>
              <textarea 
                id="projectDescription"
                [(ngModel)]="projectForm_description" 
                placeholder="輸入專案描述..."
                class="form-textarea"
                rows="3"
                maxlength="500"></textarea>
            </div>
          </div>
          
          <div class="dialog-footer">
            <button (click)="closeCreateDialog()" class="cancel-button">取消</button>
            <button 
              (click)="createProject()" 
              [disabled]="!projectForm_name.trim() || loading"
              class="create-button">
              {{ loading ? '建立中...' : '建立專案' }}
            </button>
          </div>
        </div>
      </div>

      <!-- 編輯專案對話框 -->
      <div *ngIf="showEditDialog && editingProject" class="dialog-overlay" (click)="closeEditDialog()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h2>編輯專案</h2>
            <button (click)="closeEditDialog()" class="close-button">×</button>
          </div>
          
          <div class="dialog-content">
            <div class="form-group">
              <label for="editProjectName">專案名稱 *</label>
              <input 
                id="editProjectName"
                type="text" 
                [(ngModel)]="projectForm_name" 
                placeholder="輸入專案名稱..."
                class="form-input"
                maxlength="100">
            </div>
            
            <div class="form-group">
              <label for="editProjectDescription">專案描述</label>
              <textarea 
                id="editProjectDescription"
                [(ngModel)]="projectForm_description" 
                placeholder="輸入專案描述..."
                class="form-textarea"
                rows="3"
                maxlength="500"></textarea>
            </div>

            <div class="form-group">
              <label for="editProjectStatus">專案狀態</label>
              <select 
                id="editProjectStatus"
                [(ngModel)]="projectForm_status" 
                class="form-select">
                <option value="active">進行中</option>
                <option value="completed">已完成</option>
                <option value="archived">已封存</option>
              </select>
            </div>
          </div>
          
          <div class="dialog-footer">
            <button (click)="closeEditDialog()" class="cancel-button">取消</button>
            <button 
              (click)="updateProject()" 
              [disabled]="!projectForm_name.trim() || loading"
              class="update-button">
              {{ loading ? '更新中...' : '更新專案' }}
            </button>
          </div>
        </div>
      </div>

      <!-- 刪除確認對話框 -->
      <div *ngIf="showDeleteDialog && deletingProject" class="dialog-overlay" (click)="closeDeleteDialog()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h2>確認刪除</h2>
            <button (click)="closeDeleteDialog()" class="close-button">×</button>
          </div>
          
          <div class="dialog-content">
            <p>確定要刪除專案「{{ deletingProject.projectName }}」嗎？</p>
            <p class="warning-text">⚠️ 此操作無法復原，專案中的所有資料將會被移除。</p>
          </div>
          
          <div class="dialog-footer">
            <button (click)="closeDeleteDialog()" class="cancel-button">取消</button>
            <button 
              (click)="confirmDelete()" 
              [disabled]="loading"
              class="delete-button">
              {{ loading ? '刪除中...' : '確認刪除' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./project-management.page.scss']
})
export class ProjectManagementComponent implements OnInit, OnDestroy {
  
  // 訂閱管理
  private subscriptions = new Subscription();
  
  // 狀態管理
  loading = false;
  showCreateDialog = false;
  showEditDialog = false;
  showDeleteDialog = false;
  
  // 篩選與搜尋
  selectedStatus = 'all';
  searchTerm = '';
  viewMode: 'grid' | 'list' = 'grid';
  
  // 專案資料
  projects: Project[] = [];
  filteredProjects: Project[] = [];
  statistics: ProjectStatistics | null = null;
  
  // 編輯狀態
  editingProject: Project | null = null;
  deletingProject: Project | null = null;
  
  // 表單資料
  projectForm_name = '';
  projectForm_description = '';
  projectForm_status: 'active' | 'completed' | 'archived' = 'active';
  
  constructor(
    private projectService: ProjectService,
    private router: Router
  ) {
    console.log('📋 ProjectManagementComponent 初始化');
  }

  ngOnInit(): void {
    this.loadProjects();
    this.loadStatistics();
    
    // 訂閱專案列表更新
    this.subscriptions.add(
      this.projectService.projects$.subscribe(projects => {
        this.projects = projects;
        this.applyFilters();
      })
    );

    // 訂閱載入狀態
    this.subscriptions.add(
      this.projectService.loading$.subscribe(loading => {
        this.loading = loading;
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  /**
   * 載入專案列表
   */
  loadProjects(): void {
    console.log('📋 載入專案列表');
    this.subscriptions.add(
      this.projectService.getProjects().subscribe({
        next: (response) => {
          console.log('✅ 專案列表載入成功', response.projects.length);
        },
        error: (error) => {
          console.error('❌ 載入專案列表失敗:', error);
          // 可以顯示錯誤訊息
        }
      })
    );
  }

  /**
   * 載入統計資訊
   */
  loadStatistics(): void {
    console.log('📊 載入統計資訊');
    this.subscriptions.add(
      this.projectService.getProjectStatistics().subscribe({
        next: (statistics) => {
          this.statistics = statistics;
          console.log('✅ 統計資訊載入成功');
        },
        error: (error) => {
          console.error('❌ 載入統計資訊失敗:', error);
        }
      })
    );
  }

  /**
   * 新增專案
   */
  createProject(): void {
    if (!this.projectForm_name.trim()) {
      return;
    }

    const request: CreateProjectRequest = {
      projectName: this.projectForm_name.trim(),
      projectDescription: this.projectForm_description.trim() || undefined,
      userId: this.projectService.generateDefaultUserId()
    };

    console.log('➕ 建立新專案:', request.projectName);
    
    this.subscriptions.add(
      this.projectService.createProject(request).subscribe({
        next: (response) => {
          console.log('✅ 專案建立成功:', response.projectId);
          this.closeCreateDialog();
          this.loadStatistics(); // 重新載入統計
        },
        error: (error) => {
          console.error('❌ 建立專案失敗:', error);
          // 可以顯示錯誤訊息
        }
      })
    );
  }

  /**
   * 編輯專案
   */
  editProject(project: Project): void {
    console.log('✏️ 編輯專案:', project.projectName);
    this.editingProject = project;
    this.projectForm_name = project.projectName;
    this.projectForm_description = project.projectDescription || '';
    this.projectForm_status = project.status as 'active' | 'completed' | 'archived';
    this.showEditDialog = true;
  }

  /**
   * 更新專案
   */
  updateProject(): void {
    if (!this.editingProject || !this.projectForm_name.trim()) {
      return;
    }

    const request = {
      projectName: this.projectForm_name.trim(),
      projectDescription: this.projectForm_description.trim() || undefined,
      status: this.projectForm_status
    };

    console.log('💾 更新專案:', this.editingProject.id);
    
    this.subscriptions.add(
      this.projectService.updateProject(this.editingProject.id, request).subscribe({
        next: () => {
          console.log('✅ 專案更新成功');
          this.closeEditDialog();
          this.loadStatistics(); // 重新載入統計
        },
        error: (error) => {
          console.error('❌ 更新專案失敗:', error);
          // 可以顯示錯誤訊息
        }
      })
    );
  }

  /**
   * 刪除專案
   */
  deleteProject(project: Project): void {
    console.log('🗑️ 準備刪除專案:', project.projectName);
    this.deletingProject = project;
    this.showDeleteDialog = true;
  }

  /**
   * 確認刪除專案
   */
  confirmDelete(): void {
    if (!this.deletingProject) {
      return;
    }

    console.log('🗑️ 確認刪除專案:', this.deletingProject.id);
    
    this.subscriptions.add(
      this.projectService.deleteProject(this.deletingProject.id).subscribe({
        next: () => {
          console.log('✅ 專案刪除成功');
          this.closeDeleteDialog();
          this.loadStatistics(); // 重新載入統計
        },
        error: (error) => {
          console.error('❌ 刪除專案失敗:', error);
          // 可以顯示錯誤訊息
        }
      })
    );
  }

  /**
   * 開啟專案詳情 (導航到專案)
   */
  openProjectDetail(project: Project): void {
    console.log('🎯 開啟專案:', project.projectName);
    
    // 設置當前專案
    this.projectService.setCurrentProject(project);
    
    // 導航到家族樹頁面（系統主頁）
    console.log('🚀 導航到家族樹頁面');
    this.router.navigate(['/family-tree']).then(success => {
      if (success) {
        console.log('✅ 導航成功');
      } else {
        console.error('❌ 導航失敗');
      }
    });
  }

  /**
   * 搜尋功能
   */
  onSearch(): void {
    console.log('🔍 搜尋專案:', this.searchTerm);
    this.applyFilters();
  }

  /**
   * 篩選功能
   */
  onFilterChange(): void {
    console.log('🏷️ 篩選狀態:', this.selectedStatus);
    this.applyFilters();
  }

  /**
   * 重新整理
   */
  onRefresh(): void {
    console.log('🔄 重新整理專案列表');
    this.loadProjects();
    this.loadStatistics();
  }

  /**
   * 切換檢視模式
   */
  toggleViewMode(): void {
    this.viewMode = this.viewMode === 'grid' ? 'list' : 'grid';
    console.log('👀 切換檢視模式:', this.viewMode);
  }

  /**
   * 應用篩選條件
   */
  private applyFilters(): void {
    let filtered = [...this.projects];

    // 狀態篩選
    if (this.selectedStatus !== 'all') {
      filtered = filtered.filter(p => p.status === this.selectedStatus);
    }

    // 搜尋篩選
    if (this.searchTerm.trim()) {
      const searchLower = this.searchTerm.toLowerCase();
      filtered = filtered.filter(p => 
        p.projectName.toLowerCase().includes(searchLower) ||
        p.projectDescription?.toLowerCase().includes(searchLower)
      );
    }

    this.filteredProjects = filtered;
    console.log('🎯 篩選結果:', this.filteredProjects.length, '/', this.projects.length);
  }

  /**
   * 關閉新增對話框
   */
  closeCreateDialog(): void {
    this.showCreateDialog = false;
    this.projectForm_name = '';
    this.projectForm_description = '';
  }

  /**
   * 關閉編輯對話框
   */
  closeEditDialog(): void {
    this.showEditDialog = false;
    this.editingProject = null;
    this.projectForm_name = '';
    this.projectForm_description = '';
    this.projectForm_status = 'active';
  }

  /**
   * 關閉刪除對話框
   */
  closeDeleteDialog(): void {
    this.showDeleteDialog = false;
    this.deletingProject = null;
  }

  /**
   * 格式化專案狀態
   */
  formatStatus(status: string): string {
    return this.projectService.formatProjectStatus(status);
  }

  /**
   * 格式化日期
   */
  formatDate(dateString: string): string {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('zh-TW', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
      });
    } catch {
      return '無效日期';
    }
  }
} 