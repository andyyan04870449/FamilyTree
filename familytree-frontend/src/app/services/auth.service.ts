import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError, Subject } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import { Router } from '@angular/router';

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  data?: {
    accessToken: string;
    refreshToken: string;
    expiresIn: number;
    user: UserInfo;
  };
}

export interface UserInfo {
  id: string;
  username: string;
  email: string;
  role: string;
  fullName?: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = '/api/auth';
  private currentUserSubject: BehaviorSubject<UserInfo | null>;
  public currentUser: Observable<UserInfo | null>;
  private refreshTokenTimeout?: any;
  
  // 事件通知
  private loginSuccessSubject = new Subject<UserInfo>();
  private logoutSubject = new Subject<void>();
  
  public loginSuccess$ = this.loginSuccessSubject.asObservable();
  public logout$ = this.logoutSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    const storedUser = this.getStoredUser();
    this.currentUserSubject = new BehaviorSubject<UserInfo | null>(storedUser);
    this.currentUser = this.currentUserSubject.asObservable();
    
    // 如果有儲存的 token，設定自動更新
    if (this.getAccessToken()) {
      this.startRefreshTokenTimer();
    }
  }

  /**
   * 取得當前使用者資訊
   */
  public get currentUserValue(): UserInfo | null {
    return this.currentUserSubject.value;
  }

  /**
   * 登入
   */
  login(credentials: LoginRequest): Observable<LoginResponse> {
    console.log('Attempting login with:', credentials.usernameOrEmail);
    console.log('API URL:', `${this.apiUrl}/login`);
    
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials)
      .pipe(
        tap(response => {
          console.log('Login response:', response);
          if (response.success && response.data) {
            this.handleLoginSuccess(response.data);
          }
        }),
        catchError(error => {
          console.error('Login error in service:', error);
          return this.handleError(error);
        })
      );
  }

  /**
   * 登出
   */
  logout(): Observable<any> {
    const refreshToken = this.getRefreshToken();
    
    // 清除本地資料
    this.clearAuthData();
    
    // 如果有 refresh token，呼叫後端登出 API
    if (refreshToken) {
      return this.http.post(`${this.apiUrl}/logout`, { refreshToken })
        .pipe(
          tap(() => {
            this.router.navigate(['/login']);
          }),
          catchError(() => {
            // 即使登出 API 失敗，仍然導航到登入頁
            this.router.navigate(['/login']);
            return throwError(() => new Error('登出失敗'));
          })
        );
    } else {
      this.router.navigate(['/login']);
      return new Observable(observer => {
        observer.next({ success: true });
        observer.complete();
      });
    }
  }

  /**
   * 更新 Access Token
   */
  refreshToken(): Observable<LoginResponse> {
    const refreshToken = this.getRefreshToken();
    
    if (!refreshToken) {
      this.clearAuthData();
      this.router.navigate(['/login']);
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<LoginResponse>(`${this.apiUrl}/refresh`, { refreshToken })
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.storeTokens(response.data.accessToken, response.data.refreshToken);
            this.startRefreshTokenTimer();
          }
        }),
        catchError(error => {
          this.clearAuthData();
          this.router.navigate(['/login']);
          return throwError(() => error);
        })
      );
  }

  /**
   * 取得當前使用者資訊（從後端）
   */
  getCurrentUser(): Observable<UserInfo> {
    return this.http.get<{ success: boolean; data: UserInfo }>(`${this.apiUrl}/me`)
      .pipe(
        map(response => response.data),
        tap(user => {
          this.currentUserSubject.next(user);
          this.storeUser(user);
        }),
        catchError(this.handleError)
      );
  }

  /**
   * 檢查是否已登入
   */
  isLoggedIn(): boolean {
    return !!this.getAccessToken();
  }

  /**
   * 檢查是否為管理員
   */
  isAdmin(): boolean {
    const user = this.currentUserValue;
    return user?.role === 'admin';
  }

  /**
   * 取得 Access Token
   */
  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  /**
   * 取得 Authorization Header
   */
  getAuthorizationHeader(): HttpHeaders {
    const token = this.getAccessToken();
    if (token) {
      return new HttpHeaders().set('Authorization', `Bearer ${token}`);
    }
    return new HttpHeaders();
  }

  /**
   * 處理登入成功
   */
  private handleLoginSuccess(data: LoginResponse['data']): void {
    if (!data) return;
    
    // 儲存 tokens 和使用者資訊
    this.storeTokens(data.accessToken, data.refreshToken);
    this.storeUser(data.user);
    this.currentUserSubject.next(data.user);
    
    // 設定自動更新 timer
    this.startRefreshTokenTimer();
    
    // 發送登入成功事件
    this.loginSuccessSubject.next(data.user);
  }

  /**
   * 開始 Refresh Token Timer
   */
  private startRefreshTokenTimer(): void {
    // 清除現有的 timer
    this.stopRefreshTokenTimer();
    
    // 在 token 過期前 1 分鐘更新
    const timeout = 14 * 60 * 1000; // 14 分鐘
    
    this.refreshTokenTimeout = setTimeout(() => {
      this.refreshToken().subscribe();
    }, timeout);
  }

  /**
   * 停止 Refresh Token Timer
   */
  private stopRefreshTokenTimer(): void {
    if (this.refreshTokenTimeout) {
      clearTimeout(this.refreshTokenTimeout);
      this.refreshTokenTimeout = undefined;
    }
  }

  /**
   * 儲存 Tokens
   */
  private storeTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', refreshToken);
  }

  /**
   * 儲存使用者資訊
   */
  private storeUser(user: UserInfo): void {
    localStorage.setItem('current_user', JSON.stringify(user));
  }

  /**
   * 取得儲存的使用者資訊
   */
  private getStoredUser(): UserInfo | null {
    const userJson = localStorage.getItem('current_user');
    if (userJson) {
      try {
        return JSON.parse(userJson);
      } catch {
        return null;
      }
    }
    return null;
  }

  /**
   * 取得 Refresh Token
   */
  private getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  /**
   * 清除認證資料
   */
  private clearAuthData(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('current_user');
    this.currentUserSubject.next(null);
    this.stopRefreshTokenTimer();
    
    // 發送登出事件
    this.logoutSubject.next();
  }

  /**
   * 處理錯誤
   */
  private handleError(error: any): Observable<never> {
    console.error('Auth error:', error);
    return throwError(() => error);
  }
}