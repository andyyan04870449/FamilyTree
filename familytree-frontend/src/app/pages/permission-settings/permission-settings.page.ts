import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { PermissionService, UserPermissionInfo, RoleInfo, PermissionDefinition } from '../../services/permission.service';
import { UserService, UserModel } from '../../services/user.service';
import { ToastService } from '../../services/toast.service';
import { HasPermissionDirective } from '../../directives/has-permission.directive';

interface UserWithPermissions extends UserModel {
  roles: RoleInfo[];
  permissions: string[];
  effectivePermissions: string[];
}

@Component({
  selector: 'app-permission-settings',
  templateUrl: './permission-settings.page.html',
  styleUrls: ['./permission-settings.page.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HasPermissionDirective
  ]
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

  toggleRole(roleId: string): void {
    if (this.editingUserRoles.has(roleId)) {
      this.editingUserRoles.delete(roleId);
    } else {
      this.editingUserRoles.add(roleId);
    }
    this.hasChanges = true;
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
}