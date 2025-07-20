import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/welcome/welcome.page').then(m => m.WelcomeComponent)
  },
  // 視覺化分析模組
  {
    path: 'family-tree',
    loadComponent: () => import('./pages/family-tree/family-tree.page').then(m => m.FamilyTreeComponent)
  },
  {
    path: 'person-list',
    loadComponent: () => import('./pages/person-list/person-list.page').then(m => m.PersonListComponent)
  },
  // 檔案上傳模組
  {
    path: 'file-upload',
    loadComponent: () => import('./pages/file-upload/file-upload.page').then(m => m.FileUploadComponent)
  }
];
