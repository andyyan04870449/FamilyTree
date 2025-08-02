import { Injectable } from '@angular/core';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { PermissionService } from '../services/permission.service';
import { ToastService } from '../services/toast.service';

@Injectable({
  providedIn: 'root'
})
export class PermissionGuard {
  constructor(
    private permissionService: PermissionService,
    private router: Router,
    private toastService: ToastService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> | boolean {
    // 從路由設定取得需要的權限
    const requiredPermissions = route.data['permissions'] as string[] | undefined;
    const requireMode = route.data['requireMode'] as 'any' | 'all' | undefined || 'any';
    const requiredRoles = route.data['roles'] as string[] | undefined;
    const requireAdmin = route.data['requireAdmin'] as boolean | undefined;
    const requireSuperAdmin = route.data['requireSuperAdmin'] as boolean | undefined;

    // 如果需要超級管理員
    if (requireSuperAdmin) {
      if (!this.permissionService.isSuperAdmin()) {
        this.handleUnauthorized('需要超級管理員權限');
        return false;
      }
    }

    // 如果需要管理員
    if (requireAdmin) {
      if (!this.permissionService.isAdmin()) {
        this.handleUnauthorized('需要管理員權限');
        return false;
      }
    }

    // 檢查角色
    if (requiredRoles && requiredRoles.length > 0) {
      const userRoles = (this.permissionService as any).userRolesSubject.value || [];
      const hasRole = requiredRoles.some(role => 
        userRoles.some((userRole: any) => userRole.id === role)
      );
      
      if (!hasRole) {
        this.handleUnauthorized('沒有所需的角色權限');
        return false;
      }
    }

    // 檢查權限
    if (requiredPermissions && requiredPermissions.length > 0) {
      const hasPermission = requireMode === 'all'
        ? this.permissionService.hasAllPermissions(...requiredPermissions)
        : this.permissionService.hasAnyPermission(...requiredPermissions);
      
      if (!hasPermission) {
        this.handleUnauthorized('沒有所需的權限');
        return false;
      }
    }

    return true;
  }

  private handleUnauthorized(message: string): void {
    this.toastService.error(message);
    this.router.navigate(['/dashboard']);
  }
}

// 專案權限守衛
@Injectable({
  providedIn: 'root'
})
export class ProjectPermissionGuard {
  constructor(
    private permissionService: PermissionService,
    private router: Router,
    private toastService: ToastService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    const projectId = route.params['projectId'];
    const requiredPermission = route.data['projectPermission'] as string | undefined;

    if (!projectId || !requiredPermission) {
      return of(true);
    }

    return this.permissionService.checkProjectPermission(projectId, requiredPermission).pipe(
      map(hasPermission => {
        if (!hasPermission) {
          this.toastService.error('沒有該專案的權限');
          this.router.navigate(['/projects']);
          return false;
        }
        return true;
      }),
      catchError(() => {
        this.toastService.error('權限檢查失敗');
        this.router.navigate(['/projects']);
        return of(false);
      })
    );
  }
}

// 可以匯出一個函數來簡化路由配置
export function requirePermissions(permissions: string[], mode: 'any' | 'all' = 'any') {
  return {
    canActivate: [PermissionGuard],
    data: { permissions, requireMode: mode }
  };
}

export function requireRoles(roles: string[]) {
  return {
    canActivate: [PermissionGuard],
    data: { roles }
  };
}

export function requireAdmin() {
  return {
    canActivate: [PermissionGuard],
    data: { requireAdmin: true }
  };
}

export function requireSuperAdmin() {
  return {
    canActivate: [PermissionGuard],
    data: { requireSuperAdmin: true }
  };
}

export function requireProjectPermission(permission: string) {
  return {
    canActivate: [ProjectPermissionGuard],
    data: { projectPermission: permission }
  };
}