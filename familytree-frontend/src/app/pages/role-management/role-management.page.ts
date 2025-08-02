import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { PermissionService, RoleInfo } from '../../services/permission.service';
import { ToastService } from '../../services/toast.service';
import { HasPermissionDirective, IsAdminDirective } from '../../directives/has-permission.directive';

interface EditingRole extends RoleInfo {
  isEditing?: boolean;
}

@Component({
  selector: 'app-role-management',
  templateUrl: './role-management.page.html',
  styleUrls: ['./role-management.page.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    HasPermissionDirective,
    IsAdminDirective
  ]
})
export class RoleManagementPage implements OnInit, OnDestroy {
  roles: EditingRole[] = [];
  permissions: string[] = [];
  selectedRole: EditingRole | null = null;
  selectedPermissions: Set<string> = new Set();
  
  isLoading = false;
  isCreating = false;
  isSaving = false;
  
  newRole = {
    roleId: '',
    displayName: '',
    description: '',
    level: 50
  };
  
  private destroy$ = new Subject<void>();

  constructor(
    private permissionService: PermissionService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadRoles();
    this.loadPermissions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadRoles(): void {
    this.isLoading = true;
    this.permissionService.getAllRoles()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (roles) => {
          this.roles = roles.map(role => ({ ...role, isEditing: false }));
          this.isLoading = false;
        },
        error: (error) => {
          this.toastService.error('載入角色清單失敗');
          this.isLoading = false;
        }
      });
  }

  private loadPermissions(): void {
    this.permissionService.getAllPermissionDefinitions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (definitions) => {
          this.permissions = definitions.map(def => def.permission);
        },
        error: (error) => {
          this.toastService.error('載入權限清單失敗');
        }
      });
  }

  selectRole(role: EditingRole): void {
    if (this.selectedRole?.id === role.id) {
      this.selectedRole = null;
      this.selectedPermissions.clear();
      return;
    }
    
    this.selectedRole = role;
    this.loadRolePermissions(role.id);
  }

  private loadRolePermissions(roleId: string): void {
    this.permissionService.getRolePermissions(roleId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (permissions) => {
          this.selectedPermissions = new Set(permissions);
        },
        error: (error) => {
          this.toastService.error('載入角色權限失敗');
        }
      });
  }

  togglePermission(permission: string): void {
    if (this.selectedPermissions.has(permission)) {
      this.selectedPermissions.delete(permission);
    } else {
      this.selectedPermissions.add(permission);
    }
  }

  savePermissions(): void {
    if (!this.selectedRole) return;
    
    this.isSaving = true;
    const permissions = Array.from(this.selectedPermissions);
    
    this.permissionService.setRolePermissions(this.selectedRole.id, permissions)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (success) => {
          if (success) {
            this.toastService.success('權限更新成功');
          } else {
            this.toastService.error('權限更新失敗');
          }
          this.isSaving = false;
        },
        error: (error) => {
          this.toastService.error('權限更新失敗');
          this.isSaving = false;
        }
      });
  }

  startCreateRole(): void {
    this.isCreating = true;
    this.newRole = {
      roleId: '',
      displayName: '',
      description: '',
      level: 50
    };
  }

  cancelCreateRole(): void {
    this.isCreating = false;
  }

  createRole(): void {
    if (!this.newRole.roleId || !this.newRole.displayName) {
      this.toastService.error('請填寫角色ID和顯示名稱');
      return;
    }
    
    this.permissionService.createRole(this.newRole)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (success) => {
          if (success) {
            this.toastService.success('角色建立成功');
            this.isCreating = false;
            this.loadRoles();
          } else {
            this.toastService.error('角色建立失敗');
          }
        },
        error: (error) => {
          this.toastService.error('角色建立失敗');
        }
      });
  }

  startEditRole(role: EditingRole): void {
    // 先取消其他編輯中的角色
    this.roles.forEach(r => r.isEditing = false);
    role.isEditing = true;
  }

  cancelEditRole(role: EditingRole): void {
    role.isEditing = false;
    // 重新載入以復原變更
    this.loadRoles();
  }

  updateRole(role: EditingRole): void {
    if (!role.displayName) {
      this.toastService.error('顯示名稱不能為空');
      return;
    }
    
    this.permissionService.updateRole(role.id, {
      displayName: role.displayName,
      description: role.description,
      level: role.level
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (success) => {
        if (success) {
          this.toastService.success('角色更新成功');
          role.isEditing = false;
        } else {
          this.toastService.error('角色更新失敗');
        }
      },
      error: (error) => {
        this.toastService.error('角色更新失敗');
      }
    });
  }

  deleteRole(role: RoleInfo): void {
    if (role.isSystem) {
      this.toastService.error('系統角色無法刪除');
      return;
    }
    
    if (!confirm(`確定要刪除角色「${role.displayName}」嗎？`)) {
      return;
    }
    
    this.permissionService.deleteRole(role.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (success) => {
          if (success) {
            this.toastService.success('角色刪除成功');
            this.loadRoles();
            if (this.selectedRole?.id === role.id) {
              this.selectedRole = null;
              this.selectedPermissions.clear();
            }
          } else {
            this.toastService.error('角色刪除失敗');
          }
        },
        error: (error) => {
          this.toastService.error('角色刪除失敗');
        }
      });
  }

  copyRole(sourceRole: RoleInfo): void {
    const newRoleId = prompt(`請輸入新角色ID（複製自 ${sourceRole.id}）:`);
    if (!newRoleId) return;
    
    const newDisplayName = prompt('請輸入新角色顯示名稱:', `${sourceRole.displayName} (複製)`);
    if (!newDisplayName) return;
    
    this.permissionService.copyRole(sourceRole.id, {
      newRoleId,
      displayName: newDisplayName,
      description: `${sourceRole.description} (複製自 ${sourceRole.displayName})`,
      level: sourceRole.level
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (success) => {
        if (success) {
          this.toastService.success('角色複製成功');
          this.loadRoles();
        } else {
          this.toastService.error('角色複製失敗');
        }
      },
      error: (error) => {
        this.toastService.error('角色複製失敗');
      }
    });
  }

  canManageRole(role: RoleInfo): boolean {
    return !role.isSystem && this.permissionService.canManageRole(role.level);
  }

  getRoleLevelClass(level: number): string {
    if (level >= 90) return 'level-admin';
    if (level >= 50) return 'level-normal';
    return 'level-guest';
  }

  getRoleLevelText(level: number): string {
    if (level >= 90) return '管理員';
    if (level >= 50) return '一般用戶';
    return '訪客';
  }

  getPermissionsByCategory(): Map<string, string[]> {
    const categorized = new Map<string, string[]>();
    
    // 簡單分類（實際應該從權限定義中取得分類）
    this.permissions.forEach(permission => {
      const resource = permission.split(':')[0];
      let category = '其他';
      
      switch (resource) {
        case 'system':
        case 'role':
        case 'log':
          category = '系統管理';
          break;
        case 'user':
          category = '使用者管理';
          break;
        case 'project':
          category = '專案管理';
          break;
        case 'person':
          category = '人員管理';
          break;
        case 'file':
          category = '檔案管理';
          break;
        case 'report':
          category = '報表管理';
          break;
      }
      
      if (!categorized.has(category)) {
        categorized.set(category, []);
      }
      categorized.get(category)!.push(permission);
    });
    
    return categorized;
  }

  getPermissionDisplayName(permission: string): string {
    // 簡單的顯示名稱轉換
    const [resource, action] = permission.split(':');
    const resourceNames: { [key: string]: string } = {
      system: '系統',
      role: '角色',
      log: '日誌',
      user: '使用者',
      project: '專案',
      person: '人員',
      file: '檔案',
      report: '報表'
    };
    
    const actionNames: { [key: string]: string } = {
      '*': '所有權限',
      manage: '管理',
      view: '查看',
      create: '建立',
      read: '讀取',
      update: '更新',
      delete: '刪除',
      upload: '上傳',
      download: '下載',
      export: '匯出',
      manage_members: '管理成員'
    };
    
    const resourceName = resourceNames[resource] || resource;
    const actionName = actionNames[action] || action;
    
    return `${resourceName} - ${actionName}`;
  }
}