// 檔案上傳頁面 - 用於上傳和管理家族樹相關檔案
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.page.html',
  styleUrls: ['./file-upload.page.scss'],
  standalone: true,
  imports: [CommonModule]
})
export class FileUploadComponent {
  constructor() {}
} 