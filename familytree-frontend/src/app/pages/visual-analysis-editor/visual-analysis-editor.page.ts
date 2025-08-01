/**
 * 視覺化分析編輯器頁面
 * 提供視覺化分析圖的編輯功能，直接引用關係圖譜組件
 */

import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { 
  VisualAnalysisService, 
  VisualAnalysisEditorData, 
  VisualAnalysisNode, 
  VisualAnalysisRelationship,
  ProjectNodeGroup,
  UpdateNodeVisibilityRequest,
  CreateVisualAnalysisRelationshipRequest
} from '../../services/visual-analysis.service';
import { RelationshipGraphComponent } from '../../components/relationship-graph/relationship-graph.component';
import { GraphData, GraphNode, GraphLink } from '../../services/relationship-graph.service';
import { PersonDetailDialogComponent } from '../../components/person-detail-dialog/person-detail-dialog.component';
import { LogService } from '../../services/log.service';
import { ProjectTreeComponent, ProjectGroup } from '../../shared/components/project-tree/project-tree.component';
import { PersonItem } from '../../shared/components/person-item/person-item.component';

@Component({
  selector: 'app-visual-analysis-editor',
  standalone: true,
  imports: [CommonModule, FormsModule, RelationshipGraphComponent, PersonDetailDialogComponent, ProjectTreeComponent],
  templateUrl: './visual-analysis-editor.page.html',
  styleUrls: ['./visual-analysis-editor.page.scss']
})
export class VisualAnalysisEditorComponent implements OnInit, OnDestroy {
  // 基本資料
  graphId!: number;
  editorData?: VisualAnalysisEditorData;
  loading = true;

  // 關係圖譜資料
  graphData?: GraphData;

  // 搜尋面板
  showSearchPanel = false;
  searchKeyword = '';

  // 訂閱
  private subscriptions: Subscription[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private visualAnalysisService: VisualAnalysisService,
    private logService: LogService
  ) {
    this.logService.info('VisualAnalysisEditorComponent', '編輯器組件初始化');
  }

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.graphId = +params['id'];
      this.logService.info('VisualAnalysisEditorComponent', '載入編輯器', { graphId: this.graphId });
      this.loadEditorData();
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  // 載入編輯器資料
  loadEditorData(): void {
    this.loading = true;
    const subscription = this.visualAnalysisService.getEditorData(this.graphId).subscribe({
      next: (response) => {
        this.logService.info('VisualAnalysisEditorComponent', '編輯器資料載入回應', {
          success: response.success,
          nodesCount: response.nodes?.length || 0,
          relationshipsCount: response.relationships?.length || 0
        });

        if (response.success) {
          this.editorData = response;
          this.convertToGraphData();
        } else {
          this.logService.error('VisualAnalysisEditorComponent', '載入編輯器資料失敗', { message: response.message });
        }
        this.loading = false;
      },
      error: (error) => {
        this.logService.error('VisualAnalysisEditorComponent', '載入編輯器資料時發生錯誤', error);
        this.loading = false;
      }
    });
    this.subscriptions.push(subscription);
  }

  /**
   * 將視覺化分析資料轉換為關係圖譜組件需要的格式
   */
  convertToGraphData(): void {
    if (!this.editorData) {
      this.logService.warn('VisualAnalysisEditorComponent', '沒有編輯器資料可轉換');
      return;
    }

    this.logService.info('VisualAnalysisEditorComponent', '開始轉換資料格式', {
      totalNodes: this.editorData.nodes.length,
      visibleNodes: this.editorData.nodes.filter(n => n.isVisible).length,
      totalRelationships: this.editorData.relationships.length
    });

    // 轉換節點資料 - 只包含可見節點
    const visibleNodes = this.editorData.nodes.filter(node => node.isVisible);
    const nodes: GraphNode[] = visibleNodes.map(node => ({
      id: node.personId.toString(),
      name: node.personName,
      gender: node.personGender === '男' ? 'male' : 'female', // 從後端獲取真實性別資訊
      isExpanded: true,
      x: node.nodeX || undefined,
      y: node.nodeY || undefined,
      data: {
        id: node.personId,
        name: node.personName,
        gender: node.personGender,
        photo: node.personPhoto || null, // 添加照片資訊
        projectId: node.projectId,
        projectName: node.projectName,
        visualAnalysisNodeId: node.id
      }
    }));

    // 轉換關係資料 - 只包含兩端節點都可見的關係
    const visibleNodeIds = new Set(visibleNodes.map(n => n.personId));
    const links: GraphLink[] = this.editorData.relationships
      .filter(rel => visibleNodeIds.has(rel.sourcePersonId) && visibleNodeIds.has(rel.targetPersonId))
      .map(rel => ({
        source: rel.sourcePersonId.toString(),
        target: rel.targetPersonId.toString(),
        type: rel.relationType,
        isFamily: this.isRelationshipFamily(rel.relationType),
        strength: 1.0
      }));

    // 建立圖譜資料
    this.graphData = {
      nodes,
      links,
      metadata: {
        totalNodes: nodes.length,
        totalLinks: links.length,
        familyLinks: links.filter(l => l.isFamily).length,
        friendLinks: links.filter(l => !l.isFamily).length,
        analysisDate: new Date().toISOString()
      }
    };

    this.logService.info('VisualAnalysisEditorComponent', '資料轉換完成', {
      convertedNodes: this.graphData.nodes.length,
      convertedLinks: this.graphData.links.length,
      familyLinks: this.graphData.metadata?.familyLinks,
      friendLinks: this.graphData.metadata?.friendLinks
    });
  }

