import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sidebar-nav',
  templateUrl: './sidebar-nav.component.html',
  styleUrls: ['./sidebar-nav.component.scss'],
  standalone: true,
  imports: [RouterModule, CommonModule]
})
export class SidebarNavComponent implements OnInit {
  @Output() fullTextSearchClick = new EventEmitter<void>();
  activeRoute: string = 'home';

  constructor(private router: Router) {}

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

  private updateActiveRoute(url: string) {
    if (url === '/' || url === '/home') {
      this.activeRoute = 'home';
    } else if (url.includes('/file-upload')) {
      this.activeRoute = 'file-upload';
    } else if (url.includes('/file-management')) {
      this.activeRoute = 'file-management';
    } else if (url.includes('/person-management')) {
      this.activeRoute = 'person-management';
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

  onFullTextSearchClick() {
    // 如果當前在 family-tree 路由，發送事件給父組件
    if (this.activeRoute === 'family-tree') {
      this.fullTextSearchClick.emit();
    } else {
      // 否則導航到 family-tree 頁面
      this.router.navigate(['/family-tree']);
    }
  }
} 