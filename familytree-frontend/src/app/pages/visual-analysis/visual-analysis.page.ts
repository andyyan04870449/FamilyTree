/**
 * 視覺化分析頁面
 * 提供全新的視覺化分析功能，獨立於現有的關係圖譜和組織圖功能
 */

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ProjectService, Project } from '../../services/project.service';
import { VisualAnalysisService, VisualAnalysisGraph, CreateVisualAnalysisRequest } from '../../services/visual-analysis.service';

@Component({
  selector: 'app-visual-analysis',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './visual-analysis.page.html',
  styleUrls: ['./visual-analysis.page.scss']
})
export class VisualAnalysisComponent implements OnInit {
  
  // 真實資料
  visualAnalysisData: VisualAnalysisGraph[] = [];
  
  // 分頁資訊
  totalCount = 0;
  pageNumber = 1;
  pageSize = 10;
  totalPages = 0;
  
  // 載入狀態
  loading = false;
  
  // 提供給模板使用的Math方法
  Math = Math;
  
  // 新增對話框相關屬性
  showAddDialog = false;
  newAnalysisName = '';
  availableProjects: Project[] = [];
  projectSelections: { selectedProjectId: string }[] = [];

  // 搜尋篩選器
  searchFilters = {
    name: '',           // 關聯分析圖名稱
    cases: '',          // 分析案件
    relationCount: '',  // 關聯人數
    updatedBy: '',      // 最後更新人
    updatedAt: ''       // 最後更新日期
  };

  // 篩選後的資料
  filteredVisualAnalysisData: VisualAnalysisGraph[] = [];

  constructor(
    private projectService: ProjectService,
    private visualAnalysisService: VisualAnalysisService
  ) { }

  ngOnInit(): void {
    this.loadProjects();
    this.loadVisualAnalysisData();
  }

  // 載入專案列表
  loadProjects(): void {
    this.projectService.getProjects().subscribe({
      next: (response) => {
        this.availableProjects = response.projects || [];
      },
      error: (error) => {
        console.error('載入專案列表失敗:', error);
      }
    });
  }

