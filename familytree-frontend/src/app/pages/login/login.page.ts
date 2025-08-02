import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { cn } from '../../utils/cn';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-blue-50 via-indigo-50 to-cyan-50 flex items-center justify-center p-4 relative overflow-hidden">
      <!-- Background Decorations -->
      <div class="absolute inset-0 overflow-hidden">
        <div class="absolute -top-40 -right-40 w-80 h-80 bg-blue-400 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-blob"></div>
        <div class="absolute -bottom-40 -left-40 w-80 h-80 bg-purple-400 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-blob animation-delay-2000"></div>
        <div class="absolute top-40 left-40 w-80 h-80 bg-pink-400 rounded-full mix-blend-multiply filter blur-xl opacity-20 animate-blob animation-delay-4000"></div>
      </div>

      <!-- Login Card -->
      <div class="w-full max-w-md relative">
        <div class="bg-white rounded-2xl shadow-2xl border border-gray-100 overflow-hidden backdrop-blur-sm bg-opacity-90">
          <!-- Header -->
          <div class="px-8 pt-8 pb-6 text-center">
            <div class="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-r from-blue-600 to-purple-600 rounded-2xl mb-4">
              <svg class="w-8 h-8 text-white" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>
              </svg>
            </div>
            <h1 class="text-2xl font-bold text-gray-900 mb-2">家族樹管理系統</h1>
            <p class="text-gray-600">請登入您的帳號以繼續</p>
          </div>

          <!-- Form -->
          <div class="px-8 pb-8">
            <form (submit)="onLoginSubmit($event)" class="space-y-6">
              <!-- Error Alert -->
              <div *ngIf="error" class="bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
                <svg class="w-5 h-5 text-red-500 mt-0.5 flex-shrink-0" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                </svg>
                <span class="text-red-700 text-sm">{{ error }}</span>
              </div>

              <!-- Username Field -->
              <div class="space-y-2">
                <label for="username" class="flex items-center gap-2 text-sm font-medium text-gray-700">
                  <svg class="w-4 h-4 text-gray-500" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z"/>
                  </svg>
                  帳號
                </label>
                <input
                  id="username"
                  type="text"
                  name="username"
                  [(ngModel)]="username"
                  [disabled]="loading"
                  required
                  placeholder="請輸入帳號或電子郵件"
                  [class]="cn(
                    'w-full px-4 py-3 border border-gray-300 rounded-lg text-sm transition-all duration-200',
                    'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent',
                    'disabled:bg-gray-50 disabled:text-gray-500 disabled:cursor-not-allowed',
                    'placeholder:text-gray-400'
                  )"
                />
              </div>

              <!-- Password Field -->
              <div class="space-y-2">
                <label for="password" class="flex items-center gap-2 text-sm font-medium text-gray-700">
                  <svg class="w-4 h-4 text-gray-500" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd"/>
                  </svg>
                  密碼
                </label>
                <div class="relative">
                  <input
                    id="password"
                    [type]="showPassword ? 'text' : 'password'"
                    name="password"
                    [(ngModel)]="password"
                    [disabled]="loading"
                    required
                    placeholder="請輸入密碼"
                    [class]="cn(
                      'w-full px-4 py-3 pr-12 border border-gray-300 rounded-lg text-sm transition-all duration-200',
                      'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent',
                      'disabled:bg-gray-50 disabled:text-gray-500 disabled:cursor-not-allowed',
                      'placeholder:text-gray-400'
                    )"
                  />
                  <button
                    type="button"
                    (click)="togglePassword()"
                    [disabled]="loading"
                    class="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-gray-500 hover:text-gray-700 transition-colors disabled:cursor-not-allowed"
                    [attr.aria-label]="showPassword ? '隱藏密碼' : '顯示密碼'"
                  >
                    <svg *ngIf="!showPassword" class="w-5 h-5" viewBox="0 0 20 20" fill="currentColor">
                      <path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/>
                      <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"/>
                    </svg>
                    <svg *ngIf="showPassword" class="w-5 h-5" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074l-1.78-1.781zm4.261 4.26l1.514 1.515a2.003 2.003 0 012.45 2.45l1.514 1.514a4 4 0 00-5.478-5.478z" clip-rule="evenodd"/>
                      <path d="M12.454 16.697L9.75 13.992a4 4 0 01-3.742-3.741L2.335 6.578A9.98 9.98 0 00.458 10c1.274 4.057 5.065 7 9.542 7 .847 0 1.669-.105 2.454-.303z"/>
                    </svg>
                  </button>
                </div>
              </div>

              <!-- Login Button -->
              <app-button
                type="submit"
                variant="primary"
                size="lg"
                fullWidth="true"
                [disabled]="loading || !username || !password"
                [loading]="loading"
                label="登入"
                loadingText="登入中..."
              ></app-button>

              <!-- Demo Info -->
              <div class="pt-4 border-t border-gray-200">
                <div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
                  <div class="flex items-start gap-3">
                    <svg class="w-5 h-5 text-blue-500 mt-0.5 flex-shrink-0" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
                    </svg>
                    <div class="text-sm text-blue-700">
                      <p class="font-medium mb-1">預設登入資訊</p>
                      <p>帳號：<code class="bg-blue-100 px-1.5 py-0.5 rounded text-xs font-mono">admin</code></p>
                      <p>密碼：<code class="bg-blue-100 px-1.5 py-0.5 rounded text-xs font-mono">Admin@123</code></p>
                    </div>
                  </div>
                </div>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  `
})
export class LoginPage implements OnDestroy {
  username: string = '';
  password: string = '';
  captcha: string = '';
  showPassword: boolean = false;
  loading: boolean = false;
  error: string = '';
  returnUrl: string = '/';
  
  private destroy$ = new Subject<void>();
  
  // Utility function for class names
  cn = cn;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService,
    private toastService: ToastService
  ) {
    // 如果已經登入，導航到首頁
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/']);
    }
    
    // 獲取返回 URL
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onLoginSubmit(event: Event): void {
    event.preventDefault();
    
    // 驗證輸入
    if (!this.username || !this.password) {
      this.error = '請輸入帳號和密碼';
      return;
    }
    
    this.loading = true;
    this.error = '';
    
    // 呼叫登入 API
    this.authService.login({
      usernameOrEmail: this.username,
      password: this.password
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (response) => {
        console.log('登入 API 回應:', response);
        if (response.success) {
          // 登入成功，導航到原本要去的頁面或首頁
          this.toastService.success('登入成功');
          this.router.navigate([this.returnUrl]);
        } else {
          this.error = response.message || '登入失敗';
          this.toastService.error(this.error);
          this.loading = false;
        }
      },
      error: (err) => {
        console.error('Login error:', err);
        this.error = err.error?.message || '登入失敗，請稍後再試';
        this.toastService.error(this.error);
        this.loading = false;
      }
    });
  }
  
  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
  
  refreshCaptcha(): void {
    // TODO: 實作驗證碼更新邏輯
    console.log('Refresh captcha');
  }
}