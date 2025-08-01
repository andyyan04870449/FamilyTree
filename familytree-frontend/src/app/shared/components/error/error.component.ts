import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="error" role="alert" aria-live="assertive">
      <span class="error__icon" aria-hidden="true">❌</span>
      <p class="error__message">{{ message }}</p>
    </div>
  `,
  styles: [`
    .error {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 2rem;
      gap: 1rem;
      background-color: #fee;
      border: 1px solid #fcc;
      border-radius: 8px;
      margin: 1rem 0;
    }

    .error__icon {
      font-size: 2rem;
    }

    .error__message {
      color: #c00;
      font-size: 1rem;
      margin: 0;
      text-align: center;
    }
  `]
})
export class ErrorComponent {
  @Input() message: string = '發生錯誤';
}