import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class LoginPage {
  constructor(private router: Router) {}

  onLoginSubmit(event: Event) {
    event.preventDefault();
    // 測試階段：直接導航到全文檢索頁面
    this.router.navigate(['/full-text-search']);
  }
} 