  /**
   * 判斷關係類型是否為家族關係
   */
  private isRelationshipFamily(relationType: string): boolean {
    const familyKeywords = ['父', '母', '子', '女', '夫', '妻', '兄', '弟', '姐', '妹', '祖', '孫'];
    return familyKeywords.some(keyword => relationType.includes(keyword));
  }

  // 搜尋面板切換
  toggleSearchPanel(): void {
    this.showSearchPanel = !this.showSearchPanel;
    this.logService.info('VisualAnalysisEditorComponent', '切換搜尋面板', { show: this.showSearchPanel });
  }

  // 獲取篩選後的專案
  getFilteredProjects(): ProjectNodeGroup[] {
    if (!this.editorData?.projectGroups) return [];
    
    if (!this.searchKeyword.trim()) {
      return this.editorData.projectGroups;
    }

    return this.editorData.projectGroups
      .map(project => ({
        ...project,
        persons: project.persons.filter(person => 
          person.name.toLowerCase().includes(this.searchKeyword.toLowerCase())
        )
      }))
      .filter(project => project.persons.length > 0);
  }

  // 轉換為 ProjectTree 組件所需的格式
  getProjectTreeData(): ProjectGroup[] {
    const projects = this.getFilteredProjects();
    
    return projects.map(project => ({
      projectId: parseInt(project.projectId),
      projectName: project.projectName,
      projectColor: this.getProjectColor(project.projectId),
      isExpanded: true,
      isSelected: project.persons.every(p => p.isVisible),
      persons: project.persons.map(person => ({
        personId: person.personId,
        name: person.name,
        isVisible: person.isVisible,
        projectId: parseInt(project.projectId),
        avatarColor: this.getProjectColor(project.projectId)
      } as PersonItem))
    }));
  }

  // 處理專案樹的可見性變更
  onProjectTreeVisibilityChange(event: { projectId: number; personId: number; isVisible: boolean }): void {
    this.onNodeVisibilityChange(event.projectId.toString(), event.personId, event.isVisible);
  }

  // 切換所有節點
  toggleAllNodes(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    const isChecked = checkbox.checked;

    if (isChecked) {
      this.showAllNodes();
    } else {
      this.hideAllNodes();
    }
  }

  // 節點可見性變更
  onNodeVisibilityChange(projectId: string, personId: number, isVisible: boolean): void {
    this.logService.info('VisualAnalysisEditorComponent', '節點可見性變更', {
      projectId,
      personId,
      isVisible
    });

    const request: UpdateNodeVisibilityRequest = {
      updates: [{ projectId, personId, isVisible }]
    };

    const subscription = this.visualAnalysisService.updateNodeVisibility(this.graphId, request).subscribe({
      next: (response) => {
        if (response.success) {
          this.logService.info('VisualAnalysisEditorComponent', '節點可見性更新成功');
          this.loadEditorData(); // 重新載入資料並轉換
        } else {
          this.logService.error('VisualAnalysisEditorComponent', '節點可見性更新失敗', { message: response.message });
        }
      },
      error: (error) => {
        this.logService.error('VisualAnalysisEditorComponent', '更新節點可見性失敗', error);
      }
    });
    this.subscriptions.push(subscription);
  }

