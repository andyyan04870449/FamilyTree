// 使用者帳號管理頁面
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService, UserModel } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { UserSearchComponent, UserSearchFilters } from '../../components/user-search/user-search.component';
import { UserEditDialogComponent, UserEditResult } from '../../components/user-edit-dialog/user-edit-dialog.component';
import { UserTableComponent, UserAction } from '../../components/user-table/user-table.component';
import { PaginationComponent, PaginationConfig, PaginationEvent } from '../../components/pagination/pagination.component';
import { StateDisplayComponent, DisplayState, StateConfig } from '../../components/state-display/state-display.component';
import { PageHeaderComponent, PageHeaderConfig, HeaderAction } from '../../components/page-header/page-header.component';
import { cn } from '../../utils/cn';
import { Subject, takeUntil } from 'rxjs';
import { 
  USER_MANAGEMENT_CONSTANTS, 
  CSS_CLASSES, 
  USER_STATUS_DISPLAY 
} from '../../constants/user-management.constants';

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, UserSearchComponent, UserEditDialogComponent, UserTableComponent, PaginationComponent, StateDisplayComponent, PageHeaderComponent],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-7xl mx-auto space-y-6">
        <!-- Page Header -->
        <app-page-header
          [config]="headerConfig"
          (actionClick)="onHeaderAction($event)"
        ></app-page-header>

        <!-- Search Filters -->
        <app-user-search
          [searchAccount]="searchAccount"
          [searchName]="searchName"
          [selectedStatus]="selectedStatus"
          (filtersChange)="onFiltersChange($event)"
          (search)="onSearch($event)"
          (reset)="onReset()"
        ></app-user-search>

        <!-- State Display (Loading/Error/Empty) -->
        <app-state-display
          [state]="currentState"
          [config]="stateDisplayConfig"
          (action)="onStateAction($event)"
        ></app-state-display>

        <!-- Users Table -->
        <app-user-table
          *ngIf="currentState === 'success'"
          [users]="filteredUsers"
          [currentPage]="currentPage"
          [pageSize]="pageSize"
          [totalItems]="totalItems"
          (userAction)="onUserAction($event)"
        ></app-user-table>

        <!-- Pagination -->
        <app-pagination
          [config]="paginationConfig"
          (paginationChange)="onPaginationChange($event)"
        ></app-pagination>
      </div>
    </div>

    <!-- Edit Dialog -->
    <app-user-edit-dialog
      [isOpen]="isEditDialogOpen"
      [isNewUser]="isNewUser"
      [showRoleField]="currentUser?.role === 'admin'"
      [userData]="editingUser"
      (result)="onEditResult($event)"
    ></app-user-edit-dialog>
  `
})
export class UserManagementComponent implements OnInit, OnDestroy {
  // ================== 常數與工具 ==================
  readonly CSS_CLASSES = CSS_CLASSES;
  readonly constants = USER_MANAGEMENT_CONSTANTS;
  readonly statusDisplay = USER_STATUS_DISPLAY;
  readonly Math = Math;
  readonly cn = cn;

  // 通用錯誤處理
  private handleError = (operation: string, defaultMessage: string) => (err: any) => {
    console.error(`${operation}失敗:`, err);
    const message = err.error?.message || defaultMessage;
    this.error = message;
    this.toastService.error(message);
    this.loading = false;
  };

  // 通用成功處理
  private handleSuccess = (message: string, reload = true) => () => {
    this.toastService.success(message);
    if (reload) this.loadUsers();
  };

  // 通用確認對話框
  private confirmAction(message: string, action: () => void): void {
    if (confirm(message)) {
      action();
    }
  }

  // ================== 組件配置 ==================
  readonly headerConfig: PageHeaderConfig = {
    title: '使用者帳號管理',
    subtitle: '管理系統使用者、權限設定和帳號狀態',
    icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z',
    actions: [
      { id: 'roles', label: '角色管理', route: '/role-management', variant: 'secondary' },
      { id: 'permissions', label: '權限設定', route: '/permission-settings', variant: 'secondary' },
      { id: 'audit', label: '稽核日誌', route: '/audit-logs', variant: 'secondary' },
      { id: 'export', label: '匯出', icon: '📥', action: 'export', variant: 'secondary' },
      { id: 'add', label: '新增使用者', icon: '➕', action: 'add', variant: 'primary' }
    ]
  };

  get stateDisplayConfig(): StateConfig {
    return {
      loading: {
        message: '載入使用者列表中...',
        variant: 'spinner',
        size: 'lg'
      },
      error: {
        title: '載入失敗',
        message: this.error,
        icon: '❌',
        showRetry: true,
        retryLabel: '重試'
      },
      empty: {
        title: '找不到符合條件的使用者',
        message: '請調整搜尋條件後再試',
        icon: '📭',
        showAction: false
      }
    };
  }

  get paginationConfig(): PaginationConfig {
    return {
      currentPage: this.currentPage,
      totalPages: this.totalPages,
      totalItems: this.totalItems,
      pageSize: this.pageSize,
      maxVisiblePages: 5,
      pageSizeOptions: USER_MANAGEMENT_CONSTANTS.PAGE_SIZES
    };
  }

  get currentState(): DisplayState {
    if (this.loading) return 'loading';
    if (this.error) return 'error';
    if (this.filteredUsers.length === 0) return 'empty';
    return 'success';
  }

  // ================== 應用狀態 ==================
  private readonly destroy$ = new Subject<void>();
  readonly currentUser: any = null;

  // ================== 搜尋與篩選狀態 ==================
  searchAccount = '';
  searchName = '';
  selectedStatus = '';

  // ================== 資料狀態 ==================
  users: UserModel[] = [];
  filteredUsers: UserModel[] = [];

  // ================== 分頁狀態 ==================
  currentPage = 1;
  pageSize = USER_MANAGEMENT_CONSTANTS.DEFAULT_PAGE_SIZE;
  totalItems = 0;
  totalPages = 0;

  // ================== UI 狀態 ==================
  loading = false;
  error = '';
  
  // 編輯對話框狀態
  isEditDialogOpen = false;
  editingUser: Partial<UserModel> | null = null;
  isNewUser = false;
  
  constructor(
    private readonly userService: UserService,
    private readonly authService: AuthService,
    private readonly toastService: ToastService,
    private readonly router: Router
  ) {
    console.log('👤 使用者管理頁面初始化');
    // 初始化當前使用者狀態
    (this as any).currentUser = this.authService.currentUserValue;
  }
  
  ngOnInit(): void {
    this.initializeComponent();
  }
  
  ngOnDestroy(): void {
    this.cleanup();
  }

  // ================== 生命週期管理 ==================
  
  private initializeComponent(): void {
    console.log('🚀 初始化使用者管理組件');
    this.loadUsers();
  }

  private cleanup(): void {
    console.log('🧹 清理使用者管理組件資源');
    this.resetState(['loading', 'edit']);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ================== 統一狀態重置方法 ==================
  
  private readonly stateResetters = {
    search: () => {
      this.searchAccount = '';
      this.searchName = '';
      this.selectedStatus = '';
    },
    pagination: () => {
      this.currentPage = 1;
      this.totalItems = 0;
      this.totalPages = 0;
    },
    edit: () => {
      this.isEditDialogOpen = false;
      this.editingUser = null;
      this.isNewUser = false;
    },
    loading: () => {
      this.loading = false;
      this.error = '';
    }
  };

  private resetState(states: (keyof typeof this.stateResetters)[]): void {
    states.forEach(state => this.stateResetters[state]());
  }
  
  // ================== 資料載入管理 ==================
  
  loadUsers(): void {
    this.loading = true;
    this.error = '';
    
    this.userService.getUsers(this.currentPage, this.pageSize)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.users = response.data;
          this.totalItems = response.totalCount;
          this.totalPages = response.totalPages;
          this.applyFilters();
          this.loading = false;
        },
        error: this.handleError('載入使用者', '載入使用者列表失敗')
      });
  }
  
  // 處理搜尋篩選變更
  onFiltersChange(filters: UserSearchFilters): void {
    this.searchAccount = filters.searchAccount;
    this.searchName = filters.searchName;
    this.selectedStatus = filters.selectedStatus;
    this.applyFilters();
  }
  
  // 處理搜尋
  onSearch(filters: UserSearchFilters): void {
    console.log('🔍 執行搜尋');
    this.searchAccount = filters.searchAccount;
    this.searchName = filters.searchName;
    this.selectedStatus = filters.selectedStatus;
    this.currentPage = 1;
    this.applyFilters();
  }
  
  // 處理重置
  onReset(): void {
    console.log('🔄 重置搜尋條件');
    this.resetState(['search']);
    this.currentPage = 1;
    this.applyFilters();
  }
  
  // ================== 搜尋與篩選管理 ==================
  
  // 套用過濾條件 - 優化版本：直接使用 UserModel，無需轉換
  applyFilters(): void {
    this.filteredUsers = this.users.filter(user => {
      // 帳號篩選
      if (this.searchAccount && !user.username.toLowerCase().includes(this.searchAccount.toLowerCase())) {
        return false;
      }
      
      // 姓名篩選
      const displayName = user.fullName || user.username;
      if (this.searchName && !displayName.toLowerCase().includes(this.searchName.toLowerCase())) {
        return false;
      }
      
      // 狀態篩選
      if (this.selectedStatus && user.status !== this.selectedStatus) {
        return false;
      }
      
      return true;
    });
  }
  
  // ================== 分頁管理 ==================
  
  // 頁面導航
  private goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage) {
      this.currentPage = page;
      this.loadUsers();
    }
  }
  
  // ================== 統一事件處理 ==================
  
  // 統一動作處理器
  private readonly actionHandlers = {
    // 頁面動作
    export: () => this.exportData(),
    add: () => this.addNewUser(),
    
    // 使用者動作
    viewPermissions: (user: UserModel) => this.viewPermissions(user),
    edit: (user: UserModel) => this.editUser(user),
    resetPassword: (user: UserModel) => this.resetPassword(user),
    toggleStatus: (user: UserModel) => this.toggleUserStatus(user),
    
    // 狀態動作
    retry: () => this.loadUsers(),
    
    // 分頁動作
    pageChange: (page: number) => this.goToPage(page),
    pageSizeChange: (size: number) => {
      this.pageSize = size as typeof this.pageSize;
      this.currentPage = 1;
      this.applyFilters();
    }
  } as const;

  // 處理頁面標題動作
  onHeaderAction(action: HeaderAction): void {
    if (action.action && this.actionHandlers[action.action as keyof typeof this.actionHandlers]) {
      (this.actionHandlers[action.action as keyof typeof this.actionHandlers] as Function)();
    } else {
      console.log('未處理的標題動作:', action);
    }
  }

  // 處理使用者表格動作
  onUserAction(userAction: UserAction): void {
    const handler = this.actionHandlers[userAction.type as keyof typeof this.actionHandlers] as (user: UserModel) => void;
    if (handler) {
      handler(userAction.user);
    } else {
      console.log('未處理的使用者動作:', userAction);
    }
  }

  // 處理分頁變更
  onPaginationChange(event: PaginationEvent): void {
    if (event.type === 'pageChange' && event.page) {
      this.actionHandlers.pageChange(event.page);
    } else if (event.type === 'pageSizeChange' && event.pageSize) {
      this.actionHandlers.pageSizeChange(event.pageSize);
    }
  }

  // 處理狀態顯示動作  
  onStateAction(action: 'retry' | 'add'): void {
    const handler = this.actionHandlers[action as keyof typeof this.actionHandlers] as () => void;
    if (handler) {
      handler();
    }
  }

  // ================== 使用者操作管理 ==================
  
  // 編輯使用者
  private editUser(user: UserModel): void {
    console.log('✏️ 編輯使用者', user);
    this.editingUser = { ...user };
    this.isNewUser = false;
    this.isEditDialogOpen = true;
  }
  
  // 權限檢視
  viewPermissions(user: UserModel): void {
    console.log('👁️ 檢視權限', user);
    // 導航到權限設定頁面，並傳遞使用者 ID
    this.router.navigate(['/permission-settings'], { 
      queryParams: { userId: user.id } 
    }).then(
      success => console.log('Navigation success:', success),
      error => console.error('Navigation error:', error)
    );
  }
  
  // 重置密碼
  resetPassword(user: UserModel): void {
    console.log('🔐 重置密碼', user);
    this.confirmAction(
      `確定要重置 ${user.fullName || user.username} 的密碼嗎？`,
      () => {
        this.userService.resetPassword(user.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (response) => {
              this.toastService.success(`密碼已重置，新密碼: ${response.data?.temporaryPassword || '請查看系統通知'}`);
            },
            error: this.handleError('重置密碼', '重置密碼失敗')
          });
      }
    );
  }
  
  // 停用/啟用使用者
  toggleUserStatus(user: UserModel): void {
    const isActive = user.status === 'active';
    const action = isActive ? '停用' : '啟用';
    const displayName = user.fullName || user.username;
    
    console.log(`${action}使用者`, user);
    
    this.confirmAction(
      `確定要${action} ${displayName} 嗎？`,
      () => {
        const newStatus = isActive ? 'inactive' : 'active';
        
        this.userService.updateUser(user.id, { status: newStatus })
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: this.handleSuccess(`${action}使用者成功`),
            error: this.handleError(`${action}使用者`, `${action}使用者失敗`)
          });
      }
    );
  }
  
  // ================== 對話框管理 ==================
  
  // 處理編輯對話框結果
  onEditResult(result: UserEditResult): void {
    if (result.action === 'cancel') {
      this.closeEditDialog();
      return;
    }

    if (result.action === 'save' && result.data) {
      this.saveUserData(result.data);
    }
  }

  // 儲存使用者資料
  private saveUserData(data: any): void {
    if (this.isNewUser) {
      // 新增使用者
      const { username, email, fullName, password } = data;
      
      this.userService.registerUser({ username, email, password, fullName })
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.handleSuccess('新增使用者成功')();
            this.closeEditDialog();
          },
          error: this.handleError('新增使用者', '新增使用者失敗')
        });
    } else {
      // 更新使用者
      const { email, fullName, role, status } = data;
      
      this.userService.updateUser(this.editingUser!.id!, { email, fullName, role, status })
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.handleSuccess('更新使用者成功')();
            this.closeEditDialog();
          },
          error: this.handleError('更新使用者', '更新使用者失敗')
        });
    }
  }
  
  // 關閉編輯對話框
  private closeEditDialog(): void {
    this.resetState(['edit']);
  }
  
  // ================== 工具方法 ==================
  
  // 匯出資料
  exportData(): void {
    console.log('📥 匯出使用者資料');
    // TODO: 實作匯出功能
  }
  
  // 新增使用者
  addNewUser(): void {
    console.log('➕ 新增使用者');
    this.editingUser = null; // 對話框組件會自行初始化
    this.isNewUser = true;
    this.isEditDialogOpen = true;
  }
  
}