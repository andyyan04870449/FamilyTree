import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface PersonItem {
  personId: number;
  name: string;
  isVisible?: boolean;
  projectId?: number;
  avatar?: string;
  avatarColor?: string;
}

@Component({
  selector: 'app-person-item',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="person-item" [class.person-item--selected]="person.isVisible">
      <label class="person-item__label">
        <input 
          type="checkbox" 
          class="person-item__checkbox"
          [checked]="person.isVisible"
          (change)="onVisibilityChange($event)"
          [attr.aria-label]="'選擇 ' + person.name"
        >
        <span 
          class="person-item__avatar" 
          [style.background-color]="person.avatarColor"
          [attr.aria-hidden]="true"
        >
          {{ person.avatar || '👤' }}
        </span>
        <span class="person-item__name">{{ person.name }}</span>
      </label>
    </div>
  `,
  styleUrls: ['./person-item.component.scss']
})
export class PersonItemComponent {
  @Input() person!: PersonItem;
  @Output() visibilityChanged = new EventEmitter<{ personId: number; projectId?: number; isVisible: boolean }>();

  onVisibilityChange(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    this.visibilityChanged.emit({
      personId: this.person.personId,
      projectId: this.person.projectId,
      isVisible: checkbox.checked
    });
  }
}