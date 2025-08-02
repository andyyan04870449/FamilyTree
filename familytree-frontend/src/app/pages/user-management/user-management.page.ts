// 使用者帳號管理頁面
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { UserService, UserModel } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { Subject, takeUntil } from 'rxjs';

interface User {
  id: string;
  account: string;
  name: string;
  email: string;
  status: '啟用' | '停用';
}

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './user-management.page.html',
  styleUrls: ['./user-management.page.scss']
})
export class UserManagementComponent implements OnInit, OnDestroy {
  // Math 物件綁定
  Math = Math;
  
  // 搜尋條件
  searchAccount = '';
  searchName = '';
  selectedStatus = '';
  
  // 使用者列表
  users: UserModel[] = [];
  filteredUsers: User[] = [];
  
  // 分頁
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  totalPages = 0;
  
  // 編輯對話框
  isEditDialogOpen = false;
  editingUser: Partial<UserModel & { password?: string }> | null = null;
  isNewUser = false;
  
  // 載入狀態
  loading = false;
  error = '';
  
  // 訂閱管理
  private destroy$ = new Subject<void>();
  
  // 當前使用者
  currentUser: any = null;
  
  constructor(
    private userService: UserService,
    private authService: AuthService,
    private toastService: ToastService,
    private router: Router
  ) {
    console.log('👤 使用者管理頁面初始化');
    this.currentUser = this.authService.currentUserValue;
  }
  
  ngOnInit(): void {
    this.loadUsers();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  // 載入使用者列表
  loadUsers(): void {
    this.loading = true;
    this.error = '';
    
    this.userService.getUsers(this.currentPage, this.pageSize)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.users = response.data;
          this.totalItems = response.totalCount;
          this.totalPages = response.totalPages;
          this.applyFilters();
          this.loading = false;
        },
        error: (err) => {
          console.error('載入使用者失敗:', err);
          this.error = err.error?.message || '載入使用者列表失敗';
          this.toastService.error(this.error);
          this.loading = false;
        }
      });
  }
  
  // 搜尋功能
  search(): void {
    console.log('🔍 執行搜尋');
    this.currentPage = 1;
    this.applyFilters();
  }
  
  // 重置搜尋
  resetSearch(): void {
    console.log('🔄 重置搜尋條件');
    this.searchAccount = '';
    this.searchName = '';
    this.selectedStatus = '';
    this.currentPage = 1;
    this.applyFilters();
  }
  
  // 套用過濾條件
  applyFilters(): void {
    // 將 UserModel 轉換為 User 介面格式
    this.filteredUsers = this.users.map(user => ({
      id: user.id,
      account: user.username,
      name: user.fullName || user.username,
      email: user.email,
      status: user.status === 'active' ? '啟用' : '停用'
    } as User));
    
    // 帳號篩選
    if (this.searchAccount) {
      this.filteredUsers = this.filteredUsers.filter(u => 
        u.account.toLowerCase().includes(this.searchAccount.toLowerCase())
      );
    }
    
    // 姓名篩選
    if (this.searchName) {
      this.filteredUsers = this.filteredUsers.filter(u => 
        u.name.toLowerCase().includes(this.searchName.toLowerCase())
      );
    }
    
    // 狀態篩選
    if (this.selectedStatus) {
      this.filteredUsers = this.filteredUsers.filter(u => u.status === this.selectedStatus);
    }
  }
  
  // 頁面導航
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadUsers();
    }
  }
  
  // 編輯使用者
  editUser(user: User): void {
    console.log('✏️ 編輯使用者', user);
    // 找到對應的 UserModel
    const userModel = this.users.find(u => u.id === user.id);
    if (userModel) {
      this.editingUser = { ...userModel };
      this.isNewUser = false;
      this.isEditDialogOpen = true;
    }
  }
  
  // 權限檢視
  viewPermissions(user: User): void {
    console.log('👁️ 檢視權限', user);
    // 導航到權限設定頁面，並傳遞使用者 ID
    this.router.navigate(['/permission-settings'], { 
      queryParams: { userId: user.id } 
    }).then(
      success => console.log('Navigation success:', success),
      error => console.error('Navigation error:', error)
    );
  }
  
  // 重置密碼
  resetPassword(user: User): void {
    console.log('🔐 重置密碼', user);
    if (confirm(`確定要重置 ${user.name} 的密碼嗎？`)) {
      this.userService.resetPassword(user.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            this.toastService.success(`密碼已重置，新密碼: ${response.data?.temporaryPassword || '請查看系統通知'}`);
          },
          error: (err) => {
            this.toastService.error(err.error?.message || '重置密碼失敗');
          }
        });
    }
  }
  
  // 停用/啟用使用者
  toggleUserStatus(user: User): void {
    const action = user.status === '啟用' ? '停用' : '啟用';
    console.log(`${action}使用者`, user);
    
    if (confirm(`確定要${action} ${user.name} 嗎？`)) {
      const newStatus = user.status === '啟用' ? 'inactive' : 'active';
      
      this.userService.updateUser(user.id, { status: newStatus })
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.toastService.success(`${action}使用者成功`);
            this.loadUsers();
          },
          error: (err) => {
            this.toastService.error(err.error?.message || `${action}使用者失敗`);
          }
        });
    }
  }
  
  // 儲存編輯
  saveEdit(): void {
    if (!this.editingUser) return;
    
    if (this.isNewUser) {
      // 新增使用者
      const { username, email, fullName, password } = this.editingUser as any;
      
      if (!username || !email || !password) {
        this.toastService.warning('請填寫必要欄位');
        return;
      }
      
      this.userService.registerUser({ username, email, password, fullName })
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.toastService.success('新增使用者成功');
            this.loadUsers();
            this.closeEditDialog();
          },
          error: (err) => {
            this.toastService.error(err.error?.message || '新增使用者失敗');
          }
        });
    } else {
      // 更新使用者
      const { email, fullName, role, status } = this.editingUser;
      
      this.userService.updateUser(this.editingUser.id!, { email, fullName, role, status })
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.toastService.success('更新使用者成功');
            this.loadUsers();
            this.closeEditDialog();
          },
          error: (err) => {
            this.toastService.error(err.error?.message || '更新使用者失敗');
          }
        });
    }
  }
  
  // 關閉編輯對話框
  closeEditDialog(): void {
    this.isEditDialogOpen = false;
    this.editingUser = null;
  }
  
  // 匯出資料
  exportData(): void {
    console.log('📥 匯出使用者資料');
    // TODO: 實作匯出功能
  }
  
  // 新增使用者
  addNewUser(): void {
    console.log('➕ 新增使用者');
    this.editingUser = {
      username: '',
      email: '',
      fullName: '',
      role: 'user',
      status: 'active',
      password: ''
    } as any;
    this.isNewUser = true;
    this.isEditDialogOpen = true;
  }
  
  // 生成分頁陣列
  getPageNumbers(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;
    
    let start = Math.max(1, this.currentPage - Math.floor(maxVisible / 2));
    let end = Math.min(this.totalPages, start + maxVisible - 1);
    
    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    
    return pages;
  }
}