// 檔案上傳頁面 - 用於上傳和管理家族樹相關檔案
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FileUploadService, FileUploadModel, UploadProgress } from '../../services/file-upload.service';
import { FileListComponent } from '../../components/file-list/file-list.component';

@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.page.html',
  styleUrls: ['./file-upload.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, FileListComponent]
})
export class FileUploadComponent implements OnInit, OnDestroy {
  selectedFile: File | null = null;
  isUploading = false;
  uploadProgress = 0;
  uploadMessage = '';
  dragOver = false;
  private subscription = new Subscription();

  constructor(private fileUploadService: FileUploadService) {}

  ngOnInit(): void {
    // 載入檔案列表
    this.fileUploadService.refreshFileList();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.handleFileSelection(file);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFileSelection(files[0]);
    }
  }

  private handleFileSelection(file: File): void {
    if (!this.fileUploadService.isValidFileType(file)) {
      this.uploadMessage = '❌ 只支援 .xls 和 .xlsx 檔案格式';
      return;
    }

    this.selectedFile = file;
    this.uploadMessage = `📁 已選擇檔案: ${file.name}`;
  }

  uploadFile(): void {
    if (!this.selectedFile) {
      this.uploadMessage = '❌ 請先選擇檔案';
      return;
    }

    this.isUploading = true;
    this.uploadProgress = 0;
    this.uploadMessage = '📤 正在上傳檔案...';

    this.subscription.add(
      this.fileUploadService.uploadFileWithProgress(this.selectedFile).subscribe({
        next: (result) => {
          if ('progress' in result) {
            // 上傳進度更新
            this.uploadProgress = result.progress;
          } else {
            // 上傳完成
            this.isUploading = false;
            this.uploadProgress = 100;
            
            if (result.success) {
              this.uploadMessage = '✅ 檔案上傳成功！';
              this.selectedFile = null;
              // 重新整理檔案列表
              this.fileUploadService.refreshFileList();
            } else {
              if (result.isDuplicate) {
                this.uploadMessage = '⚠️ 檔案已存在，無法重複上傳';
              } else {
                this.uploadMessage = `❌ 上傳失敗: ${result.message}`;
              }
            }
            
            // 3秒後清除訊息
            setTimeout(() => {
              this.uploadMessage = '';
              this.uploadProgress = 0;
            }, 3000);
          }
        },
        error: (err) => {
          this.isUploading = false;
          this.uploadProgress = 0;
          this.uploadMessage = '❌ 上傳過程中發生錯誤';
          console.error('檔案上傳錯誤:', err);
          
          setTimeout(() => {
            this.uploadMessage = '';
          }, 3000);
        }
      })
    );
  }

  clearSelection(): void {
    this.selectedFile = null;
    this.uploadMessage = '';
    this.uploadProgress = 0;
  }

  formatFileSize(bytes: number): string {
    return this.fileUploadService.formatFileSize(bytes);
  }
} 