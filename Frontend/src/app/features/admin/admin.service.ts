import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AdminOrderKpis,
  AdminShopifyHealth,
  CreateAdminUserRequest,
  AdminUserListItem,
  CreateAdminStoreRequest,
} from './admin.models';

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getOrderKpis(): Observable<AdminOrderKpis> {
    return this.http.get<AdminOrderKpis>(`${this.apiUrl}/api/admin/order-kpis`, {
      withCredentials: true,
    });
  }

  getShopifyHealth(): Observable<AdminShopifyHealth[]> {
    return this.http.get<AdminShopifyHealth[]>(`${this.apiUrl}/api/admin/shopify-health`, {
      withCredentials: true,
    });
  }

  createUser(request: CreateAdminUserRequest): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/api/admin/users`, request, {
      withCredentials: true,
    });
  }

  getUsers(): Observable<AdminUserListItem[]> {
    return this.http.get<AdminUserListItem[]>(`${this.apiUrl}/api/admin/users`, {
      withCredentials: true,
    });
  }

  updateUserActiveStatus(userId: string, isActive: boolean): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/api/admin/users/${userId}/active`, isActive, {
      withCredentials: true,
    });
  }

  updateUserRole(userId: string, role: string): Observable<unknown> {
    return this.http.put(
      `${this.apiUrl}/api/admin/users/${userId}/role`,
      { role },
      {
        withCredentials: true,
      },
    );
  }

  updateUserStores(userId: string, storeIds: number[]): Observable<unknown> {
    return this.http.put(
      `${this.apiUrl}/api/admin/users/${userId}/stores`,
      { storeIds },
      {
        withCredentials: true,
      },
    );
  }

  createStore(request: CreateAdminStoreRequest): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/api/admin/stores`, request, {
      withCredentials: true,
    });
  }

  syncStore(storeId: number): Observable<unknown> {
    return this.http.post(
      `${this.apiUrl}/api/admin/stores/${storeId}/sync`,
      {},
      {
        withCredentials: true,
      },
    );
  }
}
