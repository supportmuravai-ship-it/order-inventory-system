import { CommonModule } from '@angular/common';
import {
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { OrdersService } from '../../orders/services/orders.service';
import { OrderListItem } from '../../orders/models/order.model';

@Component({
  selector: 'app-workspace',
  standalone: true,
  imports: [
    CommonModule
  ],
  templateUrl: './workspace.html',
})
export class WorkspaceComponent implements OnInit {

  readonly authService = inject(AuthService);

  private readonly ordersService = inject(OrdersService);
  private readonly router = inject(Router);

  readonly orders = signal<OrderListItem[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.loadOrders();
  }

  private loadOrders(): void {

    const store = this.authService.selectedStore();

    if (!store) {
      this.router.navigate(['/stores']);
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.ordersService.getOrders(store.id)
      .subscribe({
        next: orders => {
          this.orders.set(orders);
          this.loading.set(false);
        },

        error: () => {
          this.loading.set(false);
          this.errorMessage.set(
            'Could not load orders.'
          );
        }
      });
  }

  changeStore(): void {

    this.authService.selectedStore.set(null);

    sessionStorage.removeItem('selectedStoreId');

    this.router.navigate(['/stores']);
  }

  logout(): void {

    this.authService.logout()
      .subscribe({
        next: () => {
          this.router.navigate(['/login']);
        }
      });
  }

  getStatusName(status: number): string {

    const statuses: Record<number, string> = {
      0: 'Confirmed',
      1: 'Shipped',
      2: 'Delivered',
      3: 'No Response',
      4: 'Return',
      5: 'Return In Process',
      6: 'Cancelled',
      7: 'Repeated Order'
    };

    return statuses[status] ?? 'Unknown';
  }

  getStatusClasses(status: number): string {

  const classes: Record<number, string> = {
    0: 'bg-purple-100 text-purple-700',  // Confirmed
    1: 'bg-blue-100 text-blue-700',      // Shipped
    2: 'bg-green-100 text-green-700',    // Delivered
    3: 'bg-orange-100 text-orange-700',  // No Response
    4: 'bg-red-100 text-red-700',        // Return
    5: 'bg-yellow-100 text-yellow-700',  // Return In Process
    6: 'bg-gray-200 text-gray-700',      // Cancelled
    7: 'bg-pink-100 text-pink-700'       // Repeated Order
  };

  return classes[status] ?? 'bg-gray-100 text-gray-700';
}

  getSourceName(source: number): string {

    const sources: Record<number, string> = {
      0: 'Shopify',
      1: 'CSV Import',
      2: 'WhatsApp',
      3: 'Other'
    };

    return sources[source] ?? 'Unknown';
  }

  getInvoiceName(status: number): string {
    return status === 0
      ? 'Paid'
      : 'Unpaid';
  }
}