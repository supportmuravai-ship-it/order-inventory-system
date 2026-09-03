import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AssignableTicketUser,
  CreateTicketRequest,
  PagedTickets,
  TicketDetails,
  TicketQuery,
  UpdateTicketAssignmentRequest,
  CreateTicketFromPageRequest
} from './ticket.models';

@Injectable({
  providedIn: 'root'
})
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getTickets(storeId: number, query: TicketQuery = {}): Observable<PagedTickets> {
    let params = new HttpParams()
      .set('storeId', storeId)
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 25);

    if (query.status !== undefined) {
      params = params.set('status', query.status);
    }
    if (query.search) {
  params = params.set('search', query.search);
}

    if (query.assignedToUserId) {
      params = params.set('assignedToUserId', query.assignedToUserId);
    }

    return this.http.get<PagedTickets>(`${this.apiUrl}/api/tickets`, {
      params,
      withCredentials: true,
    });
  }

  getTicket(id: number): Observable<TicketDetails> {
    return this.http.get<TicketDetails>(`${this.apiUrl}/api/tickets/${id}`, {
      withCredentials: true,
    });
  }

  createTicketFromPage(
  storeId: number,
  request: CreateTicketFromPageRequest,
): Observable<{ id: number }> {
  const params = new HttpParams().set('storeId', storeId);

  return this.http.post<{ id: number }>(
    `${this.apiUrl}/api/tickets`,
    request,
    {
      params,
      withCredentials: true,
    },
  );
}

  getMyOpenCount(storeId: number): Observable<number> {
    const params = new HttpParams().set('storeId', storeId);

    return this.http.get<number>(`${this.apiUrl}/api/tickets/my-open-count`, {
      params,
      withCredentials: true,
    });
  }

  getAssignableUsers(storeId: number): Observable<AssignableTicketUser[]> {
    const params = new HttpParams().set('storeId', storeId);

    return this.http.get<AssignableTicketUser[]>(
      `${this.apiUrl}/api/tickets/assignable-users`,
      {
        params,
        withCredentials: true,
      },
    );
  }

  createTicket(orderId: number, request: CreateTicketRequest): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(
      `${this.apiUrl}/api/orders/${orderId}/tickets`,
      request,
      {
        withCredentials: true,
      },
    );
  }

  closeTicket(id: number): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/api/tickets/${id}/close`,
      {},
      {
        withCredentials: true,
      },
    );
  }

  updateAssignment(id: number, request: UpdateTicketAssignmentRequest): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/api/tickets/${id}/assignment`,
      request,
      {
        withCredentials: true,
      },
    );
  }
}