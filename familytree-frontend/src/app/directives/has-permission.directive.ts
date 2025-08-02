import { Directive, Input, TemplateRef, ViewContainerRef, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { PermissionService } from '../services/permission.service';

@Directive({
  selector: '[appHasPermission]',
  standalone: true
})
export class HasPermissionDirective implements OnDestroy {
  private destroy$ = new Subject<void>();
  private hasView = false;
  private permissions: string[] = [];

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private permissionService: PermissionService
  ) {
    // 監聽權限變化
    this.permissionService.userPermissions$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.updateView();
      });
  }

  @Input()
  set appHasPermission(permissions: string | string[]) {
    this.permissions = Array.isArray(permissions) ? permissions : [permissions];
    this.updateView();
  }

  @Input()
  appHasPermissionMode: 'any' | 'all' = 'any';

  private updateView(): void {
    const hasPermission = this.checkPermission();

    if (hasPermission && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!hasPermission && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }

  private checkPermission(): boolean {
    if (this.permissions.length === 0) {
      return true;
    }

    if (this.appHasPermissionMode === 'all') {
      return this.permissionService.hasAllPermissions(...this.permissions);
    } else {
      return this.permissionService.hasAnyPermission(...this.permissions);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

// 反向權限指令 - 沒有權限時顯示
@Directive({
  selector: '[appHasNoPermission]',
  standalone: true
})
export class HasNoPermissionDirective implements OnDestroy {
  private destroy$ = new Subject<void>();
  private hasView = false;
  private permissions: string[] = [];

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private permissionService: PermissionService
  ) {
    // 監聽權限變化
    this.permissionService.userPermissions$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.updateView();
      });
  }

  @Input()
  set appHasNoPermission(permissions: string | string[]) {
    this.permissions = Array.isArray(permissions) ? permissions : [permissions];
    this.updateView();
  }

  private updateView(): void {
    const hasPermission = this.checkPermission();

    if (!hasPermission && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (hasPermission && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }

  private checkPermission(): boolean {
    if (this.permissions.length === 0) {
      return false;
    }

    return this.permissionService.hasAnyPermission(...this.permissions);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

// 角色檢查指令
@Directive({
  selector: '[appHasRole]',
  standalone: true
})
export class HasRoleDirective implements OnDestroy {
  private destroy$ = new Subject<void>();
  private hasView = false;
  private roles: string[] = [];

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private permissionService: PermissionService
  ) {
    // 監聽角色變化
    this.permissionService.userRoles$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.updateView();
      });
  }

  @Input()
  set appHasRole(roles: string | string[]) {
    this.roles = Array.isArray(roles) ? roles : [roles];
    this.updateView();
  }

  private updateView(): void {
    const hasRole = this.checkRole();

    if (hasRole && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!hasRole && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }

  private checkRole(): boolean {
    if (this.roles.length === 0) {
      return true;
    }

    const userRoles = (this.permissionService as any).userRolesSubject.value || [];
    return this.roles.some(role => 
      userRoles.some((userRole: any) => userRole.id === role)
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

// 管理員檢查指令
@Directive({
  selector: '[appIsAdmin]',
  standalone: true
})
export class IsAdminDirective implements OnDestroy {
  private destroy$ = new Subject<void>();
  private hasView = false;

  constructor(
    private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private permissionService: PermissionService
  ) {
    // 監聽角色變化
    this.permissionService.userRoles$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.updateView();
      });
  }

  private updateView(): void {
    const isAdmin = this.permissionService.isAdmin();

    if (isAdmin && !this.hasView) {
      this.viewContainer.createEmbeddedView(this.templateRef);
      this.hasView = true;
    } else if (!isAdmin && this.hasView) {
      this.viewContainer.clear();
      this.hasView = false;
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}