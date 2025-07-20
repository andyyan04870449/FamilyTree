// 檔案管理頁面 - 用於查看、刪除和管理已上傳的檔案
// 主要功能：檔案列表顯示、檔案刪除、檔案詳情查看、檔案狀態管理

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

interface FileItem {
  id: number;
  fileName: string;
  originalName: string;
  fileSize: number;
  fileType: string;
  uploadDate: string;
  status: 'processing' | 'completed' | 'failed';
  recordCount?: number;
  processedCount?: number;
  errorCount?: number;
}

@Component({
  selector: 'app-file-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './file-management.page.html',
  styleUrls: ['./file-management.page.scss']
})
export class FileManagementComponent implements OnInit {
  files: FileItem[] = [];
  filteredFiles: FileItem[] = [];
  loading = false;
  error = '';
  searchKeyword = '';
  selectedStatus = 'all';
  sortBy = 'uploadDate';
  sortOrder: 'asc' | 'desc' = 'desc';
  selectedFiles: number[] = [];
  selectAll = false;

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.loadFiles();
  }

  // 載入檔案列表
  async loadFiles(): Promise<void> {
    this.loading = true;
    this.error = '';

    try {
      // 模擬API調用
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      this.files = [
        {
          id: 1,
          fileName: '分公司客戶基資表-廠商測試版.xlsx',
          originalName: '分公司客戶基資表-廠商測試版.xlsx',
          fileSize: 245760,
          fileType: 'xlsx',
          uploadDate: '2025-07-21T10:30:00',
          status: 'completed',
          recordCount: 150,
          processedCount: 150,
          errorCount: 0
        },
        {
          id: 2,
          fileName: '人員資料表.csv',
          originalName: '人員資料表.csv',
          fileSize: 128000,
          fileType: 'csv',
          uploadDate: '2025-07-20T15:45:00',
          status: 'completed',
          recordCount: 99,
          processedCount: 99,
          errorCount: 0
        },
        {
          id: 3,
          fileName: '測試檔案.xlsx',
          originalName: '測試檔案.xlsx',
          fileSize: 51200,
          fileType: 'xlsx',
          uploadDate: '2025-07-19T09:15:00',
          status: 'processing',
          recordCount: 50,
          processedCount: 30,
          errorCount: 2
        },
        {
          id: 4,
          fileName: '錯誤檔案.csv',
          originalName: '錯誤檔案.csv',
          fileSize: 102400,
          fileType: 'csv',
          uploadDate: '2025-07-18T14:20:00',
          status: 'failed',
          recordCount: 0,
          processedCount: 0,
          errorCount: 5
        }
      ];

      this.applyFilters();
    } catch (error) {
      this.error = '載入檔案列表失敗';
      console.error('載入檔案列表失敗:', error);
    } finally {
      this.loading = false;
    }
  }

  // 應用篩選和排序
  applyFilters(): void {
    let filtered = [...this.files];

    // 關鍵字搜尋
    if (this.searchKeyword.trim()) {
      const keyword = this.searchKeyword.toLowerCase();
      filtered = filtered.filter(file => 
        file.fileName.toLowerCase().includes(keyword) ||
        file.originalName.toLowerCase().includes(keyword)
      );
    }

    // 狀態篩選
    if (this.selectedStatus !== 'all') {
      filtered = filtered.filter(file => file.status === this.selectedStatus);
    }

    // 排序
    filtered.sort((a, b) => {
      let aValue: any, bValue: any;

      switch (this.sortBy) {
        case 'fileName':
          aValue = a.fileName.toLowerCase();
          bValue = b.fileName.toLowerCase();
          break;
        case 'fileSize':
          aValue = a.fileSize;
          bValue = b.fileSize;
          break;
        case 'uploadDate':
          aValue = new Date(a.uploadDate).getTime();
          bValue = new Date(b.uploadDate).getTime();
          break;
        case 'status':
          aValue = a.status;
          bValue = b.status;
          break;
        default:
          aValue = new Date(a.uploadDate).getTime();
          bValue = new Date(b.uploadDate).getTime();
      }

      if (this.sortOrder === 'asc') {
        return aValue > bValue ? 1 : -1;
      } else {
        return aValue < bValue ? 1 : -1;
      }
    });

    this.filteredFiles = filtered;
    this.updateSelectAll();
  }

  // 更新全選狀態
  updateSelectAll(): void {
    this.selectAll = this.filteredFiles.length > 0 && 
                    this.filteredFiles.every(file => this.selectedFiles.includes(file.id));
  }

  // 切換全選
  toggleSelectAll(): void {
    if (this.selectAll) {
      this.selectedFiles = this.filteredFiles.map(file => file.id);
    } else {
      this.selectedFiles = [];
    }
  }

  // 切換單個檔案選中狀態
  toggleFileSelection(fileId: number): void {
    const index = this.selectedFiles.indexOf(fileId);
    if (index > -1) {
      this.selectedFiles.splice(index, 1);
    } else {
      this.selectedFiles.push(fileId);
    }
    this.updateSelectAll();
  }

  // 刪除選中的檔案
  async deleteSelectedFiles(): Promise<void> {
    if (this.selectedFiles.length === 0) {
      this.error = '請選擇要刪除的檔案';
      return;
    }

    if (!confirm(`確定要刪除選中的 ${this.selectedFiles.length} 個檔案嗎？`)) {
      return;
    }

    this.loading = true;
    this.error = '';

    try {
      // 模擬API調用
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      this.files = this.files.filter(file => !this.selectedFiles.includes(file.id));
      this.selectedFiles = [];
      this.applyFilters();
    } catch (error) {
      this.error = '刪除檔案失敗';
      console.error('刪除檔案失敗:', error);
    } finally {
      this.loading = false;
    }
  }

  // 刪除單個檔案
  async deleteFile(fileId: number): Promise<void> {
    if (!confirm('確定要刪除這個檔案嗎？')) {
      return;
    }

    this.loading = true;
    this.error = '';

    try {
      // 模擬API調用
      await new Promise(resolve => setTimeout(resolve, 500));
      
      this.files = this.files.filter(file => file.id !== fileId);
      this.selectedFiles = this.selectedFiles.filter(id => id !== fileId);
      this.applyFilters();
    } catch (error) {
      this.error = '刪除檔案失敗';
      console.error('刪除檔案失敗:', error);
    } finally {
      this.loading = false;
    }
  }

  // 重新處理檔案
  async reprocessFile(fileId: number): Promise<void> {
    this.loading = true;
    this.error = '';

    try {
      // 模擬API調用
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      const file = this.files.find(f => f.id === fileId);
      if (file) {
        file.status = 'processing';
        file.processedCount = 0;
        file.errorCount = 0;
      }
    } catch (error) {
      this.error = '重新處理檔案失敗';
      console.error('重新處理檔案失敗:', error);
    } finally {
      this.loading = false;
    }
  }

  // 格式化檔案大小
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  // 格式化日期
  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString('zh-TW');
  }

  // 獲取狀態顯示文字
  getStatusText(status: string): string {
    switch (status) {
      case 'processing': return '處理中';
      case 'completed': return '已完成';
      case 'failed': return '失敗';
      default: return '未知';
    }
  }

  // 獲取狀態顏色
  getStatusColor(status: string): string {
    switch (status) {
      case 'processing': return 'warning';
      case 'completed': return 'success';
      case 'failed': return 'error';
      default: return 'secondary';
    }
  }

  // 獲取檔案類型圖示
  getFileTypeIcon(fileType: string): string {
    switch (fileType.toLowerCase()) {
      case 'xlsx':
      case 'xls':
        return '📊';
      case 'csv':
        return '📋';
      case 'pdf':
        return '📄';
      case 'doc':
      case 'docx':
        return '📝';
      default:
        return '📁';
    }
  }

  // 清除搜尋
  clearSearch(): void {
    this.searchKeyword = '';
    this.applyFilters();
  }

  // 切換排序
  toggleSort(field: string): void {
    if (this.sortBy === field) {
      this.sortOrder = this.sortOrder === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = field;
      this.sortOrder = 'desc';
    }
    this.applyFilters();
  }

  // 導航到檔案上傳頁面
  goToUpload(): void {
    this.router.navigate(['/file-upload']);
  }
} 