  // 獲取專案顏色
  getProjectColor(projectId: string): string {
    const colors = ['#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4', '#FFEAA7', '#DDA0DD', '#98D8C8', '#F7DC6F'];
    const hash = projectId.split('').reduce((a, b) => {
      a = ((a << 5) - a) + b.charCodeAt(0);
      return a & a;
    }, 0);
    return colors[Math.abs(hash) % colors.length];
  }

  // 顯示全部節點
  showAllNodes(): void {
    if (!this.editorData) return;

    this.logService.info('VisualAnalysisEditorComponent', '顯示全部節點');

    const updates = this.editorData.projectGroups.flatMap(project =>
      project.persons.map(person => ({
        projectId: project.projectId,
        personId: person.personId,
        isVisible: true
      }))
    );

    const request: UpdateNodeVisibilityRequest = { updates };

    const subscription = this.visualAnalysisService.updateNodeVisibility(this.graphId, request).subscribe({
      next: (response) => {
        if (response.success) {
          this.logService.info('VisualAnalysisEditorComponent', '全部節點顯示成功');
          this.loadEditorData();
        } else {
          this.logService.error('VisualAnalysisEditorComponent', '顯示全部節點失敗', { message: response.message });
        }
      },
      error: (error) => {
        this.logService.error('VisualAnalysisEditorComponent', '顯示全部節點失敗', error);
      }
    });
    this.subscriptions.push(subscription);
  }

  // 隱藏全部節點
  hideAllNodes(): void {
    if (!this.editorData) return;

    this.logService.info('VisualAnalysisEditorComponent', '隱藏全部節點');

    const updates = this.editorData.projectGroups.flatMap(project =>
      project.persons.map(person => ({
        projectId: project.projectId,
        personId: person.personId,
        isVisible: false
      }))
    );

    const request: UpdateNodeVisibilityRequest = { updates };

    const subscription = this.visualAnalysisService.updateNodeVisibility(this.graphId, request).subscribe({
      next: (response) => {
        if (response.success) {
          this.logService.info('VisualAnalysisEditorComponent', '全部節點隱藏成功');
          this.loadEditorData();
        } else {
          this.logService.error('VisualAnalysisEditorComponent', '隱藏全部節點失敗', { message: response.message });
        }
      },
      error: (error) => {
        this.logService.error('VisualAnalysisEditorComponent', '隱藏全部節點失敗', error);
      }
    });
    this.subscriptions.push(subscription);
  }

  // 匯出圖表
  exportGraph(): void {
    this.logService.info('VisualAnalysisEditorComponent', '匯出圖表功能');
    // TODO: 實現匯出功能
    alert('匯出功能開發中...');
  }

  // 復原/重做
  undo(): void {
    this.logService.info('VisualAnalysisEditorComponent', '復原功能');
    // TODO: 實現復原功能
    alert('復原功能開發中...');
  }

  redo(): void {
    this.logService.info('VisualAnalysisEditorComponent', '重做功能');
    // TODO: 實現重做功能
    alert('重做功能開發中...');
  }

  // 底部按鈕
  goBack(): void {
    this.logService.info('VisualAnalysisEditorComponent', '返回視覺化分析列表');
    this.router.navigate(['/visual-analysis']);
  }

  saveGraph(): void {
    this.logService.info('VisualAnalysisEditorComponent', '儲存圖表');
    // TODO: 實現保存功能
    alert('保存功能開發中...');
  }

  exitEditor(): void {
    this.logService.info('VisualAnalysisEditorComponent', '退出編輯器');
    this.router.navigate(['/visual-analysis']);
  }

  /**
   * 處理關係建立成功事件
   */
  onRelationshipCreated(): void {
    this.logService.info('VisualAnalysisEditorComponent', '關係建立成功', {
      currentGraphDataLinksCount: this.graphData?.links.length || 0
    });
    
    // 關係建立成功後，完全依賴RelationshipGraphComponent的本地圖譜更新
    // 不重新載入編輯器資料，避免覆蓋新建立的關聯線
    // 新關聯線已經通過後端API保存，下次重新載入頁面時會正確顯示
    
    this.logService.info('VisualAnalysisEditorComponent', '關係建立後的圖譜狀態', {
      finalGraphDataLinksCount: this.graphData?.links.length || 0,
      graphDataLinks: this.graphData?.links.map(l => ({
        source: l.source,
        target: l.target,
        type: l.type
      })) || []
    });
  }
}
