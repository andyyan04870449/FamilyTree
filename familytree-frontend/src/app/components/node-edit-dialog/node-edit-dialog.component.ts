import { Component, Input, Output, EventEmitter, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrgNode } from '../../services/organization-chart.service';

@Component({
  selector: 'app-node-edit-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="dialog-overlay" *ngIf="isOpen" (click)="onCancel()">
      <div class="dialog" (click)="$event.stopPropagation()">
        <div class="dialog-header">
          <h3>{{ node ? '編輯人員資料' : '新增人員' }}</h3>
          <button class="close-btn" (click)="onCancel()">✕</button>
        </div>
        
        <div class="dialog-content">
          <div class="form-group">
            <label>姓名 <span class="required">*</span></label>
            <input
              type="text"
              [(ngModel)]="formData.name"
              placeholder="請輸入姓名"
              required
            />
          </div>
          
          <div class="form-group">
            <label>職位 <span class="required">*</span></label>
            <input
              type="text"
              [(ngModel)]="formData.position"
              placeholder="請輸入職位"
              required
            />
          </div>
          
          <div class="form-group">
            <label>部門 <span class="required">*</span></label>
            <select [(ngModel)]="formData.department" required>
              <option value="">請選擇部門</option>
              <option value="管理層">管理層</option>
              <option value="技術部">技術部</option>
              <option value="營運部">營運部</option>
              <option value="行政部">行政部</option>
              <option value="財務部">財務部</option>
            </select>
          </div>
          
          <div class="form-group">
            <label>電話</label>
            <input
              type="tel"
              [(ngModel)]="formData.phone"
              placeholder="請輸入電話號碼"
            />
          </div>
          
          <div class="form-group">
            <label>電子郵件</label>
            <input
              type="email"
              [(ngModel)]="formData.email"
              placeholder="請輸入電子郵件"
            />
          </div>
          
          <div class="form-group">
            <label>到職日期 <span class="required">*</span></label>
            <input
              type="date"
              [(ngModel)]="formData.joinDate"
              required
            />
          </div>
          
          <div class="form-group">
            <label>大頭照URL</label>
            <input
              type="url"
              [(ngModel)]="formData.avatar"
              placeholder="請輸入大頭照URL"
            />
          </div>
          
          <div class="form-group" *ngIf="!node">
            <label>位置</label>
            <div class="position-inputs">
              <input
                type="number"
                [(ngModel)]="formData.x"
                placeholder="X座標"
                class="position-input"
              />
              <input
                type="number"
                [(ngModel)]="formData.y"
                placeholder="Y座標"
                class="position-input"
              />
            </div>
          </div>
        </div>
        
        <div class="dialog-actions">
          <button class="btn-cancel" (click)="onCancel()">取消</button>
          <button 
            class="btn-confirm" 
            (click)="onSave()"
            [disabled]="!isFormValid()"
          >
            {{ node ? '儲存' : '新增' }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .required {
      color: var(--error);
    }
    
    .form-group {
      margin-bottom: 16px;
      
      label {
        display: block;
        margin-bottom: 6px;
        font-weight: 500;
        color: var(--text-primary);
        font-size: 14px;
      }
      
      input, select, textarea {
        width: 100%;
        padding: 10px 12px;
        border: 1px solid var(--border-secondary);
        border-radius: 6px;
        background: var(--bg-primary);
        color: var(--text-primary);
        font-size: 14px;
        
        &:focus {
          outline: none;
          border-color: var(--primary);
          box-shadow: 0 0 0 2px rgba(var(--primary-rgb), 0.1);
        }
        
        &::placeholder {
          color: var(--text-tertiary);
        }
      }
      
      select {
        cursor: pointer;
      }
    }
    
    .position-inputs {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }
    
    .position-input {
      width: 100%;
    }
  `]
})
export class NodeEditDialogComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() node: OrgNode | null = null;
  
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<OrgNode>();
  
  formData: Partial<OrgNode> = {
    name: '',
    position: '',
    department: '',
    phone: '',
    email: '',
    joinDate: new Date().toISOString().split('T')[0],
    avatar: '',
    x: 400,
    y: 300
  };
  
  ngOnChanges(): void {
    if (this.node) {
      this.formData = { ...this.node };
    } else {
      this.formData = {
        name: '',
        position: '',
        department: '',
        phone: '',
        email: '',
        joinDate: new Date().toISOString().split('T')[0],
        avatar: '',
        x: 400,
        y: 300
      };
    }
  }
  
  isFormValid(): boolean {
    return !!(
      this.formData.name &&
      this.formData.position &&
      this.formData.department &&
      this.formData.joinDate
    );
  }
  
  onCancel(): void {
    this.close.emit();
  }
  
  onSave(): void {
    if (!this.isFormValid()) return;
    
    const nodeData: OrgNode = {
      id: this.node?.id || `node-${Date.now()}`,
      name: this.formData.name!,
      position: this.formData.position!,
      department: this.formData.department!,
      phone: this.formData.phone,
      email: this.formData.email,
      joinDate: this.formData.joinDate!,
      avatar: this.formData.avatar,
      x: this.formData.x || 400,
      y: this.formData.y || 300
    };
    
    this.save.emit(nodeData);
  }
}