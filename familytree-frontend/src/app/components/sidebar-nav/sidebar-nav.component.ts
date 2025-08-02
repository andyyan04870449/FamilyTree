import { Component, OnInit, Output, EventEmitter, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, takeUntil } from 'rxjs/operators';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService, UserInfo } from '../../services/auth.service';
import { Subject } from 'rxjs';
import { cn } from '../../utils/cn';

@Component({
  selector: 'app-sidebar-nav',
  standalone: true,
  imports: [RouterModule, CommonModule],
  template: `
    <!-- 漢堡選單按鈕 -->
    <button 
      class="fixed top-5 left-5 z-[1001] cursor-pointer p-2.5 bg-white rounded-lg shadow-lg border border-gray-200 hidden lg:hidden md:block transition-transform duration-300 hover:scale-105"
      (click)="toggleSidebar()" 
      [attr.aria-expanded]="isSidebarOpen" 
      aria-label="切換導航選單"
    >
      <div class="w-6 h-[18px] relative flex flex-col justify-between">
        <span 
          class="block w-full h-0.5 bg-gray-800 rounded-sm transition-all duration-300 origin-center"
          [class]="cn(isSidebarOpen ? 'rotate-45 translate-x-1.5 translate-y-1.5' : '')"
        ></span>
        <span 
          class="block w-full h-0.5 bg-gray-800 rounded-sm transition-all duration-300"
          [class]="cn(isSidebarOpen ? 'opacity-0' : '')"
        ></span>
        <span 
          class="block w-full h-0.5 bg-gray-800 rounded-sm transition-all duration-300 origin-center"
          [class]="cn(isSidebarOpen ? '-rotate-45 translate-x-1.5 -translate-y-1.5' : '')"
        ></span>
      </div>
    </button>

    <!-- 手機版遮罩 -->
    <div 
      class="fixed inset-0 bg-black bg-opacity-50 z-[999] transition-all duration-300 hidden md:block lg:hidden"
      [class]="cn(
        isSidebarOpen ? 'opacity-100 visible' : 'opacity-0 invisible'
      )"
      (click)="closeSidebar()"
      [attr.aria-hidden]="!isSidebarOpen"
    ></div>

    <!-- 側邊欄 -->
    <aside 
      class="w-[230px] h-[calc(100vh-80px)] bg-gradient-to-b from-blue-400 to-transparent fixed left-0 top-20 z-[998] flex flex-col transition-transform duration-300 overflow-hidden md:transform md:-translate-x-full lg:translate-x-0"
      [class]="cn(
        'md:h-screen md:top-0 md:shadow-[2px_0_10px_rgba(0,0,0,0.3)]',
        isSidebarOpen ? 'md:translate-x-0' : ''
      )"
    >
      <!-- 側邊欄頭部 -->
      <div class="flex items-center p-4 relative">
        <!-- 手機版關閉按鈕 -->
        <button 
          class="hidden md:block absolute right-3 top-3 w-8 h-8 bg-transparent border-none text-gray-800 text-2xl cursor-pointer rounded transition-all duration-200 hover:bg-white/10"
          (click)="closeSidebar()" 
          aria-label="關閉側邊欄"
        >
          ×
        </button>
      </div>

      <!-- 主要導航選單 -->
      <nav class="px-3 flex-1" role="navigation" aria-label="主要導航">
        <!-- 導航項目列表 -->
        <ul class="list-none m-0 p-0 flex flex-col gap-0">
          <!-- 關鍵字檢索 -->
          <li 
            class="relative transition-all duration-300 rounded-lg overflow-hidden"
            [class]="cn(
              activeRoute === 'full-text-search' 
                ? 'bg-blue-700' 
                : 'hover:bg-blue-700/30'
            )"
          >
            <a 
              class="flex items-center gap-3 py-3.5 px-4 no-underline text-gray-800 text-base font-medium transition-all duration-300 cursor-pointer"
              [class]="cn(
                activeRoute === 'full-text-search' ? 'text-white' : ''
              )"
              routerLink="/full-text-search" 
              (click)="closeSidebar()" 
              [attr.aria-current]="activeRoute === 'full-text-search' ? 'page' : null"
            >
              <div class="w-8 h-8 flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
                </svg>
              </div>
              <span class="flex-1 whitespace-nowrap">關鍵字檢索</span>
            </a>
          </li>
          
          <!-- 視覺化分析 -->
          <li 
            class="relative transition-all duration-300 rounded-lg overflow-hidden"
            [class]="cn(
              activeRoute === 'visual-analysis' 
                ? 'bg-blue-700' 
                : 'hover:bg-blue-700/30'
            )"
          >
            <a 
              class="flex items-center gap-3 py-3.5 px-4 no-underline text-gray-800 text-base font-medium transition-all duration-300 cursor-pointer"
              [class]="cn(
                activeRoute === 'visual-analysis' ? 'text-white' : ''
              )"
              routerLink="/visual-analysis" 
              (click)="closeSidebar()" 
              [attr.aria-current]="activeRoute === 'visual-analysis' ? 'page' : null"
            >
              <div class="w-8 h-8 flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
                </svg>
              </div>
              <span class="flex-1 whitespace-nowrap">視覺化分析</span>
            </a>
          </li>
          
          <!-- 案件管理 -->
          <li 
            class="relative transition-all duration-300 rounded-lg overflow-hidden"
            [class]="cn(
              activeRoute === 'case-management' 
                ? 'bg-blue-700' 
                : 'hover:bg-blue-700/30'
            )"
          >
            <a 
              class="flex items-center gap-3 py-3.5 px-4 no-underline text-gray-800 text-base font-medium transition-all duration-300 cursor-pointer"
              [class]="cn(
                activeRoute === 'case-management' ? 'text-white' : ''
              )"
              routerLink="/case-management" 
              (click)="closeSidebar()" 
              [attr.aria-current]="activeRoute === 'case-management' ? 'page' : null"
            >
              <div class="w-8 h-8 flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                </svg>
              </div>
              <span class="flex-1 whitespace-nowrap">案件管理</span>
            </a>
          </li>
          
          <!-- 系統管理 (只有管理員可見) -->
          <li 
            class="relative transition-all duration-300 rounded-lg overflow-hidden"
            [class]="cn(
              isSystemManagementActive() 
                ? 'bg-blue-700' 
                : 'hover:bg-blue-700/30'
            )"
            *ngIf="isAdmin"
          >
            <a 
              class="flex items-center gap-3 py-3.5 px-4 no-underline text-gray-800 text-base font-medium transition-all duration-300 cursor-pointer"
              [class]="cn(
                isSystemManagementActive() ? 'text-white' : ''
              )"
              routerLink="/user-management" 
              (click)="closeSidebar()" 
              [attr.aria-current]="isSystemManagementActive() ? 'page' : null"
            >
              <div class="w-8 h-8 flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/>
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                </svg>
              </div>
              <span class="flex-1 whitespace-nowrap">系統管理</span>
            </a>
          </li>
        </ul>
      </nav>
      
      <!-- 使用者資訊區域 -->
      <div 
        class="absolute bottom-0 left-0 right-0 p-6 bg-white/5 border-t border-white/10 flex items-center justify-between"
        *ngIf="currentUser"
      >
        <div class="flex-1">
          <div class="text-sm font-medium text-white mb-1">
            {{ currentUser.fullName || currentUser.username }}
          </div>
          <div class="text-xs text-white/70">
            {{ currentUser.role === 'admin' ? '管理員' : '一般使用者' }}
          </div>
        </div>
        <button 
          class="bg-transparent border-none text-white/70 cursor-pointer p-2 rounded transition-all duration-200 hover:bg-white/10 hover:text-white"
          (click)="logout()" 
          title="登出"
        >
          <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
            <path d="M7.5 17.5H4.16667C3.72464 17.5 3.30072 17.3244 2.98816 17.0118C2.67559 16.6993 2.5 16.2754 2.5 15.8333V4.16667C2.5 3.72464 2.67559 3.30072 2.98816 2.98816C3.30072 2.67559 3.72464 2.5 4.16667 2.5H7.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M13.3333 14.1667L17.5 10L13.3333 5.83334" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M17.5 10H7.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>

      <!-- 背景裝飾 -->
      <div 
        class="absolute bottom-0 left-0 right-0 h-[200px] bg-[url('/assets/sidebar-bg.png')] bg-no-repeat bg-bottom bg-contain opacity-50 pointer-events-none"
        aria-hidden="true"
      ></div>
    </aside>
  `
})
export class SidebarNavComponent implements OnInit, OnDestroy {
  // Utility function for class names
  cn = cn;
  
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