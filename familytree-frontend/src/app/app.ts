import { Component } from '@angular/core';
import { RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StatusBarComponent } from './components/status-bar/status-bar.component';
import { SidebarNavComponent } from './components/sidebar-nav/sidebar-nav.component';
import { EventService } from './services/event.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrls: ['./app.scss'],
  standalone: true,
  imports: [CommonModule, RouterModule, StatusBarComponent, SidebarNavComponent]
})
export class App {
  constructor(private router: Router, private eventService: EventService) {}

  /**
   * 檢查當前是否為專案管理頁面
   */
  isProjectManagementPage(): boolean {
    return this.router.url === '/' || this.router.url.startsWith('/project-management');
  }

  onFullTextSearchClick() {
    // 如果當前在 family-tree 路由，發送事件給該組件
    if (this.router.url.includes('/family-tree')) {
      // 使用事件服務來與 family-tree 組件通信
      this.eventService.emitFullTextSearchClick();
    } else {
      // 否則正常導航
      this.router.navigate(['/family-tree']);
    }
  }
}
