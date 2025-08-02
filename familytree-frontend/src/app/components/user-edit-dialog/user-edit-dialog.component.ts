import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../ui/modal/modal.component';
import { ButtonComponent } from '../ui/button/button.component';
import { UserModel } from '../../services/user.service';
import { CSS_CLASSES } from '../../constants/user-management.constants';

export interface UserEditData {
  username: string;
  email: string;
  fullName: string;
  status: 'active' | 'inactive';
  role?: string;
  password?: string;
}

export interface UserEditResult {
  action: 'save' | 'cancel';
  data?: UserEditData;
}

@Component({
  selector: 'app-user-edit-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent, ButtonComponent],
  template: `
    <app-modal
      [isOpen]="isOpen"
      [title]="isNewUser ? '新增使用者' : '編輯使用者'"
      (close)="onCancel()"
    >
      <div *ngIf="editData" class="space-y-4">
        <!-- 帳號 -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">帳號</label>
          <input
            type="text"
            [(ngModel)]="editData.username"
            [disabled]="!isNewUser"
            placeholder="請輸入帳號"
            [class]="getInputClass(!isNewUser)"
          />
          <p *ngIf="!isNewUser" class="text-xs text-gray-500 mt-1">
            編輯模式下帳號無法修改
          </p>
        </div>
        
        <!-- 密碼 (僅新增時顯示) -->
        <div *ngIf="isNewUser">
          <label class="block text-sm font-medium text-gray-700 mb-2">密碼</label>
          <input
            type="password"
            [(ngModel)]="editData.password"
            placeholder="至少8個字元"
            [class]="CSS_CLASSES.INPUT"
          />
          <p class="text-xs text-gray-500 mt-1">
            密碼須包含大小寫字母、數字，至少8個字元
          </p>
        </div>
        
        <!-- 姓名 -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">姓名</label>
          <input
            type="text"
            [(ngModel)]="editData.fullName"
            placeholder="請輸入姓名"
            [class]="CSS_CLASSES.INPUT"
          />
        </div>
        
        <!-- 電子信箱 -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">電子信箱</label>
          <input
            type="email"
            [(ngModel)]="editData.email"
            placeholder="請輸入電子信箱"
            [class]="CSS_CLASSES.INPUT"
          />
        </div>
        
        <!-- 狀態 -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">狀態</label>
          <select
            [(ngModel)]="editData.status"
            [class]="CSS_CLASSES.SELECT"
          >
            <option value="active">啟用</option>
            <option value="inactive">停用</option>
          </select>
        </div>
        
        <!-- 角色 (僅管理員可見) -->
        <div *ngIf="showRoleField">
          <label class="block text-sm font-medium text-gray-700 mb-2">角色</label>
          <select
            [(ngModel)]="editData.role"
            [class]="CSS_CLASSES.SELECT"
          >
            <option value="user">一般使用者</option>
            <option value="admin">管理員</option>
          </select>
        </div>

        <!-- 表單驗證錯誤訊息 -->
        <div *ngIf="validationErrors.length > 0" class="bg-red-50 border border-red-200 rounded-md p-3">
          <h4 class="text-sm font-medium text-red-800 mb-2">請修正以下錯誤：</h4>
          <ul class="text-sm text-red-700 space-y-1">
            <li *ngFor="let error of validationErrors" class="flex items-center gap-1">
              <span class="text-red-500">•</span>
              {{ error }}
            </li>
          </ul>
        </div>
      </div>
      
      <!-- 操作按鈕 -->
      <div class="flex justify-end gap-3 mt-6">
        <app-button
          variant="secondary"
          label="取消"
          (clicked)="onCancel()"
        ></app-button>
        <app-button
          variant="primary"
          label="儲存"
          [disabled]="!isFormValid()"
          (clicked)="onSave()"
        ></app-button>
      </div>
    </app-modal>
  `
})
export class UserEditDialogComponent implements OnChanges {
  readonly CSS_CLASSES = CSS_CLASSES;

  @Input() isOpen = false;
  @Input() isNewUser = false;
  @Input() showRoleField = false;
  @Input() userData: Partial<UserModel> | null = null;

  @Output() result = new EventEmitter<UserEditResult>();

  editData: UserEditData | null = null;
  validationErrors: string[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['userData'] || changes['isOpen']) {
      this.initializeEditData();
      this.validateForm();
    }
  }

  private initializeEditData(): void {
    if (this.isOpen) {
      if (this.isNewUser) {
        // 新增使用者的預設值
        this.editData = {
          username: '',
          email: '',
          fullName: '',
          status: 'active',
          role: 'user',
          password: ''
        };
      } else if (this.userData) {
        // 編輯現有使用者
        this.editData = {
          username: this.userData.username || '',
          email: this.userData.email || '',
          fullName: this.userData.fullName || '',
          status: this.userData.status || 'active',
          role: this.userData.role || 'user'
        };
      }
    }
  }

  getInputClass(disabled: boolean): string {
    return disabled 
      ? `${this.CSS_CLASSES.INPUT} disabled:bg-gray-50 disabled:text-gray-500`
      : this.CSS_CLASSES.INPUT;
  }

  isFormValid(): boolean {
    this.validateForm();
    return this.validationErrors.length === 0;
  }

  private validateForm(): void {
    this.validationErrors = [];

    if (!this.editData) {
      return;
    }

    // 必填欄位驗證
    if (!this.editData.username.trim()) {
      this.validationErrors.push('帳號為必填欄位');
    }

    if (!this.editData.email.trim()) {
      this.validationErrors.push('電子信箱為必填欄位');
    }

    if (!this.editData.fullName.trim()) {
      this.validationErrors.push('姓名為必填欄位');
    }

    // 新增使用者時密碼為必填
    if (this.isNewUser && !this.editData.password?.trim()) {
      this.validationErrors.push('密碼為必填欄位');
    }

    // 帳號格式驗證
    if (this.editData.username && !/^[a-zA-Z0-9_]+$/.test(this.editData.username)) {
      this.validationErrors.push('帳號只能包含字母、數字和底線');
    }

    // 帳號長度驗證
    if (this.editData.username && (this.editData.username.length < 3 || this.editData.username.length > 20)) {
      this.validationErrors.push('帳號長度必須在3-20個字元之間');
    }

    // 電子信箱格式驗證
    if (this.editData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.editData.email)) {
      this.validationErrors.push('請輸入有效的電子信箱格式');
    }

    // 密碼強度驗證（僅新增時）
    if (this.isNewUser && this.editData.password) {
      if (this.editData.password.length < 8) {
        this.validationErrors.push('密碼長度至少需要8個字元');
      }
      if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(this.editData.password)) {
        this.validationErrors.push('密碼需包含大小寫字母和數字');
      }
    }

    // 姓名長度驗證
    if (this.editData.fullName && (this.editData.fullName.length < 2 || this.editData.fullName.length > 50)) {
      this.validationErrors.push('姓名長度必須在2-50個字元之間');
    }
  }

  onSave(): void {
    if (this.isFormValid() && this.editData) {
      this.result.emit({
        action: 'save',
        data: { ...this.editData }
      });
    }
  }

  onCancel(): void {
    this.result.emit({
      action: 'cancel'
    });
  }
}