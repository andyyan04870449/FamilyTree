// 使用者帳號管理頁面
import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService, UserModel } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { ErrorHandlerService } from '../../services/error-handler.service';
import { PermissionService } from '../../services/permission.service';
import { UserSearchComponent, UserSearchFilters } from '../../components/user-search/user-search.component';
import { UserEditDialogComponent, UserEditResult } from '../../components/user-edit-dialog/user-edit-dialog.component';
import { UserTableComponent, UserAction } from '../../components/user-table/user-table.component';
import { PaginationComponent, PaginationConfig, PaginationEvent } from '../../components/pagination/pagination.component';
import { StateDisplayComponent, DisplayState, StateConfig } from '../../components/state-display/state-display.component';
import { PageHeaderComponent, PageHeaderConfig, HeaderAction } from '../../components/page-header/page-header.component';
import { cn } from '../../utils/cn';
import { BehaviorSubject, Subject, takeUntil, finalize, debounceTime, distinctUntilChanged } from 'rxjs';
import { 
  USER_MANAGEMENT_CONSTANTS, 
  CSS_CLASSES, 
  USER_STATUS_DISPLAY 
} from '../../constants/user-management.constants';

// 載入狀態介面
interface LoadingState {
  users: boolean;
  create: boolean;
  update: boolean;
  delete: boolean;
  search: boolean;
  export: boolean;
  resetPassword: boolean;
}

