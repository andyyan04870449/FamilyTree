import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BaseApiService, ApiResponse, PagedApiResponse, ApiCallOptions } from './base-api.service';
import { ToastService } from './toast.service';
import { SYSTEM_ROLES, SystemRole } from '../constants/roles.const';

export interface UserModel {
  id: string;
  username: string;
  email: string;
  fullName: string;
  role: SystemRole;
  status: 'active' | 'inactive';
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
}

export interface UpdateUserRequest {
  email?: string;
  fullName?: string;
  status?: string;
  role?: SystemRole;
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

export interface PagedResponse<T> {
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class UserService extends BaseApiService {
  protected readonly apiUrl = this.apiConfig.user.base();

  constructor(
    protected override http: HttpClient,
    protected override toastService: ToastService
  ) {
    super(http, toastService);
  }

  /**
   * 取得使用者列表（需要管理員權限）
   */
  getUsers(page: number = 1, pageSize: number = 20): Observable<PagedResponse<UserModel>> {
    return this.getPagedData<UserModel>('', page, pageSize, undefined, {
      errorMessage: '取得使用者列表失敗'
    }).pipe(
      map(response => ({
        data: response.data || [],
        totalCount: response.totalCount,
        page: response.page,
        pageSize: response.pageSize,
        totalPages: response.totalPages
      }))
    );
  }

  /**
   * 取得特定使用者資訊
   */
  getUser(id: string): Observable<UserModel> {
    return this.get<ApiResponse<UserModel>>(id, undefined, {
      errorMessage: '取得使用者資訊失敗'
    }).pipe(
      map(response => response.data!)
    );
  }

  /**
   * 註冊新使用者
   */
  registerUser(data: RegisterRequest): Observable<ApiResponse<{ userId: string }>> {
    return this.http.post<ApiResponse<{ userId: string }>>(this.apiConfig.auth.register(), data);
  }

  /**
   * 更新使用者資訊
   */
  updateUser(id: string, data: UpdateUserRequest): Observable<ApiResponse<UserModel>> {
    return this.put<ApiResponse<UserModel>>(id, data, {
      successMessage: '使用者資訊更新成功',
      errorMessage: '更新使用者資訊失敗'
    });
  }

  /**
   * 變更密碼（僅限自己的帳號）
   */
  changePassword(id: string, data: ChangePasswordRequest): Observable<ApiResponse<{ success: boolean }>> {
    return this.post<ApiResponse<{ success: boolean }>>(`${id}/change-password`, data, {
      successMessage: '密碼變更成功',
      errorMessage: '密碼變更失敗'
    });
  }

  /**
   * 重設密碼（需要管理員權限）
   */
  resetPassword(id: string): Observable<ApiResponse<{ success: boolean; temporaryPassword: string }>> {
    return this.post<ApiResponse<{ success: boolean; temporaryPassword: string }>>(`${id}/reset-password`, {}, {
      successMessage: '密碼重設成功',
      errorMessage: '密碼重設失敗'
    });
  }

  /**
   * 停用使用者（需要管理員權限）
   */
  disableUser(id: string): Observable<ApiResponse<{ success: boolean }>> {
    return this.delete<ApiResponse<{ success: boolean }>>(id, {
      successMessage: '使用者已停用',
      errorMessage: '停用使用者失敗'
    });
  }

  /**
   * 檢查使用者名稱是否可用
   */
  checkUsernameAvailability(username: string): Observable<boolean> {
    return this.http.get<ApiResponse<{ available: boolean }>>(this.apiConfig.user.checkUsername(), {
      params: new HttpParams().set('username', username)
    }).pipe(
      map(response => response.data?.available ?? false)
    );
  }

  /**
   * 檢查電子郵件是否可用
   */
  checkEmailAvailability(email: string): Observable<boolean> {
    return this.http.get<ApiResponse<{ available: boolean }>>(this.apiConfig.user.checkEmail(), {
      params: new HttpParams().set('email', email)
    }).pipe(
      map(response => response.data?.available ?? false)
    );
  }
}