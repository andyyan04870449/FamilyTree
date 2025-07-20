// 關聯圖譜頁面：使用通用關聯圖譜組件呈現所有人員的關聯關係
// 主要功能：自動分析所有人員關係並呈現圖譜

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RelationshipGraphComponent } from '../../components/relationship-graph/relationship-graph.component';

@Component({
  selector: 'app-relationship-graph-page',
  standalone: true,
  imports: [CommonModule, RelationshipGraphComponent],
  template: `
    <div class="relationship-graph-page">
      <app-relationship-graph
        [autoAnalyze]="true"
        [selectedPersonIds]="[]">
      </app-relationship-graph>
    </div>
  `,
  styles: [`
    .relationship-graph-page {
      width: 100%;
      height: 100vh;
      background: #1a1a1a;
    }
  `]
})
export class RelationshipGraphPage implements OnInit {
  
  constructor() {
    console.log('🔗 RelationshipGraphPage 初始化');
  }

  ngOnInit(): void {
    console.log('📊 關聯圖譜頁面載入完成');
  }
} 