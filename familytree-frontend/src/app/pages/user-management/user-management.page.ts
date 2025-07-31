// 使用者帳號管理頁面
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface User {
  id: number;
  account: string;
  department: string;
  name: string;
  email: string;
  status: '啟用' | '停用';
}

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management.page.html',
  styleUrls: ['./user-management.page.scss']
})
export class UserManagementComponent implements OnInit {
  // Math 物件綁定
  Math = Math;
  
  // 搜尋條件
  searchAccount = '';
  searchDepartment = '';
  searchName = '';
  selectedStatus = '';
  
  // 使用者列表
  users: User[] = [
    { id: 1, account: 'admin123', department: '測試部門', name: '王小明', email: 'test123@email.com', status: '啟用' },
    { id: 2, account: 'admin456', department: '測試部門', name: '王中明', email: 'test123@email.com', status: '啟用' },
    { id: 3, account: 'admin111', department: '測試部門', name: '王大明', email: 'test123@email.com', status: '啟用' },
    { id: 4, account: 'admin000', department: '測試部門', name: '陳小華', email: 'test123@email.com', status: '停用' },
    { id: 5, account: 'admin999', department: '測試部門', name: '陳大華', email: 'test123@email.com', status: '停用' },
    { id: 6, account: 'admin888', department: '測試部門', name: '王中明', email: 'test123@email.com', status: '啟用' },
    { id: 7, account: 'admin222', department: '測試部門', name: '王大明', email: 'test123@email.com', status: '啟用' },
    { id: 8, account: 'admin391', department: '測試部門', name: '陳小華', email: 'test123@email.com', status: '停用' },
    { id: 9, account: 'admin018', department: '測試部門', name: '陳大華', email: 'test123@email.com', status: '停用' },
    { id: 10, account: 'admin419', department: '測試部門', name: '陳小華', email: 'test123@email.com', status: '停用' }
  ];
  
  filteredUsers: User[] = [];
  
  // 分頁
  currentPage = 1;
  pageSize = 10;
  totalItems = 0;
  totalPages = 0;
  
  // 編輯對話框
  isEditDialogOpen = false;
  editingUser: User | null = null;
  
  constructor() {
    console.log('👤 使用者管理頁面初始化');
  }
  
  ngOnInit(): void {
    this.applyFilters();
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
    this.searchDepartment = '';
    this.searchName = '';
    this.selectedStatus = '';
    this.currentPage = 1;
    this.applyFilters();
  }
  
  // 套用過濾條件
  applyFilters(): void {
    let filtered = [...this.users];
    
    // 帳號篩選
    if (this.searchAccount) {
      filtered = filtered.filter(u => 
        u.account.toLowerCase().includes(this.searchAccount.toLowerCase())
      );
    }
    
    // 部門篩選
    if (this.searchDepartment) {
      filtered = filtered.filter(u => 
        u.department.toLowerCase().includes(this.searchDepartment.toLowerCase())
      );
    }
    
    // 姓名篩選
    if (this.searchName) {
      filtered = filtered.filter(u => 
        u.name.toLowerCase().includes(this.searchName.toLowerCase())
      );
    }
    
    // 狀態篩選
    if (this.selectedStatus) {
      filtered = filtered.filter(u => u.status === this.selectedStatus);
    }
    
    // 更新總數
    this.totalItems = filtered.length;
    this.totalPages = Math.ceil(this.totalItems / this.pageSize);
    
    // 分頁
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    this.filteredUsers = filtered.slice(start, end);
  }
  
  // 頁面導航
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.applyFilters();
    }
  }
  
  // 編輯使用者
  editUser(user: User): void {
    console.log('✏️ 編輯使用者', user);
    this.editingUser = { ...user };
    this.isEditDialogOpen = true;
  }
  
  // 權限檢視
  viewPermissions(user: User): void {
    console.log('👁️ 檢視權限', user);
    // TODO: 實作權限檢視功能
  }
  
  // 重置密碼
  resetPassword(user: User): void {
    console.log('🔐 重置密碼', user);
    if (confirm(`確定要重置 ${user.name} 的密碼嗎？`)) {
      // TODO: 實作重置密碼功能
      alert('密碼已重置');
    }
  }
  
  // 停用/啟用使用者
  toggleUserStatus(user: User): void {
    const action = user.status === '啟用' ? '停用' : '啟用';
    console.log(`${action}使用者`, user);
    
    if (confirm(`確定要${action} ${user.name} 嗎？`)) {
      user.status = user.status === '啟用' ? '停用' : '啟用';
      this.applyFilters();
    }
  }
  
  // 儲存編輯
  saveEdit(): void {
    if (this.editingUser) {
      const index = this.users.findIndex(u => u.id === this.editingUser!.id);
      if (index !== -1) {
        this.users[index] = { ...this.editingUser };
        this.applyFilters();
      }
    }
    this.closeEditDialog();
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
      id: 0,
      account: '',
      department: '',
      name: '',
      email: '',
      status: '啟用'
    };
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