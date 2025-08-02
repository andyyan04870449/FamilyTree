import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of, Subject } from 'rxjs';
import { map, tap, catchError, takeUntil } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { SYSTEM_ROLES, ROLE_LEVELS, RoleHelper, SystemRole } from '../constants/roles.const';
import { PERMISSIONS, PermissionHelper } from '../constants/permissions.const';

export interface PermissionDefinition {
  resource: string;
  action: string;
  permission: string;
  displayName: string;
  description: string;
  category: string;
  isSystem: boolean;
}

export interface RoleInfo {
  id: string;
  displayName: string;
  description: string;
  level: number;
  isSystem: boolean;
  createdAt: string;
  updatedAt?: string;
  userCount?: number;
}

export interface UserPermissionInfo {
  userId: string;
  username: string;
  email: string;
  fullName: string;
  roles: RoleInfo[];
  permissions: string[];
  directPermissions: string[];
  allPermissions: string[];
  projects: ProjectPermission[];
}

export interface ProjectPermission {
  projectId: string;
  projectName: string;
  role: string;
  permissions: string[];
}

export interface PermissionCheckRequest {
  userId: string;
  permission: string;
  projectId?: string;
  resourceId?: string;
  resourceType?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PermissionService {
  private apiUrl = '/api';
  
  // 快取當前使用者的權限
  private userPermissionsSubject = new BehaviorSubject<string[]>([]);
  public userPermissions$ = this.userPermissionsSubject.asObservable();
  
  private userRolesSubject = new BehaviorSubject<RoleInfo[]>([]);
  public userRoles$ = this.userRolesSubject.asObservable();
  
  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {
    // 監聽登入事件
    this.authService.loginSuccess$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadCurrentUserPermissions();
      });
    
