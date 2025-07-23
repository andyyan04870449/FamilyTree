import { Component, OnInit, ElementRef, ViewChild, AfterViewInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PersonService, Person } from '../../services/person.service';
import { EventService } from '../../services/event.service';
import { ProjectService } from '../../services/project.service';
import { Subscription } from 'rxjs';
import * as d3 from 'd3';
import { AppConstants } from '../../constants/app.constants';

interface PersonNode {
  id: string;
  name: string;
  gender: 'male' | 'female';
  photo?: string;
  isExpanded?: boolean;
  x?: number;
  y?: number;
  fx?: number | null;
  fy?: number | null;
}

interface Relationship {
  source: string;
  target: string;
  type: string;
  isFamily: boolean;
}

interface SearchCriteria {
  idNumber: string;
  passportNumber: string;
  birthday: string;
  name: string;
  mobile: string;
  gender: string;
}

@Component({
  selector: 'app-family-tree',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="family-tree-page">
      <div class="page-header">
        <h1>🌳 {{ getCurrentProjectName() }}</h1>
        <p>成員關係網路圖</p>
      </div>
      
      <!-- 查詢條件區域 -->
      <div class="search-section" *ngIf="!showGraph">
        <h2>查詢條件</h2>
        <div class="search-form">
          <div class="form-row">
            <div class="form-column">
              <div class="form-group">
                <label for="idNumber">身分證號</label>
                <input 
                  type="text" 
                  id="idNumber"
                  [(ngModel)]="searchCriteria.idNumber" 
                  placeholder="A123456789"
                  class="form-control">
              </div>
              <div class="form-group">
                <label for="passportNumber">護照號碼</label>
                <input 
                  type="text" 
                  id="passportNumber"
                  [(ngModel)]="searchCriteria.passportNumber" 
                  placeholder="G123456789"
                  class="form-control">
              </div>
              <div class="form-group">
                <label for="birthday">生日</label>
                <input 
                  type="date" 
                  id="birthday"
                  [(ngModel)]="searchCriteria.birthday" 
                  class="form-control">
              </div>
            </div>
            <div class="form-column">
              <div class="form-group">
                <label for="name">姓名</label>
                <input 
                  type="text" 
                  id="name"
                  [(ngModel)]="searchCriteria.name" 
                  placeholder="李立朝"
                  class="form-control">
              </div>
              <div class="form-group">
                <label for="mobile">行動電話</label>
                <input 
                  type="text" 
                  id="mobile"
                  [(ngModel)]="searchCriteria.mobile" 
                  placeholder="0912345678"
                  class="form-control">
              </div>
              <div class="form-group">
                <label>性別</label>
                <div class="radio-group">
                  <label class="radio-item">
                    <input type="radio" [(ngModel)]="searchCriteria.gender" value="男">
                    <span>男</span>
                  </label>
                  <label class="radio-item">
                    <input type="radio" [(ngModel)]="searchCriteria.gender" value="女">
                    <span>女</span>
                  </label>
                </div>
              </div>
            </div>
          </div>
          
          <div class="form-note">
            備註: 所有填寫的條件都會同時匹配（AND 查詢）。
          </div>
          
          <div class="form-actions">
            <button class="btn btn-secondary" (click)="resetSearch()">
              重置
            </button>
            <button class="btn btn-primary" (click)="performSearch()">
              查詢
            </button>
          </div>
        </div>
      </div>

      <!-- 查詢結果區域 -->
      <div class="results-section" *ngIf="!showGraph && searchResults.length > 0">
        <h2>查詢結果</h2>
        <div class="results-info">
          <span>共找到 {{ searchResults.length }} 筆資料</span>
        </div>
        
        <div class="results-table">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>姓名</th>
                <th>性別</th>
                <th>生日</th>
                <th>電話</th>
                <th>國籍</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let person of searchResults">
                <td>{{ person.id }}</td>
                <td>{{ person.name }}</td>
                <td>{{ person.gender }}</td>
                <td>{{ person.birthday }}</td>
                <td>{{ person.mobile }}</td>
                <td>{{ person.nationality }}</td>
                <td>
                  <button class="btn btn-sm btn-info" (click)="viewPersonDetails(person)">
                    查看詳情
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
      
      <!-- 圖譜顯示區域 -->
      <div class="tree-container" *ngIf="showGraph">
        
        <div class="tree-viewport" #treeViewport>
          <div class="graph-container" #graphContainer></div>
          
          <!-- 圖例 -->
          <div class="legend">
            <div class="legend-item">
              <div class="legend-color male"></div>
              <span>男生</span>
            </div>
            <div class="legend-item">
              <div class="legend-color female"></div>
              <span>女生</span>
            </div>
            <div class="legend-item">
              <div class="legend-line family"></div>
              <span>家族關係</span>
            </div>
            <div class="legend-item">
              <div class="legend-line other"></div>
              <span>其他關係</span>
            </div>
          </div>
          
          <!-- 控制面板 -->
          <div class="graph-controls">
            <button class="control-btn" (click)="zoomIn()" title="放大">+</button>
            <button class="control-btn" (click)="zoomOut()" title="縮小">-</button>
            <button class="control-btn" (click)="resetView()" title="重設">⟳</button>
            <button class="control-btn" (click)="toggleDrag()" title="拖拽模式">✋</button>
          </div>
        </div>
      </div>

      <!-- 詳情彈出表單 -->
      <div class="modal-overlay" *ngIf="showDetailModal" (click)="closeDetailModal()">
        <div class="modal-content" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3>人員詳細資料</h3>
            <button class="modal-close" (click)="closeDetailModal()">×</button>
          </div>
          
          <div class="modal-body" *ngIf="selectedPerson">
            <div class="detail-grid">
              <div class="detail-section">
                <h4>基本資料</h4>
                <div class="detail-row">
                  <label>ID：</label>
                  <span>{{ selectedPerson.id }}</span>
                </div>
                <div class="detail-row">
                  <label>姓名：</label>
                  <span>{{ selectedPerson.name }}</span>
                </div>
                <div class="detail-row">
                  <label>性別：</label>
                  <span>{{ selectedPerson.gender }}</span>
                </div>
                <div class="detail-row">
                  <label>生日：</label>
                  <span>{{ selectedPerson.birthday }}</span>
                </div>
                <div class="detail-row">
                  <label>國籍：</label>
                  <span>{{ selectedPerson.nationality }}</span>
                </div>
              </div>
              
              <div class="detail-section">
                <h4>聯絡資訊</h4>
                <div class="detail-row">
                  <label>行動電話：</label>
                  <span>{{ selectedPerson.mobile || '未填寫' }}</span>
                </div>
                <div class="detail-row">
                  <label>市話：</label>
                  <span>{{ selectedPerson.phone || '未填寫' }}</span>
                </div>
                <div class="detail-row">
                  <label>身分證號：</label>
                  <span>{{ selectedPerson.idNumber || '未填寫' }}</span>
                </div>
                <div class="detail-row">
                  <label>護照號碼：</label>
                  <span>{{ selectedPerson.passportNumber || '未填寫' }}</span>
                </div>
              </div>
              
              <div class="detail-section full-width">
                <h4>家族關係</h4>
                <div class="detail-text" *ngIf="selectedPerson.familyRelationships; else noFamilyRelations">
                  <pre>{{ selectedPerson.familyRelationships }}</pre>
                </div>
                <ng-template #noFamilyRelations>
                  <div class="detail-text">無家族關係資料</div>
                </ng-template>
              </div>
              
              <div class="detail-section full-width">
                <h4>朋友關係</h4>
                <div class="detail-text" *ngIf="selectedPerson.friends && selectedPerson.friends.trim(); else noFriends">
                  <pre>{{ selectedPerson.friends }}</pre>
                </div>
                <ng-template #noFriends>
                  <div class="detail-text">無朋友關係資料</div>
                </ng-template>
              </div>
              
              <div class="detail-section full-width">
                <h4>個人資料</h4>
                <div class="detail-text" *ngIf="getPersonalInfo(); else noProfileData">
                  <pre>{{ getPersonalInfo() }}</pre>
                </div>
                <ng-template #noProfileData>
                  <div class="detail-text">無個人資料</div>
                </ng-template>
              </div>
              
              <div class="detail-section">
                <h4>系統資訊</h4>
                <div class="detail-row">
                  <label>建立時間：</label>
                  <span>{{ selectedPerson.createdAt | date:'yyyy-MM-dd HH:mm:ss' }}</span>
                </div>
                <div class="detail-row">
                  <label>更新時間：</label>
                  <span>{{ selectedPerson.updatedAt | date:'yyyy-MM-dd HH:mm:ss' }}</span>
                </div>
              </div>
            </div>
          </div>
          
          <div class="modal-footer">
            <button class="btn btn-primary" (click)="goToVisualAnalysis()">
              視覺分析
            </button>
            <button class="btn btn-secondary" (click)="closeDetailModal()">
              關閉
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./family-tree.page.scss']
})
export class FamilyTreeComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('graphContainer', { static: false }) graphContainer!: ElementRef;
  @ViewChild('treeViewport', { static: false }) treeViewport!: ElementRef;

  private svg: any;
  private simulation: any;
  private nodes: PersonNode[] = [];
  private links: Relationship[] = [];
  private isDragging = false;
  private allPersons: Person[] = [];
  private hiddenNodes: Set<string> = new Set(); // 追蹤被隱藏的節點
  private allLinks: Relationship[] = []; // 保存所有連線的原始資料

  // 查詢相關屬性
  showGraph = false;
  searchResults: Person[] = [];
  showDetailModal = false;
  selectedPerson: Person | null = null;
  currentPersonId: number = 0;
  centerNodeInitialized: boolean = false;
  searchCriteria: SearchCriteria = { ...AppConstants.DEFAULT_SEARCH_CRITERIA };

  private eventSubscription?: Subscription;

  constructor(
    private personService: PersonService, 
    private route: ActivatedRoute,
    private eventService: EventService,
    private cdr: ChangeDetectorRef,
    private projectService: ProjectService,
    private router: Router
  ) {}

  ngOnInit() {
    // 檢查是否有當前專案
    this.checkCurrentProject();

    // 訂閱全文檢索點擊事件
    this.eventSubscription = this.eventService.fullTextSearchClick$.subscribe(() => {
      this.showSearchView();
    });

    // 檢查是否從分析結果頁面導航過來
    this.route.queryParams.subscribe(params => {
      if (params['viewResults'] === 'true' && params['personId']) {
        const personId = parseInt(params['personId']);
        console.log('從分析結果頁面導航過來，載入分析結果:', personId);
        this.loadAnalysisResult(personId);
      }
    });
  }

  ngAfterViewInit() {
    // 延遲初始化圖譜，直到需要顯示時
  }

  ngOnDestroy() {
    // 清理訂閱
    if (this.eventSubscription) {
      this.eventSubscription.unsubscribe();
    }
  }

  // 查詢相關方法
  performSearch() {
    // 檢查至少填寫一個條件
    const filledFields = Object.values(this.searchCriteria).filter(value => value && value.trim()).length;
    if (filledFields < 1) {
      alert('請至少填寫一個查詢條件');
      return;
    }

    // 暫時使用完整API，然後在前端過濾
    this.personService.getPersons().subscribe({
      next: (persons: Person[]) => {
        // 在前端進一步過濾其他條件
        this.searchResults = this.filterPersons(persons);
        console.log('查詢結果:', this.searchResults);
      },
      error: (error: any) => {
        console.error('查詢失敗:', error);
        alert('查詢失敗，請檢查後端服務是否正常運行');
      }
    });
  }

  private filterPersons(persons: Person[]): Person[] {
    return persons.filter(person => {
      // 檢查身分證號
      if (this.searchCriteria.idNumber && this.searchCriteria.idNumber.trim()) {
        if (!person.idNumber || !person.idNumber.includes(this.searchCriteria.idNumber)) {
          return false;
        }
      }

      // 檢查護照號碼
      if (this.searchCriteria.passportNumber && this.searchCriteria.passportNumber.trim()) {
        if (!person.passportNumber || !person.passportNumber.includes(this.searchCriteria.passportNumber)) {
          return false;
        }
      }

      // 檢查生日
      if (this.searchCriteria.birthday && this.searchCriteria.birthday.trim()) {
        if (!person.birthday || !person.birthday.includes(this.searchCriteria.birthday)) {
          return false;
        }
      }

      // 檢查姓名
      if (this.searchCriteria.name && this.searchCriteria.name.trim()) {
        if (!person.name || !person.name.includes(this.searchCriteria.name)) {
          return false;
        }
      }

      // 檢查電話
      if (this.searchCriteria.mobile && this.searchCriteria.mobile.trim()) {
        if ((!person.mobile || !person.mobile.includes(this.searchCriteria.mobile)) &&
            (!person.phone || !person.phone.includes(this.searchCriteria.mobile))) {
          return false;
        }
      }

      // 檢查性別
      if (this.searchCriteria.gender && this.searchCriteria.gender.trim()) {
        if (person.gender !== this.searchCriteria.gender) {
          return false;
        }
      }

      // 如果所有條件都通過，則返回 true
      return true;
    });
  }

  resetSearch() {
    this.searchCriteria = { ...AppConstants.DEFAULT_SEARCH_CRITERIA };
    this.searchResults = [];
  }

  showGraphView() {
    console.log('顯示圖譜視圖');
    this.showGraph = true;
    
    // 使用 ChangeDetectorRef 確保 DOM 更新完成
    this.cdr.detectChanges();
    
    // 等待 DOM 更新完成後再初始化圖譜
    setTimeout(() => {
      console.log('延遲初始化圖譜');
      if (this.graphContainer && this.graphContainer.nativeElement) {
        this.initGraph();
        // 不載入所有資料，只初始化圖譜容器
      } else {
        console.error('DOM 元素尚未準備好，再次延遲');
        setTimeout(() => {
          if (this.graphContainer && this.graphContainer.nativeElement) {
            this.initGraph();
            // 不載入所有資料，只初始化圖譜容器
          } else {
            console.error('DOM 元素仍然未準備好');
            // 最後一次嘗試，使用更長的延遲
            setTimeout(() => {
              if (this.graphContainer && this.graphContainer.nativeElement) {
                this.initGraph();
              } else {
                console.error('DOM 元素最終未準備好，無法初始化圖譜');
              }
            }, 500);
          }
        }, 200);
      }
    }, 100);
  }

  showSearchView() {
    this.showGraph = false;
    // 清空搜尋結果和重置搜尋條件
    this.searchResults = [];
    this.resetSearch();
    // 確保搜尋表單可見
    this.showDetailModal = false;
    this.selectedPerson = null;
  }

  viewPersonDetails(person: Person) {
    console.log('查看人員詳情:', person);
    this.selectedPerson = person;
    this.showDetailModal = true;
  }

  closeDetailModal() {
    this.showDetailModal = false;
    this.selectedPerson = null;
  }

  goToVisualAnalysis() {
    if (this.selectedPerson) {
      this.startVisualAnalysis(this.selectedPerson.id);
    }
  }

  startVisualAnalysis(personId: number) {
    this.personService.startAnalysis(personId).subscribe({
      next: (response) => {
        if (response.success) {
          // 顯示成功訊息
          this.showNotification('視覺分析已開始，正在背景處理中...', 'success');
          
          // 開始監控進度
          this.monitorAnalysisProgress(personId);
          
          // 關閉詳情視窗
          this.closeDetailModal();
        } else {
          // 檢查是否是重複任務的錯誤
          if (response.message && response.message.includes('已有進行中的分析任務')) {
            this.showNotification('該人員已有進行中的分析任務，請稍後再試或重置分析狀態', 'info');
            // 提供重置選項
            if (confirm('是否要重置分析狀態？')) {
              this.resetAnalysis(personId);
            }
          } else {
            this.showNotification(response.message || '啟動分析失敗', 'error');
          }
        }
      },
      error: (error) => {
        console.error('啟動分析錯誤:', error);
        this.showNotification('啟動分析時發生錯誤', 'error');
      }
    });
  }

  resetAnalysis(personId: number) {
    this.personService.resetAnalysis(personId).subscribe({
      next: (response) => {
        if (response.success) {
          this.showNotification('分析狀態已重置，可以重新開始分析', 'success');
          // 重新嘗試啟動分析
          setTimeout(() => {
            this.startVisualAnalysis(personId);
          }, 1000);
        } else {
          this.showNotification(response.message || '重置分析狀態失敗', 'error');
        }
      },
      error: (error) => {
        console.error('重置分析錯誤:', error);
        this.showNotification('重置分析狀態時發生錯誤', 'error');
      }
    });
  }

  monitorAnalysisProgress(personId: number) {
    const progressInterval = setInterval(() => {
      this.personService.getAnalysisProgress(personId).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            const progress = response.data;
            
            // 更新進度顯示
            this.updateProgressDisplay(progress);
            
            // 如果分析完成
            if (progress.status === 'completed') {
              clearInterval(progressInterval);
              this.showNotification('視覺分析完成！正在載入結果...', 'success');
              
              // 直接查看結果，不需要用戶確認
              this.viewAnalysisResult(personId);
            } else if (progress.status === 'failed') {
              clearInterval(progressInterval);
              this.showNotification('視覺分析失敗', 'error');
            }
          }
        },
        error: (error) => {
          console.error('查詢進度錯誤:', error);
          clearInterval(progressInterval);
        }
      });
    }, 3000); // 每3秒查詢一次進度
  }

  viewAnalysisResult(personId: number) {
    this.personService.getAnalysisResult(personId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const result = response.data;
          console.log('分析結果:', result);
          
          // 將分析結果轉換為圖譜數據
          this.convertAnalysisResultToGraph(result.analysisResult);
          
          // 顯示圖譜視圖
          this.showGraphView();
          
          this.showNotification(`正在顯示 ${result.personName} 的分析結果`, 'success');
        } else {
          this.showNotification(response.message || '獲取分析結果失敗', 'error');
        }
      },
      error: (error) => {
        console.error('獲取分析結果錯誤:', error);
        this.showNotification('獲取分析結果時發生錯誤', 'error');
      }
    });
  }

  convertAnalysisResultToGraph(analysisResult: any) {
    try {
      console.log('開始轉換分析結果:', analysisResult);
      console.log('分析結果類型:', typeof analysisResult);
      
      // 清空現有的節點和連線
      this.nodes = [];
      this.links = [];
      
      // 解析分析結果 - 如果是字串，先解析為 JSON
      let parsedResult: any[] = [];
      if (typeof analysisResult === 'string') {
        try {
          parsedResult = JSON.parse(analysisResult);
          console.log('解析 JSON 字串成功:', parsedResult);
        } catch (parseError) {
          console.error('解析 JSON 字串失敗:', parseError);
          this.showNotification('解析分析結果失敗', 'error');
          return;
        }
      } else if (Array.isArray(analysisResult)) {
        parsedResult = analysisResult;
      } else {
        console.warn('分析結果格式不正確:', analysisResult);
        this.showNotification('分析結果格式不正確', 'error');
        return;
      }
      
      console.log('解析後的分析結果:', parsedResult);
      console.log('解析後是否為陣列:', Array.isArray(parsedResult));
      
      // 處理解析後的分析結果
      if (parsedResult && Array.isArray(parsedResult)) {
        console.log('分析結果陣列長度:', parsedResult.length);
        
        // 獲取當前人員的基本資料
        this.personService.getPerson(this.currentPersonId).subscribe({
          next: (person) => {
            console.log('獲取到當前人員資料:', person);
            
            // 添加中心節點（當前人員）
            const centerNode: PersonNode = {
              id: person.id.toString(),
              name: person.name,
              gender: person.gender === '男' ? 'male' : 'female',
              isExpanded: true
            };
            this.nodes.push(centerNode);
            console.log('添加中心節點:', centerNode);
            
            // 添加關係節點和連線
            parsedResult.forEach((relation: any, index: number) => {
              console.log(`處理關係 ${index}:`, relation);
              
              // 檢查關係資料的完整性 - 使用正確的欄位名稱
              if (!relation.target_name) {
                console.warn(`關係 ${index} 缺少 target_name:`, relation);
                return;
              }
              
              // 嘗試從分析結果中獲取性別，如果沒有則使用預設值
              let targetGender: 'male' | 'female' = 'male'; // 預設為男性
              if (relation.target_gender) {
                targetGender = relation.target_gender === '女' ? 'female' : 'male';
              } else if (relation.target_person_id) {
                // 如果有 person_id，嘗試從數據庫獲取性別信息
                // 這裡可以添加邏輯來獲取性別信息
                console.log('需要從數據庫獲取性別信息:', relation.target_person_id);
              }
              
              const targetNode: PersonNode = {
                id: relation.target_person_id ? relation.target_person_id.toString() : `node_${index}`,
                name: relation.target_name,
                gender: targetGender,
                isExpanded: false
              };
              
              // 檢查是否已存在相同ID的節點
              const existingNode = this.nodes.find(n => n.id === targetNode.id);
              if (!existingNode) {
                this.nodes.push(targetNode);
                console.log('添加目標節點:', targetNode);
              } else {
                console.log('節點已存在，跳過:', targetNode.name);
              }
              
              // 添加連線
              const link: Relationship = {
                source: person.id.toString(),
                target: targetNode.id,
                type: relation.relation_type || '關係',
                isFamily: relation.source_field === 'family_relationships'
              };
              
              // 檢查是否已存在相同的連線
              const existingLink = this.links.find(l => 
                (l.source === link.source && l.target === link.target) ||
                (l.source === link.target && l.target === link.source)
              );
              if (!existingLink) {
                this.links.push(link);
                console.log('添加連線:', link);
              } else {
                console.log('連線已存在，跳過:', link);
              }
            });
            
            console.log('轉換完成 - 節點數量:', this.nodes.length);
            console.log('轉換完成 - 連線數量:', this.links.length);
            console.log('轉換後的節點:', this.nodes.map(n => ({ name: n.name, gender: n.gender })));
            console.log('轉換後的連線:', this.links);
            
            // 保存所有連線的原始資料
            this.allLinks = [...this.links];
            console.log('保存原始連線資料:', this.allLinks.length, '條');
            
            // 清空隱藏節點集合
            this.hiddenNodes.clear();
            
            // 確保圖譜容器已初始化
            if (this.svg) {
              console.log('SVG 已存在，直接更新圖譜');
              this.updateGraph();
            } else {
              console.log('SVG 尚未初始化，先初始化圖譜');
              this.initGraph();
              setTimeout(() => {
                console.log('延遲更新圖譜');
                this.updateGraph();
              }, 100);
            }
          },
          error: (error) => {
            console.error('獲取人員資料失敗:', error);
            this.showNotification('獲取人員資料失敗', 'error');
          }
        });
      } else {
        console.warn('分析結果格式不正確:', analysisResult);
        console.warn('分析結果類型:', typeof analysisResult);
        this.showNotification('分析結果格式不正確', 'error');
      }
    } catch (error) {
      console.error('轉換分析結果失敗:', error);
      this.showNotification('轉換分析結果失敗', 'error');
    }
  }

  updateProgressDisplay(progress: any) {
    // 這裡可以更新UI顯示進度
    console.log(`分析進度: ${progress.progressPercentage}% - ${progress.status}`);
  }

  showNotification(message: string, type: 'success' | 'error' | 'info') {
    // 創建通知元素
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
      <div class="notification-content">
        <span class="notification-icon">${this.getNotificationIcon(type)}</span>
        <span class="notification-message">${message}</span>
      </div>
    `;
    
    // 添加樣式
    notification.style.cssText = `
      position: fixed;
      top: 20px;
      right: 20px;
      background: ${this.getNotificationColor(type)};
      color: white;
      padding: 12px 20px;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      z-index: 10000;
      transform: translateX(100%);
      transition: transform 0.3s ease;
      max-width: 300px;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    `;
    
    // 添加到頁面
    document.body.appendChild(notification);
    
    // 顯示動畫
    setTimeout(() => {
      notification.style.transform = 'translateX(0)';
    }, 100);
    
    // 自動消失
    setTimeout(() => {
      notification.style.transform = 'translateX(100%)';
      setTimeout(() => {
        if (document.body.contains(notification)) {
          document.body.removeChild(notification);
        }
      }, 300);
    }, 3000);
  }

  private getNotificationIcon(type: 'success' | 'error' | 'info'): string {
    switch (type) {
      case 'success': return '✅';
      case 'error': return '❌';
      case 'info': return 'ℹ️';
      default: return 'ℹ️';
    }
  }

  private getNotificationColor(type: 'success' | 'error' | 'info'): string {
    switch (type) {
      case 'success': return '#4caf50';
      case 'error': return '#f44336';
      case 'info': return '#2196f3';
      default: return '#2196f3';
    }
  }

  getPersonalInfo(): string {
    if (!this.selectedPerson) return '';
    
    let personalInfo = '';
    
    // 如果有 profileData，先顯示
    if (this.selectedPerson.profileData && this.selectedPerson.profileData.trim()) {
      personalInfo += this.selectedPerson.profileData + '\n\n';
    }
    
    // 從朋友關係中提取著作和活動資訊
    if (this.selectedPerson.friends && this.selectedPerson.friends.trim()) {
      const friendsData = this.selectedPerson.friends;
      
      // 提取著作資訊（包含書名號的內容）
      const publications = friendsData.match(/《[^》]+》/g);
      if (publications && publications.length > 0) {
        personalInfo += '【著作】\n';
        publications.forEach((pub, index) => {
          personalInfo += `${index + 1}. ${pub}\n`;
        });
        personalInfo += '\n';
      }
      
      // 提取活動資訊（包含機構、職位等）
      const activities = friendsData.match(/[^，、\n]+(?:學院|大學|中心|會|協會|公司|社|部|處|館|院|所|室|組|隊|團|社|會|中心|機構|組織|單位|部門|職位|職務|工作|活動|項目|計劃|研究|發表|出版|參與|擔任|任職|任職於|服務於|工作於|就職於)[^，、\n]*/g);
      if (activities && activities.length > 0) {
        personalInfo += '【參與活動】\n';
        const uniqueActivities = [...new Set(activities)];
        uniqueActivities.forEach((activity, index) => {
          if (activity.trim() && !activity.includes('，') && !activity.includes('、')) {
            personalInfo += `${index + 1}. ${activity.trim()}\n`;
          }
        });
        personalInfo += '\n';
      }
    }
    
    return personalInfo.trim();
  }

  // 圖譜相關方法
  private initGraph() {
    console.log('開始初始化圖譜');
    console.log('graphContainer:', this.graphContainer);
    
    if (!this.graphContainer) {
      console.error('graphContainer 未初始化');
      return;
    }
    
    const container = this.graphContainer.nativeElement;
    console.log('container:', container);
    
    if (!container) {
      console.error('container nativeElement 未初始化');
      return;
    }
    
    const width = container.clientWidth;
    const height = container.clientHeight;
    console.log('容器尺寸:', width, 'x', height);

    // 清除現有內容
    d3.select(container).selectAll('*').remove();

    // 創建 SVG
    this.svg = d3.select(container)
      .append('svg')
      .attr('width', width)
      .attr('height', height)
      .style('background', '#f8f9fa');

    // 創建縮放行為
    const zoom = d3.zoom()
      .scaleExtent([0.1, 4])
      .on('zoom', (event) => {
        this.svg.select('.graph-group')
          .attr('transform', event.transform);
      });

    this.svg.call(zoom);

    // 創建圖形群組
    const graphGroup = this.svg.append('g')
      .attr('class', 'graph-group');

    // 創建力導向模擬
    this.simulation = d3.forceSimulation()
      .force('link', d3.forceLink().id((d: any) => d.id).distance(100))
      .force('charge', d3.forceManyBody().strength(-300))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide().radius(30));

    this.updateGraph();
  }

  private updateGraph() {
    console.log('開始更新圖譜');
    console.log('SVG:', this.svg);
    
    if (!this.svg) {
      console.error('SVG 未初始化，無法更新圖譜');
      return;
    }
    
    const graphGroup = this.svg.select('.graph-group');
    console.log('graphGroup:', graphGroup);

    // 畫布中心
    const svgWidth = +this.svg.attr('width');
    const svgHeight = +this.svg.attr('height');
    const centerX = svgWidth / 2;
    const centerY = svgHeight / 2;

    // 固定主角節點在中央
    this.nodes.forEach(node => {
      if (node.id === this.currentPersonId.toString()) {
        node.fx = centerX;
        node.fy = centerY;
      }
    });

    // 過濾隱藏的節點
    const visibleNodes = this.nodes.filter(node => !this.hiddenNodes.has(node.id));
    console.log('可見節點:', visibleNodes.map(n => n.name));
    console.log('隱藏節點:', Array.from(this.hiddenNodes));

    // 過濾連線：只顯示兩個端點都可見的連線
    const visibleLinks = this.allLinks.filter(link => {
      const sourceVisible = !this.hiddenNodes.has(link.source);
      const targetVisible = !this.hiddenNodes.has(link.target);
      return sourceVisible && targetVisible;
    });

    console.log('可見連線數量:', visibleLinks.length, '總連線數量:', this.allLinks.length);

    // 更新連線
    const link = graphGroup.selectAll('.link')
      .data(visibleLinks)
      .join('line')
      .attr('class', 'link')
      .style('stroke', (d: any) => d.isFamily ? '#ff6b35' : '#666')
      .style('stroke-width', 2)
      .style('opacity', 0.6);

    // 添加連線標籤
    const linkLabels = graphGroup.selectAll('.link-label')
      .data(visibleLinks)
      .join('text')
      .attr('class', 'link-label')
      .text((d: any) => d.type)
      .style('font-size', '10px')
      .style('fill', '#333')
      .style('text-anchor', 'middle')
      .style('pointer-events', 'none');

    // 更新節點
    const node = graphGroup.selectAll('.node')
      .data(visibleNodes)
      .join('g')
      .attr('class', 'node')
      .call(this.dragBehavior());

    // 節點圓圈
    node.selectAll('.node-circle')
      .data((d: any) => [d])
      .join('circle')
      .attr('class', 'node-circle')
      .attr('r', (d: any) => {
        // 主角節點更大
        if (d.id === this.currentPersonId.toString()) {
          return d.isExpanded ? 40 : 35;
        }
        return d.isExpanded ? 30 : 25;
      })
      .style('fill', (d: any) => {
        // 主角節點特殊顏色
        if (d.id === this.currentPersonId.toString()) {
          console.log('主角節點顏色:', d.name, '#FFD700');
          return '#FFD700'; // 金色
        }
        let color;
        if (d.gender === 'male') {
          color = d.isExpanded ? '#42A5F5' : '#1976d2';
        } else {
          color = d.isExpanded ? '#F48FB1' : '#e91e63';
        }
        console.log('節點顏色:', d.name, d.gender, color);
        return color;
      })
      .style('stroke', (d: any) => {
        // 主角節點特殊邊框
        if (d.id === this.currentPersonId.toString()) {
          return '#FF8C00'; // 深橙色邊框
        }
        return d.isExpanded ? '#1976d2' : '#fff';
      })
      .style('stroke-width', (d: any) => {
        // 主角節點更粗的邊框
        if (d.id === this.currentPersonId.toString()) {
          return 5;
        }
        return d.isExpanded ? 4 : 3;
      })
      .style('cursor', (d: any) => {
        // 主角節點不可拖曳
        if (d.id === this.currentPersonId.toString()) {
          return 'default';
        }
        return 'pointer';
      })
      .on('click', (event: any, d: any) => {
        console.log('節點被點擊:', d.name);
        const person = this.allPersons.find(p => p.id.toString() === d.id);
        if (person) {
          this.viewPersonDetails(person);
        }
      });

    // 節點頭像或圖示
    node.selectAll('.node-icon')
      .data((d: any) => [d])
      .join('text')
      .attr('class', 'node-icon')
      .attr('dy', '0.35em')
      .style('text-anchor', 'middle')
      .style('font-size', '16px')
      .style('fill', '#fff')
      .style('pointer-events', 'none')
      .text((d: any) => d.photo ? '' : '👤');

    // 節點名稱
    node.selectAll('.node-label')
      .data((d: any) => [d])
      .join('text')
      .attr('class', 'node-label')
      .attr('dy', (d: any) => {
        // 主角節點名字位置調整
        if (d.id === this.currentPersonId.toString()) {
          return 50;
        }
        return 40;
      })
      .style('text-anchor', 'middle')
      .style('font-size', (d: any) => {
        // 主角節點名字更大
        if (d.id === this.currentPersonId.toString()) {
          return '14px';
        }
        return '12px';
      })
      .style('fill', (d: any) => {
        // 主角節點名字特殊顏色
        if (d.id === this.currentPersonId.toString()) {
          return '#FF8C00'; // 深橙色
        }
        return '#333';
      })
      .style('font-weight', (d: any) => {
        // 主角節點名字更粗
        if (d.id === this.currentPersonId.toString()) {
          return '900';
        }
        return 'bold';
      })
      .style('pointer-events', 'none')
      .text((d: any) => d.name);

    // 展開/收合按鈕
    node.selectAll('.expand-btn')
      .data((d: any) => [d])
      .join('circle')
      .attr('class', 'expand-btn')
      .attr('r', 12) // 增加按鈕大小
      .attr('cx', 20)
      .attr('cy', -20)
      .style('fill', (d: any) => d.isExpanded ? '#e3f2fd' : '#fff')
      .style('stroke', (d: any) => d.isExpanded ? '#1976d2' : '#666')
      .style('stroke-width', 2)
      .style('cursor', 'pointer')
      .style('opacity', 1)
      .style('z-index', '1000') // 確保按鈕在最上層
      .on('click', (event: any, d: any) => {
        console.log('展開/收合按鈕被點擊:', d.name);
        event.stopPropagation();
        event.preventDefault();
        this.toggleNode(d);
      })
      .on('mouseover', function(this: any) {
        d3.select(this).style('opacity', 0.8);
      })
      .on('mouseout', function(this: any) {
        d3.select(this).style('opacity', 1);
      });

    node.selectAll('.expand-icon')
      .data((d: any) => [d])
      .join('text')
      .attr('class', 'expand-icon')
      .attr('x', 20)
      .attr('y', -15)
      .style('text-anchor', 'middle')
      .style('font-size', '14px')
      .style('font-weight', 'bold')
      .style('fill', (d: any) => d.isExpanded ? '#1976d2' : '#666')
      .style('pointer-events', 'none')
      .style('user-select', 'none')
      .text((d: any) => d.isExpanded ? '−' : '+');

    // 更新模擬
    this.simulation
      .nodes(visibleNodes)
      .on('tick', () => {
        link
          .attr('x1', (d: any) => d.source.x)
          .attr('y1', (d: any) => d.source.y)
          .attr('x2', (d: any) => d.target.x)
          .attr('y2', (d: any) => d.target.y);

        linkLabels
          .attr('x', (d: any) => (d.source.x + d.target.x) / 2)
          .attr('y', (d: any) => (d.source.y + d.target.y) / 2);

        node
          .attr('transform', (d: any) => `translate(${d.x},${d.y})`);
      });

    this.simulation.force('link')
      .links(visibleLinks);
    
    // 主角節點固定邏輯 - 確保主角節點永遠在中央且不可拖動
    if (this.currentPersonId > 0) {
      console.log('檢查主角節點固定，currentPersonId:', this.currentPersonId);
      const centerNode = visibleNodes.find(n => n.id === this.currentPersonId.toString());
      if (centerNode) {
        // 強制固定主角節點在中央
        centerNode.fx = centerX;
        centerNode.fy = centerY;
        
        console.log('固定主角節點在中央:', centerNode.name, 'at', centerNode.fx, centerNode.fy);
        
        // 重新啟動模擬以應用固定位置
        this.simulation.alpha(1).restart();
      } else {
        console.warn('找不到主角節點，currentPersonId:', this.currentPersonId);
        console.log('可見節點:', visibleNodes.map(n => ({ id: n.id, name: n.name })));
      }
    }
    
    console.log('圖譜更新完成');
  }

  private dragBehavior() {
    return d3.drag()
      .on('start', (event: any, d: any) => {
        // 主角節點不可拖曳
        if (d.id === this.currentPersonId.toString()) {
          return;
        }
        if (!event.active) this.simulation.alphaTarget(0.3).restart();
        // 記錄拖動開始時的位置
        d.fx = d.x;
        d.fy = d.y;
      })
      .on('drag', (event: any, d: any) => {
        // 主角節點不可拖曳
        if (d.id === this.currentPersonId.toString()) {
          return;
        }
        // 更新拖動中的位置
        d.fx = event.x;
        d.fy = event.y;
      })
      .on('end', (event: any, d: any) => {
        // 主角節點不可拖曳
        if (d.id === this.currentPersonId.toString()) {
          return;
        }
        if (!event.active) this.simulation.alphaTarget(0);
        // 拖動結束後，釋放固定位置，讓節點可以自由移動
        d.fx = null;
        d.fy = null;
      });
  }

  private parseFamilyRelationships(relationships: string): Relationship[] {
    const links: Relationship[] = [];
    if (!relationships) return links;

    // 解析家族關係文字
    const lines = relationships.split('\n').filter(line => line.trim());
    
    for (const line of lines) {
      const parts = line.split('：');
      if (parts.length === 2) {
        const relationType = parts[0].trim();
        const targetNames = parts[1].split(/[,，、]/).map(name => name.trim());
        
        for (const targetName of targetNames) {
          if (targetName) {
            // 查找目標人物
            const targetPerson = this.allPersons.find(p => p.name === targetName);
            if (targetPerson) {
              links.push({
                source: targetPerson.id.toString(),
                target: targetPerson.id.toString(),
                type: relationType,
                isFamily: true
              });
            }
          }
        }
      }
    }
    
    return links;
  }

  private parseFriends(friends: string, currentPersonId: string): Relationship[] {
    const links: Relationship[] = [];
    if (!friends) return links;

    // 解析朋友關係文字
    const lines = friends.split('\n').filter(line => line.trim());
    
    for (const line of lines) {
      // 先嘗試用逗號分割
      const parts = line.split(/[,，、]/);
      for (const part of parts) {
        const trimmedPart = part.trim();
        if (trimmedPart) {
          // 提取名字（通常是第一個詞）
          const nameMatch = trimmedPart.match(/^([^\s，、]+)/);
          if (nameMatch) {
            const name = nameMatch[1].trim();
            if (name) {
              // 查找朋友
              const friendPerson = this.allPersons.find(p => p.name === name);
              if (friendPerson) {
                links.push({
                  source: currentPersonId,
                  target: friendPerson.id.toString(),
                  type: '朋友',
                  isFamily: false
                });
              }
            }
          }
        }
      }
    }
    
    return links;
  }

  loadTreeData() {
    this.personService.getPersons().subscribe({
      next: (persons: Person[]) => {
        this.allPersons = persons.filter((p: Person) => p.name && p.name.trim()); // 過濾掉空名字
        
        // 建立節點
        this.nodes = this.allPersons.map(person => ({
          id: person.id.toString(),
          name: person.name,
          gender: person.gender === '男' ? 'male' : 'female',
          isExpanded: true
        }));

        // 建立連線
        this.links = [];
        
        // 處理家族關係
        for (const person of this.allPersons) {
          if (person.familyRelationships) {
            const familyLinks = this.parseFamilyRelationships(person.familyRelationships);
            this.links.push(...familyLinks);
          }
        }
        
        // 處理朋友關係
        for (const person of this.allPersons) {
          if (person.friends) {
            const friendLinks = this.parseFriends(person.friends, person.id.toString());
            this.links.push(...friendLinks);
          }
        }

        // 移除重複的連線
        this.links = this.links.filter((link, index, self) => 
          index === self.findIndex(l => 
            (l.source === link.source && l.target === link.target && l.type === link.type) ||
            (l.source === link.target && l.target === link.source && l.type === link.type)
          )
        );

        // 保存所有連線的原始資料
        this.allLinks = [...this.links];

        console.log('載入的節點:', this.nodes.map(n => ({ name: n.name, gender: n.gender })));
        console.log('載入的連線:', this.links);
        
        this.updateGraph();
      },
      error: (error: any) => {
        console.error('載入資料失敗:', error);
        // 如果 API 失敗，顯示錯誤訊息
        alert('載入資料失敗，請檢查後端服務是否正常運行');
      }
    });
  }

  toggleNode(node: PersonNode) {
    console.log('切換節點展開狀態:', node.name, '當前狀態:', node.isExpanded);
    node.isExpanded = !node.isExpanded;
    
    // 顯示通知
    const action = node.isExpanded ? '展開' : '收合';
    this.showNotification(`${action}節點: ${node.name}`, 'info');
    
    if (node.isExpanded) {
      console.log('展開節點:', node.name);
      // 展開節點：顯示更多相關節點
      this.expandNode(node);
    } else {
      console.log('收合節點:', node.name);
      // 收合節點：隱藏相關節點
      this.collapseNode(node);
    }
    
    // 延遲更新圖譜，讓用戶看到狀態變化
    setTimeout(() => {
      this.updateGraph();
    }, 100);
  }

  private expandNode(node: PersonNode) {
    console.log('展開節點功能 - 顯示相關節點:', node.name);
    
    // 找到與該節點相關的所有節點
    const relatedNodes = this.findRelatedNodes(node.id);
    
    // 將相關節點從隱藏集合中移除
    relatedNodes.forEach(nodeId => {
      this.hiddenNodes.delete(nodeId);
    });
    
    console.log('展開節點，顯示相關節點:', relatedNodes);
  }

  private collapseNode(node: PersonNode) {
    console.log('收合節點功能 - 隱藏相關節點:', node.name);
    
    // 找到與該節點相關的所有節點
    const relatedNodes = this.findRelatedNodes(node.id);
    
    // 將相關節點添加到隱藏集合中
    relatedNodes.forEach(nodeId => {
      this.hiddenNodes.add(nodeId);
    });
    
    console.log('收合節點，隱藏相關節點:', relatedNodes);
  }

  private findRelatedNodes(nodeId: string): string[] {
    const relatedNodes = new Set<string>();
    
    // 找到所有與該節點相連的節點
    this.allLinks.forEach(link => {
      if (link.source === nodeId) {
        relatedNodes.add(link.target);
      } else if (link.target === nodeId) {
        relatedNodes.add(link.source);
      }
    });
    
    return Array.from(relatedNodes);
  }

  resetView() {
    this.svg.transition().duration(750).call(
      d3.zoom().transform,
      d3.zoomIdentity
    );
  }

  zoomIn() {
    this.svg.transition().duration(300).call(
      this.svg.zoom().scaleBy,
      1.3
    );
  }

  zoomOut() {
    this.svg.transition().duration(300).call(
      this.svg.zoom().scaleBy,
      1 / 1.3
    );
  }

  toggleDrag() {
    this.isDragging = !this.isDragging;
    // 可以添加拖拽模式的視覺反饋
  }

  toggleFullscreen() {
    const element = this.treeViewport.nativeElement;
    if (document.fullscreenElement) {
      document.exitFullscreen();
    } else {
      element.requestFullscreen();
    }
  }



  loadAnalysisResult(personId: number) {
    console.log('載入分析結果，personId:', personId);
    this.currentPersonId = personId;
    this.centerNodeInitialized = false; // 重置初始化標記
    this.personService.getAnalysisResult(personId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const result = response.data;
          console.log('分析結果:', result);
          
          // 先顯示圖譜視圖，確保 DOM 元素準備好
          this.showGraphView();
          
          // 延遲轉換分析結果，確保圖譜已初始化
          setTimeout(() => {
            this.convertAnalysisResultToGraph(result.analysisResult);
          }, 300);
          
          this.showNotification(`正在顯示 ${result.personName} 的分析結果`, 'success');
        } else {
          this.showNotification(response.message || '獲取分析結果失敗', 'error');
        }
      },
      error: (error) => {
        console.error('獲取分析結果錯誤:', error);
        this.showNotification('獲取分析結果時發生錯誤', 'error');
      }
    });
  }

  /**
   * 檢查當前專案
   */
  private checkCurrentProject(): void {
    const currentProject = this.projectService.getCurrentProject();
    
    if (!currentProject) {
      console.warn('⚠️ 沒有設置當前專案，導航回專案管理頁面');
      this.router.navigate(['/']).then(() => {
        console.log('🔄 已導航回專案管理頁面');
      });
    } else {
      console.log('✅ 當前專案:', currentProject.projectName);
    }
  }

  /**
   * 獲取當前專案名稱
   */
  getCurrentProjectName(): string {
    const currentProject = this.projectService.getCurrentProject();
    return currentProject ? currentProject.projectName : 'AI關聯分析系統';
  }
} 