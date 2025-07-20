// 檔案列表組件 - 顯示已上傳的檔案和其狀態
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { FileUploadService, FileUploadModel } from '../../services/file-upload.service';

@Component({
  selector: 'app-file-list',
  templateUrl: './file-list.component.html',
  styleUrls: ['./file-list.component.scss'],
  standalone: true,
  imports: [CommonModule]
})
export class FileListComponent implements OnInit, OnDestroy {
  files: FileUploadModel[] = [];
  loading = false;
  error = '';
  private subscription = new Subscription();

  constructor(private fileUploadService: FileUploadService) {}

  ngOnInit(): void {
    this.loadFiles();
    
    // 訂閱檔案列表更新
    this.subscription.add(
      this.fileUploadService.files$.subscribe(files => {
        this.files = files;
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  loadFiles(): void {
    this.loading = true;
    this.error = '';

    this.subscription.add(
      this.fileUploadService.getFileList().subscribe({
        next: (response) => {
          this.loading = false;
          if (!response.success) {
            this.error = response.message;
          }
        },
        error: (err) => {
          this.loading = false;
          this.error = '載入檔案列表失敗';
          console.error('載入檔案列表錯誤:', err);
        }
      })
    );
  }

  deleteFile(file: FileUploadModel): void {
    if (confirm(`確定要刪除檔案 "${file.originalFilename}" 嗎？`)) {
      this.subscription.add(
        this.fileUploadService.deleteFile(file.id).subscribe({
          next: (response) => {
            if (response.success) {
              console.log('檔案刪除成功');
            } else {
              alert(`刪除失敗: ${response.message}`);
            }
          },
          error: (err) => {
            alert('刪除檔案時發生錯誤');
            console.error('刪除檔案錯誤:', err);
          }
        })
      );
    }
  }

  formatFileSize(bytes: number): string {
    return this.fileUploadService.formatFileSize(bytes);
  }

  formatDate(dateString: string): string {
    return this.fileUploadService.formatDate(dateString);
  }

  getStatusText(status: string): string {
    return this.fileUploadService.getStatusText(status);
  }

  getStatusColor(status: string): string {
    return this.fileUploadService.getStatusColor(status);
  }

  refreshList(): void {
    this.loadFiles();
  }
} 