import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';
export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },
  {
    path: 'account',
    loadChildren: () => import('@abp/ng.account').then(c => c.createRoutes()),
  },
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(c => c.createRoutes()),
  },
  {
    path: 'tenant-management',
    loadChildren: () => import('@abp/ng.tenant-management').then(c => c.createRoutes()),
  },
  {
    path: 'setting-management',
    loadChildren: () => import('@abp/ng.setting-management').then(c => c.createRoutes()),
  },
  {
    path: 'books',
    loadComponent: () => import('./book/book.component').then(c => c.BookComponent),
    canActivate: [authGuard, permissionGuard],
  },
  {
    path: 'authors',
    loadComponent: () => import('./author/author.component').then(c => c.AuthorComponent),
    canActivate: [authGuard, permissionGuard],
  },
  {
    // TP10 (RF-11): "Mi despensa". Sólo pide sesión (authGuard): sin sesión redirige
    // al login de ABP. No usa permissionGuard porque cualquier usuario autenticado
    // administra su propia despensa; el aislamiento por usuario lo hace el servidor.
    path: 'pantry',
    loadComponent: () => import('./pantry/pantry.component').then(c => c.PantryComponent),
    canActivate: [authGuard],
  },
];
