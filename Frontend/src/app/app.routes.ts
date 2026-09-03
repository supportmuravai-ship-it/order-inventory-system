import { Routes } from '@angular/router';

import { LoginComponent } from './core/auth/login/login';

import { StoreSelectionComponent } from './features/stores/store-selection/store-selection';

import { UnauthorizedComponent } from './core/auth/unauthorized/unauthorized/unauthorized';
import { WorkspaceComponent } from './features/workspace/workspace/workspace';

import { authGuard } from './core/guards/auth.guard';

import { storeGuard } from './core/guards/store.guard';
import { AdminComponent } from './features/admin/admin/admin';
import { adminGuard } from './core/guards/role.guard';
import { OrderDetailsComponent } from './features/orders/order-details/order-details';
import { ImportOrdersComponent } from './features/orders/import-orders/import-orders';
import { CreateOrderComponent } from './features/orders/create-order/create-order';
import { TicketsPageComponent } from './features/Tickets/tickets-page/tickets-page';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent,
  },

  {
    path: 'stores',
    component: StoreSelectionComponent,
    canActivate: [authGuard],
  },

  {
    path: 'workspace',
    component: WorkspaceComponent,
    canActivate: [authGuard, storeGuard],
  },

  {
    path: 'admin',
    component: AdminComponent,
    canActivate: [authGuard, adminGuard],
  },

  {
    path: 'unauthorized',
    component: UnauthorizedComponent,
  },

  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full',
  },

  {
    path: 'workspace/orders/:orderId',
    component: OrderDetailsComponent,
    canActivate: [authGuard, storeGuard],
  },

  {
    path: 'workspace/import-orders',
    component: ImportOrdersComponent,
    canActivate: [authGuard, storeGuard],
  },

  {
  path: 'workspace/create-order',
  component: CreateOrderComponent,
  canActivate: [
    authGuard,
    storeGuard
  ]
},

{
  path: 'workspace/tickets',
  component: TicketsPageComponent,
  canActivate: [authGuard, storeGuard],
},

  {
    path: '**',
    redirectTo: 'login',
  },
];
