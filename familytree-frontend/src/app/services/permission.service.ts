import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of, Subject } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { SYSTEM_ROLES, ROLE_LEVELS, RoleHelper, SystemRole } from '../constants/roles.const';
import { PERMISSIONS, PermissionHelper } from '../constants/permissions.const';
import { UserInfo } from './auth.service';
import { ErrorHandlerService } from './error-handler.service';
import { BaseApiService } from './base-api.service';

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
export class PermissionService extends BaseApiService {
  private readonly endpoint = 'permission';
  
  // 快取當前使用者的權限
  private userPermissionsSubject = new BehaviorSubject<string[]>([]);
  public userPermissions$ = this.userPermissionsSubject.asObservable();
  
  private userRolesSubject = new BehaviorSubject<RoleInfo[]>([]);
  public userRoles$ = this.userRolesSubject.asObservable();
  
  private currentUserSubject = new BehaviorSubject<UserInfo | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    http: HttpClient,
    private errorHandler: ErrorHandlerService
  ) {
    super(http);
  }

  // 設定當前使用者（由 AuthService 呼叫）
  setCurrentUser(user: UserInfo | null): void {
    this.currentUserSubject.next(user);
    if (user) {
      this.loadCurrentUserPermissions();
    } else {
      this.clearPermissionCache();
    }
  }

  // 清除使用者資訊（由 AuthService 呼叫）
  clearUser(): void {
    this.currentUserSubject.next(null);
    this.clearPermissionCache();
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
    return this.post<any>(`${this.endpoint}/check`, {
      permission,
      projectId
    }).pipe(
      map(response => response.data.hasPermission),
      catchError(this.errorHandler.handleError('檢查專案權限', 'PermissionService'))
    );
  }

  // 取得當前使用者的權限
  getCurrentUserPermissions(): Observable<string[]> {
    return this.get<any>(`${this.endpoint}/current`).pipe(
      tap(response => {
        if (response?.data) {
          this.userPermissionsSubject.next(response.data.permissions || []);
          this.userRolesSubject.next(response.data.roles || []);
        }
      }),
      map(response => response?.data?.permissions || []),
      catchError(() => {
        this.userPermissionsSubject.next([]);
        this.userRolesSubject.next([]);
        return of([]);
      })
    );
  }

  // 取得使用者的權限資訊
  getUserPermissions(userId: string): Observable<UserPermissionInfo> {
    return this.get<any>(`${this.endpoint}/user/${userId}`).pipe(
      map(response => {
        const data = response.data;
        return {
          userId: data.userId,
          username: data.username,
          email: data.email,
          fullName: data.fullName,
          roles: data.roles,
          permissions: data.directPermissions || [],
          directPermissions: data.directPermissions || [],
          allPermissions: data.allPermissions || [],
          projects: data.projectPermissions ? Object.values(data.projectPermissions) : []
        };
      }),
      catchError(this.errorHandler.handleError('取得使用者權限', 'PermissionService'))
    );
  }

  // 指派角色給使用者
  assignRoleToUser(userId: string, roleId: string): Observable<boolean> {
    return this.post<any>(`${this.endpoint}/user/${userId}/role`, { roleId }).pipe(
      map(response => response.success),
      tap(() => this.errorHandler.showSuccess('角色指派成功')),
      catchError(this.errorHandler.handleError('指派角色', 'PermissionService'))
    );
  }

  // 移除使用者的角色
  removeRoleFromUser(userId: string, roleId: string): Observable<boolean> {
    return this.delete<any>(`${this.endpoint}/user/${userId}/role/${roleId}`).pipe(
      map(response => response.success),
      tap(() => this.errorHandler.showSuccess('角色移除成功')),
      catchError(this.errorHandler.handleError('移除角色', 'PermissionService'))
    );
  }

  // 設定使用者的額外權限
  setUserPermissions(userId: string, permissions: string[]): Observable<boolean> {
    return this.post<any>(`${this.endpoint}/user/${userId}`, { permissions }).pipe(
      map(response => response.success),
      tap(() => this.errorHandler.showSuccess('權限設定成功')),
      catchError(this.errorHandler.handleError('設定權限', 'PermissionService'))
    );
  }

  // 批次設定使用者權限
  batchSetUserPermissions(assignments: Array<{userId: string, permissions: string[]}>): Observable<any> {
    return this.post<any>(`${this.endpoint}/batch`, { assignments }).pipe(
      tap(() => this.errorHandler.showSuccess('批次權限設定成功')),
      catchError(this.errorHandler.handleError('批次設定權限', 'PermissionService'))
    );
  }

  // 取得所有權限定義
  getAllPermissionDefinitions(): Observable<PermissionDefinition[]> {
    return this.get<any>(`${this.endpoint}/definitions`).pipe(
      map(response => response.data),
      catchError(this.errorHandler.handleError('取得權限定義', 'PermissionService'))
    );
  }

  // 取得所有角色
  getAllRoles(): Observable<RoleInfo[]> {
    return this.get<any>('role').pipe(
      map(response => response.data),
      catchError(this.errorHandler.handleError('取得角色列表', 'PermissionService'))
    );
  }

  // 取得角色詳情
  getRole(roleId: string): Observable<RoleInfo> {
    return this.get<any>(`role/${roleId}`).pipe(
      map(response => response.data),
      catchError(this.errorHandler.handleError('取得角色詳情', 'PermissionService'))
    );
  }

  // 建立角色
  createRole(role: {
    roleId: string;
    displayName: string;
    description: string;
    level: number;
  }): Observable<boolean> {
    return this.post<any>('role', role).pipe(
      map(response => response.success),
      tap(() => this.errorHandler.showSuccess('角色建立成功')),
      catchError(this.errorHandler.handleError('建立角色', 'PermissionService'))
    );
  }

  // 更新角色
  updateRole(roleId: string, role: {
    displayName: string;
    description: string;
    level: number;
  }): Observable<boolean> {
    return this.put<any>(`role/${roleId}`, role).pipe(
      map(response => response.success),
      tap(() => this.errorHandler.showSuccess('角色更新成功')),
      catchError(this.errorHandler.handleError('更新角色', 'PermissionService'))
    );
  }

  // 刪除角色
  deleteRole(roleId: string): Observable<boolean> {
    return this.delete<any>(`role/${roleId}`).pipe(
      map(response => response.success),
      tap(() => this.errorHandler.showSuccess('角色刪除成功')),
      catchError(this.errorHandler.handleError('刪除角色', 'PermissionService'))
    );
  }

  // 取得角色權限
  getRolePermissions(roleId: string): Observable<string[]> {
    return this.get<any>(`role/${roleId}/permissions`).pipe(
      map(response => response.data),
      catchError(this.errorHandler.handleError('取得角色權限', 'PermissionService'))
    );
  }

  // 設定角色權限
  setRolePermissions(roleId: string, permissions: string[]): Observable<boolean> {
    return this.post<any>(`role/${roleId}/permissions`, { permissions }).pipe(
      map(response => response.success),
      tap(() => this.errorHandler.showSuccess('角色權限設定成功')),
      catchError(this.errorHandler.handleError('設定角色權限', 'PermissionService'))
    );
  }

  // 複製角色
  copyRole(sourceRoleId: string, newRole: {
    newRoleId: string;
    displayName: string;
    description: string;
    level: number;
  }): Observable<boolean> {
    return this.post<any>(`role/${sourceRoleId}/copy`, newRole).pipe(
      map(response => response.success),
      tap(() => this.errorHandler.showSuccess('角色複製成功')),
      catchError(this.errorHandler.handleError('複製角色', 'PermissionService'))
    );
  }

  // 取得角色使用者
  getRoleUsers(roleId: string): Observable<any[]> {
    return this.get<any>(`role/${roleId}/users`).pipe(
      map(response => response.data),
      catchError(this.errorHandler.handleError('取得角色使用者', 'PermissionService'))
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
    const user = this.currentUserSubject.value;
    if (!user?.role) return false;
    return RoleHelper.isAdminLevel(user.role);
  }

  // 檢查是否為超級管理員
  isSuperAdmin(): boolean {
    const user = this.currentUserSubject.value;
    return user?.role === SYSTEM_ROLES.SUPER_ADMIN;
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
      }),
      catchError(this.errorHandler.handleError('取得權限分組', 'PermissionService'))
    );
  }

  // 使用者管理相關的便利方法
  canManageUsers(): boolean {
    return this.hasPermission(PERMISSIONS.USER_MANAGEMENT) || this.isAdmin();
  }

  canEditUser(): boolean {
    return this.hasPermission(PERMISSIONS.USER_EDIT) || this.isAdmin();
  }

  canDeleteUser(): boolean {
    return this.hasPermission(PERMISSIONS.USER_DELETE) || this.isAdmin();
  }
}