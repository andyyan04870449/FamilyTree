import { Component, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
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
    
    // 暫時跳過驗證碼檢查（實際環境應該要檢查）
    // if (!this.captcha) {
    //   this.error = '請輸入驗證碼';
    //   return;
    // }
    
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