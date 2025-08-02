import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { PermissionService, UserPermissionInfo, RoleInfo, PermissionDefinition } from '../../services/permission.service';
import { UserService, UserModel } from '../../services/user.service';
import { ToastService } from '../../services/toast.service';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { LoadingComponent } from '../../components/ui/loading/loading.component';
import { cn } from '../../utils/cn';

interface UserWithPermissions extends UserModel {
  roles: RoleInfo[];
  permissions: string[];
  effectivePermissions: string[];
}

@Component({
  selector: 'app-permission-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    LoadingComponent,
  ],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-7xl mx-auto space-y-6">
        <!-- 頁面標題 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div>
              <h1 class="text-2xl font-bold text-gray-900 flex items-center gap-3">
                <div class="w-10 h-10 bg-green-100 rounded-lg flex items-center justify-center">
                  <svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
                  </svg>
                </div>
                權限設定
              </h1>
              <p class="text-gray-600 mt-1">管理使用者的角色和權限分配</p>
            </div>
            
            <div class="flex gap-3">
              <app-button
                variant="secondary"
                size="md"
                label="使用者管理"
                icon="👥"
                (clicked)="navigateToUserManagement()"
              ></app-button>
              <app-button
                variant="secondary"
                size="md"
                label="角色管理"
                icon="🛡️"
                (clicked)="navigateToRoleManagement()"
              ></app-button>
            </div>
          </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <!-- 左側：使用者清單 -->
          <div class="lg:col-span-1">
            <div class="bg-white rounded-lg shadow-sm border border-gray-200">
              <div class="p-6 border-b border-gray-200">
                <h2 class="text-lg font-semibold text-gray-900 mb-4">使用者清單</h2>
                
                <!-- 搜尋框 -->
                <div class="relative mb-4">
                  <svg class="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
                  </svg>
                  <input 
                    type="text" 
                    placeholder="搜尋使用者..." 
                    [(ngModel)]="searchTerm"
                    (ngModelChange)="onSearchChange($event)"
                    class="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  />
                </div>

                <!-- 篩選條件 -->
                <div class="grid grid-cols-1 gap-3 mb-4">
                  <select 
                    [(ngModel)]="filterRole" 
                    (ngModelChange)="filterUsers()" 
                    class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  >
                    <option value="">所有角色</option>
                    <option value="admin">管理員</option>
                    <option value="user">一般使用者</option>
                  </select>
                  <select 
                    [(ngModel)]="filterStatus" 
                    (ngModelChange)="filterUsers()" 
                    class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent"
                  >
                    <option value="">所有狀態</option>
                    <option value="active">啟用</option>
                    <option value="inactive">停用</option>
                  </select>
                </div>
              </div>

              <!-- 載入中狀態 -->
              <div *ngIf="isLoadingUsers" class="p-8">
                <app-loading 
                  variant="spinner" 
                  size="md"
                  message="載入使用者列表中..."
                ></app-loading>
              </div>

              <!-- 使用者列表 -->
              <div *ngIf="!isLoadingUsers" class="max-h-96 overflow-y-auto">
                <div 
                  *ngFor="let user of filteredUsers; trackBy: trackByUserId" 
                  class="p-4 border-b border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors"
                  [class]="cn(selectedUser?.id === user.id ? 'bg-green-50 border-green-200' : '')"
                  (click)="selectUser(user)"
                >
                  <div class="flex items-center space-x-3">
                    <div class="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center">
                      <span class="text-sm font-medium text-green-600">
                        {{ user.fullName?.charAt(0) || user.username.charAt(0) }}
                      </span>
                    </div>
                    <div class="flex-1 min-w-0">
                      <p class="text-sm font-medium text-gray-900 truncate">{{ user.fullName || user.username }}</p>
                      <p class="text-xs text-gray-500 truncate">{{ user.email }}</p>
                    </div>
                    <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                          [class]="cn(
                            user.status === 'active' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                          )">
                      {{ user.status === 'active' ? '啟用' : '停用' }}
                    </span>
                  </div>
                </div>

                <!-- 空狀態 -->
                <div *ngIf="filteredUsers.length === 0" class="text-center py-8">
                  <div class="text-gray-400 text-4xl mb-2">👤</div>
                  <h3 class="text-sm font-medium text-gray-900 mb-1">找不到符合條件的使用者</h3>
                  <p class="text-xs text-gray-500">請調整搜尋條件後再試</p>
                </div>
              </div>
            </div>
          </div>

          <!-- 右側：權限設定 -->
          <div class="lg:col-span-2">
            <div *ngIf="!selectedUser" class="bg-white rounded-lg shadow-sm border border-gray-200 p-12 text-center">
              <div class="text-gray-400 text-6xl mb-4">🔒</div>
              <h3 class="text-lg font-medium text-gray-900 mb-2">選擇使用者</h3>
              <p class="text-gray-600">請從左側清單選擇一個使用者來管理其權限</p>
            </div>

            <div *ngIf="selectedUser" class="space-y-6">
              <!-- 使用者資訊 -->
              <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <div class="flex items-center space-x-4 mb-4">
                  <div class="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center">
                    <span class="text-lg font-medium text-green-600">
                      {{ selectedUser.fullName?.charAt(0) || selectedUser.username.charAt(0) }}
                    </span>
                  </div>
                  <div>
                    <h3 class="text-lg font-semibold text-gray-900">{{ selectedUser.fullName || selectedUser.username }}</h3>
                    <p class="text-gray-600">{{ selectedUser.email }}</p>
                  </div>
                  <div class="ml-auto">
                    <span class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium"
                          [class]="cn(
                            selectedUser.status === 'active' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                          )">
                      {{ selectedUser.status === 'active' ? '啟用' : '停用' }}
                    </span>
                  </div>
                </div>

                <div class="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span class="text-gray-500">帳號：</span>
                    <span class="text-gray-900">{{ selectedUser.username }}</span>
                  </div>
                  <div>
                    <span class="text-gray-500">角色：</span>
                    <span class="text-gray-900">{{ selectedUser.role || '未設定' }}</span>
                  </div>
                </div>
              </div>

              <!-- 角色分配 -->
              <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h4 class="text-lg font-semibold text-gray-900 mb-4">角色分配</h4>
                
                <div *ngIf="isLoadingPermissions" class="py-8">
                  <app-loading 
                    variant="spinner" 
                    size="md"
                    message="載入權限資料中..."
                  ></app-loading>
                </div>

                <div *ngIf="!isLoadingPermissions" class="space-y-3">
                  <div *ngFor="let role of allRoles" class="flex items-center justify-between p-3 border border-gray-200 rounded-md hover:bg-gray-50">
                    <div class="flex items-center space-x-3">
                      <input
                        type="checkbox"
                        [id]="'role-' + role.id"
                        [checked]="isRoleAssigned(role.id)"
                        (change)="toggleRole(role.id, $event)"
                        class="w-4 h-4 text-green-600 bg-gray-100 border-gray-300 rounded focus:ring-green-500 focus:ring-2"
                      />
                      <label [for]="'role-' + role.id" class="flex-1 cursor-pointer">
                        <div class="font-medium text-gray-900">{{ role.displayName }}</div>
                        <div class="text-sm text-gray-500">{{ role.description }}</div>
                      </label>
                    </div>
                    <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                          [class]="cn(
                            role.level >= 90 ? 'bg-red-100 text-red-800' :
                            role.level >= 50 ? 'bg-blue-100 text-blue-800' :
                            'bg-gray-100 text-gray-800'
                          )">
                      等級 {{ role.level }}
                    </span>
                  </div>
                </div>

                <div class="mt-6 flex justify-end">
                  <app-button
                    variant="primary"
                    label="儲存變更"
                    [disabled]="isSaving"
                    [loading]="isSaving"
                    (clicked)="savePermissions()"
                  ></app-button>
                </div>
              </div>

              <!-- 有效權限檢視 -->
              <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                <h4 class="text-lg font-semibold text-gray-900 mb-4">有效權限</h4>
                
                <div *ngIf="selectedUser.effectivePermissions && selectedUser.effectivePermissions.length > 0" class="space-y-3">
                  <div *ngFor="let category of getPermissionCategories()" class="border border-gray-200 rounded-md">
                    <div class="bg-gray-50 px-4 py-3 border-b border-gray-200">
                      <h5 class="font-medium text-gray-900">{{ category }}</h5>
                    </div>
                    <div class="p-4 space-y-2">
                      <div *ngFor="let permission of getPermissionsByCategory(category)" 
                           class="flex items-center justify-between p-2 bg-gray-50 rounded">
                        <span class="text-sm text-gray-700">{{ permission }}</span>
                        <span class="text-xs text-gray-500 bg-gray-200 px-2 py-1 rounded">有效</span>
                      </div>
                    </div>
                  </div>
                </div>

                <div *ngIf="!selectedUser.effectivePermissions || selectedUser.effectivePermissions.length === 0" 
                     class="text-center py-8 text-gray-500">
                  此使用者尚未獲得任何權限
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class PermissionSettingsPage implements OnInit, OnDestroy {
  // 使用者清單
  users: UserModel[] = [];
  filteredUsers: UserModel[] = [];
  selectedUser: UserWithPermissions | null = null;
  
  // 角色和權限資料
  allRoles: RoleInfo[] = [];
  allPermissions: PermissionDefinition[] = [];
  permissionsByCategory: Map<string, PermissionDefinition[]> = new Map();
  
  // 搜尋和篩選
  searchTerm = '';
  filterRole = '';
  filterStatus = '';
  
  // 載入狀態
  isLoadingUsers = false;
  isLoadingPermissions = false;
  isSaving = false;
  
  // 編輯狀態
  editingUserRoles: Set<string> = new Set();
  editingUserPermissions: Set<string> = new Set();
  hasChanges = false;
  
  // Utility function for class names
  cn = cn;
  
  private destroy$ = new Subject<void>();
  private searchSubject$ = new Subject<string>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private permissionService: PermissionService,
    private userService: UserService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadInitialData();
    this.setupSearch();
    
    // 如果 URL 中有使用者 ID，直接選擇該使用者
    const userId = this.route.snapshot.queryParams['userId'];
    if (userId) {
      this.selectUserById(userId);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadInitialData(): void {
    this.loadUsers();
    this.loadRoles();
    this.loadPermissions();
  }

  private setupSearch(): void {
    this.searchSubject$
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(term => {
        this.searchTerm = term;
        this.filterUsers();
      });
  }

  private loadUsers(): void {
    this.isLoadingUsers = true;
    this.userService.getUsers()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.users = response.data || [];
          this.filterUsers();
          this.isLoadingUsers = false;
        },
        error: (error) => {
          this.toastService.error('載入使用者清單失敗');
          this.isLoadingUsers = false;
        }
      });
  }

  private loadRoles(): void {
    this.permissionService.getAllRoles()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (roles) => {
          this.allRoles = roles;
        },
        error: (error) => {
          this.toastService.error('載入角色清單失敗');
        }
      });
  }

  private loadPermissions(): void {
    this.permissionService.getPermissionsByCategory()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (permissions) => {
          this.permissionsByCategory = permissions;
          this.allPermissions = Array.from(permissions.values()).flat();
        },
        error: (error) => {
          this.toastService.error('載入權限清單失敗');
        }
      });
  }

  onSearchChange(term: string): void {
    this.searchSubject$.next(term);
  }

  filterUsers(): void {
    let filtered = [...this.users];
    
    // 搜尋篩選
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(user => 
        user.username.toLowerCase().includes(term) ||
        user.email.toLowerCase().includes(term) ||
        user.fullName?.toLowerCase().includes(term)
      );
    }
    
    // 角色篩選
    if (this.filterRole) {
      filtered = filtered.filter(user => user.role === this.filterRole);
    }
    
    // 狀態篩選
    if (this.filterStatus) {
      filtered = filtered.filter(user => user.status === this.filterStatus);
    }
    
    this.filteredUsers = filtered;
  }

  selectUser(user: UserModel): void {
    if (this.hasChanges && !confirm('有未儲存的變更，確定要切換使用者嗎？')) {
      return;
    }
    
    this.isLoadingPermissions = true;
    this.selectedUser = null;
    this.hasChanges = false;
    
    this.permissionService.getUserPermissions(user.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (permissionInfo) => {
          this.selectedUser = {
            ...user,
            roles: permissionInfo.roles,
            permissions: permissionInfo.permissions,
            effectivePermissions: this.calculateEffectivePermissions(permissionInfo)
          };
          
          // 初始化編輯狀態
          this.editingUserRoles = new Set(permissionInfo.roles.map(r => r.id));
          this.editingUserPermissions = new Set(permissionInfo.permissions);
          
          this.isLoadingPermissions = false;
        },
        error: (error) => {
          this.toastService.error('載入使用者權限失敗');
          this.isLoadingPermissions = false;
        }
      });
  }

  private selectUserById(userId: string): void {
    const user = this.users.find(u => u.id === userId);
    if (user) {
      this.selectUser(user);
    }
  }

  private calculateEffectivePermissions(permissionInfo: UserPermissionInfo): string[] {
    // 直接使用後端計算好的 allPermissions
    return permissionInfo.allPermissions || [];
  }


  togglePermission(permission: string): void {
    if (this.editingUserPermissions.has(permission)) {
      this.editingUserPermissions.delete(permission);
    } else {
      this.editingUserPermissions.add(permission);
    }
    this.hasChanges = true;
  }

  hasPermissionFromRole(permission: string): boolean {
    if (!this.selectedUser) return false;
    
    // 檢查是否從角色繼承此權限
    return this.selectedUser.effectivePermissions.includes(permission) && 
           !this.selectedUser.permissions.includes(permission);
  }

  savePermissions(): void {
    this.saveChanges();
  }

  async saveChanges(): Promise<void> {
    if (!this.selectedUser || !this.hasChanges) return;
    
    this.isSaving = true;
    
    try {
      // 更新角色
      const currentRoles = new Set(this.selectedUser.roles.map(r => r.id));
      const rolesToAdd = Array.from(this.editingUserRoles).filter(r => !currentRoles.has(r));
      const rolesToRemove = Array.from(currentRoles).filter(r => !this.editingUserRoles.has(r));
      
      // 新增角色
      for (const roleId of rolesToAdd) {
        await this.permissionService.assignRoleToUser(this.selectedUser.id, roleId)
          .toPromise();
      }
      
      // 移除角色
      for (const roleId of rolesToRemove) {
        await this.permissionService.removeRoleFromUser(this.selectedUser.id, roleId)
          .toPromise();
      }
      
      // 更新額外權限
      await this.permissionService.setUserPermissions(
        this.selectedUser.id, 
        Array.from(this.editingUserPermissions)
      ).toPromise();
      
      this.toastService.success('權限設定已儲存');
      this.hasChanges = false;
      
      // 重新載入使用者權限
      this.selectUser(this.selectedUser);
      
    } catch (error) {
      this.toastService.error('儲存權限設定失敗');
    } finally {
      this.isSaving = false;
    }
  }

  cancelChanges(): void {
    if (!this.selectedUser) return;
    
    this.editingUserRoles = new Set(this.selectedUser.roles.map(r => r.id));
    this.editingUserPermissions = new Set(this.selectedUser.permissions);
    this.hasChanges = false;
  }

  canManageUser(user: UserModel): boolean {
    // 檢查是否可以管理此使用者
    if (user.role === 'admin') {
      return this.permissionService.isSuperAdmin();
    }
    return this.permissionService.hasPermission('user:update');
  }

  getRoleLevel(roleId: string): number {
    const role = this.allRoles.find(r => r.id === roleId);
    return role?.level || 0;
  }

  canAssignRole(roleId: string): boolean {
    const roleLevel = this.getRoleLevel(roleId);
    return this.permissionService.canManageRole(roleLevel);
  }

  getPermissionDisplayName(permission: string): string {
    const def = this.allPermissions.find(p => p.permission === permission);
    return def?.displayName || permission;
  }

  getPermissionDescription(permission: string): string {
    const def = this.allPermissions.find(p => p.permission === permission);
    return def?.description || '';
  }

  getUserStatusClass(status: string): string {
    return status === 'active' ? 'status-active' : 'status-inactive';
  }

  getUserStatusText(status: string): string {
    return status === 'active' ? '啟用' : '停用';
  }

  navigateToUserManagement(): void {
    this.router.navigate(['/user-management']);
  }

  navigateToRoleManagement(): void {
    this.router.navigate(['/role-management']);
  }

  // 新增的方法來支持新模板
  trackByUserId(index: number, user: UserModel): string {
    return user.id;
  }

  isRoleAssigned(roleId: string): boolean {
    return this.editingUserRoles.has(roleId);
  }

  toggleRole(roleId: string, event: any): void {
    if (event.target.checked) {
      this.editingUserRoles.add(roleId);
    } else {
      this.editingUserRoles.delete(roleId);
    }
    this.hasChanges = true;
  }

  getPermissionCategories(): string[] {
    if (!this.selectedUser?.effectivePermissions) return [];
    
    const categories = new Set<string>();
    this.selectedUser.effectivePermissions.forEach(permission => {
      const category = this.getPermissionCategory(permission);
      categories.add(category);
    });
    
    return Array.from(categories);
  }

  getPermissionsByCategory(category: string): string[] {
    if (!this.selectedUser?.effectivePermissions) return [];
    
    return this.selectedUser.effectivePermissions.filter(permission => 
      this.getPermissionCategory(permission) === category
    );
  }

  private getPermissionCategory(permission: string): string {
    const [resource] = permission.split(':');
    const categoryMap: { [key: string]: string } = {
      'system': '系統管理',
      'user': '使用者管理',
      'role': '角色管理',
      'project': '專案管理',
      'file': '檔案管理',
      'audit': '稽核管理'
    };
    
    return categoryMap[resource] || '其他';
  }
}