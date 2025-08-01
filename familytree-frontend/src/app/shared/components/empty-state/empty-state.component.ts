import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="empty-state" role="status">
      <div class="empty-state__icon" aria-hidden="true">{{ icon }}</div>
      <h3 class="empty-state__title">{{ title }}</h3>
      <p class="empty-state__description" *ngIf="description">{{ description }}</p>
      <ng-content></ng-content>
    </div>
  `,
  styles: [`
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 3rem;
      text-align: center;
      gap: 1rem;
    }

    .empty-state__icon {
      font-size: 3rem;
      opacity: 0.5;
    }

    .empty-state__title {
      font-size: 1.25rem;
      color: #333;
      margin: 0;
      font-weight: 500;
    }

    .empty-state__description {
      font-size: 0.875rem;
      color: #666;
      margin: 0;
      max-width: 400px;
    }
  `]
})
export class EmptyStateComponent {
  @Input() icon: string = '📭';
  @Input() title: string = '暫無資料';
  @Input() description: string = '';
}