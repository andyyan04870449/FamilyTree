import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { PermissionService, RoleInfo } from '../../services/permission.service';
import { ToastService } from '../../services/toast.service';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { LoadingComponent } from '../../components/ui/loading/loading.component';
import { ModalComponent } from '../../components/ui/modal/modal.component';
import { HasPermissionDirective, IsAdminDirective } from '../../directives/has-permission.directive';
import { cn } from '../../utils/cn';

interface EditingRole extends RoleInfo {
  isEditing?: boolean;
  permissions?: string[];
}

@Component({
  selector: 'app-role-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    LoadingComponent,
    ModalComponent,
    HasPermissionDirective,
    IsAdminDirective
  ],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-6xl mx-auto space-y-6">
        <!-- 頁面標題 -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div>
              <h1 class="text-2xl font-bold text-gray-900 flex items-center gap-3">
                <div class="w-10 h-10 bg-purple-100 rounded-lg flex items-center justify-center">
                  <svg class="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21l-7-5-7 5V5a2 2 0 012-2h10a2 2 0 012 2v16z"/>
                  </svg>
                </div>
                角色管理
              </h1>
              <p class="text-gray-600 mt-1">管理系統角色和權限分配</p>
            </div>
            
            <app-button
              *appHasPermission="'role:manage'"
              variant="primary"
              size="md"
              label="新增角色"
              icon="➕"
              [disabled]="isCreating"
              (clicked)="startCreateRole()"
            ></app-button>
          </div>
        </div>

        <!-- 新增角色表單 -->
        <div *ngIf="isCreating" class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">新增角色</h3>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">
                角色ID <span class="text-red-500">*</span>
              </label>
              <input 
                type="text" 
                [(ngModel)]="newRole.roleId" 
                placeholder="例如：editor"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
              />
            </div>
            
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">
                顯示名稱 <span class="text-red-500">*</span>
              </label>
              <input 
                type="text" 
                [(ngModel)]="newRole.displayName" 
                placeholder="例如：編輯者"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
              />
            </div>
            
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">權限等級</label>
              <input 
                type="number" 
                [(ngModel)]="newRole.level" 
                min="0" 
                max="100"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
              />
              <p class="text-xs text-gray-500 mt-1">0-49: 訪客, 50-89: 一般用戶, 90-100: 管理員</p>
            </div>
            
            <div class="md:col-span-2">
              <label class="block text-sm font-medium text-gray-700 mb-2">描述</label>
              <textarea 
                [(ngModel)]="newRole.description" 
                placeholder="角色描述..."
                rows="3"
                class="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent resize-vertical"
              ></textarea>
            </div>
          </div>
          
          <div class="flex justify-end gap-3 mt-6">
            <app-button
              variant="secondary"
              label="取消"
              (clicked)="cancelCreate()"
            ></app-button>
            <app-button
              variant="primary"
              label="建立角色"
              [disabled]="!newRole.roleId || !newRole.displayName"
              (clicked)="createRole()"
            ></app-button>
          </div>
        </div>

        <!-- 載入中狀態 -->
        <div *ngIf="loading" class="bg-white rounded-lg shadow-sm border border-gray-200 p-12">
          <app-loading 
            variant="spinner" 
            size="lg"
            message="載入角色列表中..."
          ></app-loading>
        </div>

        <!-- 角色列表 -->
        <div *ngIf="!loading" class="bg-white rounded-lg shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-200">
            <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
              🎭 角色列表
              <span class="bg-gray-100 text-gray-600 text-sm px-2 py-1 rounded-full">{{ roles.length }}</span>
            </h2>
          </div>
          
          <div class="overflow-x-auto">
            <table class="w-full">
              <thead class="bg-gray-50">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">角色</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">等級</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">描述</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">權限數量</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-48">操作</th>
                </tr>
              </thead>
              <tbody class="bg-white divide-y divide-gray-200">
                <tr *ngFor="let role of roles" class="hover:bg-gray-50">
                  <td class="px-6 py-4 whitespace-nowrap">
                    <div class="flex items-center">
                      <div class="w-8 h-8 bg-purple-100 rounded-full flex items-center justify-center mr-3">
                        <span class="text-sm font-medium text-purple-600">{{ role.displayName.charAt(0) }}</span>
                      </div>
                      <div>
                        <div class="text-sm font-medium text-gray-900">{{ role.displayName }}</div>
                        <div class="text-sm text-gray-500">{{ role.id }}</div>
                      </div>
                    </div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                          [class]="cn(
                            role.level >= 90 ? 'bg-red-100 text-red-800' :
                            role.level >= 50 ? 'bg-blue-100 text-blue-800' :
                            'bg-gray-100 text-gray-800'
                          )">
                      {{ role.level }}
                    </span>
                  </td>
                  <td class="px-6 py-4">
                    <div class="text-sm text-gray-900 max-w-xs truncate">{{ role.description || '無描述' }}</div>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span class="text-sm text-gray-600">{{ role.permissions?.length || 0 }} 項</span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm">
                    <div class="flex gap-2">
                      <app-button
                        variant="view"
                        size="sm"
                        label="檢視權限"
                        (clicked)="viewRolePermissions(role)"
                      ></app-button>
                      <app-button
                        *appHasPermission="'role:manage'"
                        variant="secondary"
                        size="sm"
                        label="編輯"
                        (clicked)="editRole(role)"
                      ></app-button>
                      <app-button
                        *appHasPermission="'role:manage'"
                        variant="danger"
                        size="sm"
                        label="刪除"
                        [disabled]="role.id === 'admin' || role.id === 'user'"
                        (clicked)="deleteRole(role)"
                      ></app-button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
            
            <!-- 空狀態 -->
            <div *ngIf="roles.length === 0" class="text-center py-12">
              <div class="text-gray-400 text-6xl mb-4">🎭</div>
              <h3 class="text-lg font-medium text-gray-900 mb-2">尚未設定任何角色</h3>
              <p class="text-gray-600">點擊上方的「新增角色」按鈕開始建立第一個角色</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 權限檢視對話框 -->
    <app-modal
      [isOpen]="isViewingPermissions && !!selectedRole"
      [title]="'檢視角色權限 - ' + (selectedRole?.displayName || '')"
      (close)="closePermissionView()"
    >
      <div class="space-y-4" *ngIf="selectedRole">
        <div class="bg-gray-50 rounded-lg p-4">
          <h4 class="font-medium text-gray-900 mb-2">角色資訊</h4>
          <div class="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span class="text-gray-500">角色ID：</span>
              <span class="text-gray-900">{{ selectedRole.id }}</span>
            </div>
            <div>
              <span class="text-gray-500">權限等級：</span>
              <span class="text-gray-900">{{ selectedRole.level }}</span>
            </div>
          </div>
          <div class="mt-2" *ngIf="selectedRole.description">
            <span class="text-gray-500">描述：</span>
            <span class="text-gray-900">{{ selectedRole.description }}</span>
          </div>
        </div>
        
        <div>
          <h4 class="font-medium text-gray-900 mb-3">角色權限列表</h4>
          <div class="max-h-60 overflow-y-auto">
            <div *ngIf="selectedRole.permissions && selectedRole.permissions.length > 0" 
                 class="space-y-2">
              <div *ngFor="let permission of selectedRole.permissions" 
                   class="flex items-center justify-between p-3 bg-gray-50 rounded-md">
                <span class="text-sm text-gray-700">{{ permission }}</span>
                <span class="text-xs text-gray-500 bg-gray-200 px-2 py-1 rounded">權限</span>
              </div>
            </div>
            <div *ngIf="!selectedRole.permissions || selectedRole.permissions.length === 0" 
                 class="text-center py-8 text-gray-500">
              此角色尚未分配任何權限
            </div>
          </div>
        </div>
      </div>
      
      <div class="flex justify-end mt-6">
        <app-button
          variant="secondary"
          label="關閉"
          (clicked)="closePermissionView()"
        ></app-button>
      </div>
    </app-modal>
  `
})
export class RoleManagementPage implements OnInit, OnDestroy {
  roles: EditingRole[] = [];
  permissions: string[] = [];
  selectedRole: EditingRole | null = null;
  selectedPermissions: Set<string> = new Set();
  
  loading = false;
  isCreating = false;
  isSaving = false;
  isViewingPermissions = false;
  
  // Utility function for class names
  cn = cn;
  
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
    this.loading = true;
    this.permissionService.getAllRoles()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (roles) => {
          this.roles = roles.map(role => ({ ...role, isEditing: false }));
          this.loading = false;
        },
        error: (error) => {
          this.toastService.error('載入角色清單失敗');
          this.loading = false;
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

  // 新增的方法來支持新模板
  viewRolePermissions(role: EditingRole): void {
    this.selectedRole = role;
    this.isViewingPermissions = true;
  }

  closePermissionView(): void {
    this.isViewingPermissions = false;
    this.selectedRole = null;
  }

  editRole(role: EditingRole): void {
    // TODO: 實現編輯功能，可能需要打開編輯對話框
    console.log('編輯角色:', role);
    this.toastService.info('編輯功能開發中');
  }

  cancelCreate(): void {
    this.isCreating = false;
    this.newRole = {
      roleId: '',
      displayName: '',
      description: '',
      level: 50
    };
  }
}