// 錯誤狀態介面
interface ErrorState {
  users: string | null;
  create: string | null;
  update: string | null;
  delete: string | null;
  search: string | null;
  export: string | null;
}

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, UserSearchComponent, UserEditDialogComponent, UserTableComponent, PaginationComponent, StateDisplayComponent, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <!-- 全域載入遮罩 -->
      <div *ngIf="isAnyOperationLoading()" 
           class="fixed inset-0 bg-black bg-opacity-30 z-50 flex items-center justify-center">
        <div class="bg-white rounded-lg p-6 shadow-xl">
          <div class="flex items-center space-x-3">
            <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            <span class="text-gray-700">{{ getCurrentLoadingMessage() }}</span>
          </div>
        </div>
      </div>

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
      [showRoleField]="permissionService.isAdmin()"
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

  // ================== 載入狀態管理 ==================
  private loadingState: LoadingState = {
    users: false,
    create: false,
    update: false,
    delete: false,
    search: false,
    export: false,
    resetPassword: false
  };

  private errorState: ErrorState = {
    users: null,
    create: null,
    update: null,
    delete: null,
    search: null,
    export: null
  };

  // 搜尋防抖主題
  private searchSubject = new Subject<UserSearchFilters>();

  // 通用錯誤處理
  private handleError = (operation: keyof ErrorState, defaultMessage: string) => (err: any) => {
    console.error(`${operation}失敗:`, err);
    const message = err.error?.message || defaultMessage;
    this.errorState[operation] = message;
    this.error = message;
    this.errorHandler.showError(message);
    this.setLoading(operation as keyof LoadingState, false);
    this.cdr.markForCheck();
  };

  // 通用成功處理
  private handleSuccess = (message: string, reload = true) => () => {
    this.errorHandler.showSuccess(message);
    if (reload) this.loadUsers();
  };

  // 通用確認對話框
  private confirmAction(message: string, action: () => void): void {
    if (confirm(message)) {
      action();
    }
  }

  // ================== 組件配置 ==================
  get headerConfig(): PageHeaderConfig {
    const actions = [];
    
    // 根據權限動態顯示按鈕
    if (this.permissionService.hasPermission('ROLE_MANAGEMENT')) {
      actions.push({ id: 'roles', label: '角色管理', route: '/role-management', variant: 'secondary' });
    }
    if (this.permissionService.hasPermission('PERMISSION_MANAGEMENT')) {
      actions.push({ id: 'permissions', label: '權限設定', route: '/permission-settings', variant: 'secondary' });
    }
    if (this.permissionService.hasPermission('AUDIT_LOG_VIEW')) {
      actions.push({ id: 'audit', label: '稽核日誌', route: '/audit-logs', variant: 'secondary' });
    }
    if (this.permissionService.hasPermission('USER_EXPORT')) {
      actions.push({ id: 'export', label: '匯出', icon: '📥', action: 'export', variant: 'secondary' });
    }
    if (this.permissionService.canManageUsers()) {
      actions.push({ id: 'add', label: '新增使用者', icon: '➕', action: 'add', variant: 'primary' });
    }
    
    return {
      title: '使用者帳號管理',
      subtitle: '管理系統使用者、權限設定和帳號狀態',
      icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z',
      actions: actions as any
    };
  }

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
    if (this.isLoading('users')) return 'loading';
    if (this.error) return 'error';
    if (this.filteredUsers.length === 0) return 'empty';
    return 'success';
  }

  // ================== 應用狀態 ==================
  private readonly destroy$ = new Subject<void>();
  readonly currentUser: any = null;
  
  // 權限狀態
  get canCreateUser(): boolean {
    return this.permissionService.canManageUsers();
  }
  
  get canEditUser(): boolean {
    return this.permissionService.canEditUser();
  }
  
  get canDeleteUser(): boolean {
    return this.permissionService.canDeleteUser();
  }
  
  get canExportUsers(): boolean {
    return this.permissionService.hasPermission('USER_EXPORT');
  }
  
  get canResetPassword(): boolean {
    return this.permissionService.hasPermission('USER_RESET_PASSWORD') || this.permissionService.isAdmin();
  }

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
  
  // 載入狀態輔助方法
  setLoading(key: keyof LoadingState, value: boolean): void {
    this.loadingState[key] = value;
    this.loading = this.isAnyOperationLoading();
    this.cdr.markForCheck();
  }

  isLoading(key: keyof LoadingState): boolean {
    return this.loadingState[key];
  }

  isAnyOperationLoading(): boolean {
    return Object.values(this.loadingState).some(loading => loading);
  }

  getCurrentLoadingMessage(): string {
    if (this.loadingState.users) return '載入使用者資料中...';
    if (this.loadingState.create) return '建立使用者中...';
    if (this.loadingState.update) return '更新使用者中...';
    if (this.loadingState.delete) return '刪除使用者中...';
    if (this.loadingState.search) return '搜尋中...';
    if (this.loadingState.export) return '匯出資料中...';
    if (this.loadingState.resetPassword) return '重置密碼中...';
    return '處理中...';
  }

  hasAnyError(): boolean {
    return Object.values(this.errorState).some(error => error !== null);
  }

  clearErrors(): void {
    Object.keys(this.errorState).forEach(key => {
      this.errorState[key as keyof ErrorState] = null;
    });
    this.error = '';
    this.cdr.markForCheck();
  }
  
  // 編輯對話框狀態
  isEditDialogOpen = false;
  editingUser: Partial<UserModel> | null = null;
  isNewUser = false;
  
  constructor(
    private readonly userService: UserService,
    private readonly authService: AuthService,
    private readonly toastService: ToastService,
    private readonly errorHandler: ErrorHandlerService,
    private readonly permissionService: PermissionService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {
    console.log('👤 使用者管理頁面初始化');
    // 初始化當前使用者狀態
    (this as any).currentUser = this.authService.currentUserValue;
  }
  
  ngOnInit(): void {
    this.initializeComponent();
    this.setupSearchDebounce();
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
    this.setLoading('users', true);
    this.clearErrors();
    
    this.userService.getUsers(this.currentPage, this.pageSize)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.setLoading('users', false))
      )
      .subscribe({
        next: (response) => {
          this.users = response.data;
          this.totalItems = response.totalCount;
          this.totalPages = response.totalPages;
          this.applyFilters();
        },
        error: this.handleError('users', '載入使用者列表失敗')
      });
  }
  
  // 處理搜尋篩選變更
  onFiltersChange(filters: UserSearchFilters): void {
    this.searchAccount = filters.searchAccount;
    this.searchName = filters.searchName;
    this.selectedStatus = filters.selectedStatus;
    this.applyFilters();
  }
  
  // 設定搜尋防抖
  private setupSearchDebounce(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged((prev, curr) => 
        prev.searchAccount === curr.searchAccount &&
        prev.searchName === curr.searchName &&
        prev.selectedStatus === curr.selectedStatus
      ),
      takeUntil(this.destroy$)
    ).subscribe(filters => {
      this.setLoading('search', true);
      this.searchAccount = filters.searchAccount;
      this.searchName = filters.searchName;
      this.selectedStatus = filters.selectedStatus;
      this.currentPage = 1;
      this.applyFilters();
      // 模擬搜尋延遲
      setTimeout(() => this.setLoading('search', false), 300);
    });
  }

  // 處理搜尋
  onSearch(filters: UserSearchFilters): void {
    console.log('🔍 執行搜尋');
    this.searchSubject.next(filters);
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
    this.setLoading('update', true);
    // 導航到權限設定頁面，並傳遞使用者 ID
    this.router.navigate(['/permission-settings'], { 
      queryParams: { userId: user.id } 
    }).then(
      success => {
        console.log('Navigation success:', success);
        this.setLoading('update', false);
      },
      error => {
        console.error('Navigation error:', error);
        this.setLoading('update', false);
        this.errorHandler.showError('無法開啟權限設定頁面');
      }
    );
  }
  
  // 重置密碼
  resetPassword(user: UserModel): void {
    console.log('🔐 重置密碼', user);
    this.confirmAction(
      `確定要重置 ${user.fullName || user.username} 的密碼嗎？`,
      () => {
        this.setLoading('resetPassword', true);
        this.userService.resetPassword(user.id)
          .pipe(
            takeUntil(this.destroy$),
            finalize(() => this.setLoading('resetPassword', false))
          )
          .subscribe({
            next: (response) => {
              this.errorHandler.showSuccess(`密碼已重置，新密碼: ${response.data?.temporaryPassword || '請查看系統通知'}`);
            },
            error: this.handleError('users', '重置密碼失敗')
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
        this.setLoading('update', true);
        
        this.userService.updateUser(user.id, { status: newStatus })
          .pipe(
            takeUntil(this.destroy$),
            finalize(() => this.setLoading('update', false))
          )
          .subscribe({
            next: this.handleSuccess(`${action}使用者成功`),
            error: this.handleError('update', `${action}使用者失敗`)
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
      this.setLoading('create', true);
      
      this.userService.registerUser({ username, email, password, fullName })
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => this.setLoading('create', false))
        )
        .subscribe({
          next: () => {
            this.handleSuccess('新增使用者成功')();
            this.closeEditDialog();
          },
          error: this.handleError('create', '新增使用者失敗')
        });
    } else {
      // 更新使用者
      const { email, fullName, role, status } = data;
      this.setLoading('update', true);
      
      this.userService.updateUser(this.editingUser!.id!, { email, fullName, role, status })
        .pipe(
          takeUntil(this.destroy$),
          finalize(() => this.setLoading('update', false))
        )
        .subscribe({
          next: () => {
            this.handleSuccess('更新使用者成功')();
            this.closeEditDialog();
          },
          error: this.handleError('update', '更新使用者失敗')
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
    this.setLoading('export', true);
    
    // TODO: 實作匯出功能
    // 模擬匯出操作
    setTimeout(() => {
      this.errorHandler.showSuccess('使用者資料匯出成功');
      this.setLoading('export', false);
    }, 2000);
  }
  
  // 新增使用者
  addNewUser(): void {
    console.log('➕ 新增使用者');
    this.editingUser = null; // 對話框組件會自行初始化
    this.isNewUser = true;
    this.isEditDialogOpen = true;
  }
  
}