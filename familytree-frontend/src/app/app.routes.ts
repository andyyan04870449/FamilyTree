import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { requirePermissions, requireAdmin } from './guards/permission.guard';

export const routes: Routes = [
  // 登入頁面（不需要認證）
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.page').then(m => m.LoginPage)
  },
  // 預設重定向到關鍵字檢索
  {
    path: '',
    redirectTo: '/full-text-search',
    pathMatch: 'full'
  },
  // 1. 關鍵字檢索（原全文檢索）
  {
    path: 'full-text-search',
    loadComponent: () => import('./pages/full-text-search/full-text-search.page').then(m => m.FullTextSearchPage),
    canActivate: [AuthGuard]
  },
  // 2. 視覺化分析模組
  {
    path: 'visual-analysis',
    loadComponent: () => import('./pages/visual-analysis/visual-analysis.page').then(m => m.VisualAnalysisComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'visual-analysis/:id/editor',
    loadComponent: () => import('./pages/visual-analysis-editor/visual-analysis-editor.page').then(m => m.VisualAnalysisEditorComponent),
    canActivate: [AuthGuard]
  },
  // 3. 案件管理模組
  {
    path: 'case-management',
    loadComponent: () => import('./pages/case-management/case-management.page').then(m => m.CaseManagementComponent),
    canActivate: [AuthGuard]
  },
  // 4. 系統管理模組
  {
    path: 'system-settings',
    loadComponent: () => import('./pages/system-settings/system-settings.page').then(m => m.SystemSettingsComponent),
    canActivate: [AuthGuard],
    data: { role: 'admin' } // 只有管理員可以訪問
  },
  {
    path: 'user-management',
    loadComponent: () => import('./pages/user-management/user-management.page').then(m => m.UserManagementComponent),
    canActivate: [AuthGuard],
    data: { role: 'admin' } // 只有管理員可以訪問
  },
  {
    path: 'role-management',
    loadComponent: () => import('./pages/role-management/role-management.page').then(m => m.RoleManagementPage),
    ...requirePermissions(['role:manage'])
  },
  {
    path: 'permission-settings',
    loadComponent: () => import('./pages/permission-settings/permission-settings.page').then(m => m.PermissionSettingsPage),
    canActivate: [AuthGuard],
    data: { role: 'admin' } // 只有管理員可以訪問
  },
  
  // === 以下路由保留但隱藏，供內部功能使用 ===
  {
    path: 'file-upload',
    loadComponent: () => import('./pages/file-upload/file-upload.page').then(m => m.FileUploadComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'person-list',
    loadComponent: () => import('./pages/person-list/person-list.page').then(m => m.PersonListComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'relationship-graph',
    loadComponent: () => import('./pages/relationship-graph/relationship-graph.page').then(m => m.RelationshipGraphPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'relationship-graph/:personIds',
    loadComponent: () => import('./pages/relationship-graph/relationship-graph.page').then(m => m.RelationshipGraphPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'organization-chart',
    loadComponent: () => import('./pages/organization-chart/organization-chart.page').then(m => m.OrganizationChartComponent),
    canActivate: [AuthGuard]
  },
  {
    path: 'organization-chart-g6',
    loadComponent: () => import('./pages/organization-chart-g6/organization-chart-g6.page').then(m => m.OrganizationChartG6Page),
    canActivate: [AuthGuard]
  },
  {
    path: 'organization-chart-d3',
    loadComponent: () => import('./pages/organization-chart-d3/organization-chart-d3.page').then(m => m.OrganizationChartD3Page),
    canActivate: [AuthGuard]
  },
  {
    path: 'project-management',
    loadComponent: () => import('./pages/project-management/project-management.page').then(m => m.ProjectManagementComponent),
    canActivate: [AuthGuard]
  }
];
