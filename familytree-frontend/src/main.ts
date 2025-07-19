import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, withViewTransitions, withInMemoryScrolling, withComponentInputBinding } from '@angular/router';
import { ModuleRegistry, AllCommunityModule } from 'ag-grid-community';
import { App } from './app/app';
import { routes } from './app/app.routes';

// 註冊 AG Grid 模組
ModuleRegistry.registerModules([AllCommunityModule]);

bootstrapApplication(App, {
  providers: [
    provideHttpClient(),
    provideRouter(routes, withViewTransitions(), withInMemoryScrolling(), withComponentInputBinding())
  ]
}).catch((err) => console.error(err));
