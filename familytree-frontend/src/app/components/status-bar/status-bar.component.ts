import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { CommonModule } from '@angular/common';
import { AppConstants } from '../../constants/app.constants';
import { AuthService, UserInfo } from '../../services/auth.service';
import { cn } from '../../utils/cn';

@Component({
  selector: 'app-status-bar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed top-0 left-0 right-0 w-screen h-20 z-[999]">
      <!-- 背景 -->
      <div 
        class="absolute inset-0 bg-[url('/assets/header-bg.png')] bg-center bg-cover bg-no-repeat"
        style="background-size: 100% 100%;"
      ></div>
      
      <!-- 內容 -->
      <div class="relative flex items-center justify-between px-8 h-full max-w-screen-xl mx-auto">
        <!-- 左側：系統標題 -->
        <div class="flex-1">
          <div class="flex items-center gap-3">
            <span class="text-2xl">💻</span>
            <span class="font-['Noto_Sans_TC'] font-semibold text-xl text-white">
              雨聲國小人事資料分析系統
            </span>
          </div>
        </div>
        
        <!-- 中間：空白區域 -->
        <div class="flex-1 flex justify-center">
          <!-- 可在此處添加麵包屑導航 -->
        </div>
        
        <!-- 右側：用戶資訊與操作 -->
        <div class="flex-1 flex items-center justify-end gap-6">
          <!-- 用戶資訊 -->
          <div class="flex items-center gap-2">
            <span class="text-xl">👤</span>
            <span class="font-['Noto_Sans_TC'] font-medium text-base text-white">
              {{ userName }}
            </span>
          </div>
          
          <!-- 倒數計時 -->
          <div class="flex items-center gap-2">
            <span class="font-['Noto_Sans_TC'] font-normal text-sm text-white">
              倒數：{{ countdownDisplay }}
            </span>
            <button 
              class="bg-white/20 border-none text-white rounded-full w-8 h-8 cursor-pointer text-base transition-all duration-300 backdrop-blur-[10px] hover:bg-white/30 hover:scale-110"
              (click)="resetCountdown()" 
              title="重設倒數"
            >
              ⟳
            </button>
          </div>
          
          <!-- 登出按鈕 -->
          <button 
            class="bg-red-600 border-none text-white py-2 px-5 rounded-full font-['Noto_Sans_TC'] font-medium text-sm cursor-pointer transition-all duration-300 hover:bg-red-700 hover:-translate-y-px"
            (click)="logout()"
          >
            登出
          </button>
        </div>
      </div>
    </div>
  `
})
export class StatusBarComponent implements OnInit, OnDestroy {
  // Utility function for class names
  cn = cn;
  
  userName = AppConstants.DEFAULT_USER_NAME;
  countdown = 0;
  timer: any;
  currentPageName = '';
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private authService: AuthService
  ) {
    this.updateCurrentPageName(this.router.url);
    
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      takeUntil(this.destroy$)
    ).subscribe((event: NavigationEnd) => {
      this.updateCurrentPageName(event.url);
    });
  }

  ngOnInit() {
    // 訂閱當前用戶資訊
    this.authService.currentUser.pipe(
      takeUntil(this.destroy$)
    ).subscribe((user: UserInfo | null) => {
      if (user) {
        this.userName = user.fullName || user.username;
        // 當有用戶登入時，開始倒數計時
        this.startCountdown();
      } else {
        this.userName = AppConstants.DEFAULT_USER_NAME;
        this.stopCountdown();
        this.countdown = 0;
      }
    });

    // 訂閱 token 過期時間變更
    this.authService.tokenExpiration$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(() => {
      // 當 token 過期時間更新時，重新開始倒數
      if (this.authService.isLoggedIn()) {
        this.startCountdown();
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopCountdown();
  }

  startCountdown() {
    this.stopCountdown();
    
    // 更新倒數計時
    const updateCountdown = () => {
      this.countdown = this.authService.getTokenRemainingSeconds();
      
      if (this.countdown <= 0) {
        this.stopCountdown();
        // Token 已過期，可以觸發登出或重新整理
        console.warn('Token 已過期');
      }
    };
    
    // 立即更新一次
    updateCountdown();
    
    // 每秒更新
    this.timer = setInterval(updateCountdown, 1000);
  }

  stopCountdown() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  resetCountdown() {
    // 重設倒數實際上應該要重新整理 token
    console.log('重設倒數計時功能暫時無效，token 過期時間由後端控制');
    // 未來可以實作手動重新整理 token 的功能
  }

  logout() {
    this.authService.logout().subscribe({
      next: () => {
        // 登出成功，AuthService 會自動導航到登入頁
        console.log('已成功登出');
      },
      error: (error) => {
        console.error('登出時發生錯誤:', error);
        // 即使有錯誤也已經清除本地資料，會導航到登入頁
      }
    });
  }

  get countdownDisplay() {
    const min = Math.floor(this.countdown / 60).toString().padStart(2, '0');
    const sec = (this.countdown % 60).toString().padStart(2, '0');
    return `${min}:${sec}`;
  }

  getCurrentPageName() {
    return this.currentPageName;
  }

  private updateCurrentPageName(url: string) {
    // 不顯示頁面名稱，保持空字串
    this.currentPageName = '';
  }
}
