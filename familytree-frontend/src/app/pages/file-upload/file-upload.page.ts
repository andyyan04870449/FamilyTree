// 檔案上傳頁面 - 用於上傳和管理家族樹相關檔案
// 主要功能：智能檔案類型檢測、自動選擇處理邏輯、統一檔案列表顯示
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FileUploadService, FileModel, UploadProgress } from '../../services/file-upload.service';
import { PhotoUploadService, PhotoUploadProgress, PhotoUploadResponse } from '../../services/photo-upload.service';
import { ProjectService } from '../../services/project.service';

// 檔案記錄統一介面
interface UnifiedFileRecord {
  id: string;
  originalName: string;
  savedName?: string;
  fileSize: number;
  uploadTime: string;
  fileType: 'excel' | 'photo' | 'image' | 'archive' | 'unknown';
  filePath?: string;
  md5Hash?: string;
  status?: string;
  isProcessed?: boolean;
  relatedPersonsCount?: number;
}

@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.page.html',
  styleUrls: ['./file-upload.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class FileUploadComponent implements OnInit, OnDestroy {
  selectedFile: File | null = null;
  isUploading = false;
  uploadProgress = 0;
  uploadMessage = '';
  dragOver = false;
  detectedFileType: 'excel' | 'photo' | 'unknown' = 'unknown';
  
  // 統一的檔案記錄列表
  allFileRecords: UnifiedFileRecord[] = [];
  
  // 刪除狀態
  isDeleting = false;
  
  private subscription = new Subscription();

  constructor(
    private fileUploadService: FileUploadService,
    private photoUploadService: PhotoUploadService,
    private projectService: ProjectService
  ) {
    // 訂閱Excel檔案列表
    this.subscription.add(
      this.fileUploadService.files$.subscribe(excelFiles => {
        console.log('📊 [FileUpload] 收到Excel檔案列表更新:', excelFiles);
        this.updateUnifiedFileList();
      })
    );

    // 訂閱照片列表
    this.subscription.add(
      this.photoUploadService.photos$.subscribe(photos => {
        console.log('📸 [FileUpload] 收到照片列表更新:', photos);
        this.updateUnifiedFileList();
      })
    );

    // 訂閱專案變更，當專案切換時重新載入檔案列表
    this.subscription.add(
      this.projectService.currentProject$.subscribe(project => {
        if (project) {
          console.log('🔄 [FileUpload] 專案切換到:', project.projectName, '重新載入檔案列表');
          this.fileUploadService.refreshFileList();
          this.photoUploadService.refreshPhotoList();
        } else {
          console.log('⚠️ [FileUpload] 專案已清除，清空檔案列表');
          this.allFileRecords = [];
        }
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

  /**
   * 獲取當前專案
   */
  getCurrentProject() {
    return this.projectService.getCurrentProject();
  }

  /**
   * 智能檔案類型檢測
   */
  private detectFileType(file: File): 'excel' | 'photo' | 'unknown' {
    const fileName = file.name.toLowerCase();
    const fileExtension = fileName.split('.').pop() || '';
    
    // Excel 檔案類型
    const excelExtensions = ['xls', 'xlsx'];
    if (excelExtensions.includes(fileExtension)) {
      return 'excel';
    }
    
    // 照片檔案類型
    const photoExtensions = ['jpg', 'jpeg', 'png', 'zip', '7z'];
    if (photoExtensions.includes(fileExtension)) {
      return 'photo';
    }
    
    return 'unknown';
  }

  /**
   * 驗證檔案類型是否支援
   */
  private isValidFileType(file: File): boolean {
    return this.detectFileType(file) !== 'unknown';
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
    // 智能檢測檔案類型
    this.detectedFileType = this.detectFileType(file);
    
    if (this.detectedFileType === 'unknown') {
      this.uploadMessage = '❌ 不支援的檔案格式，請上傳 Excel (.xls/.xlsx) 或圖片檔案 (.jpg/.png/.zip/.7z)';
      return;
    }

    this.selectedFile = file;
    const fileTypeText = this.detectedFileType === 'excel' ? 'Excel 資料檔' : '照片檔案';
    this.uploadMessage = `📁 已選擇${fileTypeText}: ${file.name}`;
  }

  uploadFile(): void {
    if (!this.selectedFile) {
      this.uploadMessage = '❌ 請先選擇檔案';
      return;
    }

    if (this.detectedFileType === 'unknown') {
      this.uploadMessage = '❌ 檔案格式不支援';
      return;
    }

    this.isUploading = true;
    this.uploadProgress = 0;
    this.uploadMessage = `📤 正在上傳${this.getFileTypeText()}...`;

    if (this.detectedFileType === 'excel') {
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
          console.log('📊 [FileUpload] 收到上傳結果:', result);
          
          // 檢查是否為進度更新（PhotoUploadProgress類型）
          if ('status' in result && result.status === 'uploading') {
            console.log('📈 [FileUpload] 更新進度:', result.progress + '%');
            this.uploadProgress = result.progress;
          }
          // 檢查是否為完成狀態（PhotoUploadResponse類型或包含success的結果）
          else if ('success' in result || !('status' in result)) {
            console.log('✅ [FileUpload] 檢測到完成狀態，調用完成處理');
            this.handlePhotoUploadComplete(result);
          }
          // 備用邏輯：如果有progress但不是uploading狀態，可能是完成了
          else if ('progress' in result && result.progress === 100) {
            console.log('✅ [FileUpload] 進度100%，可能已完成，調用完成處理');
            this.handlePhotoUploadComplete(result);
          }
          else {
            console.warn('⚠️ [FileUpload] 未知的結果格式:', result);
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
      this.uploadMessage = '✅ Excel 檔案上傳成功！';
      this.selectedFile = null;
      this.detectedFileType = 'unknown';
      this.fileUploadService.refreshFileList();
    } else {
      if (result.isDuplicate) {
        this.uploadMessage = '⚠️ 檔案已存在，但已記錄到列表';
        // 重複檔案也需要刷新列表，因為後端已記錄到資料庫
        this.fileUploadService.refreshFileList();
        this.selectedFile = null;
        this.detectedFileType = 'unknown';
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
    console.log('🎯 [FileUpload] 處理照片上傳完成:', result);
    
    this.isUploading = false;
    this.uploadProgress = 100;
    
    if (result.success) {
      // 從新的資料格式中提取統計資訊
      const data = result.data || {};
      const uploadedCount = data.totalUploaded || data.uploadedFiles?.length || 1;
      const failedCount = data.totalFailed || data.failedFiles?.length || 0;
      
      console.log('📊 [FileUpload] 上傳統計:', {
        uploadedCount,
        failedCount,
        uploadedFiles: data.uploadedFiles,
        failedFiles: data.failedFiles
      });
      
      if (failedCount > 0) {
        this.uploadMessage = `✅ 照片上傳完成！成功: ${uploadedCount}，失敗: ${failedCount}`;
      } else {
        this.uploadMessage = `✅ 照片上傳成功！共 ${uploadedCount} 張照片`;
      }
      this.selectedFile = null;
      this.detectedFileType = 'unknown';
      
      // 刷新照片列表
      this.photoUploadService.refreshPhotoList();
      
      console.log('✅ [FileUpload] 照片上傳完成處理完畢');
    } else {
      const errorMessage = result.message || '未知錯誤';
      this.uploadMessage = `❌ 照片上傳失敗: ${errorMessage}`;
      console.error('❌ [FileUpload] 照片上傳失敗:', result);
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
    this.detectedFileType = 'unknown';
  }

  formatFileSize(bytes: number): string {
    return this.fileUploadService.formatFileSize(bytes);
  }

  /**
   * 更新統一檔案列表
   */
  private updateUnifiedFileList(): void {
    const currentProject = this.projectService.getCurrentProject();
    console.log('🔄 [FileUpload] 更新檔案列表，當前專案:', currentProject?.projectName || '無');
    
    const excelFiles = this.fileUploadService.getCurrentFiles();
    const photoFiles = this.photoUploadService.getCurrentPhotos();
    
    console.log('📊 [FileUpload] Excel檔案數量:', excelFiles.length);
    console.log('📸 [FileUpload] 照片檔案數量:', photoFiles.length);
    
    // 轉換Excel檔案格式
    const excelRecords: UnifiedFileRecord[] = excelFiles.map(file => ({
      id: `excel_${file.fileId}`,
      originalName: file.originalFilename,
      savedName: file.filename,
      fileSize: file.fileSize,
      uploadTime: file.uploadedAt,
      fileType: file.fileType as any || 'excel',
      filePath: file.filePath,
      md5Hash: file.md5Hash,
      status: file.uploadStatus,
      isProcessed: file.isProcessed,
      relatedPersonsCount: file.relatedPersonsCount || 0
    }));
    
    // 轉換照片檔案格式（適應PhotoFileInfo介面）
    const photoRecords: UnifiedFileRecord[] = photoFiles.map(photo => ({
      id: `photo_${photo.id}`,
      originalName: photo.originalFileName,
      savedName: photo.savedFileName,
      fileSize: photo.fileSize,
      uploadTime: photo.uploadTime,
      fileType: 'photo',
      filePath: undefined, // PhotoFileInfo沒有此屬性
      md5Hash: undefined   // PhotoFileInfo沒有此屬性
    }));
    
    // 合併並按上傳時間排序（最新的在前）
    this.allFileRecords = [...excelRecords, ...photoRecords]
      .sort((a, b) => new Date(b.uploadTime).getTime() - new Date(a.uploadTime).getTime());
    
    console.log('📋 [FileUpload] 統一檔案列表已更新:', this.allFileRecords.length, '個檔案');
    console.log('📋 [FileUpload] 檔案詳情:', this.allFileRecords.map(f => ({ name: f.originalName, type: f.fileType })));
  }

  // 取得支援的檔案格式說明
  getSupportedFormats(): string {
    return 'Excel (.xls/.xlsx) 或 圖片檔案 (.jpg/.png/.zip/.7z)';
  }

  // 取得檔案接受屬性
  getAcceptAttribute(): string {
    return '.xls,.xlsx,.jpg,.jpeg,.png,.zip,.7z';
  }

  // 取得檔案類型圖示
  getFileTypeIcon(fileType?: 'excel' | 'photo' | 'image' | 'archive' | 'unknown'): string {
    const type = fileType || this.detectedFileType;
    switch (type) {
      case 'excel': return '📊';
      case 'photo': return '📸';
      case 'image': return '🖼️';
      case 'archive': return '📦';
      default: return '📁';
    }
  }

  // 取得檔案類型名稱
  getFileTypeText(fileType?: 'excel' | 'photo' | 'image' | 'archive' | 'unknown'): string {
    const type = fileType || this.detectedFileType;
    switch (type) {
      case 'excel': return 'Excel 資料';
      case 'photo': return '照片檔案';
      case 'image': return '圖片檔案';
      case 'archive': return '壓縮檔案';
      default: return '檔案';
    }
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

  // 取得檔案狀態文字
  getFileStatusText(record: UnifiedFileRecord): string {
    if (record.fileType === 'excel') {
      if (record.isProcessed) {
        return '已處理';
      }
      return record.status === 'uploaded' ? '已上傳' : 
             record.status === 'processing' ? '處理中' : 
             record.status === 'failed' ? '失敗' : '已上傳';
    }
    return '已上傳';
  }

  // 取得檔案狀態樣式
  getFileStatusClass(record: UnifiedFileRecord): string {
    if (record.fileType === 'excel') {
      if (record.isProcessed) {
        return 'status-merged';
      }
      return record.status === 'uploaded' ? 'status-uploaded' : 'status-processing';
    }
    return 'status-uploaded';
  }

  /**
   * 刪除檔案
   */
  deleteFile(record: UnifiedFileRecord): void {
    const fileName = record.originalName;
    const fileType = record.fileType === 'excel' ? 'Excel檔案' : '照片檔案';
    
    if (!confirm(`確定要刪除${fileType}「${fileName}」嗎？\n\n注意：此操作無法復原。`)) {
      return;
    }

    this.isDeleting = true;
    console.log(`🗑️ [FileUpload] 開始刪除${fileType}:`, record.id);

    // 從 ID 中提取實際的檔案 ID
    const fileId = record.id.split('_')[1];
    
    if (record.fileType === 'excel' || record.fileType === 'image' || record.fileType === 'archive') {
      this.deleteExcelFile(fileId, fileName);
    } else {
      // 照片仍然使用數字 ID
      const photoId = parseInt(fileId);
      this.deletePhotoFile(photoId, fileName);
    }
  }

  /**
   * 刪除 Excel 檔案
   */
  private deleteExcelFile(fileId: string, fileName: string): void {
    console.log(`🔍 [FileUpload] 準備刪除Excel檔案，fileId: ${fileId}, fileName: ${fileName}`);
    this.subscription.add(
      this.fileUploadService.deleteFile(fileId).subscribe({
        next: (response) => {
          this.isDeleting = false;
          if (response.success) {
            this.uploadMessage = `✅ Excel檔案「${fileName}」刪除成功`;
            console.log('✅ [FileUpload] Excel檔案刪除成功:', fileName);
            
            // 重新載入檔案列表
            this.fileUploadService.refreshFileList();
          } else {
            this.uploadMessage = `❌ 刪除失敗: ${response.message}`;
            console.error('❌ [FileUpload] Excel檔案刪除失敗:', response.message);
          }
          
          // 清除訊息
          setTimeout(() => {
            this.uploadMessage = '';
          }, 3000);
        },
        error: (error) => {
          this.isDeleting = false;
          this.uploadMessage = `❌ 刪除Excel檔案時發生錯誤`;
          console.error('❌ [FileUpload] 刪除Excel檔案時發生錯誤:', error);
          
          setTimeout(() => {
            this.uploadMessage = '';
          }, 3000);
        }
      })
    );
  }

  /**
   * 刪除照片檔案
   */
  private deletePhotoFile(photoId: number, fileName: string): void {
    this.subscription.add(
      this.photoUploadService.deletePhoto(photoId).subscribe({
        next: (response) => {
          this.isDeleting = false;
          if (response.success !== false) { // PhotoUploadService 的回應格式可能不同
            this.uploadMessage = `✅ 照片檔案「${fileName}」刪除成功`;
            console.log('✅ [FileUpload] 照片檔案刪除成功:', fileName);
            
            // 重新載入照片列表
            this.photoUploadService.refreshPhotoList();
          } else {
            this.uploadMessage = `❌ 刪除失敗: ${response.message || '未知錯誤'}`;
            console.error('❌ [FileUpload] 照片檔案刪除失敗:', response);
          }
          
          // 清除訊息
          setTimeout(() => {
            this.uploadMessage = '';
          }, 3000);
        },
        error: (error) => {
          this.isDeleting = false;
          this.uploadMessage = `❌ 刪除照片檔案時發生錯誤`;
          console.error('❌ [FileUpload] 刪除照片檔案時發生錯誤:', error);
          
          setTimeout(() => {
            this.uploadMessage = '';
          }, 3000);
        }
      })
    );
  }
} 