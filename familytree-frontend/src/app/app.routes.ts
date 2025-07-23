import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/project-management/project-management.page').then(m => m.ProjectManagementComponent)
  },
  // 案件管理模組
  {
    path: 'file-upload',
    loadComponent: () => import('./pages/file-upload/file-upload.page').then(m => m.FileUploadComponent)
  },
  {
    path: 'person-list',
    loadComponent: () => import('./pages/person-list/person-list.page').then(m => m.PersonListComponent)
  },
  // 全文檢索模組
  {
    path: 'full-text-search',
    loadComponent: () => import('./pages/full-text-search/full-text-search.page').then(m => m.FullTextSearchPage)
  },
  // 視覺化分析模組
  {
    path: 'relationship-graph',
    loadComponent: () => import('./pages/relationship-graph/relationship-graph.page').then(m => m.RelationshipGraphPage)
  },
  {
    path: 'relationship-graph/:personIds',
    loadComponent: () => import('./pages/relationship-graph/relationship-graph.page').then(m => m.RelationshipGraphPage)
  },
  {
    path: 'organization-chart',
    loadComponent: () => import('./pages/organization-chart/organization-chart.page').then(m => m.OrganizationChartComponent)
  },
  // 專案管理模組
  {
    path: 'project-management',
    loadComponent: () => import('./pages/project-management/project-management.page').then(m => m.ProjectManagementComponent)
  },
  // 系統管理模組
  {
    path: 'system-settings',
    loadComponent: () => import('./pages/system-settings/system-settings.page').then(m => m.SystemSettingsComponent)
  }
];
