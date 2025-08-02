// 案件管理頁面：用於管理所有案件的主要介面
// 主要功能：案件列表展示、新增案件、編輯案件、刪除案件、搜尋篩選、分頁管理

import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProjectService, Project, CreateProjectRequest, ProjectStatistics } from '../../services/project.service';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { LoadingComponent } from '../../components/ui/loading/loading.component';
import { ModalComponent } from '../../components/ui/modal/modal.component';
import { cn } from '../../utils/cn';

@Component({
  selector: 'app-case-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    LoadingComponent,
    ModalComponent
  ],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-7xl mx-auto space-y-6">
        <!-- 頁面標題 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div>
              <h1 class="text-2xl font-bold text-gray-900 flex items-center gap-3">
                <div class="w-10 h-10 bg-indigo-100 rounded-lg flex items-center justify-center">
                  <svg class="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                  </svg>
                </div>
                案件管理
              </h1>
              <p class="text-gray-600 mt-1">管理所有案件和相關資料</p>
            </div>
            
            <app-button
              variant="primary"
              size="md"
              label="新增案件"
              icon="➕"
              (clicked)="showCreateDialog = true"
            ></app-button>
          </div>
        </div>

        <!-- 搜尋區域 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 xl:grid-cols-5 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">案件名稱</label>
              <input 
                type="text" 
                placeholder="請輸入案件名稱"
                [(ngModel)]="searchTerm"
                (input)="onSearch()"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              >
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">建立人</label>
              <input 
                type="text" 
                placeholder="請輸入建立人"
                [(ngModel)]="creatorFilter"
                (input)="onSearch()"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              >
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">相關人數</label>
              <input 
                type="text" 
                placeholder="請輸入人數"
                [(ngModel)]="memberCountFilter"
                (input)="onSearch()"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              >
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">建立時間</label>
              <input 
                type="text" 
                placeholder="請輸入日期關鍵字"
                [(ngModel)]="dateFilter"
                (input)="onSearch()"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              >
            </div>
            <div class="flex items-end">
              <app-button
                variant="secondary"
                size="md"
                label="搜尋"
                icon="🔍"
                (clicked)="onSearch()"
                class="w-full"
              ></app-button>
            </div>
          </div>
        </div>

        <!-- 載入中 -->
        <div *ngIf="loading" class="bg-white rounded-lg shadow-sm border border-gray-200 p-12">
          <app-loading 
            variant="spinner" 
            size="lg"
            message="載入案件列表中..."
          ></app-loading>
        </div>

        <!-- 案件列表 -->
        <div *ngIf="!loading" class="bg-white rounded-lg shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-200">
            <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
              📋 案件列表
              <span class="bg-gray-100 text-gray-600 text-sm px-2 py-1 rounded-full">{{ statistics?.totalProjects || 0 }}</span>
            </h2>
          </div>
          
          <!-- 案件表格 -->
          <div *ngIf="paginatedCases.length > 0" class="overflow-x-auto">
            <table class="w-full">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-16">項次</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">案件名稱</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">建立人</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">相關人數</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">建立時間</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-80">功能</th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr *ngFor="let case of paginatedCases; let i = index" class="hover:bg-gray-50">
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {{ (currentPage - 1) * pageSize + i + 1 }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <button 
                      class="text-sm font-medium text-indigo-600 hover:text-indigo-900 transition-colors text-left"
                      (click)="openCase(case)"
                    >
                      {{ case.projectName }}
                    </button>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                    {{ getCurrentCreator() }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                      {{ case.memberCount || 0 }} 人
                    </span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {{ formatDate(case.createdAt) }}
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm">
                    <div class="flex gap-2">
                      <app-button
                        variant="secondary"
                        size="sm"
                        label="編輯"
                        (clicked)="editCase(case)"
                      ></app-button>
                      <app-button
                        variant="primary"
                        size="sm"
                        label="上傳"
                        (clicked)="openFileUpload(case)"
                      ></app-button>
                      <app-button
                        variant="view"
                        size="sm"
                        label="分析"
                        (clicked)="openAnalysis(case)"
                      ></app-button>
                      <app-button
                        variant="danger"
                        size="sm"
                        label="刪除"
                        (clicked)="deleteCase(case)"
                      ></app-button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- 空狀態 -->
          <div *ngIf="paginatedCases.length === 0" class="text-center py-12">
            <div class="text-gray-400 text-6xl mb-4">📋</div>
            <h3 class="text-lg font-medium text-gray-900 mb-2">
              {{ searchTerm ? '找不到相符的案件' : '還沒有任何案件' }}
            </h3>
            <p class="text-gray-600">
              {{ searchTerm ? '請試試其他搜尋條件' : '點擊右上角的「新增案件」按鈕來建立第一個案件' }}
            </p>
          </div>
        </div>

        <!-- 分頁控制 -->
        <div *ngIf="totalPages > 1" class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col sm:flex-row justify-between items-center gap-4">
            <div class="text-sm text-gray-600">
              顯示第 {{ (currentPage - 1) * pageSize + 1 }} 至 {{ Math.min(currentPage * pageSize, filteredCases.length) }} 筆，共 {{ filteredCases.length }} 筆資料
            </div>
            
            <div class="flex items-center gap-2">
              <button 
                class="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                [disabled]="currentPage === 1"
                (click)="goToPage(1)"
              >
                ≪
              </button>
              <button 
                class="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                [disabled]="currentPage === 1"
                (click)="goToPage(currentPage - 1)"
              >
                ‹
              </button>
              
              <div class="flex gap-1">
                <button 
                  *ngFor="let page of getPageNumbers()" 
                  class="px-3 py-2 text-sm font-medium rounded-md transition-colors"
                  [class]="cn(
                    page === currentPage 
                      ? 'bg-indigo-600 text-white' 
                      : 'text-gray-700 bg-white border border-gray-300 hover:bg-gray-50'
                  )"
                  (click)="goToPage(page)"
                >
                  {{ page }}
                </button>
              </div>
              
              <button 
                class="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                [disabled]="currentPage === totalPages"
                (click)="goToPage(currentPage + 1)"
              >
                ›
              </button>
              <button 
                class="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                [disabled]="currentPage === totalPages"
                (click)="goToPage(totalPages)"
              >
                ≫
              </button>
              
              <select 
                [(ngModel)]="currentPage" 
                (change)="updatePagination()"
                class="ml-2 px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              >
                <option *ngFor="let page of getAllPageNumbers()" [value]="page">第 {{ page }} 頁</option>
              </select>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 新增案件對話框 -->
    <app-modal
      [isOpen]="showCreateDialog"
      title="新增案件"
      (close)="closeCreateDialog()"
    >
      <div class="space-y-4">
        <div>
          <label for="caseName" class="block text-sm font-medium text-gray-700 mb-2">
            案件名稱 <span class="text-red-500">*</span>
          </label>
          <input 
            id="caseName"
            type="text" 
            [(ngModel)]="caseForm_name" 
            placeholder="輸入案件名稱..."
            maxlength="100"
            class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
          >
        </div>
        
        <div>
          <label for="caseDescription" class="block text-sm font-medium text-gray-700 mb-2">案件描述</label>
          <textarea 
            id="caseDescription"
            [(ngModel)]="caseForm_description" 
            placeholder="輸入案件描述..."
            rows="3"
            maxlength="500"
            class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-vertical"
          ></textarea>
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
          [label]="loading ? '建立中...' : '建立案件'"
          [disabled]="!caseForm_name.trim() || loading"
          [loading]="loading"
          (clicked)="createCase()"
        ></app-button>
      </div>
    </app-modal>

    <!-- 編輯案件對話框 -->
    <app-modal
      [isOpen]="showEditDialog && !!editingCase"
      title="編輯案件"
      (close)="closeEditDialog()"
    >
      <div class="space-y-4">
        <div>
          <label for="editCaseName" class="block text-sm font-medium text-gray-700 mb-2">
            案件名稱 <span class="text-red-500">*</span>
          </label>
          <input 
            id="editCaseName"
            type="text" 
            [(ngModel)]="caseForm_name" 
            placeholder="輸入案件名稱..."
            maxlength="100"
            class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
          >
        </div>
        
        <div>
          <label for="editCaseDescription" class="block text-sm font-medium text-gray-700 mb-2">案件描述</label>
          <textarea 
            id="editCaseDescription"
            [(ngModel)]="caseForm_description" 
            placeholder="輸入案件描述..."
            rows="3"
            maxlength="500"
            class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-vertical"
          ></textarea>
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
          [label]="loading ? '更新中...' : '更新案件'"
          [disabled]="!caseForm_name.trim() || loading"
          [loading]="loading"
          (clicked)="updateCase()"
        ></app-button>
      </div>
    </app-modal>

    <!-- 刪除確認對話框 -->
    <app-modal
      [isOpen]="showDeleteDialog && !!deletingCase"
      title="確認刪除"
      (close)="closeDeleteDialog()"
    >
      <div class="space-y-4">
        <p class="text-gray-700">確定要刪除案件「{{ deletingCase?.projectName }}」嗎？</p>
        <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <div class="flex items-center">
            <div class="text-yellow-600 text-xl mr-3">⚠️</div>
            <div>
              <p class="text-sm text-yellow-800 font-medium">警告</p>
              <p class="text-sm text-yellow-700">此操作無法復原，案件中的所有資料將會被移除。</p>
            </div>
          </div>
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
  `,

})
export class CaseManagementComponent implements OnInit, OnDestroy {
  // Utility function for class names
  cn = cn;
  
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
  creatorFilter = '';
  memberCountFilter = '';
  dateFilter = '';
  
  // 案件資料
  cases: Project[] = [];
  filteredCases: Project[] = [];
  paginatedCases: Project[] = [];
  statistics: ProjectStatistics | null = null;
  
  // 分頁管理
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  
  // 編輯狀態
  editingCase: Project | null = null;
  deletingCase: Project | null = null;
  
  // 表單資料
  caseForm_name = '';
  caseForm_description = '';
  
  // 數學函數
  Math = Math;

  constructor(
    private projectService: ProjectService,
    private authService: AuthService,
    private router: Router
  ) {
    console.log('📋 CaseManagementComponent 初始化');
  }

  ngOnInit(): void {
    this.loadCases();
    this.loadStatistics();
    
    // 訂閱案件列表更新
    this.subscriptions.add(
      this.projectService.projects$.subscribe(projects => {
        console.log('📋 接收到專案列表更新:', projects.length, '個專案');
        this.cases = projects;
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
   * 載入案件列表
   */
  loadCases(): void {
    console.log('📋 載入案件列表');
    this.subscriptions.add(
      this.projectService.getProjects().subscribe({
        next: (response) => {
          console.log('✅ 案件列表載入成功', response.projects.length);
        },
        error: (error) => {
          console.error('❌ 載入案件列表失敗:', error);
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
   * 新增案件
   */
  createCase(): void {
    if (!this.caseForm_name.trim()) {
      return;
    }

    // 使用當前登入用戶的 ID
    const currentUser = this.authService.currentUserValue;
    console.log('🔐 當前用戶:', currentUser);
    console.log('🔐 是否已登入:', this.authService.isLoggedIn());
    
    if (!currentUser || !currentUser.id) {
      console.error('❌ 用戶未登入或缺少 ID，無法建立案件');
      alert('請先登入後再建立案件');
      return;
    }
    
    // 使用真實的用戶 ID
    const userId = currentUser.id;
    
    const request: CreateProjectRequest = {
      projectName: this.caseForm_name.trim(),
      projectDescription: this.caseForm_description.trim() || undefined,
      userId: userId
    };

    console.log('➕ 建立新案件:', request.projectName);
    console.log('📤 完整請求內容:', JSON.stringify(request));
    
    this.subscriptions.add(
      this.projectService.createProject(request).subscribe({
        next: (response) => {
          console.log('✅ 案件建立成功:', response.projectId);
          this.closeCreateDialog();
          this.loadStatistics(); // 重新載入統計
        },
        error: (error) => {
          console.error('❌ 建立案件失敗:', error);
          alert(`建立案件失敗: ${error.message || '未知錯誤'}`);
        }
      })
    );
  }

  /**
   * 編輯案件
   */
  editCase(caseItem: Project): void {
    console.log('✏️ 編輯案件:', caseItem.projectName);
    this.editingCase = caseItem;
    this.caseForm_name = caseItem.projectName;
    this.caseForm_description = caseItem.projectDescription || '';
    this.showEditDialog = true;
  }

  /**
   * 更新案件
   */
  updateCase(): void {
    if (!this.editingCase || !this.caseForm_name.trim()) {
      return;
    }

    const request = {
      projectName: this.caseForm_name.trim(),
      projectDescription: this.caseForm_description.trim() || undefined
    };

    console.log('💾 更新案件:', this.editingCase.id);
    
    this.subscriptions.add(
      this.projectService.updateProject(this.editingCase.id, request).subscribe({
        next: () => {
          console.log('✅ 案件更新成功');
          this.closeEditDialog();
          this.loadStatistics(); // 重新載入統計
          // 注意：專案列表會通過 ProjectService 的 refreshProjects 自動更新
          console.log('📋 等待專案列表自動更新...');
        },
        error: (error) => {
          console.error('❌ 更新案件失敗:', error);
          alert('更新案件失敗，請稍後再試');
        }
      })
    );
  }

  /**
   * 刪除案件
   */
  deleteCase(caseItem: Project): void {
    console.log('🗑️ 準備刪除案件:', caseItem.projectName);
    this.deletingCase = caseItem;
    this.showDeleteDialog = true;
  }

  /**
   * 確認刪除案件
   */
  confirmDelete(): void {
    if (!this.deletingCase) {
      return;
    }

    console.log('🗑️ 確認刪除案件:', this.deletingCase.id);
    
    this.subscriptions.add(
      this.projectService.deleteProject(this.deletingCase.id).subscribe({
        next: () => {
          console.log('✅ 案件刪除成功');
          this.closeDeleteDialog();
          this.loadStatistics(); // 重新載入統計
        },
        error: (error) => {
          console.error('❌ 刪除案件失敗:', error);
        }
      })
    );
  }

  /**
   * 開啟案件 (導航到案件工作區)
   */
  openCase(caseItem: Project): void {
    console.log('🎯 開啟案件:', caseItem.projectName);
    
    // 設置當前專案
    this.projectService.setCurrentProject(caseItem);
    
    // 導航到人員列表頁面（案件主要工作區域）
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
   * 開啟檔案上傳功能
   */
  openFileUpload(caseItem: Project): void {
    console.log('📤 開啟檔案上傳:', caseItem.projectName);
    
    // 設置當前專案
    this.projectService.setCurrentProject(caseItem);
    
    // 導航到檔案上傳頁面
    console.log('🚀 導航到檔案上傳頁面');
    this.router.navigate(['/file-upload']).then(success => {
      if (success) {
        console.log('✅ 檔案上傳導航成功');
      } else {
        console.error('❌ 檔案上傳導航失敗');
      }
    });
  }

  /**
   * 開啟分析功能
   */
  openAnalysis(caseItem: Project): void {
    console.log('📊 開啟關聯分析:', caseItem.projectName);
    
    // 設置當前專案
    this.projectService.setCurrentProject(caseItem);
    
    // 導航到關係圖譜頁面
    this.router.navigate(['/relationship-graph']).then(success => {
      if (success) {
        console.log('✅ 關聯分析導航成功');
      } else {
        console.error('❌ 關聯分析導航失敗');
      }
    });
  }

  /**
   * 搜尋功能
   */
  onSearch(): void {
    console.log('🔍 搜尋案件:', this.searchTerm);
    this.currentPage = 1; // 重置到第一頁
    this.applyFilters();
  }

  /**
   * 清除篩選條件
   */
  clearFilters(): void {
    this.searchTerm = '';
    this.creatorFilter = '';
    this.memberCountFilter = '';
    this.dateFilter = '';
    this.currentPage = 1;
    this.applyFilters();
    console.log('🧹 清除篩選條件');
  }

  /**
   * 篩選功能
   */
  onFilterChange(): void {
    console.log('🏷️ 篩選狀態:', this.selectedStatus);
    this.currentPage = 1; // 重置到第一頁
    this.applyFilters();
  }





  /**
   * 應用篩選條件
   */
  private applyFilters(): void {
    let filtered = [...this.cases];

    // 狀態篩選
    if (this.selectedStatus !== 'all') {
      filtered = filtered.filter(c => c.status === this.selectedStatus);
    }

    // 案件名稱搜尋
    if (this.searchTerm.trim()) {
      const searchLower = this.searchTerm.toLowerCase();
      filtered = filtered.filter(c => 
        c.projectName.toLowerCase().includes(searchLower) ||
        c.projectDescription?.toLowerCase().includes(searchLower)
      );
    }

    // 建立人篩選
    if (this.creatorFilter.trim()) {
      const creatorLower = this.creatorFilter.toLowerCase();
      filtered = filtered.filter(c => 
        this.getCurrentCreator().toLowerCase().includes(creatorLower)
      );
    }

    // 相關人數篩選
    if (this.memberCountFilter.trim()) {
      const memberCount = parseInt(this.memberCountFilter);
      if (!isNaN(memberCount)) {
        filtered = filtered.filter(c => c.memberCount !== undefined && c.memberCount === memberCount);
      }
    }

    // 建立時間篩選
    if (this.dateFilter.trim()) {
      const dateLower = this.dateFilter.toLowerCase();
      filtered = filtered.filter(c => 
        c.createdAt && this.formatDate(c.createdAt).toLowerCase().includes(dateLower)
      );
    }

    this.filteredCases = filtered;
    this.updatePagination();
    console.log('🎯 篩選結果:', this.filteredCases.length, '/', this.cases.length);
  }

  /**
   * 更新分頁資料
   */
  updatePagination(): void {
    this.totalPages = Math.ceil(this.filteredCases.length / this.pageSize);
    
    // 確保當前頁面不超過總頁數
    if (this.currentPage > this.totalPages) {
      this.currentPage = Math.max(1, this.totalPages);
    }
    
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.paginatedCases = this.filteredCases.slice(startIndex, endIndex);
  }

  /**
   * 跳轉到指定頁面
   */
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePagination();
    }
  }

  /**
   * 獲取頁碼數組（顯示部分頁碼）
   */
  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisiblePages = 5;
    const halfVisible = Math.floor(maxVisiblePages / 2);
    
    let startPage = Math.max(1, this.currentPage - halfVisible);
    let endPage = Math.min(this.totalPages, startPage + maxVisiblePages - 1);
    
    // 調整起始頁面，確保顯示足夠的頁碼
    if (endPage - startPage + 1 < maxVisiblePages) {
      startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }
    
    return pages;
  }

  /**
   * 獲取所有頁碼（用於下拉選單）
   */
  getAllPageNumbers(): number[] {
    const pages: number[] = [];
    for (let i = 1; i <= this.totalPages; i++) {
      pages.push(i);
    }
    return pages;
  }

  /**
   * 關閉新增對話框
   */
  closeCreateDialog(): void {
    this.showCreateDialog = false;
    this.caseForm_name = '';
    this.caseForm_description = '';
  }

  /**
   * 關閉編輯對話框
   */
  closeEditDialog(): void {
    this.showEditDialog = false;
    this.editingCase = null;
    this.caseForm_name = '';
    this.caseForm_description = '';
  }

  /**
   * 關閉刪除對話框
   */
  closeDeleteDialog(): void {
    this.showDeleteDialog = false;
    this.deletingCase = null;
  }

  /**
   * 格式化案件狀態
   */
  formatStatus(status: string): string {
    return this.projectService.formatProjectStatus(status);
  }

  /**
   * 格式化日期
   */
  formatDate(dateString: string | undefined): string {
    if (!dateString) {
      return '無日期';
    }
    try {
      const date = new Date(dateString);
      if (isNaN(date.getTime())) {
        return '無效日期';
      }
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, '0');
      const day = String(date.getDate()).padStart(2, '0');
      return `${year}-${month}-${day}`;
    } catch {
      return '無效日期';
    }
  }

  /**
   * 獲取當前建立者 (硬代碼)
   */
  getCurrentCreator(): string {
    // TODO: 未來實現帳號管理後，改為顯示實際用戶名稱
    return 'user'; // 暫時硬代碼
  }
} 