// 關聯圖頁面：用於視覺化顯示人員之間的關聯關係
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-relationship-graph',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relationship-graph-container">
      <h2>🔗 關聯圖</h2>
      <div class="content">
        <p>關聯圖功能正在開發中...</p>
        <p>此功能將顯示人員之間的關聯關係圖表</p>
      </div>
    </div>
  `,
  styles: [`
    .relationship-graph-container {
      padding: 20px;
    }
    
    .content {
      margin-top: 20px;
    }
  `]
})
export class RelationshipGraphComponent {
  constructor() {}
} 