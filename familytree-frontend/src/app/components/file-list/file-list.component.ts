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
        next: (response: any) => {
          this.loading = false;
          if (response.success) {
            // 後端現在回應格式是 { success: true, data: [...], message: "..." }
            // 如果是新格式，使用 data；如果是舊格式，使用 files
            this.files = response.data || response.files || [];
            this.error = '';
          } else {
            this.error = response.message || '載入檔案列表失敗';
            this.files = [];
          }
        },
        error: (err) => {
          this.loading = false;
          this.error = '載入檔案列表失敗';
          this.files = [];
          console.error('載入檔案列表錯誤:', err);
        }
      })
    );
  }

  deleteFile(file: FileUploadModel): void {
    // 先查詢刪除影響
    this.subscription.add(
      this.fileUploadService.getDeleteImpact(file.id).subscribe({
        next: (impactResponse) => {
          if (!impactResponse.success) {
            alert(`無法取得刪除影響資訊: ${impactResponse.message}`);
            return;
          }

          // 建構確認訊息
          let confirmMessage = `確定要刪除檔案 "${impactResponse.fileName}" 嗎？\n\n`;
          
          if (impactResponse.personCount > 0) {
            confirmMessage += `⚠️ 警告：刪除此檔案將同時刪除 ${impactResponse.personCount} 筆人員資料！\n\n`;
            confirmMessage += '將被刪除的人員包括：\n';
            
            impactResponse.personNames.forEach((name, index) => {
              confirmMessage += `${index + 1}. ${name}\n`;
            });
            
            if (impactResponse.hasMorePersons) {
              confirmMessage += `...還有 ${impactResponse.personCount - impactResponse.personNames.length} 筆\n`;
            }
            
            confirmMessage += '\n此操作無法復原，請確認是否繼續？';
          } else {
            confirmMessage += '此檔案沒有相關的人員資料，可以安全刪除。';
          }

          // 顯示確認對話框
          if (confirm(confirmMessage)) {
            this.performDelete(file);
          }
        },
        error: (err) => {
          console.error('查詢刪除影響錯誤:', err);
          // 如果查詢失敗，仍然允許刪除但給予警告
          if (confirm(`無法確認刪除影響（${err.message || '網路錯誤'}），確定要刪除檔案 "${file.originalFilename}" 嗎？`)) {
            this.performDelete(file);
          }
        }
      })
    );
  }

  private performDelete(file: FileUploadModel): void {
    this.subscription.add(
      this.fileUploadService.deleteFile(file.id).subscribe({
        next: (response) => {
          if (response.success) {
            console.log('檔案刪除成功:', response.message);
            // 可以選擇顯示成功訊息
            if (response.message.includes('人員資料')) {
              alert(`刪除成功：${response.message}`);
            }
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