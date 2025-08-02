// 專案管理頁面：用於管理所有家族樹專案
// 主要功能：專案展示、新增專案、編輯專案、刪除專案

import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProjectService, Project, CreateProjectRequest, ProjectStatistics } from '../../services/project.service';
import { AuthService } from '../../services/auth.service';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { LoadingComponent } from '../../components/ui/loading/loading.component';
import { ModalComponent } from '../../components/ui/modal/modal.component';
import { cn } from '../../utils/cn';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-project-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, LoadingComponent, ModalComponent],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-7xl mx-auto space-y-6">
        <!-- 頁面標題 -->
        <div class="text-center mb-8">
          <div class="w-16 h-16 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl flex items-center justify-center mx-auto mb-4">
            <svg class="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"/>
            </svg>
          </div>
          <h1 class="text-4xl font-bold text-gray-900 mb-2">專案管理</h1>
          <p class="text-gray-600">建立和管理您的家族樹專案</p>
        </div>

        <!-- 控制欄 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div class="flex flex-col sm:flex-row gap-3 flex-1">
              <input 
                type="text" 
                [(ngModel)]="searchTerm" 
                (input)="onSearch()"
                placeholder="搜尋專案名稱..." 
                class="flex-1 min-w-0 px-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent">
              
              <select 
                [(ngModel)]="selectedStatus" 
                (change)="onFilterChange()" 
                class="px-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent min-w-32">
                <option value="all">全部狀態</option>
                <option value="active">進行中</option>
                <option value="completed">已完成</option>
                <option value="archived">已封存</option>
              </select>

              <div class="flex gap-2">
                <app-button
                  variant="secondary"
                  size="sm"
                  icon="🔍"
                  title="重新整理"
                  (clicked)="onRefresh()"
                ></app-button>

                <app-button
                  variant="secondary"
                  size="sm"
                  [icon]="viewMode === 'grid' ? '☰' : '▦'"
                  [title]="viewMode === 'grid' ? '切換到列表檢視' : '切換到網格檢視'"
                  (clicked)="toggleViewMode()"
                ></app-button>
              </div>
            </div>

            <app-button
              variant="primary"
              size="md"
              label="新增專案"
              icon="➕"
              (clicked)="showCreateDialog = true"
            ></app-button>
          </div>
        </div>

        <!-- 統計資訊 -->
        <div *ngIf="statistics" class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6 text-center hover:shadow-md transition-shadow">
            <div class="text-3xl font-bold text-blue-600 mb-2">{{ statistics.totalProjects }}</div>
            <div class="text-sm text-gray-600">總專案數</div>
          </div>
          <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6 text-center hover:shadow-md transition-shadow">
            <div class="text-3xl font-bold text-green-600 mb-2">{{ statistics.activeProjects }}</div>
            <div class="text-sm text-gray-600">進行中</div>
          </div>
          <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6 text-center hover:shadow-md transition-shadow">
            <div class="text-3xl font-bold text-purple-600 mb-2">{{ statistics.completedProjects }}</div>
            <div class="text-sm text-gray-600">已完成</div>
          </div>
          <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6 text-center hover:shadow-md transition-shadow">
            <div class="text-3xl font-bold text-orange-600 mb-2">{{ statistics.totalMembers }}</div>
            <div class="text-sm text-gray-600">總成員數</div>
          </div>
        </div>

        <!-- 載入中指示器 -->
        <div *ngIf="loading" class="bg-white rounded-lg shadow-sm border border-gray-200 p-12">
          <app-loading 
            variant="spinner" 
            size="lg"
            message="載入專案列表中..."
          ></app-loading>
        </div>

        <!-- 專案列表 -->
        <div *ngIf="!loading" class="bg-white rounded-lg shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-200">
            <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
              📋 專案列表
              <span class="bg-gray-100 text-gray-600 text-sm px-2 py-1 rounded-full">{{ filteredProjects.length }}</span>
            </h2>
          </div>
          
          <div class="p-6">
            <div [class]="cn(
              viewMode === 'grid' 
                ? 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6'
                : 'space-y-4'
            )">
              <div 
                *ngFor="let project of filteredProjects" 
                class="relative bg-gradient-to-br from-white to-gray-50 rounded-xl border border-gray-200 overflow-hidden hover:shadow-lg transition-all duration-300 cursor-pointer group"
                (click)="openProjectDetail(project)">
                
                <!-- 專案標題區 -->
                <div class="bg-gradient-to-r from-blue-600 to-purple-600 p-4 text-white">
                  <div class="flex justify-between items-start">
                    <h3 class="text-lg font-semibold truncate flex-1 mr-2">{{ project.projectName }}</h3>
                    <div class="flex gap-1 opacity-70 group-hover:opacity-100 transition-opacity">
                      <app-button
                        variant="secondary"
                        size="sm"
                        icon="✏️"
                        title="編輯專案"
                        (clicked)="editProject(project); $event.stopPropagation()"
                        class="!bg-white/20 !text-white hover:!bg-white/30"
                      ></app-button>
                      <app-button
                        variant="danger"
                        size="sm"
                        icon="🗑️"
                        title="刪除專案"
                        (clicked)="deleteProject(project); $event.stopPropagation()"
                        class="!bg-white/20 !text-white hover:!bg-red-500/50"
                      ></app-button>
                    </div>
                  </div>
                </div>

                <!-- 專案內容區 -->
                <div class="p-4 space-y-4">
                  <p class="text-gray-600 text-sm line-clamp-2">
                    {{ project.projectDescription || '暫無描述' }}
                  </p>
                  
                  <div class="flex gap-4 text-sm text-gray-500">
                    <span class="flex items-center gap-1">
                      👥 {{ project.memberCount }} 成員
                    </span>
                    <span class="flex items-center gap-1">
                      🔗 {{ project.relationshipCount }} 關係
                    </span>
                  </div>

                  <div class="flex justify-between items-center">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                          [class]="cn(
                            project.status === 'active' ? 'bg-green-100 text-green-800' :
                            project.status === 'completed' ? 'bg-blue-100 text-blue-800' :
                            project.status === 'archived' ? 'bg-gray-100 text-gray-800' :
                            'bg-yellow-100 text-yellow-800'
                          )">
                      {{ formatStatus(project.status) }}
                    </span>
                    <span class="text-xs text-gray-500">
                      {{ formatDate(project.createdAt) }}
                    </span>
                  </div>

                  <div class="flex flex-wrap gap-1" *ngIf="project.tags && project.tags.length > 0">
                    <span 
                      *ngFor="let tag of project.tags" 
                      class="inline-block bg-blue-100 text-blue-800 text-xs px-2 py-1 rounded-md">
                      {{ tag }}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <!-- 空狀態 -->
            <div *ngIf="filteredProjects.length === 0" class="text-center py-12 col-span-full">
              <div class="text-gray-400 text-6xl mb-4">📋</div>
              <h3 class="text-lg font-medium text-gray-900 mb-2">
                {{ searchTerm ? '找不到相符的專案' : '還沒有任何專案' }}
              </h3>
              <p class="text-gray-600 mb-6 max-w-md mx-auto">
                {{ searchTerm ? '請試試其他搜尋條件' : '點擊上方的「新增專案」按鈕來建立第一個專案' }}
              </p>
              <app-button
                *ngIf="!searchTerm"
                variant="primary"
                label="建立第一個專案"
                icon="➕"
                (clicked)="showCreateDialog = true"
              ></app-button>
            </div>
          </div>
        </div>
      </div>

      <!-- 新增專案對話框 -->
      <app-modal
        [isOpen]="showCreateDialog"
        title="新增專案"
        (close)="closeCreateDialog()"
      >
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">專案名稱 *</label>
            <input 
              type="text" 
              [(ngModel)]="projectForm_name" 
              placeholder="輸入專案名稱..."
              class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              maxlength="100">
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">專案描述</label>
            <textarea 
              [(ngModel)]="projectForm_description" 
              placeholder="輸入專案描述..."
              class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-vertical"
              rows="3"
              maxlength="500"></textarea>
          </div>
        </div>
        
        <div class="flex justify-end gap-3 mt-6">
          <app-button
            variant="secondary"
            label="取消"
            (clicked)="closeCreateDialog()"
          ></app-button>
          <app-button
            variant="primary"
            [label]="loading ? '建立中...' : '建立專案'"
            [disabled]="!projectForm_name.trim() || loading"
            [loading]="loading"
            (clicked)="createProject()"
          ></app-button>
        </div>
      </app-modal>

      <!-- 編輯專案對話框 -->
      <app-modal
        [isOpen]="showEditDialog && !!editingProject"
        title="編輯專案"
        (close)="closeEditDialog()"
      >
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">專案名稱 *</label>
            <input 
              type="text" 
              [(ngModel)]="projectForm_name" 
              placeholder="輸入專案名稱..."
              class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              maxlength="100">
          </div>
          
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">專案描述</label>
            <textarea 
              [(ngModel)]="projectForm_description" 
              placeholder="輸入專案描述..."
              class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-vertical"
              rows="3"
              maxlength="500"></textarea>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">專案狀態</label>
            <select 
              [(ngModel)]="projectForm_status" 
              class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent">
              <option value="active">進行中</option>
              <option value="completed">已完成</option>
              <option value="archived">已封存</option>
            </select>
          </div>
        </div>
        
        <div class="flex justify-end gap-3 mt-6">
          <app-button
            variant="secondary"
            label="取消"
            (clicked)="closeEditDialog()"
          ></app-button>
          <app-button
            variant="primary"
            [label]="loading ? '更新中...' : '更新專案'"
            [disabled]="!projectForm_name.trim() || loading"
            [loading]="loading"
            (clicked)="updateProject()"
          ></app-button>
        </div>
      </app-modal>

      <!-- 刪除確認對話框 -->
      <app-modal
        [isOpen]="showDeleteDialog && !!deletingProject"
        title="確認刪除"
        (close)="closeDeleteDialog()"
      >
        <div class="space-y-4">
          <p class="text-gray-700">
            確定要刪除專案「<strong>{{ deletingProject?.projectName }}</strong>」嗎？
          </p>
          <div class="bg-red-50 border border-red-200 rounded-md p-3">
            <p class="text-red-800 text-sm font-medium flex items-center gap-2">
              <span>⚠️</span>
              此操作無法復原，專案中的所有資料將會被移除。
            </p>
          </div>
        </div>
        
        <div class="flex justify-end gap-3 mt-6">
          <app-button
            variant="secondary"
            label="取消"
            (clicked)="closeDeleteDialog()"
          ></app-button>
          <app-button
            variant="danger"
            [label]="loading ? '刪除中...' : '確認刪除'"
            [disabled]="loading"
            [loading]="loading"
            (clicked)="confirmDelete()"
          ></app-button>
        </div>
      </app-modal>
    </div>
  `
})
export class ProjectManagementComponent implements OnInit, OnDestroy {
  
  // 訂閱管理
  private subscriptions = new Subscription();
  
  // Utility function for class names
  cn = cn;
  
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
    private authService: AuthService,
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

    // 使用當前登入用戶的 ID
    const currentUser = this.authService.currentUserValue;
    if (!currentUser?.id) {
      console.error('❌ 用戶未登入或缺少 ID，無法建立專案');
      alert('請先登入後再建立專案');
      return;
    }
    const userId = currentUser.id;
    
    const request: CreateProjectRequest = {
      projectName: this.projectForm_name.trim(),
      projectDescription: this.projectForm_description.trim() || undefined,
      userId: userId
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
    
    // 導航到人員列表頁面（專案主要工作區域）
    console.log('🚀 導航到人員列表頁面');
    this.router.navigate(['/person-list']).then(success => {
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