// 檔案管理頁面：用於查看、刪除和管理已上傳的檔案
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-file-management',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="file-management-container">
      <h2>📁 檔案管理</h2>
      <div class="content">
        <p>檔案管理功能正在開發中...</p>
      </div>
    </div>
  `,
  styles: [`
    .file-management-container {
      padding: 20px;
    }
    
    .content {
      margin-top: 20px;
    }
  `]
})
export class FileManagementComponent {
  constructor() {}
} 