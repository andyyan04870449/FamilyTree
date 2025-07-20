// 人員管理頁面：用於管理人員資料，包含TAB功能
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-person-management',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="person-management-container">
      <h2>👥 人員管理</h2>
      <div class="content">
        <div class="tabs">
          <div class="tab-header">
            <button 
              class="tab-button" 
              [class.active]="activeTab === 'list'"
              (click)="setActiveTab('list')">
              人員列表
            </button>
            <button 
              class="tab-button" 
              [class.active]="activeTab === 'add'"
              (click)="setActiveTab('add')">
              新增人員
            </button>
            <button 
              class="tab-button" 
              [class.active]="activeTab === 'edit'"
              (click)="setActiveTab('edit')">
              編輯人員
            </button>
          </div>
          
          <div class="tab-content">
            <div *ngIf="activeTab === 'list'" class="tab-panel">
              <h3>人員列表</h3>
              <p>人員列表功能正在開發中...</p>
            </div>
            
            <div *ngIf="activeTab === 'add'" class="tab-panel">
              <h3>新增人員</h3>
              <p>新增人員功能正在開發中...</p>
            </div>
            
            <div *ngIf="activeTab === 'edit'" class="tab-panel">
              <h3>編輯人員</h3>
              <p>編輯人員功能正在開發中...</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .person-management-container {
      padding: 20px;
    }
    
    .content {
      margin-top: 20px;
    }
    
    .tabs {
      border: 1px solid #ddd;
      border-radius: 4px;
    }
    
    .tab-header {
      display: flex;
      background-color: #f5f5f5;
      border-bottom: 1px solid #ddd;
    }
    
    .tab-button {
      padding: 10px 20px;
      border: none;
      background: none;
      cursor: pointer;
      border-right: 1px solid #ddd;
    }
    
    .tab-button:last-child {
      border-right: none;
    }
    
    .tab-button.active {
      background-color: white;
      border-bottom: 2px solid #007bff;
    }
    
    .tab-content {
      padding: 20px;
    }
    
    .tab-panel h3 {
      margin-top: 0;
    }
  `]
})
export class PersonManagementComponent {
  activeTab: string = 'list';

  constructor() {}

  setActiveTab(tab: string) {
    this.activeTab = tab;
  }
} 