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
  @Output() keywordSearchClick = new EventEmitter<void>();
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
    } else if (url.includes('/keyword-search')) {
      this.activeRoute = 'keyword-search';
    } else if (url.includes('/search-results')) {
      this.activeRoute = 'search-results';
    } else if (url.includes('/favorites')) {
      this.activeRoute = 'favorites';
    } else if (url.includes('/family-tree')) {
      this.activeRoute = 'family-tree';
    } else if (url.includes('/person-list')) {
      this.activeRoute = 'person-list';
    } else if (url.includes('/tree-operations')) {
      this.activeRoute = 'tree-operations';
    } else if (url.includes('/file-upload')) {
      this.activeRoute = 'file-upload';
    }
  }

  onKeywordSearchClick() {
    // 如果當前在 family-tree 路由，發送事件給父組件
    if (this.activeRoute === 'family-tree') {
      this.keywordSearchClick.emit();
    } else {
      // 否則正常導航
      this.router.navigate(['/family-tree']);
    }
  }
} 