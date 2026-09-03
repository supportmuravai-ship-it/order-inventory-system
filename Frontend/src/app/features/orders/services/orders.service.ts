import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  OrderDetails,
  OrderListItem,
  PagedResult,
  OrderQuery,
  OrderSummary,
  CsvImportResult,
  CreateManualOrderRequest,
  CreateManualOrderResult,
} from '../models/order.model';

@Injectable({
  providedIn: 'root',
})
export class OrdersService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getOrders(storeId: number, query: OrderQuery): Observable<PagedResult<OrderListItem>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    if (query.dateFrom) {
      params = params.set('dateFrom', query.dateFrom);
    }

    if (query.dateTo) {
      params = params.set('dateTo', query.dateTo);
    }

    if (query.orderStatus !== undefined) {
      params = params.set('orderStatus', query.orderStatus);
    }

    if (query.product?.trim()) {
      params = params.set('product', query.product.trim());
    }

    if (query.sku?.trim()) {
      params = params.set('sku', query.sku.trim());
    }

    if (query.orderSource !== undefined) {
      params = params.set('orderSource', query.orderSource);
    }

    if (query.invoiceStatus !== undefined) {
      params = params.set('invoiceStatus', query.invoiceStatus);
    }

    if (query.needsAttention !== undefined) {
      params = params.set('needsAttention', query.needsAttention);
    }

    if (query.needToShip !== undefined) {
      params = params.set('needToShip', query.needToShip);
    }

    if (query.sort) {
      params = params.set('sort', query.sort);
    }

    return this.http.get<PagedResult<OrderListItem>>(
      `${this.apiUrl}/api/stores/${storeId}/orders`,
      {
        params,
        withCredentials: true,
      },
    );
  }

  getOrder(storeId: number, orderId: number): Observable<OrderDetails> {
    return this.http.get<OrderDetails>(`${this.apiUrl}/api/stores/${storeId}/orders/${orderId}`, {
      withCredentials: true,
    });
  }

  updateOrderStatus(
    storeId: number,
    orderId: number,
    orderStatus: number,
    reason?: string,
    evidenceUrl?: string,
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/status`,
      {
        orderStatus,
        reason,
        evidenceUrl,
      },
      {
        withCredentials: true,
      },
    );
  }
  updateNeedToShip(storeId: number, orderId: number, needToShip: boolean): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/need-to-ship`,
      {
        needToShip,
      },
      {
        withCredentials: true,
      },
    );
  }

  updateTrackingNumber(
    storeId: number,
    orderId: number,
    trackingNumber: string | null,
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/tracking`,
      {
        trackingNumber,
      },
      {
        withCredentials: true,
      },
    );
  }

  updateInvoiceStatus(storeId: number, orderId: number, invoiceStatus: number): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/invoice-status`,
      {
        invoiceStatus,
      },
      {
        withCredentials: true,
      },
    );
  }

  updateLocationLink(
    storeId: number,
    orderId: number,
    locationLink: string | null,
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/location-link`,
      {
        locationLink,
      },
      {
        withCredentials: true,
      },
    );
  }

  updateFinalDecision(
    storeId: number,
    orderId: number,
    finalDecision: string | null,
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/final-decision`,
      {
        finalDecision,
      },
      {
        withCredentials: true,
      },
    );
  }

  updateShoaibNote(storeId: number, orderId: number, text: string | null): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/shoaib-note`,
      {
        text,
      },
      {
        withCredentials: true,
      },
    );
  }

  updateTrenvoNote(storeId: number, orderId: number, text: string | null): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/trenvo-note`,
      {
        text,
      },
      {
        withCredentials: true,
      },
    );
  }
  getSummary(storeId: number, dateFrom?: string, dateTo?: string): Observable<OrderSummary> {
    let params = new HttpParams();

    if (dateFrom) {
      params = params.set('dateFrom', dateFrom);
    }

    if (dateTo) {
      params = params.set('dateTo', dateTo);
    }

    return this.http.get<OrderSummary>(`${this.apiUrl}/api/stores/${storeId}/orders/summary`, {
      params,
      withCredentials: true,
    });
  }

  importCsv(storeId: number, file: File): Observable<CsvImportResult> {
    const formData = new FormData();

    formData.append('file', file);

    return this.http.post<CsvImportResult>(
      `${this.apiUrl}/api/stores/${storeId}/orders/import-csv`,
      formData,
      {
        withCredentials: true,
      },
    );
  }

  createManualOrder(
    storeId: number,
    request: CreateManualOrderRequest,
  ): Observable<CreateManualOrderResult> {
    return this.http.post<CreateManualOrderResult>(
      `${this.apiUrl}/api/stores/${storeId}/orders`,
      request,
      {
        withCredentials: true,
      },
    );
  }

  updateAirwayBill(
    storeId: number,
    orderId: number,
    airwayBillUrl: string | null,
  ): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/api/stores/${storeId}/orders/${orderId}/airway-bill`,
      {
        airwayBillUrl,
      },
      {
        withCredentials: true,
      },
    );
  }
}
