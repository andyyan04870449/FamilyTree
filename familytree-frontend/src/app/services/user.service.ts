import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface UserModel {
  id: string;
  username: string;
  email: string;
  fullName: string;
  role: 'admin' | 'user';
  status: 'active' | 'inactive';
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
}

export interface PagedResponse<T> {
  success: boolean;
  message: string;
  data: T[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
  };
  timestamp: string;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  timestamp: string;
}

export interface UpdateUserRequest {
  email?: string;
  fullName?: string;
  status?: string;
  role?: string;
}

export interface ChangePasswordRequest {
  oldPassword: string;
  newPassword: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  fullName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = '/api/user';
  private authUrl = '/api/auth';

  constructor(private http: HttpClient) {}

  /**
   * 取得使用者列表（需要管理員權限）
   */
  getUsers(page: number = 1, pageSize: number = 20): Observable<PagedResponse<UserModel>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResponse<UserModel>>(this.apiUrl, { params });
  }

  /**
   * 取得特定使用者資訊
   */
  getUser(id: string): Observable<UserModel> {
    return this.http.get<ApiResponse<UserModel>>(`${this.apiUrl}/${id}`)
      .pipe(map(response => response.data));
  }

  /**
   * 註冊新使用者
   */
  registerUser(data: RegisterRequest): Observable<ApiResponse<{ userId: string }>> {
    return this.http.post<ApiResponse<{ userId: string }>>(`${this.authUrl}/register`, data);
  }

  /**
   * 更新使用者資訊
   */
  updateUser(id: string, data: UpdateUserRequest): Observable<ApiResponse<UserModel>> {
    return this.http.put<ApiResponse<UserModel>>(`${this.apiUrl}/${id}`, data);
  }

  /**
   * 變更密碼（僅限自己的帳號）
   */
  changePassword(id: string, data: ChangePasswordRequest): Observable<ApiResponse<{ success: boolean }>> {
    return this.http.post<ApiResponse<{ success: boolean }>>(`${this.apiUrl}/${id}/change-password`, data);
  }

  /**
   * 重設密碼（需要管理員權限）
   */
  resetPassword(id: string): Observable<ApiResponse<{ success: boolean; temporaryPassword: string }>> {
    return this.http.post<ApiResponse<{ success: boolean; temporaryPassword: string }>>(`${this.apiUrl}/${id}/reset-password`, {});
  }

  /**
   * 停用使用者（需要管理員權限）
   */
  disableUser(id: string): Observable<ApiResponse<{ success: boolean }>> {
    return this.http.delete<ApiResponse<{ success: boolean }>>(`${this.apiUrl}/${id}`);
  }

  /**
   * 檢查使用者名稱是否可用
   */
  checkUsernameAvailability(username: string): Observable<boolean> {
    return this.getUsers(1, 1000).pipe(
      map(response => !response.data.some(user => user.username === username))
    );
  }

  /**
   * 檢查電子郵件是否可用
   */
  checkEmailAvailability(email: string): Observable<boolean> {
    return this.getUsers(1, 1000).pipe(
      map(response => !response.data.some(user => user.email === email))
    );
  }
}