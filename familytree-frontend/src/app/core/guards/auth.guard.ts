import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> | Promise<boolean> | boolean {
    // 檢查是否已登入
    if (this.authService.isLoggedIn()) {
      // 檢查角色權限
      const requiredRole = route.data['role'];
      if (requiredRole) {
        const user = this.authService.currentUserValue;
        if (user && user.role === requiredRole) {
          return true;
        } else {
          // 沒有權限，導航到首頁
          this.router.navigate(['/']);
          return false;
        }
      }
      return true;
    }

    // 未登入，導航到登入頁並記錄原本要去的頁面
    this.router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
}