  // 載入視覺化分析資料
  loadVisualAnalysisData(): void {
    console.log('📊 載入視覺化分析資料');
    this.loading = true;
    this.visualAnalysisService.getVisualAnalysisGraphs(this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        if (response.success) {
          this.visualAnalysisData = response.graphs;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
          this.applyFilters(); // 套用篩選器
          console.log('✅ 視覺化分析資料載入成功', response.graphs.length, '筆');
        } else {
          console.error('❌ 載入視覺化分析資料失敗:', response.message);
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('❌ 載入視覺化分析資料時發生錯誤:', error);
        this.loading = false;
      }
    });
  }

  // 套用篩選器
  applyFilters(): void {
    this.filteredVisualAnalysisData = this.visualAnalysisData.filter(item => {
      const nameMatch = !this.searchFilters.name || 
        item.name.toLowerCase().includes(this.searchFilters.name.toLowerCase());
      
      const casesMatch = !this.searchFilters.cases || 
        item.cases.some(caseItem => 
          caseItem.toLowerCase().includes(this.searchFilters.cases.toLowerCase())
        );
      
      const relationCountMatch = !this.searchFilters.relationCount || 
        item.relationCount.toString().includes(this.searchFilters.relationCount);
      
      const updatedByMatch = !this.searchFilters.updatedBy || 
        item.updatedBy.toLowerCase().includes(this.searchFilters.updatedBy.toLowerCase());
      
      const updatedAtMatch = !this.searchFilters.updatedAt || 
        this.formatDate(item.updatedAt).includes(this.searchFilters.updatedAt);

      return nameMatch && casesMatch && relationCountMatch && updatedByMatch && updatedAtMatch;
    });
  }

  // 取得篩選後的資料筆數
  getFilteredCount(): number {
    return this.filteredVisualAnalysisData.length;
  }

  // 取得顯示範圍起始編號
  getDisplayStart(): number {
    return this.getFilteredCount() > 0 ? 1 : 0;
  }

  // 取得顯示範圍結束編號
  getDisplayEnd(): number {
    return this.getFilteredCount();
  }

  // 當篩選器輸入變更時觸發
  onFilterChange(): void {
    this.applyFilters();
  }

  // 清除所有篩選器
  clearAllFilters(): void {
    this.searchFilters = {
      name: '',
      cases: '',
      relationCount: '',
      updatedBy: '',
      updatedAt: ''
    };
    this.applyFilters();
  }

  // 開啟新增對話框
  openAddDialog(): void {
    this.showAddDialog = true;
    this.newAnalysisName = '';
    this.projectSelections = [{ selectedProjectId: '' }];
  }

  // 關閉新增對話框
  closeAddDialog(): void {
    this.showAddDialog = false;
    this.newAnalysisName = '';
    this.projectSelections = [];
  }

  // 新增專案行
  addProjectRow(): void {
    this.projectSelections.push({ selectedProjectId: '' });
  }

  // 移除專案行
  removeProjectRow(index: number): void {
    if (this.projectSelections.length > 1) {
      this.projectSelections.splice(index, 1);
    }
  }

  // 專案選擇變更
  onProjectChange(index: number): void {
    // 可以在這裡添加額外的邏輯
  }

  // 獲取指定行可選擇的專案（排除其他行已選擇的）
  getAvailableProjectsForIndex(currentIndex: number): Project[] {
    const selectedIds = this.projectSelections
      .map((selection, index) => index !== currentIndex ? selection.selectedProjectId : '')
      .filter(id => id !== '');
    
    return this.availableProjects.filter(project => 
      !selectedIds.includes(project.id)
    );
  }

  // 獲取已選擇的專案
  getSelectedProjects(): Project[] {
    return this.projectSelections
      .map(selection => this.availableProjects.find(project => project.id === selection.selectedProjectId))
      .filter(project => project !== undefined) as Project[];
  }

  // 格式化日期
  formatDate(dateString: string): string {
    if (!dateString) return '--';
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('zh-TW');
    } catch {
      return '--';
    }
  }

  // 編輯分析圖
  editGraph(id: number): void {
    // 導航到編輯器頁面
    window.location.href = `/visual-analysis/${id}/editor`;
  }

  // 刪除分析圖
  deleteGraph(id: number): void {
    if (confirm('確定要刪除這個視覺化分析圖嗎？')) {
      this.visualAnalysisService.deleteVisualAnalysisGraph(id).subscribe({
        next: (response) => {
          if (response.success) {
            alert('刪除成功');
            this.loadVisualAnalysisData(); // 重新載入資料
          } else {
            alert('刪除失敗: ' + response.message);
          }
        },
        error: (error) => {
          console.error('刪除視覺化分析圖時發生錯誤:', error);
          alert('刪除時發生錯誤，請稍後再試');
        }
      });
    }
  }

  // 檢查是否有啟用的篩選條件
  hasActiveFilters(): boolean {
    return !!(this.searchFilters.name || 
             this.searchFilters.cases || 
             this.searchFilters.relationCount || 
             this.searchFilters.updatedBy || 
             this.searchFilters.updatedAt);
  }

  // 重新命名分析圖
  renameGraph(item: VisualAnalysisGraph): void {
    const newName = prompt('請輸入新的名稱:', item.name);
    if (newName && newName.trim() && newName.trim() !== item.name) {
      console.log('🏷️ 重新命名分析圖:', item.id, '->', newName.trim());
      // TODO: 實作重新命名API
      alert('重新命名功能尚未實作');
    }
  }

  // 確認新增
  confirmAdd(): void {
    if (!this.newAnalysisName.trim()) {
      alert('請輸入分析圖名稱');
      return;
    }

    const selectedProjects = this.getSelectedProjects();
    if (selectedProjects.length === 0) {
      alert('請至少選擇一個專案');
      return;
    }

    console.log('➕ 建立新分析圖:', this.newAnalysisName);

    // 呼叫後端API創建分析圖
    const request: CreateVisualAnalysisRequest = {
      name: this.newAnalysisName,
      projectIds: selectedProjects.map(p => p.id),
      updatedBy: 'user'
    };

    this.loading = true;
    this.visualAnalysisService.createVisualAnalysisGraph(request).subscribe({
      next: (response) => {
        if (response.success) {
          console.log('✅ 分析圖建立成功:', response);
          this.loadVisualAnalysisData(); // 重新載入資料
          this.closeAddDialog();
        } else {
          console.error('❌ 建立分析圖失敗:', response.message);
          alert('建立失敗: ' + response.message);
        }
        this.loading = false;
      },
      error: (error) => {
        console.error('❌ 建立分析圖時發生錯誤:', error);
        alert('建立時發生錯誤，請稍後再試');
        this.loading = false;
      }
    });
  }

} 