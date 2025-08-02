// 案件管理頁面：用於管理所有案件的主要介面
// 主要功能：案件列表展示、新增案件、編輯案件、刪除案件、搜尋篩選、分頁管理

import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProjectService, Project, CreateProjectRequest, ProjectStatistics } from '../../services/project.service';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-case-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="case-management-page">
      <!-- 整合的頁面標題和搜尋區域 -->
      <div class="page-header">
        <div class="header-top">
          <div class="header-left">
            <h1>案件管理</h1>
            <div class="page-subtitle">管理所有案件和相關資料</div>
          </div>
          <div class="header-right">
            <button class="btn-new-case" (click)="showCreateDialog = true">新增</button>
          </div>
        </div>
        
        <div class="search-filter-section">
          <div class="search-row">
            <div class="search-group">
              <label>案件名稱</label>
              <input 
                type="text" 
                class="search-input" 
                placeholder="請輸入案件名稱"
                [(ngModel)]="searchTerm"
                (input)="onSearch()"
              >
            </div>
            <div class="search-group">
              <label>建立人</label>
              <input 
                type="text" 
                class="search-input" 
                placeholder="請輸入建立人"
                [(ngModel)]="creatorFilter"
                (input)="onSearch()"
              >
            </div>
            <div class="search-group">
              <label>相關人數</label>
              <input 
                type="text" 
                class="search-input" 
                placeholder="請輸入人數"
                [(ngModel)]="memberCountFilter"
                (input)="onSearch()"
              >
            </div>
            <div class="search-group">
              <label>建立時間</label>
              <input 
                type="text" 
                class="search-input" 
                placeholder="請輸入日期關鍵字"
                [(ngModel)]="dateFilter"
                (input)="onSearch()"
              >
            </div>
            <div class="search-actions">
              <button class="btn-search" (click)="onSearch()">搜尋</button>
            </div>
          </div>
        </div>
      </div>

      <!-- 載入中指示器 -->
      <div *ngIf="loading" class="loading-section">
        <div class="loading-spinner"></div>
        <span>載入中...</span>
      </div>

      <!-- 案件列表 -->
      <div *ngIf="!loading" class="table-section">
        <div class="table-header">
          <span class="table-title">案件列表({{ statistics?.totalProjects || 0 }})</span>
        </div>
        <table class="case-table" *ngIf="paginatedCases.length > 0">
          <thead>
            <tr>
              <th class="col-index">項次</th>
              <th class="col-name">案件名稱</th>
              <th class="col-creator">建立人</th>
              <th class="col-members">相關人數</th>
              <th class="col-date">建立時間</th>
              <th class="col-actions">功能</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let case of paginatedCases; let i = index" class="case-row">
              <td class="col-index">{{ (currentPage - 1) * pageSize + i + 1 }}</td>
              <td class="col-name">
                <div class="case-name-cell" (click)="openCase(case)">
                  <span class="case-title">{{ case.projectName }}</span>
                </div>
              </td>
              <td class="col-creator">{{ getCurrentCreator() }}</td>
              <td class="col-members">{{ case.memberCount }}</td>
              <td class="col-date">{{ formatDate(case.createdAt) }}</td>
              <td class="col-actions">
                <div class="action-buttons">
                  <button 
                    class="btn-action btn-edit" 
                    (click)="editCase(case)"
                    title="編輯">
                    編輯
                  </button>
                  <button 
                    class="btn-action btn-upload" 
                    (click)="openFileUpload(case)"
                    title="上傳">
                    上傳
                  </button>
                  <button 
                    class="btn-action btn-analysis" 
                    (click)="openAnalysis(case)"
                    title="分析">
                    分析
                  </button>
                  <button 
                    class="btn-action btn-delete" 
                    (click)="deleteCase(case)"
                    title="刪除">
                    刪除
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>

        <!-- 空狀態 -->
        <div *ngIf="paginatedCases.length === 0" class="empty-state">
          <div class="empty-icon">📋</div>
          <h3>{{ searchTerm ? '找不到相符的案件' : '還沒有任何案件' }}</h3>
          <p>{{ searchTerm ? '請試試其他搜尋條件' : '點擊右上角的「新增」按鈕來建立第一個案件' }}</p>
        </div>
      </div>

      <!-- 分頁控制 -->
      <div class="pagination-section" *ngIf="totalPages > 1">
        <div class="pagination-info">
          顯示第 {{ (currentPage - 1) * pageSize + 1 }} 至 {{ Math.min(currentPage * pageSize, filteredCases.length) }} 筆，共 {{ filteredCases.length }} 筆資料
        </div>
        
        <div class="pagination-controls">
          <button 
            class="pagination-btn" 
            [disabled]="currentPage === 1"
            (click)="goToPage(1)">
            ≪
          </button>
          <button 
            class="pagination-btn" 
            [disabled]="currentPage === 1"
            (click)="goToPage(currentPage - 1)">
            ‹
          </button>
          
          <span class="page-numbers">
            <button 
              *ngFor="let page of getPageNumbers()" 
              class="pagination-btn page-number"
              [class.active]="page === currentPage"
              (click)="goToPage(page)">
              {{ page }}
            </button>
          </span>
          
          <button 
            class="pagination-btn" 
            [disabled]="currentPage === totalPages"
            (click)="goToPage(currentPage + 1)">
            ›
          </button>
          <button 
            class="pagination-btn" 
            [disabled]="currentPage === totalPages"
            (click)="goToPage(totalPages)">
            ≫
          </button>
          
          <select 
            class="page-selector" 
            [(ngModel)]="currentPage" 
            (change)="updatePagination()">
            <option *ngFor="let page of getAllPageNumbers()" [value]="page">{{ page }}</option>
          </select>
        </div>
      </div>

      <!-- 新增案件對話框 -->
      <div *ngIf="showCreateDialog" class="dialog-overlay" (click)="closeCreateDialog()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h3>新增案件</h3>
            <button (click)="closeCreateDialog()" class="close-btn">×</button>
          </div>
          
          <div class="dialog-content">
            <div class="form-group">
              <label for="caseName">案件名稱 *</label>
              <input 
                id="caseName"
                type="text" 
                [(ngModel)]="caseForm_name" 
                placeholder="輸入案件名稱..."
                class="form-input"
                maxlength="100">
            </div>
            
            <div class="form-group">
              <label for="caseDescription">案件描述</label>
              <textarea 
                id="caseDescription"
                [(ngModel)]="caseForm_description" 
                placeholder="輸入案件描述..."
                class="form-textarea"
                rows="3"
                maxlength="500"></textarea>
            </div>
          </div>
          
          <div class="dialog-actions">
            <button (click)="closeCreateDialog()" class="btn-cancel">取消</button>
            <button 
              (click)="createCase()" 
              [disabled]="!caseForm_name.trim() || loading"
              class="btn-confirm">
              {{ loading ? '建立中...' : '建立案件' }}
            </button>
          </div>
        </div>
      </div>

      <!-- 編輯案件對話框 -->
      <div *ngIf="showEditDialog && editingCase" class="dialog-overlay" (click)="closeEditDialog()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h3>編輯案件</h3>
            <button (click)="closeEditDialog()" class="close-btn">×</button>
          </div>
          
          <div class="dialog-content">
            <div class="form-group">
              <label for="editCaseName">案件名稱 *</label>
              <input 
                id="editCaseName"
                type="text" 
                [(ngModel)]="caseForm_name" 
                placeholder="輸入案件名稱..."
                class="form-input"
                maxlength="100">
            </div>
            
            <div class="form-group">
              <label for="editCaseDescription">案件描述</label>
              <textarea 
                id="editCaseDescription"
                [(ngModel)]="caseForm_description" 
                placeholder="輸入案件描述..."
                class="form-textarea"
                rows="3"
                maxlength="500"></textarea>
            </div>
          </div>
          
          <div class="dialog-actions">
            <button (click)="closeEditDialog()" class="btn-cancel">取消</button>
            <button 
              (click)="updateCase()" 
              [disabled]="!caseForm_name.trim() || loading"
              class="btn-confirm">
              {{ loading ? '更新中...' : '更新案件' }}
            </button>
          </div>
        </div>
      </div>

      <!-- 刪除確認對話框 -->
      <div *ngIf="showDeleteDialog && deletingCase" class="dialog-overlay" (click)="closeDeleteDialog()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h3>確認刪除</h3>
            <button (click)="closeDeleteDialog()" class="close-btn">×</button>
          </div>
          
          <div class="dialog-content">
            <p>確定要刪除案件「{{ deletingCase.projectName }}」嗎？</p>
            <p class="warning-text">⚠️ 此操作無法復原，案件中的所有資料將會被移除。</p>
          </div>
          
          <div class="dialog-actions">
            <button (click)="closeDeleteDialog()" class="btn-cancel">取消</button>
            <button 
              (click)="confirmDelete()" 
              [disabled]="loading"
              class="btn-danger">
              {{ loading ? '刪除中...' : '確認刪除' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./case-management.page.scss']
})
export class CaseManagementComponent implements OnInit, OnDestroy {
  
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