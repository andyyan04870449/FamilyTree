import { Component } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AppConstants } from '../../constants/app.constants';

@Component({
  selector: 'app-status-bar',
  templateUrl: './status-bar.component.html',
  styleUrls: ['./status-bar.component.scss'],
  standalone: true
})
export class StatusBarComponent {
  userName = AppConstants.DEFAULT_USER_NAME;
  countdown = AppConstants.SESSION_TIMEOUT_SECONDS;
  timer: any;
  currentPageName = '';

  constructor(private router: Router) {
    this.startCountdown();
    this.updateCurrentPageName(this.router.url);
    
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      this.updateCurrentPageName(event.url);
    });
  }

  startCountdown() {
    this.timer = setInterval(() => {
      if (this.countdown > 0) {
        this.countdown--;
      } else {
        clearInterval(this.timer);
      }
    }, 1000);
  }

  resetCountdown() {
    this.countdown = AppConstants.SESSION_TIMEOUT_SECONDS;
    this.startCountdown();
  }

  logout() {
    alert('已登出！(模擬)');
  }

  get countdownDisplay() {
    const min = Math.floor(this.countdown / 60).toString().padStart(2, '0');
    const sec = (this.countdown % 60).toString().padStart(2, '0');
    return `${min}:${sec}`;
  }

  getCurrentPageName() {
    return this.currentPageName;
  }

  private updateCurrentPageName(url: string) {
    // 不顯示頁面名稱，保持空字串
    this.currentPageName = '';
  }
}
