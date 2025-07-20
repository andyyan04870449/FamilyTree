// 組織圖頁面：用於視覺化顯示組織架構圖
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-organization-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="organization-chart-container">
      <h2>🏢 組織圖</h2>
      <div class="content">
        <p>組織圖功能正在開發中...</p>
        <p>此功能將顯示組織架構圖表</p>
      </div>
    </div>
  `,
  styles: [`
    .organization-chart-container {
      padding: 20px;
    }
    
    .content {
      margin-top: 20px;
    }
  `]
})
export class OrganizationChartComponent {
  constructor() {}
} 