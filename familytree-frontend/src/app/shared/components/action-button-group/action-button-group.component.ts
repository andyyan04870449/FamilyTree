import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActionButtonComponent } from '../action-button/action-button.component';

export interface ActionButton {
  id: string;
  label?: string;
  icon?: string;
  title?: string;
  ariaLabel?: string;
  buttonClass?: string;
  isActive?: boolean;
  disabled?: boolean;
}

@Component({
  selector: 'app-action-button-group',
  standalone: true,
  imports: [CommonModule, ActionButtonComponent],
  template: `
    <div class="action-button-group" [class.action-button-group--vertical]="vertical">
      <app-action-button
        *ngFor="let action of actions"
        [label]="action.label"
        [icon]="action.icon"
        [title]="action.title"
        [ariaLabel]="action.ariaLabel"
        [buttonClass]="action.buttonClass || 'btn-action'"
        [isActive]="action.isActive || false"
        [disabled]="action.disabled || false"
        (clicked)="onActionClick(action.id, $event)"
      ></app-action-button>
    </div>
  `,
  styleUrls: ['./action-button-group.component.scss']
})
export class ActionButtonGroupComponent {
  @Input() actions: ActionButton[] = [];
  @Input() vertical = false;
  @Output() actionClicked = new EventEmitter<{ actionId: string; event: Event }>();

  onActionClick(actionId: string, event: Event): void {
    this.actionClicked.emit({ actionId, event });
  }
}