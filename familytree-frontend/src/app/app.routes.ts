import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/welcome').then(m => m.WelcomeComponent)
  },
  // 視覺化分析模組
  {
    path: 'family-tree',
    loadComponent: () => import('./pages/family-tree').then(m => m.FamilyTreeComponent)
  },
  {
    path: 'person-list',
    loadComponent: () => import('./pages/person-list').then(m => m.PersonListComponent)
  }
];
