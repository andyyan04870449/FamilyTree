// 檔案上傳頁面 - 用於上傳和管理家族樹相關檔案
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FileUploadService, FileUploadModel, UploadProgress } from '../../services/file-upload.service';
import { PhotoUploadService, PhotoUploadProgress, PhotoUploadResponse } from '../../services/photo-upload.service';
import { ProjectService } from '../../services/project.service';
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
  uploadType: 'excel' | 'photo' = 'excel';
  photoList: any[] = []; // 預設為Excel上傳
  private subscription = new Subscription();

  constructor(
    private fileUploadService: FileUploadService,
    private photoUploadService: PhotoUploadService,
    private projectService: ProjectService
  ) {
    // 訂閱照片列表
    this.subscription.add(
      this.photoUploadService.photos$.subscribe(photos => {
        this.photoList = photos;
        console.log('📸 [FileUpload] 收到照片列表更新:', photos);
      })
    );
  }

  ngOnInit(): void {
    // 載入檔案列表
    console.log('🔍 [FileUpload] ngOnInit - 檢查當前專案:', this.projectService.getCurrentProject());
    
    // 確保有專案才載入檔案列表
    const currentProject = this.projectService.getCurrentProject();
    if (currentProject) {
      console.log('📁 [FileUpload] 載入專案檔案列表:', currentProject.id);
      this.fileUploadService.refreshFileList();
      this.photoUploadService.refreshPhotoList();
    } else {
      console.warn('⚠️ [FileUpload] 沒有當前專案，無法載入檔案列表');
    }
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
    // 根據上傳類型驗證檔案
    const isValidFile = this.uploadType === 'excel' 
      ? this.fileUploadService.isValidFileType(file)
      : this.photoUploadService.isValidPhotoType(file);

    if (!isValidFile) {
      const supportedFormats = this.uploadType === 'excel' 
        ? '.xls 和 .xlsx' 
        : '.jpg, .jpeg, .png, .zip, .7z';
      this.uploadMessage = `❌ 只支援 ${supportedFormats} 檔案格式`;
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

    if (this.uploadType === 'excel') {
      this.uploadExcelFile();
    } else {
      this.uploadPhotoFile();
    }
  }

  private uploadExcelFile(): void {
    this.subscription.add(
      this.fileUploadService.uploadFileWithProgress(this.selectedFile!).subscribe({
        next: (result) => {
          if ('progress' in result) {
            this.uploadProgress = result.progress;
          } else {
            this.handleUploadComplete(result);
          }
        },
        error: (err) => this.handleUploadError(err)
      })
    );
  }

  private uploadPhotoFile(): void {
    const currentProject = this.projectService.getCurrentProject();
    if (!currentProject) {
      this.uploadMessage = '❌ 請先選擇專案';
      this.isUploading = false;
      return;
    }

    this.subscription.add(
      this.photoUploadService.uploadPhotoWithProgress(this.selectedFile!, currentProject.id).subscribe({
        next: (result) => {
          if ('progress' in result) {
            this.uploadProgress = result.progress;
          } else {
            this.handlePhotoUploadComplete(result);
          }
        },
        error: (err) => this.handleUploadError(err)
      })
    );
  }

  private handleUploadComplete(result: any): void {
    this.isUploading = false;
    this.uploadProgress = 100;
    
    if (result.success) {
      this.uploadMessage = '✅ 檔案上傳成功！';
      this.selectedFile = null;
      this.fileUploadService.refreshFileList();
    } else {
      if (result.isDuplicate) {
        this.uploadMessage = '⚠️ 檔案已存在，無法重複上傳';
      } else {
        this.uploadMessage = `❌ 上傳失敗: ${result.message}`;
      }
    }
    
    setTimeout(() => {
      this.uploadMessage = '';
      this.uploadProgress = 0;
    }, 3000);
  }

  private handlePhotoUploadComplete(result: any): void {
    this.isUploading = false;
    this.uploadProgress = 100;
    
    if (result.success) {
      const uploadedCount = result.uploadedFiles?.length || result.totalUploaded || 1;
      const failedCount = result.failedFiles?.length || result.totalFailed || 0;
      
      if (failedCount > 0) {
        this.uploadMessage = `✅ 照片上傳完成！成功: ${uploadedCount}，失敗: ${failedCount}`;
      } else {
        this.uploadMessage = `✅ 照片上傳成功！共 ${uploadedCount} 張照片`;
      }
      this.selectedFile = null;
      
      // 刷新照片列表
      this.photoUploadService.refreshPhotoList();
    } else {
      this.uploadMessage = `❌ 照片上傳失敗: ${result.message}`;
    }
    
    setTimeout(() => {
      this.uploadMessage = '';
      this.uploadProgress = 0;
    }, 3000);
  }

  private handleUploadError(err: any): void {
    this.isUploading = false;
    this.uploadProgress = 0;
    this.uploadMessage = '❌ 上傳過程中發生錯誤';
    console.error('檔案上傳錯誤:', err);
    
    setTimeout(() => {
      this.uploadMessage = '';
    }, 3000);
  }

  clearSelection(): void {
    this.selectedFile = null;
    this.uploadMessage = '';
    this.uploadProgress = 0;
  }

  formatFileSize(bytes: number): string {
    return this.fileUploadService.formatFileSize(bytes);
  }

  // 切換上傳類型
  onUploadTypeChange(type: 'excel' | 'photo'): void {
    this.uploadType = type;
    this.clearSelection();
    
    // 根據類型刷新對應的列表
    const currentProject = this.projectService.getCurrentProject();
    if (currentProject) {
      if (type === 'excel') {
        this.fileUploadService.refreshFileList();
      } else {
        this.photoUploadService.refreshPhotoList();
      }
    }
  }

  // 取得支援的檔案格式說明
  getSupportedFormats(): string {
    return this.uploadType === 'excel' 
      ? 'XLS, XLSX' 
      : 'JPG, PNG, ZIP, 7Z';
  }

  // 取得檔案接受屬性
  getAcceptAttribute(): string {
    return this.uploadType === 'excel' 
      ? '.xls,.xlsx' 
      : '.jpg,.jpeg,.png,.zip,.7z';
  }

  // 取得上傳類型圖示
  getUploadIcon(): string {
    return this.uploadType === 'excel' ? '📊' : '📸';
  }

  // 取得上傳類型名稱
  getUploadTypeName(): string {
    return this.uploadType === 'excel' ? 'Excel 資料' : '照片檔案';
  }

  // 格式化上傳時間
  formatUploadTime(uploadTime: string): string {
    try {
      const date = new Date(uploadTime);
      return date.toLocaleString('zh-TW', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return uploadTime;
    }
  }
} 