import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Observable, of, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CurrentUser, LoginRequest, Store } from './auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private readonly apiUrl = environment.apiUrl;

  readonly currentUser = signal<CurrentUser | null>(null);
  readonly selectedStore = signal<Store | null>(null);

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<unknown> {
    return this.http.post(
      `${this.apiUrl}/api/auth/login`,
      request,
      {
        withCredentials: true
      }
    );
  }

  loadCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(
      `${this.apiUrl}/api/auth/me`,
      {
        withCredentials: true
      }
    ).pipe(
      tap(user => {
        this.currentUser.set(user);
      })
    );
  }

  getStores(): Observable<Store[]> {
    return this.http.get<Store[]>(
      `${this.apiUrl}/api/stores`,
      {
        withCredentials: true
      }
    );
  }

  logout(): Observable<unknown> {
    return this.http.post(
      `${this.apiUrl}/api/auth/logout`,
      {},
      {
        withCredentials: true
      }
    ).pipe(
      tap(() => {
        this.currentUser.set(null);
        this.selectedStore.set(null);

        sessionStorage.removeItem('selectedStoreId');
      })
    );
  }

  setSelectedStore(store: Store): void {

    this.selectedStore.set(store);

    sessionStorage.setItem(
      'selectedStoreId',
      store.id.toString()
    );
  }

  // restores the store the user previously selected after the app starts/reloads.
  restoreSelectedStore(): Observable<Store | null> {

    const selectedStoreId =
      sessionStorage.getItem('selectedStoreId');

    if (!selectedStoreId) {
      return of(null);
    }

    return new Observable<Store | null>(observer => {

      this.getStores().subscribe({

        next: stores => {

          const store = stores.find(
            x => x.id === Number(selectedStoreId)
          );

          if (store) {
            this.selectedStore.set(store);
            observer.next(store);
          } else {
            sessionStorage.removeItem('selectedStoreId');
            this.selectedStore.set(null);
            observer.next(null);
          }

          observer.complete();
        },

        error: error => {
          observer.error(error);
        }

      });

    });
  }
}