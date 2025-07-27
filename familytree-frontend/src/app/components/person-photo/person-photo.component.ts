// 人員照片組件 - 提供統一的照片顯示功能
// 主要功能：照片顯示、錯誤處理、預設頭像、多種尺寸支援

import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PhotoUtilsService, PersonPhotoConfig, PhotoUrlResult } from '../../services/photo-utils.service';

@Component({
  selector: 'app-person-photo',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="person-photo-container" [ngClass]="containerClasses">
      <img 
        *ngIf="photoResult.url" 
        [src]="photoResult.url" 
        [alt]="altText"
        [ngClass]="imageClasses"
        (error)="onPhotoError($event)"
        [style.display]="imageVisible ? 'block' : 'none'"
      >
      
      <div 
        *ngIf="!photoResult.url || !imageVisible" 
        class="photo-placeholder"
        [ngClass]="placeholderClasses"
        [title]="placeholderTitle"
      >
        <span class="placeholder-text">{{ photoResult.fallbackText }}</span>
      </div>
      
      <!-- 載入狀態 -->
      <div *ngIf="loading" class="photo-loading">
        <div class="loading-spinner"></div>
      </div>
    </div>
  `,
  styleUrls: ['./person-photo.component.scss']
})
export class PersonPhotoComponent implements OnInit, OnChanges {
  @Input() photoIndex: string | null | undefined = null;
  @Input() personName: string = '';
  @Input() projectId?: string;
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
  @Input() shape: 'circle' | 'square' | 'rounded' = 'rounded';
  @Input() showTooltip: boolean = true;
  @Input() customClass: string = '';
  @Input() clickable: boolean = false;
  @Input() borderColor: string = '';

  photoResult: PhotoUrlResult = {
    url: null,
    isValid: false,
    fallbackText: '?',
    errorMessage: undefined
  };

  loading = false;
  imageVisible = true;

  constructor(private photoUtils: PhotoUtilsService) {}

  ngOnInit(): void {
    this.loadPhoto();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['photoIndex'] || changes['personName'] || changes['projectId']) {
      this.loadPhoto();
    }
  }

  private loadPhoto(): void {
    this.loading = true;
    this.imageVisible = true;

    const config: PersonPhotoConfig = {
      photoIndex: this.photoIndex,
      personName: this.personName,
      projectId: this.projectId,
      size: this.size
    };

    this.photoResult = this.photoUtils.getPersonPhotoResult(config);
    this.loading = false;
  }

  onPhotoError(event: Event): void {
    this.imageVisible = false;
    this.photoUtils.handlePhotoError(event);
  }

  get containerClasses(): string[] {
    const classes = [
      'person-photo',
      `photo-${this.size}`,
      `photo-${this.shape}`
    ];

    if (this.clickable) {
      classes.push('photo-clickable');
    }

    if (this.customClass) {
      classes.push(this.customClass);
    }

    if (this.borderColor) {
      classes.push('photo-bordered');
    }

    return classes;
  }

  get imageClasses(): string[] {
    return [
      'photo-image',
      `image-${this.size}`,
      `image-${this.shape}`
    ];
  }

  get placeholderClasses(): string[] {
    return [
      'placeholder',
      `placeholder-${this.size}`,
      `placeholder-${this.shape}`
    ];
  }

  get altText(): string {
    return this.personName ? `${this.personName}的照片` : '人員照片';
  }

  get placeholderTitle(): string {
    if (!this.showTooltip) return '';
    
    if (this.photoResult.errorMessage) {
      return `照片載入失敗: ${this.photoResult.errorMessage}`;
    }
    
    return this.personName ? `${this.personName}` : '無照片';
  }
} 