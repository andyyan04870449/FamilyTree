// 檔案上傳頁面 - 用於上傳和管理家族樹相關檔案
// 主要功能：智能檔案類型檢測、自動選擇處理邏輯、統一檔案列表顯示
import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { FileUploadService, FileModel, UploadProgress } from '../../services/file-upload.service';
import { PhotoUploadService, PhotoUploadProgress, PhotoUploadResponse } from '../../services/photo-upload.service';
import { ProjectService } from '../../services/project.service';
import { AppConstants } from '../../constants/app.constants';
import { ButtonComponent } from '../../components/ui/button/button.component';
import { cn } from '../../utils/cn';

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
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent],
  template: `
    <div class="min-h-screen bg-gray-50 p-6">
      <div class="max-w-6xl mx-auto space-y-6">
        <!-- Page Header -->
        <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
              <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
            </div>
            <div>
              <h1 class="text-2xl font-bold text-gray-900">檔案上傳</h1>
              <p class="text-gray-600">智能檔案上傳 - 自動識別檔案類型並選擇最佳處理方式</p>
            </div>
          </div>
          
          <!-- Current Project Info -->
          <div *ngIf="getCurrentProject()" class="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <div class="flex items-center gap-2 text-blue-700">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
              </svg>
              <span class="font-medium">當前案件名稱：</span>
              <span class="font-semibold">{{ getCurrentProject()?.projectName }}</span>
            </div>
          </div>
          
          <div *ngIf="!getCurrentProject()" class="bg-amber-50 border border-amber-200 rounded-lg p-4">
            <div class="flex items-center gap-2 text-amber-700">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"/>
              </svg>
              <span class="font-medium">請先從案件管理頁面選擇一個案件</span>
            </div>
          </div>
        </div>

        <!-- Upload Section -->
        <div *ngIf="getCurrentProject()" class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <h2 class="text-lg font-semibold text-gray-900 mb-4">上傳檔案</h2>
          
          <!-- Upload Area -->
          <div 
            class="border-2 border-dashed rounded-lg transition-all duration-200"
            [class]="cn(
              'border-gray-300 bg-gray-50',
              dragOver ? 'border-blue-500 bg-blue-50' : '',
              selectedFile ? 'border-green-500 bg-green-50' : ''
            )"
            (dragover)="onDragOver($event)"
            (dragleave)="onDragLeave($event)"
            (drop)="onDrop($event)"
          >
            <!-- Upload Placeholder -->
            <div *ngIf="!selectedFile" class="p-8 text-center">
              <svg class="w-12 h-12 text-gray-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"/>
              </svg>
              <h3 class="text-lg font-medium text-gray-900 mb-2">拖拽檔案到此處或點擊上傳</h3>
              <p class="text-gray-600 mb-4">{{ getSupportedFormats() }}</p>
              
              <!-- Format Examples -->
              <div class="flex flex-wrap justify-center gap-4 mb-6">
                <div class="flex items-center gap-2 bg-white rounded-lg px-3 py-2 border border-gray-200">
                  <span class="text-xl">📊</span>
                  <span class="text-sm text-gray-700">Excel 資料：.xls, .xlsx</span>
                </div>
                <div class="flex items-center gap-2 bg-white rounded-lg px-3 py-2 border border-gray-200">
                  <span class="text-xl">📸</span>
                  <span class="text-sm text-gray-700">照片檔案：.jpg, .png, .zip, .7z</span>
                </div>
              </div>
              
              <input 
                type="file" 
                #fileInput 
                [accept]="getAcceptAttribute()" 
                (change)="onFileSelected($event)" 
                class="hidden"
              >
              <app-button
                variant="primary"
                size="lg"
                label="選擇檔案"
                icon="📎"
                (clicked)="fileInput.click()"
              ></app-button>
            </div>
            
            <!-- Selected File -->
            <div *ngIf="selectedFile" class="p-6">
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-4">
                  <div class="text-4xl">{{ getFileTypeIcon() }}</div>
                  <div>
                    <h4 class="text-lg font-medium text-gray-900">{{ selectedFile.name }}</h4>
                    <p class="text-gray-600">{{ formatFileSize(selectedFile.size) }} • {{ getFileTypeText() }}</p>
                    <div *ngIf="detectedFileType !== 'unknown'" class="flex items-center gap-2 mt-1 text-sm text-green-600">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
                      </svg>
                      <span>已自動識別為{{ getFileTypeText() }}</span>
                    </div>
                  </div>
                </div>
                
                <div class="flex gap-2">
                  <app-button
                    variant="primary"
                    size="md"
                    label="開始上傳"
                    loadingText="上傳中..."
                    [loading]="isUploading"
                    [disabled]="isUploading || detectedFileType === 'unknown'"
                    (clicked)="uploadFile()"
                  ></app-button>
                  <app-button
                    variant="secondary"
                    size="md"
                    label="重新選擇"
                    [disabled]="isUploading"
                    (clicked)="clearSelection()"
                  ></app-button>
                </div>
              </div>
            </div>
          </div>
          
          <!-- Upload Progress -->
          <div *ngIf="isUploading || uploadProgress > 0" class="mt-4">
            <div class="flex items-center justify-between mb-2">
              <span class="text-sm font-medium text-gray-700">上傳進度</span>
              <span class="text-sm font-medium text-gray-700">{{ uploadProgress }}%</span>
            </div>
            <div class="w-full bg-gray-200 rounded-full h-2">
              <div 
                class="bg-blue-600 h-2 rounded-full transition-all duration-300"
                [style.width.%]="uploadProgress"
              ></div>
            </div>
          </div>
          
          <!-- Upload Message -->
          <div *ngIf="uploadMessage" class="mt-4">
            <div 
              class="p-4 rounded-lg border"
              [class]="cn(
                uploadMessage.includes('❌') ? 'bg-red-50 border-red-200 text-red-700' : '',
                uploadMessage.includes('✅') ? 'bg-green-50 border-green-200 text-green-700' : '',
                !uploadMessage.includes('❌') && !uploadMessage.includes('✅') ? 'bg-blue-50 border-blue-200 text-blue-700' : ''
              )"
            >
              <p class="text-sm font-medium">{{ uploadMessage }}</p>
            </div>
          </div>
        </div>

        <!-- File List Section -->
        <div *ngIf="getCurrentProject()" class="bg-white rounded-lg shadow-sm border border-gray-200">
          <div class="p-6 border-b border-gray-200">
            <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
              📋 檔案記錄
              <span class="bg-gray-100 text-gray-600 text-sm px-2 py-1 rounded-full">{{ allFileRecords.length }}</span>
            </h2>
          </div>
          
          <div class="p-6">
            <!-- File Grid -->
            <div *ngIf="allFileRecords.length > 0" class="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              <div 
                *ngFor="let record of allFileRecords"
                class="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
              >
                <div class="flex items-start justify-between">
                  <div class="flex items-center gap-3 flex-1 min-w-0">
                    <div class="text-2xl flex-shrink-0">{{ getFileTypeIcon(record.fileType) }}</div>
                    <div class="flex-1 min-w-0">
                      <h4 class="text-sm font-medium text-gray-900 truncate">{{ record.originalName }}</h4>
                      <p class="text-xs text-gray-500 mt-1">
                        {{ formatFileSize(record.fileSize) }} • {{ formatUploadTime(record.uploadTime) }}
                      </p>
                      <div *ngIf="record.fileType === 'excel' && record.relatedPersonsCount !== undefined" 
                           class="flex items-center gap-1 mt-1 text-xs text-gray-600">
                        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/>
                        </svg>
                        <span>{{ record.relatedPersonsCount }} 人</span>
                      </div>
                      <div *ngIf="record.savedName && record.savedName !== record.originalName" 
                           class="text-xs text-gray-500 mt-1">
                        儲存為: {{ record.savedName }}
                      </div>
                      <div class="flex gap-2 mt-2">
                        <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                              [class]="cn(
                                record.fileType === 'excel' ? 'bg-green-100 text-green-800' : '',
                                record.fileType === 'photo' ? 'bg-blue-100 text-blue-800' : '',
                                record.fileType === 'image' ? 'bg-purple-100 text-purple-800' : '',
                                record.fileType === 'archive' ? 'bg-orange-100 text-orange-800' : ''
                              )">
                          {{ getFileTypeText(record.fileType) }}
                        </span>
                        <span class="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                              [class]="cn(
                                getFileStatusText(record) === '已處理' ? 'bg-green-100 text-green-800' : '',
                                getFileStatusText(record) === '已上傳' ? 'bg-gray-100 text-gray-800' : '',
                                getFileStatusText(record) === '處理中' ? 'bg-yellow-100 text-yellow-800' : '',
                                getFileStatusText(record) === '失敗' ? 'bg-red-100 text-red-800' : ''
                              )">
                          {{ getFileStatusText(record) }}
                        </span>
                      </div>
                    </div>
                  </div>
                  
                  <app-button
                    variant="danger"
                    size="sm"
                    icon="🗑️"
                    title="刪除檔案"
                    [loading]="isDeleting"
                    loadingText="刪除中..."
                    [disabled]="isDeleting"
                    (clicked)="deleteFile(record)"
                  ></app-button>
                </div>
              </div>
            </div>
            
            <!-- Empty State -->
            <div *ngIf="allFileRecords.length === 0" class="text-center py-12">
              <div class="text-gray-400 text-6xl mb-4">📂</div>
              <h4 class="text-lg font-medium text-gray-900 mb-2">尚未上傳任何檔案</h4>
              <p class="text-gray-600">上傳第一個檔案開始管理您的專案資料</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class FileUploadComponent implements OnInit, OnDestroy {
  selectedFile: File | null = null;
  isUploading = false;
  uploadProgress = 0;
  uploadMessage = '';
  dragOver = false;
  detectedFileType: string = AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN;
  
  // 統一的檔案記錄列表
  allFileRecords: UnifiedFileRecord[] = [];
  
  // 刪除狀態
  isDeleting = false;
  
  private subscription = new Subscription();
  
  // Utility function for class names
  cn = cn;

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
  private detectFileType(file: File): string {
    return AppConstants.getFileTypeByExtension(file.name);
  }

  /**
   * 驗證檔案類型是否支援
   */
  private isValidFileType(file: File): boolean {
    return AppConstants.isValidFileType(file.name);
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
    
    if (this.detectedFileType === AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN) {
      this.uploadMessage = `❌ ${AppConstants.FILE_SYSTEM.MESSAGES.INVALID_FILE_TYPE}，請上傳 ${AppConstants.FILE_SYSTEM.ALLOWED_EXTENSIONS.join(', ')} 格式檔案`;
      return;
    }

    this.selectedFile = file;
    const fileTypeText = AppConstants.getFileTypeName(this.detectedFileType);
    this.uploadMessage = `📁 已選擇${fileTypeText}: ${file.name}`;
  }

  uploadFile(): void {
    if (!this.selectedFile) {
      this.uploadMessage = `❌ ${AppConstants.FILE_SYSTEM.MESSAGES.SELECT_FILE_FIRST}`;
      return;
    }

    if (this.detectedFileType === AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN) {
      this.uploadMessage = `❌ ${AppConstants.FILE_SYSTEM.MESSAGES.INVALID_FILE_TYPE}`;
      return;
    }

    this.isUploading = true;
    this.uploadProgress = 0;
    this.uploadMessage = `📤 正在上傳${this.getFileTypeText()}...`;

    if (this.detectedFileType === AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.EXCEL) {
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
      this.uploadMessage = `✅ ${AppConstants.FILE_SYSTEM.MESSAGES.UPLOAD_SUCCESS}！`;
      this.selectedFile = null;
      this.detectedFileType = AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN;
      this.fileUploadService.refreshFileList();
    } else {
      if (result.isDuplicate) {
        this.uploadMessage = '⚠️ 檔案已存在，但已記錄到列表';
        // 重複檔案也需要刷新列表，因為後端已記錄到資料庫
        this.fileUploadService.refreshFileList();
        this.selectedFile = null;
        this.detectedFileType = AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN;
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
        this.uploadMessage = `✅ ${AppConstants.FILE_SYSTEM.MESSAGES.UPLOAD_SUCCESS}！共 ${uploadedCount} 張照片`;
      }
      this.selectedFile = null;
      this.detectedFileType = AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN;
      
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
    this.detectedFileType = AppConstants.FILE_SYSTEM.SUPPORTED_TYPES.UNKNOWN;
  }

  formatFileSize(bytes: number): string {
    return AppConstants.formatFileSize(bytes);
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
    return `支援的檔案格式: ${AppConstants.FILE_SYSTEM.ALLOWED_EXTENSIONS.join(', ')}`;
  }

  // 取得檔案接受屬性
  getAcceptAttribute(): string {
    return AppConstants.FILE_SYSTEM.ALLOWED_EXTENSIONS.join(',');
  }

  // 取得檔案類型圖示
  getFileTypeIcon(fileType?: string): string {
    const type = fileType || this.detectedFileType;
    return AppConstants.getFileTypeIcon(type);
  }

  // 取得檔案類型名稱
  getFileTypeText(fileType?: string): string {
    const type = fileType || this.detectedFileType;
    return AppConstants.getFileTypeName(type);
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
    
    if (!confirm(`${AppConstants.FILE_SYSTEM.MESSAGES.DELETE_CONFIRM}「${fileName}」？\n\n注意：此操作無法復原。`)) {
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