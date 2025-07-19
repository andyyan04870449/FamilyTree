import { Component } from '@angular/core';
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

  constructor() {
    this.startCountdown();
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
}
