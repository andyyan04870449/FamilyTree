import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PersonItemComponent, PersonItem } from '../person-item/person-item.component';

export interface ProjectGroup {
  projectId: number;
  projectName: string;
  projectColor?: string;
  isExpanded?: boolean;
  isSelected?: boolean;
  persons: PersonItem[];
}

@Component({
  selector: 'app-project-tree',
  standalone: true,
  imports: [CommonModule, FormsModule, PersonItemComponent],
  template: `
    <div class="project-tree">
      <div *ngFor="let project of projects" class="project-tree__group">
        <!-- 專案標題 -->
        <header 
          class="project-tree__header" 
          [style.background-color]="project.projectColor"
          [class.project-tree__header--expanded]="project.isExpanded"
        >
          <button 
            class="project-tree__expand-btn"
            (click)="toggleProject(project)"
            [attr.aria-expanded]="project.isExpanded"
            [attr.aria-label]="(project.isExpanded ? '收起' : '展開') + ' ' + project.projectName"
          >
            <span class="project-tree__expand-icon">{{ project.isExpanded ? '▼' : '▶' }}</span>
          </button>
          
          <label class="project-tree__label">
            <input 
              type="checkbox" 
              class="project-tree__checkbox"
              [checked]="project.isSelected"
              (change)="onProjectSelectionChange(project, $event)"
              [attr.aria-label]="'選擇專案 ' + project.projectName"
            >
            <span class="project-tree__name">{{ project.projectName }}</span>
          </label>
        </header>
        
        <!-- 人員列表 -->
        <div 
          class="project-tree__persons" 
          *ngIf="project.isExpanded"
          [@slideDown]
        >
          <app-person-item
            *ngFor="let person of project.persons"
            [person]="person"
            (visibilityChanged)="onPersonVisibilityChange($event, project)"
          ></app-person-item>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./project-tree.component.scss'],
  animations: [
    // 可選：添加展開/收起動畫
  ]
})
export class ProjectTreeComponent {
  @Input() projects: ProjectGroup[] = [];
  @Output() projectToggled = new EventEmitter<ProjectGroup>();
  @Output() projectSelectionChanged = new EventEmitter<{ project: ProjectGroup; isSelected: boolean }>();
  @Output() personVisibilityChanged = new EventEmitter<{ 
    projectId: number; 
    personId: number; 
    isVisible: boolean 
  }>();

  toggleProject(project: ProjectGroup): void {
    project.isExpanded = !project.isExpanded;
    this.projectToggled.emit(project);
  }

  onProjectSelectionChange(project: ProjectGroup, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    project.isSelected = checkbox.checked;
    
    // 同步更新所有人員的選中狀態
    project.persons.forEach(person => {
      person.isVisible = checkbox.checked;
    });
    
    this.projectSelectionChanged.emit({ 
      project, 
      isSelected: checkbox.checked 
    });
  }

  onPersonVisibilityChange(event: { personId: number; isVisible: boolean }, project: ProjectGroup): void {
    // 更新人員狀態
    const person = project.persons.find(p => p.personId === event.personId);
    if (person) {
      person.isVisible = event.isVisible;
    }
    
    // 檢查是否所有人員都被選中，同步更新專案選中狀態
    project.isSelected = project.persons.every(p => p.isVisible);
    
    this.personVisibilityChanged.emit({
      projectId: project.projectId,
      personId: event.personId,
      isVisible: event.isVisible
    });
  }
}