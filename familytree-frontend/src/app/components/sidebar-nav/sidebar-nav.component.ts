import { Component, OnInit, Output, EventEmitter, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, takeUntil } from 'rxjs/operators';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService, UserInfo } from '../../services/auth.service';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-sidebar-nav',
  templateUrl: './sidebar-nav.component.html',
  styleUrls: ['./sidebar-nav-bem.scss'],
  standalone: true,
  imports: [RouterModule, CommonModule]
})
export class SidebarNavComponent implements OnInit, OnDestroy {

  activeRoute: string = 'home';
  isSidebarOpen: boolean = false;
  currentUser: UserInfo | null = null;
  
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit() {
    // 監聽路由變化，更新當前活動路由
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((event: any) => {
        this.updateActiveRoute(event.url);
      });

    // 初始化當前路由
    this.updateActiveRoute(this.router.url);
    
    // 訂閱當前使用者資訊
    this.authService.currentUser
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;
      });
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
    // 防止背景滾動
    if (this.isSidebarOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  }

  closeSidebar() {
    this.isSidebarOpen = false;
    document.body.style.overflow = '';
  }

  private updateActiveRoute(url: string) {
    if (url.includes('/full-text-search')) {
      this.activeRoute = 'full-text-search';
    } else if (url.includes('/visual-analysis')) {
      this.activeRoute = 'visual-analysis';
    } else if (url.includes('/case-management')) {
      this.activeRoute = 'case-management';
    } else if (url.includes('/user-management') || url.includes('/role-management') || url.includes('/permission-settings')) {
      this.activeRoute = 'user-management';
    } else if (url.includes('/system-settings')) {
      this.activeRoute = 'system-settings';
    } else if (url === '/' || url === '/home') {
      this.activeRoute = 'home';
    }
  }
  
  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        // 登出成功，已經由 AuthService 導航到登入頁
      },
      error: (err) => {
        console.error('Logout error:', err);
      }
    });
  }
  
  get isAdmin(): boolean {
    return this.authService.isAdmin();
  }
  
  isSystemManagementActive(): boolean {
    return this.activeRoute === 'user-management';
  }


} 