    // 監聽登出事件
    this.authService.logout$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.clearPermissionCache();
      });
    
    // 如果已經登入，載入權限
    if (this.authService.isLoggedIn()) {
      this.loadCurrentUserPermissions();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // 載入當前使用者的權限
  private loadCurrentUserPermissions(): void {
    this.getCurrentUserPermissions().subscribe();
  }

  // 檢查權限
  hasPermission(permission: string): boolean {
    const permissions = this.userPermissionsSubject.value;
    return PermissionHelper.matchesPermission(permissions, permission);
  }

  // 檢查多個權限（任一）
  hasAnyPermission(...permissions: string[]): boolean {
    return permissions.some(permission => this.hasPermission(permission));
  }

  // 檢查多個權限（全部）
  hasAllPermissions(...permissions: string[]): boolean {
    return permissions.every(permission => this.hasPermission(permission));
  }

  // 檢查專案權限
  checkProjectPermission(projectId: string, permission: string): Observable<boolean> {
    return this.http.post<any>(`${this.apiUrl}/permission/check`, {
      permission,
      projectId
    }).pipe(
      map(response => response.data.hasPermission),
      catchError(() => of(false))
    );
  }

  // 取得當前使用者的權限
  getCurrentUserPermissions(): Observable<string[]> {
    return this.http.get<any>(`${this.apiUrl}/permission/current`).pipe(
      tap(response => {
        this.userPermissionsSubject.next(response.data.permissions);
        this.userRolesSubject.next(response.data.roles);
      }),
      map(response => response.data.permissions),
      catchError(() => {
        this.userPermissionsSubject.next([]);
        this.userRolesSubject.next([]);
        return of([]);
      })
    );
  }

  // 取得使用者的權限資訊
  getUserPermissions(userId: string): Observable<UserPermissionInfo> {
    return this.http.get<any>(`${this.apiUrl}/permission/user/${userId}`).pipe(
      map(response => {
        const data = response.data;
        return {
          userId: data.userId,
          username: data.username,
          email: data.email,
          fullName: data.fullName,
          roles: data.roles,
          permissions: data.directPermissions || [], // 使用 directPermissions 作為主要的 permissions
          directPermissions: data.directPermissions || [],
          allPermissions: data.allPermissions || [],
          projects: data.projectPermissions ? Object.values(data.projectPermissions) : []
        };
      })
    );
  }

  // 指派角色給使用者
  assignRoleToUser(userId: string, roleId: string): Observable<boolean> {
    return this.http.post<any>(`${this.apiUrl}/permission/user/${userId}/role`, { roleId }).pipe(
      map(response => response.success)
    );
  }

  // 移除使用者的角色
  removeRoleFromUser(userId: string, roleId: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/permission/user/${userId}/role/${roleId}`).pipe(
      map(response => response.success)
    );
  }

  // 設定使用者的額外權限
  setUserPermissions(userId: string, permissions: string[]): Observable<boolean> {
    return this.http.post<any>(`${this.apiUrl}/permission/user/${userId}`, { permissions }).pipe(
      map(response => response.success)
    );
  }

  // 批次設定使用者權限
  batchSetUserPermissions(assignments: Array<{userId: string, permissions: string[]}>): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/permission/batch`, { assignments });
  }

  // 取得所有權限定義
  getAllPermissionDefinitions(): Observable<PermissionDefinition[]> {
    return this.http.get<any>(`${this.apiUrl}/permission/definitions`).pipe(
      map(response => response.data)
    );
  }

  // 取得所有角色
  getAllRoles(): Observable<RoleInfo[]> {
    return this.http.get<any>(`${this.apiUrl}/role`).pipe(
      map(response => response.data)
    );
  }

  // 取得角色詳情
  getRole(roleId: string): Observable<RoleInfo> {
    return this.http.get<any>(`${this.apiUrl}/role/${roleId}`).pipe(
      map(response => response.data)
    );
  }

  // 建立角色
  createRole(role: {
    roleId: string;
    displayName: string;
    description: string;
    level: number;
  }): Observable<boolean> {
    return this.http.post<any>(`${this.apiUrl}/role`, role).pipe(
      map(response => response.success)
    );
  }

  // 更新角色
  updateRole(roleId: string, role: {
    displayName: string;
    description: string;
    level: number;
  }): Observable<boolean> {
    return this.http.put<any>(`${this.apiUrl}/role/${roleId}`, role).pipe(
      map(response => response.success)
    );
  }

  // 刪除角色
  deleteRole(roleId: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/role/${roleId}`).pipe(
      map(response => response.success)
    );
  }

  // 取得角色權限
  getRolePermissions(roleId: string): Observable<string[]> {
    return this.http.get<any>(`${this.apiUrl}/role/${roleId}/permissions`).pipe(
      map(response => response.data)
    );
  }

  // 設定角色權限
  setRolePermissions(roleId: string, permissions: string[]): Observable<boolean> {
    return this.http.post<any>(`${this.apiUrl}/role/${roleId}/permissions`, { permissions }).pipe(
      map(response => response.success)
    );
  }

  // 複製角色
  copyRole(sourceRoleId: string, newRole: {
    newRoleId: string;
    displayName: string;
    description: string;
    level: number;
  }): Observable<boolean> {
    return this.http.post<any>(`${this.apiUrl}/role/${sourceRoleId}/copy`, newRole).pipe(
      map(response => response.success)
    );
  }

  // 取得角色使用者
  getRoleUsers(roleId: string): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/role/${roleId}/users`).pipe(
      map(response => response.data)
    );
  }

  // 重新載入當前使用者權限
  refreshCurrentUserPermissions(): Observable<string[]> {
    return this.getCurrentUserPermissions();
  }

  // 清除權限快取
  clearPermissionCache(): void {
    this.userPermissionsSubject.next([]);
    this.userRolesSubject.next([]);
  }

  // 檢查是否為管理員
  isAdmin(): boolean {
    const roles = this.userRolesSubject.value;
    return roles.some(role => RoleHelper.isAdminLevel(role.id));
  }

  // 檢查是否為超級管理員
  isSuperAdmin(): boolean {
    const roles = this.userRolesSubject.value;
    return roles.some(role => role.id === SYSTEM_ROLES.SUPER_ADMIN);
  }

  // 檢查角色是否達到最低等級
  hasMinimumRoleLevel(requiredLevel: number): boolean {
    const roles = this.userRolesSubject.value;
    return roles.some(role => role.level >= requiredLevel);
  }

  // 檢查是否擁有指定角色
  hasRole(roleId: SystemRole): boolean {
    const roles = this.userRolesSubject.value;
    return roles.some(role => role.id === roleId);
  }

  // 取得當前使用者的最高角色等級
  getCurrentUserLevel(): number {
    const roles = (this as any).userRolesSubject.value || [];
    return roles.length > 0 
      ? Math.max(...roles.map((r: RoleInfo) => r.level))
      : 0;
  }

  // 檢查是否可以管理指定角色
  canManageRole(targetRoleLevel: number): boolean {
    return this.getCurrentUserLevel() > targetRoleLevel;
  }

  // 取得權限分組
  getPermissionsByCategory(): Observable<Map<string, PermissionDefinition[]>> {
    return this.getAllPermissionDefinitions().pipe(
      map(permissions => {
        const grouped = new Map<string, PermissionDefinition[]>();
        permissions.forEach(permission => {
          const category = permission.category || '其他';
          if (!grouped.has(category)) {
            grouped.set(category, []);
          }
          grouped.get(category)!.push(permission);
        });
        return grouped;
      })
    );
  }
}