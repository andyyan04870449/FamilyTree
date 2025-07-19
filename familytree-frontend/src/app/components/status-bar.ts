import { Component } from '@angular/core';

@Component({
  selector: 'app-status-bar',
  templateUrl: './status-bar.html',
  styleUrls: ['./status-bar.scss'],
  standalone: true
})
export class StatusBarComponent {
  userName = '王小明';
  countdown = 600; // 10 分鐘
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
    this.countdown = 600;
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
