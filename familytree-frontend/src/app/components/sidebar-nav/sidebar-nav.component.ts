import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProjectService } from '../../services/project.service';

@Component({
  selector: 'app-sidebar-nav',
  templateUrl: './sidebar-nav.component.html',
  styleUrls: ['./sidebar-nav.component.scss'],
  standalone: true,
  imports: [RouterModule, CommonModule]
})
export class SidebarNavComponent implements OnInit {

  activeRoute: string = 'home';
  isSidebarOpen: boolean = false;

  constructor(
    private router: Router,
    private projectService: ProjectService
  ) {}

  ngOnInit() {
    // 監聽路由變化，更新當前活動路由
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        this.updateActiveRoute(event.url);
      });

    // 初始化當前路由
    this.updateActiveRoute(this.router.url);
  }

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
    // 防止背景滾動
    if (this.isSidebarOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  }

  closeSidebar() {
    this.isSidebarOpen = false;
    document.body.style.overflow = '';
  }

  private updateActiveRoute(url: string) {
    if (url === '/' || url === '/home') {
      this.activeRoute = 'home';
    } else if (url.includes('/file-upload')) {
      this.activeRoute = 'file-upload';
    } else if (url.includes('/file-management')) {
      this.activeRoute = 'file-management';
    } else if (url.includes('/person-management')) {
      this.activeRoute = 'person-management';
    } else if (url.includes('/person-list')) {
      this.activeRoute = 'person-list';
    } else if (url.includes('/full-text-search')) {
      this.activeRoute = 'full-text-search';
    } else if (url.includes('/relationship-graph')) {
      this.activeRoute = 'relationship-graph';
    } else if (url.includes('/organization-chart')) {
      this.activeRoute = 'organization-chart';
    } else if (url.includes('/system-settings')) {
      this.activeRoute = 'system-settings';
    } else if (url.includes('/family-tree')) {
      this.activeRoute = 'family-tree';
    }
  }

  /**
   * 獲取當前專案
   */
  getCurrentProject() {
    return this.projectService.getCurrentProject();
  }

  /**
   * 切換專案
   */
  switchProject(): void {
    console.log('🔄 切換專案');
    this.closeSidebar();
    this.router.navigate(['/']).then(() => {
      console.log('✅ 導航回專案管理頁面');
    });
  }
} 