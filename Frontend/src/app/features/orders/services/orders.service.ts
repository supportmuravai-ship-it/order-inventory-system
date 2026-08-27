import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { OrderDetails, OrderListItem } from '../models/order.model';

@Injectable({
  providedIn: 'root',
})
export class OrdersService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getOrders(storeId: number): Observable<OrderListItem[]> {
    return this.http.get<OrderListItem[]>(`${this.apiUrl}/api/stores/${storeId}/orders`, {
      withCredentials: true,
    });
  }

  getOrder(storeId: number, orderId: number): Observable<OrderDetails> {
    return this.http.get<OrderDetails>(`${this.apiUrl}/api/stores/${storeId}/orders/${orderId}`, {
      withCredentials: true,
    });
  }
}
