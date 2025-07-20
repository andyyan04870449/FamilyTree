import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/welcome/welcome.page').then(m => m.WelcomeComponent)
  },
  // 案件管理模組
  {
    path: 'file-upload',
    loadComponent: () => import('./pages/file-upload/file-upload.page').then(m => m.FileUploadComponent)
  },
  {
    path: 'file-management',
    loadComponent: () => import('./pages/file-management/file-management.page').then(m => m.FileManagementComponent)
  },
  {
    path: 'person-management',
    loadComponent: () => import('./pages/person-management/person-management.page').then(m => m.PersonManagementComponent)
  },
  {
    path: 'person-list',
    loadComponent: () => import('./pages/person-list/person-list.page').then(m => m.PersonListComponent)
  },
  // 全文檢索模組
  {
    path: 'family-tree',
    loadComponent: () => import('./pages/family-tree/family-tree.page').then(m => m.FamilyTreeComponent)
  },
  // 視覺化分析模組
  {
    path: 'relationship-graph',
    loadComponent: () => import('./pages/relationship-graph/relationship-graph.page').then(m => m.RelationshipGraphComponent)
  },
  {
    path: 'organization-chart',
    loadComponent: () => import('./pages/organization-chart/organization-chart.page').then(m => m.OrganizationChartComponent)
  },
  // 系統管理模組
  {
    path: 'system-settings',
    loadComponent: () => import('./pages/system-settings/system-settings.page').then(m => m.SystemSettingsComponent)
  }
];
