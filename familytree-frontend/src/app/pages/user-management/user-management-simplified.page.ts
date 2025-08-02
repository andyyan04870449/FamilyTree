import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BehaviorSubject, Subject, combineLatest, map, takeUntil, finalize, switchMap, debounceTime } from 'rxjs';
import { Router } from '@angular/router';
import { UserService, UserModel } from '../../services/user.service';
import { PermissionService } from '../../services/permission.service';
import { ErrorHandlerService } from '../../services/error-handler.service';

interface UserManagementState {
  users: UserModel[];
  loading: boolean;
  searchTerm: string;
  statusFilter: string;
  currentPage: number;
  pageSize: number;
  totalItems: number;
}

@Component({
  selector: 'app-user-management-simplified',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-7xl mx-auto space-y-6">
        <!-- Header -->
        <div class="bg-white rounded-lg shadow p-6">
          <div class="flex justify-between items-center">
            <h1 class="text-2xl font-bold">使用者管理</h1>
            <button 
              *ngIf="canCreateUser$ | async"
              (click)="createUser()"
              class="bg-blue-500 text-white px-4 py-2 rounded hover:bg-blue-600">
              新增使用者
            </button>
          </div>
        </div>

        <!-- Search & Filter -->
        <div class="bg-white rounded-lg shadow p-4 flex gap-4">
          <input 
            type="text"
            placeholder="搜尋使用者..."
            [value]="(state$ | async)?.searchTerm || ''"
            (input)="updateSearch($event)"
            class="flex-1 px-3 py-2 border rounded">
          
          <select 
            [value]="(state$ | async)?.statusFilter || ''"
            (change)="updateStatusFilter($event)"
            class="px-3 py-2 border rounded">
            <option value="">全部狀態</option>
            <option value="active">啟用</option>
            <option value="inactive">停用</option>
          </select>
        </div>

        <!-- Loading -->
        <div *ngIf="(state$ | async)?.loading" class="text-center py-8">
          <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
          <p class="mt-2 text-gray-600">載入中...</p>
        </div>

        <!-- Users Table -->
        <div *ngIf="!(state$ | async)?.loading" class="bg-white rounded-lg shadow overflow-hidden">
          <table class="min-w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">使用者</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">角色</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">狀態</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">最後登入</th>
                <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">操作</th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              <tr *ngFor="let user of filteredUsers$ | async">
                <td class="px-6 py-4 whitespace-nowrap">
                  <div>
                    <div class="text-sm font-medium text-gray-900">{{ user.fullName || user.username }}</div>
                    <div class="text-sm text-gray-500">{{ user.email }}</div>
                  </div>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800">
                    {{ user.role }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span [class]="getStatusClass(user.status)">
                    {{ user.status === 'active' ? '啟用' : '停用' }}
                  </span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {{ user.lastLoginAt || '從未登入' }}
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-center text-sm font-medium">
                  <button 
                    *ngIf="canEditUser$ | async"
                    (click)="editUser(user)"
                    class="text-indigo-600 hover:text-indigo-900 mr-3">
                    編輯
                  </button>
                  <button 
                    *ngIf="canDeleteUser$ | async"
                    (click)="toggleUserStatus(user)"
                    [class]="user.status === 'active' ? 'text-red-600 hover:text-red-900' : 'text-green-600 hover:text-green-900'">
                    {{ user.status === 'active' ? '停用' : '啟用' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div class="bg-white rounded-lg shadow p-4 flex justify-between items-center">
          <div class="text-sm text-gray-700">
            顯示第 {{ ((state$ | async)?.currentPage - 1) * (state$ | async)?.pageSize + 1 }} 
            到 {{ Math.min((state$ | async)?.currentPage * (state$ | async)?.pageSize, (state$ | async)?.totalItems) }} 
            筆，共 {{ (state$ | async)?.totalItems }} 筆
          </div>
          <div class="flex gap-2">
            <button 
              (click)="previousPage()"
              [disabled]="(state$ | async)?.currentPage === 1"
              class="px-3 py-1 border rounded disabled:opacity-50">
              上一頁
            </button>
            <button 
              (click)="nextPage()"
              [disabled]="(state$ | async)?.currentPage >= totalPages"
              class="px-3 py-1 border rounded disabled:opacity-50">
              下一頁
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserManagementSimplifiedComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  private stateSubject = new BehaviorSubject<UserManagementState>({
    users: [],
    loading: false,
    searchTerm: '',
    statusFilter: '',
    currentPage: 1,
    pageSize: 10,
    totalItems: 0
  });

  state$ = this.stateSubject.asObservable();
  
  // 權限控制
  canCreateUser$ = this.permissionService.userPermissions$.pipe(
    map(() => this.permissionService.canManageUsers())
  );
  
  canEditUser$ = this.permissionService.userPermissions$.pipe(
    map(() => this.permissionService.canEditUser())
  );
  
  canDeleteUser$ = this.permissionService.userPermissions$.pipe(
    map(() => this.permissionService.canDeleteUser())
  );

  // 過濾後的使用者列表
  filteredUsers$ = combineLatest([
    this.state$,
    this.state$.pipe(map(s => s.searchTerm)),
    this.state$.pipe(map(s => s.statusFilter))
  ]).pipe(
    map(([state, search, status]) => {
      return state.users.filter(user => {
        const matchesSearch = !search || 
          user.username.toLowerCase().includes(search.toLowerCase()) ||
          user.email.toLowerCase().includes(search.toLowerCase()) ||
          (user.fullName && user.fullName.toLowerCase().includes(search.toLowerCase()));
        
        const matchesStatus = !status || user.status === status;
        
        return matchesSearch && matchesStatus;
      });
    })
  );

  get totalPages(): number {
    const state = this.stateSubject.value;
    return Math.ceil(state.totalItems / state.pageSize);
  }

  Math = Math;

  constructor(
    private userService: UserService,
    private permissionService: PermissionService,
    private errorHandler: ErrorHandlerService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // 頁碼變更時重新載入資料
    this.state$.pipe(
      map(s => ({ page: s.currentPage, pageSize: s.pageSize })),
      debounceTime(300),
      switchMap(({ page, pageSize }) => {
        this.updateState({ loading: true });
        return this.userService.getUsers(page, pageSize).pipe(
          finalize(() => this.updateState({ loading: false }))
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        this.updateState({
          users: response.data,
          totalItems: response.totalCount
        });
      },
      error: (error) => {
        console.error('載入使用者失敗:', error);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private updateState(partial: Partial<UserManagementState>): void {
    this.stateSubject.next({ ...this.stateSubject.value, ...partial });
  }

  updateSearch(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.updateState({ searchTerm: value });
  }

  updateStatusFilter(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.updateState({ statusFilter: value });
  }

  previousPage(): void {
    const currentPage = this.stateSubject.value.currentPage;
    if (currentPage > 1) {
      this.updateState({ currentPage: currentPage - 1 });
    }
  }

  nextPage(): void {
    const currentPage = this.stateSubject.value.currentPage;
    if (currentPage < this.totalPages) {
      this.updateState({ currentPage: currentPage + 1 });
    }
  }

  createUser(): void {
    this.router.navigate(['/users/new']);
  }

  editUser(user: UserModel): void {
    this.router.navigate(['/users', user.id, 'edit']);
  }

  async toggleUserStatus(user: UserModel): Promise<void> {
    const action = user.status === 'active' ? '停用' : '啟用';
    if (!confirm(`確定要${action} ${user.fullName || user.username} 嗎？`)) return;

    const newStatus = user.status === 'active' ? 'inactive' : 'active';
    
    this.userService.updateUser(user.id, { status: newStatus })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.errorHandler.showSuccess(`${action}成功`);
          // 重新載入當前頁
          const state = this.stateSubject.value;
          this.updateState({ currentPage: state.currentPage });
        },
        error: () => {
          this.errorHandler.showError(`${action}失敗`);
        }
      });
  }

  getStatusClass(status: string): string {
    return status === 'active' 
      ? 'px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800'
      : 'px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800';
  }
}