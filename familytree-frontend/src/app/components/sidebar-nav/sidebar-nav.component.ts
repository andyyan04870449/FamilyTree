import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sidebar-nav',
  templateUrl: './sidebar-nav.component.html',
  styleUrls: ['./sidebar-nav-bem.scss'],
  standalone: true,
  imports: [RouterModule, CommonModule]
})
export class SidebarNavComponent implements OnInit {

  activeRoute: string = 'home';
  isSidebarOpen: boolean = false;

  constructor(
    private router: Router
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
    if (url.includes('/full-text-search')) {
      this.activeRoute = 'full-text-search';
    } else if (url.includes('/visual-analysis')) {
      this.activeRoute = 'visual-analysis';
    } else if (url.includes('/case-management')) {
      this.activeRoute = 'case-management';
    } else if (url.includes('/user-management')) {
      this.activeRoute = 'user-management';
    } else if (url.includes('/system-settings')) {
      this.activeRoute = 'system-settings';
    } else if (url === '/' || url === '/home') {
      this.activeRoute = 'home';
    }
  }


} 