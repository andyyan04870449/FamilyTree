// 關聯圖譜頁面：使用通用關聯圖譜組件呈現所有人員的關聯關係
// 主要功能：自動分析所有人員關係並呈現圖譜，支援從其他頁面傳入選中人員ID

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { RelationshipGraphComponent } from '../../components/relationship-graph/relationship-graph.component';

@Component({
  selector: 'app-relationship-graph-page',
  standalone: true,
  imports: [CommonModule, RelationshipGraphComponent],
  template: `
    <div class="relationship-graph-page">
      <app-relationship-graph
        [autoAnalyze]="true"
        [selectedPersonIds]="selectedPersonIds">
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
  selectedPersonIds: number[] = [];
  
  constructor(
    private route: ActivatedRoute,
    private router: Router
  ) {
    console.log('🔗 RelationshipGraphPage 初始化');
  }

  ngOnInit(): void {
    console.log('📊 關聯圖譜頁面載入完成');
    
    // 從路由參數獲取選中的人員ID
    this.route.params.subscribe(params => {
      if (params['personIds']) {
        try {
          // 解析URL參數中的personIds
          this.selectedPersonIds = params['personIds']
            .split(',')
            .map((id: string) => parseInt(id.trim()))
            .filter((id: number) => !isNaN(id));
          
          console.log('📋 從路由獲取選中人員ID:', this.selectedPersonIds);
        } catch (error) {
          console.error('解析人員ID參數失敗:', error);
          this.selectedPersonIds = [];
        }
      } else {
        this.selectedPersonIds = [];
      }
    });
  }
} 